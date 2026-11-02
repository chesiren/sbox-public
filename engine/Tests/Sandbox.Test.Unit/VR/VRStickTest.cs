// FORK-VR: file added by the fork, absent from upstream
using Sandbox.VR;

namespace VRTests;

[TestClass]
public class VRStickTest
{
	/// <summary>
	/// A resting stick's drift scrolls nothing.
	/// </summary>
	[TestMethod]
	public void DeadZoneScrollsNothing()
	{
		var wheel = VRStick.ToWheel( new Vector2( 0.1f, 0.15f ), 12, 1 );

		Assert.AreEqual( Vector2.Zero, wheel );
	}

	/// <summary>
	/// Full tilt scrolls the full speed, in the stick's direction.
	/// </summary>
	[TestMethod]
	public void FullTiltScrollsFullSpeed()
	{
		var wheel = VRStick.ToWheel( new Vector2( 0, 1 ), 12, 0.5f );

		Assert.AreEqual( 0f, wheel.x, 0.001f );
		Assert.AreEqual( 6f, wheel.y, 0.001f );
	}

	/// <summary>
	/// Half way between the dead zone and full tilt scrolls a quarter as fast, so light pushes are fine.
	/// </summary>
	[TestMethod]
	public void CurveIsSquared()
	{
		var half = VRStick.DeadZone + (1 - VRStick.DeadZone) * 0.5f;
		var wheel = VRStick.ToWheel( new Vector2( 0, -half ), 12, 1 );

		Assert.AreEqual( -3f, wheel.y, 0.001f );
	}

	/// <summary>
	/// The pointer hand setting lets through only the hands it names.
	/// </summary>
	[TestMethod]
	public void PointerHandFilters()
	{
		var previous = VRPreferences.PointerHand;

		try
		{
			VRPreferences.PointerHand = "left";
			Assert.IsTrue( VRPreferences.CanPoint( leftHand: true ) );
			Assert.IsFalse( VRPreferences.CanPoint( leftHand: false ) );

			VRPreferences.PointerHand = "both";
			Assert.IsTrue( VRPreferences.CanPoint( leftHand: true ) );
			Assert.IsTrue( VRPreferences.CanPoint( leftHand: false ) );
		}
		finally
		{
			VRPreferences.PointerHand = previous;
		}
	}
}
