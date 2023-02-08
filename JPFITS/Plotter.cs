using System;
using System.Windows.Forms;

namespace JPFITS
{
	public partial class Plotter : Form
	{
		bool SAVEPOSITION;
		bool OPENPOSITION;

		public Plotter(string name, bool savePosition, bool openPosition)
		{
			InitializeComponent();

			this.Name = name;
			SAVEPOSITION= savePosition;
			OPENPOSITION= openPosition;
		}

		private void EscBtn_Click(object sender, EventArgs e)
		{
			this.Close();
		}
		
		private void Plotter_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (SAVEPOSITION && this.Name != "")
			{
				REG.SetReg("CCDLAB", this.Name + "PlotterPOSX", this.Location.X);
				REG.SetReg("CCDLAB", this.Name + "PlotterPOSY", this.Location.Y);
				REG.SetReg("CCDLAB", this.Name + "PlotterWIDTH", this.Size.Width);
				REG.SetReg("CCDLAB", this.Name + "PlotterHEIGHT", this.Size.Height);
			}
		}

		private void Plotter_Load(object sender, EventArgs e)
		{
			if (OPENPOSITION && this.Name != "")
			{
				try
				{
					this.Left = (int)REG.GetReg("CCDLAB", this.Name + "PlotterPOSX");
					this.Top = (int)REG.GetReg("CCDLAB", this.Name + "PlotterPOSY");
					this.Width = (int)REG.GetReg("CCDLAB", this.Name + "PlotterWIDTH");
					this.Height = (int)REG.GetReg("CCDLAB", this.Name + "PlotterHEIGHT");
				}
				catch { }
			}
		}
	}
}
