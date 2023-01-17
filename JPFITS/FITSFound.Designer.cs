namespace JPFITS
{
	partial class FITSFound
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
			this.NumSelectTxt = new System.Windows.Forms.Label();
			this.ClearAllBtn = new System.Windows.Forms.Button();
			this.SaveListBtn = new System.Windows.Forms.Button();
			this.SelectAllBtn = new System.Windows.Forms.Button();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.NumFilesTxt = new System.Windows.Forms.Label();
			this.FileListTxt = new System.Windows.Forms.ListBox();
			this.FoundListContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.OpenFolderContextItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AddImageSetBtn = new System.Windows.Forms.Button();
			this.LoadImageSetBtn = new System.Windows.Forms.Button();
			this.MoveListBtn = new System.Windows.Forms.Button();
			this.CopyListBtn = new System.Windows.Forms.Button();
			this.FileCopyBGWrkr = new System.ComponentModel.BackgroundWorker();
			this.FoundListContextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// NumSelectTxt
			// 
			this.NumSelectTxt.AutoSize = true;
			this.NumSelectTxt.Location = new System.Drawing.Point(26, 34);
			this.NumSelectTxt.Name = "NumSelectTxt";
			this.NumSelectTxt.Size = new System.Drawing.Size(62, 13);
			this.NumSelectTxt.TabIndex = 17;
			this.NumSelectTxt.Text = "(0 selected)";
			// 
			// ClearAllBtn
			// 
			this.ClearAllBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.ClearAllBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.ClearAllBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.ClearAllBtn.Location = new System.Drawing.Point(311, 518);
			this.ClearAllBtn.Name = "ClearAllBtn";
			this.ClearAllBtn.Size = new System.Drawing.Size(87, 30);
			this.ClearAllBtn.TabIndex = 16;
			this.ClearAllBtn.Text = "Cl&ear All";
			this.ClearAllBtn.UseVisualStyleBackColor = true;
			this.ClearAllBtn.Click += new System.EventHandler(this.ClearAllBtn_Click);
			// 
			// SaveListBtn
			// 
			this.SaveListBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.SaveListBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.SaveListBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.SaveListBtn.ForeColor = System.Drawing.SystemColors.ControlText;
			this.SaveListBtn.Location = new System.Drawing.Point(219, 648);
			this.SaveListBtn.Name = "SaveListBtn";
			this.SaveListBtn.Size = new System.Drawing.Size(87, 30);
			this.SaveListBtn.TabIndex = 15;
			this.SaveListBtn.Text = "Sa&ve List";
			this.SaveListBtn.UseVisualStyleBackColor = true;
			this.SaveListBtn.Click += new System.EventHandler(this.SaveListBtn_Click);
			// 
			// SelectAllBtn
			// 
			this.SelectAllBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.SelectAllBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.SelectAllBtn.Location = new System.Drawing.Point(219, 518);
			this.SelectAllBtn.Name = "SelectAllBtn";
			this.SelectAllBtn.Size = new System.Drawing.Size(87, 30);
			this.SelectAllBtn.TabIndex = 14;
			this.SelectAllBtn.Text = "&Select All";
			this.SelectAllBtn.UseVisualStyleBackColor = true;
			this.SelectAllBtn.Click += new System.EventHandler(this.SelectAllBtn_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.CancelBtn.Location = new System.Drawing.Point(311, 648);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(87, 30);
			this.CancelBtn.TabIndex = 13;
			this.CancelBtn.Text = "&Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// NumFilesTxt
			// 
			this.NumFilesTxt.AutoSize = true;
			this.NumFilesTxt.Location = new System.Drawing.Point(26, 9);
			this.NumFilesTxt.Name = "NumFilesTxt";
			this.NumFilesTxt.Size = new System.Drawing.Size(71, 13);
			this.NumFilesTxt.TabIndex = 10;
			this.NumFilesTxt.Text = "Found # Files";
			// 
			// FileListTxt
			// 
			this.FileListTxt.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.FileListTxt.ContextMenuStrip = this.FoundListContextMenu;
			this.FileListTxt.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.FileListTxt.FormattingEnabled = true;
			this.FileListTxt.HorizontalScrollbar = true;
			this.FileListTxt.Location = new System.Drawing.Point(29, 64);
			this.FileListTxt.Name = "FileListTxt";
			this.FileListTxt.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.FileListTxt.Size = new System.Drawing.Size(559, 446);
			this.FileListTxt.Sorted = true;
			this.FileListTxt.TabIndex = 9;
			this.FileListTxt.MouseClick += new System.Windows.Forms.MouseEventHandler(this.FileListTxt_MouseClick);
			this.FileListTxt.SelectedIndexChanged += new System.EventHandler(this.FileListTxt_SelectedIndexChanged);
			this.FileListTxt.MouseUp += new System.Windows.Forms.MouseEventHandler(this.FileListTxt_MouseUp);
			// 
			// FoundListContextMenu
			// 
			this.FoundListContextMenu.Enabled = false;
			this.FoundListContextMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.FoundListContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenFolderContextItem});
			this.FoundListContextMenu.Name = "FoundListContextMenu";
			this.FoundListContextMenu.Size = new System.Drawing.Size(140, 26);
			// 
			// OpenFolderContextItem
			// 
			this.OpenFolderContextItem.Enabled = false;
			this.OpenFolderContextItem.Name = "OpenFolderContextItem";
			this.OpenFolderContextItem.Size = new System.Drawing.Size(139, 22);
			this.OpenFolderContextItem.Text = "Open Folder";
			this.OpenFolderContextItem.Click += new System.EventHandler(this.OpenFolderContextItem_Click);
			// 
			// AddImageSetBtn
			// 
			this.AddImageSetBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.AddImageSetBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.AddImageSetBtn.DialogResult = System.Windows.Forms.DialogResult.Ignore;
			this.AddImageSetBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.AddImageSetBtn.Location = new System.Drawing.Point(311, 554);
			this.AddImageSetBtn.Name = "AddImageSetBtn";
			this.AddImageSetBtn.Size = new System.Drawing.Size(87, 52);
			this.AddImageSetBtn.TabIndex = 19;
			this.AddImageSetBtn.Text = "&Add Selected to Image Set";
			this.AddImageSetBtn.UseVisualStyleBackColor = true;
			// 
			// LoadImageSetBtn
			// 
			this.LoadImageSetBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.LoadImageSetBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.LoadImageSetBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.LoadImageSetBtn.Location = new System.Drawing.Point(219, 554);
			this.LoadImageSetBtn.Name = "LoadImageSetBtn";
			this.LoadImageSetBtn.Size = new System.Drawing.Size(87, 52);
			this.LoadImageSetBtn.TabIndex = 18;
			this.LoadImageSetBtn.Text = "&Load Selected as Image Set";
			this.LoadImageSetBtn.UseVisualStyleBackColor = true;
			this.LoadImageSetBtn.Click += new System.EventHandler(this.LoadImageSetBtn_Click);
			// 
			// MoveListBtn
			// 
			this.MoveListBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.MoveListBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.MoveListBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.MoveListBtn.ForeColor = System.Drawing.SystemColors.ControlText;
			this.MoveListBtn.Location = new System.Drawing.Point(219, 612);
			this.MoveListBtn.Name = "MoveListBtn";
			this.MoveListBtn.Size = new System.Drawing.Size(87, 30);
			this.MoveListBtn.TabIndex = 21;
			this.MoveListBtn.Text = "Move";
			this.MoveListBtn.UseVisualStyleBackColor = true;
			this.MoveListBtn.Click += new System.EventHandler(this.MoveListBtn_Click);
			// 
			// CopyListBtn
			// 
			this.CopyListBtn.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.CopyListBtn.BackColor = System.Drawing.SystemColors.Control;
			this.CopyListBtn.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.CopyListBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
			this.CopyListBtn.Location = new System.Drawing.Point(311, 612);
			this.CopyListBtn.Name = "CopyListBtn";
			this.CopyListBtn.Size = new System.Drawing.Size(87, 30);
			this.CopyListBtn.TabIndex = 20;
			this.CopyListBtn.Text = "Copy";
			this.CopyListBtn.UseVisualStyleBackColor = true;
			this.CopyListBtn.Click += new System.EventHandler(this.CopyListBtn_Click);
			// 
			// FileCopyBGWrkr
			// 
			this.FileCopyBGWrkr.WorkerReportsProgress = true;
			this.FileCopyBGWrkr.WorkerSupportsCancellation = true;
			this.FileCopyBGWrkr.DoWork += new System.ComponentModel.DoWorkEventHandler(this.FileCopyBGWrkr_DoWork);
			this.FileCopyBGWrkr.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.FileCopyBGWrkr_ProgressChanged);
			this.FileCopyBGWrkr.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.FileCopyBGWrkr_RunWorkerCompleted);
			// 
			// FITSFound
			// 
			this.AcceptButton = this.LoadImageSetBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(613, 690);
			this.Controls.Add(this.MoveListBtn);
			this.Controls.Add(this.CopyListBtn);
			this.Controls.Add(this.AddImageSetBtn);
			this.Controls.Add(this.LoadImageSetBtn);
			this.Controls.Add(this.NumSelectTxt);
			this.Controls.Add(this.ClearAllBtn);
			this.Controls.Add(this.SaveListBtn);
			this.Controls.Add(this.SelectAllBtn);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.NumFilesTxt);
			this.Controls.Add(this.FileListTxt);
			this.MaximizeBox = false;
			this.MinimumSize = new System.Drawing.Size(226, 662);
			this.Name = "FITSFound";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Found FITS Files";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FitsFound_FormClosing);
			this.Load += new System.EventHandler(this.FitsFound_Load);
			this.FoundListContextMenu.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label NumSelectTxt;
		private System.Windows.Forms.Button ClearAllBtn;
		private System.Windows.Forms.Button SaveListBtn;
		private System.Windows.Forms.Button SelectAllBtn;
		private System.Windows.Forms.Button CancelBtn;
		public System.Windows.Forms.Label NumFilesTxt;
		public System.Windows.Forms.ListBox FileListTxt;
		public System.Windows.Forms.Button AddImageSetBtn;
		public System.Windows.Forms.Button LoadImageSetBtn;
		private System.Windows.Forms.Button MoveListBtn;
		private System.Windows.Forms.Button CopyListBtn;
		private System.Windows.Forms.ContextMenuStrip FoundListContextMenu;
		private System.Windows.Forms.ToolStripMenuItem OpenFolderContextItem;
		private System.ComponentModel.BackgroundWorker FileCopyBGWrkr;
	}
}