namespace JPFITS
{
	partial class WaitBar
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
			this.TextMsg = new System.Windows.Forms.Label();
			this.ProgressBar = new System.Windows.Forms.ProgressBar();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.OKBtn = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// TextMsg
			// 
			this.TextMsg.Location = new System.Drawing.Point(12, 14);
			this.TextMsg.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.TextMsg.Name = "TextMsg";
			this.TextMsg.Size = new System.Drawing.Size(366, 13);
			this.TextMsg.TabIndex = 0;
			this.TextMsg.Text = "Please Wait:  0%";
			this.TextMsg.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ProgressBar
			// 
			this.ProgressBar.Location = new System.Drawing.Point(12, 37);
			this.ProgressBar.Margin = new System.Windows.Forms.Padding(2);
			this.ProgressBar.Name = "ProgressBar";
			this.ProgressBar.Size = new System.Drawing.Size(366, 17);
			this.ProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.ProgressBar.TabIndex = 1;
			// 
			// CancelBtn
			// 
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(158, 63);
			this.CancelBtn.Margin = new System.Windows.Forms.Padding(2);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(75, 23);
			this.CancelBtn.TabIndex = 2;
			this.CancelBtn.Text = "Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// OKBtn
			// 
			this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.OKBtn.Location = new System.Drawing.Point(303, 65);
			this.OKBtn.Margin = new System.Windows.Forms.Padding(2);
			this.OKBtn.Name = "OKBtn";
			this.OKBtn.Size = new System.Drawing.Size(75, 23);
			this.OKBtn.TabIndex = 3;
			this.OKBtn.Text = "OK";
			this.OKBtn.UseVisualStyleBackColor = true;
			this.OKBtn.Visible = false;
			this.OKBtn.Click += new System.EventHandler(this.OKBtn_Click);
			// 
			// WaitBar
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(390, 100);
			this.ControlBox = false;
			this.Controls.Add(this.OKBtn);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.ProgressBar);
			this.Controls.Add(this.TextMsg);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Margin = new System.Windows.Forms.Padding(2);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "WaitBar";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = " ";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.WaitBar_FormClosed);
			this.ResumeLayout(false);

		}

		#endregion

		public System.Windows.Forms.Label TextMsg;
		public System.Windows.Forms.ProgressBar ProgressBar;
		public System.Windows.Forms.Button CancelBtn;
		public System.Windows.Forms.Button OKBtn;
	}
}