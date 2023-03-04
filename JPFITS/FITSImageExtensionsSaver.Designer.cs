namespace JPFITS
{
	partial class FITSImageExtensionsSaver
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
			this.CancelBtn = new System.Windows.Forms.Button();
			this.extensionsGridView = new System.Windows.Forms.DataGridView();
			this.label1 = new System.Windows.Forms.Label();
			this.FirstAsPrimaryChck = new System.Windows.Forms.CheckBox();
			this.SaveBtn = new System.Windows.Forms.Button();
			this.AppendIntoFileChck = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label2 = new System.Windows.Forms.Label();
			this.GlobalPrecisionDrop = new System.Windows.Forms.ComboBox();
			this.ViewEditPrimaryHeaderBtn = new System.Windows.Forms.Button();
			this.EXTNAME = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.PRECISION = new System.Windows.Forms.DataGridViewComboBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this.extensionsGridView)).BeginInit();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// CancelBtn
			// 
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(266, 382);
			this.CancelBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(134, 35);
			this.CancelBtn.TabIndex = 0;
			this.CancelBtn.Text = "Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// extensionsGridView
			// 
			this.extensionsGridView.AllowUserToAddRows = false;
			this.extensionsGridView.AllowUserToDeleteRows = false;
			this.extensionsGridView.AllowUserToResizeRows = false;
			this.extensionsGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.ColumnHeader;
			this.extensionsGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.extensionsGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.EXTNAME,
            this.PRECISION});
			this.extensionsGridView.Location = new System.Drawing.Point(18, 38);
			this.extensionsGridView.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.extensionsGridView.Name = "extensionsGridView";
			this.extensionsGridView.RowHeadersVisible = false;
			this.extensionsGridView.RowHeadersWidth = 62;
			this.extensionsGridView.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.extensionsGridView.Size = new System.Drawing.Size(238, 382);
			this.extensionsGridView.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(18, 14);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(137, 20);
			this.label1.TabIndex = 3;
			this.label1.Text = "Extension Naming";
			// 
			// FirstAsPrimaryChck
			// 
			this.FirstAsPrimaryChck.Location = new System.Drawing.Point(266, 38);
			this.FirstAsPrimaryChck.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.FirstAsPrimaryChck.Name = "FirstAsPrimaryChck";
			this.FirstAsPrimaryChck.Size = new System.Drawing.Size(134, 69);
			this.FirstAsPrimaryChck.TabIndex = 5;
			this.FirstAsPrimaryChck.Text = "First Image as Primary Block";
			this.FirstAsPrimaryChck.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.FirstAsPrimaryChck.UseVisualStyleBackColor = true;
			this.FirstAsPrimaryChck.CheckedChanged += new System.EventHandler(this.FirstAsPrimaryChck_CheckedChanged);
			// 
			// SaveBtn
			// 
			this.SaveBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.SaveBtn.Location = new System.Drawing.Point(266, 338);
			this.SaveBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.SaveBtn.Name = "SaveBtn";
			this.SaveBtn.Size = new System.Drawing.Size(134, 35);
			this.SaveBtn.TabIndex = 7;
			this.SaveBtn.Text = "Save";
			this.SaveBtn.UseVisualStyleBackColor = true;
			this.SaveBtn.Click += new System.EventHandler(this.SaveBtn_Click);
			// 
			// AppendIntoFileChck
			// 
			this.AppendIntoFileChck.Location = new System.Drawing.Point(266, 275);
			this.AppendIntoFileChck.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.AppendIntoFileChck.Name = "AppendIntoFileChck";
			this.AppendIntoFileChck.Size = new System.Drawing.Size(134, 54);
			this.AppendIntoFileChck.TabIndex = 8;
			this.AppendIntoFileChck.Text = "Append Onto Existing File";
			this.AppendIntoFileChck.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.AppendIntoFileChck.UseVisualStyleBackColor = true;
			this.AppendIntoFileChck.CheckedChanged += new System.EventHandler(this.AppendIntoFileChck_CheckedChanged);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.GlobalPrecisionDrop);
			this.groupBox1.Controls.Add(this.ViewEditPrimaryHeaderBtn);
			this.groupBox1.Location = new System.Drawing.Point(262, 28);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(141, 394);
			this.groupBox1.TabIndex = 10;
			this.groupBox1.TabStop = false;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 171);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(123, 20);
			this.label2.TabIndex = 2;
			this.label2.Text = "Global Precision";
			// 
			// GlobalPrecisionDrop
			// 
			this.GlobalPrecisionDrop.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.GlobalPrecisionDrop.FormattingEnabled = true;
			this.GlobalPrecisionDrop.Items.AddRange(new object[] {
            "boolean",
            "sbyte",
            "byte",
            "int16",
            "uint16",
            "int32",
            "uint32",
            "int64",
            "uint64",
            "float",
            "double",
            "(reset)"});
			this.GlobalPrecisionDrop.Location = new System.Drawing.Point(6, 194);
			this.GlobalPrecisionDrop.Name = "GlobalPrecisionDrop";
			this.GlobalPrecisionDrop.Size = new System.Drawing.Size(128, 28);
			this.GlobalPrecisionDrop.TabIndex = 1;
			this.GlobalPrecisionDrop.SelectedIndexChanged += new System.EventHandler(this.GlobalPrecisionDrop_SelectedIndexChanged);
			// 
			// ViewEditPrimaryHeaderBtn
			// 
			this.ViewEditPrimaryHeaderBtn.Location = new System.Drawing.Point(6, 85);
			this.ViewEditPrimaryHeaderBtn.Name = "ViewEditPrimaryHeaderBtn";
			this.ViewEditPrimaryHeaderBtn.Size = new System.Drawing.Size(129, 72);
			this.ViewEditPrimaryHeaderBtn.TabIndex = 0;
			this.ViewEditPrimaryHeaderBtn.Text = "View/Edit Primary Header";
			this.ViewEditPrimaryHeaderBtn.UseVisualStyleBackColor = true;
			this.ViewEditPrimaryHeaderBtn.Click += new System.EventHandler(this.ViewEditPrimaryHeaderBtn_Click);
			// 
			// EXTNAME
			// 
			this.EXTNAME.HeaderText = "EXTNAME";
			this.EXTNAME.MaxInputLength = 18;
			this.EXTNAME.MinimumWidth = 8;
			this.EXTNAME.Name = "EXTNAME";
			this.EXTNAME.Width = 122;
			// 
			// PRECISION
			// 
			this.PRECISION.DropDownWidth = 100;
			this.PRECISION.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.PRECISION.HeaderText = "PRECISION";
			this.PRECISION.Items.AddRange(new object[] {
            "boolean",
            "sbyte",
            "byte",
            "int16",
            "uint16",
            "int32",
            "uint32",
            "int64",
            "uint64",
            "float",
            "double"});
			this.PRECISION.MinimumWidth = 8;
			this.PRECISION.Name = "PRECISION";
			this.PRECISION.Width = 103;
			// 
			// FITSImageExtensionsSaver
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(410, 438);
			this.Controls.Add(this.AppendIntoFileChck);
			this.Controls.Add(this.SaveBtn);
			this.Controls.Add(this.FirstAsPrimaryChck);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.extensionsGridView);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.groupBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FITSImageExtensionsSaver";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Image Extensions Saver";
			this.TopMost = true;
			this.MouseEnter += new System.EventHandler(this.ImageExtensionsSaver_MouseEnter);
			((System.ComponentModel.ISupportInitialize)(this.extensionsGridView)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.DataGridView extensionsGridView;
		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.CheckBox FirstAsPrimaryChck;
		private System.Windows.Forms.Button SaveBtn;
		private System.Windows.Forms.CheckBox AppendIntoFileChck;

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button ViewEditPrimaryHeaderBtn;
		private System.Windows.Forms.ComboBox GlobalPrecisionDrop;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.DataGridViewTextBoxColumn EXTNAME;
		private System.Windows.Forms.DataGridViewComboBoxColumn PRECISION;
	}
}