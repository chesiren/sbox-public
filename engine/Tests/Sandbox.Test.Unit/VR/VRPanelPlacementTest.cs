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

	/// <summary>
	/// Small head movements stay inside the dead zone; turning past the angle or stepping past the distance starts a follow.
	/// </summary>
	[TestMethod]
	public void ShouldFollowPastDeadZone()
	{
		var placed = new Transform( new Vector3( 50, 0, 60 ) );

		var nudge = new Transform( new Vector3( 52, 0, 60 ), new Angles( 0, 10, 0 ).ToRotation() );
		var turn = new Transform( new Vector3( 50, 0, 60 ), new Angles( 0, 40, 0 ).ToRotation() );
		var step = new Transform( new Vector3( 70, 0, 60 ) );

		Assert.IsFalse( VRPanelPlacement.ShouldFollow( placed, nudge, 25, 12 ) );
		Assert.IsTrue( VRPanelPlacement.ShouldFollow( placed, turn, 25, 12 ) );
		Assert.IsTrue( VRPanelPlacement.ShouldFollow( placed, step, 25, 12 ) );
	}

	/// <summary>
	/// Approaching closes the same share of the gap for the same time, whether in one step or many.
	/// </summary>
	[TestMethod]
	public void ApproachIsFrameRateIndependent()
	{
		var start = new Transform( Vector3.Zero );
		var target = new Transform( new Vector3( 100, 0, 0 ) );

		var once = VRPanelPlacement.Approach( start, target, 4, 0.5f );

		var stepped = start;
		for ( int i = 0; i < 10; i++ )
			stepped = VRPanelPlacement.Approach( stepped, target, 4, 0.05f );

		Assert.AreEqual( once.Position.x, stepped.Position.x, 0.01f );
		Assert.IsTrue( once.Position.x > 0 && once.Position.x < 100 );
	}

	/// <summary>
	/// A follow ends once the panel is all but on its target.
	/// </summary>
	[TestMethod]
	public void HasArrivedWhenClose()
	{
		var target = new Transform( new Vector3( 100, 0, 0 ) );

		Assert.IsTrue( VRPanelPlacement.HasArrived( new Transform( new Vector3( 99.8f, 0, 0 ) ), target ) );
		Assert.IsFalse( VRPanelPlacement.HasArrived( new Transform( new Vector3( 95, 0, 0 ) ), target ) );
	}

	/// <summary>
	/// A follow holds still inside the dead zone, then once started keeps going until the panel arrives, even back inside it.
	/// </summary>
	[TestMethod]
	public void HeadFollowStartsPastDeadZoneAndFinishes()
	{
		var follow = new VRHeadFollow();
		var placed = new Transform( Vector3.Zero );

		var still = follow.Tick( placed, new Transform( new Vector3( 5, 0, 0 ) ), 0.1f );
		Assert.AreEqual( 0f, still.Position.x, 0.001f );

		var target = new Transform( new Vector3( 40, 0, 0 ) );
		var current = follow.Tick( placed, target, 0.1f );
		Assert.IsTrue( current.Position.x > 0 );

		for ( int i = 0; i < 200; i++ )
			current = follow.Tick( current, target, 0.1f );

		Assert.AreEqual( 40f, current.Position.x, 0.5f );
	}

	/// <summary>
	/// The anchor puts the tracked head where the view was and turns the play area so the head looks the view's way.
	/// </summary>
	[TestMethod]
	public void AnchorForPutsHeadAtView()
	{
		var head = new Transform( new Vector3( 10, 5, 64 ), new Angles( 20, 30, 0 ).ToRotation() );
		var view = new Transform( new Vector3( 200, -50, 40 ), new Angles( 0, 90, 0 ).ToRotation() );

		var anchor = VRPanelPlacement.AnchorFor( head, view );
		var world = anchor.ToWorld( head );

		Assert.AreEqual( 200f, world.Position.x, 0.01f );
		Assert.AreEqual( -50f, world.Position.y, 0.01f );
		Assert.AreEqual( 40f, world.Position.z, 0.01f );
		Assert.AreEqual( 90f, world.Rotation.Yaw(), 0.1f );
	}
}
