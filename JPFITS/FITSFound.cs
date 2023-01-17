using System;
using System.Windows.Forms;
using System.IO;
#nullable enable

namespace JPFITS
{
	/// <summary>FITSFound class to manage a list of FITS files on disk.</summary>
	public partial class FITSFound : Form
	{
		private string[]? SELECTEDFILES;
        private string[] FOUNDFILES;
        private WaitBar? WAITBAR;

        public string[] SelectedFiles
		{
			get
			{
				return SELECTEDFILES;
            }
		}

		/// <summary> Constructor </summary>
		/// <param name="foundFiles">A list of files found on disk.</param>
		public FITSFound(string[] foundFiles)
		{
			InitializeComponent();

			FOUNDFILES = foundFiles;
			this.Text = "Found " + FOUNDFILES.Length.ToString() + " files...";
			NumFilesTxt.Text = "Please Select File(s)...";
			FileListTxt.BeginUpdate();
			FileListTxt.Items.AddRange(FOUNDFILES);
			FileListTxt.EndUpdate();
		}

		/// <summary> Constructor </summary>
		/// <param name="fullFileNameFoundList">A FITSFound list filename containing the list of FITS files, beginning with the number of files (lines) as the first line of the file.</param>
		public FITSFound(string fullFileNameFoundList)
		{
			InitializeComponent();

			FileStream fs2 = new FileStream(fullFileNameFoundList, System.IO.FileMode.Open, FileAccess.Read);
			StreamReader sr2 = new StreamReader(fs2);
			int numlines = System.Convert.ToInt32(sr2.ReadLine());

			FOUNDFILES = new string[numlines];
			for (int i = 0; i < numlines; i++)
				FOUNDFILES[i] = sr2.ReadLine();

			sr2.Close();
			fs2.Close();

			this.Text = "Found " + FOUNDFILES.Length.ToString() + " files...";
			NumFilesTxt.Text = "Please Select File(s)...";
			FileListTxt.BeginUpdate();
			FileListTxt.Items.AddRange(FOUNDFILES);
			FileListTxt.EndUpdate();
		}		

		private void FileListTxt_SelectedIndexChanged(System.Object sender, System.EventArgs e)
		{
			NumSelectTxt.Text = "(" + FileListTxt.SelectedItems.Count + " selected)";

			SELECTEDFILES = new string[FileListTxt.SelectedItems.Count];
			for (int i = 0; i < SELECTEDFILES.Length; i++)
				SELECTEDFILES[i] = FileListTxt.SelectedItems[i].ToString();
        }

		private void SelectAllBtn_Click(System.Object sender, System.EventArgs e)
		{
			FileListTxt.BeginUpdate();
			for (int i = 0; i < FileListTxt.Items.Count; i++)
				FileListTxt.SetSelected(i, true);
			FileListTxt.EndUpdate();
		}

		private void ClearAllBtn_Click(System.Object sender, System.EventArgs e)
		{
			FileListTxt.BeginUpdate();
			for (int i = 0; i < FileListTxt.Items.Count; i++)
				FileListTxt.SetSelected(i, false);
			FileListTxt.EndUpdate();
		}

		private void SaveListBtn_Click(System.Object sender, System.EventArgs e)
		{
			int Ninds = FileListTxt.SelectedIndices.Count;
			if (Ninds == 0)//no files selected but asked to save files
			{
				MessageBox.Show("No Files Selected!...", "Error");
				return;
			}

			string[] selectfiles = new string[Ninds];
			for (int j = 0; j < Ninds; j++)
				selectfiles[j] = (string)FileListTxt.Items[FileListTxt.SelectedIndices[j]];

			string dir = (string)REG.GetReg("CCDLAB", "OpenFilesPath");
			SaveFileDialog dlg = new SaveFileDialog();
			dlg.InitialDirectory = dir;
			dlg.Filter = "CCDLAB File List (*.CFL)|*.CF"; DialogResult res = dlg.ShowDialog();

			if (res == DialogResult.OK)
			{
				string file = dlg.FileName;
				FileStream fs = new FileStream(file, System.IO.FileMode.Create, FileAccess.Write);
				StreamWriter sw = new StreamWriter(fs);
				sw.WriteLine(selectfiles.Length);
				for (int u = 0; u < selectfiles.Length; u++)
					sw.WriteLine(selectfiles[u]);
				sw.Flush();
				fs.Flush();
				sw.Close();
				fs.Close();
				this.DialogResult = DialogResult.Yes;
				this.Close();

				REG.SetReg("CCDLAB", "FoundFileList", file);
			}
		}

		private void CancelBtn_Click(System.Object sender, System.EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}

		private void MoveListBtn_Click(System.Object sender, System.EventArgs e)
		{
			int Ninds = FileListTxt.SelectedIndices.Count;
			if (Ninds == 0)//no files selected but asked to copy files
			{
				MessageBox.Show("No Files Selected!...", "Error");
				return;
			}

			string[] selectfiles = new string[Ninds];
			for (int j = 0; j < Ninds; j++)
				selectfiles[j] = (string)FileListTxt.Items[FileListTxt.SelectedIndices[j]];

			FolderBrowserDialog fdlg = new FolderBrowserDialog();
			fdlg.SelectedPath = selectfiles[0].Substring(0, selectfiles[0].LastIndexOf("\\") + 1);

			if (fdlg.ShowDialog() == DialogResult.Cancel)
				return;

			object[] arg = new object[3] { selectfiles, fdlg.SelectedPath, "move" };

			WAITBAR = new WaitBar();
			WAITBAR.Text = "Moving Files...";
			WAITBAR.ProgressBar.Maximum = selectfiles.Length;
			FileCopyBGWrkr.RunWorkerAsync(arg);
			WAITBAR.ShowDialog();
		}

		private void CopyListBtn_Click(System.Object sender, System.EventArgs e)
		{
			int Ninds = FileListTxt.SelectedIndices.Count;
			if (Ninds == 0)//no files selected but asked to copy files
			{
				MessageBox.Show("No Files Selected!...", "Error");
				return;
			}

			string[] selectfiles = new string[Ninds];
			for (int j = 0; j < Ninds; j++)
				selectfiles[j] = (string)FileListTxt.Items[FileListTxt.SelectedIndices[j]];

			FolderBrowserDialog fdlg = new FolderBrowserDialog();
			fdlg.SelectedPath = selectfiles[0].Substring(0, selectfiles[0].LastIndexOf("\\") + 1);

			if (fdlg.ShowDialog() == DialogResult.Cancel)
				return;

			object[] arg = new object[3] { selectfiles, fdlg.SelectedPath, "copy" };

			WAITBAR = new WaitBar();
			WAITBAR.Text = "Copying Files...";
			WAITBAR.ProgressBar.Maximum = selectfiles.Length;
			FileCopyBGWrkr.RunWorkerAsync(arg);
			WAITBAR.ShowDialog();
		}

		private void FileCopyBGWrkr_DoWork(System.Object sender, System.ComponentModel.DoWorkEventArgs e)
		{
			object[] arg = (object[])e.Argument;
			string[] selectfiles = (string[])arg[0];
			string selectedpath = (string)arg[1];
			string style = (string)arg[2];
			bool move = false;
			if (style == "move")
				move = true;

			int Ninds = selectfiles.Length;
			string newfile;

			for (int j = 0; j < Ninds; j++)
			{
				if (WAITBAR.DialogResult == DialogResult.Cancel)
					return;
				FileCopyBGWrkr.ReportProgress(j + 1, move);

				newfile = selectedpath + "\\" + selectfiles[j].Substring(selectfiles[j].LastIndexOf("\\"));

				while (File.Exists(newfile))//then need to add some appendage
				{
					int ind = newfile.LastIndexOf(".");
					if (newfile.Substring(ind - 1, 1) == ")")
					{
						int num = Convert.ToInt32(newfile.Substring(newfile.LastIndexOf("(") + 1, newfile.LastIndexOf(")") - 1 - newfile.LastIndexOf("(")));
						newfile = newfile.Replace("(" + num.ToString() + ").", "(" + (num + 1).ToString() + ").");
					}
					else
					{
						newfile = newfile.Insert(ind, " (1)");
						if (File.Exists(newfile))
						{
							int num = Convert.ToInt32(newfile.Substring(newfile.LastIndexOf("(") + 1, newfile.LastIndexOf(")") - 1 - newfile.LastIndexOf("(")));
							newfile = newfile.Replace("(" + num.ToString() + ").", "(" + (num + 1).ToString() + ").");
						}
					}
				}
				if (move)
					File.Move(selectfiles[j], newfile);
				else
					File.Copy(selectfiles[j], newfile);
			}
		}

		private void FileCopyBGWrkr_ProgressChanged(System.Object sender, System.ComponentModel.ProgressChangedEventArgs e)
		{
			bool move = Convert.ToBoolean(e.UserState);
			if (move)
				WAITBAR.TextMsg.Text = "Moving file " + e.ProgressPercentage.ToString() + " of " + WAITBAR.ProgressBar.Maximum.ToString();
			else
				WAITBAR.TextMsg.Text = "Copying file " + e.ProgressPercentage.ToString() + " of " + WAITBAR.ProgressBar.Maximum.ToString();
			WAITBAR.ProgressBar.Value = e.ProgressPercentage;
			WAITBAR.Refresh();
		}

		private void FileCopyBGWrkr_RunWorkerCompleted(System.Object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
		{
			WAITBAR.Close();
			if (WAITBAR.DialogResult != DialogResult.Cancel)
			{
				if (Convert.ToBoolean(e.UserState))
					MessageBox.Show("Finished moving files...");
				else
					MessageBox.Show("Finished copying files...");
			}
			CancelBtn.PerformClick();
		}

		private void FileListTxt_MouseClick(System.Object sender, System.Windows.Forms.MouseEventArgs e)
		{
		}

		private void FileListTxt_MouseUp(System.Object sender, System.Windows.Forms.MouseEventArgs e)
		{
			int Ninds = FileListTxt.SelectedIndices.Count;
			if (Ninds == 1)
			{
				FoundListContextMenu.Enabled = true;
				OpenFolderContextItem.Enabled = true;
			}
			else
			{
				FoundListContextMenu.Enabled = false;
				OpenFolderContextItem.Enabled = false;
			}
		}

		private void OpenFolderContextItem_Click(System.Object sender, System.EventArgs e)
		{
			string selectfile = (string)FileListTxt.Items[FileListTxt.SelectedIndices[0]];
			selectfile = selectfile.Substring(0, selectfile.LastIndexOf("\\") + 1);
			System.Diagnostics.Process.Start("Explorer.exe", selectfile);
		}

		private void FitsFound_FormClosing(System.Object sender, System.Windows.Forms.FormClosingEventArgs e)
		{
			REG.SetReg("JPFITS", "FitsFoundPOSX", this.Location.X);
			REG.SetReg("JPFITS", "FitsFoundPOSY", this.Location.Y);
			REG.SetReg("JPFITS", "FitsFoundWIDTH", this.Size.Width);
			REG.SetReg("JPFITS", "FitsFoundHEIGHT", this.Size.Height);
		}

		private void FitsFound_Load(System.Object sender, System.EventArgs e)
		{
			this.Left = (int)REG.GetReg("JPFITS", "FitsFoundPOSX");
			this.Top = (int)REG.GetReg("JPFITS", "FitsFoundPOSY");
			this.Width = (int)REG.GetReg("JPFITS", "FitsFoundWIDTH");
			this.Height = (int)REG.GetReg("JPFITS", "FitsFoundHEIGHT");
		}

		private void LoadImageSetBtn_Click(object sender, EventArgs e)
		{

		}
	}
}
