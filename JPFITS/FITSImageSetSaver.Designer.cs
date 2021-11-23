namespace JPFITS
{
	partial class FITSImageSetSaver
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
			this.AppendTxt = new System.Windows.Forms.TextBox();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.DirectoryTxt = new System.Windows.Forms.RichTextBox();
			this.FileExtension = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.OverwriteBtn = new System.Windows.Forms.Button();
			this.AppendBtn = new System.Windows.Forms.Button();
			this.DirectoryFileLabel = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.AppendBtnChangeBtn = new System.Windows.Forms.Button();
			this.ZipInOneChck = new System.Windows.Forms.CheckBox();
			this.ZipContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.ZipContextCopyChck = new System.Windows.Forms.ToolStripMenuItem();
			this.ZipContextMoveChck = new System.Windows.Forms.ToolStripMenuItem();
			this.GetCommonDirectoryBtn = new System.Windows.Forms.Button();
			this.UseOrigDirChck = new System.Windows.Forms.CheckBox();
			this.InvertGrayScaleChck = new System.Windows.Forms.CheckBox();
			this.ZipContextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// AppendTxt
			// 
			this.AppendTxt.Location = new System.Drawing.Point(33, 149);
			this.AppendTxt.MaxLength = 20;
			this.AppendTxt.Name = "AppendTxt";
			this.AppendTxt.Size = new System.Drawing.Size(73, 20);
			this.AppendTxt.TabIndex = 21;
			this.AppendTxt.Text = "_red";
			this.AppendTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.toolTip1.SetToolTip(this.AppendTxt, "Enter * to auto-increment appendage.");
			// 
			// CancelBtn
			// 
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(250, 147);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(75, 23);
			this.CancelBtn.TabIndex = 20;
			this.CancelBtn.Text = "Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// DirectoryTxt
			// 
			this.DirectoryTxt.DetectUrls = false;
			this.DirectoryTxt.Location = new System.Drawing.Point(13, 28);
			this.DirectoryTxt.Name = "DirectoryTxt";
			this.DirectoryTxt.ReadOnly = true;
			this.DirectoryTxt.Size = new System.Drawing.Size(312, 69);
			this.DirectoryTxt.TabIndex = 22;
			this.DirectoryTxt.Text = "c:/";
			this.toolTip1.SetToolTip(this.DirectoryTxt, "Click to Select Directory");
			this.DirectoryTxt.Click += new System.EventHandler(this.DirectoryTxt_Click);
			this.DirectoryTxt.TextChanged += new System.EventHandler(this.DirectoryTxt_TextChanged);
			// 
			// FileExtension
			// 
			this.FileExtension.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.FileExtension.FormattingEnabled = true;
			this.FileExtension.Items.AddRange(new object[] {
            ".fts",
            ".fit",
            ".fits",
            ".jpg",
            ".zip"});
			this.FileExtension.Location = new System.Drawing.Point(194, 148);
			this.FileExtension.Name = "FileExtension";
			this.FileExtension.Size = new System.Drawing.Size(50, 21);
			this.FileExtension.TabIndex = 24;
			this.FileExtension.SelectedIndexChanged += new System.EventHandler(this.FileExtension_SelectedIndexChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(191, 130);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(34, 13);
			this.label2.TabIndex = 23;
			this.label2.Text = "Type:";
			// 
			// OverwriteBtn
			// 
			this.OverwriteBtn.DialogResult = System.Windows.Forms.DialogResult.Ignore;
			this.OverwriteBtn.Location = new System.Drawing.Point(113, 126);
			this.OverwriteBtn.Name = "OverwriteBtn";
			this.OverwriteBtn.Size = new System.Drawing.Size(75, 44);
			this.OverwriteBtn.TabIndex = 19;
			this.OverwriteBtn.Text = "Write/Overwrite";
			this.toolTip1.SetToolTip(this.OverwriteBtn, "\"Overwrite\" will use the existing filenames; if a different directory or file ext" +
        "ension is selected, the original files will not be changed, otherwise they will " +
        "be overwritten.");
			this.OverwriteBtn.UseVisualStyleBackColor = true;
			this.OverwriteBtn.Click += new System.EventHandler(this.OverwriteBtn_Click);
			// 
			// AppendBtn
			// 
			this.AppendBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.AppendBtn.Location = new System.Drawing.Point(32, 126);
			this.AppendBtn.Name = "AppendBtn";
			this.AppendBtn.Size = new System.Drawing.Size(75, 23);
			this.AppendBtn.TabIndex = 18;
			this.AppendBtn.Text = "Append";
			this.toolTip1.SetToolTip(this.AppendBtn, "This will append the text below to the file name, thus, the original files will n" +
        "ot be overwritten.  If no text is entered, you will receive an error notificatio" +
        "n. ");
			this.AppendBtn.UseVisualStyleBackColor = true;
			this.AppendBtn.Click += new System.EventHandler(this.AppendBtn_Click);
			// 
			// DirectoryFileLabel
			// 
			this.DirectoryFileLabel.AutoSize = true;
			this.DirectoryFileLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.DirectoryFileLabel.Location = new System.Drawing.Point(12, 12);
			this.DirectoryFileLabel.Name = "DirectoryFileLabel";
			this.DirectoryFileLabel.Size = new System.Drawing.Size(52, 13);
			this.DirectoryFileLabel.TabIndex = 17;
			this.DirectoryFileLabel.Text = "Directory:";
			this.DirectoryFileLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// toolTip1
			// 
			this.toolTip1.AutomaticDelay = 50;
			this.toolTip1.AutoPopDelay = 8000;
			this.toolTip1.InitialDelay = 50;
			this.toolTip1.ReshowDelay = 10;
			// 
			// AppendBtnChangeBtn
			// 
			this.AppendBtnChangeBtn.Location = new System.Drawing.Point(15, 126);
			this.AppendBtnChangeBtn.Name = "AppendBtnChangeBtn";
			this.AppendBtnChangeBtn.Size = new System.Drawing.Size(19, 23);
			this.AppendBtnChangeBtn.TabIndex = 26;
			this.AppendBtnChangeBtn.Text = "Δ";
			this.toolTip1.SetToolTip(this.AppendBtnChangeBtn, "Click to change file name modification \"Append\" button. See its tooltip.");
			this.AppendBtnChangeBtn.UseVisualStyleBackColor = true;
			this.AppendBtnChangeBtn.Click += new System.EventHandler(this.AppendBtnChangeBtn_Click);
			// 
			// ZipInOneChck
			// 
			this.ZipInOneChck.AutoSize = true;
			this.ZipInOneChck.ContextMenuStrip = this.ZipContextMenu;
			this.ZipInOneChck.Location = new System.Drawing.Point(231, 126);
			this.ZipInOneChck.Name = "ZipInOneChck";
			this.ZipInOneChck.Size = new System.Drawing.Size(94, 17);
			this.ZipInOneChck.TabIndex = 29;
			this.ZipInOneChck.Text = "Zip in One File";
			this.toolTip1.SetToolTip(this.ZipInOneChck, "Conext menu (right click here) for more options!");
			this.ZipInOneChck.UseVisualStyleBackColor = true;
			this.ZipInOneChck.Visible = false;
			this.ZipInOneChck.CheckedChanged += new System.EventHandler(this.ZipInOneChck_CheckedChanged);
			this.ZipInOneChck.MouseEnter += new System.EventHandler(this.ZipInOneChck_MouseEnter);
			this.ZipInOneChck.MouseLeave += new System.EventHandler(this.ZipInOneChck_MouseLeave);
			// 
			// ZipContextMenu
			// 
			this.ZipContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ZipContextCopyChck,
            this.ZipContextMoveChck});
			this.ZipContextMenu.Name = "ZipContextMenu";
			this.ZipContextMenu.Size = new System.Drawing.Size(181, 70);
			// 
			// ZipContextCopyChck
			// 
			this.ZipContextCopyChck.Checked = true;
			this.ZipContextCopyChck.CheckOnClick = true;
			this.ZipContextCopyChck.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ZipContextCopyChck.Name = "ZipContextCopyChck";
			this.ZipContextCopyChck.Size = new System.Drawing.Size(180, 22);
			this.ZipContextCopyChck.Text = "Copy into Archive";
			this.ZipContextCopyChck.Click += new System.EventHandler(this.ZipContextCopyChck_Click);
			// 
			// ZipContextMoveChck
			// 
			this.ZipContextMoveChck.CheckOnClick = true;
			this.ZipContextMoveChck.Name = "ZipContextMoveChck";
			this.ZipContextMoveChck.Size = new System.Drawing.Size(180, 22);
			this.ZipContextMoveChck.Text = "Move into Archive";
			this.ZipContextMoveChck.Click += new System.EventHandler(this.ZipContextMoveChck_Click);
			// 
			// GetCommonDirectoryBtn
			// 
			this.GetCommonDirectoryBtn.Location = new System.Drawing.Point(204, 97);
			this.GetCommonDirectoryBtn.Name = "GetCommonDirectoryBtn";
			this.GetCommonDirectoryBtn.Size = new System.Drawing.Size(121, 23);
			this.GetCommonDirectoryBtn.TabIndex = 27;
			this.GetCommonDirectoryBtn.Text = "Get Common Directory";
			this.GetCommonDirectoryBtn.UseVisualStyleBackColor = true;
			this.GetCommonDirectoryBtn.Click += new System.EventHandler(this.GetCommonDirectoryBtn_Click);
			// 
			// UseOrigDirChck
			// 
			this.UseOrigDirChck.AutoSize = true;
			this.UseOrigDirChck.Location = new System.Drawing.Point(13, 103);
			this.UseOrigDirChck.Name = "UseOrigDirChck";
			this.UseOrigDirChck.Size = new System.Drawing.Size(139, 17);
			this.UseOrigDirChck.TabIndex = 25;
			this.UseOrigDirChck.Text = "Use Original Directory(s)";
			this.UseOrigDirChck.UseVisualStyleBackColor = true;
			this.UseOrigDirChck.CheckedChanged += new System.EventHandler(this.UseOrigDirChck_CheckedChanged);
			// 
			// InvertGrayScaleChck
			// 
			this.InvertGrayScaleChck.AutoSize = true;
			this.InvertGrayScaleChck.Location = new System.Drawing.Point(231, 126);
			this.InvertGrayScaleChck.Name = "InvertGrayScaleChck";
			this.InvertGrayScaleChck.Size = new System.Drawing.Size(101, 17);
			this.InvertGrayScaleChck.TabIndex = 28;
			this.InvertGrayScaleChck.Text = "Invert grayscale";
			this.InvertGrayScaleChck.UseVisualStyleBackColor = true;
			this.InvertGrayScaleChck.Visible = false;
			// 
			// FITSImageSetSaver
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(337, 179);
			this.ControlBox = false;
			this.Controls.Add(this.AppendTxt);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.DirectoryTxt);
			this.Controls.Add(this.FileExtension);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.OverwriteBtn);
			this.Controls.Add(this.AppendBtn);
			this.Controls.Add(this.DirectoryFileLabel);
			this.Controls.Add(this.GetCommonDirectoryBtn);
			this.Controls.Add(this.AppendBtnChangeBtn);
			this.Controls.Add(this.UseOrigDirChck);
			this.Controls.Add(this.ZipInOneChck);
			this.Controls.Add(this.InvertGrayScaleChck);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "FITSImageSetSaver";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Batch Save...";
			this.Shown += new System.EventHandler(this.FITSImageSetSaver_Shown);
			this.ZipContextMenu.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		public System.Windows.Forms.TextBox AppendTxt;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button CancelBtn;
		public System.Windows.Forms.RichTextBox DirectoryTxt;
		public System.Windows.Forms.ComboBox FileExtension;//Index 0 = .fts, 1 = .fit, 2 = .fits, 3 = .jpg, 4 = .zip
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button OverwriteBtn;
		public System.Windows.Forms.Button AppendBtn;
		private System.Windows.Forms.Label DirectoryFileLabel;
		private System.Windows.Forms.Button AppendBtnChangeBtn;
		public System.Windows.Forms.CheckBox UseOrigDirChck;
		private System.Windows.Forms.ContextMenuStrip ZipContextMenu;
		public System.Windows.Forms.CheckBox InvertGrayScaleChck;
		public System.Windows.Forms.CheckBox ZipInOneChck;
		public System.Windows.Forms.Button GetCommonDirectoryBtn;
		public System.Windows.Forms.ToolStripMenuItem ZipContextMoveChck;
		public System.Windows.Forms.ToolStripMenuItem ZipContextCopyChck;
	}
}