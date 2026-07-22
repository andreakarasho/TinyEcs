// Host-defined wasm imports: the generic lib defines only the ECS host imports
// (log / entity_parent / entity_children / component_get / resource_get); every
// game-specific import is described by the HOST as one of these meaning-free
// shapes and defined into the same import module (ModHostContext.HostImportModule)
// at Load. The lib never learns the game's function names or semantics — the
// descriptors are matched to wasm signatures per Kind by each executor's glue.

namespace TinyEcs.Bevy.Modding;

/// Wasm signature shape of a host-defined import (module = ModHostContext.
/// HostImportModule). Strings cross the boundary as UTF8; the executor glue owns
/// the encode/decode so handlers stay runtime-neutral.
public enum ModHostImportKind : byte
{
    /// (ptr: i32, len: i32) -> () — raw bytes in, nothing out.
    BytesIn,
    /// (arg: i32) -> i64.
    U32ToU64,
    /// (arg: i32) -> i32.
    U32ToU32,
    /// (arg: i32, ptr: i32, len: i32) -> i32 — UTF8 text in.
    U32TextToU32,
    /// (arg: i32, outPtr: i32, cap: i32) -> i32 — returns the UTF8 byte length;
    /// the text is written into outPtr only when it fits cap (caller re-calls
    /// with a bigger buffer otherwise).
    U32ToTextOut,
}

/// Bytes-in handler (named delegate — ref struct type args on Action are still
/// dicey across the NativeAOT-LLVM guest toolchain).
public delegate void ModBytesIn(ReadOnlySpan<byte> bytes);

/// One host-defined import. Set the handler matching Kind; the others stay null.
public sealed class ModHostImport
{
    public required string Name;
    public required ModHostImportKind Kind;

    public ModBytesIn? BytesIn;
    public Func<uint, ulong>? U32ToU64;
    public Func<uint, uint>? U32ToU32;
    public Func<uint, string, uint>? U32TextToU32;
    public Func<uint, string>? U32ToTextOut;
}
