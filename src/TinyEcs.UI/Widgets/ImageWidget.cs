using System;
using System.Numerics;
using Clay_cs;
using TinyEcs;
using TinyEcs.Bevy;
using EcsID = ulong;

namespace TinyEcs.UI.Widgets;

/// <summary>
/// Style configuration for image widgets.
/// </summary>
public readonly record struct ClayImageStyle(
	Vector2 Size,
	Clay_Color? BackgroundColor,
	Clay_CornerRadius CornerRadius,
	Clay_Padding Padding)
{
	public static ClayImageStyle Default => new(
		new Vector2(100f, 100f),
		null,
		Clay_CornerRadius.All(0),
		Clay_Padding.All(0));

	public static ClayImageStyle Thumbnail => Default with
	{
		Size = new Vector2(64f, 64f),
		CornerRadius = Clay_CornerRadius.All(4)
	};

	public static ClayImageStyle Icon => Default with
	{
		Size = new Vector2(24f, 24f)
	};

	public static ClayImageStyle Avatar => Default with
	{
		Size = new Vector2(48f, 48f),
		CornerRadius = Clay_CornerRadius.All(24)
	};
}

/// <summary>
/// Creates image widgets that display textures/sprites.
/// </summary>
public static class ImageWidget
{
	/// <summary>
	/// Creates an image entity with the specified style and image data pointer.
	/// </summary>
	/// <param name="commands">Command buffer for entity creation.</param>
	/// <param name="style">Visual style configuration.</param>
	/// <param name="imageData">Pointer to image data (renderer-specific).</param>
	/// <param name="parent">Optional parent entity ID.</param>
	public static EntityCommands Create(
		Commands commands,
		ClayImageStyle style,
		nint imageData,
		EcsID? parent = default)
	{
		var image = commands.Spawn();

		var declaration = new Clay_ElementDeclaration
		{
			layout = new Clay_LayoutConfig
			{
				sizing = new Clay_Sizing(
					Clay_SizingAxis.Fixed(style.Size.X),
					Clay_SizingAxis.Fixed(style.Size.Y)),
				padding = style.Padding
			},
			cornerRadius = style.CornerRadius
		};

		if (style.BackgroundColor.HasValue)
		{
			declaration.backgroundColor = style.BackgroundColor.Value;
		}

		// Add image configuration
		unsafe
		{
			declaration.image = new Clay_ImageElementConfig
			{
				imageData = (void*)imageData
			};
		}

		image.Insert(new UiNode
		{
			Declaration = declaration
		});

		if (parent.HasValue && parent.Value != 0)
		{
			image.Insert(UiNodeParent.For(parent.Value));
		}

		return image;
	}

	/// <summary>
	/// Creates a fixed-size image with custom dimensions.
	/// </summary>
	public static EntityCommands CreateFixed(
		Commands commands,
		Vector2 size,
		nint imageData,
		EcsID? parent = default)
	{
		return Create(commands, ClayImageStyle.Default with { Size = size }, imageData, parent);
	}

	/// <summary>
	/// Creates a thumbnail-sized image.
	/// </summary>
	public static EntityCommands CreateThumbnail(
		Commands commands,
		nint imageData,
		EcsID? parent = default)
	{
		return Create(commands, ClayImageStyle.Thumbnail, imageData, parent);
	}

	/// <summary>
	/// Creates an icon-sized image.
	/// </summary>
	public static EntityCommands CreateIcon(
		Commands commands,
		nint imageData,
		EcsID? parent = default)
	{
		return Create(commands, ClayImageStyle.Icon, imageData, parent);
	}

	/// <summary>
	/// Creates a circular avatar image.
	/// </summary>
	public static EntityCommands CreateAvatar(
		Commands commands,
		nint imageData,
		EcsID? parent = default)
	{
		return Create(commands, ClayImageStyle.Avatar, imageData, parent);
	}
}
