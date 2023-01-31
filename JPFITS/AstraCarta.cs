using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;
#nullable enable

namespace JPFITS
{
	public partial class AstraCarta : Form
	{
		System.Diagnostics.ProcessStartInfo? PSI;
		System.Diagnostics.Process? PROC;
		string OUTFILE = "";
		string ERR = "";
		string CURVERS = "";
		string LATVERS = "";
		bool OPENFROMARGS = false;
		bool EXECUTEONSHOW = false;
		bool PERFORMEXECUTE = false;
		bool CANCELLED = false;
		bool ERROR = false;

		public string Result_Filename
		{
			get { return OUTFILE; }
		}

		/// <summary>
		/// Perform a query with no user interface dialog form. Will throw an informative exception if something goes wrong. Returns a string which is the filename of the catalogue data downloaded.
		/// </summary>
		/// <param name="keys">A string list of keys such as "-ra", "-dec", "-scale", etc.</param>
		/// <param name="values">A string list of key values for the keys. Keys with no arguments must have empty strings passed as their values.</param>
		public static string Query(ArrayList keys, ArrayList values)
		{
			string argstring = "";
			int n;

			n = keys.IndexOf("-ra");
			if (n != -1)
				argstring += String.Format("-ra {0} ", (string)values[n]);

			n = keys.IndexOf("-dec");
			if (n != -1)
				argstring += String.Format("-dec {0} ", (string)values[n]);

			n = keys.IndexOf("-scale");
			if (n != -1)
				argstring += String.Format("-scale {0} ", (string)values[n]);

			n = keys.IndexOf("-pixwidth");
			if (n != -1)
				argstring += String.Format("-pixwidth {0} ", (string)values[n]);

			n = keys.IndexOf("-pixheight");
			if (n != -1)
				argstring += String.Format("-pixheight {0} ", (string)values[n]);

			n = keys.IndexOf("-buffer");
			if (n != -1)
				argstring += String.Format("-buffer {0} ", (string)values[n]);

			n = keys.IndexOf("-offsetra");
			if (n != -1)
				argstring += String.Format("-offsetra {0} ", (string)values[n]);

			n = keys.IndexOf("-offsetdec");
			if (n != -1)
				argstring += String.Format("-offsetdec {0} ", (string)values[n]);

			n = keys.IndexOf("-outname");
			if (n != -1)
				argstring += String.Format("-outname {0} ", "\"" + string.Join("_", ((string)values[n]).Split(Path.GetInvalidFileNameChars())) + "_AstraCarta" + "\"");

			n = keys.IndexOf("-catalogue");
			if (n != -1)
				argstring += String.Format("-catalogue {0} ", "\"" + (string)values[n] + "\"");

			n = keys.IndexOf("-filter");
			if (n != -1)
				argstring += String.Format("-filter {0} ", "\"" + (string)values[n] + "\"");

			n = keys.IndexOf("-maglimit");
			if (n != -1)
				argstring += String.Format("-maglimit {0} ", (string)values[n]);

			n = keys.IndexOf("-shape");
			if (n != -1)
				argstring += String.Format("-shape {0} ", "\"" + (string)values[n] + "\"");

			n = keys.IndexOf("-rotation");
			if (n != -1)
				argstring += String.Format("-rotation {0} ", (string)values[n]);

			n = keys.IndexOf("-nquery");
			if (n != -1)
				argstring += String.Format("-nquery {0} ", (string)values[n]);

			n = keys.IndexOf("-pmepoch");
			if (n != -1)
				argstring += String.Format("-pmepoch {0} ", (string)values[n]);

			n = keys.IndexOf("-pmlimit");
			if (n != -1)
				argstring += String.Format("-pmlimit {0} ", (string)values[n]);

			n = keys.IndexOf("-outdir");
			if (n != -1)
				argstring += String.Format("-outdir {0} ", "\"" + (string)values[n] + "\"");

			n = keys.IndexOf("-entries");
			if (n != -1)
				argstring += String.Format("-entries {0} ", "\"" + (string)values[n] + "\"");

			n = keys.IndexOf("-notable");
			if (n != -1)
				argstring += String.Format("-notable ");

			n = keys.IndexOf("-fitsout");
			if (n != -1)
				argstring += String.Format("-fitsout ");

			n = keys.IndexOf("-imageshow");
			if (n != -1)
				argstring += String.Format("-imageshow ");

			n = keys.IndexOf("-outimage");
			if (n != -1)
				argstring += String.Format("-outimage ");

			n = keys.IndexOf("-forcenew");
			if (n != -1)
				argstring += String.Format("-forcenew ");

			n = keys.IndexOf("-rmvrawquery");
			if (n != -1)
				argstring += String.Format("-rmvrawquery ");

			n = keys.IndexOf("-silent");
			if (n != -1)
				argstring += String.Format("-silent ");

			n = keys.IndexOf("-overwrite");
			if (n != -1)
				argstring += String.Format("-overwrite ");

			ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + "astracarta " + argstring);
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			psi.RedirectStandardError = true;
			psi.RedirectStandardOutput = true;
			System.Diagnostics.Process proc = new System.Diagnostics.Process();
			proc = System.Diagnostics.Process.Start(psi);
			proc.WaitForExit();

			string outfile = proc.StandardOutput.ReadToEnd().Trim();
			string test = outfile;
			string errstr = proc.StandardError.ReadToEnd().Trim();
			if (outfile == "")
				throw new Exception("Query failed with message: \r\n\r\n" + errstr + "\r\n\r\n" + proc.StandardOutput.ReadToEnd().Trim() + "\r\n\r\n" + argstring);
			
			int c = 0;
			while (!File.Exists(test))
			{
				c++;
				test = outfile.Substring(c);

				if (test.Length == 1)
					throw new Exception("Query failed from possible output file: '" + outfile + "'");
			}

			return test.Trim();
		}

		/// <summary>
		/// Construct AstraCarta. Useful for building queries with a user interface for option settings.
		/// </summary>
		public AstraCarta()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Construct AstraCarta with keys and their values. Useful for repeating/specifying user interface option settings.
		/// </summary>
		/// <param name="keys">A string list of keys such as "-ra", "-dec", "-scale", etc.</param>
		/// <param name="values">A string list of key values for the keys. Keys with no arguments must have empty strings passed as their values.</param>
		/// <param name="executeOnShow">Execute the query immediately upon Load, which is when the form is first shown.</param>
		public AstraCarta(ArrayList keys, ArrayList values, bool executeOnShow)
		{
			InitializeComponent();

			OPENFROMARGS = true;
			EXECUTEONSHOW = executeOnShow;

			int n;

			n = keys.IndexOf("-ra");
			if (n != -1)
				RATextBox.Text = (string)values[n];

			n = keys.IndexOf("-dec");
			if (n != -1)
				DecTextBox.Text = (string)values[n];

			n = keys.IndexOf("-scale");
			if (n != -1)
				ScaleTextBox.Text = (string)values[n];

			n = keys.IndexOf("-pixwidth");
			if (n != -1)
				WidthTextBox.Text = (string)values[n];

			n = keys.IndexOf("-pixheight");
			if (n != -1)
				HeightTextBox.Text = (string)values[n];

			BufferTextBox.Text = "";
			n = keys.IndexOf("-buffer");
			if (n != -1)
				BufferTextBox.Text = (string)values[n];

			RAOffsetTextBox.Text = "";
			n = keys.IndexOf("-offsetra");
			if (n != -1)
				RAOffsetTextBox.Text = (string)values[n];

			DecOffsetTextBox.Text = "";
			n = keys.IndexOf("-offsetdec");
			if (n != -1)
				DecOffsetTextBox.Text = (string)values[n];

			NameTextBox.Text = "";
			n = keys.IndexOf("-outname");
			if (n != -1)
				NameTextBox.Text = string.Join("_", ((string)values[n]).Split(Path.GetInvalidFileNameChars())) + "_AstraCarta";

			CatalogueDrop.SelectedIndex = 0;//GaiaDR3
			n = keys.IndexOf("-catalogue");
			if (n != -1)
				if ((string)values[n] == "GaiaDR3")
					CatalogueDrop.SelectedIndex = 0;
				else
					throw new Exception("Catalogue: '" + (string)values[n] + "' not recognized...");

			FilterDrop.SelectedIndex = 1;//GaiaDR3 g
			n = keys.IndexOf("-filter");
			if (n != -1)
				if (CatalogueDrop.SelectedIndex == 0)//gaiadr3
				{
					if ((string)values[n] == "bp")
						FilterDrop.SelectedIndex = 0;
					else if ((string)values[n] == "g")
						FilterDrop.SelectedIndex = 1;
					else if ((string)values[n] == "rp")
						FilterDrop.SelectedIndex = 2;
					else
						throw new Exception("Filter: '" + (string)values[n] + "' not recognized for catalogue '" + CatalogueDrop.SelectedItem.ToString() + "'");
				}
				else
					throw new Exception("Filter: '" + (string)values[n] + "' not recognized...");

			MagLimitTextBox.Text = "";
			n = keys.IndexOf("-maglimit");
			if (n != -1)
				MagLimitTextBox.Text = (string)values[n];

			ShapeDrop.SelectedIndex = 0;
			n = keys.IndexOf("-shape");
			if (n != -1)
				if ((string)values[n] == "circle")
					ShapeDrop.SelectedIndex = 1;
				else if ((string)values[n] == "rectangle" || (string)values[n] == "square")
					ShapeDrop.SelectedIndex = 0;
				else
					throw new Exception("Shape: '" + (string)values[n] + "' not recognized.");

			RotationTextBox.Text = "";
			n = keys.IndexOf("-rotation");
			if (n != -1)
				RotationTextBox.Text = (string)values[n];

			NQueryTextBox.Text = "500";
			n = keys.IndexOf("-nquery");
			if (n != -1)
				NQueryTextBox.Text = (string)values[n];

			PMEpochTextBox.Text = "";
			n = keys.IndexOf("-pmepoch");
			if (n != -1)
				PMEpochTextBox.Text = (string)values[n];

			PMLimitTextBox.Text = "";
			n = keys.IndexOf("-pmlimit");
			if (n != -1)
				PMLimitTextBox.Text = (string)values[n];

			DirectoryTextBox.Text = "";
			n = keys.IndexOf("-outdir");
			if (n != -1)
				DirectoryTextBox.Text = (string)values[n];

			EntriesTextBox.Text = "";
			n = keys.IndexOf("-entries");
			if (n != -1)
				EntriesTextBox.Text = (string)values[n];

			SaveTableChck.Checked = true;
			n = keys.IndexOf("-notable");
			if (n != -1)
				SaveTableChck.Checked = false;

			FITSTableChck.Checked = false;
			n = keys.IndexOf("-fitsout");
			if (n != -1)
				FITSTableChck.Checked = true;

			ShowImageChck.Checked = false;
			n = keys.IndexOf("-imageshow");
			if (n != -1)
				ShowImageChck.Checked = true;

			SaveImageChck.Checked = false;
			n = keys.IndexOf("-outimage");
			if (n != -1)
				SaveImageChck.Checked = true;

			ForceNewChck.Checked = false;
			n = keys.IndexOf("-forcenew");
			if (n != -1)
				ForceNewChck.Checked = true;

			RemoveRawQueryChck.Checked = false;
			n = keys.IndexOf("-rmvrawquery");
			if (n != -1)
				RemoveRawQueryChck.Checked = true;

			SilentChck.Checked = false;
			n = keys.IndexOf("-silent");
			if (n != -1)
				SilentChck.Checked = true;

			OverwriteChck.Checked = false;
			n = keys.IndexOf("-overwrite");
			if (n != -1)
				OverwriteChck.Checked = true;
		}

		private void ExecuteBtn_Click(object sender, EventArgs e)
		{
			if (ExecuteBtn.Text.Contains("Execute"))
			{
				CANCELLED = false;
				ExecuteBtn.Text = "Cancel";
			}
			else
			{
				CANCELLED = true;
				ExecuteBtn.Text = "Execute";
				ExecuteBtn.Enabled = false;
				PROC.Kill();
				PROC.Close();
				BGWrkr.CancelAsync();

				return;
			}
			ERROR = false;

			string argstring = "";// = String.Format("-ra {0} -dec {1} -scale {2} -pixwidth {3} -pixheight {4} -shape {5} -buffer {6} -outdir {7} -outname {8} -filter {9} -nquery {10} -fitsout", ra_deg, dec_deg, scale, pixwidth, pixheight, "\"" + shape + "\"", buffer, "\"" + outdir + "\"", "\"" + outname + "\"", "\"" + filter + "\"", nquery);
			argstring += String.Format("-ra {0} ", RATextBox.Text);
			argstring += String.Format("-dec {0} ", DecTextBox.Text);
			argstring += String.Format("-scale {0} ", ScaleTextBox.Text);
			argstring += String.Format("-pixwidth {0} ", WidthTextBox.Text);
			argstring += String.Format("-pixheight {0} ", HeightTextBox.Text);
			if (BufferTextBox.Text != "")
				argstring += String.Format("-buffer {0} ", BufferTextBox.Text);
			if (RAOffsetTextBox.Text != "")
				argstring += String.Format("-offsetra {0} ", RAOffsetTextBox.Text);
			if (DecOffsetTextBox.Text != "")
				argstring += String.Format("-offsetdec {0} ", DecOffsetTextBox.Text);
			if (NameTextBox.Text != "")
				argstring += String.Format("-outname {0} ", "\"" + NameTextBox.Text + "\"");
			argstring += String.Format("-catalogue {0} ", "\"" + CatalogueDrop.SelectedItem.ToString() + "\"");
			argstring += String.Format("-filter {0} ", "\"" + FilterDrop.SelectedItem.ToString() + "\"");
			if (MagLimitTextBox.Text != "")
				argstring += String.Format("-maglimit {0} ", MagLimitTextBox.Text);
			argstring += String.Format("-shape {0} ", "\"" + ShapeDrop.SelectedItem.ToString() + "\"");
			if (ShapeDrop.SelectedItem.ToString() == "rectangle")
				if (RotationTextBox.Text != "")
					argstring += String.Format("-rotation {0} ", RotationTextBox.Text);
			argstring += String.Format("-nquery {0} ", NQueryTextBox.Text);
			if (PMEpochTextBox.Text != "")
				argstring += String.Format("-pmepoch {0} ", PMEpochTextBox.Text);
			if (PMLimitTextBox.Text != "")
				argstring += String.Format("-pmlimit {0} ", PMLimitTextBox.Text);
			if (DirectoryTextBox.Text != "")
				argstring += String.Format("-outdir {0} ", "\"" + DirectoryTextBox.Text + "\"");
			argstring += String.Format("-entries {0} ", "\"" + EntriesTextBox.Text + "\"");
			if (!SaveTableChck.Checked)
				argstring += String.Format("-notable ");
			if (FITSTableChck.Checked)
				argstring += String.Format("-fitsout ");
			if (ShowImageChck.Checked)
				argstring += String.Format("-imageshow ");
			if (SaveImageChck.Checked)
				argstring += String.Format("-outimage ");
			if (ForceNewChck.Checked)
				argstring += "-forcenew ";
			if (RemoveRawQueryChck.Checked)
				argstring += "-rmvrawquery ";
			if (SilentChck.Checked)
				argstring += "-silent ";
			if (OverwriteChck.Checked)
				argstring += "-overwrite ";

			argstring = argstring.TrimEnd();
			PSI = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + "astracarta " + argstring);
			PSI.UseShellExecute = false;
			PSI.CreateNoWindow = true;
			PSI.RedirectStandardError = true;
			PSI.RedirectStandardOutput = true;
			PROC = new System.Diagnostics.Process();

			MessageTextBox.Clear();
			MessageTextBox.AppendText("Calling up AstraCarta. Please wait a moment...\r\n");
			BGWrkr.RunWorkerAsync();
		}

		private void BGWrkr_DoWork(object sender, DoWorkEventArgs e)
		{
			PROC = System.Diagnostics.Process.Start(PSI);
			PROC.WaitForExit();

			if (CANCELLED)
				return;

			OUTFILE = PROC.StandardOutput.ReadToEnd();
			ERR = PROC.StandardError.ReadToEnd();

			if (OUTFILE == "")
				if (ERR != "")
				{
					BGWrkr.ReportProgress(0, "Error: " + ERR + "\r\n");
					if (ERR.Contains("\'astracarta\' is not recognized"))
					{
						BGWrkr.ReportProgress(0, "Installing AstraCarta..." + "\r\n");
						BGWrkr.ReportProgress(0, ">>pip install astracarta" + "\r\n");
						BGWrkr.ReportProgress(0, ">>Please wait one minute..." + "\r\n");
						PSI = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + "pip install astracarta");
						PROC = System.Diagnostics.Process.Start(PSI);
						PROC.WaitForExit();

						if (CANCELLED)
							return;

						string appdatapypath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Python");
						string[] astracartapths = Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Python"), "AstraCarta.exe", SearchOption.AllDirectories);
						string acpath = "";
						bool pathchange = false;
						var oldValue = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
						if (astracartapths.Length > 0)
						{
							acpath = Directory.GetParent(astracartapths[0]).FullName;
							if (!oldValue.Contains(acpath))
							{
								BGWrkr.ReportProgress(0, "Adding to Path: " + acpath + "\r\n");
								var newValue = oldValue + ";" + acpath;
								Environment.SetEnvironmentVariable("Path", newValue, EnvironmentVariableTarget.User);
								oldValue = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
								pathchange = true;
							}
						}

						if (CANCELLED)
							return;

						PSI = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + "python -c \"import os, sys; print(os.path.dirname(sys.executable))\"");
						PSI.UseShellExecute = false;
						PSI.CreateNoWindow = true;
						PSI.RedirectStandardError = true;
						PSI.RedirectStandardOutput = true;
						PROC = new System.Diagnostics.Process();
						PROC = System.Diagnostics.Process.Start(PSI);
						PROC.WaitForExit();
						acpath = Path.Combine(PROC.StandardOutput.ReadToEnd().Trim(), "Scripts");
						oldValue = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
						if (!oldValue.Contains(acpath))
						{
							BGWrkr.ReportProgress(0, "Adding to Path: " + acpath + "\r\n");
							var newValue = oldValue + ";" + acpath;
							Environment.SetEnvironmentVariable("Path", newValue, EnvironmentVariableTarget.User);
							oldValue = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
							pathchange = true;
						}
						if (pathchange)
						{
							MessageBox.Show("WE HAD TO UPDATE THE ENVIRONMENT PATH VARIABLE FOR PYTHON.\r\n\r\nPLEASE RESTART");
							this.Close();
							return;
						}

						if (CANCELLED)
							return;

						PERFORMEXECUTE = true;
						return;
					}
					else
					{
						CloseOnCompleteChck.Checked = false;
						ERROR = true;
						BGWrkr.ReportProgress(0, "ERROR (A): Unhandled error. Please see the message box..." + "\r\n");
						return;
					}
				}
				else
				{
					CloseOnCompleteChck.Checked = false;
					ERROR = true;
					BGWrkr.ReportProgress(0, "ERROR (B): Unknown error. Is Python >= v. 3.10 installed?" + "\r\n");
					return;
				}
			else
			{
				BGWrkr.ReportProgress(0, OUTFILE + "\r\n");
			}		
		}

		private void BGWrkr_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			MessageTextBox.AppendText((string)e.UserState);
		}

		private void BGWrkr_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (ExecuteBtn.Text.Contains("Cancel"))
				ExecuteBtn.Text = "Execute";
			if (!ExecuteBtn.Enabled)
				ExecuteBtn.Enabled = true;

			if (CANCELLED)
			{
				PERFORMEXECUTE = false;
				MessageTextBox.AppendText("Cancelled...\r\n");
				return;
			}

			if (ERROR)
			{
				PERFORMEXECUTE = false;
				ERROR = false;
				return;
			}

			if (PERFORMEXECUTE)
			{
				PERFORMEXECUTE = false;

				ExecuteBtn.PerformClick();
				return;
			}

			if (SaveTableChck.Checked)
			{
				try
				{
					int c = 0;
					string test = OUTFILE.Trim();
					while (!File.Exists(test))
					{
						c++;
						test = OUTFILE.Substring(c);

						if (test == "")
						{
							MessageTextBox.AppendText("File name error..." + "\r\n");
							break;
						}
					}
					OUTFILE = test.Trim();
				}
				catch
				{
					CloseOnCompleteChck.Checked = false;
					MessageTextBox.AppendText("File name error:" + "\r\n" + OUTFILE);
				}
			}

			if (CloseOnCompleteChck.Checked)
				this.Close();
		}

		private void WorkTimer_Tick(object sender, EventArgs e)
		{
			BGWrkr.ReportProgress(0);
		}

		private void AstraCarta_Load(object sender, EventArgs e)
		{
			VersionBGWrkr.RunWorkerAsync("versioncheck");

			if (!OPENFROMARGS)
			{
				CatalogueDrop.SelectedIndex = Convert.ToInt32(REG.GetReg("AstraCarta", "CatalogueDrop"));
				ShapeDrop.SelectedIndex = Convert.ToInt32(REG.GetReg("AstraCarta", "ShapeDrop"));
				SaveTableChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "SaveTableChck"));
				FITSTableChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "FITSTableChck"));
				ShowImageChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "ShowImageChck"));
				SaveImageChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "SaveImageChck"));
				ForceNewChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "ForceNewChck"));
				RemoveRawQueryChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "RemoveRawQueryChck"));
				SilentChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "SilentChck"));
				OverwriteChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "OverwriteChck"));
				CloseOnCompleteChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "CloseOnCompleteChck"));
				DirectoryTextBox.Text = (string)REG.GetReg("AstraCarta", "DirectoryTextBox");
				RATextBox.Text = (string)REG.GetReg("AstraCarta", "RATextBox");
				DecTextBox.Text = (string)REG.GetReg("AstraCarta", "DecTextBox");
				ScaleTextBox.Text = (string)REG.GetReg("AstraCarta", "ScaleTextBox");
				WidthTextBox.Text = (string)REG.GetReg("AstraCarta", "WidthTextBox");
				HeightTextBox.Text = (string)REG.GetReg("AstraCarta", "HeightTextBox");
				BufferTextBox.Text = (string)REG.GetReg("AstraCarta", "BufferTextBox");
				MagLimitTextBox.Text = (string)REG.GetReg("AstraCarta", "MagLimitTextBox");
				RotationTextBox.Text = (string)REG.GetReg("AstraCarta", "RotationTextBox");
				PMEpochTextBox.Text = (string)REG.GetReg("AstraCarta", "PMEpochTextBox");
				PMLimitTextBox.Text = (string)REG.GetReg("AstraCarta", "PMLimitTextBox");
				NQueryTextBox.Text = (string)REG.GetReg("AstraCarta", "NQueryTextBox");
			}

			if (EXECUTEONSHOW)
				ExecuteBtn.PerformClick();
		}

		private void CatalogueDrop_SelectedIndexChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "CatalogueDrop", CatalogueDrop.SelectedIndex);

			if (CatalogueDrop.SelectedItem.ToString() == "GaiaDR3")
			{
				//EntriesTextBox.Text = "ref_epoch, ra, ra_error, dec, dec_error, pmra, pmra_error, pmdec, pmdec_error, pm, phot_bp_mean_mag, phot_g_mean_mag, phot_rp_mean_mag";
				FilterDrop.Items.Clear();
				FilterDrop.Items.AddRange(new string[] { "bp", "g", "rp" });
			}

			FilterDrop.SelectedIndex = Convert.ToInt32(REG.GetReg("AstraCarta", "FilterDrop" + CatalogueDrop.SelectedItem.ToString()));
		}

		private void FilterDrop_SelectedIndexChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "FilterDrop" + CatalogueDrop.SelectedItem.ToString(), FilterDrop.SelectedIndex);
		}

		private void ShapeDrop_SelectedIndexChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "ShapeDrop", ShapeDrop.SelectedIndex);
		}

		private void SaveTableChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "SaveTableChck", SaveTableChck.Checked);

			if (SaveTableChck.Checked)
				FITSTableChck.Enabled = true;
			else
				FITSTableChck.Enabled = false;

			if (!SaveTableChck.Checked && !SaveImageChck.Checked)
				OverwriteChck.Enabled = false;
			else
				OverwriteChck.Enabled = true;
		}

		private void FITSTableChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "FITSTableChck", FITSTableChck.Checked);
		}

		private void ShowImageChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "ShowImageChck", ShowImageChck.Checked);
		}

		private void SaveImageChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "SaveImageChck", SaveImageChck.Checked);

			if (!SaveTableChck.Checked && !SaveImageChck.Checked)
				OverwriteChck.Enabled = false;
			else
				OverwriteChck.Enabled = true;
		}

		private void ForceNewChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "ForceNewChck", ForceNewChck.Checked);
		}

		private void RemoveRawQueryChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "RemoveRawQueryChck", RemoveRawQueryChck.Checked);
		}

		private void SilentChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "SilentChck", SilentChck.Checked);
		}

		private void OverwriteChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "OverwriteChck", OverwriteChck.Checked);
		}

		private void DirectoryTextBox_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog fbd = new FolderBrowserDialog();
			fbd.SelectedPath = (string)REG.GetReg("AstraCarta", "DirectoryTextBox");
			fbd.Description = "Please select the directory for saving outputs.";
			if (fbd.ShowDialog() == DialogResult.Cancel)
				return;
			DirectoryTextBox.Text = fbd.SelectedPath;
			REG.SetReg("AstraCarta", "DirectoryTextBox", DirectoryTextBox.Text);
		}

		private void CloseOnCompleteChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "CloseOnCompleteChck", CloseOnCompleteChck.Checked);
		}

		private void NumericTextBox_TextChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", ((TextBox)sender).Name, ((TextBox)sender).Text);			
		}

		private void VersionBGWrkr_DoWork(object sender, DoWorkEventArgs e)
		{
			if ((string)e.Argument == "versioncheck")
			{
				System.Diagnostics.ProcessStartInfo verspsi = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + "py --version");
				verspsi.UseShellExecute = false;
				verspsi.CreateNoWindow = true;
				verspsi.RedirectStandardError = true;
				verspsi.RedirectStandardOutput = true;
				System.Diagnostics.Process versproc = new System.Diagnostics.Process();
				versproc = System.Diagnostics.Process.Start(verspsi);
				versproc.WaitForExit();
				string versout = versproc.StandardOutput.ReadToEnd();
				if (versout.Trim() == "")
				{
					ERROR = true;
					MessageBox.Show("No Python found. Please install Python version 3.10 or greater on your machine first. Thank you!" + "\r\n\r\nMAKE SURE to CHECK \"Add Python 3.10 to PATH;\r\nCustomize Installation->Next\r\nCHECK Install for all users.", "Error...");
					this.DialogResult = DialogResult.Cancel;
					this.Close();
					return;
				}
				else
				{
					bool update = false;
					string[] splitvers = versout.Replace("Python ","").Trim().Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
					if (Convert.ToInt32(splitvers[0]) < 3)
						update = true;
					else if (Convert.ToInt32(splitvers[0]) == 3 && Convert.ToInt32(splitvers[1]) < 10)
						update = true;
					if (update)
					{
						ERROR = true;
						MessageBox.Show("Please install Python version 3.10 or greater on your machine first. Thank you!" + "\r\n\r\nCurrent Version: Python " + splitvers[0] + "." + splitvers[1] + "\r\n\r\nMAKE SURE to CHECK \"Add Python 3.10 to PATH;\r\nCustomize Installation->Next\r\nCHECK Install for all users.", "Error...");
						this.DialogResult = DialogResult.Cancel;
						this.Close();
						return;
					}
				}

				verspsi = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + "pip index versions astracarta");
				verspsi.UseShellExecute = false;
				verspsi.CreateNoWindow = true;
				verspsi.RedirectStandardError = true;
				verspsi.RedirectStandardOutput = true;
				versproc = new System.Diagnostics.Process();
				versproc = System.Diagnostics.Process.Start(verspsi);
				versproc.WaitForExit();
				versout = versproc.StandardOutput.ReadToEnd();

				if (versout.IndexOf("INSTALLED:") == -1)
				{
					ERROR = true;
					return;
				}

				CURVERS = versout.Substring(versout.IndexOf("INSTALLED:"), versout.IndexOf("LATEST:") - versout.IndexOf("INSTALLED:")).Replace("INSTALLED:", "").Trim();
				LATVERS = versout.Substring(versout.IndexOf("LATEST:")).Replace("LATEST:", "").Trim();

				if (LATVERS != CURVERS)
				{
					CloseOnCompleteChck.Checked = false;
					UpdatMenuBtn.Visible = true;
					UpdatMenuBtn.BackColor = System.Drawing.Color.PaleVioletRed;
					Refresh();
				}
			}

			if ((string)e.Argument == "update")
			{
				if (BGWrkr.IsBusy)
				{
					MessageBox.Show("Please wait until AstraCarta is finished its query process, and close any waiting plot windows if open...", "Wait...");
					return;
				}

				if (MessageBox.Show("Update AstraCarta from " + CURVERS + " to " + LATVERS + "?", "Update...", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
					return;

				MessageTextBox.Clear();
				MessageTextBox.AppendText("Updating AstraCarta. Please wait a moment..." + "\r\n");
				MessageTextBox.AppendText(">>pip install astracarta --upgrade" + "\r\n");
				System.Diagnostics.ProcessStartInfo verspsi = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + "pip install astracarta --upgrade");
				System.Diagnostics.Process versproc = new System.Diagnostics.Process();
				versproc = System.Diagnostics.Process.Start(verspsi);
				versproc.WaitForExit();
				UpdatMenuBtn.BackColor = System.Drawing.SystemColors.Control;
				UpdatMenuBtn.Visible = false;
				MessageTextBox.AppendText("AstraCarta updated to version: " + LATVERS + "\r\n");
			}
		}

		private void UpdatMenuBtn_Click(object sender, EventArgs e)
		{
			VersionBGWrkr.RunWorkerAsync("update");
		}

		private void AstraCarta_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
				this.Close();
		}
	}
}
