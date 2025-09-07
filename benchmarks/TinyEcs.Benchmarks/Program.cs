using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using TinyEcs;

namespace TinyEcs.Benchmarks
{
	public class Program
	{
		public static void Main(string[] args)
		{
			// See https://aka.ms/new-console-template for more information
			Console.WriteLine("Hello, World!");
			BenchmarkRunner.Run<QueryBenchmarks>();
		}
	}

	[MemoryDiagnoser]
	[SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.HostProcess, warmupCount: 1, iterationCount: 3)]
	public class QueryBenchmarks
	{
		struct Position { public float X, Y, Z; }
		struct Velocity { public float X, Y; }

		private World _world;
		private Query _query;

		[GlobalSetup]
		public void Setup()
		{
			_world = new();

			for (var i = 0; i < 1_000_000; ++i)
				_world.Entity()
					.Set(new Position())
					.Set(new Velocity());

			_query = _world.QueryBuilder()
				.With<Position>()
				.With<Velocity>()
				.Build();
		}

		[Benchmark]
		public void Query()
		{
			foreach ((var pos, var vel) in Data<Position, Velocity>.CreateIterator(_query.Iter()))
			{
				pos.Ref.X *= vel.Ref.X;
				pos.Ref.Y *= vel.Ref.Y;
			}
		}

		// [Benchmark]
		// public void Query2()
		// {
		// 	foreach ((var pos, var vel) in Data<Position, Velocity>.CreateIterator(_query.Iter()))
		// 	{
		// 		pos.Rw.X *= vel.Ref.X;
		// 		pos.Rw.Y *= vel.Ref.Y;
		// 	}
		// }

		[Benchmark]
		public void QueryWithEntity()
		{
			foreach ((var ent, var pos, var vel) in Data<Position, Velocity>.CreateIterator(_query.Iter()))
			{
				pos.Ref.X *= vel.Ref.X;
				pos.Ref.Y *= vel.Ref.Y;
			}
		}
	}
}
