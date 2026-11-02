// FORK-VR: file added by the fork, absent from upstream
namespace Sandbox.VR;

/// <summary>
/// Turns a controller's stick into UI scrolling. Kept free of engine state so it can be unit tested.
/// </summary>
internal static class VRStick
{
	/// <summary>
	/// A resting stick drifts a little, so anything this close to the centre scrolls nothing.
	/// </summary>
	public const float DeadZone = 0.2f;

	/// <summary>
	/// The mouse wheel movement for one frame of <paramref name="delta"/> seconds, at <see cref="VRPreferences.ScrollSpeed"/>.
	/// </summary>
	public static Vector2 ToWheel( Vector2 stick, float delta ) => ToWheel( stick, VRPreferences.ScrollSpeed, delta );

	/// <summary>
	/// The mouse wheel movement for one frame of <paramref name="delta"/> seconds, <paramref name="notchesPerSecond"/> at full tilt.
	/// Squared past the dead zone, so a light push scrolls finely and a full one quickly.
	/// </summary>
	public static Vector2 ToWheel( Vector2 stick, float notchesPerSecond, float delta )
	{
		var length = stick.Length;
		if ( length <= DeadZone )
			return Vector2.Zero;

		var tilt = MathF.Min( (length - DeadZone) / (1.0f - DeadZone), 1.0f );

		return stick / length * (tilt * tilt * notchesPerSecond * delta);
	}
}
