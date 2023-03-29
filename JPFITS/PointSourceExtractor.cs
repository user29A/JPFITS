using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.IO;
#nullable enable

namespace JPFITS
{
	/// <summary>SourceExtractor class provides functionality for extracting sources from image arrays.</summary>
	public class PointSourceExtractor
	{
		/// <summary>
		/// Provides functionality for handling multiple PSE objects.
		/// </summary>
		public class PSESet
		{
			private ArrayList PSESET = new ArrayList();

			/// <summary>
			/// Add an existing PointSourceExtractor instance to the set. 
			/// </summary>
			/// <param name="PSE"></param>
			public void Add(PointSourceExtractor PSE)
			{
				PSESET.Add(PSE);
			}

			/// <summary>
			/// Adds a new PointSourceExtractor instance to the set.
			/// </summary>
			public void Add()
			{
				PSESET.Add(new PointSourceExtractor());
			}

			/// <summary>
			/// Removes the PointSourceExtractor instance at the given index.
			/// </summary>
			/// <param name="pseIndex"></param>
			public void RemoveAt(int pseIndex)
			{
				PSESET.RemoveAt(pseIndex);
			}

			public void Clear()
			{
				PSESET.Clear();
			}

			public int Count
			{
				get { return PSESET.Count; }
			}

			/// <summary>
			/// Returns the PointSourceExtractor instance at the index.
			/// </summary>
			public PointSourceExtractor this[int i]
			{
				get { return ((PointSourceExtractor)(PSESET[i])); }
				set { PSESET[i] = value; }
			}
		}

		public class Group
		{
			public Group(int id)
			{
				ID = id;
			}

			public Color GroupColor
			{
				get { return COLOR; }
				set { COLOR = value; }
			}

			public int ID = -1;
			public int NElements = 0;
			public int[]? ElementIndices;
			public Region? REGION;
			private Color COLOR;
		};

		#region CONSTRUCTORS
		/// <summary>The default constructor, used when an image is to be examined for sources.</summary>
		public PointSourceExtractor()
		{
			this.BGWRKR = new BackgroundWorker();
			this.BGWRKR.WorkerReportsProgress = true;
			this.BGWRKR.WorkerSupportsCancellation = true;
			this.BGWRKR.DoWork += new DoWorkEventHandler(BGWRKR_DoWork);
			this.BGWRKR.ProgressChanged += new ProgressChangedEventHandler(BGWRKR_ProgressChanged);
			this.BGWRKR.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGWRKR_RunWorkerCompleted);
			WAITBAR = new WaitBar();
		}

		/// <summary>
		/// The constructor used when an image already has a list of valid centroids for sources in the image. Centroids will NOT be redetermined.
		/// </summary>
		/// <param name="image">The 2D image array to which the centroids belong.</param>
		/// <param name="XCoords">The x-axis coordinates of the sources.</param>
		/// <param name="YCoords">The y-axis coordinates of the sources.</param>
		/// <param name="pix_saturation">The saturation threshold of of the image pixels, for finding saturation islands. Set equal to zero (0) if not needed.</param>
		/// <param name="kernel_radius">The radius (pixels) of the kernel to centroid.</param>
		/// <param name="auto_background">Estimate the background at the sources.</param>
		/// <param name="kernel_filename_template">The template full file name for the kernels to be saved. Sources will be numbered sequentially. Pass empty string for no saving.</param>
		/// <param name="pix_saturation_mapmin">The minimum vale to which to map out a saturated source. Default of 0 results in the same value as pix_saturation.</param> 
		public PointSourceExtractor(double[,] image, double[] XCoords, double[] YCoords, double pix_saturation, int kernel_radius, int background_radius, bool auto_background, string kernel_filename_template, double pix_saturation_mapmin = 0)
		{
			this.BGWRKR = new BackgroundWorker();
			this.BGWRKR.WorkerReportsProgress = true;
			this.BGWRKR.WorkerSupportsCancellation = true;
			this.BGWRKR.DoWork += new DoWorkEventHandler(BGWRKR_DoWork);
			this.BGWRKR.ProgressChanged += new ProgressChangedEventHandler(BGWRKR_ProgressChanged);
			this.BGWRKR.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGWRKR_RunWorkerCompleted);
			WAITBAR = new WaitBar();

			IMAGE = image;
			PIX_SAT = pix_saturation;
			if (pix_saturation_mapmin == 0)
				PIX_SAT_MINMAP = PIX_SAT;
			else
				PIX_SAT_MINMAP = pix_saturation_mapmin;
			IMAGEWIDTH = IMAGE.GetLength(0);
			IMAGEHEIGHT = IMAGE.GetLength(1);
			KERNEL_RADIUS = kernel_radius;
			BACKGROUND_RADIUS = background_radius;
			KERNEL_WIDTH = KERNEL_RADIUS * 2 + 1;
			N_SRC = XCoords.Length;
			INITARRAYS();
			AUTO_BG = auto_background;
			SAVE_PS = kernel_filename_template != "";
			SAVE_PS_FILENAME = kernel_filename_template;
			SOURCE_BOOLEAN_MAP = new bool[IMAGEWIDTH, IMAGEHEIGHT];
			SOURCE_INDEX_MAP = new int[IMAGEWIDTH, IMAGEHEIGHT];
			Parallel.For(0, IMAGE.GetLength(0), i =>
			{
				for (int j = 0; j < IMAGE.GetLength(1); j++)
					SOURCE_INDEX_MAP[i, j] = -1;
			});
			FITTED = false;
			WCS_GENERATED = false;
			VALIDPSERECTS = false;
			double bgstd = -1, bg_est;
			double[,] kernel;
			int npixels;

			for (int i = 0; i < N_SRC; i++)
			{
				bg_est = ESTIMATELOCALBACKGROUND((int)Math.Round(XCoords[i]), (int)Math.Round(YCoords[i]), BACKGROUND_RADIUS, BackGroundEstimateStyle.Corners2ndMin, ref bgstd);

				if (PIX_SAT != 0 && IMAGE[(int)Math.Round(XCoords[i]), (int)Math.Round(YCoords[i])] >= PIX_SAT)
				{
					int xmin = (int)Math.Round(XCoords[i]), xmax = xmin, ymin = (int)Math.Round(YCoords[i]), ymax = ymin;
					MAPSATURATIONISLAND((int)Math.Round(XCoords[i]), (int)Math.Round(YCoords[i]), i, ref xmin, ref xmax, ref ymin, ref ymax);
					SATURATED[i] = true;

					if (xmax - xmin == 0)//single pixel, expand to 3 pixels
					{
						xmin--;
						if (xmin < 0)
							xmin = 0;
						xmax++;
						if (xmax >= IMAGE.GetLength(0))
							xmax = IMAGE.GetLength(0) - 1;
					}
					if (ymax - ymin == 0)//single pixel, expand to 3 pixels
					{
						ymin--;
						if (ymin < 0)
							ymin = 0;
						ymax++;
						if (ymax >= IMAGE.GetLength(1))
							ymax = IMAGE.GetLength(1) - 1;
					}
					npixels = (xmax - xmin + 1) * (ymax - ymin + 1);

					Centroid(xmin, xmax, ymin, ymax, bg_est, out _, out _, out kernel);
				}
				else
				{
					kernel = GetKernel(image, (int)Math.Round(XCoords[i]), (int)Math.Round(YCoords[i]), KERNEL_RADIUS, out int[] xrange, out int[] yrange);
					if (bg_est != 0)
						kernel = JPMath.MatrixAddScalar(kernel, -bg_est, false);

					RADIALIZE_MAPS_KERNEL(xrange[KERNEL_RADIUS], yrange[KERNEL_RADIUS], i, out npixels);
				}

				CENTROIDS_PIXEL_X[i] = (int)Math.Round(XCoords[i]);
				CENTROIDS_PIXEL_Y[i] = (int)Math.Round(YCoords[i]);
				CENTROIDS_X[i] = XCoords[i];
				CENTROIDS_Y[i] = YCoords[i];
				CENTROIDS_AMPLITUDE[i] = JPMath.Max(kernel);
				CENTROIDS_VOLUME[i] = JPMath.Sum(kernel);
				CENTROIDS_AUTOBGEST[i] = bg_est;
				CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_VOLUME[i]);
				bgstd = 0;
				CENTROIDS_ANULMEDBGEST[i] = ESTIMATELOCALBACKGROUND(CENTROIDS_PIXEL_X[i], CENTROIDS_PIXEL_Y[i], BACKGROUND_RADIUS, BackGroundEstimateStyle.AnnulusMedian, ref bgstd);
				CENTROIDS_ANULMEDBGSTD[i] = bgstd;
				CENTROIDS_SNR[i] = CENTROIDS_VOLUME[i] / Math.Sqrt(CENTROIDS_VOLUME[i] + CENTROIDS_ANULMEDBGEST[i] * npixels);

				if (SAVE_PS)
				{
					string file = SAVE_PS_FILENAME;
					int ind = file.LastIndexOf(".");//for saving PS
					file = String.Concat(file.Substring(0, ind), "_", (i + 1).ToString("00000000"), ".fits");

					JPFITS.FITSImage f = new JPFITS.FITSImage(file, kernel, false, false);
					f.WriteImage(DiskPrecision.Double, false);
				}
			}			
		}

		#endregion

		#region MEMBER METHODS
		/// <summary>Searches for sources withn a 2D image array.</summary>
		/// <param name="image">The 2D image array to find sources in.</param>
		/// <param name="pix_saturation">The saturation threshold of of the image pixels, for finding saturation islands. Set equal to zero (0) if not needed.</param>
		/// <param name="pix_min">The minimum pixel threshold value (or SN) to consider a potential source.</param>
		/// <param name="pix_max">The maximum pixel threshold value (or SN) to consider a potential source.</param>
		/// <param name="kernel_min">The minimum kernel pixel sum threshold value (or SN) to consider a potential source.</param>
		/// <param name="kernel_max">The maximum kernel pixel sum threshold value (or SN) to consider a potential source.</param>
		/// <param name="threshholds_as_SN">Treat the thresholds as Signal to Noise instead of pixel values.</param>
		/// <param name="kernel_radius">The radius (pixels) of the kernel to find sources within. Secondary sources within the radius will be ignored.</param>
		/// <param name="background_radius">The separation (pixels) between sources. Only the brightest source within the separation radius is kept.</param>
		/// <param name="auto_background">Automatically determine the local background for potential sources. Use None if the image is already background subtracted.</param>
		/// <param name="kernel_filename_template">The template full file name for the kernels to be saved. Sources will be numbered sequentially. Pass empty string for no saving.</param>
		/// <param name="ROI_region">A boolean array of valid area to examine. Pass null or array of equal dimension to source image all true for entire image search.</param>
		/// <param name="show_waitbar">Show a cancellable wait bar. False equates to a syncronous call.</param>
		/// <param name="pix_saturation_mapmin">The minimum vale to which to map out a saturated source. Default of 0 results in the same value as pix_saturation.</param>
		/// <param name="reject_saturated">Option to reject saturated sources from the extraction results.</param>
		/// <param name="background_radius_as_source_separation">The background radius will be used to isolate only the brightest sources within the radius as valid sources.</param>
		public void Extract_Sources(double[,] image, double pix_saturation, double pix_min, double pix_max, double kernel_min, double kernel_max, bool threshholds_as_SN, int kernel_radius, int background_radius, bool auto_background, string kernel_filename_template, bool[,]? ROI_region, bool show_waitbar, double pix_saturation_mapmin = 0, bool reject_saturated = false, bool background_radius_as_source_separation = false)
		{
			IMAGE = image;
			PIX_SAT = pix_saturation;
			if (pix_saturation_mapmin == 0)
				PIX_SAT_MINMAP = PIX_SAT;
			else
				PIX_SAT_MINMAP = pix_saturation_mapmin;
			IMAGEWIDTH = IMAGE.GetLength(0);
			IMAGEHEIGHT = IMAGE.GetLength(1);
			KERNEL_RADIUS = kernel_radius;
			KERNEL_WIDTH = KERNEL_RADIUS * 2 + 1;
			BACKGROUND_RADIUS = background_radius;
			PIX_MAX = pix_max;
			PIX_MIN = pix_min;
			N_SRC = 0;
			KERNEL_MIN = kernel_min;
			KERNEL_MAX = kernel_max;
			AUTO_BG = auto_background;
			SAVE_PS = kernel_filename_template != "";
			SAVE_PS_FILENAME = kernel_filename_template;
			SOURCE_BOOLEAN_MAP = new bool[IMAGEWIDTH, IMAGEHEIGHT];
			SOURCE_INDEX_MAP = new int[IMAGEWIDTH, IMAGEHEIGHT];
			Parallel.For(0, IMAGE.GetLength(0), i =>
			{
				for (int j = 0; j < IMAGE.GetLength(1); j++)
					SOURCE_INDEX_MAP[i, j] = -1;
			});
			WCS_GENERATED = false;
			VALIDPSERECTS = false;
			FITTED = false;
			THRESHHOLDS_AS_SN = threshholds_as_SN;
			SEARCH_ROI = ROI_region != null;
			ROI_REGION = ROI_region;
			SHOWWAITBAR = show_waitbar;
			REJECT_SATURATED = reject_saturated;
			BACKGROUND_RAD_AS_SSEP = background_radius_as_source_separation;

			if (SHOWWAITBAR)
			{
				WAITBAR = new WaitBar();
				WAITBAR.ProgressBar.Maximum = 100;
				WAITBAR.Text = "Scanning Image...";
				BGWRKR.RunWorkerAsync(1);
				WAITBAR.ShowDialog();

				if (WAITBAR.DialogResult == DialogResult.Cancel)
					N_SRC = -1;
			}
			else
			{
				object sender = new Object();
				DoWorkEventArgs e = new DoWorkEventArgs(1);
				BGWRKR_DoWork(sender, e);
			}
		}

		/// <summary>Determines centroids and other kernel information for sources at given coordinates. Centroids WILL be attempted to be redetermined; if one is NaN or Inf, the X/YCoords is then used for default.</summary>
		/// <param name="image">The 2D image array containing the known sources to extract.</param>
		/// <param name="XCoords">The x-axis coordinates of the sources.</param>
		/// <param name="YCoords">The y-axis coordinates of the sources.</param>
		/// <param name="pix_saturation">The saturation threshold of of the image pixels, for finding saturation islands. Set equal to zero (0) if not needed.</param>
		/// <param name="kernel_radius">The radius (pixels) of the kernel to centroid.</param>
		/// <param name="auto_background">Automatically determine the local background for potential sources.  Not required if background is known to be zeroed, but should have no effect if used in this case.</param>
		/// <param name="kernel_filename_template">The template full file name for the kernels to be saved. Sources will be numbered sequentially. Pass empty string for no saving.</param>
		/// <param name="pix_saturation_mapmin">The minimum vale to which to map out a saturated source. Default of 0 results in the same value as pix_saturation.</param>
		public void Extract_Sources(double[,] image, double[] XCoords, double[] YCoords, double pix_saturation, int kernel_radius, int background_radius, bool auto_background, string kernel_filename_template, double pix_saturation_mapmin = 0)
		{
			IMAGE = image;
			PIX_SAT = pix_saturation;
			if (pix_saturation_mapmin == 0)
				PIX_SAT_MINMAP = PIX_SAT;
			else
				PIX_SAT_MINMAP = pix_saturation_mapmin;
			IMAGEWIDTH = IMAGE.GetLength(0);
			IMAGEHEIGHT = IMAGE.GetLength(1);
			KERNEL_RADIUS = kernel_radius;
			BACKGROUND_RADIUS = background_radius;
			KERNEL_WIDTH = KERNEL_RADIUS * 2 + 1;
			N_SRC = XCoords.Length;
			INITARRAYS();
			AUTO_BG = auto_background;
			SAVE_PS = kernel_filename_template != "";
			SAVE_PS_FILENAME = kernel_filename_template;
			SOURCE_BOOLEAN_MAP = new bool[IMAGEWIDTH, IMAGEHEIGHT];
			SOURCE_INDEX_MAP = new int[IMAGEWIDTH, IMAGEHEIGHT];
			Parallel.For(0, IMAGE.GetLength(0), i =>
			{
				for (int j = 0; j < IMAGE.GetLength(1); j++)
					SOURCE_INDEX_MAP[i, j] = -1;
			});
			FITTED = false;
			WCS_GENERATED = false;
			VALIDPSERECTS = false;
			double y_centroid, x_centroid, bgstd = -1, bg_est;
			int kernelxmax, kernelymax, npixels;
			double[,] kernel;

			for (int i = 0; i < N_SRC; i++)
			{
				bg_est = ESTIMATELOCALBACKGROUND((int)Math.Round(XCoords[i]), (int)Math.Round(YCoords[i]), BACKGROUND_RADIUS, BackGroundEstimateStyle.Corners2ndMin, ref bgstd);

				if (PIX_SAT != 0 && IMAGE[(int)Math.Round(XCoords[i]), (int)Math.Round(YCoords[i])] >= PIX_SAT)
				{
					int xmin = (int)Math.Round(XCoords[i]), xmax = xmin, ymin = (int)Math.Round(YCoords[i]), ymax = ymin;
					MAPSATURATIONISLAND((int)Math.Round(XCoords[i]), (int)Math.Round(YCoords[i]), i, ref xmin, ref xmax, ref ymin, ref ymax);
					SATURATED[i] = true;

					if (xmax - xmin == 0)//single pixel, expand to 3 pixels
					{
						xmin--;
						if (xmin < 0)
							xmin = 0;
						xmax++;
						if (xmax >= IMAGE.GetLength(0))
							xmax = IMAGE.GetLength(0) - 1;
					}
					if (ymax - ymin == 0)//single pixel, expand to 3 pixels
					{
						ymin--;
						if (ymin < 0)
							ymin = 0;
						ymax++;
						if (ymax >= IMAGE.GetLength(1))
							ymax = IMAGE.GetLength(1) - 1;
					}
					npixels = (xmax - xmin + 1) * (ymax - ymin + 1);

					kernelxmax = (xmin + xmax) / 2;
					kernelymax = (ymin + ymax) / 2;

					Centroid(xmin, xmax, ymin, ymax, bg_est, out x_centroid, out y_centroid, out kernel);
				}
				else
				{
					kernel = GetKernel(image, (int)Math.Round(XCoords[i]), (int)Math.Round(YCoords[i]), KERNEL_RADIUS, out int[] xrange, out int[] yrange);
					if (bg_est != 0)
						kernel = JPMath.MatrixAddScalar(kernel, -bg_est, false);

					RADIALIZE_MAPS_KERNEL(xrange[KERNEL_RADIUS], yrange[KERNEL_RADIUS], i, out npixels);
					Centroid(xrange, yrange, kernel, out x_centroid, out y_centroid);
					JPMath.Max(kernel, out kernelxmax, out kernelymax);
					kernelxmax += xrange[0];
					kernelymax += yrange[0];
				}

				if (Double.IsNaN(x_centroid) || Double.IsInfinity(x_centroid) || Double.IsNaN(y_centroid) || Double.IsInfinity(y_centroid))
				{
					x_centroid = XCoords[i];
					y_centroid = YCoords[i];
				}

				CENTROIDS_PIXEL_X[i] = kernelxmax;
				CENTROIDS_PIXEL_Y[i] = kernelymax;
				CENTROIDS_X[i] = x_centroid;
				CENTROIDS_Y[i] = y_centroid;
				CENTROIDS_AMPLITUDE[i] = JPMath.Max(kernel);
				CENTROIDS_VOLUME[i] = JPMath.Sum(kernel);
				CENTROIDS_AUTOBGEST[i] = bg_est;
				CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_VOLUME[i]);
				bgstd = 0;
				CENTROIDS_ANULMEDBGEST[i] = ESTIMATELOCALBACKGROUND(CENTROIDS_PIXEL_X[i], CENTROIDS_PIXEL_Y[i], BACKGROUND_RADIUS, BackGroundEstimateStyle.AnnulusMedian, ref bgstd);
				CENTROIDS_ANULMEDBGSTD[i] = bgstd;
				CENTROIDS_SNR[i] = CENTROIDS_VOLUME[i] / Math.Sqrt(CENTROIDS_VOLUME[i] + CENTROIDS_ANULMEDBGEST[i] * npixels);

				if (SAVE_PS)
				{
					string file = SAVE_PS_FILENAME;
					int ind = file.LastIndexOf(".");//for saving PS
					file = String.Concat(file.Substring(0, ind), "_", (i + 1).ToString("00000000"), ".fits");

					JPFITS.FITSImage f = new JPFITS.FITSImage(file, kernel, false, false);
					f.WriteImage(DiskPrecision.Double, false);
				}
			}			
		}

		/// <summary>Attempt to find N strongest sources in an image.</summary>
		/// <param name="nBrightestSources">The number of strongest sources to try to find.</param>
		/// <param name="image">The 2D image array to find sources in.</param>
		/// <param name="pix_saturation">The saturation threshold of of the image pixels, for finding saturation islands. Set equal to zero (0) if not needed.</param>
		/// <param name="kernel_radius">The radius (pixels) of the kernel to find sources within. Secondary sources within the radius will be ignored.</param>
		/// <param name="source_separation">The separation (pixels) between sources. Only the brightest source within the separation radius is kept.</param>
		/// <param name="auto_background">Automatically determine the local background for potential sources.  Not required if background is known to be zeroed, but should have no effect if used in this case.</param>
		/// <param name="kernel_filename_template">The template full file name for the kernels to be saved. Sources will be numbered sequentially. Pass empty string for no saving.</param>
		/// <param name="ROI_region">A boolean array of valid area to examine. Pass null or array of equal dimension to source image all true for entire image search.</param>
		/// <param name="show_waitbar">Show a cancellable wait bar.</param>
		//public void Extract_Attempt_N_Sources(int N, double[,] image, double pix_saturation, double pix_min, double pix_max, double kernel_min, double kernel_max, bool threshholds_as_SN, int kernel_radius, int source_separation, bool auto_background, string kernel_filename_template, bool[,]? ROI_region, bool show_waitbar)
		public void Extract_Attempt_NBrightestSources(int nBrightestSources, double[,] image, double pix_saturation, int kernel_radius, int source_separation, bool auto_background, string kernel_filename_template, bool[,]? ROI_region, bool show_waitbar, out int niters)
		{
			double immax = JPMath.Max(image, true);
			double immed = JPMath.Median(image);
			double imamp = immax - immed;
			double div = 8;
			double pixthresh = imamp / div + immed;
			niters = 0;
			int maxiters = 11;

			while (this.N_Sources != nBrightestSources && niters <= maxiters)
			{
				niters++;

				Extract_Sources(image, pix_saturation, pixthresh, Double.MaxValue, 0, Double.MaxValue, false, kernel_radius, source_separation, auto_background, kernel_filename_template, ROI_region, show_waitbar, 0, false, true);

				if (this.N_Sources >= nBrightestSources)
					break;

				div *= 2;
				pixthresh = imamp / div + immed;
			}
			if (this.N_Sources > nBrightestSources)
				this.ClipToNBrightest(nBrightestSources);
		}

		/// <summary>Performs a least-squares fit on all sources of the form:
		/// <br />G(x,y|P) = P(0) * exp( -((x - P(1)).^2 + (y - P(2)).^2 ) / (2*P(3)^2)) + P(4).</summary>
		/// <param name="Pinit">Initial guesses for the fit parameters. Only P(3) is used, all other parameter initial estimates are determined locally. Can pass null for auto-estimate.</param>
		/// <param name="LBnds">Lower bounds for the fit parameters. Same restrictions as Pinit.</param>
		/// <param name="UBnds">Upper bounds for the fit parameters. Same restrictions as Pinit.</param>
		/// <param name="showwaitbar">Show a cancellable waitbar. False equates to a syncronous call.</param>
		public void Fit_Sources_Gaussian_Circular(double[]? Pinit, double[]? LBnds, double[]? UBnds, bool showwaitbar = true)
		{
			//G = P(0) * exp( -((X-P(1)).^2 + (Y-P(2)).^2 ) / (2*P(3)^2)) + P(4);

			FITS_PARAMS = new double[5, N_SRC];
			FITTED = true;
			FIT_EQUATION = "Gaussian (Circular): P(0) * exp( -((X-P(1)).^2 + (Y-P(2)).^2 ) / (2*P(3)^2)) + P(4)";
			LBND = LBnds;
			UBND = UBnds;
			PINI = Pinit;
			SHOWWAITBAR = showwaitbar;

			if (showwaitbar)
			{
				WAITBAR = new WaitBar();
				WAITBAR.ProgressBar.Maximum = 100;
				WAITBAR.Text = "Fitting Sources...";
				BGWRKR.RunWorkerAsync(2);
				WAITBAR.ShowDialog();

				if (WAITBAR.DialogResult == DialogResult.Cancel)
					FITTED = false;
			}
			else
				BGWRKR_DoWork(this, new DoWorkEventArgs(2));
		}

		/// <summary>Performs a least-squares fit on all sources of the form:
		/// <br />G(x,y|P) = P(0) * exp( -((x - P(1))*cosd(P(3)) + (y - P(2))*sind(P(3))).^2 / (2*P(4)^2) - ( -(x - P(1))*sind(P(3)) + (y - P(2))*cosd(P(3))).^2 / (2*P(5)^2) ) + P(6).</summary>
		/// <param name="Pinit">Initial guesses for the fit parameters. Only P(3), P(4), P(5) are used, all other parameter initial estimates are determined locally.</param>
		/// <param name="LBnds">Lower bounds for the fit parameters. Same restrictions as Pinit.</param>
		/// <param name="UBnds">Upper bounds for the fit parameters. Same restrictions as Pinit.</param>
		public void Fit_Sources_Gaussian_Elliptical(double[]? Pinit, double[]? LBnds, double[]? UBnds, bool showwaitbar = true)
		{
			//G = P(0) * exp( -((x-P(1))*cosd(P(3)) + (y-P(2))*sind(P(3))).^2 / (2*P(4)^2) - ( -(x-P(1))*sind(P(3)) + (y-P(2))*cosd(P(3))).^2 / (2*P(5)^2) ) + P(6);

			FITS_PARAMS = new double[7, N_SRC];
			FITTED = true;
			FIT_EQUATION = "Gaussian (Elliptical): P(0) * exp( -((x-P(1))*cos(P(3)) + (y-P(2))*sin(P(3))).^2 / (2*P(4)^2) - ( -(x-P(1))*sin(P(3)) + (y-P(2))*cos(P(3))).^2 / (2*P(5)^2) ) + P(6)";
			LBND = LBnds;
			UBND = UBnds;
			PINI = Pinit;
			SHOWWAITBAR = showwaitbar;

			if (showwaitbar)
			{
				WAITBAR = new WaitBar();
				WAITBAR.ProgressBar.Maximum = 100;
				WAITBAR.Text = "Fitting Sources...";
				BGWRKR.RunWorkerAsync(3);
				WAITBAR.ShowDialog();

				if (WAITBAR.DialogResult == DialogResult.Cancel)
					FITTED = false;
			}
			else
				BGWRKR_DoWork(this, new DoWorkEventArgs(3));
		}

		/// <summary>Performs a least-squares fit on all sources of the form:
		/// <br />M(x,y|P) = P(0) * ( 1 + { (x - P(1))^2 + (y - P(2))^2 } / P(3)^2 ) ^ (-P(4)) + P(5).</summary>
		/// <param name="Pinit">Initial guesses for the fit parameters. Only P(3), P(4) are used, all other parameter initial estimates are determined locally.</param>
		/// <param name="LBnds">Lower bounds for the fit parameters. Same restrictions as Pinit.</param>
		/// <param name="UBnds">Upper bounds for the fit parameters. Same restrictions as Pinit.</param>
		public void Fit_Sources_Moffat_Circular(double[]? Pinit, double[]? LBnds, double[]? UBnds, bool showwaitbar = true)
		{
			// M = P(0) * ( 1 + { (X-P(1))^2 + (Y-P(2))^2 } / P(3)^2 ) ^ (-P(4)) + P(5)

			FITS_PARAMS = new double[6, N_SRC];
			FITTED = true;
			FIT_EQUATION = "Moffat (Circular): P(0) * ( 1 + { (X-P(1))^2 + (Y-P(2))^2 } / P(3)^2 ) ^ (-P(4)) + P(5)";
			LBND = LBnds;
			UBND = UBnds;
			PINI = Pinit;
			SHOWWAITBAR = showwaitbar;

			if (showwaitbar)
			{
				WAITBAR = new WaitBar();
				WAITBAR.ProgressBar.Maximum = 100;
				WAITBAR.Text = "Fitting Sources...";
				BGWRKR.RunWorkerAsync(4);
				WAITBAR.ShowDialog();

				if (WAITBAR.DialogResult == DialogResult.Cancel)
					FITTED = false;
			}
			else
				BGWRKR_DoWork(this, new DoWorkEventArgs(4));
		}

		/// <summary>Performs a least-squares fit on all sources of the form:
		/// <br />M(x,y|P) = P(0) * (1 + { ((x - P(1))*cosd(P(3)) + (y - P(2))*sind(P(3))) ^ 2 } / P(4) ^ 2 + { (-(x - P(1))*sind(P(3)) + (y - P(2))*cosd(P(3))) ^ 2 } / P(5) ^ 2) ^ (-P(6)) + P(7).</summary>
		/// <param name="Pinit">Initial guesses for the fit parameters. Only P(3), P(4), P(5),  P(6) are used, all other parameter initial estimates are determined locally.</param>
		/// <param name="LBnds">Lower bounds for the fit parameters. Same restrictions as Pinit.</param>
		/// <param name="UBnds">Upper bounds for the fit parameters. Same restrictions as Pinit.</param>
		public void Fit_Sources_Moffat_Elliptical(double[]? Pinit, double[]? LBnds, double[]? UBnds, bool showwaitbar = true)
		{
			//M = P(0) * ( 1 + { ((X-P(1))*cosd(P(3)) + (Y-P(2))*sind(P(3)))^2 } / P(4)^2 + { (-(X-P(1))*sind(P(3)) + (Y-P(2))*cosd(P(3)))^2 } / P(5)^2 ) ^ (-P(6)) + P(7);

			FITS_PARAMS = new double[8, N_SRC];
			FITTED = true;
			FIT_EQUATION = "Moffat (Elliptical): P(0) * ( 1 + { ((X-P(1))*cos(P(3)) + (Y-P(2))*sin(P(3)))^2 } / P(4)^2 + { (-(X-P(1))*sin(P(3)) + (Y-P(2))*cos(P(3)))^2 } / P(5)^2 ) ^ (-P(6)) + P(7)";
			LBND = LBnds;
			UBND = UBnds;
			PINI = Pinit;
			SHOWWAITBAR = showwaitbar;

			if (showwaitbar)
			{
				WAITBAR = new WaitBar();
				WAITBAR.ProgressBar.Maximum = 100;
				WAITBAR.Text = "Fitting Sources...";
				BGWRKR.RunWorkerAsync(5);
				WAITBAR.ShowDialog();

				if (WAITBAR.DialogResult == DialogResult.Cancel)
					FITTED = false;
			}
			else
				BGWRKR_DoWork(this, new DoWorkEventArgs(5));
		}

		/// <summary>Saves the metadata table of the extracted sources as a delimited text file.</summary>
		/// <param name="delimit">The delimit argument string: &quot;tab&quot; specifies a tab-delimit, otherwise provide a character (such as the comma &quot;,&quot; etc).</param>
		public void Save_Source_Table(string delimit)
		{
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "Delimited Text file (*.txt)|*.txt";
			if (sfd.ShowDialog() == DialogResult.Cancel)
				return;

			this.GENERATEPSETABLE();

			if (delimit == "tab")
				delimit = "\t";

			string line;
			StreamWriter sw = new StreamWriter(sfd.FileName);
			for (int j = 0; j < PSE_TABLE.GetLength(1); j++)
			{
				line = "";
				for (int i = 0; i < PSE_TABLE.GetLength(0); i++)
					line += PSE_TABLE[i, j] + delimit;
				line = line.Substring(0, line.LastIndexOf(delimit) + 1);
				sw.WriteLine(line);
			}
			sw.Close();
		}

		public void View_Source_Table()
		{
			string[,] table = this.Source_Table;

			PSETableViewer PSETABLEVIEWER = new PSETableViewer(table);
			PSETABLEVIEWER.Text = this.LSFit_Equation;
			PSETABLEVIEWER.Show();
		}

		/// <summary>Generates RA and Dec coordinates for the sources in this instance, using the supplied World Coordinate System instance.</summary>
		/// <param name="wcs">The world coordinate system to use for converting image pixel locations to world coordinates.</param>
		public void Generate_Source_RADec_Coords(JPFITS.WorldCoordinateSolution wcs)
		{
			for (int i = 0; i < N_SRC; i++)
			{
				wcs.Get_Coordinate(CENTROIDS_X[i], CENTROIDS_Y[i], true, "TAN", out double a, out double d, out string RAhms, out string DECdamas);

				CENTROIDS_RADEG[i] = a;
				CENTROIDS_RAHMS[i] = RAhms;
				CENTROIDS_DECDEG[i] = d;
				CENTROIDS_DECDMS[i] = DECdamas;

				if (FITTED)
				{
					wcs.Get_Coordinate(FITS_X[i], FITS_Y[i], true, "TAN", out a, out d, out RAhms, out DECdamas);

					FITS_RA_DEG[i] = a;
					FITS_RA_HMS[i] = RAhms;
					FITS_DEC_DEG[i] = d;
					FITS_DEC_DMS[i] = DECdamas;
				}
			}

			WCS_GENERATED = true;
		}

		/// <summary>
		/// Generate signal to noise ratios for all sources
		/// </summary>
		/// <param name="sigma">The standard deviation of the image-sky background. Pass -1 and a value will be estimated.</param>
		public void Generate_SNR(double sigma)
		{
			double r = KERNEL_RADIUS;
			if (r == 0)
				r = 1;

			double bg = r * r * sigma * sigma;

			for (int i = 0; i < N_Sources; i++)
				CENTROIDS_SNR[i] = CENTROIDS_VOLUME[i] / Math.Sqrt(CENTROIDS_VOLUME[i] + bg);
		}

		/// <summary>
		/// Draw PSE source locations on a PictureBox control. Can plot to a main window or optionally a subwindow.
		/// </summary>
		/// <param name="e">The PaintEventArgs from the PictureBox.Paint callback.</param>
		/// <param name="pen">The pen to draw with.</param>
		/// <param name="pictureBox">The PictureBox control issuing the paint request.</param>
		/// <param name="PSEImage">The 2d array image or subimage.</param>
		/// <param name="xmin">Optional parameter for specifying subimage x-start.</param>
		/// <param name="xmax">Optional parameter for specifying subimage x-end.</param>
		/// <param name="ymin">Optional parameter for specifying subimage y-start.</param>
		/// <param name="ymax">Optional parameter for specifying subimage y-end.</param>
		/// <param name="brush">Optional parameter for subwidow brush to fill pixels.</param>
		public void Draw_PSEKernels(PaintEventArgs e, Pen pen, PictureBox pictureBox, Array PSEImage, int xmin = 0, int xmax = Int32.MaxValue, int ymin = 0, int ymax = Int32.MaxValue, Brush? brush = null)
		{
			if (CENTROIDS_PIXEL_X == null)
				return;

			if (xmin == 0 && xmax == Int32.MaxValue && ymin == 0 && ymax == Int32.MaxValue)
			{
				if (!VALIDPSERECTS)
				{
					VALIDPSERECTS = true;

					float xscale = (float)pictureBox.Width / (float)PSEImage.GetLength(0);
					float yscale = (float)pictureBox.Height / (float)PSEImage.GetLength(1);

					PSESRECTS = new Rectangle[this.N_Sources];
					int radius = KERNEL_RADIUS;
					if (radius < 2)
						radius = 2;

					for (int i = 0; i < this.N_Sources; i++)
						PSESRECTS[i] = new Rectangle((int)(((float)CENTROIDS_PIXEL_X[i] + 0.5 - radius) * xscale), (int)(((float)CENTROIDS_PIXEL_Y[i] + 0.5 - radius) * yscale), (int)((float)(radius * 2 + 1) * xscale), (int)((float)(radius * 2 + 1) * yscale));
				}
				e.Graphics.DrawRectangles(pen, PSESRECTS);
			}
			else
			{
				float xscale = (float)pictureBox.Width / (float)PSEImage.GetLength(0);
				float yscale = (float)pictureBox.Height / (float)PSEImage.GetLength(1);
				float xleft, ytop;
				SolidBrush br = new SolidBrush(pen.Color);

				for (int i = 0; i < this.N_Sources; i++)
					if (this.Centroids_X_Pixel[i] + 2 >= xmin && this.Centroids_X_Pixel[i] - 2 <= xmax && this.Centroids_Y_Pixel[i] + 2 >= ymin && this.Centroids_Y_Pixel[i] - 2 <= ymax)
						e.Graphics.FillEllipse(br, (float)(((float)(this.Centroids_X[i]) - (float)xmin + 0.5) * xscale - 3.0), (float)(((float)(this.Centroids_Y[i]) - (float)ymin + 0.5) * yscale - 3.0), (float)7.0, (float)7.0);

				for (int x = xmin; x <= xmax; x++)
					for (int y = ymin; y <= ymax; y++)
						if (SOURCE_BOOLEAN_MAP[x, y])
						{
							xleft = (float)(x - xmin) * xscale;
							ytop = (float)(y - ymin) * yscale;
							e.Graphics.FillRectangle(brush, xleft, ytop, xscale, yscale);
						}
			}
		}

		public void KernelRectangles_Refresh()
		{
			VALIDPSERECTS = false;
		}

		public void ClipToNBrightest(int NBright)
		{
			if (NBright >= N_SRC)
				return;

			VALIDPSERECTS = false;

			double[] volkey = new double[N_SRC];
			Array.Copy(CENTROIDS_VOLUME, volkey, N_SRC);

			int[] indices = new int[N_SRC];
			for (int i = 0; i < N_SRC; i++)
				indices[i] = i;

			Array.Sort(volkey, indices);//by increasing brightness
			Array.Reverse(indices);//by decreasing brightness; now all location at indices at index >= NBright are no longer wanted

			double dum;
			string dumstr;
			for (int i = 0; i < NBright; i++)
			{
				dum = CENTROIDS_X[i];
				CENTROIDS_X[i] = CENTROIDS_X[indices[i]];
				CENTROIDS_X[indices[i]] = dum;

				dum = CENTROIDS_Y[i];
				CENTROIDS_Y[i] = CENTROIDS_Y[indices[i]];
				CENTROIDS_Y[indices[i]] = dum;

				dum = CENTROIDS_PIXEL_X[i];
				CENTROIDS_PIXEL_X[i] = CENTROIDS_PIXEL_X[indices[i]];
				CENTROIDS_PIXEL_X[indices[i]] = (int)dum;

				dum = CENTROIDS_PIXEL_Y[i];
				CENTROIDS_PIXEL_Y[i] = CENTROIDS_PIXEL_Y[indices[i]];
				CENTROIDS_PIXEL_Y[indices[i]] = (int)dum;

				REMAP((int)Math.Round(CENTROIDS_X[i]), (int)Math.Round(CENTROIDS_Y[i]), indices[i], i);
				REMAP((int)Math.Round(CENTROIDS_X[indices[i]]), (int)Math.Round(CENTROIDS_Y[indices[i]]), i, indices[i]);

				dum = FITS_X[i];
				FITS_X[i] = FITS_X[indices[i]];
				FITS_X[indices[i]] = dum;

				dum = FITS_Y[i];
				FITS_Y[i] = FITS_Y[indices[i]];
				FITS_Y[indices[i]] = dum;

				dum = FITS_FWHM_X[i];
				FITS_FWHM_X[i] = FITS_FWHM_X[indices[i]];
				FITS_FWHM_X[indices[i]] = dum;

				dum = FITS_FWHM_Y[i];
				FITS_FWHM_Y[i] = FITS_FWHM_Y[indices[i]];
				FITS_FWHM_Y[indices[i]] = dum;

				dum = FITS_PHI[i];
				FITS_PHI[i] = FITS_PHI[indices[i]];
				FITS_PHI[indices[i]] = dum;

				dum = FITS_CHISQNORM[i];
				FITS_CHISQNORM[i] = FITS_CHISQNORM[indices[i]];
				FITS_CHISQNORM[indices[i]] = dum;

				dum = CENTROIDS_RADEG[i];
				CENTROIDS_RADEG[i] = CENTROIDS_RADEG[indices[i]];
				CENTROIDS_RADEG[indices[i]] = dum;

				dumstr = CENTROIDS_RAHMS[i];
				CENTROIDS_RAHMS[i] = CENTROIDS_RAHMS[indices[i]];
				CENTROIDS_RAHMS[indices[i]] = dumstr;

				dum = CENTROIDS_DECDEG[i];
				CENTROIDS_DECDEG[i] = CENTROIDS_DECDEG[indices[i]];
				CENTROIDS_DECDEG[indices[i]] = dum;

				dumstr = CENTROIDS_DECDMS[i];
				CENTROIDS_DECDMS[i] = CENTROIDS_DECDMS[indices[i]];
				CENTROIDS_DECDMS[indices[i]] = dumstr;

				dum = CENTROIDS_AMPLITUDE[i];
				CENTROIDS_AMPLITUDE[i] = CENTROIDS_AMPLITUDE[indices[i]];
				CENTROIDS_AMPLITUDE[indices[i]] = dum;

				dum = CENTROIDS_AUTOBGEST[i];
				CENTROIDS_AUTOBGEST[i] = CENTROIDS_AUTOBGEST[indices[i]];
				CENTROIDS_AUTOBGEST[indices[i]] = dum;

				dum = CENTROIDS_ANULMEDBGEST[i];
				CENTROIDS_ANULMEDBGEST[i] = CENTROIDS_ANULMEDBGEST[indices[i]];
				CENTROIDS_ANULMEDBGEST[indices[i]] = dum;

				dum = CENTROIDS_ANULMEDBGSTD[i];
				CENTROIDS_ANULMEDBGSTD[i] = CENTROIDS_ANULMEDBGSTD[indices[i]];
				CENTROIDS_ANULMEDBGSTD[indices[i]] = dum;

				dum = CENTROIDS_SNR[i];
				CENTROIDS_SNR[i] = CENTROIDS_SNR[indices[i]];
				CENTROIDS_SNR[indices[i]] = dum;

				dum = FITS_AMPLITUDE[i];
				FITS_AMPLITUDE[i] = FITS_AMPLITUDE[indices[i]];
				FITS_AMPLITUDE[indices[i]] = dum;

				dum = FITS_VOLUME[i];
				FITS_VOLUME[i] = FITS_VOLUME[indices[i]];
				FITS_VOLUME[indices[i]] = dum;

				dum = FITS_ECCENTRICITY[i];
				FITS_ECCENTRICITY[i] = FITS_ECCENTRICITY[indices[i]];
				FITS_ECCENTRICITY[indices[i]] = dum;

				dum = FITS_FLATNESS[i];
				FITS_FLATNESS[i] = FITS_FLATNESS[indices[i]];
				FITS_FLATNESS[indices[i]] = dum;

				dum = FITS_BGESTIMATE[i];
				FITS_BGESTIMATE[i] = FITS_BGESTIMATE[indices[i]];
				FITS_BGESTIMATE[indices[i]] = dum;

				dum = FITS_RA_DEG[i];
				FITS_RA_DEG[i] = FITS_RA_DEG[indices[i]];
				FITS_RA_DEG[indices[i]] = dum;

				dumstr = FITS_RA_HMS[i];
				FITS_RA_HMS[i] = FITS_RA_HMS[indices[i]];
				FITS_RA_HMS[indices[i]] = dumstr;

				dum = FITS_DEC_DEG[i];
				FITS_DEC_DEG[i] = FITS_DEC_DEG[indices[i]];
				FITS_DEC_DEG[indices[i]] = dum;

				dumstr = FITS_DEC_DMS[i];
				FITS_DEC_DMS[i] = FITS_DEC_DMS[indices[i]];
				FITS_DEC_DMS[indices[i]] = dumstr;

				dum = CENTROIDS_VOLUME[i];
				CENTROIDS_VOLUME[i] = CENTROIDS_VOLUME[indices[i]];
				CENTROIDS_VOLUME[indices[i]] = dum;
			}

			for (int i = NBright; i < N_SRC; i++)//all location at indices[i] where i >= NBright are no longer wanted
				DEMAP((int)Math.Round(CENTROIDS_X[i]), (int)Math.Round(CENTROIDS_Y[i]), SOURCE_INDEX_MAP[(int)Math.Round(CENTROIDS_X[i]), (int)Math.Round(CENTROIDS_Y[i])]);

			Array.Resize(ref CENTROIDS_X, NBright);
			Array.Resize(ref CENTROIDS_Y, NBright);
			Array.Resize(ref FITS_X, NBright);
			Array.Resize(ref FITS_Y, NBright);
			Array.Resize(ref FITS_FWHM_X, NBright);
			Array.Resize(ref FITS_FWHM_Y, NBright);
			Array.Resize(ref FITS_PHI, NBright);
			Array.Resize(ref FITS_CHISQNORM, NBright);
			Array.Resize(ref CENTROIDS_RADEG, NBright);
			Array.Resize(ref CENTROIDS_RAHMS, NBright);
			Array.Resize(ref CENTROIDS_SNR, NBright);
			Array.Resize(ref CENTROIDS_DECDEG, NBright);
			Array.Resize(ref CENTROIDS_DECDMS, NBright);
			Array.Resize(ref CENTROIDS_AMPLITUDE, NBright);
			Array.Resize(ref CENTROIDS_VOLUME, NBright);
			Array.Resize(ref CENTROIDS_AUTOBGEST, NBright);
			Array.Resize(ref CENTROIDS_ANULMEDBGEST, NBright);
			Array.Resize(ref CENTROIDS_ANULMEDBGSTD, NBright);
			Array.Resize(ref FITS_AMPLITUDE, NBright);
			Array.Resize(ref FITS_VOLUME, NBright);
			Array.Resize(ref FITS_ECCENTRICITY, NBright);
			Array.Resize(ref FITS_FLATNESS, NBright);
			Array.Resize(ref FITS_BGESTIMATE, NBright);
			Array.Resize(ref FITS_RA_DEG, NBright);
			Array.Resize(ref FITS_RA_HMS, NBright);
			Array.Resize(ref FITS_DEC_DEG, NBright);
			Array.Resize(ref FITS_DEC_DMS, NBright);
			N_SRC = NBright;

			CENTROID_POINTS = new JPMath.PointD[N_SRC];
			for (int i = 0; i < N_SRC; i++)
				CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_VOLUME[i]);

			/*JPFITS.FITSImage^ ff = new FITSImage("C:\\Users\\Joseph E Postma\\Desktop\\test.fits", IMAGE_KERNEL_INDEX_SOURCE, false);
			ff.WriteFile(TypeCode.Int32);*/
		}

		public void GroupizePSE(double groupRadius)
		{
			GROUPIDS = new int[N_SRC];
			for (int i = 0; i < N_SRC; i++)
				GROUPIDS[i] = -1;

			int currgroupid = -1;
			ArrayList groups = new ArrayList();
			Color[] colors = new Color[11] { Color.OrangeRed, Color.Cyan, Color.LawnGreen, Color.BlueViolet, Color.DeepPink, Color.Aqua, Color.Crimson, Color.DarkGoldenrod, Color.Red, Color.Chartreuse, Color.HotPink };

			for (int i = 0; i < N_SRC - 1; i++)
				for (int j = i + 1; j < N_SRC; j++)
					if (CENTROID_POINTS[i].DistanceTo(CENTROID_POINTS[j]) <= groupRadius)
						if (GROUPIDS[i] == -1)
						{
							GROUPIDS[i] = ++currgroupid;

							GraphicsPath path = new GraphicsPath();
							path.AddEllipse((float)(CENTROIDS_X[i] - groupRadius), (float)(CENTROIDS_Y[i] - groupRadius), (float)(groupRadius * 2), (float)(groupRadius * 2));

							groups.Add(new Group(currgroupid));
							((Group)groups[currgroupid]).REGION = new Region(path);
							Math.DivRem(i, colors.Length, out int rem);
							((Group)groups[currgroupid]).GroupColor = colors[rem];

							RECURSGROUP(i, j, groupRadius, currgroupid, groups);
						}
						else if (GROUPIDS[j] == -1)
							RECURSGROUP(i, j, groupRadius, currgroupid, groups);

			NGROUPS = groups.Count;

			GROUPS = new Group[NGROUPS];
			for (int i = 0; i < NGROUPS; i++)
				GROUPS[i] = (Group)groups[i];

			for (int i = 0; i < N_SRC; i++)
				if (GROUPIDS[i] != -1)
					GROUPS[GROUPIDS[i]].NElements++;

			for (int i = 0; i < NGROUPS; i++)
			{
				GROUPS[i].ElementIndices = new int[GROUPS[i].NElements];

				int c = 0;
				for (int j = 0; j < N_SRC; j++)
					if (GROUPIDS[j] == i)
					{
						GROUPS[i].ElementIndices[c] = j;
						c++;
					}
			}

			SOURCE_GROUP_MAP = new int[IMAGE.GetLength(0), IMAGE.GetLength(1)];

			Parallel.For(0, SOURCE_GROUP_MAP.GetLength(0), i =>
			{
				for (int j = 0; j < SOURCE_GROUP_MAP.GetLength(1); j++)
					SOURCE_GROUP_MAP[i, j] = -1;
			});

			Parallel.For(0, NGROUPS, i =>
			{
				for (int j = 0; j < GROUPS[i].NElements; j++)
					for (int x = (int)(CENTROIDS_X[GROUPS[i].ElementIndices[j]] - groupRadius); x <= (int)(CENTROIDS_X[GROUPS[i].ElementIndices[j]] + groupRadius); x++)
						for (int y = (int)(CENTROIDS_Y[GROUPS[i].ElementIndices[j]] - groupRadius); y <= (int)(CENTROIDS_Y[GROUPS[i].ElementIndices[j]] + groupRadius); y++)
							if (x >= 0 && x < IMAGE.GetLength(0) && y >= 0 && y < IMAGE.GetLength(0))
								if ((x - CENTROIDS_X[GROUPS[i].ElementIndices[j]]) * (x - CENTROIDS_X[GROUPS[i].ElementIndices[j]]) + (y - CENTROIDS_Y[GROUPS[i].ElementIndices[j]]) * (y - CENTROIDS_Y[GROUPS[i].ElementIndices[j]]) < groupRadius)
									if (SOURCE_GROUP_MAP[x, y] == -1)
										SOURCE_GROUP_MAP[x, y] = i;
			});
		}

		public void Save(string fileName, string extensionName)
		{
			string[,] table = this.Source_Table;

			string[] ttypes = new string[table.GetLength(0)];
			for (int j = 0; j < ttypes.Length; j++)
				ttypes[j] = table[j, 0];

			Array[] entries = new Array[ttypes.Length];
			for (int j = 0; j < ttypes.Length; j++)
			{
				if (!JPMath.IsNumeric(table[j, 1]))
				{
					entries[j] = new string[table.GetLength(1) - 1];
					for (int k = 1; k < table.GetLength(1); k++)
						((string[])entries[j])[k - 1] = table[j, k];
				}
				else
				{
					entries[j] = new double[table.GetLength(1) - 1];
					for (int k = 1; k < table.GetLength(1); k++)
						((double[])entries[j])[k - 1] = Convert.ToDouble(table[j, k]);
				}
			}

			FITSBinTable bt = new FITSBinTable(extensionName);
			bt.AddExtraHeaderKey("PIXSAT", this.PixelSaturation.ToString(), "Pixel saturation threshold");
			bt.AddExtraHeaderKey("PIXMIN", this.PixelMinimum.ToString(), "Pixel minimum threshold");
			bt.AddExtraHeaderKey("PIXMAX", this.PixelMaximum.ToString(), "Pixel maximum threshold");
			bt.AddExtraHeaderKey("KERNMIN", this.KernelMinimum.ToString(), "Kernel minimum threshold");
			bt.AddExtraHeaderKey("KERNMAX", this.KernelMaximum.ToString(), "Kernel maximum threshold");
			bt.AddExtraHeaderKey("KERNRAD", this.KernelRadius.ToString(), "Pixel kernel radius");
			bt.AddExtraHeaderKey("BGRADIUS", this.BackgroundRadius.ToString(), "Source background radius");
			bt.AddExtraHeaderKey("AUTOBG", this.AutoBackground.ToString(), "Auto background");
			bt.AddExtraHeaderKey("SAVESRC", this.SavePointSources.ToString(), "Save sources");
			if (this.SavePointSources)
				bt.AddExtraHeaderKey("", "", this.SAVE_PS_FILENAME);
			bt.AddExtraHeaderKey("ROIONLY", this.SearchROI.ToString(), "Search ROI only");
			if (this.SearchROI)
				bt.AddExtraHeaderKey("ROIEXTN", extensionName + "_ROI", "name of the extension which contains the ROI map");
			bt.SetTTYPEEntries(ttypes, null, entries);
			bt.Write(fileName, true);

			if (this.SearchROI)
			{
				FITSImage roifits = new FITSImage(fileName, ROI_REGION, false, false);
				roifits.WriteImage(fileName, extensionName + "_ROI", true, DiskPrecision.Boolean, false);
			}

			FITSImage imapfits = new FITSImage(fileName, SOURCE_INDEX_MAP, false, false);
			imapfits.WriteImage(fileName, extensionName + "_IMAP", true, DiskPrecision.Int32, false);

			FITSImage bmapfits = new FITSImage(fileName, SOURCE_BOOLEAN_MAP, false, false);
			bmapfits.WriteImage(fileName, extensionName + "_BMAP", true, DiskPrecision.Boolean, false);
		}

		/// <summary>The constructor for the class object based on a PointSourceExtractor saved from another session.</summary>
		public void Load(string fileName, string extensionName)
		{
			FITSBinTable BinTablePSE = new FITSBinTable(fileName, extensionName);

			PIX_SAT = Convert.ToDouble(BinTablePSE.GetExtraHeaderKeyValue("PIXSAT"));
			PIX_MIN = Convert.ToDouble(BinTablePSE.GetExtraHeaderKeyValue("PIXMIN"));
			PIX_MAX = Convert.ToDouble(BinTablePSE.GetExtraHeaderKeyValue("PIXMAX"));
			KERNEL_MIN = Convert.ToDouble(BinTablePSE.GetExtraHeaderKeyValue("KERNMIN"));
			KERNEL_MAX = Convert.ToDouble(BinTablePSE.GetExtraHeaderKeyValue("KERNMAX"));
			KERNEL_RADIUS = Convert.ToInt32(BinTablePSE.GetExtraHeaderKeyValue("KERNRAD"));
			KERNEL_WIDTH = KERNEL_RADIUS * 2 + 1;
			BACKGROUND_RADIUS = Convert.ToInt32(BinTablePSE.GetExtraHeaderKeyValue("BGRADIUS"));
			AUTO_BG = Convert.ToBoolean(BinTablePSE.GetExtraHeaderKeyValue("AUTOBG"));
			SEARCH_ROI = Convert.ToBoolean(BinTablePSE.GetExtraHeaderKeyValue("ROIONLY"));
			SAVE_PS = Convert.ToBoolean(BinTablePSE.GetExtraHeaderKeyValue("SAVESRC"));
			if (this.SavePointSources)
				SAVE_PS_FILENAME = "";//needs work...the key index after last...
			SEARCH_ROI = Convert.ToBoolean(BinTablePSE.GetExtraHeaderKeyValue("ROIONLY"));

			FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
			ArrayList header = null;
			FITSFILEOPS.SeekExtension(fs, "IMAGE", extensionName + "_IMAP", ref header, out long start, out _, out _, out _, out _);
			fs.Position = start;
			FITSFILEOPS.ScanImageHeaderUnit(fs, false, ref header, out _, out int bitpix, out int[] naxisn, out double bscale, out double bzero);
			SOURCE_INDEX_MAP = (int[,])FITSFILEOPS.ReadImageDataUnit(fs, null, false, bitpix, ref naxisn, bscale, bzero, RankFormat.NAXIS, ReadReturnPrecision.Native);
			fs.Close();

			fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
			header = null;
			FITSFILEOPS.SeekExtension(fs, "IMAGE", extensionName + "_BMAP", ref header, out start, out _, out _, out _, out _);
			fs.Position = start;
			FITSFILEOPS.ScanImageHeaderUnit(fs, false, ref header, out _, out bitpix, out naxisn, out bscale, out bzero);
			SOURCE_BOOLEAN_MAP = (bool[,])FITSFILEOPS.ReadImageDataUnit(fs, null, false, bitpix, ref naxisn, bscale, bzero, RankFormat.NAXIS, ReadReturnPrecision.Boolean);
			fs.Close();

			if (this.SearchROI)
			{
				fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
				header = null;
				FITSFILEOPS.SeekExtension(fs, "IMAGE", extensionName + "_ROI", ref header, out start, out _, out _, out _, out _);
				fs.Position = start;
				FITSFILEOPS.ScanImageHeaderUnit(fs, false, ref header, out _, out bitpix, out naxisn, out bscale, out bzero);
				ROI_REGION = (bool[,])FITSFILEOPS.ReadImageDataUnit(fs, null, false, bitpix, ref naxisn, bscale, bzero, RankFormat.NAXIS, ReadReturnPrecision.Boolean);
				fs.Close();
			}

			N_SRC = BinTablePSE.Naxis2;
			CENTROIDS_X = (double[])BinTablePSE.GetTTYPEEntry("PSE X-Centroid", out _, out _, FITSBinTable.TTYPEReturn.AsDouble);
			CENTROIDS_Y = (double[])BinTablePSE.GetTTYPEEntry("PSE Y-Centroid", out _, out _, FITSBinTable.TTYPEReturn.AsDouble);
			CENTROIDS_AMPLITUDE = (double[])BinTablePSE.GetTTYPEEntry("PSE Amplitude", out _, out _, FITSBinTable.TTYPEReturn.AsDouble);
			CENTROIDS_VOLUME = (double[])BinTablePSE.GetTTYPEEntry("PSE Volume", out _, out _, FITSBinTable.TTYPEReturn.AsDouble);
			CENTROIDS_AUTOBGEST = (double[])BinTablePSE.GetTTYPEEntry("PSE Background", out _, out _, FITSBinTable.TTYPEReturn.AsDouble);

			CENTROID_POINTS = new JPMath.PointD[N_SRC];
			for (int i = 0; i < N_SRC; i++)
				CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_VOLUME[i]);

			if (BinTablePSE.TTYPEEntryExists("PSE RA (deg)"))
				CENTROIDS_RADEG = (double[])BinTablePSE.GetTTYPEEntry("PSE RA (deg)", out _, out _, FITSBinTable.TTYPEReturn.AsDouble);
			if (BinTablePSE.TTYPEEntryExists("PSE Dec (deg)"))
				CENTROIDS_DECDEG = (double[])BinTablePSE.GetTTYPEEntry("PSE Dec (deg)", out _, out _, FITSBinTable.TTYPEReturn.AsDouble);
			if (BinTablePSE.TTYPEEntryExists("PSE RA (sxgsml)"))
				CENTROIDS_RAHMS = (string[])BinTablePSE.GetTTYPEEntry("PSE RA (sxgsml)", out _, out _, FITSBinTable.TTYPEReturn.AsDouble);
			if (BinTablePSE.TTYPEEntryExists("PSE Dec (sxgsml)"))
				CENTROIDS_DECDMS = (string[])BinTablePSE.GetTTYPEEntry("PSE Dec (sxgsml)", out _, out _, FITSBinTable.TTYPEReturn.AsDouble);
		}

		#endregion

		#region PRIVATE MEMBERS

		private string? NAME;
		private bool FITTED = false;
		private string? FIT_EQUATION;
		private bool WCS_GENERATED = false;
		//private bool VIEWFITS = false;
		Rectangle[]? PSESRECTS;
		bool VALIDPSERECTS = false;

		private int IMAGEWIDTH, IMAGEHEIGHT;
		private int N_SRC = -1;
		private int N_SATURATED = 0;
		private int KERNEL_RADIUS;
		private int KERNEL_WIDTH;
		private int BACKGROUND_RADIUS;
		private double PIX_SAT;
		private double PIX_SAT_MINMAP;
		private double PIX_MIN = 0;
		private double PIX_MAX = UInt16.MaxValue;
		private double KERNEL_MIN = 0;
		private double KERNEL_MAX = UInt16.MaxValue * 10;
		bool AUTO_BG;
		private bool SAVE_PS;
		private bool THRESHHOLDS_AS_SN;
		private bool SEARCH_ROI;
		private bool[,]? ROI_REGION;
		private int NGROUPS = 0;

		private string? SAVE_PS_FILENAME;
		private double[,]? IMAGE;
		private bool[,]? SOURCE_BOOLEAN_MAP;
		private int[,]? SOURCE_INDEX_MAP;
		private int[,]? SOURCE_GROUP_MAP;

		private int[]? GROUPIDS;
		private Group[]? GROUPS;
		private JPMath.PointD[]? CENTROID_POINTS;
		private double[]? CENTROIDS_X;
		private double[]? CENTROIDS_Y;
		private int[]? CENTROIDS_PIXEL_X;
		private int[]? CENTROIDS_PIXEL_Y;
		private double[]? CENTROIDS_RADEG;
		private string[]? CENTROIDS_RAHMS;
		private double[]? CENTROIDS_DECDEG;
		private string[]? CENTROIDS_DECDMS;
		private double[]? CENTROIDS_AMPLITUDE;
		private double[]? CENTROIDS_VOLUME;
		private double[]? CENTROIDS_AUTOBGEST;
		private double[]? CENTROIDS_ANULMEDBGEST;
		private double[]? CENTROIDS_ANULMEDBGSTD;
		private double[]? CENTROIDS_SNR;
		private bool[]? SATURATED;
		private bool REJECT_SATURATED;
		private bool BACKGROUND_RAD_AS_SSEP;

		private double[]? FITS_X;
		private double[]? FITS_Y;
		private double[]? FITS_FWHM_X;
		private double[]? FITS_FWHM_Y;
		private double[]? FITS_PHI;
		private double[]? FITS_RA_DEG;
		private string[]? FITS_RA_HMS;
		private double[]? FITS_DEC_DEG;
		private string[]? FITS_DEC_DMS;
		private double[,]? FITS_PARAMS;
		private double[]? FITS_AMPLITUDE;
		private double[]? FITS_VOLUME;
		private double[]? FITS_ECCENTRICITY;
		private double[]? FITS_FLATNESS;
		private double[]? FITS_BGESTIMATE;
		private double[]? FITS_CHISQNORM;

		private string[,]? PSE_TABLE;

		private double[]? LBND;
		private double[]? UBND;
		private double[]? PINI;

		private bool SHOWWAITBAR;
		private WaitBar WAITBAR;
		private BackgroundWorker BGWRKR;
		//private object BGWRKR_RESULT;
		private readonly object LOCKOBJECT = new object();

		private void BGWRKR_DoWork(object sender, DoWorkEventArgs e)
		{
			if (Convert.ToInt32(e.Argument) == 1)//Extract
			{
				ArrayList XPIXs = new ArrayList();// x position
				ArrayList YPIXs = new ArrayList();// y position
				ArrayList Xs = new ArrayList();// x centroids
				ArrayList Ys = new ArrayList();// y centroids
				ArrayList Ks = new ArrayList();// kernel sums
				ArrayList Ps = new ArrayList();// pixel values
				ArrayList Bs = new ArrayList();// background estimates
				ArrayList Ns = new ArrayList();// number of pixels
				double KRAD2 = KERNEL_RADIUS * KERNEL_RADIUS;
				double SSEP2 = BACKGROUND_RADIUS * BACKGROUND_RADIUS;
				int src_index = 0;

				if (PIX_SAT > 0)//check for saturation islands
				{
					Parallel.For(BACKGROUND_RADIUS, IMAGEWIDTH - BACKGROUND_RADIUS, x =>
					{
						for (int y = BACKGROUND_RADIUS; y < IMAGEHEIGHT - BACKGROUND_RADIUS; y++)
						{
							if (SEARCH_ROI)
								if (!ROI_REGION[x, y])
									continue;

							if (IMAGE[x, y] < PIX_SAT)
								continue;						

							lock (LOCKOBJECT)
							{
								if (SOURCE_BOOLEAN_MAP[x, y])
									continue;

								int xmin = x, xmax = x, ymin = y, ymax = y;
								MAPSATURATIONISLAND(x, y, src_index, ref xmin, ref xmax, ref ymin, ref ymax);

								if (xmax - xmin == 0)//single pixel, expand to 3 pixels
								{
									xmin--;
									if (xmin < 0)
										xmin = 0;
									xmax++;
									if (xmax >= IMAGE.GetLength(0))
										xmax = IMAGE.GetLength(0) - 1;
								}
								if (ymax - ymin == 0)//single pixel, expand to 3 pixels
								{
									ymin--;
									if (ymin < 0)
										ymin = 0;
									ymax++;
									if (ymax >= IMAGE.GetLength(1))
										ymax = IMAGE.GetLength(1) - 1;
								}
								double npixels = (xmax - xmin + 1) * (ymax - ymin + 1);

								double bg_est = 0, sigma = -1;
								if (AUTO_BG)
									bg_est = ESTIMATELOCALBACKGROUND((xmin + xmax) / 2, (ymin + ymax) / 2, BACKGROUND_RADIUS, BackGroundEstimateStyle.Corners2ndMin, ref sigma);

								double[,] kernel = new double[xmax - xmin + 1, ymax - ymin + 1];
								double x_centroid = 0, y_centroid = 0, kernel_sum = 0;
								for (int i = xmin; i <= xmax; i++)
									for (int j = ymin; j <= ymax; j++)
									{
										kernel[i - xmin, j - ymin] = IMAGE[i, j];
										x_centroid += (IMAGE[i, j] - bg_est) * i;
										y_centroid += (IMAGE[i, j] - bg_est) * j;
										kernel_sum += IMAGE[i, j] - bg_est;
									}
								x_centroid /= kernel_sum;
								y_centroid /= kernel_sum;

								if (Double.IsNaN(x_centroid) || Double.IsInfinity(x_centroid) || Double.IsNaN(y_centroid) || Double.IsInfinity(y_centroid))
									continue;
								//if (x_centroid < xmin || x_centroid > xmax || y_centroid < ymin || y_centroid > ymax)
								//	continue;

								src_index++;
								N_SATURATED++;
								XPIXs.Add((xmin + xmax) / 2);
								YPIXs.Add((ymin + ymax) / 2);
								Xs.Add(x_centroid);
								Ys.Add(y_centroid);
								Ks.Add(kernel_sum);
								Ps.Add(IMAGE[(xmin + xmax) / 2, (ymin + ymax) / 2]);
								Bs.Add(bg_est);
								Ns.Add(npixels);

								if (SAVE_PS)
								{
									string file = SAVE_PS_FILENAME;
									int ind = file.LastIndexOf(".");//for saving PS
									file = String.Concat(file.Substring(0, ind), "_", Xs.Count.ToString("00000000"), ".fits");

									JPFITS.FITSImage f = new JPFITS.FITSImage(file, kernel, false, false);
									f.WriteImage(DiskPrecision.Double, false);
								}
							}
						}
					});

					/*JPFITS.FITSImage^ ff = new FITSImage("C:\\Users\\Joseph E Postma\\Desktop\\atest.fits", IMAGE_KERNEL_INDEX_SOURCE, false);
					ff.WriteFile(TypeCode.Int32);*/
				}

				int intprg = 0;
				int n0 = (IMAGEWIDTH - 2 * BACKGROUND_RADIUS) / Environment.ProcessorCount + BACKGROUND_RADIUS;

				Parallel.For(BACKGROUND_RADIUS, IMAGEWIDTH - BACKGROUND_RADIUS, (Action<int, ParallelLoopState>)((int x, ParallelLoopState state) =>
				{
					if (SHOWWAITBAR)
					{
						if (WAITBAR.DialogResult == DialogResult.Cancel)
							state.Stop();

						if (x < n0 && (x - BACKGROUND_RADIUS) * 100 / (n0 - BACKGROUND_RADIUS) > intprg)//keep the update of progress bar to only one thread of the team...avoids locks
						{
							intprg = x * 100 / n0;
							BGWRKR.ReportProgress(intprg + 1);
						}
					}

					for (int y = BACKGROUND_RADIUS; y < IMAGEHEIGHT - BACKGROUND_RADIUS; y++)
					{
						if (SEARCH_ROI)
							if (!ROI_REGION[x, y])
								continue;

						if (SOURCE_BOOLEAN_MAP[x, y])
							continue;

						double bg_est = 0, sigma = -1;
						if (AUTO_BG)
							bg_est = ESTIMATELOCALBACKGROUND(x, y, BACKGROUND_RADIUS, BackGroundEstimateStyle.Corners2ndMin, ref sigma);

						double pixel = IMAGE[x, y] - bg_est;

						if (!THRESHHOLDS_AS_SN)
						{
							if (pixel < PIX_MIN || pixel > PIX_MAX)
								continue;
						}
						else//check as S/N
						{
							double Nbg = 0;
							if (bg_est < 1)
								Nbg = 1;
							else
								Nbg = Math.Sqrt(bg_est);
							double SNval = pixel / Nbg;
							if (SNval < PIX_MIN || SNval > PIX_MAX)
								continue;
						}

						if (BACKGROUND_RAD_AS_SSEP)
						{
							bool brek = false;
							for (int i = x - BACKGROUND_RADIUS; i <= x + BACKGROUND_RADIUS; i++)
							{
								double sx2 = (double)(x - i);
								sx2 *= sx2;
								for (int j = y - BACKGROUND_RADIUS; j <= y + BACKGROUND_RADIUS; j++)
								{
									double sy = (double)(y - j);
									double r2 = (sx2 + sy * sy) / SSEP2;

									if (r2 > 1)//outside the source separation circle
										continue;

									if (IMAGE[i, j] - bg_est > pixel)// max failure, the pixel isn't the maximum in the source separation circle
									{
										brek = true;
										break;
									}

									/*The minimum radial distance between two sources must be the SSR.
									When another source is just over (one pixel, say) the SSR from[x, y], its Boolean map which is from the CKR will extend towards[x, y] so that its Boolean map extends within the SSR of[x, y].
									Thus, it is OK to find some true values in the Boolean map from another source when exploring +-SSR from a given [x, y].
									However, what wouldn’t be OK is if when exploring the SSR, another source’s Boolean/index map extends to within less than half of the SSR of[x, y], 
									because then there would be no way that the radial distance between[x, y] and the other source-center would be at least SSR.*/
									if (r2 < 0.25 && SOURCE_BOOLEAN_MAP[i, j]) //a source was already found within the source separation
									{
										brek = true;
										break;
									}
								}
								if (brek)
									break;
							}
							if (brek)
								continue;
						}
						else
						{
							bool brek = false;
							for (int i = x - KERNEL_RADIUS; i <= x + KERNEL_RADIUS; i++)
							{
								double sx2 = (double)(x - i);
								sx2 *= sx2;
								for (int j = y - KERNEL_RADIUS; j <= y + KERNEL_RADIUS; j++)
								{
									double sy = (double)(y - j);
									double r2 = (sx2 + sy * sy) / KRAD2;

									if (r2 > 1)//outside the source separation circle
										continue;

									if (IMAGE[i, j] - bg_est > pixel)// max failure, the pixel isn't the maximum in the kernel circle
									{
										brek = true;
										break;
									}
								}
								if (brek)
									break;
							}
							if (brek)
								continue;
						}

						//do PSF kernel sum
						double kernel_psf_sum = 0, n_psf_pixels = 0;
						for (int i = x - KERNEL_RADIUS; i <= x + KERNEL_RADIUS; i++)
							for (int j = y - KERNEL_RADIUS; j <= y + KERNEL_RADIUS; j++)
							{
								double r2 = (double)((i - x) * (i - x) + (j - y) * (j - y));
								if (r2 > KRAD2)
									continue;

								kernel_psf_sum += IMAGE[i, j];
								n_psf_pixels++;
							}
						kernel_psf_sum -= (bg_est * n_psf_pixels);

						if (!THRESHHOLDS_AS_SN)
						{
							if (kernel_psf_sum < KERNEL_MIN || kernel_psf_sum > KERNEL_MAX)
								continue;
						}
						else//check as S/N
						{
							double Nbg = 0;
							if (bg_est < 1)
								Nbg = Math.Sqrt((double)(KERNEL_WIDTH * KERNEL_WIDTH));
							else
								Nbg = Math.Sqrt(bg_est * (double)(KERNEL_WIDTH * KERNEL_WIDTH));
							double SNenergy = kernel_psf_sum / Nbg;
							if (kernel_psf_sum < KERNEL_MIN || kernel_psf_sum > KERNEL_MAX)
								continue;
						}

						lock (LOCKOBJECT)
						{
							if (SOURCE_BOOLEAN_MAP[x, y])
								continue;

							double[,] kernel = GetKernel(IMAGE, (int)x, (int)y, KERNEL_RADIUS, out int[] xdata, out int[] ydata);
							if (bg_est != 0)
								kernel = JPMath.MatrixSubScalar(kernel, bg_est);

							Centroid(xdata, ydata, kernel, out double x_centroid, out double y_centroid);

							if (Double.IsNaN(x_centroid) || Double.IsInfinity(x_centroid) || Double.IsNaN(y_centroid) || Double.IsInfinity(y_centroid))
								continue;
							if (x_centroid < x - KERNEL_RADIUS || x_centroid > x + KERNEL_RADIUS || y_centroid < y - KERNEL_RADIUS || y_centroid > y + KERNEL_RADIUS)
								continue;

							RADIALIZE_MAPS_KERNEL(x, y, src_index, out int npixels);

							src_index++;
							XPIXs.Add(x);
							YPIXs.Add(y);
							Xs.Add(x_centroid);
							Ys.Add(y_centroid);
							Ks.Add(kernel_psf_sum);
							Ps.Add(pixel);
							Bs.Add(bg_est);
							Ns.Add((double)npixels);

							if (SAVE_PS)
							{
								string file = SAVE_PS_FILENAME;
								int ind = file.LastIndexOf(".");//for saving PS
								file = string.Concat(file.Substring(0, ind), "_", Xs.Count.ToString("00000000"), ".fits");

								FITSImage f = new JPFITS.FITSImage(file, kernel, false, false);
								f.WriteImage(DiskPrecision.Double, false);
							}
						}
					}
				}));

				if (SHOWWAITBAR)
					if (WAITBAR.DialogResult == DialogResult.Cancel)
						return;

				if (REJECT_SATURATED)
				{
					for (int i = 0; i < N_SATURATED; i++)
						DEMAP((int)XPIXs[i], (int)YPIXs[i], i);

					XPIXs.RemoveRange(0, N_SATURATED);
					YPIXs.RemoveRange(0, N_SATURATED);
					Xs.RemoveRange(0, N_SATURATED);
					Ys.RemoveRange(0, N_SATURATED);
					Ks.RemoveRange(0, N_SATURATED);
					Ps.RemoveRange(0, N_SATURATED);
					Bs.RemoveRange(0, N_SATURATED);
					Ns.RemoveRange(0, N_SATURATED);

					int[] indexes = new int[XPIXs.Count];
					for (int i = 0; i < XPIXs.Count; i++)
						indexes[i] = SOURCE_INDEX_MAP[(int)XPIXs[i], (int)YPIXs[i]];

					for (int i = 0; i < XPIXs.Count; i++)
						REMAP((int)XPIXs[i], (int)YPIXs[i], indexes[i], i);

					N_SATURATED = 0;
				}

				N_SRC = Xs.Count;
				INITARRAYS();

				for (int i = 0; i < N_SRC; i++)
				{
					CENTROIDS_PIXEL_X[i] = Convert.ToInt32(XPIXs[i]);
					CENTROIDS_PIXEL_Y[i] = Convert.ToInt32(YPIXs[i]);
					CENTROIDS_X[i] = Convert.ToDouble(Xs[i]);
					CENTROIDS_Y[i] = Convert.ToDouble(Ys[i]);
					CENTROIDS_AMPLITUDE[i] = Convert.ToDouble(Ps[i]);
					CENTROIDS_VOLUME[i] = Convert.ToDouble(Ks[i]);
					CENTROIDS_AUTOBGEST[i] = Convert.ToDouble(Bs[i]);
					CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_VOLUME[i]);
					if (i < N_SATURATED)
						SATURATED[i] = true;

					double bgstd = 0;
					CENTROIDS_ANULMEDBGEST[i] = ESTIMATELOCALBACKGROUND(CENTROIDS_PIXEL_X[i], CENTROIDS_PIXEL_Y[i], BACKGROUND_RADIUS, BackGroundEstimateStyle.AnnulusMedian, ref bgstd);
					CENTROIDS_ANULMEDBGSTD[i] = bgstd;
					CENTROIDS_SNR[i] = CENTROIDS_VOLUME[i] / Math.Sqrt(CENTROIDS_VOLUME[i] + CENTROIDS_ANULMEDBGEST[i] * Convert.ToDouble(Ns[i]));
				}

				return;
			}
			//returned if after Source Extraction

			//perform Fitting
			int intprog = 0;
			int np = N_SRC / Environment.ProcessorCount;
			Parallel.For(0, N_SRC, (int k, ParallelLoopState state) =>
			{
				if (SHOWWAITBAR)
				{
					if (WAITBAR.DialogResult == DialogResult.Cancel)
						state.Stop();

					if (k < np && k * 100 / np > intprog)//keep the update of progress bar to only one thread of the team...avoids locks
					{
						intprog = k * 100 / np;
						BGWRKR.ReportProgress(intprog);
					}
				}

				double[,] kernel = GetKernel(IMAGE, CENTROIDS_PIXEL_X[k], CENTROIDS_PIXEL_Y[k], KERNEL_RADIUS, out int[] xcoords, out int[] ycoords);
				double chisq_norm = 0;

				double[] P0 = PINI;
				double[] lb = LBND;
				double[] ub = UBND;
				if (PINI != null)
				{
					P0[0] = CENTROIDS_AMPLITUDE[k];
					P0[1] = CENTROIDS_X[k];
					P0[2] = CENTROIDS_Y[k];
					for (int i = 3; i < P0.Length - 1; i++)
						P0[i] = PINI[i];
					P0[P0.Length - 1] = CENTROIDS_AUTOBGEST[k];
				}	
				if (LBND != null)//set bounds to make sense
				{
					lb[0] = CENTROIDS_AMPLITUDE[k] / 2;
					lb[1] = CENTROIDS_X[k] - KERNEL_RADIUS;
					lb[2] = CENTROIDS_Y[k] - KERNEL_RADIUS;
					for (int i = 3; i < LBND.Length - 1; i++)
						lb[i] = LBND[i];
					lb[lb.Length - 1] = CENTROIDS_AUTOBGEST[k] - CENTROIDS_AMPLITUDE[k] / 3;
				}
				if (UBND != null)//set bounds to make sense
				{
					ub[0] = CENTROIDS_AMPLITUDE[k] * 2;
					ub[1] = CENTROIDS_X[k] + KERNEL_RADIUS;
					ub[2] = CENTROIDS_Y[k] + KERNEL_RADIUS;
					for (int i = 3; i < UBND.Length - 1; i++)
						ub[i] = UBND[i];
					ub[ub.Length - 1] = CENTROIDS_AUTOBGEST[k] + CENTROIDS_AMPLITUDE[k] / 3;
				}				

				if (Convert.ToInt32(e.Argument) == 2)//Fit circular Gaussian
				{
					if (P0 == null)
						P0 = new double[5];

					JPMath.Fit_PointSource(PointSourceModel.CircularGaussian, FitMinimizationType.ChiSquared, xcoords, ycoords, kernel, ref P0, lb, ub, out _, out _, out chisq_norm, out _);
					FITS_VOLUME[k] = 2 * Math.PI * P0[0] * P0[3] * P0[3];
					FITS_FWHM_X[k] = 2.355 * P0[3];
					FITS_FWHM_Y[k] = FITS_FWHM_X[k];
					FITS_ECCENTRICITY[k] = 0;
					FITS_FLATNESS[k] = 0;
					for (int i = 0; i < P0.Length; i++)
						FITS_PARAMS[i, k] = P0[i];
				}

				if (Convert.ToInt32(e.Argument) == 3)//Fit elliptical Gaussian
				{
					if (P0 == null)
						P0 = new double[7];

					JPMath.Fit_PointSource(PointSourceModel.EllipticalGaussian, FitMinimizationType.ChiSquared, xcoords, ycoords, kernel, ref P0, lb, ub, out _, out _, out chisq_norm, out _);
					FITS_VOLUME[k] = 2 * Math.PI * P0[0] * P0[4] * P0[5];
					FITS_FWHM_X[k] = 2.355 * P0[4];
					FITS_FWHM_Y[k] = 2.355 * P0[5];
					double a = FITS_FWHM_X[k], b = FITS_FWHM_Y[k];
					if (FITS_FWHM_Y[k] > FITS_FWHM_X[k])
					{
						a = FITS_FWHM_Y[k];
						b = FITS_FWHM_X[k];
					}
					FITS_ECCENTRICITY[k] = Math.Sqrt(1 - b * b / a / a);
					FITS_FLATNESS[k] = a / b - 1;
					FITS_PHI[k] = P0[3];
					for (int i = 0; i < P0.Length; i++)
						FITS_PARAMS[i, k] = P0[i];
				}

				if (Convert.ToInt32(e.Argument) == 4)//Fit circular Moffat
				{
					if (P0 == null)
						P0 = new double[6];

					JPMath.Fit_PointSource(PointSourceModel.CircularMoffat, FitMinimizationType.ChiSquared, xcoords, ycoords, kernel, ref P0, lb, ub, out _, out _, out chisq_norm, out _);
					FITS_VOLUME[k] = Math.PI * P0[3] * P0[3] * P0[0] / (P0[4] - 1);
					FITS_FWHM_X[k] = 2 * P0[3] * Math.Sqrt(Math.Pow(2, 1 / (P0[4])) - 1);
					FITS_FWHM_Y[k] = FITS_FWHM_X[k];
					FITS_ECCENTRICITY[k] = 0;
					FITS_FLATNESS[k] = 0;
					for (int i = 0; i < P0.Length; i++)
						FITS_PARAMS[i, k] = P0[i];
				}

				if (Convert.ToInt32(e.Argument) == 5)//Fit elliptical Moffat
				{
					if (P0 == null)
						P0 = new double[8];

					JPMath.Fit_PointSource(PointSourceModel.EllipticalMoffat, FitMinimizationType.ChiSquared, xcoords, ycoords, kernel, ref P0, lb, ub, out _, out _, out chisq_norm, out _);
					FITS_VOLUME[k] = Math.PI * P0[4] * P0[5] * P0[0] / (P0[6] - 1);
					FITS_FWHM_X[k] = 2 * P0[4] * Math.Sqrt(Math.Pow(2, 1 / (P0[6])) - 1);
					FITS_FWHM_Y[k] = 2 * P0[5] * Math.Sqrt(Math.Pow(2, 1 / (P0[6])) - 1);
					double a = FITS_FWHM_X[k], b = FITS_FWHM_Y[k];
					if (FITS_FWHM_Y[k] > FITS_FWHM_X[k])
					{
						a = FITS_FWHM_Y[k];
						b = FITS_FWHM_X[k];
					}
					FITS_ECCENTRICITY[k] = Math.Sqrt(1 - b * b / a / a);
					FITS_FLATNESS[k] = a / b - 1;
					FITS_PHI[k] = P0[3];
					for (int i = 0; i < P0.Length; i++)
						FITS_PARAMS[i, k] = P0[i];
				}

				FITS_AMPLITUDE[k] = P0[0];
				FITS_X[k] = P0[1];
				FITS_Y[k] = P0[2];
				FITS_BGESTIMATE[k] = P0[P0.Length - 1];				
				FITS_CHISQNORM[k] = chisq_norm;
			});
		}

		private void BGWRKR_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (e.ProgressPercentage >= 0 && e.ProgressPercentage <= 100)
			{
				WAITBAR.ProgressBar.Value = e.ProgressPercentage;
				WAITBAR.TextMsg.Text = "Scanned " + e.ProgressPercentage.ToString() + "% of the image...";
			}

			/*if (e.ProgressPercentage < 0)
			{
				WAITBAR.ProgressBar.Value = -e.ProgressPercentage;
				WAITBAR.TextMsg.Text = "Please wait while I remove adjacent sources..." + (-e.ProgressPercentage).ToString() + "%";
			}*/

			if (e.ProgressPercentage > 100)
			{
				WAITBAR.ProgressBar.Value = e.ProgressPercentage - 100;
				WAITBAR.TextMsg.Text = "Fitted " + (e.ProgressPercentage - 100).ToString() + "% of the sources...";
			}

			WAITBAR.Refresh();
		}

		private void BGWRKR_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (WAITBAR != null)
				WAITBAR.Close();
		}

		private void GENERATEPSETABLE()
		{
			if (!FITTED)
				PSE_TABLE = new string[14, N_SRC + 1];
			else
				PSE_TABLE = new string[28 + FITS_PARAMS.GetLength(0), N_SRC + 1];

			int c = 0;

			PSE_TABLE[c++, 0] = "PSE XCentroid";
			PSE_TABLE[c++, 0] = "PSE YCentroid";
			PSE_TABLE[c++, 0] = "PSE XPixel";
			PSE_TABLE[c++, 0] = "PSE YPixel";
			PSE_TABLE[c++, 0] = "PSE Amplitude";
			PSE_TABLE[c++, 0] = "PSE Volume";
			PSE_TABLE[c++, 0] = "PSE Background";
			PSE_TABLE[c++, 0] = "Annulus Background";
			PSE_TABLE[c++, 0] = "Annulus Background Stdv";
			PSE_TABLE[c++, 0] = "PSE SNR";
			PSE_TABLE[c++, 0] = "PSE RA (deg)";
			PSE_TABLE[c++, 0] = "PSE Dec (deg)";
			PSE_TABLE[c++, 0] = "PSE RA (sxgsml)";
			PSE_TABLE[c++, 0] = "PSE Dec (sxgsml)";

			if (FITTED)
			{
				PSE_TABLE[c++, 0] = "Fit X-Centroid";
				PSE_TABLE[c++, 0] = "Fit Y-Centroid";
				PSE_TABLE[c++, 0] = "Fit Amplitude";
				PSE_TABLE[c++, 0] = "Fit Volume";
				PSE_TABLE[c++, 0] = "Fit Background";
				PSE_TABLE[c++, 0] = "Fit RA (deg)";
				PSE_TABLE[c++, 0] = "Fit Dec (deg)";
				PSE_TABLE[c++, 0] = "Fit RA (sxgsml)";
				PSE_TABLE[c++, 0] = "Fit Dec (sxgsml)";
				PSE_TABLE[c++, 0] = "Fit FWHM_X";
				PSE_TABLE[c++, 0] = "Fit FWHM_Y";
				PSE_TABLE[c++, 0] = "Fit Eccentricity";
				PSE_TABLE[c++, 0] = "Fit Phi";
				PSE_TABLE[c++, 0] = "Fit ChiSqNorm";

				for (int j = 0; j < FITS_PARAMS.GetLength(0); j++)
					PSE_TABLE[c++, 0] = "Fit P(" + j + ")";
			}

			for (int i = 0; i < N_SRC; i++)
			{
				c = 0;
				PSE_TABLE[c++, i + 1] = CENTROIDS_X[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_Y[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_PIXEL_X[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_PIXEL_Y[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_AMPLITUDE[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_VOLUME[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_AUTOBGEST[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_ANULMEDBGEST[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_ANULMEDBGSTD[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_SNR[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_RADEG[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_DECDEG[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_RAHMS[i];
				PSE_TABLE[c++, i + 1] = CENTROIDS_DECDMS[i];

				if (FITTED)
				{
					PSE_TABLE[c++, i + 1] = FITS_X[i].ToString();
					PSE_TABLE[c++, i + 1] = FITS_Y[i].ToString();
					PSE_TABLE[c++, i + 1] = FITS_AMPLITUDE[i].ToString();
					PSE_TABLE[c++, i + 1] = FITS_VOLUME[i].ToString();
					PSE_TABLE[c++, i + 1] = FITS_BGESTIMATE[i].ToString();
					PSE_TABLE[c++, i + 1] = FITS_RA_DEG[i].ToString();
					PSE_TABLE[c++, i + 1] = FITS_DEC_DEG[i].ToString();
					PSE_TABLE[c++, i + 1] = FITS_RA_HMS[i];
					PSE_TABLE[c++, i + 1] = FITS_DEC_DMS[i];
					PSE_TABLE[c++, i + 1] = FITS_FWHM_X[i].ToString();
					PSE_TABLE[c++, i + 1] = FITS_FWHM_Y[i].ToString();
					PSE_TABLE[c++, i + 1] = FITS_ECCENTRICITY[i].ToString();
					PSE_TABLE[c++, i + 1] = FITS_PHI[i].ToString();
					PSE_TABLE[c++, i + 1] = FITS_CHISQNORM[i].ToString();

					for (int j = 0; j < FITS_PARAMS.GetLength(0); j++)
						PSE_TABLE[c++, i + 1] = FITS_PARAMS[j, i].ToString();
				}
			}
		}

		public enum BackGroundEstimateStyle
		{
			/// <summary>
			/// No background estimation is performed - this implies that the images have already been background subtracted. Return value is zero.
			/// </summary>
			None,

			/// <summary>
			/// The 2nd-minimum of the 4-corners of the Background Radius box centered on the source pixel. Very fast and reasonably accurate.
			/// </summary>
			Corners2ndMin,

			/// <summary>
			/// The median of the square periphery annulus of the Background Radius box centered on the source pixel. Very accurate but slower.
			/// </summary>
			AnnulusMedian
		}

		/// <summary>
		/// Estimate the local background around a given pixel location in the image.
		/// </summary>
		/// <param name="x">The horizontal x-pixel location.</param>
		/// <param name="y">The vertical y-pixel location.</param>
		/// <param name="radius">The radius around the pixel at which to estimate the background.</param>
		/// <param name="bgeststyle">The style of background estimation.</param>
		/// <param name="sigma">Pass -1 to not do as it significantly increases computation time. Best to use only after sources have already been found. Returns the standard deviation of all possible background values given the estimation method.</param>
		[MethodImpl(256)]
		private double ESTIMATELOCALBACKGROUND(int x, int y, int radius, BackGroundEstimateStyle bgeststyle, ref double sigma)
		{
			if (bgeststyle == BackGroundEstimateStyle.None)
				return 0;

			int xmin = x - radius, xmax = x + radius, ymin = y - radius, ymax = y + radius;
			if (xmin < 0) xmin = 0;
			if (ymin < 0) ymin = 0;
			if (xmax >= IMAGE.GetLength(0)) xmax = IMAGE.GetLength(0) - 1;
			if (ymax >= IMAGE.GetLength(1)) ymax = IMAGE.GetLength(1) - 1;

			if (bgeststyle == BackGroundEstimateStyle.Corners2ndMin)
			{
				double[] corners = new double[4] { IMAGE[xmin, ymin], IMAGE[xmin, ymax], IMAGE[xmax, ymin], IMAGE[xmax, ymax] };

				if (sigma != -1)
					sigma = JPMath.Stdv(corners, false);

				int minind = 0;
				if (corners[1] < corners[minind])
					minind = 1;
				if (corners[2] < corners[minind])
					minind = 2;
				if (corners[3] < corners[minind])
					minind = 3;

				corners[minind] = Double.MaxValue;
				double min = corners[0];
				if (corners[1] < min)
					min = corners[1];
				if (corners[2] < min)
					min = corners[2];
				if (corners[3] < min)
					min = corners[3];

				return min;
			}
			else if (bgeststyle == BackGroundEstimateStyle.AnnulusMedian)
			{
				double[] xtop = new double[xmax - xmin + 1];
				double[] xbot = new double[xmax - xmin + 1];
				double[] yleft = new double[ymax - ymin - 1];
				double[] yrite = new double[ymax - ymin - 1];

				for (int i = 0; i < xtop.Length; i++)
				{
					xtop[i] = IMAGE[xmin + i, ymin];
					xbot[i] = IMAGE[xmin + i, ymax];
				}

				for (int i = 0; i < yleft.Length; i++)
				{
					yleft[i] = IMAGE[xmin, ymin + 1 + i];
					yrite[i] = IMAGE[xmax, ymin + 1 + i];
				}

				double[] vect = new double[xtop.Length + xbot.Length + yleft.Length + yrite.Length];

				Array.Copy(xtop, 0, vect, 0, xtop.Length);
				Array.Copy(xbot, 0, vect, xtop.Length, xbot.Length);
				Array.Copy(yleft, 0, vect, xtop.Length + xbot.Length, yleft.Length);
				Array.Copy(yrite, 0, vect, xtop.Length + xbot.Length + yleft.Length, yrite.Length);

				if (sigma != -1)
					sigma = JPMath.Stdv(vect, false);

				return JPMath.Median(vect);
			}
			else
				throw new Exception("Made it to end of ESTIMATELOCALBACKGROUND without returning.");
		}

		[MethodImpl(256)]/*256 = agressive inlining*/
		public void RADIALIZE_MAPS_KERNEL(int x, int y, int sourceIndex, out int npixels)
		{
			int krad2 = KERNEL_RADIUS * KERNEL_RADIUS;
			npixels = 0;

			for (int i = x - KERNEL_RADIUS; i <= x + KERNEL_RADIUS; i++)
			{
				double sx2 = x - i;
				sx2 *= sx2;

				for (int j = y - KERNEL_RADIUS; j <= y + KERNEL_RADIUS; j++)
				{
					double sy = y - j;
					double r2 = sx2 + sy * sy;
					if (r2 > krad2)
						continue;

					SOURCE_BOOLEAN_MAP[i, j] = true;//this kernel radius position has a source detected
					SOURCE_INDEX_MAP[i, j] = sourceIndex;//this is the source index of the given detection kernel radius position
					npixels++;
				}
			}
		}

		[MethodImpl(256)]
		private void MAPSATURATIONISLAND(int X, int Y, int sourceindex, ref int xmin, ref int xmax, ref int ymin, ref int ymax)
		{
			SOURCE_BOOLEAN_MAP[X, Y] = true;
			SOURCE_INDEX_MAP[X, Y] = sourceindex;

			if (X < xmin)
				xmin = X;
			if (X > xmax)
				xmax = X;
			if (Y < ymin)
				ymin = Y;
			if (Y > ymax)
				ymax = Y;

			for (int x = X - 1; x <= X + 1; x++)
				for (int y = Y - 1; y <= Y + 1; y++)
					if (SAFETOMAPSATURATION(x, y))
						MAPSATURATIONISLAND(x, y, sourceindex, ref xmin, ref xmax, ref ymin, ref ymax);
		}

		[MethodImpl(256)]
		private bool SAFETOMAPSATURATION(int x, int y)
		{
			//return (x >= 0) && (x < IMAGEWIDTH) && (y >= 0) && (y < IMAGEHEIGHT) && (IMAGE[x, y] >= PIX_SAT) && (SOURCE_INDEX_MAP[x, y] == -1);
			return (x >= 0) && (x < IMAGEWIDTH) && (y >= 0) && (y < IMAGEHEIGHT) && (IMAGE[x, y] >= PIX_SAT_MINMAP) && (SOURCE_INDEX_MAP[x, y] == -1);
		}
		
		[MethodImpl(256)]
		private void DEMAP(int X, int Y, int sourceindex)
		{
			SOURCE_BOOLEAN_MAP[X, Y] = false;
			SOURCE_INDEX_MAP[X, Y] = -1;

			for (int x = X - 1; x <= X + 1; x++)
				for (int y = Y - 1; y <= Y + 1; y++)
					if (SAFETODEMAP(x, y, sourceindex))
						DEMAP(x, y, sourceindex);
		}

		[MethodImpl(256)]
		private bool SAFETODEMAP(int x, int y, int sourceindex)
		{
			return (x >= 0) && (x < IMAGEWIDTH) && (y >= 0) && (y < IMAGEHEIGHT) && SOURCE_BOOLEAN_MAP[x, y] && (sourceindex == SOURCE_INDEX_MAP[x, y]);
		}

		[MethodImpl(256)]
		private void REMAP(int X, int Y, int oldindex, int newindex)
		{
			if (oldindex == newindex)
				return;

			SOURCE_INDEX_MAP[X, Y] = newindex;

			for (int x = X - 1; x <= X + 1; x++)
				for (int y = Y - 1; y <= Y + 1; y++)
					if (SAFETOREMAP(x, y, oldindex))
						REMAP(x, y, oldindex, newindex);
		}

		[MethodImpl(256)]
		private bool SAFETOREMAP(int x, int y, int oldindex)
		{
			return (x >= 0) && (x < IMAGEWIDTH) && (y >= 0) && (y < IMAGEHEIGHT) && SOURCE_BOOLEAN_MAP[x, y] && (oldindex == SOURCE_INDEX_MAP[x, y]);
		}

		private void INITARRAYS()
		{			
			CENTROIDS_X = new double[N_SRC];
			CENTROIDS_Y = new double[N_SRC];
			CENTROIDS_PIXEL_X = new int[N_SRC];
			CENTROIDS_PIXEL_Y = new int[N_SRC];
			CENTROIDS_AMPLITUDE = new double[N_SRC];
			CENTROIDS_VOLUME = new double[N_SRC];
			CENTROIDS_AUTOBGEST = new double[N_SRC];
			CENTROIDS_ANULMEDBGEST = new double[N_SRC];
			CENTROIDS_ANULMEDBGSTD = new double[N_SRC];
			CENTROIDS_SNR = new double[N_SRC];
			SATURATED = new bool[N_SRC];
			CENTROIDS_RADEG = new double[N_SRC];
			CENTROIDS_DECDEG = new double[N_SRC];
			CENTROIDS_RAHMS = new string[N_SRC];
			CENTROIDS_DECDMS = new string[N_SRC];
			FITS_X = new double[N_SRC];
			FITS_Y = new double[N_SRC];
			FITS_AMPLITUDE = new double[N_SRC];
			FITS_VOLUME = new double[N_SRC];
			FITS_BGESTIMATE = new double[N_SRC];
			FITS_FWHM_X = new double[N_SRC];
			FITS_FWHM_Y = new double[N_SRC];
			FITS_PHI = new double[N_SRC];
			FITS_CHISQNORM = new double[N_SRC];			
			FITS_RA_DEG = new double[N_SRC];
			FITS_RA_HMS = new string[N_SRC];
			FITS_DEC_DEG = new double[N_SRC];
			FITS_DEC_DMS = new string[N_SRC];
			FITS_ECCENTRICITY = new double[N_SRC];
			FITS_FLATNESS = new double[N_SRC];
			CENTROID_POINTS = new JPMath.PointD[N_SRC];
		}

		private void RECURSGROUP(int I, int J, double groupRadius, int groupid, ArrayList groups)
		{
			GROUPIDS[J] = groupid;

			GraphicsPath path = new GraphicsPath();
			path.AddEllipse((float)(CENTROIDS_X[J] - groupRadius), (float)(CENTROIDS_Y[J] - groupRadius), (float)(groupRadius * 2), (float)(groupRadius * 2));
			((Group)groups[groupid]).REGION.Union(path);

			for (int i = 0; i < N_SRC; i++)
				if (i != J && CENTROID_POINTS[J].DistanceTo(CENTROID_POINTS[i]) <= groupRadius)
					if (GROUPIDS[i] == -1)
						RECURSGROUP(J, i, groupRadius, groupid, groups);
		}

		#endregion
		
		#region PROPERTIES

		public Group[] Groups
		{
			get { return GROUPS; }
		}

		public int[] GroupIDs
		{
			get { return GROUPIDS; }
		}

		public int[,] SourceGroupMap
		{
			get { return SOURCE_GROUP_MAP; }
		}

		/// <summary>Gets a metadata table of the extracted sources.</summary>
		public string[,] Source_Table
		{
			get
			{
				this.GENERATEPSETABLE();
				return this.PSE_TABLE;
			}
		}

		public Rectangle[] PSEImageRectangles
		{
			get { return PSESRECTS; }
		}

		/// <summary>Gets the x-axis center kernel pixel of extracted sources.</summary>
		public int[] Centroids_X_Pixel
		{
			get { return CENTROIDS_PIXEL_X; }
		}

		/// <summary>Gets the y-axis center kernel pixel of extracted sources.</summary>
		public int[] Centroids_Y_Pixel
		{
			get { return CENTROIDS_PIXEL_Y; }
		}

		/// <summary>Gets or Sets the x-axis centroids of extracted sources.</summary>
		public double[] Centroids_X
		{
			get { return CENTROIDS_X; }
			set
			{
				CENTROIDS_X = value;
				N_SRC = CENTROIDS_X.Length;
				//CENTROID_POINTS = new JPMath.PointD[N_SRC];
				//for (int i = 0; i < N_SRC; i++)
				//	CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_VOLUME[i]);
			}
		}

		/// <summary>Gets or Sets the y-axis centroids of extracted sources.</summary>
		public double[] Centroids_Y
		{
			get { return CENTROIDS_Y; }
			set
			{
				CENTROIDS_Y = value;
				N_SRC = CENTROIDS_Y.Length;
				//CENTROID_POINTS = new JPMath.PointD[N_SRC];
				//for (int i = 0; i < N_SRC; i++)
				//	CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_VOLUME[i]);
			}
		}

		/// <summary>Gets the volume (total count) of extracted sources.</summary>
		public double[] Centroids_Volume
		{
			get { return CENTROIDS_VOLUME; }
			set { CENTROIDS_VOLUME = value; }
		}

		public double[] Fit_Volumes
		{
			get { return FITS_VOLUME; }
		}

		public double[] Signal_to_Noise_SNR
		{
			get { return CENTROIDS_SNR; }
		}

		public double[] Fit_RA_Deg
		{
			get { return FITS_RA_DEG; }
		}

		public double[] Fit_Dec_Deg
		{
			get { return FITS_DEC_DEG; }
		}

		public double[] Centroids_RA_Deg
		{
			get { return CENTROIDS_RADEG; }
		}

		public double[] Centroids_Dec_Deg
		{
			get { return CENTROIDS_DECDEG; }
		}

		public double[] Fit_FWHM_X
		{
			get { return FITS_FWHM_X; }
		}

		public double[] Fit_FWHM_Y
		{
			get { return FITS_FWHM_Y; }
		}

		public double[] Fit_FWHM
		{
			get 
			{
				double[] fwhm = new double[FITS_FWHM_Y.Length];
				for (int i = 0; i < fwhm.Length; i++)
					fwhm[i] = (FITS_FWHM_Y[i] + FITS_FWHM_X[i]) / 2;

				return fwhm;
			}
		}

		public double[] Fit_Eccentricity
		{
			get
			{
				return FITS_ECCENTRICITY;
			}
		}

		public double[] Fit_Flatness
		{
			get
			{
				return FITS_FLATNESS;
			}
		}

		/// <summary>Gets the total number of extracted sources.</summary>
		public int N_Sources
		{
			get { return N_SRC; }
		}

		/// <summary>Gets the number of saturated sources.</summary>
		public int N_SaturatedSources
		{
			get { return N_SATURATED; }
		}

		/// <summary>Gets a list of the fitted parameters for all sources.</summary>
		public double[,] Fitted_Parameter_List
		{
			get { return FITS_PARAMS; }
		}

		/// <summary>Gets a string of the equation used for least-squares fitting.</summary>
		public string LSFit_Equation
		{
			get { return FIT_EQUATION; }
		}

		/// <summary>Gets a boolean to indicate whether least-squares fits have been performed.</summary>
		public bool Fitted
		{
			get { return FITTED; }
		}

		public bool WCS_Coordinates_Generated
		{
			get { return WCS_GENERATED; }
		}

		public string Name
		{
			get { return NAME; }
			set { NAME = value; }
		}

		/// <summary>Returns the source Boolean map. Returns whether or not a location in the image has an extracted source.</summary>
		public bool[,] SourceBooleanMap
		{
			get { return SOURCE_BOOLEAN_MAP; }
			set { SOURCE_BOOLEAN_MAP = value; }
		}

		/// <summary>Returns the integer source index map. Locations in the PSE image which have an extracted kernel will return the index of the source in the various lists.</summary>
		public int[,] SourceIndexMap
		{
			get { return SOURCE_INDEX_MAP; }
			set { SOURCE_INDEX_MAP = value; }
		}

		public bool IsBusy
		{
			get { return BGWRKR.IsBusy; }
		}

		/// <summary>Returns the pixel saturation value which was passed when performing the source extraction</summary>
		public double PixelSaturation
		{
			get { return PIX_SAT; }
		}

		/// <summary>Returns whether the source at the specified index is a saturated source.</summary>
		public bool IsSaturated(int index)
		{
			return SATURATED[index];
		}

		/// <summary>Returns the kernel radius value which was passed when performing the source extraction</summary>
		public int KernelRadius
		{
			get { return KERNEL_RADIUS; }
			set { KERNEL_RADIUS = value; }
		}

		public int KernelWidth
		{
			get { return KERNEL_WIDTH; }
		}

		/// <summary>Returns the source separation value which was passed when performing the source extraction</summary>
		public int BackgroundRadius
		{
			get { return BACKGROUND_RADIUS; }
		}

		/// <summary>Returns the maximum pixel value which was passed when performing the source extraction</summary>
		public double PixelMaximum
		{
			get { return PIX_MAX; }
		}

		/// <summary>Returns the minimum pixel value which was passed when performing the source extraction</summary>
		public double PixelMinimum
		{
			get { return PIX_MIN; }
		}

		/// <summary>Returns the maximum kernel value which was passed when performing the source extraction</summary>
		public double KernelMaximum
		{
			get { return KERNEL_MAX; }
		}

		/// <summary>Returns the minimum kernel value which was passed when performing the source extraction</summary>
		public double KernelMinimum
		{
			get { return KERNEL_MIN; }
		}

		/// <summary>Returns whether the background was automatically determined when performing the source extraction.</summary>
		public bool AutoBackground
		{
			get { return AUTO_BG; }
		}

		/// <summary>Returns whether the point sources were saved when performing the source extraction.</summary>
		public bool SavePointSources
		{
			get { return SAVE_PS; }
		}

		/*property string SavePointSourcesFileNameTemplate
		{
			string get() { return SAVE_PS_FILENAME; }
		}*/

		/// <summary>Returns whether a region of interest of the image was only used when performing the source extraction.</summary>
		public bool SearchROI
		{
			get { return SEARCH_ROI; }
		}

		public int NGroups
		{
			get { return NGROUPS; }
		}
		#endregion
		
		#region STATIC METHODS

		/// <summary>Gets a square sub-array kernel from a primary image given a center position and square half-width radius.</summary>
		/// <param name="image">The source image to extract the kernel from.</param>
		/// <param name="x0">The center pixel of the kernel on the horizontal axis of the image.</param>
		/// <param name="y0">The center pixel of the kernel on the vertical axis of the image.</param>
		/// <param name="radius">The radius of the kernel.</param>
		public static double[,] GetKernel(double[,] image, int x0, int y0, int radius, out int[] xrange, out int[] yrange)
		{
			int width = radius * 2 + 1, kx = -1, x, y, xmin = x0 - radius, ymin = y0 - radius;
			double[,] kernel = new double[width, width];
			xrange = new int[width];
			yrange = new int[width];

			for (x = 0; x < width; x++)
			{
				kx = xmin + x;
				for (y = 0; y < width; y++)
					kernel[x, y] = image[kx, ymin + y];

				xrange[x] = kx;
				yrange[x] = ymin + x;
			}

			return kernel;
		}

		/// <summary>Determines the [x, y] centroid location of a given kernel.</summary>
		/// <param name="xdata">The horizontal axis values of the kernel.</param>
		/// <param name="ydata">The vertical axis values of the kernel.</param>
		/// <param name="kernel">The kernel to centroid.</param>
		/// <param name="x_centroid">The weighed mean centroid of the kernel on the horizontal axis.</param>
		/// <param name="y_centroid">The weighed mean centroid of the kernel on the vertical axis.</param>
		public static void Centroid(int[] xdata, int[] ydata, double[,] kernel, out double x_centroid, out double y_centroid)
		{
			int xw = kernel.GetLength(0);
			int yh = kernel.GetLength(1);

			if (xdata == null)
			{
				xdata = new int[xw];
				for (int i = -xw / 2; i <= xw / 2; i++)
					xdata[i] = i;
			}
			if (ydata == null)
			{
				ydata = new int[yh];
				for (int i = -yh / 2; i <= yh / 2; i++)
					ydata[i] = i;
			}

			double xweighted = 0, yweighted = 0, kernel_sum = 0;
			for (int x = 0; x < xw; x++)
				for (int y = 0; y < yh; y++)
				{
					xweighted += kernel[x, y] * (double)(xdata[x]);
					yweighted += kernel[x, y] * (double)(ydata[y]);
					kernel_sum += kernel[x, y];
				}

			x_centroid = xweighted / kernel_sum;
			y_centroid = yweighted / kernel_sum;
		}

		public void Centroid(int xmin, int xmax, int ymin, int ymax, double bg_est, out double x_centroid, out double y_centroid, out double[,] kernel)
		{
			kernel = new double[xmax - xmin + 1, ymax - ymin + 1];
			x_centroid = 0;
			y_centroid = 0;
			double kernel_sum = 0;
			for (int x = xmin; x <= xmax; x++)
				for (int y = ymin; y <= ymax; y++)
				{
					kernel[x - xmin, y - ymin] = IMAGE[x, y] - bg_est;
					x_centroid += (IMAGE[x, y] - bg_est) * x;
					y_centroid += (IMAGE[x, y] - bg_est) * y;
					kernel_sum += IMAGE[x, y] - bg_est;
				}
			x_centroid /= kernel_sum;
			y_centroid /= kernel_sum;
		}

		/// <summary>Determines the Curve of Growth photometry for a source centered in the ROI image.</summary>
		/// <param name="ROI">The region of interest image to determine the curve of growth for.</param>
		/// <param name="N_last_fit_pts">The number of tailing points to fit for linear slope - intercept of this line is source counts, slope is the background count per pixel.</param>
		/// <param name="N_points_COG">The number of points for each curve of growth point. Used as the abscissa against the return value.</param>
		/// <param name="background_signal_per_pix">The slope of the linear fit line to the tailing points, i.e., the counts per pixel background.</param>
		/// <param name="source_signal">The intercept of the linear fit line to the tailing points, i.e., the total central source counts.</param>
		public static double[] COG(double[,] ROI, int N_last_fit_pts, out double[] N_points_COG, out double background_signal_per_pix, out double source_signal)
		{
			if (ROI.GetLength(0) != ROI.GetLength(1))
				throw new Exception("Error: ROI array must be square.");

			if (ROI.GetLength(0) < 5)
				throw new Exception("Error: Region of interest SubWindow must be at least 5x5 pixels...");

			if (JPMath.IsEven(ROI.GetLength(0)) || JPMath.IsEven(ROI.GetLength(1)))
				throw new Exception("Error: ROI array not odd-size.");

			int HalfWidth = (ROI.GetLength(0) - 1) / 2;
			double[] N_points_COGlocal = new double[HalfWidth + 1];
			double[] cog = new double[HalfWidth + 1];

			Parallel.For(0, HalfWidth + 1, i =>
			//for (int i = 0; i <= HalfWidth; i++)
			{
				double data = 0;
				double diskmask = 0;
				double isq = i * i;

				for (int ii = -i; ii <= i; ii++)
				{
					double iisq = ii * ii;
					for (int jj = -i; jj <= i; jj++)
					{
						if (iisq + jj * jj > isq)
							continue;

						data += ROI[HalfWidth + jj, HalfWidth + ii];
						diskmask++;
					}
				}
				cog[i] = data;
				N_points_COGlocal[i] = diskmask;
			});

			N_points_COG = new double[HalfWidth + 1];
			N_points_COGlocal.CopyTo(N_points_COG, 0);

			if (N_last_fit_pts > cog.Length)
				N_last_fit_pts = cog.Length;

			double[] xdata = new double[N_last_fit_pts];
			double[] ydata = new double[N_last_fit_pts];
			for (int i = cog.Length - N_last_fit_pts; i < cog.Length; i++)
			{
				xdata[i - (cog.Length - N_last_fit_pts)] = N_points_COG[i];
				ydata[i - (cog.Length - N_last_fit_pts)] = cog[i];
			}
			JPMath.Fit_Poly1d(xdata, ydata, 1, true, out double[] param);

			source_signal = param[0];
			background_signal_per_pix = param[1];

			return cog;
		}

		#endregion
				
	}
}

