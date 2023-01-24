namespace JPFITS
{
	partial class FitsBinTableViewer
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			this.ExtensionTableGrid = new System.Windows.Forms.DataGridView();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.MenuFile = new System.Windows.Forms.ToolStripMenuItem();
			this.FileOpenMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.MenuChooseTable = new System.Windows.Forms.ToolStripMenuItem();
			this.MenuChooseTableEntries = new System.Windows.Forms.ToolStripMenuItem();
			this.ViewAllChck = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.PlotEntryMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.xToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.XDrop = new System.Windows.Forms.ToolStripComboBox();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.YDrop = new System.Windows.Forms.ToolStripComboBox();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.PlotMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ViewHeaderMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.button1 = new System.Windows.Forms.Button();
			this.HeaderListBox = new System.Windows.Forms.ListBox();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.ExtensionTableGrid)).BeginInit();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// ExtensionTableGrid
			// 
			this.ExtensionTableGrid.AllowUserToOrderColumns = true;
			this.ExtensionTableGrid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			this.ExtensionTableGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.ExtensionTableGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ExtensionTableGrid.Location = new System.Drawing.Point(0, 33);
			this.ExtensionTableGrid.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.ExtensionTableGrid.Name = "ExtensionTableGrid";
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.Format = "N0";
			dataGridViewCellStyle1.NullValue = null;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.ExtensionTableGrid.RowHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.ExtensionTableGrid.RowHeadersWidth = 125;
			this.ExtensionTableGrid.Size = new System.Drawing.Size(874, 510);
			this.ExtensionTableGrid.TabIndex = 0;
			this.ExtensionTableGrid.VirtualMode = true;
			this.ExtensionTableGrid.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.ExtensionTableGrid_CellValueNeeded);
			this.ExtensionTableGrid.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.ExtensionTableGrid_RowsAdded);
			this.ExtensionTableGrid.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ExtensionTableGrid_Scroll);
			// 
			// menuStrip1
			// 
			this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
			this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuFile,
            this.MenuChooseTable,
            this.MenuChooseTableEntries,
            this.PlotEntryMenu,
            this.ViewHeaderMenu});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Padding = new System.Windows.Forms.Padding(6, 2, 0, 2);
			this.menuStrip1.Size = new System.Drawing.Size(874, 33);
			this.menuStrip1.TabIndex = 1;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// MenuFile
			// 
			this.MenuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileOpenMenu});
			this.MenuFile.Name = "MenuFile";
			this.MenuFile.Size = new System.Drawing.Size(54, 29);
			this.MenuFile.Text = "File";
			// 
			// FileOpenMenu
			// 
			this.FileOpenMenu.Name = "FileOpenMenu";
			this.FileOpenMenu.Size = new System.Drawing.Size(158, 34);
			this.FileOpenMenu.Text = "Open";
			this.FileOpenMenu.Click += new System.EventHandler(this.FileOpenMenu_Click);
			// 
			// MenuChooseTable
			// 
			this.MenuChooseTable.Name = "MenuChooseTable";
			this.MenuChooseTable.Size = new System.Drawing.Size(76, 29);
			this.MenuChooseTable.Text = "Tables";
			// 
			// MenuChooseTableEntries
			// 
			this.MenuChooseTableEntries.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ViewAllChck,
            this.toolStripSeparator1});
			this.MenuChooseTableEntries.Name = "MenuChooseTableEntries";
			this.MenuChooseTableEntries.Size = new System.Drawing.Size(125, 29);
			this.MenuChooseTableEntries.Text = "Table Entries";
			this.MenuChooseTableEntries.Click += new System.EventHandler(this.MenuChooseTableEntries_Click);
			// 
			// ViewAllChck
			// 
			this.ViewAllChck.Name = "ViewAllChck";
			this.ViewAllChck.Size = new System.Drawing.Size(199, 34);
			this.ViewAllChck.Text = "View None";
			this.ViewAllChck.CheckedChanged += new System.EventHandler(this.ViewAllChck_CheckedChanged);
			this.ViewAllChck.Click += new System.EventHandler(this.ViewAllChck_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(196, 6);
			// 
			// PlotEntryMenu
			// 
			this.PlotEntryMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.xToolStripMenuItem,
            this.XDrop,
            this.toolStripSeparator2,
            this.toolStripMenuItem2,
            this.YDrop,
            this.toolStripSeparator3,
            this.PlotMenuItem});
			this.PlotEntryMenu.Name = "PlotEntryMenu";
			this.PlotEntryMenu.Size = new System.Drawing.Size(104, 29);
			this.PlotEntryMenu.Text = "Plot Entry";
			// 
			// xToolStripMenuItem
			// 
			this.xToolStripMenuItem.CheckOnClick = true;
			this.xToolStripMenuItem.Name = "xToolStripMenuItem";
			this.xToolStripMenuItem.Size = new System.Drawing.Size(221, 34);
			this.xToolStripMenuItem.Text = "X";
			this.xToolStripMenuItem.CheckedChanged += new System.EventHandler(this.xToolStripMenuItem_CheckedChanged);
			// 
			// XDrop
			// 
			this.XDrop.BackColor = System.Drawing.Color.Gainsboro;
			this.XDrop.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.XDrop.Enabled = false;
			this.XDrop.Name = "XDrop";
			this.XDrop.Size = new System.Drawing.Size(121, 33);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(218, 6);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(221, 34);
			this.toolStripMenuItem2.Text = "Y";
			// 
			// YDrop
			// 
			this.YDrop.BackColor = System.Drawing.Color.Gainsboro;
			this.YDrop.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.YDrop.Name = "YDrop";
			this.YDrop.Size = new System.Drawing.Size(121, 33);
			this.YDrop.SelectedIndexChanged += new System.EventHandler(this.YDrop_SelectedIndexChanged);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(218, 6);
			// 
			// PlotMenuItem
			// 
			this.PlotMenuItem.Enabled = false;
			this.PlotMenuItem.Name = "PlotMenuItem";
			this.PlotMenuItem.Size = new System.Drawing.Size(221, 34);
			this.PlotMenuItem.Text = "Plot Selection";
			this.PlotMenuItem.Click += new System.EventHandler(this.PlotMenuItem_Click);
			// 
			// ViewHeaderMenu
			// 
			this.ViewHeaderMenu.Name = "ViewHeaderMenu";
			this.ViewHeaderMenu.Size = new System.Drawing.Size(127, 29);
			this.ViewHeaderMenu.Text = "View Header";
			this.ViewHeaderMenu.Click += new System.EventHandler(this.ViewHeaderMenu_Click);
			// 
			// button1
			// 
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button1.Location = new System.Drawing.Point(358, 205);
			this.button1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(112, 35);
			this.button1.TabIndex = 2;
			this.button1.Text = "cancelbtn";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// HeaderListBox
			// 
			this.HeaderListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.HeaderListBox.Font = new System.Drawing.Font("Courier New", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.HeaderListBox.FormattingEnabled = true;
			this.HeaderListBox.ItemHeight = 22;
			this.HeaderListBox.Location = new System.Drawing.Point(0, 0);
			this.HeaderListBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.HeaderListBox.Name = "HeaderListBox";
			this.HeaderListBox.Size = new System.Drawing.Size(874, 543);
			this.HeaderListBox.TabIndex = 3;
			// 
			// toolTip1
			// 
			this.toolTip1.AutomaticDelay = 100;
			this.toolTip1.AutoPopDelay = 10000;
			this.toolTip1.InitialDelay = 100;
			this.toolTip1.ReshowDelay = 20;
			// 
			// FitsBinTableViewer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.button1;
			this.ClientSize = new System.Drawing.Size(874, 543);
			this.Controls.Add(this.ExtensionTableGrid);
			this.Controls.Add(this.menuStrip1);
			this.Controls.Add(this.HeaderListBox);
			this.Controls.Add(this.button1);
			this.MainMenuStrip = this.menuStrip1;
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.Name = "FitsBinTableViewer";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "FitsExtensionTableViewer";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FitsExtensionTableViewer_FormClosing);
			this.Load += new System.EventHandler(this.FitsExtensionTableViewer_Load);
			this.Shown += new System.EventHandler(this.FitsBinTableViewer_Shown);
			this.ResizeBegin += new System.EventHandler(this.FitsExtensionTableViewer_ResizeBegin);
			this.ResizeEnd += new System.EventHandler(this.FitsExtensionTableViewer_ResizeEnd);
			this.LocationChanged += new System.EventHandler(this.FitsExtensionTableViewer_LocationChanged);
			this.SizeChanged += new System.EventHandler(this.FitsExtensionTableViewer_SizeChanged);
			((System.ComponentModel.ISupportInitialize)(this.ExtensionTableGrid)).EndInit();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView ExtensionTableGrid;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem MenuChooseTableEntries;
		private System.Windows.Forms.ToolStripMenuItem ViewAllChck;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem PlotEntryMenu;
		private System.Windows.Forms.ToolStripComboBox XDrop;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripComboBox YDrop;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem PlotMenuItem;
		private System.Windows.Forms.ToolStripMenuItem MenuFile;
		private System.Windows.Forms.ToolStripMenuItem FileOpenMenu;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.ToolStripMenuItem ViewHeaderMenu;
		private System.Windows.Forms.ListBox HeaderListBox;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.ToolStripMenuItem xToolStripMenuItem;
		public System.Windows.Forms.ToolStripMenuItem MenuChooseTable;
	}
}