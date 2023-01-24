using System;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections.Concurrent;
#nullable enable

namespace JPFITS
{
	/// <summary>WCS_AutoSolver class provides functionality for automatically solving astrometric solutions for FITS image data.</summary>
	public class WCSAutoSolver
	{
		#region PRIVATE MEMBERS

		private bool SOLVED = false, CANCELLED = true, DO_PSE = false, DO_PARALLEL = true, SOLVING = false, CONDITION_ARRAYS = false, SHOW_REPORT_FORM = false;
		private string STATUS_LOG = "";
		private bool[,]? IMAGE_ROI;
		private string? CAT_FILENAME;
		private string? CAT_EXTNAME;
		private string? CAT_CVAL1NAME;
		private string? CAT_CVAL2NAME;
		private string? CAT_MAGNAME;
		private FITSImage? FITS_IMG;
		//FITSImage FITS_CAT;
		private double[]? CAT_CVAL1s;
		private double[]? CAT_CVAL2s;
		private double[]? CAT_MAGs;
		private JPMath.PointD[]? PSE_PTS;
		private JPMath.PointD[]? CAT_PTS;
		private int PROGRESS, IMAGE_WIDTH, IMAGE_HEIGHT, N_POINTS, PSE_KERNEL_RADIUS, PSE_SEP_RADIUS, N_MATCHES_STOP, PERC_MATCHES_STOP;
		private double SCALE_INIT, SCALE_LB, SCALE_UB, ROTATION_INIT, ROTATION_LB, ROTATION_UB, WCS_VERTEX_TOL, PIX_SAT;
		//ulong NCOMPARES;
		private WorldCoordinateSolution? WCS;
		private PointSourceExtractor? PSE;
		private DateTime DATE;
		private string? WCS_TYPE;
		private bool ZERO_BASED_PIX;
		private bool AUTO_BACKGROUND;
		private bool REFINE = false;
		//WaitBar WAITBAR;
		BackgroundWorker BGWRKR;
		private WCSAutoSolverReportingForm? WCSARF;
		double DIV;
		int NITERS, MAXITERS;
		double IMMAX, IMMED, IMAMP, PIXTHRESH;

		private void BGWRKR_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
		{
			if (DO_PSE)
			{
				//get catalogue RA, Dec, and mag's
				BGWRKR.ReportProgress(0, "Reading the Catalogue FITS binary tables...");

				FITSBinTable bt = new JPFITS.FITSBinTable(CAT_FILENAME, CAT_EXTNAME);
				CAT_CVAL1s = (double[])(double[])bt.GetTTYPEEntry(CAT_CVAL1NAME, out _, out _, FITSBinTable.TTYPEReturn.AsDouble);
				CAT_CVAL2s = (double[])(double[])bt.GetTTYPEEntry(CAT_CVAL2NAME, out _, out _, FITSBinTable.TTYPEReturn.AsDouble);
				CAT_MAGs = (double[])(double[])bt.GetTTYPEEntry(CAT_MAGNAME, out _, out _, FITSBinTable.TTYPEReturn.AsDouble);

				//need to check mag for NaN's and re-form ra dec mag
				BGWRKR.ReportProgress(0, "Formatting the Catalogue FITS binary tables...");
				int catcnt = 0;
				for (int i = 0; i < CAT_CVAL1s.Length; i++)
				{
					if (Double.IsNaN(CAT_MAGs[i]))
						continue;

					CAT_CVAL1s[catcnt] = CAT_CVAL1s[i];
					CAT_CVAL2s[catcnt] = CAT_CVAL2s[i];
					CAT_MAGs[catcnt] = CAT_MAGs[i];
					catcnt++;
				}
				Array.Resize(ref CAT_CVAL1s, catcnt);
				Array.Resize(ref CAT_CVAL2s, catcnt);
				Array.Resize(ref CAT_MAGs, catcnt);

				//sort the catalogue list by magnitude
				BGWRKR.ReportProgress(0, "Sorting the Catalogue FITS binary tables...");
				double[] keysref = new double[(CAT_MAGs.Length)];
				Array.Copy(CAT_MAGs, keysref, CAT_MAGs.Length);
				Array.Sort(CAT_MAGs, CAT_CVAL1s);
				Array.Copy(keysref, CAT_MAGs, CAT_MAGs.Length);
				Array.Sort(CAT_MAGs, CAT_CVAL2s);

				//get the brightest few catlaogue points
				BGWRKR.ReportProgress(0, "Making Catalogue points...");
				CAT_PTS = new JPMath.PointD[(N_POINTS)];
				for (int i = 0; i < CAT_PTS.Length; i++)
					CAT_PTS[i] = new JPMath.PointD(CAT_CVAL1s[i], CAT_CVAL2s[i], CAT_MAGs[i]);

				//process the fits image
				BGWRKR.ReportProgress(0, Environment.NewLine + "Processing the FITS Image...");
				IMAGE_WIDTH = FITS_IMG.Width;
				IMAGE_HEIGHT = FITS_IMG.Height;

				BGWRKR.ReportProgress(0, Environment.NewLine + "Searching '" + FITS_IMG.FileName + "' for " + N_POINTS + " point sources...");				
				while (NITERS <= MAXITERS)
				{
					NITERS++;

					PSE.Extract_Sources(FITS_IMG.Image, PIX_SAT, PIXTHRESH, Double.MaxValue, 0, Double.MaxValue, false, PSE_KERNEL_RADIUS, PSE_SEP_RADIUS, AUTO_BACKGROUND, "", IMAGE_ROI, false);

					BGWRKR.ReportProgress(0, "Found " + PSE.N_Sources + " point sources on iteration " + NITERS);

					if (PSE.N_Sources >= N_POINTS)
						break;

					DIV *= 2;
					PIXTHRESH = IMAMP / DIV + IMMED;
				}
				if (PSE.N_Sources > N_POINTS)
					PSE.ClipToNBrightest(N_POINTS);
				BGWRKR.ReportProgress(0, "Stopped searching on iteration " + NITERS + " with " + PSE.N_Sources + " point sources");

				//turn the PSE results into points
				BGWRKR.ReportProgress(0, Environment.NewLine + "Making point source points...");
				PSE_PTS = new JPMath.PointD[PSE.N_Sources];
				for (int i = 0; i < PSE_PTS.Length; i++)
					PSE_PTS[i] = new JPMath.PointD(IMAGE_WIDTH - 1 - PSE.Centroids_X[i], IMAGE_HEIGHT - 1 - PSE.Centroids_Y[i], PSE.Centroids_Volume[i]);

				//now run the auto solver by returning from run worker compeleted
				return;
			}

			if (CANCELLED)
				return;

			//convert field and rotation parameters to radians here:
			SCALE_INIT *= Math.PI / 180 / 3600;
			SCALE_LB *= Math.PI / 180 / 3600;
			SCALE_UB *= Math.PI / 180 / 3600;
			ROTATION_INIT *= Math.PI / 180;
			ROTATION_LB *= Math.PI / 180;
			ROTATION_UB *= Math.PI / 180;
			WCS_VERTEX_TOL *= Math.PI / 180;

			//get pixel initial conditions and bounadries, and CRVAL values
			BGWRKR.ReportProgress(0, "Determining pixel initial conditions and boundaries, and sky coordinate references...");
			double crpix1_init = 0, crpix2_init = 0, crpix1_lb = Double.MaxValue, crpix1_ub = Double.MinValue, crpix2_lb = Double.MaxValue, crpix2_ub = Double.MinValue, crval1 = 0, crval2 = 0;
			for (int i = 0; i < PSE_PTS.Length; i++)
			{
				PSE_PTS[i] = new JPMath.PointD(IMAGE_WIDTH - 1 - PSE.Centroids_X[i], IMAGE_HEIGHT - 1 - PSE.Centroids_Y[i], PSE.Centroids_Volume[i]);
				crpix1_init += PSE_PTS[i].X;
				crpix2_init += PSE_PTS[i].Y;
				if (crpix1_ub < PSE_PTS[i].X)
					crpix1_ub = PSE_PTS[i].X;
				if (crpix1_lb > PSE_PTS[i].X)
					crpix1_lb = PSE_PTS[i].X;
				if (crpix2_ub < PSE_PTS[i].Y)
					crpix2_ub = PSE_PTS[i].Y;
				if (crpix2_lb > PSE_PTS[i].Y)
					crpix2_lb = PSE_PTS[i].Y;
			}
			crpix1_init /= (double)PSE_PTS.Length;//the reference pixel initial guesses can be the means
			crpix2_init /= (double)PSE_PTS.Length;

			for (int i = 0; i < CAT_PTS.Length; i++)
			{
				CAT_PTS[i] = new JPMath.PointD(CAT_CVAL1s[i], CAT_CVAL2s[i], CAT_MAGs[i]);
				crval1 += CAT_PTS[i].X;
				crval2 += CAT_PTS[i].Y;
			}
			crval1 /= (double)CAT_PTS.Length;//the reference value can be the mean
			crval2 /= (double)CAT_PTS.Length;//the reference value can be the mean

			if (CANCELLED)
				return;

			//make PSE triangles
			BGWRKR.ReportProgress(0, "Making point source triangles...");
			int nPSEtriangles = PSE_PTS.Length * (PSE_PTS.Length - 1) * (PSE_PTS.Length - 2) / 6;
			JPMath.Triangle[] PSEtriangles = new JPMath.Triangle[(nPSEtriangles)];
			int c = 0;
			for (int i = 0; i < PSE_PTS.Length - 2; i++)
				for (int j = i + 1; j < PSE_PTS.Length - 1; j++)
					for (int k = j + 1; k < PSE_PTS.Length; k++)
					{
						PSEtriangles[c] = new JPMath.Triangle(PSE_PTS[i], PSE_PTS[j], PSE_PTS[k]/*, true*/);
						c++;
					}

			//convert the catalogue points to intermediate points
			BGWRKR.ReportProgress(0, "Making catalogue intermediate points...");
			JPMath.PointD[] CATpts_intrmdt = new JPMath.PointD[CAT_PTS.Length];
			double a0 = crval1 * Math.PI / 180, d0 = crval2 * Math.PI / 180;
			for (int i = 0; i < CATpts_intrmdt.Length; i++)
			{
				double a = CAT_PTS[i].X * Math.PI / 180;//radians
				double d = CAT_PTS[i].Y * Math.PI / 180;//radians

				//for tangent plane Gnomic
				double Xint = Math.Cos(d) * Math.Sin(a - a0) / (Math.Cos(d0) * Math.Cos(d) * Math.Cos(a - a0) + Math.Sin(d0) * Math.Sin(d));
				double Yint = (Math.Cos(d0) * Math.Sin(d) - Math.Cos(d) * Math.Sin(d0) * Math.Cos(a - a0)) / (Math.Sin(d0) * Math.Sin(d) + Math.Cos(d0) * Math.Cos(d) * Math.Cos(a - a0));

				CATpts_intrmdt[i] = new JPMath.PointD(Xint, Yint, CAT_PTS[i].Value);
			}

			if (CANCELLED)
				return;

			//make intermediate coordinate triangles
			BGWRKR.ReportProgress(0, "Making catalogue intermediate triangles...");
			int nCATtriangles = CATpts_intrmdt.Length * (CATpts_intrmdt.Length - 1) * (CATpts_intrmdt.Length - 2) / 6;
			JPMath.Triangle[] CATtriangles_intrmdt = new JPMath.Triangle[(nCATtriangles)];
			c = 0;
			for (int i = 0; i < CATpts_intrmdt.Length - 2; i++)
				for (int j = i + 1; j < CATpts_intrmdt.Length - 1; j++)
					for (int k = j + 1; k < CATpts_intrmdt.Length; k++)
					{
						CATtriangles_intrmdt[c] = new JPMath.Triangle(CATpts_intrmdt[i], CATpts_intrmdt[j], CATpts_intrmdt[k]/*, true*/);
						c++;
					}

			if (CANCELLED)
				return;

			if (DO_PARALLEL)
				if (CONDITION_ARRAYS)
				{
					BGWRKR.ReportProgress(0, Environment.NewLine + "Conditioning the triangle arrays...");
					PSEtriangles = JPFITS.WCSAutoSolver.ConditionTriangleArrayBrightnessThreads(PSEtriangles, Environment.ProcessorCount, false);
					CATtriangles_intrmdt = JPFITS.WCSAutoSolver.ConditionTriangleArrayBrightnessThreads(CATtriangles_intrmdt, 1, true);
				}

			if (CANCELLED)
				return;

			//for each PSE triangle, fit it to a CAT intermediate triangle, and then check if this fit satisfies the other CAT points to the PSE points
			//rotation transformation p[0] = scale; p[1] = phi (radians); p[2] = x-axis coordinate reference; p[3] = y-axis coordinate reference;
			double[] plb = new double[(4)] { SCALE_LB, ROTATION_LB, crpix1_lb, crpix2_lb };
			double[] pub = new double[(4)] { SCALE_UB, ROTATION_UB, crpix1_ub, crpix2_ub };
			double[] psc = new double[(4)] { SCALE_INIT, 1, Math.Abs(crpix1_init), Math.Abs(crpix2_init) };
			double kern_diam = (double)(2 * PSE_KERNEL_RADIUS) + 1;
			double p00 = 0, p01 = 0, p02 = 0, p03 = 0;
			int total_pt_matches = 0;
			DATE = DateTime.Now;
			TimeSpan ts = new TimeSpan();
			int prog = 0, threadnum = 0;
			ulong ncompares = 0, nfalsepositives = 0, nfalsenegatives = 0, similartriangles = 0;
			bool compare_fieldvectors = ROTATION_LB != -Math.PI && ROTATION_UB != Math.PI;

			ParallelOptions opts  = new ParallelOptions();
			if (DO_PARALLEL)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;
			var rangePartitioner = Partitioner.Create(0, PSEtriangles.Length);
			object locker = new object();
			int thrgrpsz = PSEtriangles.Length / opts.MaxDegreeOfParallelism;
			double mdpT100ovrlen = (double)(opts.MaxDegreeOfParallelism * 100) / (double)PSEtriangles.Length;

			BGWRKR.ReportProgress(0, Environment.NewLine + "Starting search for matching triangles among " + ((long)(PSEtriangles.Length) * (long)(CATtriangles_intrmdt.Length)).ToString("0.00##e+00") + " possible comparisons...");

			Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
			{
				if (SOLVED || CANCELLED)
					loopState.Stop();

				ulong ncompareslocal = 0, nfalsepositives_local = 0, nfalsenegatives_local = 0, similartriangles_local = 0;
				//create these here so that each thread when parallel has own copy
				double[] xpix_triplet = new double[3];
				double[] ypix_triplet = new double[3];
				double[] Xintrmdt_triplet = new double[3];
				double[] Yintrmdt_triplet = new double[3];
				double[] P0 = new double[4];
				double[] PLB = plb;
				double[] PUB = pub;
				double minlength2, maxlength2;

				for (int i = range.Item1; i < range.Item2; i++)
				{
					if (SOLVED)
						break;
					if (CANCELLED)
						break;

					if (i < thrgrpsz)
						if ((int)((double)i * mdpT100ovrlen) > prog)
							BGWRKR.ReportProgress(++prog);
					
					xpix_triplet[0] = PSEtriangles[i].GetVertex(0).X;
					ypix_triplet[0] = PSEtriangles[i].GetVertex(0).Y;
					xpix_triplet[1] = PSEtriangles[i].GetVertex(1).X;
					ypix_triplet[1] = PSEtriangles[i].GetVertex(1).Y;
					xpix_triplet[2] = PSEtriangles[i].GetVertex(2).X;
					ypix_triplet[2] = PSEtriangles[i].GetVertex(2).Y;
					minlength2 = SCALE_LB * (PSEtriangles[i].GetSideLength(2) - kern_diam);//longest side length min
					maxlength2 = SCALE_UB * (PSEtriangles[i].GetSideLength(2) + kern_diam);//longest side length max

					for (int j = 0; j < CATtriangles_intrmdt.Length; j++)
					{
						if (SOLVED)
							break;
						if (CANCELLED)
							break;

						ncompareslocal++;

						//compare AAS (vertex0, vertex1, longest side)
						if (Math.Abs(PSEtriangles[i].GetVertexAngle(0) - CATtriangles_intrmdt[j].GetVertexAngle(0)) > WCS_VERTEX_TOL)
							continue;
						if (Math.Abs(PSEtriangles[i].GetVertexAngle(1) - CATtriangles_intrmdt[j].GetVertexAngle(1)) > WCS_VERTEX_TOL)
							continue;
						if (CATtriangles_intrmdt[j].GetSideLength(2) < minlength2 || CATtriangles_intrmdt[j].GetSideLength(2) > maxlength2)
							continue;

						//this is the angle subtended between the two field vectors of the PSE and intermediate triangles...in the correct direction
						double theta = Math.Atan2(PSEtriangles[i].FieldVector.X * CATtriangles_intrmdt[j].FieldVector.Y - PSEtriangles[i].FieldVector.Y * CATtriangles_intrmdt[j].FieldVector.X, PSEtriangles[i].FieldVector.X * CATtriangles_intrmdt[j].FieldVector.X + PSEtriangles[i].FieldVector.Y * CATtriangles_intrmdt[j].FieldVector.Y);

						if (compare_fieldvectors)//if a rotation estimate has been provided
						{
							//if the angle between the field vectors is smaller/larger than the estimate and bounds, then continue – not the correct triangles to fit given the rotation bounds estimate
							if (theta > (ROTATION_UB + WCS_VERTEX_TOL) || theta < (ROTATION_LB - WCS_VERTEX_TOL))//+- WCS_VERTEX_TOL to bounds to provide tolerance when bounds are equal
								continue;

							P0[1] = ROTATION_INIT;//if here, then reset the fitter’s rotation parameter to initial estimate provided (others reset below)...//reset P0 for j'th iteration
						}
						else//no rotation estimate provided, so we can make our own
						{
							P0[1] = theta;//set the initial rotation to the angle between the field vectors...//reset P0 for j'th iteration
							PLB[1] = theta - WCS_VERTEX_TOL;// provide some tolerance bounds
							PUB[1] = theta + WCS_VERTEX_TOL;// provide some tolerance bounds
						}

                        similartriangles_local++;

                        Xintrmdt_triplet[0] = CATtriangles_intrmdt[j].GetVertex(0).X;
						Yintrmdt_triplet[0] = CATtriangles_intrmdt[j].GetVertex(0).Y;
						Xintrmdt_triplet[1] = CATtriangles_intrmdt[j].GetVertex(1).X;
						Yintrmdt_triplet[1] = CATtriangles_intrmdt[j].GetVertex(1).Y;
						Xintrmdt_triplet[2] = CATtriangles_intrmdt[j].GetVertex(2).X;
						Yintrmdt_triplet[2] = CATtriangles_intrmdt[j].GetVertex(2).Y;

						//reset P0 for j'th iteration
						P0[0] = SCALE_INIT;
						//P0[1] = ROTATION_INIT;//done above in if (compare_fieldvectors)
						P0[2] = crpix1_init;
						P0[3] = crpix2_init;

						//try a fit
						JPMath.Fit_WCSTransform2d(Xintrmdt_triplet, Yintrmdt_triplet, xpix_triplet, ypix_triplet, ref P0, PLB, PUB, psc);

						int N_pt_matches = 0;
						for (int k = 0; k < 3; k++)
						{
							int x = (int)Math.Round((double)IMAGE_WIDTH - 1 - (1 / P0[0] * (Math.Cos(-P0[1]) * Xintrmdt_triplet[k] - Math.Sin(-P0[1]) * Yintrmdt_triplet[k]) + P0[2]));
							int y = (int)Math.Round((double)IMAGE_HEIGHT - 1 - (1 / P0[0] * (Math.Sin(-P0[1]) * Xintrmdt_triplet[k] + Math.Cos(-P0[1]) * Yintrmdt_triplet[k]) + P0[3]));

							if (x > 0 && y > 0 && x < IMAGE_WIDTH && y < IMAGE_HEIGHT && PSE.SourceIndexMap[x, y] == PSE.SourceIndexMap[IMAGE_WIDTH - 1 - (int)Math.Round(xpix_triplet[k]), IMAGE_HEIGHT - 1 - (int)Math.Round(ypix_triplet[k])])
								N_pt_matches++;
						}

						if (N_pt_matches != 3)//not a possible solution
						{
							nfalsenegatives_local++;
							continue;
						}

						//need to check if the other CAT points match the PSE pts
						N_pt_matches = 0;
						for (int k = 0; k < CATpts_intrmdt.Length; k++)
						{
							double X_int = CATpts_intrmdt[k].X;
							double Y_int = CATpts_intrmdt[k].Y;

							int x = (int)Math.Round((double)IMAGE_WIDTH - 1 - (1 / P0[0] * (Math.Cos(-P0[1]) * X_int - Math.Sin(-P0[1]) * Y_int) + P0[2]));
							int y = (int)Math.Round((double)IMAGE_HEIGHT - 1 - (1 / P0[0] * (Math.Sin(-P0[1]) * X_int + Math.Cos(-P0[1]) * Y_int) + P0[3]));

							if (x > 0 && y > 0 && x < IMAGE_WIDTH && y < IMAGE_HEIGHT && PSE.SourceBooleanMap[x, y])
								N_pt_matches++;
						}
						if (N_pt_matches >= N_MATCHES_STOP || N_pt_matches * 100 / CATpts_intrmdt.Length >= PERC_MATCHES_STOP)
						{
							SOLVED = true;
							ts = DateTime.Now - DATE;
							total_pt_matches = N_pt_matches;
							p00 = P0[0];
							p01 = P0[1];
							p02 = P0[2];
							p03 = P0[3];
							threadnum = i / thrgrpsz;// Thread.CurrentThread.ManagedThreadId;
						}
						else
							nfalsepositives_local++;
					}
				}
				lock (locker)
				{
					similartriangles += similartriangles_local;
					ncompares += ncompareslocal;
					nfalsepositives += nfalsepositives_local;
					nfalsenegatives += nfalsenegatives_local;
				}
			});

			if (!SOLVED)
				BGWRKR.ReportProgress(0, "No solution...");
			if (CANCELLED)
				BGWRKR.ReportProgress(0, "Cancelled...");
			if (!SOLVED || CANCELLED)
				return;

			//solve on all matching coordinates
			BGWRKR.ReportProgress(0, "Found an initial solution given the stopping criteria of either " + N_MATCHES_STOP + " matching points or " + PERC_MATCHES_STOP + "% matching points" + Environment.NewLine);
			BGWRKR.ReportProgress(0, "Field Scale: " + Math.Round(p00 * 180 / Math.PI * 3600, 4));
			BGWRKR.ReportProgress(0, "Field Rotation: " + Math.Round(p01 * 180 / 3.14159265, 3));
			BGWRKR.ReportProgress(0, "N Pt. Matches: " + total_pt_matches + " (" + (total_pt_matches * 100 / CATpts_intrmdt.Length).ToString("00.0") + "%)");
			BGWRKR.ReportProgress(0, "N Comparisons: " + ncompares.ToString("0.00e00") + " (" + Math.Round((double)(ncompares * 100) / (double)(PSEtriangles.Length) / (double)(CATtriangles_intrmdt.Length), 1) + "%)");
			BGWRKR.ReportProgress(0, "Similar Triangles: " + similartriangles.ToString());
			BGWRKR.ReportProgress(0, "N False Negatives: " + nfalsenegatives);
			BGWRKR.ReportProgress(0, "N False Postives: " + nfalsepositives);
			BGWRKR.ReportProgress(0, "Thread: " + threadnum);
			BGWRKR.ReportProgress(0, "Completed in: " + ts.Minutes.ToString() + "m" + ((double)(ts.Seconds) + (double)ts.Milliseconds / 1000).ToString() + "s");
			BGWRKR.ReportProgress(0, "Comparisons per Second: " + (ncompares / ts.TotalSeconds).ToString("0.00e00") + Environment.NewLine);

			if (CANCELLED)
				return;

			BGWRKR.ReportProgress(0, "Matching all the points that I can given the " + N_POINTS + " input point pairs...");
			double[] cval1 = new double[(total_pt_matches)];
			double[] cval2 = new double[(total_pt_matches)];
			double[] cpix1 = new double[(total_pt_matches)];
			double[] cpix2 = new double[(total_pt_matches)];
			c = 0;
			for (int k = 0; k < CATpts_intrmdt.Length; k++)
			{
				double X_int = CATpts_intrmdt[k].X;
				double Y_int = CATpts_intrmdt[k].Y;

				int x = (int)Math.Round((double)IMAGE_WIDTH - 1 - (1 / p00 * (Math.Cos(-p01) * X_int - Math.Sin(-p01) * Y_int) + p02));
				int y = (int)Math.Round((double)IMAGE_HEIGHT - 1 - (1 / p00 * (Math.Sin(-p01) * X_int + Math.Cos(-p01) * Y_int) + p03));

				if (x > 0 && y > 0 && x < IMAGE_WIDTH && y < IMAGE_HEIGHT && PSE.SourceBooleanMap[x, y])
				{
					int index = PSE.SourceIndexMap[x, y];
					cpix1[c] = PSE.Centroids_X[index];
					cpix2[c] = PSE.Centroids_Y[index];
					cval1[c] = CAT_PTS[k].X;
					cval2[c] = CAT_PTS[k].Y;
					c++;
				}
			}

			if (CANCELLED)
				return;

			BGWRKR.ReportProgress(0, "Solving for " + c + " point pair matches out of a possible " + N_POINTS);
			WCS.Solve_WCS("TAN", cpix1, cpix2, true, cval1, cval2, FITS_IMG.Header);
			BGWRKR.ReportProgress(0, "Solution:" + Environment.NewLine);
			BGWRKR.ReportProgress(0, "CRPIX1 = " + WCS.GetCRPIXn(1));
			BGWRKR.ReportProgress(0, "CRPIX2 = " + WCS.GetCRPIXn(2));
			BGWRKR.ReportProgress(0, "CRVAL1 = " + WCS.GetCRVALn(1));
			BGWRKR.ReportProgress(0, "CRVAL2 = " + WCS.GetCRVALn(2));
			BGWRKR.ReportProgress(0, "CD_1_1 = " + WCS.GetCDi_j(1, 1));
			BGWRKR.ReportProgress(0, "CD_1_2 = " + WCS.GetCDi_j(1, 2));
			BGWRKR.ReportProgress(0, "CD_2_1 = " + WCS.GetCDi_j(2, 1));
			BGWRKR.ReportProgress(0, "CD_2_2 = " + WCS.GetCDi_j(2, 2));
			BGWRKR.ReportProgress(0, "CDELT1 = " + WCS.GetCDELTn(1));
			BGWRKR.ReportProgress(0, "CDELT2 = " + WCS.GetCDELTn(2));
			BGWRKR.ReportProgress(0, "CROTA1 = " + WCS.GetCROTAn(1));
			BGWRKR.ReportProgress(0, "CROTA2 = " + WCS.GetCROTAn(2));
			BGWRKR.ReportProgress(0, "WCS Fit Residual Mean (pixels) = " + WCS.WCSFitResidual_MeanPix);
			BGWRKR.ReportProgress(0, "WCS Fit Residual Stdv (pixels) = " + WCS.WCSFitResidual_StdvPix);
			BGWRKR.ReportProgress(0, "WCS Fit Residual Mean (arcsec) = " + WCS.WCSFitResidual_MeanSky);
			BGWRKR.ReportProgress(0, "WCS Fit Residual Stdv (arcsec) = " + WCS.WCSFitResidual_StdvSky + Environment.NewLine);

			if (!REFINE || FITS_IMG == null)
			{
				WCSARF.CancelBtn.Text = "Finished";
				BGWRKR.ReportProgress(0, "Finished...");
				return;
			}

			if (CANCELLED)
				return;
			
			BGWRKR.ReportProgress(0, "Refining solution...");
			N_POINTS *= 3;
			NITERS = 0;
			PSE = new JPFITS.PointSourceExtractor();
			BGWRKR.ReportProgress(0, Environment.NewLine + "Searching '" + FITS_IMG.FileName + "' for " + N_POINTS + " point sources...");
			while (NITERS <= MAXITERS)
			{
				NITERS++;

				PSE.Extract_Sources(FITS_IMG.Image, PIX_SAT, PIXTHRESH, Double.MaxValue, 0, Double.MaxValue, false, PSE_KERNEL_RADIUS, PSE_SEP_RADIUS, AUTO_BACKGROUND, "", IMAGE_ROI, false);

				BGWRKR.ReportProgress(0, "Found " + PSE.N_Sources + " point sources on iteration " + NITERS);

				if (PSE.N_Sources >= N_POINTS)
					break;

				DIV *= 2;
				PIXTHRESH = IMAMP / DIV + IMMED;
			}
			if (PSE.N_Sources > N_POINTS)
				PSE.ClipToNBrightest(N_POINTS);
			BGWRKR.ReportProgress(0, "Stopped searching on iteration " + NITERS + " with " + PSE.N_Sources + " point sources");

			if (CANCELLED)
				return;

			if (N_POINTS > CAT_CVAL1s.Length)
				N_POINTS = CAT_CVAL1s.Length;

			//get the brightest catlaogue points
			cval1 = new double[(N_POINTS)];
			cval2 = new double[(N_POINTS)];
			for (int i = 0; i < N_POINTS; i++)
			{
				cval1[i] = CAT_CVAL1s[i];
				cval2[i] = CAT_CVAL2s[i];
			}

			//get the catlaogue pixel locations
			cpix1 = new double[(N_POINTS)];
			cpix2 = new double[(N_POINTS)];
			WCS.Get_Pixels(cval1, cval2, "TAN", out cpix1, out cpix2, true);

			//check for catlaogue pixels which fall onto PSE pixels
			int nmatches = 0;
			bool[] match = new bool[(N_POINTS)];
			int[] matchinds = new int[(N_POINTS)];
			for (int i = 0; i < N_POINTS; i++)
			{
				int x = (int)Math.Round(cpix1[i]);
				int y = (int)Math.Round(cpix2[i]);
				if (x > 0 && x < IMAGE_WIDTH && y > 0 && y < IMAGE_HEIGHT)
					if (PSE.SourceBooleanMap[x, y])
					{
						nmatches++;
						match[i] = true;
						matchinds[i] = PSE.SourceIndexMap[x, y];
					}
			}

			int n = 0;
			for (int i = 0; i < N_POINTS; i++)
			{
				if (!match[i])
					continue;

				cval1[n] = CAT_CVAL1s[i];
				cval2[n] = CAT_CVAL2s[i];
				cpix1[n] = PSE.Centroids_X[matchinds[i]];
				cpix2[n] = PSE.Centroids_Y[matchinds[i]];
				n++;
			}
			Array.Resize(ref cval1, nmatches);
			Array.Resize(ref cval2, nmatches);
			Array.Resize(ref cpix1, nmatches);
			Array.Resize(ref cpix2, nmatches);

			if (CANCELLED)
				return;

			WorldCoordinateSolution.ClearWCS(FITS_IMG.Header);
			WCS.Solve_WCS("TAN", cpix1, cpix2, true, cval1, cval2, FITS_IMG.Header);
			BGWRKR.ReportProgress(0, Environment.NewLine + nmatches + " sources of " + N_POINTS + " were able to be used for WCS refinement.");
			BGWRKR.ReportProgress(0, Environment.NewLine + "Refined solution:" + Environment.NewLine);
			BGWRKR.ReportProgress(0, "CRPIX1 = " + WCS.GetCRPIXn(1));
			BGWRKR.ReportProgress(0, "CRPIX2 = " + WCS.GetCRPIXn(2));
			BGWRKR.ReportProgress(0, "CRVAL1 = " + WCS.GetCRVALn(1));
			BGWRKR.ReportProgress(0, "CRVAL2 = " + WCS.GetCRVALn(2));
			BGWRKR.ReportProgress(0, "CD_1_1 = " + WCS.GetCDi_j(1, 1));
			BGWRKR.ReportProgress(0, "CD_1_2 = " + WCS.GetCDi_j(1, 2));
			BGWRKR.ReportProgress(0, "CD_2_1 = " + WCS.GetCDi_j(2, 1));
			BGWRKR.ReportProgress(0, "CD_2_2 = " + WCS.GetCDi_j(2, 2));
			BGWRKR.ReportProgress(0, "CDELT1 = " + WCS.GetCDELTn(1));
			BGWRKR.ReportProgress(0, "CDELT2 = " + WCS.GetCDELTn(2));
			BGWRKR.ReportProgress(0, "CROTA1 = " + WCS.GetCROTAn(1));
			BGWRKR.ReportProgress(0, "CROTA2 = " + WCS.GetCROTAn(2));
			BGWRKR.ReportProgress(0, "WCS Fit Residual Mean (pixels) = " + WCS.WCSFitResidual_MeanPix);
			BGWRKR.ReportProgress(0, "WCS Fit Residual Stdv (pixels) = " + WCS.WCSFitResidual_StdvPix);
			BGWRKR.ReportProgress(0, "WCS Fit Residual Mean (arcsec) = " + WCS.WCSFitResidual_MeanSky);
			BGWRKR.ReportProgress(0, "WCS Fit Residual Stdv (arcsec) = " + WCS.WCSFitResidual_StdvSky + Environment.NewLine);
			BGWRKR.ReportProgress(0, "Finished..." + Environment.NewLine);
			WCSARF.CancelBtn.Text = "Finished";
		}

		private void BGWRKR_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
		{
			PROGRESS = e.ProgressPercentage;

			if (PROGRESS == 0)
				STATUS_LOG += Environment.NewLine + (string)e.UserState;
			else
			{
				STATUS_LOG = STATUS_LOG.Remove(STATUS_LOG.LastIndexOf(Environment.NewLine));
				STATUS_LOG += Environment.NewLine + "Approximate progress: " + (PROGRESS + 1).ToString() + "%";
			}
		}

		private void BGWRKR_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
		{
			if (CANCELLED)
			{
				WCS = null;
				SOLVED = false;
				SOLVING = false;
				return;
			}

			if (DO_PSE)
			{
				DO_PSE = false;
				SolveAsync(SCALE_INIT, SCALE_LB, SCALE_UB, ROTATION_INIT, ROTATION_LB, ROTATION_UB, WCS_VERTEX_TOL, N_MATCHES_STOP, PERC_MATCHES_STOP, CONDITION_ARRAYS, SHOW_REPORT_FORM);
				return;
			}

			if (!SOLVED)
			{
				SOLVING = false;
				if (WCSARF != null)
					WCSARF.CancelBtn.DialogResult = DialogResult.No;
				return;
			}

			SOLVING = false;
			if (WCSARF != null)
				WCSARF.CancelBtn.DialogResult = DialogResult.OK;
		}

		//private static void MAKEASTROQUERYSCRIPT(string script_filename, string catalogue)
		//{
		//	string script = "";
		//	script += "import argparse" + Environment.NewLine;
		//	script += "import sys" + Environment.NewLine;
		//	script += "from astroquery.simbad import Simbad" + Environment.NewLine;
		//	script += "from astropy.coordinates import SkyCoord" + Environment.NewLine;
		//	script += "import astropy.units as u" + Environment.NewLine;
		//	if (catalogue == "gaia")
		//		script += "from astroquery.gaia import Gaia" + Environment.NewLine;

		//	script += "ra = float(sys.argv[1])" + Environment.NewLine;
		//	script += "dec = float(sys.argv[2])" + Environment.NewLine;
		//	script += "filename = str(sys.argv[3])" + Environment.NewLine;
		//	script += "radius = float(sys.argv[4])" + Environment.NewLine;
		//	script += "square = float(sys.argv[5])" + Environment.NewLine;

		//	script += "radvals = radius * u.arcmin" + Environment.NewLine;
		//	script += "if square == 1:" + Environment.NewLine;
		//	script += "    radvals = radvals * 2;" + Environment.NewLine;

		//	script += "coords = SkyCoord(ra = ra, dec = dec, unit = (u.deg, u.deg), frame = 'fk5')" + Environment.NewLine;

		//	if (catalogue == "gaia")
		//	{
		//		script += "jobstr = \"SELECT * FROM gaiadr2.gaia_source\"" + Environment.NewLine;
		//		script += "jobstr += \" WHERE CONTAINS(POINT('ICRS', gaiadr2.gaia_source.ra,gaiadr2.gaia_source.dec),\"" + Environment.NewLine;
		//	}
		//	script += "if square == 1:" + Environment.NewLine;
		//	script += "    jobstr += \"BOX('ICRS',{0},{1},{2},{2}))=1;\".format(coords.ra.deg, coords.dec.deg, radvals.to(u.deg).value)" + Environment.NewLine;
		//	script += "else:" + Environment.NewLine;
		//	script += "    jobstr += \"CIRCLE('ICRS',{0},{1},{2}))=1;\".format(coords.ra.deg, coords.dec.deg, radvals.to(u.deg).value)" + Environment.NewLine;

		//	if (catalogue == "gaia")
		//		script += "print(\"Launching job query to Gaia archive\")" + Environment.NewLine;
		//	script += "print(jobstr)" + Environment.NewLine;
		//	script += "print(\" \")" + Environment.NewLine;
		//	script += "print(\"Waiting for query results...\")" + Environment.NewLine;
		//	if (catalogue == "gaia")
		//		script += "job = Gaia.launch_job_async(jobstr, dump_to_file = False)" + Environment.NewLine;
		//	script += "print(job)" + Environment.NewLine;
		//	script += "results = job.get_results()" + Environment.NewLine;
		//	script += "removelist = []" + Environment.NewLine;

		//	//Strip object columns from FITS table
		//	script += "for col in results.columns:" + Environment.NewLine;
		//	script += "    if results[col].dtype == 'object' :" + Environment.NewLine;
		//	script += "        removelist += [col]" + Environment.NewLine;
		//	script += "results.remove_columns(removelist)" + Environment.NewLine;
		//	script += "results.write(filename, overwrite = True, format = 'fits')";

		//	StreamWriter sw = new StreamWriter(script_filename);
		//	sw.Write(script);
		//	sw.Close();
		//}

		//private static void MAKEGAIADR3QUERYNSCRIPT(string script_filename)
		//{
		//	string script = "";
		//	script += "import argparse" + Environment.NewLine;
		//	script += "import sys" + Environment.NewLine;
		//	script += "from astroquery.simbad import Simbad" + Environment.NewLine;
		//	script += "from astropy.coordinates import SkyCoord" + Environment.NewLine;
		//	script += "import astropy.units as u" + Environment.NewLine;
		//	script += "from astroquery.gaia import Gaia" + Environment.NewLine;

		//	script += "ra = float(sys.argv[1])" + Environment.NewLine;
		//	script += "dec = float(sys.argv[2])" + Environment.NewLine;
		//	script += "filename = str(sys.argv[3])" + Environment.NewLine;
		//	script += "radius = float(sys.argv[4])" + Environment.NewLine;
		//	script += "square = int(sys.argv[5])" + Environment.NewLine;
		//	script += "number = int(sys.argv[6])" + Environment.NewLine;
		//	script += "sortfilt = str(sys.argv[7])" + Environment.NewLine;

		//	script += "radvals = radius * u.arcmin" + Environment.NewLine;
		//	script += "if square == 1:" + Environment.NewLine;
		//	script += "    radvals = radvals * 2;" + Environment.NewLine;

		//	script += "coords = SkyCoord(ra=ra, dec=dec, unit=(u.deg, u.deg), frame='fk5')" + Environment.NewLine;

		//	script += "jobstr = \"SELECT TOP {0} * FROM gaiaedr3.gaia_source\\n\".format(number)" + Environment.NewLine;
		//	script += "jobstr += \"WHERE 1=CONTAINS(POINT('ICRS', gaiaedr3.gaia_source.ra,gaiaedr3.gaia_source.dec),\"" + Environment.NewLine;

		//	script += "if square == 1:" + Environment.NewLine;
		//	script += "    jobstr += \"BOX('ICRS',{0},{1},{2},{2}))\\n\".format(coords.ra.deg,coords.dec.deg,radvals.to(u.deg).value)" + Environment.NewLine;
		//	script += "else:" + Environment.NewLine;
		//	script += "    jobstr += \"CIRCLE('ICRS',{0},{1},{2}))\\n\".format(coords.ra.deg,coords.dec.deg,radvals.to(u.deg).value)" + Environment.NewLine;
		//	script += "if sortfilt == \"bp\":" + Environment.NewLine;
		//	script += "    jobstr += \"ORDER by gaiaedr3.gaia_source.phot_bp_mean_mag ASC\"" + Environment.NewLine;
		//	script += "elif sortfilt == \"rp\":" + Environment.NewLine;
		//	script += "    jobstr += \"ORDER by gaiaedr3.gaia_source.phot_rp_mean_mag ASC\"" + Environment.NewLine;
		//	script += "else:" + Environment.NewLine;
		//	script += "    jobstr += \"ORDER by gaiaedr3.gaia_source.phot_g_mean_mag ASC\"" + Environment.NewLine;

		//	script += "print(\"Launching job query to Gaia archive\")" + Environment.NewLine;
		//	script += "print(jobstr)" + Environment.NewLine;
		//	script += "print(\" \")" + Environment.NewLine;
		//	script += "print(\"Waiting for query results...\")" + Environment.NewLine;
		//	script += "try:" + Environment.NewLine;
		//	script += "    job = Gaia.launch_job_async(jobstr, dump_to_file = False)" + Environment.NewLine;
		//	script += "except:" + Environment.NewLine;
		//	script += "    print(job)" + Environment.NewLine;
		//	script += "    print(\"Press enter to continue...\")" + Environment.NewLine;
		//	script += "    input()" + Environment.NewLine;
		//	script += "print(job)" + Environment.NewLine;
		//	script += "results = job.get_results()" + Environment.NewLine;

		//	//Strip object columns from FITS table
		//	script += "removelist = []" + Environment.NewLine;
		//	script += "for col in results.columns:" + Environment.NewLine;
		//	script += "    if results[col].dtype == 'object' :" + Environment.NewLine;
		//	script += "        removelist += [col]" + Environment.NewLine;
		//	script += "results.remove_columns(removelist)" + Environment.NewLine;
		//	script += "results.write(filename, overwrite = True, format = 'fits')";

		//	StreamWriter sw = new StreamWriter(script_filename);
		//	sw.Write(script);
		//	sw.Close();
		//}

		#endregion

		#region CONSTRUCTORS

		/// <summary>Initializes the WCS_AutoSolver class including performing source extraction on a given FITS image.</summary>
		/// <param name="WCS_type">The WCS transformation type. Solution only uses TAN at this time.</param>
		/// <param name="Number_of_Points">The number of points N to use to compare image coordinates to catalogue coordinates. Suggest N equals 25 for good correspondence, N equals 50 for poor, N equals 100 for very poor.</param>
		/// <param name="Fits_Img">The JPFITS.FITSImage containing the primary image data.</param>
		/// <param name="Image_ROI">The region of interest of the FITS image to search for point sources, of identical size to the FITS image. Pass null or all true for entire image.</param>
		/// <param name="Image_Saturation">The saturation level of the source image for mapping saturated sources. Pass zero if no saturated sources exist.</param>
		/// <param name="auto_background">Automatically determine local background for each centroiding kernel.</param>
		/// <param name="PSE_kernel_radius">The radius of the point-source-extraction kernel, in pixels. PSEkernel_radius greater than or equal to 1.</param>
		/// <param name="PSE_separation_radius">The minimum separation of point sources, in pixels. PSESeparation_radius greater than or equal to PSEkernel_radius.</param>
		/// <param name="Fits_Catalogue_BinTable_File">The full path file name of the FITS binary table containing the catalogue data.</param>
		/// <param name="Catalogue_Extension_Name">The extension name of the FITS binary table which contains the catalogue data. If empty string is passed then the first binary table extension is assumed.</param>
		/// <param name="Catalogue_CVAL1_Name">The name of the entry inside the binary table which lists the CVAL1 (i.e. right ascension) coordinates.</param>
		/// <param name="Catalogue_CVAL2_Name">The name of the entry inside the binary table which lists the CVAL2 (i.e. declination) coordinates.</param>
		/// <param name="Catalogue_Magnitude_Name">The name of the entry inside the binary table which lists the source magnitudes.</param>
		/// <param name="Refine">Option to automatically refine the solution further with additional points after the initial solution is found.</param>
		public WCSAutoSolver(string WCS_type, int Number_of_Points, JPFITS.FITSImage Fits_Img, bool[,] Image_ROI, double Image_Saturation, bool auto_background, int PSE_kernel_radius, int PSE_separation_radius, string Fits_Catalogue_BinTable_File, string Catalogue_Extension_Name, string Catalogue_CVAL1_Name, string Catalogue_CVAL2_Name, string Catalogue_Magnitude_Name, bool Refine)
		{
			this.BGWRKR = new BackgroundWorker();
			this.BGWRKR.WorkerReportsProgress = true;
			this.BGWRKR.WorkerSupportsCancellation = true;
			this.BGWRKR.DoWork += new System.ComponentModel.DoWorkEventHandler(BGWRKR_DoWork);
			this.BGWRKR.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(BGWRKR_ProgressChanged);
			this.BGWRKR.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(BGWRKR_RunWorkerCompleted);

			WCS_TYPE = WCS_type;
			N_POINTS = Number_of_Points;
			FITS_IMG = Fits_Img;
			IMAGE_ROI = Image_ROI;
			PIX_SAT = Image_Saturation;
			AUTO_BACKGROUND = auto_background;
			PSE_KERNEL_RADIUS = PSE_kernel_radius;
			PSE_SEP_RADIUS = PSE_separation_radius;
			CAT_FILENAME = Fits_Catalogue_BinTable_File;
			CAT_EXTNAME = Catalogue_Extension_Name;
			CAT_CVAL1NAME = Catalogue_CVAL1_Name;
			CAT_CVAL2NAME = Catalogue_CVAL2_Name;
			CAT_MAGNAME = Catalogue_Magnitude_Name;
			REFINE = Refine;
			DO_PSE = true;
			CANCELLED = false;
			PROGRESS = 0;
			SOLVED = false;
			PSE = new JPFITS.PointSourceExtractor();
			WCS = new JPFITS.WorldCoordinateSolution();

			DIV = 8;
			NITERS = 0;
			MAXITERS = 11;
			IMMAX = FITS_IMG.Max;
			IMMED = FITS_IMG.Median;
			IMAMP = IMMAX - IMMED;
			PIXTHRESH = IMAMP / DIV + IMMED;
		}

		/// <summary>Initializes the WCS_AutoSolver class for an existing pair of pixel source and catalogue coordinates.</summary>
		/// <param name="WCS_type">The WCS transformation type. Solution only uses TAN at this time.</param>
		/// <param name="pixels">The source pixel positions in computer graphics coordinate orientation, i.e., origin top left of image.</param>
		/// <param name="zero_based_pixels">If the source pixel positions are zero-based.</param>
		/// <param name="pixels_tolerance_radius">The tolerance of the source positions, identical to usage as the PSE_kernel_radius in the other constructor. Typically 2 (pixels).</param>
		/// <param name="image_width">The 1-based width of the source image from where the source pixels points originate.</param>
		/// <param name="image_height">The 1-based height of the source image from where the source pixels points originate.</param>
		/// <param name="wcspoints">The catalogue sky coordinate values, in degrees, corresponding to the region in the image of the source pixel positions.</param>
		public WCSAutoSolver(string WCS_type, JPMath.PointD[] pixels, bool zero_based_pixels, int pixels_tolerance_radius, int image_width, int image_height, JPMath.PointD[] wcspoints)
		{
			this.BGWRKR = new BackgroundWorker();
			this.BGWRKR.WorkerReportsProgress = true;
			this.BGWRKR.WorkerSupportsCancellation = true;
			this.BGWRKR.DoWork += new System.ComponentModel.DoWorkEventHandler(BGWRKR_DoWork);
			this.BGWRKR.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(BGWRKR_ProgressChanged);
			this.BGWRKR.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(BGWRKR_RunWorkerCompleted);

			WCS_TYPE = WCS_type;
			PSE_PTS = pixels;
			ZERO_BASED_PIX = zero_based_pixels;
			PSE_KERNEL_RADIUS = pixels_tolerance_radius;
			IMAGE_WIDTH = image_width;
			IMAGE_HEIGHT = image_height;
			CAT_PTS = wcspoints;
			CANCELLED = false;
			PROGRESS = 0;
			DO_PSE = false;
			N_POINTS = pixels.Length;
			SOLVED = false;
			PSE = null;
			WCS = new JPFITS.WorldCoordinateSolution();
		}

		#endregion

		#region PROPERTIES

		/// <summary>Returns the World Coordinate Solution</summary>
		public WorldCoordinateSolution WCS_Solution
		{
			get { return WCS; }
		}

		/// <summary>Returns the most recent Point Source Extraction</summary>
		public PointSourceExtractor PSE_Extraction
		{
			get { return PSE; }
		}

		/// <summary>Gets or Sets the Status Log</summary>
		public string Status_Log
		{
			get { return STATUS_LOG; }
			set { STATUS_LOG += "\r" + value; }
		}		

		/// <summary>Gets or Sets the Cancel State of the Solver</summary>
		public bool Cancelled
		{
			get { return CANCELLED; }
			set { CANCELLED = value; }
		}

		/// <summary>Gets the Solving State of the Solver</summary>
		public bool Solving
		{
			get { return SOLVING; }
		}

		/// <summary>Gets the Solution State of the Solver</summary>
		public bool Solved
		{
			get { return SOLVED; }
		}

		/// <summary>Gets Progress percentage of the Solver</summary>
		public int Progress
		{
			get { return PROGRESS; }
		}

		/// <summary>Gets or Sets the Solver to run parallelized (default is true)</summary>
		public bool Solver_Parallelized
		{
			get { return DO_PARALLEL; }
			set { DO_PARALLEL = value; }
		}

		#endregion

		#region PUBLIC METHODS

		/// <summary>Clears the Status Log</summary>
		public void Status_Log_Clear()
		{
			STATUS_LOG = "";
		}

		/// <summary>Executes the auto-solver algorithm.</summary>
		/// <param name="scale_init">The initial scale guess, in arcseconds per pixel.</param>
		/// <param name="scale_lb">The lower bound of the scale range, in arcseconds per pixel.</param>
		/// <param name="scale_ub">The upper bound of the scale range, in arcseconds per pixel.</param>
		/// <param name="rotation_init">The initial field rotation guess, in degrees.</param>
		/// <param name="rotation_lb">The lower bound of the field rotation range, in degrees, greater than or equal to -180</param>
		/// <param name="rotation_ub">The upper bound of the field rotation range, in degrees, less than or equal to 180</param>
		/// <param name="vertex_tolerance">The tolerance of the vertex angles when comparing triangles, in degrees. Suggest 0.25.</param>
		/// <param name="N_matches_stop">Stop and solve solution when N matches are found between image and catalogue coordinates. N_matches_stop greater than or equal to 3. Suggest 6. Solution likely requires confirmation at 3 or 4.</param>
		/// <param name="Percentage_matches_stop">Stop and solve solution when Percentage matches are found between image and catalogue coordinates. Suggest 25.</param>
		/// <param name="condition_arrays">Optionally condition the triangle arrays. Suggest true.</param>
		/// <param name="show_report_form">Optionally shows a cancellable Form which displays the solution progress.</param>
		public void SolveAsync(double scale_init, double scale_lb, double scale_ub, double rotation_init, double rotation_lb, double rotation_ub, double vertex_tolerance, int N_matches_stop, int Percentage_matches_stop, bool condition_arrays, bool show_report_form)
		{
			SCALE_INIT = scale_init;
			SCALE_LB = scale_lb;
			SCALE_UB = scale_ub;
			ROTATION_INIT = rotation_init;
			ROTATION_LB = rotation_lb;
			ROTATION_UB = rotation_ub;
			WCS_VERTEX_TOL = vertex_tolerance;
			N_MATCHES_STOP = N_matches_stop;
			PERC_MATCHES_STOP = Percentage_matches_stop;
			CONDITION_ARRAYS = condition_arrays;
			SHOW_REPORT_FORM = show_report_form;
			SOLVING = true;

			if (SHOW_REPORT_FORM)
			{
				SHOW_REPORT_FORM = false;
				WCSARF = new WCSAutoSolverReportingForm(this);
				WCSARF.WCSAutoReportingTimer.Enabled = true;
				BGWRKR.RunWorkerAsync();
				DialogResult res = WCSARF.ShowDialog();
				if (res == DialogResult.Cancel)
					CANCELLED = true;
			}
			else
				BGWRKR.RunWorkerAsync();
		}

		#endregion

		#region STATIC METHODS

		/// <summary>Conditions the traingle array so that all threads begin with the brightest triangles.</summary>
		/// <param name="triarray">An array of triangles.</param>
		/// <param name="Nthreads">The number of threads to condition the array for.</param>
		/// <param name="invertNumericBrightness">Magnitudes = true, otherwise counts = false.</param>
		public static JPMath.Triangle[] ConditionTriangleArrayBrightnessThreads(JPMath.Triangle[] triarray, int Nthreads, bool invertNumericBrightness)
		{
			//reformat traingle arrays for threading
			int Ntris = triarray.Length;
			double[] trivals = new double[Ntris];

			Parallel.For(0, Ntris, i =>
			{
				trivals[i] = triarray[i].VertexPointSum;
			});

			Array.Sort(trivals, triarray);

			if (!invertNumericBrightness)
				Array.Reverse(triarray);

			if (Nthreads == 1)
				return triarray;

			Ntris = (Ntris / Nthreads) * Nthreads;
			Array.Resize(ref triarray, Ntris);

			int dim1 = triarray.Length / Nthreads;
			JPMath.Triangle[,] temptris = new JPMath.Triangle[Nthreads, dim1];

			Parallel.For(0, dim1, i =>
			{
				for (int j = 0; j < Nthreads; j++)
					temptris[j, i] = triarray[i * Nthreads + j];
			});

			Parallel.For(0, dim1, i =>
			{
				for (int j = 0; j < Nthreads; j++)
					triarray[i + j * dim1] = temptris[j, i];
			});

			JPMath.Triangle[] retarray = new JPMath.Triangle[Ntris];
			Array.Copy(triarray, retarray, Ntris);
			return retarray;
		}

		///// <summary>Queries the Gaia catalogue for entries within a specified region. Returns 0 if the query was successful.</summary>
		///// <param name="catalogue">A string for the catalogue to query. Options are (case insensitive): "Gaia"</param>
		///// <param name="ra_deg">A string of the right ascension in degrees.</param>
		///// <param name="dec_deg">A string of the declination in degrees.</param>
		///// <param name="result_savepathfilename">The filename to save the query catalogue. If saving is not required, pass an empty string.</param>
		///// <param name="radius">A string of the region radius in arcminutes.</param>
		///// <param name="square">Pass 1 if the region is square, 0 for circle.</param>
		//public static int AstroQuery(string catalogue, string ra_deg, string dec_deg, ref string result_savepathfilename, string radius, string square)
		//{
		//	string pypath = (string)REG.GetReg("CCDLAB", "PythonExePath");

		//	if (pypath == null || !File.Exists(pypath))
		//	{
		//		string[] dirsappdata = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "*Python*", SearchOption.AllDirectories);
		//		string[] dirsprogdata = Directory.GetDirectories("C:\\Program Files\\", "*Python*", SearchOption.TopDirectoryOnly);

		//		ArrayList locs = new ArrayList();

		//		for (int i = 0; i < dirsappdata.Length; i++)
		//		{
		//			string[] files = Directory.GetFiles(dirsappdata[i], "*python.exe", SearchOption.TopDirectoryOnly);
		//			if (files.Length == 1)
		//				locs.Add(files[0]);
		//		}
		//		for (int i = 0; i < dirsprogdata.Length; i++)
		//		{
		//			string[] files = Directory.GetFiles(dirsprogdata[i], "*python.exe", SearchOption.TopDirectoryOnly);
		//			if (files.Length == 1)
		//				locs.Add(files[0]);
		//		}

		//		if (locs.Count == 0)
		//		{
		//			if (MessageBox.Show("Is Python installed? Please show me where your Python installation is located, OK? \r\n\r\nIf Python is not installed, please gather it from:\r\n\r\n https://www.python.org/downloads/windows/", "I cannot find python.exe", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
		//				return -2;

		//			OpenFileDialog ofd = new OpenFileDialog();
		//			ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		//			ofd.Filter = "Executable|*.exe;";
		//			if (ofd.ShowDialog() == DialogResult.Cancel)
		//				return -2;

		//			pypath = ofd.FileName;
		//		}
		//		else
		//		{
		//			pypath = (string)locs[0];
		//			DateTime date = File.GetCreationTimeUtc((string)locs[0]);
		//			for (int i = 0; i < locs.Count; i++)
		//				if (File.GetCreationTimeUtc((string)locs[i]) > date)
		//				{
		//					date = File.GetCreationTimeUtc((string)locs[i]);
		//					pypath = (string)locs[i];
		//				}
		//		}

		//		REG.SetReg("CCDLAB", "PythonExePath", pypath);
		//	}

		//	catalogue = catalogue.ToLower();
		//	string script = "C:\\ProgramData\\Astrowerks\\CCDLAB\\astro_query.py";
		//	MAKEASTROQUERYSCRIPT(script, catalogue);

		//	if (result_savepathfilename == "")
		//	{
		//		if (!Directory.Exists("C:\\ProgramData\\Astrowerks\\CCDLAB\\"))
		//			Directory.CreateDirectory("C:\\ProgramData\\Astrowerks\\CCDLAB\\");
		//		result_savepathfilename = "C:\\ProgramData\\Astrowerks\\CCDLAB\\queryCatalog.fit";
		//	}

		//	System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
		//	psi.FileName = pypath;
		//	psi.Arguments = String.Format("\"" + script + "\"" + " {0} {1} {2} {3} {4}", ra_deg, dec_deg, "\"" + result_savepathfilename + "\"", radius, square);

		//	/*psi.UseShellExecute = false;//??????
		//	psi.CreateNoWindow = true;//????
		//	psi.RedirectStandardError = true;
		//	psi.RedirectStandardOutput = true;
		//	string errs = "";
		//	string res = "";*/

		//	System.Diagnostics.Process proc = System.Diagnostics.Process.Start(psi);
		//	proc.WaitForExit();
		//	int res = proc.ExitCode;
		//	if (res != 0)
		//		return res;

		//	/*errs = proc.StandardError.ReadToEnd();
		//	res = proc.StandardOutput.ReadToEnd();
		//	MessageBox.Show(errs + "\r\n" + res);*/

		//	/*array<string>^ ExtensionEntryLabels = FITSBinTable.GetExtensionEntryLabels(result_savepathfilename, "");
		//	array<TypeCode>^ ExtensionEntryDataTypes = FITSBinTable.GetExtensionEntryDataTypes(result_savepathfilename, "");
		//	array<string>^ ExtensionEntryDataUnits = FITSBinTable.GetExtensionEntryUnits(result_savepathfilename, "");
		//	array<double, 2>^ table = FITSBinTable.GetExtensionEntries(result_savepathfilename, "", ExtensionEntryLabels);
		//	FITSImage fits = new FITSImage(result_savepathfilename, null, true, true, false, false);
		//	fits.WriteFile(TypeCode.Double, false);
		//	array<string>^ exkeys = new array<string>(2) { "RA", "DEC" };
		//	array<string>^ exvals = new array<string>(2) { ra_deg.ToString(), dec_deg.ToString() };
		//	array<string>^ excoms = new array<string>(2) { "Right Ascension of query field center, degrees", "Declination of query field center, degrees" };
		//	FITSBinTable.WriteExtension(result_savepathfilename, "", true, ExtensionEntryLabels, ExtensionEntryDataTypes, ExtensionEntryDataUnits, exkeys, exvals, excoms, table);*/

		//	return res;
		//}

		///// <summary>Queries the Gaia catalogue for entries within a specified region. Returns 0 if the query was successful.</summary>
		///// <param name="ra_deg">A string of the right ascension in degrees.</param>
		///// <param name="dec_deg">A string of the declination in degrees.</param>
		///// <param name="result_savepathfilename">The filename to save the catalogue query FITS binary table.</param>
		///// <param name="radius">A string of the region radius in arcminutes.</param>
		///// <param name="square">Pass 1 if the region is square, 0 for circle.</param>
		///// <param name="N">Number of entries for the region to return.</param>
		///// <param name="sortfilter">The Gaia filter to sort the return by: bp, g, or rp.</param>
		//public static int GaiaDR3QueryN(string ra_deg, string dec_deg, string result_savepathfilename, string radius, string square, string N, string sortfilter)
		//{
		//	string pypath = (string)REG.GetReg("CCDLAB", "PythonExePath");

		//	if (pypath == null || !File.Exists(pypath))
		//	{
		//		string[] dirsappdata = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "*Python*", SearchOption.AllDirectories);
		//		string[] dirsprogdata = Directory.GetDirectories("C:\\Program Files\\", "*Python*", SearchOption.TopDirectoryOnly);

		//		ArrayList locs = new ArrayList();

		//		for (int i = 0; i < dirsappdata.Length; i++)
		//		{
		//			string[] files = Directory.GetFiles(dirsappdata[i], "*python.exe", SearchOption.TopDirectoryOnly);
		//			if (files.Length == 1)
		//				locs.Add(files[0]);
		//		}
		//		for (int i = 0; i < dirsprogdata.Length; i++)
		//		{
		//			string[] files = Directory.GetFiles(dirsprogdata[i], "*python.exe", SearchOption.TopDirectoryOnly);
		//			if (files.Length == 1)
		//				locs.Add(files[0]);
		//		}

		//		if (locs.Count == 0)
		//		{
		//			if (MessageBox.Show("Is Python installed? Please show me where your Python installation is located, OK? \r\n\r\nIf Python is not installed, please gather it from:\r\n\r\n https://www.python.org/downloads/windows/", "I cannot find python.exe", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
		//				return -2;

		//			OpenFileDialog ofd = new OpenFileDialog();
		//			ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		//			ofd.Filter = "Executable|*.exe;";
		//			if (ofd.ShowDialog() == DialogResult.Cancel)
		//				return -2;

		//			pypath = ofd.FileName;
		//		}
		//		else
		//		{
		//			pypath = (string)locs[0];
		//			DateTime date = File.GetCreationTimeUtc((string)locs[0]);
		//			for (int i = 0; i < locs.Count; i++)
		//				if (File.GetCreationTimeUtc((string)locs[i]) > date)
		//				{
		//					date = File.GetCreationTimeUtc((string)locs[i]);
		//					pypath = (string)locs[i];
		//				}
		//		}

		//		REG.SetReg("CCDLAB", "PythonExePath", pypath);
		//	}

		//	if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Astrowerks\\CCDLAB\\"))
		//		Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Astrowerks\\CCDLAB\\");
			
		//	string script = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Astrowerks\\CCDLAB\\GaiaDR3QueryN.py";
		//	MAKEGAIADR3QUERYNSCRIPT(script);

		//	if (result_savepathfilename == "")
		//	{
		//		if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Astrowerks\\CCDLAB\\"))
		//			Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Astrowerks\\CCDLAB\\");
		//		result_savepathfilename = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Astrowerks\\CCDLAB\\GaiaDR3Query.fit";
		//	}

		//	System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
		//	psi.FileName = pypath;
		//	psi.Arguments = String.Format("\"" + script + "\"" + " {0} {1} {2} {3} {4} {5} {6}", ra_deg, dec_deg, "\"" + result_savepathfilename + "\"", radius, square, N, sortfilter);

		//	/*psi.UseShellExecute = false;//??????
		//	psi.CreateNoWindow = true;//????
		//	psi.RedirectStandardError = true;
		//	psi.RedirectStandardOutput = true;*/

		//	System.Diagnostics.Process proc = System.Diagnostics.Process.Start(psi);
		//	proc.WaitForExit();
		//	int res = proc.ExitCode;
		//	if (res != 0)
		//	{
		//		/*string stderr = proc.StandardError.ReadToEnd();
		//		string stdout = proc.StandardOutput.ReadToEnd();
		//		MessageBox.Show(stderr + "\r\n" + stdout);*/
		//		return res;
		//	}

		//	return res;
		//}

		//public static int AstraCarta(double ra_deg, double dec_deg, double scale, int pixwidth, int pixheight, string shape, double buffer, string outdir, string outname, string filter, int nquery, bool showplot, bool forcenew, out string result_savepathfilename)
		//{
		//	result_savepathfilename = "";
		//	string pypath = (string)REG.GetReg("CCDLAB", "PythonExePath");

		//	if (pypath == null || !File.Exists(pypath))
		//	{
		//		string[] dirsappdata = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "*Python*", SearchOption.AllDirectories);
		//		string[] dirsprogdata = Directory.GetDirectories("C:\\Program Files\\", "*Python*", SearchOption.TopDirectoryOnly);

		//		ArrayList locs = new ArrayList();

		//		for (int i = 0; i < dirsappdata.Length; i++)
		//		{
		//			string[] files = Directory.GetFiles(dirsappdata[i], "*python.exe", SearchOption.TopDirectoryOnly);
		//			if (files.Length == 1)
		//				locs.Add(files[0]);
		//		}
		//		for (int i = 0; i < dirsprogdata.Length; i++)
		//		{
		//			string[] files = Directory.GetFiles(dirsprogdata[i], "*python.exe", SearchOption.TopDirectoryOnly);
		//			if (files.Length == 1)
		//				locs.Add(files[0]);
		//		}

		//		if (locs.Count == 0)
		//		{
		//			if (MessageBox.Show("Is Python installed? Please show me where your Python installation is located, OK? \r\n\r\nIf Python is not installed, please gather it from:\r\n\r\n https://www.python.org/downloads/windows/", "I cannot find python.exe", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
		//				return -2;

		//			OpenFileDialog ofd = new OpenFileDialog();
		//			ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		//			ofd.Filter = "Executable|*.exe;";
		//			if (ofd.ShowDialog() == DialogResult.Cancel)
		//				return -2;

		//			pypath = ofd.FileName;
		//		}
		//		else
		//		{
		//			pypath = (string)locs[0];
		//			DateTime date = File.GetCreationTimeUtc((string)locs[0]);
		//			for (int i = 0; i < locs.Count; i++)
		//				if (File.GetCreationTimeUtc((string)locs[i]) > date)
		//				{
		//					date = File.GetCreationTimeUtc((string)locs[i]);
		//					pypath = (string)locs[i];
		//				}
		//		}

		//		REG.SetReg("CCDLAB", "PythonExePath", pypath);
		//	}

		//	string argstring = String.Format("-ra {0} -dec {1} -scale {2} -pixwidth {3} -pixheight {4} -shape {5} -buffer {6} -outdir {7} -outname {8} -filter {9} -nquery {10} -fitsout", ra_deg, dec_deg, scale, pixwidth, pixheight, "\"" + shape + "\"", buffer, "\"" + outdir + "\"", "\"" + outname + "\"", "\"" + filter + "\"", nquery);
		//	if (showplot)
		//		argstring += " -imageshow";
		//	if (forcenew)
		//		argstring += " -forcenew";
		//	System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + "astracarta " + argstring);

		//	psi.UseShellExecute = false;
		//	psi.CreateNoWindow = true;
		//	psi.RedirectStandardError = true;
		//	psi.RedirectStandardOutput = true;
		//	System.Diagnostics.Process proc = System.Diagnostics.Process.Start(psi);
		//	proc.WaitForExit();
		//	int res = proc.ExitCode;

		//	string stdout = proc.StandardOutput.ReadToEnd().Trim();
		//	string stderr = proc.StandardError.ReadToEnd().Trim();

		//	if (stderr != "")
		//	{
		//		MessageBox.Show(stderr + "\r\n\r\n" + stdout, "Error...");
		//		return res;
		//	}

		//	result_savepathfilename = stdout;

		//	int c = 0;
		//	string test = result_savepathfilename;
		//	while (!File.Exists(test))
		//	{
		//		c++;
		//		test = result_savepathfilename.Substring(c);
		//	}
		//	result_savepathfilename = test;

		//	return res;
		//}

		#endregion
	}
}
