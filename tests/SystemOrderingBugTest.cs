using System.Collections.Generic;
using TinyEcs;
using TinyEcs.Bevy;
using Xunit;

namespace TinyEcs.Tests
{
	using Stage = TinyEcs.Bevy.Stage;

	public class SystemOrderingBugTests
	{
		[Fact]
		public void SystemsWithChainedDependenciesShouldPreserveOrderWhenUnrelatedSystemAdded()
		{
			using var world = new World();
			var app = new App(world);
			var executed = new List<string>();

			// Plugin 1: Chain of systems with dependencies
			app.AddSystem(() => executed.Add("begin"))
				.InStage(Stage.PostUpdate)
				.Label("begin")
				.Build();

			app.AddSystem(() => executed.Add("rendering"))
				.InStage(Stage.PostUpdate)
				.Label("rendering")
				.After("begin")
				.Build();

			app.AddSystem(() => executed.Add("end"))
				.InStage(Stage.PostUpdate)
				.Label("end")
				.After("rendering")
				.Build();

			// Plugin 2: Unrelated system added after the chain
			app.AddSystem(() => executed.Add("othersystem"))
				.InStage(Stage.PostUpdate)
				.Label("othersystem")
				.Build();

			app.Run();

			// Expected order: begin -> rendering -> end -> othersystem (declaration order)
			// Actual bug: begin -> rendering -> othersystem -> end
			Assert.Equal(new[] { "begin", "rendering", "end", "othersystem" }, executed);
		}

		[Fact]
		public void SystemsWithoutDependenciesShouldPreserveDeclarationOrder()
		{
			using var world = new World();
			var app = new App(world, ThreadingMode.Single);
			var executed = new List<string>();

			app.AddSystem(() => executed.Add("system1"))
				.InStage(Stage.Update)
				.Build();

			app.AddSystem(() => executed.Add("system2"))
				.InStage(Stage.Update)
				.Build();

			app.AddSystem(() => executed.Add("system3"))
				.InStage(Stage.Update)
				.Build();

			app.Run();

			Assert.Equal(new[] { "system1", "system2", "system3" }, executed);
		}

		[Fact]
		public void MixedDependenciesAndIndependentSystemsShouldRespectBoth()
		{
			using var world = new World();
			var app = new App(world);
			var executed = new List<string>();

			// Independent system 1
			app.AddSystem(() => executed.Add("independent1"))
				.InStage(Stage.Update)
				.Build();

			// Dependent chain
			app.AddSystem(() => executed.Add("chainA"))
				.InStage(Stage.Update)
				.Label("chainA")
				.Build();

			app.AddSystem(() => executed.Add("chainB"))
				.InStage(Stage.Update)
				.After("chainA")
				.Build();

			// Independent system 2
			app.AddSystem(() => executed.Add("independent2"))
				.InStage(Stage.Update)
				.Build();

			app.Run();

			// Declaration order should be preserved where no conflicts
			Assert.Equal(new[] { "independent1", "chainA", "chainB", "independent2" }, executed);
		}
	}
}
