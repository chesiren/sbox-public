// FORK-VR: file added by the fork, absent from upstream
using NativeEngine;
using Sandbox.VR;

namespace Sandbox;

/// <summary>
/// A bare scene for the headset while no game has one: the menu has no scene of its own, and a game without a menu
/// scene isn't loaded until it's played. Holds a tracked stereo camera so the menu's panels have somewhere to float.
/// </summary>
internal static class VRMenuScene
{
	static Scene _scene;

	/// <summary>
	/// The scene, made on first use. Null outside VR.
	/// </summary>
	public static Scene Ensure()
	{
		if ( !Application.IsVR )
		{
			Destroy();
			return null;
		}

		if ( _scene.IsValid() )
			return _scene;

		_scene = new Scene();

		using ( _scene.Push() )
		{
			var rig = new GameObject( true, "VR Rig" );
			rig.Components.Create<VRAnchor>();

			var head = new GameObject( rig, true, "Head" );
			head.Components.Create<VRTrackedObject>().PoseSource = VRTrackedObject.PoseSources.Head;

			var camera = head.Components.Create<CameraComponent>();
			camera.TargetEye = StereoTargetEye.Both;
			camera.BackgroundColor = new Color( 0.05f, 0.06f, 0.08f );
			camera.ZNear = 1.0f;
			camera.ZFar = 10000.0f;

			var sun = new GameObject( true, "Sun" );
			sun.WorldRotation = new Angles( 50, 30, 0 ).ToRotation();
			sun.Components.Create<DirectionalLight>();
		}

		return _scene;
	}

	public static void Tick()
	{
		if ( !_scene.IsValid() )
			return;

		using ( _scene.PushScope() )
		{
			_scene.GameTick( RealTime.Delta );
		}
	}

	/// <summary>
	/// Draws the scene to the headset. A game with a camera of its own draws there instead, so this stands down then.
	/// </summary>
	public static void Render( SwapChainHandle_t swapChain )
	{
		if ( !_scene.IsValid() || _scene.IsLoading )
			return;

		using ( _scene.PushScope() )
		{
			_scene.Render( swapChain, default );
		}
	}

	public static void Destroy()
	{
		_scene?.Destroy();
		_scene = null;
	}
}
