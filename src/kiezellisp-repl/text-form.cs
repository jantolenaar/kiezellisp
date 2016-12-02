#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Concurrent;
    using System.Drawing;
    using System.Windows.Forms;

    public class ReplTextForm : TextFormBase
    {
        #region Constructors

        public ReplTextForm(TextWindowCreateArgs args)
            : base(args)
        {
            Content.Window = new ReplTextWindow(Content, args);
            Content.InitScrollBars();
        }

        #endregion Constructors

        #region Protected Methods

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            RuntimeConsoleBase.Quit();
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
            var control = ParentForm.Content;
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
            var control = ParentForm.Content;
            control.Window.WindowTop = se.NewValue;
            control.Invalidate();
            control.Focus();
        }

        #endregion Protected Methods
    }

    public class TextForm : TextFormBase
    {
        #region Constructors

        public TextForm(TextWindowCreateArgs args)
            : base(args)
        {
            Content.Window = new TextWindow(Content, args);
            Content.InitScrollBars();
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
            else {
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
            else {
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
            else {
                FormBorderStyle = FormBorderStyle.FixedToolWindow;
                ShowInTaskbar = false;
            }

            Content = new TextControl(this);
            Content.Location = new Point(0, 0);
            Content.Size = new Size(CharWidth * Cols, LineHeight * Rows);
            ClientSize = new Size(Content.Width + ExtraWidth, Content.Height + ExtraHeight);
            Content.Anchor = AnchorStyles.Bottom | AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(Content);

            Visible = args.Visible;
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

        public IApply OnCloseFunction { get; internal set; }

        public ThreadContext OwningThread { get; internal set; }

        public int Rows { get; internal set; }

        public TextControl Content { get; internal set; }

        public TerminalVScrollBar VertScrollBar { get; internal set; }

        #endregion Public Properties

        #region Protected Methods

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (OnCloseFunction != null)
            {
                Runtime.Funcall(OnCloseFunction, Content.Window);
            }

            base.OnFormClosed(e);
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            Cols = Content.Width / CharWidth;
            Rows = Content.Height / LineHeight;
            HoriScrollBar.LargeChange = Cols;
            VertScrollBar.LargeChange = Rows;
            if (Content.Window != null)
            {
                Content.Window.OnResizeWindow();
            }
        }

        #endregion Protected Methods
    }
}