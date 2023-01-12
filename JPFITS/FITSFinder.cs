using System;
using System.Collections;
using System.Windows.Forms;
using System.IO;

namespace JPFITS
{
	/// <summary>FITSFinder class for seaching for FITS files given filename and FITS keyword characteristics.</summary>
	public partial class FITSFinder : Form
	{
		/// <summary>FITSFinder class constructor.</summary>
		public FITSFinder()
		{
			InitializeComponent();
		}

		/// <summary>FoundFiles gets or sets the list of files found by the FITSFinder.</summary>
		public string[] FoundFiles
		{
			get { return FOUNDFILES; }
			set { FOUNDFILES = value; }
		}

		private void CustomExtensionChck_CheckedChanged(object sender, EventArgs e)
		{
			if (CustomExtensionChck.Checked)
			{
				CustomExtensionTxtBox.Enabled = true;
				ExtensionDrop.Enabled = false;
			}
			else
			{
				CustomExtensionTxtBox.Enabled = false;
				ExtensionDrop.Enabled = true;
			}
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void DirectoryTxt_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog fb = new FolderBrowserDialog();
			fb.SelectedPath = (string)REG.GetReg("CCDLAB", "OpenFilesPath");
			fb.ShowDialog();
			string dir = fb.SelectedPath;
			DirectoryTxt.Text = dir;
			REG.SetReg("CCDLAB", "OpenFilesPath", dir);
		}

		private void FitsFinder_Load(object sender, EventArgs e)
		{
			DirectoryTxt.Text = (string)REG.GetReg("CCDLAB", "OpenFilesPath");
			string tmplt = (string)REG.GetReg("CCDLAB", "FindFilesTemplate");
			FileTemplateTxt.Text = tmplt.Substring(0, tmplt.LastIndexOf("."));
			int NumKeyValPairs = Convert.ToInt32(REG.GetReg("CCDLAB", "FindFilesNumKeyValPairs"));
			if (NumKeyValPairs > 0)
			{
				string key;
				string keyval;
				System.Windows.Forms.TextBox[] k = new System.Windows.Forms.TextBox[] { Key1, Key2, Key3, Key4 };
				System.Windows.Forms.RichTextBox[] kv = new System.Windows.Forms.RichTextBox[] { Key1Value, Key2Value, Key3Value, Key4Value };
				for (int i = 0; i < NumKeyValPairs; i++)
				{
					key = (string)REG.GetReg("CCDLAB", String.Concat("FindFilesKey", i));
					keyval = (string)REG.GetReg("CCDLAB", String.Concat("FindFilesKeyVal", i));
					k[i].Text = key;
					kv[i].Text = keyval;
				}
			}
			ExtensionDrop.SelectedIndex = Convert.ToInt32(REG.GetReg("CCDLAB", "FindFilesExtIndex"));
			this.Tag = DialogResult.None;

			SubFoldersChck.Checked = Convert.ToBoolean(REG.GetReg("CCDLAB", "SubFoldersChck"));

			CustomExtensionChck.Checked = Convert.ToBoolean(REG.GetReg("CCDLAB", "CustomExtChck"));
			CustomExtensionTxtBox.Text = (string)REG.GetReg("CCDLAB", "CustomExtTxt");
		}

		private void FindBtn_Click(object sender, EventArgs e)
		{
			string dir = DirectoryTxt.Text;
			if (!Directory.Exists(dir))
				throw new Exception("Directory doesn't exist...");

			bool subdirs = SubFoldersChck.Checked;
			REG.SetReg("CCDLAB", "SubFoldersChck", subdirs);

			//need to get search params and write them to reg for later FindFiles()
			string extension;
			if (CustomExtensionChck.Checked)
				extension = CustomExtensionTxtBox.Text;
			else
				extension = ExtensionDrop.Items[ExtensionDrop.SelectedIndex].ToString();

			if (FileTemplateTxt.Text == "")
			{
				FileTemplateTxt.Text = "*";
				this.Refresh();
			}

            string filetemplate = String.Concat(FileTemplateTxt.Text, extension);//file template for cursory directory search, which we'll start with

			int count = 0;
			System.Windows.Forms.TextBox[] k = new System.Windows.Forms.TextBox[] { Key1, Key2, Key3, Key4 };
			System.Windows.Forms.RichTextBox[] kv = new System.Windows.Forms.RichTextBox[] { Key1Value, Key2Value, Key3Value, Key4Value };
			for (int i = 0; i < k.Length; i++)
			{
				if (k[i].Text.Length == 0)
					continue;
				REG.SetReg("CCDLAB", String.Concat("FindFilesKey", count), k[i].Text);
				REG.SetReg("CCDLAB", String.Concat("FindFilesKeyVal", count), kv[i].Text);
				count++;
			}
			REG.SetReg("CCDLAB", "FindFilesNumKeyValPairs", count.ToString());
			REG.SetReg("CCDLAB", "FindFilesExtIndex", ExtensionDrop.SelectedIndex);
			REG.SetReg("CCDLAB", "FindFilesTemplate", filetemplate);
			REG.SetReg("CCDLAB", "CustomExtChck", CustomExtensionChck.Checked);
			REG.SetReg("CCDLAB", "CustomExtTxt", CustomExtensionTxtBox.Text);

			string[] fullfilesinit;
			if (!subdirs)
				fullfilesinit = Directory.GetFiles(dir, filetemplate, System.IO.SearchOption.TopDirectoryOnly);//cursory search
			else
				fullfilesinit = Directory.GetFiles(dir, filetemplate, System.IO.SearchOption.AllDirectories);//cursory search

			if (count > 0)//then we're doing more than just a cursory file template search
			{
				this.WAITBAR = new WaitBar();
				this.WAITBAR.ProgressBar.Maximum = fullfilesinit.Length;
				this.WAITBAR.Text = "Searching files...";
				FitsFinderWrkr.RunWorkerAsync(fullfilesinit);
				this.WAITBAR.ShowDialog();
			}
			else
			{
				FOUNDFILES = fullfilesinit;
				this.Tag = DialogResult.OK;
				this.DialogResult = DialogResult.OK;
			}
		}

		private void FitsFinderWrkr_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
		{
			int numparams = Convert.ToInt32(REG.GetReg("CCDLAB", "FindFilesNumKeyValPairs"));
			string[] fullfilesinit = (string[])e.Argument;
			ArrayList filelist = new ArrayList();
			string[] KeyParams = new string[(numparams)];
			string[] KeyValParams = new string[(numparams)];
			int match = 0;
			for (int i = 0; i < numparams; i++)//get the key/keyvalue pairs
			{
				KeyParams[i] = (string)REG.GetReg("CCDLAB", String.Concat("FindFilesKey", i));
				KeyValParams[i] = (string)REG.GetReg("CCDLAB", String.Concat("FindFilesKeyVal", i));
			}
			//done filling the param pairs...now need to do the work

			for (int ii = 0; ii < fullfilesinit.Length; ii++)
			{
				if (this.WAITBAR.DialogResult == DialogResult.Cancel)
				{
					this.DialogResult = DialogResult.Cancel;
					this.Tag = DialogResult.Cancel;
					return;
				}
				FitsFinderWrkr.ReportProgress(ii + 1, filelist.Count);

				FITSImage f1;

				try
				{
					f1 = new FITSImage(fullfilesinit[ii], null, true, false, false, false);
				}
				catch
				{
					continue;
				}
				
				match = 0;
				for (int j = 0; j < f1.Header.Length; j++)
				{
					string key = f1.Header[j].Name;
					for (int k = 0; k < numparams; k++)
						if (KeyParams[k] == key && KeyValParams[k] == f1.Header[j].Value)
							match++;
				}
				if (match == numparams)
					filelist.Add(fullfilesinit[ii]);
			}

			string[] matchedfiles = new string[filelist.Count];
			for (int h = 0; h < filelist.Count; h++)
				matchedfiles[h] = (string)filelist[h];

			e.Result = matchedfiles;

			this.DialogResult = DialogResult.OK;
		}

		private void FitsFinderWrkr_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
		{
			this.WAITBAR.Text = "Searching files. Found " + Convert.ToInt32(e.UserState).ToString() + " matches...";
			this.WAITBAR.ProgressBar.Value = e.ProgressPercentage;
			this.WAITBAR.TextMsg.Text = "Examining File: " + e.ProgressPercentage.ToString() + " of " + this.WAITBAR.ProgressBar.Maximum.ToString();
			this.WAITBAR.Refresh();
		}

		private void FitsFinderWrkr_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
		{
			if (this.WAITBAR.DialogResult == DialogResult.Cancel)
			{
				FOUNDFILES = new string[0];
				this.DialogResult = DialogResult.Cancel;
				this.Tag = DialogResult.Cancel;
				this.Close();
				this.WAITBAR.Close();
			}
			else
			{
				FOUNDFILES = (string[])e.Result;
				this.Tag = DialogResult.OK;
				this.Close();
				this.WAITBAR.DialogResult = DialogResult.OK;
				this.WAITBAR.Close();
			}
		}

		private void CustomExtensionTxtBox_TextChanged(object sender, EventArgs e)
		{
			
		}
	}
}
