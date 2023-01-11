namespace JPFITS
{
	partial class Plotter
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
			System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
			this.jpChart1 = new JPChart.JPChart();
			this.EscBtn = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.jpChart1)).BeginInit();
			this.SuspendLayout();
			// 
			// jpChart1
			// 
			this.jpChart1.BackColor = System.Drawing.Color.DarkGray;
			this.jpChart1.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.TopBottom;
			chartArea1.AxisX.MajorGrid.Enabled = false;
			chartArea1.AxisX.ScrollBar.Enabled = false;
			chartArea1.AxisY.MajorGrid.Enabled = false;
			chartArea1.AxisY.ScrollBar.Enabled = false;
			chartArea1.Name = "ChartArea1";
			this.jpChart1.ChartAreas.Add(chartArea1);
			this.jpChart1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.jpChart1.Location = new System.Drawing.Point(0, 0);
			this.jpChart1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.jpChart1.Name = "jpChart1";
			this.jpChart1.Size = new System.Drawing.Size(848, 683);
			this.jpChart1.TabIndex = 0;
			this.jpChart1.Text = "jpChart1";
			// 
			// EscBtn
			// 
			this.EscBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.EscBtn.Location = new System.Drawing.Point(292, 308);
			this.EscBtn.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.EscBtn.Name = "EscBtn";
			this.EscBtn.Size = new System.Drawing.Size(112, 35);
			this.EscBtn.TabIndex = 1;
			this.EscBtn.Text = "button1";
			this.EscBtn.UseVisualStyleBackColor = true;
			this.EscBtn.Click += new System.EventHandler(this.EscBtn_Click);
			// 
			// Plotter
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.EscBtn;
			this.ClientSize = new System.Drawing.Size(848, 683);
			this.Controls.Add(this.jpChart1);
			this.Controls.Add(this.EscBtn);
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.Name = "Plotter";
			this.ShowIcon = false;
			this.Text = "FitsExtensionTablePlotter";
			((System.ComponentModel.ISupportInitialize)(this.jpChart1)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		public JPChart.JPChart jpChart1;
		private System.Windows.Forms.Button EscBtn;
	}
}