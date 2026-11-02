// FORK-VR: file added by the fork, absent from upstream
using Sandbox.Engine;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace Sandbox.VR;

/// <summary>
/// An on-screen keyboard for the menu's text fields in VR, where there's no keyboard to reach for.
/// It opens on the same world panel as the focused field and types into it, keeping the field focused while its keys are clicked.
/// </summary>
internal sealed class VRKeyboard : Panel
{
	const string Styles = @"
		.vr-keyboard {
			position: absolute; left: 22%; right: 22%;
			flex-direction: column; gap: 8px; padding: 14px;
			background-color: rgba( 16, 18, 22, 0.94 ); border-radius: 14px;
			z-index: 99000; pointer-events: all;
		}
		.vr-keyboard.at-top { top: 40px; }
		.vr-keyboard.at-bottom { bottom: 40px; }
		.vr-keyboard .row { flex-direction: row; justify-content: center; gap: 8px; }
		.vr-keyboard .key {
			min-width: 64px; height: 64px; padding: 0 14px;
			justify-content: center; align-items: center;
			background-color: rgba( 255, 255, 255, 0.08 ); border-radius: 8px;
			color: white; font-size: 26px; font-family: Poppins; font-weight: 500;
		}
		.vr-keyboard .key:hover { background-color: rgba( 255, 255, 255, 0.22 ); }
		.vr-keyboard .key:active { background-color: rgba( 123, 196, 255, 0.5 ); }
		.vr-keyboard .key.wide { min-width: 140px; }
		.vr-keyboard .key.space { min-width: 480px; }
		.vr-keyboard .key.on { background-color: rgba( 123, 196, 255, 0.4 ); }
	";

	static readonly string[] Rows = ["1234567890", "qwertyuiop", "asdfghjkl'", "zxcvbnm,.-"];
	static readonly string[] ShiftedRows = ["!@#$%^&*()", "QWERTYUIOP", "ASDFGHJKL\"", "ZXCVBNM;:_"];

	static VRKeyboard _current;

	/// <summary>
	/// The field being typed into.
	/// </summary>
	public TextEntry Target { get; private set; }

	/// <summary>
	/// Clicks on the keys keep the field focused, as a click in a popup keeps its source's.
	/// </summary>
	internal override Panel FocusOwner => Target.IsValid() ? Target : Parent;

	bool _shift;
	readonly List<(Label label, int row, int column)> _letters = new();
	Panel _shiftKey;

	VRKeyboard( TextEntry target )
	{
		Target = target;

		AddClass( "vr-keyboard" );
		StyleSheet.Parse( Styles );

		for ( int row = 0; row < Rows.Length; row++ )
		{
			var line = Add.Panel( "row" );

			if ( row == 3 )
				_shiftKey = AddKey( line, "shift", ToggleShift, "wide" );

			for ( int column = 0; column < Rows[row].Length; column++ )
			{
				var (r, c) = (row, column);
				var key = AddKey( line, Rows[row][column].ToString(), () => Type( r, c ) );
				_letters.Add( (key.Children.OfType<Label>().First(), r, c) );
			}

			if ( row == 0 )
				AddKey( line, "backspace", () => Press( "backspace" ), "wide" );
		}

		var bottom = Add.Panel( "row" );
		AddKey( bottom, "@", () => TypeText( "@" ) );
		AddKey( bottom, "/", () => TypeText( "/" ) );
		AddKey( bottom, "space", () => TypeText( " " ), "space" );
		AddKey( bottom, "enter", () => Press( "enter" ), "wide" );
		AddKey( bottom, "close", Close, "wide" );
	}

	/// <summary>
	/// Opens, moves or closes the keyboard to follow the menu's focused text field. Only fields on world panels get one:
	/// on the desktop window there's a real keyboard.
	/// </summary>
	internal static void TickFocus()
	{
		if ( !Application.IsVR )
		{
			CloseCurrent();
			return;
		}

		var focus = GlobalContext.Menu?.UISystem?.CurrentFocus;
		var field = focus as TextEntry ?? focus?.Ancestors.OfType<TextEntry>().FirstOrDefault();
		var root = field?.FindRootPanel();

		if ( !field.IsValid() || root is null || !root.IsWorldPanel || field.ReadOnly || field.Disabled )
		{
			CloseCurrent();
			return;
		}

		if ( _current.IsValid() && _current.Target == field && _current.Parent == root )
			return;

		CloseCurrent();

		_current = new VRKeyboard( field );
		root.AddChild( _current );

		// Out of the field's way: above it when it's low on the panel, below it otherwise
		var low = field.Box.Rect.Center.y > root.Box.Rect.Center.y;
		_current.SetClass( "at-top", low );
		_current.SetClass( "at-bottom", !low );
	}

	static void CloseCurrent()
	{
		if ( _current.IsValid() )
			_current.Delete( true );

		_current = null;
	}

	static Panel AddKey( Panel row, string text, Action onClick, string extraClass = null )
	{
		var key = row.Add.Panel( "key" );
		if ( extraClass is not null )
			key.AddClass( extraClass );

		var icon = text is "shift" or "backspace" or "space" or "enter" or "close";
		var label = icon ? key.Add.Icon( IconFor( text ) ) : key.Add.Label( text );
		label.Style.PointerEvents = PointerEvents.None;

		key.AddEventListener( "onclick", onClick );
		return key;
	}

	static string IconFor( string key ) => key switch
	{
		"shift" => "arrow_upward",
		"backspace" => "backspace",
		"space" => "space_bar",
		"enter" => "keyboard_return",
		_ => "keyboard_hide"
	};

	void Type( int row, int column )
	{
		var rows = _shift ? ShiftedRows : Rows;
		TypeText( rows[row][column].ToString() );

		// Shift is for one character, like a phone's
		if ( _shift )
			ToggleShift();
	}

	void TypeText( string text )
	{
		if ( !Target.IsValid() )
			return;

		foreach ( var c in text )
			Target.OnKeyTyped( c );

		Target.Focus();
	}

	void Press( string button )
	{
		if ( !Target.IsValid() )
			return;

		Target.OnButtonTyped( new ButtonEvent( button, true, 0, default ) );

		// Enter submits and blurs a single line field, which closes the keyboard on the next tick
		if ( button != "enter" )
			Target.Focus();
	}

	void ToggleShift()
	{
		_shift = !_shift;
		_shiftKey?.SetClass( "on", _shift );

		var rows = _shift ? ShiftedRows : Rows;
		foreach ( var (label, row, column) in _letters )
			label.Text = rows[row][column].ToString();
	}

	void Close()
	{
		if ( Target.IsValid() )
			Target.Blur();
	}
}
