using System;
using System.Windows.Forms;

namespace JPFITS
{
	public partial class WaitBar : Form
	{
		public bool CANCELLED = false;

		public WaitBar()
		{
			InitializeComponent();
		}

		private void WaitBar_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (!CANCELLED)
				this.DialogResult = DialogResult.OK;
			else
				this.DialogResult = DialogResult.Cancel;
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			CANCELLED = true;
		}

		private void OKBtn_Click(object sender, EventArgs e)
		{
			CANCELLED = false;
		}
	}
}