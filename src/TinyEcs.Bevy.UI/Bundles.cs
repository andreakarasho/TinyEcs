using ClayColor = Clay.Color;

namespace TinyEcs.Bevy.UI;

public struct NodeBundle : IBundle
{
	public Node Node;
	public BackgroundColor Background;

	public readonly void Insert(EntityCommands e)
	{
		e.Insert(Node);
		if (Background.Value.A > 0) e.Insert(Background);
	}
}

public struct TextBundle : IBundle
{
	public Node Node;
	public Text Text;
	public TextFont Font;
	public TextColor Color;

	public readonly void Insert(EntityCommands e)
	{
		e.Insert(Node);
		e.Insert(Text);
		e.Insert(Font);
		e.Insert(Color);
	}
}

public struct ButtonBundle : IBundle
{
	public Node Node;
	public Button Marker;
	public Interaction Interaction;
	public FocusPolicy Focus;
	public BackgroundColor Background;

	public readonly void Insert(EntityCommands e)
	{
		e.Insert(Node);
		e.Insert(Marker);
		e.Insert(Interaction);
		e.Insert(Focus);
		e.Insert(Background);
	}
}

public struct ImageBundle : IBundle
{
	public Node Node;
	public UiImage Image;

	public readonly void Insert(EntityCommands e)
	{
		e.Insert(Node);
		e.Insert(Image);
	}
}
