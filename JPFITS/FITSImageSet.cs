using System;
using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
#nullable enable

namespace JPFITS
{
	/// <summary>FITSImageSet class is an ArrayList object to hold, manage, and perform operations on a set of FITSImage objects.</summary>
	public class FITSImageSet
	{
		#region PRIVATE
		private ArrayList FITSLIST;
		private bool CODIMENSIONAL;
		private static WaitBar? WAITBAR;
		private static BackgroundWorker? BGWRKR;
		private static object? BGWRKR_RESULT;

		private void CHECK_CODIMENSIONAL()
		{
			CODIMENSIONAL = true;
			for (int i = 1; i < FITSLIST.Count; i++)
				if (((FITSImage)(FITSLIST[0])).Width != ((FITSImage)(FITSLIST[i])).Width || ((FITSImage)(FITSLIST[0])).Height != ((FITSImage)(FITSLIST[i])).Height)
					CODIMENSIONAL = false;
		}		

		private void BGWRKR_DoWork(object sender, DoWorkEventArgs e)
		{
			object[] arg = (object[])e.Argument;
			string op = arg[1].ToString();

			if (op == "save")
			{
				bool do_parallel = (bool)(arg[2]);
				TypeCode precision = (TypeCode)arg[3];
				string waitbar_message = (string)arg[4];
				object countlock = new object();

				ParallelOptions opts = new ParallelOptions();
				if (do_parallel)
					opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
				else
					opts.MaxDegreeOfParallelism = 1;
				var rangePartitioner = Partitioner.Create(0, FITSLIST.Count);
				int count = 0;
				int prog = 0;

				Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
				{
					if (WAITBAR.DialogResult == DialogResult.Cancel)
						loopState.Stop();

					for (int i = range.Item1; i < range.Item2; i++)
					{
						if (WAITBAR.DialogResult == DialogResult.Cancel)
							break;

						Interlocked.Increment(ref count);

						lock (WAITBAR)
						{
							if (count * 100 / FITSLIST.Count > prog)
							{
								prog = count * 100 / FITSLIST.Count;
								BGWRKR.ReportProgress(count, "save " + waitbar_message);
							}
						}

						((FITSImage)FITSLIST[i]).WriteImage(precision, !do_parallel);
					}

				});
				return;
			}

			if (op == "load")
			{
				string[] files = (string[])arg[0];
				bool do_stats = (bool)(arg[2]);
				bool do_parallel = (bool)(arg[3]);
				int[] imgrange = (int[])arg[4];
				string waitbar_message = (string)arg[5];

				ParallelOptions opts = new ParallelOptions();
				if (do_parallel)
					opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
				else
					opts.MaxDegreeOfParallelism = 1;
				var rangePartitioner = Partitioner.Create(0, files.Length);
				int count = 0;				
				int prog = 0;

				JPFITS.FITSImage[] set = new JPFITS.FITSImage[files.Length];

				Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
				{
					if (WAITBAR.DialogResult == DialogResult.Cancel)
						loopState.Stop();

					for (int i = range.Item1; i < range.Item2; i++)
					{
						if (WAITBAR.DialogResult == DialogResult.Cancel)
							break;

						Interlocked.Increment(ref count);
						lock (set)
						{
							if (count * 100 / files.Length > prog)
							{
								prog = count * 100 / files.Length;
								BGWRKR.ReportProgress(count, "load " + waitbar_message);
							}
						}

						set[i] = new FITSImage(files[i], imgrange, true, true, do_stats, !do_parallel);
					}
				});

				if (WAITBAR.DialogResult == DialogResult.Cancel)
					return;

				for (int i = 0; i < files.Length; i++)
					FITSLIST.Add(set[i]);
				return;
			}

			if (op.Equals("loadextensions"))
			{
				string file = (string)arg[0];
				bool do_stats = (bool)(arg[2]);
				int[] extensionIndexes = (int[])(arg[3]);
				int[] imgrange = (int[])arg[4];
				string waitbar_message = (string)arg[5];
				
				JPFITS.FITSImage[] set = new JPFITS.FITSImage[extensionIndexes.Length];
				int prog = 0;

				for (int i = 0; i < extensionIndexes.Length; i++)
				{
					if (WAITBAR.DialogResult == DialogResult.Cancel)
						break;

					if (i * 100 / extensionIndexes.Length > prog)
					{
						prog = i * 100 / extensionIndexes.Length;
						BGWRKR.ReportProgress(i, "load " + waitbar_message);
					}

					FITSLIST.Add(new JPFITS.FITSImage(file, extensionIndexes[i], imgrange, true, true, do_stats, true));
				}
				return;
			}

			if (op.Equals("Min"))
			{
				FITSImageSet ImageSet = (FITSImageSet)arg[0];
				double[,] img = new double[ImageSet[0].Width, ImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = ImageSet.Count;
				double N = (double)L;
				int prog = 0;
				int n0 = width / Environment.ProcessorCount;

				Parallel.For(0, width, (i, state) =>
				{
					if (WAITBAR.DialogResult == DialogResult.Cancel)
					{
						e.Result = null;
						state.Stop();
					}

					if (i < n0 && i * 100 / n0 > prog)//keep the update of progress bar to only one thread of the team...avoids locks
					{
						prog = i * 100 / n0;
						BGWRKR.ReportProgress(prog);
					}

					for (int j = 0; j < height; j++)
					{
						double min = Double.MaxValue;
						for (int k = 0; k < L; k++)
							if (ImageSet[k][i, j] < min)
								min = ImageSet[k][i, j];
						img[i, j] = min;
					}
				});

				e.Result = img;
				return;
			}

			if (op.Equals("Max"))
			{
				FITSImageSet ImageSet = (FITSImageSet)arg[0];
				double[,] img = new double[ImageSet[0].Width, ImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = ImageSet.Count;
				double N = (double)L;
				int prog = 0;
				int n0 = width / Environment.ProcessorCount;

				Parallel.For(0, width, (i, state) =>
				{
					if (WAITBAR.DialogResult == DialogResult.Cancel)
					{
						e.Result = null;
						state.Stop();
					}

					if (i < n0 && i * 100 / n0 > prog)//keep the update of progress bar to only one thread of the team...avoids locks
					{
						prog = i * 100 / n0;
						BGWRKR.ReportProgress(prog);
					}

					for (int j = 0; j < height; j++)
					{
						double max = Double.MinValue;
						for (int k = 0; k < L; k++)
							if (ImageSet[k][i, j] > max)
								max = ImageSet[k][i, j];
						img[i, j] = max;
					}
				});

				e.Result = img;
				return;
			}

			if (op.Equals("Mean"))
			{
				FITSImageSet ImageSet = (FITSImageSet)arg[0];
				double[,] img = new double[ImageSet[0].Width, ImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = ImageSet.Count;
				double N = (double)L;
				int prog = 0;
				int n0 = width / Environment.ProcessorCount;

				Parallel.For(0, width, (i, state) =>
				{
					if (WAITBAR.DialogResult == DialogResult.Cancel)
					{
						e.Result = null;
						state.Stop();
					}

					if (i < n0 && i * 100 / n0 > prog)//keep the update of progress bar to only one thread of the team...avoids locks
					{
						prog = i * 100 / n0;
						BGWRKR.ReportProgress(prog);
					}

					for (int j = 0; j < height; j++)
					{
						double mean = 0;
						for (int k = 0; k < L; k++)
							mean = mean + ImageSet[k][i, j];
						img[i, j] = mean / N;
					}
				});

				e.Result = img;
				return;
			}

			if (op.Equals("Sum"))
			{
				FITSImageSet ImageSet = (FITSImageSet)arg[0];
				double[,] img = new double[ImageSet[0].Width, ImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = ImageSet.Count;
				double N = (double)L;
				int prog = 0;
				int n0 = width / Environment.ProcessorCount;

				Parallel.For(0, width, (i, state) =>
				{
					if (WAITBAR.DialogResult == DialogResult.Cancel)
					{
						e.Result = null;
						state.Stop();
					}

					if (i < n0 && i * 100 / n0 > prog)//keep the update of progress bar to only one thread of the team...avoids locks
					{
						prog = i * 100 / n0;
						BGWRKR.ReportProgress(prog);
					}

					for (int j = 0; j < height; j++)
					{
						double sum = 0;
						for (int k = 0; k < L; k++)
							sum = sum + ImageSet[k][i, j];
						img[i, j] = sum;
					}
				});

				e.Result = img;
				return;
			}

			if (op.Equals("Quadrature"))
			{
				FITSImageSet ImageSet = (FITSImageSet)arg[0];
				double[,] img = new double[ImageSet[0].Width, ImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = ImageSet.Count;
				double N = (double)L;
				int prog = 0;
				int n0 = width / Environment.ProcessorCount;

				Parallel.For(0, width, (i, state) =>
				{
					if (WAITBAR.DialogResult == DialogResult.Cancel)
					{
						e.Result = null;
						state.Stop();
					}

					if (i < n0 && i * 100 / n0 > prog)//keep the update of progress bar to only one thread of the team...avoids locks
					{
						prog = i * 100 / n0;
						BGWRKR.ReportProgress(prog);
					}

					for (int j = 0; j < height; j++)
					{
						double quadsum = 0;
						for (int k = 0; k < L; k++)
							quadsum += (ImageSet[k][i, j] * ImageSet[k][i, j]);
						img[i, j] = Math.Sqrt(quadsum);
					}
				});

				e.Result = img;
				return;
			}

			if (op.Equals("Median"))
			{
				FITSImageSet ImageSet = (FITSImageSet)arg[0];
				string waitbar_message = (string)(arg[2]);
				double[,] img = new double[ImageSet[0].Width, ImageSet[0].Height]; ;
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = ImageSet.Count;
				double N = (double)L;
				int prog = 0;
				int n0 = width / Environment.ProcessorCount;

				Parallel.For(0, width, (i, state) =>
				{
					if (WAITBAR.DialogResult == DialogResult.Cancel)
					{
						e.Result = null;
						state.Stop();
					}

					if (i < n0 && i * 100 / n0 > prog)//keep the update of progress bar to only one thread of the team...avoids locks
					{
						prog = i * 100 / n0;
						BGWRKR.ReportProgress(prog);
					}

					for (int j = 0; j < height; j++)
					{
						double[] medarray = new double[L];
						for (int k = 0; k < L; k++)
							medarray[k] = ImageSet[k][i, j];
						img[i, j] = JPMath.Median(medarray);
					}
				});

				e.Result = img;
				return;
			}

			if (op.Equals("Stdv"))
			{
				FITSImageSet ImageSet = (FITSImageSet)arg[0];
				double[,] img = new double[ImageSet[0].Width, ImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = ImageSet.Count;
				double N = (double)L;
				int prog = 0;
				int n0 = width / Environment.ProcessorCount;

				Parallel.For(0, width, (i, state) =>
				{
					if (WAITBAR.DialogResult == DialogResult.Cancel)
					{
						e.Result = null;
						state.Stop();
					}

					if (i < n0 && i * 100 / n0 > prog)//keep the update of progress bar to only one thread of the team...avoids locks
					{
						prog = i * 100 / n0;
						BGWRKR.ReportProgress(prog);
					}

					for (int j = 0; j < height; j++)
					{
						double std = 0;
						double mean = 0;
						for (int k = 0; k < L; k++)
							mean = mean + ImageSet[k][i, j];
						mean = mean / N;
						for (int q = 0; q < L; q++)
							std = std + (ImageSet[q][i, j] - mean) * (ImageSet[q][i, j] - mean);
						std = Math.Sqrt(std / (N - 1.0));
						img[i, j] = std;
					}
				});

				e.Result = img;
				return;
			}

			if (op.Equals("AutoReg"))
			{
				FITSImageSet ImageSet = (FITSImageSet)arg[0];
				double[,] img = new double[ImageSet[0].Width, ImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = ImageSet.Count;
				double N = (double)L;

				int RefImgIndex = (int)arg[2];
				bool Do_Stats = (bool)arg[3];
				string style = (string)arg[4];

				double[,] refim = new double[ImageSet[RefImgIndex].Width, ImageSet[RefImgIndex].Height];
				Array.Copy(ImageSet[RefImgIndex].Image, refim, ImageSet[RefImgIndex].Image.LongLength);

				refim = JPMath.DeGradient(refim, 0, true);
				refim = JPMath.DeGradient(refim, 1, true);
				refim = JPMath.Hanning(refim, true);
				double[] Href = JPMath.Sum(refim, 1, true);
				double[] Vref = JPMath.Sum(refim, 0, true);
				Href = JPMath.VectorSubScalar(Href, JPMath.Mean(Href, false), true);
				Vref = JPMath.VectorSubScalar(Vref, JPMath.Mean(Vref, false), true);

				for (int c = 0; c < ImageSet.Count; c++)//create the array with the missing reference index
				{
					BGWRKR.ReportProgress(c * 100 / (ImageSet.Count - 1));

					if (WAITBAR.DialogResult == DialogResult.Cancel)
						continue;
					if (c == RefImgIndex)
						continue;//don't register to ones' self

					JPMath.XCorrImageLagShifts(Href, Vref, ImageSet[c].Image, true, true, true, out double xshift, out double yshift, true);

					ImageSet[c].SetImage(JPMath.RotateShiftArray(ImageSet[c].Image, 0, Double.MaxValue, Double.MaxValue, style, -xshift, -yshift, true), true, true);
                }
				return;
			}

			if (op.Equals("SCM"))
			{
				//FITSImageSet ImageSet = (FITSImageSet)arg[0];
				//double sigma = (double)arg[2];
				//double[,] result = new double[ImageSet[0].Width, ImageSet[0].Height];

				//ParallelOptions opts = new ParallelOptions();
				//opts.MaxDegreeOfParallelism = Environment.ProcessorCount;

				//Parallel.For(0, ImageSet[0].Width, opts, (x, state) =>
				//{
				//	if (WAITBAR.DialogResult == DialogResult.Cancel)
				//		state.Stop();

				//	if (x < ImageSet[0].Width / Environment.ProcessorCount)
				//	{
				//		//BGWRKR.ReportProgress(waitbar_count + 1, String.Concat("Iteration: ", iteration_count + 1, ". # of Offending Points: ", xinds.Length));
				//	}

				//	double[] pixstack = new double[ImageSet.Count];
				//	double stdv;

				//	for (int y = 0; y < ImageSet[0].Height; y++)
				//	{
				//		for (int z = 0; z < ImageSet.Count; z++)
				//			pixstack[z] = ImageSet[z][x, y];

				//		while (true)
				//		{
				//			for (int z = 0; z < ImageSet.Count; z++)
				//				result[x, y] += pixstack[z];

				//			result[x, y] /= (double)ImageSet.Count;
				//			stdv = JPMath.Stdv(pixstack, result[x, y], false);

				//			int zoffind = -1;
				//			double maxoffend = 0;

				//			for (int z = 0; z < ImageSet.Count; z++)
				//			{
				//				double delta = Math.Abs(pixstack[z] - result[x, y]);
				//				if (delta > sigma * stdv && delta > maxoffend)
				//				{
				//					zoffind = z;
				//					maxoffend = delta;

				//					//MessageBox.Show(x + " " + y + " " + stdv + " " + (sigma * stdv) + " " + pixstack[z] + " " + result[x, y] + " " + delta);
				//				}
				//			}

				//			if (zoffind >= 0)
				//				pixstack[zoffind] = JPMath.Median(pixstack);
				//			else
				//				break;

				//			result[x, y] = 0;
				//		}
				//	}
				//});

				//e.Result = result;
				//return;




























				FITSImageSet ImageSet = (FITSImageSet)arg[0];
				double[,] meanimg = new double[ImageSet[0].Width, ImageSet[0].Height];
				int width = meanimg.GetLength(0);
				int height = meanimg.GetLength(1);
				int L = ImageSet.Count;
				double N = (double)L;

				double sigma = (double)arg[2];
				double[,] stdvimg = (double[,])arg[3];
				meanimg = (double[,])arg[4];

				int iteration_count = 0;
				int waitbar_count = 0;

				int nptsrepeat1 = 0;
				int nptsrepeat2 = 0;

				while (true)
				{
					if (WAITBAR.DialogResult == DialogResult.Cancel)
						break;

					double std = JPMath.Stdv(stdvimg, true);
					double mean = JPMath.Mean(stdvimg, true);
					JPMath.Find(stdvimg, mean + sigma * std, ">", true, out int[] xinds, out int[] yinds);//find pts > clip range

					if (xinds.Length == 0)
						break;

					BGWRKR.ReportProgress(waitbar_count + 1, String.Concat("Iteration: ", iteration_count + 1, ". # of Offending Points: ", xinds.Length));

					if (xinds.Length != 0)//then do some clippin!
					{
						if (nptsrepeat1 != xinds.Length)
							nptsrepeat1 = xinds.Length;
						else
							nptsrepeat2++;

						double med;
						int count = 0;//breakout counter if too high
						double[] clipvec = new double[L];
						double max;

						for (int c = 0; c < xinds.Length; c++)
						{
							for (int i = 0; i < L; i++)
								clipvec[i] = ImageSet[i][xinds[c], yinds[c]];

							while (true)
							{
								med = JPMath.Median(clipvec);
								max = JPMath.Max(JPMath.Abs(JPMath.VectorSubScalar(clipvec, med, false), false), out int index, false);
								if (max > sigma * std)
								{
									clipvec[index] = med;
									meanimg[xinds[c], yinds[c]] = JPMath.Mean(clipvec, true);
									stdvimg[xinds[c], yinds[c]] = JPMath.Stdv(clipvec, true);
								}
								else
									break;

								if (++count > 3 * xinds.Length)//?arbitrary? number of iterations limit
									break;//and go to next point
							}
						}
					}
					else
						break;

					if (++iteration_count > 2000 || nptsrepeat2 > 5)
						break;

					if (++waitbar_count >= 100)
						waitbar_count = 0;
				}

				e.Result = meanimg;
				return;
			}
		}
		
		private void BGWRKR_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			WAITBAR.ProgressBar.Value = e.ProgressPercentage;

			if (e.UserState == null)
			{
				WAITBAR.TextMsg.Text = e.ProgressPercentage + "%";
			}
			else if (((string)e.UserState).Substring(0, 4) == "load")
			{
				WAITBAR.TextMsg.Text = e.ProgressPercentage + " " + ((string)e.UserState).Substring(4);
			}
			else if (((string)e.UserState).Substring(0, 4) == "save")
			{
				WAITBAR.TextMsg.Text = e.ProgressPercentage + " " + ((string)e.UserState).Substring(4);
			}
			else
			{
				WAITBAR.TextMsg.Text = e.ProgressPercentage + "% " + (string)e.UserState;
			}
			WAITBAR.Refresh();
		}
		
		private void BGWRKR_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			BGWRKR_RESULT = e.Result;
			WAITBAR.DialogResult = DialogResult.OK;
			WAITBAR.Close();
		}

		#endregion

		#region CONSTRUCTORS

		/// <summary>Constructor. Images can be added via Add, or Load, etc.</summary>
		public FITSImageSet()
		{
			FITSLIST = new ArrayList();
			CODIMENSIONAL = true;
			BGWRKR = new BackgroundWorker();
			BGWRKR.WorkerReportsProgress = true;
			BGWRKR.WorkerSupportsCancellation = true;
			BGWRKR.DoWork += new DoWorkEventHandler(BGWRKR_DoWork);
			BGWRKR.ProgressChanged += new ProgressChangedEventHandler(BGWRKR_ProgressChanged);
			BGWRKR.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGWRKR_RunWorkerCompleted);
		}

		/// <summary>
		/// Constructor with a list of file names, and file reading settings as with the Load function. Displays a cancellable WaitBar when loading the files.
		/// </summary>
		/// <param name="fullFileNames">The full path list of files to load into the set.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax].  Pass null or Range[0] = -1 to default to full image size.</param>
		/// <param name="doStats">Determine stats for each FITS object when loaded.</param>
		/// <param name="diskParallel">Load the FITS files in parallel off of disk. Requires high-performance hard disk.</param>
		/// <param name="waitbarMessage">Message to display on Waitbar progress if it is shown.</param>
		public FITSImageSet(string[] fullFileNames, int[]? range, bool doStats, bool diskParallel, string waitbarMessage)
		{
			FITSLIST = new ArrayList();
			CODIMENSIONAL = true;
			BGWRKR = new BackgroundWorker();
			BGWRKR.WorkerReportsProgress = true;
			BGWRKR.WorkerSupportsCancellation = true;
			BGWRKR.DoWork += new DoWorkEventHandler(BGWRKR_DoWork);
			BGWRKR.ProgressChanged += new ProgressChangedEventHandler(BGWRKR_ProgressChanged);
			BGWRKR.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGWRKR_RunWorkerCompleted);

			this.Load(fullFileNames, range, doStats, diskParallel, waitbarMessage);
		}

		/// <summary>
		/// Constructor with a list of file names, and file reading settings as with the Load function.
		/// </summary>
		/// <param name="fullFileNames">The full path list of files to load into the set.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax].  Pass null or Range[0] = -1 to default to full image size.</param>
		/// <param name="doStats">Determine stats for each FITS object when loaded.</param>
		/// <param name="diskParallel">Load the FITS files in parallel off of disk. Requires high-performance hard disk.</param>
		public FITSImageSet(string[] fullFileNames, int[]? range, bool doStats, bool diskParallel)
		{
			FITSLIST = new ArrayList();
			CODIMENSIONAL = true;
			BGWRKR = new BackgroundWorker();
			BGWRKR.WorkerReportsProgress = true;
			BGWRKR.WorkerSupportsCancellation = true;
			BGWRKR.DoWork += new DoWorkEventHandler(BGWRKR_DoWork);
			BGWRKR.ProgressChanged += new ProgressChangedEventHandler(BGWRKR_ProgressChanged);
			BGWRKR.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGWRKR_RunWorkerCompleted);

			this.Load(fullFileNames, range, doStats, diskParallel);
		}

		#endregion

		#region PROPERTIES
		/// <summary>FITSImageSet indexer accesses the FITSImage object in the FITSImageSet at a given index, i.e. FITSImage f = FITSImageSet[i].</summary>
		public FITSImage this[int i]
		{
			get { return ((FITSImage)(FITSLIST[i])); }
			set { FITSLIST[i] = value; }
		}

		/// <summary>Returns the number of FITSImage objects currently held within the FITSImageSet.</summary>
		public int Count
		{
			get { return FITSLIST.Count; }
		}

		/// <summary>Returns whether all primary images in the current FITSImageSet have the same dimension.</summary>
		public bool CoDimensional
		{
			get { CHECK_CODIMENSIONAL(); return CODIMENSIONAL; }
		}

		/// <summary>Returns a String array of the full file names (path + file name) of all FITSImage objects in the current FITSImageSet.</summary>
		public string[] FullFileNames
		{
			get
			{
				string[] names = new string[FITSLIST.Count];
				for (int i = 0; i < FITSLIST.Count; i++)
					names[i] = ((FITSImage)(FITSLIST[i])).FullFileName;

				return names;
			}
		}

		/// <summary>Returns a String array of the file names (excluding file path) of all FITSImage objects in the current FITSImageSet.</summary>
		public string[] FileNames
		{
			get
			{
				string[] names = new string[FITSLIST.Count];
				for (int i = 0; i < FITSLIST.Count; i++)
					names[i] = ((FITSImage)(FITSLIST[i])).FileName;

				return names;
			}
		}

		/// <summary>
		/// Getter returns a String array of the file paths (excluding file names) of all FITSImage objects in the current FITSImageSet. If Setting, argument can be either a 1-element array, which therefore changes all file paths to the same value, or an array the same length as the number of members to set each individually. User is responsible for managing possibility of identical full file names in the members.
		/// </summary>
		public string[] FilePaths
		{
			get
			{
				string[] paths = new string[FITSLIST.Count];
				for (int i = 0; i < FITSLIST.Count; i++)
					paths[i] = ((FITSImage)(FITSLIST[i])).FilePath;

				return paths;
			}

			set
			{
				if (value.Length == 1)
					for (int i = 0; i < FITSLIST.Count; i++)
						((FITSImage)FITSLIST[i]).FilePath = value[0];
				else
					for (int i = 0; i < FITSLIST.Count; i++)
						((FITSImage)FITSLIST[i]).FilePath = value[i];
			}
		}
		#endregion

		#region FILEIO
		/// <summary>Loads FITS objects into the FITSImageSet with a cancellable WaitBar. If the FITSImageSet already has members (not previously cleared), then the new memers are added (appended) to this FITSImageSet.</summary>
		/// <param name="fullFileNames">The full path list of files to load into the set.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax].  Pass null or Range[0] = -1 to default to full image size.</param>
		/// <param name="doStats">Determine stats for each FITS object when loaded.</param>
		/// <param name="diskParallel">True to load the FITS files with parallelized requests to disk, false for serial disk access. True requires high-performance hard disk.</param>
		/// <param name="waitbarMessage">Message to display on Waitbar progress.</param>
		public bool Load(string[] fullFileNames, int[]? range, bool doStats, bool diskParallel, string waitbarMessage)
		{
			WAITBAR = new WaitBar();
			WAITBAR.ProgressBar.Maximum = fullFileNames.Length;
			WAITBAR.Text = "Loading Image Set: " + fullFileNames.Length + " files...";
			object[] arg = new object[6];
			arg[0] = fullFileNames;
			arg[1] = "load";
			arg[2] = doStats;
			arg[3] = diskParallel;
			arg[4] = range;
			arg[5] = waitbarMessage;
			BGWRKR.RunWorkerAsync(arg);
			WAITBAR.ShowDialog();
			if (WAITBAR.DialogResult == DialogResult.Cancel)
				return false;
			else
				return true;
		}

		/// <summary>Loads FITS objects into the FITSImageSet. If the FITSImageSet already has members (not previously cleared), then the new memers are added (appended) to this FITSImageSet.</summary>
		/// <param name="fullFileNames">The full path list of files to load into the set.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax].  Pass null or Range[0] = -1 to default to full image size.</param>
		/// <param name="doStats">Determine stats for each FITS image.</param>
		/// <param name="diskParallel">True to load the FITS files with parallelized requests to disk, false for serial disk access. True requires high-performance hard disk.</param>
		public void Load(string[] fullFileNames, int[]? range, bool doStats, bool diskParallel)
		{
			ParallelOptions opts = new ParallelOptions();
			if (diskParallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			JPFITS.FITSImage[] set = new JPFITS.FITSImage[fullFileNames.Length];

			Parallel.For(0, fullFileNames.Length, opts, i =>
			{
				set[i] = new FITSImage(fullFileNames[i], range, true, true, doStats, !diskParallel);
			});

			for (int i = 0; i < fullFileNames.Length; i++)
				FITSLIST.Add(set[i]);
		}

		/// <summary>
		/// Load FITSImage from the extensions in the FITS file as a FITSImageSet.
		/// </summary>
		/// <param name="fullFileName">The full path file name of the FITS file containing the IMAGE extensions.</param>
		/// <param name="extensionIndexes">The indices of the extensions to read.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax].  Pass null or Range[0] = -1 to default to full image size.</param>
		/// <param name="doStats">>Determine stats for each FITS image.</param>
		/// <param name="waitbarMessage">Message to display on Waitbar progress.</param>
		/// <returns></returns>
		public bool LoadExtensions(string fullFileName, int[] extensionIndexes, int[]? range, bool doStats, string waitbarMessage)
		{
			WAITBAR = new WaitBar();
			WAITBAR.ProgressBar.Maximum = extensionIndexes.Length;
			WAITBAR.Text = "Loading Image Set from extensions: " + extensionIndexes.Length + " files...";
			WAITBAR.TopMost = true;
			object[] arg = new object[6];
			arg[0] = fullFileName;
			arg[1] = "loadextensions";
			arg[2] = doStats;
			arg[3] = extensionIndexes;
			arg[4] = range;
			arg[5] = waitbarMessage;
			BGWRKR.RunWorkerAsync(arg);
			WAITBAR.ShowDialog();
			if (WAITBAR.DialogResult == DialogResult.Cancel)
				return false;
			else
				return true;
		}

		/// <summary>
		/// Load FITSImage from the extensions in the FITS file as a FITSImageSet.
		/// </summary>
		/// <param name="fullFileName">The full path file name of the FITS file containing the IMAGE extensions.</param>
		/// <param name="extensionIndexes">The indices of the extensions to read.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax].  Pass null or Range[0] = -1 to default to full image size.</param>
		/// <param name="doStats">>Determine stats for each FITS image.</param>
		public void LoadExtensions(string fullFileName, int[] extensionIndexes, int[]? range, bool doStats)
		{
			for (int i = 0; i < extensionIndexes.Length; i++)
				FITSLIST.Add(new JPFITS.FITSImage(fullFileName, extensionIndexes[i], range, true, true, doStats, true));
		}

		/// <summary>Write the FITSImage objects from the FITSImageSet to disk.</summary>
		/// <param name="precision">The precision at which to write the image data.</param>
		/// <param name="doParallel">Write the images with parallelism. In the past with platter drives this would have been impossible, but fast solid state drives can handle it. If there's only a few images then don't bother, but useful when writing hundreds.</param>
		/// <param name="waitbarMessage">Message to display on Waitbar progress if it is shown.</param>
		public bool Write(TypeCode precision, bool doParallel, string waitbarMessage)
		{			
			WAITBAR = new WaitBar();
			WAITBAR.ProgressBar.Maximum = FITSLIST.Count;
			WAITBAR.Text = "Saving Image Set: " + FITSLIST.Count + " files...";
			object[] arg = new object[5];
			arg[0] = "";
			arg[1] = "save";
			arg[2] = doParallel;
			arg[3] = precision;
			arg[4] = waitbarMessage;
			BGWRKR.RunWorkerAsync(arg);
			WAITBAR.ShowDialog();
			if (WAITBAR.DialogResult == DialogResult.Cancel)
				return false;
			else
				return true;
		}

		/// <summary>Write the FITSImage objects from the FITSImageSet to disk.</summary>
		/// <param name="precision">The precision at which to write the image data.</param>
		/// <param name="doParallel">Write the images with parallelism. In the past with platter drives this would have been impossible, but fast solid state drives can handle it. If there's only a few images then don't bother, but useful when writing hundreds.</param>
		public void Write(TypeCode precision, bool doParallel)
		{
			ParallelOptions opts = new ParallelOptions();
			if (doParallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, FITSLIST.Count, opts, i =>
			{
				((FITSImage)FITSLIST[i]).WriteImage(precision, !doParallel);
			});
		}

		/// <summary>Write the FITSImage objects from the FITSImageSet as extensions.</summary>
		/// <param name="fileName">The file name to write to.</param>
		/// <param name="appendToExistingFile">Option to write extensions into existing FITS file. Throws an exception if firstAsPrimary is true. If the file doesn't exist, will create a new file with the logic for the following parameters.</param>
		/// <param name="firstAsPrimary">Option to write the first image in the set as the primary data block, otherwise all images to be written as extensions. If true, will overwrite any existing file.</param>
		/// <param name="primaryHeader">If the first image is not to be written as the primary data block, then a header may be supplied for the primary block. Pass null for default header. Throws an exception if firstAsPrimary is true and primaryHeader is not null.</param>
		/// <param name="extensionNames">The names of the extensions. No elements may be empty strings; all elements must be unique. Pass null for automatic incremenetal naming as number ######.</param>
		/// <param name="imagePrecisions">The precisions at which to write the image data. If a single element array is passed then this precision is applied to all images.</param>
		public bool WriteAsExtensions(string fileName, bool appendToExistingFile, bool firstAsPrimary, JPFITS.FITSHeader? primaryHeader, string[]? extensionNames, TypeCode[] imagePrecisions)
		{
			if (firstAsPrimary)
				if (appendToExistingFile)
					throw new Exception("Specified to append extensions to existing file, but firstAsPrimary was true indicating a new file should be created or overwritten.");
				else if (primaryHeader != null)
					throw new Exception("Requested first image to be written as the primary block, but supplied primaryHeader as if all images are extensions. primaryHeader may only be supplied when all images in the set are to be written as extensions. Pass null for primaryHeader when the first image and its header is to be written as the primary unit.");

			bool extnameWASnull = extensionNames == null;
			if (extensionNames == null)
			{
				extensionNames = new string[this.Count];

				for (int i = 0; i < extensionNames.Length; i++)
					extensionNames[i] = "EXT_" + (i + 1).ToString("000000");
			}
			else
				if (extensionNames.Length != this.Count)
					throw new Exception("The number of images (" + this.Count + ") is not equal to the number of extensionNames (" + extensionNames.Length + ")");

			if (!extnameWASnull)
			{
				for (int i = 0; i < extensionNames.Length - 1; i++)
					for (int j = i + 1; j < extensionNames.Length; j++)
						if (extensionNames[i] == extensionNames[j])
							throw new Exception("Extension names extensionNames are not all unique: element " + (i + 1) + " is equal to element " + (j + 1) + " '" + extensionNames[i] + "'");

				for (int i = 0; i < extensionNames.Length; i++)
					if (i == 0 && firstAsPrimary)
						continue;
					else if (extensionNames[i] == "")
						throw new Exception("Extension name at element " + (i + 1) + " is an empty String. Please use good practice and name your extensions...");
			}

			if (imagePrecisions.Length == 1 && this.Count > 1)
			{
				TypeCode tc = imagePrecisions[0];
				imagePrecisions = new TypeCode[this.Count];
				for (int i = 0; i < this.Count; i++)
					imagePrecisions[i] = tc;
			}

			if (!firstAsPrimary && primaryHeader == null)
				primaryHeader = new JPFITS.FITSHeader(true, null);

			if (firstAsPrimary)
			{
				if (File.Exists(fileName))
					File.Delete(fileName);

				if (this.Count > 1)
					this[0].Header.SetKey("EXTEND", "T", "File may contain extensions", true, 7);
				this[0].WriteImage(fileName, imagePrecisions[0], true);
			}

			FileStream fs;
			if (firstAsPrimary || appendToExistingFile)
				fs = new FileStream(fileName, FileMode.Append, FileAccess.Write);
			else
				fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

			string[] head;
			byte[] writedata;

			if (!firstAsPrimary)
			{
				primaryHeader.SetKey("EXTEND", "T", "File may contain extensions", true, 7);//do this in case where primary header was supplied, but might not have had EXTEND keyword

				head = primaryHeader.GetFormattedHeaderBlock(FITSHeader.HeaderUnitType.Primary, false);
				writedata = new byte[head.Length * 80];
				for (int i = 0; i < head.Length; i++)
					for (int j = 0; j < 80; j++)
						writedata[i * 80 + j] = (byte)head[i][j];

				fs.Write(writedata, 0, writedata.Length);
			}

			for (int c = 0; c < this.Count; c++)
			{
				if (c == 0 && firstAsPrimary)
					continue;

				this[c].Header.SetKey("EXTNAME", extensionNames[c], "Name of this Extension", true, 7);

				head = this[c].Header.GetFormattedHeaderBlock(FITSHeader.HeaderUnitType.ExtensionIMAGE, false);
				writedata = new byte[head.Length * 80];
				for (int i = 0; i < head.Length; i++)
					for (int j = 0; j < 80; j++)
						writedata[i * 80 + j] = (byte)head[i][j];
				fs.Write(writedata, 0, writedata.Length);

				writedata = FITSFILEOPS.GetByteFormattedImageDataUnit(imagePrecisions[c], true, this[c].Image);
				fs.Write(writedata, 0, writedata.Length);
			}
			fs.Close();

			return true;
		}

		///// <summary>Write the FITSImage objects from the FITSImageSet as layers of a 3D image cube.</summary>
		///// <param name="fileName">The file name to write to.</param>		
		///// <param name="primaryHeader">Pass null for default header which is the header will all identical key values over all headers in the current FITSImageSet.</param>
		///// <param name="primaryPrecision">The precision at which to write the primary data block.</param>
		//public bool WriteAsImageLayerCube(string fileName, JPFITS.FITSHeader? primaryHeader, TypeCode primaryPrecision)
		//{
		//	if (primaryHeader == null)
		//		primaryHeader = new JPFITS.FITSHeader(true, this, true);

		//	string[] phead = primaryHeader.GetFormattedHeaderBlock(false, false);

		//	for (int z = 0; z < this.Count; z++)
		//	{

		//	}

		//	return false;
		//}

		#endregion

		#region INSTANCE MEMBERS
		/// <summary>Appends a FITSImage object to the ArrayList FITSImageSet object.</summary>
		public void Add(FITSImage FITS)
		{
			FITSLIST.Add(FITS);
			CHECK_CODIMENSIONAL();
		}

		/// <summary>Inserts a FITSImage object to the ArrayList FITSImageSet object at a given index.
		/// <br />If index is larger than the FITSImageSet count, the FITS object will be appended to the end.<br /></summary>
		public void AddAt(int index, FITSImage FITS)
		{
			if (index >= FITSLIST.Count)
				FITSLIST.Add(FITS);
			else
				FITSLIST.Insert(index, FITS);
			CHECK_CODIMENSIONAL();
		}

		/// <summary>Removes the FITSImage object at index from the FITSImageSet.
		/// <br />If index is beyond the set size, nothing happens.</summary>
		public void RemoveAt(int index)
		{
			if (index < FITSLIST.Count)
			{
				FITSLIST.RemoveAt(index);
				FITSLIST.TrimToSize();
			}
		}

		/// <summary>Removes the FITSImage objects starting at index from the FITSImageSet.
		/// <br />If index is beyond the set size, nothing happens.</summary>
		public void RemoveFrom(int index)
		{
			if (index < FITSLIST.Count)
			{
				FITSLIST.RemoveRange(index, FITSLIST.Count - index);
				FITSLIST.TrimToSize();
			}
		}

		/// <summary>Removes the count range of FITSImage objects starting at index from the FITSImageSet.
		/// <br />If index is beyond the set size, nothing happens.
		/// <br />If index plus count is beyond the set size, all elements from index are removed.</summary>
		public void RemoveRange(int index, int count)
		{
			if (index < FITSLIST.Count && index + count <= FITSLIST.Count)
			{
				FITSLIST.RemoveRange(index, count);
				FITSLIST.TrimToSize();
				return;
			}

			if (index < FITSLIST.Count && index + count > FITSLIST.Count)
			{
				FITSLIST.RemoveRange(index, FITSLIST.Count - index);
				FITSLIST.TrimToSize();
			}
		}

		/// <summary>
		/// Removes a series of FITSImage indexed entries in the FITSImageSet
		/// </summary>
		/// <param name="indices">The indexes of the items to remove.</param>
		public void RemoveRange(int[] indices)
		{
			for (int i = indices.Length - 1; i >= 0; i--)
				FITSLIST.RemoveAt(indices[i]);

			FITSLIST.TrimToSize();
		}

		/// <summary>Gets the common directory of the FITSImage objects in the FITSImageSet based on their file paths.</summary>
		public string GetCommonDirectory()
		{
			return FITSImageSet.GetCommonDirectory(this.FilePaths);
		}		

		/// <summary>Clears the ArrayList FITSImageSet object of all members.</summary>
		public void Clear()
		{
			FITSLIST.Clear();
			System.GC.Collect();
		}

		/// <summary>Sort sorts the FITSImageSet list given the key. Returns -1 if there was an error with the sort.</summary>
		/// <param name="headerkey">If key is &quot;filename&quot; then the FITSImageSet list is sorted according to the member file names.
		/// <br /> For example if the file names are alphabetical or numeric then the FITSImageSet list will be sorted by increasing file name.
		/// <br /> Otherwise key is a primary header key and then their corresponding values will be used to sort by increasing value the FITSImageSet list.</param>
		public int Sort(string headerkey)
		{
			if (headerkey == "filename")//filenames are nice because they are always unique
			{
				string[] keys = new string[FITSLIST.Count];

				for (int i = 0; i < FITSLIST.Count; i++)
					keys[i] = ((FITSImage)(FITSLIST[i])).FullFileName;

				Array.Sort(keys);

				for (int i = 0; i < FITSLIST.Count; i++)
				{
					if (keys[i] == ((FITSImage)(FITSLIST[i])).FullFileName)
						continue;

					for (int j = i + 1; j < FITSLIST.Count; j++)
						if (keys[i] == ((FITSImage)(FITSLIST[j])).FullFileName)
						{
							FITSImage tempfits = (FITSImage)FITSLIST[i];
							FITSLIST[i] = FITSLIST[j];
							FITSLIST[j] = tempfits;
						}
				}
				return 0;
			}

			//else use header key value, returned already if for filename
			//key values aren't as nice because they might not always be unique

			//check for either numeric or alphabetical case
			bool numeric = true;
			try
			{
				double d = Convert.ToDouble(((FITSImage)(FITSLIST[0])).Header.GetKeyValue(headerkey));
			}
			catch
			{
				numeric = false;
			}

			if (!numeric)
			{
				string[] keys = new string[FITSLIST.Count];

				bool keycheck = true;

				for (int i = 0; i < FITSLIST.Count; i++)
				{
					keys[i] = ((FITSImage)(FITSLIST[i])).Header.GetKeyValue(headerkey);

					if (keys[i] == "" && keycheck)
					{
						if (MessageBox.Show("Key not found in at least one FITS header: continue?", "Warning", MessageBoxButtons.YesNo) == DialogResult.No)
							return -1;
						keycheck = false;
					}
				}

				Array.Sort(keys);

				for (int i = 0; i < FITSLIST.Count; i++)
				{
					if (keys[i] == ((FITSImage)(FITSLIST[i])).Header.GetKeyValue(headerkey))
						continue;

					for (int j = i + 1; j < FITSLIST.Count; j++)
						if (keys[i] == ((FITSImage)(FITSLIST[j])).Header.GetKeyValue(headerkey))
						{
							FITSImage tempfits = (FITSImage)FITSLIST[i];
							FITSLIST[i] = FITSLIST[j];
							FITSLIST[j] = tempfits;
						}
				}
				return 0;
			}
			else//numeric
			{
				double[] keys = new double[FITSLIST.Count];

				bool keycheck = true;

				for (int i = 0; i < FITSLIST.Count; i++)
				{
					string k = ((FITSImage)(FITSLIST[i])).Header.GetKeyValue(headerkey);

					if (k == "" && keycheck)
					{
						if (MessageBox.Show("Key not found in at least one FITS header: continue?", "Warning", MessageBoxButtons.YesNo) == DialogResult.No)
							return -1;
						keycheck = false;
					}

					try
					{
						keys[i] = Convert.ToDouble(((FITSImage)(FITSLIST[i])).Header.GetKeyValue(headerkey));
					}
					catch
					{
						MessageBox.Show("Tried to convert what had been numeric key values for the sorting keys.  Check FITSImageSet index (1-based) " + (i + 1).ToString() + ".", "Sorting Error");
						return -1;
					}
				}

				Array.Sort(keys);

				for (int i = 0; i < FITSLIST.Count; i++)
				{
					if (keys[i] == Convert.ToDouble(((FITSImage)(FITSLIST[i])).Header.GetKeyValue(headerkey)))
						continue;

					for (int j = i + 1; j < FITSLIST.Count; j++)
						if (keys[i] == Convert.ToDouble(((FITSImage)(FITSLIST[j])).Header.GetKeyValue(headerkey)))
						{
							FITSImage tempfits = (FITSImage)FITSLIST[i];
							FITSLIST[i] = FITSLIST[j];
							FITSLIST[j] = tempfits;
						}
				}
				return 0;
			}
		}
		#endregion

		#region STATIC MEMBERS
		/// <summary>Gets the common directory of a series of file names, based on their file paths.</summary>
		public static string GetCommonDirectory(string[] filelist)
		{
			string first = filelist[0];
			for (int i = 1; i < filelist.Length; i++)
			{
				string second = filelist[i];
				int N = first.Length;
				for (int j = 0; j < second.Length; j++)
				{
					if (j == N || first[j] != second[j])
					{
						first = first.Substring(0, j);
						break;
					}
				}
			}
			if (!Directory.Exists(first))
				first = first.Substring(0, first.LastIndexOf("\\"));

			return first;// + "\\";
		}

		/// <summary>Create a FITSImage object with primary image that is the pixel-wise mean of the FITSImageSet primary images.</summary>
		/// <param name="fitsImageSet">The FITSImageSet object.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and stdv of the FITSImage result - saves time if you don't need those.</param>
		/// <param name="waitbar">Optionally compute the function with a cancellable Waitbar. If cancelled, return value is null.</param>
		public static FITSImage Mean(JPFITS.FITSImageSet fitsImageSet, bool doStats, bool waitbar)
		{
			if (fitsImageSet.CoDimensional == false)
				throw new Exception("Can Not Perform Mean: Data Stack not Co-Dimensional");
			if (fitsImageSet.Count <= 1)
				throw new Exception("Can Not Perform Mean: Data Stack Contains One or Fewer Images");

			if (waitbar)
			{
				WAITBAR = new WaitBar();
				WAITBAR.ProgressBar.Maximum = 100;
				WAITBAR.Text = "Computing Mean Data Stack Image";
				object[] arg = new object[] { fitsImageSet, "Mean" };
				BGWRKR.RunWorkerAsync(arg);
				WAITBAR.ShowDialog();
				if (WAITBAR.DialogResult == DialogResult.Cancel)
				{
					WAITBAR.Close();
					return null;
				}

				return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "mean.fits"), (double[,])BGWRKR_RESULT, doStats, true);
			}
			else
			{
				double[,] img = new double[fitsImageSet[0].Width, fitsImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = fitsImageSet.Count;
				double N = (double)L;

				Parallel.For(0, width, i =>
				{
					for (int j = 0; j < height; j++)
					{
						double sum = 0;
						for (int k = 0; k < L; k++)
						{
							sum = sum + fitsImageSet[k][i, j];
						}
						img[i, j] = sum / N;
					}
				});

				return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "mean.fits"), img, doStats, true);
			}
		}

		/// <summary>Create a FITSImage object with primary image that is the pixel-wise sigma-clipped mean of the FITSImageSet primary images.
		/// <br />The computation is iterative and may take a long time in some situations and so a cancellable WaitBar is mandatory.
		/// <br />If the computation is cancelled the function will return with the most recent iteration of the sigma-clipped stack.</summary>
		/// <param name="fitsImageSet">The FITSImageSet object.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and stdv of the FITSImage result - saves time if you don't need those.</param>
		/// <param name="sigma">The maximum standard deviation allowed for each pixel column; values beyond sigma are clipped and replaced with the median of the pixel column.</param>
		public static FITSImage MeanClipped(JPFITS.FITSImageSet fitsImageSet, bool doStats, double sigma)
		{
			if (fitsImageSet.CoDimensional == false)
				throw new Exception("Can Not Perform Mean: Data Stack not Co-Dimensional");
			if (fitsImageSet.Count <= 1)
				throw new Exception("Can Not Perform Mean: Data Stack Contains One or Fewer Images");

			FITSImage f = Stdv(fitsImageSet, false, true);
			if (f == null)
				return null;
			double[,] simg = f.Image;
			f = Mean(fitsImageSet, false, true);
			if (f == null)
				return null;
			double[,] img = f.Image;

			WAITBAR = new WaitBar();
			WAITBAR.ProgressBar.Maximum = 100;
			WAITBAR.Text = "Computing Sigma Clipped Mean Data Stack Image";
			WAITBAR.CancelBtn.Text = "Stop Iterating (Max = 2000)";
			object[] arg = new object[] { fitsImageSet, "SCM", sigma, simg, img };
			BGWRKR.RunWorkerAsync(arg);
			WAITBAR.ShowDialog();
			return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "ClippedMean.fits"), (double[,])BGWRKR_RESULT, doStats, true);
		}

		/// <summary>Create a FITSImage object with primary image that is the pixel-wise median of the FITSImageSet primary images.</summary>
		/// <param name="fitsImageSet">The FITSImageSet object.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and stdv of the FITSImage result - saves time if you don't need those.</param>
		/// <param name="waitbar">Optionally compute the function with a cancellable Waitbar. If cancelled, return value is null.</param>
		/// <param name="waitbarMessage">Message to display on Waitbar progress if it is shown.</param>
		public static FITSImage Median(JPFITS.FITSImageSet fitsImageSet, bool doStats, bool waitbar, string waitbarMessage)
		{
			if (fitsImageSet.CoDimensional == false)
				throw new Exception("Can Not Perform Mean: Data Stack not Co-Dimensional");
			if (fitsImageSet.Count <= 1)
				throw new Exception("Can Not Perform Mean: Data Stack Contains One or Fewer Images");

			if (waitbar)
			{
				WAITBAR = new WaitBar();
				WAITBAR.ProgressBar.Maximum = 100;
				WAITBAR.Text = "Computing Median Data Stack Image";
				object[] arg = new object[] { fitsImageSet, "Median", waitbarMessage };
				BGWRKR.RunWorkerAsync(arg);
				WAITBAR.ShowDialog();
				if (WAITBAR.DialogResult == DialogResult.OK)
				{
					return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "median.fits"), (double[,])BGWRKR_RESULT, doStats, true);
				}
				else
					return null;
			}
			else
			{
				double[,] img = new double[fitsImageSet[0].Width, fitsImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = fitsImageSet.Count;

				Parallel.For(0, width, i =>
				{
					for (int j = 0; j < height; j++)
					{
						double[] medarray = new double[L];
						for (int k = 0; k < L; k++)
							medarray[k] = fitsImageSet[k][i, j];
						img[i, j] = JPMath.Median(medarray);
					}
				});

				return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "median.fits"), img, doStats, true);
			}
		}

		/// <summary>Create a FITSImage object with primary image that is the pixel-wise sum of the FITSImageSet primary images.</summary>
		/// <param name="fitsImageSet">The FITSImageSet object.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and stdv of the FITSImage result - saves time if you don't need those.</param>
		/// <param name="waitbar">Optionally compute the function with a cancellable Waitbar. If cancelled, return value is null.</param>
		public static FITSImage Sum(JPFITS.FITSImageSet fitsImageSet, bool doStats, bool waitbar)
		{
			if (fitsImageSet.CoDimensional == false)
				throw new Exception("Can Not Perform Mean: Data Stack not Co-Dimensional");
			if (fitsImageSet.Count <= 1)
				throw new Exception("Can Not Perform Mean: Data Stack Contains One or Fewer Images");

			if (waitbar)
			{
				WAITBAR = new WaitBar();
				WAITBAR.ProgressBar.Maximum = 100;
				WAITBAR.Text = "Computing Summed Data Stack Image";
				object[] arg = new object[] { fitsImageSet, "Sum" };
				BGWRKR.RunWorkerAsync(arg);
				WAITBAR.ShowDialog();
				if (WAITBAR.DialogResult == DialogResult.OK)
				{
					return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "sum.fits"), (double[,])BGWRKR_RESULT, doStats, true);
				}
				else
					return null;
			}
			else
			{
				double[,] img = new double[fitsImageSet[0].Width, fitsImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = fitsImageSet.Count;

				Parallel.For(0, width, i =>
				{
					for (int j = 0; j < height; j++)
					{
						double sum = 0;
						for (int k = 0; k < L; k++)
							sum = sum + fitsImageSet[k][i, j];
						img[i, j] = sum;
					}
				});

				return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "sum.fits"), img, doStats, true);
			}
		}

		/// <summary>Create a FITSImage object with primary image that is the pixel-wise quadrature sum of the FITSImageSet primary images.</summary>
		/// <param name="fitsImageSet">The FITSImageSet object.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and stdv of the FITSImage result - saves time if you don't need those.</param>
		/// <param name="waitbar">Optionally compute the function with a cancellable Waitbar. If cancelled, return value is null.</param>
		public static FITSImage Quadrature(JPFITS.FITSImageSet fitsImageSet, bool doStats, bool waitbar)
		{
			if (fitsImageSet.CoDimensional == false)
				throw new Exception("Can Not Perform Mean: Data Stack not Co-Dimensional");
			if (fitsImageSet.Count <= 1)
				throw new Exception("Can Not Perform Mean: Data Stack Contains One or Fewer Images");

			if (waitbar)
			{
				WAITBAR = new WaitBar();
				WAITBAR.ProgressBar.Maximum = 100;
				WAITBAR.Text = "Computing Quadrature Summed Data Stack Image";
				object[] arg = new object[] { fitsImageSet, "Quadrature" };
				BGWRKR.RunWorkerAsync(arg);
				WAITBAR.ShowDialog();
				if (WAITBAR.DialogResult == DialogResult.OK)
				{
					return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "quadrature.fits"), (double[,])BGWRKR_RESULT, doStats, true);
				}
				else
					return null;
			}
			else
			{
				double[,] img = new double[fitsImageSet[0].Width, fitsImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = fitsImageSet.Count;

				Parallel.For(0, width, i =>
				{
					for (int j = 0; j < height; j++)
					{
						double quadsum = 0;
						for (int k = 0; k < L; k++)
							quadsum += (fitsImageSet[k][i, j] * fitsImageSet[k][i, j]);
						img[i, j] = Math.Sqrt(quadsum);
					}
				});

				return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "quadrature.fits"), img, doStats, true);
			}
		}

		/// <summary>Create a FITSImage object with primary image that is the pixel-wise maximum of the FITSImageSet primary images.</summary>
		/// <param name="fitsImageSet">The FITSImageSet object.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and stdv of the FITSImage result - saves time if you don't need those.</param>
		/// <param name="waitbar">Optionally compute the function with a cancellable Waitbar. If cancelled, return value is null.</param>
		public static FITSImage Max(JPFITS.FITSImageSet fitsImageSet, bool doStats, bool waitbar)
		{
			if (fitsImageSet.CoDimensional == false)
				throw new Exception("Can Not Perform Mean: Data Stack not Co-Dimensional");
			if (fitsImageSet.Count <= 1)
				throw new Exception("Can Not Perform Mean: Data Stack Contains One or Fewer Images");

			if (waitbar)
			{
				WAITBAR = new WaitBar();
				WAITBAR.ProgressBar.Maximum = 100;
				WAITBAR.Text = "Computing Max Data Stack Image";
				object[] arg = new object[] { fitsImageSet, "Max" };
				BGWRKR.RunWorkerAsync(arg);
				WAITBAR.ShowDialog();
				if (WAITBAR.DialogResult == DialogResult.OK)
				{
					return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "max.fits"), (double[,])BGWRKR_RESULT, doStats, true);
				}
				else
				{
					WAITBAR.Close();
					return null;
				}
			}
			else
			{
				double[,] img = new double[fitsImageSet[0].Width, fitsImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = fitsImageSet.Count;

				Parallel.For(0, width, i =>
				{
					for (int j = 0; j < height; j++)
					{
						double max = Double.MinValue;
						for (int k = 0; k < L; k++)
							if (fitsImageSet[k][i, j] > max)
								max = fitsImageSet[k][i, j];
						img[i, j] = max;
					}
				});

				return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "max.fits"), img, doStats, true);
			}
		}

		/// <summary>Create a FITSImage object with primary image that is the pixel-wise minimum of the FITSImageSet primary images.</summary>
		/// <param name="fitsImageSet">The FITSImageSet object.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and stdv of the FITSImage result - saves time if you don't need those.</param>
		/// <param name="waitbar">Optionally compute the function with a cancellable Waitbar. If cancelled, return value is null.</param>
		public static FITSImage Min(JPFITS.FITSImageSet fitsImageSet, bool doStats, bool waitbar)
		{
			if (fitsImageSet.CoDimensional == false)
				throw new Exception("Can Not Perform Mean: Data Stack not Co-Dimensional");
			if (fitsImageSet.Count <= 1)
				throw new Exception("Can Not Perform Mean: Data Stack Contains One or Fewer Images");

			if (waitbar)
			{
				WAITBAR = new WaitBar();
				WAITBAR.ProgressBar.Maximum = 100;
				WAITBAR.Text = "Computing Min Data Stack Image";
				object[] arg = new object[] { fitsImageSet, "Min" };
				BGWRKR.RunWorkerAsync(arg);
				WAITBAR.ShowDialog();
				if (WAITBAR.DialogResult == DialogResult.OK)
				{
					return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "min.fits"), (double[,])BGWRKR_RESULT, doStats, true);
				}
				else
				{
					WAITBAR.Close();
					return null;
				}
			}
			else
			{
				double[,] img = new double[fitsImageSet[0].Width, fitsImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = fitsImageSet.Count;

				Parallel.For(0, width, i =>
				{
					for (int j = 0; j < height; j++)
					{
						double min = Double.MaxValue;
						for (int k = 0; k < L; k++)
							if (fitsImageSet[k][i, j] < min)
								min = fitsImageSet[k][i, j];
						img[i, j] = min;
					}
				});

				return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "min.fits"), img, doStats, true);
			}
		}

		/// <summary>Create a FITSImage object with primary image that is the pixel-wise standard deviation of the FITSImageSet primary images.</summary>
		/// <param name="fitsImageSet">The FITSImageSet object.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and stdv of the FITSImage result - saves time if you don't need those.</param>
		/// <param name="waitbar">Optionally compute the function with a cancellable Waitbar. If cancelled, return value is null.</param>
		public static FITSImage Stdv(JPFITS.FITSImageSet fitsImageSet, bool doStats, bool waitbar)
		{
			if (fitsImageSet.CoDimensional == false)
				throw new Exception("Can Not Perform Mean: Data Stack not Co-Dimensional");
			if (fitsImageSet.Count <= 1)
				throw new Exception("Can Not Perform Mean: Data Stack Contains One or Fewer Images");

			if (waitbar)
			{
				WAITBAR = new WaitBar();
				WAITBAR.ProgressBar.Maximum = 100;
				WAITBAR.Text = "Computing Stdv Data Stack Image";
				object[] arg = new object[] { fitsImageSet, "Stdv" };
				BGWRKR.RunWorkerAsync(arg);
				WAITBAR.ShowDialog();
				if (WAITBAR.DialogResult == DialogResult.OK)
				{
					return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "stdv.fits"), (double[,])BGWRKR_RESULT, doStats, true);
				}
				else
					return null;
			}
			else
			{
				double[,] img = new double[fitsImageSet[0].Width, fitsImageSet[0].Height];
				int width = img.GetLength(0);
				int height = img.GetLength(1);
				int L = fitsImageSet.Count;
				double N = (double)L;

				Parallel.For(0, width, i =>
				{
					for (int j = 0; j < height; j++)
					{
						double std = 0;
						double mean = 0;
						for (int k = 0; k < L; k++)
							mean = mean + fitsImageSet[k][i, j];
						mean = mean / N;
						for (int q = 0; q < L; q++)
							std = std + (fitsImageSet[q][i, j] - mean) * (fitsImageSet[q][i, j] - mean);
						std = Math.Sqrt(std / (N - 1.0));
						img[i, j] = std;
					}
				});
				return new FITSImage(Path.Combine(fitsImageSet.GetCommonDirectory(), "stdv.fits"), img, doStats, true);
			}
		}

        /// <summary>Auto-register non-rotational primary images from the FITSImageSet. Only works when there is no field rotation in the image set, only translational shifts, and the shifts are less than half of the field.</summary>
        /// <param name="fitsImageSet">The FITSImageSet object.</param>
        /// <param name="refImgIndex">The index in the FitSet list of the reference image to register all the other images to.</param>
        /// <param name="interpStyle">&quot;nearest&quot; - nearest-neighbor pixel, or, &quot;bilinear&quot; - for 2x2 interpolation, or, &quot;lanc_n&quot; - for Lanczos interpolation of order n = 3, 4, 5.</param>
        /// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and stdv of the registered images - saves time if you don't need those.</param>
        public static void Register(JPFITS.FITSImageSet fitsImageSet, int refImgIndex, string interpStyle, bool doStats)
		{
			WAITBAR = new WaitBar();
			WAITBAR.ProgressBar.Maximum = 100;
			WAITBAR.Text = "Auto-Registering Images";
			object[] arg = new object[] { fitsImageSet, "AutoReg", refImgIndex, doStats, interpStyle };
			BGWRKR.RunWorkerAsync(arg);
			WAITBAR.ShowDialog();
		}

		/// <summary>Scans all primary FITS headers in the FITSImageSet for identical lines and copies such lines to the specified FITSImage destination primary header.
		/// <br />Usage is that perhaps you form the mean of the FITSImageSet as a new FITSImage, and this new FITSImage should contain all the primary header
		/// <br /> lines which are identical in the FITSImageSet.
		/// <br /> The existing primary header of the FITS_destination is cleared before the operation, except for essential keywords.</summary>
		public static void GatherHeaders(JPFITS.FITSImageSet fitsImageSet, FITSImage FITS_destination)
		{
			FITS_destination.Header.RemoveAllKeys(FITS_destination.Image);

			bool skip = false;
			for (int j = 0; j < fitsImageSet[0].Header.Length; j++)
			{
				skip = false;
				string key = fitsImageSet[0].Header.GetKeyName(j);
				string val = fitsImageSet[0].Header.GetKeyValue(j);
				string com = fitsImageSet[0].Header.GetKeyComment(j);

				if (!JPFITS.FITSHeader.ValidKeyEdit(key, false))
					continue;

				for (int i = 1; i < fitsImageSet.Count; i++)
					if (fitsImageSet[i].Header.GetKeyIndex(key, val, com) == -1)
					{
						skip = true;
						break;
					}

				if (!skip)
					if (fitsImageSet[0].Header[j].IsCommentKey)
						FITS_destination.Header.AddCommentKeyLine(com, -1);
					else
						FITS_destination.Header.AddKey(key, val, com, -1);
			}
		}

		/// <summary>Scans all primary FITS headers from the file names for identical lines and copies such lines to the specified FITSImage destination primary header.
		/// <br />Usage is that perhaps you form the mean of the FITSImageSet as a new FITSImage, and this new FITSImage should contain all the primary header
		/// <br /> lines which are identical in the file names.
		/// <br />The existing primary header of the FITSImage is cleared before the operation, except for essential keywords.</summary>
		public static void GatherHeaders(string[] filenames, JPFITS.FITSImage FITS_destination)
		{
			JPFITS.FITSImageSet mergeset = new JPFITS.FITSImageSet();
			for (int i = 0; i < filenames.Length; i++)
				mergeset.Add(new JPFITS.FITSImage(filenames[i], null, true, false, false, false));

			JPFITS.FITSImageSet.GatherHeaders(mergeset, FITS_destination);
		}

		/// <summary></summary>
		/// <param name="sourceFullFileName"></param>
		public static FITSImageSet ReadPrimaryImageLayerCubeAsSet(string sourceFullFileName)
		{
			double[] cube = FITSImage.ReadPrimaryNDimensionalData(sourceFullFileName, out int[] axesN);

			if (axesN.Length != 3)
				throw new Exception("File does not contain a data cube: NAXIS = " + axesN.Length);
			if (axesN[3] == 1)
				throw new Exception("File does not contain a data cube: NAXIS3 = 1");

			string destFullFileName = sourceFullFileName.Substring(0, sourceFullFileName.LastIndexOf("\\")) + "\\";

			FITSImageSet set = new FITSImageSet();

			for (int z = 0; z < axesN[2]; z++)//z is each layer of the cube
			{
				double[,] layer = new double[axesN[0], axesN[1]];

				Parallel.For(0, axesN[1], y =>
				{
					for (int x = 0; x < axesN[0]; x++)
						layer[x, y] = cube[z * axesN[1] * axesN[0] + y * axesN[0] + x];
				});

				FITSImage fi = new FITSImage(destFullFileName + z.ToString("000000000") + ".fits", layer, false, true);
				set.Add(fi);
			}

			return set;
		}
		#endregion
	}
}

