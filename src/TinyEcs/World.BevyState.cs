namespace TinyEcs;

// This partial extension reserves a single nullable slot on World for the Bevy
// layer to attach its per-world state (resources, events, state machines, etc.).
// The core TinyEcs assembly has no dependency on Bevy: the field is typed as
// object? so this file compiles without any Bevy reference. The Bevy layer
// casts to its concrete WorldState type when reading/writing through
// WorldExtensions.GetState.
//
// Rationale: previously the Bevy layer used a static ConditionalWeakTable
// keyed by World instance, which added an extra hash lookup on every
// resource/event/state access. Moving the slot onto World itself collapses
// that to a single field read.
public sealed partial class World
{
	// Lazily allocated on first Bevy access. Worlds that never touch the Bevy
	// layer (pure core ECS usage) pay zero cost: the field stays null and no
	// WorldState is ever constructed.
	//
	// Concurrent first-access could race on initialization; the Bevy layer
	// accepts the benign race (worst case: a second WorldState is allocated
	// and immediately GC'd). If strict single-instance guarantees ever
	// matter, swap the Bevy-side accessor to use Interlocked.CompareExchange.
	internal object? BevyStateSlot;
}
