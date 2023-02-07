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
			System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
			this.ChartGraph = new JPChart.JPChartControl();
			this.EscBtn = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.ChartGraph)).BeginInit();
			this.SuspendLayout();
			// 
			// ChartGraph
			// 
			this.ChartGraph.BackColor = System.Drawing.Color.DarkGray;
			this.ChartGraph.BackGradientStyle = System.Windows.Forms.DataVisualization.Charting.GradientStyle.TopBottom;
			this.ChartGraph.BackSecondaryColor = System.Drawing.Color.Gainsboro;
			this.ChartGraph.BorderlineColor = System.Drawing.Color.Black;
			chartArea2.AxisX.MajorGrid.Enabled = false;
			chartArea2.AxisX.Maximum = 0D;
			chartArea2.AxisX.Minimum = 0D;
			chartArea2.AxisX.ScrollBar.Enabled = false;
			chartArea2.AxisY.MajorGrid.Enabled = false;
			chartArea2.AxisY.Maximum = 0D;
			chartArea2.AxisY.Minimum = 0D;
			chartArea2.AxisY.ScrollBar.Enabled = false;
			chartArea2.BorderDashStyle = System.Windows.Forms.DataVisualization.Charting.ChartDashStyle.Solid;
			chartArea2.Name = "ChartArea1";
			this.ChartGraph.ChartAreas.Add(chartArea2);
			this.ChartGraph.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ChartGraph.Location = new System.Drawing.Point(0, 0);
			this.ChartGraph.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.ChartGraph.Name = "ChartGraph";
			this.ChartGraph.Size = new System.Drawing.Size(848, 683);
			this.ChartGraph.TabIndex = 0;
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
			this.Controls.Add(this.ChartGraph);
			this.Controls.Add(this.EscBtn);
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.Name = "Plotter";
			this.ShowIcon = false;
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Plotter_FormClosing);
			this.Load += new System.EventHandler(this.Plotter_Load);
			((System.ComponentModel.ISupportInitialize)(this.ChartGraph)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		public JPChart.JPChartControl ChartGraph;
		private System.Windows.Forms.Button EscBtn;
	}
}