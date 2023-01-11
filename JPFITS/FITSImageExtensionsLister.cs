using System;
using System.Windows.Forms;
#nullable enable

namespace JPFITS
{
	public partial class FITSImageExtensionsLister : Form
	{
		private string[]? EXTNAMES;
		private int[]? EXTINDEXES_ONEBASED;

		public string[] ExtensionNames
		{
			get { return EXTNAMES; }
		}

		public int[] ExtensionIndexesOneBased
		{
			get { return EXTINDEXES_ONEBASED; }
		}

		public FITSImageExtensionsLister(string fileName)
		{
			InitializeComponent();

			string[] extlist = FITSImage.GetAllExtensionNames(fileName);

			if (extlist.Length == 0)
			{
				MessageBox.Show("No IMAGE extensions exist in the file...", "Error");
				this.CancelBtn.PerformClick();
				this.Close();
				return;
			}

			FITSImage fi = new FITSImage(fileName, null, true, false, false, false);
			if (fi.Header.GetKeyValue("NAXIS") == "0")
				IncludePrimaryChck.Enabled = false;

			int n = 1;
			for (int i = 0; i < extlist.Length; i++)
			{
				if (extlist[i] == "")
				{
					ExtensionListBox.Items.Add("Unnamed extension: " + n);
					n++;
				}
				else
					ExtensionListBox.Items.Add(extlist[i]);

				ExtensionListBox.SetSelected(i, true);
			}
		}

		private void SelectAllBtn_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < ExtensionListBox.Items.Count; i++)
				ExtensionListBox.SetSelected(i, true);
		}

		private void ClearSelectionBtn_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < ExtensionListBox.Items.Count; i++)
				ExtensionListBox.SetSelected(i, false);
		}

		private void ReturnSelectionBtn_Click(object sender, EventArgs e)
		{
			if (ExtensionListBox.SelectedItems.Count == 0)
				return;

			EXTNAMES = new string[ExtensionListBox.SelectedItems.Count];
			EXTINDEXES_ONEBASED = new int[ExtensionListBox.SelectedItems.Count];

			for (int i = 0; i < ExtensionListBox.SelectedItems.Count; i++)
			{ 
				EXTNAMES[i] = ExtensionListBox.SelectedItems[i].ToString();
				EXTINDEXES_ONEBASED[i] = ExtensionListBox.SelectedIndices[i] + 1;
			}
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{

		}

		private void ExtensionListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (ExtensionListBox.SelectedItems.Count == 0)
				ReturnSelectionBtn.Enabled = false;
			else
				ReturnSelectionBtn.Enabled = true;

			this.Text = string.Format("Image Extensions ({0} selected)", ExtensionListBox.SelectedItems.Count);
		}

		private void ExtensionListBox_DoubleClick(object sender, EventArgs e)
		{
			ReturnSelectionBtn.PerformClick();
		}
	}
}
