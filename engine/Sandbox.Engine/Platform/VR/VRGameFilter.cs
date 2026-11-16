// FORK-VR: file added by the fork, absent from upstream
namespace Sandbox.VR;

/// <summary>
/// Which games can be played, and listed, from a headset or from the desktop, by the control modes they published.
/// The Workshop can't be asked for those, so its answers are filtered as they come in.
/// </summary>
internal static class VRGameFilter
{
	/// <summary>
	/// Whether a game with these <paramref name="modes"/> can be played. In VR it must support VR; on the desktop it
	/// mustn't be VR only. A game that supports both plays on both; one that published no modes is a desktop game.
	/// </summary>
	public static bool CanPlay( bool inVR, ControlModeSettings modes )
	{
		if ( inVR )
			return modes?.VR == true;

		return modes?.IsVROnly != true;
	}

	/// <summary>
	/// Why a game with these <paramref name="modes"/> can't be played, or null if it can.
	/// </summary>
	public static string WhyCantPlay( bool inVR, ControlModeSettings modes )
	{
		if ( CanPlay( inVR, modes ) )
			return null;

		return inVR ? "This game can't be played in VR" : "This game needs a VR headset";
	}

	/// <summary>
	/// Stops a load of <paramref name="game"/> that can't be played here. Every way into a game goes through the load
	/// (the menu, a server, quick play, the command line), so the menu's own checks can't be got around.
	/// </summary>
	public static void ThrowIfCantPlay( Package game )
	{
		var why = WhyCantPlay( Application.IsVR, game?.GetMeta<ControlModeSettings>( "ControlModes", null ) );
		if ( why is not null )
			throw new InvalidOperationException( why );
	}

	/// <summary>
	/// Whether to list an item with these Workshop <paramref name="tags"/> and published <paramref name="modes"/>.
	/// Anything but a game is kept.
	/// </summary>
	public static bool Keep( bool inVR, IEnumerable<string> tags, ControlModeSettings modes )
	{
		if ( tags is null || !tags.Contains( "game", StringComparer.OrdinalIgnoreCase ) )
			return true;

		return CanPlay( inVR, modes );
	}
}
