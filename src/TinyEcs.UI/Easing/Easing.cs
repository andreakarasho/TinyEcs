using System;

namespace TinyEcs.UI.Easing;

/// <summary>
/// Easing function types for smooth animations.
/// Ported from sickle_ui's sickle_math::ease module.
/// See https://easings.net for visual reference of each curve.
/// </summary>
public enum Ease
{
	/// <summary>Linear interpolation (no easing)</summary>
	Linear,

	// Sine easing - smooth, gentle curves
	InSine,
	OutSine,
	InOutSine,

	// Quadratic easing - moderate acceleration
	InQuad,
	OutQuad,
	InOutQuad,

	// Cubic easing - stronger acceleration
	InCubic,
	OutCubic,
	InOutCubic,

	// Quartic easing - very strong acceleration
	InQuart,
	OutQuart,
	InOutQuart,

	// Quintic easing - extreme acceleration
	InQuint,
	OutQuint,
	InOutQuint,

	// Exponential easing - explosive acceleration
	InExpo,
	OutExpo,
	InOutExpo,

	// Circular easing - smooth circular arc
	InCirc,
	OutCirc,
	InOutCirc,

	// Back easing - overshoots then settles
	InBack,
	OutBack,
	InOutBack,

	// Elastic easing - spring-like bounce
	InElastic,
	OutElastic,
	InOutElastic,

	// Bounce easing - bouncing ball effect
	InBounce,
	OutBounce,
	InOutBounce
}

/// <summary>
/// Extension methods for applying easing functions to float values.
/// All easing functions expect input in range [0, 1] and return output in range [0, 1].
/// </summary>
public static class EasingExtensions
{
	// Constants for easing calculations (from sickle_ui)
	private const float PI = MathF.PI;
	private const float C1 = 1.70158f;
	private const float C2 = C1 * 1.525f;
	private const float C3 = C1 + 1f;
	private const float C4 = (2f * PI) / 3f;
	private const float C5 = (2f * PI) / 4.5f;
	private const float N1 = 7.5625f;
	private const float D1 = 2.75f;

	/// <summary>
	/// Applies an easing function to a normalized time value (0-1).
	/// Input is automatically clamped to [0, 1] range.
	/// </summary>
	/// <param name="t">Time value in range [0, 1]</param>
	/// <param name="easeType">Type of easing curve to apply</param>
	/// <returns>Eased value in range [0, 1]</returns>
	public static float Apply(this float t, Ease easeType)
	{
		var x = Clamp(t, 0f, 1f);

		return easeType switch
		{
			Ease.Linear => x,

			// Sine easing
			Ease.InSine => 1f - MathF.Cos((x * PI) / 2f),
			Ease.OutSine => MathF.Sin((x * PI) / 2f),
			Ease.InOutSine => -(MathF.Cos(PI * x) - 1f) / 2f,

			// Quadratic easing
			Ease.InQuad => x * x,
			Ease.OutQuad => 1f - (1f - x) * (1f - x),
			Ease.InOutQuad => x < 0.5f
				? 2f * x * x
				: 1f - MathF.Pow(-2f * x + 2f, 2f) / 2f,

			// Cubic easing
			Ease.InCubic => x * x * x,
			Ease.OutCubic => 1f - MathF.Pow(1f - x, 3f),
			Ease.InOutCubic => x < 0.5f
				? 4f * x * x * x
				: 1f - MathF.Pow(-2f * x + 2f, 3f) / 2f,

			// Quartic easing
			Ease.InQuart => x * x * x * x,
			Ease.OutQuart => 1f - MathF.Pow(1f - x, 4f),
			Ease.InOutQuart => x < 0.5f
				? 8f * x * x * x * x
				: 1f - MathF.Pow(-2f * x + 2f, 4f) / 2f,

			// Quintic easing
			Ease.InQuint => x * x * x * x * x,
			Ease.OutQuint => 1f - MathF.Pow(1f - x, 5f),
			Ease.InOutQuint => x < 0.5f
				? 16f * x * x * x * x * x
				: 1f - MathF.Pow(-2f * x + 2f, 5f) / 2f,

			// Exponential easing
			Ease.InExpo => x == 0f ? 0f : MathF.Pow(2f, 10f * x - 10f),
			Ease.OutExpo => x == 1f ? 1f : 1f - MathF.Pow(2f, -10f * x),
			Ease.InOutExpo => x == 0f ? 0f
				: x == 1f ? 1f
				: x < 0.5f ? MathF.Pow(2f, 20f * x - 10f) / 2f
				: (2f - MathF.Pow(2f, -20f * x + 10f)) / 2f,

			// Circular easing
			Ease.InCirc => 1f - MathF.Sqrt(1f - MathF.Pow(x, 2f)),
			Ease.OutCirc => MathF.Sqrt(1f - MathF.Pow(x - 1f, 2f)),
			Ease.InOutCirc => x < 0.5f
				? (1f - MathF.Sqrt(1f - MathF.Pow(2f * x, 2f))) / 2f
				: (MathF.Sqrt(1f - MathF.Pow(-2f * x + 2f, 2f)) + 1f) / 2f,

			// Back easing (overshoots)
			Ease.InBack => C3 * x * x * x - C1 * x * x,
			Ease.OutBack => 1f + C3 * MathF.Pow(x - 1f, 3f) + C1 * MathF.Pow(x - 1f, 2f),
			Ease.InOutBack => x < 0.5f
				? (MathF.Pow(2f * x, 2f) * ((C2 + 1f) * 2f * x - C2)) / 2f
				: (MathF.Pow(2f * x - 2f, 2f) * ((C2 + 1f) * (x * 2f - 2f) + C2) + 2f) / 2f,

			// Elastic easing (spring-like)
			Ease.InElastic => x == 0f ? 0f
				: x == 1f ? 1f
				: -MathF.Pow(2f, 10f * x - 10f) * MathF.Sin((x * 10f - 10.75f) * C4),
			Ease.OutElastic => x == 0f ? 0f
				: x == 1f ? 1f
				: MathF.Pow(2f, -10f * x) * MathF.Sin((x * 10f - 0.75f) * C4) + 1f,
			Ease.InOutElastic => x == 0f ? 0f
				: x == 1f ? 1f
				: x < 0.5f
					? -(MathF.Pow(2f, 20f * x - 10f) * MathF.Sin((20f * x - 11.125f) * C5)) / 2f
					: (MathF.Pow(2f, -20f * x + 10f) * MathF.Sin((20f * x - 11.125f) * C5)) / 2f + 1f,

			// Bounce easing
			Ease.InBounce => 1f - EaseBounceOut(1f - x),
			Ease.OutBounce => EaseBounceOut(x),
			Ease.InOutBounce => x < 0.5f
				? (1f - EaseBounceOut(1f - 2f * x)) / 2f
				: (1f + EaseBounceOut(2f * x - 1f)) / 2f,

			_ => x // Fallback to linear
		};
	}

	/// <summary>
	/// Helper function for bounce easing calculations.
	/// </summary>
	private static float EaseBounceOut(float x)
	{
		if (x < 1f / D1)
		{
			return N1 * x * x;
		}
		else if (x < 2f / D1)
		{
			var x2 = x - 1.5f / D1;
			return N1 * x2 * x2 + 0.75f;
		}
		else if (x < 2.5f / D1)
		{
			var x2 = x - 2.25f / D1;
			return N1 * x2 * x2 + 0.9375f;
		}
		else
		{
			var x2 = x - 2.625f / D1;
			return N1 * x2 * x2 + 0.984375f;
		}
	}

	/// <summary>
	/// Clamps a value between min and max.
	/// </summary>
	private static float Clamp(float value, float min, float max)
	{
		if (value < min) return min;
		if (value > max) return max;
		return value;
	}
}
