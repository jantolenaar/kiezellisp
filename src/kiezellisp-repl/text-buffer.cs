#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    public struct TextBufferItem
    {
        #region Fields

        public Color Bg;
        public char Code;
        public Color Fg;
        public int FontIndex;

        #endregion Fields

        #region Constructors

        public TextBufferItem(char code, Color fg, Color bg, int fontIndex)
        {
            Code = code;
            Fg = fg;
            Bg = bg;
            FontIndex = fontIndex;
        }

        #endregion Constructors
    }

    public class TextBuffer
    {
        #region Fields

        internal TextBufferItem[] Data;

        #endregion Fields

        #region Constructors

        public TextBuffer(int width, int height, Color fg, Color bg)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Size = Width * Height;
            Mark = Bound = -1;
            ForeColor = fg;
            BackColor = bg;
            Data = new TextBufferItem[Size];
            ClearRect(0, 0, Width, Height);
        }

        #endregion Constructors

        #region Properties

        public Color BackColor { get; set; }

        public int Bound { get; set; }

        public Color ForeColor { get; set; }

        public int Height { get; set; }

        public int Mark { get; set; }

        public int Size { get; set; }

        public int Width { get; set; }

        #endregion Properties

        #region Indexers

        public TextBufferItem this[int pos]
        {
            get
            {
                if (0 <= pos && pos < Size)
                {
                    return Data[pos];
                }
                else
                {
                    return new TextBufferItem(' ', ForeColor, BackColor, 0);
                }
            }
            set
            {
                Data[pos] = value;
            }
        }

        public TextBufferItem this[int col, int row]
        {
            get
            {
                if (0 <= col && col < Width && 0 <= row && row < Height)
                {
                    return Data[col + row * Width];
                }
                else if (0 <= col && col < Width && row == Height)
                {
                    // Should never be visible
                    return new TextBufferItem('-', ForeColor, BackColor, 0);
                }
                else
                {
                    // Should never be visible
                    return new TextBufferItem(' ', ForeColor, BackColor, 0);
                }
            }
            set
            {
                Data[col + row * Width] = value;
            }
        }

        #endregion Indexers

        #region Methods

        public static void CopyArray(Array adst, int wdst, int xdst, int ydst, Array asrc, int wsrc, int xsrc, int ysrc, int w, int h)
        {
            if (adst == asrc && ysrc <= ydst && ydst < ysrc + h)
            {
                var odst = (ydst + h - 1) * wdst + xdst;
                var osrc = (ysrc + h - 1) * wsrc + xsrc;
                for (int i = 0; i < h; ++i)
                {
                    Array.Copy(asrc, osrc, adst, odst, w);
                    odst -= wdst;
                    osrc -= wsrc;
                }
            }
            else
            {
                var odst = ydst * wdst + xdst;
                var osrc = ysrc * wsrc + xsrc;
                for (int i = 0; i < h; ++i)
                {
                    Array.Copy(asrc, osrc, adst, odst, w);
                    odst += wdst;
                    osrc += wsrc;
                }
            }
        }

        public void ClearRect(int x, int y, int w, int h)
        {
            FillRect(x, y, w, h, ' ', ForeColor, BackColor);
        }

        public TextBuffer Copy(int x, int y, int w, int h)
        {
            var buf = new TextBuffer(w, h, ForeColor, BackColor);
            buf.CopyRect(0, 0, this, x, y, w, h);
            return buf;
        }

        public void CopyRect(int xdst, int ydst, TextBuffer bsrc, int x, int y, int w, int h)
        {
            w = Math.Min(Width - xdst, w);
            h = Math.Min(Height - ydst, h);
            CopyArray(Data, Width, xdst, ydst, bsrc.Data, bsrc.Width, x, y, w, h);
        }

        public void FillRect(int x, int y, int w, int h, char ch, Color fg, Color bg)
        {
            var pos = y * Width + x;

            for (int r = 0; r < h; ++r)
            {
                for (int c = 0; c < w; ++c)
                {
                    Data[pos + c].Code = ch;
                    Data[pos + c].Fg = fg;
                    Data[pos + c].Bg = bg;
                    Data[pos + c].FontIndex = 0;
                }
                pos += Width;
            }
        }

        public char Get(int col, int row)
        {
            var pos = row * Width + col;
            return Data[pos].Code;
        }

        public void Get(int col, int row, out char ch, out Color fg, out Color bg, out int fontIndex)
        {
            var pos = row * Width + col;
            ch = Data[pos].Code;
            fg = Data[pos].Fg;
            bg = Data[pos].Bg;
            fontIndex = Data[pos].FontIndex;
        }

        public void Paste(int x, int y, TextBuffer src)
        {
            CopyRect(x, y, src, 0, 0, src.Width, src.Height);
        }

        public int Scroll(int lines)
        {
            lines = Math.Min(lines, Height);
            CopyRect(0, 0, this, 0, lines, Width, Height - lines);
            ClearRect(0, Height - lines, Width, lines);
            if (Mark != -1)
            {
                Mark = Math.Max(0, Mark - lines * Width);
            }
            if (Bound != -1)
            {
                Bound = Math.Max(0, Bound - lines * Width);
            }
            return lines;
        }

        public void Set(int col, int row, char ch, Color fg, Color bg, int fontIndex)
        {
            var pos = row * Width + col;
            if (ch != (char)0)
            {
                Data[pos].Code = ch;
            }
            Data[pos].Fg = fg;
            Data[pos].Bg = bg;
            Data[pos].FontIndex = fontIndex;
        }

        internal string FindColorLine(int col, int end, int row, out Color fg, out Color bg, out int fontIndex)
        {
            var line = new StringBuilder();
            var begItem = this[col, row];
            var begSelected = Selected(col, row);
            if (begSelected)
            {
                fg = RuntimeRepl.DefaultHighlightForeColor;
                if (fg == Color.Empty)
                {
                    fg = begItem.Fg;
                }
                bg = RuntimeRepl.DefaultHighlightBackColor;
                if (bg == Color.Empty)
                {
                    bg = begItem.Bg;
                }
                fontIndex = begItem.FontIndex;
            }
            else
            {
                fg = begItem.Fg;
                bg = begItem.Bg;
                fontIndex = begItem.FontIndex;
            }

            for (var c = col; c < end; ++c)
            {
                var item = this[c, row];
                if (begItem.Fg != item.Fg)
                {
                    break;
                }
                if (begItem.FontIndex != item.FontIndex)
                {
                    break;
                }
                if (begSelected != Selected(c, row))
                {
                    break;
                }
                if (!(begSelected || begItem.Bg == item.Bg))
                {
                    break;
                }

                line.Append(item.Code);
            }
            return line.ToString();
        }

        internal string GetSelection(bool insertlf)
        {
            return GetString(Mark, Bound, insertlf);
        }

        internal string GetString(int beg, int end, bool insertlf)
        {
            if (beg > end)
            {
                var tmp = beg;
                beg = end;
                end = tmp;
            }

            var buf = new StringWriter();
            for (int p = beg; p < end; ++p)
            {
                if (insertlf && p != beg && p % Width == 0)
                {
                    buf.Write('\n');
                }
                buf.Write(this[p].Code);
            }
            return buf.ToString();
        }

        internal bool Selected(int col, int row)
        {
            if (Mark < Bound)
            {
                var p = col + Width * row;
                return (Mark <= p && p < Bound);
            }
            else if (Mark > Bound)
            {
                var p = col + Width * row;
                return (Bound <= p && p < Mark);
            }
            else
            {
                return false;
            }
        }

        internal void SetMark(int pos)
        {
            if (pos == -1)
            {
                Mark = Bound = -1;
            }
            else if (Mark == -1)
            {
                Mark = Bound = pos;
            }
            else
            {
                Bound = pos;
            }
        }

        #endregion Methods
    }
}