// Encoding.Typed, the one value encoding of the modding runtime (abi/mod-abi.fbs header):
// compact little-endian binary, laid out by the value's WIT type. The primitives live
// here; a host supplies the per-type codecs (usually generated from its WIT records) and
// registers them on its ModComponentRegistry (RegisterBinary* / RegisterTag*). Every
// value the runtime holds (event buffers, resource change detection, observer fires,
// command payloads) and every value the relay carries is in this encoding.

using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace TinyEcs.Bevy.Modding;

/// Writes one Encoding.Typed value. `writer` is appended to.
public delegate void ModBinaryWrite<T>(in T value, IBufferWriter<byte> writer);

/// Reads one Encoding.Typed value. `resolve` (null outside a guest's command buffer) maps
/// a placeholder in an entity field to the entity the same buffer spawned.
public delegate T ModBinaryRead<T>(ref ModBinaryReader reader, ModEntityResolver? resolve);

/// Encoding.Typed primitives (abi/mod-abi.fbs).
public static class ModBinary
{
    public static void Bool(IBufferWriter<byte> w, bool v) => U8(w, v ? (byte)1 : (byte)0);

    public static void U8(IBufferWriter<byte> w, byte v)
    {
        w.GetSpan(1)[0] = v;
        w.Advance(1);
    }

    public static void S8(IBufferWriter<byte> w, sbyte v) => U8(w, (byte)v);

    public static void U16(IBufferWriter<byte> w, ushort v)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(w.GetSpan(2), v);
        w.Advance(2);
    }

    public static void S16(IBufferWriter<byte> w, short v) => U16(w, (ushort)v);

    public static void U32(IBufferWriter<byte> w, uint v)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(w.GetSpan(4), v);
        w.Advance(4);
    }

    public static void S32(IBufferWriter<byte> w, int v) => U32(w, (uint)v);

    public static void U64(IBufferWriter<byte> w, ulong v)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(w.GetSpan(8), v);
        w.Advance(8);
    }

    public static void S64(IBufferWriter<byte> w, long v) => U64(w, (ulong)v);

    public static void F32(IBufferWriter<byte> w, float v)
    {
        BinaryPrimitives.WriteSingleLittleEndian(w.GetSpan(4), v);
        w.Advance(4);
    }

    public static void F64(IBufferWriter<byte> w, double v)
    {
        BinaryPrimitives.WriteDoubleLittleEndian(w.GetSpan(8), v);
        w.Advance(8);
    }

    public static void Char(IBufferWriter<byte> w, char v) => U32(w, v);

    /// u32 byte length + UTF-8 (null = "").
    public static void String(IBufferWriter<byte> w, string? v)
    {
        v ??= string.Empty;
        var max = Encoding.UTF8.GetMaxByteCount(v.Length);
        var span = w.GetSpan(4 + max);
        var n = Encoding.UTF8.GetBytes(v, span[4..]);
        BinaryPrimitives.WriteUInt32LittleEndian(span, (uint)n);
        w.Advance(4 + n);
    }

    /// A list's / string's u32 count.
    public static void Count(IBufferWriter<byte> w, int n) => U32(w, (uint)n);
}

/// Reads an Encoding.Typed value front to back. Every read is bounds-checked: a
/// truncated payload throws (the command applier logs and skips that one command).
public ref struct ModBinaryReader(ReadOnlySpan<byte> data)
{
    private readonly ReadOnlySpan<byte> _data = data;
    private int _pos;

    /// Bytes not read yet.
    public readonly int Remaining => _data.Length - _pos;

    private ReadOnlySpan<byte> Take(int n)
    {
        if ((uint)n > (uint)(_data.Length - _pos))
            throw new InvalidOperationException($"typed value truncated: {n} bytes wanted at {_pos} of {_data.Length}");
        var s = _data.Slice(_pos, n);
        _pos += n;
        return s;
    }

    public bool Bool() => Take(1)[0] != 0;
    public byte U8() => Take(1)[0];
    public sbyte S8() => (sbyte)Take(1)[0];
    public ushort U16() => BinaryPrimitives.ReadUInt16LittleEndian(Take(2));
    public short S16() => BinaryPrimitives.ReadInt16LittleEndian(Take(2));
    public uint U32() => BinaryPrimitives.ReadUInt32LittleEndian(Take(4));
    public int S32() => BinaryPrimitives.ReadInt32LittleEndian(Take(4));
    public ulong U64() => BinaryPrimitives.ReadUInt64LittleEndian(Take(8));
    public long S64() => BinaryPrimitives.ReadInt64LittleEndian(Take(8));
    public float F32() => BinaryPrimitives.ReadSingleLittleEndian(Take(4));
    public double F64() => BinaryPrimitives.ReadDoubleLittleEndian(Take(8));
    public char Char() => (char)U32();

    /// An entity field: a placeholder resolves through `resolve` when there is one.
    public ulong Entity(ModEntityResolver? resolve) => resolve != null ? resolve(U64()) : U64();

    public string String() => Encoding.UTF8.GetString(Take(Count()));

    /// A string's UTF-8 bytes (no decode).
    public ReadOnlySpan<byte> Utf8() => Take(Count());

    /// A list's u32 count, checked against what is left (an item is >= 1 byte except a
    /// zero-size one, which the WIT types never produce).
    public int Count()
    {
        var n = U32();
        if (n > (uint)Remaining)
            throw new InvalidOperationException($"typed value: count {n} past the end of the payload");
        return (int)n;
    }
}

/// Encoding.Typed access to one registered component (ModComponentRegistry.
/// RegisterBinaryComponent / RegisterTagComponent).
public interface IModBinaryComponent
{
    /// Presence, change detection, ReadOnly.
    IModComponent Component { get; }

    /// Writes `entity`'s value; false (nothing written) when it has none.
    bool TryWrite(World world, ulong entity, IBufferWriter<byte> writer);

    /// Inserts / overwrites the component from a value.
    void Set(World world, ulong entity, ReadOnlySpan<byte> data, ModEntityResolver? resolve);

    /// A host observer of the component's insert / remove, firing the value as bytes;
    /// `wanted` is asked first. Throws when the mapper can't hand its payload over
    /// (IModComponentObserverOf).
    void ObserveInsert(App app, Func<bool> wanted, ModValueFire onFire);

    void ObserveRemove(App app, Func<bool> wanted, ModValueFire onFire);
}

/// Encoding.Typed access to one registered event (RegisterBinaryEvent / RegisterTagEvent).
public interface IModBinaryEvent
{
    /// Emits the event from a value.
    void Emit(World world, ulong entity, ReadOnlySpan<byte> data, ModEntityResolver? resolve);

    /// A host observer firing each payload as bytes; `wanted` is asked first.
    void Observe(App app, Func<bool> wanted, ModValueFire onFire);
}

/// Encoding.Typed access to one registered resource. `modName` as in IModResourceOf.
public interface IModBinaryResource
{
    bool ReadOnly { get; }

    /// Writes the value; false (nothing written) when the host has none right now.
    bool TryWrite(App app, string modName, IBufferWriter<byte> writer);

    void Set(App app, ReadOnlySpan<byte> data, string modName);
}

internal sealed class ModBinaryComponent<T>(IModComponent presence, IModComponentOf<T> typed, ModBinaryWrite<T> write, ModBinaryRead<T> read)
    : IModBinaryComponent where T : struct
{
    // One fire at a time (the observers run on the scheduler thread; the receiver copies).
    private readonly ModByteBuffer _fire = new();

    public IModComponent Component => presence;

    public void ObserveInsert(App app, Func<bool> wanted, ModValueFire onFire)
        => Observed().ObserveInsert(app, wanted, (e, v) => Fire(onFire, e, v));

    public void ObserveRemove(App app, Func<bool> wanted, ModValueFire onFire)
        => Observed().ObserveRemove(app, wanted, (e, v) => Fire(onFire, e, v));

    private IModComponentObserverOf<T> Observed()
        => presence as IModComponentObserverOf<T>
            ?? throw new InvalidOperationException($"{presence.GetType().Name} can't be observed: no IModComponentObserverOf<{typeof(T).Name}>");

    private void Fire(ModValueFire onFire, ulong entity, in T value)
    {
        _fire.Reset();
        write(value, _fire);
        onFire(entity, _fire.WrittenSpan);
    }

    public bool TryWrite(World world, ulong entity, IBufferWriter<byte> writer)
    {
        if (!world.Exists(entity) || !presence.Has(world, entity))
            return false;
        write(typed.Get(world, entity), writer);
        return true;
    }

    public void Set(World world, ulong entity, ReadOnlySpan<byte> data, ModEntityResolver? resolve)
    {
        var r = new ModBinaryReader(data);
        typed.Set(world, entity, read(ref r, resolve));
    }
}

internal sealed class ModBinaryResource<T>(IModResource mapper, IModResourceOf<T> typed, ModBinaryWrite<T> write, ModBinaryRead<T> read) : IModBinaryResource
{
    public bool ReadOnly => mapper.ReadOnly;

    public bool TryWrite(App app, string modName, IBufferWriter<byte> writer)
    {
        if (!typed.TryGet(app, modName, out var value))
            return false;
        write(value, writer);
        return true;
    }

    public void Set(App app, ReadOnlySpan<byte> data, string modName)
    {
        var r = new ModBinaryReader(data);
        typed.Set(app, read(ref r, null), modName);
    }
}

internal sealed class ModBinaryEvent<T>(IModEventOf<T> typed, ModBinaryWrite<T> write, ModBinaryRead<T> read) : IModBinaryEvent where T : struct
{
    private readonly ModByteBuffer _fire = new();

    public void Emit(World world, ulong entity, ReadOnlySpan<byte> data, ModEntityResolver? resolve)
    {
        var r = new ModBinaryReader(data);
        typed.Emit(world, entity, read(ref r, resolve));
    }

    public void Observe(App app, Func<bool> wanted, ModValueFire onFire)
        => typed.Observe(app, wanted, (e, v) =>
        {
            _fire.Reset();
            write(v, _fire);
            onFire(e, _fire.WrittenSpan);
        });
}
