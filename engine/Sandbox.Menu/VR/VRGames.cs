// FORK-VR: file added by the fork, absent from upstream
namespace Sandbox.VR;

/// <summary>
/// What the menu addon needs to know about games and VR: the default game in a headset, and which games can be
/// played here.
/// </summary>
public static class VRGames
{
	/// <summary>
	/// Sandbox VR's Workshop item, the fork's own Sandbox playable in VR and on the desktop: the game the menu opens on
	/// in VR before anything's been picked, since Facepunch's Sandbox is desktop only.
	/// </summary>
	public const ulong SandboxVRWorkshopId = 3833868585;

	/// <summary>
	/// The Workshop item of the game the menu opens on: Sandbox VR in VR, <paramref name="desktop"/> otherwise.
	/// </summary>
	public static ulong DefaultGame( ulong desktop ) => Application.IsVR ? SandboxVRWorkshopId : desktop;

	/// <summary>
	/// The cookie the last picked game is kept in, one for VR and one for the desktop, so a headset doesn't open on a
	/// game picked on the desktop that it can't play, and the other way round.
	/// </summary>
	public static string GameCookie( string desktop ) => Application.IsVR ? $"{desktop}.vr" : desktop;

	/// <summary>
	/// Why a game with these published <paramref name="modes"/> can't be played here, or null if it can: in VR it must
	/// support VR, on the desktop it mustn't be VR only.
	/// </summary>
	public static string WhyCantPlay( ControlModeSettings modes ) => VRGameFilter.WhyCantPlay( Application.IsVR, modes );

	public static string WhyCantPlay( WorkshopItemDetails game ) => WhyCantPlay( game?.GetMeta<ControlModeSettings>( "ControlModes", null ) );

	public static string WhyCantPlay( Package game ) => WhyCantPlay( game?.GetMeta<ControlModeSettings>( "ControlModes", null ) );
}
