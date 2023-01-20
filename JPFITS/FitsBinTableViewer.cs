using System;
using System.Collections;
using System.Windows.Forms;
#nullable enable

namespace JPFITS
{
	/// <summary>FitsExtensionTableViewer class for viewing any of the FITS binary table extensions which may exist in a file as a table.</summary>
	public partial class FitsBinTableViewer : Form
	{
		private string? FILENAME;
		private string? EXTENSIONNAME;
		private FITSBinTable? FITSBINTABLE;
		private bool HEADERFRONT = false;

		/// <summary>FitsExtensionTableViewer class constructor.</summary>
		/// <param name="FileName">The fill file name to open the extensions from.</param>
		public FitsBinTableViewer(string FileName)
		{
			InitializeComponent();
			OpenBINTABLE(FileName);
		}

		public FitsBinTableViewer()
		{
			InitializeComponent();
		}

		private void FileOpenMenu_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog
			{
				Filter = "FITS|*.fits;*.fit;*.fts|All Files|*.*",
				InitialDirectory = (String)REG.GetReg("JPFITS", "BinTableOpenFilesPath")
			};

			if (ofd.ShowDialog() == DialogResult.Cancel)
				return;

			XDrop.Items.Clear();
			YDrop.Items.Clear();

			int c = MenuChooseTableEntries.DropDownItems.Count;
			for (int i = 2; i < c; i++)
				MenuChooseTableEntries.DropDownItems.RemoveAt(2);

			MenuChooseTable.DropDownItems.Clear();

			OpenBINTABLE(ofd.FileName);
		}

		public void OpenBINTABLE(string FileName)
		{
			try
			{
				FILENAME = FileName;
				REG.SetReg("JPFITS", "BinTableOpenFilesPath", FileName.Substring(0, FileName.LastIndexOf("\\")));
				string file = FILENAME.Substring(FILENAME.LastIndexOf("\\"));
				this.Text = file.Substring(1);
				string[] extensions = FITSBinTable.GetAllExtensionNames(FileName);				

				for (int i = 0; i < extensions.Length; i++)
				{
					if (extensions[i] == "")
						MenuChooseTable.DropDownItems.Add("UNNAMED");
					else
						MenuChooseTable.DropDownItems.Add(extensions[i]);

					((ToolStripMenuItem)MenuChooseTable.DropDownItems[MenuChooseTable.DropDownItems.Count - 1]).CheckOnClick = true;
					this.MenuChooseTable.DropDownItems[MenuChooseTable.DropDownItems.Count - 1].Click += new System.EventHandler(MenuChooseTable_Click);
					((ToolStripMenuItem)this.MenuChooseTable.DropDownItems[MenuChooseTable.DropDownItems.Count - 1]).CheckedChanged += new System.EventHandler(MenuChooseTable_CheckedChanged);
				}

				if (extensions.Length > 1)
					MenuChooseTable.ShowDropDown();
				else
					MenuChooseTable.DropDownItems[0].PerformClick();
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Data + "	" + e.InnerException + "	" + e.Message + "	" + e.Source + "	" + e.StackTrace + "	" + e.TargetSite);
			}
		}

		private void MenuChooseTable_CheckedChanged(object sender, EventArgs e)
		{
			if (((ToolStripMenuItem)sender).Checked == false)
				return;

			if (HEADERFRONT)
			{
				HEADERFRONT = !HEADERFRONT;
				ViewHeaderMenu.PerformClick();
			}

			EXTENSIONNAME = ((ToolStripMenuItem)sender).Text;
			if (EXTENSIONNAME.Contains("UNNAMED"))
				EXTENSIONNAME = "";

			XDrop.Items.Clear();
			YDrop.Items.Clear();
			int c = MenuChooseTableEntries.DropDownItems.Count;
			for (int i = 2; i < c; i++)
				MenuChooseTableEntries.DropDownItems.RemoveAt(2);

			FITSBINTABLE = new FITSBinTable(FILENAME, EXTENSIONNAME);
			string[] ttypes = FITSBINTABLE.TableDataLabelTTYPEs;

			for (int i = 0; i < ttypes.Length; i++)
			{
				MenuChooseTableEntries.DropDownItems.Add(ttypes[i]);
				((ToolStripMenuItem)MenuChooseTableEntries.DropDownItems[MenuChooseTableEntries.DropDownItems.Count - 1]).CheckOnClick = true;
				((ToolStripMenuItem)MenuChooseTableEntries.DropDownItems[MenuChooseTableEntries.DropDownItems.Count - 1]).Checked = true;
				this.MenuChooseTableEntries.DropDownItems[MenuChooseTableEntries.DropDownItems.Count - 1].Click += new System.EventHandler(MenuChooseTableEntries_Click);
				((ToolStripMenuItem)this.MenuChooseTableEntries.DropDownItems[MenuChooseTableEntries.DropDownItems.Count - 1]).CheckedChanged += new System.EventHandler(ViewAllChck_CheckedChanged);
			}

			XDrop.Items.AddRange(ttypes);
			YDrop.Items.AddRange(ttypes);

			ExtensionTableGrid.Columns.Clear();
			ExtensionTableGrid.Rows.Clear();
			ExtensionTableGrid.ColumnCount = ttypes.Length;
			ExtensionTableGrid.RowCount = FITSBINTABLE.Naxis2 + 1;

			for (int i = 0; i < ttypes.Length; i++)
				ExtensionTableGrid.Columns[i].HeaderText = ttypes[i];

			ExtensionTableGrid.Refresh();
		}

		private void FitsExtensionTableViewer_SizeChanged(object sender, EventArgs e)
		{
			REG.SetReg("JPChart", this.Text + "FitsTableLeft", this.Left);
			REG.SetReg("JPChart", this.Text + "FitsTableTop", this.Top);
			REG.SetReg("JPChart", this.Text + "FitsTableWidth", this.Width);
			REG.SetReg("JPChart", this.Text + "FitsTableHeight", this.Height);
		}

		private void FitsExtensionTableViewer_LocationChanged(object sender, EventArgs e)
		{
			REG.SetReg("JPChart", this.Text + "FitsTableLeft", this.Left);
			REG.SetReg("JPChart", this.Text + "FitsTableTop", this.Top);
			REG.SetReg("JPChart", this.Text + "FitsTableWidth", this.Width);
			REG.SetReg("JPChart", this.Text + "FitsTableHeight", this.Height);
		}

		private void button1_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void ExtensionTableGrid_RowsAdded(object sender, System.Windows.Forms.DataGridViewRowsAddedEventArgs e)
		{
			try
			{
				for (int i = 0; i < 50; i++)
					ExtensionTableGrid.Rows[e.RowIndex + i].HeaderCell.Value = (e.RowIndex + i + 1).ToString();
			}
			catch { }
		}

		private void ExtensionTableGrid_Scroll(object sender, System.Windows.Forms.ScrollEventArgs e)
		{
			try
			{
				if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
					for (int i = 0; i < 50; i++)
						ExtensionTableGrid.Rows[e.NewValue + i].HeaderCell.Value = (e.NewValue + i + 1).ToString();
			}
			catch { }
		}

		private void ExtensionTableGrid_CellValueNeeded(object sender, System.Windows.Forms.DataGridViewCellValueEventArgs e)
		{
			try
			{
				e.Value = FITSBINTABLE.GetTTypeEntryRow(ExtensionTableGrid.Columns[e.ColumnIndex].HeaderText, e.RowIndex);
			}
			catch {	}
		}

		private void MenuChooseTable_Click(object sender, EventArgs e)
		{
			MenuChooseTable.ShowDropDown();

			for (int i = 0; i < MenuChooseTable.DropDownItems.Count; i++)
				if (MenuChooseTable.DropDownItems[i].Text != ((ToolStripMenuItem)sender).Text)
					((ToolStripMenuItem)MenuChooseTable.DropDownItems[i]).Checked = false;

			if (((ToolStripMenuItem)sender).Checked == false)
				((ToolStripMenuItem)sender).Checked = true;
		}

		private void FitsExtensionTableViewer_Load(object sender, EventArgs e)
		{
			this.Left = (int)REG.GetReg("JPChart", "FitsTableLeft");
			this.Top = (int)REG.GetReg("JPChart", "FitsTableTop");
			this.Width = (int)REG.GetReg("JPChart", "FitsTableWidth");
			this.Height = (int)REG.GetReg("JPChart", "FitsTableHeight");
		}
		
		private void FitsExtensionTableViewer_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
		{
			REG.SetReg("JPChart", "FitsTableLeft", this.Left);
			REG.SetReg("JPChart", "FitsTableTop", this.Top);
			REG.SetReg("JPChart", "FitsTableWidth", this.Width);
			REG.SetReg("JPChart", "FitsTableHeight", this.Height);
		}

		private void FitsExtensionTableViewer_ResizeBegin(object sender, EventArgs e)
		{
			this.SuspendLayout();
		}

		private void FitsExtensionTableViewer_ResizeEnd(object sender, EventArgs e)
		{
			this.ResumeLayout();
		}

		private void ViewAllChck_Click(object sender, EventArgs e)
		{
			this.ExtensionTableGrid.SuspendLayout();

			if (ViewAllChck.Text == "View None")
			{
				ViewAllChck.Text = "View All";
				for (int i = 2; i < MenuChooseTableEntries.DropDownItems.Count; i++)
				{
					MenuChooseTableEntries.DropDownItems[i].Tag = "ViewAll";
					((ToolStripMenuItem)MenuChooseTableEntries.DropDownItems[i]).Checked = false;
					MenuChooseTableEntries.DropDownItems[i].Tag = null;
				}
				ExtensionTableGrid.Columns.Clear();

				ExtensionTableGrid.ColumnCount = MenuChooseTableEntries.DropDownItems.Count - 2;

				for (int i = 0; i < ExtensionTableGrid.ColumnCount; i++)
					ExtensionTableGrid.Columns[i].HeaderText = MenuChooseTableEntries.DropDownItems[i + 2].Text;
			}
			else
			{
				ViewAllChck.Text = "View None";
				for (int i = 2; i < MenuChooseTableEntries.DropDownItems.Count; i++)
				{
					MenuChooseTableEntries.DropDownItems[i].Tag = "ViewAll";
					((ToolStripMenuItem)MenuChooseTableEntries.DropDownItems[i]).Checked = true;
					MenuChooseTableEntries.DropDownItems[i].Tag = null;
				}

				ExtensionTableGrid.RowCount = FITSBINTABLE.Naxis2 + 1;
			}

			ExtensionTableGrid.Refresh();

			this.ExtensionTableGrid.ResumeLayout();
		}

		private void MenuChooseTableEntries_Click(object sender, EventArgs e)
		{
			MenuChooseTableEntries.ShowDropDown();
		}

		private void ViewAllChck_CheckedChanged(object sender, EventArgs e)
		{
			if (((ToolStripMenuItem)sender).Text.Contains("View"))
				return;

			if ((String)(((ToolStripMenuItem)sender).Tag) == "ViewAll")
				return;	

			this.ExtensionTableGrid.SuspendLayout();

			ArrayList checkd = new ArrayList();

			for (int i = 2; i < MenuChooseTableEntries.DropDownItems.Count; i++)
				if (((ToolStripMenuItem)MenuChooseTableEntries.DropDownItems[i]).Checked)
					checkd.Add(i);

			ExtensionTableGrid.Columns.Clear();
			ExtensionTableGrid.ColumnCount = checkd.Count;
			ExtensionTableGrid.RowCount = FITSBINTABLE.Naxis2 + 1;

			for (int i = 0; i < checkd.Count; i++)
				ExtensionTableGrid.Columns[i].HeaderText = MenuChooseTableEntries.DropDownItems[(int)checkd[i]].Text;

			ExtensionTableGrid.Refresh();

			this.ExtensionTableGrid.ResumeLayout();
		}

		private void PlotMenuItem_Click(object sender, EventArgs e)
		{
			//Plotter plot = new Plotter();
			//double[] x = new double[(((double[])DATATABLE[YDrop.SelectedIndex]).Length)];
			//double[] y = new double[(((double[])DATATABLE[YDrop.SelectedIndex]).Length)];

			//int xind = XDrop.SelectedIndex;
			//int yind = YDrop.SelectedIndex;
			//if (yind == -1)
			//	return;

			//for (int i = 0; i < x.Length; i++)
			//{
			//	if (xind == -1)
			//		x[i] = i;
			//	else
			//		x[i] = ((double[])DATATABLE[xind])[i];
			//	y[i] = ((double[])DATATABLE[yind])[i];
			//}

			//String xlabel;
			//if (xind == -1)
			//	xlabel = "index";
			//else
			//	xlabel = ExtensionTableGrid.Columns[xind].HeaderText;
			//String ylabel = ExtensionTableGrid.Columns[yind].HeaderText;
			//String title = ylabel;
			//if (xind != -1)
			//	title += " vs. " + xlabel;

			//plot.jpChart1.PlotXYData(x, y, title, xlabel, ylabel, System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastPoint, title.Replace(" ", ""), null);
			//plot.Text = title;
			//plot.Show();
		}

		private void ViewHeaderMenu_Click(object sender, EventArgs e)
		{
			FITSHeaderViewer fhv = new FITSHeaderViewer(new FITSHeader(FITSBINTABLE.Header));
			fhv.Show();
		}

		private void XDrop_KeyDown(object sender, KeyEventArgs e)
		{
			//THIS IS WHY THE CHECKBOX WAS THERE!
			//if (e.KeyCode == Keys.Escape)
			//{
			//	XDrop.Items.Clear();

			//	string[] labels = FITSBINTABLE.TableDataLabelsTTYPE;

			//	for (int i = 0; i < labels.Length; i++)
			//		labels[i] = labels[i].Trim();

			//	XDrop.Items.AddRange(labels);
			//}
		}
	}
}
