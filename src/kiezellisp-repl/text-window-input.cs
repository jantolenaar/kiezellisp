#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Windows.Forms;

    public partial class TextWindow
    {
        #region Fields

        internal bool cancelled;
        internal int charsWritten;
        internal bool done;
        internal Dictionary<Keys, object> editHandlers = new Dictionary<Keys, object>();
        internal int homePos;
        internal int lineBound;
        internal StringBuilder lineBuffer;
        internal int lineIndex;
        internal int lineMark;
        internal int maxChars;
        internal int savedPos;

        #endregion Fields

        #region Internal Methods

        internal void CmdAbortEdit()
        {
            CmdEscape();
        }

        internal void CmdBackspace()
        {
            if (lineIndex > 0)
            {
                --lineIndex;
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
            var searchTerm = Runtime.GetWordFromString(lineBuffer.ToString().Substring(0, lineIndex), lineIndex, Runtime.IsLispWordChar);
            var completions = RuntimeConsoleBase.GetCompletions(searchTerm);
            var index = 0;
            savedPos = CursorPos;

            while (true)
            {
                CursorPos = savedPos + charsWritten;
                WriteLine();
                for (var i = 0; i < completions.Count; ++i)
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
                    CursorPos = savedPos + charsWritten;
                    ClearToBot();
                    CursorPos = savedPos;
                    var newPos = lineIndex - searchTerm.Length;
                    while (lineIndex != newPos)
                    {
                        CmdBackspace();
                    }
                    InsertString(completions[index] + " ");
                    break;
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
                    CursorPos = savedPos + charsWritten;
                    ClearToBot();
                    CursorPos = savedPos;
                    break;
                }
            }
        }

        internal void CmdCopy()
        {
            string text = "";

            if (lineMark == -1)
            {
                text = lineBuffer.ToString();
            }
            else if (lineMark <= lineBound)
            {
                text = lineBuffer.ToString().Substring(lineMark, lineBound - lineMark);
            }
            else if (lineBound < lineMark)
            {
                text = lineBuffer.ToString().Substring(lineBound, lineMark - lineBound);
            }
            Runtime.SetClipboardData(text);
        }

        internal void CmdDataChar(char ch)
        {
            // Inserts a character at Pos and increments EndPos.
            if (maxChars == -1 || lineBuffer.Length < maxChars)
            {
                InsertChar(ch);
            }
        }

        internal void CmdDeleteChar()
        {
            if (lineIndex < lineBuffer.Length)
            {
                lineBuffer.Remove(lineIndex, 1);
            }
        }

        internal void CmdEnd()
        {
            lineIndex = lineBuffer.Length;
        }

        internal void CmdEnter()
        {
            CmdEnd();
            done = true;
        }

        internal void CmdEscape()
        {
            lineBuffer.Clear();
            lineIndex = 0;
            cancelled = true;
        }

        internal void CmdHome()
        {
            lineIndex = 0;
        }

        internal void CmdLeft()
        {
            lineIndex = Math.Max(0, lineIndex - 1);
        }

        internal void CmdMarkEnd()
        {
            SetMarkAction(CmdEnd);
        }

        internal void CmdMarkHome()
        {
            SetMarkAction(CmdHome);
        }

        internal void CmdMarkLeft()
        {
            SetMarkAction(CmdLeft);
        }

        internal void CmdMarkRight()
        {
            SetMarkAction(CmdRight);
        }

        internal void CmdMoveBackOverSpaces()
        {
            if (lineIndex == lineBuffer.Length || char.IsWhiteSpace(lineBuffer[lineIndex]))
            {
                while (lineIndex > 0 && char.IsWhiteSpace(lineBuffer[lineIndex - 1]))
                {
                    --lineIndex;
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
            lineIndex = Math.Min(lineIndex + 1, lineBuffer.Length);
        }

        internal string GetStringInput(string initialText, int maxLength)
        {
            maxChars = maxLength;
            homePos = CursorPos;
            done = false;
            cancelled = false;
            lineBuffer = new StringBuilder();
            lineIndex = 0;

            InsertString(initialText);

            while (!done && !cancelled)
            {
                PaintLineBuffer();
                ShowCursor();
                var info = ReadKey(false);
                HideCursor();

                Runtime.InitRandom();

                var handled = false;

                if (info.KeyData != 0)
                {
                    if (info.KeyData != (Keys.C | Keys.Control) && (info.KeyData & Keys.Shift) == 0)
                    {
                        if (lineMark != -1)
                        {
                            SetMark(-1);
                        }
                    }

                    object handler;
                    if (editHandlers.TryGetValue(info.KeyData, out handler))
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
                    if (lineMark != -1)
                    {
                        SetMark(-1);
                    }
                    CmdDataChar(info.KeyChar);
                }
            }

            SetMark(-1);
            PaintLineBuffer();

            var result = cancelled ? null : lineBuffer.ToString();

            lineBuffer = null;

            return result;
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
            lineBuffer.Insert(lineIndex, newch);
            ++lineIndex;
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

        internal void PaintLineBuffer()
        {
            var nextCursorPos = homePos;
            CursorPos = homePos;

            if (lineMark != -1)
            {
                //defaults
                BufferMark = BufferBound = homePos;
            }
            else
            {
                BufferMark = BufferBound = -1;
            }

            var swap = lineBound < lineMark;
            var m = swap ? lineBound : lineMark;
            var b = swap ? lineMark : lineBound;

            for (var i = 0; i < lineBuffer.Length; ++i)
            {
                if (i == m)
                {
                    BufferMark = CursorPos;
                }

                Write(lineBuffer[i]);

                if (i + 1 == b)
                {
                    BufferBound = CursorPos;
                }

                if (i < lineIndex)
                {
                    nextCursorPos = CursorPos;
                }

            }

            if (swap)
            {
                var temp = BufferMark;
                BufferMark = BufferBound;
                BufferBound = temp;
            }

            charsWritten = CursorPos - homePos;
            ClearToBot();
            CursorPos = nextCursorPos;
        }

        internal void SetMark(int index)
        {
            if (index == -1)
            {
                lineMark = lineBound = -1;
            }
            else if (lineMark == -1)
            {
                lineMark = lineBound = index;
            }
            else {
                lineBound = index;
            }
        }

        internal void SetMarkAction(Action movement)
        {
            SetMark(lineIndex);
            movement();
            SetMark(lineIndex);
        }

        #endregion Internal Methods

        #region Public Methods

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
            if (dirty)
            {
                Refresh();
            }

            while (true)
            {
                var info = ParentControl.ReadKey();
                if (info == null)
                {
                    continue;
                }

                object handler;
                if (scrollHandlers.TryGetValue(info.KeyData, out handler))
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
                    ScrollIntoView(false);
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

        #endregion Public Methods
    }
}