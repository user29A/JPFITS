namespace JPFITS
{
    partial class FITSImageViewer
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.mainMenu_File = new System.Windows.Forms.ToolStripMenuItem();
            this.mainMenu_Scaling = new System.Windows.Forms.ToolStripMenuItem();
            this.linearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.squareRootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.squaredToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.logToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainMenu_Color = new System.Windows.Forms.ToolStripMenuItem();
            this.grayscaleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.jetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.linesContoursEdgesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainMenu_Contrast = new System.Windows.Forms.ToolStripMenuItem();
            this.fullToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.darkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.minimumStdvToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBox1 = new System.Windows.Forms.ToolStripTextBox();
            this.maximumStdvToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBox2 = new System.Windows.Forms.ToolStripTextBox();
            this.ImageWindow = new System.Windows.Forms.PictureBox();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ImageWindow)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mainMenu_File,
            this.mainMenu_Scaling,
            this.mainMenu_Color,
            this.mainMenu_Contrast});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1086, 35);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // mainMenu_File
            // 
            this.mainMenu_File.Name = "mainMenu_File";
            this.mainMenu_File.Size = new System.Drawing.Size(54, 29);
            this.mainMenu_File.Text = "File";
            // 
            // mainMenu_Scaling
            // 
            this.mainMenu_Scaling.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.linearToolStripMenuItem,
            this.squareRootToolStripMenuItem,
            this.squaredToolStripMenuItem,
            this.logToolStripMenuItem});
            this.mainMenu_Scaling.Name = "mainMenu_Scaling";
            this.mainMenu_Scaling.Size = new System.Drawing.Size(84, 29);
            this.mainMenu_Scaling.Text = "Scaling";
            // 
            // linearToolStripMenuItem
            // 
            this.linearToolStripMenuItem.CheckOnClick = true;
            this.linearToolStripMenuItem.Name = "linearToolStripMenuItem";
            this.linearToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.linearToolStripMenuItem.Text = "Linear";
            // 
            // squareRootToolStripMenuItem
            // 
            this.squareRootToolStripMenuItem.CheckOnClick = true;
            this.squareRootToolStripMenuItem.Name = "squareRootToolStripMenuItem";
            this.squareRootToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.squareRootToolStripMenuItem.Text = "Square Root";
            // 
            // squaredToolStripMenuItem
            // 
            this.squaredToolStripMenuItem.CheckOnClick = true;
            this.squaredToolStripMenuItem.Name = "squaredToolStripMenuItem";
            this.squaredToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.squaredToolStripMenuItem.Text = "Squared";
            // 
            // logToolStripMenuItem
            // 
            this.logToolStripMenuItem.CheckOnClick = true;
            this.logToolStripMenuItem.Name = "logToolStripMenuItem";
            this.logToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.logToolStripMenuItem.Text = "Log";
            // 
            // mainMenu_Color
            // 
            this.mainMenu_Color.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.grayscaleToolStripMenuItem,
            this.jetToolStripMenuItem,
            this.linesContoursEdgesToolStripMenuItem});
            this.mainMenu_Color.Name = "mainMenu_Color";
            this.mainMenu_Color.Size = new System.Drawing.Size(71, 29);
            this.mainMenu_Color.Text = "Color";
            // 
            // grayscaleToolStripMenuItem
            // 
            this.grayscaleToolStripMenuItem.Name = "grayscaleToolStripMenuItem";
            this.grayscaleToolStripMenuItem.Size = new System.Drawing.Size(313, 34);
            this.grayscaleToolStripMenuItem.Text = "Grayscale";
            // 
            // jetToolStripMenuItem
            // 
            this.jetToolStripMenuItem.Name = "jetToolStripMenuItem";
            this.jetToolStripMenuItem.Size = new System.Drawing.Size(313, 34);
            this.jetToolStripMenuItem.Text = "Jet";
            // 
            // linesContoursEdgesToolStripMenuItem
            // 
            this.linesContoursEdgesToolStripMenuItem.Name = "linesContoursEdgesToolStripMenuItem";
            this.linesContoursEdgesToolStripMenuItem.Size = new System.Drawing.Size(313, 34);
            this.linesContoursEdgesToolStripMenuItem.Text = "Lines (Contours && Edges)";
            // 
            // mainMenu_Contrast
            // 
            this.mainMenu_Contrast.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fullToolStripMenuItem,
            this.wideToolStripMenuItem,
            this.darkToolStripMenuItem,
            this.toolStripSeparator1,
            this.minimumStdvToolStripMenuItem,
            this.toolStripTextBox1,
            this.maximumStdvToolStripMenuItem,
            this.toolStripTextBox2});
            this.mainMenu_Contrast.Name = "mainMenu_Contrast";
            this.mainMenu_Contrast.Size = new System.Drawing.Size(95, 29);
            this.mainMenu_Contrast.Text = "Contrast";
            // 
            // fullToolStripMenuItem
            // 
            this.fullToolStripMenuItem.Name = "fullToolStripMenuItem";
            this.fullToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.fullToolStripMenuItem.Text = "Full";
            // 
            // wideToolStripMenuItem
            // 
            this.wideToolStripMenuItem.Name = "wideToolStripMenuItem";
            this.wideToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.wideToolStripMenuItem.Text = "Wide";
            // 
            // darkToolStripMenuItem
            // 
            this.darkToolStripMenuItem.Name = "darkToolStripMenuItem";
            this.darkToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.darkToolStripMenuItem.Text = "Dark";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(267, 6);
            // 
            // minimumStdvToolStripMenuItem
            // 
            this.minimumStdvToolStripMenuItem.Name = "minimumStdvToolStripMenuItem";
            this.minimumStdvToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.minimumStdvToolStripMenuItem.Text = "Minimum Stdv";
            // 
            // toolStripTextBox1
            // 
            this.toolStripTextBox1.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.toolStripTextBox1.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.toolStripTextBox1.Name = "toolStripTextBox1";
            this.toolStripTextBox1.Size = new System.Drawing.Size(100, 31);
            // 
            // maximumStdvToolStripMenuItem
            // 
            this.maximumStdvToolStripMenuItem.Name = "maximumStdvToolStripMenuItem";
            this.maximumStdvToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.maximumStdvToolStripMenuItem.Text = "Maximum Stdv";
            // 
            // toolStripTextBox2
            // 
            this.toolStripTextBox2.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.toolStripTextBox2.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.toolStripTextBox2.Name = "toolStripTextBox2";
            this.toolStripTextBox2.Size = new System.Drawing.Size(100, 31);
            // 
            // ImageWindow
            // 
            this.ImageWindow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ImageWindow.Location = new System.Drawing.Point(0, 35);
            this.ImageWindow.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ImageWindow.Name = "ImageWindow";
            this.ImageWindow.Size = new System.Drawing.Size(1086, 905);
            this.ImageWindow.TabIndex = 1;
            this.ImageWindow.TabStop = false;
            this.ImageWindow.Paint += new System.Windows.Forms.PaintEventHandler(this.ImageWindow_Paint);
            // 
            // FITSImageViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1086, 940);
            this.Controls.Add(this.ImageWindow);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FITSImageViewer";
            this.ShowIcon = false;
            this.Text = "FITS Image Viewer";
            this.Load += new System.EventHandler(this.FITSImageViewer_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ImageWindow)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem mainMenu_File;
        private System.Windows.Forms.PictureBox ImageWindow;
        private System.Windows.Forms.ToolStripMenuItem mainMenu_Scaling;
        private System.Windows.Forms.ToolStripMenuItem mainMenu_Color;
        private System.Windows.Forms.ToolStripMenuItem mainMenu_Contrast;
        private System.Windows.Forms.ToolStripMenuItem linearToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem squareRootToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem squaredToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem logToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem grayscaleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem jetToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem linesContoursEdgesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fullToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem wideToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem darkToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem minimumStdvToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBox1;
        private System.Windows.Forms.ToolStripMenuItem maximumStdvToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBox2;
    }
}