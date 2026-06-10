using System;
using System.Collections.Generic;

namespace TinyEcs.Bevy;

/// <summary>
/// Fire-and-forget deferred callbacks: <c>Add(fn, delayMs)</c> runs
/// <paramref name="fn"/> once, roughly <c>delayMs</c> milliseconds later
/// (first frame whose accumulated delta passes the deadline). Ticked from
/// <see cref="DelayedActionPlugin"/> against the shared <see cref="Time"/>
/// resource.
/// </summary>
public sealed class DelayedAction
{
	private readonly List<(Action Fn, float RemainingMs)> _actions = new();
	private readonly List<Action> _due = new();

	public void Add(Action fn, float delayMs)
	{
		ArgumentNullException.ThrowIfNull(fn);

		if (delayMs < 0)
			throw new ArgumentOutOfRangeException(nameof(delayMs), "Delay must be non-negative.");

		_actions.Add((fn, delayMs));
	}

	public void Tick(float frameMs)
	{
		// Collect-then-invoke so a callback can Add() new delayed actions
		// without mutating the list mid-iteration.
		for (var i = _actions.Count - 1; i >= 0; i--)
		{
			var (fn, remaining) = _actions[i];
			remaining -= frameMs;

			if (remaining <= 0f)
			{
				_due.Add(fn);
				_actions.RemoveAt(i);
			}
			else
			{
				_actions[i] = (fn, remaining);
			}
		}

		foreach (var fn in _due)
			fn();
		_due.Clear();
	}

	public void Clear() => _actions.Clear();
}

/// <summary>Registers <see cref="DelayedAction"/> and ticks it each PreUpdate from <see cref="Time"/>.</summary>
public readonly struct DelayedActionPlugin : IPlugin
{
	public void Build(App app)
	{
		app.AddPlugin(new TimePlugin());
		app.AddResource(new DelayedAction());

		app.AddSystem((Res<DelayedAction> delayed, Res<Time> time) =>
				delayed.Value.Tick(time.Value.Frame * 1000f))
			.InStage(Stage.PreUpdate)
			.Build();
	}
}
