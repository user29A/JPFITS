using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
				if (extlist[i] == "")
				{
					ExtensionChckdListBox.Items.Add("Unnamed extension: " + n, true);
					n++;
				}
				else
					ExtensionChckdListBox.Items.Add(extlist[i], true);
		}

		private void SelectAllBtn_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < ExtensionChckdListBox.Items.Count; i++)
				ExtensionChckdListBox.SetItemChecked(i, true);
		}

		private void ClearSelectionBtn_Click(object sender, EventArgs e)
		{
			for (int i = 0; i < ExtensionChckdListBox.Items.Count; i++)
				ExtensionChckdListBox.SetItemChecked(i, false);
		}

		private void ReturnSelectionBtn_Click(object sender, EventArgs e)
		{
			if (ExtensionChckdListBox.CheckedItems.Count == 0)
				return;

			EXTNAMES = new string[ExtensionChckdListBox.CheckedItems.Count];
			EXTINDEXES_ONEBASED = new int[ExtensionChckdListBox.CheckedItems.Count];

			for (int i = 0; i < ExtensionChckdListBox.Items.Count; i++)
				if (ExtensionChckdListBox.GetItemChecked(i))
				{ 
					EXTNAMES[i] = ExtensionChckdListBox.Items[i].ToString();
					EXTINDEXES_ONEBASED[i] = i + 1;
				}

			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{

		}
	}
}
