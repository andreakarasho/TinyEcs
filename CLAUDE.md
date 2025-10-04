# CLAUDE Project Brief

## Overview
TinyEcs is a high-performance, reflection-free entity component system (ECS) framework for .NET. It targets zero-runtime-allocation workflows, supports NativeAOT/bflat, and ships with an optional Bevy-inspired scheduling layer that brings modern stage orchestration, observers, and system-parameter injection to C# game and simulation projects.

## Core Ideas
- **World-first ECS**: Entities, components, and archetypes live in `TinyEcs` (see `src/TinyEcs/`). The design favors cache-friendly layouts and compile-time safety.
- **Reflection-free APIs**: All component registration and lookups avoid runtime reflection, enabling AOT and high determinism.
- **Deterministic scheduling**: The Bevy-compatible layer (`src/TinyEcs.Bevy/`) adds stages, plugins, events, and system parameters for structured gameplay loops.
- **Observer hooks**: Automatic triggers (spawn, despawn, add, insert, remove) let systems react to world changes without polling.
- **Deferred commands**: Systems can queue entity/resource mutations safely, keeping multi-threaded execution predictable.

## Repository Layout
- `src/TinyEcs/` – Base ECS runtime (world, entity views, archetype storage, queries, scheduler primitives).
- `src/TinyEcs.Bevy/` – Bevy-inspired extensions: `App`, stages, plugins, observers, system parameters, command buffers.
- `samples/` – Example programs demonstrating TinyEcs in real use (e.g., game loops, system params, parallel systems).
- `tests/` – xUnit suites covering ECS primitives and Bevy integration. `tests/BevyApp.cs` exercises scheduling, observers, events, and command buffers.
- `benchmarks/` – Performance evaluation scenarios.
- `tools/` & `scripts/` – Helper utilities for development workflows.

## Getting Started
1. Install .NET 9.0 preview (or .NET 8.0 for core ECS only).
2. Restore and build: `dotnet build TinyEcs.slnx`.
3. Run tests: `dotnet test tests/TinyEcs.Tests.csproj`.
4. Explore samples, e.g. `dotnet run --project samples/TinyEcsGame/TinyEcsGame.csproj -c Release`.

## Key Features Snapshot
- Entities and components managed via `World`, `EntityView`, and query APIs.
- Stage-based execution with topological ordering, parallel batches, and enter/exit state hooks.
- Observer API for component lifecycle events (`OnInsert`, `OnRemove`, etc.).
- System parameters delivering resources, events, deferred commands, and local state into systems.
- Native resource/event management and deferred mutation pipeline for safe multithreading.

## Testing & Quality
- Tests live under `tests/`. Run `dotnet test tests/TinyEcs.Tests.csproj`.
- Coverage includes ECS basics, archetype operations, deferred queues, and Bevy integration scenarios.

## Useful Links
- README: Project overview and API walkthrough.
- Samples: Practical references for building schedulers, plugins, and systems (`samples/MyBattleground`, `samples/TinyEcsGame`).
- Benchmarks: Compare TinyEcs performance with other ECS implementations (`benchmarks/`).

## Status & Contributions
The library is under active development—APIs may change. Issues and contributions should align with the reflection-free, high-performance philosophy and include tests whenever feasible.
