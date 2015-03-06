// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Kiezel
{
    public class CommandLineParser
    {
        private string[] args;

        private int endOfOptions;

        private List<Option> options = new List<Option>();

        private Dictionary<string, string> values;

        [Flags]
        public enum Flags
        {
            None = 0,
            Required = 1,
            Joined = 2,
            Placed = 4,
            Long = 8,
            Short = 16,
        }

        public void AddOption( string spec1, string spec2 )
        {
            Option option = new Option();
            ParseOption( spec1, ref option );
            ParseOption( spec2, ref option );
            options.Add( option );
        }

        public void AddOption( string spec )
        {
            Option option = new Option();
            ParseOption( spec, ref option );
            options.Add( option );
        }

        public Option FindOption( bool longName, string arg, out string value )
        {
            value = null;
            foreach ( Option option in options )
            {
                if ( option.Match( longName, arg, out value ) )
                {
                    return option;
                }
            }
            return null;
        }

        public string GetArgument( int pos )
        {
            if ( 0 <= pos && pos < args.Length - endOfOptions )
            {
                return args[ pos + endOfOptions ];
            }
            else
            {
                return null;
            }
        }

        public string[] GetArgumentArray( int pos )
        {
            if ( 0 <= pos && pos <= args.Length - endOfOptions )
            {
                string[] dest = new string[ args.Length - endOfOptions - pos ];
                Array.Copy( args, pos + endOfOptions, dest, 0, dest.Length );
                return dest;
            }
            else
            {
                return new string[ 0 ];
            }
        }

        public string GetOption( string name )
        {
            string value;
            if ( values.TryGetValue( name, out value ) )
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        public void Parse( string[] args )
        {
            this.args = args;
            values = new Dictionary<string, string>();
            int i = 0;
            while ( i < args.Length && args[ i ].StartsWith( "-" ) )
            {
                string arg = args[ i++ ];

                if ( arg == "--" )
                {
                    break;
                }

                bool isLong = arg.StartsWith( "--" );
                string spec = isLong ? arg.Substring( 2 ) : arg.Substring( 1 );

            repeat:

                string value;
                Option option = FindOption( isLong, spec, out value );

                if ( option == null )
                {
                    throw new LispException( "Invalid command line option: \"{0}\"", arg );
                }

                if ( ( option.Flags & Flags.Joined ) != 0 )
                {
                    // value already calculated by FindOption
                }
                else if ( ( option.Flags & Flags.Placed ) != 0 )
                {
                    if ( value != "" )
                    {
                        throw new LispException( "Invalid command line option: \"{0}\"", arg );
                    }

                    if ( i < args.Length && args[ i ].StartsWith( "-" ) == false )
                    {
                        value = args[ i++ ];
                    }
                }
                else if ( value != "" )
                {
                    // concatenated options?
                    if ( option.LongName != null )
                    {
                        values[ option.LongName ] = "";
                    }
                    if ( option.ShortName != null )
                    {
                        values[ option.ShortName ] = "";
                    }
                    spec = value;
                    goto repeat;
                }

                if ( value == "" && ( option.Flags & Flags.Required ) != 0 )
                {
                    throw new LispException( "Command line option {0} must have a value", arg );
                }

                if ( option.LongName != null )
                {
                    values[ option.LongName ] = value;
                }

                if ( option.ShortName != null )
                {
                    values[ option.ShortName ] = value;
                }
            }

            endOfOptions = i;
        }

        public void ParseOption( string spec, ref Option option )
        {
            var cases = new[]
			{
				new { Pattern = @"^--(\w+)[:=]\[\w+\]$",	Long=true,  Flags = Flags.Joined },
				new { Pattern = @"^--(\w+)[:=]\w+\$",		Long=true,  Flags = Flags.Joined | Flags.Required },
				new { Pattern = @"^--(\w+)\s+\[\w+\]$",		Long=true,  Flags = Flags.Placed },
				new { Pattern = @"^--(\w+)\s+\w+$",			Long=true,  Flags = Flags.Placed | Flags.Required },
				new { Pattern = @"^--(\w+)\s*$",			Long=true,  Flags = Flags.None },
				new { Pattern = @"^-(\w)[:=]?\[\w+\]$",		Long=false, Flags = Flags.Joined },
				new { Pattern = @"^-(\w)[:=]?\w+\$",		Long=false, Flags = Flags.Joined | Flags.Required },
				new { Pattern = @"^-(\w)\s+\[\w+\]$",		Long=false, Flags = Flags.Placed },
				new { Pattern = @"^-(\w)\s+\w+$",			Long=false, Flags = Flags.Placed | Flags.Required },
				new { Pattern = @"^-(\w)\s*$",				Long=false, Flags = Flags.None },
				new { Pattern = @"^-()\s*$",				Long=false, Flags = Flags.None }
			};

            foreach ( var item in cases )
            {
                Match match = Regex.Match( spec, item.Pattern );
                if ( match.Success )
                {
                    if ( item.Long )
                    {
                        option.LongName = match.Groups[ 1 ].Value;
                    }
                    else
                    {
                        option.ShortName = match.Groups[ 1 ].Value;
                    }

                    if ( option.Flags == 0 )
                    {
                        option.Flags = item.Flags;
                    }
                    else if ( item.Flags != option.Flags )
                    {
                        throw new LispException( "Inconsistent command line option specification \"{0}\"", spec );
                    }

                    return;
                }
            }

            throw new LispException( "Invalid command line option specification: \"{0}\"", spec );
        }

        private void AddOption( Option option )
        {
            options.Add( option );
        }

        public class Option
        {
            public Flags Flags;
            public string LongName;
            public string ShortName;
            public Option()
            {
            }

            public bool Match( bool longName, string str, out string value )
            {
                value = "";
                if ( LongName != null && longName == true )
                {
                    if ( str == LongName )
                    {
                        return true;
                    }
                    else if ( ( Flags & Flags.Joined ) != 0 && ( str.StartsWith( LongName + "=" ) || str.StartsWith( LongName + ":" ) ) )
                    {
                        value = str.Substring( LongName.Length + 1 );
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if ( ShortName != null && longName == false )
                {
                    if ( str == ShortName )
                    {
                        return true;
                    }
                    else if ( ( Flags & Flags.Joined ) != 0 && ( str.StartsWith( ShortName + "=" ) || str.StartsWith( ShortName + ":" ) ) )
                    {
                        value = str.Substring( ShortName.Length + 1 );
                        return true;
                    }
                    else if ( str.StartsWith( ShortName ) )
                    {
                        value = str.Substring( ShortName.Length );
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
    }
}