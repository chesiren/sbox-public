// FORK-VR: file added by the fork, absent from upstream
using Sandbox.VR;

namespace VRTests;

[TestClass]
public class VRGameFilterTest
{
	static readonly string[] GameTags = ["game", "Shooter"];

	/// <summary>
	/// On the desktop every game is listed but those that are VR only.
	/// </summary>
	[TestMethod]
	public void KeepsDesktopGamesOutsideVR()
	{
		Assert.IsTrue( VRGameFilter.Keep( false, GameTags, new ControlModeSettings { Keyboard = true } ) );
		Assert.IsTrue( VRGameFilter.Keep( false, GameTags, new ControlModeSettings { VR = true, Gamepad = true } ) );
		Assert.IsTrue( VRGameFilter.Keep( false, GameTags, null ) );
		Assert.IsFalse( VRGameFilter.Keep( false, GameTags, new ControlModeSettings { VR = true } ) );
	}

	/// <summary>
	/// A game that supports both plays on both; one that supports a single side only plays there.
	/// </summary>
	[TestMethod]
	public void PlaysWhereItsControlsAre()
	{
		var both = new ControlModeSettings { VR = true, Keyboard = true };
		var vrOnly = new ControlModeSettings { VR = true };
		var flat = new ControlModeSettings { Keyboard = true };

		Assert.IsTrue( VRGameFilter.CanPlay( true, both ) );
		Assert.IsTrue( VRGameFilter.CanPlay( false, both ) );
		Assert.IsTrue( VRGameFilter.CanPlay( true, vrOnly ) );
		Assert.IsFalse( VRGameFilter.CanPlay( false, vrOnly ) );
		Assert.IsFalse( VRGameFilter.CanPlay( true, flat ) );
		Assert.IsTrue( VRGameFilter.CanPlay( false, flat ) );

		Assert.IsNull( VRGameFilter.WhyCantPlay( true, both ) );
		Assert.IsNotNull( VRGameFilter.WhyCantPlay( true, flat ) );
		Assert.IsNotNull( VRGameFilter.WhyCantPlay( false, vrOnly ) );
	}

	/// <summary>
	/// Maps are kept on the desktop too.
	/// </summary>
	[TestMethod]
	public void KeepsMapsOutsideVR()
	{
		Assert.IsTrue( VRGameFilter.Keep( false, ["map"], new ControlModeSettings { VR = true } ) );
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
