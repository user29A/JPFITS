using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;
#nullable enable

namespace JPFITS
{
	/// <summary>SourceExtractor class provides functionality for extracting sources from image arrays.</summary>
	public class PointSourceExtractor
	{
		#region PRIVATE MEMBERS

		private string? NAME;
		private bool FITTED = false;
		private string? FIT_EQUATION;
		//private bool WCS_GENERATED = false;
		//private bool VIEWFITS = false;
		private bool PSEPARAMSSET = false;

		private int IMAGEWIDTH, IMAGEHEIGHT;
		private int N_SRC = 0;                          // number of sources found
		private int N_SATURATED = 0;
		private int KERNEL_RADIUS;                      // source widths for centroiding
		private int KERNEL_WIDTH;
		private int SOURCE_SEPARATION;
		private double PIX_SAT;                         //pixel saturation
		private double PIX_MIN;                         // source min pixel thresh
		private double PIX_MAX;                         // source max pixel thresh
		private double KERNEL_MIN;                      // total source min count thresh
		private double KERNEL_MAX;                      // total source max count thresh
		bool AUTO_BG;                           //automatic background approximation
		private bool SAVE_PS;
		private bool THRESHHOLDS_AS_SN;                 //interpret pixel value and total count thresholds as SN
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
		private double[]? CENTROIDS_X;               // x centroid positions of sources
		private double[]? CENTROIDS_Y;               // y centroid positions of sources
		private int[]? CENTROIDS_PIXEL_X;
		private int[]? CENTROIDS_PIXEL_Y;
		private double[]? CENTROIDS_RADEG;      // right ascension centroid positions of sources - if available
		private string[]? CENTROIDS_RAHMS;      // right ascension centroid positions of sources - if available
		private double[]? CENTROIDS_DECDEG;     // declination centroid positions of sources - if available
		private string[]? CENTROIDS_DECDMS;     // declination centroid positions of sources - if available
		private double[]? CENTROIDS_AMPLITUDE;       // sources values (above fcmin)
		private double[]? CENTROIDS_VOLUME;      // sources energies (above fcmin)
		private double[]? CENTROIDS_AUTOBGEST;  // corner minimum - estimate of background
		private double[]? CENTROIDS_ANULMEDBGEST;  // annulus median background estimate
		private double[]? CENTROIDS_SNR;

		private double[]? FITS_X;                    // x fitted positions of sources
		private double[]? FITS_Y;                    // y fitted positions of sources
		private double[]? FITS_FWHM_X;               // FWHM of sources
		private double[]? FITS_FWHM_Y;               // FWHM of sources
		private double[]? FITS_PHI;              // rotation theta of elliptical fits
		private double[]? FITS_RA_DEG;               // right ascension centroid positions of sources - if available
		private string[]? FITS_RA_HMS;           // right ascension centroid positions of sources - if available
		private double[]? FITS_DEC_DEG;          // declination centroid positions of sources - if available
		private string[]? FITS_DEC_DMS;          // declination centroid positions of sources - if available
		private double[,]? FITS_PARAMS;          // fitted paramaters of sources - 2d because multiple parameters per source
		private double[]? FITS_AMPLITUDE;            // 
		private double[]? FITS_VOLUME;               // 
		private double[]? FITS_BGESTIMATE;           // 
		private double[]? FITS_CHISQNORM;            //

		private string[,]? PSE_TABLE;

		private double[]? LBND;
		private double[]? UBND;
		private double[]? PINI;

		private bool SHOWWAITBAR;
		private WaitBar WAITBAR;
		private BackgroundWorker BGWRKR;
		//private object BGWRKR_RESULT;

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
				double SSEP2 = SOURCE_SEPARATION * SOURCE_SEPARATION, KRAD2 = KERNEL_RADIUS * KERNEL_RADIUS;
				int src_index = 0;

				if (PIX_SAT > 0)//check for saturation islands
				{
					Parallel.For(SOURCE_SEPARATION, IMAGEWIDTH - SOURCE_SEPARATION, x =>
					{
						for (int y = SOURCE_SEPARATION; y < IMAGEHEIGHT - SOURCE_SEPARATION; y++)
						{
							if (SEARCH_ROI)
								if (!ROI_REGION[x, y])
									continue;

							if (IMAGE[x, y] < PIX_SAT)
								continue;

							if (SOURCE_BOOLEAN_MAP[x, y])
								continue;

							lock (SOURCE_BOOLEAN_MAP)
							{
								if (SOURCE_BOOLEAN_MAP[x, y])
									continue;

								int Xmin = x, Xmax = x, Ymin = y, Ymax = y;
								MAPSATURATIONISLAND(x, y, src_index, ref Xmin, ref Xmax, ref Ymin, ref Ymax);

								if (Xmax - Xmin == 0)//single pixel, expand to 3 pixels
								{
									Xmin--;
									Xmax++;
								}
								if (Ymax - Ymin == 0)//single pixel, expand to 3 pixels
								{
									Ymin--;
									Ymax++;
								}

								double bg_est = 0, sigma = -1;
								if (AUTO_BG)
									bg_est = ESTIMATELOCALBACKGROUND((Xmin + Xmax) / 2, (Ymin + Ymax) / 2, SOURCE_SEPARATION, BackGroundEstimateStyle.Corners, ref sigma);

								double[,] kernel = new double[Xmax - Xmin + 1, Ymax - Ymin + 1];
								double x_centroid = 0, y_centroid = 0, kernel_sum = 0;
								for (int i = Xmin; i <= Xmax; i++)
									for (int j = Ymin; j <= Ymax; j++)
									{
										kernel[i - Xmin, j - Ymin] = IMAGE[i, j];
										x_centroid += (IMAGE[i, j] - bg_est) * i;
										y_centroid += (IMAGE[i, j] - bg_est) * j;
										kernel_sum += IMAGE[i, j] - bg_est;
									}
								x_centroid /= kernel_sum;
								y_centroid /= kernel_sum;

								src_index++;
								N_SATURATED++;
								XPIXs.Add((Xmin + Xmax) / 2);
								YPIXs.Add((Ymin + Ymax) / 2);
								Xs.Add(x_centroid);
								Ys.Add(y_centroid);
								Ks.Add(kernel_sum);
								Ps.Add(IMAGE[x, y]);
								Bs.Add(bg_est);

								if (SAVE_PS)
								{
									string file = SAVE_PS_FILENAME;
									int ind = file.LastIndexOf(".");//for saving PS
									file = String.Concat(file.Substring(0, ind), "_", Xs.Count.ToString("00000000"), ".fits");

									JPFITS.FITSImage f = new JPFITS.FITSImage(file, kernel, false, false);
									f.WriteImage(TypeCode.Double, false);
								}
							}
						}
					});

					/*JPFITS.FITSImage^ ff = new FITSImage("C:\\Users\\Joseph E Postma\\Desktop\\atest.fits", IMAGE_KERNEL_INDEX_SOURCE, false);
					ff.WriteFile(TypeCode.Int32);*/
				}

				int intprg = 0;
				int n0 = (IMAGEWIDTH - 2 * SOURCE_SEPARATION) / Environment.ProcessorCount + SOURCE_SEPARATION;

				Parallel.For(SOURCE_SEPARATION, IMAGEWIDTH - SOURCE_SEPARATION, (Action<int, ParallelLoopState>)((int x, ParallelLoopState state) =>
				{
					if (SHOWWAITBAR)
					{
						if (WAITBAR.DialogResult == DialogResult.Cancel)
							state.Stop();

						if (x < n0 && (x - SOURCE_SEPARATION) * 100 / (n0 - SOURCE_SEPARATION) > intprg)//keep the update of progress bar to only one thread of the team...avoids locks
						{
							intprg = x * 100 / n0;
							BGWRKR.ReportProgress(intprg + 1);
						}
					}

					for (int y = SOURCE_SEPARATION; y < IMAGEHEIGHT - SOURCE_SEPARATION; y++)
					{
						if (SEARCH_ROI)
							if (!ROI_REGION[x, y])
								continue;

						if (SOURCE_BOOLEAN_MAP[x, y])
							continue;

						double bg_est = 0, sigma = -1;
						if (AUTO_BG)
							bg_est = ESTIMATELOCALBACKGROUND(x, y, SOURCE_SEPARATION, BackGroundEstimateStyle.Corners, ref sigma);

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

						bool brek = false;
						for (int i = x - SOURCE_SEPARATION; i <= x + SOURCE_SEPARATION; i++)
						{
							double sx2 = (double)(x - i);
							sx2 *= sx2;
							for (int j = y - SOURCE_SEPARATION; j <= y + SOURCE_SEPARATION; j++)
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

						//if got to here then must centroid at this pixel
						lock (SOURCE_BOOLEAN_MAP)
						{
							if (SOURCE_BOOLEAN_MAP[(int)x, (int)y])
								continue;
							
							double[,] kernel = JPMath.MatrixSubScalar(GetKernel(IMAGE, (int)x, (int)y, KERNEL_RADIUS, out int[] xdata, out int[] ydata), bg_est, false);
							Centroid(xdata, ydata, kernel, out double x_centroid, out double y_centroid);						

							for (int ii = x - KERNEL_RADIUS; ii <= x + KERNEL_RADIUS; ii++)
							{
								double sx2 = (double)(x - ii);
								sx2 *= sx2;
								for (int jj = y - KERNEL_RADIUS; jj <= y + KERNEL_RADIUS; jj++)
								{
									double sy = (double)(y - jj);
									double r2 = (sx2 + sy * sy);
									if (r2 > KRAD2)
										continue;

									if (ii > 0 && jj > 0 && ii < SOURCE_BOOLEAN_MAP.GetLength(0) && jj < SOURCE_BOOLEAN_MAP.GetLength(1))
									{
										SOURCE_BOOLEAN_MAP[ii, jj] = true;//this kernel radius position has a source detected
										SOURCE_INDEX_MAP[ii, jj] = src_index;//this is the source index of the given detection kernel radius position
									}
								}
							}

							src_index++;
							XPIXs.Add(x);
							YPIXs.Add(y);
							Xs.Add(x_centroid);
							Ys.Add(y_centroid);
							Ks.Add(kernel_psf_sum);
							Ps.Add(pixel);
							Bs.Add(bg_est);

							if (SAVE_PS)
							{
								string file = SAVE_PS_FILENAME;
								int ind = file.LastIndexOf(".");//for saving PS
								file = string.Concat(file.Substring(0, ind), "_", Xs.Count.ToString("00000000"), ".fits");

								FITSImage f = new JPFITS.FITSImage(file, kernel, false, false);
								f.WriteImage(TypeCode.Double, false);
							}
						}
					}
				}));

				if (SHOWWAITBAR)
					if (WAITBAR.DialogResult == DialogResult.Cancel)
						return;

				N_SRC = Xs.Count;
				INITARRAYS();

				double[] sigma = new double[N_SRC];
				double rsq = KERNEL_RADIUS * KERNEL_RADIUS;
				if (rsq == 0)
					rsq = 1;

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

					double sig = 0;
					CENTROIDS_ANULMEDBGEST[i] = ESTIMATELOCALBACKGROUND(CENTROIDS_PIXEL_X[i], CENTROIDS_PIXEL_Y[i], SOURCE_SEPARATION, BackGroundEstimateStyle.SourceSeparationSquareAnnulus, ref sig);
					sigma[i] = sig;
				}
				double mediansigma = JPMath.Median(sigma);
				double bg = rsq * mediansigma * mediansigma;
				for (int i = 0; i < N_SRC; i++)
					CENTROIDS_SNR[i] = CENTROIDS_VOLUME[i] / Math.Sqrt(CENTROIDS_VOLUME[i] + bg);

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

					JPMath.Fit_PointSource(JPMath.PointSourceModel.CircularGaussian, JPMath.FitMinimizationType.ChiSquared, xcoords, ycoords, kernel, ref P0, lb, ub, out _, out _, out chisq_norm, out _);
					FITS_VOLUME[k] = 2 * Math.PI * P0[0] * P0[3] * P0[3];
					FITS_FWHM_X[k] = 2.355 * P0[3];
					FITS_FWHM_Y[k] = FITS_FWHM_X[k];
					for (int i = 0; i < P0.Length; i++)
						FITS_PARAMS[i, k] = P0[i];
				}

				if (Convert.ToInt32(e.Argument) == 3)//Fit elliptical Gaussian
				{
					if (P0 == null)
						P0 = new double[7];

					JPMath.Fit_PointSource(JPMath.PointSourceModel.EllipticalGaussian, JPMath.FitMinimizationType.ChiSquared, xcoords, ycoords, kernel, ref P0, lb, ub, out _, out _, out chisq_norm, out _);
					FITS_VOLUME[k] = 2 * Math.PI * P0[0] * P0[4] * P0[5];
					FITS_FWHM_X[k] = 2.355 * P0[4];
					FITS_FWHM_Y[k] = 2.355 * P0[5];
					FITS_PHI[k] = P0[3];
					for (int i = 0; i < P0.Length; i++)
						FITS_PARAMS[i, k] = P0[i];
				}

				if (Convert.ToInt32(e.Argument) == 4)//Fit circular Moffat
				{
					if (P0 == null)
						P0 = new double[6];

					JPMath.Fit_PointSource(JPMath.PointSourceModel.CircularMoffat, JPMath.FitMinimizationType.ChiSquared, xcoords, ycoords, kernel, ref P0, lb, ub, out _, out _, out chisq_norm, out _);
					FITS_VOLUME[k] = Math.PI * P0[3] * P0[3] * P0[0] / (P0[4] - 1);
					FITS_FWHM_X[k] = 2 * P0[3] * Math.Sqrt(Math.Pow(2, 1 / (P0[4])) - 1);
					FITS_FWHM_Y[k] = FITS_FWHM_X[k];
					for (int i = 0; i < P0.Length; i++)
						FITS_PARAMS[i, k] = P0[i];
				}

				if (Convert.ToInt32(e.Argument) == 5)//Fit elliptical Moffat
				{
					if (P0 == null)
						P0 = new double[8];

					JPMath.Fit_PointSource(JPMath.PointSourceModel.EllipticalMoffat, JPMath.FitMinimizationType.ChiSquared, xcoords, ycoords, kernel, ref P0, lb, ub, out _, out _, out chisq_norm, out _);
					FITS_VOLUME[k] = Math.PI * P0[4] * P0[5] * P0[0] / (P0[6] - 1);
					FITS_FWHM_X[k] = 2 * P0[4] * Math.Sqrt(Math.Pow(2, 1 / (P0[6])) - 1);
					FITS_FWHM_Y[k] = 2 * P0[5] * Math.Sqrt(Math.Pow(2, 1 / (P0[6])) - 1);
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
				PSE_TABLE = new string[11, N_SRC + 1];
			else
				PSE_TABLE = new string[24 + FITS_PARAMS.GetLength(0), N_SRC + 1];

			int c = 0;

			PSE_TABLE[c++, 0] = "PSE X-Centroid";
			PSE_TABLE[c++, 0] = "PSE Y-Centroid";
			PSE_TABLE[c++, 0] = "PSE Amplitude";
			PSE_TABLE[c++, 0] = "PSE Volume";
			PSE_TABLE[c++, 0] = "PSE Background Est";
			PSE_TABLE[c++, 0] = "Annulus Background Est";
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
				PSE_TABLE[c++, i + 1] = CENTROIDS_AMPLITUDE[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_VOLUME[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_AUTOBGEST[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_ANULMEDBGEST[i].ToString();
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
			/// No background estimation is performed - this implies that the images have already been background subtracted.
			/// </summary>
			None,

			/// <summary>
			/// The 2nd-minimum of the 4-corners of the Source Separation box centered on the source pixel. Very fast and reasonably accurate.
			/// </summary>
			Corners,

			/// <summary>
			/// The median of the square periphery annulus of the Source Separation box centered on the source pixel. Very accurate but slower.
			/// </summary>
			SourceSeparationSquareAnnulus
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="HW"></param>
		/// <param name="bgeststyle"></param>
		/// <param name="sigma">pass -1 to not do</param>
		[MethodImpl(256)]
		private double ESTIMATELOCALBACKGROUND(int x, int y, int HW, BackGroundEstimateStyle bgeststyle, ref double sigma)
		{
			if (bgeststyle == BackGroundEstimateStyle.None)
				return 0;

			int xmin = x - HW, xmax = x + HW, ymin = y - HW, ymax = y + HW;

			if (bgeststyle == BackGroundEstimateStyle.Corners)
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
			else //BACKGROUNDESTIMATESTYLE.SourceSeparationSquareAnnulus
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
			return (x >= 0) && (x < IMAGEWIDTH) && (y >= 0) && (y < IMAGEHEIGHT) && (IMAGE[x, y] >= PIX_SAT) && (SOURCE_INDEX_MAP[x, y] == -1);
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
			CENTROIDS_PIXEL_X = new int[N_SRC];
			CENTROIDS_PIXEL_Y = new int[N_SRC];
			CENTROIDS_X = new double[N_SRC];
			CENTROIDS_Y = new double[N_SRC];
			FITS_X = new double[N_SRC];
			FITS_Y = new double[N_SRC];
			FITS_FWHM_X = new double[N_SRC];
			FITS_FWHM_Y = new double[N_SRC];
			FITS_PHI = new double[N_SRC];
			FITS_CHISQNORM = new double[N_SRC];
			CENTROIDS_RADEG = new double[N_SRC];
			CENTROIDS_RAHMS = new string[N_SRC];
			CENTROIDS_DECDEG = new double[N_SRC];
			CENTROIDS_DECDMS = new string[N_SRC];
			CENTROIDS_AMPLITUDE = new double[N_SRC];
			CENTROIDS_VOLUME = new double[N_SRC];
			CENTROIDS_AUTOBGEST = new double[N_SRC];
			CENTROIDS_ANULMEDBGEST = new double[N_SRC];
			CENTROIDS_SNR = new double[N_SRC];
			FITS_AMPLITUDE = new double[N_SRC];
			FITS_VOLUME = new double[N_SRC];
			FITS_BGESTIMATE = new double[N_SRC];
			FITS_RA_DEG = new double[N_SRC];
			FITS_RA_HMS = new string[N_SRC];
			FITS_DEC_DEG = new double[N_SRC];
			FITS_DEC_DMS = new string[N_SRC];
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
				CENTROID_POINTS = new JPMath.PointD[N_SRC];
				for (int i = 0; i < N_SRC; i++)
					CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_VOLUME[i]);
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
				CENTROID_POINTS = new JPMath.PointD[N_SRC];
				for (int i = 0; i < N_SRC; i++)
					CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_VOLUME[i]);
			}
		}

		/// <summary>Gets the volume (total count) of extracted sources.</summary>
		public double[] Centroids_Volume
		{
			get { return CENTROIDS_VOLUME; }
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

		//public double[] 

		public double[] Centroids_Fit_FWHMX
		{
			get { return FITS_FWHM_X; }
		}

		public double[] Centroids_Fit_FWHMY
		{
			get { return FITS_FWHM_Y; }
		}

		public double[] Centroids_Fit_FWHM
		{
			get 
			{
				double[] fwhm = new double[FITS_FWHM_Y.Length];
				for (int i = 0; i < fwhm.Length; i++)
					fwhm[i] = (FITS_FWHM_Y[i] + FITS_FWHM_X[i]) / 2;

				return fwhm;
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

		public string Name
		{
			get { return NAME; }
			set { NAME = value; }
		}

		/// <summary>Returns the boolean source map.</summary>
		public bool[,] SourceBooleanMap
		{
			get { return SOURCE_BOOLEAN_MAP; }
			set { SOURCE_BOOLEAN_MAP = value; }
		}

		/// <summary>Returns the integer index source map.</summary>
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

		/// <summary>Returns the kernel radius value which was passed when performing the source extraction</summary>
		public double KernelRadius
		{
			get { return KERNEL_RADIUS; }
		}

		/// <summary>Returns the source separation value which was passed when performing the source extraction</summary>
		public double SourceSeparation
		{
			get { return SOURCE_SEPARATION; }
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

		public bool PSEParametersSet
		{
			get { return PSEPARAMSSET; }
		}

		public int NGroups
		{
			get { return NGROUPS; }
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
		/// <param name="source_separation">The separation (pixels) between sources. Only the brightest source within the separation radius is kept.</param>
		/// <param name="auto_background">Automatically determine the local background for potential sources. Use None if the image is already background subtracted.</param>
		/// <param name="kernel_filename_template">The template full file name for the kernels to be saved. Sources will be numbered sequentially. Pass empty string for no saving.</param>
		/// <param name="ROI_region">A boolean array of valid area to examine. Pass null or array of equal dimension to source image all true for entire image search.</param>
		/// <param name="show_waitbar">Show a cancellable wait bar. False equates to a syncronous call.</param>
		public void Extract_Sources(double[,] image, double pix_saturation, double pix_min, double pix_max, double kernel_min, double kernel_max, bool threshholds_as_SN, int kernel_radius, int source_separation, bool auto_background, string kernel_filename_template, bool[,]? ROI_region, bool show_waitbar)
		{
			IMAGE = image;
			PIX_SAT = pix_saturation;
			IMAGEWIDTH = IMAGE.GetLength(0);
			IMAGEHEIGHT = IMAGE.GetLength(1);
			KERNEL_RADIUS = kernel_radius;
			KERNEL_WIDTH = KERNEL_RADIUS * 2 + 1;
			SOURCE_SEPARATION = source_separation;
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
			PSEPARAMSSET = true;
			Parallel.For(0, IMAGE.GetLength(0), i =>
			{
				for (int j = 0; j < IMAGE.GetLength(1); j++)
					SOURCE_INDEX_MAP[i, j] = -1;
			});
			FITTED = false;
			THRESHHOLDS_AS_SN = threshholds_as_SN;
			SEARCH_ROI = ROI_region != null;
			ROI_REGION = ROI_region;
			SHOWWAITBAR = show_waitbar;

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

		/// <summary>Determines centroids and other kernel information for known sources at given coordinates.</summary>
		/// <param name="image">The 2D image array containing the known sources to extract.</param>
		/// <param name="XCoords">The x-axis coordinates of the sources.</param>
		/// <param name="YCoords">The y-axis coordinates of the sources.</param>
		/// <param name="kernel_radius">The radius (pixels) of the kernel to centroid.</param>
		/// <param name="auto_background">Automatically determine the local background for potential sources.  Not required if background is known to be zeroed, but should have no effect if used in this case.</param>
		/// <param name="kernel_filename_template">The template full file name for the kernels to be saved. Sources will be numbered sequentially. Pass empty string for no saving.</param>
		public void Extract_Sources(double[,] image, double[] XCoords, double[] YCoords, int kernel_radius, bool auto_background, string kernel_filename_template)
		{
			KERNEL_RADIUS = kernel_radius;
			KERNEL_WIDTH = KERNEL_RADIUS * 2 + 1;
			N_SRC = XCoords.Length;
			INITARRAYS();
			AUTO_BG = auto_background;
			SAVE_PS = kernel_filename_template != "";
			SAVE_PS_FILENAME = kernel_filename_template;
			IMAGE = image;
			FITTED = false;

			Parallel.For(0, N_SRC, i =>
			{
				double[,] kernel = GetKernel(image, (int)XCoords[i], (int)YCoords[i], KERNEL_RADIUS, out int[] xrange, out int[] yrange);

				double sigma = -1;
				double bg_est = ESTIMATELOCALBACKGROUND((int)XCoords[i], (int)YCoords[i], KERNEL_RADIUS, BackGroundEstimateStyle.Corners, ref sigma);
				if (bg_est != 0)
					kernel = JPMath.MatrixAddScalar(kernel, -bg_est, false);

				double xweighted = 0, yweighted = 0;
				for (int x = 0; x < KERNEL_WIDTH; x++)
					for (int y = 0; y < KERNEL_WIDTH; y++)
					{
						xweighted += kernel[x, y] * xrange[x];
						yweighted += kernel[x, y] * yrange[y];
					}

				//centroids
				double kernel_sum = JPMath.Sum(kernel, false);
				CENTROIDS_X[i] = xweighted / kernel_sum;
				CENTROIDS_Y[i] = yweighted / kernel_sum;
				CENTROIDS_VOLUME[i] = kernel_sum;
				CENTROIDS_AMPLITUDE[i] = kernel[KERNEL_RADIUS, KERNEL_RADIUS];
				CENTROIDS_AUTOBGEST[i] = bg_est;
				CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_VOLUME[i]);

				if (SAVE_PS)
				{
					string file = SAVE_PS_FILENAME;
					int ind = file.LastIndexOf(".");//for saving PS
					file = String.Concat(file.Substring(0, ind), "_", (i + 1).ToString("00000000"), ".fits");

					JPFITS.FITSImage f = new JPFITS.FITSImage(file, kernel, false, false);
					f.WriteImage(TypeCode.Double, false);
				}
			});
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

				Extract_Sources(image, pix_saturation, pixthresh, Double.MaxValue, 0, Double.MaxValue, false, kernel_radius, source_separation, auto_background, kernel_filename_template, ROI_region, show_waitbar);

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

			//WCS_GENERATED = true;
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

		public void ClipToNBrightest(int NBright)
		{
			if (NBright >= N_SRC)
				return;

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

				dum = CENTROIDS_SNR[i];
				CENTROIDS_SNR[i] = CENTROIDS_SNR[indices[i]];
				CENTROIDS_SNR[indices[i]] = dum;

				dum = FITS_AMPLITUDE[i];
				FITS_AMPLITUDE[i] = FITS_AMPLITUDE[indices[i]];
				FITS_AMPLITUDE[indices[i]] = dum;

				dum = FITS_VOLUME[i];
				FITS_VOLUME[i] = FITS_VOLUME[indices[i]];
				FITS_VOLUME[indices[i]] = dum;

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
			Array.Resize(ref FITS_AMPLITUDE, NBright);
			Array.Resize(ref FITS_VOLUME, NBright);
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
				xdata = new int[(xw)];
				for (int i = 0; i < xw; i++)
					xdata[i] = i;
			}
			if (ydata == null)
			{
				ydata = new int[(yh)];
				for (int i = 0; i < yh; i++)
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

		/// <summary>Determines the Curve of Growth photometry for a source centered in the ROI image.</summary>
		/// <param name="ROI">The region of interest image to determine the curve of growth for.</param>
		/// <param name="N_last_fit_pts">The number of tailing points to fit for linear slope - intercept of this line is source counts, slope is the background count per pixel.</param>
		/// <param name="N_points_COG">The number of points for each curve of growth point. Used as the abscissa against the return value.</param>
		/// <param name="background_signal_per_pix">The slope of the linear fit line to the tailing points, i.e., the counts per pixel background.</param>
		/// <param name="source_signal">The intercept of the linear fit line to the tailing points, i.e., the total central source counts.</param>
		public static double[] COG(double[,] ROI, int N_last_fit_pts, out double[] N_points_COG, out double background_signal_per_pix, out double source_signal)
		{
			if (ROI.GetLength(0) != ROI.GetLength(1))
			{
				throw new Exception("Error: ROI array must be square.");
			}
			if (ROI.GetLength(0) < 5)
			{
				throw new Exception("Error: Region of interest SubWindow must be at least 5x5 pixels...");
			}
			if (JPMath.IsEven(ROI.GetLength(0)) || JPMath.IsEven(ROI.GetLength(1)))
			{
				throw new Exception("Error: ROI array not odd-size.");
			}

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

		#region CONSTRUCTORS
		/// <summary>The default constructor for the class object, used when an image is to be examined for sources.</summary>
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

		/// <summary>The constructor for the class object used when an image already has a given list of coordinate locations for sources in the image.</summary>
		public PointSourceExtractor(double[] XCoords, double[] YCoords)
		{
			this.BGWRKR = new BackgroundWorker();
			this.BGWRKR.WorkerReportsProgress = true;
			this.BGWRKR.WorkerSupportsCancellation = true;
			this.BGWRKR.DoWork += new DoWorkEventHandler(BGWRKR_DoWork);
			this.BGWRKR.ProgressChanged += new ProgressChangedEventHandler(BGWRKR_ProgressChanged);
			this.BGWRKR.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGWRKR_RunWorkerCompleted);
			WAITBAR = new WaitBar();

			this.N_SRC = XCoords.Length;
			this.INITARRAYS();
			this.Centroids_X = XCoords;
			this.Centroids_Y = YCoords;
			for (int i = 0; i < N_SRC; i++)
				CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], 0);
		}

		/// <summary>The constructor for the class object based on a PointSourceExtractor saved from another session.</summary>
		public PointSourceExtractor(JPFITS.FITSBinTable BinTablePSE)
		{
			this.BGWRKR = new BackgroundWorker();
			this.BGWRKR.WorkerReportsProgress = true;
			this.BGWRKR.WorkerSupportsCancellation = true;
			this.BGWRKR.DoWork += new DoWorkEventHandler(BGWRKR_DoWork);
			this.BGWRKR.ProgressChanged += new ProgressChangedEventHandler(BGWRKR_ProgressChanged);
			this.BGWRKR.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGWRKR_RunWorkerCompleted);
			WAITBAR = new WaitBar();

			PSEPARAMSSET = Convert.ToBoolean(BinTablePSE.GetExtraHeaderKeyValue("PSESET"));
			if (PSEPARAMSSET)
			{
				PIX_SAT = Convert.ToDouble(BinTablePSE.GetExtraHeaderKeyValue("PIXSAT"));
				KERNEL_RADIUS = Convert.ToInt32(BinTablePSE.GetExtraHeaderKeyValue("KERNRAD"));
				KERNEL_WIDTH = KERNEL_RADIUS * 2 + 1;
				SOURCE_SEPARATION = Convert.ToInt32(BinTablePSE.GetExtraHeaderKeyValue("SRCSEP"));
				PIX_MIN = Convert.ToDouble(BinTablePSE.GetExtraHeaderKeyValue("PIXMIN"));
				PIX_MAX = Convert.ToDouble(BinTablePSE.GetExtraHeaderKeyValue("PIXMAX"));
				KERNEL_MIN = Convert.ToDouble(BinTablePSE.GetExtraHeaderKeyValue("KERNMIN"));
				KERNEL_MAX = Convert.ToDouble(BinTablePSE.GetExtraHeaderKeyValue("KERNMAX"));
				//AUTO_BG = Convert.ToBoolean(BinTablePSE.GetExtraHeaderKeyValue("AUTOBG"));
				SEARCH_ROI = Convert.ToBoolean(BinTablePSE.GetExtraHeaderKeyValue("ROIONLY"));
				SAVE_PS = Convert.ToBoolean(BinTablePSE.GetExtraHeaderKeyValue("SAVESRC"));
				//SAVE_PS_FILENAME = kernel_filename_template;
				//SOURCE_BOOLEAN_MAP = new array<bool, 2>(IMAGEWIDTH, IMAGEHEIGHT);
				//SOURCE_INDEX_MAP = new int[,](IMAGEWIDTH, IMAGEHEIGHT);
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
	}
}

