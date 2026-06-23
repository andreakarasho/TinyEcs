using Wasmtime;
using Xunit;

namespace TinyEcs.Bevy.Modding.Tests;

// Wasmtime.ComponentValue.ReadListOfPrimitives is the batch read the generated host-import
// binding emits for list<primitive> params (e.g. cuo:net/send's list<u8>): it strides the
// native value array directly instead of N managed ListBuilder-indexer reads (each of which
// copies a tagged-union ComponentValue by value + does a kind check). The risk is the union
// reinterpret + stride. These round-trip a list the same way the guest path does — build a
// list ComponentValue, then read it back via ToListBuilder() exactly like `args[i].ToListBuilder()`.
//
// Lives here (not in the fork's own Wasmtime.Tests) because that assembly has an assembly-wide
// fixture requiring a built wasm component (wasm/csharp.wasm) absent in this tree; this assembly
// is runnable and transitively references Wasmtime + copies the native dll.
public class ReadListOfPrimitivesTests
{
    [Fact]
    public void Bytes_roundtrip()
    {
        var builder = new ListBuilder(4); // moved into the list; do not dispose separately
        builder[0] = new ComponentValue((byte)0);
        builder[1] = new ComponentValue((byte)127);
        builder[2] = new ComponentValue((byte)200);
        builder[3] = new ComponentValue((byte)255);
        using var list = ComponentValue.CreateList(builder, externallyOwned: false);

        var view = list.ToListBuilder();
        var dst = new byte[view.Length];
        ComponentValue.ReadListOfPrimitives<byte>(view, dst);

        Assert.Equal(new byte[] { 0, 127, 200, 255 }, dst);
    }

    [Fact]
    public void UInt32_roundtrip_exercises_full_width_and_stride()
    {
        var builder = new ListBuilder(3);
        builder[0] = new ComponentValue(1u);
        builder[1] = new ComponentValue(0xDEADBEEFu);
        builder[2] = new ComponentValue(uint.MaxValue);
        using var list = ComponentValue.CreateList(builder, externallyOwned: false);

        var view = list.ToListBuilder();
        var dst = new uint[view.Length];
        ComponentValue.ReadListOfPrimitives<uint>(view, dst);

        Assert.Equal(new uint[] { 1u, 0xDEADBEEFu, uint.MaxValue }, dst);
    }

    [Fact]
    public void Empty_list_is_a_no_op()
    {
        var builder = new ListBuilder(0);
        using var list = ComponentValue.CreateList(builder, externallyOwned: false);

        var view = list.ToListBuilder();
        Assert.Equal(0, view.Length);
        ComponentValue.ReadListOfPrimitives<byte>(view, new byte[0]);
    }
}
