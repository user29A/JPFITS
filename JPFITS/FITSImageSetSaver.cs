using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace JPFITS
{
	/// <summary>
	/// Provides functionality to save FITSImageSets with manipulation and modification of their file names and locations, and also as ZIP archives or as JPG images.
	/// </summary>
	public partial class FITSImageSetSaver : Form
	{
		private FITSImageSet IMAGESET;

		/// <summary>
		/// Constructor is given an existing FITSImageSet
		/// </summary>
		public FITSImageSetSaver(FITSImageSet imageSet)
		{
			InitializeComponent();

			IMAGESET = imageSet;

			this.Text = "Batch Save " + IMAGESET.Count + " files...";
		}

		private void FITSImageSetSaver_Shown(object sender, EventArgs e)
		{
			//get appendage
			AppendTxt.Text = (string)REG.GetReg("CCDLAB", "Appendage");

			//get directory
			DirectoryTxt.Text = IMAGESET.GetCommonDirectory();// (string)REG.GetReg("CCDLAB", "BatchSavePath");

			//get file extension index
			FileExtension.SelectedIndex = Convert.ToInt32(REG.GetReg("CCDLAB", "FileExtensionIndex"));
		}

		private void DirectoryTxt_Click(object sender, EventArgs e)
		{
			if (DirectoryFileLabel.Text.Contains("Directory"))
			{
				FolderBrowserDialog fbd = new FolderBrowserDialog();
				fbd.SelectedPath = DirectoryTxt.Text;
				fbd.Description = "Please select the foler in which to save the files...";
				if (fbd.ShowDialog() == DialogResult.Cancel)
					return;

				string dir = fbd.SelectedPath + "\\";
				DirectoryTxt.Text = dir;
				REG.SetReg("CCDLAB", "BatchSavePath", dir);
				UseOrigDirChck.Checked = false;
			}
			else if (DirectoryFileLabel.Text.Contains("File"))
			{
				SaveFileDialog sfd = new SaveFileDialog();
				sfd.Filter = "ZIP|*.zip";
				sfd.InitialDirectory = IMAGESET.GetCommonDirectory();				
				sfd.FileName = sfd.InitialDirectory.Remove(sfd.InitialDirectory.LastIndexOf("\\"));
				sfd.FileName = sfd.FileName.Substring(sfd.FileName.LastIndexOf("\\") + 1);

				if (sfd.ShowDialog() == DialogResult.Cancel)
					return;

				DirectoryTxt.Text = sfd.FileName;
			}
		}

		private void UseOrigDirChck_CheckedChanged(object sender, EventArgs e)
		{
			if (UseOrigDirChck.Checked)
			{
				string paths = "";

				for (int i = 0; i < IMAGESET.Count; i++)
				{
					string currentPath = IMAGESET[i].FilePath;
					DirectoryInfo dirinf = new DirectoryInfo(currentPath);
					paths += "..." + dirinf.Name + "\\\r\n";
				}

				DirectoryTxt.Text = paths;
			}
			else
				DirectoryTxt.Text = (string)REG.GetReg("CCDLAB", "BatchSavePath");
		}

		private void GetCommonDirectoryBtn_Click(object sender, EventArgs e)
		{
			DirectoryTxt.Text = IMAGESET.GetCommonDirectory();
			REG.SetReg("CCDLAB", "BatchSavePath", DirectoryTxt.Text);

			UseOrigDirChck.Checked = false;
		}

		private void AppendBtnChangeBtn_Click(object sender, EventArgs e)
		{
			if (AppendBtn.Text == "Append")
			{
				AppendBtn.Text = "Remove";
				toolTip1.SetToolTip(AppendBtn, "This will remove the text below from the file name, thus, the original files will not be overwritten. If no text is entered, you will receive an error notification.");
			}
			else if (AppendBtn.Text == "Remove")
			{
				AppendBtn.Text = "Remove Aft";
				toolTip1.SetToolTip(AppendBtn, "This will remove everything after the text below from the file name, thus, the original files will not be overwritten. If no text is entered, you will receive an error notification.");
			}
			else if (AppendBtn.Text == "Remove Aft")
			{
				AppendBtn.Text = "Append";
				toolTip1.SetToolTip(AppendBtn, "This will append the text below to the file name, thus, the original files will not be overwritten. If no text is entered, you will receive an error notification.");
			}
		}

		private void AppendBtn_Click(object sender, EventArgs e)
		{
			//write extension index
			REG.SetReg("CCDLAB", "FileExtensionIndex", FileExtension.SelectedIndex.ToString());
			//write appendage
			REG.SetReg("CCDLAB", "Appendage", AppendTxt.Text);

			WriteImageSet("append");
		}

		private void OverwriteBtn_Click(object sender, EventArgs e)
		{
			//write extension index
			REG.SetReg("CCDLAB", "FileExtensionIndex", FileExtension.SelectedIndex.ToString());

			WriteImageSet("overwrite");
		}

		private void WriteImageSet(string appendORoverwrite)
		{
			if (appendORoverwrite == "append")//   append/remove/remove aft btn
			{
				if (AppendBtn.Text.Contains("Remove") && AppendTxt.Text == "*")
				{
					this.DialogResult = DialogResult.None;
					MessageBox.Show("Can't auto-increment when removing subtext...", "Error");
					return;
				}

				if (AppendTxt.Text == string.Empty)//then do nothing and notify
				{
					this.DialogResult = DialogResult.None;
					MessageBox.Show("Blank text field entered so nothing to do...", "Error");
					return;
				}
			}

			if (appendORoverwrite == "append")//then append
				if (MessageBox.Show("Are you sure?", "Proceed?", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
				{
					this.DialogResult = DialogResult.None;
					return;
				}

			if (appendORoverwrite == "overwrite")//then overwrite, but first confirm
				if (MessageBox.Show("Are you sure you want to write the files with their existing file names? Doing this in their original directory(s) will overwrite them, but if you have selected a new directory it will not.", "Warning...", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
				{
					this.DialogResult = DialogResult.None;
					return;
				}

			//then go ahead and make the (new?) filenames
			bool useExistingPaths = UseOrigDirChck.Checked;
			string filepath = DirectoryTxt.Text;
			string extension = FileExtension.SelectedItem.ToString();
			string fullfilename;
			bool appendremovebtn = appendORoverwrite == "append";//else write/overwrite
			string apptxt = AppendTxt.Text;
			bool autoinc = apptxt == "*";
			bool append = AppendBtn.Text == "Append";//else remove
			for (int i = 0; i < IMAGESET.Count; i++)
			{
				if (FileExtension.SelectedIndex == 4 && ZipInOneChck.Checked)//zip
					continue;// extension = new FileInfo(IMAGESET[i].FullFileName).Extension;//keep the file extensions and they're all going into a single zip file

				string filename = IMAGESET[i].FileName;
				int ind = filename.LastIndexOf(".");

				if (appendremovebtn)
				{
					if (append)
					{
						if (autoinc)
							filename = String.Concat(filename.Substring(0, ind), " (", i.ToString(), ")", extension);
						else
							filename = String.Concat(filename.Substring(0, ind), apptxt, extension);
					}
					else//remove or remove aft
					{
						if (AppendBtn.Text == "Remove")
							filename = filename.Replace(apptxt, "");
						else//remove after
							if (filename.Contains(apptxt))
						{
							int ind1 = filename.IndexOf(apptxt);
							filename = filename.Remove(ind1) + extension;
						}
					}
				}
				else//write/overwrite
					filename = String.Concat(filename.Substring(0, ind), extension);

				if (useExistingPaths)
					filepath = IMAGESET[i].FilePath;

				fullfilename = filepath + filename;
				IMAGESET[i].FullFileName = fullfilename;
			}

			if (FileExtension.SelectedIndex <= 2)//extension == ".fts" || extension == ".fits" || extension == ".fit")
				IMAGESET.Write(DiskPrecision.Double, true, "Saving image set");
			else if (FileExtension.SelectedIndex == 3 /*(extension == ".jpg"*/)
			{
				for (int i = 0; i < IMAGESET.Count; i++)
				{
					Bitmap bmp1 = JPBitMap.ArrayToBmp(IMAGESET[i].Image, 0, 0, InvertGrayScaleChck.Checked, new double[2] { IMAGESET[i].Mean - IMAGESET[i].Stdv * 0.5, IMAGESET[i].Mean + IMAGESET[i].Stdv * 5 }, IMAGESET[i].Width, IMAGESET[i].Height, false);
					bmp1.Save(IMAGESET[i].FullFileName, ImageFormat.Jpeg);
				}
			}
			else if (FileExtension.SelectedIndex == 4 /*extension == ".zip"*/)
			{
				if (!ZipInOneChck.Checked)//would need to write each file first as a fits, and then zip it, then delete the fits file
					for (int i = 0; i < IMAGESET.Count; i++)
					{
						string name = IMAGESET[i].FullFileName;
						IMAGESET[i].WriteImage(IMAGESET[i].FullFileName + "tozip.fits", DiskPrecision.Double, true);

						string ziplist = IMAGESET[i].FilePath + "//tozip.txt";
						StreamWriter sw = new StreamWriter(ziplist);
						sw.WriteLine(IMAGESET[i].FullFileName);
						sw.Close();

						Process p = new Process();
						p.StartInfo.FileName = "c:\\Program Files\\7-Zip\\7zG.exe";
						p.StartInfo.Arguments = "\"a\" " + "\"-tzip\" " + "\"" + name + "\" " + "\"@" + ziplist;
						p.Start();
						p.WaitForExit();
						File.Delete(ziplist);
						File.Delete(IMAGESET[i].FullFileName);//tozip.fits temp
						IMAGESET[i].FullFileName = name;
						if (p.ExitCode != 0)
						{
							this.DialogResult = DialogResult.None;
							return;
						}
						if (ZipContextMoveChck.Checked)
							File.Delete(IMAGESET[i].FullFileName);
					}
				else//zip into one archive
				{
					string ziplist = IMAGESET.GetCommonDirectory() + "//tozip.txt";
					StreamWriter sw = new StreamWriter(ziplist);
					for (int i = 0; i < IMAGESET.Count; i++)
						sw.WriteLine(IMAGESET[i].FullFileName);
					sw.Close();

					Process p = new Process();
					p.StartInfo.FileName = "c:\\Program Files\\7-Zip\\7zG.exe";
					p.StartInfo.Arguments = "\"a\" " + "\"-tzip\" " + "\"" + DirectoryTxt.Text + "\" " + "\"@" + ziplist;
					p.Start();
					p.WaitForExit();
					File.Delete(ziplist);
					if (p.ExitCode != 0)
					{
						this.DialogResult = DialogResult.None;
						File.Delete(DirectoryTxt.Text);
						return;
					}

					if (ZipContextMoveChck.Checked)
						for (int i = 0; i < IMAGESET.Count; i++)
							File.Delete(IMAGESET[i].FullFileName);
				}
			}
		}

		private void FileExtension_SelectedIndexChanged(object sender, EventArgs e)
		{
			REG.SetReg("CCDLAB", "FileExtensionIndex", FileExtension.SelectedIndex.ToString());

			if (FileExtension.SelectedIndex == 3)//jpg
			{
				InvertGrayScaleChck.BringToFront();
				InvertGrayScaleChck.Visible = true;
			}
			else if (FileExtension.SelectedIndex == 4)//zip
			{
				ZipInOneChck.BringToFront();
				ZipInOneChck.Visible = true;
			}
			else
			{
				InvertGrayScaleChck.Visible = false;
				InvertGrayScaleChck.Checked = false;

				ZipInOneChck.Visible = false;
				ZipInOneChck.Checked = false;
			}
		}

		private void DirectoryTxt_TextChanged(object sender, EventArgs e)
		{
			REG.SetReg("CCDLAB", "BatchSavePath", DirectoryTxt.Text);
		}

		private void ZipInOneChck_CheckedChanged(object sender, EventArgs e)
		{
			if (ZipInOneChck.Checked)
			{
				UseOrigDirChck.Checked = false;
				UseOrigDirChck.Enabled = false;
				GetCommonDirectoryBtn.Enabled = false;
				AppendBtn.Enabled = false;
				AppendTxt.Enabled = false;
				AppendBtnChangeBtn.Enabled = false;

				DirectoryFileLabel.Text = "File Name:";

				string name = IMAGESET.GetCommonDirectory();
				name = Path.Combine(name, new DirectoryInfo(name).Name + ".zip");
				DirectoryTxt.Text = name;

				REG.SetReg("CCDLAB", "BatchSavePath", DirectoryTxt.Text);
			}
			else
			{
				UseOrigDirChck.Enabled = true;
				GetCommonDirectoryBtn.Enabled = true;
				AppendBtn.Enabled = true;
				AppendTxt.Enabled = true;
				AppendBtnChangeBtn.Enabled = true;

				GetCommonDirectoryBtn.PerformClick();
				DirectoryFileLabel.Text = "Directory:";
			}
		}

		private void ZipInOneChck_MouseEnter(object sender, EventArgs e)
		{
			ZipInOneChck.Font = new Font(ZipInOneChck.Font, FontStyle.Bold);
		}

		private void ZipInOneChck_MouseLeave(object sender, EventArgs e)
		{
			ZipInOneChck.Font = new Font(ZipInOneChck.Font, FontStyle.Regular);
		}

		private void ZipContextMoveChck_Click(object sender, EventArgs e)
		{
			ZipContextMoveChck.Checked = true;
			ZipContextCopyChck.Checked = false;

			ZipContextMenu.Show();
		}

		private void ZipContextCopyChck_Click(object sender, EventArgs e)
		{
			ZipContextMoveChck.Checked = false;
			ZipContextCopyChck.Checked = true;

			ZipContextMenu.Show();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			this.Close();
		}		
	}
}
