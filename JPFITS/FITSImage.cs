using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
		#region PRIVATEMEMBERS

		//Image Conditions
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
		private double[,]? DIMAGE;//double precision image

		//Image Stats
		private double MIN, MAX, MEAN, MEDIAN, STD, SUM;

		//Fits Info
		private int NAXIS1 = -1, NAXIS2 = -1, BITPIX = -1, NAXIS = -1;
		private double BZERO = -1, BSCALE = -1;
		//private int[] NAXISN;

		//File Info
		private string FILENAME;
		private string FILEPATH;
		private string FULLFILENAME;
		private string? DISKBUFFERFULLNAME;

		//File IO
		private void READIMAGE(FileStream fs, int[]? Range, bool do_parallel)
		{
			if (NAXIS <= 0)
			{
				NAXIS1 = 0;
				NAXIS2 = 0;
				if (HEADER_POP)
				{
					this.Header.SetKey("NAXIS1", "0", false, 0);
					this.Header.SetKey("NAXIS2", "0", false, 0);
				}
				DIMAGE = new double[0, 0];
				return;
			}

			int bpix = Math.Abs(BITPIX);
			int NBytes = NAXIS1 * NAXIS2 * (bpix / 8);
			int[] R = new int[4];

			if (Range != null)
			{
				if (Range[1] > NAXIS1 - 1 || Range[1] == -1)
					Range[1] = NAXIS1 - 1;
				if (Range[3] > NAXIS2 - 1 || Range[3] == -1)
					Range[3] = NAXIS2 - 1;
			}

			if (Range == null || Range[0] == -1)//then it is a full frame read
			{
				R[0] = 0;
				R[1] = NAXIS1 - 1;
				R[2] = 0;
				R[3] = NAXIS2 - 1;
			}
			else//else it is a sub-frame read
			{
				R[0] = Range[0];
				R[1] = Range[1];
				R[2] = Range[2];
				R[3] = Range[3];
			}

			DIMAGE = new double[R[1] - R[0] + 1, R[3] - R[2] + 1];
			byte[] arr = new byte[NBytes];
			fs.Read(arr, 0, NBytes);//seems to be fastest to just read the entire data even if only subimage will be used

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			switch (BITPIX)
			{
				case 8:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = j * NAXIS1 + R[0];
						for (int i = R[0]; i <= R[1]; i++)
							DIMAGE[i - R[0], j - R[2]] = (double)(arr[cc + i]) * BSCALE + BZERO;
					});
					break;
				}

				case 16:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						short val;
						int cc = (j * NAXIS1 + R[0]) * 2;
						for (int i = R[0]; i <= R[1]; i++)
						{
							val = (short)((arr[cc] << 8) | arr[cc + 1]);
							DIMAGE[i - R[0], j - R[2]] = (double)(val) * BSCALE + BZERO;
							cc += 2;
						}
					});
					break;
				}

				case 32:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int val;
						int cc = (j * NAXIS1 + R[0]) * 4;
						for (int i = R[0]; i <= R[1]; i++)
						{
							val = (arr[cc] << 24) | (arr[cc + 1] << 16) | (arr[cc + 2] << 8) | arr[cc + 3];
							DIMAGE[i - R[0], j - R[2]] = (double)(val) * BSCALE + BZERO;
							cc += 4;
						}
					});
					break;
				}

				case 64:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = (j * NAXIS1 + R[0]) * 8;
						byte[] dbl = new byte[8];
						for (int i = R[0]; i <= R[1]; i++)
						{
							dbl[7] = arr[cc];
							dbl[6] = arr[cc + 1];
							dbl[5] = arr[cc + 2];
							dbl[4] = arr[cc + 3];
							dbl[3] = arr[cc + 4];
							dbl[2] = arr[cc + 5];
							dbl[1] = arr[cc + 6];
							dbl[0] = arr[cc + 7];
							DIMAGE[i - R[0], j - R[2]] = (double)(BitConverter.ToInt64(dbl, 0)) * BSCALE + BZERO;
							cc += 8;
						}
					});
					break;
				}

				case -32:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = (j * NAXIS1 + R[0]) * 4;
						byte[] flt = new byte[4];
						for (int i = R[0]; i <= R[1]; i++)
						{
							flt[3] = arr[cc];
							flt[2] = arr[cc + 1];
							flt[1] = arr[cc + 2];
							flt[0] = arr[cc + 3];
							DIMAGE[i - R[0], j - R[2]] = (double)(BitConverter.ToSingle(flt, 0)) * BSCALE + BZERO;
							cc += 4;
						}
					});
					break;
				}

				case -64:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = (j * NAXIS1 + R[0]) * 8;
						byte[] dbl = new byte[8];
						for (int i = R[0]; i <= R[1]; i++)
						{
							dbl[7] = arr[cc];
							dbl[6] = arr[cc + 1];
							dbl[5] = arr[cc + 2];
							dbl[4] = arr[cc + 3];
							dbl[3] = arr[cc + 4];
							dbl[2] = arr[cc + 5];
							dbl[1] = arr[cc + 6];
							dbl[0] = arr[cc + 7];
							DIMAGE[i - R[0], j - R[2]] = (BitConverter.ToDouble(dbl, 0)) * BSCALE + BZERO;
							cc += 8;
						}
					});
					break;
				}

				/*//subimage reading only...seems much slower than just reading entire image
				if (BITPIX == -64)
				{
					array<unsigned char> arr = new array<unsigned char>(W*8);
					double val;
					int cc;
					array<unsigned char> dbl = new array<unsigned char>(8);

					bs.Seek((R[2]*NAXIS1 + R[0])*8,.SeekOrigin.Current);

					for (int j = R[2]; j <= R[3]; j++)
					{
						bs.Read(arr,0,W*8);

						for (int i = R[0]; i <= R[1]; i++)
						{
							cc = (i - R[0])*8;
							dbl[7] = arr[cc];
							dbl[6] = arr[cc + 1];
							dbl[5] = arr[cc + 2];
							dbl[4] = arr[cc + 3];
							dbl[3] = arr[cc + 4];
							dbl[2] = arr[cc + 5];
							dbl[1] = arr[cc + 6];
							dbl[0] = arr[cc + 7];
							val = BitConverter.ToDouble(dbl, 0);
							DIMAGE[i-R[0],j-R[2]] = (val)*bscale + bzero;
						}

						bs.Seek((NAXIS1 - R[1] - 1 + R[0])*8,.SeekOrigin.Current);
					}
				}*/
			}

			if (Range != null && Range[0] != -1)//then it is a full frame read
			{
				NAXIS1 = DIMAGE.GetLength(0);
				NAXIS2 = DIMAGE.GetLength(1);
				if (NAXIS2 == 1)
				{
					NAXIS2 = NAXIS1;
					NAXIS1 = 1;
				}
				this.Header.SetKey("NAXIS1", NAXIS1.ToString(), false, 0);
				this.Header.SetKey("NAXIS2", NAXIS2.ToString(), false, 0);
			}
		}

		private void EATIMAGEHEADER()
		{
			BITPIX = Convert.ToInt32(HEADER.GetKeyValue("BITPIX"));
			NAXIS = Convert.ToInt32(HEADER.GetKeyValue("NAXIS"));
			if (HEADER.GetKeyValue("NAXIS1") != "")
				NAXIS1 = Convert.ToInt32(HEADER.GetKeyValue("NAXIS1"));
			if (HEADER.GetKeyValue("NAXIS2") != "")
				NAXIS2 = Convert.ToInt32(HEADER.GetKeyValue("NAXIS2"));
			if (HEADER.GetKeyValue("BZERO") != "")
				BZERO = Convert.ToDouble(HEADER.GetKeyValue("BZERO"));
			if (HEADER.GetKeyValue("BSCALE") != "")
				BSCALE = Convert.ToDouble(HEADER.GetKeyValue("BSCALE"));

			if (NAXIS == 0)
			{
				NAXIS1 = 0;
				NAXIS2 = 0;
			}
			else if (NAXIS == 1)
			{
				NAXIS2 = NAXIS1;
				NAXIS1 = 1;
			}
			if (BZERO == -1)
				BZERO = 0;
			if (BSCALE == -1)
				BSCALE = 1;
		}

		private void WRITEIMAGE(TypeCode prec, bool do_parallel)
		{
			//try
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
					bool hasext = false;
					ArrayList header_return = null;
					FITSFILEOPS.SCANPRIMARYUNIT(fs, true, ref header_return, ref hasext);
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
					bool hasext = false;
					ArrayList header_return = null;
					if (!FITSFILEOPS.SCANPRIMARYUNIT(fs, true, ref header_return, ref hasext))
					{
						fs.Close();
						throw new Exception("Primary data unit of file is not a FITS file.");
					}

					long extensionstartposition, extensionendposition, tableendposition, pcount, theap;
					bool extexists = FITSFILEOPS.SEEKEXTENSION(fs, "IMAGE", EXTNAME, ref header_return, out extensionstartposition, out extensionendposition, out tableendposition, out pcount, out theap);
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
					string[] pheader = hed.GetFormattedHeaderBlock(true, false);
					prependdata = new byte[pheader.Length * 80];
					for (int i = 0; i < pheader.Length; i++)
						for (int j = 0; j < 80; j++)
							prependdata[i * 80 + j] = (byte)pheader[i][j];
					fs = new FileStream(FULLFILENAME, FileMode.Create);
					fs.Write(prependdata, 0, prependdata.Length);
				}

				//set header BZERO and BCSALE key values depending on prec type.
				this.Header.SetBITPIXNAXISBSCZ(prec, DIMAGE);
				EATIMAGEHEADER();

				//get formatted header block
				string[] header = this.Header.GetFormattedHeaderBlock(ISEXTENSION, false);
				int NHead = header.Length * 80;//number of bytes in fitsheader...should always be multiple of 2880.
				long NIm = ((long)NAXIS1) * ((long)NAXIS2) * ((long)Math.Abs(BITPIX / 8));//number of bytes in image
				long NImNHead = ((long)NHead) + NIm;//this is the number of bytes in the file + header...but need to write file so multiple of 2880 bytes
				int NBlocks = (int)(Math.Ceiling((double)(NImNHead) / 2880.0));
				int NBytesTot = NBlocks * 2880;

				double bscale = (double)BSCALE;
				double bzero = (double)BZERO;

				byte[] data = new byte[NBytesTot];

				for (int i = 0; i < header.Length; i++)
					for (int j = 0; j < 80; j++)
						data[i * 80 + j] = (byte)header[i][j];

				ParallelOptions opts = new ParallelOptions();
				if (do_parallel)
					opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
				else
					opts.MaxDegreeOfParallelism = 1;

				switch (prec)
				{
					case TypeCode.Byte:
					case TypeCode.SByte:
					{
						Parallel.For(0, NAXIS2, opts, j =>
						{
							int cc;
							sbyte val;							
							for (int i = 0; i < NAXIS1; i++)
							{
								cc = NHead + (j * NAXIS1 + i) * 2;
								val = (sbyte)((DIMAGE[i, j] - bzero) / bscale);
								data[cc] = (byte)val;
							}
						});
						break;
					}

					case TypeCode.UInt16:
					case TypeCode.Int16:
					{
						Parallel.For(0, NAXIS2, opts, j =>
						{
							int cc;
							short val;
							for (int i = 0; i < NAXIS1; i++)
							{
								cc = NHead + (j * NAXIS1 + i) * 2;
								val = (short)((DIMAGE[i, j] - bzero) / bscale);
								data[cc] = (byte)((val >> 8) & 0xff);
								data[cc + 1] = (byte)(val & 0xff);
							}
						});
						break;
					}

					case TypeCode.UInt32:
					case TypeCode.Int32:
					{
						Parallel.For(0, NAXIS2, opts, j =>
						{
							int cc;
							int val;
							for (int i = 0; i < NAXIS1; i++)
							{
								cc = NHead + (j * NAXIS1 + i) * 4;
								val = (int)((DIMAGE[i, j] - bzero) / bscale);
								data[cc] = (byte)((val >> 24) & 0xff);
								data[cc + 1] = (byte)((val >> 16) & 0xff);
								data[cc + 2] = (byte)((val >> 8) & 0xff);
								data[cc + 3] = (byte)(val & 0xff);
							}
						});
						break;
					}

					case TypeCode.UInt64:
					case TypeCode.Int64:
					{
						Parallel.For(0, NAXIS2, opts, j =>
						{
							int cc;
							long val;
							for (int i = 0; i < NAXIS1; i++)
							{
								cc = NHead + (j * NAXIS1 + i) * 8;
								val = (long)((DIMAGE[i, j] - bzero) / bscale);
								data[cc] = (byte)((val >> 56) & 0xff);
								data[cc + 1] = (byte)((val >> 48) & 0xff);
								data[cc + 2] = (byte)((val >> 40) & 0xff);
								data[cc + 3] = (byte)((val >> 32) & 0xff);
								data[cc + 4] = (byte)((val >> 24) & 0xff);
								data[cc + 5] = (byte)((val >> 16) & 0xff);
								data[cc + 6] = (byte)((val >> 8) & 0xff);
								data[cc + 7] = (byte)(val & 0xff);
							}
						});
						break;
					}

					case TypeCode.Single:
					{
						Parallel.For(0, NAXIS2, opts, j =>
						{
							int cc;
							byte[] sng = new byte[4];
							for (int i = 0; i < NAXIS1; i++)
							{
								cc = NHead + (j * NAXIS1 + i) * 4;
								sng = BitConverter.GetBytes((float)((DIMAGE[i, j] - bzero) / bscale));
								data[cc] = sng[3];
								data[cc + 1] = sng[2];
								data[cc + 2] = sng[1];
								data[cc + 3] = sng[0];
							}
						});
						break;
					}

					case TypeCode.Double:
					{
						Parallel.For(0, NAXIS2, opts, j =>
						{
							int cc;
							byte[] dbl = new byte[8];
							for (int i = 0; i < NAXIS1; i++)
							{
								cc = NHead + (j * NAXIS1 + i) * 8;
								dbl = BitConverter.GetBytes((DIMAGE[i, j] - bzero) / bscale);
								data[cc] = dbl[7];
								data[cc + 1] = dbl[6];
								data[cc + 2] = dbl[5];
								data[cc + 3] = dbl[4];
								data[cc + 4] = dbl[3];
								data[cc + 5] = dbl[2];
								data[cc + 6] = dbl[1];
								data[cc + 7] = dbl[0];
							}
						});
						break;
					}
				}

				fs.Write(data, 0, NBytesTot);
				if (appenddata != null)
					fs.Write(appenddata, 0, appenddata.Length);
				fs.Close();
			}
			/*catch (Exception e)
			{
				MessageBox.Show(e.Data + "	" + e.InnerException + "	" + e.Message + "	" + e.Source + "	" + e.StackTrace + "	" + e.TargetSite);
			}*/
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
		public double Std
		{
			get { return STD; }
		}

		/// <summary>Sum returns the sum of the FITS image data array.  Returns zero if there is no array loaded or if stats have not been performed.</summary>
		public double Sum
		{
			get { return SUM; }
		}

		/// <summary>Width returns the width of the FITS image data array.  Returns zero if there is no array loaded.</summary>
		public int Width
		{
			get { return NAXIS1; }
		}

		/// <summary>Height returns the height of the FITS image data array.  Returns zero if there is no array loaded.</summary>
		public int Height
		{
			get { return NAXIS2; }
		}

		/// <summary>Length returns the total number of elements of the FITS image data array.  Returns zero if there is no array loaded.</summary>
		public int Length
		{
			get { return NAXIS2 * NAXIS1; }
		}

		/// <summary>FileName accesses just the file name of the FITS object.</summary>
		public string FileName
		{
			get { return FILENAME; }
			set
			{
				FILENAME = value;
				FULLFILENAME = FILEPATH + FILENAME;
			}
		}

		/// <summary>FilePath accesses just the file path of the FITS object.</summary>
		public string FilePath
		{
			get { return FILEPATH; }
			set
			{
				FILEPATH = value + "\\";
				FULLFILENAME = FILEPATH + FILENAME;
			}
		}

		/// <summary>FullFileName accesses the full file path + name of the FITS object.</summary>
		public string FullFileName
		{
			get { return FULLFILENAME; }
			set
			{
				FULLFILENAME = value;
				int index = FULLFILENAME.LastIndexOf("\\");
				FILENAME = FULLFILENAME.Substring(index + 1);
				FILEPATH = FULLFILENAME.Substring(0, index + 1);
			}
		}

		/// <summary>Image accesses the 2-D double array of the primary FITS object image.
		/// <para>Individual elements of the array can be accessed by indexing -&gt;Image[x,y].</para>
		/// <para>Property setter automatically performs image stats when Image is set.  Use -&gt;SetImage instead for option to not perform stats.</para></summary>
		public double[,] Image
		{
			get { return DIMAGE; }
			set { SetImage(value, true, true); }
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
			set { WORLDCOORDINATESOLUTION = value; }
		}
		#endregion

		#region OPERATORS
		public static double[,] operator +(FITSImage lhs_img, FITSImage rhs_img)
		{
			if (lhs_img.Width != rhs_img.Width || lhs_img.Height != rhs_img.Height)
			{
				throw new System.ArrayTypeMismatchException("Image Data Matrices not the Same Size...Discontinuing.");
			}
			int W = lhs_img.Width;
			int H = lhs_img.Height;
			double[,] result = new double[W, H];

			Parallel.For(0, W, i =>
			{
				for (int j = 0; j < H; j++)
					result[i, j] = lhs_img[i, j] + rhs_img[i, j];
			});

			return result;
		}
		public static double[,] operator +(FITSImage lhs_img, double scalar)
		{
			int W = lhs_img.Width;
			int H = lhs_img.Height;
			double[,] result = new double[W, H];

			Parallel.For(0, W, i =>
			{
				for (int j = 0; j < H; j++)
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
			{
				throw new System.ArrayTypeMismatchException("Image Data Matrices not the Same Size...Discontinuing.");
			}
			int W = lhs_img.Width;
			int H = lhs_img.Height;
			double[,] result = new double[W, H];

			Parallel.For(0, W, i =>
			{
				for (int j = 0; j < H; j++)
					result[i, j] = lhs_img[i, j] - rhs_img[i, j];
			});

			return result;
		}
		public static double[,] operator -(FITSImage lhs_img, double scalar)
		{
			int W = lhs_img.Width;
			int H = lhs_img.Height;
			double[,] result = new double[W, H];

			Parallel.For(0, W, i =>
			{
				for (int j = 0; j < H; j++)
					result[i, j] = lhs_img[i, j] - scalar;
			});

			return result;
		}
		public static double[,] operator -(double scalar, FITSImage rhs_img)
		{
			int W = rhs_img.Width;
			int H = rhs_img.Height;
			double[,] result = new double[W, H];

			Parallel.For(0, W, i =>
			{
				for (int j = 0; j < H; j++)
					result[i, j] = scalar - rhs_img[i, j];
			});

			return result;
		}
		public static double[,] operator /(FITSImage lhs_img, FITSImage rhs_img)
		{
			if (lhs_img.Width != rhs_img.Width || lhs_img.Height != rhs_img.Height)
			{
				throw new System.ArrayTypeMismatchException("Image Data Matrices not the Same Size...Discontinuing.");
			}
			int W = lhs_img.Width;
			int H = lhs_img.Height;
			double[,] result = new double[W, H];

			Parallel.For(0, W, i =>
			{
				for (int j = 0; j < H; j++)
					result[i, j] = lhs_img[i, j] / rhs_img[i, j];
			});

			return result;
		}
		public static double[,] operator /(FITSImage lhs_img, double scalar)
		{
			int W = lhs_img.Width;
			int H = lhs_img.Height;
			double[,] result = new double[W, H];
			scalar = 1 / scalar;

			Parallel.For(0, W, i =>
			{
				for (int j = 0; j < H; j++)
					result[i, j] = lhs_img[i, j] * scalar;
			});

			return result;
		}
		public static double[,] operator /(double scalar, FITSImage rhs_img)
		{
			int W = rhs_img.Width;
			int H = rhs_img.Height;
			double[,] result = new double[W, H];

			Parallel.For(0, W, i =>
			{
				for (int j = 0; j < H; j++)
					result[i, j] = scalar / rhs_img[i, j];
			});

			return result;
		}
		public static double[,] operator *(FITSImage lhs_img, FITSImage rhs_img)
		{
			if (lhs_img.Width != rhs_img.Width || lhs_img.Height != rhs_img.Height)
			{
				throw new System.ArrayTypeMismatchException("Image Data Matrices not the Same Size...Discontinuing.");
			}
			int W = lhs_img.Width;
			int H = lhs_img.Height;
			double[,] result = new double[W, H];

			Parallel.For(0, W, i =>
			{
				for (int j = 0; j < H; j++)
					result[i, j] = lhs_img[i, j] * rhs_img[i, j];
			});

			return result;
		}
		public static double[,] operator *(FITSImage lhs_img, double scalar)
		{
			int W = lhs_img.Width;
			int H = lhs_img.Height;
			double[,] result = new double[W, H];

			Parallel.For(0, W, i =>
			{
				for (int j = 0; j < H; j++)
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
			{
				throw new System.ArrayTypeMismatchException("Image Data Matrices not the Same Size...Discontinuing.");
			}
			int W = lhs_img.Width;
			int H = lhs_img.Height;
			double[,] result = new double[W, H];

			Parallel.For(0, W, i =>
			{
				for (int j = 0; j < H; j++)
					result[i, j] = Math.Pow(lhs_img[i, j], rhs_img[i, j]);
			});

			return result;
		}
		public static double[,] operator ^(FITSImage lhs_img, double scalar)
		{
			int W = lhs_img.Width;
			int H = lhs_img.Height;
			double[,] result = new double[W, H];

			Parallel.For(0, W, i =>
			{
				for (int j = 0; j < H; j++)
					result[i, j] = Math.Pow(lhs_img[i, j], scalar);
			});

			return result;
		}
		public static double[,] operator ^(double scalar, FITSImage rhs_img)
		{
			int W = rhs_img.Width;
			int H = rhs_img.Height;
			double[,] result = new double[W, H];

			Parallel.For(0, W, i =>
			{
				for (int j = 0; j < H; j++)
					result[i, j] = Math.Pow(scalar, rhs_img[i, j]);
			});

			return result;
		}
		#endregion

		#region IMAGEOPS
		/// <summary>StatsUpD updates the statistics for the primary image: maximum, minimum, mean, median, and standard deviation.</summary>
		public void StatsUpD(bool doParallel)
		{
			if (doParallel)
			{
				object locker = new object();
				var rangePartitioner = Partitioner.Create(0, NAXIS1);
				ParallelOptions opts = new ParallelOptions();
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;

				MIN = Double.MaxValue;
				MAX = Double.MinValue;
				SUM = 0;
				STD = 0;

				Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
				{
					double sum = 0;
					double min = Double.MaxValue;
					double max = Double.MinValue;

					for (int i = range.Item1; i < range.Item2; i++)
						for (int j = 0; j < NAXIS2; j++)
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
						for (int j = 0; j < NAXIS2; j++)
						{
							delta = (DIMAGE[i, j] - MEAN);
							std += delta * delta;
						}

					lock (locker)
					{
						STD += std;
					}
				});
				STD = Math.Sqrt(STD / ((double)DIMAGE.Length - 1.0));

				MEDIAN = JPMath.Median(DIMAGE);
			}
			else
			{
				MIN = Double.MaxValue;
				MAX = Double.MinValue;
				STD = 0;
				SUM = 0;

				for (int i = 0; i < NAXIS1; i++)
					for (int j = 0; j < NAXIS2; j++)
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
				for (int i = 0; i < NAXIS1; i++)
					for (int j = 0; j < NAXIS2; j++)
					{
						delta = (DIMAGE[i, j] - MEAN);
						STD += delta * delta; ;
					}

				STD = Math.Sqrt(STD / ((double)DIMAGE.Length - 1.0));
			}
		}

		/// <summary>Use SetImage to replace the existing double array for the FITSImage object with a new double array.</summary>
		/// <param name="imageArrayData">The 2-D double array to set for the FITSImage object.</param>
		/// <param name="Do_Stats">Optionally update the stats for the new array.</param>
		public void SetImage(double[,] imageArrayData, bool Do_Stats, bool doParallel)
		{
			DIMAGE = imageArrayData;
			NAXIS1 = imageArrayData.GetLength(0);
			NAXIS2 = imageArrayData.GetLength(1);
			this.Header.SetKey("NAXIS1", NAXIS1.ToString(), false, -1);
			this.Header.SetKey("NAXIS2", NAXIS2.ToString(), false, -1);
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
			int n1p1o2 = (NAXIS1 + 1) / 2;
			int n2p1o2 = (NAXIS2 + 1) / 2;
			int n1m1 = NAXIS1 - 1;
			int n2m1 = NAXIS2 - 1;

			double[,] rotimg = new double[NAXIS2, NAXIS1];
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
			this.Header.SetKey("NAXIS1", NAXIS2.ToString(), false, 0);
			this.Header.SetKey("NAXIS2", NAXIS1.ToString(), false, 0);
			int dumn = NAXIS1;
			NAXIS1 = NAXIS2;
			NAXIS2 = dumn;
		}

		/// <summary>FlipVertical flips the image across the horizontal axis, i.e. up to down.</summary>
		public void FlipVertical()
		{
			int n2o2 = NAXIS2 / 2;
			int n2m1 = NAXIS2 - 1;

			Parallel.For(0, NAXIS1, i =>
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
			int n1o2 = NAXIS1 / 2;
			int n1m1 = NAXIS1 - 1;
			
			Parallel.For(0, NAXIS2, j =>
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

		#region STATICFILEIO

		/// <summary>Convert a (possibly poorly formatted) delimited text file to a double array.
		/// <para>If the text file is large (>2MB) the program may seem to hang...just let it run until control is returned.</para></summary>
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
		/// <param name="Range">Range is ZERO based 1-D int array [xmin xmax ymin ymax]. Pass null or Range[0] = -1 to default to full image size.</param>
		public static double[,] ReadImageArrayOnly(string fullFileName, int[]? Range, bool doParallel)
		{
			FileStream fs = new FileStream(fullFileName, FileMode.Open);

			double bzero = 0;
			double bscale = 1;
			int naxis1 = -1, naxis2 = -1, bitpix = -1;

			//read primary header
			bool endheader = false;
			int headerlines = 0, headerblocks = 0;
			string strheaderline;
			byte[] charheaderline = new byte[80];
			while (!endheader)
			{
				headerlines++;
				fs.Read(charheaderline, 0, 80);
				strheaderline = System.Text.Encoding.ASCII.GetString(charheaderline);

				if (strheaderline.Substring(0, 8) == "BZERO   ")
					bzero = Convert.ToDouble(strheaderline.Substring(10, 20));
				if (strheaderline.Substring(0, 8) == "BSCALE  ")
					bscale = Convert.ToDouble(strheaderline.Substring(10, 20));
				if (strheaderline.Substring(0, 8) == "NAXIS1  ")
					naxis1 = Convert.ToInt32(strheaderline.Substring(10, 20));
				if (strheaderline.Substring(0, 8) == "NAXIS2  ")
					naxis2 = Convert.ToInt32(strheaderline.Substring(10, 20));
				if (strheaderline.Substring(0, 8) == "BITPIX  ")
					bitpix = Convert.ToInt32(strheaderline.Substring(10, 20));
				if (strheaderline.Substring(0, 8) == "END     ")
					endheader = true;
			}
			headerblocks = (headerlines - 1) / 36 + 1;
			fs.Position += (headerblocks * 36 - headerlines) * 80;

			int bpix = Math.Abs(bitpix);
			int NBytes = naxis1 * naxis2 * (bpix / 8);
			int[] R = new int[4];

			if (Range == null || Range[0] == -1)//then it is a full frame read
			{
				R[0] = 0;
				R[1] = naxis1 - 1;
				R[2] = 0;
				R[3] = naxis2 - 1;
			}
			else//else it is a sub-frame read
			{
				R[0] = Range[0];
				R[1] = Range[1];
				R[2] = Range[2];
				R[3] = Range[3];
			}

			double[,] result = new double[R[1] - R[0] + 1, R[3] - R[2] + 1];
			byte[] arr = new byte[NBytes];
			fs.Read(arr, 0, NBytes);//seems to be fastest to just read the entire data even if only subimage will be used
			fs.Close();

			ParallelOptions opts = new ParallelOptions();
			if (doParallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			switch (bitpix)
			{
				case 8:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = j * naxis1 + R[0];
						for (int i = R[0]; i <= R[1]; i++)
							result[i - R[0], j - R[2]] = (double)(arr[cc + i]) * bscale + bzero;
					});
					break;
				}

				case 16:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = (j * naxis1 + R[0]) * 2;
						for (int i = R[0]; i <= R[1]; i++)
						{
							short val = (short)((arr[cc] << 8) | arr[cc + 1]);
							result[i - R[0], j - R[2]] = (double)(val) * bscale + bzero;
							cc += 2;
						}
					});
					break;
				}

				case 32:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = (j * naxis1 + R[0]) * 4;
						for (int i = R[0]; i <= R[1]; i++)
						{
							int val = (arr[cc] << 24) | (arr[cc + 1] << 16) | (arr[cc + 2] << 8) | arr[cc + 3];
							result[i - R[0], j - R[2]] = (double)(val) * bscale + bzero;
							cc += 4;
						}
					});
					break;
				}

				case 64:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = (j * naxis1 + R[0]) * 8;
						byte[] dbl = new byte[8];
						for (int i = R[0]; i <= R[1]; i++)
						{
							dbl[7] = arr[cc];
							dbl[6] = arr[cc + 1];
							dbl[5] = arr[cc + 2];
							dbl[4] = arr[cc + 3];
							dbl[3] = arr[cc + 4];
							dbl[2] = arr[cc + 5];
							dbl[1] = arr[cc + 6];
							dbl[0] = arr[cc + 7];
							result[i - R[0], j - R[2]] = (double)(BitConverter.ToInt64(dbl, 0)) * bscale + bzero;
							cc += 8;
						}
					});
					break;
				}

				case -32:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = (j * naxis1 + R[0]) * 4;
						byte[] flt = new byte[4];
						for (int i = R[0]; i <= R[1]; i++)
						{
							flt[3] = arr[cc];
							flt[2] = arr[cc + 1];
							flt[1] = arr[cc + 2];
							flt[0] = arr[cc + 3];
							result[i - R[0], j - R[2]] = (double)(BitConverter.ToSingle(flt, 0)) * bscale + bzero;
							cc += 4;
						}
					});
					break;
				}

				case -64:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = (j * naxis1 + R[0]) * 8;
						byte[] dbl = new byte[8];
						for (int i = R[0]; i <= R[1]; i++)
						{
							dbl[7] = arr[cc];
							dbl[6] = arr[cc + 1];
							dbl[5] = arr[cc + 2];
							dbl[4] = arr[cc + 3];
							dbl[3] = arr[cc + 4];
							dbl[2] = arr[cc + 5];
							dbl[1] = arr[cc + 6];
							dbl[0] = arr[cc + 7];
							result[i - R[0], j - R[2]] = (BitConverter.ToDouble(dbl, 0)) * bscale + bzero;
							cc += 8;
						}
					});
					break;
				}
			}

			return result;
		}

		/// <summary>Return the primary image of the FITS file as a double 1-D array.</summary>
		/// <param name="fullFileName">The full file name to read from disk.</param>
		/// <param name="Range">Range is ZERO based 1-D int array [xmin xmax ymin ymax]. One of the axes ranges must be length equal to 1.
		/// <para> Pass null or Range[0] = -1 to default to full image size, assuming the image data is a vector.</para></param>
		public static double[] ReadImageVectorOnly(string fullFileName, int[]? Range, bool doParallel)
		{
			if (Range != null && (Range[1] - Range[0]) > 0 && (Range[3] - Range[2]) > 0)
			{
				throw new Exception("Requested Vector Output but Specified Range is 2D.");
			}

			FileStream fs = new FileStream(fullFileName, FileMode.Open);
			byte[] c = new byte[2880];

			double bzero = 0;
			double bscale = 1;
			int naxis1 = -1, naxis2 = -1, bitpix = -1;

			//read primary header
			bool endheader = false;
			int headerlines = 0, headerblocks = 0;
			string strheaderline;
			byte[] charheaderline = new byte[80];
			while (!endheader)
			{
				headerlines++;
				fs.Read(charheaderline, 0, 80);
				strheaderline = System.Text.Encoding.ASCII.GetString(charheaderline);

				if (strheaderline.Substring(0, 8) == "BZERO   ")
					bzero = Convert.ToDouble(strheaderline.Substring(10, 20));
				if (strheaderline.Substring(0, 8) == "BSCALE  ")
					bscale = Convert.ToDouble(strheaderline.Substring(10, 20));
				if (strheaderline.Substring(0, 8) == "NAXIS1  ")
					naxis1 = Convert.ToInt32(strheaderline.Substring(10, 20));
				if (strheaderline.Substring(0, 8) == "NAXIS2  ")
					naxis2 = Convert.ToInt32(strheaderline.Substring(10, 20));
				if (strheaderline.Substring(0, 8) == "BITPIX  ")
					bitpix = Convert.ToInt32(strheaderline.Substring(10, 20));
				if (strheaderline.Substring(0, 8) == "END     ")
					endheader = true;
			}
			headerblocks = (headerlines - 1) / 36 + 1;
			fs.Position += (headerblocks * 36 - headerlines) * 80;

			int bpix = Math.Abs(bitpix);
			int NBytes = naxis1 * naxis2 * (bpix / 8);
			int[] R = new int[4];

			if (Range == null || Range[0] == -1)//then it is a full frame read
			{
				R[0] = 0;
				R[1] = naxis1 - 1;
				R[2] = 0;
				R[3] = naxis2 - 1;
			}
			else//else it is a sub-frame read
			{
				R[0] = Range[0];
				R[1] = Range[1];
				R[2] = Range[2];
				R[3] = Range[3];
			}

			if ((R[1] - R[0]) > 0 && (R[3] - R[2]) > 0)
			{
				throw new Exception("Requested defualt Vector Output but Image is 2D");
			}

			double[] result = new double[(R[1] - R[0] + 1) * (R[3] - R[2] + 1)];
			byte[] arr = new byte[NBytes];
			fs.Read(arr, 0, NBytes);//seems to be fastest to just read the entire data even if only subimage will be used
			fs.Close();

			ParallelOptions opts = new ParallelOptions();
			if (doParallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			switch (bitpix)
			{
				case 8:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = j * naxis1 + R[0];
						int jmr2 = j - R[2];
						for (int i = R[0]; i <= R[1]; i++)
							result[(i - R[0]) * jmr2 + jmr2] = (double)(arr[cc + i]) * bscale + bzero;
					});
					break;
				}

				case 16:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = (j * naxis1 + R[0]) * 2;
						int jmr2 = j - R[2];
						for (int i = R[0]; i <= R[1]; i++)
						{
							short val = (short)((arr[cc] << 8) | arr[cc + 1]);
							result[(i - R[0]) * jmr2 + jmr2] = (double)(val) * bscale + bzero;
							cc += 2;
						}
					});
					break;
				}

				case 32:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = (j * naxis1 + R[0]) * 4;
						int jmr2 = j - R[2];
						for (int i = R[0]; i <= R[1]; i++)
						{
							int val = (arr[cc] << 24) | (arr[cc + 1] << 16) | (arr[cc + 2] << 8) | arr[cc + 3];
							result[(i - R[0]) * jmr2 + jmr2] = (double)(val) * bscale + bzero;
							cc += 4;
						}
					});
					break;
				}

				case 64:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = (j * naxis1 + R[0]) * 8;
						int jmr2 = j - R[2];
						byte[] dbl = new byte[8];
						for (int i = R[0]; i <= R[1]; i++)
						{
							dbl[7] = arr[cc];
							dbl[6] = arr[cc + 1];
							dbl[5] = arr[cc + 2];
							dbl[4] = arr[cc + 3];
							dbl[3] = arr[cc + 4];
							dbl[2] = arr[cc + 5];
							dbl[1] = arr[cc + 6];
							dbl[0] = arr[cc + 7];
							result[(i - R[0]) * jmr2 + jmr2] = (double)(BitConverter.ToInt64(dbl, 0)) * bscale + bzero;
							cc += 8;
						}
					});
					break;
				}

				case -32:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = (j * naxis1 + R[0]) * 4;
						int jmr2 = j - R[2];
						byte[] flt = new byte[4];
						for (int i = R[0]; i <= R[1]; i++)
						{
							flt[3] = arr[cc];
							flt[2] = arr[cc + 1];
							flt[1] = arr[cc + 2];
							flt[0] = arr[cc + 3];
							result[(i - R[0]) * jmr2 + jmr2] = (double)(BitConverter.ToSingle(flt, 0)) * bscale + bzero;
							cc += 4;
						}
					});
					break;
				}

				case -64:
				{
					Parallel.For(R[2], R[3] + 1, opts, j =>
					{
						int cc = (j * naxis1 + R[0]) * 8;
						int jmr2 = j - R[2];
						byte[] dbl = new byte[8];
						for (int i = R[0]; i <= R[1]; i++)
						{
							dbl[7] = arr[cc];
							dbl[6] = arr[cc + 1];
							dbl[5] = arr[cc + 2];
							dbl[4] = arr[cc + 3];
							dbl[3] = arr[cc + 4];
							dbl[2] = arr[cc + 5];
							dbl[1] = arr[cc + 6];
							dbl[0] = arr[cc + 7];
							result[(i - R[0]) * jmr2 + jmr2] = (BitConverter.ToDouble(dbl, 0)) * bscale + bzero;
							cc += 8;
						}
					});
					break;
				}
			}

			return result;
		}

		/// <summary>Reads an N-dimensional array and returns the results as a double array. User may reorginize the array based on the return variable axis lengths vector nAxisN.</summary>
		/// <param name="nAxisN">A declared, but not instantiated, int vector to return the axis lengths for each axis.</param>
		public static double[] ReadPrimaryNDimensionalData(string fullFileName, out int[] nAxisN)
		{
			FileStream fs = new FileStream(fullFileName, FileMode.Open);
			ArrayList header = new ArrayList();
			bool hasext = false;
			if (!FITSFILEOPS.SCANPRIMARYUNIT(fs, false, ref header, ref hasext))
			{
				fs.Close();
				throw new Exception("File '" + fullFileName + "' not formatted as FITS file.");
			}

			nAxisN = new int[0];

			int bitpix = -1, nAxis = -1, nAxisn = 1;
			double bscale = 1, bzero = 0;
			for (int i = 0; i < header.Count; i++)
			{
				string line = (string)header[i];

				if (bitpix == -1)
					if (line.Substring(0, 8).Trim() == "BITPIX")
						bitpix = Convert.ToInt32(line.Substring(10, 20));

				if (nAxis == -1)
					if (line.Substring(0, 8).Trim() == "NAXIS")
					{
						nAxis = Convert.ToInt32(line.Substring(10, 20));
						nAxisN = new int[nAxis];
					}

				if (nAxisn <= nAxis)
					if (line.Substring(0, 8).Trim() == ("NAXIS" + nAxisn.ToString()))
					{
						nAxisN[nAxisn - 1] = Convert.ToInt32(line.Substring(10, 20));
						nAxisn++;
					}

				if (line.Substring(0, 8) == "BZERO   ")
					bzero = Convert.ToDouble(line.Substring(10, 20));

				if (line.Substring(0, 8) == "BSCALE  ")
					bscale = Convert.ToDouble(line.Substring(10, 20));
			}

			int NBytes = Math.Abs(bitpix) / 8;
			for (int i = 0; i < nAxisN.Length; i++)
				NBytes *= nAxisN[i];

			byte[] arr = new byte[NBytes];
			fs.Read(arr, 0, NBytes);
			fs.Close();

			double[] result = new double[NBytes / Math.Abs(bitpix)];

			if (bitpix == 8)
			{
				Parallel.For(0, result.Length, i =>
				{
					result[i] = (double)arr[i] * bscale + bzero;
				});
			}

			if (bitpix == 16)
			{
				Parallel.For(0, result.Length, i =>
				{
					int cc = i * 2;
					short val = (short)((arr[cc] << 8) | arr[cc + 1]);
					result[i] = (double)val * bscale + bzero;
				});
			}

			if (bitpix == 32)
			{
				Parallel.For(0, result.Length, i =>
				{
					int cc = i * 4;
					int val = (arr[cc] << 24) | (arr[cc + 1] << 16) | (arr[cc + 2] << 8) | arr[cc + 3];
					result[i] = (double)(val) * bscale + bzero;
				});
			}

			if (bitpix == -32)
			{
				Parallel.For(0, result.Length, i =>
				{
					byte[] flt = new byte[4];
					int cc = i * 4;
					flt[3] = arr[cc];
					flt[2] = arr[cc + 1];
					flt[1] = arr[cc + 2];
					flt[0] = arr[cc + 3];
					float val = BitConverter.ToSingle(flt, 0);
					result[i] = (double)(val) * bscale + bzero;
				});
			}

			if (bitpix == -64)
			{
				Parallel.For(0, result.Length, i =>
				{
					int cc = i * 8;
					byte[] dbl = new byte[8];
					dbl[7] = arr[cc];
					dbl[6] = arr[cc + 1];
					dbl[5] = arr[cc + 2];
					dbl[4] = arr[cc + 3];
					dbl[3] = arr[cc + 4];
					dbl[2] = arr[cc + 5];
					dbl[1] = arr[cc + 6];
					dbl[0] = arr[cc + 7];
					double val = BitConverter.ToDouble(dbl, 0);
					result[i] = val * bscale + bzero;
				});
			}

			return result;
		}

		/// <summary>If a Primary data unit is saved as a layered image cube where each layer is unique, separate the layers into individual named extensions instead.</summary>
		/// <param name="sourceFullFileName">The file name of the FITS file with the layered primary data unit.</param>
		/// <param name="destFullFileName">The file name to write the extensions to. If it is the same name as the source, then the source will be completely overwritten, including any other existing extensions which that file may have had.</param>
		/// <param name="layerExtensionNames">The names for each layer extension. Must be equal in length to the number of layers to pull out of the primary data unit; all extenions must have a unique name.</param>
		public static void ExtendizePrimaryImageLayerCube(string sourceFullFileName, string destFullFileName, string[] layerExtensionNames)
		{
			for (int i = 0; i < layerExtensionNames.Length - 1; i++)
				for (int j = i + 1; j < layerExtensionNames.Length; j++)
					if (layerExtensionNames[i] == layerExtensionNames[j])
					{
						throw new Exception("layerExtensionNames are not all unique");
					}
			for (int i = 0; i < layerExtensionNames.Length; i++)
				if (layerExtensionNames[i] == "")
				{
					throw new Exception("layerExtensionNames cannot contain a nameless extension (empty string)");
				}

			int[] axesN;
			double[] cube = FITSImage.ReadPrimaryNDimensionalData(sourceFullFileName, out axesN);

			if (layerExtensionNames.Length != axesN.Length)
			{
				throw new Exception("layerExtensionNames array not equal in length to the number of layers");
			}

			if (destFullFileName == sourceFullFileName)
				File.Delete(destFullFileName);
			JPFITS.FITSImage fi = new JPFITS.FITSImage(destFullFileName, true);
			fi.WriteImage(TypeCode.Double, false);

			for (int z = 0; z < axesN[2]; z++)//z is each layer of the cube
			{
				double[,] layer = new double[axesN[0], axesN[1]];

				Parallel.For(0, axesN[1], y =>
				{
					for (int x = 0; x < axesN[0]; x++)
						layer[x, y] = cube[z * axesN[1] * axesN[0] + y * axesN[0] + x];
				});

				fi = new FITSImage(destFullFileName, layer, false, true);
				fi.WriteImage(destFullFileName, layerExtensionNames[z], false, TypeCode.Double, true);
			}
		}

		/// <summary>Returns an array of all image table extension names in a FITS file. If there are no image table extensions, returns an empty array.</summary>
		/// <param name="FileName">The full file name to read from disk.</param>
		public static string[] GetAllExtensionNames(string FileName)
		{
			return FITSFILEOPS.GETALLEXTENSIONNAMES(FileName, "IMAGE");
		}

		/// <summary>Identifies and removes hot pixels from an image. The algorithm is not a simple find and replace, but assesses whether a pixel is part of a source<br />
		/// with legitimate high values or is a solitary or paired high value which is simply hot.</summary>
		/// <param name="image">A FITS image with hot pixels.</param>
		/// <param name="countThreshold">The pixel value above which a pixel might be considered to be hot. Recommend background + 5*sigma of the background noise stdv...NOT a high "hot" value.</param>
		/// <param name="Nhot">The maximum number of hot pixels in a hot pixel cluster. Recommend 1-3.</param>
		/// <param name="doParallel">Perform array scan with parallelism.</param>
		public static double[,] DeHotPixel(FITSImage image, double countThreshold, int Nhot, bool doParallel)
		{
			ParallelOptions opts = new ParallelOptions();
			if (doParallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			if (countThreshold == 0)
				countThreshold = image.Median + image.Std * 8;

			double[,] result = new double[image.Width, image.Height];
			for (int y = 0; y < image.Height; y++)
			{
				result[0, y] = image[0, y];
				result[image.Width - 1, y] = image[image.Width - 1, y];
			}
			for (int x = 0; x < image.Width; x++)
			{
				result[x, 0] = image[x, 0];
				result[x, image.Height - 1] = image[x, image.Height - 1];
			}

			//for (int y = 1; y < image.Height - 1; y++)
			Parallel.For(1, image.Height - 1, opts, y => 
			{
				int npix = 0;
				for (int x = 1; x < image.Width - 1; x++)
				{
					result[x, y] = image[x, y];
					npix = 0;

					if (image[x, y] > countThreshold)
					{
						npix++;

						if (image[x - 1, y] > countThreshold)
							npix++;
						if (image[x + 1, y] > countThreshold)
							npix++;
						if (image[x - 1, y - 1] > countThreshold)
							npix++;
						if (image[x, y - 1] > countThreshold)
							npix++;
						if (image[x + 1, y - 1] > countThreshold)
							npix++;
						if (image[x - 1, y + 1] > countThreshold)
							npix++;
						if (image[x, y + 1] > countThreshold)
							npix++;
						if (image[x + 1, y + 1] > countThreshold)
							npix++;

						if (npix <= Nhot)
							result[x, y] = JPMath.Median(new double[8] { image[x - 1, y], image[x + 1, y], image[x - 1, y - 1], image[x, y - 1], image[x + 1, y - 1], image[x - 1, y + 1], image[x, y + 1], image[x + 1, y + 1] });
					}
				}
			});

			return result;
		}

		///// <summary>Convert a FITS image on disk to an image.</summary>
		//static void ConvertToImage(string Source_FullFileName, string Destination_FullFileName, string file_type, string contrast_scaling, bool invert_colormap, bool do_parallel);  
		
		#endregion

		#region WRITING

		/// <summary>Write a FITS image to disk as a primary header and primary image from the FITSImage object with its existing file name.
		/// <para>If the file name already exists on disk, the primary unit will be overwritten, and any existing extensions will be appended to conserve the data file.</para></summary>
		/// <param name="precision">Byte precision at which to write the image data.</param>
		/// <param name="doParallel">Populate the underlying byte arrays for writing with parallelization.</param>
		public void WriteImage(TypeCode precision, bool doParallel)
		{
			ISEXTENSION = false;
			EXTNAME = null;

			WRITEIMAGE(precision, doParallel);
		}

		/// <summary>Write a FITS image to disk as a primary header and primary image from the FITSImage object with a given file name.
		/// <para>If the file name already exists on disk, the primary unit will be overwritten, and any existing extensions will be appended to conserve the data file.</para></summary>
		/// <param name="fullFileName">File name.</param>
		/// <param name="precision">Byte precision at which to write the image data.</param>
		/// <param name="doParallel">Populate the underlying byte arrays for writing with parallelization.</param>
		public void WriteImage(string fullFileName, TypeCode precision, bool doParallel)
		{
			ISEXTENSION = false;
			EXTNAME = null;

			FULLFILENAME = fullFileName;
			int index = FULLFILENAME.LastIndexOf("\\");
			FILENAME = FULLFILENAME.Substring(index + 1);
			FILEPATH = FULLFILENAME.Substring(0, index + 1);

			WRITEIMAGE(precision, doParallel);
		}

		/// <summary>Write a FITS image to disk as an extension from the FITSImage object with a given file name.</summary>
		/// <param name="fullFileName">File name. Pass the object&apos;s own FullFileName to write to its existing file name.
		/// <para>If the file doesn't yet exist on disk, then a new file will be created with an empty Primary Unit, and the image will be written as an extension.</para>
		/// <para>If the file does exist, then the extension will be written with the logic for the overwriteIfExists parameter, and</para>
		/// <para>the existing primary unit and any other extensions will be conserved to the file.</para></param>
		/// <param name="extensionName">The EXTNAME extension name of the IMAGE extension. 
		/// <para>If an empty string is passed, the first nameless IMAGE extension will be written to.</para>
		/// <para>If no such extension exists, the extension will be written as a new extension to the FITS file.</para></param>
		/// <param name="overwriteExtensionIfExists">If the image extension already exists it can be overwritten. If it exists and the option is given to not overwrite it, then an exception will be thrown.</param>
		/// <param name="precision">Byte precision at which to write the image data.</param>
		/// <param name="doParallel">Populate the underlying byte arrays for writing with parallelization.</param>
		public void WriteImage(string fullFileName, string extensionName, bool overwriteExtensionIfExists, TypeCode precision, bool doParallel)
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

			string[] HEADER = this.Header.GetFormattedHeaderBlock(false, false);
			FileStream fits_fs = new FileStream(FULLFILENAME, FileMode.Create);

			byte[] head = new byte[HEADER.Length * 80];
			for (int i = 0; i < HEADER.Length; i++)
				for (int j = 0; j < 80; j++)
					head[i * 80 + j] = (byte)HEADER[i][j];

			fits_fs.Write(head, 0, head.Length);//header is written

			FileStream buff_fs = new FileStream(DISKBUFFERFULLNAME, FileMode.Open, FileAccess.Read);

			int NBytes = (int)buff_fs.Length;//size of disk data buffer
			int buffsize = 1024 * 1024 * 64;//size of memory array buffer
			byte[] buff_arr = new byte[buffsize];

			int NBuffArrs = (int)(Math.Ceiling((double)(NBytes) / (double)(buffsize) - Double.Epsilon * 100));
			for (int i = 0; i < NBuffArrs; i++)
			{
				int bytestoread = buffsize;
				if (i == NBuffArrs - 1 && NBuffArrs != 1 || NBuffArrs == 1)
					bytestoread = NBytes - i * buffsize;
				buff_fs.Read(buff_arr, 0, bytestoread);
				fits_fs.Write(buff_arr, 0, bytestoread);
			}
			buff_fs.Close();
			int resid = (int)(Math.Ceiling((double)(fits_fs.Position) / 2880.0)) * 2880 - (int)(fits_fs.Position);
			byte[] resds = new byte[resid];
			fits_fs.Write(resds, 0, resid);
			fits_fs.Close();

			if (DeleteOrigDiskBuffer)
				File.Delete(DISKBUFFERFULLNAME);
		}

		public byte[] GetFormattedDataBlock(TypeCode precision, bool doParallel)
		{
			long NIm = ((long)NAXIS1) * ((long)NAXIS2) * ((long)Math.Abs(BITPIX / 8));//number of bytes in image
			int NBlocks = (int)(Math.Ceiling((double)(NIm) / 2880.0));
			int NBytesTot = NBlocks * 2880;
			double bscale = (double)BSCALE;
			double bzero = (double)BZERO;
			byte[] data = new byte[NBytesTot];

			ParallelOptions opts = new ParallelOptions();
			if (doParallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			switch (precision)
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				{
					Parallel.For(0, NAXIS2, opts, j =>
					{
						for (int i = 0; i < NAXIS1; i++)
						{
							int cc = (j * NAXIS1 + i) * 2;
							byte val = (byte)((DIMAGE[i, j] - bzero) / bscale);
							data[cc] = val;
						}
					});
					break;
				}

				case TypeCode.UInt16:
				case TypeCode.Int16:
				{
					Parallel.For(0, NAXIS2, opts, j =>
					{
						for (int i = 0; i < NAXIS1; i++)
						{
							int cc = (j * NAXIS1 + i) * 2;
							short val = (short)((DIMAGE[i, j] - bzero) / bscale);
							data[cc] = (byte)((val >> 8) & 0xff);
							data[cc + 1] = (byte)(val & 0xff);
						}
					});
					break;
				}

				case TypeCode.UInt32:
				case TypeCode.Int32:
				{
					Parallel.For(0, NAXIS2, opts, j =>
					{
						for (int i = 0; i < NAXIS1; i++)
						{
							int cc = (j * NAXIS1 + i) * 4;
							int val = (int)((DIMAGE[i, j] - bzero) / bscale);
							data[cc] = (byte)((val >> 24) & 0xff);
							data[cc + 1] = (byte)((val >> 16) & 0xff);
							data[cc + 2] = (byte)((val >> 8) & 0xff);
							data[cc + 3] = (byte)(val & 0xff);
						}
					});
					break;
				}

				case TypeCode.UInt64:
				case TypeCode.Int64:
				{
					Parallel.For(0, NAXIS2, opts, j =>
					{
						for (int i = 0; i < NAXIS1; i++)
						{
							int cc = (j * NAXIS1 + i) * 8;
							long val = (long)((DIMAGE[i, j] - bzero) / bscale);
							data[cc] = (byte)((val >> 56) & 0xff);
							data[cc + 1] = (byte)((val >> 48) & 0xff);
							data[cc + 2] = (byte)((val >> 40) & 0xff);
							data[cc + 3] = (byte)((val >> 32) & 0xff);
							data[cc + 4] = (byte)((val >> 24) & 0xff);
							data[cc + 5] = (byte)((val >> 16) & 0xff);
							data[cc + 6] = (byte)((val >> 8) & 0xff);
							data[cc + 7] = (byte)(val & 0xff);
						}
					});
					break;
				}

				case TypeCode.Single:
				{
					Parallel.For(0, NAXIS2, opts, j =>
					{
						byte[] sng = new byte[4];
						for (int i = 0; i < NAXIS1; i++)
						{
							int cc = (j * NAXIS1 + i) * 4;
							sng = BitConverter.GetBytes((float)((DIMAGE[i, j] - bzero) / bscale));
							data[cc] = sng[3];
							data[cc + 1] = sng[2];
							data[cc + 2] = sng[1];
							data[cc + 3] = sng[0];
						}
					});
					break;
				}

				case TypeCode.Double:
				{
					Parallel.For(0, NAXIS2, opts, j =>
					{
						byte[] dbl = new byte[8];
						for (int i = 0; i < NAXIS1; i++)
						{
							int cc = (j * NAXIS1 + i) * 8;
							dbl = BitConverter.GetBytes((DIMAGE[i, j] - bzero) / bscale);
							data[cc] = dbl[7];
							data[cc + 1] = dbl[6];
							data[cc + 2] = dbl[5];
							data[cc + 3] = dbl[4];
							data[cc + 4] = dbl[3];
							data[cc + 5] = dbl[2];
							data[cc + 6] = dbl[1];
							data[cc + 7] = dbl[0];
						}
					});
					break;
				}
			}

			return data;
		}
		
		#endregion

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
				int index;
				FULLFILENAME = fullFileName;
				index = FULLFILENAME.LastIndexOf("\\");
				FILENAME = FULLFILENAME.Substring(index + 1);
				FILEPATH = FULLFILENAME.Substring(0, index + 1);
			}
			else
			{
				FULLFILENAME = "";
				FILENAME = "";
				FILEPATH = "";
			}

			HEADER = new FITSHeader(mayContainExtensions, null);
			EATIMAGEHEADER();

			WORLDCOORDINATESOLUTION = new JPFITS.WorldCoordinateSolution();
		}

		/// <summary>Create a FITSImage object from an array object containing existing data.
		/// <para>Image data is maintained at or converted to double precision.</para></summary>
		/// <param name="fullFileName">File name.</param>
		/// <param name="imageArrayVectorData">The data array or vector to use for the FITS image data. The precision and rank of the underlying array will be automatically determined. Vectors will be converted to an array with column-rank.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and standard deviation of the image data - saves time if you don't need those.</param>
		/// <param name="doParallel">Populate the FITSImage object ImageData and perform stats (if true) with parallelization.</param>
		public FITSImage(string fullFileName, Object imageArrayVectorData, bool doStats, bool doParallel)
		{
			if (!(imageArrayVectorData.GetType().IsArray))
			{
				throw new Exception("Error: Object 'ImageData' is not an array.");
			}

			int rank = ((Array)imageArrayVectorData).Rank;

			if (rank > 2)
			{
				throw new Exception("Error: Rank of Object 'ImageData' is not a 1D or 2D array. Use other interface functions for creating larger dimensional FITS primary data.");
			}

			NAXIS = 2;
			if (rank == 1)
			{
				NAXIS1 = 1;
				NAXIS2 = ((Array)imageArrayVectorData).Length;
			}
			else
			{
				NAXIS1 = ((Array)imageArrayVectorData).GetLength(0);
				NAXIS2 = ((Array)imageArrayVectorData).GetLength(1);
			}

			DIMAGE = new double[NAXIS1, NAXIS2];

			TypeCode type = Type.GetTypeCode((((Array)imageArrayVectorData).GetType()).GetElementType());

			ParallelOptions opts =	new ParallelOptions();
			if (doParallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			switch (type)
			{
				case TypeCode.Double:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXIS2, opts, x =>
						{
							DIMAGE[0, x] = ((double[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXIS1, opts, x =>
						{
							for (int y = 0; y < NAXIS2; y++)
								DIMAGE[x, y] = ((double[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.Single:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXIS2, opts, x =>
						{
							DIMAGE[0, x] = (double)((float[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXIS1, opts, x =>
						{
							for (int y = 0; y < NAXIS2; y++)
								DIMAGE[x, y] = (double)((float[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.UInt16:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXIS2, opts, x =>
						{
							DIMAGE[0, x] = (double)((ushort[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXIS1, opts, x =>
						{
							for (int y = 0; y < NAXIS2; y++)
								DIMAGE[x, y] = (double)((ushort[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.Int16:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXIS2, opts, x =>
						{
							DIMAGE[0, x] = (double)((short[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXIS1, opts, x =>
						{
							for (int y = 0; y < NAXIS2; y++)
								DIMAGE[x, y] = (double)((short[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.UInt32:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXIS2, opts, x =>
						{
							DIMAGE[0, x] = (double)((uint[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXIS1, opts, x =>
						{
							for (int y = 0; y < NAXIS2; y++)
								DIMAGE[x, y] = (double)((uint[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.Int32:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXIS2, opts, x =>
						{
							DIMAGE[0, x] = (double)((int[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXIS1, opts, x =>
						{
							for (int y = 0; y < NAXIS2; y++)
								DIMAGE[x, y] = (double)((int[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.UInt64:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXIS2, opts, x =>
						{
							DIMAGE[0, x] = (double)((ulong[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXIS1, opts, x =>
						{
							for (int y = 0; y < NAXIS2; y++)
								DIMAGE[x, y] = (double)((ulong[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				case TypeCode.Int64:
				{
					if (rank == 1)
					{
						Parallel.For(0, NAXIS2, opts, x =>
						{
							DIMAGE[0, x] = (double)((long[])imageArrayVectorData)[x];
						});
					}
					else
					{
						Parallel.For(0, NAXIS1, opts, x =>
						{
							for (int y = 0; y < NAXIS2; y++)
								DIMAGE[x, y] = (double)((long[,])imageArrayVectorData)[x, y];
						});
					}

					break;
				}

				default:
				{
					throw new Exception("Data of TypeCode:" + type.ToString() + " not suported.");
				}

			}

			ISEXTENSION = false;
			EXTNAME = null;

			FULLFILENAME = fullFileName;
			int index = FULLFILENAME.LastIndexOf("\\");
			FILENAME = FULLFILENAME.Substring(index + 1);
			FILEPATH = FULLFILENAME.Substring(0, index + 1);

			MIN = 0.0;
			MAX = 0.0;
			MEDIAN = 0.0;
			MEAN = 0.0;
			STD = 0.0;
			SUM = 0.0;

			HEADER_POP = true;
			DATA_POP = true;
			STATS_POP = doStats;

			//create default header
			HEADER = new FITSHeader(true, DIMAGE);
			EATIMAGEHEADER();

			if (STATS_POP)
				StatsUpD(doParallel);

			WORLDCOORDINATESOLUTION = new JPFITS.WorldCoordinateSolution();
		}

		/// <summary>Create a FITSImage object with Primary image data loaded to RAM memory from disk.
		/// <para>Image data is loaded as double precision independent of storage precision on disk.</para></summary>
		/// <param name="fullFileName">File name.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax].  Pass null or Range[0] = -1 to default to full image size.</param>
		/// <param name="populateHeader">Optionally populate the header - sometimes you just want the data, and can skip reading the non-essential header lines.</param>
		/// <param name="populateData">Optionally populate the image data array - sometimes you just want the header and don't need the data.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and standard deviation of the image data (if populated) - saves time if you don't need those.</param>
		/// <param name="doParallel">Populate the FITSImage object ImageData and perform stats (if true) with parallelization.</param>
		public FITSImage(string fullFileName, int[]? range, bool populateHeader, bool populateData, bool doStats, bool doParallel)
		{
			HEADER_POP = populateHeader;
			DATA_POP = populateData;
			STATS_POP = doStats;
			ISEXTENSION = false;
			EXTNAME = null;

			if (DATA_POP == false)
			{
				doStats = false;//can't do stats on data that isn't read
				STATS_POP = false;
			}

			FULLFILENAME = fullFileName;
			int index = FULLFILENAME.LastIndexOf("\\");
			FILENAME = FULLFILENAME.Substring(index + 1);
			FILEPATH = FULLFILENAME.Substring(0, index + 1);

			FileStream fs = new FileStream(FULLFILENAME, FileMode.Open);
			ArrayList header = new ArrayList();
			bool hasext = false;
			if (!FITSFILEOPS.SCANPRIMARYUNIT(fs, false, ref header, ref hasext))
			{
				fs.Close();
				throw new Exception("File '" + fullFileName + "' not formatted as FITS file.");
			}
			HEADER = new JPFITS.FITSHeader(header, HEADER_POP);
			EATIMAGEHEADER();

			if (DATA_POP == true)
				READIMAGE(fs, range, doParallel);

			fs.Close();

			if (STATS_POP)
				StatsUpD(doParallel);

			WORLDCOORDINATESOLUTION = new JPFITS.WorldCoordinateSolution();
		}

		/// <summary>Create a FITSImage object with extension image data loaded to RAM memory from disk.
		/// <para>Image data is loaded as double precision independent of storage precision on disk.</para></summary>
		/// <param name="fullFileName">File name.</param>
		/// <param name="extensionName">The EXTNAME extension name of the image. If an empty string is passed, the first nameless IMAGE extension will be read. Exception if no such extension exits.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax].  Pass null or Range[0] = -1 to default to full image size.</param>
		/// <param name="populateHeader">Optionally populate the header - sometimes you just want the data, and can skip reading the non-essential header lines.</param>
		/// <param name="populateData">Optionally populate the image data array - sometimes you just want the header and don't need the data.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and standard deviation of the image data (if populated) - saves time if you don't need those.</param>
		/// <param name="doParallel">Populate the FITSImage object ImageData and perform stats (if true) with parallelization.</param>
		public FITSImage(string fullFileName, string extensionName, int[]? range, bool populateHeader, bool populateData, bool doStats, bool doParallel)
		{
			FileStream fs = new FileStream(fullFileName, FileMode.Open);
			bool hasext = false;
			ArrayList header = null;
			if (!FITSFILEOPS.SCANPRIMARYUNIT(fs, true, ref header, ref hasext) || !hasext)
			{
				fs.Close();
				if (!hasext)
					throw new Exception("File '" + fullFileName + "' indicates no extensions present.");
				else
					throw new Exception("File '" + fullFileName + "' not formatted as FITS file.");
			}

			header = new ArrayList();
			long extensionstartposition, extensionendposition, tableendposition, pcount, theap;

			if (!FITSFILEOPS.SEEKEXTENSION(fs, "IMAGE", extensionName, ref header, out extensionstartposition, out extensionendposition, out tableendposition, out pcount, out theap))
			{
				fs.Close();
				throw new Exception("Could not find IMAGE extension with name '" + extensionName + "'");
			}

			HEADER = new JPFITS.FITSHeader(header, populateHeader);
			EATIMAGEHEADER();
			HEADER.SetKey(0, "SIMPLE", "T", "");

			HEADER_POP = populateHeader;
			DATA_POP = populateData;
			STATS_POP = doStats;
			ISEXTENSION = false;//because changed to primary above
			EXTNAME = extensionName;

			if (DATA_POP == false)
			{
				doStats = false;//can't do stats on data that isn't read
				STATS_POP = false;
			}

			FULLFILENAME = fullFileName;
			int index = FULLFILENAME.LastIndexOf("\\");
			FILENAME = FULLFILENAME.Substring(index + 1);
			FILEPATH = FULLFILENAME.Substring(0, index + 1);

			this.FileName = this.FileName.Substring(0, this.FileName.LastIndexOf(".")) + "_" + EXTNAME + this.FileName.Substring(this.FileName.LastIndexOf("."));

			if (DATA_POP == true)
				READIMAGE(fs, range, doParallel);

			fs.Close();

			if (STATS_POP)
				StatsUpD(doParallel);

			WORLDCOORDINATESOLUTION = new JPFITS.WorldCoordinateSolution();
		}

		/// <summary>Create a FITSImage object with extension image data loaded to RAM memory from disk. <br />Useful when extensions are not named with EXTNAME keyword. Will return the image at the extension number if they are named regardless.
		/// <para>Image data is loaded as double precision independent of storage precision on disk.</para></summary>
		/// <param name="fullFileName">File name.</param>
		/// <param name="extensionNumber">The ONE-BASED extension number of the image.</param>
		/// <param name="range">Range is ZERO based 1-D int array [xmin xmax ymin ymax].  Pass null or Range[0] = -1 to default to full image size.</param>
		/// <param name="populateHeader">Optionally populate the header - sometimes you just want the data, and can skip reading the non-essential header lines.</param>
		/// <param name="populateData">Optionally populate the image data array - sometimes you just want the header and don't need the data.</param>
		/// <param name="doStats">Optionally perform the statistics to determine min, max, mean, median, and standard deviation of the image data (if populated) - saves time if you don't need those.</param>
		/// <param name="doParallel">Populate the FITSImage object ImageData and perform stats (if true) with parallelization.</param>
		public FITSImage(string fullFileName, int extensionNumber, int[]? range, bool populateHeader, bool populateData, bool doStats, bool doParallel)
			: this(fullFileName, "_FINDEXTNUM_" + extensionNumber.ToString(), range, populateHeader, populateData, doStats, doParallel)
		{
			//FITSImage(FullFileName, "_FINDEXTNUM_" + extensionNumber.ToString(), range, populateHeader, populateData, doStats, doParallel);

			FileStream fs = new FileStream(fullFileName, FileMode.Open);
			bool hasext = false;
			ArrayList header = null;
			if (!FITSFILEOPS.SCANPRIMARYUNIT(fs, true, ref header, ref hasext) || !hasext)
			{
				fs.Close();
				if (!hasext)
					throw new Exception("File '" + fullFileName + "' indicates no extensions present.");
				else
					throw new Exception("File '" + fullFileName + "' not formatted as FITS file.");
			}

			header = new ArrayList();
			long extensionstartposition, extensionendposition, tableendposition, pcount, theap;
			if (!FITSFILEOPS.SEEKEXTENSION(fs, "IMAGE", extensionNumber, ref header, out extensionstartposition, out extensionendposition, out tableendposition, out pcount, out theap))
			{
				fs.Close();
				throw new Exception("Could not find IMAGE extension number '" + extensionNumber + "'");
			}

			HEADER = new JPFITS.FITSHeader(header, populateHeader);
			EATIMAGEHEADER();
			HEADER.SetKey(0, "SIMPLE", "T", "");

			HEADER_POP = populateHeader;
			DATA_POP = populateData;
			STATS_POP = doStats;
			ISEXTENSION = false;//because changed to primary above
			EXTNAME = HEADER.GetKeyValue("EXTNAME");
			if (EXTNAME == "")
				EXTNAME = "EXT_" + extensionNumber.ToString("000000");

			if (DATA_POP == false)
			{
				doStats = false;//can't do stats on data that isn't read
				STATS_POP = false;
			}

			FULLFILENAME = FullFileName;
			int index = FULLFILENAME.LastIndexOf("\\");
			FILENAME = FULLFILENAME.Substring(index + 1);
			FILEPATH = FULLFILENAME.Substring(0, index + 1);
			this.FileName = this.FileName.Substring(0, this.FileName.LastIndexOf(".")) + "_" + EXTNAME + this.FileName.Substring(this.FileName.LastIndexOf("."));

			if (DATA_POP == true)
				READIMAGE(fs, range, doParallel);

			fs.Close();

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
		public FITSImage(string fullFileName, string DiskUCharBufferName, TypeCode Precision, int NAxis1, int NAxis2)
		{
			ISEXTENSION = false;
			EXTNAME = null;

			FULLFILENAME = fullFileName;
			int index = FULLFILENAME.LastIndexOf("\\");
			FILENAME = FULLFILENAME.Substring(index + 1);
			FILEPATH = FULLFILENAME.Substring(0, index + 1);
			DISKBUFFERFULLNAME = DiskUCharBufferName;
			FROMDISKBUFFER = true;

			DIMAGE = new double[NAxis1, NAxis2];//default fill

			HEADER = new FITSHeader(true, DIMAGE);
			this.Header.SetBITPIXNAXISBSCZ(Precision, DIMAGE);
			EATIMAGEHEADER();

			WORLDCOORDINATESOLUTION = new JPFITS.WorldCoordinateSolution();
		}

		#endregion
	}
}
