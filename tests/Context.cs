namespace TinyEcs.Tests
{
	public sealed class Context : IDisposable
	{
		public World World { get; }

		public Context()
		{
			World = new World();

			// we need to register components in order to avoid ID conflics
			World.Component<LargeComponent>();
			World.Component<FloatComponent>();
			World.Component<IntComponent>();
			World.Component<BoolComponent>();
			World.Component<NormalTag>();
		}

		public void Dispose()
		{
			World.Dispose();
		}
	}
}
