// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

//
// getline.cs: A command line editor
//
// Authors:
// Miguel de Icaza (miguel@novell.com)
//
// Copyright 2008 Novell, Inc.
//
// Dual-licensed under the terms of the MIT X11 license or the
// Apache License 2.0
//
// USE -define:DEMO to build this as a standalone file and test it
//
// TODO:
//    Enter an error (a = 1);  Notice how the prompt is in the wrong line
//		This is caused by Stderr not being tracked by System.Console.
//    Completion support
//    Why is Thread.Interrupt not working?   Currently I resort to Abort which is too much.
//
// Limitations in System.Console:
//    Console needs SIGWINCH support of some sort
//    Console needs a way of updating its position after things have been written
//    behind its back (P/Invoke puts for example).
//    System.Console needs to get the DELETE character, and report accordingly.
//

//
// Adapted for Kiezellisp
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Kiezel
{
    public class LineEditor
    {
        /// <summary>
        ///   Invoked when the user requests auto-completion using the tab character
        /// </summary>
        /// <remarks>
        ///    The result is null for no values found, an array with a single
        ///    string, in that case the string should be the text to be inserted
        ///    for example if the word at pos is "T", the result for a completion
        ///    of "ToString" should be "oString", not "ToString".
        ///
        ///    When there are multiple results, the result should be the full
        ///    text
        /// </remarks>
        public AutoCompleteHandler AutoCompleteEvent;

        private static List<CustomHandler> customHandlers = new List<CustomHandler>();

        private static Handler[] handlers;

        // The current cursor position, indexes into "text", for an index
        // into rendered_text, use TextToRenderPos
        private int cursor;

        // If we are done editing, this breaks the interactive loop
        private bool done = false;

        private string externalInput = "";

        private bool externalInputInserted = false;

        // Our object that tracks history
        private CHistory history;

        private int home_col;

        // The row where we started displaying data.
        private int home_row;

        private KeyHandler last_handler;

        private Completion lastCompletion;

        private Completion lastCompletionSaved;

        // The maximum length that has been displayed on the screen
        private int max_rendered;

        // The prompt specified, and the prompt shown to the user.
        private string prompt;

        // The text as it is rendered (replaces (char)1 with ^A on display for example).
        private StringBuilder rendered_text;

        private string shown_prompt;

        // The text being edited.
        private StringBuilder text;

        public LineEditor( string name )
        {
            handlers = new Handler[]
            {
				new Handler(ConsoleKey.Home,       0,                          CmdHome),
				new Handler(ConsoleKey.End,        0,                          CmdEnd),
				new Handler(ConsoleKey.LeftArrow,  0,                          CmdLeft),
				new Handler(ConsoleKey.RightArrow, 0,                          CmdRight),
				new Handler(ConsoleKey.PageUp,     0,                          CmdPageUp),
				new Handler(ConsoleKey.PageDown,   0,                          CmdPageDown),
				new Handler(ConsoleKey.UpArrow,    0,                          CmdHistoryPrev),
				new Handler(ConsoleKey.DownArrow,  0,                          CmdHistoryNext),
				new Handler(ConsoleKey.Enter,      0,                          CmdEnter),
				new Handler(ConsoleKey.Backspace,  0,                          CmdBackspace),
				new Handler(ConsoleKey.Delete,     0,                          CmdDeleteChar),
				new Handler(ConsoleKey.Tab,        0,                          CmdTab),

                // Windows keys
				new Handler(ConsoleKey.Home,       ConsoleModifiers.Control,   CmdHomeBuffer),
				new Handler(ConsoleKey.End,        ConsoleModifiers.Control,   CmdEndBuffer),
				new Handler(ConsoleKey.UpArrow,    ConsoleModifiers.Control,   CmdScrollUp),
				new Handler(ConsoleKey.DownArrow,  ConsoleModifiers.Control,   CmdScrollDown),
				new Handler(ConsoleKey.Enter,      ConsoleModifiers.Control,   CmdControlEnter),
				new Handler(ConsoleKey.X,          ConsoleModifiers.Control,   CmdCut),
				new Handler(ConsoleKey.C,          ConsoleModifiers.Control,   CmdCopy),
				new Handler(ConsoleKey.C,          ConsoleModifiers.Control|ConsoleModifiers.Shift,   CmdCopyIt),
				new Handler(ConsoleKey.V,          ConsoleModifiers.Control,   CmdPaste),
				new Handler(ConsoleKey.Z,          ConsoleModifiers.Control,   CmdEof),
				new Handler(ConsoleKey.LeftArrow,  ConsoleModifiers.Control,   CmdBackwardWord),
				new Handler(ConsoleKey.RightArrow, ConsoleModifiers.Control,   CmdForwardWord),
				new Handler(ConsoleKey.Delete,     ConsoleModifiers.Control,   CmdDeleteWord),
				new Handler(ConsoleKey.Backspace,  ConsoleModifiers.Control,   CmdDeleteBackword),

				// Emacs keys
                //Handler.Control ('A', CmdHome),
                //Handler.Control ('E', CmdEnd),
                //Handler.Control ('B', CmdLeft),
                //Handler.Control ('F', CmdRight),
                //Handler.Control ('P', CmdHistoryPrev),
                //Handler.Control ('N', CmdHistoryNext),
                //Handler.Control ('K', CmdKillToEOF),
                //Handler.Control ('Y', CmdYank),
                //Handler.Control ('D', CmdDeleteChar),
                //Handler.Control ('L', CmdRefresh),
                //Handler.Control ('R', CmdReverseSearch),
                //Handler.Control ('G', delegate {} ),
                //Handler.Alt ('B', ConsoleKey.B, CmdBackwardWord),
                //Handler.Alt ('F', ConsoleKey.F, CmdForwardWord),
				//Handler.Alt ('D', ConsoleKey.D, CmdDeleteWord),
				//Handler.Alt ((char) 8, ConsoleKey.Backspace, CmdDeleteBackword),

				// DEBUG
				//Handler.Control ('T', CmdDebug),

				// quote
				//Handler.Control ('Q', delegate { HandleChar (Console.ReadKey (true).KeyChar); })
			};

            rendered_text = new StringBuilder();
            text = new StringBuilder();

            history = new CHistory( name );
        }

        public delegate bool AcceptReturnAsCommandHandler( string text );

        public delegate Completion AutoCompleteHandler( string text, int pos );

        public delegate bool CanAddToHistoryHandler( string text );

        private delegate void KeyHandler();

        public AcceptReturnAsCommandHandler AcceptReturnAsCommand
        {
            get;
            set;
        }

        public CanAddToHistoryHandler CanAddToHistory
        {
            get;
            set;
        }

        public CHistory History
        {
            get
            {
                return history;
            }
        }

        public bool ControlEnterPressed
        {
            get;
            set;
        }

        public bool ReadingFromREPL
        {
            get;
            set;
        }

        private int LineCount
        {
            get
            {
                return ( home_col + shown_prompt.Length + rendered_text.Length ) / Console.WindowWidth;
            }
        }

        private string Prompt
        {
            get
            {
                return prompt;
            }
            set
            {
                prompt = value;
            }
        }

        public void ClearHistory()
        {
            history.Clear();
        }

        public string Edit( string prompt, string initial, out bool isExternalInput, out bool controlEnterPressed )
        {
            done = false;
            history.CursorToEnd();
            max_rendered = 0;
            home_col = Console.CursorLeft;
            Prompt = prompt;
            shown_prompt = prompt;
            InitText( initial );
            history.Append( initial );

            isExternalInput = false;
            ControlEnterPressed = false;

            do
            {
                try
                {
                    EditLoop();
                }
                catch ( ThreadAbortException )
                {
                    Thread.ResetAbort();
                    Console.WriteLine();
                    SetPrompt( prompt );
                    SetText( "" );
                }
            } while ( !done );

            isExternalInput = externalInputInserted;
            controlEnterPressed = ControlEnterPressed;

            Console.WriteLine();

            if ( text == null )
            {
                history.RemoveLast();
                history.Close();
                return null;
            }

            string result = text.ToString();

            if ( !ReadingFromREPL || CanAddToHistory == null || CanAddToHistory( result ) )
            {
                history.Accept( result );
            }
            else
            {
                history.RemoveLast();
            }

            return result;
        }

        public void SaveHistory()
        {
            history.Close();
        }

        public void SetExternalInput( string str )
        {
            externalInput = str;
        }

        public void SetKeyBinding( Symbol key, Cons modifierList, object func )
        {
            var keyTable = new Prototype(
                            "f1", ConsoleKey.F1,
                            "f2", ConsoleKey.F2,
                            "f3", ConsoleKey.F3,
                            "f4", ConsoleKey.F4,
                            "f5", ConsoleKey.F5,
                            "f6", ConsoleKey.F6,
                            "f7", ConsoleKey.F7,
                            "f8", ConsoleKey.F8,
                            "f9", ConsoleKey.F9,
                            "f10", ConsoleKey.F10,
                            "f11", ConsoleKey.F11,
                            "f12", ConsoleKey.F12
                            );
            var modifierTable = new Prototype( "alt", ConsoleModifiers.Alt,
                                           "shift", ConsoleModifiers.Shift,
                                           "ctrl", ConsoleModifiers.Control );

            var key2 = ( ConsoleKey ) ( keyTable.GetValue( key ) ?? 0 );

            ConsoleModifiers modifiers = 0;
            foreach ( var m in Runtime.ToIter( modifierList ) )
            {
                var a = ( ConsoleModifiers ) modifierTable.GetValue( m );
                modifiers |= a;
            }

            var handler = Runtime.GetClosure( func );

            for ( var i = 0; i < customHandlers.Count; ++i )
            {
                var item = customHandlers[ i ];

                if ( item.CKI.Key == key2 && item.CKI.Modifiers == modifiers )
                {
                    item.KeyHandler = handler;
                    return;
                }
            }

            customHandlers.Add( new CustomHandler( key2, modifiers, handler ) );
        }

        private void CmdBackspace()
        {
            if ( !EraseCompletion() )
            {
                if ( cursor == 0 )
                {
                    return;
                }

                text.Remove( --cursor, 1 );
                ComputeRendered();
                RenderAfter( cursor );
            }
        }

        private void CmdBackwardWord()
        {
            int p = WordBackward( cursor );
            if ( p == -1 )
            {
                return;
            }

            UpdateCursor( p );
        }

        private void CmdControlEnter()
        {
            CmdEnter();
            ControlEnterPressed = true;
            return;
        }

        private void CmdCopy()
        {
            SetClipboardData( text.ToString() );
        }

        private void CmdCopyIt()
        {
            var it = Symbols.It.Value ?? "";
            var str = Runtime.WriteToString( it, Symbols.Pretty, true );
            SetClipboardData( str );
        }

        private void CmdCut()
        {
            CmdCopy();
            EraseLine();
        }

        private void CmdDebug()
        {
            //history.Dump();
            Console.WriteLine();
            Render();
        }

        private void CmdDeleteBackword()
        {
            int pos = WordBackward( cursor );
            if ( pos == -1 )
            {
                return;
            }

            string k = text.ToString( pos, cursor - pos );

            if ( last_handler == CmdDeleteBackword )
            {
                SetClipboardData( k + GetClipboardData() );
            }
            else
            {
                SetClipboardData( k );
            }

            text.Remove( pos, cursor - pos );
            ComputeRendered();
            RenderAfter( pos );
        }

        private void CmdDeleteChar()
        {
            if ( !EraseCompletion() )
            {
                if ( cursor == text.Length )
                {
                    return;
                }

                text.Remove( cursor, 1 );
                ComputeRendered();
                RenderAfter( cursor );
            }
        }

        private void CmdDeleteWord()
        {
            int pos = WordForward( cursor );

            if ( pos == -1 )
            {
                return;
            }

            string k = text.ToString( cursor, pos - cursor );

            if ( last_handler == CmdDeleteWord )
            {
                SetClipboardData( GetClipboardData() + k );
            }
            else
            {
                SetClipboardData( k );
            }

            text.Remove( cursor, pos - cursor );
            ComputeRendered();
            RenderAfter( cursor );
        }

        private void CmdEnter()
        {
            if ( !ReadingFromREPL || AcceptReturnAsCommand == null || AcceptReturnAsCommand( text.ToString() ) )
            {
                ControlEnterPressed = false;
                done = true;
            }
            else
            {
                InsertChar( '\n' );
            }
        }

        private void CmdEnd()
        {
            UpdateCursor( text.Length );
        }

        private void CmdEndBuffer()
        {
            ScrollTo( Console.BufferHeight );
        }

        private void CmdEof()
        {
            done = true;
            text = null;
            Console.WriteLine();
        }

        private void CmdForwardWord()
        {
            int p = WordForward( cursor );
            if ( p == -1 )
            {
                return;
            }

            UpdateCursor( p );
        }

        private void CmdHistoryNext()
        {
            if ( history.NextAvailable() )
            {
                history.Update( text.ToString() );
                SetText( history.Next() );
            }
        }

        private void CmdHistoryPrev()
        {
            if ( history.PreviousAvailable() )
            {
                history.Update( text.ToString() );
                SetText( history.Previous() );
            }
        }

        private void CmdHome()
        {
            UpdateCursor( 0 );
        }

        private void CmdHomeBuffer()
        {
            ScrollTo( 0 );
        }

        private void CmdKillToEOF()
        {
            SetClipboardData( text.ToString( cursor, text.Length - cursor ) );
            text.Length = cursor;
            ComputeRendered();
            RenderAfter( cursor );
        }

        private void CmdLeft()
        {
            if ( cursor == 0 )
                return;

            UpdateCursor( cursor - 1 );
        }

        private void CmdPageDown()
        {
            ScrollTo( Console.WindowTop + Console.WindowHeight );
        }

        private void CmdPageUp()
        {
            ScrollTo( Console.WindowTop - Console.WindowHeight );
        }

        private void CmdPaste()
        {
            string str = GetClipboardData();
            InsertTextAtCursor( str );
        }

        private void CmdRefresh()
        {
            Console.Clear();
            max_rendered = 0;
            Render();
            ForceCursor( cursor );
        }

        private void CmdRight()
        {
            if ( cursor == text.Length )
            {
                return;
            }

            UpdateCursor( cursor + 1 );
        }

        private void CmdScrollDown()
        {
            ScrollTo( Console.WindowTop + 1 );
        }

        private void CmdScrollUp()
        {
            ScrollTo( Console.WindowTop - 1 );
        }

        private void CmdTab()
        {
            if ( !ReadingFromREPL || AutoCompleteEvent == null )
            {
                // Insert tab character, unless we are completing something in the REPL.
                HandleChar( '\t' );
                return;
            }

            // We will have a last completion when hitting the TAB key twice in a row
            lastCompletion = lastCompletionSaved;

            if ( lastCompletion == null )
            {
                bool complete = false;

                for ( int i = 0; i < cursor; i++ )
                {
                    if ( !Char.IsWhiteSpace( text[ i ] ) )
                    {
                        complete = true;
                        break;
                    }
                }

                if ( !complete )
                {
                    return;
                }

                var completion = AutoCompleteEvent( text.ToString(), cursor );

                if ( completion.Result.Length == 0 )
                {
                    return;
                }

                if ( completion.Result.Length == 1 )
                {
                    InsertTextAtCursor( completion.Result[ 0 ] );
                    return;
                }

                // Show all options
                Console.WriteLine();
                foreach ( string s in completion.Result )
                {
                    Console.Write( completion.Prefix );
                    Console.Write( s );
                    Console.Write( ' ' );
                }
                Console.WriteLine();
                Render();
                ForceCursor( cursor );

                // Show first option
                InsertTextAtCursor( completion.Result[ 0 ] );
                completion.IsShown = true;
                completion.Index = 0;

                // Remember for next time
                lastCompletion = completion;
            }
            else
            {
                var completion = lastCompletion;

                // Undo current option
                int count = completion.Result[ completion.Index ].Length;
                cursor -= count;
                text.Remove( cursor, count );
                ComputeRendered();
                RenderAfter( cursor );

                // Show next option
                completion.Index = ( completion.Index + 1 ) % completion.Result.Length;
                InsertTextAtCursor( completion.Result[ completion.Index ] );
            }
        }

        private void CmdYank()
        {
            InsertTextAtCursor( GetClipboardData() );
        }

        private void ComputeRendered()
        {
            rendered_text.Length = 0;

            for ( int i = 0; i < text.Length; i++ )
            {
                int c = ( int ) text[ i ];
                if ( c < ' ' )
                {
                    if ( c == '\t' )
                    {
                        rendered_text.Append( "    " );
                    }
                    else if ( c == '\n' )
                    {
                        int p = ( home_col + shown_prompt.Length + rendered_text.Length ) % Console.WindowWidth;
                        if ( p != 0 )
                        {
                            while ( p < Console.WindowWidth )
                            {
                                rendered_text.Append( " " );
                                ++p;
                            }
                        }
                    }
                    else if ( c == '\r' )
                    {
                        // ignore
                    }
                    else
                    {
                        rendered_text.Append( '^' );
                        rendered_text.Append( ( char ) ( c + ( int ) 'A' - 1 ) );
                    }
                }
                else
                {
                    rendered_text.Append( ( char ) c );
                }
            }
        }

        private void EditLoop()
        {
            externalInputInserted = false;

            while ( !done )
            {
                ConsoleModifiers mod;

                while ( !Console.KeyAvailable )
                {
                    if ( !String.IsNullOrWhiteSpace( externalInput ) )
                    {
                        InsertTextAtCursor( externalInput );
                        externalInput = "";
                        externalInputInserted = true;
                        done = true;
                        ControlEnterPressed = false;
                        return;
                    }
                    Runtime.Sleep( 10 );
                }

                var cki = Console.ReadKey( true );
                mod = cki.Modifiers;

                Runtime.InitRandom();

                if ( cki.Key == ConsoleKey.Escape )
                {
                    if ( EraseCompletion() )
                    {
                        continue;
                    }
                    else
                    {
                        done = true;
                        text = null;
                        break;
                    }
                }

                bool handled = false;

                lastCompletionSaved = lastCompletion;
                lastCompletion = null;

                foreach ( CustomHandler handler in customHandlers )
                {
                    ConsoleKeyInfo t = handler.CKI;

                    if ( t.Key == cki.Key && t.Modifiers == mod )
                    {
                        handled = true;
                        var input = text.ToString();
                        var start = cursor;
                        while ( start > 0 )
                        {
                            var ch = input[ start - 1 ];
                            if ( !Runtime.IsWordChar( ch ) )
                            {
                                break;
                            }
                            --start;
                        }
                        input = input.Substring( start, cursor - start );
                        var output = Runtime.ToPrintString( Runtime.Funcall( handler.KeyHandler, input ), false );
                        if ( output.EndsWith( "\n" ) )
                        {
                            InsertTextAtCursor( output.TrimEnd() );
                            done = true;
                        }
                        else
                        {
                            InsertTextAtCursor( output );
                        }

                        last_handler = null;
                        break;
                    }
                }

                if ( !handled )
                {
                    foreach ( Handler handler in handlers )
                    {
                        ConsoleKeyInfo t = handler.CKI;

                        if ( t.Key == cki.Key && t.Modifiers == mod )
                        {
                            handled = true;
                            handler.KeyHandler();
                            last_handler = handler.KeyHandler;
                            break;
                        }
                    }
                }

                if ( handled )
                {
                    continue;
                }

                if ( mod == 0 || mod == ConsoleModifiers.Shift )
                {
                    var ch = cki.KeyChar;

                    if ( Char.IsWhiteSpace( ch ) || ch > ' ' )
                    {
                        HandleChar( ch );
                    }
                }
            }
        }

        private bool EraseCompletion()
        {
            if ( lastCompletionSaved != null )
            {
                var completion = lastCompletionSaved;

                // Undo current option
                int count = completion.Result[ completion.Index ].Length;
                cursor -= count;
                text.Remove( cursor, count );
                ComputeRendered();
                RenderAfter( cursor );

                // No next option
                lastCompletion = lastCompletionSaved = null;

                return true;
            }
            else
            {
                return false;
            }
        }

        private void EraseLine()
        {
            SetClipboardData( text.ToString() );
            text.Length = 0;
            Console.WriteLine();
            Console.WriteLine();
            SetPrompt( prompt );
            Console.Write( prompt );
            SetText( "" );
        }

        private void ForceCursor( int newpos )
        {
            cursor = newpos;

            int actual_pos = home_col + shown_prompt.Length + TextToRenderPos( cursor );
            int row = home_row + ( actual_pos / Console.WindowWidth );
            int col = actual_pos % Console.WindowWidth;

            if ( row >= Console.BufferHeight )
            {
                row = Console.BufferHeight - 1;
            }
            Console.SetCursorPosition( col, row );

            //log.WriteLine ("Going to cursor={0} row={1} col={2} actual={3} prompt={4} ttr={5} old={6}", newpos, row, col, actual_pos, prompt.Length, TextToRenderPos (cursor), cursor);
            //log.Flush ();
        }

        private string GetClipboardData()
        {
            string str = System.Windows.Forms.Clipboard.GetText();
            return str;
        }

        private void HandleChar( char c )
        {
            InsertChar( c );
        }

        private bool HaveLispExpression()
        {
            var s = text.ToString();

            return !s.StartsWith( "?" ) && !s.StartsWith( ":" ) && AcceptReturnAsCommand != null && AcceptReturnAsCommand( s );
        }

        private void InitText( string initial )
        {
            text = new StringBuilder( initial );
            ComputeRendered();
            cursor = text.Length;
            Render();
            ForceCursor( cursor );
        }

        private void InsertChar( char c )
        {
            int prev_lines = LineCount;
            text = text.Insert( cursor, c );
            ComputeRendered();
            if ( prev_lines != LineCount )
            {
                Console.SetCursorPosition( home_col, home_row );
                Render();
                ForceCursor( ++cursor );
            }
            else
            {
                RenderFrom( cursor );
                ForceCursor( ++cursor );
                UpdateHomeRow( TextToScreenPos( cursor ) );
            }
        }

        private void InsertTextAtCursor( string str )
        {
            int prev_lines = LineCount;
            text.Insert( cursor, str );
            ComputeRendered();
            if ( prev_lines != LineCount )
            {
                Console.SetCursorPosition( home_col, home_row );
                Render();
                cursor += str.Length;
                ForceCursor( cursor );
            }
            else
            {
                RenderFrom( cursor );
                cursor += str.Length;
                ForceCursor( cursor );
                UpdateHomeRow( TextToScreenPos( cursor ) );
            }
        }

        private bool IsWhiteChar( char ch )
        {
            return Char.IsWhiteSpace( ch );
        }

        private void Render()
        {
            Console.Write( shown_prompt );
            Console.Write( rendered_text );

            int max = System.Math.Max( rendered_text.Length + home_col + shown_prompt.Length, max_rendered );

            for ( int i = rendered_text.Length + shown_prompt.Length + home_col; i < max_rendered; i++ )
            {
                Console.Write( ' ' );
            }

            max_rendered = home_col + shown_prompt.Length + rendered_text.Length;

            // Write one more to ensure that we always wrap around properly if we are at the
            // end of a line.
            Console.Write( ' ' );

            UpdateHomeRow( max );
        }

        private void RenderAfter( int p )
        {
            ForceCursor( p );
            RenderFrom( p );
            ForceCursor( cursor );
        }

        private void RenderFrom( int pos )
        {
            int rpos = TextToRenderPos( pos );
            int i;

            for ( i = rpos; i < rendered_text.Length; i++ )
            {
                Console.Write( rendered_text[ i ] );
            }

            if ( ( home_col + shown_prompt.Length + rendered_text.Length ) > max_rendered )
            {
                max_rendered = home_col + shown_prompt.Length + rendered_text.Length;
            }
            else
            {
                int max_extra = max_rendered - home_col - shown_prompt.Length;
                for ( ; i < max_extra; i++ )
                {
                    Console.Write( ' ' );
                }
            }
        }

        private void ScrollTo( int pos )
        {
            pos = Math.Max( 0, Math.Min( Console.BufferHeight - Console.WindowHeight, pos ) );
            Console.SetWindowPosition( Console.WindowLeft, pos );
        }

        private void SetClipboardData( string str )
        {
            if ( String.IsNullOrEmpty( str ) )
            {
                System.Windows.Forms.Clipboard.Clear();
            }
            else
            {
                System.Windows.Forms.Clipboard.SetText( str );
            }
        }

        private void SetPrompt( string newprompt )
        {
            shown_prompt = newprompt;
            Console.SetCursorPosition( home_col, home_row );
            Render();
            ForceCursor( cursor );
        }

        private void SetSearchPrompt( string s )
        {
            SetPrompt( "(reverse-i-search)`" + s + "': " );
        }

        private void SetText( string newtext )
        {
            Console.SetCursorPosition( home_col, home_row );
            InitText( newtext );
        }

        private int TextToRenderPos( int pos )
        {
            int p = home_col + shown_prompt.Length;

            for ( int i = 0; i < pos; i++ )
            {
                int c;

                c = ( int ) text[ i ];

                if ( c < ' ' )
                {
                    if ( c == '\t' )
                    {
                        p += 4;
                    }
                    else if ( c == '\n' )
                    {
                        if ( p % Console.WindowWidth == 0 )
                        {
                        }
                        else
                        {
                            p += Console.WindowWidth - ( p % Console.WindowWidth );
                        }
                    }
                    else if ( c == '\r' )
                    {
                        // ignore
                    }
                    else
                    {
                        p += 2;
                    }
                }
                else
                {
                    p++;
                }
            }

            return p - home_col - shown_prompt.Length;
        }

        private int TextToScreenPos( int pos )
        {
            return home_col + shown_prompt.Length + TextToRenderPos( pos );
        }

        private void UpdateCursor( int newpos )
        {
            if ( cursor == newpos )
                return;

            ForceCursor( newpos );
        }

        private void UpdateHomeRow( int screenpos )
        {
            // May change due to scrolling
            int lines = screenpos / Console.WindowWidth;

            home_row = Console.CursorTop - lines;

            if ( home_row < 0 )
            {
                home_row = 0;
            }
        }

        private int WordBackward( int p )
        {
            if ( p == 0 )
            {
                return -1;
            }

            int i = p - 1;

            if ( i == 0 )
            {
                return 0;
            }

            while ( i >= 0 && !Runtime.IsWordChar( text[ i ] ) )
            {
                --i;
            }

            while ( i >= 0 && Runtime.IsWordChar( text[ i ] ) )
            {
                --i;
            }

            ++i;

            return ( i == p ) ? -1 : i;
        }

        //
        // Commands
        //
        private int WordForward( int p )
        {
            if ( p >= text.Length )
            {
                return -1;
            }

            int i = p;

            while ( i < text.Length && Runtime.IsWordChar( text[ i ] ) )
            {
                ++i;
            }

            while ( i < text.Length && !Runtime.IsWordChar( text[ i ] ) )
            {
                ++i;
            }

            return ( i == p ) ? -1 : i;
        }

        private struct CustomHandler
        {
            public ConsoleKeyInfo CKI;
            public IApply KeyHandler;

            public CustomHandler( ConsoleKey key, ConsoleModifiers mod, IApply h )
            {
                var control = ( mod & ConsoleModifiers.Control ) != 0;
                var alt = ( mod & ConsoleModifiers.Alt ) != 0;
                var shift = ( mod & ConsoleModifiers.Shift ) != 0;

                CKI = new ConsoleKeyInfo( ( char ) 0, key, shift, alt, control );
                KeyHandler = h;
            }
        }

        private struct Handler
        {
            public ConsoleKeyInfo CKI;
            public KeyHandler KeyHandler;

            public Handler( ConsoleKey key, ConsoleModifiers mod, KeyHandler h )
            {
                var control = ( mod & ConsoleModifiers.Control ) != 0;
                var alt = ( mod & ConsoleModifiers.Alt ) != 0;
                var shift = ( mod & ConsoleModifiers.Shift ) != 0;

                CKI = new ConsoleKeyInfo( ( char ) 0, key, shift, alt, control );
                KeyHandler = h;
            }

            public Handler( ConsoleKey key, KeyHandler h )
                : this( key, 0, h )
            {
            }

            public Handler( char c, KeyHandler h )
            {
                KeyHandler = h;
                // Use the "Zoom" as a flag that we only have a character.
                CKI = new ConsoleKeyInfo( c, ConsoleKey.Zoom, false, false, false );
            }

            public Handler( ConsoleKeyInfo cki, KeyHandler h )
            {
                CKI = cki;
                KeyHandler = h;
            }

            public static Handler Alt( char c, ConsoleKey k, KeyHandler h )
            {
                ConsoleKeyInfo cki = new ConsoleKeyInfo( ( char ) c, k, false, true, false );
                return new Handler( cki, h );
            }

            public static Handler Control( char c, KeyHandler h )
            {
                return new Handler( ( char ) ( c - 'A' + 1 ), h );
            }

            public static Handler Control( ConsoleKey k, KeyHandler h )
            {
                ConsoleKeyInfo cki = new ConsoleKeyInfo( ( char ) 0, k, false, false, true );
                return new Handler( cki, h );
            }
        }

        //
        // Emulates the bash-like behavior, where edits done to the
        // history are recorded
        //
        public class CHistory
        {
            private int cursor;
            private string histfile;
            private Vector lines;
            public CHistory( string app )
            {
                if ( app != null )
                {
                    string dir = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );

                    if ( !Directory.Exists( dir ) )
                    {
                        try
                        {
                            Directory.CreateDirectory( dir );
                        }
                        catch
                        {
                            app = null;
                        }
                    }
                    if ( app != null )
                    {
                        histfile = PathExtensions.Combine( dir, app ) + ".history";
                    }
                }

                lines = new Vector();
                cursor = 0;

                if ( File.Exists( histfile ) )
                {
                    using ( StreamReader sr = File.OpenText( histfile ) )
                    {
                        StringBuilder buf = new StringBuilder();
                        string line;

                        while ( ( line = sr.ReadLine() ) != null )
                        {
                            if ( line != "" )
                            {
                                if ( line[ 0 ] == '\x1F' )
                                {
                                    Append( buf.ToString() );
                                    buf.Length = 0;
                                }
                                else
                                {
                                    if ( buf.Length != 0 )
                                    {
                                        buf.Append( '\n' );
                                    }
                                    buf.Append( line );
                                }
                            }
                        }
                    }
                }
            }

            public int Count
            {
                get
                {
                    return lines.Count;
                }
            }

            public void Accept( string s )
            {
                lines[ lines.Count - 1 ] = s;
                if ( lines.Count >= 2 && ( string ) lines[ lines.Count - 2 ] == ( string ) lines[ lines.Count - 1 ] )
                {
                    RemoveLast();
                }
            }

            public void Append( string s )
            {
                lines.Add( s );
                cursor = lines.Count - 1;
            }

            public void Clear()
            {
                lines.Clear();
                cursor = 0;
            }

            public void Close()
            {
                if ( histfile == null )
                {
                    return;
                }

                try
                {
                    using ( StreamWriter sw = File.CreateText( histfile ) )
                    {
                        foreach ( string s in lines )
                        {
                            sw.WriteLine( s );
                            sw.WriteLine( '\x1F' );
                        }
                    }
                }
                catch
                {
                    // ignore
                }
            }

            public void CursorToEnd()
            {
                cursor = lines.Count - 1;
            }

            public string Line( int index )
            {
                if ( 0 <= index && index < lines.Count )
                {
                    return ( string ) lines[ index ];
                }
                else
                {
                    return "";
                }
            }
            public string Next()
            {
                if ( !NextAvailable() )
                {
                    return null;
                }

                return ( string ) lines[ ++cursor ];
            }

            public bool NextAvailable()
            {
                return cursor + 1 < lines.Count;
            }

            //
            // Returns: a string with the previous line contents, or
            // nul if there is no data in the history to move to.
            //
            public string Previous()
            {
                if ( !PreviousAvailable() )
                {
                    return null;
                }

                return ( string ) lines[ --cursor ];
            }

            public bool PreviousAvailable()
            {
                return cursor > 0;
            }

            public void RemoveLast()
            {
                if ( lines.Count > 0 )
                {
                    lines.RemoveAt( lines.Count - 1 );
                }
            }

            //
            // Updates the current cursor location with the string,
            // to support editing of history items.   For the current
            // line to participate, an Append must be done before.
            //
            public void Update( string s )
            {
                lines[ cursor ] = s;
            }
        }

        public class Completion
        {
            public int Index;
            public bool IsShown;
            public string Prefix;
            public string[] Result;
            public Completion( string prefix, string[] result )
            {
                Index = -1;
                IsShown = false;
                Prefix = prefix;
                Result = result;
            }
        }
    }
}