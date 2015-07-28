// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace Kiezel
{
    [RestrictedImport]
    public class Prototype : DynamicObject, IEnumerable, /*ICustomTypeDescriptor, */ IApply
    {
        internal static Prototype Dummy = new Prototype();

        internal PrototypeDictionary Dict = new PrototypeDictionary( false );

        internal List<Prototype> Parents = new List<Prototype>();

        [Lisp]
        public Prototype( params object[] args )
        {
            Create( args );
        }

        public Symbol ClassName
        {
            get;
            internal set;
        }

        [Lisp]
        public IEnumerable Keys
        {
            get
            {
                return Runtime.AsList( AsDictionary().Keys );
            }
        }

        public object this[ object index ]
        {
            get
            {
                return GetValue( index );
            }
            set
            {
                SetValue( index, value );
            }
        }

        [Lisp]
        public static Prototype FromDictionary( PrototypeDictionary dict )
        {
            var proto = new Prototype();
            if ( dict != null )
            {
                proto.Dict = dict;
            }
            return proto;
        }

        [Lisp]
        public PrototypeDictionary AsDictionary()
        {
            var dict = new PrototypeDictionary();
            MergeInto( this, dict );
            return dict;
        }

        public IEnumerator GetEnumerator()
        {
            var h = new Hashtable();
            foreach ( var pair in AsDictionary() )
            {
                h[ pair.Key ] = pair.Value;
            }
            return h.GetEnumerator();
        }

        [Lisp]
        public Cons GetParents( params object[] args )
        {
            object[] kwargs = Runtime.ParseKwargs( args, new string[] { "inherited" }, false );
            var inherited = Runtime.ToBool( kwargs[ 0 ] );
            var v = new Vector();
            GetParents( v, inherited );
            return Runtime.AsList( v );
        }

        [Lisp]
        public object GetTypeSpecifier()
        {
            var result = new Vector();
            GetTypeNames( result );
            switch ( result.Count )
            {
                case 0:
                {
                    return null;
                }
                case 1:
                {
                    return result[ 0 ];
                }
                default:
                {
                    return Runtime.AsList( result );
                }
            }
        }

        [Lisp]
        public object GetValue( object ident )
        {
            return GetValueFor( this, ident );
        }

        public bool HasInheritedProperty( object ident )
        {
            var name = GetKey( ident );

            if ( Dict.ContainsKey( name ) )
            {
                return true;
            }

            return Parents.Any( x => x.Dict.ContainsKey( name ) );
        }

        [Lisp]
        public bool HasProperty( object ident, params object[] args )
        {
            var name = GetKey( ident );

            object[] kwargs = Runtime.ParseKwargs( args, new string[] { "inherited" }, true );
            var inherited = Runtime.ToBool( kwargs[ 0 ] );

            if ( inherited )
            {
                return HasInheritedProperty( name );
            }
            else if ( Dict.ContainsKey( name ) )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        object IApply.Apply( object[] args )
        {
            return GetValue( args[ 0 ] );
        }

        /*
        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return new AttributeCollection();
        }

        //
        // Makes prototype work as a DataSource element
        //
        string ICustomTypeDescriptor.GetClassName()
        {
            return this.GetType().Name;
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return null;
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return null;
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return null;
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            throw new NotImplementedException();
        }

        object ICustomTypeDescriptor.GetEditor( Type editorBaseType )
        {
            return null;
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents( Attribute[] attributes )
        {
            return ( ( ICustomTypeDescriptor ) this ).GetEvents();
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return EventDescriptorCollection.Empty;
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties( Attribute[] attributes )
        {
            return ( ( ICustomTypeDescriptor ) this ).GetProperties();
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return new PropertyDescriptorCollection( Keys.Cast<string>().Select( x => new StringPropertyDescriptor( x, GetValue( x ) ) ).ToArray() );
        }

        object ICustomTypeDescriptor.GetPropertyOwner( PropertyDescriptor pd )
        {
            return this;
        }
*/
        [Lisp]
        public void SetParents( IEnumerable parents )
        {
            var newlist = new List<Prototype>();
            foreach ( Prototype p in Runtime.ToIter( parents ) )
            {
                if ( p != null )
                {
                    newlist.Add( p );
                }
            }
            Parents = newlist;
        }

        [Lisp]
        public object SetValue( object name, object value )
        {
            Dict[ GetKey( name ) ] = value;
            return value;
        }

        public override bool TryCreateInstance( CreateInstanceBinder binder, object[] args, out object result )
        {
            //if ( this == Dummy )
            //{
            //    result = new Prototype( null, args );
            //}
            //else
            {
                result = new Prototype( this, args );
            }

            return true;
        }

        public override bool TryGetIndex( GetIndexBinder binder, object[] indexes, out object result )
        {
            if ( indexes.Length != 1 )
            {
                throw new LispException( "Prototype objects support only one index" );
            }

            var index = GetKey( indexes[ 0 ] );
            return TryGetValue( index, out result );
        }

        public override bool TryGetMember( GetMemberBinder binder, out object result )
        {
            return TryGetValue( binder.Name, out result );
        }

        public bool TryGetValue( object name, out object result )
        {
            result = GetValue( name );
            return true;
        }

        public override bool TryInvokeMember( InvokeMemberBinder binder, object[] args, out object result )
        {
            if ( args != null && args.Length != 0 )
            {
                throw new LispException( "Prototype accessors cannot have arguments" );
            }
            return TryGetValue( binder.Name, out result );
        }

        public override bool TrySetIndex( SetIndexBinder binder, object[] indexes, object value )
        {
            if ( indexes.Length != 1 )
            {
                throw new LispException( "Prototype objects support only one index" );
            }

            var index = GetKey( indexes[ 0 ] );
            SetValue( index, value );
            return true;
        }

        public override bool TrySetMember( SetMemberBinder binder, object value )
        {
            SetValue( binder.Name, value );
            return true;
        }

        internal void AddDictionary( PrototypeDictionary dict )
        {
            foreach ( var pair in dict )
            {
                Dict[ pair.Key ] = pair.Value;
            }
        }
        internal void AddParent( Prototype parent )
        {
            if ( parent != null )
            {
                Parents.Add( parent );
            }
        }

        internal void Create( object[] args )
        {
            if ( args == null )
            {
                return;
            }

            int offset = 0;

            if ( ( args.Length & 1 ) != 0 )
            {
                offset = 1;

                // Odd length: first item is type specification
                var a = args[ 0 ];

                if ( Runtime.Listp( a ) )
                {
                    foreach ( var b in Runtime.ToIter( a ) )
                    {
                        ProcessTypeArg( b );
                    }
                }
                else
                {
                    ProcessTypeArg( a );
                }
            }

            for ( var i = offset; i + 1 < args.Length; i += 2 )
            {
                var key = GetKey( args[ i ] );
                if ( key != null )
                {
                    var value = args[ i + 1 ];
                    Dict[ key ] = value;
                }
            }
        }

        internal void ProcessTypeArg( object a )
        {
            if ( a is Prototype )
            {
                AddParent( ( Prototype ) a );
            }
            else if ( a is Symbol )
            {
                var type = Runtime.GetType( ( Symbol ) a ) as Prototype;
                if ( type == null )
                {
                    throw new LispException( "Type not found or not a prototype/structure: {0}", a );
                }
                AddParent( type );
            }
            else
            {
                throw new LispException( "Invalid type specifier in prototype constructor: {0}", a );
            }
        }

        internal object GetKey( object key )
        {
            if ( key is Symbol )
            {
                return ( ( Symbol ) key ).Name;
            }
            else
            {
                return key;
            }
        }

        internal void GetParents( Vector result, bool inherited )
        {
            foreach ( var parent in Parents )
            {
                result.Add( parent );
                if ( inherited )
                {
                    parent.GetParents( result, inherited );
                }
            }
        }

        internal void GetTypeNames( Vector result )
        {
            if ( ClassName != null )
            {
                result.Add( ClassName );
            }
            else
            {
                foreach ( var parent in Parents )
                {
                    parent.GetTypeNames( result );
                }
            }
        }

        internal object GetValueFor( Prototype target, object ident )
        {
            var name = GetKey( ident );
            object result = null;

            if ( Dict.TryGetValue( name, out result ) )
            {
                if ( result is LambdaClosure )
                {
                    var lambda = ( LambdaClosure ) result;
                    if ( lambda.IsGetter )
                    {
                        return Runtime.Funcall( lambda, target );
                    }
                    else
                    {
                        return result;
                    }
                }
                else
                {
                    return result;
                }
            }

            foreach ( var parent in Parents )
            {
                result = parent.GetValueFor( target, ident );
                if ( result != null )
                {
                    return result;
                }
            }

            return null;
        }

        internal bool IsSubTypeOf( Prototype proto )
        {
            if ( proto == this )
            {
                return true;
            }

            foreach ( var parent in Parents )
            {
                if ( parent.IsSubTypeOf( proto ) )
                {
                    return true;
                }
            }

            return false;
        }

        internal void MergeInto( Prototype original, PrototypeDictionary dict )
        {
            foreach ( var item in Dict )
            {
                if ( !dict.ContainsKey( item.Key ) )
                {
                    var result = item.Value;
                    if ( result is LambdaClosure )
                    {
                        var lambda = ( LambdaClosure ) result;
                        if ( lambda.IsGetter )
                        {
                            result = Runtime.Funcall( lambda, original );
                        }
                    }
                    dict[ item.Key ] = result;
                }
            }

            foreach ( var parent in Parents )
            {
                parent.MergeInto( original, dict );
            }
        }
    }

    public class PrototypeDictionary : Dictionary<object, object>
    {
        public PrototypeDictionary()
        {
        }

        public PrototypeDictionary( bool caseInsensitive )
            : base( caseInsensitive ? new CaseInsensitiveEqualityComparer() : null )
        {
        }
    }

    /*
    public class StringPropertyDescriptor : PropertyDescriptor
    {
        private readonly string m_Name;
        private readonly object m_Value;

        public StringPropertyDescriptor( string name, object val )
            : base( name, new Attribute[] { } )
        {
            m_Name = name;
            m_Value = val;
        }

        public override Type ComponentType
        {
            get
            {
                return typeof( Prototype );
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public override Type PropertyType
        {
            get
            {
                return typeof( object );
            }
        }

        public override bool CanResetValue( object component )
        {
            return false;
        }

        public override bool Equals( object obj )
        {

            if ( obj is StringPropertyDescriptor )
            {
                return ( ( StringPropertyDescriptor ) obj ).m_Name == m_Name;
            }

            return base.Equals( obj );
        }

        public override int GetHashCode()
        {
            return m_Name.GetHashCode();
        }

        public override object GetValue( object component )
        {
            var proto = ( Prototype ) component;
            return proto.GetValue( m_Name );
        }

        public override void ResetValue( object component )
        {
        }
        public override void SetValue( object component, object value )
        {
            var proto = ( Prototype ) component;
            proto.SetValue( m_Name, value );
        }

        public override bool ShouldSerializeValue( object component )
        {
            return false;
        }
    }
*/
    internal class CaseInsensitiveEqualityComparer : IEqualityComparer<object>
    {
        private CaseInsensitiveComparer comparer = CaseInsensitiveComparer.DefaultInvariant;

        bool IEqualityComparer<object>.Equals( object x, object y )
        {
            return comparer.Compare( x, y ) == 0;
        }

        int IEqualityComparer<object>.GetHashCode( object obj )
        {
            if ( obj is string )
            {
                return obj.ToString().ToLowerInvariant().GetHashCode();
            }
            else
            {
                return obj.GetHashCode();
            }
        }
    }
}