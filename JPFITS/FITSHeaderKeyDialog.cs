/*  
    JPFITS: object-oriented FITS file interaction
    Copyright (C) 2023  Joseph E. Postma

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

	joepostma@live.ca
*/

using System;
using System.Globalization;
using System.Windows.Forms;

namespace JPFITS
{
	public partial class FITSHeaderKeyDialog : Form
	{
		public FITSHeaderKeyDialog()
		{
			InitializeComponent();
			CommentKeyLineChck.Visible = true;
		}

		public FITSHeaderKeyDialog(FITSHeaderKey headerKey)
		{
			InitializeComponent();

			HEADERLINE = headerKey;

			if (HEADERLINE.IsCommentKey)
			{
				CommentKeyLineChck.Checked = true;
				CommentKeyLineTxt.Text = HEADERLINE.Comment;
			}
			else
			{
				KeyNameTxt.Text = HEADERLINE.Name;
				KeyValueTxt.Text = HEADERLINE.Value;
				KeyCommentTxt.Text = HEADERLINE.Comment;
			}
		}

		public FITSHeaderKey HeaderLine
		{
			get { return HEADERLINE; }
		}

		void OKBtn_Click(object sender, EventArgs e)
		{
			if (CommentKeyLineChck.Checked)
				HEADERLINE = new FITSHeaderKey(CommentKeyLineTxt.Text);
			else
				HEADERLINE = new FITSHeaderKey(KeyNameTxt.Text, KeyValueTxt.Text, KeyCommentTxt.Text);
		}

		void CommentKeyLineChck_CheckedChanged(object sender, EventArgs e)
		{
			if (CommentKeyLineChck.Checked)
			{
				CommentKeyLineTxt.Visible = true;
				CommentKeyLineTxt.BringToFront();
				label1.Text = "Comment Line";
				label2.Visible = false;
				label3.Visible = false;
			}
			else
			{
				CommentKeyLineTxt.Visible = false;
				CommentKeyLineTxt.SendToBack();
				label1.Text = "Key Name";
				label2.Visible = true;
				label3.Visible = true;
			}
		}

		void KeyNameTxt_TextChanged(object sender, EventArgs e)
		{
			if (KeyNameTxt.Text == "COMMENT")
			{
				CommentKeyLineChck.Checked = true;
				CommentKeyLineTxt.Text = "COMMENT";
				CommentKeyLineTxt.Focus();
				CommentKeyLineTxt.SelectionStart = CommentKeyLineTxt.Text.Length;
			}
			else
				CommentKeyLineChck.Checked = false;
		}

		private void KeyCommentTxt_TextChanged(object sender, EventArgs e)
		{
			label3.Text = String.Format(CultureInfo.GetCultureInfo("en-US").NumberFormat, "Key Comment ({0})", (48 - KeyCommentTxt.Text.Length).ToString());
		}

		private void CommentKeyLineTxt_TextChanged(object sender, EventArgs e)
		{
			label1.Text = string.Format(CultureInfo.GetCultureInfo("en-US").NumberFormat, "Comment Line ({0})", (80 - CommentKeyLineTxt.Text.Length).ToString());
		}
	}
}
