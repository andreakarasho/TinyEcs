using System.Buffers;
using TinyEcs.Bevy.Modding;
using Wasmtime;

namespace TinyEcs.Bevy.Modding.Tests;

// Host half of the fixture's `test:fixture/values` interface
// (fixtures/component-guest/wit/world.wit): every value crosses as its Encoding.Typed
// bytes (`list<u8>`), read and written through ComponentModImports' byte-level
// accessors on the call's own borrowed handles. Defined on the component linker from a
// ModdingConfig.ComponentImports callback.
internal static unsafe class TestValuesHost
{
    public const string Interface = "test:fixture/values@0.1.0";

    public static void Define(ComponentModImports imports)
    {
        using var i = imports.DefineInstance(Interface);
        i.DefineFunction("column", Column, imports);
        i.DefineFunction("set", Set, imports);
        i.DefineFunction("insert", Insert, imports);
        i.DefineFunction("set-resource", SetResource, imports);
        i.DefineFunction("send", Send, imports);
        i.DefineFunction("get", Get, imports);
        i.DefineFunction("put", Put, imports);
        i.DefineFunction("read", Read, imports);
    }

    private static ComponentModImports Imports(object? state) => (ComponentModImports)state!;

    private static byte[] Bytes(in ComponentValue v)
    {
        var list = v.ToListBuilder();
        var bytes = new byte[list.Length];
        ComponentValue.ReadListOfPrimitives(list, bytes.AsSpan());
        return bytes;
    }

    private static ComponentValue Value(ReadOnlySpan<byte> bytes)
    {
        var list = new ListBuilder(bytes.Length);
        for (var i = 0; i < bytes.Length; i++)
            list[i] = ComponentValue.CreateByte(bytes[i]);
        return new ComponentValue(list, externallyOwned: true);
    }

    private static ComponentValue Values(IReadOnlyList<byte[]> values)
    {
        var list = new ListBuilder(values.Count);
        for (var i = 0; i < values.Count; i++)
            list[i] = Value(values[i]);
        return new ComponentValue(list, externallyOwned: true);
    }

    // column(q, term, path) -> list<value>
    private static void Column(object? state, ComponentCallResults args, ComponentValue* results, StoreContext cx)
    {
        var q = Imports(state).Query(args[0].ToResourceRep(cx));
        var n = q.Entities.Length;
        var buf = new ArrayBufferWriter<byte>();
        var ends = new int[n];
        q.ColumnBytes(args[1].ToByte(), args[2].ToStringValue(), buf, ends);
        var list = new ListBuilder(n);
        var start = 0;
        for (var i = 0; i < n; i++)
        {
            list[i] = Value(buf.WrittenSpan[start..ends[i]]);
            start = ends[i];
        }
        results[0] = new ComponentValue(list, externallyOwned: true);
    }

    // set(q, e, term, path, v)
    private static void Set(object? state, ComponentCallResults args, ComponentValue* results, StoreContext cx)
        => Imports(state).Query(args[0].ToResourceRep(cx)).SetBytes(args[1].ToUInt64(), args[2].ToByte(), args[3].ToStringValue(), Bytes(args[4]));

    // insert(c, e, path, v)
    private static void Insert(object? state, ComponentCallResults args, ComponentValue* results, StoreContext cx)
        => Imports(state).Commands(args[0].ToResourceRep(cx)).InsertBytes(args[1].ToUInt64(), args[2].ToStringValue(), Bytes(args[3]));

    // set-resource(c, path, v)
    private static void SetResource(object? state, ComponentCallResults args, ComponentValue* results, StoreContext cx)
        => Imports(state).Commands(args[0].ToResourceRep(cx)).SetResourceBytes(args[1].ToStringValue(), Bytes(args[2]));

    // send(c, path, v)
    private static void Send(object? state, ComponentCallResults args, ComponentValue* results, StoreContext cx)
        => Imports(state).Commands(args[0].ToResourceRep(cx)).SendBytes(args[1].ToStringValue(), Bytes(args[2]));

    // get(r, path) -> option<value>
    private static void Get(object? state, ComponentCallResults args, ComponentValue* results, StoreContext cx)
    {
        var buf = new ArrayBufferWriter<byte>();
        results[0] = Imports(state).Res(args[0].ToResourceRep(cx)).TryGetBytes(args[1].ToStringValue(), buf)
            ? ComponentValue.CreateOption(Value(buf.WrittenSpan))
            : ComponentValue.CreateOption(null);
    }

    // put(r, path, v)
    private static void Put(object? state, ComponentCallResults args, ComponentValue* results, StoreContext cx)
        => Imports(state).Res(args[0].ToResourceRep(cx)).PutBytes(args[1].ToStringValue(), Bytes(args[2]));

    // read(e, path) -> list<value>
    private static void Read(object? state, ComponentCallResults args, ComponentValue* results, StoreContext cx)
        => results[0] = Values(Imports(state).Events(args[0].ToResourceRep(cx)).Read(args[1].ToStringValue()));
}
