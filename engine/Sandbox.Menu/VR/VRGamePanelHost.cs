// FORK-VR: file added by the fork, absent from upstream
using Sandbox.Engine;
using Sandbox.UI;
using System;

namespace Sandbox.VR;

/// <summary>
/// World panels in the running game's scene for the menu's UI in VR. The menu overlay is drawn flat on the desktop window,
/// which the headset never shows, and the menu scene is suspended in game.
/// The panels belong to the menu's UI system but sit in the game's world, so the game's stereo camera draws them.
/// </summary>
[Hide]
public static class VRGamePanelHost
{
	static float Distance => VRPreferences.MenuDistance.Clamp( 20.0f, 200.0f );
	static float Width => VRPreferences.MenuWidth.Clamp( 16.0f, 200.0f );
	static float HudDistance => VRPreferences.HudDistance.Clamp( 20.0f, 200.0f );
	static float HudWidth => VRPreferences.HudWidth.Clamp( 16.0f, 200.0f );

	/// <summary>
	/// How far below eye level the middle of the pause menu sits, in world units.
	/// </summary>
	public static float Drop { get; set; } = 6.0f;

	/// <summary>
	/// How far below eye level the middle of the HUD sits, in world units.
	/// </summary>
	public static float HudDrop { get; set; } = 4.0f;

	/// <summary>
	/// The trigger has to be pulled this far to click.
	/// </summary>
	const float PressThreshold = 0.75f;

	/// <summary>
	/// Twice the menu's 1920x1080 layout, since world panels default to a scale of 2.
	/// </summary>
	static readonly Rect PanelBounds = new( -1920, -1080, 3840, 2160 );

	/// <summary>
	/// One world panel and the cursor drawn on it.
	/// </summary>
	sealed class Surface
	{
		public Sandbox.UI.WorldPanel Root;
		public Panel Cursor;
		public bool WasShowing;
		public readonly VRHeadFollow Follow = new();

		public bool IsShowing => Root.IsValid() && Root.ChildrenWantMouseInput;
	}

	static readonly Surface _menu = new();
	static readonly Surface _hud = new();

	static SceneWorld _world;
	static SceneLineObject _beam;

	static readonly WorldPanelInput _input = new();
	static readonly List<RootPanel> _roots = new();

	static bool _useLeftHand;
	static bool _menuWasDown;
	static Panel _lastHovered;
	static RealTimeSince _sinceTick;

	/// <summary>
	/// The pause menu's panel: opens in front of the player whenever something interactive shows on it, then stays put.
	/// Null outside VR, outside a game, or while the game has no camera.
	/// </summary>
	public static Panel HostPanel => EnsureWorld() ? EnsureSurface( _menu, Distance, Drop, Width ) : null;

	/// <summary>
	/// The HUD's panel: floats ahead of the player and follows the head lazily, for toasts, chat, subtitles and loading.
	/// Null outside VR, outside a game, or while the game has no camera.
	/// </summary>
	public static Panel HudPanel => EnsureWorld() ? EnsureSurface( _hud, HudDistance, HudDrop, HudWidth ) : null;

	/// <summary>
	/// Puts both panels back in front of the player's head.
	/// </summary>
	public static void Recenter()
	{
		if ( !TryGetCamera( out var camera ) )
			return;

		var head = camera.WorldTransform;
		Place( _menu, head, Distance, Drop, Width );
		Place( _hud, head, HudDistance, HudDrop, HudWidth );
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
		var delta = MathF.Min( _sinceTick, 0.1f );
		_sinceTick = 0;

		try
		{
			// Fields on the VR menu's panel need it as much as those on the in-game panels
			VRKeyboard.TickFocus();
		}
		catch ( Exception e )
		{
			Log.Error( e, "Error in VR keyboard tick" );
		}

		// Only made when something asks for them, so a game that never shows any menu UI pays nothing
		if ( !_menu.Root.IsValid() && !_hud.Root.IsValid() )
			return;

		try
		{
			if ( !EnsureWorld() || !TryGetCamera( out var camera ) )
				return;

			var head = camera.WorldTransform;

			// The pause menu opens in front of wherever the player is looking, then stays put in the world
			var menuShowing = _menu.IsShowing;
			if ( menuShowing && !_menu.WasShowing ) Place( _menu, head, Distance, Drop, Width );
			_menu.WasShowing = menuShowing;

			FollowHead( _hud, head, delta );

			_roots.Clear();
			if ( menuShowing ) _roots.Add( _menu.Root );
			if ( _hud.IsShowing ) _roots.Add( _hud.Root );

			if ( _roots.Count > 0 )
				TickPointer( head, delta );
			else
				HidePointer();

			_roots.Clear();
		}
		catch ( Exception e )
		{
			Log.Error( e, "Error in VR game panel tick" );
		}
	}

	static bool TryGetCamera( out CameraComponent camera )
	{
		camera = GlobalContext.Game?.ActiveScene?.Camera;
		return camera.IsValid();
	}

	/// <summary>
	/// Makes sure the panels live in the game's current world, dropping them when there's no game to put them in.
	/// </summary>
	static bool EnsureWorld()
	{
		var scene = GlobalContext.Game?.ActiveScene;
		if ( !Application.IsVR || !Game.InGame || !scene.IsValid() || scene.SceneWorld is null )
		{
			Destroy();
			return false;
		}

		if ( _world == scene.SceneWorld )
			return true;

		// A new game scene has its own world, so the panels are made again in it
		Destroy();
		_world = scene.SceneWorld;

		_beam = new SceneLineObject( _world );
		_beam.RenderingEnabled = false;
		_beam.Face = SceneLineObject.FaceMode.Camera;
		_beam.Opaque = false;
		_beam.Flags.CastShadows = false;
		_beam.Flags.IsOpaque = false;
		_beam.Flags.IsTranslucent = true;
		_beam.Attributes.SetCombo( "D_BLEND", 1 );

		return true;
	}

	static Panel EnsureSurface( Surface surface, float distance, float drop, float width )
	{
		if ( surface.Root.IsValid() )
			return surface.Root;

		using var _ = GlobalContext.MenuScope();

		surface.Root = new Sandbox.UI.WorldPanel( _world ) { PanelBounds = PanelBounds };
		surface.Cursor = CreateCursor( surface.Root );
		surface.WasShowing = false;
		surface.Follow.Stop();

		if ( TryGetCamera( out var camera ) )
			Place( surface, camera.WorldTransform, distance, drop, width );

		return surface.Root;
	}

	static Panel CreateCursor( Panel root )
	{
		var cursor = root.AddChild<Panel>();
		cursor.Style.Position = PositionMode.Absolute;
		cursor.Style.Width = 16;
		cursor.Style.Height = 16;
		cursor.Style.BorderTopLeftRadius = 8;
		cursor.Style.BorderTopRightRadius = 8;
		cursor.Style.BorderBottomLeftRadius = 8;
		cursor.Style.BorderBottomRightRadius = 8;
		cursor.Style.BackgroundColor = Color.White.WithAlpha( 0.8f );
		cursor.Style.PointerEvents = PointerEvents.None;
		cursor.Style.ZIndex = 100000;
		cursor.Style.Display = DisplayMode.None;
		return cursor;
	}

	static void Destroy()
	{
		_input.CancelPointerInteraction();
		_input.Clear();

		_beam?.Delete();
		_beam = null;

		DestroySurface( _menu );
		DestroySurface( _hud );

		_world = null;
		_lastHovered = null;
	}

	static void DestroySurface( Surface surface )
	{
		if ( surface.Root.IsValid() )
			surface.Root.Delete( true );

		surface.Root = null;
		surface.Cursor = null;
		surface.WasShowing = false;
		surface.Follow.Stop();
	}

	static Transform Target( Transform head, float distance, float drop, float width )
	{
		var transform = VRPanelPlacement.InFrontOf( head, distance, drop );
		return transform.WithScale( VRPanelPlacement.ScaleForWidth( PanelBounds.Width, width ) );
	}

	static void Place( Surface surface, Transform head, float distance, float drop, float width )
	{
		if ( !surface.Root.IsValid() )
			return;

		surface.Root.Transform = Target( head, distance, drop, width );
		surface.Follow.Stop();
	}

	static void FollowHead( Surface surface, Transform head, float delta )
	{
		if ( !surface.Root.IsValid() || !VRPreferences.HudFollow )
			return;

		var target = Target( head, HudDistance, HudDrop, HudWidth );
		surface.Root.Transform = surface.Follow.Tick( surface.Root.Transform, target, delta );
	}

	static void TickPointer( Transform head, float delta )
	{
		if ( Input.VR is not { } vr )
			return;

		// Point with whichever hand last pulled its trigger
		if ( !VRPreferences.CanPoint( leftHand: true ) ) _useLeftHand = false;
		else if ( !VRPreferences.CanPoint( leftHand: false ) ) _useLeftHand = true;
		else if ( vr.RightHand.Trigger.Value > PressThreshold ) _useLeftHand = false;
		else if ( vr.LeftHand.Trigger.Value > PressThreshold ) _useLeftHand = true;

		var hand = _useLeftHand ? vr.LeftHand : vr.RightHand;
		var aim = VRPanelPlacement.RelativeTo( hand.AimTransform, vr.Head, head );
		var ray = new Ray( aim.Position, aim.Rotation.Forward );

		_input.Ray = ray;
		_input.MouseLeftPressed = hand.Trigger.Value > PressThreshold;
		_input.MouseRightPressed = hand.Grip.Value > PressThreshold;
		_input.MouseWheel = VRStick.ToWheel( hand.Joystick.Value, delta );
		_input.Tick( _roots, true );

		var hovered = _input.Hovered;
		if ( hovered is not null && hovered != _lastHovered )
			hand.Rumble( 0.01f, 150.0f, 0.15f );

		_lastHovered = hovered;

		var hoveredRoot = hovered?.FindRootPanel();
		UpdateCursor( _menu, hoveredRoot );
		UpdateCursor( _hud, hoveredRoot );

		var hitRoot = hoveredRoot as Sandbox.UI.WorldPanel;
		UpdateBeam( ray, hitRoot?.WorldDistance ?? 0.0f, hitRoot is not null, hand.Trigger.Value );
	}

	static void HidePointer()
	{
		_input.CancelPointerInteraction();
		_input.Clear();
		_lastHovered = null;

		UpdateCursor( _menu, null );
		UpdateCursor( _hud, null );

		if ( _beam.IsValid() )
			_beam.RenderingEnabled = false;
	}

	static void UpdateCursor( Surface surface, RootPanel hoveredRoot )
	{
		if ( !surface.Cursor.IsValid() )
			return;

		var visible = hoveredRoot is not null && hoveredRoot == surface.Root;
		surface.Cursor.Style.Display = visible ? DisplayMode.Flex : DisplayMode.None;
		if ( !visible )
			return;

		var root = surface.Root;
		var bounds = root.Box.Rect;

		surface.Cursor.Style.Left = (root.WorldCursor.x - bounds.Left) / root.Scale - 8;
		surface.Cursor.Style.Top = (root.WorldCursor.y - bounds.Top) / root.Scale - 8;
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
