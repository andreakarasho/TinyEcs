using System;

namespace TinyEcs.Bevy;

/// <summary>
/// Strategy for running one stage's systems. Selected once at <see cref="App"/>
/// construction — either mapped from <see cref="ThreadingMode"/> or injected
/// directly via <c>new App(world, new SequentialSystemExecutor())</c>. Injecting
/// <see cref="SequentialSystemExecutor"/> keeps every thread primitive out of the
/// reachable code, which is what a single-threaded wasi guest needs (no
/// preprocessor gate required).
/// </summary>
public abstract class SystemExecutor
{
	// Internal so the strategy set stays closed: external assemblies can't
	// override an internal abstract, and the two shipped executors cover the
	// only two execution models the scheduler supports.
	internal abstract void ExecuteStage(App.StageRuntime runtime, TinyEcs.World world);
}

/// <summary>
/// Runs every system of the stage on the calling thread, in topological order.
/// No thread is ever created. Used for <see cref="ThreadingMode.Single"/>, for
/// <see cref="Stage.Startup"/> (always sequential for deterministic init), and
/// for hosts without OS threads (wasi guest).
/// </summary>
public sealed class SequentialSystemExecutor : SystemExecutor
{
	internal override void ExecuteStage(App.StageRuntime runtime, TinyEcs.World world)
	{
		foreach (var descriptor in runtime.Sorted!)
		{
			if (descriptor.ShouldRun(world))
				descriptor.RunProfiled(world);
		}
	}
}

/// <summary>
/// Runs the stage's cached conflict-free batches; single-system batches run
/// inline on the calling thread, multi-system batches fan out across the
/// persistent <see cref="ParallelSystemExecutor"/> worker pool. The pool is
/// created lazily on the first multi-system batch, so apps whose schedule never
/// yields a parallel batch spawn no worker threads.
/// </summary>
public sealed class ThreadedSystemExecutor : SystemExecutor, IDisposable
{
	private ParallelSystemExecutor? _pool;

	internal override void ExecuteStage(App.StageRuntime runtime, TinyEcs.World world)
	{
		foreach (var batch in runtime.Batches!)
		{
			if (batch.Count == 1)
			{
				var descriptor = batch[0];
				if (descriptor.ShouldRun(world))
					descriptor.RunProfiled(world);
			}
			else
			{
				(_pool ??= new ParallelSystemExecutor()).Run(batch, world);
			}
		}
	}

	public void Dispose() => _pool?.Dispose();
}
