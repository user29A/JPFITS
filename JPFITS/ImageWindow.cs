using System;
using System.Drawing;
using System.Windows.Forms;
#nullable enable

namespace JPFITS
{
    public partial class FITSImageWindow : PictureBox
    {
        Bitmap? IMAGEBMP;

        public FITSImageWindow()
        {
            InitializeComponent();
        }

        private void ImageWindow_Load(object sender, EventArgs e)
        {

        }

        private void ImageWindow_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.DrawImage(IMAGEBMP, new Rectangle(0, 0, this.Size.Width, this.Size.Height));
        }

        public void DrawImage(Bitmap bmp)
        {
            IMAGEBMP = bmp;
            this.Refresh();
        }
    }
}
