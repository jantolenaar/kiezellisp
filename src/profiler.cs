// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Kiezel
{
    public partial class Runtime
    {
        internal static List<ProfilingEntry> ProfilingEntries = null;
        internal static Stopwatch ProfilingTimer = Stopwatch.StartNew();

        [Lisp( "profiler.close-session" )]
        public static void CloseProfilingSession()
        {
            StopTimer();
            ProfilingEntries = null;
        }

        [Lisp( "profiler.open-session" )]
        public static void OpenProfilingSession()
        {
            ProfilingEntries = new List<ProfilingEntry>();
            RestartTimer();
        }

        [Lisp( "profiler.read-timer" )]
        public static long ReadTimer()
        {
            return ProfilingTimer.ElapsedMilliseconds;
        }

        [Lisp( "profiler.reset-timer" )]
        public static void ResetTimer()
        {
            ProfilingTimer.Reset();
        }

        [Lisp( "profiler.restart-timer" )]
        public static void RestartTimer()
        {
            ProfilingTimer.Restart();
        }
        [Lisp( "profiler.save-session" )]
        public static void SaveProfilingSession( string path )
        {
            //string comma = ",";

            using ( var stream = new StreamWriter( path ) )
            {
                stream.WriteLine( "nr,start,end,time,code" );

                foreach ( ProfilingEntry item in ToIter( ProfilingEntries ) )
                {
                    if ( item.End != -1 )
                    {
                        if ( item.Start != item.End )
                        {
                            stream.WriteLine( "{0},{1},{2},{3},\"{4}\"", item.SeqNr, item.Start, item.End, item.End - item.Start, ToPrintString( item.Form, true ).Replace( "\"", "\"\"" ) );
                        }
                    }
                    else
                    {
                        stream.WriteLine( "{0},{1},{2},{3},\"{4}\"", item.SeqNr, item.Start, "", "", ToPrintString( item.Form, true ).Replace( "\"", "\"\"" ) );
                    }
                }
            }

            CloseProfilingSession();
        }

        [Lisp( "profiler.start-timer" )]
        public static void StartTimer()
        {
            ProfilingTimer.Start();
        }

        [Lisp( "profiler.stop-timer" )]
        public static void StopTimer()
        {
            ProfilingTimer.Stop();
        }
        internal static int LogBeginCall( Cons form )
        {
            if ( ProfilingEntries != null )
            {
                long time = ReadTimer();
                int index = ProfilingEntries.Count;
                ProfilingEntries.Add( new ProfilingEntry( index + 1, time, form ) );
                return index;
            }
            else
            {
                return -1;
            }
        }

        internal static void LogEndCall( int index )
        {
            if ( ProfilingEntries != null && index != -1 )
            {
                long time = ReadTimer();
                ProfilingEntries[ index ].End = time;
            }
        }

        internal class ProfilingEntry
        {
            internal long End;
            internal Cons Form;
            internal long SeqNr;
            internal long Start;
            internal ProfilingEntry( int seqnr, long time, Cons form )
            {
                SeqNr = seqnr;
                Start = time;
                End = -1;
                Form = form;
            }
        }
    }
}