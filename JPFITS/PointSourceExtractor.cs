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
		#region PRIVATE MEMBERS

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
		private bool AUTO_BG;                           //automatic background determination (corner min method)
		private bool SAVE_PS;
		private bool THRESHHOLDS_AS_SN;                 //interpret pixel value and total count thresholds as SN
		private bool SEARCH_ROI;
		private bool[,]? ROI_REGION;
		private bool SHOWWAITBAR;
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
		private double[]? CENTROIDS_RA_DEG;      // right ascension centroid positions of sources - if available
		private string[]? CENTROIDS_RA_HMS;      // right ascension centroid positions of sources - if available
		private double[]? CENTROIDS_DEC_DEG;     // declination centroid positions of sources - if available
		private string[]? CENTROIDS_DEC_DMS;     // declination centroid positions of sources - if available
		private double[]? CENTROIDS_AMPLITUDE;       // sources values (above fcmin)
		private double[]? CENTROIDS_VOLUME;      // sources energies (above fcmin)
		private double[]? CENTROIDS_BGESTIMATE;  // corner minimum - estimate of background

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

		private WaitBar WAITBAR;
		private BackgroundWorker BGWRKR;
		//private object BGWRKR_RESULT;

		private void BGWRKR_DoWork(object sender, DoWorkEventArgs e)
		{
			if (Convert.ToInt32(e.Argument) == 1)//Extract
			{
				ArrayList Xs = new ArrayList();// x positions
				ArrayList Ys = new ArrayList();// y positions
				ArrayList Ks = new ArrayList();// kernel sums
				ArrayList Ps = new ArrayList();// pixel values
				ArrayList Bs = new ArrayList();// background estimates
				double SSEP2 = SOURCE_SEPARATION * SOURCE_SEPARATION, KRAD2 = KERNEL_RADIUS * KERNEL_RADIUS;
				int src_index = 0;

				if (PIX_SAT > 0)//check for saturation islands
				{
					Parallel.For(SOURCE_SEPARATION, IMAGEWIDTH - SOURCE_SEPARATION, x =>
					//for (int x = SOURCE_SEPARATION; x < IMAGEWIDTH - SOURCE_SEPARATION; x++)
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
								if (!SOURCE_BOOLEAN_MAP[x, y] && IMAGE[x, y] >= PIX_SAT)
								{
									int Xmin = x, Xmax = x, Ymin = y, Ymax = y;
									MAPSATURATIONISLAND(x, y, src_index, ref Xmin, ref Xmax, ref Ymin, ref Ymax);

									if (Xmax - Xmin == 0)//single pixel, expand to 3 pixels
									{
										Xmin--;
										Xmax++;
									}
									else if (!JPMath.IsEven(Xmax - Xmin))//544-543 = 1 = 2 pixels, so make odd number of pixels...with Xmax I guess
										Xmax++;
									if (Ymax - Ymin == 0)//single pixel, expand to 3 pixels
									{
										Ymin--;
										Ymax++;
									}
									else if (!JPMath.IsEven(Ymax - Ymin))//544-543 = 1 = 2 pixels, so make odd number of pixels...with Xmax I guess
										Ymax++;

									double[,] kernel = new double[Xmax - Xmin + 1, Ymax - Ymin + 1];
									double x_centroid = 0, y_centroid = 0, kernel_sum = 0;
									for (int i = Xmin; i <= Xmax; i++)
										for (int j = Ymin; j <= Ymax; j++)
										{
											kernel[i - Xmin, j - Ymin] = IMAGE[i, j];
											x_centroid += (IMAGE[i, j] * (double)(i));
											y_centroid += (IMAGE[i, j] * (double)(j));
											kernel_sum += (IMAGE[i, j] /*- bg_est*/);
											/*IMAGE_KERNEL_BOOL_SOURCE[i, j] = true;
											IMAGE_KERNEL_INDEX_SOURCE[i, j] = src_index;*/
										}
									x_centroid /= kernel_sum;
									y_centroid /= kernel_sum;

									//MAPSATURATIONISLAND((int)x_centroid, (int)y_centroid, src_index, Xmin, Xmax, Ymin, Ymax);
									SOURCE_BOOLEAN_MAP[(int)x_centroid, (int)y_centroid] = true;
									SOURCE_INDEX_MAP[(int)x_centroid, (int)y_centroid] = src_index;

									src_index++;
									N_SATURATED++;
									Xs.Add(x_centroid);
									Ys.Add(y_centroid);
									Ks.Add(kernel_sum);
									Ps.Add(IMAGE[x, y]/*pixel*/);
									Bs.Add(0.0/*bg_est*/);

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
						}
					});

					/*JPFITS.FITSImage^ ff = new FITSImage("C:\\Users\\Joseph E Postma\\Desktop\\atest.fits", IMAGE_KERNEL_INDEX_SOURCE, false);
					ff.WriteFile(TypeCode.Int32);*/
				}

				int intprg = 0;
				int n0 = (IMAGEWIDTH - 2 * SOURCE_SEPARATION) / Environment.ProcessorCount + SOURCE_SEPARATION;

				Parallel.For(SOURCE_SEPARATION, IMAGEWIDTH - SOURCE_SEPARATION, (int x, ParallelLoopState state) =>
				//for (int x = SOURCE_SEPARATION; x < IMAGEWIDTH - SOURCE_SEPARATION; x++)
				{
					if (SHOWWAITBAR)
					{
						if (WAITBAR.DialogResult == DialogResult.Cancel)
							state.Stop();

						if (/*(x - SOURCE_SEPARATION) * 100 / (IMAGEWIDTH - 2 * SOURCE_SEPARATION) > intprg*/x < n0 && (x - SOURCE_SEPARATION) * 100 / (n0 - SOURCE_SEPARATION) > intprg)//keep the update of progress bar to only one thread of the team...avoids locks
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

						double bg_est = 0;
						if (AUTO_BG)
							bg_est = ESTIMATELOCALBACKGROUND(x, y, SOURCE_SEPARATION);

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

								if (r2 > 1) //outside the source separation circle
									continue;

								if (IMAGE[i, j] - bg_est > pixel) // max failure, the pixel isn't the maximum in the source separation circle
								{
									brek = true;
									break;
								}

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

						//if got to here then x,y is a possible source depending on total sum of kernel
						double[,] kernel = GetKernel(IMAGE, x, y, KERNEL_RADIUS);
						double kernel_sum = JPMath.Sum(kernel, false) - (double)(KERNEL_WIDTH * KERNEL_WIDTH) * bg_est;//square kernel sum

						/////do PSF kernel sum????????????????????????????????????????????????????????
						/*double kernel_psf_sum = 0, n_psf_pixels = 0;
						for (int i = x - KERNEL_RADIUS; i <= x + KERNEL_RADIUS; i++)
							for (int j = y - KERNEL_RADIUS; j <= y + KERNEL_RADIUS; j++)
							{
								double r2 = double((i - x) * (i - x) + (j - y) * (j - y));
								if (r2 > KRAD2)
									continue;

								kernel_psf_sum += IMAGE[i, j];
								n_psf_pixels++;
							}
						kernel_psf_sum -= (bg_est * n_psf_pixels);*/

						if (!THRESHHOLDS_AS_SN)
						{
							if (kernel_sum < KERNEL_MIN || kernel_sum > KERNEL_MAX)
								continue;
						}
						else//check as S/N
						{
							double Nbg = 0;
							if (bg_est < 1)
								Nbg = Math.Sqrt((double)(KERNEL_WIDTH * KERNEL_WIDTH));
							else
								Nbg = Math.Sqrt(bg_est * (double)(KERNEL_WIDTH * KERNEL_WIDTH));
							double SNenergy = kernel_sum / Nbg;
							if (kernel_sum < KERNEL_MIN || kernel_sum > KERNEL_MAX)
								continue;
						}

						//if got to here then must centroid at this pixel
						double x_centroid, y_centroid;
						int[] xdata = new int[(KERNEL_WIDTH)];
						int[] ydata = new int[(KERNEL_WIDTH)];
						for (int i = -KERNEL_RADIUS; i <= KERNEL_RADIUS; i++)
						{
							xdata[i + KERNEL_RADIUS] = x + i;
							ydata[i + KERNEL_RADIUS] = y + i;
						}
						kernel = JPMath.MatrixSubScalar(kernel, bg_est, false);
						Centroid(xdata, ydata, kernel, out x_centroid, out y_centroid);

						int r_x_cent = (int)Math.Round(x_centroid);
						int r_y_cent = (int)Math.Round(y_centroid);

						lock(SOURCE_BOOLEAN_MAP)
						{
							for (int ii = r_x_cent - KERNEL_RADIUS; ii <= r_x_cent + KERNEL_RADIUS; ii++)
							{
								double sx2 = (double)(r_x_cent - ii);
								sx2 *= sx2;
								for (int jj = r_y_cent - KERNEL_RADIUS; jj <= r_y_cent + KERNEL_RADIUS; jj++)
								{
									double sy = (double)(r_y_cent - jj);
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
							Xs.Add(x_centroid);
							Ys.Add(y_centroid);
							Ks.Add(kernel_sum);
							Ps.Add(pixel);
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

				if (SHOWWAITBAR)
					if (WAITBAR.DialogResult == DialogResult.Cancel)
						return;

				this.N_SRC = Xs.Count;
				this.INITARRAYS();

				for (int i = 0; i < N_SRC; i++)
				{
					CENTROIDS_X[i] = Convert.ToDouble(Xs[i]);
					CENTROIDS_Y[i] = Convert.ToDouble(Ys[i]);
					CENTROIDS_AMPLITUDE[i] = Convert.ToDouble(Ps[i]);
					CENTROIDS_VOLUME[i] = Convert.ToDouble(Ks[i]);
					CENTROIDS_BGESTIMATE[i] = Convert.ToDouble(Bs[i]);
					CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_AMPLITUDE[i]);
				}
				return;
			}
			//returned if after Source Extraction

			double[] empty = new double[(0)];
			if (LBND == null)
				LBND = new double[(0)];
			if (UBND == null)
				UBND = new double[(0)];

			int intprog = 0;
			int np = N_SRC / Environment.ProcessorCount;

			Parallel.For(0, N_SRC, (int k, ParallelLoopState state) =>
			{
				if (WAITBAR.DialogResult == DialogResult.Cancel)
				{
					state.Stop();// break;
				}

				if (k < np && k * 100 / np > intprog)//keep the update of progress bar to only one thread of the team...avoids locks
				{
					intprog = k * 100 / np;
					BGWRKR.ReportProgress(intprog);
				}

				double[,] kernel = GetKernel(IMAGE, (int)(CENTROIDS_X[k] + .5), (int)(CENTROIDS_Y[k] + .5), KERNEL_RADIUS);
				int[] xcoords = new int[(KERNEL_WIDTH)];
				int[] ycoords = new int[(KERNEL_WIDTH)];
				for (int i = 0; i < KERNEL_WIDTH; i++)
				{
					xcoords[i] = (int)(CENTROIDS_X[k] + .5) - KERNEL_RADIUS + i;
					ycoords[i] = (int)(CENTROIDS_Y[k] + .5) - KERNEL_RADIUS + i;
				}

				double[,] fit_resid = new double[KERNEL_WIDTH, KERNEL_WIDTH];
				double[] P0 = null;
				double[] lb = new double[(LBND.Length)];
				double[] ub = new double[(UBND.Length)];
				if (LBND.Length > 0)//set bounds to make sense
				{
					lb[0] = 0;
					lb[1] = CENTROIDS_X[k] - KERNEL_RADIUS;
					lb[2] = CENTROIDS_Y[k] - KERNEL_RADIUS;
					for (int i = 3; i < LBND.Length; i++)
						lb[i] = LBND[i];
				}
				if (UBND.Length > 0)//set bounds to make sense
				{
					ub[0] = Math.Abs(CENTROIDS_AMPLITUDE[k] * 2);
					ub[1] = CENTROIDS_X[k] + KERNEL_RADIUS;
					ub[2] = CENTROIDS_Y[k] + KERNEL_RADIUS;
					for (int i = 3; i < UBND.Length; i++)
						ub[i] = UBND[i];
				}

				if (Convert.ToInt32(e.Argument) == 2)//Fit circular Gaussian
				{
					if (PINI != null)
						P0 = new double[5] { Math.Abs(CENTROIDS_AMPLITUDE[k]), CENTROIDS_X[k], CENTROIDS_Y[k], PINI[3], PINI[4] };
					else
						P0 = new double[5] { Math.Abs(CENTROIDS_AMPLITUDE[k]), CENTROIDS_X[k], CENTROIDS_Y[k], 2, CENTROIDS_BGESTIMATE[k] };

					JPMath.Fit_Gaussian2d(xcoords, ycoords, kernel, ref P0, lb, ub, ref empty, ref fit_resid);
					FITS_VOLUME[k] = 2 * Math.PI * P0[0] * P0[3] * P0[3];
					FITS_FWHM_X[k] = 2.355 * P0[3];
					FITS_FWHM_Y[k] = FITS_FWHM_X[k];
					for (int i = 0; i < P0.Length; i++)
						FITS_PARAMS[i, k] = P0[i];
				}

				if (Convert.ToInt32(e.Argument) == 3)//Fit elliptical Gaussian
				{
					if (PINI != null)
						P0 = new double[7] { Math.Abs(CENTROIDS_AMPLITUDE[k]), CENTROIDS_X[k], CENTROIDS_Y[k], PINI[3], PINI[4], PINI[5], PINI[6] };
					else
						P0 = new double[7] { Math.Abs(CENTROIDS_AMPLITUDE[k]), CENTROIDS_X[k], CENTROIDS_Y[k], 0, 2, 2, CENTROIDS_BGESTIMATE[k] };

					JPMath.Fit_Gaussian2d(xcoords, ycoords, kernel, ref P0, lb, ub, ref empty, ref fit_resid);
					FITS_VOLUME[k] = 2 * Math.PI * P0[0] * P0[4] * P0[5];
					FITS_FWHM_X[k] = 2.355 * P0[4];
					FITS_FWHM_Y[k] = 2.355 * P0[5];
					FITS_PHI[k] = P0[3];
					for (int i = 0; i < P0.Length; i++)
						FITS_PARAMS[i, k] = P0[i];
				}

				if (Convert.ToInt32(e.Argument) == 4)//Fit circular Moffat
				{
					if (PINI != null)
						P0 = new double[6] { Math.Abs(CENTROIDS_AMPLITUDE[k]), CENTROIDS_X[k], CENTROIDS_Y[k], PINI[3], PINI[4], PINI[5] };
					else
						P0 = new double[6] { Math.Abs(CENTROIDS_AMPLITUDE[k]), CENTROIDS_X[k], CENTROIDS_Y[k], 2, 2, CENTROIDS_BGESTIMATE[k] };

					JPMath.Fit_Moffat2d(xcoords, ycoords, kernel, ref P0, lb, ub, ref empty, ref fit_resid);
					FITS_VOLUME[k] = Math.PI * P0[3] * P0[3] * P0[0] / (P0[4] - 1);
					FITS_FWHM_X[k] = 2 * P0[3] * Math.Sqrt(Math.Pow(2, 1 / (P0[4])) - 1);
					FITS_FWHM_Y[k] = FITS_FWHM_X[k];
					for (int i = 0; i < P0.Length; i++)
						FITS_PARAMS[i, k] = P0[i];
				}

				if (Convert.ToInt32(e.Argument) == 5)//Fit elliptical Moffat
				{
					if (PINI != null)
						P0 = new double[(8)] { Math.Abs(CENTROIDS_AMPLITUDE[k]), CENTROIDS_X[k], CENTROIDS_Y[k], PINI[3], PINI[4], PINI[5], PINI[6], PINI[7] };
					else
						P0 = new double[(8)] { Math.Abs(CENTROIDS_AMPLITUDE[k]), CENTROIDS_X[k], CENTROIDS_Y[k], 0, 2, 2, 2, CENTROIDS_BGESTIMATE[k] };

					JPMath.Fit_Moffat2d(xcoords, ycoords, kernel, ref P0, lb, ub, ref empty, ref fit_resid);
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

				double chisq_norm = 0;
				for (int i = 0; i < KERNEL_WIDTH; i++)
					for (int j = 0; j < KERNEL_WIDTH; j++)
					{
						if (kernel[i, j] - P0[P0.Length - 1] == 0)
							chisq_norm += fit_resid[i, j] * fit_resid[i, j];
						else
							chisq_norm += fit_resid[i, j] * fit_resid[i, j] / Math.Abs(kernel[i, j] - P0[P0.Length - 1]);
					}
				chisq_norm /= (kernel.Length - P0.Length);
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
				PSE_TABLE = new string[9, N_SRC + 1];
			else
				PSE_TABLE = new string[22 + FITS_PARAMS.GetLength(0), N_SRC + 1];

			int c = 0;

			PSE_TABLE[c++, 0] = "PSE X-Centroid";
			PSE_TABLE[c++, 0] = "PSE Y-Centroid";
			PSE_TABLE[c++, 0] = "PSE Amplitude";
			PSE_TABLE[c++, 0] = "PSE Volume";
			PSE_TABLE[c++, 0] = "PSE Background";
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
				PSE_TABLE[c++, i + 1] = CENTROIDS_BGESTIMATE[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_RA_DEG[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_DEC_DEG[i].ToString();
				PSE_TABLE[c++, i + 1] = CENTROIDS_RA_HMS[i];
				PSE_TABLE[c++, i + 1] = CENTROIDS_DEC_DMS[i];

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

		[MethodImpl(256)]
		private double ESTIMATELOCALBACKGROUND(int x, int y, int HW)
		{
			int xmin = x - HW, xmax = x + HW, ymin = y - HW, ymax = y + HW;
			double[] corners = new double[4] { IMAGE[xmin, ymin], IMAGE[xmin, ymax], IMAGE[xmax, ymin], IMAGE[xmax, ymax] };

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
			return (x >= 0) && (x < IMAGEWIDTH) && (y >= 0) && (y < IMAGEHEIGHT) && (IMAGE[x, y] > PIX_SAT) && /*!SOURCE_BOOLEAN_MAP[x, y]*/ (SOURCE_INDEX_MAP[x, y] == -1);
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
			CENTROIDS_X = new double[(N_SRC)];
			CENTROIDS_Y = new double[(N_SRC)];
			FITS_X = new double[(N_SRC)];
			FITS_Y = new double[(N_SRC)];
			FITS_FWHM_X = new double[(N_SRC)];
			FITS_FWHM_Y = new double[(N_SRC)];
			FITS_PHI = new double[(N_SRC)];
			FITS_CHISQNORM = new double[(N_SRC)];
			CENTROIDS_RA_DEG = new double[(N_SRC)];
			CENTROIDS_RA_HMS = new string[(N_SRC)];
			CENTROIDS_DEC_DEG = new double[(N_SRC)];
			CENTROIDS_DEC_DMS = new string[(N_SRC)];
			CENTROIDS_AMPLITUDE = new double[(N_SRC)];
			CENTROIDS_VOLUME = new double[(N_SRC)];
			CENTROIDS_BGESTIMATE = new double[(N_SRC)];
			FITS_AMPLITUDE = new double[(N_SRC)];
			FITS_VOLUME = new double[(N_SRC)];
			FITS_BGESTIMATE = new double[(N_SRC)];
			FITS_RA_DEG = new double[(N_SRC)];
			FITS_RA_HMS = new string[(N_SRC)];
			FITS_DEC_DEG = new double[(N_SRC)];
			FITS_DEC_DMS = new string[(N_SRC)];
			CENTROID_POINTS = new JPMath.PointD[(N_SRC)];
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

		/// <summary>Gets or Sets the x-axis centroids of extracted sources.</summary>
		public double[] Centroids_X
		{
			get { return CENTROIDS_X; }
			set
			{
				CENTROIDS_X = value;
				N_SRC = CENTROIDS_X.Length;
				CENTROID_POINTS = new JPMath.PointD[(N_SRC)];
				for (int i = 0; i < N_SRC; i++)
					CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_AMPLITUDE[i]);
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
				CENTROID_POINTS = new JPMath.PointD[(N_SRC)];
				for (int i = 0; i < N_SRC; i++)
					CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_AMPLITUDE[i]);
			}
		}

		public double[] Centroids_Volume
		{
			get { return CENTROIDS_VOLUME; }
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

		public double PixelSaturation
		{
			get { return PIX_SAT; }
		}

		public double KernelRadius
		{
			get { return KERNEL_RADIUS; }
		}

		public double SourceSeparation
		{
			get { return SOURCE_SEPARATION; }
		}

		public double PixelMaximum
		{
			get { return PIX_MAX; }
		}

		public double PixelMinimum
		{
			get { return PIX_MIN; }
		}

		public double KernelMaximum
		{
			get { return KERNEL_MAX; }
		}

		public double KernelMinimum
		{
			get { return KERNEL_MIN; }
		}

		public bool AutoBackground
		{
			get { return AUTO_BG; }
		}

		public bool SavePointSources
		{
			get { return SAVE_PS; }
		}

		/*property string SavePointSourcesFileNameTemplate
		{
			string get() { return SAVE_PS_FILENAME; }
		}*/

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
		/// <param name="auto_background">Automatically determine the local background for potential sources.  Not required if background is known to be zeroed, but should have no effect if used in this case.</param>
		/// <param name="kernel_filename_template">The template full file name for the kernels to be saved. Sources will be numbered sequentially. Pass empty string for no saving.</param>
		/// <param name="ROI_region">A boolean array of valid area to examine. Pass null or array of equal dimension to source image all true for entire image search.</param>
		/// <param name="show_waitbar">Show a cancellable wait bar.</param>
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
			//IMAGE_KERNEL_BOOL_SOURCE = new array<bool, 2>(IMAGE.GetLength(0), IMAGE.GetLength(1));
			FITTED = false;

			int HW = KERNEL_RADIUS;

			Parallel.For(0, N_SRC, i =>
			{
				double[,] kernel = GetKernel(image, (int)XCoords[i], (int)YCoords[i], KERNEL_RADIUS);
				/*int xmax, ymax;
				JPMath.Max(kernel, xmax, ymax);
				if (xmax != KERNEL_RADIUS || ymax != KERNEL_RADIUS)
				{
					XCoords[i] += double(xmax - KERNEL_RADIUS);
					YCoords[i] += double(ymax - KERNEL_RADIUS);
					kernel = GetKernel(image, (int)XCoords[i], (int)YCoords[i], KERNEL_RADIUS);
				}*/

				double bg_est = 0;//default
				if (AUTO_BG)
				{
					bg_est = ESTIMATELOCALBACKGROUND((int)XCoords[i], (int)YCoords[i], HW);
					kernel = JPMath.MatrixAddScalar(kernel, -bg_est, false);
				}

				double[] xcoords = new double[(KERNEL_WIDTH)];// x coords
				double[] ycoords = new double[(KERNEL_WIDTH)];// y coords
				for (int j = 0; j < KERNEL_WIDTH; j++)
				{
					xcoords[j] = (double)((int)XCoords[i] - HW + j);
					ycoords[j] = (double)((int)YCoords[i] - HW + j);
				}

				double xweighted = 0, yweighted = 0;
				for (int x = 0; x < KERNEL_WIDTH; x++)
					for (int y = 0; y < KERNEL_WIDTH; y++)
					{
						xweighted += kernel[x, y] * xcoords[x];
						yweighted += kernel[x, y] * ycoords[y];
					}

				//centroids
				double kernel_sum = JPMath.Sum(kernel, false);
				CENTROIDS_X[i] = xweighted / kernel_sum;
				CENTROIDS_Y[i] = yweighted / kernel_sum;
				CENTROIDS_VOLUME[i] = kernel_sum;
				CENTROIDS_AMPLITUDE[i] = kernel[KERNEL_RADIUS, KERNEL_RADIUS];
				CENTROIDS_BGESTIMATE[i] = bg_est;
				CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_AMPLITUDE[i]);

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

		public void Extract_Attempt_N_Sources(int N, double[,] image, double pix_saturation, double pix_min, double pix_max, double kernel_min, double kernel_max, bool threshholds_as_SN, int kernel_radius, int source_separation, bool auto_background, string kernel_filename_template, bool[,]? ROI_region, bool show_waitbar)
		{
			//JPFITS.FITSImage^ FITS = new FITSImage("", image, true, true);

			double immax = JPMath.Max(image, true);
			double pixthresh = immax / 16;
			double div = 2;
			double amp = pixthresh;
			int PSEiters = 0;
			int maxPSEiters = 20;
			int nPSEpts_min = N;
			int nPSEpts_max = N + 1;
			while (this.N_Sources < nPSEpts_min || this.N_Sources > nPSEpts_max)
			{
				PSEiters++;
				if (PSEiters > maxPSEiters)
					break;

				if (this.N_SaturatedSources >= nPSEpts_min)
					break;

				if (this.N_Sources >= nPSEpts_min)
					break;

				Extract_Sources(image, pix_saturation, pixthresh, pix_max, kernel_min, kernel_max, threshholds_as_SN, kernel_radius, source_separation, auto_background, kernel_filename_template, ROI_region, show_waitbar);

				if (this.N_Sources < nPSEpts_min)
					pixthresh -= amp / div;
				if (this.N_Sources > nPSEpts_max)
					pixthresh += amp / div;
				div *= 2;

				if (pixthresh < pix_min)
				{
					Extract_Sources(image, pix_saturation, pix_min, pix_max, kernel_min, kernel_max, threshholds_as_SN, kernel_radius, source_separation, auto_background, kernel_filename_template, ROI_region, show_waitbar);
					break;
				}
			}
			if (this.N_Sources > nPSEpts_min)
				this.ClipToNBrightest(nPSEpts_min);
		}

		/// <summary>Performs a least-squares fit on all sources of the form:
		/// <para>G(x,y|P) = P(0) * exp( -((x - P(1)).^2 + (y - P(2)).^2 ) / (2*P(3)^2)) + P(4).</para></summary>
		public void Extract_Source_LSFits_Gaussian_Circular(double[] Pinit, double[] LBnds, double[] UBnds/*bool view,*/)//2-D Circular Gaussian
		{
			//G = P(0) * exp( -((X-P(1)).^2 + (Y-P(2)).^2 ) / (2*P(3)^2)) + P(4);

			FITS_PARAMS = new double[5, N_SRC];
			FITTED = true;
			FIT_EQUATION = "Gaussian (Circular): P(0) * exp( -((X-P(1)).^2 + (Y-P(2)).^2 ) / (2*P(3)^2)) + P(4)";
			//VIEWFITS = view;
			//P_INIT = P0;
			LBND = LBnds;
			UBND = UBnds;
			PINI = Pinit;

			WAITBAR = new WaitBar();
			WAITBAR.ProgressBar.Maximum = 100;
			WAITBAR.Text = "Fitting Sources...";
			BGWRKR.RunWorkerAsync(2);
			WAITBAR.ShowDialog();

			if (WAITBAR.DialogResult == DialogResult.Cancel)
				FITTED = false;
		}

		/// <summary>Performs a least-squares fit on all sources of the form:
		/// <para>G(x,y|P) = P(0) * exp( -((x - P(1))*cosd(P(3)) + (y - P(2))*sind(P(3))).^2 / (2*P(4)^2) - ( -(x - P(1))*sind(P(3)) + (y - P(2))*cosd(P(3))).^2 / (2*P(5)^2) ) + P(6).</para></summary>
		public void Extract_Source_LSFits_Gaussian_Elliptical(double[] Pinit, double[] LBnds, double[] UBnds/*bool view,*/)// 2-D Elliptical Gaussian
		{
			//G = P(0) * exp( -((x-P(1))*cosd(P(3)) + (y-P(2))*sind(P(3))).^2 / (2*P(4)^2) - ( -(x-P(1))*sind(P(3)) + (y-P(2))*cosd(P(3))).^2 / (2*P(5)^2) ) + P(6);

			FITS_PARAMS = new double[7, N_SRC];
			FITTED = true;
			FIT_EQUATION = "Gaussian (Elliptical): P(0) * exp( -((x-P(1))*cos(P(3)) + (y-P(2))*sin(P(3))).^2 / (2*P(4)^2) - ( -(x-P(1))*sin(P(3)) + (y-P(2))*cos(P(3))).^2 / (2*P(5)^2) ) + P(6)";
			/*VIEWFITS = view;
			P_INIT = P0;*/
			LBND = LBnds;
			UBND = UBnds;
			PINI = Pinit;

			WAITBAR = new WaitBar();
			WAITBAR.ProgressBar.Maximum = 100;
			WAITBAR.Text = "Fitting Sources...";
			BGWRKR.RunWorkerAsync(3);
			WAITBAR.ShowDialog();

			if (WAITBAR.DialogResult == DialogResult.Cancel)
				FITTED = false;
		}

		/// <summary>Performs a least-squares fit on all sources of the form:
		/// <para>M(x,y|P) = P(0) * ( 1 + { (x - P(1))^2 + (y - P(2))^2 } / P(3)^2 ) ^ (-P(4)) + P(5).</para></summary>
		public void Extract_Source_LSFits_Moffat_Circular(double[] Pinit, double[] LBnds, double[] UBnds/*bool view,*/)// 2-D Circular Moffat
		{
			// M = P(0) * ( 1 + { (X-P(1))^2 + (Y-P(2))^2 } / P(3)^2 ) ^ (-P(4)) + P(5)

			FITS_PARAMS = new double[6, N_SRC];
			FITTED = true;
			FIT_EQUATION = "Moffat (Circular): P(0) * ( 1 + { (X-P(1))^2 + (Y-P(2))^2 } / P(3)^2 ) ^ (-P(4)) + P(5)";
			/*VIEWFITS = view;
			P_INIT = P0;*/
			LBND = LBnds;
			UBND = UBnds;
			PINI = Pinit;

			WAITBAR = new WaitBar();
			WAITBAR.ProgressBar.Maximum = 100;
			WAITBAR.Text = "Fitting Sources...";
			BGWRKR.RunWorkerAsync(4);
			WAITBAR.ShowDialog();

			if (WAITBAR.DialogResult == DialogResult.Cancel)
				FITTED = false;
		}

		/// <summary>Performs a least-squares fit on all sources of the form:
		/// <para>M(x,y|P) = P(0) * (1 + { ((x - P(1))*cosd(P(3)) + (y - P(2))*sind(P(3))) ^ 2 } / P(4) ^ 2 + { (-(x - P(1))*sind(P(3)) + (y - P(2))*cosd(P(3))) ^ 2 } / P(5) ^ 2) ^ (-P(6)) + P(7).</para></summary>
		public void Extract_Source_LSFits_Moffat_Elliptical(double[] Pinit, double[] LBnds, double[] UBnds/*bool view,*/)// 2-D Elliptical Moffat
		{
			//M = P(0) * ( 1 + { ((X-P(1))*cosd(P(3)) + (Y-P(2))*sind(P(3)))^2 } / P(4)^2 + { (-(X-P(1))*sind(P(3)) + (Y-P(2))*cosd(P(3)))^2 } / P(5)^2 ) ^ (-P(6)) + P(7);

			FITS_PARAMS = new double[8, N_SRC];
			FITTED = true;
			FIT_EQUATION = "Moffat (Elliptical): P(0) * ( 1 + { ((X-P(1))*cos(P(3)) + (Y-P(2))*sin(P(3)))^2 } / P(4)^2 + { (-(X-P(1))*sin(P(3)) + (Y-P(2))*cos(P(3)))^2 } / P(5)^2 ) ^ (-P(6)) + P(7)";
			/*VIEWFITS = view;
			P_INIT = P0;*/
			LBND = LBnds;
			UBND = UBnds;
			PINI = Pinit;

			WAITBAR = new WaitBar();
			WAITBAR.ProgressBar.Maximum = 100;
			WAITBAR.Text = "Fitting Sources...";
			BGWRKR.RunWorkerAsync(5);
			WAITBAR.ShowDialog();

			if (WAITBAR.DialogResult == DialogResult.Cancel)
				FITTED = false;
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

		/// <summary>Generates RA and Dec coordinates for the sources in this instance, using the supplied World Coordinate System instance.</summary>
		/// <param name="wcs">The world coordinate system to use for converting image pixel locations to world coordinates.</param>
		public void Generate_Source_RADec_Coords(JPFITS.WorldCoordinateSolution wcs)
		{
			for (int i = 0; i < N_SRC; i++)
			{
				double a, d;
				string RAhms;
				string DECdamas;

				wcs.Get_Coordinate(CENTROIDS_X[i], CENTROIDS_Y[i], true, "TAN", out a, out d, out RAhms, out DECdamas);

				CENTROIDS_RA_DEG[i] = a;
				CENTROIDS_RA_HMS[i] = RAhms;
				CENTROIDS_DEC_DEG[i] = d;
				CENTROIDS_DEC_DMS[i] = DECdamas;

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

		public void ClipToNBrightest(int NBright)
		{
			if (NBright >= N_SRC)
				return;

			double[] volkey = new double[N_SRC];
			Array.Copy(CENTROIDS_VOLUME, volkey, N_SRC);

			int[] indices = new int[N_SRC];
			for (int i = 0; i < N_SRC; i++)
			{
				indices[i] = i; //IMAGE_KERNEL_INDEX_SOURCE[(int)CENTROIDS_X[i], (int)CENTROIDS_Y[i]];
				if (indices[i] == -1)
					MessageBox.Show("-1 a " + CENTROIDS_X[i] + " " + Math.Round(CENTROIDS_X[i]) + " " + CENTROIDS_Y[i] + " " + Math.Round(CENTROIDS_Y[i]));
			}

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

				REMAP((int)(CENTROIDS_X[i]), (int)(CENTROIDS_Y[i]), indices[i], i);
				REMAP((int)(CENTROIDS_X[indices[i]]), (int)(CENTROIDS_Y[indices[i]]), i, indices[i]);

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

				dum = CENTROIDS_RA_DEG[i];
				CENTROIDS_RA_DEG[i] = CENTROIDS_RA_DEG[indices[i]];
				CENTROIDS_RA_DEG[indices[i]] = dum;

				dumstr = CENTROIDS_RA_HMS[i];
				CENTROIDS_RA_HMS[i] = CENTROIDS_RA_HMS[indices[i]];
				CENTROIDS_RA_HMS[indices[i]] = dumstr;

				dum = CENTROIDS_DEC_DEG[i];
				CENTROIDS_DEC_DEG[i] = CENTROIDS_DEC_DEG[indices[i]];
				CENTROIDS_DEC_DEG[indices[i]] = dum;

				dumstr = CENTROIDS_DEC_DMS[i];
				CENTROIDS_DEC_DMS[i] = CENTROIDS_DEC_DMS[indices[i]];
				CENTROIDS_DEC_DMS[indices[i]] = dumstr;

				dum = CENTROIDS_AMPLITUDE[i];
				CENTROIDS_AMPLITUDE[i] = CENTROIDS_AMPLITUDE[indices[i]];
				CENTROIDS_AMPLITUDE[indices[i]] = dum;

				dum = CENTROIDS_BGESTIMATE[i];
				CENTROIDS_BGESTIMATE[i] = CENTROIDS_BGESTIMATE[indices[i]];
				CENTROIDS_BGESTIMATE[indices[i]] = dum;

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
				DEMAP((int)CENTROIDS_X[i], (int)CENTROIDS_Y[i], SOURCE_INDEX_MAP[(int)CENTROIDS_X[i], (int)CENTROIDS_Y[i]]);

			Array.Resize(ref CENTROIDS_X, NBright);
			Array.Resize(ref CENTROIDS_Y, NBright);
			Array.Resize(ref FITS_X, NBright);
			Array.Resize(ref FITS_Y, NBright);
			Array.Resize(ref FITS_FWHM_X, NBright);
			Array.Resize(ref FITS_FWHM_Y, NBright);
			Array.Resize(ref FITS_PHI, NBright);
			Array.Resize(ref FITS_CHISQNORM, NBright);
			Array.Resize(ref CENTROIDS_RA_DEG, NBright);
			Array.Resize(ref CENTROIDS_RA_HMS, NBright);
			Array.Resize(ref CENTROIDS_DEC_DEG, NBright);
			Array.Resize(ref CENTROIDS_DEC_DMS, NBright);
			Array.Resize(ref CENTROIDS_AMPLITUDE, NBright);
			Array.Resize(ref CENTROIDS_VOLUME, NBright);
			Array.Resize(ref CENTROIDS_BGESTIMATE, NBright);
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
				CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_AMPLITUDE[i]);

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
			int rem;

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
							Math.DivRem(i, colors.Length, out rem);
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

		/// <summary>Gets a sub-array kernel from a primary image given a center position and square half-width radius.</summary>
		public static double[,] GetKernel(double[,] image, int x0, int y0, int radius)
		{
			int width = radius * 2 + 1, kx = -1, x, y, xmin = x0 - radius, ymin = y0 - radius;
			double[,] kernel = new double[width, width];

			for (x = 0; x < width; x++)
			{
				kx = xmin + x;
				for (y = 0; y < width; y++)
					kernel[x, y] = image[kx, ymin + y];
			}

			return kernel;
		}

		/// <summary>Determines the [x, y] centroid location of a given kernel.</summary>
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
			double[] param;
			JPMath.Fit_Poly1d(xdata, ydata, 1, true, out param);

			source_signal = param[0];
			background_signal_per_pix = param[1];

			return cog;
		}

		#endregion


		#region CONSTRUCTORS
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
				AUTO_BG = Convert.ToBoolean(BinTablePSE.GetExtraHeaderKeyValue("AUTOBG"));
				SEARCH_ROI = Convert.ToBoolean(BinTablePSE.GetExtraHeaderKeyValue("ROIONLY"));
				SAVE_PS = Convert.ToBoolean(BinTablePSE.GetExtraHeaderKeyValue("SAVESRC"));
				//SAVE_PS_FILENAME = kernel_filename_template;
				//SOURCE_BOOLEAN_MAP = new array<bool, 2>(IMAGEWIDTH, IMAGEHEIGHT);
				//SOURCE_INDEX_MAP = new int[,](IMAGEWIDTH, IMAGEHEIGHT);
			}
			N_SRC = BinTablePSE.Naxis2;
			CENTROIDS_X = BinTablePSE.GetTTYPEEntry("PSE X-Centroid");
			CENTROIDS_Y = BinTablePSE.GetTTYPEEntry("PSE Y-Centroid");
			CENTROIDS_AMPLITUDE = BinTablePSE.GetTTYPEEntry("PSE Amplitude");
			CENTROIDS_VOLUME = BinTablePSE.GetTTYPEEntry("PSE Volume");
			CENTROIDS_BGESTIMATE = BinTablePSE.GetTTYPEEntry("PSE Background");

			CENTROID_POINTS = new JPMath.PointD[N_SRC];
			for (int i = 0; i < N_SRC; i++)
				CENTROID_POINTS[i] = new JPMath.PointD(CENTROIDS_X[i], CENTROIDS_Y[i], CENTROIDS_AMPLITUDE[i]);

			if (BinTablePSE.TTYPEEntryExists("PSE RA (deg)"))
				CENTROIDS_RA_DEG = BinTablePSE.GetTTYPEEntry("PSE RA (deg)");
			if (BinTablePSE.TTYPEEntryExists("PSE Dec (deg)"))
				CENTROIDS_DEC_DEG = BinTablePSE.GetTTYPEEntry("PSE Dec (deg)");
			if (BinTablePSE.TTYPEEntryExists("PSE RA (sxgsml)"))
			{
				TypeCode t;
				int[] d;
				CENTROIDS_RA_HMS = (string[])BinTablePSE.GetTTYPEEntry("PSE RA (sxgsml)", out t, out d);
			}
			if (BinTablePSE.TTYPEEntryExists("PSE Dec (sxgsml)"))
			{
				TypeCode t;
				int[] d;
				CENTROIDS_DEC_DMS = (string[])BinTablePSE.GetTTYPEEntry("PSE Dec (sxgsml)", out t, out d);
			}
		}

		#endregion
	}
}
