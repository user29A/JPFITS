using System;
using System.Windows.Forms;
using System.IO;
#nullable enable

namespace JPFITS
{
	public partial class FITSImageExtensionsSaver : Form
	{
		public JPFITS.FITSImageSet IMAGESET;
		public string FILENAME = "";
		public string[]? EXTENSIONNAMES;
		public DiskPrecision[]? PRECISIONTYPECODES;
		public JPFITS.FITSHeader? HEADER;

		public FITSImageExtensionsSaver(JPFITS.FITSImageSet imageSet)
		{
			InitializeComponent();

			HEADER = null;
			IMAGESET = imageSet;
			extensionsGridView.RowCount = IMAGESET.Count;
			string extname;
			int c = 1;
			for (int i = 0; i < IMAGESET.Count; i++)
			{
				extname = IMAGESET[i].Header.GetKeyValue("EXTNAME");
				if (extname != "")
					extensionsGridView[0, i].Value = extname;
				else
				{
					extensionsGridView[0, i].Value = "EXT_" + c.ToString("000000");
					c++;
				}

				int bzero = 0;
				try
				{
					bzero = Convert.ToInt32(IMAGESET[i].Header.GetKeyValue("BZERO"));
				}
				catch { }

				extensionsGridView.Rows[i].Cells[1].Value = BITPIXtoString(Convert.ToInt32(IMAGESET[i].Header.GetKeyValue("BITPIX")), bzero);
			}
		}

		private string BITPIXtoString(int bitPix, int bZero)
		{
			switch (bitPix)
			{
				case (8):
				{
					if (bZero == -128)
						return "sbyte";
					else
						return "byte";
				}

				case (16):
				{
					if (bZero == 0)
						return "int16";
					else
						return "uint16";
				}

				case (32):
				{
					if (bZero == 0)
						return "int32";
					else
						return "uint32";
				}

				case (64):
				{
					if (bZero == 0)
						return "int64";
					else
						return "uint64";
				}

				case (-32):
				{
					return "float";
				}

				case (-64):
				{
					return "double";
				}
			}

			return "double";
		}

		private DiskPrecision BITPIXStringToTYPECODE(string bitpixstr)
		{
			if (bitpixstr == "boolean")
				return DiskPrecision.Boolean;
			if (bitpixstr == "sbyte")
				return DiskPrecision.SByte;
			if (bitpixstr == "byte")
				return DiskPrecision.Byte;
			if (bitpixstr == "int16")
				return DiskPrecision.Int16;
			else if (bitpixstr == "uint16")
				return DiskPrecision.UInt16;
			if (bitpixstr == "int32")
				return DiskPrecision.Int32;
			else if (bitpixstr == "uint32")
				return DiskPrecision.UInt32;
			if (bitpixstr == "int64")
				return DiskPrecision.Int64;
			else if (bitpixstr == "uint64")
				return DiskPrecision.UInt64;
			else if (bitpixstr == "float")
				return DiskPrecision.Single;
			else if (bitpixstr == "double")
				return DiskPrecision.Double;
			else
				throw new Exception("Unsupported type at BITPIXStringToTYPECODE with '" + bitpixstr + "'");
		}

		private void FirstAsPrimaryChck_CheckedChanged(object sender, EventArgs e)
		{
			string extname;
			int c = 1, jjii;

			if (FirstAsPrimaryChck.Checked)
			{
				AppendIntoFileChck.Enabled = false;
				ViewEditPrimaryHeaderBtn.Enabled = false;

				extensionsGridView[0, 0].Value = "PRIMARY";

				jjii = 1;
			}
			else
			{
				AppendIntoFileChck.Enabled = true;
				ViewEditPrimaryHeaderBtn.Enabled = true;
				jjii = 0;
			}

			for (int i = jjii; i < IMAGESET.Count; i++)
			{
				extname = IMAGESET[i].Header.GetKeyValue("EXTNAME");
				if (extname != "")
					extensionsGridView[0, i].Value = extname;
				else
				{
					extensionsGridView[0, i].Value = "EXT_" + c.ToString("000000");
					c++;
				}
			}
		}

		private void AppendIntoFileChck_CheckedChanged(object sender, EventArgs e)
		{
			if (AppendIntoFileChck.Checked)
			{
				FirstAsPrimaryChck.Enabled = false;
				ViewEditPrimaryHeaderBtn.Enabled = false;
			}
			else
			{
				FirstAsPrimaryChck.Enabled = true;
				ViewEditPrimaryHeaderBtn.Enabled = true;
			}
		}

		private void ViewEditPrimaryHeaderBtn_Click(object sender, EventArgs e)
		{
			bool headerwasnull = HEADER == null;
			if (HEADER == null)
				HEADER = new FITSHeader(true, null);

			FITSHeaderViewer fhv = new FITSHeaderViewer(HEADER);
			if (fhv.ShowDialog() == DialogResult.Cancel)
			{
				if (headerwasnull)
					HEADER = null;
				return;
			}
			else
				HEADER = fhv.Header;
		}

		private void SaveBtn_Click(object sender, EventArgs e)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "FITS|*.fits;*.fit;*.fts|All|*.*";
			sfd.InitialDirectory = IMAGESET.GetCommonDirectory();
			bool appendtofile = AppendIntoFileChck.Checked;
			if (appendtofile)
				sfd.OverwritePrompt = false;
			if (sfd.ShowDialog() == DialogResult.Cancel)
				return;

			if (AppendIntoFileChck.Checked && !File.Exists(sfd.FileName))
				if (MessageBox.Show("Requested to save extensions into existing file, but specified file name does not exist. Write into a new file?", "Warning", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
					return;
				else
					appendtofile = false;

			FILENAME = sfd.FileName;
			EXTENSIONNAMES = new string[extensionsGridView.RowCount];
			PRECISIONTYPECODES = new DiskPrecision[extensionsGridView.RowCount];

			for (int i = 0; i < extensionsGridView.RowCount; i++)
			{
				EXTENSIONNAMES[i] = (string)extensionsGridView[0, i].Value;
				PRECISIONTYPECODES[i] = BITPIXStringToTYPECODE((string)extensionsGridView[1, i].Value);
			}

			if (FirstAsPrimaryChck.Checked)
				HEADER = null;

			this.Close();

			IMAGESET.WriteAsExtensions(FILENAME, appendtofile, FirstAsPrimaryChck.Checked, HEADER, EXTENSIONNAMES, PRECISIONTYPECODES);
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void ImageExtensionsSaver_MouseEnter(object sender, EventArgs e)
		{
			this.Activate();
		}

		private void GlobalPrecisionDrop_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (GlobalPrecisionDrop.SelectedIndex == -1)
				return;

			if ((string)GlobalPrecisionDrop.SelectedItem != "(reset)")
				for (int i = 0; i < IMAGESET.Count; i++)
					extensionsGridView.Rows[i].Cells[1].Value = GlobalPrecisionDrop.SelectedItem;
			else
			{
				for (int i = 0; i < IMAGESET.Count; i++)
				{
					int bzero = 0;
					try
					{
						bzero = Convert.ToInt32(IMAGESET[i].Header.GetKeyValue("BZERO"));
					}
					catch { }

					extensionsGridView.Rows[i].Cells[1].Value = BITPIXtoString(Convert.ToInt32(IMAGESET[i].Header.GetKeyValue("BITPIX")), bzero);
				}
				GlobalPrecisionDrop.SelectedIndex = -1;
			}
		}
	}
}

