using System;
using System.Windows.Forms;
#nullable enable

namespace JPFITS
{
	public partial class FITSHeaderViewer : Form
	{
		private FITSHeader HEADER;
		private FITSImageSet IMAGESET;
		private int IMAGESETHEADERINDEX = -1;

		public FITSHeaderViewer(FITSHeader header)
		{
			InitializeComponent();

			IMAGESET = new FITSImageSet();
			IMAGESET.Add(new FITSImage("c:\\dum.fits", true));
			IMAGESET[0].Header = header;
			IMAGESETHEADERINDEX = 0;

			HEADER = header;

			HeaderKeysListBox.SuspendLayout();
			HeaderKeysListBox.Items.Clear();
			HeaderKeysListBox.Items.AddRange(HEADER.GetFormattedHeaderBlock(FITSHeader.HeaderUnitType.Primary, true));
			HeaderKeysListBox.ResumeLayout();
		}

		public FITSHeaderViewer(FITSImageSet imageSet, int imageSetIndex)
		{
			InitializeComponent();

			IMAGESET = imageSet;
			IMAGESETHEADERINDEX = imageSetIndex;
			HEADER = IMAGESET[IMAGESETHEADERINDEX].Header;
			this.Text = "Header " + (IMAGESETHEADERINDEX + 1) + " of " + IMAGESET.Count + ": " + IMAGESET[IMAGESETHEADERINDEX].FileName;

			HeaderKeysListBox.SuspendLayout();
			HeaderKeysListBox.Items.Clear();
			HeaderKeysListBox.Items.AddRange(HEADER.GetFormattedHeaderBlock(FITSHeader.HeaderUnitType.Primary, true));
			HeaderKeysListBox.ResumeLayout();
		}

		public FITSHeader Header
		{
			get 
			{ 
				return HEADER; 
			}
			set
			{
				HEADER = value;

				HeaderKeysListBox.SuspendLayout();
				HeaderKeysListBox.Items.Clear();
				HeaderKeysListBox.Items.AddRange(HEADER.GetFormattedHeaderBlock(FITSHeader.HeaderUnitType.Primary, true));
				HeaderKeysListBox.ResumeLayout();
			}
		}

		public void UpdateImageSetHeaderIndex(int index)
		{
			if (index == IMAGESETHEADERINDEX)
				return;

			IMAGESETHEADERINDEX = index;
			this.Header = IMAGESET[IMAGESETHEADERINDEX].Header;
			this.Text = "Header " + (IMAGESETHEADERINDEX + 1) + " of " + IMAGESET.Count + ": " + IMAGESET[IMAGESETHEADERINDEX].FileName;
		}

		private void FileSaveCloseMenuItem_Click(System.Object sender, System.EventArgs e)
		{
			OKbtn.PerformClick();
		}

		private void discardCloseToolStripMenuItem_Click(System.Object sender, System.EventArgs e)
		{
			CancelBtn.PerformClick();
		}

		private void HeaderContextMenu_Opening(System.Object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (HeaderKeysListBox.SelectedItems.Count == 0)
			{
				HeaderContextEditKey.Enabled = false;
				HeaderContextRemoveKeys.Enabled = false;
				HeaderContextCopyValuesList.Enabled = false;
			}
			else
			{
				HeaderContextEditKey.Enabled = true;
				HeaderContextRemoveKeys.Enabled = true;
				HeaderContextCopyValuesList.Enabled = true;
			}

			if (IMAGESET.Count > 1)
			{
				HeaderContextCopyValuesList.Visible = true;
				HeaderContextApplyAll.Visible = true;
				HeaderContextInsertKeys.Visible = true;
			}
			else
			{
				HeaderContextCopyValuesList.Visible = false;
				HeaderContextApplyAll.Visible = false;
				HeaderContextInsertKeys.Visible = false;
			}
		}

		private void HeaderContextEditKey_Click(System.Object sender, System.EventArgs e)
		{
			if (!FITSHeader.ValidKeyEdit(HEADER[HeaderKeysListBox.SelectedIndex].Name, true))
				return;

			FITSHeaderKeyDialog hkd = new JPFITS.FITSHeaderKeyDialog(HEADER[HeaderKeysListBox.SelectedIndex]);
			if (hkd.ShowDialog() == DialogResult.Cancel)
				return;

			String origkeyline = HEADER[HeaderKeysListBox.SelectedIndex].GetFullyFomattedFITSLine();
			String origkeyname = HEADER[HeaderKeysListBox.SelectedIndex].Name;
			bool iscomment = HEADER[HeaderKeysListBox.SelectedIndex].IsCommentKey;
			int ind;

			if (HeaderContextApplyAll.Checked)
				for (int i = 0; i < IMAGESET.Count; i++)
				{
					if (iscomment)
						ind = IMAGESET[i].Header.GetKeyIndex(origkeyline, true);
					else
						ind = IMAGESET[i].Header.GetKeyIndex(origkeyname, false);

					if (ind != -1)
						IMAGESET[i].Header[ind] = hkd.HeaderLine;
				}
			else
				IMAGESET[IMAGESETHEADERINDEX].Header[HeaderKeysListBox.SelectedIndex] = hkd.HeaderLine;

			this.Header = IMAGESET[IMAGESETHEADERINDEX].Header;
		}

		private void HeaderContextRemoveKeys_Click(System.Object sender, System.EventArgs e)
		{
			for (int i = 0; i < HeaderKeysListBox.SelectedIndices.Count; i++)
				if (!FITSHeader.ValidKeyEdit(HEADER[(int)HeaderKeysListBox.SelectedIndices[i]].Name, false))
				{
					MessageBox.Show("At least one of the keys ('" + HEADER[(int)HeaderKeysListBox.SelectedIndices[i]].Name + "') is restricted. Try again.", "Warning...:");
					return;
				}

			int ind;
			string[] origkeylines = null;
			string[] origkeynames = null;
			bool[] iscomment = null;
			if (HeaderContextApplyAll.Checked)
			{
				origkeylines = new string[HeaderKeysListBox.SelectedIndices.Count];
				origkeynames = new string[HeaderKeysListBox.SelectedIndices.Count];
				iscomment = new bool[HeaderKeysListBox.SelectedIndices.Count];
				for (int i = 0; i < HeaderKeysListBox.SelectedIndices.Count; i++)
				{
					origkeylines[i] = HEADER[(int)HeaderKeysListBox.SelectedIndices[i]].GetFullyFomattedFITSLine();
					origkeynames[i] = HEADER[(int)HeaderKeysListBox.SelectedIndices[i]].Name;
					iscomment[i] = HEADER[(int)HeaderKeysListBox.SelectedIndices[i]].IsCommentKey;
				}
			}

			for (int i = HeaderKeysListBox.SelectedIndices.Count - 1; i >= 0; i--)
				if (!HeaderContextApplyAll.Checked)
					IMAGESET[IMAGESETHEADERINDEX].Header.RemoveKey((int)HeaderKeysListBox.SelectedIndices[i]);
				else
					for (int j = 0; j < IMAGESET.Count; j++)
					{
						if (iscomment[i])
							ind = IMAGESET[j].Header.GetKeyIndex(origkeylines[i], true);
						else
							ind = IMAGESET[j].Header.GetKeyIndex(origkeynames[i], false);

						if (ind != -1)
							IMAGESET[j].Header.RemoveKey(ind);
					}

			this.Header = IMAGESET[IMAGESETHEADERINDEX].Header;
		}

		private void HeaderContextAddKey_Click(System.Object sender, System.EventArgs e)
		{
			JPFITS.FITSHeaderKeyDialog hkd = new JPFITS.FITSHeaderKeyDialog();
			if (hkd.ShowDialog() == DialogResult.Cancel)
				return;

			if (HeaderContextApplyAll.Checked)
			{
				for (int i = 0; i < IMAGESET.Count; i++)
					if (hkd.HeaderLine.IsCommentKey)
						IMAGESET[i].Header.AddCommentKeyLine(hkd.HeaderLine.Comment, -1);
					else
						IMAGESET[i].Header.AddKey(hkd.HeaderLine.Name, hkd.HeaderLine.Value, hkd.HeaderLine.Comment, -1);
			}
			else
				if (hkd.HeaderLine.IsCommentKey)
					IMAGESET[IMAGESETHEADERINDEX].Header.AddCommentKeyLine(hkd.HeaderLine.Comment, -1);
				else
					IMAGESET[IMAGESETHEADERINDEX].Header.AddKey(hkd.HeaderLine.Name, hkd.HeaderLine.Value, hkd.HeaderLine.Comment, -1);

			this.Header = IMAGESET[IMAGESETHEADERINDEX].Header;
		}

		private void HeaderContextCopyValuesList_DoubleClick(System.Object sender, System.EventArgs e)
		{
			String copy = "";
			if (CopyIncludeKeyNameHeaderChck.Checked)
			{
				for (int j = 0; j < HeaderKeysListBox.SelectedIndices.Count; j++)
					if (!HEADER[(int)HeaderKeysListBox.SelectedIndices[j]].IsCommentKey)
						copy += HEADER[(int)HeaderKeysListBox.SelectedIndices[j]].Name + "\t";
				copy += Environment.NewLine;
			}

			for (int i = 0; i < IMAGESET.Count; i++)
			{
				for (int j = 0; j < HeaderKeysListBox.SelectedIndices.Count; j++)
					if (!HEADER[(int)HeaderKeysListBox.SelectedIndices[j]].IsCommentKey)
						copy += IMAGESET[i].Header.GetKeyValue(HEADER[(int)HeaderKeysListBox.SelectedIndices[j]].Name) + "\t";
				copy += Environment.NewLine;
			}

			Clipboard.SetText(copy);
		}

		private void HeaderContextInsertKeys_DoubleClick(System.Object sender, System.EventArgs e)
		{
			for (int j = 0; j < IMAGESET.Count; j++)
				if (j == IMAGESETHEADERINDEX)
					continue;
				else
					for (int i = 0; i < HeaderKeysListBox.SelectedIndices.Count; i++)
						if (InsertOverwriteChck.Checked)
						{
							if (HEADER[(int)HeaderKeysListBox.SelectedIndices[i]].IsCommentKey)
							{
								if (IMAGESET[j].Header.GetKeyIndex(HEADER[(int)HeaderKeysListBox.SelectedIndices[i]].Name, true) == -1)
									IMAGESET[j].Header.AddKey(HEADER[(int)HeaderKeysListBox.SelectedIndices[i]], (int)HeaderKeysListBox.SelectedIndices[i]);
							}
							else
								IMAGESET[j].Header.SetKey(HEADER[(int)HeaderKeysListBox.SelectedIndices[i]].Name, HEADER[(int)HeaderKeysListBox.SelectedIndices[i]].Value, HEADER[(int)HeaderKeysListBox.SelectedIndices[i]].Comment, true, (int)HeaderKeysListBox.SelectedIndices[i]);
						}
						else
							if (IMAGESET[j].Header.GetKeyIndex(HEADER[(int)HeaderKeysListBox.SelectedIndices[i]].Name, false) == -1)
							IMAGESET[j].Header.AddKey(HEADER[(int)HeaderKeysListBox.SelectedIndices[i]], (int)HeaderKeysListBox.SelectedIndices[i]);
		}

		private void InsertOverwriteChck_Click(System.Object sender, System.EventArgs e)
		{
			HeaderContextMenu.Show();
			HeaderContextInsertKeys.ShowDropDown();
		}

		private void CopyIncludeKeyNameHeaderChck_Click(System.Object sender, System.EventArgs e)
		{
			HeaderContextMenu.Show();
			HeaderContextCopyValuesList.ShowDropDown();
		}

		private void HeaderContextApplyAll_Click(System.Object sender, System.EventArgs e)
		{
			HeaderContextMenu.Show();
		}

		private void HeaderKeysListBox_MouseDoubleClick(System.Object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (HeaderKeysListBox.SelectedIndex != -1)
				HeaderContextEditKey.PerformClick();
		}

		private void HeaderKeysListBox_KeyDown(System.Object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Delete)
			{
				if (HeaderKeysListBox.SelectedIndices.Count == 0)
					return;

				if (MessageBox.Show("Are you sure you wish to delete these keys?", "Warning...", MessageBoxButtons.OKCancel) == DialogResult.Cancel) 
					return;

				DialogResult res = DialogResult.No;
				if (IMAGESET.Count > 1)
					res = MessageBox.Show("Delete selected key(s) from all headers (YES), or only current header (NO)?", "Warning...", MessageBoxButtons.YesNoCancel);

				if (res == DialogResult.Cancel)
					return;
				else if (res == DialogResult.Yes)
					HeaderContextApplyAll.Checked = true;
				else
					HeaderContextApplyAll.Checked = false;

				HeaderContextRemoveKeys.PerformClick();
				return;
			}

			if (e.Control && e.KeyCode == Keys.C)//copy
			{
				if (HeaderKeysListBox.SelectedIndices.Count == 0)
					return;

				String copy = "";
				for (int i = 0; i < HeaderKeysListBox.SelectedIndices.Count; i++)
					copy += HEADER[(int)HeaderKeysListBox.SelectedIndices[i]].GetFullyFomattedFITSLine() + Environment.NewLine;

				if (copy.Length > 0)
					Clipboard.SetText(copy);

				return;
			}

			//if (e.Control && e.KeyCode == Keys.V)//paste
			//{
			//	String paste = Clipboard.GetText();
			//	if (paste == null || paste.Length == 0)
			//		return;

			//	if (!JPMath.IsInteger((double)(paste.Length) / 80))
			//		return;

			//	int nlines = paste.Length / 80;
			//	for (int i = 0; i < nlines; i++)
			//	{
			//		FITSHeaderKey key = new FITSHeaderKey(paste.Substring(i * 80, 80));
			//		HEADER.AddKey(key, -1);
			//	}

			//	HeaderKeysListBox.SuspendLayout();
			//	HeaderKeysListBox.Items.Clear();
			//	HeaderKeysListBox.Items.AddRange(HEADER.GetFormattedHeaderBlock(FITSHeader.HeaderUnitType.Primary, true));
			//	HeaderKeysListBox.ResumeLayout();
			//}
		}

		private void EditCopyfromFileBtn_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "FITS|*.fts; *.fit; *.fits|All|*.*";
			if (ofd.ShowDialog() == DialogResult.Cancel)
				return;

			FITSHeader hed = new FITSHeader(ofd.FileName);
			this.HEADER.CopyHeaderFrom(hed);

			HeaderKeysListBox.SuspendLayout();
			HeaderKeysListBox.Items.Clear();
			HeaderKeysListBox.Items.AddRange(HEADER.GetFormattedHeaderBlock(FITSHeader.HeaderUnitType.Primary, true));
			HeaderKeysListBox.ResumeLayout();
		}

		private void EditClearBtn_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Are you really sure that you want to clear this header of all values?", "WARNING!", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
				return;

			this.Header = new FITSHeader(true, IMAGESET[IMAGESETHEADERINDEX].Image);
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}

