using Clay_cs;

namespace TinyEcs.UI.Clay;

/// <summary>
/// Theme configuration for Clay UI widgets.
/// Defines colors, sizes, and other styling properties for consistent UI appearance.
/// </summary>
public class ClayTheme
{
	// === Button Theme ===
	public ButtonTheme Button { get; set; } = new();

	// === Checkbox Theme ===
	public CheckboxTheme Checkbox { get; set; } = new();

	// === Radio Theme ===
	public RadioTheme Radio { get; set; } = new();

	// === Slider Theme ===
	public SliderTheme Slider { get; set; } = new();

	// === Scrollbar Theme ===
	public ScrollbarTheme Scrollbar { get; set; } = new();

	// === Panel Theme ===
	public PanelTheme Panel { get; set; } = new();

	// === Dropdown Theme ===
	public DropdownTheme Dropdown { get; set; } = new();

	// === Text Input Theme ===
	public TextInputTheme TextInput { get; set; } = new();

	// === Progress Bar Theme ===
	public ProgressBarTheme ProgressBar { get; set; } = new();

	// === Typography ===
	public TypographyTheme Typography { get; set; } = new();

	/// <summary>
	/// Creates a default dark theme.
	/// </summary>
	public static ClayTheme Dark()
	{
		return new ClayTheme
		{
			Button = new ButtonTheme
			{
				BackgroundColor = new Clay_Color(70, 130, 180, 255),
				HoverColor = new Clay_Color(90, 150, 200, 255),
				PressedColor = new Clay_Color(50, 110, 160, 255),
				DisabledBackgroundColor = new Clay_Color(60, 60, 60, 255),
				TextColor = new Clay_Color(255, 255, 255, 255),
				DisabledTextColor = new Clay_Color(150, 150, 150, 255),
				CornerRadius = 8,
				Height = 40,
				Padding = 8
			},
			Checkbox = new CheckboxTheme
			{
				BoxColor = new Clay_Color(60, 60, 60, 255),
				CheckedColor = new Clay_Color(70, 130, 180, 255),
				HoverColor = new Clay_Color(80, 80, 80, 255),
				FillColor = new Clay_Color(255, 255, 255, 255),
				BorderColor = new Clay_Color(100, 100, 100, 255),
				DisabledColor = new Clay_Color(40, 40, 40, 255),
				LabelColor = new Clay_Color(220, 220, 220, 255),
				Size = 20,
				BorderWidth = 2,
				CornerRadius = 4
			},
			Radio = new RadioTheme
			{
				CircleColor = new Clay_Color(60, 60, 60, 255),
				SelectedColor = new Clay_Color(70, 130, 180, 255),
				HoverColor = new Clay_Color(80, 80, 80, 255),
				DotColor = new Clay_Color(255, 255, 255, 255),
				BorderColor = new Clay_Color(100, 100, 100, 255),
				DisabledColor = new Clay_Color(40, 40, 40, 255),
				LabelColor = new Clay_Color(220, 220, 220, 255),
				Size = 20,
				DotSize = 10,
				BorderWidth = 2
			},
			Slider = new SliderTheme
			{
				TrackColor = new Clay_Color(60, 60, 60, 255),
				FillColor = new Clay_Color(70, 130, 180, 255),
				ThumbColor = new Clay_Color(200, 200, 200, 255),
				HoverThumbColor = new Clay_Color(220, 220, 220, 255),
				DraggingThumbColor = new Clay_Color(255, 255, 255, 255),
				DisabledTrackColor = new Clay_Color(40, 40, 40, 255),
				DisabledFillColor = new Clay_Color(50, 50, 50, 255),
				LabelColor = new Clay_Color(220, 220, 220, 255),
				TrackHeight = 6,
				ThumbSize = 18,
				CornerRadius = 3
			},
			Scrollbar = new ScrollbarTheme
			{
				TrackColor = new Clay_Color(40, 40, 40, 255),
				ThumbColor = new Clay_Color(100, 100, 100, 255),
				HoverThumbColor = new Clay_Color(120, 120, 120, 255),
				DraggingThumbColor = new Clay_Color(140, 140, 140, 255),
				Size = 12,
				MinThumbSize = 30,
				CornerRadius = 6
			},
			Panel = new PanelTheme
			{
				BackgroundColor = new Clay_Color(30, 30, 30, 255),
				BorderColor = new Clay_Color(60, 60, 60, 255),
				TitleBackgroundColor = new Clay_Color(40, 40, 40, 255),
				TitleTextColor = new Clay_Color(220, 220, 220, 255),
				BorderWidth = 1,
				CornerRadius = 8,
				Padding = 12
			},
			Dropdown = new DropdownTheme
			{
				ButtonBackgroundColor = new Clay_Color(60, 60, 60, 255),
				ButtonHoverColor = new Clay_Color(70, 70, 70, 255),
				ButtonTextColor = new Clay_Color(220, 220, 220, 255),
				MenuBackgroundColor = new Clay_Color(50, 50, 50, 255),
				MenuBorderColor = new Clay_Color(80, 80, 80, 255),
				ItemBackgroundColor = new Clay_Color(50, 50, 50, 255),
				ItemHoverColor = new Clay_Color(70, 130, 180, 255),
				ItemTextColor = new Clay_Color(220, 220, 220, 255),
				Height = 36,
				CornerRadius = 4,
				BorderWidth = 1
			},
			TextInput = new TextInputTheme
			{
				BackgroundColor = new Clay_Color(40, 40, 40, 255),
				BorderColor = new Clay_Color(80, 80, 80, 255),
				FocusBorderColor = new Clay_Color(70, 130, 180, 255),
				TextColor = new Clay_Color(220, 220, 220, 255),
				PlaceholderColor = new Clay_Color(120, 120, 120, 255),
				SelectionColor = new Clay_Color(70, 130, 180, 128),
				CursorColor = new Clay_Color(220, 220, 220, 255),
				DisabledBackgroundColor = new Clay_Color(30, 30, 30, 255),
				Height = 36,
				CornerRadius = 4,
				BorderWidth = 1,
				Padding = 8
			},
			ProgressBar = new ProgressBarTheme
			{
				BackgroundColor = new Clay_Color(40, 40, 40, 255),
				FillColor = new Clay_Color(70, 130, 180, 255),
				BorderColor = new Clay_Color(60, 60, 60, 255),
				TextColor = new Clay_Color(220, 220, 220, 255),
				Height = 24,
				CornerRadius = 4,
				BorderWidth = 1
			},
			Typography = new TypographyTheme
			{
				DefaultFontId = 0,
				DefaultFontSize = 16,
				HeaderFontSize = 24,
				SubheaderFontSize = 20,
				SmallFontSize = 14,
				DefaultTextColor = new Clay_Color(220, 220, 220, 255),
				MutedTextColor = new Clay_Color(150, 150, 150, 255),
				LinkColor = new Clay_Color(70, 130, 180, 255)
			}
		};
	}

	/// <summary>
	/// Creates a default light theme.
	/// </summary>
	public static ClayTheme Light()
	{
		return new ClayTheme
		{
			Button = new ButtonTheme
			{
				BackgroundColor = new Clay_Color(70, 130, 180, 255),
				HoverColor = new Clay_Color(90, 150, 200, 255),
				PressedColor = new Clay_Color(50, 110, 160, 255),
				DisabledBackgroundColor = new Clay_Color(200, 200, 200, 255),
				TextColor = new Clay_Color(255, 255, 255, 255),
				DisabledTextColor = new Clay_Color(150, 150, 150, 255),
				CornerRadius = 8,
				Height = 40,
				Padding = 8
			},
			Checkbox = new CheckboxTheme
			{
				BoxColor = new Clay_Color(240, 240, 240, 255),
				CheckedColor = new Clay_Color(70, 130, 180, 255),
				HoverColor = new Clay_Color(230, 230, 230, 255),
				FillColor = new Clay_Color(255, 255, 255, 255),
				BorderColor = new Clay_Color(180, 180, 180, 255),
				DisabledColor = new Clay_Color(220, 220, 220, 255),
				LabelColor = new Clay_Color(40, 40, 40, 255),
				Size = 20,
				BorderWidth = 2,
				CornerRadius = 4
			},
			Radio = new RadioTheme
			{
				CircleColor = new Clay_Color(240, 240, 240, 255),
				SelectedColor = new Clay_Color(70, 130, 180, 255),
				HoverColor = new Clay_Color(230, 230, 230, 255),
				DotColor = new Clay_Color(255, 255, 255, 255),
				BorderColor = new Clay_Color(180, 180, 180, 255),
				DisabledColor = new Clay_Color(220, 220, 220, 255),
				LabelColor = new Clay_Color(40, 40, 40, 255),
				Size = 20,
				DotSize = 10,
				BorderWidth = 2
			},
			Slider = new SliderTheme
			{
				TrackColor = new Clay_Color(220, 220, 220, 255),
				FillColor = new Clay_Color(70, 130, 180, 255),
				ThumbColor = new Clay_Color(255, 255, 255, 255),
				HoverThumbColor = new Clay_Color(245, 245, 245, 255),
				DraggingThumbColor = new Clay_Color(235, 235, 235, 255),
				DisabledTrackColor = new Clay_Color(230, 230, 230, 255),
				DisabledFillColor = new Clay_Color(200, 200, 200, 255),
				LabelColor = new Clay_Color(40, 40, 40, 255),
				TrackHeight = 6,
				ThumbSize = 18,
				CornerRadius = 3
			},
			Scrollbar = new ScrollbarTheme
			{
				TrackColor = new Clay_Color(240, 240, 240, 255),
				ThumbColor = new Clay_Color(180, 180, 180, 255),
				HoverThumbColor = new Clay_Color(160, 160, 160, 255),
				DraggingThumbColor = new Clay_Color(140, 140, 140, 255),
				Size = 12,
				MinThumbSize = 30,
				CornerRadius = 6
			},
			Panel = new PanelTheme
			{
				BackgroundColor = new Clay_Color(255, 255, 255, 255),
				BorderColor = new Clay_Color(220, 220, 220, 255),
				TitleBackgroundColor = new Clay_Color(245, 245, 245, 255),
				TitleTextColor = new Clay_Color(40, 40, 40, 255),
				BorderWidth = 1,
				CornerRadius = 8,
				Padding = 12
			},
			Dropdown = new DropdownTheme
			{
				ButtonBackgroundColor = new Clay_Color(240, 240, 240, 255),
				ButtonHoverColor = new Clay_Color(230, 230, 230, 255),
				ButtonTextColor = new Clay_Color(40, 40, 40, 255),
				MenuBackgroundColor = new Clay_Color(255, 255, 255, 255),
				MenuBorderColor = new Clay_Color(200, 200, 200, 255),
				ItemBackgroundColor = new Clay_Color(255, 255, 255, 255),
				ItemHoverColor = new Clay_Color(70, 130, 180, 255),
				ItemTextColor = new Clay_Color(40, 40, 40, 255),
				Height = 36,
				CornerRadius = 4,
				BorderWidth = 1
			},
			TextInput = new TextInputTheme
			{
				BackgroundColor = new Clay_Color(255, 255, 255, 255),
				BorderColor = new Clay_Color(200, 200, 200, 255),
				FocusBorderColor = new Clay_Color(70, 130, 180, 255),
				TextColor = new Clay_Color(40, 40, 40, 255),
				PlaceholderColor = new Clay_Color(150, 150, 150, 255),
				SelectionColor = new Clay_Color(70, 130, 180, 128),
				CursorColor = new Clay_Color(40, 40, 40, 255),
				DisabledBackgroundColor = new Clay_Color(240, 240, 240, 255),
				Height = 36,
				CornerRadius = 4,
				BorderWidth = 1,
				Padding = 8
			},
			ProgressBar = new ProgressBarTheme
			{
				BackgroundColor = new Clay_Color(240, 240, 240, 255),
				FillColor = new Clay_Color(70, 130, 180, 255),
				BorderColor = new Clay_Color(220, 220, 220, 255),
				TextColor = new Clay_Color(40, 40, 40, 255),
				Height = 24,
				CornerRadius = 4,
				BorderWidth = 1
			},
			Typography = new TypographyTheme
			{
				DefaultFontId = 0,
				DefaultFontSize = 16,
				HeaderFontSize = 24,
				SubheaderFontSize = 20,
				SmallFontSize = 14,
				DefaultTextColor = new Clay_Color(40, 40, 40, 255),
				MutedTextColor = new Clay_Color(120, 120, 120, 255),
				LinkColor = new Clay_Color(70, 130, 180, 255)
			}
		};
	}
}

// === Theme Classes ===

public class ButtonTheme
{
	public Clay_Color BackgroundColor { get; set; }
	public Clay_Color HoverColor { get; set; }
	public Clay_Color PressedColor { get; set; }
	public Clay_Color DisabledBackgroundColor { get; set; }
	public Clay_Color TextColor { get; set; }
	public Clay_Color DisabledTextColor { get; set; }
	public ushort CornerRadius { get; set; }
	public float Height { get; set; }
	public float Padding { get; set; }
}

public class CheckboxTheme
{
	public Clay_Color BoxColor { get; set; }
	public Clay_Color CheckedColor { get; set; }
	public Clay_Color HoverColor { get; set; }
	public Clay_Color FillColor { get; set; }
	public Clay_Color BorderColor { get; set; }
	public Clay_Color DisabledColor { get; set; }
	public Clay_Color LabelColor { get; set; }
	public float Size { get; set; }
	public ushort BorderWidth { get; set; }
	public ushort CornerRadius { get; set; }
}

public class RadioTheme
{
	public Clay_Color CircleColor { get; set; }
	public Clay_Color SelectedColor { get; set; }
	public Clay_Color HoverColor { get; set; }
	public Clay_Color DotColor { get; set; }
	public Clay_Color BorderColor { get; set; }
	public Clay_Color DisabledColor { get; set; }
	public Clay_Color LabelColor { get; set; }
	public float Size { get; set; }
	public float DotSize { get; set; }
	public ushort BorderWidth { get; set; }
}

public class SliderTheme
{
	public Clay_Color TrackColor { get; set; }
	public Clay_Color FillColor { get; set; }
	public Clay_Color ThumbColor { get; set; }
	public Clay_Color HoverThumbColor { get; set; }
	public Clay_Color DraggingThumbColor { get; set; }
	public Clay_Color DisabledTrackColor { get; set; }
	public Clay_Color DisabledFillColor { get; set; }
	public Clay_Color LabelColor { get; set; }
	public float TrackHeight { get; set; }
	public float ThumbSize { get; set; }
	public ushort CornerRadius { get; set; }
}

public class ScrollbarTheme
{
	public Clay_Color TrackColor { get; set; }
	public Clay_Color ThumbColor { get; set; }
	public Clay_Color HoverThumbColor { get; set; }
	public Clay_Color DraggingThumbColor { get; set; }
	public float Size { get; set; }
	public float MinThumbSize { get; set; }
	public ushort CornerRadius { get; set; }
}

public class PanelTheme
{
	public Clay_Color BackgroundColor { get; set; }
	public Clay_Color BorderColor { get; set; }
	public Clay_Color TitleBackgroundColor { get; set; }
	public Clay_Color TitleTextColor { get; set; }
	public ushort BorderWidth { get; set; }
	public ushort CornerRadius { get; set; }
	public float Padding { get; set; }
}

public class DropdownTheme
{
	public Clay_Color ButtonBackgroundColor { get; set; }
	public Clay_Color ButtonHoverColor { get; set; }
	public Clay_Color ButtonTextColor { get; set; }
	public Clay_Color MenuBackgroundColor { get; set; }
	public Clay_Color MenuBorderColor { get; set; }
	public Clay_Color ItemBackgroundColor { get; set; }
	public Clay_Color ItemHoverColor { get; set; }
	public Clay_Color ItemTextColor { get; set; }
	public float Height { get; set; }
	public ushort CornerRadius { get; set; }
	public ushort BorderWidth { get; set; }
}

public class TextInputTheme
{
	public Clay_Color BackgroundColor { get; set; }
	public Clay_Color BorderColor { get; set; }
	public Clay_Color FocusBorderColor { get; set; }
	public Clay_Color TextColor { get; set; }
	public Clay_Color PlaceholderColor { get; set; }
	public Clay_Color SelectionColor { get; set; }
	public Clay_Color CursorColor { get; set; }
	public Clay_Color DisabledBackgroundColor { get; set; }
	public float Height { get; set; }
	public ushort CornerRadius { get; set; }
	public ushort BorderWidth { get; set; }
	public float Padding { get; set; }
}

public class ProgressBarTheme
{
	public Clay_Color BackgroundColor { get; set; }
	public Clay_Color FillColor { get; set; }
	public Clay_Color BorderColor { get; set; }
	public Clay_Color TextColor { get; set; }
	public float Height { get; set; }
	public ushort CornerRadius { get; set; }
	public ushort BorderWidth { get; set; }
}

public class TypographyTheme
{
	public ushort DefaultFontId { get; set; }
	public ushort DefaultFontSize { get; set; }
	public ushort HeaderFontSize { get; set; }
	public ushort SubheaderFontSize { get; set; }
	public ushort SmallFontSize { get; set; }
	public Clay_Color DefaultTextColor { get; set; }
	public Clay_Color MutedTextColor { get; set; }
	public Clay_Color LinkColor { get; set; }
}
