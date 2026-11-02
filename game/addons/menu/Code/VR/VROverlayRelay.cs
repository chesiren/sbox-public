// FORK-VR: file added by the fork, absent from upstream
using MenuProject.Modals;
using MenuProject.Overlay;
using MenuProject.Overlay.Overlays;
using Sandbox;
using Sandbox.UI;

namespace SboxMenu;

/// <summary>
/// In VR, moves the overlay's toasts, chat, subtitles, mic meter and loading screen onto a panel the headset shows,
/// along with any modal that still landed on the overlay (one opened before the VR menu's panel existed, say).
/// Their hosts go with the scene they're in, so anything deleted with one is made again on the overlay and moved on.
/// </summary>
public static class VROverlayRelay
{
	static MicOverlay _mic;
	static SubtitleOverlay _subtitles;
	static ChatOverlay _chat;
	static LoadingOverlay _loading;

	public static void Tick( MenuOverlay overlay )
	{
		if ( !Application.IsVR )
			return;

		Restore( overlay );

		var overlayParent = VRUIHost.GetOverlayParent() ?? overlay;
		Move( overlay.Top, overlayParent );
		Move( overlay.TopCenter, overlayParent );
		Move( overlay.TopLeft, overlayParent );
		Move( overlay.BottomCenter, overlayParent );
		Move( overlay.NotificationCorner, overlayParent );
		Move( _mic, overlayParent );
		Move( _subtitles, overlayParent );
		Move( _chat, overlayParent );

		if ( LoadingRoot.Instance.IsValid() )
			Move( _loading, VRUIHost.GetOverlayParent() ?? LoadingRoot.Instance );

		var modalParent = VRUIHost.GetModalParent();
		if ( modalParent is null )
			return;

		foreach ( var modal in overlay.Children.OfType<BaseModal>().ToArray() )
			Move( modal, modalParent );
	}

	/// <summary>
	/// Makes again on the overlay whatever was deleted along with the host it had been moved to, as the overlay first made it.
	/// </summary>
	static void Restore( MenuOverlay overlay )
	{
		if ( !overlay.Top.IsValid() ) overlay.Top = overlay.AddChild<ToastArea>( "popup_canvas" );
		if ( !overlay.TopCenter.IsValid() ) overlay.TopCenter = overlay.AddChild<ToastArea>( "popup_canvas_top" );
		if ( !overlay.TopLeft.IsValid() ) overlay.TopLeft = overlay.AddChild<ToastArea>( "popup_canvas_topleft" );
		if ( !overlay.BottomCenter.IsValid() ) overlay.BottomCenter = overlay.AddChild<ToastArea>( "popup_canvas_bottomcenter" );

		if ( !overlay.NotificationCorner.IsValid() )
		{
			overlay.NotificationCorner = overlay.AddChild<Panel>( "notification-corner" );
			overlay.BottomRight = overlay.NotificationCorner.AddChild<ToastArea>( "popup_canvas_bottomright" );
		}

		_mic = Find( _mic, overlay );
		_subtitles = Find( _subtitles, overlay );
		_chat = Find( _chat, overlay );

		if ( LoadingRoot.Instance.IsValid() )
			_loading = Find( _loading, LoadingRoot.Instance );
	}

	/// <summary>
	/// The panel of this type we're already moving around, or the one on <paramref name="owner"/>, or a new one there.
	/// </summary>
	static T Find<T>( T current, Panel owner ) where T : Panel, new()
	{
		if ( current.IsValid() )
			return current;

		return owner.Children.OfType<T>().FirstOrDefault() ?? owner.AddChild<T>();
	}

	static void Move( Panel panel, Panel parent )
	{
		if ( panel.IsValid() && parent.IsValid() && panel.Parent != parent )
			panel.Parent = parent;
	}
}
