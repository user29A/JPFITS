using System;
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
			label3.Text = String.Format("Key Comment ({0})", (48 - KeyCommentTxt.Text.Length).ToString());
		}

		private void CommentKeyLineTxt_TextChanged(object sender, EventArgs e)
		{
			label1.Text = string.Format("Comment Line ({0})", (80 - CommentKeyLineTxt.Text.Length).ToString());
		}
	}
}
