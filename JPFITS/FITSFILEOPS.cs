using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
#nullable enable

namespace JPFITS
{
	///<summary>FITSFILEOPS static class for facilitating interaction with FITS data on disk.</summary>
	public class FITSFILEOPS
	{
		/// <summary>Array formatting options for the data unit returned by the READDATAUNIT method.</summary>
		public enum ImageDataUnitFormatting
		{
			/// <summary>
			/// The Array is returned as the rank indicated by the NAXIS keyword
			/// </summary>
			Default,

			/// <summary>
			/// If the default Array would be a vector, return it as a 2D horizontal array. Indexing will then be [i, 0].
			/// </summary>
			VectorAsHorizontalTable,

			/// <summary>
			/// If the default Array would be a vector, return it as a 2D vertical array. Indexing will then be [0, i].
			/// </summary>
			VectorAsVerticalTable,

			/// <summary>
			/// If the range dimensions indicate a vector when reading from a table or cube, or a table when reading from a cube, then return the Array formatted as the range rank.
			/// </summary>
			ArrayAsRangeRank
		}

		/// <summary>Scans the primary header unit and data of a FITS file. Returns false if the file is not a FITS file.</summary>
		/// <param name="fs">The FileStream of the FITS file, positioned at the start of the stream.</param>
		/// <param name="scanpastprimarydata">True to set the FileStream fs position to the end of the data block, otherwise the fs position will be at the end of the primary header block, i.e. at the beginning of the primary data.</param>
		/// <param name="header_return">Returns the header of the extension as an ArrayList with each 80-character header line being a String^ member of this list. Pass nullptr if not required.</param>
		/// <param name="has_extensions">Returns whether or not the FITS file may contain extensions.</param>
		public static bool SCANPRIMARYUNIT(FileStream fs, bool scanpastprimarydata, ref ArrayList? header_return, out bool has_extensions)
		{
			byte[] c = new byte[2880];
			int naxis = -1, bitpix = -1, Nnaxisn = 1;
			int[] naxisn = new int[0];
			has_extensions = false;

			//read primary header
			bool endheader = false, FITSformat = false, extend = false;

			while (!endheader)
			{
				//read 2880 block
				fs.Read(c, 0, 2880);

				for (int i = 0; i < 36; i++)
				{
					string line = System.Text.Encoding.ASCII.GetString(c, i * 80, 80);

					if (header_return != null)
						header_return.Add(line);

					if (!FITSformat && i == 0)
						if (line.Substring(0, 8).Trim() == "SIMPLE")
							if (line.Substring(10, 20).Trim() == "T")
							{
								FITSformat = true;
								continue;
							}
							else
							{
								endheader = true;
								break;
							}
						else
						{
							endheader = true;
							break;
						}

					if (bitpix == -1)
						if (line.Substring(0, 8).Trim() == "BITPIX")
						{
							bitpix = Convert.ToInt32(line.Substring(10, 20));
							continue;
						}

					if (naxis == -1)
						if (line.Substring(0, 8).Trim() == "NAXIS")
						{
							naxis = Convert.ToInt32(line.Substring(10, 20));
							naxisn = new int[naxis];
							continue;
						}

					if (Nnaxisn <= naxis)
						if (line.Substring(0, 8).Trim() == ("NAXIS" + Nnaxisn.ToString()))
						{
							naxisn[Nnaxisn - 1] = Convert.ToInt32(line.Substring(10, 20));
							Nnaxisn++;
							continue;
						}

					if (!extend)
						if (line.Substring(0, 8).Trim() == "EXTEND")
						{
							extend = true;
							if (line.Substring(10, 20).Trim() == "T")
							{
								has_extensions = true;
								continue;
							}
						}

					if (line.Substring(0, 8).Trim() == "END")
					{
						endheader = true;
						break;
					}
				}

				if (fs.Position >= fs.Length && !endheader)
				{
					FITSformat = false;
					break;
				}
			}
			//now at end of primary header block

			if (!FITSformat)
				return false;

			//if primary header has image data, may skip past it, otherwise stay at primary data start
			if (scanpastprimarydata)
				if (naxis != 0)
				{
					long NBytes = (long)(Math.Abs(bitpix)) / 8;
					for (int i = 0; i < naxisn.Length; i++)
						NBytes *= naxisn[i];
					fs.Seek((long)(Math.Ceiling(((double)(NBytes)) / 2880) * 2880), SeekOrigin.Current);//should now be at the 1st extension header
				}

			return true;
		}

		/// <summary>Scans the header unit and data of a FITS file. Returns false if the file is not a FITS file. Populates Primary Image Unit keywords.</summary>
		/// <param name="fs">The FileStream of the FITS file, positioned at the start of the stream or at the start of any image extension unit.</param>
		/// <param name="scanpastdataunit">True to set the FileStream fs position to the end of the data block, otherwise the fs position will be at the end of the primary header block, i.e. at the beginning of the primary data.</param>
		/// <param name="header_return">Returns the header of the extension as an ArrayList with each 80-character header line being a String^ member of this list. Pass nullptr if not required.</param>
		/// <param name="has_extensions">Returns whether or not the FITS file may contain extensions.</param>
		/// <param name="bitpix">BITPIX keyword value. Bits per pixel keyword, following the meaning of the FITS convection.</param>
		/// <param name="naxisn">An array of length NAXIS containing the dimension (length) of each axis. If there are no axes, is an emtpty array. NAXIS1 = naxisn[0]; NAXIS2 = naxisn[1]; etc.</param>
		/// <param name="bscale">The BSCALE keyword value. Default is one (1) if not present.</param>
		/// <param name="bzero">The BZERO keyword value. Default is zero (0) if not present.</param>
		public static bool SCANIMAGEHEADERUNIT(FileStream fs, bool scanpastdataunit, ref ArrayList? header_return, out bool has_extensions, out int bitpix, out int[] naxisn, out double bscale, out double bzero)
		{
			bitpix = -1;
			int naxis = -1;
			naxisn = new int[0];
			bzero = -1;
			bscale = -1;
			has_extensions = false;

			//read primary header
			byte[] c = new byte[2880];
			bool endheader = false, FITSformat = false, extend = false;
			int Nnaxisn = 1;

			while (!endheader)
			{
				//read 2880 block
				fs.Read(c, 0, 2880);

				for (int i = 0; i < 36; i++)
				{
					string line = System.Text.Encoding.ASCII.GetString(c, i * 80, 80);

					if (header_return != null)
						header_return.Add(line);

					if (!FITSformat && i == 0)
					{
						if (line.Substring(0, 8).Trim() == "SIMPLE")
							if (line.Substring(10, 20).Trim() == "T")
							{
								FITSformat = true;
								continue;
							}
							else
							{
								endheader = true;
								break;
							}
						else if (line.Substring(0, 8).Trim() == "XTENSION")
							if (line.Substring(10, 20).Trim(new char[2] { ' ', '\'' }) == "IMAGE")
							{
								FITSformat = true;
								continue;
							}
							else
							{
								endheader = true;
								break;
							}
						else
						{
							endheader = true;
							break;
						}
					}

					if (bitpix == -1)
						if (line.Substring(0, 8).Trim() == "BITPIX")
						{
							bitpix = Convert.ToInt32(line.Substring(10, 20));
							continue;
						}

					if (naxis == -1)
						if (line.Substring(0, 8).Trim() == "NAXIS")
						{
							naxis = Convert.ToInt32(line.Substring(10, 20));
							naxisn = new int[naxis];
							continue;
						}

					if (Nnaxisn <= naxis)
						if (line.Substring(0, 8).Trim() == ("NAXIS" + Nnaxisn.ToString()))
						{
							naxisn[Nnaxisn - 1] = Convert.ToInt32(line.Substring(10, 20));
							Nnaxisn++;
							continue;
						}

					if (bzero == -1)
						if (line.Substring(0, 8).Trim() == ("BZERO"))
							bzero = Convert.ToDouble(line.Substring(10, 20));

					if (bscale == -1)
						if (line.Substring(0, 8).Trim() == ("BSCALE"))
							bscale = Convert.ToDouble(line.Substring(10, 20));

					if (!extend)
						if (line.Substring(0, 8).Trim() == "EXTEND")
						{
							extend = true;
							if (line.Substring(10, 20).Trim() == "T")
							{
								has_extensions = true;
								continue;
							}
						}

					if (line.Substring(0, 8).Trim() == "END")
					{
						endheader = true;
						break;
					}
				}

				if (fs.Position >= fs.Length && !endheader)
				{
					FITSformat = false;
					break;
				}
			}
			//now at end of primary header block

			if (bzero == -1)
				bzero = 0;
			if (bscale == -1)
				bscale = 1;

			if (!FITSformat)
				return false;

			//if primary header has image data, may skip past it, otherwise stay at primary data start
			if (scanpastdataunit)
				if (naxis != 0)
				{
					long NBytes = (long)(Math.Abs(bitpix)) / 8 * JPMath.Product(naxisn);
					fs.Seek((long)(Math.Ceiling(((double)(NBytes)) / 2880) * 2880), SeekOrigin.Current);//should now be at the 1st extension header
				}

			return true;
		}

		/// <summary>Reads the data unit from a FITS file and returns its Array. Supports data units with up to 3 axes. May return either a vector, table, or cube.</summary>
		/// <param name="fs">The FileStream of the FITS file, positioned at the start of the primary or image extension data unit.</param>
		/// <param name="range">Pass null or range[0] = -1 to default to full data unit size. Otherwise range is ZERO BASED 1-D int array [xmin xmax] or [xmin xmax ymin ymax]  or [xmin xmax ymin ymax zmin zmax] to return a sub-array.</param>
		/// <param name="doParallel">Populate the Array object with parallelization after serial disk read.</param>
		/// <param name="bitpix">The BITPIX keyword value of the data unit header.</param>
		/// <param name="naxisn">An array containing the values of the NAXISn keywords from the data unit header. Specifies the rank of the return Array, i.e., if naxisn.Length == 1, then it is a vector, if 2 then a table, if 3 then a cube. The value may change from the input given the DataUnitReturn options.</param>
		/// <param name="bscale">The BSCALE keyword value of the data unit header.</param>
		/// <param name="bzero">The BZERO keyword value of the data unit header.</param>
		/// <param name="returnOptions">Options for formatting the return Array rank and dimensions.</param>
		public static Array READIMAGEDATAUNIT(FileStream fs, int[]? range, bool doParallel, int bitpix, ref int[] naxisn, double bscale, double bzero, ImageDataUnitFormatting returnOptions = ImageDataUnitFormatting.Default)
		{
			if (range == null || range[0] == -1)//then it is a full frame read
				if (naxisn.Length == 1)
					range = new int[2] { 0, naxisn[0] - 1 };
				else if (naxisn.Length == 2)
					range = new int[4] { 0, naxisn[0] - 1, 0, naxisn[1] - 1 };
				else if (naxisn.Length == 3)
					range = new int[6] { 0, naxisn[0] - 1, 0, naxisn[1] - 1, 0, naxisn[2] - 1 };
				else
					throw new Exception("Error: I can only read up to 3-dimensional data units - SORRY!");

			if (naxisn.Length >= 1)
				if (range[1] >= naxisn[0])
					throw new Exception("Requested range exceeds data unit size: NAXIS1 = " + naxisn[0] + "; range[1] = " + range[1] + "; range is zero-based.");
				else if (range[0] > range[1])
					throw new Exception("Requested range doesn't make sense: range[0] = " + range[0] + " is greater than range[1] = " + range[1]);

			if (naxisn.Length >= 2)
				if (range[3] >= naxisn[1])
					throw new Exception("Requested range exceeds data unit size: NAXIS2 = " + naxisn[1] + "; range[3] = " + range[3] + "; range is zero-based.");
				else if (range[2] > range[3])
					throw new Exception("Requested range doesn't make sense: range[2] = " + range[2] + " is greater than range[3] = " + range[3]);

			if (naxisn.Length >= 3)
				if (range[5] >= naxisn[2])
					throw new Exception("Requested range exceeds data unit size: NAXIS3 = " + naxisn[2] + "; range[5] = " + range[5] + "; range is zero-based.");
				else if (range[4] > range[5])
					throw new Exception("Requested range doesn't make sense: range[4] = " + range[4] + " is greater than range[5] = " + range[5]);

			int bpix = Math.Abs(bitpix);
			int NBytes = (int)JPMath.Product(naxisn) * (bpix / 8);
			byte[] arr = new byte[NBytes];
			fs.Read(arr, 0, NBytes);//fastest to just read the entire data even if only subimage will be used - though this may needs to be checked with new m2 faster drives!!!!!!!!!!!!!!

			ParallelOptions opts = new ParallelOptions();
			if (doParallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			if (naxisn.Length == 1)//then a vector return
			{
				switch (bitpix)
				{
					case 8:
					{
						if (returnOptions == ImageDataUnitFormatting.Default)
						{
							double[] dvector = new double[range[1] - range[0] + 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
							{
								dvector[i - range[0]] = ((double)arr[i]) * bscale + bzero;
							});
							return dvector;
						}
						else if (returnOptions == ImageDataUnitFormatting.VectorAsHorizontalTable)
						{
							double[,] dtable = new double[range[1] - range[0] + 1, 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
							{
								dtable[i - range[0], 0] = ((double)arr[i]) * bscale + bzero;
							});
							naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };
							return dtable;
						}
						else if (returnOptions == ImageDataUnitFormatting.VectorAsVerticalTable)
						{
							double[,] dtable = new double[1, range[1] - range[0] + 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
							{
								dtable[0, i - range[0]] = ((double)arr[i]) * bscale + bzero;
							});
							naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };
							return dtable;
						}
						break;
					}

					case 16:
					{
						if (returnOptions == ImageDataUnitFormatting.Default)
						{
							double[] dvector = new double[range[1] - range[0] + 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
							{
								int cc = i * 2;
								short val = (short)((arr[cc] << 8) | arr[cc + 1]);
								dvector[i - range[0]] = ((double)val) * bscale + bzero;
							});
							return dvector;
						}
						else if (returnOptions == ImageDataUnitFormatting.VectorAsHorizontalTable)
						{
							double[,] dtable = new double[range[1] - range[0] + 1, 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
							{
								int cc = i * 2;
								short val = (short)((arr[cc] << 8) | arr[cc + 1]);
								dtable[i - range[0], 0] = ((double)val) * bscale + bzero;
							});
							naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };
							return dtable;
						}
						else if (returnOptions == ImageDataUnitFormatting.VectorAsVerticalTable)
						{
							double[,] dtable = new double[1, range[1] - range[0] + 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
							{
								int cc = i * 2;
								short val = (short)((arr[cc] << 8) | arr[cc + 1]);
								dtable[0, i - range[0]] = ((double)val) * bscale + bzero;
							});
							naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };
							return dtable;
						}
						break;
					}

					case 32:
					{
						if (returnOptions == ImageDataUnitFormatting.Default)
						{
							double[] dvector = new double[range[1] - range[0] + 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
							{
								int cc = i * 4;
								int val = (arr[cc] << 24) | (arr[cc + 1] << 16) | (arr[cc + 2] << 8) | arr[cc + 3];
								dvector[i - range[0]] = ((double)val) * bscale + bzero;
							});
							return dvector;
						}
						else if (returnOptions == ImageDataUnitFormatting.VectorAsHorizontalTable)
						{
							double[,] dtable = new double[range[1] - range[0] + 1, 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
							{
								int cc = i * 4;
								int val = (arr[cc] << 24) | (arr[cc + 1] << 16) | (arr[cc + 2] << 8) | arr[cc + 3];
								dtable[i - range[0], 0] = ((double)val) * bscale + bzero;
							});
							naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };
							return dtable;
						}
						else if (returnOptions == ImageDataUnitFormatting.VectorAsVerticalTable)
						{
							double[,] dtable = new double[1, range[1] - range[0] + 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
							{
								int cc = i * 4;
								int val = (arr[cc] << 24) | (arr[cc + 1] << 16) | (arr[cc + 2] << 8) | arr[cc + 3];
								dtable[0, i - range[0]] = ((double)val) * bscale + bzero;
							});
							naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };
							return dtable;
						}
						break;
					}

					case 64:
					{
						if (returnOptions == ImageDataUnitFormatting.Default)
						{
							double[] dvector = new double[range[1] - range[0] + 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
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
								dvector[i - range[0]] = ((double)BitConverter.ToInt64(dbl, 0)) * bscale + bzero;
							});
							return dvector;
						}
						else if (returnOptions == ImageDataUnitFormatting.VectorAsHorizontalTable)
						{
							double[,] dtable = new double[range[1] - range[0] + 1, 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
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
								dtable[i - range[0], 0] = ((double)BitConverter.ToInt64(dbl, 0)) * bscale + bzero;								
							});
							naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };
							return dtable;
						}
						else if (returnOptions == ImageDataUnitFormatting.VectorAsVerticalTable)
						{
							double[,] dtable = new double[1, range[1] - range[0] + 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
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
								dtable[0, i - range[0]] = ((double)BitConverter.ToInt64(dbl, 0)) * bscale + bzero;
							});
							naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };
							return dtable;
						}
						break;
					}

					case -32:
					{
						if (returnOptions == ImageDataUnitFormatting.Default)
						{
							double[] dvector = new double[range[1] - range[0] + 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
							{
								int cc = i * 4;
								byte[] flt = new byte[4];
								flt[3] = arr[cc];
								flt[2] = arr[cc + 1];
								flt[1] = arr[cc + 2];
								flt[0] = arr[cc + 3];
								dvector[i - range[0]] = ((double)BitConverter.ToSingle(flt, 0)) * bscale + bzero;
							});
							return dvector;
						}
						else if (returnOptions == ImageDataUnitFormatting.VectorAsHorizontalTable)
						{
							double[,] dtable = new double[range[1] - range[0] + 1, 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
							{
								int cc = i * 4;
								byte[] flt = new byte[4];
								flt[3] = arr[cc];
								flt[2] = arr[cc + 1];
								flt[1] = arr[cc + 2];
								flt[0] = arr[cc + 3];
								dtable[i - range[0], 0] = ((double)BitConverter.ToSingle(flt, 0)) * bscale + bzero;
							});
							naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };
							return dtable;
						}
						else if (returnOptions == ImageDataUnitFormatting.VectorAsVerticalTable)
						{
							double[,] dtable = new double[1, range[1] - range[0] + 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
							{
								int cc = i * 4;
								byte[] flt = new byte[4];
								flt[3] = arr[cc];
								flt[2] = arr[cc + 1];
								flt[1] = arr[cc + 2];
								flt[0] = arr[cc + 3];
								dtable[0, i - range[0]] = ((double)BitConverter.ToSingle(flt, 0)) * bscale + bzero;
							});
							naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };
							return dtable;
						}
						break;
					}

					case -64:
					{
						if (returnOptions == ImageDataUnitFormatting.Default)
						{
							double[] dvector = new double[range[1] - range[0] + 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
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
								dvector[i - range[0]] = BitConverter.ToDouble(dbl, 0) * bscale + bzero;
							});
							return dvector;
						}
						else if (returnOptions == ImageDataUnitFormatting.VectorAsHorizontalTable)
						{
							double[,] dtable = new double[range[1] - range[0] + 1, 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
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
								dtable[i - range[0], 0] = BitConverter.ToDouble(dbl, 0) * bscale + bzero;
							});
							naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };
							return dtable;
						}
						else if (returnOptions == ImageDataUnitFormatting.VectorAsVerticalTable)
						{
							double[,] dtable = new double[1, range[1] - range[0] + 1];
							Parallel.For(range[0], range[1] + 1, opts, i =>
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
								dtable[0, i - range[0]] = BitConverter.ToDouble(dbl, 0) * bscale + bzero;
							});
							naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };
							return dtable;
						}
						break;
					}
				}
			}

			if (naxisn.Length == 2)//then a table or image return
			{
				double[,] dimage = new double[range[1] - range[0] + 1, range[3] - range[2] + 1];
				int naxis0 = naxisn[0];

				switch (bitpix)
				{
					case 8:
					{
						Parallel.For(range[2], range[3] + 1, opts, j =>
						{
							int cc = j * naxis0 + range[0];
							for (int i = range[0]; i <= range[1]; i++)
								dimage[i - range[0], j - range[2]] = ((double)arr[cc + i]) * bscale + bzero;
						});
						break;
					}

					case 16:
					{
						Parallel.For(range[2], range[3] + 1, opts, j =>
						{
							short val;
							int cc = (j * naxis0 + range[0]) * 2;
							for (int i = range[0]; i <= range[1]; i++)
							{
								val = (short)((arr[cc] << 8) | arr[cc + 1]);
								dimage[i - range[0], j - range[2]] = ((double)val) * bscale + bzero;
								cc += 2;
							}
						});
						break;
					}

					case 32:
					{
						Parallel.For(range[2], range[3] + 1, opts, j =>
						{
							int val;
							int cc = (j * naxis0 + range[0]) * 4;
							for (int i = range[0]; i <= range[1]; i++)
							{
								val = (arr[cc] << 24) | (arr[cc + 1] << 16) | (arr[cc + 2] << 8) | arr[cc + 3];
								dimage[i - range[0], j - range[2]] = ((double)val) * bscale + bzero;
								cc += 4;
							}
						});
						break;
					}

					case 64:
					{
						Parallel.For(range[2], range[3] + 1, opts, j =>
						{
							int cc = (j * naxis0 + range[0]) * 8;
							byte[] dbl = new byte[8];
							for (int i = range[0]; i <= range[1]; i++)
							{
								dbl[7] = arr[cc];
								dbl[6] = arr[cc + 1];
								dbl[5] = arr[cc + 2];
								dbl[4] = arr[cc + 3];
								dbl[3] = arr[cc + 4];
								dbl[2] = arr[cc + 5];
								dbl[1] = arr[cc + 6];
								dbl[0] = arr[cc + 7];
								dimage[i - range[0], j - range[2]] = ((double)BitConverter.ToInt64(dbl, 0)) * bscale + bzero;
								cc += 8;
							}
						});
						break;
					}

					case -32:
					{
						Parallel.For(range[2], range[3] + 1, opts, j =>
						{
							int cc = (j * naxis0 + range[0]) * 4;
							byte[] flt = new byte[4];
							for (int i = range[0]; i <= range[1]; i++)
							{
								flt[3] = arr[cc];
								flt[2] = arr[cc + 1];
								flt[1] = arr[cc + 2];
								flt[0] = arr[cc + 3];
								dimage[i - range[0], j - range[2]] = ((double)BitConverter.ToSingle(flt, 0)) * bscale + bzero;
								cc += 4;
							}
						});
						break;
					}

					case -64:
					{
						Parallel.For(range[2], range[3] + 1, opts, j =>
						{
							int cc = (j * naxis0 + range[0]) * 8;
							byte[] dbl = new byte[8];
							for (int i = range[0]; i <= range[1]; i++)
							{
								dbl[7] = arr[cc];
								dbl[6] = arr[cc + 1];
								dbl[5] = arr[cc + 2];
								dbl[4] = arr[cc + 3];
								dbl[3] = arr[cc + 4];
								dbl[2] = arr[cc + 5];
								dbl[1] = arr[cc + 6];
								dbl[0] = arr[cc + 7];
								dimage[i - range[0], j - range[2]] = BitConverter.ToDouble(dbl, 0) * bscale + bzero;
								cc += 8;
							}
						});
						break;
					}
				}

				if (returnOptions == ImageDataUnitFormatting.Default || returnOptions == ImageDataUnitFormatting.VectorAsVerticalTable || returnOptions == ImageDataUnitFormatting.VectorAsHorizontalTable)
					return dimage;
				else if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0)
					return dimage;
				else if (returnOptions == ImageDataUnitFormatting.ArrayAsRangeRank)
				{
					double[] dvector = new double[(range[1] - range[0] + 1) * (range[3] - range[2] + 1)];
					int cc = 0;
					for (int i = 0; i < dimage.GetLength(0); i++)
						for (int j = 0; j < dimage.GetLength(1); j++)
							dvector[cc++] = dimage[i, j];

					naxisn = new int[1] { dvector.Length };
					return dvector;
				}
			}

			if (naxisn.Length == 3)//then a data cube
			{
				double[,,] dcube = new double[range[1] - range[0] + 1, range[3] - range[2] + 1, range[5] - range[4] + 1];
				int naxis0 = naxisn[0], naxis1 = naxisn[1];

				switch (bitpix)
				{
					case 8:
					{
						Parallel.For(range[4], range[5] + 1, opts, k =>
						{
							int cc = k * naxis0 * naxis1 + range[2] * naxis1 + range[0];

							for (int j = range[2]; j <= range[3]; j++)
								for (int i = range[0]; i <= range[1]; i++)
									dcube[i - range[0], j - range[2], k - range[4]] = ((double)arr[cc + i]) * bscale + bzero;
						});
						break;
					}

					case 16:
					{
						Parallel.For(range[4], range[5] + 1, opts, k =>
						{
							short val;
							int cc = (k * naxis0 * naxis1 + range[2] * naxis1 + range[0]) * 2;

							for (int j = range[2]; j <= range[3]; j++)
								for (int i = range[0]; i <= range[1]; i++)
								{
									val = (short)((arr[cc] << 8) | arr[cc + 1]);
									dcube[i - range[0], j - range[2], k - range[4]] = ((double)val) * bscale + bzero;
									cc += 2;
								}
						});
						break;
					}

					case 32:
					{
						Parallel.For(range[4], range[5] + 1, opts, k =>
						{
							int val;
							int cc = (k * naxis0 * naxis1 + range[2] * naxis1 + range[0]) * 4;

							for (int j = range[2]; j <= range[3]; j++)
								for (int i = range[0]; i <= range[1]; i++)
								{
									val = (arr[cc] << 24) | (arr[cc + 1] << 16) | (arr[cc + 2] << 8) | arr[cc + 3];
									dcube[i - range[0], j - range[2], k - range[4]] = ((double)val) * bscale + bzero;
									cc += 4;
								}
						});
						break;
					}

					case 64:
					{
						Parallel.For(range[4], range[5] + 1, opts, k =>
						{
							int cc = (k * naxis0 * naxis1 + range[2] * naxis1 + range[0]) * 8;
							byte[] dbl = new byte[8];

							for (int j = range[2]; j <= range[3]; j++)
								for (int i = range[0]; i <= range[1]; i++)
								{
									dbl[7] = arr[cc];
									dbl[6] = arr[cc + 1];
									dbl[5] = arr[cc + 2];
									dbl[4] = arr[cc + 3];
									dbl[3] = arr[cc + 4];
									dbl[2] = arr[cc + 5];
									dbl[1] = arr[cc + 6];
									dbl[0] = arr[cc + 7];
									dcube[i - range[0], j - range[2], k - range[4]] = ((double)BitConverter.ToInt64(dbl, 0)) * bscale + bzero;
									cc += 8;
								}
						});
						break;
					}

					case -32:
					{
						Parallel.For(range[4], range[5] + 1, opts, k =>
						{
							int cc = (k * naxis0 * naxis1 + range[2] * naxis1 + range[0]) * 4;
							byte[] flt = new byte[4];

							for (int j = range[2]; j <= range[3]; j++)
								for (int i = range[0]; i <= range[1]; i++)
								{
									flt[3] = arr[cc];
									flt[2] = arr[cc + 1];
									flt[1] = arr[cc + 2];
									flt[0] = arr[cc + 3];
									dcube[i - range[0], j - range[2], k - range[4]] = ((double)BitConverter.ToSingle(flt, 0)) * bscale + bzero;
									cc += 4;
								}
						});
						break;
					}

					case -64:
					{
						Parallel.For(range[4], range[5] + 1, opts, k =>
						{
							int cc = (k * naxis0 * naxis1 + range[2] * naxis1 + range[0]) * 8;
							byte[] dbl = new byte[8];

							for (int j = range[2]; j <= range[3]; j++)
								for (int i = range[0]; i <= range[1]; i++)
								{
									dbl[7] = arr[cc];
									dbl[6] = arr[cc + 1];
									dbl[5] = arr[cc + 2];
									dbl[4] = arr[cc + 3];
									dbl[3] = arr[cc + 4];
									dbl[2] = arr[cc + 5];
									dbl[1] = arr[cc + 6];
									dbl[0] = arr[cc + 7];
									dcube[i - range[0], j - range[2], k - range[4]] = ((double)BitConverter.ToDouble(dbl, 0)) * bscale + bzero;
									cc += 8;
								}
						});						
						break;
					}
				}

				if (returnOptions == ImageDataUnitFormatting.Default || returnOptions == ImageDataUnitFormatting.VectorAsVerticalTable || returnOptions == ImageDataUnitFormatting.VectorAsHorizontalTable)
					return dcube;
				else if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 && (range[5] - range[4]) > 0)
					return dcube;
				else if (returnOptions == ImageDataUnitFormatting.ArrayAsRangeRank)
				{
					//check if vector
					if (((range[1] - range[0]) == 0 && (range[3] - range[2]) == 0) || ((range[1] - range[0]) == 0 && (range[5] - range[4]) == 0) || ((range[3] - range[2]) == 0 && (range[5] - range[4]) == 0))
					{
						double[] dvector = new double[(range[1] - range[0] + 1) * (range[3] - range[2] + 1) * (range[5] - range[4] + 1)];
						int cc = 0;
						for (int i = 0; i < dcube.GetLength(0); i++)
							for (int j = 0; j < dcube.GetLength(1); j++)
								for (int k = 0; k < dcube.GetLength(2); k++)
									dvector[cc++] = dcube[i, j, k];

						naxisn = new int[1] { dvector.Length };
						return dvector;
					}
					else//must be 2d if gotten here
					{
						double[,] dimage;
						if ((range[1] - range[0]) == 0)
						{
							dimage = new double[(range[3] - range[2] + 1), (range[5] - range[4] + 1)];
							for (int i = 0; i < dimage.GetLength(0); i++)
								for (int j = 0; j < dimage.GetLength(1); j++)
									dimage[i, j] = dcube[0, i, j];
						}
						else if ((range[3] - range[2]) == 0)
						{
							dimage = new double[(range[1] - range[0] + 1), (range[5] - range[4] + 1)];
							for (int i = 0; i < dimage.GetLength(0); i++)
								for (int j = 0; j < dimage.GetLength(1); j++)
									dimage[i, j] = dcube[i, 0, j];
						}
						else
						{
							dimage = new double[(range[1] - range[0] + 1), (range[3] - range[2] + 1)];
							for (int i = 0; i < dimage.GetLength(0); i++)
								for (int j = 0; j < dimage.GetLength(1); j++)
									dimage[i, j] = dcube[i, j, 0];
						}

						naxisn = new int[2] { dimage.GetLength(0), dimage.GetLength(1) };
						return dimage;
					}
				}
				else
					throw new Exception("DataUnitReturn option DataUnitReturn.VectorAsTable is not valid for a 3D cube.");
			}

			throw new Exception("Error in FITSFILEOPS.READDATAUNIT - made it to end without returning data.");
		}

		/// <summary>Returns a data unit as a byte array formatted at a specified precision.</summary>
		/// <param name="formatPrecision">The precision at which to format the byte array of the underlying double precision data unit. If double values of the data unit exceed the precision, the values are max scale.</param>
		/// <param name="doParallel">Populate the byte array with parallelism over the data unit. Can speed things up the data unit is very large.</param>
		/// <param name="doubleDataUnit">The data unit of up to rank three (data cube). Higher dimensional data than rank = 3 not supported.</param>
		public static byte[] GETBYTEFORMATTEDDATAUNIT(TypeCode formatPrecision, bool doParallel, Array doubleDataUnit)
		{
			if (doubleDataUnit.Rank > 3)
				throw new Exception("Error: I can only handle up to 3-dimensional data units - SORRY!");

			int bitpix;
			int[] naxisn;
			double bzero, bscale;
			GETBITPIXNAXISnBSCALEBZERO(formatPrecision, doubleDataUnit, out bitpix, out naxisn, out bscale, out bzero);

			long Ndatabytes = doubleDataUnit.Length * Math.Abs(bitpix) / 8;
			int NBlocks = (int)(Math.Ceiling((double)(Ndatabytes) / 2880.0));
			int NBytesTot = NBlocks * 2880;
			byte[] data = new byte[NBytesTot];

			ParallelOptions opts = new ParallelOptions();
			if (doParallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			switch (formatPrecision)
			{
				case TypeCode.Byte:
				case TypeCode.SByte:
				{
					if (doubleDataUnit.Rank == 1)
					{
						Parallel.For(0, naxisn[0], opts, i =>
						{
							byte val = (byte)((((double[])doubleDataUnit)[i] - bzero) / bscale);
							data[i] = val;
						});
					}
					else if (doubleDataUnit.Rank == 2)
					{
						Parallel.For(0, naxisn[1], opts, j =>
						{
							int cc = j * naxisn[0];
							byte val;
							for (int i = 0; i < naxisn[0]; i++)
							{
								val = (byte)((((double[,])doubleDataUnit)[i, j] - bzero) / bscale);
								data[cc] = val;
								cc += 1;
							}
						});
					}
					else if (doubleDataUnit.Rank == 3)
					{
						Parallel.For(0, naxisn[2], opts, k =>
						{
							int cc = k * naxisn[0] * naxisn[1];
							byte val;
							for (int j = 0; j < naxisn[1]; j++)
							{
								cc += j * naxisn[0];
								for (int i = 0; i < naxisn[0]; i++)
								{
									val = (byte)((((double[,])doubleDataUnit)[i, j] - bzero) / bscale);
									data[cc] = val;
									cc += 1;
								}
							}
						});
					}
					break;
				}

				case TypeCode.UInt16:
				case TypeCode.Int16:
				{
					if (doubleDataUnit.Rank == 1)
					{
						Parallel.For(0, naxisn[0], opts, i =>
						{
							int cc = i * 2;
							short val = (short)((((double[])doubleDataUnit)[i] - bzero) / bscale);
							data[cc] = (byte)((val >> 8) & 0xff);
							data[cc + 1] = (byte)(val & 0xff);
						});
					}
					else if (doubleDataUnit.Rank == 2)
					{
						Parallel.For(0, naxisn[1], opts, j =>
						{
							int cc = j * naxisn[0] * 2;
							short val;
							for (int i = 0; i < naxisn[0]; i++)
							{
								val = (short)((((double[,])doubleDataUnit)[i, j] - bzero) / bscale);
								data[cc] = (byte)((val >> 8) & 0xff);
								data[cc + 1] = (byte)(val & 0xff);
								cc += 2;
							}
						});
					}
					else if (doubleDataUnit.Rank == 3)
					{
						Parallel.For(0, naxisn[2], opts, k =>
						{
							int cc = (k * naxisn[0] * naxisn[1]) * 2;
							short val;
							for (int j = 0; j < naxisn[1]; j++)
							{
								cc += (j * naxisn[0]) * 2;
								for (int i = 0; i < naxisn[0]; i++)
								{
									val = (short)((((double[,])doubleDataUnit)[i, j] - bzero) / bscale);
									data[cc] = (byte)((val >> 8) & 0xff);
									data[cc + 1] = (byte)(val & 0xff);
									cc += 2;
								}
							}
						});
					}
					break;
				}

				case TypeCode.UInt32:
				case TypeCode.Int32:
				{
					if (doubleDataUnit.Rank == 1)
					{
						Parallel.For(0, naxisn[0], opts, i =>
						{
							int cc = i * 4;
							int val = (int)((((double[])doubleDataUnit)[i] - bzero) / bscale);
							data[cc] = (byte)((val >> 24) & 0xff);
							data[cc + 1] = (byte)((val >> 16) & 0xff);
							data[cc + 2] = (byte)((val >> 8) & 0xff);
							data[cc + 3] = (byte)(val & 0xff);
						});
					}
					else if (doubleDataUnit.Rank == 2)
					{
						Parallel.For(0, naxisn[1], opts, j =>
						{
							int cc = j * naxisn[0] * 4;
							int val;
							for (int i = 0; i < naxisn[0]; i++)
							{
								val = (int)((((double[,])doubleDataUnit)[i, j] - bzero) / bscale);
								data[cc] = (byte)((val >> 24) & 0xff);
								data[cc + 1] = (byte)((val >> 16) & 0xff);
								data[cc + 2] = (byte)((val >> 8) & 0xff);
								data[cc + 3] = (byte)(val & 0xff);
								cc += 4;
							}
						});
					}
					else if (doubleDataUnit.Rank == 3)
					{
						Parallel.For(0, naxisn[2], opts, k =>
						{
							int cc = (k * naxisn[0] * naxisn[1]) * 4;
							int val;
							for (int j = 0; j < naxisn[1]; j++)
							{
								cc += (j * naxisn[0]) * 4;
								for (int i = 0; i < naxisn[0]; i++)
								{
									val = (int)((((double[,])doubleDataUnit)[i, j] - bzero) / bscale);
									data[cc] = (byte)((val >> 24) & 0xff);
									data[cc + 1] = (byte)((val >> 16) & 0xff);
									data[cc + 2] = (byte)((val >> 8) & 0xff);
									data[cc + 3] = (byte)(val & 0xff);
									cc += 4;
								}
							}
						});
					}
					break;
				}

				case TypeCode.UInt64:
				case TypeCode.Int64:
				{
					if (doubleDataUnit.Rank == 1)
					{
						Parallel.For(0, naxisn[0], opts, i =>
						{
								int cc = i * 8;
								long val = (long)((((double[])doubleDataUnit)[i] - bzero) / bscale);
								data[cc] = (byte)((val >> 56) & 0xff);
								data[cc + 1] = (byte)((val >> 48) & 0xff);
								data[cc + 2] = (byte)((val >> 40) & 0xff);
								data[cc + 3] = (byte)((val >> 32) & 0xff);
								data[cc + 4] = (byte)((val >> 24) & 0xff);
								data[cc + 5] = (byte)((val >> 16) & 0xff);
								data[cc + 6] = (byte)((val >> 8) & 0xff);
								data[cc + 7] = (byte)(val & 0xff);
						});
					}
					else if (doubleDataUnit.Rank == 2)
					{
						Parallel.For(0, naxisn[1], opts, j =>
						{
							int cc = j * naxisn[0] * 8;
							long val;
							for (int i = 0; i < naxisn[0]; i++)
							{
								val = (long)((((double[,])doubleDataUnit)[i, j] - bzero) / bscale);
								data[cc] = (byte)((val >> 56) & 0xff);
								data[cc + 1] = (byte)((val >> 48) & 0xff);
								data[cc + 2] = (byte)((val >> 40) & 0xff);
								data[cc + 3] = (byte)((val >> 32) & 0xff);
								data[cc + 4] = (byte)((val >> 24) & 0xff);
								data[cc + 5] = (byte)((val >> 16) & 0xff);
								data[cc + 6] = (byte)((val >> 8) & 0xff);
								data[cc + 7] = (byte)(val & 0xff);
								cc += 8;
							}
						});
					}
					else if (doubleDataUnit.Rank == 3)
					{
						Parallel.For(0, naxisn[2], opts, k =>
						{
							int cc = (k * naxisn[0] * naxisn[1]) * 8;
							long val;
							for (int j = 0; j < naxisn[1]; j++)
							{
								cc += (j * naxisn[0]) * 8;
								for (int i = 0; i < naxisn[0]; i++)
								{
									val = (long)((((double[,])doubleDataUnit)[i, j] - bzero) / bscale);
									data[cc] = (byte)((val >> 56) & 0xff);
									data[cc + 1] = (byte)((val >> 48) & 0xff);
									data[cc + 2] = (byte)((val >> 40) & 0xff);
									data[cc + 3] = (byte)((val >> 32) & 0xff);
									data[cc + 4] = (byte)((val >> 24) & 0xff);
									data[cc + 5] = (byte)((val >> 16) & 0xff);
									data[cc + 6] = (byte)((val >> 8) & 0xff);
									data[cc + 7] = (byte)(val & 0xff);
									cc += 8;
								}
							}
						});
					}
					break;
				}

				case TypeCode.Single:
				{
					if (doubleDataUnit.Rank == 1)
					{
						Parallel.For(0, naxisn[0], opts, i =>
						{
							int cc = i * 4;
							byte[] sng = BitConverter.GetBytes((float)((((double[])doubleDataUnit)[i] - bzero) / bscale));
							data[cc] = sng[3];
							data[cc + 1] = sng[2];
							data[cc + 2] = sng[1];
							data[cc + 3] = sng[0];
						});
					}
					else if (doubleDataUnit.Rank == 2)
					{
						Parallel.For(0, naxisn[1], opts, j =>
						{
							int cc = j * naxisn[0] * 4;
							byte[] sng = new byte[4];
							for (int i = 0; i < naxisn[0]; i++)
							{
								sng = BitConverter.GetBytes((float)((((double[,])doubleDataUnit)[i, j] - bzero) / bscale));
								data[cc] = sng[3];
								data[cc + 1] = sng[2];
								data[cc + 2] = sng[1];
								data[cc + 3] = sng[0];
								cc += 4;
							}
						});
					}
					else if (doubleDataUnit.Rank == 3)
					{
						Parallel.For(0, naxisn[2], opts, k =>
						{
							int cc = (k * naxisn[0] * naxisn[1]) * 4;
							byte[] sng = new byte[4];
							for (int j = 0; j < naxisn[1]; j++)
							{
								cc += (j * naxisn[0]) * 4;
								for (int i = 0; i < naxisn[0]; i++)
								{
									sng = BitConverter.GetBytes((float)((((double[,])doubleDataUnit)[i, j] - bzero) / bscale));
									data[cc] = sng[3];
									data[cc + 1] = sng[2];
									data[cc + 2] = sng[1];
									data[cc + 3] = sng[0];
									cc += 4;
								}
							}
						});
					}
					break;
				}

				case TypeCode.Double:
				{
					if (doubleDataUnit.Rank == 1)
					{
						Parallel.For(0, naxisn[0], opts, i =>
						{
							int cc = i * 8;
							byte[] dbl = BitConverter.GetBytes((((double[])doubleDataUnit)[i] - bzero) / bscale);
							data[cc] = dbl[7];
							data[cc + 1] = dbl[6];
							data[cc + 2] = dbl[5];
							data[cc + 3] = dbl[4];
							data[cc + 4] = dbl[3];
							data[cc + 5] = dbl[2];
							data[cc + 6] = dbl[1];
							data[cc + 7] = dbl[0];
						});
					}
					else if (doubleDataUnit.Rank == 2)
					{
						Parallel.For(0, naxisn[1], opts, j =>
						{
							int cc = j * naxisn[0] * 8;
							byte[] dbl = new byte[8];
							for (int i = 0; i < naxisn[0]; i++)
							{
								dbl = BitConverter.GetBytes((((double[,])doubleDataUnit)[i, j] - bzero) / bscale);
								data[cc] = dbl[7];
								data[cc + 1] = dbl[6];
								data[cc + 2] = dbl[5];
								data[cc + 3] = dbl[4];
								data[cc + 4] = dbl[3];
								data[cc + 5] = dbl[2];
								data[cc + 6] = dbl[1];
								data[cc + 7] = dbl[0];
								cc += 8;
							}
						});
					}
					else if (doubleDataUnit.Rank == 3)
					{
						Parallel.For(0, naxisn[2], opts, k =>
						{
							int cc = (k * naxisn[0] * naxisn[1]) * 8;
							byte[] dbl = new byte[8];
							for (int j = 0; j < naxisn[1]; j++)
							{
								cc += (j * naxisn[0]) * 8;
								for (int i = 0; i < naxisn[0]; i++)
								{
									dbl = BitConverter.GetBytes((((double[,])doubleDataUnit)[i, j] - bzero) / bscale);
									data[cc] = dbl[7];
									data[cc + 1] = dbl[6];
									data[cc + 2] = dbl[5];
									data[cc + 3] = dbl[4];
									data[cc + 4] = dbl[3];
									data[cc + 5] = dbl[2];
									data[cc + 6] = dbl[1];
									data[cc + 7] = dbl[0];
									cc += 8;
								}
							}
						});
					}
					break;
				}
			}

			return data;
		}

		/// <summary>Outputs essential header keywords for a given precision and data unit.</summary>
		/// <param name="precision">The intended precision of the data unit.</param>
		/// <param name="doubleDataUnit">The data unit.</param>
		/// <param name="bitpix">BITPIX keyword value.</param>
		/// <param name="naxisn">The length of this array provides the NAXIS keyword value. The elements of this array provide the NAXISn keyword values.</param>
		/// <param name="bscale">BSCALE keyword value.</param>
		/// <param name="bzero">BZERO keyword value.</param>
		public static void GETBITPIXNAXISnBSCALEBZERO(TypeCode precision, Array? doubleDataUnit, out int bitpix, out int[] naxisn, out double bscale, out double bzero)
		{
			if (doubleDataUnit == null || doubleDataUnit.Length == 0)
			{
				bitpix = 8;
				naxisn = new int[0];
			}
			else
			{
				naxisn = new int[doubleDataUnit.Rank];
				for (int i = 0; i < naxisn.Length; i++)
					naxisn[i] = doubleDataUnit.GetLength(i);
			}

			switch (precision)
			{
				case TypeCode.SByte:
				{
					bitpix = 8;
					bzero = 0;
					bscale = 1;
					break;
				}

				case TypeCode.Byte:
				{
					bitpix = 8;
					bzero = 128;
					bscale = 1;
					break;
				}

				case TypeCode.Int16:
				{
					bitpix = 16;
					bzero = 0;
					bscale = 1;
					break;
				}

				case TypeCode.UInt16:
				{
					bitpix = 16;
					bzero = 32768;
					bscale = 1;
					break;
				}

				case TypeCode.Int32:
				{
					bitpix = 32;
					bzero = 0;
					bscale = 1;
					break;
				}

				case TypeCode.UInt32:
				{
					bitpix = 32;
					bzero = 2147483648;
					bscale = 1;
					break;
				}

				case TypeCode.Int64:
				{
					bitpix = 64;
					bzero = 0;
					bscale = 1;
					break;
				}

				case TypeCode.UInt64:
				{
					bitpix = 64;
					bzero = 9223372036854775808;
					bscale = 1;
					break;
				}

				case TypeCode.Single:
				{
					bitpix = -32;
					bzero = 0;
					bscale = 1;
					break;
				}

				case TypeCode.Double:
				{
					bitpix = -64;
					bzero = 0;
					bscale = 1;
					break;
				}

				default:
				{
					throw new Exception("TypeCode '" + precision.ToString() + "' not recognized at GetBITPIXNAXISnBSCALEBZERO.");
				}
			}
		}

		/// <summary>Find the FITS extension table of the given type and name. Returns false if the XTENSION type of the specified EXTNAME is not found. If extension_name is found the FileStream fs will be placed at the beginning of the extension's main data table block.</summary>
		/// <param name="fs">The FileStream of the FITS file, positioned at the start of the stream.
		/// <br />If EXTNAME is found the FileStream fs will be placed at the beginning of the extension's main data table block.
		/// <br />If EXTNAME is NOT found it will be at the end of the file.</param>
		/// <param name="extension_type">The XTENSION extension type, either: &quot;BINTABLE&quot;, &quot;TABLE&quot;, or &quot;IMAGE&quot;.</param>
		/// <param name="extension_name">The EXTNAME extension name. If the extension is known to have no EXTNAME keyword and name, then pass an empty String and the first nameless extension of the specified type will be seeked.</param>
		/// <param name="header_return">Returns the header of the extension as an ArrayList with each 80-character header line being a String member of this list. Pass nullptr if not required.</param>
		/// <param name="extensionStartPosition">Returns the start position within the FileStream of the extension...i.e. at the block boundary at the start of its header.</param>
		/// <param name="extensionEndPosition">Returns the end position within the FileStream of the extension, including after any heap, rounded up to a multiple of 2880 bytes at the last block boundary.</param>
		/// <param name="tableEndPosition">Returns the end position within the FileStream of the main data table, NOT rounded to a data block boundary.</param>
		/// <param name="pcount">Returns the number of bytes of any remaining fill plus supplemental heap data area after the main table endposition, IF any heap data exists. Does not represent fill bytes after the main table if no heap exists. Does not include fill bytes after the heap.</param>
		/// <param name="theap">Returns the position within the filestream of the beginning of the heap relative to the beginning of the main table. Nominally equal to NAXIS1 * NAXIS2 unless THEAP keyword specifies a larger value.</param>
		public static bool SEEKEXTENSION(FileStream fs, string extension_type, string extension_name, ref ArrayList? header_return, out long extensionStartPosition, out long extensionEndPosition, out long tableEndPosition, out long pcount, out long theap)
		{
			if (fs.Position == 0)
			{
				throw new Exception("SEEKEXTENSION only after SCANPRIMARYUNIT with scanpastprimarydata");
			}
			
			extensionStartPosition = -1;
			extensionEndPosition = -1;
			tableEndPosition = -1;
			pcount = -1;
			theap = -1;

			byte[] charheaderblock = new byte[2880];
			int naxis = 0, naxis1 = 0, naxis2 = 0, bitpix = 0, extnum = -1, seekedextensionnum = 0;
			bool endheader = false, extensionnamefound = false, extensiontypefound = false, endfile = false, extnamekeyexists = false, extensionfound = false;
			string strheaderline;

			bool findextnum = extension_name.Contains("_FINDEXTNUM_");
			if (findextnum)
				extnum = Convert.ToInt32(extension_name.Substring(extension_name.LastIndexOf("_") + 1));

			if (fs.Position >= fs.Length)
				endfile = true;

			while (!extensionfound && !endfile)
			{
				//reset
				extensionStartPosition = fs.Position;
				endheader = false;
				extnamekeyexists = false;
				extensiontypefound = false;
				naxis = -1;
				naxis1 = 0;
				naxis2 = 0;
				bitpix = 0;
				pcount = -1;
				theap = -1;

				if (header_return != null)
					header_return.Clear();
				while (!endheader)
				{
					fs.Read(charheaderblock, 0, 2880);

					for (int i = 0; i < 36; i++)
					{
						strheaderline = System.Text.Encoding.ASCII.GetString(charheaderblock, i * 80, 80);

						if (header_return != null)
							header_return.Add(strheaderline);

						if (!extensiontypefound)
							if (strheaderline.Substring(0, 8) == "XTENSION")
							{
								int f = strheaderline.IndexOf("'");
								int l = strheaderline.LastIndexOf("'");
								if (strheaderline.Substring(f + 1, l - f - 1).Trim() == extension_type)
								{
									extensiontypefound = true;
									seekedextensionnum++;
								}
								continue;
							}

						if (!extnamekeyexists)
							if (strheaderline.Substring(0, 8) == "EXTNAME ")
							{
								extnamekeyexists = true;
								int f = strheaderline.IndexOf("'");
								int l = strheaderline.LastIndexOf("'");
								if (extension_name == strheaderline.Substring(f + 1, l - f - 1).Trim())
									extensionnamefound = true;
								continue;
							}

						if (bitpix == 0)
							if (strheaderline.Substring(0, 8).Trim().Equals("BITPIX"))
							{
								bitpix = Convert.ToInt32(strheaderline.Substring(10, 20));
								continue;
							}

						if (naxis == -1)
							if (strheaderline.Substring(0, 8).Trim().Equals("NAXIS"))
							{
								naxis = Convert.ToInt32(strheaderline.Substring(10, 20));
								continue;
							}

						if (naxis1 == 0)
							if (strheaderline.Substring(0, 8).Trim().Equals("NAXIS1"))
							{
								naxis1 = Convert.ToInt32(strheaderline.Substring(10, 20));
								continue;
							}

						if (naxis2 == 0)
							if (strheaderline.Substring(0, 8).Trim().Equals("NAXIS2"))
							{
								naxis2 = Convert.ToInt32(strheaderline.Substring(10, 20));
								theap = naxis1 * naxis2;
								continue;
							}

						if (pcount == -1)
							if (strheaderline.Substring(0, 8).Trim().Equals("PCOUNT"))
							{
								pcount = Convert.ToInt64(strheaderline.Substring(10, 20));
								continue;
							}

						if (theap == naxis1 * naxis2)
							if (strheaderline.Substring(0, 8).Trim().Equals("THEAP"))
							{
								theap = Convert.ToInt64(strheaderline.Substring(10, 20));
								continue;
							}

						if (strheaderline.Substring(0, 8).Trim().Equals("END"))
						{
							endheader = true;
							break;
						}
					}
				}

				if (extensiontypefound)
					if ((extnamekeyexists && extensionnamefound && !findextnum) || (!extnamekeyexists && extension_name == "" && !findextnum) || (findextnum && extnum == seekedextensionnum))
						extensionfound = true;

				long TableBytes = (long)(naxis1) * (long)(naxis2) * (long)(Math.Abs(bitpix)) / 8;
				if (!extensionfound)
				{
					fs.Position += (long)(Math.Ceiling((double)(TableBytes + pcount) / 2880) * 2880);
					if (fs.Position >= fs.Length)
						endfile = true;
				}
				else
				{
					extensionEndPosition = fs.Position + (long)(Math.Ceiling((double)(TableBytes + pcount) / 2880) * 2880);
					tableEndPosition = fs.Position + TableBytes;
				}
			}

			return extensionfound;
		}

		/// <summary>Find the FITS extension table of the given type and position index number. Returns false if the XTENSION type of the specified number is not found. If the index number is found the FileStream fs will be placed at the beginning of the extension's main data table block.</summary>
		/// <param name="fs">The FileStream of the FITS file, positioned at the start of the stream.
		/// <br />If the extension number is found the FileStream fs will be placed at the beginning of the extension's main data table block.
		/// <br />If the extension number is NOT found it will be at the end of the file.</param>
		/// <param name="extension_type">The XTENSION extension type, either: &quot;BINTABLE&quot;, &quot;TABLE&quot;, or &quot;IMAGE&quot;.</param>
		/// <param name="extension_number">The ONE-BASED extension number. This can be used when extensions aren't named with the EXTNAME keyword; alternatively if they are named this still returns the XTENSION extension type of the specified number.</param>
		/// <param name="header_return">Returns the header of the extension as an ArrayList with each 80-character header line being a String member of this list. Pass nullptr if not required.</param>
		/// <param name="extensionStartPosition">Returns the start position within the FileStream of the extension...i.e. at the block boundary at the start of its header.</param>
		/// <param name="extensionEndPosition">Returns the end position within the FileStream of the extension, including after any heap, rounded up to a multiple of 2880 bytes at the last block boundary.</param>
		/// <param name="tableEndPosition">Returns the end position within the FileStream of the main data table, NOT rounded to a data block boundary.</param>
		/// <param name="pcount">Returns the number of bytes of any remaining fill plus supplemental heap data area after the main table endposition, IF any heap data exists. Does not represent fill bytes after the main table if no heap exists. Does not include fill bytes after the heap.</param>
		/// <param name="theap">Returns the position within the filestream of the beginning of the heap relative to the beginning of the main table. Nominally equal to NAXIS1 * NAXIS2 unless THEAP keyword specifies a larger value.</param>
		public static bool SEEKEXTENSION(FileStream fs, string extension_type, int extension_number, ref ArrayList? header_return, out long extensionStartPosition, out long extensionEndPosition, out long tableEndPosition, out long pcount, out long theap)
		{
			return SEEKEXTENSION(fs, extension_type, "_FINDEXTNUM_" + extension_number.ToString(), ref header_return, out extensionStartPosition, out extensionEndPosition, out tableEndPosition, out pcount, out theap);
		}

		/// <summary>Gets all extension names of a specified extension type in the FITS file. If no extensions of the type exist, returns and empty array.</summary>
		/// <param name="FileName">The full file name to read from disk.</param>
		/// <param name="extension_type">The XTENSION extension type, either: &quot;BINTABLE&quot;, &quot;TABLE&quot;, or &quot;IMAGE&quot;.</param>
		public static string[] GETALLEXTENSIONNAMES(string FileName, string extension_type)
		{
			FileStream fs = new FileStream(FileName, FileMode.Open);
			ArrayList header_return = null;
			bool hasext;
			if (!FITSFILEOPS.SCANPRIMARYUNIT(fs, true, ref header_return, out hasext) || !hasext)
			{
				fs.Close();
				if (!hasext)
					return new string[0];
				else
					throw new Exception("File '" + FileName + "'  not formatted as FITS file.");
			}

			byte[] charheaderblock = new byte[2880];
			int naxis = 0, naxis1 = 0, naxis2 = 0, bitpix = 0;
			long pcount = -1;
			bool endheader = false, extensiontypefound = false, endfile = false, extnamekeyexists = false;
			string strheaderline;
			ArrayList namelist = new ArrayList();
			string extname = "";

			if (fs.Position >= fs.Length)
				endfile = true;

			while (!endfile)
			{
				//reset
				extname = "";
				endheader = false;
				extnamekeyexists = false;
				extensiontypefound = false;
				naxis = 0;
				naxis1 = 0;
				naxis2 = 0;
				pcount = -1;
				bitpix = 0;

				while (!endheader)
				{
					fs.Read(charheaderblock, 0, 2880);

					for (int i = 0; i < 36; i++)
					{
						strheaderline = System.Text.Encoding.ASCII.GetString(charheaderblock, i * 80, 80);

						if (!extensiontypefound)
							if (strheaderline.Substring(0, 8) == "XTENSION")
							{
								int f = strheaderline.IndexOf("'");
								int l = strheaderline.LastIndexOf("'");
								if (strheaderline.Substring(f + 1, l - f - 1).Trim() == extension_type)
									extensiontypefound = true;
								continue;
							}
						if (!extnamekeyexists)
							if (strheaderline.Substring(0, 8) == "EXTNAME ")
							{
								extnamekeyexists = true;
								int f = strheaderline.IndexOf("'");
								int l = strheaderline.LastIndexOf("'");
								extname = strheaderline.Substring(f + 1, l - f - 1).Trim();
								continue;
							}
						if (naxis == 0)
							if (strheaderline.Substring(0, 8).Trim().Equals("NAXIS"))
							{
								naxis = Convert.ToInt32(strheaderline.Substring(10, 20));
								continue;
							}
						if (naxis1 == 0)
							if (strheaderline.Substring(0, 8).Trim().Equals("NAXIS1"))
							{
								naxis1 = Convert.ToInt32(strheaderline.Substring(10, 20));
								continue;
							}
						if (naxis2 == 0)
							if (strheaderline.Substring(0, 8).Trim().Equals("NAXIS2"))
							{
								naxis2 = Convert.ToInt32(strheaderline.Substring(10, 20));
								continue;
							}
						if (bitpix == 0)
							if (strheaderline.Substring(0, 8).Trim().Equals("BITPIX"))
							{
								bitpix = Convert.ToInt32(strheaderline.Substring(10, 20));
								continue;
							}
						if (pcount == -1)
							if (strheaderline.Substring(0, 8).Trim().Equals("PCOUNT"))
							{
								pcount = Convert.ToInt64(strheaderline.Substring(10, 20));
								continue;
							}
						if (strheaderline.Substring(0, 8) == "END     ")//check if we're at the end of the header keys
						{
							if (extensiontypefound)
								namelist.Add(extname);
							endheader = true;
							break;
						}
					}
				}

				long TableBytes = (long)(naxis1) * (long)(naxis2) * (long)(Math.Abs(bitpix)) / 8;
				fs.Seek((long)(Math.Ceiling((double)(TableBytes + pcount) / 2880) * 2880), SeekOrigin.Current);

				if (fs.Position >= fs.Length)
					endfile = true;
			}

			fs.Close();

			string[] list = new string[namelist.Count];
			for (int i = 0; i < namelist.Count; i++)
				list[i] = (string)namelist[i];

			return list;
		}
	}
}

