using System;
using System.Windows.Forms;



namespace JPFITS
{
	public partial class Plotter : Form
	{
		public Plotter()
		{
			InitializeComponent();
		}

		private void EscBtn_Click(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}
