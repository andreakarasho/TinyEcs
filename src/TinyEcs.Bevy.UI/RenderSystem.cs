using Clay;

namespace TinyEcs.Bevy.UI;

internal static class RenderSystem
{
	public static void Publish(
		Res<UiClayContext> ctx,
		ResMut<UiRenderCommands> output)
	{
		// LastCommands only changes on a real relayout. Generation-gated rather
		// than flag-gated so it does not matter whether this runs before or
		// after the other post-layout consumers.
		if (ctx.Value.PublishedGeneration == ctx.Value.LayoutGeneration)
			return;
		ctx.Value.PublishedGeneration = ctx.Value.LayoutGeneration;

		var src = ctx.Value.LastCommands;
		ref var dst = ref output.Value;
		if (dst.Buffer.Length < src.Length)
			dst.Buffer = new RenderCommand[Math.Max(src.Length, dst.Buffer.Length * 2)];
		src.CopyTo(dst.Buffer);
		dst.Count = src.Length;
	}
}
