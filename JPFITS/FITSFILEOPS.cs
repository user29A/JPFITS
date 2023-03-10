using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
#nullable enable

namespace JPFITS
{
	public enum DiskPrecision
	{
		Boolean = TypeCode.Boolean,

		Byte = TypeCode.Byte,

		SByte = TypeCode.SByte,

		UInt16 = TypeCode.UInt16,

		Int16 = TypeCode.Int16,

		UInt32 = TypeCode.UInt32,

		Int32 = TypeCode.Int32,

		UInt64 = TypeCode.UInt64,

		Int64 = TypeCode.Int64,

		Single = TypeCode.Single,

		Double = TypeCode.Double
	}

	/// <summary>Array formatting options for the data unit returned by the ReadImageDataUnit method.</summary>
	public enum RankFormat
	{
		/// <summary>
		/// The Array is returned as the rank indicated by the NAXIS keyword, up to rank = 3 (data cube).
		/// </summary>
		NAXIS,

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
		ArrayAsRangeRank,

		/// <summary>
		/// Will return the image data as a vector. Useful for retreiving data of rank greater than 3. naxisn will contain the header NAXISn values. The range parameter argument does not apply with this option.
		/// </summary>
		Vector
	}

	/// <summary>Array precision options for the data unit returned by the ReadImageDataUnit method.</summary>
	public enum ReadReturnPrecision
	{
		/// <summary>
		/// Return the array at its native on-disk precision
		/// </summary>
		Native,

		/// <summary>
		/// Return the array at double-precision regardless of its precision on-disk.
		/// </summary>
		Double,

		/// <summary>
		/// Return the array as a Boolean. The on-disk data 1's are true, all else are false. The on-disk data must be unsigned bytes.
		/// </summary>
		Boolean
	}

	///<summary>FITSFILEOPS static class for facilitating interaction with FITS data on disk.</summary>
	public class FITSFILEOPS
	{
		#region FITS unsigned integer mapping

		[MethodImpl(256)]/*256 = agressive inlining*/
		public static long MapUlongToLong(ulong ulongValue)
		{
			return unchecked((long)ulongValue + long.MinValue);
		}

		[MethodImpl(256)]/*256 = agressive inlining*/
		public static int MapUintToInt(uint uintValue)
		{
			return unchecked((int)uintValue + int.MinValue);
		}

		[MethodImpl(256)]/*256 = agressive inlining*/
		public static short MapUshortToShort(ushort ushortValue)
		{
			return unchecked((short)((short)ushortValue + short.MinValue));
		}

		[MethodImpl(256)]/*256 = agressive inlining*/
		public static ulong MapLongToUlong(long longValue)
		{
			return unchecked((ulong)(longValue - long.MinValue));
		}

		[MethodImpl(256)]/*256 = agressive inlining*/
		public static uint MapIntToUint(int intValue)
		{
			return unchecked((uint)(intValue - int.MinValue));
		}

		[MethodImpl(256)]/*256 = agressive inlining*/
		public static ushort MapShortToUshort(short shortValue)
		{
			return unchecked((ushort)(shortValue - short.MinValue));
		}

		#endregion
		
		/// <summary>Scans the primary unit of a FITS file. Returns false if the file is not a FITS file.</summary>
		/// <param name="fs">The FileStream of the FITS file, positioned at the start of the stream.</param>
		/// <param name="scanpastprimarydata">True to set the FileStream fs position to the end of the data block, otherwise the fs position will be at the end of the primary header block, i.e. at the beginning of the primary data.</param>
		/// <param name="header_return">Returns the header of the unit as an ArrayList with each 80-character header line being a String member of this list. Pass null if not required.</param>
		/// <param name="has_extensions">Returns whether or not the FITS file may contain extensions.</param>
		public static bool ScanPrimaryUnit(FileStream fs, bool scanpastprimarydata, ref ArrayList? header_return, out bool has_extensions)
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

		/// <summary>Scans an IMAGE unit of a FITS file. Returns false if the file is not a FITS file. Returns essential IMAGE keyword values. Can be used to scan the primary image unit or IMAGE extensions.</summary>
		/// <param name="fs">The FileStream of the FITS file, positioned at the start of the primary header block, or, at the start of any IMAGE extension unit.</param>
		/// <param name="scanpastdataunit">True to set the FileStream fs position to the end of the data block, otherwise the fs position will be at the end of the header block, i.e. at the beginning of the data unit.</param>
		/// <param name="header_return">Returns the header of the unit as an ArrayList with each 80-character header line being a String member of this list. Pass null if not required.</param>
		/// <param name="has_extensions">Returns whether or not the FITS file may contain extensions. Returns true if the unit being scanned is itself an extension.</param>
		/// <param name="bitpix">BITPIX keyword value. Bits per pixel keyword, following the meaning of the FITS convection.</param>
		/// <param name="naxisn">An array of length NAXIS containing the dimension (length) of each axis. If there are no axes, is an emtpty array. NAXIS1 = naxisn[0]; NAXIS2 = naxisn[1]; etc.</param>
		/// <param name="bscale">The BSCALE keyword value. Default return is one (1) if not present.</param>
		/// <param name="bzero">The BZERO keyword value. Default return is zero (0) if not present.</param>
		public static bool ScanImageHeaderUnit(FileStream fs, bool scanpastdataunit, ref ArrayList? header_return, out bool has_extensions, out int bitpix, out int[] naxisn, out double bscale, out double bzero)
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
						{
							has_extensions = true;

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

		/// <summary>Reads the image data unit from a FITS file and returns its Array at native or alternative precisions. Supports image data units with up to 3 axes. May return either a vector, table, or cube. Closes the file stream.</summary>
		/// <param name="fs">The FileStream of the FITS file, positioned at the start of the primary or image extension data unit.</param>
		/// <param name="range">Pass null or range[0] = -1 to default to full data unit size. Otherwise range is ZERO BASED 1-D int array [xmin xmax] or [xmin xmax ymin ymax]  or [xmin xmax ymin ymax zmin zmax] to return a sub-array.</param>
		/// <param name="doParallel">Populate the Array object with parallelization after serial disk read.</param>
		/// <param name="bitpix">The BITPIX keyword value of the data unit header.</param>
		/// <param name="naxisn">An array containing the values of the NAXISn keywords from the data unit header. Specifies the rank of the return Array, i.e., if naxisn.Length == 1, then it is a vector, if 2 then a table, if 3 then a cube. The value may change from the input given the RankFormat options.</param>
		/// <param name="bscale">The BSCALE keyword value of the data unit header.</param>
		/// <param name="bzero">The BZERO keyword value of the data unit header.</param>
		/// <param name="returnRankFormat">Options for formatting the return Array rank and dimensions.</param>
		/// <param name="returnPrecision">Options for the precision type of the return Array.</param>
		public static Array ReadImageDataUnit(FileStream fs, int[]? range, bool doParallel, int bitpix, ref int[] naxisn, double bscale, double bzero, RankFormat returnRankFormat = RankFormat.NAXIS, ReadReturnPrecision returnPrecision = ReadReturnPrecision.Double)
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
			fs.Read(arr, 0, NBytes);//fastest to just read the entire data even if only subimage will be used - though this may needs to be checked with new m2 faster drives!?

			ParallelOptions opts = new ParallelOptions();
			if (doParallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			if (returnRankFormat == RankFormat.Vector)
			{
				int nelements = (int)JPMath.Product(naxisn);

				if (returnPrecision == ReadReturnPrecision.Double)
				{
					double[] vector = new double[nelements];

					switch (bitpix)
					{
						case 8:
						{
							Parallel.For(0, nelements, opts, i =>
							{
								vector[i] = arr[i] * bscale + bzero;
							});
							return vector;
						}

						case 16:
						{
							Parallel.For(0, nelements, opts, i =>
							{
								int cc = i * 2;
								short val = (short)((arr[cc] << 8) | arr[cc + 1]);
								vector[i] = val * bscale + bzero;
							});
							return vector;
						}

						case 32:
						{
							Parallel.For(0, nelements, opts, i =>
							{
								int cc = i * 4;
								int val = (arr[cc] << 24) | (arr[cc + 1] << 16) | (arr[cc + 2] << 8) | arr[cc + 3];
								vector[i] = val * bscale + bzero;
							});
							return vector;
						}

						case 64:
						{
							Parallel.For(0, nelements, opts, i =>
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
								vector[i] = BitConverter.ToInt64(dbl, 0) * bscale + bzero;
							});
							return vector;
						}

						case -32:
						{
							Parallel.For(0, nelements, opts, i =>
							{
								int cc = i * 4;
								byte[] flt = new byte[4];
								flt[3] = arr[cc];
								flt[2] = arr[cc + 1];
								flt[1] = arr[cc + 2];
								flt[0] = arr[cc + 3];
								vector[i] = BitConverter.ToSingle(flt, 0) * bscale + bzero;
							});
							return vector;
						}

						case -64:
						{
							Parallel.For(0, nelements, opts, i =>
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
								vector[i] = BitConverter.ToDouble(dbl, 0) * bscale + bzero;
							});
							return vector;
						}
					}
				}

				else if (returnPrecision == ReadReturnPrecision.Native)
				{
					switch (bitpix)
					{
						case 8:
						{
							if (bzero == -128)//signed byte
							{
								sbyte[] vector = new sbyte[nelements];

								Parallel.For(0, nelements, opts, i =>
								{
									vector[i] = (sbyte)(arr[i] * bscale + bzero);
								});
								return vector;
							}
							else if (bzero == 0)//unsigned byte
							{
								byte[] vector = new byte[nelements];

								Parallel.For(0, nelements, opts, i =>
								{
									vector[i] = (byte)(arr[i] * bscale + bzero);
								});
								return vector;
							}
							break;
						}

						case 16:
						{
							if (bzero == 0)//signed int16
							{
								short[] vector = new short[nelements];

								Parallel.For(0, nelements, opts, i =>
								{
									int cc = i * 2;
									byte[] bytes = new byte[2];
									bytes[1] = arr[cc];
									bytes[0] = arr[cc + 1];
									vector[i] = (short)(BitConverter.ToInt16(bytes, 0) * bscale + bzero);
								});
								return vector;
							}
							else if (bzero == 32768)//unsigned uint16
							{
								ushort[] vector = new ushort[nelements];

								Parallel.For(0, nelements, opts, i =>
								{
									int cc = i * 2;
									byte[] bytes = new byte[2];
									bytes[1] = arr[cc];
									bytes[0] = arr[cc + 1];
									vector[i] = (ushort)(BitConverter.ToInt16(bytes, 0) * bscale + bzero);
								});
								return vector;
							}
							break;
						}

						case 32:
						{
							if (bzero == 0)//signed int32
							{
								int[] vector = new int[nelements];

								Parallel.For(0, nelements, opts, i =>
								{
									int cc = i * 4;
									byte[] bytes = new byte[4];
									bytes[3] = arr[cc];
									bytes[2] = arr[cc + 1];
									bytes[1] = arr[cc + 2];
									bytes[0] = arr[cc + 3];
									vector[i] = (int)(BitConverter.ToInt32(bytes, 0) * bscale + bzero);
								});
								return vector;								
							}
							else if (bzero == 2147483648)//unsigned uint32
							{
								uint[] vector = new uint[nelements];

								Parallel.For(0, nelements, opts, i =>
								{
									int cc = i * 4;
									byte[] bytes = new byte[4];
									bytes[3] = arr[cc];
									bytes[2] = arr[cc + 1];
									bytes[1] = arr[cc + 2];
									bytes[0] = arr[cc + 3];
									vector[i] = (uint)(BitConverter.ToInt32(bytes, 0) * bscale + bzero);
								});
								return vector;								
							}
							break;
						}

						case 64:
						{
							if (bzero == 0)//signed int64
							{
								long[] vector = new long[nelements];

								Parallel.For(0, nelements, opts, i =>
								{
									int cc = i * 8;
									byte[] bytes = new byte[8];
									bytes[7] = arr[cc];
									bytes[6] = arr[cc + 1];
									bytes[5] = arr[cc + 2];
									bytes[4] = arr[cc + 3];
									bytes[3] = arr[cc + 4];
									bytes[2] = arr[cc + 5];
									bytes[1] = arr[cc + 6];
									bytes[0] = arr[cc + 7];
									vector[i] = (long)(BitConverter.ToInt64(bytes, 0) * bscale + bzero);
								});
								return vector;
							}
							else if (bzero == 9223372036854775808)//unsigned uint64
							{
								ulong[] vector = new ulong[nelements];

								Parallel.For(0, nelements, opts, i =>
								{
									int cc = i * 8;
									byte[] bytes = new byte[8];
									bytes[7] = arr[cc];
									bytes[6] = arr[cc + 1];
									bytes[5] = arr[cc + 2];
									bytes[4] = arr[cc + 3];
									bytes[3] = arr[cc + 4];
									bytes[2] = arr[cc + 5];
									bytes[1] = arr[cc + 6];
									bytes[0] = arr[cc + 7];
									vector[i] = (ulong)(BitConverter.ToInt64(bytes, 0) * bscale + bzero);
								});
								return vector;
							}
							break;
						}

						case -32://single precision float
						{
							float[] vector = new float[nelements];

							Parallel.For(0, nelements, opts, i =>
							{
								int cc = i * 4;
								byte[] flt = new byte[4];
								flt[3] = arr[cc];
								flt[2] = arr[cc + 1];
								flt[1] = arr[cc + 2];
								flt[0] = arr[cc + 3];
								vector[i] = (float)(BitConverter.ToSingle(flt, 0) * bscale + bzero);
							});
							return vector;
						}

						case -64://double precision float
						{
							double[] vector = new double[nelements];

							Parallel.For(0, nelements, opts, i =>
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
								vector[i] = BitConverter.ToDouble(dbl, 0) * bscale + bzero;
							});
							return vector;
						}
					}
				}

				else if (returnPrecision == ReadReturnPrecision.Boolean)
				{
					if (bitpix != 8 && bzero != 0)//unsigned byte
						throw new Exception("Boolean data must be unsigned bytes on disk.");

					bool[] vector = new bool[nelements];

					Parallel.For(0, nelements, opts, i =>
					{
						if (arr[i] == 1)
							vector[i] = true;
					});
					return vector;
				}

				throw new Exception("Made it to end of ReadImageDataUnit returnRankFormat == RankFormat.Vector without returning data.");
			}

			if (returnPrecision == ReadReturnPrecision.Double)
			{
				if (naxisn.Length == 1)//then a vector return
				{
					switch (bitpix)
					{
						case 8:
						{
							if (returnRankFormat == RankFormat.NAXIS)
							{
								double[] dvector = new double[range[1] - range[0] + 1];
								naxisn = new int[1] { dvector.Length };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									dvector[i - range[0]] = arr[i] * bscale + bzero;
								});
								return dvector;
							}
							else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
							{
								double[,] dtable = new double[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									dtable[i - range[0], 0] = arr[i] * bscale + bzero;
								});
								return dtable;
							}
							else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
							{
								double[,] dtable = new double[1, range[1] - range[0] + 1];
								naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									dtable[0, i - range[0]] = arr[i] * bscale + bzero;
								});
								return dtable;
							}
							break;
						}

						case 16:
						{
							if (returnRankFormat == RankFormat.NAXIS)
							{
								double[] dvector = new double[range[1] - range[0] + 1];
								naxisn = new int[1] { dvector.Length };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									int cc = i * 2;
									short val = (short)((arr[cc] << 8) | arr[cc + 1]);
									dvector[i - range[0]] = val * bscale + bzero;
								});
								return dvector;
							}
							else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
							{
								double[,] dtable = new double[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									int cc = i * 2;
									short val = (short)((arr[cc] << 8) | arr[cc + 1]);
									dtable[i - range[0], 0] = val * bscale + bzero;
								});
								return dtable;
							}
							else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
							{
								double[,] dtable = new double[1, range[1] - range[0] + 1];
								naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									int cc = i * 2;
									short val = (short)((arr[cc] << 8) | arr[cc + 1]);
									dtable[0, i - range[0]] = val * bscale + bzero;
								});
								return dtable;
							}
							break;
						}

						case 32:
						{
							if (returnRankFormat == RankFormat.NAXIS)
							{
								double[] dvector = new double[range[1] - range[0] + 1];
								naxisn = new int[1] { dvector.Length };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									int cc = i * 4;
									int val = (arr[cc] << 24) | (arr[cc + 1] << 16) | (arr[cc + 2] << 8) | arr[cc + 3];
									dvector[i - range[0]] = val * bscale + bzero;
								});
								return dvector;
							}
							else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
							{
								double[,] dtable = new double[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									int cc = i * 4;
									int val = (arr[cc] << 24) | (arr[cc + 1] << 16) | (arr[cc + 2] << 8) | arr[cc + 3];
									dtable[i - range[0], 0] = val * bscale + bzero;
								});
								return dtable;
							}
							else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
							{
								double[,] dtable = new double[1, range[1] - range[0] + 1];
								naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									int cc = i * 4;
									int val = (arr[cc] << 24) | (arr[cc + 1] << 16) | (arr[cc + 2] << 8) | arr[cc + 3];
									dtable[0, i - range[0]] = val * bscale + bzero;
								});
								return dtable;
							}
							break;
						}

						case 64:
						{
							if (returnRankFormat == RankFormat.NAXIS)
							{
								double[] dvector = new double[range[1] - range[0] + 1];
								naxisn = new int[1] { dvector.Length };

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
									dvector[i - range[0]] = BitConverter.ToInt64(dbl, 0) * bscale + bzero;
								});
								return dvector;
							}
							else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
							{
								double[,] dtable = new double[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };

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
									dtable[i - range[0], 0] = BitConverter.ToInt64(dbl, 0) * bscale + bzero;
								});
								return dtable;
							}
							else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
							{
								double[,] dtable = new double[1, range[1] - range[0] + 1];
								naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };

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
									dtable[0, i - range[0]] = BitConverter.ToInt64(dbl, 0) * bscale + bzero;
								});
								return dtable;
							}
							break;
						}

						case -32:
						{
							if (returnRankFormat == RankFormat.NAXIS)
							{
								double[] dvector = new double[range[1] - range[0] + 1];
								naxisn = new int[1] { dvector.Length };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									int cc = i * 4;
									byte[] flt = new byte[4];
									flt[3] = arr[cc];
									flt[2] = arr[cc + 1];
									flt[1] = arr[cc + 2];
									flt[0] = arr[cc + 3];
									dvector[i - range[0]] = BitConverter.ToSingle(flt, 0) * bscale + bzero;
								});
								return dvector;
							}
							else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
							{
								double[,] dtable = new double[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									int cc = i * 4;
									byte[] flt = new byte[4];
									flt[3] = arr[cc];
									flt[2] = arr[cc + 1];
									flt[1] = arr[cc + 2];
									flt[0] = arr[cc + 3];
									dtable[i - range[0], 0] = BitConverter.ToSingle(flt, 0) * bscale + bzero;
								});
								return dtable;
							}
							else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
							{
								double[,] dtable = new double[1, range[1] - range[0] + 1];
								naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									int cc = i * 4;
									byte[] flt = new byte[4];
									flt[3] = arr[cc];
									flt[2] = arr[cc + 1];
									flt[1] = arr[cc + 2];
									flt[0] = arr[cc + 3];
									dtable[0, i - range[0]] = BitConverter.ToSingle(flt, 0) * bscale + bzero;
								});
								return dtable;
							}
							break;
						}

						case -64:
						{
							if (returnRankFormat == RankFormat.NAXIS)
							{
								double[] dvector = new double[range[1] - range[0] + 1];
								naxisn = new int[1] { dvector.Length };

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
							else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
							{
								double[,] dtable = new double[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };

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
								return dtable;
							}
							else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
							{
								double[,] dtable = new double[1, range[1] - range[0] + 1];
								naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };

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
								return dtable;
							}
							break;
						}
					}
				}

				else if (naxisn.Length == 2)//then a table or image return
				{
					int naxis0 = naxisn[0];
					double[,] dtable = new double[range[1] - range[0] + 1, range[3] - range[2] + 1];
					naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };

					switch (bitpix)
					{
						case 8:
						{
							Parallel.For(range[2], range[3] + 1, opts, j =>
							{
								int cc = j * naxis0 + range[0];
								for (int i = range[0]; i <= range[1]; i++)
									dtable[i - range[0], j - range[2]] = arr[cc + i] * bscale + bzero;
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
									dtable[i - range[0], j - range[2]] = val * bscale + bzero;
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
									dtable[i - range[0], j - range[2]] = val * bscale + bzero;
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
									dtable[i - range[0], j - range[2]] = BitConverter.ToInt64(dbl, 0) * bscale + bzero;
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
									dtable[i - range[0], j - range[2]] = BitConverter.ToSingle(flt, 0) * bscale + bzero;
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
									dtable[i - range[0], j - range[2]] = BitConverter.ToDouble(dbl, 0) * bscale + bzero;
									cc += 8;
								}
							});
							break;
						}
					}

					if (returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)
						return dtable;
					else if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0)
						return dtable;
					else if (returnRankFormat == RankFormat.ArrayAsRangeRank)
					{
						double[] dvector = new double[(range[1] - range[0] + 1) * (range[3] - range[2] + 1)];

						int cc = 0;
						for (int i = 0; i < dtable.GetLength(0); i++)
							for (int j = 0; j < dtable.GetLength(1); j++)
								dvector[cc++] = dtable[i, j];

						naxisn = new int[1] { dvector.Length };
						return dvector;
					}
				}

				else if (naxisn.Length == 3)//then a data cube
				{
					int naxis0 = naxisn[0], naxis1 = naxisn[1];
					double[,,] dcube = new double[range[1] - range[0] + 1, range[3] - range[2] + 1, range[5] - range[4] + 1];
					naxisn = new int[3] { dcube.GetLength(0), dcube.GetLength(1), dcube.GetLength(2) };

					switch (bitpix)
					{
						case 8:
						{
							Parallel.For(range[4], range[5] + 1, opts, k =>
							{
								int cc = k * naxis0 * naxis1 + range[2] * naxis1 + range[0];

								for (int j = range[2]; j <= range[3]; j++)
									for (int i = range[0]; i <= range[1]; i++)
										dcube[i - range[0], j - range[2], k - range[4]] = arr[cc + i] * bscale + bzero;
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
										dcube[i - range[0], j - range[2], k - range[4]] = val * bscale + bzero;
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
										dcube[i - range[0], j - range[2], k - range[4]] = val * bscale + bzero;
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
										dcube[i - range[0], j - range[2], k - range[4]] = BitConverter.ToInt64(dbl, 0) * bscale + bzero;
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
										dcube[i - range[0], j - range[2], k - range[4]] = BitConverter.ToSingle(flt, 0) * bscale + bzero;
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
										dcube[i - range[0], j - range[2], k - range[4]] = BitConverter.ToDouble(dbl, 0) * bscale + bzero;
										cc += 8;
									}
							});
							break;
						}
					}

					if (returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)
						return dcube;
					else if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 && (range[5] - range[4]) > 0)
						return dcube;
					else if (returnRankFormat == RankFormat.ArrayAsRangeRank)
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
							double[,] dtable;
							if ((range[1] - range[0]) == 0)
							{
								dtable = new double[(range[3] - range[2] + 1), (range[5] - range[4] + 1)];
								for (int i = 0; i < dtable.GetLength(0); i++)
									for (int j = 0; j < dtable.GetLength(1); j++)
										dtable[i, j] = dcube[0, i, j];
							}
							else if ((range[3] - range[2]) == 0)
							{
								dtable = new double[(range[1] - range[0] + 1), (range[5] - range[4] + 1)];
								for (int i = 0; i < dtable.GetLength(0); i++)
									for (int j = 0; j < dtable.GetLength(1); j++)
										dtable[i, j] = dcube[i, 0, j];
							}
							else
							{
								dtable = new double[(range[1] - range[0] + 1), (range[3] - range[2] + 1)];
								for (int i = 0; i < dtable.GetLength(0); i++)
									for (int j = 0; j < dtable.GetLength(1); j++)
										dtable[i, j] = dcube[i, j, 0];
							}

							naxisn = new int[2] { dtable.GetLength(0), dtable.GetLength(1) };
							return dtable;
						}
					}
					else
						throw new Exception("DataUnitReturn option DataUnitReturn.VectorAsTable is not valid for a 3D cube.");
				}
			}

			else if (returnPrecision == ReadReturnPrecision.Native)
			{
				if (naxisn.Length == 1)//then a vector return
				{
					switch (bitpix)
					{
						case 8:
						{
							if (bzero == -128)//signed byte
							{
								if (returnRankFormat == RankFormat.NAXIS)
								{
									sbyte[] vector = new sbyte[range[1] - range[0] + 1];
									naxisn = new int[1] { vector.Length };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										vector[i - range[0]] = (sbyte)(arr[i] * bscale + bzero);
									});
									return vector;
								}
								else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
								{
									sbyte[,] table = new sbyte[range[1] - range[0] + 1, 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										table[i - range[0], 0] = (sbyte)(arr[i] * bscale + bzero);
									});
									return table;
								}
								else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
								{
									sbyte[,] table = new sbyte[1, range[1] - range[0] + 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										table[0, i - range[0]] = (sbyte)(arr[i] * bscale + bzero);
									});
									return table;
								}
							}
							else if (bzero == 0)//unsigned byte
							{
								if (returnRankFormat == RankFormat.NAXIS)
								{
									byte[] vector = new byte[range[1] - range[0] + 1];
									naxisn = new int[1] { vector.Length };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										vector[i - range[0]] = (byte)(arr[i] * bscale + bzero);
									});
									return vector;
								}
								else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
								{
									byte[,] table = new byte[range[1] - range[0] + 1, 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										table[i - range[0], 0] = (byte)(arr[i] * bscale + bzero);
									});
									return table;
								}
								else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
								{
									byte[,] table = new byte[1, range[1] - range[0] + 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										table[0, i - range[0]] = (byte)(arr[i] * bscale + bzero);
									});
									return table;
								}
							}

							break;
						}

						case 16:
						{
							if (bzero == 0)//signed int16
							{
								if (returnRankFormat == RankFormat.NAXIS)
								{
									short[] vector = new short[range[1] - range[0] + 1];
									naxisn = new int[1] { vector.Length };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 2;
										byte[] bytes = new byte[2];
										bytes[1] = arr[cc];
										bytes[0] = arr[cc + 1];
										vector[i - range[0]] = (short)(BitConverter.ToInt16(bytes, 0) * bscale + bzero);
									});
									return vector;
								}
								else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
								{
									short[,] table = new short[range[1] - range[0] + 1, 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 2;
										byte[] bytes = new byte[2];
										bytes[1] = arr[cc];
										bytes[0] = arr[cc + 1];
										table[i - range[0], 0] = (short)(BitConverter.ToInt16(bytes, 0) * bscale + bzero);
									});
									return table;
								}
								else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
								{
									short[,] table = new short[1, range[1] - range[0] + 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 2;
										byte[] bytes = new byte[2];
										bytes[1] = arr[cc];
										bytes[0] = arr[cc + 1];
										table[0, i - range[0]] = (short)(BitConverter.ToInt16(bytes, 0) * bscale + bzero);
									});
									return table;
								}
							}
							else if (bzero == 32768)//unsigned uint16
							{
								if (returnRankFormat == RankFormat.NAXIS)
								{
									ushort[] vector = new ushort[range[1] - range[0] + 1];
									naxisn = new int[1] { vector.Length };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 2;
										byte[] bytes = new byte[2];
										bytes[1] = arr[cc];
										bytes[0] = arr[cc + 1];
										vector[i - range[0]] = (ushort)(BitConverter.ToInt16(bytes, 0) * bscale + bzero);
									});
									return vector;
								}
								else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
								{
									ushort[,] table = new ushort[range[1] - range[0] + 1, 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 2;
										byte[] bytes = new byte[2];
										bytes[1] = arr[cc];
										bytes[0] = arr[cc + 1];
										table[i - range[0], 0] = (ushort)(BitConverter.ToInt16(bytes, 0) * bscale + bzero);
									});
									return table;
								}
								else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
								{
									ushort[,] table = new ushort[1, range[1] - range[0] + 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 2;
										byte[] bytes = new byte[2];
										bytes[1] = arr[cc];
										bytes[0] = arr[cc + 1];
										table[0, i - range[0]] = (ushort)(BitConverter.ToInt16(bytes, 0) * bscale + bzero);
									});
									return table;
								}
							}

							break;
						}

						case 32:
						{
							if (bzero == 0)//signed int32
							{
								if (returnRankFormat == RankFormat.NAXIS)
								{
									int[] vector = new int[range[1] - range[0] + 1];
									naxisn = new int[1] { vector.Length };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 4;
										byte[] bytes = new byte[4];
										bytes[3] = arr[cc];
										bytes[2] = arr[cc + 1];
										bytes[1] = arr[cc + 2];
										bytes[0] = arr[cc + 3];
										vector[i - range[0]] = (int)(BitConverter.ToInt32(bytes, 0) * bscale + bzero);
									});
									return vector;
								}
								else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
								{
									int[,] table = new int[range[1] - range[0] + 1, 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 4;
										byte[] bytes = new byte[4];
										bytes[3] = arr[cc];
										bytes[2] = arr[cc + 1];
										bytes[1] = arr[cc + 2];
										bytes[0] = arr[cc + 3];
										table[i - range[0], 0] = (int)(BitConverter.ToInt32(bytes, 0) * bscale + bzero);
									});
									return table;
								}
								else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
								{
									int[,] table = new int[1, range[1] - range[0] + 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 4;
										byte[] bytes = new byte[4];
										bytes[3] = arr[cc];
										bytes[2] = arr[cc + 1];
										bytes[1] = arr[cc + 2];
										bytes[0] = arr[cc + 3];
										table[0, i - range[0]] = (int)(BitConverter.ToInt32(bytes, 0) * bscale + bzero);
									});
									return table;
								}
							}
							else if (bzero == 2147483648)//unsigned uint32
							{
								if (returnRankFormat == RankFormat.NAXIS)
								{
									uint[] vector = new uint[range[1] - range[0] + 1];
									naxisn = new int[1] { vector.Length };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 4;
										byte[] bytes = new byte[4];
										bytes[3] = arr[cc];
										bytes[2] = arr[cc + 1];
										bytes[1] = arr[cc + 2];
										bytes[0] = arr[cc + 3];
										vector[i - range[0]] = (uint)(BitConverter.ToInt32(bytes, 0) * bscale + bzero);
									});
									return vector;
								}
								else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
								{
									uint[,] table = new uint[range[1] - range[0] + 1, 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 4;
										byte[] bytes = new byte[4];
										bytes[3] = arr[cc];
										bytes[2] = arr[cc + 1];
										bytes[1] = arr[cc + 2];
										bytes[0] = arr[cc + 3];
										table[i - range[0], 0] = (uint)(BitConverter.ToInt32(bytes, 0) * bscale + bzero);
									});
									return table;
								}
								else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
								{
									uint[,] table = new uint[1, range[1] - range[0] + 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 4;
										byte[] bytes = new byte[4];
										bytes[3] = arr[cc];
										bytes[2] = arr[cc + 1];
										bytes[1] = arr[cc + 2];
										bytes[0] = arr[cc + 3];
										table[0, i - range[0]] = (uint)(BitConverter.ToInt32(bytes, 0) * bscale + bzero);
									});
									return table;
								}
							}
							
							break;
						}

						case 64:
						{
							if (bzero == 0)//signed int64
							{
								if (returnRankFormat == RankFormat.NAXIS)
								{
									long[] vector = new long[range[1] - range[0] + 1];
									naxisn = new int[1] { vector.Length };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 8;
										byte[] bytes = new byte[8];
										bytes[7] = arr[cc];
										bytes[6] = arr[cc + 1];
										bytes[5] = arr[cc + 2];
										bytes[4] = arr[cc + 3];
										bytes[3] = arr[cc + 4];
										bytes[2] = arr[cc + 5];
										bytes[1] = arr[cc + 6];
										bytes[0] = arr[cc + 7];
										vector[i - range[0]] = (long)(BitConverter.ToInt64(bytes, 0) * bscale + bzero);
									});
									return vector;
								}
								else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
								{
									long[,] table = new long[range[1] - range[0] + 1, 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 8;
										byte[] bytes = new byte[8];
										bytes[7] = arr[cc];
										bytes[6] = arr[cc + 1];
										bytes[5] = arr[cc + 2];
										bytes[4] = arr[cc + 3];
										bytes[3] = arr[cc + 4];
										bytes[2] = arr[cc + 5];
										bytes[1] = arr[cc + 6];
										bytes[0] = arr[cc + 7];
										table[i - range[0], 0] = (long)(BitConverter.ToInt64(bytes, 0) * bscale + bzero);
									});
									return table;
								}
								else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
								{
									long[,] table = new long[1, range[1] - range[0] + 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 8;
										byte[] bytes = new byte[8];
										bytes[7] = arr[cc];
										bytes[6] = arr[cc + 1];
										bytes[5] = arr[cc + 2];
										bytes[4] = arr[cc + 3];
										bytes[3] = arr[cc + 4];
										bytes[2] = arr[cc + 5];
										bytes[1] = arr[cc + 6];
										bytes[0] = arr[cc + 7];
										table[0, i - range[0]] = (long)(BitConverter.ToInt64(bytes, 0) * bscale + bzero);
									});
									return table;
								}
							}
							else if (bzero == 9223372036854775808)//unsigned uint64
							{
								if (returnRankFormat == RankFormat.NAXIS)
								{
									ulong[] vector = new ulong[range[1] - range[0] + 1];
									naxisn = new int[1] { vector.Length };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 8;
										byte[] bytes = new byte[8];
										bytes[7] = arr[cc];
										bytes[6] = arr[cc + 1];
										bytes[5] = arr[cc + 2];
										bytes[4] = arr[cc + 3];
										bytes[3] = arr[cc + 4];
										bytes[2] = arr[cc + 5];
										bytes[1] = arr[cc + 6];
										bytes[0] = arr[cc + 7];
										vector[i - range[0]] = (ulong)(BitConverter.ToInt64(bytes, 0) * bscale + bzero);
									});
									return vector;
								}
								else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
								{
									ulong[,] table = new ulong[range[1] - range[0] + 1, 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 8;
										byte[] bytes = new byte[8];
										bytes[7] = arr[cc];
										bytes[6] = arr[cc + 1];
										bytes[5] = arr[cc + 2];
										bytes[4] = arr[cc + 3];
										bytes[3] = arr[cc + 4];
										bytes[2] = arr[cc + 5];
										bytes[1] = arr[cc + 6];
										bytes[0] = arr[cc + 7];
										table[i - range[0], 0] = (ulong)(BitConverter.ToInt64(bytes, 0) * bscale + bzero);
									});
									return table;
								}
								else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
								{
									ulong[,] table = new ulong[1, range[1] - range[0] + 1];
									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

									Parallel.For(range[0], range[1] + 1, opts, i =>
									{
										int cc = i * 8;
										byte[] bytes = new byte[8];
										bytes[7] = arr[cc];
										bytes[6] = arr[cc + 1];
										bytes[5] = arr[cc + 2];
										bytes[4] = arr[cc + 3];
										bytes[3] = arr[cc + 4];
										bytes[2] = arr[cc + 5];
										bytes[1] = arr[cc + 6];
										bytes[0] = arr[cc + 7];
										table[0, i - range[0]] = (ulong)(BitConverter.ToInt64(bytes, 0) * bscale + bzero);
									});
									return table;
								}
							}
							
							break;
						}

						case -32://single precision float
						{
							if (returnRankFormat == RankFormat.NAXIS)
							{
								float[] vector = new float[range[1] - range[0] + 1];
								naxisn = new int[1] { vector.Length };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									int cc = i * 4;
									byte[] flt = new byte[4];
									flt[3] = arr[cc];
									flt[2] = arr[cc + 1];
									flt[1] = arr[cc + 2];
									flt[0] = arr[cc + 3];
									vector[i - range[0]] = (float)(BitConverter.ToSingle(flt, 0) * bscale + bzero);
								});
								return vector;
							}
							else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
							{
								float[,] table = new float[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									int cc = i * 4;
									byte[] flt = new byte[4];
									flt[3] = arr[cc];
									flt[2] = arr[cc + 1];
									flt[1] = arr[cc + 2];
									flt[0] = arr[cc + 3];
									table[i - range[0], 0] = (float)(BitConverter.ToSingle(flt, 0) * bscale + bzero);
								});
								return table;
							}
							else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
							{
								float[,] table = new float[1, range[1] - range[0] + 1];
								naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

								Parallel.For(range[0], range[1] + 1, opts, i =>
								{
									int cc = i * 4;
									byte[] flt = new byte[4];
									flt[3] = arr[cc];
									flt[2] = arr[cc + 1];
									flt[1] = arr[cc + 2];
									flt[0] = arr[cc + 3];
									table[0, i - range[0]] = (float)(BitConverter.ToSingle(flt, 0) * bscale + bzero);
								});
								return table;
							}
							break;
						}

						case -64://double precision float
						{
							if (returnRankFormat == RankFormat.NAXIS)
							{
								double[] vector = new double[range[1] - range[0] + 1];
								naxisn = new int[1] { vector.Length };

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
									vector[i - range[0]] = BitConverter.ToDouble(dbl, 0) * bscale + bzero;
								});
								return vector;
							}
							else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
							{
								double[,] table = new double[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

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
									table[i - range[0], 0] = BitConverter.ToDouble(dbl, 0) * bscale + bzero;
								});
								return table;
							}
							else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
							{
								double[,] table = new double[1, range[1] - range[0] + 1];
								naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

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
									table[0, i - range[0]] = BitConverter.ToDouble(dbl, 0) * bscale + bzero;
								});
								return table;
							}
							break;
						}
					}
				}

				else if (naxisn.Length == 2)//then a table or image return
				{
					int naxis0 = naxisn[0];

					switch (bitpix)
					{
						case 8:
						{
							if (bzero == -128)//signed byte
							{
								sbyte[,] table = new sbyte[range[1] - range[0] + 1, range[3] - range[2] + 1];
								naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

								Parallel.For(range[2], range[3] + 1, opts, j =>
								{
									int cc = j * naxis0 + range[0];
									for (int i = range[0]; i <= range[1]; i++)
										table[i - range[0], j - range[2]] = (sbyte)(arr[cc + i] * bscale + bzero);
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)//must be a table
									return table;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)//must be a vector if gotten to here
								{
									sbyte[] vector = new sbyte[(range[1] - range[0] + 1) * (range[3] - range[2] + 1)];
									System.Buffer.BlockCopy(table, 0, vector, 0, vector.Length * 1);
									naxisn = new int[1] { vector.Length };
									return vector;
								}
							}
							else if (bzero == 0)//unsigned byte
							{
								byte[,] table = new byte[range[1] - range[0] + 1, range[3] - range[2] + 1];
								naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

								Parallel.For(range[2], range[3] + 1, opts, j =>
								{
									int cc = j * naxis0 + range[0];
									for (int i = range[0]; i <= range[1]; i++)
										table[i - range[0], j - range[2]] = (byte)(arr[cc + i] * bscale + bzero);
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)//must be a table
									return table;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)//must be a vector if gotten to here
								{
									byte[] vector = new byte[(range[1] - range[0] + 1) * (range[3] - range[2] + 1)];
									System.Buffer.BlockCopy(table, 0, vector, 0, vector.Length * 1);
									naxisn = new int[1] { vector.Length };
									return vector;
								}
							}
							break;
						}

						case 16:
						{
							if (bzero == 0)//signed int16
							{
								short[,] table = new short[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

								Parallel.For(range[2], range[3] + 1, opts, j =>
								{
									int cc = (j * naxis0 + range[0]) * 2;
									byte[] bytes = new byte[2];
									for (int i = range[0]; i <= range[1]; i++)
									{
										bytes[1] = arr[cc];
										bytes[0] = arr[cc + 1];
										table[i - range[0], j - range[2]] = (short)(BitConverter.ToInt16(bytes, 0) * bscale + bzero);
										cc += 2;
									}
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)//must be a table
									return table;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)//must be a vector if gotten to here
								{
									short[] vector = new short[(range[1] - range[0] + 1) * (range[3] - range[2] + 1)];
									System.Buffer.BlockCopy(table, 0, vector, 0, vector.Length * 2);
									naxisn = new int[1] { vector.Length };
									return vector;
								}
							}
							else if (bzero == 32768)//unsigned uint16
							{
								ushort[,] table = new ushort[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

								Parallel.For(range[2], range[3] + 1, opts, j =>
								{
									int cc = (j * naxis0 + range[0]) * 2;
									byte[] bytes = new byte[2];
									for (int i = range[0]; i <= range[1]; i++)
									{
										bytes[1] = arr[cc];
										bytes[0] = arr[cc + 1];
										table[i - range[0], j - range[2]] = (ushort)(BitConverter.ToInt16(bytes, 0) * bscale + bzero);
										cc += 2;
									}
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)//must be a table
									return table;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)//must be a vector if gotten to here
								{
									ushort[] vector = new ushort[(range[1] - range[0] + 1) * (range[3] - range[2] + 1)];
									System.Buffer.BlockCopy(table, 0, vector, 0, vector.Length * 2);
									naxisn = new int[1] { vector.Length };
									return vector;
								}
							}
							
							break;
						}

						case 32:
						{
							if (bzero == 0)//signed int32
							{
								int[,] table = new int[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

								Parallel.For(range[2], range[3] + 1, opts, j =>
								{
									int cc = (j * naxis0 + range[0]) * 4;
									byte[] bytes = new byte[4];
									for (int i = range[0]; i <= range[1]; i++)
									{
										bytes[3] = arr[cc];
										bytes[2] = arr[cc + 1];
										bytes[1] = arr[cc + 2];
										bytes[0] = arr[cc + 3];
										table[i - range[0], j - range[2]] = (int)(BitConverter.ToInt32(bytes, 0) * bscale + bzero);
										cc += 4;
									}
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)//must be a table
									return table;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)//must be a vector if gotten to here
								{
									int[] vector = new int[(range[1] - range[0] + 1) * (range[3] - range[2] + 1)];
									System.Buffer.BlockCopy(table, 0, vector, 0, vector.Length * 4);
									naxisn = new int[1] { vector.Length };
									return vector;
								}
							}
							else if (bzero == 2147483648)//unsigned uint32
							{
								uint[,] table = new uint[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

								Parallel.For(range[2], range[3] + 1, opts, j =>
								{
									int cc = (j * naxis0 + range[0]) * 4;
									byte[] bytes = new byte[4];
									for (int i = range[0]; i <= range[1]; i++)
									{
										bytes[3] = arr[cc];
										bytes[2] = arr[cc + 1];
										bytes[1] = arr[cc + 2];
										bytes[0] = arr[cc + 3];
										table[i - range[0], j - range[2]] = (uint)(BitConverter.ToInt32(bytes, 0) * bscale + bzero);
										cc += 4;
									}
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)//must be a table
									return table;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)//must be a vector if gotten to here
								{
									uint[] vector = new uint[(range[1] - range[0] + 1) * (range[3] - range[2] + 1)];
									System.Buffer.BlockCopy(table, 0, vector, 0, vector.Length * 4);
									naxisn = new int[1] { vector.Length };
									return vector;
								}
							}

							break;
						}

						case 64:
						{
							if (bzero == 0)//signed int64
							{
								long[,] table = new long[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

								Parallel.For(range[2], range[3] + 1, opts, j =>
								{
									int cc = (j * naxis0 + range[0]) * 8;
									byte[] bytes = new byte[8];
									for (int i = range[0]; i <= range[1]; i++)
									{
										bytes[7] = arr[cc];
										bytes[6] = arr[cc + 1];
										bytes[5] = arr[cc + 2];
										bytes[4] = arr[cc + 3];
										bytes[3] = arr[cc + 4];
										bytes[2] = arr[cc + 5];
										bytes[1] = arr[cc + 6];
										bytes[0] = arr[cc + 7];
										table[i - range[0], j - range[2]] = (long)(BitConverter.ToInt64(bytes, 0) * bscale + bzero);
										cc += 8;
									}
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)//must be a table
									return table;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)//must be a vector if gotten to here
								{
									long[] vector = new long[(range[1] - range[0] + 1) * (range[3] - range[2] + 1)];
									System.Buffer.BlockCopy(table, 0, vector, 0, vector.Length * 8);
									naxisn = new int[1] { vector.Length };
									return vector;
								}
							}
							else if (bzero == 9223372036854775808)//unsigned uint64
							{
								ulong[,] table = new ulong[range[1] - range[0] + 1, 1];
								naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

								Parallel.For(range[2], range[3] + 1, opts, j =>
								{
									int cc = (j * naxis0 + range[0]) * 8;
									byte[] bytes = new byte[8];
									for (int i = range[0]; i <= range[1]; i++)
									{
										bytes[7] = arr[cc];
										bytes[6] = arr[cc + 1];
										bytes[5] = arr[cc + 2];
										bytes[4] = arr[cc + 3];
										bytes[3] = arr[cc + 4];
										bytes[2] = arr[cc + 5];
										bytes[1] = arr[cc + 6];
										bytes[0] = arr[cc + 7];
										table[i - range[0], j - range[2]] = (ulong)(BitConverter.ToInt64(bytes, 0) * bscale + bzero);
										cc += 8;
									}
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)//must be a table
									return table;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)//must be a vector if gotten to here
								{
									long[] vector = new long[(range[1] - range[0] + 1) * (range[3] - range[2] + 1)];
									System.Buffer.BlockCopy(table, 0, vector, 0, vector.Length * 8);
									naxisn = new int[1] { vector.Length };
									return vector;
								}
							}

							break;
						}

						case -32:
						{
							float[,] table = new float[range[1] - range[0] + 1, 1];
							naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

							Parallel.For(range[2], range[3] + 1, opts, j =>
							{
								int cc = (j * naxis0 + range[0]) * 4;
								byte[] bytes = new byte[4];
								for (int i = range[0]; i <= range[1]; i++)
								{
									bytes[3] = arr[cc];
									bytes[2] = arr[cc + 1];
									bytes[1] = arr[cc + 2];
									bytes[0] = arr[cc + 3];
									table[i - range[0], j - range[2]] = (float)(BitConverter.ToSingle(bytes, 0) * bscale + bzero);
									cc += 4;
								}
							});

							if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)//must be a table
								return table;
							else if (returnRankFormat == RankFormat.ArrayAsRangeRank)//must be a vector if gotten to here
							{
								float[] vector = new float[(range[1] - range[0] + 1) * (range[3] - range[2] + 1)];
								System.Buffer.BlockCopy(table, 0, vector, 0, vector.Length * 4);
								naxisn = new int[1] { vector.Length };
								return vector;
							}

							break;
						}

						case -64:
						{
							double[,] table = new double[range[1] - range[0] + 1, 1];
							naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

							Parallel.For(range[2], range[3] + 1, opts, j =>
							{
								int cc = (j * naxis0 + range[0]) * 8;
								byte[] bytes = new byte[8];
								for (int i = range[0]; i <= range[1]; i++)
								{
									bytes[7] = arr[cc];
									bytes[6] = arr[cc + 1];
									bytes[5] = arr[cc + 2];
									bytes[4] = arr[cc + 3];
									bytes[3] = arr[cc + 4];
									bytes[2] = arr[cc + 5];
									bytes[1] = arr[cc + 6];
									bytes[0] = arr[cc + 7];
									table[i - range[0], j - range[2]] = BitConverter.ToDouble(bytes, 0) * bscale + bzero;
									cc += 8;
								}
							});

							if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)//must be a table
								return table;
							else if (returnRankFormat == RankFormat.ArrayAsRangeRank)//must be a vector if gotten to here
							{
								double[] vector = new double[(range[1] - range[0] + 1) * (range[3] - range[2] + 1)];
								System.Buffer.BlockCopy(table, 0, vector, 0, vector.Length * 8);
								naxisn = new int[1] { vector.Length };
								return vector;
							}

							break;
						}
					}
				}

				else if (naxisn.Length == 3)//then a data cube
				{
					int naxis0 = naxisn[0], naxis1 = naxisn[1];

					switch (bitpix)
					{
						case 8:
						{
							if (bzero == -128)//signed byte
							{
								sbyte[,,] cube = new sbyte[range[1] - range[0] + 1, range[3] - range[2] + 1, range[5] - range[4] + 1];
								naxisn = new int[3] { cube.GetLength(0), cube.GetLength(1), cube.GetLength(2) };

								Parallel.For(range[4], range[5] + 1, opts, k =>
								{
									int cc = k * naxis0 * naxis1 + range[2] * naxis1 + range[0];

									for (int j = range[2]; j <= range[3]; j++)
										for (int i = range[0]; i <= range[1]; i++)
											cube[i - range[0], j - range[2], k - range[4]] = (sbyte)(arr[cc + i] * bscale + bzero);
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 && (range[5] - range[4]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)
									return cube;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)
								{
									//check if vector
									if (((range[1] - range[0]) == 0 && (range[3] - range[2]) == 0) || ((range[1] - range[0]) == 0 && (range[5] - range[4]) == 0) || ((range[3] - range[2]) == 0 && (range[5] - range[4]) == 0))
									{
										sbyte[] vector = new sbyte[(range[1] - range[0] + 1) * (range[3] - range[2] + 1) * (range[5] - range[4] + 1)];
										int cc = 0;
										for (int i = 0; i < cube.GetLength(0); i++)
											for (int j = 0; j < cube.GetLength(1); j++)
												for (int k = 0; k < cube.GetLength(2); k++)
													vector[cc++] = cube[i, j, k];

										naxisn = new int[1] { vector.Length };
										return vector;
									}
									else//must be 2d if gotten here
									{
										sbyte[,] table = null;
										if ((range[1] - range[0]) == 0)
										{
											table = new sbyte[(range[3] - range[2] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[0, i, j];
										}
										else if ((range[3] - range[2]) == 0)
										{
											table = new sbyte[(range[1] - range[0] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, 0, j];
										}
										else if ((range[5] - range[4]) == 0)
										{
											table = new sbyte[(range[1] - range[0] + 1), (range[3] - range[2] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, j, 0];
										}

										naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };
										return table;
									}
								}
							}
							else if (bzero == 0)//unsigned byte
							{
								byte[,,] cube = new byte[range[1] - range[0] + 1, range[3] - range[2] + 1, range[5] - range[4] + 1];
								naxisn = new int[3] { cube.GetLength(0), cube.GetLength(1), cube.GetLength(2) };

								Parallel.For(range[4], range[5] + 1, opts, k =>
								{
									int cc = k * naxis0 * naxis1 + range[2] * naxis1 + range[0];

									for (int j = range[2]; j <= range[3]; j++)
										for (int i = range[0]; i <= range[1]; i++)
											cube[i - range[0], j - range[2], k - range[4]] = (byte)(arr[cc + i] * bscale + bzero);
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 && (range[5] - range[4]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)
									return cube;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)
								{
									//check if vector
									if (((range[1] - range[0]) == 0 && (range[3] - range[2]) == 0) || ((range[1] - range[0]) == 0 && (range[5] - range[4]) == 0) || ((range[3] - range[2]) == 0 && (range[5] - range[4]) == 0))
									{
										byte[] vector = new byte[(range[1] - range[0] + 1) * (range[3] - range[2] + 1) * (range[5] - range[4] + 1)];
										int cc = 0;
										for (int i = 0; i < cube.GetLength(0); i++)
											for (int j = 0; j < cube.GetLength(1); j++)
												for (int k = 0; k < cube.GetLength(2); k++)
													vector[cc++] = cube[i, j, k];

										naxisn = new int[1] { vector.Length };
										return vector;
									}
									else//must be 2d if gotten here
									{
										byte[,] table = null;
										if ((range[1] - range[0]) == 0)
										{
											table = new byte[(range[3] - range[2] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[0, i, j];
										}
										else if ((range[3] - range[2]) == 0)
										{
											table = new byte[(range[1] - range[0] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, 0, j];
										}
										else if ((range[5] - range[4]) == 0)
										{
											table = new byte[(range[1] - range[0] + 1), (range[3] - range[2] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, j, 0];
										}

										naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };
										return table;
									}
								}
							}

							break;
						}

						case 16:
						{
							if (bzero == 0)//signed int16
							{
								short[,,] cube = new short[range[1] - range[0] + 1, range[3] - range[2] + 1, range[5] - range[4] + 1];
								naxisn = new int[3] { cube.GetLength(0), cube.GetLength(1), cube.GetLength(2) };

								Parallel.For(range[4], range[5] + 1, opts, k =>
								{
									int cc = (k * naxis0 * naxis1 + range[2] * naxis1 + range[0]) * 2;
									byte[] bytes = new byte[2];
									for (int j = range[2]; j <= range[3]; j++)
										for (int i = range[0]; i <= range[1]; i++)
										{
											bytes[1] = arr[cc];
											bytes[0] = arr[cc + 1];
											cube[i - range[0], j - range[2], k - range[4]] = (short)(BitConverter.ToInt16(bytes, 0) * bscale + bzero);
											cc += 2;
										}
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 && (range[5] - range[4]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)
									return cube;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)
								{
									//check if vector
									if (((range[1] - range[0]) == 0 && (range[3] - range[2]) == 0) || ((range[1] - range[0]) == 0 && (range[5] - range[4]) == 0) || ((range[3] - range[2]) == 0 && (range[5] - range[4]) == 0))
									{
										short[] vector = new short[(range[1] - range[0] + 1) * (range[3] - range[2] + 1) * (range[5] - range[4] + 1)];
										int cc = 0;
										for (int i = 0; i < cube.GetLength(0); i++)
											for (int j = 0; j < cube.GetLength(1); j++)
												for (int k = 0; k < cube.GetLength(2); k++)
													vector[cc++] = cube[i, j, k];

										naxisn = new int[1] { vector.Length };
										return vector;
									}
									else//must be 2d if gotten here
									{
										short[,] table = null;
										if ((range[1] - range[0]) == 0)
										{
											table = new short[(range[3] - range[2] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[0, i, j];
										}
										else if ((range[3] - range[2]) == 0)
										{
											table = new short[(range[1] - range[0] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, 0, j];
										}
										else if ((range[5] - range[4]) == 0)
										{
											table = new short[(range[1] - range[0] + 1), (range[3] - range[2] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, j, 0];
										}

										naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };
										return table;
									}
								}
							}
							else if (bzero == 32768)//unsigned uint16
							{
								ushort[,,] cube = new ushort[range[1] - range[0] + 1, range[3] - range[2] + 1, range[5] - range[4] + 1];
								naxisn = new int[3] { cube.GetLength(0), cube.GetLength(1), cube.GetLength(2) };

								Parallel.For(range[4], range[5] + 1, opts, k =>
								{
									int cc = (k * naxis0 * naxis1 + range[2] * naxis1 + range[0]) * 2;
									byte[] bytes = new byte[2];
									for (int j = range[2]; j <= range[3]; j++)
										for (int i = range[0]; i <= range[1]; i++)
										{
											bytes[1] = arr[cc];
											bytes[0] = arr[cc + 1];
											cube[i - range[0], j - range[2], k - range[4]] = (ushort)(BitConverter.ToInt16(bytes, 0) * bscale + bzero);
											cc += 2;
										}
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 && (range[5] - range[4]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)
									return cube;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)
								{
									//check if vector
									if (((range[1] - range[0]) == 0 && (range[3] - range[2]) == 0) || ((range[1] - range[0]) == 0 && (range[5] - range[4]) == 0) || ((range[3] - range[2]) == 0 && (range[5] - range[4]) == 0))
									{
										ushort[] vector = new ushort[(range[1] - range[0] + 1) * (range[3] - range[2] + 1) * (range[5] - range[4] + 1)];
										int cc = 0;
										for (int i = 0; i < cube.GetLength(0); i++)
											for (int j = 0; j < cube.GetLength(1); j++)
												for (int k = 0; k < cube.GetLength(2); k++)
													vector[cc++] = cube[i, j, k];

										naxisn = new int[1] { vector.Length };
										return vector;
									}
									else//must be 2d if gotten here
									{
										ushort[,] table = null;
										if ((range[1] - range[0]) == 0)
										{
											table = new ushort[(range[3] - range[2] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[0, i, j];
										}
										else if ((range[3] - range[2]) == 0)
										{
											table = new ushort[(range[1] - range[0] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, 0, j];
										}
										else if ((range[5] - range[4]) == 0)
										{
											table = new ushort[(range[1] - range[0] + 1), (range[3] - range[2] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, j, 0];
										}

										naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };
										return table;
									}
								}
							}

							break;
						}

						case 32:
						{
							if (bzero == 0)//signed int32
							{
								int[,,] cube = new int[range[1] - range[0] + 1, range[3] - range[2] + 1, range[5] - range[4] + 1];
								naxisn = new int[3] { cube.GetLength(0), cube.GetLength(1), cube.GetLength(2) };

								Parallel.For(range[4], range[5] + 1, opts, k =>
								{
									int cc = (k * naxis0 * naxis1 + range[2] * naxis1 + range[0]) * 4;
									byte[] bytes = new byte[4];
									for (int j = range[2]; j <= range[3]; j++)
										for (int i = range[0]; i <= range[1]; i++)
										{
											bytes[3] = arr[cc];
											bytes[2] = arr[cc + 1];
											bytes[1] = arr[cc + 2];
											bytes[0] = arr[cc + 3];
											cube[i - range[0], j - range[2], k - range[4]] = (int)(BitConverter.ToInt32(bytes, 0) * bscale + bzero);
											cc += 4;
										}
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 && (range[5] - range[4]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)
									return cube;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)
								{
									//check if vector
									if (((range[1] - range[0]) == 0 && (range[3] - range[2]) == 0) || ((range[1] - range[0]) == 0 && (range[5] - range[4]) == 0) || ((range[3] - range[2]) == 0 && (range[5] - range[4]) == 0))
									{
										int[] vector = new int[(range[1] - range[0] + 1) * (range[3] - range[2] + 1) * (range[5] - range[4] + 1)];
										int cc = 0;
										for (int i = 0; i < cube.GetLength(0); i++)
											for (int j = 0; j < cube.GetLength(1); j++)
												for (int k = 0; k < cube.GetLength(2); k++)
													vector[cc++] = cube[i, j, k];

										naxisn = new int[1] { vector.Length };
										return vector;
									}
									else//must be 2d if gotten here
									{
										int[,] table = null;
										if ((range[1] - range[0]) == 0)
										{
											table = new int[(range[3] - range[2] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[0, i, j];
										}
										else if ((range[3] - range[2]) == 0)
										{
											table = new int[(range[1] - range[0] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, 0, j];
										}
										else if ((range[5] - range[4]) == 0)
										{
											table = new int[(range[1] - range[0] + 1), (range[3] - range[2] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, j, 0];
										}

										naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };
										return table;
									}
								}
							}
							else if (bzero == 2147483648)//unsigned uint32
							{
								uint[,,] cube = new uint[range[1] - range[0] + 1, range[3] - range[2] + 1, range[5] - range[4] + 1];
								naxisn = new int[3] { cube.GetLength(0), cube.GetLength(1), cube.GetLength(2) };

								Parallel.For(range[4], range[5] + 1, opts, k =>
								{
									int cc = (k * naxis0 * naxis1 + range[2] * naxis1 + range[0]) * 4;
									byte[] bytes = new byte[4];
									for (int j = range[2]; j <= range[3]; j++)
										for (int i = range[0]; i <= range[1]; i++)
										{
											bytes[3] = arr[cc];
											bytes[2] = arr[cc + 1];
											bytes[1] = arr[cc + 2];
											bytes[0] = arr[cc + 3];
											cube[i - range[0], j - range[2], k - range[4]] = (uint)(BitConverter.ToInt32(bytes, 0) * bscale + bzero);
											cc += 4;
										}
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 && (range[5] - range[4]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)
									return cube;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)
								{
									//check if vector
									if (((range[1] - range[0]) == 0 && (range[3] - range[2]) == 0) || ((range[1] - range[0]) == 0 && (range[5] - range[4]) == 0) || ((range[3] - range[2]) == 0 && (range[5] - range[4]) == 0))
									{
										uint[] vector = new uint[(range[1] - range[0] + 1) * (range[3] - range[2] + 1) * (range[5] - range[4] + 1)];
										int cc = 0;
										for (int i = 0; i < cube.GetLength(0); i++)
											for (int j = 0; j < cube.GetLength(1); j++)
												for (int k = 0; k < cube.GetLength(2); k++)
													vector[cc++] = cube[i, j, k];

										naxisn = new int[1] { vector.Length };
										return vector;
									}
									else//must be 2d if gotten here
									{
										uint[,] table = null;
										if ((range[1] - range[0]) == 0)
										{
											table = new uint[(range[3] - range[2] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[0, i, j];
										}
										else if ((range[3] - range[2]) == 0)
										{
											table = new uint[(range[1] - range[0] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, 0, j];
										}
										else if ((range[5] - range[4]) == 0)
										{
											table = new uint[(range[1] - range[0] + 1), (range[3] - range[2] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, j, 0];
										}

										naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };
										return table;
									}
								}
							}

							break;
						}

						case 64:
						{
							if (bzero == 0)//signed int64
							{
								long[,,] cube = new long[range[1] - range[0] + 1, range[3] - range[2] + 1, range[5] - range[4] + 1];
								naxisn = new int[3] { cube.GetLength(0), cube.GetLength(1), cube.GetLength(2) };

								Parallel.For(range[4], range[5] + 1, opts, k =>
								{
									int cc = (k * naxis0 * naxis1 + range[2] * naxis1 + range[0]) * 8;
									byte[] bytes = new byte[8];
									for (int j = range[2]; j <= range[3]; j++)
										for (int i = range[0]; i <= range[1]; i++)
										{
											bytes[7] = arr[cc];
											bytes[6] = arr[cc + 1];
											bytes[5] = arr[cc + 2];
											bytes[4] = arr[cc + 3];
											bytes[3] = arr[cc + 4];
											bytes[2] = arr[cc + 5];
											bytes[1] = arr[cc + 6];
											bytes[0] = arr[cc + 7];
											cube[i - range[0], j - range[2], k - range[4]] = (long)(BitConverter.ToInt64(bytes, 0) * bscale + bzero);
											cc += 8;
										}
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 && (range[5] - range[4]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)
									return cube;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)
								{
									//check if vector
									if (((range[1] - range[0]) == 0 && (range[3] - range[2]) == 0) || ((range[1] - range[0]) == 0 && (range[5] - range[4]) == 0) || ((range[3] - range[2]) == 0 && (range[5] - range[4]) == 0))
									{
										long[] vector = new long[(range[1] - range[0] + 1) * (range[3] - range[2] + 1) * (range[5] - range[4] + 1)];
										int cc = 0;
										for (int i = 0; i < cube.GetLength(0); i++)
											for (int j = 0; j < cube.GetLength(1); j++)
												for (int k = 0; k < cube.GetLength(2); k++)
													vector[cc++] = cube[i, j, k];

										naxisn = new int[1] { vector.Length };
										return vector;
									}
									else//must be 2d if gotten here
									{
										long[,] table = null;
										if ((range[1] - range[0]) == 0)
										{
											table = new long[(range[3] - range[2] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[0, i, j];
										}
										else if ((range[3] - range[2]) == 0)
										{
											table = new long[(range[1] - range[0] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, 0, j];
										}
										else if ((range[5] - range[4]) == 0)
										{
											table = new long[(range[1] - range[0] + 1), (range[3] - range[2] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, j, 0];
										}

										naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };
										return table;
									}
								}
							}
							else if (bzero == 9223372036854775808)//unsigned uint64
							{
								ulong[,,] cube = new ulong[range[1] - range[0] + 1, range[3] - range[2] + 1, range[5] - range[4] + 1];
								naxisn = new int[3] { cube.GetLength(0), cube.GetLength(1), cube.GetLength(2) };

								Parallel.For(range[4], range[5] + 1, opts, k =>
								{
									int cc = (k * naxis0 * naxis1 + range[2] * naxis1 + range[0]) * 8;
									byte[] bytes = new byte[8];
									for (int j = range[2]; j <= range[3]; j++)
										for (int i = range[0]; i <= range[1]; i++)
										{
											bytes[7] = arr[cc];
											bytes[6] = arr[cc + 1];
											bytes[5] = arr[cc + 2];
											bytes[4] = arr[cc + 3];
											bytes[3] = arr[cc + 4];
											bytes[2] = arr[cc + 5];
											bytes[1] = arr[cc + 6];
											bytes[0] = arr[cc + 7];
											cube[i - range[0], j - range[2], k - range[4]] = (ulong)(BitConverter.ToInt64(bytes, 0) * bscale + bzero);
											cc += 8;
										}
								});

								if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 && (range[5] - range[4]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)
									return cube;
								else if (returnRankFormat == RankFormat.ArrayAsRangeRank)
								{
									//check if vector
									if (((range[1] - range[0]) == 0 && (range[3] - range[2]) == 0) || ((range[1] - range[0]) == 0 && (range[5] - range[4]) == 0) || ((range[3] - range[2]) == 0 && (range[5] - range[4]) == 0))
									{
										ulong[] vector = new ulong[(range[1] - range[0] + 1) * (range[3] - range[2] + 1) * (range[5] - range[4] + 1)];
										int cc = 0;
										for (int i = 0; i < cube.GetLength(0); i++)
											for (int j = 0; j < cube.GetLength(1); j++)
												for (int k = 0; k < cube.GetLength(2); k++)
													vector[cc++] = cube[i, j, k];

										naxisn = new int[1] { vector.Length };
										return vector;
									}
									else//must be 2d if gotten here
									{
										ulong[,] table = null;
										if ((range[1] - range[0]) == 0)
										{
											table = new ulong[(range[3] - range[2] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[0, i, j];
										}
										else if ((range[3] - range[2]) == 0)
										{
											table = new ulong[(range[1] - range[0] + 1), (range[5] - range[4] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, 0, j];
										}
										else if ((range[5] - range[4]) == 0)
										{
											table = new ulong[(range[1] - range[0] + 1), (range[3] - range[2] + 1)];
											for (int i = 0; i < table.GetLength(0); i++)
												for (int j = 0; j < table.GetLength(1); j++)
													table[i, j] = cube[i, j, 0];
										}

										naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };
										return table;
									}
								}
							}

							break;
						}

						case -32:
						{
							float[,,] cube = new float[range[1] - range[0] + 1, range[3] - range[2] + 1, range[5] - range[4] + 1];
							naxisn = new int[3] { cube.GetLength(0), cube.GetLength(1), cube.GetLength(2) };

							Parallel.For(range[4], range[5] + 1, opts, k =>
							{
								int cc = (k * naxis0 * naxis1 + range[2] * naxis1 + range[0]) * 4;
								byte[] bytes = new byte[4];

								for (int j = range[2]; j <= range[3]; j++)
									for (int i = range[0]; i <= range[1]; i++)
									{
										bytes[3] = arr[cc];
										bytes[2] = arr[cc + 1];
										bytes[1] = arr[cc + 2];
										bytes[0] = arr[cc + 3];
										cube[i - range[0], j - range[2], k - range[4]] = (float)(BitConverter.ToSingle(bytes, 0) * bscale + bzero);
										cc += 4;
									}
							});

							if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 && (range[5] - range[4]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)
								return cube;
							else if (returnRankFormat == RankFormat.ArrayAsRangeRank)
							{
								//check if vector
								if (((range[1] - range[0]) == 0 && (range[3] - range[2]) == 0) || ((range[1] - range[0]) == 0 && (range[5] - range[4]) == 0) || ((range[3] - range[2]) == 0 && (range[5] - range[4]) == 0))
								{
									float[] vector = new float[(range[1] - range[0] + 1) * (range[3] - range[2] + 1) * (range[5] - range[4] + 1)];
									int cc = 0;
									for (int i = 0; i < cube.GetLength(0); i++)
										for (int j = 0; j < cube.GetLength(1); j++)
											for (int k = 0; k < cube.GetLength(2); k++)
												vector[cc++] = cube[i, j, k];

									naxisn = new int[1] { vector.Length };
									return vector;
								}
								else//must be 2d if gotten here
								{
									float[,] table = null;
									if ((range[1] - range[0]) == 0)
									{
										table = new float[(range[3] - range[2] + 1), (range[5] - range[4] + 1)];
										for (int i = 0; i < table.GetLength(0); i++)
											for (int j = 0; j < table.GetLength(1); j++)
												table[i, j] = cube[0, i, j];
									}
									else if ((range[3] - range[2]) == 0)
									{
										table = new float[(range[1] - range[0] + 1), (range[5] - range[4] + 1)];
										for (int i = 0; i < table.GetLength(0); i++)
											for (int j = 0; j < table.GetLength(1); j++)
												table[i, j] = cube[i, 0, j];
									}
									else if ((range[5] - range[4]) == 0)
									{
										table = new float[(range[1] - range[0] + 1), (range[3] - range[2] + 1)];
										for (int i = 0; i < table.GetLength(0); i++)
											for (int j = 0; j < table.GetLength(1); j++)
												table[i, j] = cube[i, j, 0];
									}

									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };
									return table;
								}
							}

							break;
						}

						case -64:
						{
							double[,,] cube = new double[range[1] - range[0] + 1, range[3] - range[2] + 1, range[5] - range[4] + 1];
							naxisn = new int[3] { cube.GetLength(0), cube.GetLength(1), cube.GetLength(2) };

							Parallel.For(range[4], range[5] + 1, opts, k =>
							{
								int cc = (k * naxis0 * naxis1 + range[2] * naxis1 + range[0]) * 8;
								byte[] bytes = new byte[8];

								for (int j = range[2]; j <= range[3]; j++)
									for (int i = range[0]; i <= range[1]; i++)
									{
										bytes[7] = arr[cc];
										bytes[6] = arr[cc + 1];
										bytes[5] = arr[cc + 2];
										bytes[4] = arr[cc + 3];
										bytes[3] = arr[cc + 4];
										bytes[2] = arr[cc + 5];
										bytes[1] = arr[cc + 6];
										bytes[0] = arr[cc + 7];
										cube[i - range[0], j - range[2], k - range[4]] = BitConverter.ToDouble(bytes, 0) * bscale + bzero;
										cc += 8;
									}
							});

							if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 && (range[5] - range[4]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)
								return cube;
							else if (returnRankFormat == RankFormat.ArrayAsRangeRank)
							{
								//check if vector
								if (((range[1] - range[0]) == 0 && (range[3] - range[2]) == 0) || ((range[1] - range[0]) == 0 && (range[5] - range[4]) == 0) || ((range[3] - range[2]) == 0 && (range[5] - range[4]) == 0))
								{
									double[] vector = new double[(range[1] - range[0] + 1) * (range[3] - range[2] + 1) * (range[5] - range[4] + 1)];
									int cc = 0;
									for (int i = 0; i < cube.GetLength(0); i++)
										for (int j = 0; j < cube.GetLength(1); j++)
											for (int k = 0; k < cube.GetLength(2); k++)
												vector[cc++] = cube[i, j, k];

									naxisn = new int[1] { vector.Length };
									return vector;
								}
								else//must be 2d if gotten here
								{
									double[,] table = null;
									if ((range[1] - range[0]) == 0)
									{
										table = new double[(range[3] - range[2] + 1), (range[5] - range[4] + 1)];
										for (int i = 0; i < table.GetLength(0); i++)
											for (int j = 0; j < table.GetLength(1); j++)
												table[i, j] = cube[0, i, j];
									}
									else if ((range[3] - range[2]) == 0)
									{
										table = new double[(range[1] - range[0] + 1), (range[5] - range[4] + 1)];
										for (int i = 0; i < table.GetLength(0); i++)
											for (int j = 0; j < table.GetLength(1); j++)
												table[i, j] = cube[i, 0, j];
									}
									else if ((range[5] - range[4]) == 0)
									{
										table = new double[(range[1] - range[0] + 1), (range[3] - range[2] + 1)];
										for (int i = 0; i < table.GetLength(0); i++)
											for (int j = 0; j < table.GetLength(1); j++)
												table[i, j] = cube[i, j, 0];
									}

									naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };
									return table;
								}
							}

							break;
						}
					}
				}
			}
			
			else if (returnPrecision == ReadReturnPrecision.Boolean)
			{
				if (bitpix != 8 && bzero != 0)//unsigned byte
					throw new Exception("Boolean data must be unsigned bytes on disk.");

				if (naxisn.Length == 1)//then a vector return
				{
					if (returnRankFormat == RankFormat.NAXIS)
					{
						bool[] vector = new bool[range[1] - range[0] + 1];
						naxisn = new int[1] { vector.Length };

						Parallel.For(range[0], range[1] + 1, opts, i =>
						{
							if (arr[i] == 1)
								vector[i - range[0]] = true;
						});
						return vector;
					}
					else if (returnRankFormat == RankFormat.VectorAsHorizontalTable)
					{
						bool[,] table = new bool[range[1] - range[0] + 1, 1];
						naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

						Parallel.For(range[0], range[1] + 1, opts, i =>
						{
							if (arr[i] == 1)
								table[i - range[0], 0] = true;
						});
						return table;
					}
					else if (returnRankFormat == RankFormat.VectorAsVerticalTable)
					{
						bool[,] table = new bool[1, range[1] - range[0] + 1];
						naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

						Parallel.For(range[0], range[1] + 1, opts, i =>
						{
							if (arr[i] == 1)
								table[0, i - range[0]] = true;
						});
						return table;
					}
				}

				else if (naxisn.Length == 2)//then a table or image return
				{
					int naxis0 = naxisn[0];

					bool[,] table = new bool[range[1] - range[0] + 1, range[3] - range[2] + 1];
					naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };

					Parallel.For(range[2], range[3] + 1, opts, j =>
					{
						int cc = j * naxis0 + range[0];
						for (int i = range[0]; i <= range[1]; i++)
							if (arr[cc + i] == 1)
								table[i - range[0], j - range[2]] = true;
					});

					if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)//must be a table
						return table;
					else if (returnRankFormat == RankFormat.ArrayAsRangeRank)//must be a vector if gotten to here
					{
						bool[] vector = new bool[(range[1] - range[0] + 1) * (range[3] - range[2] + 1)];
						System.Buffer.BlockCopy(table, 0, vector, 0, vector.Length * 1);
						naxisn = new int[1] { vector.Length };
						return vector;
					}
				}

				else if (naxisn.Length == 3)//then a data cube
				{
					int naxis0 = naxisn[0], naxis1 = naxisn[1];

					bool[,,] cube = new bool[range[1] - range[0] + 1, range[3] - range[2] + 1, range[5] - range[4] + 1];
					naxisn = new int[3] { cube.GetLength(0), cube.GetLength(1), cube.GetLength(2) };

					Parallel.For(range[4], range[5] + 1, opts, k =>
					{
						int cc = k * naxis0 * naxis1 + range[2] * naxis1 + range[0];

						for (int j = range[2]; j <= range[3]; j++)
							for (int i = range[0]; i <= range[1]; i++)
								if (arr[cc + i] == 1)
									cube[i - range[0], j - range[2], k - range[4]] = true;
					});

					if ((range[1] - range[0]) > 0 && (range[3] - range[2]) > 0 && (range[5] - range[4]) > 0 || returnRankFormat == RankFormat.NAXIS || returnRankFormat == RankFormat.VectorAsVerticalTable || returnRankFormat == RankFormat.VectorAsHorizontalTable)
						return cube;
					else if (returnRankFormat == RankFormat.ArrayAsRangeRank)
					{
						//check if vector
						if (((range[1] - range[0]) == 0 && (range[3] - range[2]) == 0) || ((range[1] - range[0]) == 0 && (range[5] - range[4]) == 0) || ((range[3] - range[2]) == 0 && (range[5] - range[4]) == 0))
						{
							bool[] vector = new bool[(range[1] - range[0] + 1) * (range[3] - range[2] + 1) * (range[5] - range[4] + 1)];
							int cc = 0;
							for (int i = 0; i < cube.GetLength(0); i++)
								for (int j = 0; j < cube.GetLength(1); j++)
									for (int k = 0; k < cube.GetLength(2); k++)
										vector[cc++] = cube[i, j, k];

							naxisn = new int[1] { vector.Length };
							return vector;
						}
						else//must be 2d if gotten here
						{
							bool[,] table = null;
							if ((range[1] - range[0]) == 0)
							{
								table = new bool[(range[3] - range[2] + 1), (range[5] - range[4] + 1)];
								for (int i = 0; i < table.GetLength(0); i++)
									for (int j = 0; j < table.GetLength(1); j++)
										table[i, j] = cube[0, i, j];
							}
							else if ((range[3] - range[2]) == 0)
							{
								table = new bool[(range[1] - range[0] + 1), (range[5] - range[4] + 1)];
								for (int i = 0; i < table.GetLength(0); i++)
									for (int j = 0; j < table.GetLength(1); j++)
										table[i, j] = cube[i, 0, j];
							}
							else if ((range[5] - range[4]) == 0)
							{
								table = new bool[(range[1] - range[0] + 1), (range[3] - range[2] + 1)];
								for (int i = 0; i < table.GetLength(0); i++)
									for (int j = 0; j < table.GetLength(1); j++)
										table[i, j] = cube[i, j, 0];
							}

							naxisn = new int[2] { table.GetLength(0), table.GetLength(1) };
							return table;
						}
					}
				}
			}

			throw new Exception("Error in FITSFILEOPS.ReadImageDataUnit - made it to end without returning data.");
		}

		/// <summary>Returns a data unit as a byte array formatted at a specified precision. Useful for writing.</summary>
		/// <param name="formatPrecision">The precision at which to format the byte array of the underlying data unit. If values of the data unit exceed the precision, the values are clipped.</param>
		/// <param name="doParallel">Populate the byte array with parallelism over the data unit. Can speed things up when the data unit is very large.</param>
		/// <param name="dataUnit">The data unit of up to rank three (cube). Higher dimensional data than rank = 3 not supported.</param>
		public static byte[] GetByteFormattedImageDataUnit(DiskPrecision formatPrecision, bool doParallel, Array? dataUnit)
		{
			if (dataUnit == null)
				return new byte[] { };

			if (dataUnit.Rank > 3)
				throw new Exception("Error: I can only handle up to 3-dimensional data units - SORRY!");

			TypeCode dataUnitType = Type.GetTypeCode(dataUnit.GetType().GetElementType());

			GetBitpixNaxisnBscaleBzero(formatPrecision, dataUnit, out int bitpix, out int[] naxisn, out double bscale, out double bzero);

			long Ndatabytes = ((long)dataUnit.Length) * ((long)Math.Abs(bitpix) / 8);
			int NBlocks = (int)(Math.Ceiling((double)Ndatabytes / 2880.0));
			int NBytesTot = NBlocks * 2880;
			byte[] bytearray = new byte[NBytesTot];

			ParallelOptions opts = new ParallelOptions();
			if (doParallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			switch (dataUnitType)
			{
				case TypeCode.Double:
				{
					switch (formatPrecision)
					{
						case DiskPrecision.Boolean:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									if (((double[])dataUnit)[i] == 1)
										bytearray[i] = 1;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									for (int i = 0; i < naxisn[0]; i++)
									{
										if (((double[,])dataUnit)[i, j] == 1)
											bytearray[cc] = 1;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
							{
								Parallel.For(0, naxisn[2], opts, k =>
								{
									int cc = k * naxisn[0] * naxisn[1];
									for (int j = 0; j < naxisn[1]; j++)
									{
										cc += j * naxisn[0];
										for (int i = 0; i < naxisn[0]; i++)
										{
											if (((double[,,])dataUnit)[i, j, k] == 1) ;
											bytearray[cc] = 1;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Byte:
						case DiskPrecision.SByte:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									byte val = (byte)((((double[])dataUnit)[i] - bzero) / bscale);
									bytearray[i] = val;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									byte val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (byte)((((double[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = val;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (byte)((((double[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = val;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt16:
						case DiskPrecision.Int16:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 2;
									short val = (short)((((double[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 1] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 2;
									short val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (short)((((double[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 1] = (byte)(val & 0xff);
										cc += 2;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (short)((((double[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 1] = (byte)(val & 0xff);
											cc += 2;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt32:
						case DiskPrecision.Int32:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									int val = (int)((((double[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 3] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									int val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (int)((((double[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 3] = (byte)(val & 0xff);
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (int)((((double[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 3] = (byte)(val & 0xff);
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt64:
						case DiskPrecision.Int64:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									long val = (long)((((double[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 56) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
									bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
									bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 7] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									long val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (long)((((double[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 56) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
										bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
										bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 7] = (byte)(val & 0xff);
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (long)((((double[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 56) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
											bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
											bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 7] = (byte)(val & 0xff);
											cc += 8;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Single:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									byte[] sng = BitConverter.GetBytes((float)((((double[])dataUnit)[i] - bzero) / bscale));
									bytearray[cc] = sng[3];
									bytearray[cc + 1] = sng[2];
									bytearray[cc + 2] = sng[1];
									bytearray[cc + 3] = sng[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									byte[] sng = new byte[4];
									for (int i = 0; i < naxisn[0]; i++)
									{
										sng = BitConverter.GetBytes((float)((((double[,])dataUnit)[i, j] - bzero) / bscale));
										bytearray[cc] = sng[3];
										bytearray[cc + 1] = sng[2];
										bytearray[cc + 2] = sng[1];
										bytearray[cc + 3] = sng[0];
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											sng = BitConverter.GetBytes((float)((((double[,,])dataUnit)[i, j, k] - bzero) / bscale));
											bytearray[cc] = sng[3];
											bytearray[cc + 1] = sng[2];
											bytearray[cc + 2] = sng[1];
											bytearray[cc + 3] = sng[0];
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Double:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									byte[] dbl = BitConverter.GetBytes((((double[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = dbl[7];
									bytearray[cc + 1] = dbl[6];
									bytearray[cc + 2] = dbl[5];
									bytearray[cc + 3] = dbl[4];
									bytearray[cc + 4] = dbl[3];
									bytearray[cc + 5] = dbl[2];
									bytearray[cc + 6] = dbl[1];
									bytearray[cc + 7] = dbl[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									byte[] dbl = new byte[8];
									for (int i = 0; i < naxisn[0]; i++)
									{
										dbl = BitConverter.GetBytes((((double[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = dbl[7];
										bytearray[cc + 1] = dbl[6];
										bytearray[cc + 2] = dbl[5];
										bytearray[cc + 3] = dbl[4];
										bytearray[cc + 4] = dbl[3];
										bytearray[cc + 5] = dbl[2];
										bytearray[cc + 6] = dbl[1];
										bytearray[cc + 7] = dbl[0];
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											dbl = BitConverter.GetBytes((((double[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = dbl[7];
											bytearray[cc + 1] = dbl[6];
											bytearray[cc + 2] = dbl[5];
											bytearray[cc + 3] = dbl[4];
											bytearray[cc + 4] = dbl[3];
											bytearray[cc + 5] = dbl[2];
											bytearray[cc + 6] = dbl[1];
											bytearray[cc + 7] = dbl[0];
											cc += 8;
										}
									}
								});
							}
							break;
						}
					}
					break;
				}

				case TypeCode.Single:
				{
					switch (formatPrecision)
					{
						case DiskPrecision.Boolean:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									if (((float[])dataUnit)[i] == 1)
										bytearray[i] = 1;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									for (int i = 0; i < naxisn[0]; i++)
									{
										if (((float[,])dataUnit)[i, j] == 1)
											bytearray[cc] = 1;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
							{
								Parallel.For(0, naxisn[2], opts, k =>
								{
									int cc = k * naxisn[0] * naxisn[1];
									for (int j = 0; j < naxisn[1]; j++)
									{
										cc += j * naxisn[0];
										for (int i = 0; i < naxisn[0]; i++)
										{
											if (((float[,,])dataUnit)[i, j, k] == 1) ;
											bytearray[cc] = 1;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Byte:
						case DiskPrecision.SByte:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									byte val = (byte)((((float[])dataUnit)[i] - bzero) / bscale);
									bytearray[i] = val;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									byte val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (byte)((((float[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = val;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (byte)((((float[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = val;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt16:
						case DiskPrecision.Int16:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 2;
									short val = (short)((((float[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 1] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 2;
									short val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (short)((((float[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 1] = (byte)(val & 0xff);
										cc += 2;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (short)((((float[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 1] = (byte)(val & 0xff);
											cc += 2;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt32:
						case DiskPrecision.Int32:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									int val = (int)((((float[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 3] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									int val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (int)((((float[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 3] = (byte)(val & 0xff);
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (int)((((float[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 3] = (byte)(val & 0xff);
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt64:
						case DiskPrecision.Int64:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									long val = (long)((((float[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 56) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
									bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
									bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 7] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									long val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (long)((((float[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 56) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
										bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
										bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 7] = (byte)(val & 0xff);
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (long)((((float[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 56) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
											bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
											bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 7] = (byte)(val & 0xff);
											cc += 8;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Single:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									byte[] sng = BitConverter.GetBytes((float)((((float[])dataUnit)[i] - bzero) / bscale));
									bytearray[cc] = sng[3];
									bytearray[cc + 1] = sng[2];
									bytearray[cc + 2] = sng[1];
									bytearray[cc + 3] = sng[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									byte[] sng = new byte[4];
									for (int i = 0; i < naxisn[0]; i++)
									{
										sng = BitConverter.GetBytes((float)((((float[,])dataUnit)[i, j] - bzero) / bscale));
										bytearray[cc] = sng[3];
										bytearray[cc + 1] = sng[2];
										bytearray[cc + 2] = sng[1];
										bytearray[cc + 3] = sng[0];
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											sng = BitConverter.GetBytes((float)((((float[,,])dataUnit)[i, j, k] - bzero) / bscale));
											bytearray[cc] = sng[3];
											bytearray[cc + 1] = sng[2];
											bytearray[cc + 2] = sng[1];
											bytearray[cc + 3] = sng[0];
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Double:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									byte[] dbl = BitConverter.GetBytes((((float[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = dbl[7];
									bytearray[cc + 1] = dbl[6];
									bytearray[cc + 2] = dbl[5];
									bytearray[cc + 3] = dbl[4];
									bytearray[cc + 4] = dbl[3];
									bytearray[cc + 5] = dbl[2];
									bytearray[cc + 6] = dbl[1];
									bytearray[cc + 7] = dbl[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									byte[] dbl = new byte[8];
									for (int i = 0; i < naxisn[0]; i++)
									{
										dbl = BitConverter.GetBytes((((float[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = dbl[7];
										bytearray[cc + 1] = dbl[6];
										bytearray[cc + 2] = dbl[5];
										bytearray[cc + 3] = dbl[4];
										bytearray[cc + 4] = dbl[3];
										bytearray[cc + 5] = dbl[2];
										bytearray[cc + 6] = dbl[1];
										bytearray[cc + 7] = dbl[0];
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											dbl = BitConverter.GetBytes((((float[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = dbl[7];
											bytearray[cc + 1] = dbl[6];
											bytearray[cc + 2] = dbl[5];
											bytearray[cc + 3] = dbl[4];
											bytearray[cc + 4] = dbl[3];
											bytearray[cc + 5] = dbl[2];
											bytearray[cc + 6] = dbl[1];
											bytearray[cc + 7] = dbl[0];
											cc += 8;
										}
									}
								});
							}
							break;
						}
					}
					break;
				}

				case TypeCode.UInt64:
				{
					switch (formatPrecision)
					{
						case DiskPrecision.Boolean:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									if (((ulong[])dataUnit)[i] == 1)
										bytearray[i] = 1;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									for (int i = 0; i < naxisn[0]; i++)
									{
										if (((ulong[,])dataUnit)[i, j] == 1)
											bytearray[cc] = 1;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
							{
								Parallel.For(0, naxisn[2], opts, k =>
								{
									int cc = k * naxisn[0] * naxisn[1];
									for (int j = 0; j < naxisn[1]; j++)
									{
										cc += j * naxisn[0];
										for (int i = 0; i < naxisn[0]; i++)
										{
											if (((ulong[,,])dataUnit)[i, j, k] == 1) ;
											bytearray[cc] = 1;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Byte:
						case DiskPrecision.SByte:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									byte val = (byte)((((ulong[])dataUnit)[i] - bzero) / bscale);
									bytearray[i] = val;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									byte val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (byte)((((ulong[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = val;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (byte)((((ulong[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = val;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt16:
						case DiskPrecision.Int16:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 2;
									short val = (short)((((ulong[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 1] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 2;
									short val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (short)((((ulong[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 1] = (byte)(val & 0xff);
										cc += 2;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (short)((((ulong[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 1] = (byte)(val & 0xff);
											cc += 2;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt32:
						case DiskPrecision.Int32:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									int val = (int)((((ulong[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 3] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									int val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (int)((((ulong[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 3] = (byte)(val & 0xff);
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (int)((((ulong[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 3] = (byte)(val & 0xff);
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt64:
						case DiskPrecision.Int64:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									long val = (long)((((ulong[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 56) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
									bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
									bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 7] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									long val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (long)((((ulong[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 56) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
										bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
										bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 7] = (byte)(val & 0xff);
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (long)((((ulong[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 56) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
											bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
											bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 7] = (byte)(val & 0xff);
											cc += 8;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Single:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									byte[] sng = BitConverter.GetBytes((float)((((ulong[])dataUnit)[i] - bzero) / bscale));
									bytearray[cc] = sng[3];
									bytearray[cc + 1] = sng[2];
									bytearray[cc + 2] = sng[1];
									bytearray[cc + 3] = sng[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									byte[] sng = new byte[4];
									for (int i = 0; i < naxisn[0]; i++)
									{
										sng = BitConverter.GetBytes((float)((((ulong[,])dataUnit)[i, j] - bzero) / bscale));
										bytearray[cc] = sng[3];
										bytearray[cc + 1] = sng[2];
										bytearray[cc + 2] = sng[1];
										bytearray[cc + 3] = sng[0];
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											sng = BitConverter.GetBytes((float)((((ulong[,,])dataUnit)[i, j, k] - bzero) / bscale));
											bytearray[cc] = sng[3];
											bytearray[cc + 1] = sng[2];
											bytearray[cc + 2] = sng[1];
											bytearray[cc + 3] = sng[0];
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Double:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									byte[] dbl = BitConverter.GetBytes((((ulong[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = dbl[7];
									bytearray[cc + 1] = dbl[6];
									bytearray[cc + 2] = dbl[5];
									bytearray[cc + 3] = dbl[4];
									bytearray[cc + 4] = dbl[3];
									bytearray[cc + 5] = dbl[2];
									bytearray[cc + 6] = dbl[1];
									bytearray[cc + 7] = dbl[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									byte[] dbl = new byte[8];
									for (int i = 0; i < naxisn[0]; i++)
									{
										dbl = BitConverter.GetBytes((((ulong[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = dbl[7];
										bytearray[cc + 1] = dbl[6];
										bytearray[cc + 2] = dbl[5];
										bytearray[cc + 3] = dbl[4];
										bytearray[cc + 4] = dbl[3];
										bytearray[cc + 5] = dbl[2];
										bytearray[cc + 6] = dbl[1];
										bytearray[cc + 7] = dbl[0];
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											dbl = BitConverter.GetBytes((((ulong[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = dbl[7];
											bytearray[cc + 1] = dbl[6];
											bytearray[cc + 2] = dbl[5];
											bytearray[cc + 3] = dbl[4];
											bytearray[cc + 4] = dbl[3];
											bytearray[cc + 5] = dbl[2];
											bytearray[cc + 6] = dbl[1];
											bytearray[cc + 7] = dbl[0];
											cc += 8;
										}
									}
								});
							}
							break;
						}
					}
					break;
				}

				case TypeCode.Int64:
				{
					switch (formatPrecision)
					{
						case DiskPrecision.Boolean:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									if (((long[])dataUnit)[i] == 1)
										bytearray[i] = 1;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									for (int i = 0; i < naxisn[0]; i++)
									{
										if (((long[,])dataUnit)[i, j] == 1)
											bytearray[cc] = 1;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
							{
								Parallel.For(0, naxisn[2], opts, k =>
								{
									int cc = k * naxisn[0] * naxisn[1];
									for (int j = 0; j < naxisn[1]; j++)
									{
										cc += j * naxisn[0];
										for (int i = 0; i < naxisn[0]; i++)
										{
											if (((long[,,])dataUnit)[i, j, k] == 1) ;
											bytearray[cc] = 1;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Byte:
						case DiskPrecision.SByte:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									byte val = (byte)((((long[])dataUnit)[i] - bzero) / bscale);
									bytearray[i] = val;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									byte val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (byte)((((long[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = val;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (byte)((((long[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = val;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt16:
						case DiskPrecision.Int16:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 2;
									short val = (short)((((long[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 1] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 2;
									short val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (short)((((long[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 1] = (byte)(val & 0xff);
										cc += 2;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (short)((((long[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 1] = (byte)(val & 0xff);
											cc += 2;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt32:
						case DiskPrecision.Int32:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									int val = (int)((((long[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 3] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									int val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (int)((((long[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 3] = (byte)(val & 0xff);
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (int)((((long[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 3] = (byte)(val & 0xff);
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt64:
						case DiskPrecision.Int64:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									long val = (long)((((long[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 56) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
									bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
									bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 7] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									long val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (long)((((long[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 56) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
										bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
										bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 7] = (byte)(val & 0xff);
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (long)((((long[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 56) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
											bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
											bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 7] = (byte)(val & 0xff);
											cc += 8;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Single:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									byte[] sng = BitConverter.GetBytes((float)((((long[])dataUnit)[i] - bzero) / bscale));
									bytearray[cc] = sng[3];
									bytearray[cc + 1] = sng[2];
									bytearray[cc + 2] = sng[1];
									bytearray[cc + 3] = sng[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									byte[] sng = new byte[4];
									for (int i = 0; i < naxisn[0]; i++)
									{
										sng = BitConverter.GetBytes((float)((((long[,])dataUnit)[i, j] - bzero) / bscale));
										bytearray[cc] = sng[3];
										bytearray[cc + 1] = sng[2];
										bytearray[cc + 2] = sng[1];
										bytearray[cc + 3] = sng[0];
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											sng = BitConverter.GetBytes((float)((((long[,,])dataUnit)[i, j, k] - bzero) / bscale));
											bytearray[cc] = sng[3];
											bytearray[cc + 1] = sng[2];
											bytearray[cc + 2] = sng[1];
											bytearray[cc + 3] = sng[0];
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Double:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									byte[] dbl = BitConverter.GetBytes((((long[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = dbl[7];
									bytearray[cc + 1] = dbl[6];
									bytearray[cc + 2] = dbl[5];
									bytearray[cc + 3] = dbl[4];
									bytearray[cc + 4] = dbl[3];
									bytearray[cc + 5] = dbl[2];
									bytearray[cc + 6] = dbl[1];
									bytearray[cc + 7] = dbl[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									byte[] dbl = new byte[8];
									for (int i = 0; i < naxisn[0]; i++)
									{
										dbl = BitConverter.GetBytes((((long[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = dbl[7];
										bytearray[cc + 1] = dbl[6];
										bytearray[cc + 2] = dbl[5];
										bytearray[cc + 3] = dbl[4];
										bytearray[cc + 4] = dbl[3];
										bytearray[cc + 5] = dbl[2];
										bytearray[cc + 6] = dbl[1];
										bytearray[cc + 7] = dbl[0];
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											dbl = BitConverter.GetBytes((((long[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = dbl[7];
											bytearray[cc + 1] = dbl[6];
											bytearray[cc + 2] = dbl[5];
											bytearray[cc + 3] = dbl[4];
											bytearray[cc + 4] = dbl[3];
											bytearray[cc + 5] = dbl[2];
											bytearray[cc + 6] = dbl[1];
											bytearray[cc + 7] = dbl[0];
											cc += 8;
										}
									}
								});
							}
							break;
						}
					}
					break;
				}

				case TypeCode.UInt32:
				{
					switch (formatPrecision)
					{
						case DiskPrecision.Boolean:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									if (((uint[])dataUnit)[i] == 1)
										bytearray[i] = 1;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									for (int i = 0; i < naxisn[0]; i++)
									{
										if (((uint[,])dataUnit)[i, j] == 1)
											bytearray[cc] = 1;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
							{
								Parallel.For(0, naxisn[2], opts, k =>
								{
									int cc = k * naxisn[0] * naxisn[1];
									for (int j = 0; j < naxisn[1]; j++)
									{
										cc += j * naxisn[0];
										for (int i = 0; i < naxisn[0]; i++)
										{
											if (((uint[,,])dataUnit)[i, j, k] == 1) ;
											bytearray[cc] = 1;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Byte:
						case DiskPrecision.SByte:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									byte val = (byte)((((uint[])dataUnit)[i] - bzero) / bscale);
									bytearray[i] = val;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									byte val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (byte)((((uint[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = val;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (byte)((((uint[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = val;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt16:
						case DiskPrecision.Int16:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 2;
									short val = (short)((((uint[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 1] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 2;
									short val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (short)((((uint[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 1] = (byte)(val & 0xff);
										cc += 2;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (short)((((uint[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 1] = (byte)(val & 0xff);
											cc += 2;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt32:
						case DiskPrecision.Int32:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									int val = (int)((((uint[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 3] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									int val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (int)((((uint[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 3] = (byte)(val & 0xff);
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (int)((((uint[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 3] = (byte)(val & 0xff);
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt64:
						case DiskPrecision.Int64:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									long val = (long)((((uint[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 56) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
									bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
									bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 7] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									long val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (long)((((uint[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 56) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
										bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
										bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 7] = (byte)(val & 0xff);
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (long)((((uint[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 56) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
											bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
											bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 7] = (byte)(val & 0xff);
											cc += 8;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Single:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									byte[] sng = BitConverter.GetBytes((float)((((uint[])dataUnit)[i] - bzero) / bscale));
									bytearray[cc] = sng[3];
									bytearray[cc + 1] = sng[2];
									bytearray[cc + 2] = sng[1];
									bytearray[cc + 3] = sng[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									byte[] sng = new byte[4];
									for (int i = 0; i < naxisn[0]; i++)
									{
										sng = BitConverter.GetBytes((float)((((uint[,])dataUnit)[i, j] - bzero) / bscale));
										bytearray[cc] = sng[3];
										bytearray[cc + 1] = sng[2];
										bytearray[cc + 2] = sng[1];
										bytearray[cc + 3] = sng[0];
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											sng = BitConverter.GetBytes((float)((((uint[,,])dataUnit)[i, j, k] - bzero) / bscale));
											bytearray[cc] = sng[3];
											bytearray[cc + 1] = sng[2];
											bytearray[cc + 2] = sng[1];
											bytearray[cc + 3] = sng[0];
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Double:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									byte[] dbl = BitConverter.GetBytes((((uint[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = dbl[7];
									bytearray[cc + 1] = dbl[6];
									bytearray[cc + 2] = dbl[5];
									bytearray[cc + 3] = dbl[4];
									bytearray[cc + 4] = dbl[3];
									bytearray[cc + 5] = dbl[2];
									bytearray[cc + 6] = dbl[1];
									bytearray[cc + 7] = dbl[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									byte[] dbl = new byte[8];
									for (int i = 0; i < naxisn[0]; i++)
									{
										dbl = BitConverter.GetBytes((((uint[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = dbl[7];
										bytearray[cc + 1] = dbl[6];
										bytearray[cc + 2] = dbl[5];
										bytearray[cc + 3] = dbl[4];
										bytearray[cc + 4] = dbl[3];
										bytearray[cc + 5] = dbl[2];
										bytearray[cc + 6] = dbl[1];
										bytearray[cc + 7] = dbl[0];
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											dbl = BitConverter.GetBytes((((uint[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = dbl[7];
											bytearray[cc + 1] = dbl[6];
											bytearray[cc + 2] = dbl[5];
											bytearray[cc + 3] = dbl[4];
											bytearray[cc + 4] = dbl[3];
											bytearray[cc + 5] = dbl[2];
											bytearray[cc + 6] = dbl[1];
											bytearray[cc + 7] = dbl[0];
											cc += 8;
										}
									}
								});
							}
							break;
						}
					}
					break;
				}

				case TypeCode.Int32:
				{
					switch (formatPrecision)
					{
						case DiskPrecision.Boolean:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									if (((int[])dataUnit)[i] == 1)
										bytearray[i] = 1;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									for (int i = 0; i < naxisn[0]; i++)
									{
										if (((int[,])dataUnit)[i, j] == 1)
											bytearray[cc] = 1;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
							{
								Parallel.For(0, naxisn[2], opts, k =>
								{
									int cc = k * naxisn[0] * naxisn[1];
									for (int j = 0; j < naxisn[1]; j++)
									{
										cc += j * naxisn[0];
										for (int i = 0; i < naxisn[0]; i++)
										{
											if (((int[,,])dataUnit)[i, j, k] == 1) ;
											bytearray[cc] = 1;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Byte:
						case DiskPrecision.SByte:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									byte val = (byte)((((int[])dataUnit)[i] - bzero) / bscale);
									bytearray[i] = val;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									byte val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (byte)((((int[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = val;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (byte)((((int[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = val;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt16:
						case DiskPrecision.Int16:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 2;
									short val = (short)((((int[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 1] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 2;
									short val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (short)((((int[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 1] = (byte)(val & 0xff);
										cc += 2;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (short)((((int[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 1] = (byte)(val & 0xff);
											cc += 2;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt32:
						case DiskPrecision.Int32:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									int val = (int)((((int[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 3] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									int val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (int)((((int[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 3] = (byte)(val & 0xff);
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (int)((((int[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 3] = (byte)(val & 0xff);
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt64:
						case DiskPrecision.Int64:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									long val = (long)((((int[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 56) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
									bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
									bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 7] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									long val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (long)((((int[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 56) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
										bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
										bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 7] = (byte)(val & 0xff);
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (long)((((int[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 56) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
											bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
											bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 7] = (byte)(val & 0xff);
											cc += 8;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Single:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									byte[] sng = BitConverter.GetBytes((float)((((int[])dataUnit)[i] - bzero) / bscale));
									bytearray[cc] = sng[3];
									bytearray[cc + 1] = sng[2];
									bytearray[cc + 2] = sng[1];
									bytearray[cc + 3] = sng[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									byte[] sng = new byte[4];
									for (int i = 0; i < naxisn[0]; i++)
									{
										sng = BitConverter.GetBytes((float)((((int[,])dataUnit)[i, j] - bzero) / bscale));
										bytearray[cc] = sng[3];
										bytearray[cc + 1] = sng[2];
										bytearray[cc + 2] = sng[1];
										bytearray[cc + 3] = sng[0];
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											sng = BitConverter.GetBytes((float)((((int[,,])dataUnit)[i, j, k] - bzero) / bscale));
											bytearray[cc] = sng[3];
											bytearray[cc + 1] = sng[2];
											bytearray[cc + 2] = sng[1];
											bytearray[cc + 3] = sng[0];
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Double:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									byte[] dbl = BitConverter.GetBytes((((int[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = dbl[7];
									bytearray[cc + 1] = dbl[6];
									bytearray[cc + 2] = dbl[5];
									bytearray[cc + 3] = dbl[4];
									bytearray[cc + 4] = dbl[3];
									bytearray[cc + 5] = dbl[2];
									bytearray[cc + 6] = dbl[1];
									bytearray[cc + 7] = dbl[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									byte[] dbl = new byte[8];
									for (int i = 0; i < naxisn[0]; i++)
									{
										dbl = BitConverter.GetBytes((((int[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = dbl[7];
										bytearray[cc + 1] = dbl[6];
										bytearray[cc + 2] = dbl[5];
										bytearray[cc + 3] = dbl[4];
										bytearray[cc + 4] = dbl[3];
										bytearray[cc + 5] = dbl[2];
										bytearray[cc + 6] = dbl[1];
										bytearray[cc + 7] = dbl[0];
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											dbl = BitConverter.GetBytes((((int[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = dbl[7];
											bytearray[cc + 1] = dbl[6];
											bytearray[cc + 2] = dbl[5];
											bytearray[cc + 3] = dbl[4];
											bytearray[cc + 4] = dbl[3];
											bytearray[cc + 5] = dbl[2];
											bytearray[cc + 6] = dbl[1];
											bytearray[cc + 7] = dbl[0];
											cc += 8;
										}
									}
								});
							}
							break;
						}
					}
					break;
				}

				case TypeCode.UInt16:
				{
					switch (formatPrecision)
					{
						case DiskPrecision.Boolean:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									if (((ushort[])dataUnit)[i] == 1)
										bytearray[i] = 1;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									for (int i = 0; i < naxisn[0]; i++)
									{
										if (((ushort[,])dataUnit)[i, j] == 1)
											bytearray[cc] = 1;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
							{
								Parallel.For(0, naxisn[2], opts, k =>
								{
									int cc = k * naxisn[0] * naxisn[1];
									for (int j = 0; j < naxisn[1]; j++)
									{
										cc += j * naxisn[0];
										for (int i = 0; i < naxisn[0]; i++)
										{
											if (((ushort[,,])dataUnit)[i, j, k] == 1) ;
											bytearray[cc] = 1;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Byte:
						case DiskPrecision.SByte:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									byte val = (byte)((((ushort[])dataUnit)[i] - bzero) / bscale);
									bytearray[i] = val;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									byte val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (byte)((((ushort[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = val;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (byte)((((ushort[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = val;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt16:
						case DiskPrecision.Int16:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 2;
									short val = (short)((((ushort[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 1] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 2;
									short val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (short)((((ushort[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 1] = (byte)(val & 0xff);
										cc += 2;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (short)((((ushort[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 1] = (byte)(val & 0xff);
											cc += 2;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt32:
						case DiskPrecision.Int32:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									int val = (int)((((ushort[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 3] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									int val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (int)((((ushort[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 3] = (byte)(val & 0xff);
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (int)((((ushort[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 3] = (byte)(val & 0xff);
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt64:
						case DiskPrecision.Int64:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									long val = (long)((((ushort[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 56) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
									bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
									bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 7] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									long val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (long)((((ushort[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 56) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
										bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
										bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 7] = (byte)(val & 0xff);
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (long)((((ushort[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 56) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
											bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
											bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 7] = (byte)(val & 0xff);
											cc += 8;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Single:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									byte[] sng = BitConverter.GetBytes((float)((((ushort[])dataUnit)[i] - bzero) / bscale));
									bytearray[cc] = sng[3];
									bytearray[cc + 1] = sng[2];
									bytearray[cc + 2] = sng[1];
									bytearray[cc + 3] = sng[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									byte[] sng = new byte[4];
									for (int i = 0; i < naxisn[0]; i++)
									{
										sng = BitConverter.GetBytes((float)((((ushort[,])dataUnit)[i, j] - bzero) / bscale));
										bytearray[cc] = sng[3];
										bytearray[cc + 1] = sng[2];
										bytearray[cc + 2] = sng[1];
										bytearray[cc + 3] = sng[0];
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											sng = BitConverter.GetBytes((float)((((ushort[,,])dataUnit)[i, j, k] - bzero) / bscale));
											bytearray[cc] = sng[3];
											bytearray[cc + 1] = sng[2];
											bytearray[cc + 2] = sng[1];
											bytearray[cc + 3] = sng[0];
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Double:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									byte[] dbl = BitConverter.GetBytes((((ushort[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = dbl[7];
									bytearray[cc + 1] = dbl[6];
									bytearray[cc + 2] = dbl[5];
									bytearray[cc + 3] = dbl[4];
									bytearray[cc + 4] = dbl[3];
									bytearray[cc + 5] = dbl[2];
									bytearray[cc + 6] = dbl[1];
									bytearray[cc + 7] = dbl[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									byte[] dbl = new byte[8];
									for (int i = 0; i < naxisn[0]; i++)
									{
										dbl = BitConverter.GetBytes((((ushort[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = dbl[7];
										bytearray[cc + 1] = dbl[6];
										bytearray[cc + 2] = dbl[5];
										bytearray[cc + 3] = dbl[4];
										bytearray[cc + 4] = dbl[3];
										bytearray[cc + 5] = dbl[2];
										bytearray[cc + 6] = dbl[1];
										bytearray[cc + 7] = dbl[0];
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											dbl = BitConverter.GetBytes((((ushort[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = dbl[7];
											bytearray[cc + 1] = dbl[6];
											bytearray[cc + 2] = dbl[5];
											bytearray[cc + 3] = dbl[4];
											bytearray[cc + 4] = dbl[3];
											bytearray[cc + 5] = dbl[2];
											bytearray[cc + 6] = dbl[1];
											bytearray[cc + 7] = dbl[0];
											cc += 8;
										}
									}
								});
							}
							break;
						}
					}
					break;
				}

				case TypeCode.Int16:
				{
					switch (formatPrecision)
					{
						case DiskPrecision.Boolean:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									if (((short[])dataUnit)[i] == 1)
										bytearray[i] = 1;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									for (int i = 0; i < naxisn[0]; i++)
									{
										if (((short[,])dataUnit)[i, j] == 1)
											bytearray[cc] = 1;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
							{
								Parallel.For(0, naxisn[2], opts, k =>
								{
									int cc = k * naxisn[0] * naxisn[1];
									for (int j = 0; j < naxisn[1]; j++)
									{
										cc += j * naxisn[0];
										for (int i = 0; i < naxisn[0]; i++)
										{
											if (((short[,,])dataUnit)[i, j, k] == 1) ;
											bytearray[cc] = 1;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Byte:
						case DiskPrecision.SByte:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									byte val = (byte)((((short[])dataUnit)[i] - bzero) / bscale);
									bytearray[i] = val;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									byte val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (byte)((((short[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = val;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (byte)((((short[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = val;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt16:
						case DiskPrecision.Int16:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 2;
									short val = (short)((((short[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 1] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 2;
									short val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (short)((((short[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 1] = (byte)(val & 0xff);
										cc += 2;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (short)((((short[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 1] = (byte)(val & 0xff);
											cc += 2;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt32:
						case DiskPrecision.Int32:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									int val = (int)((((short[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 3] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									int val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (int)((((short[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 3] = (byte)(val & 0xff);
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (int)((((short[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 3] = (byte)(val & 0xff);
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt64:
						case DiskPrecision.Int64:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									long val = (long)((((short[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 56) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
									bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
									bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 7] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									long val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (long)((((short[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 56) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
										bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
										bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 7] = (byte)(val & 0xff);
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (long)((((short[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 56) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
											bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
											bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 7] = (byte)(val & 0xff);
											cc += 8;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Single:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									byte[] sng = BitConverter.GetBytes((float)((((short[])dataUnit)[i] - bzero) / bscale));
									bytearray[cc] = sng[3];
									bytearray[cc + 1] = sng[2];
									bytearray[cc + 2] = sng[1];
									bytearray[cc + 3] = sng[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									byte[] sng = new byte[4];
									for (int i = 0; i < naxisn[0]; i++)
									{
										sng = BitConverter.GetBytes((float)((((short[,])dataUnit)[i, j] - bzero) / bscale));
										bytearray[cc] = sng[3];
										bytearray[cc + 1] = sng[2];
										bytearray[cc + 2] = sng[1];
										bytearray[cc + 3] = sng[0];
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											sng = BitConverter.GetBytes((float)((((short[,,])dataUnit)[i, j, k] - bzero) / bscale));
											bytearray[cc] = sng[3];
											bytearray[cc + 1] = sng[2];
											bytearray[cc + 2] = sng[1];
											bytearray[cc + 3] = sng[0];
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Double:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									byte[] dbl = BitConverter.GetBytes((((short[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = dbl[7];
									bytearray[cc + 1] = dbl[6];
									bytearray[cc + 2] = dbl[5];
									bytearray[cc + 3] = dbl[4];
									bytearray[cc + 4] = dbl[3];
									bytearray[cc + 5] = dbl[2];
									bytearray[cc + 6] = dbl[1];
									bytearray[cc + 7] = dbl[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									byte[] dbl = new byte[8];
									for (int i = 0; i < naxisn[0]; i++)
									{
										dbl = BitConverter.GetBytes((((short[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = dbl[7];
										bytearray[cc + 1] = dbl[6];
										bytearray[cc + 2] = dbl[5];
										bytearray[cc + 3] = dbl[4];
										bytearray[cc + 4] = dbl[3];
										bytearray[cc + 5] = dbl[2];
										bytearray[cc + 6] = dbl[1];
										bytearray[cc + 7] = dbl[0];
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											dbl = BitConverter.GetBytes((((short[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = dbl[7];
											bytearray[cc + 1] = dbl[6];
											bytearray[cc + 2] = dbl[5];
											bytearray[cc + 3] = dbl[4];
											bytearray[cc + 4] = dbl[3];
											bytearray[cc + 5] = dbl[2];
											bytearray[cc + 6] = dbl[1];
											bytearray[cc + 7] = dbl[0];
											cc += 8;
										}
									}
								});
							}
							break;
						}
					}
					break;
				}

				case TypeCode.Byte:
				{
					switch (formatPrecision)
					{
						case DiskPrecision.Boolean:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									if (((byte[])dataUnit)[i] == 1)
										bytearray[i] = 1;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									for (int i = 0; i < naxisn[0]; i++)
									{
										if (((byte[,])dataUnit)[i, j] == 1)
											bytearray[cc] = 1;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
							{
								Parallel.For(0, naxisn[2], opts, k =>
								{
									int cc = k * naxisn[0] * naxisn[1];
									for (int j = 0; j < naxisn[1]; j++)
									{
										cc += j * naxisn[0];
										for (int i = 0; i < naxisn[0]; i++)
										{
											if (((byte[,,])dataUnit)[i, j, k] == 1) ;
											bytearray[cc] = 1;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Byte:
						case DiskPrecision.SByte:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									byte val = (byte)((((byte[])dataUnit)[i] - bzero) / bscale);
									bytearray[i] = val;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									byte val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (byte)((((byte[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = val;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (byte)((((byte[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = val;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt16:
						case DiskPrecision.Int16:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 2;
									short val = (short)((((byte[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 1] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 2;
									short val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (short)((((byte[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 1] = (byte)(val & 0xff);
										cc += 2;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (short)((((byte[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 1] = (byte)(val & 0xff);
											cc += 2;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt32:
						case DiskPrecision.Int32:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									int val = (int)((((byte[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 3] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									int val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (int)((((byte[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 3] = (byte)(val & 0xff);
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (int)((((byte[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 3] = (byte)(val & 0xff);
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt64:
						case DiskPrecision.Int64:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									long val = (long)((((byte[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 56) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
									bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
									bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 7] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									long val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (long)((((byte[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 56) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
										bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
										bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 7] = (byte)(val & 0xff);
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (long)((((byte[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 56) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
											bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
											bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 7] = (byte)(val & 0xff);
											cc += 8;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Single:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									byte[] sng = BitConverter.GetBytes((float)((((byte[])dataUnit)[i] - bzero) / bscale));
									bytearray[cc] = sng[3];
									bytearray[cc + 1] = sng[2];
									bytearray[cc + 2] = sng[1];
									bytearray[cc + 3] = sng[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									byte[] sng = new byte[4];
									for (int i = 0; i < naxisn[0]; i++)
									{
										sng = BitConverter.GetBytes((float)((((byte[,])dataUnit)[i, j] - bzero) / bscale));
										bytearray[cc] = sng[3];
										bytearray[cc + 1] = sng[2];
										bytearray[cc + 2] = sng[1];
										bytearray[cc + 3] = sng[0];
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											sng = BitConverter.GetBytes((float)((((byte[,,])dataUnit)[i, j, k] - bzero) / bscale));
											bytearray[cc] = sng[3];
											bytearray[cc + 1] = sng[2];
											bytearray[cc + 2] = sng[1];
											bytearray[cc + 3] = sng[0];
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Double:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									byte[] dbl = BitConverter.GetBytes((((byte[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = dbl[7];
									bytearray[cc + 1] = dbl[6];
									bytearray[cc + 2] = dbl[5];
									bytearray[cc + 3] = dbl[4];
									bytearray[cc + 4] = dbl[3];
									bytearray[cc + 5] = dbl[2];
									bytearray[cc + 6] = dbl[1];
									bytearray[cc + 7] = dbl[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									byte[] dbl = new byte[8];
									for (int i = 0; i < naxisn[0]; i++)
									{
										dbl = BitConverter.GetBytes((((byte[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = dbl[7];
										bytearray[cc + 1] = dbl[6];
										bytearray[cc + 2] = dbl[5];
										bytearray[cc + 3] = dbl[4];
										bytearray[cc + 4] = dbl[3];
										bytearray[cc + 5] = dbl[2];
										bytearray[cc + 6] = dbl[1];
										bytearray[cc + 7] = dbl[0];
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											dbl = BitConverter.GetBytes((((byte[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = dbl[7];
											bytearray[cc + 1] = dbl[6];
											bytearray[cc + 2] = dbl[5];
											bytearray[cc + 3] = dbl[4];
											bytearray[cc + 4] = dbl[3];
											bytearray[cc + 5] = dbl[2];
											bytearray[cc + 6] = dbl[1];
											bytearray[cc + 7] = dbl[0];
											cc += 8;
										}
									}
								});
							}
							break;
						}
					}
					break;
				}

				case TypeCode.SByte:
				{
					switch (formatPrecision)
					{
						case DiskPrecision.Boolean:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									if (((sbyte[])dataUnit)[i] == 1)
										bytearray[i] = 1;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									for (int i = 0; i < naxisn[0]; i++)
									{
										if (((sbyte[,])dataUnit)[i, j] == 1)
											bytearray[cc] = 1;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
							{
								Parallel.For(0, naxisn[2], opts, k =>
								{
									int cc = k * naxisn[0] * naxisn[1];
									for (int j = 0; j < naxisn[1]; j++)
									{
										cc += j * naxisn[0];
										for (int i = 0; i < naxisn[0]; i++)
										{
											if (((sbyte[,,])dataUnit)[i, j, k] == 1) ;
											bytearray[cc] = 1;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Byte:
						case DiskPrecision.SByte:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									byte val = (byte)((((sbyte[])dataUnit)[i] - bzero) / bscale);
									bytearray[i] = val;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									byte val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (byte)((((sbyte[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = val;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (byte)((((sbyte[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = val;
											cc += 1;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt16:
						case DiskPrecision.Int16:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 2;
									short val = (short)((((sbyte[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 1] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 2;
									short val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (short)((((sbyte[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 1] = (byte)(val & 0xff);
										cc += 2;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (short)((((sbyte[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 1] = (byte)(val & 0xff);
											cc += 2;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt32:
						case DiskPrecision.Int32:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									int val = (int)((((sbyte[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 3] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									int val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (int)((((sbyte[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 3] = (byte)(val & 0xff);
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (int)((((sbyte[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 3] = (byte)(val & 0xff);
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.UInt64:
						case DiskPrecision.Int64:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									long val = (long)((((sbyte[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = (byte)((val >> 56) & 0xff);
									bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
									bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
									bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
									bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
									bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
									bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
									bytearray[cc + 7] = (byte)(val & 0xff);
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									long val;
									for (int i = 0; i < naxisn[0]; i++)
									{
										val = (long)((((sbyte[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = (byte)((val >> 56) & 0xff);
										bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
										bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
										bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
										bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
										bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
										bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
										bytearray[cc + 7] = (byte)(val & 0xff);
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											val = (long)((((sbyte[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = (byte)((val >> 56) & 0xff);
											bytearray[cc + 1] = (byte)((val >> 48) & 0xff);
											bytearray[cc + 2] = (byte)((val >> 40) & 0xff);
											bytearray[cc + 3] = (byte)((val >> 32) & 0xff);
											bytearray[cc + 4] = (byte)((val >> 24) & 0xff);
											bytearray[cc + 5] = (byte)((val >> 16) & 0xff);
											bytearray[cc + 6] = (byte)((val >> 8) & 0xff);
											bytearray[cc + 7] = (byte)(val & 0xff);
											cc += 8;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Single:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 4;
									byte[] sng = BitConverter.GetBytes((float)((((sbyte[])dataUnit)[i] - bzero) / bscale));
									bytearray[cc] = sng[3];
									bytearray[cc + 1] = sng[2];
									bytearray[cc + 2] = sng[1];
									bytearray[cc + 3] = sng[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 4;
									byte[] sng = new byte[4];
									for (int i = 0; i < naxisn[0]; i++)
									{
										sng = BitConverter.GetBytes((float)((((sbyte[,])dataUnit)[i, j] - bzero) / bscale));
										bytearray[cc] = sng[3];
										bytearray[cc + 1] = sng[2];
										bytearray[cc + 2] = sng[1];
										bytearray[cc + 3] = sng[0];
										cc += 4;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											sng = BitConverter.GetBytes((float)((((sbyte[,,])dataUnit)[i, j, k] - bzero) / bscale));
											bytearray[cc] = sng[3];
											bytearray[cc + 1] = sng[2];
											bytearray[cc + 2] = sng[1];
											bytearray[cc + 3] = sng[0];
											cc += 4;
										}
									}
								});
							}
							break;
						}

						case DiskPrecision.Double:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									int cc = i * 8;
									byte[] dbl = BitConverter.GetBytes((((sbyte[])dataUnit)[i] - bzero) / bscale);
									bytearray[cc] = dbl[7];
									bytearray[cc + 1] = dbl[6];
									bytearray[cc + 2] = dbl[5];
									bytearray[cc + 3] = dbl[4];
									bytearray[cc + 4] = dbl[3];
									bytearray[cc + 5] = dbl[2];
									bytearray[cc + 6] = dbl[1];
									bytearray[cc + 7] = dbl[0];
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0] * 8;
									byte[] dbl = new byte[8];
									for (int i = 0; i < naxisn[0]; i++)
									{
										dbl = BitConverter.GetBytes((((sbyte[,])dataUnit)[i, j] - bzero) / bscale);
										bytearray[cc] = dbl[7];
										bytearray[cc + 1] = dbl[6];
										bytearray[cc + 2] = dbl[5];
										bytearray[cc + 3] = dbl[4];
										bytearray[cc + 4] = dbl[3];
										bytearray[cc + 5] = dbl[2];
										bytearray[cc + 6] = dbl[1];
										bytearray[cc + 7] = dbl[0];
										cc += 8;
									}
								});
							}
							else if (dataUnit.Rank == 3)
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
											dbl = BitConverter.GetBytes((((sbyte[,,])dataUnit)[i, j, k] - bzero) / bscale);
											bytearray[cc] = dbl[7];
											bytearray[cc + 1] = dbl[6];
											bytearray[cc + 2] = dbl[5];
											bytearray[cc + 3] = dbl[4];
											bytearray[cc + 4] = dbl[3];
											bytearray[cc + 5] = dbl[2];
											bytearray[cc + 6] = dbl[1];
											bytearray[cc + 7] = dbl[0];
											cc += 8;
										}
									}
								});
							}
							break;
						}
					}
					break;
				}

				case TypeCode.Boolean:
				{
					switch (formatPrecision)
					{
						case DiskPrecision.Boolean:
						{
							if (dataUnit.Rank == 1)
							{
								Parallel.For(0, naxisn[0], opts, i =>
								{
									if (((bool[])dataUnit)[i])
										bytearray[i] = 1;
								});
							}
							else if (dataUnit.Rank == 2)
							{
								Parallel.For(0, naxisn[1], opts, j =>
								{
									int cc = j * naxisn[0];
									for (int i = 0; i < naxisn[0]; i++)
									{
										if (((bool[,])dataUnit)[i, j])
											bytearray[cc] = 1;
										cc += 1;
									}
								});
							}
							else if (dataUnit.Rank == 3)
							{
								Parallel.For(0, naxisn[2], opts, k =>
								{
									int cc = k * naxisn[0] * naxisn[1];
									for (int j = 0; j < naxisn[1]; j++)
									{
										cc += j * naxisn[0];
										for (int i = 0; i < naxisn[0]; i++)
										{
											if (((bool[,,])dataUnit)[i, j, k]) ;
											bytearray[cc] = 1;
											cc += 1;
										}
									}
								});
							}
							break;
						}
					}
					break;
				}

				default:
					throw new Exception("TypeCode '" + formatPrecision + "' not acceptable in GetByteFormattedImageDataUnit for dataUnit type " + dataUnitType + ".");
			}			

			return bytearray;
		}

		/// <summary>Outputs essential header keywords for a given precision and data unit.</summary>
		/// <param name="precision">The intended precision of the data unit.</param>
		/// <param name="dataUnit">The data unit.</param>
		/// <param name="bitpix">BITPIX keyword value.</param>
		/// <param name="naxisn">The length of this array provides the NAXIS keyword value. The elements of this array provide the NAXISn keyword values.</param>
		/// <param name="bscale">BSCALE keyword value.</param>
		/// <param name="bzero">BZERO keyword value.</param>
		public static void GetBitpixNaxisnBscaleBzero(DiskPrecision precision, Array? dataUnit, out int bitpix, out int[] naxisn, out double bscale, out double bzero)
		{
			if (dataUnit == null || dataUnit.Length == 0)
			{
				bitpix = 8;
				naxisn = new int[0];
			}
			else
			{
				naxisn = new int[dataUnit.Rank];
				for (int i = 0; i < naxisn.Length; i++)
					naxisn[i] = dataUnit.GetLength(i);
			}

			switch (precision)
			{
				case DiskPrecision.SByte:
				{
					bitpix = 8;
					bzero = -128;
					bscale = 1;
					break;
				}

				case DiskPrecision.Boolean:
				case DiskPrecision.Byte:
				{
					bitpix = 8;
					bzero = 0;
					bscale = 1;
					break;
				}

				case DiskPrecision.Int16:
				{
					bitpix = 16;
					bzero = 0;
					bscale = 1;
					break;
				}

				case DiskPrecision.UInt16:
				{
					bitpix = 16;
					bzero = 32768;
					bscale = 1;
					break;
				}

				case DiskPrecision.Int32:
				{
					bitpix = 32;
					bzero = 0;
					bscale = 1;
					break;
				}

				case DiskPrecision.UInt32:
				{
					bitpix = 32;
					bzero = 2147483648;
					bscale = 1;
					break;
				}

				case DiskPrecision.Int64:
				{
					bitpix = 64;
					bzero = 0;
					bscale = 1;
					break;
				}

				case DiskPrecision.UInt64:
				{
					bitpix = 64;
					bzero = 9223372036854775808;
					bscale = 1;
					break;
				}

				case DiskPrecision.Single:
				{
					bitpix = -32;
					bzero = 0;
					bscale = 1;
					break;
				}

				case DiskPrecision.Double:
				{
					bitpix = -64;
					bzero = 0;
					bscale = 1;
					break;
				}

				default:
					throw new Exception("TypeCode '" + precision.ToString() + "' not recognized at GetBitpixNaxisnBscaleBzero.");
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
		public static bool SeekExtension(FileStream fs, string extension_type, string extension_name, ref ArrayList? header_return, out long extensionStartPosition, out long extensionEndPosition, out long tableEndPosition, out long pcount, out long theap)
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
		public static bool SeekExtension(FileStream fs, string extension_type, int extension_number, ref ArrayList? header_return, out long extensionStartPosition, out long extensionEndPosition, out long tableEndPosition, out long pcount, out long theap)
		{
			return SeekExtension(fs, extension_type, "_FINDEXTNUM_" + extension_number.ToString(), ref header_return, out extensionStartPosition, out extensionEndPosition, out tableEndPosition, out pcount, out theap);
		}

		/// <summary>Gets all extension names of a specified extension type in the FITS file. If no extensions of the type exist, returns and empty array.</summary>
		/// <param name="FileName">The full file name to read from disk.</param>
		/// <param name="extension_type">The XTENSION extension type, either: &quot;BINTABLE&quot;, &quot;TABLE&quot;, or &quot;IMAGE&quot;.</param>
		public static string[] GetAllExtensionNames(string FileName, string extension_type)
		{
			FileStream fs = new FileStream(FileName, FileMode.Open);
			ArrayList header_return = null;
			if (!FITSFILEOPS.ScanPrimaryUnit(fs, true, ref header_return, out bool hasext) || !hasext)
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

