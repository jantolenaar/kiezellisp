#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    public partial class Runtime
    {
        #region Fields

        public static List<ProfilingEntry> ProfilingEntries = null;
        public static Stopwatch ProfilingTimer = Stopwatch.StartNew();

        #endregion Fields

        #region Methods

        [Lisp("profiler.close-session")]
        public static void CloseProfilingSession()
        {
            StopTimer();
            ProfilingEntries = null;
        }

        public static int LogBeginCall(Cons form)
        {
            if (ProfilingEntries != null)
            {
                long time = ReadTimer();
                int index = ProfilingEntries.Count;
                ProfilingEntries.Add(new ProfilingEntry(index + 1, time, form));
                return index;
            }
            else
            {
                return -1;
            }
        }

        public static void LogEndCall(int index)
        {
            if (ProfilingEntries != null && index != -1)
            {
                long time = ReadTimer();
                ProfilingEntries[index].End = time;
            }
        }

        [Lisp("profiler.open-session")]
        public static void OpenProfilingSession()
        {
            ProfilingEntries = new List<ProfilingEntry>();
            RestartTimer();
        }

        [Lisp("profiler.read-timer")]
        public static long ReadTimer()
        {
            return ProfilingTimer.ElapsedMilliseconds;
        }

        [Lisp("profiler.reset-timer")]
        public static void ResetTimer()
        {
            ProfilingTimer.Reset();
        }

        [Lisp("profiler.restart-timer")]
        public static void RestartTimer()
        {
            ProfilingTimer.Restart();
        }

        [Lisp("profiler.save-session")]
        public static void SaveProfilingSession(string path)
        {
            //string comma = ",";

            using (var stream = new StreamWriter(path))
            {
                stream.WriteLine("nr,start,end,time,code");

                foreach (ProfilingEntry item in ToIter( ProfilingEntries ))
                {
                    if (item.End != -1)
                    {
                        if (item.Start != item.End)
                        {
                            stream.WriteLine("{0},{1},{2},{3},\"{4}\"", item.SeqNr, item.Start, item.End, item.End - item.Start, ToPrintString(item.Form, true).Replace("\"", "\"\""));
                        }
                    }
                    else
                    {
                        stream.WriteLine("{0},{1},{2},{3},\"{4}\"", item.SeqNr, item.Start, "", "", ToPrintString(item.Form, true).Replace("\"", "\"\""));
                    }
                }
            }

            CloseProfilingSession();
        }

        [Lisp("profiler.start-timer")]
        public static void StartTimer()
        {
            ProfilingTimer.Start();
        }

        [Lisp("profiler.stop-timer")]
        public static void StopTimer()
        {
            ProfilingTimer.Stop();
        }

        #endregion Methods

        #region Nested Types

        public class ProfilingEntry
        {
            #region Fields

            public long End;
            public Cons Form;
            public long SeqNr;
            public long Start;

            #endregion Fields

            #region Constructors

            public ProfilingEntry(int seqnr, long time, Cons form)
            {
                SeqNr = seqnr;
                Start = time;
                End = -1;
                Form = form;
            }

            #endregion Constructors
        }

        #endregion Nested Types
    }
}