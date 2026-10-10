using System.Buffers;
using TinyEcs.Bevy.Modding;

namespace TinyEcs.Bevy.Modding.Tests;

// The Encoding.Typed codecs a host would generate from its WIT records, hand-written for
// the test payloads, plus registry shorthands (register a mapper and its codec at once).

public struct WitPos { public int X { get; set; } public int Y { get; set; } }
public struct WitTag { } // zero-size marker
public struct WitScore { public int Value { get; set; } }
public struct WitPing { public int N { get; set; } } // custom event payload

public struct V3Score { public int Value; }
public struct V3Ping { public int N; }
public struct V3Link { public ulong Target; }
public struct V3Tag { public int X; }

public struct HardFlag { public bool On; }

internal static class Codecs
{
    public static void WritePos(in WitPos v, IBufferWriter<byte> w) { ModBinary.S32(w, v.X); ModBinary.S32(w, v.Y); }
    public static WitPos ReadPos(ref ModBinaryReader r, ModEntityResolver? _) => new() { X = r.S32(), Y = r.S32() };
    public static void WriteScore(in WitScore v, IBufferWriter<byte> w) => ModBinary.S32(w, v.Value);
    public static WitScore ReadScore(ref ModBinaryReader r, ModEntityResolver? _) => new() { Value = r.S32() };
    public static void WritePing(in WitPing v, IBufferWriter<byte> w) => ModBinary.S32(w, v.N);
    public static WitPing ReadPing(ref ModBinaryReader r, ModEntityResolver? _) => new() { N = r.S32() };

    public static void WriteV3Score(in V3Score v, IBufferWriter<byte> w) => ModBinary.S32(w, v.Value);
    public static V3Score ReadV3Score(ref ModBinaryReader r, ModEntityResolver? _) => new() { Value = r.S32() };
    public static void WriteV3Ping(in V3Ping v, IBufferWriter<byte> w) => ModBinary.S32(w, v.N);
    public static V3Ping ReadV3Ping(ref ModBinaryReader r, ModEntityResolver? _) => new() { N = r.S32() };
    public static void WriteV3Tag(in V3Tag v, IBufferWriter<byte> w) => ModBinary.S32(w, v.X);
    public static V3Tag ReadV3Tag(ref ModBinaryReader r, ModEntityResolver? _) => new() { X = r.S32() };
    // An entity field: a placeholder resolves through the command buffer's resolver.
    public static void WriteV3Link(in V3Link v, IBufferWriter<byte> w) => ModBinary.U64(w, v.Target);
    public static V3Link ReadV3Link(ref ModBinaryReader r, ModEntityResolver? resolve) => new() { Target = r.Entity(resolve) };

    public static void WriteFlag(in HardFlag v, IBufferWriter<byte> w) => ModBinary.Bool(w, v.On);
    public static HardFlag ReadFlag(ref ModBinaryReader r, ModEntityResolver? _) => new() { On = r.Bool() };
}

internal static class Bin
{
    public static byte[] Encode<T>(ModBinaryWrite<T> write, in T value)
    {
        var w = new ArrayBufferWriter<byte>();
        write(value, w);
        return w.WrittenSpan.ToArray();
    }

    public static T Decode<T>(ModBinaryRead<T> read, ReadOnlySpan<byte> bytes)
    {
        var r = new ModBinaryReader(bytes);
        return read(ref r, null);
    }

    public static byte[] S32(int v) => Encode(static (in int x, IBufferWriter<byte> w) => ModBinary.S32(w, x), v);
    public static byte[] S64(long v) => Encode(static (in long x, IBufferWriter<byte> w) => ModBinary.S64(w, x), v);
    public static byte[] U64(ulong v) => Encode(static (in ulong x, IBufferWriter<byte> w) => ModBinary.U64(w, x), v);

    public static byte[] Written(Action<IBufferWriter<byte>> write)
    {
        var w = new ArrayBufferWriter<byte>();
        write(w);
        return w.WrittenSpan.ToArray();
    }
}

internal static class RegistryTestExtensions
{
    public static ModComponentRegistry Component<T>(this ModComponentRegistry reg, string path, IModComponent mapper,
        ModBinaryWrite<T> write, ModBinaryRead<T> read) where T : struct
    {
        reg.Register(path, mapper);
        reg.RegisterBinaryComponent(path, write, read);
        return reg;
    }

    public static ModComponentRegistry Tag<T>(this ModComponentRegistry reg, string path, IModComponent mapper) where T : struct
    {
        reg.Register(path, mapper);
        reg.RegisterTagComponent<T>(path);
        return reg;
    }

    public static ModComponentRegistry Resource<T>(this ModComponentRegistry reg, string path, IModResource mapper,
        ModBinaryWrite<T> write, ModBinaryRead<T> read)
    {
        reg.RegisterResource(path, mapper);
        reg.RegisterBinaryResource(path, write, read);
        return reg;
    }

    public static ModComponentRegistry Event<T>(this ModComponentRegistry reg, string path,
        ModBinaryWrite<T> write, ModBinaryRead<T> read) where T : struct
    {
        reg.RegisterEvent(path, new ModEvent<T>());
        reg.RegisterBinaryEvent(path, write, read);
        return reg;
    }

    // The common test registrations.
    public static ModComponentRegistry Pos(this ModComponentRegistry reg, string path = "test/pos", bool readOnly = false)
        => reg.Component(path, new ModComponent<WitPos>(readOnly: readOnly), Codecs.WritePos, Codecs.ReadPos);

    public static ModComponentRegistry Flag(this ModComponentRegistry reg, string path = "test/flag", bool readOnly = false)
        => reg.Component(path, new ModComponent<HardFlag>(readOnly: readOnly), Codecs.WriteFlag, Codecs.ReadFlag);
}
