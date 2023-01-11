namespace JPFITS
{
	partial class FITSImageExtensionsLister
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
			this.SelectAllBtn = new System.Windows.Forms.Button();
			this.ClearSelectionBtn = new System.Windows.Forms.Button();
			this.IncludePrimaryChck = new System.Windows.Forms.CheckBox();
			this.ReturnSelectionBtn = new System.Windows.Forms.Button();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.ExtensionListBox = new System.Windows.Forms.ListBox();
			this.SuspendLayout();
			// 
			// SelectAllBtn
			// 
			this.SelectAllBtn.Location = new System.Drawing.Point(177, 6);
			this.SelectAllBtn.Name = "SelectAllBtn";
			this.SelectAllBtn.Size = new System.Drawing.Size(99, 23);
			this.SelectAllBtn.TabIndex = 3;
			this.SelectAllBtn.Text = "Select All";
			this.SelectAllBtn.UseVisualStyleBackColor = true;
			this.SelectAllBtn.Click += new System.EventHandler(this.SelectAllBtn_Click);
			// 
			// ClearSelectionBtn
			// 
			this.ClearSelectionBtn.Location = new System.Drawing.Point(177, 35);
			this.ClearSelectionBtn.Name = "ClearSelectionBtn";
			this.ClearSelectionBtn.Size = new System.Drawing.Size(99, 23);
			this.ClearSelectionBtn.TabIndex = 4;
			this.ClearSelectionBtn.Text = "Clear Selection";
			this.ClearSelectionBtn.UseVisualStyleBackColor = true;
			this.ClearSelectionBtn.Click += new System.EventHandler(this.ClearSelectionBtn_Click);
			// 
			// IncludePrimaryChck
			// 
			this.IncludePrimaryChck.Location = new System.Drawing.Point(177, 64);
			this.IncludePrimaryChck.Name = "IncludePrimaryChck";
			this.IncludePrimaryChck.Size = new System.Drawing.Size(99, 26);
			this.IncludePrimaryChck.TabIndex = 6;
			this.IncludePrimaryChck.Text = "Include Primary";
			this.IncludePrimaryChck.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.IncludePrimaryChck.UseVisualStyleBackColor = true;
			// 
			// ReturnSelectionBtn
			// 
			this.ReturnSelectionBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.ReturnSelectionBtn.Location = new System.Drawing.Point(177, 227);
			this.ReturnSelectionBtn.Name = "ReturnSelectionBtn";
			this.ReturnSelectionBtn.Size = new System.Drawing.Size(99, 23);
			this.ReturnSelectionBtn.TabIndex = 8;
			this.ReturnSelectionBtn.Text = "Return Selection";
			this.ReturnSelectionBtn.UseVisualStyleBackColor = true;
			this.ReturnSelectionBtn.Click += new System.EventHandler(this.ReturnSelectionBtn_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(177, 256);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(99, 23);
			this.CancelBtn.TabIndex = 7;
			this.CancelBtn.Text = "Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// ExtensionListBox
			// 
			this.ExtensionListBox.Dock = System.Windows.Forms.DockStyle.Left;
			this.ExtensionListBox.FormattingEnabled = true;
			this.ExtensionListBox.Location = new System.Drawing.Point(0, 0);
			this.ExtensionListBox.Name = "ExtensionListBox";
			this.ExtensionListBox.ScrollAlwaysVisible = true;
			this.ExtensionListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.ExtensionListBox.Size = new System.Drawing.Size(171, 282);
			this.ExtensionListBox.TabIndex = 10;
			this.ExtensionListBox.SelectedIndexChanged += new System.EventHandler(this.ExtensionListBox_SelectedIndexChanged);
			this.ExtensionListBox.DoubleClick += new System.EventHandler(this.ExtensionListBox_DoubleClick);
			// 
			// FITSImageExtensionsLister
			// 
			this.AcceptButton = this.ReturnSelectionBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(281, 282);
			this.ControlBox = false;
			this.Controls.Add(this.ExtensionListBox);
			this.Controls.Add(this.ReturnSelectionBtn);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.IncludePrimaryChck);
			this.Controls.Add(this.ClearSelectionBtn);
			this.Controls.Add(this.SelectAllBtn);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Margin = new System.Windows.Forms.Padding(2);
			this.Name = "FITSImageExtensionsLister";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Image Extensions";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button SelectAllBtn;
		private System.Windows.Forms.Button ClearSelectionBtn;
		public System.Windows.Forms.CheckBox IncludePrimaryChck;
		private System.Windows.Forms.Button ReturnSelectionBtn;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.ListBox ExtensionListBox;
	}
}