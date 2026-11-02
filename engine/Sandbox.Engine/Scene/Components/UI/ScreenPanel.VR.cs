// FORK-VR: file added by the fork, absent from upstream
using Sandbox.UI;
using Sandbox.VR;

namespace Sandbox;

public sealed partial class ScreenPanel
{
	/// <summary>
	/// In VR, draw this panel on a world panel that floats ahead of the player, since nothing drawn flat on the screen
	/// reaches the headset. Turn it off for UI that's only meant for the desktop window.
	/// </summary>
	[Property, Group( "VR" )] public bool ShowInVR { get; set; } = true;

	/// <summary>
	/// The game's HUD sits behind the menu's, so the two don't overlap.
	/// </summary>
	const float VRExtraDistance = 10.0f;

	/// <summary>
	/// A game's HUD is laid out for the whole screen, so it's given more room than the menu's.
	/// </summary>
	const float VRWidthFactor = 1.4f;

	Sandbox.UI.WorldPanel _vrPanel;
	readonly VRHeadFollow _vrFollow = new();
	RealTimeSince _vrSinceUpdate;
	bool _vrPlaced;

	bool CreateVRPanel()
	{
		if ( !ShowInVR || !Game.IsRunningInVR || Scene.IsEditor || Scene.SceneWorld is null )
			return false;

		_vrPanel = new Sandbox.UI.WorldPanel( Scene.SceneWorld );
		_vrPanel.GameObject = GameObject;
		_vrPanel.Style.Display = DisplayMode.None;

		UpdateVRPanel( snap: true );
		return true;
	}

	bool EnableVRPanel()
	{
		if ( !_vrPanel.IsValid() )
			return false;

		_vrPanel.Style.Display = DisplayMode.Flex;
		EnsurePanelParents();
		return true;
	}

	bool DisableVRPanel()
	{
		if ( !_vrPanel.IsValid() )
			return false;

		_vrPanel.Style.Display = DisplayMode.None;
		return true;
	}

	void DestroyVRPanel()
	{
		_vrPanel?.Delete();
		_vrPanel = null;
	}

	bool UpdateVRPanel( bool snap = false )
	{
		if ( !_vrPanel.IsValid() )
			return false;

		var delta = MathF.Min( _vrSinceUpdate, 0.1f );
		_vrSinceUpdate = 0;

		_vrPanel.Style.ZIndex = ZIndex;
		_vrPanel.Style.Opacity = Opacity;

		// The root's scale can't be set from here, so a bigger UI scale shows less of the layout on the same width instead
		var scale = Scale.Clamp( 0.25f, 5.0f );
		var width = 3840.0f / scale;
		var height = 2160.0f / scale;
		_vrPanel.PanelBounds = new Rect( -width * 0.5f, -height * 0.5f, width, height );

		var camera = TargetCamera ?? Scene.Camera;
		if ( !camera.IsValid() )
			return true;

		var target = VRPanelPlacement.InFrontOf( camera.WorldTransform, VRPreferences.HudDistance + VRExtraDistance, 0.0f )
			.WithScale( VRPanelPlacement.ScaleForWidth( width, VRPreferences.HudWidth * VRWidthFactor ) );

		// Placed outright the first time there's a camera to place it by, then left to follow
		if ( snap || !_vrPlaced )
		{
			_vrPanel.Transform = target;
			_vrFollow.Stop();
			_vrPlaced = true;
		}
		else if ( VRPreferences.HudFollow )
		{
			_vrPanel.Transform = _vrFollow.Tick( _vrPanel.Transform, target, delta );
		}

		return true;
	}
}
