// FORK-VR: file added by the fork, absent from upstream
namespace Sandbox.VR;

/// <summary>
/// Which Workshop items the menu lists in VR: only games that can be played in a headset. The Workshop can't be asked
/// for those, so its answers are filtered as they come in.
/// </summary>
internal static class VRGameFilter
{
	/// <summary>
	/// Whether to list an item with these Workshop <paramref name="tags"/> and published <paramref name="modes"/>.
	/// Anything but a game is kept; a game that never said it supports VR is not.
	/// </summary>
	public static bool Keep( bool inVR, IEnumerable<string> tags, ControlModeSettings modes )
	{
		if ( !inVR )
			return true;

		if ( tags is null || !tags.Contains( "game", StringComparer.OrdinalIgnoreCase ) )
			return true;

		return modes?.VR == true;
	}
}
