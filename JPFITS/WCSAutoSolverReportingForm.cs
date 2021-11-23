using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace JPFITS
{
	public partial class WCSAutoSolverReportingForm : Form
	{
		public WCSAutoSolverReportingForm(WCSAutoSolver solver)
		{
			InitializeComponent();

			WCSAS = solver;
		}

		public void WCSAutoReportingTimer_Tick(System.Object sender, System.EventArgs e)
		{
			this.MsgTxt.Text = this.WCSAS.Status_Log;
			this.MsgTxt.SelectionStart = this.MsgTxt.TextLength;
			this.MsgTxt.ScrollToCaret();

			if (this.DialogResult == System.Windows.Forms.DialogResult.Cancel)
				this.WCSAS.Cancelled = true;

			if (!this.WCSAS.Solving)
				this.WCSAutoReportingTimer.Enabled = false;
		}
	}
}
