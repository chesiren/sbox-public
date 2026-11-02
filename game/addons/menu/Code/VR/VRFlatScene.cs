// FORK-VR: file added by the fork, absent from upstream
using Sandbox;
using Sandbox.VR;

namespace SboxMenu;

/// <summary>
/// Makes the menu's flat scenes (the avatar editor, crate opening) usable in VR without touching their scene files.
/// A scene with no VR rig gets one at runtime: its camera draws to the headset from where it was looking, and each hand
/// gets a laser and a world input. Scripts that move the camera or aim with the mouse ask <see cref="IsActive"/> and
/// <see cref="PointerRay"/> instead.
/// </summary>
public static class VRFlatScene
{
	const float PressThreshold = 0.75f;

	static Scene _rigged;
	static GameObject _rightHand;

	static ulong _clickFrame = ulong.MaxValue;
	static bool _click;
	static bool _triggerWasDown;

	/// <summary>
	/// Whether <paramref name="scene"/> is a flat scene being shown in VR, so its own camera control should stand down.
	/// </summary>
	public static bool IsActive( Scene scene ) => Application.IsVR && scene.IsValid() && scene == _rigged;

	/// <summary>
	/// Where the right hand points, in a flat scene shown in VR. Null otherwise, so the caller falls back to the mouse.
	/// </summary>
	public static Ray? PointerRay( Scene scene )
	{
		if ( !IsActive( scene ) || !_rightHand.IsValid() )
			return null;

		return new Ray( _rightHand.WorldPosition, _rightHand.WorldRotation.Forward );
	}

	/// <summary>
	/// True on the frame the right trigger is pulled, standing in for a mouse click in a flat scene shown in VR.
	/// The same all frame, however many scripts ask.
	/// </summary>
	public static bool ClickPressed( Scene scene )
	{
		if ( !IsActive( scene ) || Input.VR is not { } vr )
			return false;

		if ( _clickFrame != Application.FrameCount )
		{
			_clickFrame = Application.FrameCount;

			var down = vr.RightHand.Trigger.Value > PressThreshold;
			_click = down && !_triggerWasDown;
			_triggerWasDown = down;
		}

		return _click;
	}

	/// <summary>
	/// Rigs the menu's active scene for VR if it was built flat. Scenes made for VR already track the head somewhere.
	/// </summary>
	public static void Ensure()
	{
		if ( !Application.IsVR || Sandbox.Game.InGame )
			return;

		var scene = Sandbox.Game.ActiveScene;
		if ( !scene.IsValid() || scene == _rigged )
			return;

		// The flat main menu scene swaps itself for the VR one
		var camera = scene.Camera;
		if ( !camera.IsValid() || scene.GetAllComponents<VRTrackedObject>().Any() || scene.GetAllComponents<VRSceneSwitcher>().Any() )
			return;

		_rigged = scene;

		var rig = new GameObject( true, "VR Rig (fork)" );
		rig.Flags |= GameObjectFlags.NotSaved;
		rig.WorldTransform = VRMenuPlacement.AnchorFor( camera.WorldTransform );
		rig.AddComponent<VRAnchor>();

		camera.TargetEye = StereoTargetEye.Both;
		var head = camera.AddComponent<VRTrackedObject>();
		head.PoseSource = VRTrackedObject.PoseSources.Head;

		AddHand( rig, VRTrackedObject.PoseSources.LeftHand, VRHand.HandSources.Left );
		_rightHand = AddHand( rig, VRTrackedObject.PoseSources.RightHand, VRHand.HandSources.Right );
	}

	static GameObject AddHand( GameObject rig, VRTrackedObject.PoseSources pose, VRHand.HandSources source )
	{
		var hand = new GameObject( rig, true, source == VRHand.HandSources.Left ? "Left Hand" : "Right Hand" );
		hand.Flags |= GameObjectFlags.NotSaved;

		var tracked = hand.AddComponent<VRTrackedObject>();
		tracked.PoseSource = pose;
		tracked.PoseType = VRTrackedObject.PoseTypes.Aim;

		var input = hand.AddComponent<Sandbox.WorldInput>();
		input.VRHandSource = source;

		hand.AddComponent<VRPointer>();
		return hand;
	}
}
