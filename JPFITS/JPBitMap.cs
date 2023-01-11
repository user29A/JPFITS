using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

namespace JPFITS
{
	/// <summary>
	/// Class for converting 2D image data arrays to bitmaps, including three layer images for color image bitmaps, and single layers as grayscale.
	/// </summary>
	public class JPBitMap
	{
		/// <summary>
		/// Convert a single-layer 2D image to a bitmap.
		/// </summary>
		/// <param name="fitsImg">The FITSImage to pull the 2D array from to make the bitmap with.</param>
		/// <param name="scaling">Data scaling: <br />0 = linear; <br />1 = square root; <br />2 = squared; 3 = log.</param>
		/// <param name="color">Artificial color mapping: 
		/// <br />0 = grayscale; 
		/// <br />1 = jet; 
		/// <br />2 = winter; 
		/// <br />3 = lines (detects contours and edges).</param>
		/// <param name="invert">Invert tone...i.e. black becomes white.</param>
		/// <param name="stdImclim">Pass a single-element array to specify a pre-set contrast limit as per below, or pass a 2-element array to specify the literal contrast limits in terms of standard deviation about the image mean.
		/// <br />Contrast limits:
		/// <br />0 = full range (min to max);
		/// <br />1 = wide (-1 stdv to 2 stdv, stdImclim = [-1, 2]);
		/// <br />2 = dark (-0.5 stdv to 5 stdv, stdImclim = [-0.5, 5]);
		/// <br />any other single-element value throws an exception.</param>
		public static unsafe Bitmap FITSImageToBmp(FITSImage fitsImg, int scaling, int color, bool invert, double[] stdImclim)
		{
			double[] dimclim;
			if (stdImclim.Length == 1)
				if (stdImclim[0] == 0)
					dimclim = new double[] { fitsImg.Min, fitsImg.Max };
				else if (stdImclim[0] == 1)
					dimclim = new double[] { fitsImg.Mean - fitsImg.Stdv, fitsImg.Mean + fitsImg.Stdv * 2 };
				else if (stdImclim[0] == 2)
					dimclim = new double[] { fitsImg.Mean - fitsImg.Stdv * 0.5, fitsImg.Mean + fitsImg.Stdv * 5 };
				else
					throw new Exception("Image contrast limit \"stdImclim\" value invalid: " + stdImclim[0]);
			else if (stdImclim.Length == 2)
				dimclim = new double[] { fitsImg.Mean + fitsImg.Stdv * stdImclim[0], fitsImg.Mean + fitsImg.Stdv * stdImclim[1] };
			else
				throw new Exception("Something wrong with parameter \"stdImclim\"");

			return ArrayToBmp(fitsImg.Image, scaling, color, invert, dimclim, Int32.MaxValue, Int32.MaxValue, false);
		}

		/// <summary>
		/// Convert a single layer 2D image to bitmap.
		/// </summary>
		/// <param name="image">The image data array.</param>
		/// <param name="scaling">Data scaling: 
		/// <br />0 = linear; 
		/// <br />1 = square root; 
		/// <br />2 = squared; 
		/// <br />3 = log.</param>
		/// <param name="colour">Artificial color mapping: 
		/// <br />0 = grayscale; 
		/// <br />1 = jet; 
		/// <br />2 = winter; 
		/// <br />3 = lines (detects contours and edges).</param>
		/// <param name="invert">Invert tone...i.e. black becomes white.</param>
		/// <param name="DImCLim">The image contrast limits. A 2-element vector which clips the low (element 1) and high (element 2) values of the image array when forming the bitmap. 
		/// <br />Suggest [mean(image)-0.5*stdv(image), mean(image)+5*stdv(image)]</param>
		/// <param name="WinWidth">If it is a small image required, the function will bin if necessary. If no binning desired then set to Int32.Maxvalue.</param>
		/// <param name="WinHeight">If it is a small image required, the function will bin if necessary. If no binning desired then set to Int32.Maxvalue.</param>
		/// <param name="invertYaxis">Flip the image vertically...i.e. about the central horizontal axis.</param>
		public static unsafe Bitmap ArrayToBmp(double[,] image, int scaling, int colour, bool invert, double[] DImCLim, int WinWidth, int WinHeight, bool invertYaxis)
		{
			if (image.GetLength(0) > WinWidth * 2 || image.GetLength(1) > WinHeight * 2)
			{
				int Nx = 1;
				int Ny = 1;
				if (image.GetLength(0) > WinWidth * 2)
					Nx = image.GetLength(0) / WinWidth;
				if (image.GetLength(1) > WinHeight * 2)
					Ny = image.GetLength(1) / WinHeight;
				if (Nx > 1 || Ny > 1)
					image = Bin(image, Nx, Ny);
			}

			Bitmap bmp = new Bitmap(image.GetLength(0), image.GetLength(1), PixelFormat.Format24bppRgb);
			BitmapData data = bmp.LockBits(new Rectangle(0, 0, image.GetLength(0), image.GetLength(1)), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
			byte* bits = (byte*)data.Scan0.ToPointer();

			int bytesPerPixel = 3; // 3 bytes per pixel for 24 bpp rgb
			int height = data.Height;
			int width = data.Width;
			int stride = data.Stride;
			int bytesWidth = width * bytesPerPixel;
			double invDImCLimRange = 1 / (DImCLim[1] - DImCLim[0]);

			Parallel.For(0, height, i =>
			{
				int istride = i * stride;
				int jcounter = -1;
				for (int j = 0; j < bytesWidth; j += bytesPerPixel)
				{
					jcounter++;
					double val = image[jcounter, i];
					if (val < DImCLim[0])
						val = DImCLim[0];
					else if (val > DImCLim[1])
						val = DImCLim[1];
					val = (val - DImCLim[0]) * invDImCLimRange;

					switch (scaling)
					{
						case (0)://linear
						{
							val = val * 255;
							break;
						}
						case (1)://square root
						{
							val = Math.Sqrt(val) * 255;
							break;
						}
						case (2)://squared
						{
							val = val * val * 255;
							break;
						}
						case (3)://log
						{
							val = Math.Log(Math.Sqrt(val * val) + 1) * 255;
							break;
						}
					}

					if (invert)
						val = 255 - val;

					switch (colour)
					{
						case (0)://grayscale
						{
							bits[istride + j + 0] = (byte)(val);   // blue
							bits[istride + j + 1] = (byte)(val); // green
							bits[istride + j + 2] = (byte)(val);   // red
							break;
						}
						case (1)://Jet
						{
							bits[istride + j + 0] = (byte)(JetB(val));   // blue
							bits[istride + j + 1] = (byte)(JetG(val)); // green
							bits[istride + j + 2] = (byte)(JetR(val));   // red
							break;
						}
						case (2)://Winter
						{
							bits[istride + j + 0] = (byte)(255 - 0.5 * val);   // blue
							bits[istride + j + 1] = (byte)(val); // green
							bits[istride + j + 2] = 0;   // red
							break;
						}
						case (3)://Lines
						{
							bits[istride + j + 0] = (byte)(LinesB(val));   // blue
							bits[istride + j + 1] = (byte)(LinesG(val)); // green
							bits[istride + j + 2] = (byte)(LinesR(val));   // red
							break;
						}
					}
				}
			});

			bmp.UnlockBits(data);
			if (invertYaxis)
				bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

			return bmp;
		}

		/// <summary>
		/// Convert three 2D arrays representing R (reg), G (green), and B (blue) channels to a color image Bitmap. The R,G,B arrays must already be scaled to 24Bpp Bitmap range, i.e. values between 0 - 255.
		/// </summary>
		/// <param name="R">The RED channel array</param>
		/// <param name="G">The GREEN channel array</param>
		/// <param name="B">The BLUE channel array</param>
		public static unsafe Bitmap RGBBitMap(double[,] R, double[,] G, double[,] B)
		{
			bool codim = true;
			if (R.GetLength(0) != G.GetLength(0) || R.GetLength(0) != B.GetLength(0) || G.GetLength(0) != B.GetLength(0))
				codim = false;
			if (R.GetLength(1) != G.GetLength(1) || R.GetLength(1) != B.GetLength(1) || G.GetLength(1) != B.GetLength(1))
				codim = false;
			if (codim == false)
			{
				throw new Exception("Error: RGB array set not co-dimensional...");
			}

			Bitmap bmp = new Bitmap(R.GetLength(0), R.GetLength(1), PixelFormat.Format24bppRgb);

			BitmapData data = bmp.LockBits(new Rectangle(0, 0, R.GetLength(0), R.GetLength(1)), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
			byte* bits = (byte*)data.Scan0.ToPointer();

			int bytesPerPixel = 3; // 3 bytes per pixel for 24 bpp rgb

			int height = data.Height;
			int width = data.Width;
			int stride = data.Stride;
			int bytesWidth = width * bytesPerPixel;

			Parallel.For(0, height, i =>
			{
				int istride = i * stride;
				int jcounter = -1;
				for (int j = 0; j < bytesWidth; j += bytesPerPixel)
				{
					jcounter++;

					bits[istride + j + 0] = (byte)(B[jcounter, i]);// blue
					bits[istride + j + 1] = (byte)(G[jcounter, i]);// green
					bits[istride + j + 2] = (byte)(R[jcounter, i]);// red
				}
			});
			bmp.UnlockBits(data);
			return bmp;
		}

		private static double[,] Bin(double[,] data, int Nx, int Ny)
		{
			int Lx = data.GetLength(0) / Nx;
			int Ly = data.GetLength(1) / Ny;
			double[,] result = new double[Lx, Ly];
			double inv_size = 1 / (double)(Nx * Ny);

			Parallel.For(0, Lx, i =>
			{
				int kmin = i * Nx, kmax = i * Nx + Nx;
				for (int j = 0; j < Ly; j++)
				{
					double s = 0;
					int lmax = j * Ny + Ny;
					for (int k = kmin; k < kmax; k++)
						for (int l = j * Ny; l < lmax; l++)
							s += data[k, l];

					result[i, j] = s * inv_size;
				}
			});

			return result;
		}

		private static double JetR(double val)
		{
			if (val < 96)
				return 0;
			if (val >= 96 && val < 160)
				return 4 * (val - 96) - 1;
			if (val >= 160 && val < 224)
				return 255;
			return 255 - (val - 224) * 4;
		}

		private static double JetG(double val)
		{
			if (val < 32)
				return 0;
			if (val >= 32 && val < 96)
				return 4 * (val - 32) - 1;
			if (val >= 96 && val < 160)
				return 255;
			if (val >= 160 && val < 224)
				return 255 - (val - 160) * 4;
			return 0;
		}

		private static double JetB(double val)
		{
			if (val < 28)
				return 144 + (val) * 4 - 1;
			if (val >= 28 && val < 96)
				return 255;
			if (val >= 96 && val < 160)
				return 255 - (val - 160) * 4;
			return 0;
		}

		private static double WinterR(double val)
		{
			//this channel dead
			return 0;
		}

		private static double WinterG(double val)
		{
			//this channel linear
			return val;
		}

		private static double WinterB(double val)
		{
			//this channel half linear backwards
			return 255 - 0.5 * val;
		}

		private static double LinesR(double val)
		{
			double res = 0;
			int mod = (((int)(val - 2)) % 7);
			switch (mod)
			{
				case (0):
				{
					res = 255;
					break;
				}
				case (4):
				{
					res = 63;
					break;
				}
				case (2):
				case (3):
				{
					res = 191;
					break;
				}
			}
			return res;
		}

		private static double LinesG(double val)
		{
			double res = 0;
			int mod = ((int)(val - 1)) % 7;
			switch (mod)
			{
				case (0):
				{
					res = 127;
					break;
				}
				case (2):
				case (4):
				{
					res = 191;
					break;
				}
				case (5):
				{
					res = 63;
					break;
				}
			}
			return res;
		}

		private static double LinesB(double val)
		{
			double res = 0;
			int mod = ((int)(val)) % 7;
			switch (mod)
			{
				case (0):
				{
					res = 255;
					break;
				}
				case (3):
				case (4):
				{
					res = 191;
					break;
				}
				case (6):
				{
					res = 63;
					break;
				}
			}
			return res;
		}
	}
}
