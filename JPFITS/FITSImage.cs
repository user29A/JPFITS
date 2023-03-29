using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Collections.Concurrent;
#nullable enable

namespace JPFITS
{
	/// <summary> FITSImage class to create, read, interact with, modify components of, and write FITS Primary image data and its Header.</summary>
	public class FITSImage
	{
		#region CONSTRUCTORS

		/// <summary>Create a dummy FITSImage object with a simple primary header.</summary>
		/// <param name="fullFileName">File name. May be anything for this dummy object, but if provided should follow a normal directory structure with file name and extension. Otherwise pass empty string.</param>
		/// <param name="mayContainExtensions">Sets the EXTEND keyword if true for the dummy header of this FITSImage.</param>
		public FITSImage(string fullFileName, bool mayContainExtensions)
		{
			ISEXTENSION = false;
			EXTNAME = null;

			if (fullFileName != "")
			{
				FULLFILENAME = fullFileName;
				FILENAME = Path.GetFileName(FULLFILENAME);
				FILEPATH = Path.GetDirectoryName(FULLFILENAME);
			}
			else
			{
				FULLFILENAME = "";
				FILENAME = "";
				FILEPATH = "";
			}

			HEADER = new FITSHeader(mayContainExtensions, null);

			WORLDCOORDINATESOLUTION = new JPFITS.WorldCoordinateSolution();
		}

		/// <summary>Create a FITSImage object from an array object containing existing data.
		/// <br />Image data is maintained at or converted to double precision.</summary>
		/// <param name="fullFileName">File name.</param>
		/// <param name="imageArrayVectorData">The data array or vector to use for the FITS image data. The precision and rank of the underlying array will be automatically determined. Vectors will be converted to a 1D array.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and standard deviation of the image data - saves time if you don't need those.</param>
		/// <param name="doParallel">Populate the FITSImage object ImageData and perform stats (if true) with parallelization.</param>
		public FITSImage(string fullFileName, Array imageArrayVectorData, bool doStats, bool doParallel)
		{
			int rank = imageArrayVectorData.Rank;

			if (rank > 2)
				throw new Exception("Error: Rank of Array 'imageArrayVectorData' is not a 1D or 2D array. Use other interface functions for creating larger dimensional FITS primary data.");

			if (rank == 1)
				NAXISN = new int[2] { 1, imageArrayVectorData.Length };
			else
				NAXISN = new int[2] { imageArrayVectorData.GetLength(0), imageArrayVectorData.GetLength(1) };

			DIMAGE = new double[NAXISN[0], NAXISN[1]];

			ISEXTENSION = false;
			EXTNAME = null;

			FULLFILENAME = fullFileName;
			FILENAME = Path.GetFileName(FULLFILENAME);
			FILEPATH = Path.GetDirectoryName(FULLFILENAME);

			HEADER_POP = true;
			DATA_POP = true;
			STATS_POP = doStats;

			HEADER = new FITSHeader(true, DIMAGE);

			WORLDCOORDINATESOLUTION = new JPFITS.WorldCoordinateSolution();

			TypeCode type = Type.GetTypeCode(imageArrayVectorData.GetType().GetElementType());

			ParallelOptions opts = new ParallelOptions();
			if (doParallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			switch (type)
			{
				case TypeCode.Boolean:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXISN[1], opts, x =>
						{
							if (((bool[])imageArrayVectorData)[x])
								DIMAGE[0, x] = 1;
						});
					}
					else
					{
						Parallel.For(0, NAXISN[0], opts, x =>
						{
							for (int y = 0; y < NAXISN[1]; y++)
								if (((bool[,])imageArrayVectorData)[x, y])
									DIMAGE[x, y] = 1;
						});
					}

					break;
				}

				case TypeCode.Double:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXISN[1], opts, x =>
						{
							DIMAGE[0, x] = ((double[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXISN[0], opts, x =>
						{
							for (int y = 0; y < NAXISN[1]; y++)
								DIMAGE[x, y] = ((double[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.Single:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXISN[1], opts, x =>
						{
							DIMAGE[0, x] = ((float[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXISN[0], opts, x =>
						{
							for (int y = 0; y < NAXISN[1]; y++)
								DIMAGE[x, y] = ((float[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.UInt16:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXISN[1], opts, x =>
						{
							DIMAGE[0, x] = ((ushort[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXISN[0], opts, x =>
						{
							for (int y = 0; y < NAXISN[1]; y++)
								DIMAGE[x, y] = ((ushort[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.Int16:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXISN[1], opts, x =>
						{
							DIMAGE[0, x] = ((short[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXISN[0], opts, x =>
						{
							for (int y = 0; y < NAXISN[1]; y++)
								DIMAGE[x, y] = ((short[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.UInt32:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXISN[1], opts, x =>
						{
							DIMAGE[0, x] = ((uint[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXISN[0], opts, x =>
						{
							for (int y = 0; y < NAXISN[1]; y++)
								DIMAGE[x, y] = ((uint[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.Int32:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXISN[1], opts, x =>
						{
							DIMAGE[0, x] = ((int[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXISN[0], opts, x =>
						{
							for (int y = 0; y < NAXISN[1]; y++)
								DIMAGE[x, y] = ((int[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.UInt64:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXISN[1], opts, x =>
						{
							DIMAGE[0, x] = ((ulong[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXISN[0], opts, x =>
						{
							for (int y = 0; y < NAXISN[1]; y++)
								DIMAGE[x, y] = ((ulong[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.Int64:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXISN[1], opts, x =>
						{
							DIMAGE[0, x] = ((long[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXISN[0], opts, x =>
						{
							for (int y = 0; y < NAXISN[1]; y++)
								DIMAGE[x, y] = ((long[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				default:
					throw new Exception("Data of TypeCode:" + type.ToString() + " not suported.");
			}
			if (STATS_POP)
				StatsUpD(doParallel);
		}

		/// <summary>Create a FITSImage object with Primary image data loaded to RAM memory from disk.
		/// <br />Image data is loaded as double precision independent of storage precision on disk.</summary>
		/// <param name="fullFileName">File name.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax].  Pass null or Range[0] = -1 to default to full image size.</param>
		/// <param name="populateHeader">Optionally populate the header - sometimes you just want the data, and can skip reading the non-essential header lines.</param>
		/// <param name="populateData">Optionally populate the image data array - sometimes you just want the header and don't need the data.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and standard deviation of the image data (if populated) - saves time if you don't need those.</param>
		/// <param name="doParallel">Populate the FITSImage object ImageData and perform stats (if true) with parallelization.</param>
		public FITSImage(string fullFileName, int[]? range, bool populateHeader, bool populateData, bool doStats, bool doParallel)
		{
			FULLFILENAME = fullFileName;
			FILENAME = Path.GetFileName(FULLFILENAME);
			FILEPATH = Path.GetDirectoryName(FULLFILENAME);

			HEADER_POP = populateHeader;
			DATA_POP = populateData;
			STATS_POP = doStats;
			ISEXTENSION = false;
			EXTNAME = null;			

			FileStream fs = new FileStream(FULLFILENAME, FileMode.Open, FileAccess.Read, FileShare.Read);
			ArrayList header = new ArrayList();
			if (!FITSFILEOPS.ScanImageHeaderUnit(fs, false, ref header, out bool hasext, out int BITPIX, out NAXISN, out double BSCALE, out double BZERO))
			{
				fs.Close();
				throw new Exception("File '" + fullFileName + "' not formatted as FITS file.");
			}
			HEADER = new JPFITS.FITSHeader(header, HEADER_POP);

			if (DATA_POP)
				DIMAGE = (double[,])FITSFILEOPS.ReadImageDataUnit(fs, range, doParallel, BITPIX, ref NAXISN, BSCALE, BZERO, RankFormat.VectorAsVerticalTable);
			else
				if (NAXISN.Length == 0)
					NAXISN = new int[2];
				else if (NAXISN.Length == 1)
				{
					int n = NAXISN[0];
					NAXISN = new int[2] { 1, n };
				}
			fs.Close();

			if (!DATA_POP)
				STATS_POP = false;
			if (STATS_POP)
				StatsUpD(doParallel);

			WORLDCOORDINATESOLUTION = new JPFITS.WorldCoordinateSolution();			
		}

		/// <summary>Create a FITSImage object with extension image data loaded to RAM memory from disk.
		/// <br />Image data is loaded as double precision independent of storage precision on disk.</summary>
		/// <param name="fullFileName">File name.</param>
		/// <param name="extensionName">The EXTNAME extension name of the image. If an empty string is passed, the first nameless IMAGE extension will be read. Exception if no such extension exits.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax].  Pass null or Range[0] = -1 to default to full image size.</param>
		/// <param name="populateHeader">Optionally populate the header - sometimes you just want the data, and can skip reading the non-essential header lines.</param>
		/// <param name="populateData">Optionally populate the image data array - sometimes you just want the header and don't need the data.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and standard deviation of the image data (if populated) - saves time if you don't need those.</param>
		/// <param name="doParallel">Populate the FITSImage object ImageData and perform stats (if true) with parallelization.</param>
		public FITSImage(string fullFileName, string extensionName, int[]? range, bool populateHeader, bool populateData, bool doStats, bool doParallel)
		{
			FULLFILENAME = fullFileName;
			FILENAME = Path.GetFileName(FULLFILENAME);
			FILEPATH = Path.GetDirectoryName(FULLFILENAME);
			this.FileName = this.FileName.Substring(0, this.FileName.LastIndexOf(".")) + "_" + EXTNAME + this.FileName.Substring(this.FileName.LastIndexOf("."));

			HEADER_POP = populateHeader;
			DATA_POP = populateData;
			STATS_POP = doStats;
			ISEXTENSION = false;//because change to primary
			EXTNAME = extensionName;

			FileStream fs = new FileStream(fullFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			ArrayList header = null;
			if (!FITSFILEOPS.ScanPrimaryUnit(fs, true, ref header, out bool hasext) || !hasext)
			{
				fs.Close();
				if (!hasext)
					throw new Exception("File '" + fullFileName + "' indicates no extensions present.");
				else
					throw new Exception("File '" + fullFileName + "' not formatted as FITS file.");
			}

			if (!FITSFILEOPS.SeekExtension(fs, "IMAGE", extensionName, ref header, out long extensionstartposition, out _, out _, out _, out _))
			{
				fs.Close();
				throw new Exception("Could not find IMAGE extension with name '" + extensionName + "'");
			}

			fs.Position = extensionstartposition;
			header = new ArrayList();
			if (!FITSFILEOPS.ScanImageHeaderUnit(fs, false, ref header, out hasext, out int BITPIX, out NAXISN, out double BSCALE, out double BZERO))
			{
				fs.Close();
				throw new Exception("File '" + fullFileName + "' not formatted as FITS file.");
			}
			HEADER = new JPFITS.FITSHeader(header, populateHeader);
			HEADER.SetKey(0, "SIMPLE", "T", "file conforms to FITS standard");

			if (DATA_POP)
				DIMAGE = (double[,])FITSFILEOPS.ReadImageDataUnit(fs, range, doParallel, BITPIX, ref NAXISN, BSCALE, BZERO, RankFormat.VectorAsVerticalTable);
			else
				if (NAXISN.Length == 0)
					NAXISN = new int[2];
				else if (NAXISN.Length == 1)
				{
					int n = NAXISN[0];
					NAXISN = new int[2] { 1, n };
				}
			fs.Close();

			if (!DATA_POP)
				STATS_POP = false;
			if (STATS_POP)
				StatsUpD(doParallel);

			WORLDCOORDINATESOLUTION = new JPFITS.WorldCoordinateSolution();
		}

		/// <summary>Create a FITSImage object with extension image data loaded to RAM memory from disk. <br />Useful when extensions are not named with EXTNAME keyword. Will return the image at the extension number if they are named regardless.
		/// <br />Image data is loaded as double precision independent of storage precision on disk.</summary>
		/// <param name="fullFileName">File name.</param>
		/// <param name="extensionNumber">The ONE-BASED extension number of the image.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax].  Pass null or Range[0] = -1 to default to full image size.</param>
		/// <param name="populateHeader">Optionally populate the header - sometimes you just want the data, and can skip reading the non-essential header lines.</param>
		/// <param name="populateData">Optionally populate the image data array - sometimes you just want the header and don't need the data.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and standard deviation of the image data (if populated) - saves time if you don't need those.</param>
		/// <param name="doParallel">Populate the FITSImage object ImageData and perform stats (if true) with parallelization.</param>
		public FITSImage(string fullFileName, int extensionNumber, int[]? range, bool populateHeader, bool populateData, bool doStats, bool doParallel)
		{
			FULLFILENAME = fullFileName;
			FILENAME = Path.GetFileName(FULLFILENAME);
			FILEPATH = Path.GetDirectoryName(FULLFILENAME);

			HEADER_POP = populateHeader;
			DATA_POP = populateData;
			STATS_POP = doStats;
			ISEXTENSION = false;//because changed to primary above

			FileStream fs = new FileStream(fullFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			ArrayList header = null;
			if (!FITSFILEOPS.ScanPrimaryUnit(fs, true, ref header, out bool hasext) || !hasext)
			{
				fs.Close();
				if (!hasext)
					throw new Exception("File '" + fullFileName + "' indicates no extensions present.");
				else
					throw new Exception("File '" + fullFileName + "' not formatted as FITS file.");
			}

			if (!FITSFILEOPS.SeekExtension(fs, "IMAGE", extensionNumber, ref header, out long extensionstartposition, out _, out _, out _, out _))
			{
				fs.Close();
				throw new Exception("Could not find IMAGE extension number '" + extensionNumber + "'");
			}

			fs.Position = extensionstartposition;
			header = new ArrayList();
			if (!FITSFILEOPS.ScanImageHeaderUnit(fs, false, ref header, out hasext, out int BITPIX, out NAXISN, out double BSCALE, out double BZERO))
			{
				fs.Close();
				throw new Exception("File '" + fullFileName + "' not formatted as FITS file.");
			}
			HEADER = new JPFITS.FITSHeader(header, populateHeader);
			HEADER.SetKey(0, "SIMPLE", "T", "file conforms to FITS standard");
			
			EXTNAME = HEADER.GetKeyValue("EXTNAME");
			if (EXTNAME == "")
				EXTNAME = "EXT_" + extensionNumber.ToString("000000");
			this.FileName = this.FileName.Substring(0, this.FileName.LastIndexOf(".")) + "_" + EXTNAME + this.FileName.Substring(this.FileName.LastIndexOf("."));

			if (DATA_POP)
				DIMAGE = (double[,])FITSFILEOPS.ReadImageDataUnit(fs, range, doParallel, BITPIX, ref NAXISN, BSCALE, BZERO, RankFormat.VectorAsVerticalTable);
			else
				if (NAXISN.Length == 0)
					NAXISN = new int[2];
				else if (NAXISN.Length == 1)
				{
					int n = NAXISN[0];
					NAXISN = new int[2] { 1, n };
				}
			fs.Close();

			if (!DATA_POP)
				STATS_POP = false;
			if (STATS_POP)
				StatsUpD(doParallel);

			WORLDCOORDINATESOLUTION = new JPFITS.WorldCoordinateSolution();
		}

		/// <summary>Create a FITSImage object referencing raw UChar data on disk. Image data is loaded as double precision independent of storage precision on disk.</summary>
		/// <param name="fullFileName">File name for the FITS object.</param>
		/// <param name="DiskUCharBufferName">File name of the disk byte data.</param>
		/// <param name="Precision">Precision of the data stored in the disk char array.</param>
		/// <param name="NAxis1">Length of the 1st axis (x-axis)</param>
		/// <param name="NAxis2">Length of the 2nd axis (y-axis)</param>
		public FITSImage(string fullFileName, string DiskUCharBufferName, DiskPrecision Precision, int NAxis1, int NAxis2)
		{
			ISEXTENSION = false;
			EXTNAME = null;

			FULLFILENAME = fullFileName;
			FILENAME = Path.GetFileName(FULLFILENAME);
			FILEPATH = Path.GetDirectoryName(FULLFILENAME);
			DISKBUFFERFULLNAME = DiskUCharBufferName;
			FROMDISKBUFFER = true;

			NAXISN = new int[2] { NAxis1, NAxis2 };
			DIMAGE = new double[NAxis1, NAxis2];//default fill

			HEADER = new FITSHeader(true, DIMAGE);
			SetBITPIXNAXISBSCZ(Precision, DIMAGE, HEADER);

			WORLDCOORDINATESOLUTION = new JPFITS.WorldCoordinateSolution();
		}

		#endregion

		#region PROPERTIES

		/// <summary>Default indexer accesses the image element of the primary image of the FITSImage object.</summary>
		public double this[int x]
		{
			get { return DIMAGE[0, x]; }
			set { DIMAGE[0, x] = value; }
		}

		/// <summary>Default indexer accesses the image element of the primary image of the FITSImage object.</summary>
		public double this[int x, int y]
		{
			get { return DIMAGE[x, y]; }
			set { DIMAGE[x, y] = value; }
		}

		public DiskPrecision HeaderTypeCode
		{
			get { return FITSHeader.GetHeaderTypeCode(HEADER); }
		}

		/// <summary>Min returns the minimum of the FITS image data array.  Returns zero if there is no array loaded or if stats have not been performed.</summary>
		public double Min
		{
			get { return MIN; }
		}

		/// <summary>Max returns the maximum of the FITS image data array.  Returns zero if there is no array loaded or if stats have not been performed.</summary>
		public double Max
		{
			get { return MAX; }
		}

		/// <summary>Median returns the median of the FITS image data array.  Returns zero if there is no array loaded or if stats have not been performed.</summary>
		public double Median
		{
			get { return MEDIAN; }
		}

		/// <summary>Mean returns the average of the FITS image data array.  Returns zero if there is no array loaded or if stats have not been performed.</summary>
		public double Mean
		{
			get { return MEAN; }
		}

		/// <summary>Std returns the standard deviation of the FITS image data array.  Returns zero if there is no array loaded or if stats have not been performed.</summary>
		public double Stdv
		{
			get { return STDV; }
		}

		/// <summary>Sum returns the sum of the FITS image data array.  Returns zero if there is no array loaded or if stats have not been performed.</summary>
		public double Sum
		{
			get { return SUM; }
		}

		/// <summary>Width returns the width of the FITS image data array.  Returns zero if there is no array loaded.</summary>
		public int Width
		{
			get { return NAXISN[0]; }
		}

		/// <summary>Height returns the height of the FITS image data array.  Returns zero if there is no array loaded.</summary>
		public int Height
		{
			get { return NAXISN[1]; }
		}

		/// <summary>Length returns the total number of elements of the FITS image data array.  Returns zero if there is no array loaded.</summary>
		public int Length
		{
			get { return NAXISN[1] * NAXISN[0]; }
		}

		/// <summary>FileName accesses just the file name of the FITS object.</summary>
		public string FileName
		{
			get { return FILENAME; }
			set
			{
				FILENAME = value;
				FULLFILENAME = Path.Combine(FILEPATH, FILENAME);
			}
		}

		/// <summary>FilePath accesses just the file path of the FITS object.</summary>
		public string FilePath
		{
			get { return FILEPATH + "\\"; }
			set
			{
				FILEPATH = value;
				FULLFILENAME = Path.Combine(FILEPATH, FILENAME);
			}
		}

		/// <summary>FullFileName accesses the full file path + name of the FITS object.</summary>
		public string FullFileName
		{
			get { return FULLFILENAME; }
			set
			{
				FULLFILENAME = value;
				FILENAME = Path.GetFileName(FULLFILENAME);
				FILEPATH = Path.GetDirectoryName(FULLFILENAME);
			}
		}

		/// <summary>Image accesses the 2-D double array of the primary FITS object image.
		/// <br />Individual elements of the array can be accessed by indexing -&gt;Image[x,y].
		/// <br />Property setter automatically performs image stats when Image is set.  Use -&gt;SetImage instead for option to not perform stats.</summary>
		public double[,] Image
		{
			get { return DIMAGE; }
			//set { SetImage(value, true, true); }
		}

		/// <summary>Provides access to the image header.</summary>
		public JPFITS.FITSHeader Header
		{
			get { return HEADER; }
			set { HEADER = value; }
		}

		/// <summary>Provides access to the image WCS.</summary>
		public JPFITS.WorldCoordinateSolution WCS
		{
			get { return WORLDCOORDINATESOLUTION; }
			set 
			{
				WORLDCOORDINATESOLUTION = value;
			}
		}
		#endregion

		#region IMAGEOPS
		/// <summary>StatsUpD updates the statistics for the primary image: maximum, minimum, mean, median, and standard deviation.</summary>
		public void StatsUpD(bool doParallel)
		{
			if (doParallel)
			{
				object locker = new object();
				var rangePartitioner = Partitioner.Create(0, NAXISN[0]);
				ParallelOptions opts = new ParallelOptions();
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;

				MIN = Double.MaxValue;
				MAX = Double.MinValue;
				SUM = 0;
				STDV = 0;

				Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
				{
					double sum = 0;
					double min = Double.MaxValue;
					double max = Double.MinValue;

					for (int i = range.Item1; i < range.Item2; i++)
						for (int j = 0; j < NAXISN[1]; j++)
						{
							sum += DIMAGE[i, j];
							if (DIMAGE[i, j] > max)
								max = DIMAGE[i, j];
							else if (DIMAGE[i, j] < min)
								min = DIMAGE[i, j];
						}

					lock (locker)
					{
						SUM += sum;
						MIN = Math.Min(MIN, min);
						MAX = Math.Max(MAX, max);
					}
				});

				MEAN = SUM / (double)DIMAGE.Length;

				Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
				{
					double std = 0, delta;
					for (int i = range.Item1; i < range.Item2; i++)
						for (int j = 0; j < NAXISN[1]; j++)
						{
							delta = (DIMAGE[i, j] - MEAN);
							std += delta * delta;
						}

					lock (locker)
					{
						STDV += std;
					}
				});
				STDV = Math.Sqrt(STDV / ((double)DIMAGE.Length - 1.0));

				MEDIAN = JPMath.Median(DIMAGE);
			}
			else
			{
				MIN = Double.MaxValue;
				MAX = Double.MinValue;
				STDV = 0;
				SUM = 0;

				for (int i = 0; i < NAXISN[0]; i++)
					for (int j = 0; j < NAXISN[1]; j++)
					{
						SUM += DIMAGE[i, j];
						if (MIN > DIMAGE[i, j])
							MIN = DIMAGE[i, j];
						if (MAX < DIMAGE[i, j])
							MAX = DIMAGE[i, j];
					}
				MEAN = SUM / (double)DIMAGE.Length;

				MEDIAN = JPMath.Median(DIMAGE);

				double delta;
				for (int i = 0; i < NAXISN[0]; i++)
					for (int j = 0; j < NAXISN[1]; j++)
					{
						delta = (DIMAGE[i, j] - MEAN);
						STDV += delta * delta; ;
					}

				STDV = Math.Sqrt(STDV / ((double)DIMAGE.Length - 1.0));
			}
		}

		/// <summary>Use SetImage to replace the existing double array for the FITSImage object with a new double array.</summary>
		/// <param name="imageArrayData">The 2-D double array to set for the FITSImage object.</param>
		/// <param name="Do_Stats">Optionally update the stats for the new array.</param>
		public void SetImage(double[,] imageArrayData, bool Do_Stats, bool doParallel)
		{
			DIMAGE = imageArrayData;
			NAXISN = new int[2] { imageArrayData.GetLength(0), imageArrayData.GetLength(1) };
			//NAXIS1 = imageArrayData.GetLength(0);
			//NAXIS2 = imageArrayData.GetLength(1);
			this.Header.SetKey("NAXIS1", NAXISN[0].ToString(), false, -1);
			this.Header.SetKey("NAXIS2", NAXISN[1].ToString(), false, -1);
			STATS_POP = Do_Stats;
			if (STATS_POP)
				StatsUpD(doParallel);
		}

		/// <summary>Returns a double array of a subset of coordinates from the primary image.</summary>
		/// <param name="X_Center">The zero-based center-position of the primary x-axis of the subimage.</param>
		/// <param name="Y_Center">The zero-based center-position of the primary y-axis of the subimage.</param>
		/// <param name="X_HalfWidth">The +- half-width of the x-axis of the subimage.</param>
		/// <param name="Y_HalfWidth">The +- half-width of the y-axis of the subimage.</param>

		public double[,] GetSubImage(int X_Center, int Y_Center, int X_HalfWidth, int Y_HalfWidth)
		{
			double[,] result = new double[X_HalfWidth * 2 + 1, Y_HalfWidth * 2 + 1];

			/*if (X_Center - X_HalfWidth < 0 || Y_Center - Y_HalfWidth < 0 || X_Center - X_HalfWidth + result.GetLength(0) > NAXIS1 || Y_Center - Y_HalfWidth + result.GetLength(1) > NAXIS2)
			{

			}*/

			for (int x = 0; x < result.GetLength(0); x++)
				for (int y = 0; y < result.GetLength(1); y++)
					result[x, y] = DIMAGE[X_Center - X_HalfWidth + x, Y_Center - Y_HalfWidth + y];

			return result;
		}

		/// <summary>Returns a double array of a subset of coordinates from the primary image.</summary>
		/// <param name="X_Center">The zero-based center-position of the primary x-axis of the subimage.</param>
		/// <param name="Y_Center">The zero-based center-position of the primary y-axis of the subimage.</param>
		/// <param name="X_HalfWidth">The +- half-width of the x-axis of the subimage.</param>
		/// <param name="Y_HalfWidth">The +- half-width of the y-axis of the subimage.</param>
		/// <param name="xdata">The x-indices of the subimage.</param>
		/// <param name="ydata">The y-indices of the subimage.</param>
		public double[,] GetSubImage(int X_Center, int Y_Center, int X_HalfWidth, int Y_HalfWidth, ref int[] xdata, ref int[] ydata)
		{
			double[,] result = new double[X_HalfWidth * 2 + 1, Y_HalfWidth * 2 + 1];

			/*if (X_Center - X_HalfWidth < 0 || Y_Center - Y_HalfWidth < 0 || X_Center - X_HalfWidth + result.GetLength(0) > NAXIS1 || Y_Center - Y_HalfWidth + result.GetLength(1) > NAXIS2)
			{

			}*/

			for (int x = 0; x < result.GetLength(0); x++)
				for (int y = 0; y < result.GetLength(1); y++)
					result[x, y] = DIMAGE[X_Center - X_HalfWidth + x, Y_Center - Y_HalfWidth + y];

			for (int x = 0; x < result.GetLength(0); x++)
				xdata[x] = X_Center - X_HalfWidth + x;
			for (int y = 0; y < result.GetLength(1); y++)
				ydata[y] = Y_Center - Y_HalfWidth + y;

			return result;
		}

		/// <summary>Returns a double array of a subset of coordinates from the primary image.</summary>
		/// <param name="Range">The zero-based start and end coordinates of the subimage in the primary image. Range is: [xmin xmax ymin ymax].</param>
		public double[,] GetSubImage(int[] Range)
		{
			double[,] result = new double[Range[1] - Range[0] + 1, Range[3] - Range[2] + 1];

			for (int x = 0; x < result.GetLength(0); x++)
				for (int y = 0; y < result.GetLength(1); y++)
					result[x, y] = DIMAGE[Range[0] + x, Range[2] + y];

			return result;
		}

		/// <summary>RotateCW rotates the primary image by 90 degrees.</summary>
		/// <param name="CW">True to rotate clockwise, false to rotate counter-clock-wise.</param>
		public void RotateCW(bool CW)
		{
			int n1p1o2 = (NAXISN[0] + 1) / 2;
			int n2p1o2 = (NAXISN[1] + 1) / 2;
			int n1m1 = NAXISN[0] - 1;
			int n2m1 = NAXISN[1] - 1;

			double[,] rotimg = new double[NAXISN[1], NAXISN[0]];
			if (CW)
			{
				Parallel.For(0, n1p1o2, i =>
				{
					double dum1, dum2, dum3, dum4;
					for (int j = 0; j < n2p1o2; j++)
					{
						dum1 = DIMAGE[i, j];
						dum2 = DIMAGE[i, n2m1 - j];
						dum3 = DIMAGE[n1m1 - i, n2m1 - j];
						dum4 = DIMAGE[n1m1 - i, j];

						rotimg[j, i] = dum2;
						rotimg[n2m1 - j, i] = dum1;
						rotimg[n2m1 - j, n1m1 - i] = dum4;
						rotimg[j, n1m1 - i] = dum3;
					}
				});
				this.Header.AddKey("ROTATN", "-90", "Image Rotated CW 90", -1);
			}
			else
			{
				Parallel.For(0, n1p1o2, i =>
				{
					double dum1, dum2, dum3, dum4;
					for (int j = 0; j < n2p1o2; j++)
					{
						dum1 = DIMAGE[i, j];
						dum2 = DIMAGE[i, n2m1 - j];
						dum3 = DIMAGE[n1m1 - i, n2m1 - j];
						dum4 = DIMAGE[n1m1 - i, j];

						rotimg[j, i] = dum4;
						rotimg[n2m1 - j, i] = dum3;
						rotimg[n2m1 - j, n1m1 - i] = dum2;
						rotimg[j, n1m1 - i] = dum1;
					}
				});
				this.Header.AddKey("ROTATN", "90", "Image Rotated CCW 90", -1);
			}

			DIMAGE = rotimg;
			this.Header.SetKey("NAXIS1", NAXISN[1].ToString(), false, 0);
			this.Header.SetKey("NAXIS2", NAXISN[0].ToString(), false, 0);
			//int dumn = NAXIS1;
			//NAXIS1 = NAXIS2;
			//NAXIS2 = dumn;
			NAXISN = new int[2] { DIMAGE.GetLength(0), DIMAGE.GetLength(1) };
		}

		/// <summary>FlipVertical flips the image across the horizontal axis, i.e. up to down.</summary>
		public void FlipVertical()
		{
			int n2o2 = NAXISN[1] / 2;
			int n2m1 = NAXISN[1] - 1;

			Parallel.For(0, NAXISN[0], i =>
			{
				double dum1, dum2;
				for (int j = 0; j < n2o2; j++)
				{
					dum1 = DIMAGE[i, j];
					dum2 = DIMAGE[i, n2m1 - j];
					DIMAGE[i, j] = dum2;
					DIMAGE[i, n2m1 - j] = dum1;
				}
			});
			this.Header.AddKey("VFLIP", "true", "Image Vertically Flipped", -1);
		}

		/// <summary>FlipVertical flips the image across the vertical axis, i.e. left to right.</summary>
		public void FlipHorizontal()
		{
			int n1o2 = NAXISN[0] / 2;
			int n1m1 = NAXISN[0] - 1;

			Parallel.For(0, NAXISN[1], j =>
			{
				double dum1, dum2;
				for (int i = 0; i < n1o2; i++)
				{
					dum1 = DIMAGE[i, j];
					dum2 = DIMAGE[n1m1 - i, j];
					DIMAGE[i, j] = dum2;
					DIMAGE[n1m1 - i, j] = dum1;
				}
			});
			this.Header.AddKey("HFLIP", "true", "Image Horizontally Flipped", -1);
		}

		#endregion

		#region WRITING

		/// <summary>Write a FITS image to disk as a primary header and primary image from the FITSImage object with its existing file name.
		/// <br />If the file name already exists on disk, the primary unit will be overwritten, and any existing extensions will be appended to conserve the data file.</summary>
		/// <param name="precision">Byte precision at which to write the image data.</param>
		/// <param name="doParallel">Populate the underlying byte arrays for writing with parallelization.</param>
		public void WriteImage(DiskPrecision precision, bool doParallel)
		{
			ISEXTENSION = false;
			EXTNAME = null;

			WRITEIMAGE(precision, doParallel);
		}

		/// <summary>Write a FITS image to disk as a primary header and primary image from the FITSImage object with a given file name.
		/// <br />If the file name already exists on disk, the primary unit will be overwritten, and any existing extensions will be appended to conserve the data file.</summary>
		/// <param name="fullFileName">File name.</param>
		/// <param name="precision">Byte precision at which to write the image data.</param>
		/// <param name="doParallel">Populate the underlying byte arrays for writing with parallelization.</param>
		public void WriteImage(string fullFileName, DiskPrecision precision, bool doParallel)
		{
			ISEXTENSION = false;
			EXTNAME = null;

			FULLFILENAME = fullFileName;
			int index = FULLFILENAME.LastIndexOf("\\");
			FILENAME = FULLFILENAME.Substring(index + 1);
			FILEPATH = FULLFILENAME.Substring(0, index + 1);

			WRITEIMAGE(precision, doParallel);
		}

		/// <summary>Write a FITS image to disk as an extension to a given file name.</summary>
		/// <param name="fullFileName">File name. Pass the object&apos;s own FullFileName to write to its existing file name.
		/// <br />If the file doesn't yet exist on disk, then a new file will be created with an empty Primary Unit, and the image will be written as an extension.
		/// <br />If the file does exist, then the extension will be written with the logic for the overwriteIfExists parameter, and
		/// <br />the existing primary unit and any other extensions will be conserved to the file.</param>
		/// <param name="extensionName">The EXTNAME extension name of the IMAGE extension. Please do not use nameless extensions.
		/// <br />If an empty string is passed, the first nameless IMAGE extension will be written to.
		/// <br />If no such extension exists, the extension will be written as a new extension to the FITS file.</param>
		/// <param name="overwriteExtensionIfExists">If the image extension already exists it can be overwritten. If it exists and the option is given to not overwrite it, then an exception will be thrown.</param>
		/// <param name="precision">Byte precision at which to write the image data.</param>
		/// <param name="doParallel">Populate the underlying byte arrays for writing with parallelization.</param>
		public void WriteImage(string fullFileName, string extensionName, bool overwriteExtensionIfExists, DiskPrecision precision, bool doParallel)
		{
			ISEXTENSION = true;
			EXTNAME = extensionName;
			EXTNAMEOVERWRITE = overwriteExtensionIfExists;

			FULLFILENAME = fullFileName;
			int index = FULLFILENAME.LastIndexOf("\\");
			FILENAME = FULLFILENAME.Substring(index + 1);
			FILEPATH = FULLFILENAME.Substring(0, index + 1);

			WRITEIMAGE(precision, doParallel);
		}

		public void WriteFileFromDiskBuffer(bool DeleteOrigDiskBuffer)
		{
			if (FROMDISKBUFFER != true)
				throw new Exception("This FITS not created from 'JPFITS.FITSImage(string FullFileName, string DiskUCharBufferName, TypeCode Precision, int NAxis1, int NAxis2)' constructor.");

			string[] HEADER = this.Header.GetFormattedHeaderBlock(FITSHeader.HeaderUnitType.Primary, false);
			FileStream fits_fs = new FileStream(FULLFILENAME, FileMode.Create);

			byte[] head = new byte[HEADER.Length * 80];
			for (int i = 0; i < HEADER.Length; i++)
				for (int j = 0; j < 80; j++)
					head[i * 80 + j] = (byte)HEADER[i][j];

			fits_fs.Write(head, 0, head.Length);//header is written

			FileStream buff_fs = new FileStream(DISKBUFFERFULLNAME, FileMode.Open, FileAccess.Read);

			int NBytes = (int)buff_fs.Length;//size of disk data buffer
			byte[] buff_arr = new byte[NBytes];
			buff_fs.Read(buff_arr, 0, NBytes);
			buff_fs.Close();

			fits_fs.Write(buff_arr, 0, NBytes);
			int resid = (int)(Math.Ceiling((double)(fits_fs.Position) / 2880.0)) * 2880 - (int)(fits_fs.Position);
			byte[] resds = new byte[resid];
			fits_fs.Write(resds, 0, resid);
			fits_fs.Close();






			//int NBytes = (int)buff_fs.Length;//size of disk data buffer
			//int buffsize = 1024 * 1024 * 64;//size of memory array buffer
			//byte[] buff_arr = new byte[buffsize];

			//int NBuffArrs = (int)(Math.Ceiling((double)(NBytes) / (double)(buffsize) - Double.Epsilon * 100));
			//for (int i = 0; i < NBuffArrs; i++)
			//{
			//	int bytestoread = buffsize;
			//	if (i == NBuffArrs - 1 && NBuffArrs != 1 || NBuffArrs == 1)
			//		bytestoread = NBytes - i * buffsize;
			//	buff_fs.Read(buff_arr, 0, bytestoread);
			//	fits_fs.Write(buff_arr, 0, bytestoread);
			//}
			//buff_fs.Close();
			//int resid = (int)(Math.Ceiling((double)(fits_fs.Position) / 2880.0)) * 2880 - (int)(fits_fs.Position);
			//byte[] resds = new byte[resid];
			//fits_fs.Write(resds, 0, resid);
			//fits_fs.Close();





			if (DeleteOrigDiskBuffer)
				File.Delete(DISKBUFFERFULLNAME);
		}

		#endregion

		#region STATICFILEIO

		/// <summary>Create a FITSImage object from raw data on disk.</summary>
		/// <param name="diskRawDataFileName">File name of the disk byte data in big-endian format.</param>
		/// <param name="fitsFileWriteName">File name for the FITS image object to write to. Pass an empty string to use the same file name with .fits extension. If the destination file exists, it will be overwritten.</param>
		/// <param name="precision">TypeCode precision of the data stored in the disk raw byte data.</param>
		/// <param name="NAxis1">Length of the 1st axis (x-axis)</param>
		/// <param name="NAxis2">Length of the 2nd axis (y-axis)</param>
		public static void RawDataToFITSImage(string diskRawDataFileName, string fitsFileWriteName, DiskPrecision precision, int NAxis1, int NAxis2)
		{
			FileStream buff_fs = new FileStream(diskRawDataFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			int NBytes = (int)buff_fs.Length;//size of disk data buffer
			byte[] buff_arr = new byte[NBytes];
			buff_fs.Read(buff_arr, 0, NBytes);
			buff_fs.Close();

			if (fitsFileWriteName == "")
				fitsFileWriteName = Path.ChangeExtension(diskRawDataFileName, ".fits");

			double[,] dimage = new double[NAxis1, NAxis2];
			FITSHeader header = new FITSHeader(true, dimage);
			SetBITPIXNAXISBSCZ(precision, dimage, header);
			string[] strHEADER = header.GetFormattedHeaderBlock(FITSHeader.HeaderUnitType.Primary, false);
			byte[] byteHEADER = new byte[strHEADER.Length * 80];
			for (int i = 0; i < strHEADER.Length; i++)
				for (int j = 0; j < 80; j++)
					byteHEADER[i * 80 + j] = (byte)strHEADER[i][j];

			FileStream fits_fs = new FileStream(fitsFileWriteName, FileMode.Create);
			fits_fs.Write(byteHEADER, 0, byteHEADER.Length);//header is written

			fits_fs.Write(buff_arr, 0, NBytes);

			int resid = (int)(Math.Ceiling((double)fits_fs.Length / 2880.0)) * 2880 - (int)(fits_fs.Length);
			fits_fs.Write(new byte[resid], 0, resid);
			fits_fs.Close();
		}

		/// <summary>Convert a (possibly poorly formatted) delimited text file to a double array.
		/// <br />If the text file is large (>2MB) the program may seem to hang...just let it run until control is returned.</summary>
		/// <param name="fullFileName">File name.</param>
		/// <param name="delimit">The field delimiter. If unknown pass empty string.</param>
		public static double[,] ConvertTxtToDblArray(string fullFileName, string delimit)
		{
			StreamReader sr = new StreamReader(fullFileName);
			string line = "";
			double[,] data;

			if (delimit == "")//find the delimiter
			{
				line = sr.ReadLine();
				line = line.Trim();
				string chstr;

				for (int i = 0; i < line.Length; i++)
				{
					chstr = line[i].ToString();

					if (JPMath.IsNumeric(chstr) || chstr == "+" || chstr == "-" || chstr == "E" || chstr == "e" || chstr == ".")
						continue;

					delimit = chstr;
					break;
				}
				sr.Close();
				sr = new StreamReader(fullFileName);
			}

			try
			{
				int jj = 0;
				int cols = 0;
				line = sr.ReadLine();
				line = line.Trim();
				while (line.IndexOf(delimit, jj + 1) != -1)
				{
					cols++;
					jj = line.IndexOf(delimit, jj + 1);
				}
				cols++; //because there will be one more data point after the last delimit

				ArrayList lines = new ArrayList();
				lines.Add(line);//because one row was already read
				while (!sr.EndOfStream)
				{
					line = sr.ReadLine();
					line = line.Trim();
					lines.Add(line);
				}
				sr.Close();

				data = new double[cols, lines.Count];
				int[] data_inds = new int[cols];
				double datum = 0;

				for (int i = 0; i < lines.Count; i++)
				{
					line = (string)lines[i];
					int jjj = 0;
					int c = 0;
					data_inds[0] = 0;
					while (line.IndexOf(delimit, jjj + 1) != -1)
					{
						jjj = line.IndexOf(delimit, jjj + 1);
						c++;
						data_inds[c] = jjj + 1;
					}
					for (int j = 0; j < cols; j++)
					{
						if (j < cols - 1)
							datum = Convert.ToDouble(line.Substring(data_inds[j], data_inds[j + 1] - data_inds[j] - 1));
						else
							datum = Convert.ToDouble(line.Substring(data_inds[j]));

						data[j, i] = datum;
					}
				}
			}
			catch (Exception e)
			{
				sr.Close();
				throw new Exception("Problem with line: '" + line + "'\r\n" + e.Message + e.StackTrace + e.Source + e.TargetSite + e.Data + e.InnerException + e.HelpLink);
			}
			sr.Close();
			return data;
		}		

		/// <summary>Return the primary image of the FITS file as a double 2-D array.</summary>
		/// <param name="fullFileName">The full file name to read from disk.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax]. Pass null or Range[0] = -1 to default to full image size.</param>
		public static double[,] ReadImageArrayOnly(string fullFileName, int[]? range, bool doParallel)
		{
			return new FITSImage(fullFileName, range, false, true, false, doParallel).Image;
		}

		/// <summary>Return the primary image of the FITS file as a double 1-D array.</summary>
		/// <param name="fullFileName">The full file name to read from disk.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax]. One of the axes ranges must be length equal to 1.
		/// <br /> Pass null or Range[0] = -1 to default to full image size, assuming the image data is a vector.</param>
		public static double[] ReadImageVectorOnly(string fullFileName, int[]? range, bool doParallel)
		{
			FileStream fs = new FileStream(fullFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			ArrayList header = null;
			FITSFILEOPS.ScanImageHeaderUnit(fs, false, ref header, out bool hasext, out int BITPIX, out int[] NAXISN, out double BSCALE, out double BZERO);

			double[] result = (double[])FITSFILEOPS.ReadImageDataUnit(fs, range, doParallel, BITPIX, ref NAXISN, BSCALE, BZERO, RankFormat.ArrayAsRangeRank);
			fs.Close();
			return result;
		}

		/// <summary>Reads an N-dimensional array and returns the results at its native on-disk precision by default. User may reorginize the array based on the return variable axis lengths vector nAxisN.</summary>
		/// <param name="nAxisN">int vector to return the axis lengths for each axis.</param>
		public static Array ReadPrimaryNDimensionalData(string fullFileName, out int[] nAxisN, ReadReturnPrecision returnPrecision = ReadReturnPrecision.Native)
		{
			FileStream fs = new FileStream(fullFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			ArrayList header = null;

			if (!FITSFILEOPS.ScanImageHeaderUnit(fs, false, ref header, out _, out int bitpix, out nAxisN, out double bscale, out double bzero))
			{
				fs.Close();
				throw new Exception("File '" + fullFileName + "' not formatted as FITS file.");
			}

			Array result = FITSFILEOPS.ReadImageDataUnit(fs, null, true, bitpix, ref nAxisN, bscale, bzero, RankFormat.Vector, returnPrecision);
			fs.Close();
			return result;
		}

		/// <summary>Reads an N-dimensional array and returns the results at its native on-disk precision by default. User may reorginize the array based on the return variable axis lengths vector nAxisN.</summary>
		/// <param name="nAxisN">int vector to return the axis lengths for each axis.</param>
		public static Array ReadExtensionNDimensionalData(string fullFileName, string extensionName, out int[] nAxisN, ReadReturnPrecision returnPrecision = ReadReturnPrecision.Native)
		{
			FileStream fs = new FileStream(fullFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			ArrayList header = null;

			if (!FITSFILEOPS.SeekExtension(fs, "IMAGE", extensionName, ref header, out long startpos, out _, out _, out _, out _))
			{
				fs.Close();
				throw new Exception("Extension " + extensionName + " not found.");
			}

			fs.Position = startpos;

			if (!FITSFILEOPS.ScanImageHeaderUnit(fs, false, ref header, out _, out int bitpix, out nAxisN, out double bscale, out double bzero))
			{
				fs.Close();
				throw new Exception("File '" + fullFileName + "' not formatted as FITS file.");
			}

			Array result = FITSFILEOPS.ReadImageDataUnit(fs, null, true, bitpix, ref nAxisN, bscale, bzero, RankFormat.Vector, returnPrecision);
			fs.Close();
			return result;
		}

		/// <summary>If a Primary data unit is saved as a layered image cube where each layer is unique, separate the layers into individual named extensions instead. The primary header will be copied into the extensions.</summary>
		/// <param name="sourceFullFileName">The file name of the FITS file with the layered primary data unit cube.</param>
		/// <param name="destFullFileName">The file name to write the extensions to. If it is the same name as the source, the source file will be backed up with a _bkp extension, or pass empty string for this behaviour.</param>
		/// <param name="layerExtensionNames">The names for each layer extension. Must be equal in length to the number of layers to pull out of the primary data unit; all extenions must have a unique name. Pass null for auto-naming.</param>
		public static void ExtendizePrimaryImageCube(string sourceFullFileName, string destFullFileName, string[]? layerExtensionNames)
		{
			if (destFullFileName == "")
				destFullFileName = sourceFullFileName;

			FITSHeader origheader = new FITSHeader(sourceFullFileName);
			FITSImageSet set = FITSImageSet.ReadPrimaryImageCubeAsSet(sourceFullFileName);

			if (layerExtensionNames == null)
			{
				layerExtensionNames = new string[set.Count];
				for (int i = 0; i < layerExtensionNames.Length; i++)
					layerExtensionNames[i] = i.ToString("00000");
			}
			else
			{
				if (layerExtensionNames.Length != set.Count)
					throw new Exception("layerExtensionNames array not equal in length (" + layerExtensionNames.Length + ") to the number of layers (" + set.Count + ")");

				for (int i = 0; i < layerExtensionNames.Length - 1; i++)
					for (int j = i + 1; j < layerExtensionNames.Length; j++)
						if (layerExtensionNames[i] == layerExtensionNames[j])
							throw new Exception("layerExtensionNames are not all unique: " + (i + 1) + ": " + layerExtensionNames[i] + "; " + (j + 1) + ": " + layerExtensionNames[j]);

				for (int i = 0; i < layerExtensionNames.Length; i++)
					if (layerExtensionNames[i] == "")
						throw new Exception("layerExtensionNames cannot contain a nameless extension (empty string): " + (i + 1));
			}

			if (destFullFileName == sourceFullFileName)
			{
				File.Move(sourceFullFileName, sourceFullFileName + "_bkp");
				File.Delete(destFullFileName);
			}

			set.WriteAsExtensions(destFullFileName, false, false, null, layerExtensionNames, new DiskPrecision[1] { FITSHeader.GetHeaderTypeCode(origheader) });
		}
		
		/// <summary>Returns an array of all image table extension names in a FITS file. If there are no image table extensions, returns an empty array.</summary>
		/// <param name="FileName">The full file name to read from disk.</param>
		public static string[] GetAllExtensionNames(string FileName)
		{
			return FITSFILEOPS.GetAllExtensionNames(FileName, "IMAGE");
		}

		#endregion

		#region OPERATORS

		public static double[,] operator +(FITSImage lhs_img, FITSImage rhs_img)
		{
			if (lhs_img.Width != rhs_img.Width || lhs_img.Height != rhs_img.Height)
				throw new System.ArrayTypeMismatchException("Image Data Matrices not the Same Size...Discontinuing.");

			double[,] result = new double[lhs_img.Width, lhs_img.Height];

			Parallel.For(0, lhs_img.Width, i =>
			{
				for (int j = 0; j < lhs_img.Height; j++)
					result[i, j] = lhs_img[i, j] + rhs_img[i, j];
			});

			return result;
		}

		public static double[,] operator +(FITSImage lhs_img, double scalar)
		{
			double[,] result = new double[lhs_img.Width, lhs_img.Height];

			Parallel.For(0, lhs_img.Width, i =>
			{
				for (int j = 0; j < lhs_img.Height; j++)
					result[i, j] = lhs_img[i, j] + scalar;
			});

			return result;
		}

		public static double[,] operator +(double scalar, FITSImage rhs_img)
		{
			return rhs_img + scalar;
		}

		public static double[,] operator -(FITSImage lhs_img, FITSImage rhs_img)
		{
			if (lhs_img.Width != rhs_img.Width || lhs_img.Height != rhs_img.Height)
				throw new System.ArrayTypeMismatchException("Image Data Matrices not the Same Size...Discontinuing.");

			double[,] result = new double[lhs_img.Width, lhs_img.Height];

			Parallel.For(0, lhs_img.Width, i =>
			{
				for (int j = 0; j < lhs_img.Height; j++)
					result[i, j] = lhs_img[i, j] - rhs_img[i, j];
			});

			return result;
		}

		public static double[,] operator -(FITSImage lhs_img, double scalar)
		{
			double[,] result = new double[lhs_img.Width, lhs_img.Height];

			Parallel.For(0, lhs_img.Width, i =>
			{
				for (int j = 0; j < lhs_img.Height; j++)
					result[i, j] = lhs_img[i, j] - scalar;
			});

			return result;
		}

		public static double[,] operator -(double scalar, FITSImage rhs_img)
		{
			double[,] result = new double[rhs_img.Width, rhs_img.Height];

			Parallel.For(0, rhs_img.Width, i =>
			{
				for (int j = 0; j < rhs_img.Height; j++)
					result[i, j] = scalar - rhs_img[i, j];
			});

			return result;
		}

		public static double[,] operator /(FITSImage lhs_img, FITSImage rhs_img)
		{
			if (lhs_img.Width != rhs_img.Width || lhs_img.Height != rhs_img.Height)
				throw new System.ArrayTypeMismatchException("Image Data Matrices not the Same Size...Discontinuing.");

			double[,] result = new double[lhs_img.Width, lhs_img.Height];

			Parallel.For(0, lhs_img.Width, i =>
			{
				for (int j = 0; j < lhs_img.Height; j++)
					result[i, j] = lhs_img[i, j] / rhs_img[i, j];
			});

			return result;
		}

		public static double[,] operator /(FITSImage lhs_img, double scalar)
		{
			double[,] result = new double[lhs_img.Width, lhs_img.Height];
			scalar = 1 / scalar;

			Parallel.For(0, lhs_img.Width, i =>
			{
				for (int j = 0; j < lhs_img.Height; j++)
					result[i, j] = lhs_img[i, j] * scalar;
			});

			return result;
		}

		public static double[,] operator /(double scalar, FITSImage rhs_img)
		{
			double[,] result = new double[rhs_img.Width, rhs_img.Height];

			Parallel.For(0, rhs_img.Width, i =>
			{
				for (int j = 0; j < rhs_img.Height; j++)
					result[i, j] = scalar / rhs_img[i, j];
			});

			return result;
		}

		public static double[,] operator *(FITSImage lhs_img, FITSImage rhs_img)
		{
			if (lhs_img.Width != rhs_img.Width || lhs_img.Height != rhs_img.Height)
				throw new System.ArrayTypeMismatchException("Image Data Matrices not the Same Size...Discontinuing.");

			double[,] result = new double[lhs_img.Width, lhs_img.Height];

			Parallel.For(0, lhs_img.Width, i =>
			{
				for (int j = 0; j < lhs_img.Height; j++)
					result[i, j] = lhs_img[i, j] * rhs_img[i, j];
			});

			return result;
		}

		public static double[,] operator *(FITSImage lhs_img, double scalar)
		{
			double[,] result = new double[lhs_img.Width, lhs_img.Height];

			Parallel.For(0, lhs_img.Width, i =>
			{
				for (int j = 0; j < lhs_img.Height; j++)
					result[i, j] = lhs_img[i, j] * scalar;
			});

			return result;
		}

		public static double[,] operator *(double scalar, FITSImage rhs_img)
		{
			return rhs_img * scalar;
		}

		public static double[,] operator ^(FITSImage lhs_img, FITSImage rhs_img)
		{
			if (lhs_img.Width != rhs_img.Width || lhs_img.Height != rhs_img.Height)
				throw new System.ArrayTypeMismatchException("Image Data Matrices not the Same Size...Discontinuing.");

			double[,] result = new double[lhs_img.Width, lhs_img.Height];

			Parallel.For(0, lhs_img.Width, i =>
			{
				for (int j = 0; j < lhs_img.Height; j++)
					result[i, j] = Math.Pow(lhs_img[i, j], rhs_img[i, j]);
			});

			return result;
		}

		public static double[,] operator ^(FITSImage lhs_img, double scalar)
		{
			double[,] result = new double[lhs_img.Width, lhs_img.Height];

			Parallel.For(0, lhs_img.Width, i =>
			{
				for (int j = 0; j < lhs_img.Height; j++)
					result[i, j] = Math.Pow(lhs_img[i, j], scalar);
			});

			return result;
		}

		public static double[,] operator ^(double scalar, FITSImage rhs_img)
		{
			int W = rhs_img.Width;
			int H = rhs_img.Height;
			double[,] result = new double[rhs_img.Width, rhs_img.Height];

			Parallel.For(0, rhs_img.Width, i =>
			{
				for (int j = 0; j < rhs_img.Height; j++)
					result[i, j] = Math.Pow(scalar, rhs_img[i, j]);
			});

			return result;
		}
		#endregion

		#region PRIVATEMEMBERS

		//Conditions
		private bool HEADER_POP;
		private bool DATA_POP;
		private bool STATS_POP;
		private bool FROMDISKBUFFER;
		private bool ISEXTENSION;
		private string? EXTNAME;
		private bool EXTNAMEOVERWRITE;

		private JPFITS.FITSHeader HEADER;
		private JPFITS.WorldCoordinateSolution WORLDCOORDINATESOLUTION;

		//Image
		private double[,]? DIMAGE;//double precision data unit table array
		int[]? NAXISN;

		//Image Stats
		private double MIN, MAX, MEAN, MEDIAN, STDV, SUM;

		//File Info
		private string FILENAME;
		private string FILEPATH;
		private string FULLFILENAME;
		private string? DISKBUFFERFULLNAME;

		//File IO		
		//		/*//subimage reading only...seems much slower than just reading entire image
		//		if (BITPIX == -64)
		//		{
		//			array<unsigned char> arr = new array<unsigned char>(W*8);
		//			double val;
		//			int cc;
		//			array<unsigned char> dbl = new array<unsigned char>(8);

		//			bs.Seek((R[2]*NAXIS1 + R[0])*8,.SeekOrigin.Current);

		//			for (int j = R[2]; j <= R[3]; j++)
		//			{
		//				bs.Read(arr,0,W*8);

		//				for (int i = R[0]; i <= R[1]; i++)
		//				{
		//					cc = (i - R[0])*8;
		//					dbl[7] = arr[cc];
		//					dbl[6] = arr[cc + 1];
		//					dbl[5] = arr[cc + 2];
		//					dbl[4] = arr[cc + 3];
		//					dbl[3] = arr[cc + 4];
		//					dbl[2] = arr[cc + 5];
		//					dbl[1] = arr[cc + 6];
		//					dbl[0] = arr[cc + 7];
		//					val = BitConverter.ToDouble(dbl, 0);
		//					DIMAGE[i-R[0],j-R[2]] = (val)*bscale + bzero;
		//				}

		//				bs.Seek((NAXIS1 - R[1] - 1 + R[0])*8,.SeekOrigin.Current);
		//			}
		//		}*/
		//	}

		private void WRITEIMAGE(DiskPrecision precision, bool do_parallel)
		{
			FileStream fs = null;
			bool filexists = File.Exists(FULLFILENAME);
			byte[] prependdata;
			byte[] appenddata = null;
			if (!filexists && !ISEXTENSION)//then just write to a new file
				fs = new FileStream(FULLFILENAME, FileMode.Create);
			else if (filexists && !ISEXTENSION)//then write the primary unit, and append any extensions if they already exist on the existing file
			{
				fs = new FileStream(FULLFILENAME, FileMode.Open);
				//check for extensions
				ArrayList header_return = null;
				FITSFILEOPS.ScanPrimaryUnit(fs, true, ref header_return, out bool hasext);
				if (hasext)
				{
					appenddata = new byte[(int)(fs.Length - fs.Position)];
					fs.Read(appenddata, 0, appenddata.Length);
				}
				fs.Close();
				fs = new FileStream(FULLFILENAME, FileMode.Create);//write the primary unit, don't forget to append the extensions after if it isn't null
			}
			else if (filexists && ISEXTENSION)//then get the primary data unit and also check for other extensions, etc
			{
				fs = new FileStream(FULLFILENAME, FileMode.Open);
				ArrayList header_return = null;
				if (!FITSFILEOPS.ScanPrimaryUnit(fs, true, ref header_return, out bool hasext))
				{
					fs.Close();
					throw new Exception("Primary data unit of file is not a FITS file.");
				}

				bool extexists = FITSFILEOPS.SeekExtension(fs, "IMAGE", EXTNAME, ref header_return, out long extensionstartposition, out long extensionendposition, out _, out _, out _);
				if (extexists && !EXTNAMEOVERWRITE)
				{
					fs.Close();
					throw new Exception("IMAGE extension '" + EXTNAME + "' exists but told to not overwrite it.");
				}
				if (!extexists)//then the fs stream will be at the end of the file, get all prependdata
				{
					prependdata = new byte[(int)fs.Position];
					fs.Position = 0;
					fs.Read(prependdata, 0, prependdata.Length);
					fs.Close();
					fs = new FileStream(FULLFILENAME, FileMode.Create);
					fs.Write(prependdata, 0, prependdata.Length);
				}
				else//then get the prepend units and any append data
				{
					prependdata = new byte[(int)extensionstartposition];
					fs.Position = 0;
					fs.Read(prependdata, 0, prependdata.Length);
					appenddata = new byte[(int)(fs.Length - extensionendposition)];
					fs.Position = extensionendposition;
					fs.Read(appenddata, 0, appenddata.Length);
					fs.Close();
					fs = new FileStream(FULLFILENAME, FileMode.Create);
					fs.Write(prependdata, 0, prependdata.Length);
				}
			}
			else if (!filexists && ISEXTENSION)//then write the extension to a new file with an empty primary unit
			{
				FITSHeader hed = new FITSHeader(true, null);
				string[] pheader = hed.GetFormattedHeaderBlock(FITSHeader.HeaderUnitType.ExtensionIMAGE, false);
				prependdata = new byte[pheader.Length * 80];
				for (int i = 0; i < pheader.Length; i++)
					for (int j = 0; j < 80; j++)
						prependdata[i * 80 + j] = (byte)pheader[i][j];
				fs = new FileStream(FULLFILENAME, FileMode.Create);
				fs.Write(prependdata, 0, prependdata.Length);
			}

			//set header BZERO and BCSALE key values depending on prec type.
			SetBITPIXNAXISBSCZ(precision, DIMAGE, HEADER);

			FITSHeader.HeaderUnitType type = FITSHeader.HeaderUnitType.Primary;
			if (ISEXTENSION)
			{
				type = FITSHeader.HeaderUnitType.ExtensionIMAGE;
				this.Header.SetKey("EXTNAME", EXTNAME, true, -1);
			}

			//get formatted header block
			string[] header = this.Header.GetFormattedHeaderBlock(type, false);
			byte[] data = new byte[header.Length * 80];
			for (int i = 0; i < header.Length; i++)
				for (int j = 0; j < 80; j++)
					data[i * 80 + j] = (byte)header[i][j];

			fs.Write(data, 0, data.Length);

			//get formatted data block
			data = FITSFILEOPS.GetByteFormattedImageDataUnit(precision, do_parallel, DIMAGE);
			fs.Write(data, 0, data.Length);
			if (appenddata != null)
				fs.Write(appenddata, 0, appenddata.Length);
			fs.Close();
		}		

		/// <summary>This sets the BITPIX, NAXIS, NAXISn, BSCALE and BZERO keywords of the header given the TypeCode and the image. If the image is null then NAXIS = 0 and any NAXISn keywords are removed as well as BSCALE and BZERO.</summary>
		private static void SetBITPIXNAXISBSCZ(DiskPrecision precision, double[,]? image, FITSHeader HEADER)
		{
			FITSFILEOPS.GetBitpixNaxisnBscaleBzero(precision, image, out int bitpix, out int[] naxnisn, out double bscale, out double bzero);

			HEADER.SetKey("BITPIX", bitpix.ToString(), true, 1);
			HEADER.SetKey("NAXIS", naxnisn.Length.ToString(), true, 2);

			if (naxnisn.Length == 0)
			{
				int c = 1;
				while (HEADER.GetKeyIndex("NAXIS" + c, false) != -1)
					HEADER.RemoveKey("NAXIS" + c++);
				HEADER.RemoveKey("BZERO");
				HEADER.RemoveKey("BSCALE");
				return;
			}
			else
			{
				for (int i = 0; i < naxnisn.Length; i++)
					HEADER.SetKey("NAXIS" + (i + 1).ToString(), naxnisn[i].ToString(), "number of elements on this axis", true, 3 + i);

				HEADER.SetKey("BZERO", bzero.ToString(), "data offset", true, 3 + naxnisn.Length);
				HEADER.SetKey("BSCALE", bscale.ToString(), "data scaling", true, 3 + naxnisn.Length + 1);
			}
		}

		#endregion

	}
}
