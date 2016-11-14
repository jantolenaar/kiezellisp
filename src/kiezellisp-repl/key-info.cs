#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Kiezel
{
	public class KeyInfo
	{
		#region Constructors

		public KeyInfo(Keys data)
		{
			KeyData = (Keys)data;
			KeyChar = (char)0;
		}

		public KeyInfo(Keys data, int col, int row, int clicks, int wheel)
		{
			KeyData = data;
			KeyChar = (char)0;
			MouseCol = col;
			MouseRow = row;
			MouseClicks = clicks;
			MouseWheel = wheel;
		}

		public KeyInfo(char data)
		{
			KeyData = 0;
			KeyChar = data;
		}

		public KeyInfo()
		{
		}

		#endregion Constructors

		#region Public Properties

		public char KeyChar { get; set; }

		public Keys KeyData { get; set; }

		public int MouseClicks { get; set; }

		public int MouseCol { get; set; }

		public int MouseRow { get; set; }

		public int MouseWheel { get; set; }

		#endregion Public Properties
	}

}
