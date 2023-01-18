namespace JPFITS
{
	partial class FITSHeaderKeyDialog
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
			this.KeyNameTxt = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.KeyValueTxt = new System.Windows.Forms.TextBox();
			this.KeyCommentTxt = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.OKBtn = new System.Windows.Forms.Button();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.CommentKeyLineTxt = new System.Windows.Forms.TextBox();
			this.CommentKeyLineChck = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// KeyNameTxt
			// 
			this.KeyNameTxt.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.KeyNameTxt.Font = new System.Drawing.Font("Courier New", 9F);
			this.KeyNameTxt.Location = new System.Drawing.Point(9, 31);
			this.KeyNameTxt.MaxLength = 8;
			this.KeyNameTxt.Name = "KeyNameTxt";
			this.KeyNameTxt.Size = new System.Drawing.Size(67, 21);
			this.KeyNameTxt.TabIndex = 0;
			this.KeyNameTxt.TextChanged += new System.EventHandler(this.KeyNameTxt_TextChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label1.Location = new System.Drawing.Point(6, 15);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(56, 13);
			this.label1.TabIndex = 88;
			this.label1.Text = "Key Name";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label2.Location = new System.Drawing.Point(79, 15);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(55, 13);
			this.label2.TabIndex = 89;
			this.label2.Text = "Key Value";
			// 
			// KeyValueTxt
			// 
			this.KeyValueTxt.Font = new System.Drawing.Font("Courier New", 9F);
			this.KeyValueTxt.Location = new System.Drawing.Point(82, 31);
			this.KeyValueTxt.MaxLength = 18;
			this.KeyValueTxt.Name = "KeyValueTxt";
			this.KeyValueTxt.Size = new System.Drawing.Size(136, 21);
			this.KeyValueTxt.TabIndex = 1;
			this.KeyValueTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			// 
			// KeyCommentTxt
			// 
			this.KeyCommentTxt.Font = new System.Drawing.Font("Courier New", 9F);
			this.KeyCommentTxt.Location = new System.Drawing.Point(224, 31);
			this.KeyCommentTxt.MaxLength = 48;
			this.KeyCommentTxt.Name = "KeyCommentTxt";
			this.KeyCommentTxt.Size = new System.Drawing.Size(347, 21);
			this.KeyCommentTxt.TabIndex = 2;
			this.KeyCommentTxt.TextChanged += new System.EventHandler(this.KeyCommentTxt_TextChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline);
			this.label3.Location = new System.Drawing.Point(221, 15);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(72, 13);
			this.label3.TabIndex = 90;
			this.label3.Text = "Key Comment";
			// 
			// OKBtn
			// 
			this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.OKBtn.Location = new System.Drawing.Point(403, 58);
			this.OKBtn.Name = "OKBtn";
			this.OKBtn.Size = new System.Drawing.Size(75, 27);
			this.OKBtn.TabIndex = 3;
			this.OKBtn.Text = "OK";
			this.OKBtn.UseVisualStyleBackColor = true;
			this.OKBtn.Click += new System.EventHandler(this.OKBtn_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(484, 58);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(75, 27);
			this.CancelBtn.TabIndex = 4;
			this.CancelBtn.Text = "Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			// 
			// CommentKeyLineTxt
			// 
			this.CommentKeyLineTxt.Font = new System.Drawing.Font("Courier New", 9F);
			this.CommentKeyLineTxt.Location = new System.Drawing.Point(9, 31);
			this.CommentKeyLineTxt.MaxLength = 80;
			this.CommentKeyLineTxt.Name = "CommentKeyLineTxt";
			this.CommentKeyLineTxt.Size = new System.Drawing.Size(562, 21);
			this.CommentKeyLineTxt.TabIndex = 91;
			this.CommentKeyLineTxt.Visible = false;
			this.CommentKeyLineTxt.TextChanged += new System.EventHandler(this.CommentKeyLineTxt_TextChanged);
			// 
			// CommentKeyLineChck
			// 
			this.CommentKeyLineChck.AutoSize = true;
			this.CommentKeyLineChck.Location = new System.Drawing.Point(9, 58);
			this.CommentKeyLineChck.Name = "CommentKeyLineChck";
			this.CommentKeyLineChck.Size = new System.Drawing.Size(129, 17);
			this.CommentKeyLineChck.TabIndex = 92;
			this.CommentKeyLineChck.Text = "Line is Comment Form";
			this.CommentKeyLineChck.UseVisualStyleBackColor = true;
			this.CommentKeyLineChck.Visible = false;
			this.CommentKeyLineChck.CheckedChanged += new System.EventHandler(this.CommentKeyLineChck_CheckedChanged);
			// 
			// FITSHeaderKeyDialog
			// 
			this.AcceptButton = this.OKBtn;
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(583, 94);
			this.ControlBox = false;
			this.Controls.Add(this.CommentKeyLineChck);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.OKBtn);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.KeyValueTxt);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.KeyNameTxt);
			this.Controls.Add(this.KeyCommentTxt);
			this.Controls.Add(this.CommentKeyLineTxt);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "FITSHeaderKeyDialog";
			this.RightToLeftLayout = true;
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Edit/Create Key...";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		FITSHeaderKey HEADERLINE;

		public System.Windows.Forms.TextBox KeyNameTxt;
		public System.Windows.Forms.Label label1;
		public System.Windows.Forms.Label label2;
		public System.Windows.Forms.TextBox KeyValueTxt;
		public System.Windows.Forms.TextBox KeyCommentTxt;
		public System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button OKBtn;
		private System.Windows.Forms.Button CancelBtn;
		public System.Windows.Forms.TextBox CommentKeyLineTxt;
		public System.Windows.Forms.CheckBox CommentKeyLineChck;
	}
}