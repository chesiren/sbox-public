// FORK-VR: file added by the fork, absent from upstream
using Sandbox;

namespace SboxMenu;

/// <summary>
/// In VR only games that support it are worth listing, wherever games are listed.
/// </summary>
public static class VRPackageFilter
{
	/// <summary>
	/// The query with <c>+vr</c> added if it lists games in VR. Leaves it alone if it already asks for VR games.
	/// </summary>
	public static string Apply( string query )
	{
		if ( !Application.IsVR || string.IsNullOrEmpty( query ) )
			return query;

		if ( !query.Contains( "type:game" ) || query.Contains( "+vr" ) )
			return query;

		return $"{query} +vr";
	}
}
