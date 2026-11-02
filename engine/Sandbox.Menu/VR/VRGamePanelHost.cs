// FORK-VR: file added by the fork, absent from upstream
using Sandbox.Engine;
using Sandbox.UI;
using System;

namespace Sandbox.VR;

/// <summary>
/// A world panel in the running game's scene, for the menu's UI in VR: the pause menu and the modals opened over the game.
/// The menu overlay is drawn flat on the desktop window, which the headset never shows, and the menu scene is suspended in game.
/// The panel belongs to the menu's UI system but sits in the game's world, so the game's stereo camera draws it.
/// </summary>
[Hide]
public static class VRGamePanelHost
{
	/// <summary>
	/// How far in front of the head the panel opens, in world units.
	/// </summary>
	public static float Distance { get; set; } = 60.0f;

	/// <summary>
	/// How wide the panel is, in world units.
	/// </summary>
	public static float Width { get; set; } = 64.0f;

	/// <summary>
	/// How far below eye level the middle of the panel sits, in world units.
	/// </summary>
	public static float Drop { get; set; } = 6.0f;

	/// <summary>
	/// The trigger has to be pulled this far to click.
	/// </summary>
	const float PressThreshold = 0.75f;

	/// <summary>
	/// Twice the menu's 1920x1080 layout, since world panels default to a scale of 2.
	/// </summary>
	static readonly Rect PanelBounds = new( -1920, -1080, 3840, 2160 );

	static Sandbox.UI.WorldPanel _root;
	static SceneWorld _world;
	static Panel _cursor;
	static SceneLineObject _beam;

	static readonly WorldPanelInput _input = new();
	static readonly List<RootPanel> _roots = new();

	static bool _wasShowing;
	static bool _useLeftHand;
	static bool _menuWasDown;
	static Panel _lastHovered;

	/// <summary>
	/// The panel to put the menu's UI on while a game runs in VR. Null outside VR, outside a game, or while the game has no camera.
	/// </summary>
	public static Panel HostPanel => EnsureRoot() ? _root : null;

	/// <summary>
	/// Puts the panel back in front of the player's head.
	/// </summary>
	public static void Recenter()
	{
		if ( TryGetCamera( out var camera ) && _root.IsValid() )
			Place( camera.WorldTransform );
	}

	/// <summary>
	/// True on the frame either controller's menu button goes down.
	/// </summary>
	internal static bool MenuButtonPressed()
	{
		if ( !Application.IsVR || Input.VR is not { } vr )
			return false;

		var down = vr.LeftHand.ButtonMenu.IsPressed || vr.RightHand.ButtonMenu.IsPressed;
		var pressed = down && !_menuWasDown;
		_menuWasDown = down;

		return pressed;
	}

	internal static void Tick()
	{
		// Only made when something asks for it, so a game nobody pauses pays nothing
		if ( !_root.IsValid() )
			return;

		try
		{
			if ( !EnsureRoot() || !TryGetCamera( out var camera ) )
				return;

			var head = camera.WorldTransform;

			// Opens in front of wherever the player is looking, then stays put in the world
			var showing = _root.ChildrenWantMouseInput;
			if ( showing && !_wasShowing ) Place( head );
			_wasShowing = showing;

			if ( showing )
				TickPointer( head );
			else
				HidePointer();
		}
		catch ( System.Exception e )
		{
			Log.Error( e, "Error in VR game panel tick" );
		}
	}

	static bool TryGetCamera( out CameraComponent camera )
	{
		camera = GlobalContext.Game?.ActiveScene?.Camera;
		return camera.IsValid();
	}

	static bool EnsureRoot()
	{
		var scene = GlobalContext.Game?.ActiveScene;
		if ( !Application.IsVR || !Game.InGame || !scene.IsValid() || scene.SceneWorld is null )
		{
			Destroy();
			return false;
		}

		if ( _root.IsValid() && _world == scene.SceneWorld )
			return true;

		// A new game scene has its own world, so the panel is made again in it
		Destroy();

		using var _ = GlobalContext.MenuScope();

		_world = scene.SceneWorld;
		_root = new Sandbox.UI.WorldPanel( _world ) { PanelBounds = PanelBounds };

		_cursor = _root.AddChild<Panel>();
		_cursor.Style.Position = PositionMode.Absolute;
		_cursor.Style.Width = 16;
		_cursor.Style.Height = 16;
		_cursor.Style.BorderTopLeftRadius = 8;
		_cursor.Style.BorderTopRightRadius = 8;
		_cursor.Style.BorderBottomLeftRadius = 8;
		_cursor.Style.BorderBottomRightRadius = 8;
		_cursor.Style.BackgroundColor = Color.White.WithAlpha( 0.8f );
		_cursor.Style.PointerEvents = PointerEvents.None;
		_cursor.Style.ZIndex = 100000;
		_cursor.Style.Display = DisplayMode.None;

		_beam = new SceneLineObject( _world );
		_beam.RenderingEnabled = false;
		_beam.Face = SceneLineObject.FaceMode.Camera;
		_beam.Opaque = false;
		_beam.Flags.CastShadows = false;
		_beam.Flags.IsOpaque = false;
		_beam.Flags.IsTranslucent = true;
		_beam.Attributes.SetCombo( "D_BLEND", 1 );

		if ( TryGetCamera( out var camera ) )
			Place( camera.WorldTransform );

		return true;
	}

	static void Destroy()
	{
		_input.CancelPointerInteraction();
		_input.Clear();

		_beam?.Delete();
		_beam = null;

		if ( _root.IsValid() )
			_root.Delete( true );

		_root = null;
		_cursor = null;
		_world = null;
		_wasShowing = false;
		_lastHovered = null;
	}

	static void Place( Transform head )
	{
		var transform = VRPanelPlacement.InFrontOf( head, Distance, Drop );
		_root.Transform = transform.WithScale( VRPanelPlacement.ScaleForWidth( PanelBounds.Width, Width ) );
	}

	static void TickPointer( Transform head )
	{
		if ( Input.VR is not { } vr )
			return;

		// Point with whichever hand last pulled its trigger
		if ( vr.RightHand.Trigger.Value > PressThreshold ) _useLeftHand = false;
		else if ( vr.LeftHand.Trigger.Value > PressThreshold ) _useLeftHand = true;

		var hand = _useLeftHand ? vr.LeftHand : vr.RightHand;
		var aim = VRPanelPlacement.RelativeTo( hand.AimTransform, vr.Head, head );
		var ray = new Ray( aim.Position, aim.Rotation.Forward );

		_input.Ray = ray;
		_input.MouseLeftPressed = hand.Trigger.Value > PressThreshold;
		_input.MouseRightPressed = hand.Grip.Value > PressThreshold;
		_input.MouseWheel = hand.Joystick.Value;

		_roots.Clear();
		_roots.Add( _root );
		_input.Tick( _roots, true );
		_roots.Clear();

		var hovered = _input.Hovered;
		if ( hovered is not null && hovered != _lastHovered )
			hand.Rumble( 0.01f, 150.0f, 0.15f );

		_lastHovered = hovered;

		UpdateCursor( hovered is not null );
		UpdateBeam( ray, hovered is not null ? _root.WorldDistance : 0.0f, hovered is not null, hand.Trigger.Value );
	}

	static void HidePointer()
	{
		_input.CancelPointerInteraction();
		_input.Clear();
		_lastHovered = null;

		UpdateCursor( false );

		if ( _beam.IsValid() )
			_beam.RenderingEnabled = false;
	}

	static void UpdateCursor( bool visible )
	{
		if ( !_cursor.IsValid() )
			return;

		_cursor.Style.Display = visible ? DisplayMode.Flex : DisplayMode.None;
		if ( !visible )
			return;

		var bounds = _root.Box.Rect;
		var scale = _root.Scale;

		_cursor.Style.Left = (_root.WorldCursor.x - bounds.Left) / scale - 8;
		_cursor.Style.Top = (_root.WorldCursor.y - bounds.Top) / scale - 8;
	}

	static void UpdateBeam( Ray ray, float distance, bool hit, float trigger )
	{
		if ( !_beam.IsValid() )
			return;

		var length = hit ? distance : 32.0f;
		var width = 0.15f;
		var color = Color.White;

		// Fades in just off the controller, brightens with the trigger, and stays faint up to a panel it hits
		_beam.RenderingEnabled = true;
		_beam.Transform = new Transform( ray.Position );
		_beam.StartLine();
		_beam.AddLinePoint( ray.Position, color.WithAlpha( 0.0f ), width );
		_beam.AddLinePoint( ray.Project( MathF.Min( 2.0f, length * 0.25f ) ), color.WithAlpha( 0.6f + 0.4f * trigger ), width );
		_beam.AddLinePoint( ray.Project( length ), color.WithAlpha( hit ? 0.3f : 0.0f ), width );
		_beam.EndLine();
	}
}
