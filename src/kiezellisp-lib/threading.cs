// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ThreadFunc = System.Func<object>;

namespace Kiezel
{
    public struct ThreadContextState
    {
        public Cons EvaluationStack;
        public Frame Frame;
        public int NestingDepth;
        public SpecialVariables SpecialStack;
    }

    public class GeneratorThreadContext : ThreadContext, IEnumerable
    {
        public BlockingCollection<object> list;

        public GeneratorThreadContext(int capacity) :
            base(Runtime.GetCurrentThread().SpecialStack)
        {
            list = new BlockingCollection<object>(capacity);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetConsumingEnumerable().GetEnumerator();
        }

        public object Resume()
        {
            // called by client of generator
            return list.Take();
        }

        public void Yield(object item)
        {
            // called by generator
            list.Add(item);
        }

        public void YieldBreak()
        {
            list.CompleteAdding();
        }
    }

    public partial class Runtime
    {
        public static TcpListener CommandListenerSocket = null;

        [Lisp("system:create-generator")]
        public static object CreateGenerator(ThreadFunc code, params object[] kwargs)
        {
            object[] args = ParseKwargs(kwargs, new string[] { "capacity" }, 1);
            var capacity = Convert.ToInt32(args[0]);
            var context = new GeneratorThreadContext(capacity);

            ThreadFunc wrapper = () =>
            {
                object val = code();
                context.YieldBreak();
                return val;
            };

            return CreateTaskWithContext(wrapper, context, true);
        }

        [Lisp("system:create-task")]
        public static object CreateTask(ThreadFunc code)
        {
            return CreateTask(code, true);
        }

        [Lisp("system:create-task")]
        public static object CreateTask(ThreadFunc code, bool start)
        {
            var specials = GetCurrentThread().SpecialStack;
            return CreateTaskWithContext(code, new ThreadContext(specials), start);
        }

        [Lisp("system:enable-benchmark")]
        public static void EnableBenchmark(bool flag)
        {
            if (flag)
            {
                Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(1);
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            }
            else
            {
                Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(0);
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                Thread.CurrentThread.Priority = ThreadPriority.Normal;
            }
        }

        [Lisp("system:get-current-thread")]
        public static ThreadContext GetCurrentThread()
        {
            return CurrentThreadContext;
        }

        [Lisp("system:get-task-result")]
        public static object GetTaskResult(ThreadContext task)
        {
            return task.GetResult();
        }

        [Lisp("resume")]
        public static object Resume(ThreadContext ctx)
        {
            var context = CheckGeneratorThreadContext(ctx);
            return context.Resume();
        }

        //[Lisp("send")]
        public static int Send(string text)
        {
            return Send(text, 8080);
        }

        //[Lisp( "send" )]
        public static int Send(string text, int port)
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(new IPEndPoint(IPAddress.Loopback, port));
                var data = System.Text.Encoding.ASCII.GetBytes(text);
                return socket.Send(data);
            }
        }

        [Lisp("sleep")]
        public static void Sleep(int msec)
        {
            System.Threading.Thread.Sleep(msec);
        }

        [Lisp("yield")]
        public static void Yield(object item)
        {
            var context = CheckGeneratorThreadContext(CurrentThreadContext);
            context.Yield(item);
        }

        public static GeneratorThreadContext CheckGeneratorThreadContext(ThreadContext ctx)
        {
            var context = ctx as GeneratorThreadContext;

            if (context == null)
            {
                throw new LispException("Thread is not a generator thread");
            }

            return (GeneratorThreadContext)context;
        }

        public static object CreateTaskWithContext(ThreadFunc code, ThreadContext context, bool start)
        {
            Func<object> wrapper = () =>
            {
                try
                {
                    CurrentThreadContext = context;
                    return code();
                }
                catch (Exception ex)
                {
                    throw Runtime.UnwindExceptionIntoNewException(ex);
                }
                finally
                {
                    CurrentThreadContext = null;
                }
            };

            var task = new Task<object>(wrapper);
            context.Task = task;
            if (start)
            {
                context.Start();
            }

            return context;
        }

        public static void Listener(object state)
        {
            var data = new byte[ 60000 ];

            while (true)
            {
                try
                {
                    var port = (int)state;
                    CommandListenerSocket = new TcpListener(IPAddress.Loopback, port);
                    CommandListenerSocket.Start();

                    while (true)
                    {
                        using (var socket = CommandListenerSocket.AcceptTcpClient())
                        {
                            socket.NoDelay = true;
                            socket.LingerState = new LingerOption(false, 0);
                            using (var stream = socket.GetStream())
                            {
                                var count = stream.Read(data, 0, data.Length);
                                var str = System.Text.Encoding.UTF8.GetString(data, 0, count);
                                // TO DO
                                InsertExternalCommand(str);
                            }
                        }
                    }
                }
                catch
                {
                    // Mono: long enough for program to exit before restarting listener
                    Sleep(1000);
                }

                // no recovery
                break;
            }
        }

        static void InsertExternalCommand(string str)
        {
        }

        private static void AbortCommandListener()
        {
            if (CommandListenerSocket != null)
            {
                CommandListenerSocket.Stop();
                CommandListenerSocket = null;
            }
        }

        [Lisp("start-listener")]
        public static void CreateCommandListener()
        {
            var port = (int)GetDynamic(Symbols.ReplListenerPort);
            CreateCommandListener(port);
        }

        [Lisp("start-listener")]
        public static void CreateCommandListener(int port)
        {
            Task.Factory.StartNew(Listener, port);
        }
    }

    public class ThreadContext
    {
        public Frame _Frame = null;
        public Cons EvaluationStack = null;
        public int NestingDepth = 0;
        public SpecialVariables SpecialStack = null;
        public Task<object> Task;

        public ThreadContext(SpecialVariables specialStack)
        {
            this.SpecialStack = SpecialVariables.Clone(specialStack);
        }

        public bool IsCompleted
        {
            get
            {
                return Task.IsCompleted;
            }
        }

        public Frame Frame
        {
            get
            {
                if (_Frame == null)
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

        public object GetResult()
        {
            Start();
            return Task.Result;
        }

        public void Start()
        {
            if (Task.Status == TaskStatus.Created)
            {
                try
                {
                    Task.Start();
                }
                catch
                {
                }
            }
        }

        public void RestoreFrame(ThreadContextState saved)
        {
            Frame = saved.Frame;
        }

        public void RestoreStackAndFrame(ThreadContextState saved)
        {
            Frame = saved.Frame;
            EvaluationStack = saved.EvaluationStack;
            SpecialStack = saved.SpecialStack;
            NestingDepth = saved.NestingDepth;
        }

        public ThreadContextState SaveStackAndFrame(Frame frame = null, Cons form = null)
        {
            var saved = new ThreadContextState();

            saved.EvaluationStack = EvaluationStack;
            saved.SpecialStack = SpecialStack;
            saved.Frame = Frame;
            saved.NestingDepth = NestingDepth;

            ++NestingDepth;

            if (NestingDepth == 10000)
            {
                System.Diagnostics.Debugger.Break();
            }

            if (frame != null)
            {
                Frame = new Frame(frame, null);
                Frame.Link = saved.Frame;
            }

            if (form != null)
            {
                EvaluationStack = Runtime.MakeCons(form, EvaluationStack);
            }

            return saved;
        }
    }

    public class SpecialVariables
    {
        public object _value;
        public bool Constant;
        public SpecialVariables Link;
        public Symbol Sym;

        public SpecialVariables(Symbol sym, bool constant, object value, SpecialVariables link)
        {
            Sym = sym;
            Constant = constant;
            Value = value;
            Link = link;
        }

        public object CheckedValue
        {
            set
            {
                if (Constant)
                {
                    throw new LispException("Cannot assign to constant: {0}", Sym);
                }

                _value = value;
            }
        }

        public object Value
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

        public static SpecialVariables Clone(SpecialVariables var)
        {
            if (var == null)
            {
                return null;
            }
            else
            {
                return new SpecialVariables(var.Sym, var.Constant, var.Value, Clone(var.Link));
            }
        }
    }
}