namespace JPFITS
{
	partial class FITSFinder
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
			this.label11 = new System.Windows.Forms.Label();
			this.ExtensionDrop = new System.Windows.Forms.ComboBox();
			this.SubFoldersChck = new System.Windows.Forms.CheckBox();
			this.FileTemplateTxt = new System.Windows.Forms.TextBox();
			this.label10 = new System.Windows.Forms.Label();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.FindBtn = new System.Windows.Forms.Button();
			this.Key4Value = new System.Windows.Forms.RichTextBox();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.Key3Value = new System.Windows.Forms.RichTextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.Key2Value = new System.Windows.Forms.RichTextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.Key1Value = new System.Windows.Forms.RichTextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.DirectoryTxt = new System.Windows.Forms.RichTextBox();
			this.FitsFinderWrkr = new System.ComponentModel.BackgroundWorker();
			this.Key1 = new System.Windows.Forms.TextBox();
			this.Key2 = new System.Windows.Forms.TextBox();
			this.Key3 = new System.Windows.Forms.TextBox();
			this.Key4 = new System.Windows.Forms.TextBox();
			this.CustomExtensionChck = new System.Windows.Forms.CheckBox();
			this.CustomExtensionTxtBox = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label11.Location = new System.Drawing.Point(100, 99);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(56, 13);
			this.label11.TabIndex = 45;
			this.label11.Text = "Extension:";
			// 
			// ExtensionDrop
			// 
			this.ExtensionDrop.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ExtensionDrop.FormattingEnabled = true;
			this.ExtensionDrop.Items.AddRange(new object[] {
            ".fts",
            ".fit",
            ".fits"});
			this.ExtensionDrop.Location = new System.Drawing.Point(103, 115);
			this.ExtensionDrop.Name = "ExtensionDrop";
			this.ExtensionDrop.Size = new System.Drawing.Size(45, 21);
			this.ExtensionDrop.TabIndex = 32;
			// 
			// SubFoldersChck
			// 
			this.SubFoldersChck.AutoSize = true;
			this.SubFoldersChck.Location = new System.Drawing.Point(162, 72);
			this.SubFoldersChck.Name = "SubFoldersChck";
			this.SubFoldersChck.Size = new System.Drawing.Size(136, 17);
			this.SubFoldersChck.TabIndex = 30;
			this.SubFoldersChck.Text = "Include Sub Directories";
			this.SubFoldersChck.UseVisualStyleBackColor = true;
			// 
			// FileTemplateTxt
			// 
			this.FileTemplateTxt.Location = new System.Drawing.Point(14, 116);
			this.FileTemplateTxt.MaxLength = 100;
			this.FileTemplateTxt.Name = "FileTemplateTxt";
			this.FileTemplateTxt.Size = new System.Drawing.Size(71, 20);
			this.FileTemplateTxt.TabIndex = 31;
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label10.Location = new System.Drawing.Point(11, 100);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(73, 13);
			this.label10.TabIndex = 29;
			this.label10.Text = "File Template:";
			// 
			// CancelBtn
			// 
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(162, 305);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(75, 23);
			this.CancelBtn.TabIndex = 42;
			this.CancelBtn.Text = "&Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// FindBtn
			// 
			this.FindBtn.Location = new System.Drawing.Point(73, 305);
			this.FindBtn.Name = "FindBtn";
			this.FindBtn.Size = new System.Drawing.Size(71, 23);
			this.FindBtn.TabIndex = 41;
			this.FindBtn.Text = "&Find";
			this.FindBtn.UseVisualStyleBackColor = true;
			this.FindBtn.Click += new System.EventHandler(this.FindBtn_Click);
			// 
			// Key4Value
			// 
			this.Key4Value.Location = new System.Drawing.Point(103, 271);
			this.Key4Value.MaxLength = 30;
			this.Key4Value.Multiline = false;
			this.Key4Value.Name = "Key4Value";
			this.Key4Value.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
			this.Key4Value.Size = new System.Drawing.Size(197, 18);
			this.Key4Value.TabIndex = 40;
			this.Key4Value.Text = "";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label8.Location = new System.Drawing.Point(100, 255);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(108, 13);
			this.label8.TabIndex = 25;
			this.label8.Text = "Keyname Four Value:";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label9.Location = new System.Drawing.Point(11, 255);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(78, 13);
			this.label9.TabIndex = 24;
			this.label9.Text = "Keyname Four:";
			// 
			// Key3Value
			// 
			this.Key3Value.Location = new System.Drawing.Point(103, 234);
			this.Key3Value.MaxLength = 30;
			this.Key3Value.Multiline = false;
			this.Key3Value.Name = "Key3Value";
			this.Key3Value.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
			this.Key3Value.Size = new System.Drawing.Size(197, 18);
			this.Key3Value.TabIndex = 38;
			this.Key3Value.Text = "";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label6.Location = new System.Drawing.Point(100, 218);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(115, 13);
			this.label6.TabIndex = 27;
			this.label6.Text = "Keyname Three Value:";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label7.Location = new System.Drawing.Point(11, 218);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(85, 13);
			this.label7.TabIndex = 23;
			this.label7.Text = "Keyname Three:";
			// 
			// Key2Value
			// 
			this.Key2Value.Location = new System.Drawing.Point(103, 197);
			this.Key2Value.MaxLength = 30;
			this.Key2Value.Multiline = false;
			this.Key2Value.Name = "Key2Value";
			this.Key2Value.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
			this.Key2Value.Size = new System.Drawing.Size(197, 18);
			this.Key2Value.TabIndex = 36;
			this.Key2Value.Text = "";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label4.Location = new System.Drawing.Point(100, 181);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(108, 13);
			this.label4.TabIndex = 21;
			this.label4.Text = "Keyname Two Value:";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label5.Location = new System.Drawing.Point(11, 181);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(78, 13);
			this.label5.TabIndex = 22;
			this.label5.Text = "Keyname Two:";
			// 
			// Key1Value
			// 
			this.Key1Value.Location = new System.Drawing.Point(103, 160);
			this.Key1Value.MaxLength = 30;
			this.Key1Value.Multiline = false;
			this.Key1Value.Name = "Key1Value";
			this.Key1Value.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
			this.Key1Value.Size = new System.Drawing.Size(197, 18);
			this.Key1Value.TabIndex = 34;
			this.Key1Value.Text = "";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label3.Location = new System.Drawing.Point(100, 144);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(107, 13);
			this.label3.TabIndex = 30;
			this.label3.Text = "Keyname One Value:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label2.Location = new System.Drawing.Point(11, 144);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(77, 13);
			this.label2.TabIndex = 28;
			this.label2.Text = "Keyname One:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label1.Location = new System.Drawing.Point(11, 27);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(52, 13);
			this.label1.TabIndex = 26;
			this.label1.Text = "Directory:";
			// 
			// DirectoryTxt
			// 
			this.DirectoryTxt.BackColor = System.Drawing.SystemColors.Control;
			this.DirectoryTxt.DetectUrls = false;
			this.DirectoryTxt.Location = new System.Drawing.Point(69, 15);
			this.DirectoryTxt.MaxLength = 500;
			this.DirectoryTxt.Name = "DirectoryTxt";
			this.DirectoryTxt.ReadOnly = true;
			this.DirectoryTxt.Size = new System.Drawing.Size(233, 51);
			this.DirectoryTxt.TabIndex = 29;
			this.DirectoryTxt.Text = "";
			this.DirectoryTxt.Click += new System.EventHandler(this.DirectoryTxt_Click);
			// 
			// FitsFinderWrkr
			// 
			this.FitsFinderWrkr.WorkerReportsProgress = true;
			this.FitsFinderWrkr.WorkerSupportsCancellation = true;
			this.FitsFinderWrkr.DoWork += new System.ComponentModel.DoWorkEventHandler(this.FitsFinderWrkr_DoWork);
			this.FitsFinderWrkr.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.FitsFinderWrkr_ProgressChanged);
			this.FitsFinderWrkr.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.FitsFinderWrkr_RunWorkerCompleted);
			// 
			// Key1
			// 
			this.Key1.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.Key1.Location = new System.Drawing.Point(14, 160);
			this.Key1.MaxLength = 8;
			this.Key1.Name = "Key1";
			this.Key1.Size = new System.Drawing.Size(71, 20);
			this.Key1.TabIndex = 33;
			// 
			// Key2
			// 
			this.Key2.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.Key2.Location = new System.Drawing.Point(13, 197);
			this.Key2.MaxLength = 8;
			this.Key2.Name = "Key2";
			this.Key2.Size = new System.Drawing.Size(71, 20);
			this.Key2.TabIndex = 35;
			// 
			// Key3
			// 
			this.Key3.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.Key3.Location = new System.Drawing.Point(14, 234);
			this.Key3.MaxLength = 8;
			this.Key3.Name = "Key3";
			this.Key3.Size = new System.Drawing.Size(71, 20);
			this.Key3.TabIndex = 37;
			// 
			// Key4
			// 
			this.Key4.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.Key4.Location = new System.Drawing.Point(14, 271);
			this.Key4.MaxLength = 8;
			this.Key4.Name = "Key4";
			this.Key4.Size = new System.Drawing.Size(71, 20);
			this.Key4.TabIndex = 39;
			// 
			// CustomExtensionChck
			// 
			this.CustomExtensionChck.AutoSize = true;
			this.CustomExtensionChck.Location = new System.Drawing.Point(162, 99);
			this.CustomExtensionChck.Name = "CustomExtensionChck";
			this.CustomExtensionChck.Size = new System.Drawing.Size(61, 17);
			this.CustomExtensionChck.TabIndex = 46;
			this.CustomExtensionChck.Text = "Custom";
			this.CustomExtensionChck.UseVisualStyleBackColor = true;
			this.CustomExtensionChck.CheckedChanged += new System.EventHandler(this.CustomExtensionChck_CheckedChanged);
			// 
			// CustomExtensionTxtBox
			// 
			this.CustomExtensionTxtBox.Enabled = false;
			this.CustomExtensionTxtBox.Location = new System.Drawing.Point(162, 116);
			this.CustomExtensionTxtBox.Name = "CustomExtensionTxtBox";
			this.CustomExtensionTxtBox.Size = new System.Drawing.Size(100, 20);
			this.CustomExtensionTxtBox.TabIndex = 47;
			this.CustomExtensionTxtBox.Text = ".events";
			this.CustomExtensionTxtBox.TextChanged += new System.EventHandler(this.CustomExtensionTxtBox_TextChanged);
			// 
			// FITSFinder
			// 
			this.AcceptButton = this.FindBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(313, 343);
			this.ControlBox = false;
			this.Controls.Add(this.CustomExtensionTxtBox);
			this.Controls.Add(this.CustomExtensionChck);
			this.Controls.Add(this.Key4);
			this.Controls.Add(this.Key3);
			this.Controls.Add(this.Key2);
			this.Controls.Add(this.Key1);
			this.Controls.Add(this.label11);
			this.Controls.Add(this.ExtensionDrop);
			this.Controls.Add(this.SubFoldersChck);
			this.Controls.Add(this.FileTemplateTxt);
			this.Controls.Add(this.label10);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.FindBtn);
			this.Controls.Add(this.Key4Value);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.label9);
			this.Controls.Add(this.Key3Value);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.Key2Value);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.Key1Value);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.DirectoryTxt);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "FITSFinder";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Search for FITS File(s)...";
			this.Load += new System.EventHandler(this.FitsFinder_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private WaitBar WAITBAR;
		private string[] FOUNDFILES;

		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.CheckBox SubFoldersChck;
		private System.Windows.Forms.TextBox FileTemplateTxt;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Button FindBtn;
		private System.Windows.Forms.RichTextBox Key4Value;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.RichTextBox Key3Value;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.RichTextBox Key2Value;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.RichTextBox Key1Value;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RichTextBox DirectoryTxt;
		private System.ComponentModel.BackgroundWorker FitsFinderWrkr;
		private System.Windows.Forms.TextBox Key1;
		private System.Windows.Forms.TextBox Key2;
		private System.Windows.Forms.TextBox Key3;
		private System.Windows.Forms.TextBox Key4;
		private System.Windows.Forms.CheckBox CustomExtensionChck;
		private System.Windows.Forms.TextBox CustomExtensionTxtBox;
		private System.Windows.Forms.ComboBox ExtensionDrop;
	}
}