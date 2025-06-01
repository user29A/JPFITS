using System;
using System.Drawing;
using System.Windows.Forms;
#nullable enable

namespace JPFITS
{
    public partial class FITSImageViewer : Form
    {
        bool FIRSTLOAD;
        Bitmap? IMAGEBMP;

        public FITSImageViewer()
        {
            InitializeComponent();

            FIRSTLOAD = true;

        }

        public void ShowImage(FITSImage fitsimg)
        {
            IMAGEBMP = JPBitMap.FITSImageToBmp(fitsimg, 0, 0, false, new double[] { 2 });
            ImageWindow.Refresh();
        }

        private void FITSImageViewer_Load(object sender, EventArgs e)
        {

        }

        private void ImageWindow_Paint(object sender, PaintEventArgs e)
        {
            if (FIRSTLOAD)
                return;

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.DrawImage(IMAGEBMP, new Rectangle(0, 0, ImageWindow.Size.Width, ImageWindow.Size.Height));
        }
                
    }
}
