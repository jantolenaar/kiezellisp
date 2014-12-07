// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;

namespace Kiezel
{
    public delegate object ReadtableHandler( LispReader stream, char ch );

    public delegate object ReadtableHandler2( LispReader stream, char ch, int arg );

    public enum CharacterType
    {
        Invalid = 0,
        Constituent,
        Whitespace,
        SingleEscape,
        MultipleEscape,
        TerminatingMacro,
        NonTerminatingMacro,
        EOF = 32,
    }

    public class EOF
    {
        public static EOF Value = new EOF();
    }

    public class Readtable
    {
        internal static ReadtableEntry DefaultItem = new ReadtableEntry
        {
            Type = CharacterType.Constituent
        };

        internal static ReadtableEntry EofItem = new ReadtableEntry
        {
            Type = CharacterType.EOF
        };

        internal ReadtableEntry[] Items;
        internal Dictionary<char, ReadtableEntry> OtherItems;
        internal Readtable()
        {
            Items = new ReadtableEntry[ 127 ];
            for ( var i = 0; i < Items.Length; ++i )
            {
                Items[ i ] = new ReadtableEntry();
            }
            OtherItems = new Dictionary<char, ReadtableEntry>();
        }

        internal void DefineMacro( char ch, CharacterType type = CharacterType.TerminatingMacro )
        {
            var item = GetEntry( ch, true );
            item.Handler = null;
            item.Handler2 = null;
            item.Type = type;
        }

        internal void DefineMacro( char ch, ReadtableHandler2 handler2, CharacterType type = CharacterType.TerminatingMacro )
        {
            var item = GetEntry( ch, true );
            item.Handler = null;
            item.Handler2 = handler2;
            item.Type = type;
        }

        internal ReadtableEntry GetEntry( char code, bool defining = false )
        {
            ReadtableEntry item;

            if ( code < 0 )
            {
                return EofItem;
            }
            else if ( code < Items.Length )
            {
                return Items[ code ];
            }
            else if ( OtherItems.TryGetValue( code, out item ) )
            {
                return item;
            }
            else if ( defining )
            {
                item = new ReadtableEntry();
                item.Character = code;
                OtherItems[ code ] = item;
                return item;
            }
            else
            {
                return DefaultItem;
            }
        }

        internal void Init()
        {
            for ( var i = 0; i < Items.Length; ++i )
            {
                var ch = ( char ) i;
                Items[ i ].Character = ch;
                Items[ i ].Type = Char.IsWhiteSpace( ch ) || Char.IsControl( ch ) ? CharacterType.Whitespace : CharacterType.Constituent;
                Items[ i ].Handler = null;
                Items[ i ].DispatchReadtable = null;
            }

            DefineMacro( LispReader.EofChar, CharacterType.EOF );
            DefineMacro( '\\', CharacterType.SingleEscape );
            DefineMacro( '|', CharacterType.MultipleEscape );

            SetMacroCharacter( Runtime.LambdaCharacter, Runtime.ReadLambdaCharacterHandler, CharacterType.NonTerminatingMacro );
            SetMacroCharacter( '(', Runtime.ReadListHandler );
            DefineMacro( ')' );
            SetMacroCharacter( '\'', Runtime.ReadQuoteHandler );
            DefineMacro( '@', CharacterType.NonTerminatingMacro );
            DefineMacro( '#', CharacterType.NonTerminatingMacro );
            SetMacroCharacter( ',', Runtime.ReadCommaHandler );
            SetMacroCharacter( ';', Runtime.ReadLineCommentHandler );
            SetMacroCharacter( '`', Runtime.ReadQuasiQuoteHandler );
            SetMacroCharacter( '{', Runtime.ReadPrototypeHandler );
            DefineMacro( '}' );
            SetMacroCharacter( '[', Runtime.ReadVectorHandler );
            DefineMacro( ']' );
            SetMacroCharacter( '"', Runtime.ReadStringHandler );

            SetDispatchMacroCharacter( '@', '"', Runtime.ReadStringHandler2 );
            SetDispatchMacroCharacter( '#', '(', Runtime.ReadShortLambdaExpressionHandler );
            SetDispatchMacroCharacter( '#', '|', Runtime.ReadBlockCommentHandler );
            SetDispatchMacroCharacter( '#', 'r', Runtime.ReadNumberHandler );
            SetDispatchMacroCharacter( '#', 'x', Runtime.ReadNumberHandler );
            SetDispatchMacroCharacter( '#', 'o', Runtime.ReadNumberHandler );
            SetDispatchMacroCharacter( '#', 'b', Runtime.ReadNumberHandler );
            SetDispatchMacroCharacter( '#', 'q', Runtime.ReadSpecialStringHandler );
            SetDispatchMacroCharacter( '#', '"', Runtime.ReadRegexHandler );
            //DefineMacro( '#', '/', Runtime.ReadRegexHandler );
            SetDispatchMacroCharacter( '#', '.', Runtime.ReadExecuteHandler );
            SetDispatchMacroCharacter( '#', '!', Runtime.ReadLineCommentHandler2 );
            SetDispatchMacroCharacter( '#', ';', Runtime.ReadExprCommentHandler );
            SetDispatchMacroCharacter( '#', '+', Runtime.ReadPlusExprHandler );
            SetDispatchMacroCharacter( '#', '-', Runtime.ReadMinusExprHandler );
            SetDispatchMacroCharacter( '#', '\\', Runtime.ReadCharacterHandler );
            SetDispatchMacroCharacter( '#', 'c', Runtime.ReadComplexNumberHandler );
            SetDispatchMacroCharacter( '#', 'i', Runtime.ReadInfixHandler );
            SetDispatchMacroCharacter( '#', ':', Runtime.ReadUninternedSymbolHandler );
        }
        internal void SetDispatchMacroCharacter( char ch1, char ch2, ReadtableHandler2 handler2, CharacterType type = CharacterType.TerminatingMacro )
        {
            var item = GetEntry( ch1, true );
            if ( item.DispatchReadtable == null )
            {
                item.DispatchReadtable = new Readtable();
            }
            item.Type = type;
            item.DispatchReadtable.DefineMacro( ch2, handler2 );
        }

        internal void SetMacroCharacter( char ch, ReadtableHandler handler, CharacterType type = CharacterType.TerminatingMacro )
        {
            var item = GetEntry( ch, true );
            item.Handler = handler;
            item.Handler2 = null;
            item.Type = type;
        }
    }

    public class ReadtableEntry
    {
        public char Character;
        public Readtable DispatchReadtable;
        public ReadtableHandler Handler;
        public ReadtableHandler2 Handler2;
        public CharacterType Type;
        public ReadtableEntry Clone()
        {
            var dest = new ReadtableEntry();
            dest.Character = Character;
            dest.Type = Type;
            dest.Handler = Handler;
            dest.Handler2 = Handler2;
            dest.DispatchReadtable = DispatchReadtable == null ? null : Runtime.CopyReadtable( DispatchReadtable );
            return dest;
        }
    }

    public partial class Runtime
    {
        [Lisp( "copy-readtable" )]
        public static Readtable CopyReadtable( params object[] kwargs )
        {
            var args = ParseKwargs( kwargs, new string[] { "readtable" }, GetReadtable() );
            var source = ( Readtable ) args[ 0 ];
            var dest = new Readtable();

            if ( source == null )
            {
                dest.Init();
            }
            else
            {
                for ( var i = 0; i < source.Items.Length; ++i )
                {
                    dest.Items[ i ] = source.Items[ i ].Clone();
                }
                foreach ( var pair in source.OtherItems )
                {
                    dest.OtherItems[ pair.Key ] = pair.Value.Clone();
                }
            }
            return dest;
        }

        [Lisp( "set-dispatch-macro-character" )]
        public static void SetDispatchMacroCharacter( char dispatchChar, char subChar, ReadtableHandler2 handler, params object[] kwargs )
        {
            var args = ParseKwargs( kwargs, new string[] { "readtable" }, GetReadtable() );
            var readtable = ( Readtable ) args[ 0 ];
            readtable.SetDispatchMacroCharacter( dispatchChar, subChar, handler );
        }

        [Lisp( "set-macro-character" )]
        public static void SetMacroCharacter( char dispatchChar, ReadtableHandler handler, params object[] kwargs )
        {
            var args = ParseKwargs( kwargs, new string[] { "non-terminating?", "readtable" }, false, GetReadtable() );
            var nonTerminating = ToBool( args[ 0 ] );
            var readtable = ( Readtable ) args[ 1 ];
            readtable.SetMacroCharacter( dispatchChar, handler, nonTerminating ? CharacterType.NonTerminatingMacro : CharacterType.TerminatingMacro );
        }

        [Lisp( "void" )]
        public static VOID Void()
        {
            return VOID.Value;
        }

        internal static Readtable GetReadtable()
        {
            var readtable = ( Readtable ) Runtime.GetDynamic( Symbols.Readtable );
            return readtable;
        }

        internal static Readtable GetStandardReadtable()
        {
            var table = new Readtable();
            table.Init();
            return table;
        }

        internal static Readtable GetPrettyPrintingReadtable()
        {
            var table = new Readtable();
            table.Init();
            //table.SetMacroCharacter( '`', Runtime.ReadPrettyPrintingQuasiQuoteHandler );
            return table;
        }

        internal static bool IsWordChar( char ch )
        {
            var item = DefaultReadtable.GetEntry( ch );
            return item.Type == CharacterType.Constituent;
        }

        internal static bool MustEscapeChar( char ch )
        {
            var item = DefaultReadtable.GetEntry( ch );
            return item.Type != CharacterType.Constituent && item.Type != CharacterType.NonTerminatingMacro;
        }
    }

    public class VOID
    {
        public static VOID Value = new VOID();
    }
}