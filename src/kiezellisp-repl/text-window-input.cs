#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;

    public partial class TextWindow
    {
        #region Fields

        internal int CharsWritten;
        internal bool Done;
        internal Dictionary<Keys, object> EditHandlers = new Dictionary<Keys, object>();
        internal int HomePos;
        internal int LineBound;
        internal StringBuilder LineBuffer;
        internal int LineIndex;
        internal int LineMark;
        internal int MaxChars;
        internal int SavedPos;

        #endregion Fields

        #region Methods

        public string Read(params object[] args)
        {
            object[] kwargs = Runtime.ParseKwargs(args, new string[] { "initial-value", "max-chars", "code-completion" }, "", -1, Runtime.MissingValue);
            var initialText = (string)kwargs[0];
            var maxChars = (int)kwargs[1];
            var codeCompletion = kwargs[2];
            var saved = CodeCompletion;
            var result = "";
            if (codeCompletion != Runtime.MissingValue)
            {
                CodeCompletion = Runtime.ToBool(codeCompletion);
            }
            result = GetStringInput(initialText, maxChars);
            CodeCompletion = saved;
            return result;
        }

        public char ReadChar()
        {
            ShowCursor();

            while (true)
            {
                var key = ReadKey(true);
                if (key.KeyChar != 0)
                {
                    HideCursor();
                    return key.KeyChar;
                }
            }
        }

        public KeyInfo ReadKey()
        {
            return ReadKey(false);
        }

        public KeyInfo ReadKey(bool echo)
        {
            if (Dirty)
            {
                Refresh();
            }

            ScrollIntoView(false);

            while (true)
            {
                var info = ParentControl.ReadKey();
                if (info == null)
                {
                    continue;
                }

                object handler;
                if (ScrollHandlers.TryGetValue(info.KeyData, out handler))
                {
                    if (handler is ScrollHandler)
                    {
                        var handled = ((ScrollHandler)handler)();
                        if (handled)
                        {
                            continue;
                        }
                    }
                    else if (handler is MouseHandler)
                    {
                        var handled = ((MouseHandler)handler)(info.MouseCol, info.MouseRow, info.MouseWheel);
                        if (handled)
                        {
                            continue;
                        }
                    }
                }

                if (echo && info.KeyChar != 0)
                {
                    Write(info.KeyChar);
                    return info;
                }
                else {
                    return info;
                }
            }
        }

        public string ReadLine(params object[] args)
        {
            var s = Read(args);
            WriteLine();
            ShowCursor();
            return s;
        }

        internal void CmdAbortEdit()
        {
            CmdEscape();
            //Done = true;
        }

        internal void CmdBackspace()
        {
            if (LineIndex > 0)
            {
                --LineIndex;
                CmdDeleteChar();
            }
        }

        internal void CmdCodeCompletion()
        {
            if (!CodeCompletion)
            {
                return;
            }
            CmdMoveBackOverSpaces();
            var searchTerm = Runtime.GetWordFromString(LineBuffer.ToString().Substring(0, LineIndex), LineIndex, Runtime.IsLispWordChar);
            var completions = RuntimeConsoleBase.GetCompletions(searchTerm);
            var index = 0;
            var done = false;
            SavedPos = CursorPos;

            while (!done)
            {
                CursorPos = SavedPos + CharsWritten;
                WriteLine();
                for (int i = 0; i < completions.Count; ++i)
                {
                    if (i == index)
                    {
                        Highlight = true;
                    }
                    Write(completions[i]);
                    if (i == index)
                    {
                        Highlight = false;
                    }
                    Write(' ');
                }
                ClearToBot();
                var info2 = ReadKey(false);
                var key2 = info2.KeyData;
                if (key2 == Keys.Down || key2 == Keys.Enter || (key2 == Keys.Tab && completions.Count == 1))
                {
                    CursorPos = SavedPos + CharsWritten;
                    ClearToBot();
                    CursorPos = SavedPos;
                    var newPos = LineIndex - searchTerm.Length;
                    while (LineIndex != newPos)
                    {
                        CmdBackspace();
                    }
                    InsertString(completions[index] + " ");
                    done = true;
                }
                else if (key2 == Keys.Left || key2 == (Keys.Tab | Keys.Shift))
                {
                    // Stay positive.
                    index = (index + completions.Count - 1) % completions.Count;
                }
                else if (key2 == Keys.Right || key2 == Keys.Tab)
                {
                    index = (index + 1) % completions.Count;
                }
                else if (key2 == Keys.Up || key2 == Keys.Escape || key2 == RuntimeRepl.PseudoKeyForResizeEvent)
                {
                    CursorPos = SavedPos + CharsWritten;
                    ClearToBot();
                    CursorPos = SavedPos;
                    done = true;
                }
            }
        }

        internal void CmdCopy()
        {
            string text = "";

            if (LineMark == -1)
            {
                text = LineBuffer.ToString();
            }
            else if (LineMark <= LineBound)
            {
                text = LineBuffer.ToString().Substring(LineMark, LineBound - LineMark);
            }
            else if (LineBound < LineMark)
            {
                text = LineBuffer.ToString().Substring(LineBound, LineMark - LineBound);
            }
            Runtime.SetClipboardData(text);
        }

        internal void CmdDataChar(char ch)
        {
            // Inserts a character at Pos and increments EndPos.
            if (MaxChars == -1 || LineBuffer.Length < MaxChars)
            {
                InsertChar(ch);
            }
        }

        internal void CmdDeleteChar()
        {
            if (LineIndex < LineBuffer.Length)
            {
                LineBuffer.Remove(LineIndex, 1);
            }
        }

        internal void CmdEnd()
        {
            LineIndex = LineBuffer.Length;
        }

        internal void CmdEnter()
        {
            CmdEnd();
            Text = LineBuffer.ToString();
            Done = true;
        }

        internal void CmdEscape()
        {
            LineBuffer.Clear();
            LineIndex = 0;
            Text = null;
            Done = true;
        }

        internal void CmdHome()
        {
            LineIndex = 0; ;
        }

        internal void CmdLeft()
        {
            LineIndex = Math.Max(0, LineIndex - 1);
        }

        internal void CmdMarkEnd()
        {
            Mark(CmdEnd);
        }

        internal void CmdMarkHome()
        {
            Mark(CmdHome);
        }

        internal void CmdMarkLeft()
        {
            Mark(CmdLeft);
        }

        internal void CmdMarkRight()
        {
            Mark(CmdRight);
        }

        internal void CmdMoveBackOverSpaces()
        {
            if (LineIndex == LineBuffer.Length || char.IsWhiteSpace(LineBuffer[LineIndex]))
            {
                while (LineIndex > 0 && char.IsWhiteSpace(LineBuffer[LineIndex - 1]))
                {
                    --LineIndex;
                }
            }
        }

        internal void CmdPaste()
        {
            string str = Runtime.GetClipboardData();
            InsertString(str);
        }

        internal void CmdRight()
        {
            LineIndex = Math.Min(LineIndex + 1, LineBuffer.Length);
        }

        internal string GetStringInput(string initialText, int maxChars)
        {
            MaxChars = maxChars;
            HomePos = CursorPos;
            Done = false;
            LineBuffer = new StringBuilder();
            LineIndex = 0;

            InsertString(initialText);

            while (!Done)
            {
                PaintLineBuffer();
                ShowCursor();
                var info = ReadKey(false);
                HideCursor();

                Runtime.InitRandom();

                bool handled = false;

                if (info.KeyData != 0)
                {
                    if (info.KeyData != (Keys.C | Keys.Control) && (info.KeyData & Keys.Shift) == 0)
                    {
                        if (LineMark != -1)
                        {
                            SetMark(-1);
                        }
                    }

                    object handler;
                    if (EditHandlers.TryGetValue(info.KeyData, out handler))
                    {
                        ScrollIntoView();

                        if (handler is EditHandler)
                        {
                            ((EditHandler)handler)();
                            handled = true;
                        }
                        if (handler is ScrollHandler)
                        {
                            ((ScrollHandler)handler)();
                            handled = true;
                        }
                        else if (handler is MouseHandler)
                        {
                            handled = true;
                            ((MouseHandler)handler)(info.MouseCol, info.MouseRow, info.MouseWheel);
                        }
                    }
                }

                if (!handled && info.KeyChar >= ' ')
                {
                    if (LineMark != -1)
                    {
                        SetMark(-1);
                    }
                    CmdDataChar(info.KeyChar);
                }
            }

            PaintLineBuffer();

            LineBuffer = null;

            return Text;
        }

        internal void InitEditHandlers()
        {
            AddEditHandler(Keys.Home, CmdHome);
            AddEditHandler(Keys.End, CmdEnd);
            AddEditHandler(Keys.Left, CmdLeft);
            AddEditHandler(Keys.Right, CmdRight);
            AddEditHandler(Keys.Enter, CmdEnter);
            AddEditHandler(Keys.Escape, CmdEscape);
            AddEditHandler(Keys.Back, CmdBackspace);
            AddEditHandler(Keys.Delete, CmdDeleteChar);
            AddEditHandler(Keys.Tab, CmdCodeCompletion);
            AddEditHandler(Keys.C | Keys.Control, CmdCopy);
            AddEditHandler(Keys.V | Keys.Control, CmdPaste);
            AddEditHandler(Keys.Left | Keys.Shift, CmdMarkLeft);
            AddEditHandler(Keys.Right | Keys.Shift, CmdMarkRight);
            AddEditHandler(Keys.Home | Keys.Shift, CmdMarkHome);
            AddEditHandler(Keys.End | Keys.Shift, CmdMarkEnd);
            AddEditHandler(RuntimeRepl.PseudoKeyForResizeEvent, CmdAbortEdit);
        }

        internal void InsertChar(char newch)
        {
            LineBuffer.Insert(LineIndex, newch);
            ++LineIndex;
        }

        internal void InsertString(string str)
        {
            if (str != null)
            {
                foreach (var ch in str)
                {
                    InsertChar(ch);
                }
            }
        }

        internal void Mark(Action movement)
        {
            SetMark(LineIndex);
            movement();
            SetMark(LineIndex);
        }

        internal void PaintLineBuffer()
        {
            var nextCursorPos = HomePos;
            CursorPos = HomePos;
            for (var i = 0; i < LineBuffer.Length; ++i)
            {
                Write(LineBuffer[i]);

                if (i < LineIndex)
                {
                    nextCursorPos = CursorPos;
                }

                if (LineMark == -1)
                {
                    Buffer.Mark = -1;
                }
                else if (i < LineMark)
                {
                    Buffer.Mark = CursorPos;
                }

                if (LineBound == -1)
                {
                    Buffer.Bound = -1;
                }
                else if (i < LineBound)
                {
                    Buffer.Bound = CursorPos;
                }

            }
            CharsWritten = CursorPos - HomePos;
            ClearToBot();
            CursorPos = nextCursorPos;
        }

        internal void SetMark(int index)
        {
            if (index == -1)
            {
                LineMark = LineBound = -1;
            }
            else if (LineMark == -1)
            {
                LineMark = LineBound = index;
            }
            else
            {
                LineBound = index;
            }
        }

        #endregion Methods
    }
}