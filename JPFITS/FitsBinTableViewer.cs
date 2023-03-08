using System;
using System.Collections;
using System.Diagnostics.Eventing.Reader;
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

			OpenBINTABLE(ofd.FileName);
		}

		public void OpenBINTABLE(string FileName)
		{
			try
			{
				MenuChooseTable.DropDownItems.Clear();
				ViewAllChck.Text = "View All";
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
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Data + "	" + e.InnerException + "	" + e.Message + "	" + e.Source + "	" + e.StackTrace + "	" + e.TargetSite);
			}
		}

		private void FitsBinTableViewer_Shown(object sender, EventArgs e)
		{
			if (MenuChooseTable.DropDownItems.Count > 1)
				MenuChooseTable.ShowDropDown();
			else
			{
				MenuChooseTable.DropDownItems[0].PerformClick();
				MenuChooseTable.HideDropDown();
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

				ExtensionTableGrid.ColumnCount = MenuChooseTableEntries.DropDownItems.Count - 2;
				for (int i = 0; i < ExtensionTableGrid.ColumnCount; i++)
					ExtensionTableGrid.Columns[i].HeaderText = MenuChooseTableEntries.DropDownItems[i + 2].Text;
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

			if (((ToolStripMenuItem)sender).Checked == false)
				ViewAllChck.Text = "View All";

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
			//first need to check that the items being plotted are vectors...can only plot y vs.x as vectors
			if (XDrop.Enabled)
				if (FITSBINTABLE.GetTTYPEIsHeapEntry(XDrop.SelectedItem.ToString()) || FITSBINTABLE.TableDataRepeats[XDrop.SelectedIndex] > 1)
				{
					MessageBox.Show("Error: X-Axis selection is either a heap entry or has multiple columns. Can only plot with X as a vector of values.");
					return;
				}
				else if (FITSBINTABLE.GetTTYPETypeCode(XDrop.SelectedItem.ToString()) == TypeCode.Char || FITSBINTABLE.GetTTYPETypeCode(XDrop.SelectedItem.ToString()) == TypeCode.Boolean)
				{
					MessageBox.Show("Error: X-Axis selection is " + FITSBINTABLE.GetTTYPETypeCode(XDrop.SelectedItem.ToString()).ToString());
					return;
				}

			if (FITSBINTABLE.GetTTYPEIsHeapEntry(YDrop.SelectedItem.ToString()) || FITSBINTABLE.TableDataRepeats[YDrop.SelectedIndex] > 1)
			{
				MessageBox.Show("Error: Y-Axis selection is either a heap entry or has multiple columns. Can only plot with Y as a vector of values.");
				return;
			}
			else if (FITSBINTABLE.GetTTYPETypeCode(YDrop.SelectedItem.ToString()) == TypeCode.Char || FITSBINTABLE.GetTTYPETypeCode(YDrop.SelectedItem.ToString()) == TypeCode.Boolean)
			{
				MessageBox.Show("Error: Y-Axis selection is " + FITSBINTABLE.GetTTYPETypeCode(XDrop.SelectedItem.ToString()).ToString());
				return;
			}

			double[] x = new double[FITSBINTABLE.Naxis2];
			if (!XDrop.Enabled)
				for (int i = 0; i < x.Length; i++)
					x[i] = i;
			else
				x = (double[])FITSBINTABLE.GetTTYPEEntry(XDrop.SelectedItem.ToString(), out _, out _, FITSBinTable.TTYPEReturn.AsDouble);

			double[] y = (double[])FITSBINTABLE.GetTTYPEEntry(YDrop.SelectedItem.ToString(), out _, out _, FITSBinTable.TTYPEReturn.AsDouble);

			Plotter plot = new Plotter("", false, false);

			String xlabel;
			if (!XDrop.Enabled)
				xlabel = "index";
			else
				xlabel = XDrop.SelectedItem.ToString();

			String ylabel = YDrop.SelectedItem.ToString();
			String title = ylabel;
			if (XDrop.Enabled)
				title += " vs. " + xlabel;

			plot.ChartGraph.PlotXYData(x, y, title, xlabel, ylabel, JPChart.SeriesType.FastPoint, title.Replace(" ", ""), null);
			plot.Text = title;
			plot.Show();
		}

		private void xToolStripMenuItem_CheckedChanged(object sender, EventArgs e)//THIS IS WHY THE CHECKBOX WAS THERE! - because you cannot unselect a drop down once any value is selected
		{
			if (xToolStripMenuItem.Checked)
			{
				XDrop.Enabled = true;
				if (XDrop.SelectedIndex == -1)
					XDrop.SelectedIndex = 0;
			}
			else
				XDrop.Enabled = false;

			PlotEntryMenu.ShowDropDown();
		}

		private void ViewHeaderMenu_Click(object sender, EventArgs e)
		{
			FITSHeaderViewer fhv = new FITSHeaderViewer(new FITSHeader(FITSBINTABLE.Header));
			fhv.Show();
		}

		private void YDrop_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (YDrop.SelectedIndex != -1)
				PlotMenuItem.Enabled = true;
		}		
	}
}
