// FORK-VR: file added by the fork, absent from upstream
using Sandbox.VR;

namespace VRTests;

[TestClass]
public class VRPanelPlacementTest
{
	/// <summary>
	/// The panel goes straight ahead along the head's yaw and keeps the head's heading, so its front faces the player.
	/// </summary>
	[TestMethod]
	public void InFrontOfFollowsYaw()
	{
		var head = new Transform( new Vector3( 10, 20, 64 ), new Angles( 0, 90, 0 ).ToRotation() );

		var placed = VRPanelPlacement.InFrontOf( head, 60, 0 );

		Assert.AreEqual( 10f, placed.Position.x, 0.001f );
		Assert.AreEqual( 80f, placed.Position.y, 0.001f );
		Assert.AreEqual( 64f, placed.Position.z, 0.001f );
		Assert.AreEqual( 90f, placed.Rotation.Yaw(), 0.01f );
	}

	/// <summary>
	/// Looking up, down or tilting the head doesn't tip the panel or move it off eye level.
	/// </summary>
	[TestMethod]
	public void InFrontOfStaysUpright()
	{
		var head = new Transform( new Vector3( 0, 0, 64 ), new Angles( -45, 0, 30 ).ToRotation() );

		var placed = VRPanelPlacement.InFrontOf( head, 60, 6 );

		Assert.AreEqual( 60f, placed.Position.x, 0.001f );
		Assert.AreEqual( 58f, placed.Position.z, 0.001f );
		Assert.AreEqual( 0f, placed.Rotation.Pitch(), 0.01f );
		Assert.AreEqual( 0f, placed.Rotation.Roll(), 0.01f );
	}

	/// <summary>
	/// A hand tracked 10 units ahead of the tracked head ends up 10 units ahead of the camera, wherever the camera is.
	/// </summary>
	[TestMethod]
	public void RelativeToFollowsCamera()
	{
		var trackedHead = new Transform( new Vector3( 1, 2, 3 ) );
		var hand = new Transform( new Vector3( 11, 2, 3 ) );
		var camera = new Transform( new Vector3( 100, 0, 50 ), new Angles( 0, 90, 0 ).ToRotation() );

		var moved = VRPanelPlacement.RelativeTo( hand, trackedHead, camera );

		Assert.AreEqual( 100f, moved.Position.x, 0.001f );
		Assert.AreEqual( 10f, moved.Position.y, 0.001f );
		Assert.AreEqual( 50f, moved.Position.z, 0.001f );
	}

	/// <summary>
	/// The scale turns the panel's pixel width into the asked world width.
	/// </summary>
	[TestMethod]
	public void ScaleForWidthMatchesWorldWidth()
	{
		var scale = VRPanelPlacement.ScaleForWidth( 3840, 64 );

		Assert.AreEqual( 64f, 3840 * scale * Sandbox.UI.WorldPanel.ScreenToWorldScale, 0.001f );
	}
}
