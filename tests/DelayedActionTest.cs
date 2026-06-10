using TinyEcs.Bevy;
using Xunit;

namespace TinyEcs.Tests
{
	public class DelayedActionTest
	{
		[Fact]
		public void FiresAfterDelayNotBefore()
		{
			var d = new DelayedAction();
			var fired = 0;
			d.Add(() => fired++, 100f);

			d.Tick(50f);
			Assert.Equal(0, fired);
			d.Tick(50f);
			Assert.Equal(1, fired);
			d.Tick(50f);
			Assert.Equal(1, fired); // one-shot
		}

		[Fact]
		public void AllDueActionsFireSameFrame()
		{
			var d = new DelayedAction();
			var fired = 0;
			d.Add(() => fired++, 10f);
			d.Add(() => fired++, 20f);

			d.Tick(30f);
			Assert.Equal(2, fired);
		}

		[Fact]
		public void CallbackCanScheduleAnother()
		{
			var d = new DelayedAction();
			var chained = false;
			d.Add(() => d.Add(() => chained = true, 10f), 10f);

			d.Tick(10f);
			Assert.False(chained);
			d.Tick(10f);
			Assert.True(chained);
		}

		[Fact]
		public void PluginTicksFromTime()
		{
			var app = new App();
			app.AddPlugin(new DelayedActionPlugin());
			app.RunStartup();

			var fired = false;
			app.GetResource<DelayedAction>().Add(() => fired = true, 100f);
			var time = app.GetResource<Time>();

			time.Frame = 0.05f; // 50 ms
			app.Update();
			Assert.False(fired);
			app.Update();
			Assert.True(fired);
		}
	}
}
