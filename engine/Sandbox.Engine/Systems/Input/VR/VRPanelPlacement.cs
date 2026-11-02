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
}
