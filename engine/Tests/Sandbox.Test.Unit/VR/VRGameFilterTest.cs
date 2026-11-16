// FORK-VR: file added by the fork, absent from upstream
using Sandbox.VR;

namespace VRTests;

[TestClass]
public class VRGameFilterTest
{
	static readonly string[] GameTags = ["game", "Shooter"];

	/// <summary>
	/// On the desktop every game is listed, whatever it supports.
	/// </summary>
	[TestMethod]
	public void KeepsEverythingOutsideVR()
	{
		Assert.IsTrue( VRGameFilter.Keep( false, GameTags, new ControlModeSettings { Keyboard = true } ) );
		Assert.IsTrue( VRGameFilter.Keep( false, GameTags, null ) );
	}

	/// <summary>
	/// In VR a game is listed if it supports VR, alone or with other controls.
	/// </summary>
	[TestMethod]
	public void KeepsVRGames()
	{
		Assert.IsTrue( VRGameFilter.Keep( true, GameTags, new ControlModeSettings { VR = true } ) );
		Assert.IsTrue( VRGameFilter.Keep( true, GameTags, new ControlModeSettings { VR = true, Keyboard = true } ) );
	}

	/// <summary>
	/// In VR a flat game, or one that never published its controls, is hidden.
	/// </summary>
	[TestMethod]
	public void HidesFlatGames()
	{
		Assert.IsFalse( VRGameFilter.Keep( true, GameTags, new ControlModeSettings { Keyboard = true, Gamepad = true } ) );
		Assert.IsFalse( VRGameFilter.Keep( true, GameTags, null ) );
		Assert.IsFalse( VRGameFilter.Keep( true, ["Game"], null ) );
	}

	/// <summary>
	/// Maps go through the same lists and have no controls of their own, so they're kept.
	/// </summary>
	[TestMethod]
	public void KeepsMaps()
	{
		Assert.IsTrue( VRGameFilter.Keep( true, ["map"], null ) );
		Assert.IsTrue( VRGameFilter.Keep( true, null, null ) );
	}
}
