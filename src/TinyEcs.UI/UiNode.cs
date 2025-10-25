using System;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using EcsID = ulong;

namespace TinyEcs.UI;

/// <summary>
/// Describes a Clay element that should be emitted during the layout pass.
/// </summary>
public struct UiNode
{
	/// <summary>
	/// Raw Clay element declaration. Configure sizing, flex, styling, etc.
	/// </summary>
	public Clay_ElementDeclaration Declaration;

	/// <summary>
	/// Clay element identifier (stored separately from declaration as of Clay API change).
	/// </summary>
	public Clay_ElementId ElementId;

	/// <summary>
	/// Assign a Clay identifier using <see cref="ClayId"/> helpers.
	/// </summary>
	public void SetId(ClayId id)
	{
		ElementId = id.ToElementId();
	}
}

/// <summary>
/// Optional text payload attached to a <see cref="UiNode"/>.
/// </summary>
public struct UiText
{
	public Clay_String Value;
	public Clay_TextElementConfig Config;

	public static UiText From(string text, Clay_TextElementConfig config = default)
	{
		return new UiText
		{
			Value = Clay.ClayStrings[text],
			Config = config
		};
	}

	public static UiText From(ReadOnlySpan<char> text, Clay_TextElementConfig config = default)
	{
		return new UiText
		{
			Value = Clay.ClayStrings[text],
			Config = config
		};
	}

	public readonly bool HasContent => Value.length > 0;

	public void SetText(string text) => Value = Clay.ClayStrings[text];

	public void SetText(ReadOnlySpan<char> text) => Value = Clay.ClayStrings[text];
}

/// <summary>
/// Declarative parent relationship for UI nodes. The Clay UI systems will keep the ECS relationship graph in sync.
/// </summary>
public struct UiNodeParent
{
	public UiNodeParent()
	{
		Parent = 0;
		Index = -1;
	}

	public EcsID Parent;
	public int Index;

	public static UiNodeParent For(EcsID parent, int index = -1) => new() { Parent = parent, Index = index };

	public readonly bool HasParent => Parent != 0;
}

/// <summary>
/// Convenience bundle for spawning UI nodes via <see cref="Commands"/>.
/// </summary>
public struct UiNodeBundle : IBundle
{
	public UiNode Node;
	public UiText? Text;
	public UiNodeParent? Parent;

	public readonly void Insert(EntityView entity)
	{
		entity.Set(Node);
		if (Text.HasValue)
			entity.Set(Text.Value);
		if (Parent.HasValue)
			entity.Set(Parent.Value);
	}

	public readonly void Insert(EntityCommands entity)
	{
		entity.Insert(Node);
		if (Text.HasValue)
			entity.Insert(Text.Value);
		if (Parent.HasValue)
			entity.Insert(Parent.Value);
	}
}

public static class ClayIdExtensions
{
	public static Clay_ElementId ToElementId(this ClayId id)
	{
		return Clay.HashId(id.Text, id.Offset, id.Seed);
	}
}
