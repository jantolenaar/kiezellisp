#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
	using System;
	using System.Drawing;
	using System.IO;
	using System.Text;

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
			ForeColor = fg;
			BackColor = bg;
			Data = new TextBufferItem[Size];
			ClearRect(0, 0, Width, Height);
		}

		#endregion Constructors

		#region Public Properties

		public Color BackColor { get; set; }

		public Color ForeColor { get; set; }

		public int Height { get; set; }

		public int Size { get; set; }

		public TextBufferItem this[int pos]
		{
			get
			{
				if (0 <= pos && pos < Size)
				{
					return Data[pos];
				}
				else {
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
				else {
					// Should never be visible
					return new TextBufferItem(' ', ForeColor, BackColor, 0);
				}
			}
			set
			{
				Data[col + row * Width] = value;
			}
		}

		public int Width { get; set; }

		#endregion Public Properties

		#region Internal Methods

		internal string GetString(int beg, int end, bool insertlf)
		{
			if (beg > end)
			{
				var tmp = beg;
				beg = end;
				end = tmp;
			}

			var buf = new StringWriter();
			for (var p = beg; p < end; ++p)
			{
				if (insertlf && p != beg && p % Width == 0)
				{
					buf.Write('\n');
				}
				buf.Write(this[p].Code);
			}
			return buf.ToString();
		}

		#endregion Internal Methods

		#region Public Methods

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

		public static void CopyArray(Array adst, int wdst, int xdst, int ydst, Array asrc, int wsrc, int xsrc, int ysrc, int w, int h)
		{
			if (adst == asrc && ysrc <= ydst && ydst < ysrc + h)
			{
				var odst = (ydst + h - 1) * wdst + xdst;
				var osrc = (ysrc + h - 1) * wsrc + xsrc;
				for (var i = 0; i < h; ++i)
				{
					Array.Copy(asrc, osrc, adst, odst, w);
					odst -= wdst;
					osrc -= wsrc;
				}
			}
			else {
				var odst = ydst * wdst + xdst;
				var osrc = ysrc * wsrc + xsrc;
				for (var i = 0; i < h; ++i)
				{
					Array.Copy(asrc, osrc, adst, odst, w);
					odst += wdst;
					osrc += wsrc;
				}
			}
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

			for (var r = 0; r < h; ++r)
			{
				for (var c = 0; c < w; ++c)
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

		public void Paste(int x, int y, TextBuffer src)
		{
			CopyRect(x, y, src, 0, 0, src.Width, src.Height);
		}

		public int Scroll(int lines)
		{
			lines = Math.Min(lines, Height);
			CopyRect(0, 0, this, 0, lines, Width, Height - lines);
			ClearRect(0, Height - lines, Width, lines);
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

		#endregion Public Methods
	}

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
}