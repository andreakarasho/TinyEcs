namespace TinyEcs.Tests
{
	public sealed class Context : IDisposable
	{
		public World World { get; }

		public Context()
		{
			World = new World();

			// we need to register components in order to avoid ID conflics
			World.Entity<LargeComponent>();
			World.Entity<FloatComponent>();
			World.Entity<IntComponent>();
			World.Entity<BoolComponent>();
			World.Entity<NormalTag>();
			World.Entity<Pair<NormalTag, FloatComponent>>();
		}

		public void Dispose()
		{
			World.Dispose();
		}
	}
}
