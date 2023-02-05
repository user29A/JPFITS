namespace JPFITS
{
	partial class AstraCarta
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
			this.MessageTextBox = new System.Windows.Forms.TextBox();
			this.ForceNewChck = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.RATextBox = new System.Windows.Forms.TextBox();
			this.DecTextBox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.ScaleTextBox = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.WidthTextBox = new System.Windows.Forms.TextBox();
			this.HeightTextBox = new System.Windows.Forms.TextBox();
			this.BufferTextBox = new System.Windows.Forms.TextBox();
			this.MagLimitTextBox = new System.Windows.Forms.TextBox();
			this.RotationTextBox = new System.Windows.Forms.TextBox();
			this.NameTextBox = new System.Windows.Forms.TextBox();
			this.PMEpochTextBox = new System.Windows.Forms.TextBox();
			this.PMLimitTextBox = new System.Windows.Forms.TextBox();
			this.NQueryTextBox = new System.Windows.Forms.TextBox();
			this.RAOffsetTextBox = new System.Windows.Forms.TextBox();
			this.DecOffsetTextBox = new System.Windows.Forms.TextBox();
			this.FITSTableChck = new System.Windows.Forms.CheckBox();
			this.EntriesTextBox = new System.Windows.Forms.TextBox();
			this.DirectoryTextBox = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.CatalogueDrop = new System.Windows.Forms.ComboBox();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.FilterDrop = new System.Windows.Forms.ComboBox();
			this.label10 = new System.Windows.Forms.Label();
			this.ShapeDrop = new System.Windows.Forms.ComboBox();
			this.label11 = new System.Windows.Forms.Label();
			this.ShowImageChck = new System.Windows.Forms.CheckBox();
			this.SaveImageChck = new System.Windows.Forms.CheckBox();
			this.SilentChck = new System.Windows.Forms.CheckBox();
			this.label12 = new System.Windows.Forms.Label();
			this.RemoveRawQueryChck = new System.Windows.Forms.CheckBox();
			this.label13 = new System.Windows.Forms.Label();
			this.SaveTableChck = new System.Windows.Forms.CheckBox();
			this.label14 = new System.Windows.Forms.Label();
			this.label15 = new System.Windows.Forms.Label();
			this.label16 = new System.Windows.Forms.Label();
			this.ExecuteBtn = new System.Windows.Forms.Button();
			this.label17 = new System.Windows.Forms.Label();
			this.EscapeBtn = new System.Windows.Forms.Button();
			this.MainMenu = new System.Windows.Forms.MenuStrip();
			this.LoadMenuBtn = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveMenuBtn = new System.Windows.Forms.ToolStripMenuItem();
			this.CloseOnCompleteChck = new System.Windows.Forms.CheckBox();
			this.BGWrkr = new System.ComponentModel.BackgroundWorker();
			this.OverwriteChck = new System.Windows.Forms.CheckBox();
			this.label18 = new System.Windows.Forms.Label();
			this.label19 = new System.Windows.Forms.Label();
			this.MainMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// MessageTextBox
			// 
			this.MessageTextBox.Dock = System.Windows.Forms.DockStyle.Top;
			this.MessageTextBox.Location = new System.Drawing.Point(0, 24);
			this.MessageTextBox.Multiline = true;
			this.MessageTextBox.Name = "MessageTextBox";
			this.MessageTextBox.ReadOnly = true;
			this.MessageTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.MessageTextBox.Size = new System.Drawing.Size(435, 160);
			this.MessageTextBox.TabIndex = 0;
			// 
			// ForceNewChck
			// 
			this.ForceNewChck.AutoSize = true;
			this.ForceNewChck.Location = new System.Drawing.Point(190, 450);
			this.ForceNewChck.Name = "ForceNewChck";
			this.ForceNewChck.Size = new System.Drawing.Size(78, 17);
			this.ForceNewChck.TabIndex = 1;
			this.ForceNewChck.Text = "Force New";
			this.ForceNewChck.UseVisualStyleBackColor = true;
			this.ForceNewChck.CheckedChanged += new System.EventHandler(this.ForceNewChck_CheckedChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(8, 193);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(28, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "RA:";
			// 
			// RATextBox
			// 
			this.RATextBox.Location = new System.Drawing.Point(49, 190);
			this.RATextBox.Name = "RATextBox";
			this.RATextBox.Size = new System.Drawing.Size(100, 20);
			this.RATextBox.TabIndex = 3;
			this.toolTip1.SetToolTip(this.RATextBox, "degree.degree");
			this.RATextBox.TextChanged += new System.EventHandler(this.NumericTextBox_TextChanged);
			// 
			// DecTextBox
			// 
			this.DecTextBox.Location = new System.Drawing.Point(49, 216);
			this.DecTextBox.Name = "DecTextBox";
			this.DecTextBox.Size = new System.Drawing.Size(100, 20);
			this.DecTextBox.TabIndex = 5;
			this.toolTip1.SetToolTip(this.DecTextBox, "degree.degree");
			this.DecTextBox.TextChanged += new System.EventHandler(this.NumericTextBox_TextChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.Location = new System.Drawing.Point(8, 219);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(34, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Dec:";
			// 
			// ScaleTextBox
			// 
			this.ScaleTextBox.Location = new System.Drawing.Point(49, 242);
			this.ScaleTextBox.Name = "ScaleTextBox";
			this.ScaleTextBox.Size = new System.Drawing.Size(46, 20);
			this.ScaleTextBox.TabIndex = 7;
			this.toolTip1.SetToolTip(this.ScaleTextBox, "arcseconds per pixel");
			this.ScaleTextBox.TextChanged += new System.EventHandler(this.NumericTextBox_TextChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.Location = new System.Drawing.Point(8, 245);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(43, 13);
			this.label3.TabIndex = 6;
			this.label3.Text = "Scale:";
			// 
			// toolTip1
			// 
			this.toolTip1.AutomaticDelay = 100;
			this.toolTip1.AutoPopDelay = 8000;
			this.toolTip1.InitialDelay = 100;
			this.toolTip1.ReshowDelay = 20;
			this.toolTip1.ShowAlways = true;
			// 
			// WidthTextBox
			// 
			this.WidthTextBox.Location = new System.Drawing.Point(49, 268);
			this.WidthTextBox.Name = "WidthTextBox";
			this.WidthTextBox.Size = new System.Drawing.Size(46, 20);
			this.WidthTextBox.TabIndex = 9;
			this.toolTip1.SetToolTip(this.WidthTextBox, "pixels");
			this.WidthTextBox.TextChanged += new System.EventHandler(this.NumericTextBox_TextChanged);
			// 
			// HeightTextBox
			// 
			this.HeightTextBox.Location = new System.Drawing.Point(49, 294);
			this.HeightTextBox.Name = "HeightTextBox";
			this.HeightTextBox.Size = new System.Drawing.Size(46, 20);
			this.HeightTextBox.TabIndex = 11;
			this.toolTip1.SetToolTip(this.HeightTextBox, "pixels");
			this.HeightTextBox.TextChanged += new System.EventHandler(this.NumericTextBox_TextChanged);
			// 
			// BufferTextBox
			// 
			this.BufferTextBox.Location = new System.Drawing.Point(49, 320);
			this.BufferTextBox.Name = "BufferTextBox";
			this.BufferTextBox.Size = new System.Drawing.Size(46, 20);
			this.BufferTextBox.TabIndex = 13;
			this.toolTip1.SetToolTip(this.BufferTextBox, "arcminutes");
			this.BufferTextBox.TextChanged += new System.EventHandler(this.NumericTextBox_TextChanged);
			// 
			// MagLimitTextBox
			// 
			this.MagLimitTextBox.Location = new System.Drawing.Point(371, 249);
			this.MagLimitTextBox.Name = "MagLimitTextBox";
			this.MagLimitTextBox.Size = new System.Drawing.Size(52, 20);
			this.MagLimitTextBox.TabIndex = 15;
			this.toolTip1.SetToolTip(this.MagLimitTextBox, "filter magnitude");
			this.MagLimitTextBox.TextChanged += new System.EventHandler(this.NumericTextBox_TextChanged);
			// 
			// RotationTextBox
			// 
			this.RotationTextBox.Location = new System.Drawing.Point(371, 302);
			this.RotationTextBox.Name = "RotationTextBox";
			this.RotationTextBox.Size = new System.Drawing.Size(52, 20);
			this.RotationTextBox.TabIndex = 16;
			this.toolTip1.SetToolTip(this.RotationTextBox, "degrees");
			this.RotationTextBox.TextChanged += new System.EventHandler(this.NumericTextBox_TextChanged);
			// 
			// NameTextBox
			// 
			this.NameTextBox.Location = new System.Drawing.Point(49, 346);
			this.NameTextBox.Name = "NameTextBox";
			this.NameTextBox.Size = new System.Drawing.Size(202, 20);
			this.NameTextBox.TabIndex = 29;
			this.toolTip1.SetToolTip(this.NameTextBox, "output file name");
			// 
			// PMEpochTextBox
			// 
			this.PMEpochTextBox.Location = new System.Drawing.Point(354, 355);
			this.PMEpochTextBox.Name = "PMEpochTextBox";
			this.PMEpochTextBox.Size = new System.Drawing.Size(69, 20);
			this.PMEpochTextBox.TabIndex = 18;
			this.toolTip1.SetToolTip(this.PMEpochTextBox, "year.year");
			this.PMEpochTextBox.TextChanged += new System.EventHandler(this.NumericTextBox_TextChanged);
			// 
			// PMLimitTextBox
			// 
			this.PMLimitTextBox.Location = new System.Drawing.Point(354, 381);
			this.PMLimitTextBox.Name = "PMLimitTextBox";
			this.PMLimitTextBox.Size = new System.Drawing.Size(69, 20);
			this.PMLimitTextBox.TabIndex = 19;
			this.toolTip1.SetToolTip(this.PMLimitTextBox, "mas per year");
			this.PMLimitTextBox.TextChanged += new System.EventHandler(this.NumericTextBox_TextChanged);
			// 
			// NQueryTextBox
			// 
			this.NQueryTextBox.Location = new System.Drawing.Point(371, 328);
			this.NQueryTextBox.Name = "NQueryTextBox";
			this.NQueryTextBox.Size = new System.Drawing.Size(52, 20);
			this.NQueryTextBox.TabIndex = 17;
			this.toolTip1.SetToolTip(this.NQueryTextBox, "Number of sources to request");
			this.NQueryTextBox.TextChanged += new System.EventHandler(this.NumericTextBox_TextChanged);
			// 
			// RAOffsetTextBox
			// 
			this.RAOffsetTextBox.Location = new System.Drawing.Point(222, 190);
			this.RAOffsetTextBox.Name = "RAOffsetTextBox";
			this.RAOffsetTextBox.Size = new System.Drawing.Size(29, 20);
			this.RAOffsetTextBox.TabIndex = 49;
			this.toolTip1.SetToolTip(this.RAOffsetTextBox, "arcminutes");
			// 
			// DecOffsetTextBox
			// 
			this.DecOffsetTextBox.Location = new System.Drawing.Point(222, 216);
			this.DecOffsetTextBox.Name = "DecOffsetTextBox";
			this.DecOffsetTextBox.Size = new System.Drawing.Size(29, 20);
			this.DecOffsetTextBox.TabIndex = 51;
			this.toolTip1.SetToolTip(this.DecOffsetTextBox, "arcminutes");
			// 
			// FITSTableChck
			// 
			this.FITSTableChck.AutoSize = true;
			this.FITSTableChck.Location = new System.Drawing.Point(11, 473);
			this.FITSTableChck.Name = "FITSTableChck";
			this.FITSTableChck.Size = new System.Drawing.Size(79, 17);
			this.FITSTableChck.TabIndex = 24;
			this.FITSTableChck.Text = "FITS Table";
			this.toolTip1.SetToolTip(this.FITSTableChck, "Save as FITS binary table. CSV is default.");
			this.FITSTableChck.UseVisualStyleBackColor = true;
			this.FITSTableChck.CheckedChanged += new System.EventHandler(this.FITSTableChck_CheckedChanged);
			// 
			// EntriesTextBox
			// 
			this.EntriesTextBox.Location = new System.Drawing.Point(11, 424);
			this.EntriesTextBox.Name = "EntriesTextBox";
			this.EntriesTextBox.Size = new System.Drawing.Size(409, 20);
			this.EntriesTextBox.TabIndex = 23;
			// 
			// DirectoryTextBox
			// 
			this.DirectoryTextBox.Location = new System.Drawing.Point(11, 385);
			this.DirectoryTextBox.Name = "DirectoryTextBox";
			this.DirectoryTextBox.Size = new System.Drawing.Size(268, 20);
			this.DirectoryTextBox.TabIndex = 43;
			this.DirectoryTextBox.Click += new System.EventHandler(this.DirectoryTextBox_Click);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.Location = new System.Drawing.Point(8, 271);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(44, 13);
			this.label4.TabIndex = 8;
			this.label4.Text = "Width:";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label5.Location = new System.Drawing.Point(8, 297);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(48, 13);
			this.label5.TabIndex = 10;
			this.label5.Text = "Height:";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(8, 323);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(38, 13);
			this.label6.TabIndex = 12;
			this.label6.Text = "Buffer:";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(313, 252);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(52, 13);
			this.label7.TabIndex = 14;
			this.label7.Text = "MagLimit:";
			// 
			// CatalogueDrop
			// 
			this.CatalogueDrop.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.CatalogueDrop.FormattingEnabled = true;
			this.CatalogueDrop.Items.AddRange(new object[] {
            "GaiaDR3"});
			this.CatalogueDrop.Location = new System.Drawing.Point(354, 193);
			this.CatalogueDrop.Name = "CatalogueDrop";
			this.CatalogueDrop.Size = new System.Drawing.Size(69, 21);
			this.CatalogueDrop.TabIndex = 19;
			this.CatalogueDrop.SelectedIndexChanged += new System.EventHandler(this.CatalogueDrop_SelectedIndexChanged);
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(290, 196);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(58, 13);
			this.label8.TabIndex = 17;
			this.label8.Text = "Catalogue:";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(316, 225);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(32, 13);
			this.label9.TabIndex = 19;
			this.label9.Text = "Filter:";
			// 
			// FilterDrop
			// 
			this.FilterDrop.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.FilterDrop.FormattingEnabled = true;
			this.FilterDrop.Items.AddRange(new object[] {
            "bp",
            "g",
            "rp"});
			this.FilterDrop.Location = new System.Drawing.Point(354, 222);
			this.FilterDrop.Name = "FilterDrop";
			this.FilterDrop.Size = new System.Drawing.Size(69, 21);
			this.FilterDrop.TabIndex = 20;
			this.FilterDrop.SelectedIndexChanged += new System.EventHandler(this.FilterDrop_SelectedIndexChanged);
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(307, 278);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(41, 13);
			this.label10.TabIndex = 21;
			this.label10.Text = "Shape:";
			// 
			// ShapeDrop
			// 
			this.ShapeDrop.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ShapeDrop.FormattingEnabled = true;
			this.ShapeDrop.Items.AddRange(new object[] {
            "rectangle",
            "circle"});
			this.ShapeDrop.Location = new System.Drawing.Point(354, 275);
			this.ShapeDrop.Name = "ShapeDrop";
			this.ShapeDrop.Size = new System.Drawing.Size(69, 21);
			this.ShapeDrop.TabIndex = 21;
			this.ShapeDrop.SelectedIndexChanged += new System.EventHandler(this.ShapeDrop_SelectedIndexChanged);
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(315, 305);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(50, 13);
			this.label11.TabIndex = 22;
			this.label11.Text = "Rotation:";
			// 
			// ShowImageChck
			// 
			this.ShowImageChck.AutoSize = true;
			this.ShowImageChck.Location = new System.Drawing.Point(101, 450);
			this.ShowImageChck.Name = "ShowImageChck";
			this.ShowImageChck.Size = new System.Drawing.Size(85, 17);
			this.ShowImageChck.TabIndex = 25;
			this.ShowImageChck.Text = "Show Image";
			this.ShowImageChck.UseVisualStyleBackColor = true;
			this.ShowImageChck.CheckedChanged += new System.EventHandler(this.ShowImageChck_CheckedChanged);
			// 
			// SaveImageChck
			// 
			this.SaveImageChck.AutoSize = true;
			this.SaveImageChck.Location = new System.Drawing.Point(101, 473);
			this.SaveImageChck.Name = "SaveImageChck";
			this.SaveImageChck.Size = new System.Drawing.Size(83, 17);
			this.SaveImageChck.TabIndex = 26;
			this.SaveImageChck.Text = "Save Image";
			this.SaveImageChck.UseVisualStyleBackColor = true;
			this.SaveImageChck.CheckedChanged += new System.EventHandler(this.SaveImageChck_CheckedChanged);
			// 
			// SilentChck
			// 
			this.SilentChck.AutoSize = true;
			this.SilentChck.Location = new System.Drawing.Point(274, 450);
			this.SilentChck.Name = "SilentChck";
			this.SilentChck.Size = new System.Drawing.Size(52, 17);
			this.SilentChck.TabIndex = 27;
			this.SilentChck.Text = "Silent";
			this.SilentChck.UseVisualStyleBackColor = true;
			this.SilentChck.CheckedChanged += new System.EventHandler(this.SilentChck_CheckedChanged);
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(8, 349);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(38, 13);
			this.label12.TabIndex = 28;
			this.label12.Text = "Name:";
			// 
			// RemoveRawQueryChck
			// 
			this.RemoveRawQueryChck.AutoSize = true;
			this.RemoveRawQueryChck.Location = new System.Drawing.Point(190, 473);
			this.RemoveRawQueryChck.Name = "RemoveRawQueryChck";
			this.RemoveRawQueryChck.Size = new System.Drawing.Size(122, 17);
			this.RemoveRawQueryChck.TabIndex = 31;
			this.RemoveRawQueryChck.Text = "Remove Raw Query";
			this.RemoveRawQueryChck.UseVisualStyleBackColor = true;
			this.RemoveRawQueryChck.CheckedChanged += new System.EventHandler(this.RemoveRawQueryChck_CheckedChanged);
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(316, 331);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(49, 13);
			this.label13.TabIndex = 33;
			this.label13.Text = "N Query:";
			// 
			// SaveTableChck
			// 
			this.SaveTableChck.AutoSize = true;
			this.SaveTableChck.Location = new System.Drawing.Point(11, 450);
			this.SaveTableChck.Name = "SaveTableChck";
			this.SaveTableChck.Size = new System.Drawing.Size(81, 17);
			this.SaveTableChck.TabIndex = 34;
			this.SaveTableChck.Text = "Save Table";
			this.SaveTableChck.UseVisualStyleBackColor = true;
			this.SaveTableChck.CheckedChanged += new System.EventHandler(this.SaveTableChck_CheckedChanged);
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(288, 358);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(60, 13);
			this.label14.TabIndex = 35;
			this.label14.Text = "PM Epoch:";
			// 
			// label15
			// 
			this.label15.AutoSize = true;
			this.label15.Location = new System.Drawing.Point(295, 384);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(50, 13);
			this.label15.TabIndex = 37;
			this.label15.Text = "PM Limit:";
			// 
			// label16
			// 
			this.label16.AutoSize = true;
			this.label16.Location = new System.Drawing.Point(8, 408);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(91, 13);
			this.label16.TabIndex = 39;
			this.label16.Text = "Additional Entries:";
			// 
			// ExecuteBtn
			// 
			this.ExecuteBtn.Location = new System.Drawing.Point(348, 492);
			this.ExecuteBtn.Name = "ExecuteBtn";
			this.ExecuteBtn.Size = new System.Drawing.Size(75, 23);
			this.ExecuteBtn.TabIndex = 41;
			this.ExecuteBtn.Text = "Execute";
			this.ExecuteBtn.UseVisualStyleBackColor = true;
			this.ExecuteBtn.Click += new System.EventHandler(this.ExecuteBtn_Click);
			// 
			// label17
			// 
			this.label17.AutoSize = true;
			this.label17.Location = new System.Drawing.Point(8, 369);
			this.label17.Name = "label17";
			this.label17.Size = new System.Drawing.Size(87, 13);
			this.label17.TabIndex = 42;
			this.label17.Text = "Output Directory:";
			// 
			// EscapeBtn
			// 
			this.EscapeBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.EscapeBtn.Location = new System.Drawing.Point(165, 80);
			this.EscapeBtn.Name = "EscapeBtn";
			this.EscapeBtn.Size = new System.Drawing.Size(75, 23);
			this.EscapeBtn.TabIndex = 44;
			this.EscapeBtn.Text = "escape";
			this.EscapeBtn.UseVisualStyleBackColor = true;
			// 
			// MainMenu
			// 
			this.MainMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.MainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LoadMenuBtn,
            this.SaveMenuBtn});
			this.MainMenu.Location = new System.Drawing.Point(0, 0);
			this.MainMenu.Name = "MainMenu";
			this.MainMenu.Padding = new System.Windows.Forms.Padding(4, 1, 0, 1);
			this.MainMenu.Size = new System.Drawing.Size(435, 24);
			this.MainMenu.TabIndex = 45;
			this.MainMenu.Text = "menuStrip1";
			// 
			// LoadMenuBtn
			// 
			this.LoadMenuBtn.Name = "LoadMenuBtn";
			this.LoadMenuBtn.Size = new System.Drawing.Size(45, 22);
			this.LoadMenuBtn.Text = "Load";
			this.LoadMenuBtn.Click += new System.EventHandler(this.LoadMenuBtn_Click);
			// 
			// SaveMenuBtn
			// 
			this.SaveMenuBtn.Name = "SaveMenuBtn";
			this.SaveMenuBtn.Size = new System.Drawing.Size(43, 22);
			this.SaveMenuBtn.Text = "Save";
			this.SaveMenuBtn.Click += new System.EventHandler(this.SaveMenuBtn_Click);
			// 
			// CloseOnCompleteChck
			// 
			this.CloseOnCompleteChck.Location = new System.Drawing.Point(348, 450);
			this.CloseOnCompleteChck.Name = "CloseOnCompleteChck";
			this.CloseOnCompleteChck.Size = new System.Drawing.Size(75, 36);
			this.CloseOnCompleteChck.TabIndex = 46;
			this.CloseOnCompleteChck.Text = "Close on Success";
			this.CloseOnCompleteChck.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.CloseOnCompleteChck.UseVisualStyleBackColor = true;
			this.CloseOnCompleteChck.CheckedChanged += new System.EventHandler(this.CloseOnCompleteChck_CheckedChanged);
			// 
			// BGWrkr
			// 
			this.BGWrkr.WorkerReportsProgress = true;
			this.BGWrkr.WorkerSupportsCancellation = true;
			this.BGWrkr.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BGWrkr_DoWork);
			this.BGWrkr.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BGWrkr_ProgressChanged);
			this.BGWrkr.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BGWrkr_RunWorkerCompleted);
			// 
			// OverwriteChck
			// 
			this.OverwriteChck.AutoSize = true;
			this.OverwriteChck.Location = new System.Drawing.Point(11, 496);
			this.OverwriteChck.Name = "OverwriteChck";
			this.OverwriteChck.Size = new System.Drawing.Size(111, 17);
			this.OverwriteChck.TabIndex = 47;
			this.OverwriteChck.Text = "Overwrite Outputs";
			this.OverwriteChck.UseVisualStyleBackColor = true;
			this.OverwriteChck.CheckedChanged += new System.EventHandler(this.OverwriteChck_CheckedChanged);
			// 
			// label18
			// 
			this.label18.AutoSize = true;
			this.label18.Location = new System.Drawing.Point(155, 193);
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size(56, 13);
			this.label18.TabIndex = 48;
			this.label18.Text = "RA Offset:";
			// 
			// label19
			// 
			this.label19.AutoSize = true;
			this.label19.Location = new System.Drawing.Point(155, 219);
			this.label19.Name = "label19";
			this.label19.Size = new System.Drawing.Size(61, 13);
			this.label19.TabIndex = 50;
			this.label19.Text = "Dec Offset:";
			// 
			// AstraCarta
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(435, 523);
			this.Controls.Add(this.DecOffsetTextBox);
			this.Controls.Add(this.label19);
			this.Controls.Add(this.RAOffsetTextBox);
			this.Controls.Add(this.label18);
			this.Controls.Add(this.OverwriteChck);
			this.Controls.Add(this.NQueryTextBox);
			this.Controls.Add(this.CloseOnCompleteChck);
			this.Controls.Add(this.DirectoryTextBox);
			this.Controls.Add(this.label17);
			this.Controls.Add(this.ExecuteBtn);
			this.Controls.Add(this.EntriesTextBox);
			this.Controls.Add(this.label16);
			this.Controls.Add(this.PMLimitTextBox);
			this.Controls.Add(this.label15);
			this.Controls.Add(this.PMEpochTextBox);
			this.Controls.Add(this.label14);
			this.Controls.Add(this.SaveTableChck);
			this.Controls.Add(this.label13);
			this.Controls.Add(this.RemoveRawQueryChck);
			this.Controls.Add(this.NameTextBox);
			this.Controls.Add(this.label12);
			this.Controls.Add(this.SilentChck);
			this.Controls.Add(this.SaveImageChck);
			this.Controls.Add(this.ShowImageChck);
			this.Controls.Add(this.FITSTableChck);
			this.Controls.Add(this.RotationTextBox);
			this.Controls.Add(this.label11);
			this.Controls.Add(this.label10);
			this.Controls.Add(this.ShapeDrop);
			this.Controls.Add(this.label9);
			this.Controls.Add(this.FilterDrop);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.CatalogueDrop);
			this.Controls.Add(this.MagLimitTextBox);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.BufferTextBox);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.HeightTextBox);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.WidthTextBox);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.ScaleTextBox);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.DecTextBox);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.RATextBox);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.ForceNewChck);
			this.Controls.Add(this.MessageTextBox);
			this.Controls.Add(this.MainMenu);
			this.Controls.Add(this.EscapeBtn);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.HelpButton = true;
			this.KeyPreview = true;
			this.MainMenuStrip = this.MainMenu;
			this.Margin = new System.Windows.Forms.Padding(2);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AstraCarta";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Text = "AstraCarta";
			this.Load += new System.EventHandler(this.AstraCarta_Load);
			this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.AstraCarta_HelpRequested);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.AstraCarta_KeyDown);
			this.MainMenu.ResumeLayout(false);
			this.MainMenu.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox MessageTextBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Label label17;
		public System.Windows.Forms.CheckBox ForceNewChck;
		public System.Windows.Forms.TextBox RATextBox;
		public System.Windows.Forms.TextBox DecTextBox;
		public System.Windows.Forms.TextBox ScaleTextBox;
		public System.Windows.Forms.TextBox WidthTextBox;
		public System.Windows.Forms.TextBox HeightTextBox;
		public System.Windows.Forms.TextBox BufferTextBox;
		public System.Windows.Forms.TextBox MagLimitTextBox;
		public System.Windows.Forms.ComboBox CatalogueDrop;
		public System.Windows.Forms.ComboBox FilterDrop;
		public System.Windows.Forms.ComboBox ShapeDrop;
		public System.Windows.Forms.TextBox RotationTextBox;
		public System.Windows.Forms.CheckBox FITSTableChck;
		public System.Windows.Forms.CheckBox ShowImageChck;
		public System.Windows.Forms.CheckBox SaveImageChck;
		public System.Windows.Forms.CheckBox SilentChck;
		public System.Windows.Forms.TextBox NameTextBox;
		public System.Windows.Forms.CheckBox RemoveRawQueryChck;
		public System.Windows.Forms.CheckBox SaveTableChck;
		public System.Windows.Forms.TextBox PMEpochTextBox;
		public System.Windows.Forms.TextBox PMLimitTextBox;
		public System.Windows.Forms.TextBox EntriesTextBox;
		public System.Windows.Forms.Button ExecuteBtn;
		public System.Windows.Forms.TextBox DirectoryTextBox;
		public System.Windows.Forms.Button EscapeBtn;
		private System.Windows.Forms.MenuStrip MainMenu;
		private System.Windows.Forms.ToolStripMenuItem LoadMenuBtn;
		private System.Windows.Forms.ToolStripMenuItem SaveMenuBtn;
		public System.Windows.Forms.CheckBox CloseOnCompleteChck;
		private System.ComponentModel.BackgroundWorker BGWrkr;
		public System.Windows.Forms.TextBox NQueryTextBox;
		public System.Windows.Forms.CheckBox OverwriteChck;
		public System.Windows.Forms.TextBox RAOffsetTextBox;
		private System.Windows.Forms.Label label18;
		public System.Windows.Forms.TextBox DecOffsetTextBox;
		private System.Windows.Forms.Label label19;
	}
}