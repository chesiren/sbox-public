// FORK-VR: file added by the fork, absent from upstream
namespace Sandbox.VR;

/// <summary>
/// The VR placement math the menu addon needs, which can't reach the engine's internals itself.
/// </summary>
[Hide]
public static class VRMenuPlacement
{
	/// <summary>
	/// The play area anchor that puts the player's head where <paramref name="view"/> is, looking its way.
	/// </summary>
	public static Transform AnchorFor( Transform view )
	{
		var vr = Input.VR;
		if ( vr is null )
			return view;

		return VRPanelPlacement.AnchorFor( vr.Anchor.ToLocal( vr.Head ), view );
	}
}
