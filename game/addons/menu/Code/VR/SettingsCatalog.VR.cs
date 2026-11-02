// FORK-VR: file added by the fork, absent from upstream
using Sandbox;
using Sandbox.UI;
using Sandbox.VR;

namespace MenuProject.Settings;

public partial class SettingsCatalog
{
	/// <summary>
	/// The VR page, only in VR. The display page goes instead: its window, resolution and frame rate rows only reach the desktop mirror.
	/// </summary>
	void AddVR()
	{
		if ( !Application.IsVR )
			return;

		Categories.RemoveAll( x => x.Id == "display" );

		var index = Categories.FindIndex( x => x.Id == "controls" ) + 1;
		Categories.Insert( index, new() { Id = "vr", Title = "VR", Icon = "view_in_ar", Blurb = "Where the menus float and how you point at them." } );

		Items.Add( new SettingItem
		{
			Id = "vr.pointer_hand",
			Category = "vr",
			Section = "Pointing",
			Title = "Pointer hand",
			Description = "Which hands point at menus. With both, the last hand to pull its trigger points in game.",
			Options = [new( "Both", "both" ), new( "Left", "left" ), new( "Right", "right" )],
			Read = () => VRPreferences.PointerHand,
			Write = value => ConsoleSystem.SetValue( "vr_ui_pointer_hand", value?.ToString() )
		} );

		AddVRSlider( "vr.scroll_speed", "Pointing", "Scroll speed", "How fast the stick scrolls, pushed all the way.", "vr_ui_scroll_speed", 2, 40, 1 );

		AddVRSlider( "vr.menu_distance", "Pause menu", "Distance", "How far in front of you the pause menu opens.", "vr_ui_distance", 30, 120, 1 );
		AddVRSlider( "vr.menu_width", "Pause menu", "Size", "How wide the pause menu is.", "vr_ui_width", 32, 128, 1 );

		Items.Add( new SettingItem
		{
			Id = "vr.hud_follow",
			Category = "vr",
			Section = "Notifications",
			Title = "Follow your head",
			Description = "Notifications, chat and subtitles catch up when you turn away. Off, they stay where they first showed.",
			Options = OffOn,
			Read = () => VRPreferences.HudFollow,
			Write = value => ConsoleSystem.SetValue( "vr_ui_hud_follow", ToBool( value ) )
		} );

		AddVRSlider( "vr.hud_distance", "Notifications", "Distance", "How far in front of you notifications, chat and subtitles float.", "vr_ui_hud_distance", 30, 120, 1 );
		AddVRSlider( "vr.hud_width", "Notifications", "Size", "How wide the area holding notifications, chat and subtitles is.", "vr_ui_hud_width", 32, 128, 1 );

		Items.Add( new SettingItem
		{
			Id = "vr.recenter",
			Category = "vr",
			Section = "Notifications",
			Title = "Recenter",
			Description = "Brings the pause menu and notifications back in front of you.",
			Kind = SettingKind.Action,
			ActionLabel = "Recenter",
			IsVisible = () => Sandbox.Game.InGame,
			Invoke = VRGamePanelHost.Recenter
		} );
	}

	void AddVRSlider( string id, string section, string title, string description, string convar, float min, float max, float step )
	{
		Items.Add( new SettingItem
		{
			Id = id,
			Category = "vr",
			Section = section,
			Title = title,
			Description = description,
			Kind = SettingKind.Slider,
			Min = min,
			Max = max,
			Step = step,
			NumberFormat = "0",
			Read = () => ConsoleSystem.GetValue( convar ).ToFloat(),
			Write = value => ConsoleSystem.SetValue( convar, ToFloat( value ) )
		} );
	}
}
