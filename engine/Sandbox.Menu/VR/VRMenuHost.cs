// FORK-VR: file added by the fork, absent from upstream
using Sandbox.Engine;
using Sandbox.UI;
using System;

namespace Sandbox.VR;

/// <summary>
/// Shows the menu's UI in VR. Its roots are drawn flat on the desktop window, which the headset never shows, so each one
/// is projected onto a shared plane in the world: the game's scene when it has a camera, <see cref="VRMenuScene"/> otherwise.
/// While anything on the plane can be clicked it opens ahead of the player and stays put; otherwise it floats as a HUD
/// that follows the head lazily. The controllers point at it, with a laser, a cursor and haptics.
/// </summary>
[Hide]
public static class VRMenuHost
{
	static float Distance => VRPreferences.MenuDistance.Clamp( 20.0f, 200.0f );
	static float Width => VRPreferences.MenuWidth.Clamp( 16.0f, 200.0f );
	static float HudDistance => VRPreferences.HudDistance.Clamp( 20.0f, 200.0f );
	static float HudWidth => VRPreferences.HudWidth.Clamp( 16.0f, 200.0f );

	/// <summary>
	/// How far below eye level the middle of the menu sits, in world units.
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
	/// Gap between stacked roots, so the one drawn on top on the desktop is nearest, drawn last and hit first.
	/// </summary>
	const float LayerSpacing = 0.25f;

	/// <summary>
	/// The menu's 1920x1080 layout at a scale of 2, which keeps text sharp at the size a world panel is seen.
	/// </summary>
	static readonly Rect PanelBounds = new( -1920, -1080, 3840, 2160 );
	const float PanelScale = 2.0f;

	static readonly List<RootPanel> _projected = new();
	static readonly List<RootPanel> _roots = new();
	static readonly List<RootPanel> _interactive = new();

	static SceneWorld _world;
	static SceneLineObject _beam;
	static Panel _cursor;

	static readonly WorldPanelInput _input = new();
	static readonly VRHeadFollow _follow = new();

	static Transform _placement;
	static bool _placed;
	static bool _recenter;
	static bool _wasInteractive;

	static bool _useLeftHand;
	static bool _menuWasDown;
	static Panel _lastHovered;
	static RealTimeSince _sinceTick;

	/// <summary>
	/// Puts the menu back in front of the player's head.
	/// </summary>
	public static void Recenter() => _recenter = true;

	/// <summary>
	/// Holding the menu button this long recenters the menu instead of pausing.
	/// </summary>
	const float RecenterHoldTime = 0.8f;

	static RealTimeSince _menuDownFor;
	static bool _menuHeldToRecenter;

	/// <summary>
	/// True on the frame a short press of either controller's menu button ends, which pauses like Escape.
	/// Holding it instead recenters the menu in front of the player, without pausing.
	/// </summary>
	internal static bool MenuButtonPressed()
	{
		if ( !Application.IsVR || Input.VR is not { } vr )
			return false;

		var down = vr.LeftHand.ButtonMenu.IsPressed || vr.RightHand.ButtonMenu.IsPressed;
		var wasDown = _menuWasDown;
		_menuWasDown = down;

		if ( down && !wasDown )
		{
			_menuDownFor = 0;
			_menuHeldToRecenter = false;
			return false;
		}

		if ( down )
		{
			if ( !_menuHeldToRecenter && _menuDownFor > RecenterHoldTime )
			{
				_menuHeldToRecenter = true;
				Recenter();
			}

			return false;
		}

		return wasDown && !_menuHeldToRecenter;
	}

	internal static void Tick()
	{
		var delta = MathF.Min( _sinceTick, 0.1f );
		_sinceTick = 0;

		try
		{
			VRKeyboard.TickFocus();
		}
		catch ( Exception e )
		{
			Log.Error( e, "Error in VR keyboard tick" );
		}

		if ( !Application.IsVR )
		{
			if ( _world is not null || _projected.Count > 0 )
				Stop();

			return;
		}

		try
		{
			var scene = PickScene();
			if ( !scene.IsValid() || scene.SceneWorld is null || !scene.Camera.IsValid() )
			{
				StopProjecting();
				DestroyPointer();
				_world = null;
				return;
			}

			EnsureWorld( scene.SceneWorld );
			Project();

			var head = scene.Camera.WorldTransform;
			var interactive = _interactive.Count > 0;
			Place( head, interactive, delta );

			if ( interactive )
				TickPointer( head, delta );
			else
				HidePointer();
		}
		catch ( Exception e )
		{
			Log.Error( e, "Error in VR menu tick" );
		}
	}

	/// <summary>
	/// The game's scene when it can be seen, otherwise the bare one made for the menu.
	/// </summary>
	static Scene PickScene()
	{
		var game = GlobalContext.Game?.ActiveScene;
		if ( game.IsValid() && game.SceneWorld is not null && game.Camera.IsValid() )
		{
			VRMenuScene.Destroy();
			return game;
		}

		var scene = VRMenuScene.Ensure();
		VRMenuScene.Tick();
		return scene;
	}

	/// <summary>
	/// A new scene has its own world, so everything is made again in it.
	/// </summary>
	static void EnsureWorld( SceneWorld world )
	{
		if ( _world == world )
			return;

		StopProjecting();
		DestroyPointer();

		_world = world;
		_placed = false;

		_beam = new SceneLineObject( _world );
		_beam.RenderingEnabled = false;
		_beam.Face = SceneLineObject.FaceMode.Camera;
		_beam.Opaque = false;
		_beam.Flags.CastShadows = false;
		_beam.Flags.IsOpaque = false;
		_beam.Flags.IsTranslucent = true;
		_beam.Attributes.SetCombo( "D_BLEND", 1 );
	}

	/// <summary>
	/// Projects every flat root of the menu, highest z-index first, made first among equals, as on the desktop.
	/// </summary>
	static void Project()
	{
		_roots.Clear();
		_interactive.Clear();

		foreach ( var root in GlobalContext.Menu.UISystem.RootPanels )
		{
			if ( !root.IsValid() || root is Sandbox.UI.WorldPanel )
				continue;

			var z = root.ComputedStyle?.ZIndex ?? 0;
			int i = _roots.Count;
			while ( i > 0 && (_roots[i - 1].ComputedStyle?.ZIndex ?? 0) < z ) i--;
			_roots.Insert( i, root );
		}

		for ( int i = _projected.Count - 1; i >= 0; i-- )
		{
			if ( _roots.Contains( _projected[i] ) )
				continue;

			_projected[i].StopProjecting();
			_projected.RemoveAt( i );
		}

		foreach ( var root in _roots )
		{
			root.Project( _world, PanelBounds, PanelScale );
			if ( !_projected.Contains( root ) )
				_projected.Add( root );

			if ( root.IsVisible && root.ChildrenWantMouseInput )
				_interactive.Add( root );
		}
	}

	static Transform Target( Transform head, float distance, float drop, float width )
	{
		var transform = VRPanelPlacement.InFrontOf( head, distance, drop );
		return transform.WithScale( VRPanelPlacement.ScaleForWidth( PanelBounds.Width, width ) );
	}

	static void Place( Transform head, bool interactive, float delta )
	{
		if ( interactive )
		{
			// Opens in front of wherever the player is looking, then stays put in the world
			if ( !_placed || _recenter || !_wasInteractive )
			{
				_placement = Target( head, Distance, Drop, Width );
				_follow.Stop();
			}
		}
		else
		{
			var target = Target( head, HudDistance, HudDrop, HudWidth );

			if ( !_placed || _recenter || _wasInteractive )
			{
				_placement = target;
				_follow.Stop();
			}
			else if ( VRPreferences.HudFollow )
			{
				_placement = _follow.Tick( _placement, target, delta );
			}
		}

		_placed = true;
		_recenter = false;
		_wasInteractive = interactive;

		for ( int i = 0; i < _roots.Count; i++ )
			_roots[i].PlaceProjection( VRPanelPlacement.Layer( _placement, i, LayerSpacing ) );
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
		_input.Tick( _interactive, true );

		var hovered = _input.Hovered;
		if ( hovered is not null && hovered != _lastHovered )
			hand.Rumble( 0.01f, 150.0f, 0.15f );

		_lastHovered = hovered;

		var hoveredRoot = hovered?.FindRootPanel();
		UpdateCursor( hoveredRoot );
		UpdateBeam( ray, hoveredRoot?.WorldDistance ?? 0.0f, hoveredRoot is not null, hand.Trigger.Value );
	}

	static void HidePointer()
	{
		_input.CancelPointerInteraction();
		_input.Clear();
		_lastHovered = null;

		UpdateCursor( null );

		if ( _beam.IsValid() )
			_beam.RenderingEnabled = false;
	}

	static void UpdateCursor( RootPanel root )
	{
		if ( root is null || !root.IsProjected )
		{
			if ( _cursor.IsValid() )
				_cursor.Style.Display = DisplayMode.None;

			return;
		}

		if ( !_cursor.IsValid() || _cursor.Parent != root )
		{
			_cursor?.Delete( true );
			_cursor = CreateCursor( root );
		}

		var bounds = root.Box.Rect;
		_cursor.Style.Display = DisplayMode.Flex;
		_cursor.Style.Left = (root.WorldCursor.x - bounds.Left) / root.Scale - 8;
		_cursor.Style.Top = (root.WorldCursor.y - bounds.Top) / root.Scale - 8;
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
		return cursor;
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

	static void StopProjecting()
	{
		foreach ( var root in _projected )
			root.StopProjecting();

		_projected.Clear();
		_roots.Clear();
		_interactive.Clear();
	}

	static void DestroyPointer()
	{
		_input.CancelPointerInteraction();
		_input.Clear();
		_lastHovered = null;

		_cursor?.Delete( true );
		_cursor = null;

		_beam?.Delete();
		_beam = null;
	}

	/// <summary>
	/// Everything back on the desktop window, as outside VR.
	/// </summary>
	static void Stop()
	{
		StopProjecting();
		DestroyPointer();
		VRMenuScene.Destroy();

		_world = null;
		_placed = false;
		_wasInteractive = false;
	}
}
