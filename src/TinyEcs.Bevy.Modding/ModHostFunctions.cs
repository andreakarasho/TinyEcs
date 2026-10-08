// Host functions a mod calls by name: everything outside the ECS contract (data
// files, game actions, raw messages, storage, ...). The HOST registers them; the lib
// only routes a call to the body. One wire for every target:
//   - the guest relay (abi/mod-abi.fbs): the single `env.mod_call(name_ptr, name_len,
//     args_ptr, args_len) -> i64` import of a relay executor; args and result are
//     UTF-8 JSON, the result written into the guest arena.
//   - components / tests: call Call(...) directly, or call the host's typed
//     methods behind the adapters without JSON at all.
//
// Names are "<package>/<interface>#<function>" without the version (the WIT names the
// host chose). Args = JSON array of the parameters in declaration order. A body writes
// ONE JSON value into the writer, or nothing for a function without a result.
// AOT-safe: no reflection serialization here; bodies use Utf8JsonWriter / JsonElement.

using System.Buffers;
using System.Text;
using System.Text.Json;

namespace TinyEcs.Bevy.Modding;

/// A host function body. `args` is the JSON array of the call's parameters; write the
/// result as ONE JSON value into `result`, or write nothing (unit). Throw
/// <see cref="ModCallException"/> (or let an args-shape exception escape) for a
/// malformed call — the calling guest traps with that message.
public delegate void ModHostFn(ModHostContext mod, JsonElement args, Utf8JsonWriter result);

/// A mod made a call the host can't serve (unknown function, malformed args). The
/// executor turns it into a guest trap.
public sealed class ModCallException(string message, Exception? inner = null) : Exception(message, inner);

/// The host-function table. Registered as a resource by <see cref="ModdingPlugin"/>;
/// a host adds its functions at plugin build time (before mods load).
public sealed class ModHostFunctions
{
    private const int MaxNameChars = 256;

    private readonly Dictionary<string, ModHostFn> _fns = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ModHostFn>.AlternateLookup<ReadOnlySpan<char>> _byChars;
    // The result of the call in flight: consumed (copied into the guest) before the
    // next call, and calls never nest (host bodies don't call mods).
    private readonly ArrayBufferWriter<byte> _out = new(256);
    private readonly Utf8JsonWriter _writer;

    public ModHostFunctions()
    {
        _byChars = _fns.GetAlternateLookup<ReadOnlySpan<char>>();
        _writer = new Utf8JsonWriter(_out);
    }

    /// Registers (or replaces) a function.
    public void Add(string name, ModHostFn fn) => _fns[name] = fn;

    public bool Contains(string name) => _fns.ContainsKey(name);

    /// Every registered name (diagnostics, typegen).
    public IReadOnlyCollection<string> Names => _fns.Keys;

    /// Calls `name` with UTF-8 JSON `args`. Returns the UTF-8 JSON result, EMPTY for a
    /// function without a result; valid until the next call. Throws
    /// <see cref="ModCallException"/> for an unknown name or malformed args.
    public ReadOnlySpan<byte> Call(ModHostContext mod, ReadOnlySpan<byte> nameUtf8, ReadOnlySpan<byte> argsUtf8)
    {
        if (nameUtf8.Length > MaxNameChars * 3)
            throw new ModCallException("mod_call: function name too long");
        Span<char> chars = stackalloc char[Encoding.UTF8.GetMaxCharCount(nameUtf8.Length)];
        var n = Encoding.UTF8.GetChars(nameUtf8, chars);
        if (!_byChars.TryGetValue(chars[..n], out var fn))
            throw new ModCallException($"mod_call: unknown host function '{chars[..n].ToString()}'");
        if (!ModProfiler.Enabled)
            return Invoke(mod, chars[..n], fn, argsUtf8);
        var t0 = ModProfiler.Now();
        var r = Invoke(mod, chars[..n], fn, argsUtf8);
        var stat = ModProfiler.Get(mod.Name, "fn:" + chars[..n].ToString());
        stat.Calls++;
        stat.BytesIn += argsUtf8.Length;
        stat.BytesOut += r.Length;
        stat.GuestTicks += ModProfiler.Now() - t0;
        return r;
    }

    /// String overload of <see cref="Call(ModHostContext, ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>;
    /// returns null for a function without a result.
    public string? Call(ModHostContext mod, string name, string argsJson)
    {
        if (!_fns.TryGetValue(name, out var fn))
            throw new ModCallException($"mod_call: unknown host function '{name}'");
        var result = Invoke(mod, name, fn, Encoding.UTF8.GetBytes(argsJson));
        return result.IsEmpty ? null : Encoding.UTF8.GetString(result);
    }

    // Shared, never disposed: the argument list of every argument-less call.
    private static readonly JsonDocument EmptyArgs = JsonDocument.Parse("[]");

    private ReadOnlySpan<byte> Invoke(ModHostContext mod, scoped ReadOnlySpan<char> name, ModHostFn fn, scoped ReadOnlySpan<byte> argsUtf8)
    {
        if (argsUtf8.IsEmpty)
        {
            Run(mod, name, fn, EmptyArgs.RootElement);
            return _out.WrittenSpan;
        }

        // A JsonDocument per call is the price of the JsonElement body contract; its
        // copy of the args and its index are ArrayPool-rented and returned on Dispose.
        JsonDocument doc;
        try
        {
            var reader = new Utf8JsonReader(argsUtf8);
            doc = JsonDocument.ParseValue(ref reader);
        }
        catch (JsonException e)
        {
            throw new ModCallException($"mod_call {name.ToString()}: args are not JSON ({e.Message})", e);
        }

        using (doc)
            Run(mod, name, fn, doc.RootElement);
        return _out.WrittenSpan;
    }

    private void Run(ModHostContext mod, scoped ReadOnlySpan<char> name, ModHostFn fn, JsonElement args)
    {
        if (args.ValueKind != JsonValueKind.Array)
            throw new ModCallException($"mod_call {name.ToString()}: args must be a JSON array");

        _out.ResetWrittenCount();
        _writer.Reset(_out);
        try
        {
            fn(mod, args, _writer);
            _writer.Flush();
        }
        catch (ModCallException)
        {
            throw;
        }
        // Reading a parameter of the wrong JSON kind / a missing index / a bad enum
        // name surfaces here: that is a malformed call, so it traps the guest.
        catch (Exception e) when (e is InvalidOperationException or FormatException
                                      or IndexOutOfRangeException or ArgumentException or KeyNotFoundException)
        {
            throw new ModCallException($"mod_call {name.ToString()}: bad args ({e.Message})", e);
        }
    }
}
