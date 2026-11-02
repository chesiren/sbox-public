// FORK-VR: file added by the fork, absent from upstream
using Sandbox;
using Sandbox.UI;
using Sandbox.VR;

namespace SboxMenu;

/// <summary>
/// Decides where the menu's UI goes in VR. The overlay it normally goes on is drawn flat on the desktop window,
/// where the headset can't see it: in the menu it goes on the menu's world panel, in a game on a panel in the game's world.
/// </summary>
public static class VRUIHost
{
	static VRModalHost _menuHost;
	static VRModalHost _gameHost;

	/// <summary>
	/// Where modals and the pause menu go, or null to use the overlay as usual.
	/// </summary>
	public static Panel GetModalParent()
	{
		if ( !Application.IsVR )
			return null;

		return Sandbox.Game.InGame ? GetGameHost() : GetMenuHost();
	}

	static Panel GetMenuHost()
	{
		var menu = MenuProject.MainMenu.Instance?.Panel;
		if ( !menu.IsValid() )
			return null;

		// The menu's panel is made again when its scene is
		if ( !_menuHost.IsValid() || _menuHost.Parent != menu )
			_menuHost = menu.AddChild<VRModalHost>();

		return _menuHost;
	}

	static Panel GetGameHost()
	{
		var panel = VRGamePanelHost.HostPanel;
		if ( !panel.IsValid() )
			return null;

		// Made again with each game scene, which takes the modals on it along
		if ( !_gameHost.IsValid() || _gameHost.Parent != panel )
			_gameHost = panel.AddChild<VRModalHost>();

		return _gameHost;
	}
}
