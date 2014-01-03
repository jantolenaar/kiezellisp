// Copyright (C) 2012-2014 Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using ThreadFunc = System.Func<object>;

namespace Kiezel
{
    internal struct ThreadContextState
    {
        internal SpecialVariables SpecialStack;
        internal Cons EvaluationStack;
        internal Frame Frame;
        internal int NestingDepth;
    }

    internal class SpecialVariables
    {
        internal Symbol Sym;
        internal object _value;
        internal SpecialVariables Link;

        internal SpecialVariables( Symbol sym, object value, SpecialVariables link )
        {
            Sym = sym;
            Value = value;
            Link = link;
        }

        internal object Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        internal static SpecialVariables Clone( SpecialVariables var )
        {
            if ( var == null )
            {
                return null;
            }
            else
            {
                return new SpecialVariables( var.Sym, var.Value, Clone( var.Link ) );
            }
        }
    }

    public class ThreadContext
    {
        internal Cons EvaluationStack = null;
        internal SpecialVariables SpecialStack = null;
        internal Frame _Frame = null;
        internal int NestingDepth = 0;

        internal ThreadContext( SpecialVariables specialStack )
        {
            this.SpecialStack = SpecialVariables.Clone( specialStack );
        }

        internal Frame Frame
        {
            get
            {
                if ( _Frame == null )
                {
                    _Frame = new Frame();
                }

                return _Frame;
            }
            set
            {
                _Frame = value;
            }
        }

        internal Task<object> Task;

        public object Result
        {
            get
            {
                return Task.Result;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return Task.IsCompleted;
            }
        }

        internal ThreadContextState SaveStackAndFrame( Frame frame = null, Cons form = null )
        {
            var saved = new ThreadContextState();

            saved.EvaluationStack = EvaluationStack;
            saved.SpecialStack = SpecialStack;
            saved.Frame = Frame;
            saved.NestingDepth = NestingDepth;

            ++NestingDepth;

            if ( NestingDepth == 10000 )
            {
                System.Diagnostics.Debugger.Break();
            }

            if ( frame != null )
            {
                Frame = new Frame( frame, null );
                Frame.Link = saved.Frame;
            }

            if ( form != null )
            {
                EvaluationStack = Runtime.MakeCons( form, EvaluationStack );
            }

            return saved;
        }

        internal void RestoreStackAndFrame( ThreadContextState saved )
        {
            Frame = saved.Frame;
            EvaluationStack = saved.EvaluationStack;
            SpecialStack = saved.SpecialStack;
            NestingDepth = saved.NestingDepth;
        }

        internal void RestoreFrame( ThreadContextState saved )
        {
            Frame = saved.Frame;
        }

    }

    public class GeneratorThreadContext : ThreadContext, IEnumerable
    {
        internal BlockingCollection<object> list;

        internal GeneratorThreadContext( int capacity ):
            base( Runtime.GetCurrentThread().SpecialStack )
        {
            list = new BlockingCollection<object>( capacity );
        }

        internal void YieldBreak()
        {
            list.CompleteAdding();
        }

        internal void Yield( object item )
        {
            // called by generator
            list.Add( item );
        }

        internal object Resume()
        {
            // called by client of generator
            return list.Take();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetConsumingEnumerable().GetEnumerator();
        }
    }

    public partial class Runtime
    {
        [Lisp( "sleep" )]
        public static void Sleep( int msec )
        {
            System.Threading.Thread.Sleep( msec );
        }

        [Lisp( "system.get-current-thread" )]
        public static ThreadContext GetCurrentThread()
        {
            return CurrentThreadContext;
        }

        [Lisp( "system.create-task" )]
        public static object CreateTask( ThreadFunc code )
        {
            var specials = GetCurrentThread().SpecialStack;
            return CreateTaskWithContext( code, new ThreadContext( specials ) );
        }

        [Lisp( "system.create-generator" )]
        public static object CreateGenerator( ThreadFunc code, params object[] kwargs )
        {
            object[] args = ParseKwargs( kwargs, new string[] { "capacity" }, 1 );
            var capacity = Convert.ToInt32( args[ 0 ] );
            var context = new GeneratorThreadContext( capacity );

            ThreadFunc wrapper = () =>
            {
                object val = code();
                context.YieldBreak();
                return val;
            };

            return CreateTaskWithContext( wrapper, context );
        }

        internal static object CreateTaskWithContext( ThreadFunc code, ThreadContext context )
        {
            Func<object> wrapper = () =>
            {
                try
                {
                    CurrentThreadContext = context;
                    return code();
                }
                catch ( Exception ex )
                {
                    throw Runtime.UnwindExceptionIntoNewException( ex );
                }
                finally
                {
                    CurrentThreadContext = null;
                }
            };

            var task = new Task<object>( wrapper );
            context.Task = task;
            task.Start();

            return context;
        }

        internal static GeneratorThreadContext CheckGeneratorThreadContext( ThreadContext ctx )
        {
            var context = ctx as GeneratorThreadContext;

            if ( context == null )
            {
                throw new LispException( "Thread is not a generator thread" );
            }

            return ( GeneratorThreadContext ) context;
        }

        [Lisp( "resume" )]
        public static object Resume( ThreadContext ctx )
        {
            var context = CheckGeneratorThreadContext( ctx );
            return context.Resume();
        }

        [Lisp( "yield" )]
        public static void Yield( object item )
        {
            var context = CheckGeneratorThreadContext( CurrentThreadContext );
            context.Yield( item );
        }

        [Lisp( "system.enable-benchmark" )]
        public static void EnableBenchmark( bool flag )
        {
            if ( flag )
            {
                Process.GetCurrentProcess().ProcessorAffinity = new IntPtr( 1 );
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            }
            else
            {
                Process.GetCurrentProcess().ProcessorAffinity = new IntPtr( 0 );
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                Thread.CurrentThread.Priority = ThreadPriority.Normal;
            }
        }

        static void CreateCommandListener( int port )
        {
            Task.Factory.StartNew( Listener, port );
        }

        static void AbortCommandListener()
        {
            if ( CommandListenerSocket != null )
            {
                CommandListenerSocket.Stop();
                CommandListenerSocket = null;
            }
        }

        internal static TcpListener CommandListenerSocket = null;

        internal static void Listener( object state )
        {
            var data = new byte[ 60000 ];

            while ( true )
            {
                try
                {
                    var port = ( int ) state;
                    CommandListenerSocket = new TcpListener( IPAddress.Loopback, port );
                    CommandListenerSocket.Start();

                    while ( true )
                    {
                        using ( var socket = CommandListenerSocket.AcceptTcpClient() )
                        {
                            socket.NoDelay = true;
                            socket.LingerState = new LingerOption( false, 0 );
                            using ( var stream = socket.GetStream() )
                            {
                                var count = stream.Read( data, 0, data.Length );
                                var str = System.Text.Encoding.UTF8.GetString( data, 0, count );
                                InsertExternalCommand( str );
                            }
                        }
                    }
                }
                catch
                {
                    // Mono: long enough for program to exit before restarting listener
                    Sleep( 1000 );
                }

                // no recovery
                break;
            }
        }

        //[Lisp("send")]
        public static int Send( string text )
        {
            return Send( text, 8080 );
        }

        //[Lisp( "send" )]
        public static int Send( string text, int port )
        {
            using ( var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
            {
                socket.Connect( new IPEndPoint( IPAddress.Loopback, port ) );
                var data = System.Text.Encoding.ASCII.GetBytes( text );
                return socket.Send( data );
            }
        }

    }
}
