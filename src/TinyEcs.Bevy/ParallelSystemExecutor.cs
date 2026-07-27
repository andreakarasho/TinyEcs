// Only ever constructed by ThreadedSystemExecutor. A host without OS threads
// (wasi guest) builds its App with a SequentialSystemExecutor, which makes this
// type unreachable and lets the trimmer drop it — no preprocessor gate needed.
using System;
using System.Collections.Generic;
using System.Threading;

namespace TinyEcs.Bevy;

/// <summary>
/// Persistent worker pool that runs a batch of conflict-free systems in
/// parallel with NO per-dispatch allocation. Replaces the per-frame
/// <c>Parallel.ForEach</c> the scheduler used before, which
/// allocated the whole <c>TaskReplicator</c> / <c>RangeWorker</c> / <c>Task</c>
/// scaffolding plus a capturing closure on every call — measured at ~80% of
/// frame-time GC garbage when profiling a real workload.
/// </summary>
/// <remarks>
/// Model: <c>threadCount</c> background workers block on a semaphore. Each
/// <see cref="Run"/> publishes the current batch, wakes <c>min(N, count-1)</c>
/// helpers, runs work on the CALLING thread too (so the main thread is never
/// idle on small batches), then barriers on a <see cref="CountdownEvent"/>.
/// The batch index is handed out via <see cref="Interlocked"/> so any number of
/// workers drains the whole batch. Every sync primitive is reused across calls,
/// so steady-state allocation is zero. Reflection-free — NativeAOT safe.
///
/// Thread-safety: <see cref="Run"/> is single-caller (the stage loop runs on one
/// thread; only execution WITHIN a batch is parallel). The publish→wake→barrier
/// ordering relies on the release/acquire fences of <see cref="SemaphoreSlim"/>
/// and <see cref="CountdownEvent"/>.
/// </remarks>
internal sealed class ParallelSystemExecutor : IDisposable
{
	private readonly Thread[] _workers;
	private readonly SemaphoreSlim _wake = new(0);
	private readonly CountdownEvent _done = new(1);

	private List<SystemDescriptor>? _batch;
	private TinyEcs.World? _world;
	private int _cursor;
	private volatile bool _shutdown;
	private Exception? _error; // first/last worker or caller exception, rethrown on Run's caller

	public ParallelSystemExecutor(int threadCount = 0)
	{
		if (threadCount <= 0)
		{
			// One helper is woken per system per batch per frame, so pool size drives
			// semaphore traffic, not throughput: sizing it at ProcessorCount - 1 made a
			// 28-core box spend ~1 core in worker wakeups and 25% of the caller thread in
			// SemaphoreSlim.Release. Measured in-world on the ClassicUO client, standing
			// still (cores of CPU / fps): 27 -> 2.11/1752, 4 -> 1.93/1949, 2 -> 1.65/2050,
			// 1 -> 1.54/2203. Same ordering during chunk streaming. Small boxes keep the
			// old value; the cap only bites above 5 cores. TINYECS_WORKERS overrides —
			// the sweet spot is workload-dependent and worth re-measuring per game.
			threadCount = int.TryParse(Environment.GetEnvironmentVariable("TINYECS_WORKERS"), out var configured) && configured > 0
				? configured
				: Math.Min(Math.Max(1, Environment.ProcessorCount - 1), 4);
		}

		_workers = new Thread[threadCount];
		for (var i = 0; i < threadCount; i++)
		{
			var t = new Thread(WorkerLoop) { IsBackground = true, Name = $"tinyecs-worker-{i}" };
			_workers[i] = t;
			t.Start();
		}
	}

	/// <summary>
	/// Runs every system in <paramref name="batch"/> across the pool plus the
	/// calling thread, blocking until all complete. The caller guarantees the
	/// batch is conflict-free (that is the batch invariant from BuildOneBatch).
	/// Exceptions thrown by a system are surfaced on the calling thread after
	/// the barrier, mirroring Parallel.ForEach's propagate-on-join behaviour.
	/// </summary>
	public void Run(List<SystemDescriptor> batch, TinyEcs.World world)
	{
		_batch = batch;
		_world = world;
		_error = null;
		Volatile.Write(ref _cursor, 0);

		// The calling thread is itself one worker, so wake at most count-1 helpers.
		var helpers = Math.Min(_workers.Length, batch.Count - 1);
		if (helpers > 0)
		{
			_done.Reset(helpers);
			_wake.Release(helpers); // release fence publishes _batch/_world/_cursor
		}

		try
		{
			Drain(batch, world); // caller participates instead of idling
		}
		catch (Exception ex)
		{
			_error = ex;
		}

		if (helpers > 0)
			_done.Wait(); // always barrier before returning, even if the caller threw

		if (_error != null)
		{
			var e = _error;
			_error = null;
			throw e;
		}
	}

	private void Drain(List<SystemDescriptor> batch, TinyEcs.World world)
	{
		int i;
		while ((i = Interlocked.Increment(ref _cursor) - 1) < batch.Count)
		{
			var d = batch[i];
			if (d.ShouldRun(world))
				d.RunProfiled(world);
		}
	}

	private void WorkerLoop()
	{
		while (true)
		{
			_wake.Wait(); // acquire fence: sees _batch/_world/_cursor published before Release
			if (_shutdown)
				return;

			try
			{
				Drain(_batch!, _world!);
			}
			catch (Exception ex)
			{
				_error = ex; // last-writer-wins; enough to surface one failure
			}
			finally
			{
				_done.Signal();
			}
		}
	}

	public void Dispose()
	{
		_shutdown = true;
		_wake.Release(_workers.Length);
		foreach (var t in _workers)
			t.Join();
		_wake.Dispose();
		_done.Dispose();
	}
}
