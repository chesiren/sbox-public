// FORK-VR: file added by the fork, absent from upstream
namespace Sandbox.VR;

/// <summary>
/// Where UI panels go around the player in VR. Kept free of engine state so it can be unit tested.
/// </summary>
internal static class VRPanelPlacement
{
	/// <summary>
	/// An upright transform <paramref name="distance"/> ahead of the head and <paramref name="drop"/> below eye level.
	/// It faces the way the head does, which is how a world panel shows its front to the player.
	/// </summary>
	public static Transform InFrontOf( Transform head, float distance, float drop )
	{
		// Only the yaw, so looking up or tilting the head doesn't tip the panel over
		var rotation = new Angles( 0, head.Rotation.Yaw(), 0 ).ToRotation();
		var position = head.Position + rotation.Forward * distance + Vector3.Down * drop;

		return new Transform( position, rotation );
	}

	/// <summary>
	/// Moves a tracked pose from around the tracked head to around the camera that draws the head.
	/// Games can move their camera without a <c>VRAnchor</c>, and the hands still have to line up with what's seen.
	/// </summary>
	public static Transform RelativeTo( Transform pose, Transform trackedHead, Transform camera )
	{
		return camera.ToWorld( trackedHead.ToLocal( pose ) );
	}

	/// <summary>
	/// The uniform scale that makes a world panel <paramref name="panelWidth"/> pixels wide span <paramref name="worldWidth"/> units.
	/// </summary>
	public static float ScaleForWidth( float panelWidth, float worldWidth )
	{
		return worldWidth / (panelWidth * Sandbox.UI.WorldPanel.ScreenToWorldScale);
	}

	/// <summary>
	/// Whether a panel at <paramref name="placed"/> has drifted from <paramref name="target"/> enough to start moving.
	/// Following the head only past a dead zone keeps a panel still through small head movements, which is far less tiring than one glued to the view.
	/// </summary>
	public static bool ShouldFollow( Transform placed, Transform target, float maxAngle, float maxDistance )
	{
		return Vector3.GetAngle( placed.Rotation.Forward, target.Rotation.Forward ) > maxAngle
			|| placed.Position.Distance( target.Position ) > maxDistance;
	}

	/// <summary>
	/// Whether a following panel is close enough to <paramref name="target"/> to stop.
	/// </summary>
	public static bool HasArrived( Transform placed, Transform target )
	{
		return Vector3.GetAngle( placed.Rotation.Forward, target.Rotation.Forward ) < 1.0f
			&& placed.Position.Distance( target.Position ) < 0.5f;
	}

	/// <summary>
	/// Moves <paramref name="current"/> towards <paramref name="target"/>, covering the same share of the way per second whatever the frame rate.
	/// <paramref name="speed"/> is how quickly it closes in: about 63% of the way is covered in 1/speed seconds.
	/// </summary>
	public static Transform Approach( Transform current, Transform target, float speed, float delta )
	{
		var fraction = 1.0f - MathF.Exp( -speed * delta );

		return new Transform(
			Vector3.Lerp( current.Position, target.Position, fraction ),
			Rotation.Slerp( current.Rotation, target.Rotation, fraction ),
			current.Scale );
	}
}
