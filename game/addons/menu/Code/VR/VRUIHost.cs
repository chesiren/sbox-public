// FORK-VR: file added by the fork, absent from upstream
using Sandbox;
using Sandbox.UI;
using Sandbox.VR;

namespace SboxMenu;

/// <summary>
/// Decides where the menu's UI goes in VR. The overlay it normally goes on is drawn flat on the desktop window,
/// where the headset can't see it: in the menu it goes on the menu's world panel, in a game on panels in the game's world.
/// </summary>
public static class VRUIHost
{
	static VRModalHost _menuHost;
	static VRModalHost _gameHost;
	static VRModalHost _hudHost;

	/// <summary>
	/// Where modals and the pause menu go, or null to use the overlay as usual.
	/// </summary>
	public static Panel GetModalParent()
	{
		if ( !Application.IsVR )
			return null;

		return Sandbox.Game.InGame ? Wrap( ref _gameHost, VRGamePanelHost.HostPanel ) : GetMenuHost();
	}

	/// <summary>
	/// Where toasts, chat, subtitles and the loading screen go, or null to use the overlay as usual.
	/// In the menu they share the menu's panel with the modals; in a game they float on a HUD that follows the head.
	/// </summary>
	public static Panel GetOverlayParent()
	{
		if ( !Application.IsVR )
			return null;

		return Sandbox.Game.InGame ? Wrap( ref _hudHost, VRGamePanelHost.HudPanel ) : GetMenuHost();
	}

	static Panel GetMenuHost()
	{
		return Wrap( ref _menuHost, MenuProject.MainMenu.Instance?.Panel );
	}

	/// <summary>
	/// The host on <paramref name="panel"/>, made again whenever the panel is, which happens with each scene.
	/// </summary>
	static Panel Wrap( ref VRModalHost host, Panel panel )
	{
		if ( !panel.IsValid() )
			return null;

		if ( !host.IsValid() || host.Parent != panel )
			host = panel.AddChild<VRModalHost>();

		return host;
	}
}
