// FORK-VR: file added by the fork, absent from upstream
using Sandbox;
using Sandbox.VR;

namespace SboxMenu;

/// <summary>
/// Gives the VR menu scene's left hand the pointers its right hand has. The scene only puts them on the right hand;
/// mirroring them at runtime keeps the upstream scene file untouched.
/// </summary>
public static class VRMenuRig
{
	static Scene _rigged;
	static readonly List<Sandbox.WorldInput> _inputs = new();

	public static void Ensure()
	{
		if ( !Application.IsVR || Sandbox.Game.InGame )
			return;

		var scene = Sandbox.Game.ActiveScene;
		if ( !scene.IsValid() )
			return;

		if ( scene != _rigged )
		{
			_rigged = scene;
			Rig( scene );
		}

		ApplyPointerHand();
	}

	/// <summary>
	/// Turns each hand's pointers on or off to match <see cref="VRPreferences.PointerHand"/>.
	/// </summary>
	static void ApplyPointerHand()
	{
		foreach ( var input in _inputs )
		{
			if ( !input.IsValid() )
				continue;

			var allowed = VRPreferences.CanPoint( input.VRHandSource == VRHand.HandSources.Left );
			if ( input.Enabled == allowed )
				continue;

			input.Enabled = allowed;
			if ( input.GetComponent<VRPointer>( true ) is { } pointer )
				pointer.Enabled = allowed;
		}
	}

	static void Rig( Scene scene )
	{
		_inputs.Clear();

		var left = FindHand( scene, VRTrackedObject.PoseSources.LeftHand );
		var right = FindHand( scene, VRTrackedObject.PoseSources.RightHand );
		if ( left is null || right is null )
			return;

		var inputs = right.Components.GetAll<Sandbox.WorldInput>( FindMode.EverythingInSelfAndDescendants ).ToArray();
		_inputs.AddRange( inputs );

		var leftInputs = left.Components.GetAll<Sandbox.WorldInput>( FindMode.EverythingInSelfAndDescendants ).ToArray();
		if ( leftInputs.Length > 0 )
		{
			_inputs.AddRange( leftInputs );
			return;
		}

		foreach ( var input in inputs )
		{
			if ( input.VRHandSource != VRHand.HandSources.Right )
				continue;

			var source = input.GameObject;
			var parent = FindMirror( right, left, source.Parent );
			if ( parent is null )
				continue;

			var copy = source.Clone( new Transform( Vector3.Zero ), parent, source.Enabled, source.Name );
			copy.Flags |= GameObjectFlags.NotSaved;

			// Mirrored across the hand's left/right axis, like the hand itself
			var position = source.LocalPosition;
			var rotation = source.LocalRotation;
			copy.LocalPosition = new Vector3( position.x, -position.y, position.z );
			copy.LocalRotation = new Rotation( -rotation.x, rotation.y, -rotation.z, rotation.w );

			foreach ( var copied in copy.Components.GetAll<Sandbox.WorldInput>( FindMode.EverythingInSelfAndDescendants ) )
			{
				copied.VRHandSource = VRHand.HandSources.Left;
				_inputs.Add( copied );
			}
		}
	}

	static GameObject FindHand( Scene scene, VRTrackedObject.PoseSources source )
	{
		return scene.GetAllComponents<VRTrackedObject>().FirstOrDefault( x => x.PoseSource == source )?.GameObject;
	}

	/// <summary>
	/// The object under <paramref name="toRoot"/> at the same path of names as <paramref name="target"/> has under <paramref name="fromRoot"/>.
	/// </summary>
	static GameObject FindMirror( GameObject fromRoot, GameObject toRoot, GameObject target )
	{
		var path = new List<string>();
		for ( var go = target; go != fromRoot; go = go.Parent )
		{
			if ( go is null )
				return null;

			path.Insert( 0, go.Name );
		}

		var found = toRoot;
		foreach ( var name in path )
		{
			found = found.Children.FirstOrDefault( x => x.Name == name );
			if ( found is null )
				return null;
		}

		return found;
	}
}
