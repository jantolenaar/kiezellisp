#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    using ThreadFunc = System.Func<object>;

    public class GeneratorThreadContext : ThreadContext, IEnumerable
    {
        #region Fields

        public BlockingCollection<object> list;

        #endregion Fields

        #region Constructors

        public GeneratorThreadContext(int capacity)
            : base(Runtime.GetCurrentThread().SpecialStack)
        {
            list = new BlockingCollection<object>(capacity);
        }

        #endregion Constructors

        #region Private Methods

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetConsumingEnumerable().GetEnumerator();
        }

        #endregion Private Methods

        #region Public Methods

        public object Resume()
        {
            // called by client of generator
            return list.Take();
        }

        public object TryResume(object eofValue)
        {
            // called by client of generator
            object val;
            if (list.TryTake(out val))
            {
                return val;
            }
            else
            {
                return eofValue;
            }
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

        #endregion Public Methods
    }

    public partial class Runtime
    {

        #region Public Methods

        public static GeneratorThreadContext CheckGeneratorThreadContext(ThreadContext ctx)
        {
            var context = ctx as GeneratorThreadContext;

            if (context == null)
            {
                throw new LispException("Thread is not a generator thread");
            }

            return context;
        }

        [Lisp("system:create-generator")]
        public static ThreadContext CreateGenerator(ThreadFunc code, params object[] kwargs)
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
        public static ThreadContext CreateTask(ThreadFunc code)
        {
            return CreateTask(code, true);
        }

        [Lisp("system:create-task")]
        public static ThreadContext CreateTask(ThreadFunc code, bool start)
        {
            var specials = GetCurrentThread().SpecialStack;
            return CreateTaskWithContext(code, new ThreadContext(specials), start);
        }

        public static ThreadContext CreateTaskWithContext(ThreadFunc code, ThreadContext context, bool start)
        {
            Func<object> wrapper = () =>
            {
                try
                {
                    CurrentThreadContext = context;
                    return code();
                }
                catch (InterruptException)
                {
                    return null;
                }
                catch (Exception ex)
                {
                    var ex2 = UnwindExceptionIntoNewException(ex);
                    PrintError(ex.ToString());
                    throw ex2;
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

        [Lisp("system:create-thread")]
        public static ThreadContext CreateThread(ThreadFunc code)
        {
            return CreateThread(code, true);
        }

        [Lisp("system:create-thread")]
        public static ThreadContext CreateThread(ThreadFunc code, bool start)
        {
            var specials = GetCurrentThread().SpecialStack;
            return CreateThreadWithContext(code, new ThreadContext(specials), start);
        }

        public static ThreadContext CreateThreadWithContext(ThreadFunc code, ThreadContext context, bool start)
        {
            ThreadStart wrapper = () =>
            {
                try
                {
                    CurrentThreadContext = context;
                    context.ThreadResult = code();
                }
                catch (InterruptException)
                {

                }
                catch (Exception ex)
                {
                    var ex2 = UnwindExceptionIntoNewException(ex);
                    PrintError(ex.ToString());
                    throw ex2;
                }
                finally
                {
                    CurrentThreadContext = null;
                }
            };

            var thread = new Thread(wrapper);
            context.Thread = thread;
            if (start)
            {
                context.Start();
            }

            return context;
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

        [Lisp("try-resume")]
        public static object TryResume(ThreadContext ctx, object eofValue)
        {
            var context = CheckGeneratorThreadContext(ctx);
            return context.TryResume(eofValue);
        }

        [Lisp("sleep")]
        public static void Sleep(int msec)
        {
            Thread.Sleep(msec);
        }

        [Lisp("yield")]
        public static void Yield(object item)
        {
            var context = CheckGeneratorThreadContext(CurrentThreadContext);
            context.Yield(item);
        }

        #endregion Public Methods
    }

    public class SpecialVariables
    {
        #region Fields

        public bool Constant;
        public SpecialVariables Link;
        public Symbol Sym;
        public object _value;

        #endregion Fields

        #region Constructors

        public SpecialVariables(Symbol sym, bool constant, object value, SpecialVariables link)
        {
            Sym = sym;
            Constant = constant;
            Value = value;
            Link = link;
        }

        #endregion Constructors

        #region Public Properties

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

        #endregion Public Properties

        #region Public Methods

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

        #endregion Public Methods
    }

    public class ThreadContext
    {
        #region Fields

        public Cons EvaluationStack;
        public int NestingDepth;
        public SpecialVariables SpecialStack;
        public Task<object> Task;
        public Thread Thread;
        public object ThreadResult;
        public Frame _Frame;

        #endregion Fields

        #region Constructors

        public ThreadContext(SpecialVariables specialStack)
        {
            SpecialStack = SpecialVariables.Clone(specialStack);
        }

        #endregion Constructors

        #region Public Properties

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

        public bool IsCompleted
        {
            get
            {
                if (Task != null)
                {
                    return Task.IsCompleted;
                }
                else
                {
                    return Thread.ThreadState == System.Threading.ThreadState.Stopped;
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public object GetResult()
        {
            Start();
            if (Task != null)
            {
                return Task.Result;
            }
            else
            {
                Thread.Join();
                return ThreadResult;
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

        public ThreadContextState SaveStackAndFrame(Frame frame, Cons form)
        {
            var saved = new ThreadContextState();

            saved.EvaluationStack = EvaluationStack;
            saved.SpecialStack = SpecialStack;
            saved.Frame = Frame;
            saved.NestingDepth = NestingDepth;

            ++NestingDepth;

            if (NestingDepth == 10000)
            {
                Debugger.Break();
            }

            if (frame != null)
            {
                Frame = frame;
                Frame.Link = saved.Frame;
            }

            if (form != null)
            {
                EvaluationStack = Runtime.MakeCons(form, EvaluationStack);
            }

            return saved;
        }

        public void Start()
        {
            if (Task != null)
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
            else
            {
                if (Thread.ThreadState == System.Threading.ThreadState.Unstarted)
                {
                    try
                    {
                        Thread.Start();
                    }
                    catch
                    {
                    }
                }
            }
        }

        #endregion Public Methods
    }

    public class ThreadContextState
    {
        #region Fields

        public Cons EvaluationStack;
        public Frame Frame;
        public int NestingDepth;
        public SpecialVariables SpecialStack;

        #endregion Fields
    }
}