// FORK-VR: file added by the fork, absent from upstream
using System;

namespace Sandbox.VR;

/// <summary>
/// Links and Steam pages open on the desktop, which a player in a headset doesn't see. The menu listens so it can say so.
/// </summary>
[Hide]
public static class VRDesktopLinks
{
	/// <summary>
	/// Called with the link whenever one is opened on the desktop while in VR.
	/// </summary>
	public static event Action<string> OnOpened;

	internal static void Opened( string url )
	{
		if ( !Application.IsVR )
			return;

		try
		{
			OnOpened?.Invoke( url );
		}
		catch ( Exception e )
		{
			Log.Error( e, "Error telling the menu a link opened on the desktop" );
		}
	}
}
