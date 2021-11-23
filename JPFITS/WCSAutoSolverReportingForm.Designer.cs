namespace JPFITS
{
	partial class WCSAutoSolverReportingForm
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
			this.MsgTxt = new System.Windows.Forms.TextBox();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.WCSAutoReportingTimer = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// MsgTxt
			// 
			this.MsgTxt.AcceptsReturn = true;
			this.MsgTxt.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
			| System.Windows.Forms.AnchorStyles.Left)
			| System.Windows.Forms.AnchorStyles.Right)));
			this.MsgTxt.Location = new System.Drawing.Point(12, 13);
			this.MsgTxt.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.MsgTxt.Multiline = true;
			this.MsgTxt.Name = "MsgTxt";
			this.MsgTxt.ReadOnly = true;
			this.MsgTxt.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.MsgTxt.Size = new System.Drawing.Size(574, 860);
			this.MsgTxt.TabIndex = 4;
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(592, 13);
			this.CancelBtn.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(128, 79);
			this.CancelBtn.TabIndex = 3;
			this.CancelBtn.Text = "Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			// 
			// WCSAutoReportingTimer
			// 
			this.WCSAutoReportingTimer.Tick += new System.EventHandler(this.WCSAutoReportingTimer_Tick);
			// 
			// WCSAutoSolverReportingForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(732, 886);
			this.Controls.Add(this.MsgTxt);
			this.Controls.Add(this.CancelBtn);
			this.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
			this.Name = "WCSAutoSolverReportingForm";
			this.Text = "Auto Solver Report...";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private JPFITS.WCSAutoSolver WCSAS;

		public System.Windows.Forms.TextBox MsgTxt;
		public System.Windows.Forms.Button CancelBtn;
		public System.Windows.Forms.Timer WCSAutoReportingTimer;
	}
}