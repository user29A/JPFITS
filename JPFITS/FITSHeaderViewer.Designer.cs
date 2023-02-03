namespace JPFITS
{
	partial class FITSHeaderViewer
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
			this.HeaderKeysListBox = new System.Windows.Forms.ListBox();
			this.HeaderContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.HeaderContextApplyAll = new System.Windows.Forms.ToolStripMenuItem();
			this.HeaderContextAddKey = new System.Windows.Forms.ToolStripMenuItem();
			this.HeaderContextEditKey = new System.Windows.Forms.ToolStripMenuItem();
			this.HeaderContextRemoveKeys = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.HeaderContextCopyValuesList = new System.Windows.Forms.ToolStripMenuItem();
			this.CopyIncludeKeyNameHeaderChck = new System.Windows.Forms.ToolStripMenuItem();
			this.HeaderContextInsertKeys = new System.Windows.Forms.ToolStripMenuItem();
			this.InsertOverwriteChck = new System.Windows.Forms.ToolStripMenuItem();
			this.MenuStrip = new System.Windows.Forms.MenuStrip();
			this.FileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.FileSaveCloseMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.discardCloseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.EditMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.EditCopyfromFileBtn = new System.Windows.Forms.ToolStripMenuItem();
			this.EditClearMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.EditClearBtn = new System.Windows.Forms.ToolStripMenuItem();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.OKbtn = new System.Windows.Forms.Button();
			this.HeaderContextMenu.SuspendLayout();
			this.MenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// HeaderKeysListBox
			// 
			this.HeaderKeysListBox.BackColor = System.Drawing.Color.Gainsboro;
			this.HeaderKeysListBox.ContextMenuStrip = this.HeaderContextMenu;
			this.HeaderKeysListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.HeaderKeysListBox.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.HeaderKeysListBox.FormattingEnabled = true;
			this.HeaderKeysListBox.ItemHeight = 15;
			this.HeaderKeysListBox.Location = new System.Drawing.Point(0, 24);
			this.HeaderKeysListBox.Name = "HeaderKeysListBox";
			this.HeaderKeysListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.HeaderKeysListBox.Size = new System.Drawing.Size(613, 582);
			this.HeaderKeysListBox.TabIndex = 0;
			this.HeaderKeysListBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HeaderKeysListBox_KeyDown);
			this.HeaderKeysListBox.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.HeaderKeysListBox_MouseDoubleClick);
			// 
			// HeaderContextMenu
			// 
			this.HeaderContextMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.HeaderContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator2,
            this.HeaderContextApplyAll,
            this.HeaderContextAddKey,
            this.HeaderContextEditKey,
            this.HeaderContextRemoveKeys,
            this.toolStripSeparator1,
            this.HeaderContextCopyValuesList,
            this.HeaderContextInsertKeys});
			this.HeaderContextMenu.Name = "HeaderContextMenu";
			this.HeaderContextMenu.Size = new System.Drawing.Size(279, 148);
			this.HeaderContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.HeaderContextMenu_Opening);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(275, 6);
			// 
			// HeaderContextApplyAll
			// 
			this.HeaderContextApplyAll.CheckOnClick = true;
			this.HeaderContextApplyAll.Name = "HeaderContextApplyAll";
			this.HeaderContextApplyAll.Size = new System.Drawing.Size(278, 22);
			this.HeaderContextApplyAll.Text = "Apply to All Headers";
			this.HeaderContextApplyAll.Click += new System.EventHandler(this.HeaderContextApplyAll_Click);
			// 
			// HeaderContextAddKey
			// 
			this.HeaderContextAddKey.Name = "HeaderContextAddKey";
			this.HeaderContextAddKey.Size = new System.Drawing.Size(278, 22);
			this.HeaderContextAddKey.Text = "Add New Key";
			this.HeaderContextAddKey.Click += new System.EventHandler(this.HeaderContextAddKey_Click);
			// 
			// HeaderContextEditKey
			// 
			this.HeaderContextEditKey.Name = "HeaderContextEditKey";
			this.HeaderContextEditKey.Size = new System.Drawing.Size(278, 22);
			this.HeaderContextEditKey.Text = "Edit Selected Key";
			this.HeaderContextEditKey.Click += new System.EventHandler(this.HeaderContextEditKey_Click);
			// 
			// HeaderContextRemoveKeys
			// 
			this.HeaderContextRemoveKeys.Name = "HeaderContextRemoveKeys";
			this.HeaderContextRemoveKeys.Size = new System.Drawing.Size(278, 22);
			this.HeaderContextRemoveKeys.Text = "Remove Selected Key(s)";
			this.HeaderContextRemoveKeys.Click += new System.EventHandler(this.HeaderContextRemoveKeys_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(275, 6);
			// 
			// HeaderContextCopyValuesList
			// 
			this.HeaderContextCopyValuesList.DoubleClickEnabled = true;
			this.HeaderContextCopyValuesList.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CopyIncludeKeyNameHeaderChck});
			this.HeaderContextCopyValuesList.Name = "HeaderContextCopyValuesList";
			this.HeaderContextCopyValuesList.Size = new System.Drawing.Size(278, 22);
			this.HeaderContextCopyValuesList.Text = "Copy Selected Key Value(s) to List";
			this.HeaderContextCopyValuesList.DoubleClick += new System.EventHandler(this.HeaderContextCopyValuesList_DoubleClick);
			// 
			// CopyIncludeKeyNameHeaderChck
			// 
			this.CopyIncludeKeyNameHeaderChck.CheckOnClick = true;
			this.CopyIncludeKeyNameHeaderChck.Name = "CopyIncludeKeyNameHeaderChck";
			this.CopyIncludeKeyNameHeaderChck.Size = new System.Drawing.Size(224, 22);
			this.CopyIncludeKeyNameHeaderChck.Text = "Include Key Name Header(s)";
			this.CopyIncludeKeyNameHeaderChck.Click += new System.EventHandler(this.CopyIncludeKeyNameHeaderChck_Click);
			// 
			// HeaderContextInsertKeys
			// 
			this.HeaderContextInsertKeys.DoubleClickEnabled = true;
			this.HeaderContextInsertKeys.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.InsertOverwriteChck});
			this.HeaderContextInsertKeys.Name = "HeaderContextInsertKeys";
			this.HeaderContextInsertKeys.Size = new System.Drawing.Size(278, 22);
			this.HeaderContextInsertKeys.Text = "Insert Selected Key(s) to Other Headers";
			this.HeaderContextInsertKeys.DoubleClick += new System.EventHandler(this.HeaderContextInsertKeys_DoubleClick);
			// 
			// InsertOverwriteChck
			// 
			this.InsertOverwriteChck.Name = "InsertOverwriteChck";
			this.InsertOverwriteChck.Size = new System.Drawing.Size(227, 22);
			this.InsertOverwriteChck.Text = "Overwrite Existing Key Values";
			this.InsertOverwriteChck.Click += new System.EventHandler(this.InsertOverwriteChck_Click);
			// 
			// MenuStrip
			// 
			this.MenuStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileMenuItem,
            this.EditMenuItem});
			this.MenuStrip.Location = new System.Drawing.Point(0, 0);
			this.MenuStrip.Name = "MenuStrip";
			this.MenuStrip.Padding = new System.Windows.Forms.Padding(4, 1, 0, 1);
			this.MenuStrip.Size = new System.Drawing.Size(613, 24);
			this.MenuStrip.TabIndex = 1;
			this.MenuStrip.Text = "menuStrip1";
			// 
			// FileMenuItem
			// 
			this.FileMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSaveCloseMenuItem,
            this.discardCloseToolStripMenuItem});
			this.FileMenuItem.Name = "FileMenuItem";
			this.FileMenuItem.Size = new System.Drawing.Size(38, 22);
			this.FileMenuItem.Text = "Exit";
			// 
			// FileSaveCloseMenuItem
			// 
			this.FileSaveCloseMenuItem.Name = "FileSaveCloseMenuItem";
			this.FileSaveCloseMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.FileSaveCloseMenuItem.Size = new System.Drawing.Size(199, 22);
			this.FileSaveCloseMenuItem.Text = "Save && Close";
			this.FileSaveCloseMenuItem.Click += new System.EventHandler(this.FileSaveCloseMenuItem_Click);
			// 
			// discardCloseToolStripMenuItem
			// 
			this.discardCloseToolStripMenuItem.Name = "discardCloseToolStripMenuItem";
			this.discardCloseToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.discardCloseToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
			this.discardCloseToolStripMenuItem.Text = "Discard && Close";
			this.discardCloseToolStripMenuItem.Click += new System.EventHandler(this.discardCloseToolStripMenuItem_Click);
			// 
			// EditMenuItem
			// 
			this.EditMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.EditCopyfromFileBtn,
            this.EditClearMenu});
			this.EditMenuItem.Name = "EditMenuItem";
			this.EditMenuItem.Size = new System.Drawing.Size(39, 22);
			this.EditMenuItem.Text = "Edit";
			// 
			// EditCopyfromFileBtn
			// 
			this.EditCopyfromFileBtn.Name = "EditCopyfromFileBtn";
			this.EditCopyfromFileBtn.Size = new System.Drawing.Size(152, 22);
			this.EditCopyfromFileBtn.Text = "Copy from File";
			this.EditCopyfromFileBtn.Click += new System.EventHandler(this.EditCopyfromFileBtn_Click);
			// 
			// EditClearMenu
			// 
			this.EditClearMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.EditClearBtn});
			this.EditClearMenu.Name = "EditClearMenu";
			this.EditClearMenu.Size = new System.Drawing.Size(152, 22);
			this.EditClearMenu.Text = "Clear";
			// 
			// EditClearBtn
			// 
			this.EditClearBtn.Name = "EditClearBtn";
			this.EditClearBtn.Size = new System.Drawing.Size(101, 22);
			this.EditClearBtn.Text = "Clear";
			this.EditClearBtn.Click += new System.EventHandler(this.EditClearBtn_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(219, 246);
			this.CancelBtn.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(61, 31);
			this.CancelBtn.TabIndex = 2;
			this.CancelBtn.Text = "cancel button";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// OKbtn
			// 
			this.OKbtn.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.OKbtn.Location = new System.Drawing.Point(219, 303);
			this.OKbtn.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.OKbtn.Name = "OKbtn";
			this.OKbtn.Size = new System.Drawing.Size(61, 31);
			this.OKbtn.TabIndex = 3;
			this.OKbtn.Text = "OK save button";
			this.OKbtn.UseVisualStyleBackColor = true;
			// 
			// FITSHeaderViewer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(613, 606);
			this.Controls.Add(this.HeaderKeysListBox);
			this.Controls.Add(this.MenuStrip);
			this.Controls.Add(this.OKbtn);
			this.Controls.Add(this.CancelBtn);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MainMenuStrip = this.MenuStrip;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(625, 633);
			this.Name = "FITSHeaderViewer";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Header";
			this.TopMost = true;
			this.HeaderContextMenu.ResumeLayout(false);
			this.MenuStrip.ResumeLayout(false);
			this.MenuStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion		

		public System.Windows.Forms.ListBox HeaderKeysListBox;
		private System.Windows.Forms.MenuStrip MenuStrip;
		private System.Windows.Forms.ToolStripMenuItem EditMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FileMenuItem;
		private System.Windows.Forms.ToolStripMenuItem FileSaveCloseMenuItem;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Button OKbtn;
		private System.Windows.Forms.ToolStripMenuItem discardCloseToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip HeaderContextMenu;
		private System.Windows.Forms.ToolStripMenuItem HeaderContextEditKey;
		private System.Windows.Forms.ToolStripMenuItem HeaderContextRemoveKeys;
		private System.Windows.Forms.ToolStripMenuItem HeaderContextAddKey;
		private System.Windows.Forms.ToolStripMenuItem HeaderContextApplyAll;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem HeaderContextCopyValuesList;
		private System.Windows.Forms.ToolStripMenuItem CopyIncludeKeyNameHeaderChck;
		private System.Windows.Forms.ToolStripMenuItem HeaderContextInsertKeys;
		private System.Windows.Forms.ToolStripMenuItem InsertOverwriteChck;
		private System.Windows.Forms.ToolStripMenuItem EditCopyfromFileBtn;
		private System.Windows.Forms.ToolStripMenuItem EditClearMenu;
		private System.Windows.Forms.ToolStripMenuItem EditClearBtn;
	}
}