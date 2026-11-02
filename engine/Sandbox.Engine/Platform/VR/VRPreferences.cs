// FORK-VR: file added by the fork, absent from upstream
namespace Sandbox.VR;

/// <summary>
/// The player's choices for the menu's UI in VR, saved between sessions and set from the VR settings page.
/// </summary>
public static class VRPreferences
{
	/// <summary>
	/// How far in front of the head the pause menu opens, in world units.
	/// </summary>
	[ConVar( "vr_ui_distance", ConVarFlags.Saved | ConVarFlags.Protected, Help = "How far in front of the head the VR pause menu opens" )]
	public static float MenuDistance { get; set; } = 60.0f;

	/// <summary>
	/// How wide the pause menu is, in world units.
	/// </summary>
	[ConVar( "vr_ui_width", ConVarFlags.Saved | ConVarFlags.Protected, Help = "How wide the VR pause menu is" )]
	public static float MenuWidth { get; set; } = 64.0f;

	/// <summary>
	/// How far in front of the head the HUD floats, in world units.
	/// </summary>
	[ConVar( "vr_ui_hud_distance", ConVarFlags.Saved | ConVarFlags.Protected, Help = "How far in front of the head the VR HUD floats" )]
	public static float HudDistance { get; set; } = 50.0f;

	/// <summary>
	/// How wide the HUD is, in world units.
	/// </summary>
	[ConVar( "vr_ui_hud_width", ConVarFlags.Saved | ConVarFlags.Protected, Help = "How wide the VR HUD is" )]
	public static float HudWidth { get; set; } = 56.0f;

	/// <summary>
	/// Whether the HUD follows the head. Off, it stays where it first showed until the panels are recentered.
	/// </summary>
	[ConVar( "vr_ui_hud_follow", ConVarFlags.Saved | ConVarFlags.Protected, Help = "Whether the VR HUD follows the head" )]
	public static bool HudFollow { get; set; } = true;

	/// <summary>
	/// Which hands point at the UI: <c>both</c>, <c>left</c> or <c>right</c>.
	/// </summary>
	[ConVar( "vr_ui_pointer_hand", ConVarFlags.Saved | ConVarFlags.Protected, Help = "Which hands point at the VR UI: both, left or right" )]
	public static string PointerHand { get; set; } = "both";

	/// <summary>
	/// Wheel notches a second with the stick pushed all the way.
	/// </summary>
	[ConVar( "vr_ui_scroll_speed", ConVarFlags.Saved | ConVarFlags.Protected, Help = "How fast the stick scrolls the VR UI" )]
	public static float ScrollSpeed { get; set; } = 12.0f;

	/// <summary>
	/// Whether this hand may point at the UI, going by <see cref="PointerHand"/>.
	/// </summary>
	public static bool CanPoint( bool leftHand )
	{
		return PointerHand switch
		{
			"left" => leftHand,
			"right" => !leftHand,
			_ => true
		};
	}
}
