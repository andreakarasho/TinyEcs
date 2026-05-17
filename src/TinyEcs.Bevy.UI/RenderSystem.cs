using Clay;

namespace TinyEcs.Bevy.UI;

internal static class RenderSystem
{
	public static void Publish(
		Res<UiClayContext> ctx,
		ResMut<UiRenderCommands> output)
	{
		var src = ctx.Value.LastCommands;
		ref var dst = ref output.Value;
		if (dst.Buffer.Length < src.Length)
			dst.Buffer = new RenderCommand[Math.Max(src.Length, dst.Buffer.Length * 2)];
		src.CopyTo(dst.Buffer);
		dst.Count = src.Length;
	}
}
