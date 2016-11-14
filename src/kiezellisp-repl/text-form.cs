#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Kiezel
{
    public class ReplTextForm : TextFormBase
    {
        #region Constructors

        public ReplTextForm(TextWindowCreateArgs args)
            : base(args)
        {
            TermControl.Window = new ReplTextWindow(TermControl, args);
            TermControl.InitScrollBars();
        }

        #endregion Constructors

        #region Protected Methods

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            RuntimeRepl.Quit();
        }

        #endregion Protected Methods
    }

    public class TerminalHScrollBar : HScrollBar
    {
        #region Constructors

        public TerminalHScrollBar(TextFormBase parent)
        {
            ParentForm = parent;
            Dock = DockStyle.Bottom;
        }

        #endregion Constructors

        #region Public Properties

        public TextFormBase ParentForm { get; internal set; }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            var control = ParentForm.TermControl;
            control.Window.WindowLeft = se.NewValue;
            control.Invalidate();
            control.Focus();
        }

        #endregion Protected Methods
    }

    public class TerminalVScrollBar : VScrollBar
    {
        #region Constructors

        public TerminalVScrollBar(TextFormBase parent)
        {
            ParentForm = parent;
            Dock = DockStyle.Right;
        }

        #endregion Constructors

        #region Public Properties

        public TextFormBase ParentForm { get; internal set; }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            var control = ParentForm.TermControl;
            control.Window.WindowTop = se.NewValue;
            control.Invalidate();
            control.Focus();
        }

        #endregion Protected Methods
    }

    public class TextControl : Control
    {
        #region Fields

        BlockingCollection<KeyInfo> keyBuffer;

        #endregion Fields

        #region Constructors

        public TextControl(TextFormBase parent)
        {
            ParentForm = parent;
            Dock = DockStyle.Fill;
            Text = "Kiezellisp";
            BackColor = RuntimeRepl.DefaultBackColor;
            DoubleBuffered = true;
            keyBuffer = new BlockingCollection<KeyInfo>();
        }

        #endregion Constructors

        #region Public Properties

        public int CharHeight
        {
            get { return RuntimeRepl.CharHeight; }
        }

        public int CharWidth
        {
            get { return RuntimeRepl.CharWidth; }
        }

        public int Cols
        {
            get { return ParentForm.Cols; }
        }

        public Font[] Fonts
        {
            get { return RuntimeRepl.Fonts; }
        }

        public TerminalHScrollBar HoriScrollBar
        {
            get { return ParentForm.HoriScrollBar; }
        }

        public int LineHeight
        {
            get { return RuntimeRepl.LineHeight; }
        }

        public TextFormBase ParentForm { get; internal set; }

        public int Rows
        {
            get { return ParentForm.Rows; }
        }

        public TerminalVScrollBar VertScrollBar
        {
            get { return ParentForm.VertScrollBar; }
        }

        public TextWindow Window { get; internal set; }

        public int WindowLeftPixels
        {
            get
            {
                return Window.WindowLeft * CharWidth;
            }
        }

        public int WindowTopPixels
        {
            get
            {
                return Window.WindowTop * LineHeight;
            }
        }

        #endregion Public Properties

        #region Private Methods

        void PaintArea(Graphics g, int x, int y, int w, int h)
        {
            if (Window == null)
            {
                return;
            }

            var g2 = g;
            for (var i = 0; i < h; ++i)
            {
                var beg = x;
                var end = x + w;

                while (beg < end)
                {
                    var top = y + i;
                    var left = beg;
                    Color fg;
                    Color bg;
                    int fontIndex;
                    var text = Window.Buffer.FindColorLine(left, end, top, out fg, out bg, out fontIndex);
                    PaintString(g2, text, left, top, text.Length, fg, bg, fontIndex);
                    beg += text.Length;
                }
            }
        }

        void PaintString(Graphics g, string text, int x, int y, int w, Color fg, Color bg, int fontIndex)
        {
            var xx = x * CharWidth - WindowLeftPixels;
            var yy = y * LineHeight - WindowTopPixels;
            var ww = w * CharWidth;
            var hh = 1 * LineHeight;
            var bounds3 = new Rectangle(xx, yy, ww, hh);
            g.FillRectangle(new SolidBrush(bg), xx, yy, ww, hh);
            var flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping | TextFormatFlags.Left | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, text, Fonts[fontIndex], bounds3, fg, bg, flags);
        }

        #endregion Private Methods

        #region Protected Methods

        protected override bool IsInputKey(Keys keyData)
        {
            return true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyData != (Keys.Control | Keys.ControlKey))
            {
                keyBuffer.Add(new KeyInfo(e.KeyData));
                //keyBuffer.Enqueue(new KeyInfo(e.KeyData));
            }
            e.Handled = true;
            base.OnKeyDown(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            keyBuffer.Add(new KeyInfo(e.KeyChar));
            //keyBuffer.Enqueue(new KeyInfo(e.KeyChar));
            e.Handled = true;
            base.OnKeyPress(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            var x = (e.Location.X - WindowLeftPixels) / CharWidth;
            var y = (e.Location.Y - WindowTopPixels) / LineHeight;
            var c = e.Clicks;
            switch (e.Button)
            {
                case MouseButtons.Left:
                {
                    keyBuffer.Add(new KeyInfo(Keys.LButton, x, y, c, 0));
                    break;
                }
                case MouseButtons.Right:
                {
                    keyBuffer.Add(new KeyInfo(Keys.RButton, x, y, c, 0));
                    break;
                }
            }
            base.OnMouseClick(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            var x = (e.Location.X - WindowLeftPixels) / CharWidth;
            var y = (e.Location.Y - WindowTopPixels) / LineHeight;
            var c = e.Clicks;
            switch (e.Button)
            {
                case MouseButtons.Left:
                {
                    keyBuffer.Add(new KeyInfo(Keys.LButton, x, y, c, 0));
                    break;
                }
                case MouseButtons.Right:
                {
                    keyBuffer.Add(new KeyInfo(Keys.RButton, x, y, c, 0));
                    break;
                }
            }
            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var x = (e.Location.X - WindowLeftPixels) / CharWidth;
            var y = (e.Location.Y - WindowTopPixels) / LineHeight;
            var w = e.Delta;
            keyBuffer.Add(new KeyInfo(RuntimeRepl.PseudoKeyForMouseWheel, x, y, 0, w));
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            base.OnPaint(e);
            var rect = e.ClipRectangle;
            rect.Offset(WindowLeftPixels, WindowTopPixels);
            var x1 = rect.Left / CharWidth;
            var y1 = rect.Top / LineHeight;
            var x2 = (rect.Left + rect.Width + CharWidth - 1) / CharWidth;
            var y2 = (rect.Top + rect.Height + LineHeight - 1) / LineHeight;
            PaintArea(g, x1, y1, x2 - x1 + 1, y2 - y1 + 1);
        }

        #endregion Protected Methods

        #region Public Methods

        public void ClearKeyboardBuffer()
        {
            //keyBuffer.Clear();
        }

        public void GuiBringToFront()
        {
            RuntimeRepl.GuiInvoke(new Action(() =>
            {
                ParentForm.BringToFront();
            }));
        }

        public void GuiInvalidate(int x, int y, int w, int h)
        {
            if (w == 1)
            {
                // cursor update needs more bits to avoid artefacts
                if (x > 0)
                {
                    --x;
                    ++w;
                }
                if (x + w < Cols)
                {
                    ++w;
                }
            }

            RuntimeRepl.GuiInvoke(new Action(() =>
            {
                var screen = new Rectangle(0, 0, Cols, Rows);
                var dirty = new Rectangle(x, y, w, h);
                dirty.Intersect(screen);
                var bounds = new Rectangle(dirty.Left * CharWidth, dirty.Top * LineHeight, dirty.Width * CharWidth, dirty.Height * LineHeight);
                Invalidate(bounds);
            }));
        }

        public void GuiInvalidate()
        {
            RuntimeRepl.GuiInvoke(new Action(() =>
            {
                Invalidate();
            }));
        }

        public void GuiUpdateHoriScrollBarPos()
        {
            RuntimeRepl.GuiInvoke(new Action(() =>
            {
                if (HoriScrollBar != null)
                {
                    HoriScrollBar.Value = 0;
                    HoriScrollBar.Maximum = Window.BufferWidth - 1;
                    HoriScrollBar.Value = Window.WindowLeft;
                }
            }));
        }

        public void GuiUpdateVertScrollBarPos()
        {
            RuntimeRepl.GuiInvoke(new Action(() =>
            {
                if (VertScrollBar != null)
                {
                    VertScrollBar.Value = 0;
                    VertScrollBar.Maximum = Window.BufferHeight - 1;
                    VertScrollBar.Value = Window.WindowTop;
                }
            }));
        }

        public void InitScrollBars()
        {
            Window.WindowLeft = 0;
            Window.WindowTop = 0;

            if (HoriScrollBar != null)
            {
                HoriScrollBar.Minimum = 0;
                HoriScrollBar.Maximum = Window.BufferWidth - 1;
                HoriScrollBar.Value = 0;
                HoriScrollBar.LargeChange = Cols;
                HoriScrollBar.SmallChange = 1;
            }

            if (VertScrollBar != null)
            {
                VertScrollBar.Minimum = 0;
                VertScrollBar.Maximum = Window.BufferHeight - 1;
                VertScrollBar.Value = 0;
                VertScrollBar.LargeChange = Rows;
                VertScrollBar.SmallChange = 1;
            }
        }

        public void SendKey(KeyInfo key)
        {
            keyBuffer.Add(key);
        }

        public void SendInterruptKey()
        {
            SendKey(new KeyInfo(RuntimeRepl.InterruptKey));
        }

        public KeyInfo ReadKey()
        {
            KeyInfo info = keyBuffer.Take();
            if (info.KeyData == RuntimeRepl.InterruptKey)
            {
                throw new InterruptException();
            }
            return info;
        }

        #endregion Public Methods
    }

    public class TextForm : TextFormBase
    {
        #region Constructors

        public TextForm(TextWindowCreateArgs args)
            : base(args)
        {
            TermControl.Window = new TextWindow(TermControl, args);
            TermControl.InitScrollBars();
        }

        #endregion Constructors
    }

    public class TextFormBase : Form
    {
        #region Constructors

        public TextFormBase(TextWindowCreateArgs args)
        {
            Text = args.Caption;
            Cols = args.Width;
            Rows = args.Height;
            BackColor = args.BackColor;
            OnCloseFunction = args.OnCloseFunction;

            if (args.Left == -1 || args.Top == -1)
            {
                StartPosition = FormStartPosition.CenterScreen;
            }
            else
            {
                SetDesktopLocation(args.Left * CharWidth, args.Top * LineHeight);
                StartPosition = FormStartPosition.Manual;
            }

            if (args.Scrollable)
            {
                VertScrollBar = new TerminalVScrollBar(this);
                Controls.Add(VertScrollBar);
                HoriScrollBar = new TerminalHScrollBar(this);
                Controls.Add(HoriScrollBar);
            }
            else
            {
                VertScrollBar = null;
                HoriScrollBar = null;
            }

            if (args.Resizable)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
            }
            else if (args.Border)
            {
                FormBorderStyle = FormBorderStyle.FixedSingle;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.FixedToolWindow;
                ShowInTaskbar = false;
            }
            ClientSize = new Size(CharWidth * Cols + ExtraWidth, LineHeight * Rows + ExtraHeight);
            TermControl = new TextControl(this);
            Controls.Add(TermControl);

            Visible = args.Visible;
        }

        #endregion Constructors

        #region Public Properties

        public ThreadContext OwningThread { get; internal set; }

        public int CharHeight
        {
            get { return RuntimeRepl.CharHeight; }
        }

        public int CharWidth
        {
            get { return RuntimeRepl.CharWidth; }
        }

        public IApply OnCloseFunction { get; internal set; }

        public int Cols { get; internal set; }

        public int ExtraHeight
        {
            get { return HoriScrollBar == null ? 0 : HoriScrollBar.Height; }
        }

        public int ExtraWidth
        {
            get { return VertScrollBar == null ? 0 : VertScrollBar.Width; }
        }

        public Font[] Fonts
        {
            get { return RuntimeRepl.Fonts; }
        }

        public TerminalHScrollBar HoriScrollBar { get; internal set; }

        public int LineHeight
        {
            get { return RuntimeRepl.LineHeight; }
        }

        public int Rows { get; internal set; }

        public TextControl TermControl { get; internal set; }

        public TerminalVScrollBar VertScrollBar { get; internal set; }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (OnCloseFunction != null)
            {
                Runtime.Funcall(OnCloseFunction, TermControl.Window);
            }

            base.OnFormClosed(e);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            Cols = (ClientSize.Width - ExtraWidth) / CharWidth;
            Rows = (ClientSize.Height - ExtraHeight) / LineHeight;
            HoriScrollBar.LargeChange = Cols;
            VertScrollBar.LargeChange = Rows;
            if (TermControl != null && TermControl.Window != null)
            {
                TermControl.Window.OnResizeWindow();
            }
        }

        #endregion Protected Methods
    }
}
