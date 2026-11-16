// FORK-VR: file added by the fork, absent from upstream
namespace Sandbox.UI;

public partial class RootPanel
{
	ScenePanelObject _projection;
	Rect _projectedBounds;
	float _projectedScale;
	bool _wasRenderedManually;
	bool _wasWorldPanel;

	/// <summary>
	/// Drawn in a scene world like a world panel, instead of flat on the screen. The menu's roots are, in VR,
	/// since nothing drawn on the desktop window reaches the headset.
	/// </summary>
	internal bool IsProjected => _projection.IsValid();

	/// <summary>
	/// Where the projected panel sits. Its middle is at the transform's position, facing the transform's forward.
	/// </summary>
	internal Transform ProjectedTransform => IsProjected ? _projection.Transform : Transform.Zero;

	/// <summary>
	/// Draws this root in <paramref name="world"/>, laid out in <paramref name="bounds"/> at <paramref name="scale"/>
	/// instead of the screen's size, and reached by world inputs instead of the mouse.
	/// </summary>
	internal void Project( SceneWorld world, Rect bounds, float scale )
	{
		if ( _projection is null )
		{
			_wasRenderedManually = RenderedManually;
			_wasWorldPanel = IsWorldPanel;
		}
		else if ( _projection.IsValid() && _projection.World != world )
		{
			_projection.Delete();
		}

		if ( !_projection.IsValid() )
		{
			_projection = new ScenePanelObject( world, this );
			_projection.Flags.IsOpaque = false;
			_projection.Flags.IsTranslucent = true;
		}

		_projectedBounds = bounds;
		_projectedScale = scale;

		RenderedManually = true;
		IsWorldPanel = true;
	}

	/// <summary>
	/// Back on the screen. Safe to call on a deleted root, to drop its scene object.
	/// </summary>
	internal void StopProjecting()
	{
		if ( _projection is null )
			return;

		_projection.Delete();
		_projection = null;

		RenderedManually = _wasRenderedManually;
		IsWorldPanel = _wasWorldPanel;
	}

	/// <summary>
	/// Moves the projected panel, and snapshots the move for the render thread.
	/// </summary>
	internal void PlaceProjection( Transform transform )
	{
		if ( !IsProjected )
			return;

		_projection.Transform = transform;
		_projection.Bounds = ProjectedWorldBounds( transform );
		_projection.BuildCommandList();
	}

	BBox ProjectedWorldBounds( Transform transform )
	{
		var right = transform.Rotation.Right;
		var down = transform.Rotation.Down;
		var rect = _projectedBounds * transform.UniformScale * ScenePanelObject.ScreenToWorldScale;

		var bounds = BBox.FromPositionAndSize( right * rect.Left + down * rect.Top );
		bounds = bounds.AddPoint( right * rect.Left + down * rect.Bottom );
		bounds = bounds.AddPoint( right * rect.Right + down * rect.Top );
		bounds = bounds.AddPoint( right * rect.Right + down * rect.Bottom );

		return bounds + transform.Position;
	}

	bool UpdateProjectedBounds()
	{
		if ( !IsProjected )
			return false;

		PanelBounds = _projectedBounds;
		return true;
	}

	bool UpdateProjectedScale()
	{
		if ( !IsProjected )
			return false;

		Scale = _projectedScale;
		return true;
	}

	/// <summary>
	/// The painter's panel to world matrix while projected, null on the screen.
	/// </summary>
	Matrix? ProjectedViewport() => IsProjected ? ScenePanelObject.BuildPanelToWorldMatrix( _projection.Transform ) : null;

	public override bool RayToLocalPosition( Ray ray, out Vector2 position, out float distance )
	{
		position = default;
		distance = 0;

		if ( !IsProjected )
			return false;

		var transform = _projection.Transform;
		var hit = new Plane( transform.Position, transform.Rotation.Forward ).Trace( ray, false, 10000.0f );
		if ( !hit.HasValue )
			return false;

		distance = Vector3.DistanceBetween( hit.Value, ray.Position );
		if ( distance < 1 )
			return false;

		var local = transform.PointToLocal( hit.Value );
		var point = new Vector2( local.y, -local.z ) * (1f / ScenePanelObject.ScreenToWorldScale);

		if ( !IsInside( point ) )
			return false;

		position = point;
		return true;
	}
}
