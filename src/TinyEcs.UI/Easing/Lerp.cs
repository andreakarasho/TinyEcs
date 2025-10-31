using System.Numerics;
using Flexbox;

namespace TinyEcs.UI.Easing;

/// <summary>
/// Linear interpolation utilities for smooth animations.
/// Ported from sickle_ui's sickle_math::lerp module.
/// </summary>
public static class LerpExtensions
{
	/// <summary>
	/// Linear interpolation between two float values.
	/// </summary>
	/// <param name="a">Start value</param>
	/// <param name="b">End value</param>
	/// <param name="t">Interpolation factor (0-1)</param>
	/// <returns>Interpolated value</returns>
	public static float Lerp(this float a, float b, float t)
	{
		return a + (b - a) * t;
	}

	/// <summary>
	/// Linear interpolation between two Vector2 values.
	/// </summary>
	/// <param name="a">Start vector</param>
	/// <param name="b">End vector</param>
	/// <param name="t">Interpolation factor (0-1)</param>
	/// <returns>Interpolated vector</returns>
	public static Vector2 Lerp(this Vector2 a, Vector2 b, float t)
	{
		return new Vector2(
			a.X.Lerp(b.X, t),
			a.Y.Lerp(b.Y, t)
		);
	}

	/// <summary>
	/// Linear interpolation between two Vector4 values.
	/// Commonly used for color animations (RGBA).
	/// </summary>
	/// <param name="a">Start vector (color)</param>
	/// <param name="b">End vector (color)</param>
	/// <param name="t">Interpolation factor (0-1)</param>
	/// <returns>Interpolated vector (color)</returns>
	public static Vector4 Lerp(this Vector4 a, Vector4 b, float t)
	{
		return new Vector4(
			a.X.Lerp(b.X, t),
			a.Y.Lerp(b.Y, t),
			a.Z.Lerp(b.Z, t),
			a.W.Lerp(b.W, t)
		);
	}

	/// <summary>
	/// Linear interpolation between two FlexValue values.
	/// Only works for Point-based values (not percentages or auto).
	/// </summary>
	/// <param name="a">Start value</param>
	/// <param name="b">End value</param>
	/// <param name="t">Interpolation factor (0-1)</param>
	/// <returns>Interpolated FlexValue, or 'a' if types don't match</returns>
	public static Bevy.FlexValue Lerp(this Bevy.FlexValue a, Bevy.FlexValue b, float t)
	{
		// Only interpolate if both are Points
		if (a.Unit == Unit.Point && b.Unit == Unit.Point)
		{
			var interpolated = a.Value.Lerp(b.Value, t);
			return Bevy.FlexValue.Points(interpolated);
		}

		// If types don't match or not Points, return start value
		// (could also throw, but this is more forgiving for animation edge cases)
		return a;
	}

	/// <summary>
	/// Clamps a value between min and max.
	/// Helper for ensuring interpolation factors stay in valid range.
	/// </summary>
	/// <param name="value">Value to clamp</param>
	/// <param name="min">Minimum value</param>
	/// <param name="max">Maximum value</param>
	/// <returns>Clamped value</returns>
	public static float Clamp(this float value, float min, float max)
	{
		if (value < min) return min;
		if (value > max) return max;
		return value;
	}

	/// <summary>
	/// Inverse lerp - converts a value in range [a, b] to normalized [0, 1].
	/// Useful for calculating animation progress from absolute values.
	/// </summary>
	/// <param name="a">Range start</param>
	/// <param name="b">Range end</param>
	/// <param name="value">Value to normalize</param>
	/// <returns>Normalized value (0-1)</returns>
	public static float InverseLerp(float a, float b, float value)
	{
		if (System.Math.Abs(b - a) < 0.0001f)
			return 0f;

		return ((value - a) / (b - a)).Clamp(0f, 1f);
	}

	/// <summary>
	/// Smoothly interpolates between a and b using Hermite interpolation.
	/// Provides smoother transitions than linear interpolation.
	/// </summary>
	/// <param name="a">Start value</param>
	/// <param name="b">End value</param>
	/// <param name="t">Interpolation factor (0-1)</param>
	/// <returns>Smoothly interpolated value</returns>
	public static float SmoothStep(float a, float b, float t)
	{
		var clamped = t.Clamp(0f, 1f);
		var smoothed = clamped * clamped * (3f - 2f * clamped);
		return a.Lerp(b, smoothed);
	}

	/// <summary>
	/// Even smoother interpolation using a 5th-order polynomial.
	/// Provides zero first and second derivatives at boundaries.
	/// </summary>
	/// <param name="a">Start value</param>
	/// <param name="b">End value</param>
	/// <param name="t">Interpolation factor (0-1)</param>
	/// <returns>Smoothly interpolated value</returns>
	public static float SmootherStep(float a, float b, float t)
	{
		var clamped = t.Clamp(0f, 1f);
		var smoothed = clamped * clamped * clamped * (clamped * (clamped * 6f - 15f) + 10f);
		return a.Lerp(b, smoothed);
	}
}
