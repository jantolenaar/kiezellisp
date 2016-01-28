
// Copyright (C) Jan Tolenaar. See the file LICENSE for details.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.IO;

namespace Kiezel
{
    public class Buffer
    {
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int Size { get; internal set; }
        internal char[] Data;
        internal ColorType[] Fg;
        internal ColorType[] Bg;
        internal int[] Attr;

        public Buffer(int width,int height)
        {
            Initialize(width, height);
        }

        void Initialize(int width, int height)
        {
            Width = width;
            Height = height;
            Size = width * height;
            Data = new char[Size];
            Fg = new ColorType[Size];
            Bg = new ColorType[Size];
            Attr = new int[Size];
            ClearRect(0, 0, Width, Height, Terminal.DefaultForeColor, Terminal.DefaultBackColor);
        }


        public static void CopyArray(Array adst, int wdst, int xdst, int ydst, Array asrc, int wsrc, int xsrc, int ysrc, int w, int h)
        {
            if (adst == asrc && ysrc <= ydst && ydst < ysrc + h)
            {
                var odst = (ydst + h - 1 ) * wdst + xdst;
                var osrc = (ysrc + h - 1) * wsrc + xsrc;
                for (int i=0; i< h; ++i)
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
                for (int i=0; i< h; ++i)
                {
                    Array.Copy(asrc, osrc, adst, odst, w);
                    odst += wdst;
                    osrc += wsrc;
                }
            }
        }

        public static void CopyRect(Buffer bdst, int xdst, int ydst, Buffer bsrc, int xsrc, int ysrc, int w, int h)
        {
            if (bdst.Width < xdst + w)
            {
                w = bdst.Width - xdst;
            }
            if (bdst.Height < ydst + h)
            {
                h = bdst.Height - ydst;
            }

            CopyArray(bdst.Data, bdst.Width, xdst, ydst, bsrc.Data, bsrc.Width, xsrc, ysrc, w, h);
            CopyArray(bdst.Fg, bdst.Width, xdst, ydst, bsrc.Fg, bsrc.Width, xsrc, ysrc, w, h);
            CopyArray(bdst.Bg, bdst.Width, xdst, ydst, bsrc.Bg, bsrc.Width, xsrc, ysrc, w, h);
            CopyArray(bdst.Attr, bdst.Width, xdst, ydst, bsrc.Attr, bsrc.Width, xsrc, ysrc, w, h);
        }

        public void ClearRect(int x, int y, int w, int h, ColorType foreColor,ColorType backColor)
        {
            var pos = y * Width + x;

            for (int r=0; r<h; ++r)
            {
                for (int c=0; c<w; ++c)
                {
                    Data[pos + c] = ' ';
                    Fg[pos + c] = foreColor;
                    Bg[pos + c] = backColor;
                    Attr[pos + c] = 0;
                }
                pos += Width;
            }
        }

        public void Set(int col, int row, char ch, ColorType fg, ColorType bg, int style)
        {
            var pos = row * Width + col;
            if (ch != (char)0)
            {
                Data[pos] = ch;
            }
            Fg[pos] = fg;
            Bg[pos] = bg;
            Attr[pos] = style;
        }

        public char Get(int col, int row)
        {
            var pos = row * Width + col;
            return Data[pos];
        }

        public void Get(int col, int row, out char ch, out ColorType fg, out ColorType bg, out int attr)
        {
            var pos = row * Width + col;
            ch = Data[pos];
            fg = Fg[pos];
            bg = Bg[pos];
            attr = Attr[pos];
        }

    }



 
}

