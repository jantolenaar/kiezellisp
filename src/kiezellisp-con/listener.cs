#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    using ThreadFunc = System.Func<object>;

    public partial class RuntimeConsole
    {
        #region Static Fields

        public static ThreadContext CommandListener;
        public static TcpListener CommandListenerSocket;

        #endregion Static Fields

        #region Private Methods

        private static void AbortCommandListener()
        {
            if (CommandListenerSocket != null)
            {
                CommandListenerSocket.Stop();
                CommandListenerSocket = null;
            }
        }

        private static bool TryReadCommandListener(out char ch)
        {
            ch = (char)0;

            if (CommandListener != null)
            {
                var temp = Runtime.TryResume(CommandListener, null);
                if (temp != null)
                {
                    ch = (char)temp;
                    return true;
                }
            }

            return false;
        }

        #endregion Private Methods

        [Lisp("send")]
        public static int Send(string text)
        {
            return Send(text, 8080);
        }

        [Lisp("send")]
        public static int Send(string text, int port)
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(new IPEndPoint(IPAddress.Loopback, port));
                var data = System.Text.Encoding.ASCII.GetBytes(text);
                return socket.Send(data);
            }
        }


        [Lisp("start-listener")]
        public static void CreateCommandListener()
        {
            var port = (int)Runtime.GetDynamic(Symbols.ReplListenerPort);
            CreateCommandListener(port);
        }

        [Lisp("start-listener")]
        public static void CreateCommandListener(int port)
        {
            ThreadFunc func = () =>
            {
                Listener(port);
                return null;
            };

            CommandListener = Runtime.CreateGenerator(func);
        }

        public static void Listener(int port)
        {
            var data = new byte[60000];

            while (true)
            {
                try
                {
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
                                foreach (var ch in str)
                                {
                                    Runtime.Yield(ch);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Mono: long enough for program to exit before restarting listener
                    Runtime.Sleep(1000);
                }

                // no recovery
                break;
            }
        }
    }
}