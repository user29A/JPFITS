using System;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.CompilerServices;
#nullable enable

namespace JPFITS
{
	/// <summary> FITSBinTable class to create, read, interact with, modify components of, and write FITS BINTABLE binary table data extensions.</summary>
	public class FITSBinTable
	{
		#region PRIVATE CLASS MEMBERS

		private int BITPIX = 0, NAXIS = 0, NAXIS1 = 0, NAXIS2 = 0, TFIELDS = 0;
		private string[]? TTYPES;//names of each table entry
		private string[]? TFORMS;//FITS name for the table entry precisions
		private bool[]? TTYPEISCOMPLEX;//for tracking complex singles and doubles
		private bool[]? TTYPEISHEAPARRAYDESC;//for tracking array descriptor entries for heap area data
		private int[][,]? TTYPEHEAPARRAYNELSPOS;//for tracking array descriptor entries for heap area data
		private TypeCode[]? HEAPTCODES;//.NET typcodes for each table entry
		private int[][]? TDIMS;//for tracking multidimensional (rank >= 3) arrays
		private string[]? TUNITS;//FITS name for the table entry units
		private int[]? TBYTES;//number of total bytes for each table entry
		private int[]? TREPEATS;//number of TFORM instances of the table entry
		private TypeCode[]? TCODES;//.NET typcodes for each table entry
		private string[]? HEADER;//header for the table
		private string? FILENAME;
		private string? EXTENSIONNAME;
		private string[]? EXTRAKEYS;
		private string[]? EXTRAKEYVALS;
		private string[]? EXTRAKEYCOMS;
		private byte[]? BINTABLE;//the table in raw byte format read from disk
		private byte[]? HEAPDATA;//the table in raw byte format read from disk

		#region FITS bzero integer mapping

		[MethodImpl(256)]/*256 = agressive inlining*/
		private static long MapUlongToLong(ulong ulongValue)
		{
			return unchecked((long)ulongValue + long.MinValue);
		}

		[MethodImpl(256)]/*256 = agressive inlining*/
		private static int MapUintToInt(uint uintValue)
		{
			return unchecked((int)uintValue + int.MinValue);
		}

		[MethodImpl(256)]/*256 = agressive inlining*/
		private static short MapUshortToShort(ushort ushortValue)
		{
			return unchecked((short)((short)ushortValue + short.MinValue));
		}

		[MethodImpl(256)]/*256 = agressive inlining*/
		private static ulong MapLongToUlong(long longValue)
		{
			return unchecked((ulong)(longValue - long.MinValue));
		}

		[MethodImpl(256)]/*256 = agressive inlining*/
		private static uint MapIntToUint(int intValue)
		{
			return unchecked((uint)(intValue - int.MinValue));
		}

		[MethodImpl(256)]/*256 = agressive inlining*/
		private static ushort MapShortToUshort(short shortValue)
		{
			return unchecked((ushort)(shortValue - short.MinValue));
		}

		#endregion

		private void MAKEBINTABLEBYTEARRAY(Array[] ExtensionEntryData)
		{
			for (int i = 0; i < ExtensionEntryData.Length; i++)
				if (!ExtensionEntryData[i].GetType().IsArray)
					throw new Exception("Error: Object at index '" + i + "' is not an array. Stopping write.");

			MAKEHEAPBYTEARRAY(ExtensionEntryData);//will do nothing if there's no heap data

			int TBytes = NAXIS1 * NAXIS2;
			BINTABLE = new byte[TBytes];			
			bool exception = false;
			TypeCode exceptiontypecode = TypeCode.Empty;

			ParallelOptions opts = new ParallelOptions();
			if (NAXIS2 >= Environment.ProcessorCount)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			//now write the table data into the array
			Parallel.For(0, NAXIS2, opts, (i, loopstate) =>
			{
				if (exception)
					loopstate.Break();

				for (int j = 0; j < ExtensionEntryData.Length; j++)
				{
					int jtps = 0;
					for (int jj = 0; jj < j; jj++)
						jtps += TBYTES[jj];

					int cc = i * NAXIS1 + jtps;

					if (TTYPEISHEAPARRAYDESC[j])
					{
						for (int ii = 0; ii < 2; ii++)
						{
							int nelpos = TTYPEHEAPARRAYNELSPOS[j][ii, i];
							BINTABLE[cc + ii * 4] = (byte)((nelpos >> 24) & 0xff);
							BINTABLE[cc + ii * 4 + 1] = (byte)((nelpos >> 16) & 0xff);
							BINTABLE[cc + ii * 4 + 2] = (byte)((nelpos >> 8) & 0xff);
							BINTABLE[cc + ii * 4 + 3] = (byte)(nelpos & 0xff);
						}
						continue;
					}

					switch (TCODES[j])
					{
						case TypeCode.Double:
						{
							byte[] dbl = new byte[8];
							if (TREPEATS[j] == 1)
							{
								dbl = BitConverter.GetBytes(((double[])ExtensionEntryData[j])[i]);
								BINTABLE[cc] = dbl[7];
								BINTABLE[cc + 1] = dbl[6];
								BINTABLE[cc + 2] = dbl[5];
								BINTABLE[cc + 3] = dbl[4];
								BINTABLE[cc + 4] = dbl[3];
								BINTABLE[cc + 5] = dbl[2];
								BINTABLE[cc + 6] = dbl[1];
								BINTABLE[cc + 7] = dbl[0];
							}
							else
								for (int ii = 0; ii < TREPEATS[j]; ii++)
								{
									dbl = BitConverter.GetBytes(((double[,])ExtensionEntryData[j])[ii, i]);
									BINTABLE[cc + ii * 8] = dbl[7];
									BINTABLE[cc + ii * 8 + 1] = dbl[6];
									BINTABLE[cc + ii * 8 + 2] = dbl[5];
									BINTABLE[cc + ii * 8 + 3] = dbl[4];
									BINTABLE[cc + ii * 8 + 4] = dbl[3];
									BINTABLE[cc + ii * 8 + 5] = dbl[2];
									BINTABLE[cc + ii * 8 + 6] = dbl[1];
									BINTABLE[cc + ii * 8 + 7] = dbl[0];
								}
							break;
						}

						case TypeCode.Single:
						{
							byte[] flt = new byte[4];
							if (TREPEATS[j] == 1)
							{
								flt = BitConverter.GetBytes(((float[])ExtensionEntryData[j])[i]);
								BINTABLE[cc] = flt[3];
								BINTABLE[cc + 1] = flt[2];
								BINTABLE[cc + 2] = flt[1];
								BINTABLE[cc + 3] = flt[0];
							}
							else
								for (int ii = 0; ii < TREPEATS[j]; ii++)
								{
									flt = BitConverter.GetBytes(((float[,])ExtensionEntryData[j])[ii, i]);
									BINTABLE[cc + ii * 4] = flt[3];
									BINTABLE[cc + ii * 4 + 1] = flt[2];
									BINTABLE[cc + ii * 4 + 2] = flt[1];
									BINTABLE[cc + ii * 4 + 3] = flt[0];
								}
							break;
						}

						case TypeCode.Int64:
						{
							long val;
							if (TREPEATS[j] == 1)
							{
								val = ((long[])ExtensionEntryData[j])[i];
								BINTABLE[cc] = (byte)((val >> 56) & 0xff);
								BINTABLE[cc + 1] = (byte)((val >> 48) & 0xff);
								BINTABLE[cc + 2] = (byte)((val >> 40) & 0xff);
								BINTABLE[cc + 3] = (byte)((val >> 32) & 0xff);
								BINTABLE[cc + 4] = (byte)((val >> 24) & 0xff);
								BINTABLE[cc + 5] = (byte)((val >> 16) & 0xff);
								BINTABLE[cc + 6] = (byte)((val >> 8) & 0xff);
								BINTABLE[cc + 7] = (byte)(val & 0xff);
							}
							else
								for (int ii = 0; ii < TREPEATS[j]; ii++)
								{
									val = ((long[,])ExtensionEntryData[j])[ii, i];
									BINTABLE[cc + ii * 8] = (byte)((val >> 56) & 0xff);
									BINTABLE[cc + ii * 8 + 1] = (byte)((val >> 48) & 0xff);
									BINTABLE[cc + ii * 8 + 2] = (byte)((val >> 40) & 0xff);
									BINTABLE[cc + ii * 8 + 3] = (byte)((val >> 32) & 0xff);
									BINTABLE[cc + ii * 8 + 4] = (byte)((val >> 24) & 0xff);
									BINTABLE[cc + ii * 8 + 5] = (byte)((val >> 16) & 0xff);
									BINTABLE[cc + ii * 8 + 6] = (byte)((val >> 8) & 0xff);
									BINTABLE[cc + ii * 8 + 7] = (byte)(val & 0xff);
								}
							break;
						}

						case TypeCode.UInt64:
						{
							long val;
							if (TREPEATS[j] == 1)
							{
								val = MapUlongToLong(((ulong[])ExtensionEntryData[j])[i]);
								BINTABLE[cc] = (byte)((val >> 56) & 0xff);
								BINTABLE[cc + 1] = (byte)((val >> 48) & 0xff);
								BINTABLE[cc + 2] = (byte)((val >> 40) & 0xff);
								BINTABLE[cc + 3] = (byte)((val >> 32) & 0xff);
								BINTABLE[cc + 4] = (byte)((val >> 24) & 0xff);
								BINTABLE[cc + 5] = (byte)((val >> 16) & 0xff);
								BINTABLE[cc + 6] = (byte)((val >> 8) & 0xff);
								BINTABLE[cc + 7] = (byte)(val & 0xff);
							}
							else
								for (int ii = 0; ii < TREPEATS[j]; ii++)
								{
									val = MapUlongToLong(((ulong[,])ExtensionEntryData[j])[ii, i]);
									BINTABLE[cc + ii * 8] = (byte)((val >> 56) & 0xff);
									BINTABLE[cc + ii * 8 + 1] = (byte)((val >> 48) & 0xff);
									BINTABLE[cc + ii * 8 + 2] = (byte)((val >> 40) & 0xff);
									BINTABLE[cc + ii * 8 + 3] = (byte)((val >> 32) & 0xff);
									BINTABLE[cc + ii * 8 + 4] = (byte)((val >> 24) & 0xff);
									BINTABLE[cc + ii * 8 + 5] = (byte)((val >> 16) & 0xff);
									BINTABLE[cc + ii * 8 + 6] = (byte)((val >> 8) & 0xff);
									BINTABLE[cc + ii * 8 + 7] = (byte)(val & 0xff);
								}
							break;
						}

						case TypeCode.Int32:
						{
							int val;
							if (TREPEATS[j] == 1)
							{
								val = ((int[])ExtensionEntryData[j])[i];
								BINTABLE[cc] = (byte)((val >> 24) & 0xff);
								BINTABLE[cc + 1] = (byte)((val >> 16) & 0xff);
								BINTABLE[cc + 2] = (byte)((val >> 8) & 0xff);
								BINTABLE[cc + 3] = (byte)(val & 0xff);
							}
							else
								for (int ii = 0; ii < TREPEATS[j]; ii++)
								{
									val = ((int[,])ExtensionEntryData[j])[ii, i];
									BINTABLE[cc + ii * 4] = (byte)((val >> 24) & 0xff);
									BINTABLE[cc + ii * 4 + 1] = (byte)((val >> 16) & 0xff);
									BINTABLE[cc + ii * 4 + 2] = (byte)((val >> 8) & 0xff);
									BINTABLE[cc + ii * 4 + 3] = (byte)(val & 0xff);
								}
							break;
						}

						case TypeCode.UInt32:
						{
							int val;
							if (TREPEATS[j] == 1)
							{
								val = MapUintToInt(((uint[])ExtensionEntryData[j])[i]);
								BINTABLE[cc] = (byte)((val >> 24) & 0xff);
								BINTABLE[cc + 1] = (byte)((val >> 16) & 0xff);
								BINTABLE[cc + 2] = (byte)((val >> 8) & 0xff);
								BINTABLE[cc + 3] = (byte)(val & 0xff);
							}
							else
								for (int ii = 0; ii < TREPEATS[j]; ii++)
								{
									val = MapUintToInt(((uint[,])ExtensionEntryData[j])[ii, i]);
									BINTABLE[cc + ii * 4] = (byte)((val >> 24) & 0xff);
									BINTABLE[cc + ii * 4 + 1] = (byte)((val >> 16) & 0xff);
									BINTABLE[cc + ii * 4 + 2] = (byte)((val >> 8) & 0xff);
									BINTABLE[cc + ii * 4 + 3] = (byte)(val & 0xff);
								}
							break;
						}

						case TypeCode.Int16:
						{
							short val;
							if (TREPEATS[j] == 1)
							{
								val = ((short[])ExtensionEntryData[j])[i];
								BINTABLE[cc] = (byte)((val >> 8) & 0xff);
								BINTABLE[cc + 1] = (byte)(val & 0xff);
							}
							else
								for (int ii = 0; ii < TREPEATS[j]; ii++)
								{
									val = ((short[,])ExtensionEntryData[j])[ii, i];
									BINTABLE[cc + ii * 2] = (byte)((val >> 8) & 0xff);
									BINTABLE[cc + ii * 2 + 1] = (byte)(val & 0xff);
								}
							break;
						}

						case TypeCode.UInt16:
						{
							short val;
							if (TREPEATS[j] == 1)
							{
								val = MapUshortToShort(((ushort[])ExtensionEntryData[j])[i]);
								BINTABLE[cc] = (byte)((val >> 8) & 0xff);
								BINTABLE[cc + 1] = (byte)(val & 0xff);
							}
							else
								for (int ii = 0; ii < TREPEATS[j]; ii++)
								{
									val = MapUshortToShort(((ushort[,])ExtensionEntryData[j])[ii, i]);
									BINTABLE[cc + ii * 2] = (byte)((val >> 8) & 0xff);
									BINTABLE[cc + ii * 2 + 1] = (byte)(val & 0xff);
								}
							break;
						}

						case TypeCode.SByte:
						{
							if (TREPEATS[j] == 1)
								BINTABLE[cc] = (byte)(((sbyte[])ExtensionEntryData[j])[i]);
							else
								for (int ii = 0; ii < TREPEATS[j]; ii++)
									BINTABLE[cc + ii] = (byte)(((sbyte[,])ExtensionEntryData[j])[ii, i]);
							break;
						}

						case TypeCode.Byte:
						{
							if (TREPEATS[j] == 1)
								BINTABLE[cc] = (((byte[])ExtensionEntryData[j])[i]);
							else
								for (int ii = 0; ii < TREPEATS[j]; ii++)
									BINTABLE[cc + ii] = (((byte[,])ExtensionEntryData[j])[ii, i]);
							break;
						}

						case TypeCode.Boolean:
						{
							if (TREPEATS[j] == 1)
								BINTABLE[cc] = Convert.ToByte(((bool[])ExtensionEntryData[j])[i]);
							else
								for (int ii = 0; ii < TREPEATS[j]; ii++)
									BINTABLE[cc + ii] = Convert.ToByte(((bool[,])ExtensionEntryData[j])[ii, i]);
							break;
						}

						case TypeCode.Char:
						{
							for (int ii = 0; ii < TREPEATS[j]; ii++)
								BINTABLE[cc + ii] = (byte)(((string[])ExtensionEntryData[j])[i][ii]);
							break;
						}

						default:
						{
							exception = true;
							exceptiontypecode = TCODES[j];
							break;
						}
					}
				}
			});
			if (exception)
			{
				throw new Exception("Data type not recognized for writing as FITS table: '" + exceptiontypecode.ToString() + "'");
			}
		}

		private void MAKEHEAPBYTEARRAY(Array[] ExtensionEntryData)
		{
			long totalbytes;
			MAKETTYPEHEAPARRAYNELSPOS(ExtensionEntryData, out totalbytes);
			HEAPDATA = new byte[(int)totalbytes];

			ParallelOptions opts = new ParallelOptions();
			if (NAXIS2 >= Environment.ProcessorCount)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			for (int i = 0; i < TTYPEISHEAPARRAYDESC.Length; i++)//same as extension entry data length
			{
				if (!TTYPEISHEAPARRAYDESC[i])
					continue;

				switch (HEAPTCODES[i])
				{
					case TypeCode.Double:
					{
						Parallel.For(0, NAXIS2, opts, y =>
						{
							byte[] dbl = new byte[8];
							int pos = TTYPEHEAPARRAYNELSPOS[i][1, y];

							for (int x = 0; x < TTYPEHEAPARRAYNELSPOS[i][0, y]; x++)
							{
								dbl = BitConverter.GetBytes(((double[])(((double[][])(ExtensionEntryData[i]))[y]))[x]);
								HEAPDATA[pos] = dbl[7];
								HEAPDATA[pos + 1] = dbl[6];
								HEAPDATA[pos + 2] = dbl[5];
								HEAPDATA[pos + 3] = dbl[4];
								HEAPDATA[pos + 4] = dbl[3];
								HEAPDATA[pos + 5] = dbl[2];
								HEAPDATA[pos + 6] = dbl[1];
								HEAPDATA[pos + 7] = dbl[0];
								pos += 8;
							}
						});
						break;
					}

					case TypeCode.Single:
					{
						Parallel.For(0, NAXIS2, opts, y =>
						{
							byte[] sng = new byte[4];
							int pos = TTYPEHEAPARRAYNELSPOS[i][1, y];

							for (int x = 0; x < TTYPEHEAPARRAYNELSPOS[i][0, y]; x++)
							{
								sng = BitConverter.GetBytes(((float[])(((float[][])(ExtensionEntryData[i]))[y]))[x]);
								HEAPDATA[pos] = sng[3];
								HEAPDATA[pos + 1] = sng[2];
								HEAPDATA[pos + 2] = sng[1];
								HEAPDATA[pos + 3] = sng[0];
								pos += 4;
							}
						});
						break;
					}

					case TypeCode.Int64:
					{
						Parallel.For(0, NAXIS2, opts, y =>
						{
							long val;
							int pos = TTYPEHEAPARRAYNELSPOS[i][1, y];

							for (int x = 0; x < TTYPEHEAPARRAYNELSPOS[i][0, y]; x++)
							{
								val = ((long[])(((long[][])(ExtensionEntryData[i]))[y]))[x];
								HEAPDATA[pos] = (byte)((val >> 56) & 0xff);
								HEAPDATA[pos + 1] = (byte)((val >> 48) & 0xff);
								HEAPDATA[pos + 2] = (byte)((val >> 40) & 0xff);
								HEAPDATA[pos + 3] = (byte)((val >> 32) & 0xff);
								HEAPDATA[pos + 4] = (byte)((val >> 24) & 0xff);
								HEAPDATA[pos + 5] = (byte)((val >> 16) & 0xff);
								HEAPDATA[pos + 6] = (byte)((val >> 8) & 0xff);
								HEAPDATA[pos + 7] = (byte)(val & 0xff);
								pos += 8;
							}
						});
						break;
					}

					case TypeCode.UInt64:
					{
						Parallel.For(0, NAXIS2, opts, y =>
						{
							long val;
							int pos = TTYPEHEAPARRAYNELSPOS[i][1, y];

							for (int x = 0; x < TTYPEHEAPARRAYNELSPOS[i][0, y]; x++)
							{
								val = MapUlongToLong(((ulong[])(((ulong[][])(ExtensionEntryData[i]))[y]))[x]);
								HEAPDATA[pos] = (byte)((val >> 56) & 0xff);
								HEAPDATA[pos + 1] = (byte)((val >> 48) & 0xff);
								HEAPDATA[pos + 2] = (byte)((val >> 40) & 0xff);
								HEAPDATA[pos + 3] = (byte)((val >> 32) & 0xff);
								HEAPDATA[pos + 4] = (byte)((val >> 24) & 0xff);
								HEAPDATA[pos + 5] = (byte)((val >> 16) & 0xff);
								HEAPDATA[pos + 6] = (byte)((val >> 8) & 0xff);
								HEAPDATA[pos + 7] = (byte)(val & 0xff);
								pos += 8;
							}
						});
						break;
					}

					case TypeCode.Int32:
					{
						Parallel.For(0, NAXIS2, opts, y =>
						{
							int val;
							int pos = TTYPEHEAPARRAYNELSPOS[i][1, y];

							for (int x = 0; x < TTYPEHEAPARRAYNELSPOS[i][0, y]; x++)
							{
								val = ((int[])(((int[][])(ExtensionEntryData[i]))[y]))[x];
								HEAPDATA[pos] = (byte)((val >> 24) & 0xff);
								HEAPDATA[pos + 1] = (byte)((val >> 16) & 0xff);
								HEAPDATA[pos + 2] = (byte)((val >> 8) & 0xff);
								HEAPDATA[pos + 3] = (byte)(val & 0xff);
								pos += 4;
							}
						});
						break;
					}

					case TypeCode.UInt32:
					{
						Parallel.For(0, NAXIS2, opts, y =>
						{
							int val;
							int pos = TTYPEHEAPARRAYNELSPOS[i][1, y];

							for (int x = 0; x < TTYPEHEAPARRAYNELSPOS[i][0, y]; x++)
							{
								val = MapUintToInt(((uint[])(((uint[][])(ExtensionEntryData[i]))[y]))[x]);
								HEAPDATA[pos] = (byte)((val >> 24) & 0xff);
								HEAPDATA[pos + 1] = (byte)((val >> 16) & 0xff);
								HEAPDATA[pos + 2] = (byte)((val >> 8) & 0xff);
								HEAPDATA[pos + 3] = (byte)(val & 0xff);
								pos += 4;
							}
						});
						break;
					}

					case TypeCode.Int16:
					{
						Parallel.For(0, NAXIS2, opts, y =>
						{
							short val;
							int pos = TTYPEHEAPARRAYNELSPOS[i][1, y];

							for (int x = 0; x < TTYPEHEAPARRAYNELSPOS[i][0, y]; x++)
							{
								val = ((short[])(((short[][])(ExtensionEntryData[i]))[y]))[x];
								HEAPDATA[pos] = (byte)((val >> 8) & 0xff);
								HEAPDATA[pos + 1] = (byte)(val & 0xff);
								pos += 2;
							}
						});
						break;
					}

					case TypeCode.UInt16:
					{
						Parallel.For(0, NAXIS2, opts, y =>
						{
							short val;
							int pos = TTYPEHEAPARRAYNELSPOS[i][1, y];

							for (int x = 0; x < TTYPEHEAPARRAYNELSPOS[i][0, y]; x++)
							{
								val = MapUshortToShort(((ushort[])(((ushort[][])(ExtensionEntryData[i]))[y]))[x]);
								HEAPDATA[pos] = (byte)((val >> 8) & 0xff);
								HEAPDATA[pos + 1] = (byte)(val & 0xff);
								pos += 2;
							}
						});
						break;
					}

					case TypeCode.SByte:
					{
						Parallel.For(0, NAXIS2, opts, y =>
						{
							for (int x = 0; x < TTYPEHEAPARRAYNELSPOS[i][0, y]; x++)
								HEAPDATA[TTYPEHEAPARRAYNELSPOS[i][1, y] + x] = (byte)((sbyte[])(((sbyte[][])(ExtensionEntryData[i]))[y]))[x];
						});
						break;
					}

					case TypeCode.Byte:
					{
						Parallel.For(0, NAXIS2, opts, y =>
						{
							for (int x = 0; x < TTYPEHEAPARRAYNELSPOS[i][0, y]; x++)
								HEAPDATA[TTYPEHEAPARRAYNELSPOS[i][1, y] + x] = ((byte[])(((byte[][])(ExtensionEntryData[i]))[y]))[x];
						});
						break;
					}

					case TypeCode.Boolean:
					{
						Parallel.For(0, NAXIS2, opts, y =>
						{
							for (int x = 0; x < TTYPEHEAPARRAYNELSPOS[i][0, y]; x++)
								HEAPDATA[TTYPEHEAPARRAYNELSPOS[i][1, y] + x] = Convert.ToByte(((bool[])(((bool[][])(ExtensionEntryData[i]))[y]))[x]);
						});
						break;
					}

					case TypeCode.Char:
					{
						Parallel.For(0, NAXIS2, opts, y =>
						{
							for (int x = 0; x < TTYPEHEAPARRAYNELSPOS[i][0, y]; x++)
								HEAPDATA[TTYPEHEAPARRAYNELSPOS[i][1, y] + x] = (byte)((string)(((string[])(ExtensionEntryData[i]))[y]))[x];
						});
						break;
					}

					default:
					{
						throw new Exception("Data type not recognized for writing as FITS table: '" + HEAPTCODES[i].ToString() + "'");
					}
				}
			}
		}

		private void MAKETTYPEHEAPARRAYNELSPOS(Array[] ExtensionEntryData, out long totalBytes)
		{
			int pos = 0, nels;
			totalBytes = 0;
			for (int i = 0; i < TTYPEISHEAPARRAYDESC.Length; i++)//same as extension entry data length
				if (TTYPEISHEAPARRAYDESC[i])
				{
					TTYPEHEAPARRAYNELSPOS[i] = new int[2, NAXIS2];
					if (HEAPTCODES[i] == TypeCode.Char)
						for (int j = 0; j < NAXIS2; j++)
						{
							nels = ((string)(((Array)ExtensionEntryData[i]).GetValue(j))).Length;
							TTYPEHEAPARRAYNELSPOS[i][0, j] = nels;
							TTYPEHEAPARRAYNELSPOS[i][1, j] = pos;
							pos += (nels * TYPECODETONBYTES(HEAPTCODES[i]));
						}
					else
						for (int j = 0; j < NAXIS2; j++)
						{
							nels = ((Array)(((Array)ExtensionEntryData[i]).GetValue(j))).Length;
							TTYPEHEAPARRAYNELSPOS[i][0, j] = nels;
							TTYPEHEAPARRAYNELSPOS[i][1, j] = pos;
							pos += (nels * TYPECODETONBYTES(HEAPTCODES[i]));
						}
				}
			totalBytes = pos;
		}

		private string[] FORMATBINARYTABLEEXTENSIONHEADER()
		{
			ArrayList hkeyslist = new ArrayList();
			ArrayList hvalslist = new ArrayList();
			ArrayList hcomslist = new ArrayList();

			hkeyslist.Add("XTENSION");
			hvalslist.Add("BINTABLE");
			hcomslist.Add("binary table extension");
			hkeyslist.Add("BITPIX");
			hvalslist.Add("8");
			hcomslist.Add("8-bit bytes");
			hkeyslist.Add("NAXIS");
			hvalslist.Add("2");
			hcomslist.Add("2-dimensional binary table");
			hkeyslist.Add("NAXIS1");
			hvalslist.Add(NAXIS1.ToString());
			hcomslist.Add("width of table in bytes");
			hkeyslist.Add("NAXIS2");
			hvalslist.Add(NAXIS2.ToString());
			hcomslist.Add("number of rows in table");
			hkeyslist.Add("PCOUNT");
			if (HEAPDATA == null)
				hvalslist.Add("0");
			else
				hvalslist.Add(HEAPDATA.Length.ToString());//we do not write the heap with a gap, it comes right after the bintable, hence PCOUNT is simply the heap size, and the THEAP gap is zero
			hcomslist.Add("size of heap data area (bytes)");
			hkeyslist.Add("GCOUNT");
			hvalslist.Add("1");
			hcomslist.Add("one data group");
			hkeyslist.Add("TFIELDS");
			hvalslist.Add(TFIELDS.ToString());
			hcomslist.Add("number of fields in each row");
			if (EXTENSIONNAME != "")
			{
				hkeyslist.Add("EXTNAME");
				hvalslist.Add(EXTENSIONNAME);
				hcomslist.Add("name of this binary table extension");
			}

			//KEY formats
			for (int i = 0; i < TTYPES.Length; i++)
			{
				//TFORM
				hkeyslist.Add("TFORM" + (i + 1).ToString());
				if (!TTYPEISHEAPARRAYDESC[i])
					hvalslist.Add(TFORMS[i]);
				else
				{
					int max = 0;
					for (int j = 0; j < NAXIS2; j++)
						if (TTYPEHEAPARRAYNELSPOS[i][0, j] > max)
							max = TTYPEHEAPARRAYNELSPOS[i][0, j];
					hvalslist.Add(TFORMS[i] + "(" + max.ToString() + ")");
				}
				if (TTYPEISHEAPARRAYDESC[i])
					if (!TTYPEISCOMPLEX[i])
						hcomslist.Add((2 * TYPECODETONBYTES(TCODES[i])).ToString() + "-byte " + TYPECODESTRING(TCODES[i]) + " heap descriptor for " + TYPECODESTRING(HEAPTCODES[i]));
					else
						hcomslist.Add((2 * TYPECODETONBYTES(TCODES[i])).ToString() + "-byte " + TYPECODESTRING(TCODES[i]) + " heap descriptor for " + TYPECODESTRING(HEAPTCODES[i]) + " complex pair");
				else if (TTYPEISCOMPLEX[i])
					hcomslist.Add((2 * TYPECODETONBYTES(TCODES[i])).ToString() + "-byte " + TYPECODESTRING(TCODES[i]) + " complex pair");
				else
					hcomslist.Add(TYPECODETONBYTES(TCODES[i]).ToString() + "-byte " + TYPECODESTRING(TCODES[i]));

				//TTYPE
				hkeyslist.Add("TTYPE" + (i + 1).ToString());
				hvalslist.Add(TTYPES[i]);
				hcomslist.Add("label for field " + (i + 1).ToString());

				//TZERO and TSCAL
				if (!TTYPEISHEAPARRAYDESC[i] && TCODES[i] == TypeCode.SByte || HEAPTCODES[i] == TypeCode.SByte)
				{
					hkeyslist.Add("TZERO" + (i + 1).ToString());
					hvalslist.Add("-128");
					hcomslist.Add("offset for signed 8-bit integers");

					hkeyslist.Add("TSCAL" + (i + 1).ToString());
					hvalslist.Add("1");
					hcomslist.Add("data are not scaled");
				}
				else if (!TTYPEISHEAPARRAYDESC[i] && TCODES[i] == TypeCode.UInt16 || HEAPTCODES[i] == TypeCode.UInt16)
				{
					hkeyslist.Add("TZERO" + (i + 1).ToString());
					hvalslist.Add("32768");
					hcomslist.Add("offset for unsigned 16-bit integers");

					hkeyslist.Add("TSCAL" + (i + 1).ToString());
					hvalslist.Add("1");
					hcomslist.Add("data are not scaled");
				}
				else if (!TTYPEISHEAPARRAYDESC[i] && TCODES[i] == TypeCode.UInt32 || HEAPTCODES[i] == TypeCode.UInt32)
				{
					hkeyslist.Add("TZERO" + (i + 1).ToString());
					hvalslist.Add("2147483648");
					hcomslist.Add("offset for unsigned 32-bit integers");

					hkeyslist.Add("TSCAL" + (i + 1).ToString());
					hvalslist.Add("1");
					hcomslist.Add("data are not scaled");
				}
				else if (!TTYPEISHEAPARRAYDESC[i] && TCODES[i] == TypeCode.UInt64 || HEAPTCODES[i] == TypeCode.UInt64)
				{
					hkeyslist.Add("TZERO" + (i + 1).ToString());
					hvalslist.Add("9223372036854775808");
					hcomslist.Add("offset for unsigned 64-bit integers");

					hkeyslist.Add("TSCAL" + (i + 1).ToString());
					hvalslist.Add("1");
					hcomslist.Add("data are not scaled");
				}

				//TUNIT
				if (TUNITS != null && TUNITS[i] != null && TUNITS[i] != "")
				{
					hkeyslist.Add("TUNIT" + (i + 1).ToString());
					hvalslist.Add(TUNITS[i]);
					hcomslist.Add("physical unit of field");
				}

				//TDIM
				if (TDIMS[i] != null)//then it is a multi D array, and the dims should exist for this entry
				{
					hkeyslist.Add("TDIM" + (i + 1).ToString());
					string dim = "(";
					for (int j = 0; j < TDIMS[i].Length; j++)
						dim += (TDIMS[i][j].ToString() + ",");
					dim = dim.Remove(dim.Length - 1) + ")";
					hvalslist.Add(dim);
					hcomslist.Add("N-dim array dimensions");
				}
			}

			//EXTRAKEYS
			if (EXTRAKEYS != null)
				for (int i = 0; i < EXTRAKEYS.Length; i++)
				{
					hkeyslist.Add(EXTRAKEYS[i].ToUpper());
					hvalslist.Add(EXTRAKEYVALS[i]);
					hcomslist.Add(EXTRAKEYCOMS[i]);
				}

			hkeyslist.Add("END     ");
			hvalslist.Add("");
			hcomslist.Add("");

			int NKeys = hkeyslist.Count;
			int NCards = (NKeys - 1) / 36;
			int NBlankKeys = (NCards + 1) * 36 - NKeys;
			string[] headerkeys = new string[((NCards + 1) * 36)];
			string[] headerkeyvals = new string[((NCards + 1) * 36)];
			string[] headerkeycoms = new string[((NCards + 1) * 36)];
			string[] header = new string[((NCards + 1) * 36)];

			for (int i = 0; i < NKeys; i++)
			{
				headerkeys[i] = (string)hkeyslist[i];
				headerkeyvals[i] = (string)hvalslist[i];
				headerkeycoms[i] = (string)hcomslist[i];
			}

			string key;
			string value;
			string comment;
			for (int i = 0; i < NKeys - 1; i++)
			{
				key = headerkeys[i];
				key = key.Trim();//in case some idiot put spaces in the front or back...if in the middle??
				int L = key.Length;
				if (key == "COMMENT")
					key = "COMMENT ";//8 long, comment follows; key now = "COMMENT "..., no "=" needed
				else if (L >= 8)
				{
					key = key.Substring(0, 8);
					key += "= ";//key formatting done
				}
				else if (L < 8)
				{
					for (int ii = 0; ii < 8 - L; ii++)
						key += " ";//pad right
					key += "= ";//key formatting done
				}

				//do value formatting
				if (JPMath.IsNumeric(headerkeyvals[i]))//then we have a numeric key value
				{
					double val = Convert.ToDouble(headerkeyvals[i]);
					if (val == 9223372036854775808)
						value = "9223372036854775808";
					else
						value = val.ToString();
					L = value.Length;
					if (L > 20)
						value = value.Substring(0, 20);
					if (L < 20)
						for (int ii = 0; ii < 20 - L; ii++)
							value = " " + value;//pad left
				}
				else//else it must be a string or comment.
				{
					value = headerkeyvals[i];
					L = value.Length;
					if (L >= 18)
					{
						value = value.Substring(0, 18);
						value = "'" + value + "'";
					}
					if (L < 18)
					{
						value = "'" + value + "'";
						for (int ii = 0; ii < 18 - L; ii++)
							value += " ";//pad right
					}
					if (headerkeyvals[i].Trim() == "T")
						value = "                   T";
					if (headerkeyvals[i].Trim() == "F")
						value = "                   F";
				}
				//value formatting done

				//do comment formatting...always a string
				comment = headerkeycoms[i];
				L = comment.Length;
				if (L > 48)
					comment = comment.Substring(0, 48);
				if (L < 48)
					for (int ii = 0; ii < 48 - L; ii++)
						comment += " ";//pad right
				comment = " /" + comment;//comment formatting done

				//check for COMMENT and reconfigure key line...it isn't the most efficient approach but I dont care.
				if (key == "COMMENT ")
				{
					comment = headerkeyvals[i] + headerkeycoms[i];
					value = "";
					L = comment.Length;
					if (L > 72)
						comment = comment.Substring(0, 72);
					if (L < 72)
						for (int ii = 0; ii < 72 - L; ii++)
							comment += " ";//pad right
				}

				header[i] = key + value + comment;
			}

			header[NKeys - 1] = ("END").PadRight(80);

			for (int i = 0; i < NBlankKeys; i++)
				header[NKeys + i] = ("").PadRight(80);

			return header;
		}

		private int TFORMTONBYTES(string tform, out int instances)
		{
			int N = 1;
			if (tform.Length > 1)
				if (tform.Contains("Q") || tform.Contains("P"))////heap ttype
				{
					if (JPMath.IsNumeric(tform.Substring(0, 1)))
						N = Convert.ToInt32(tform.Substring(0, 1));//might be zero...one default
					if (tform.Contains("Q"))
						tform = "Q";
					else
						tform = "P";
				}
				else
					N = Convert.ToInt32(tform.Substring(0, tform.Length - 1));//bintable ttype
			instances = N;

			char f = Convert.ToChar(tform.Substring(tform.Length - 1));

			switch (f)
			{
				case 'M':
				case 'Q':
				{
					instances *= 2;
					return N *= 16;
				}

				case 'C':
				case 'P':
				{
					instances *= 2;
					return N *= 8;
				}

				case 'D':
				case 'K':
					return N *= 8;

				case 'J':
				case 'E':
					return N *= 4;

				case 'I':
					return N *= 2;

				case 'A':
				case 'B':
				case 'L':
					return N *= 1;

				case 'X':
					return (int)(Math.Ceiling((double)(N) / 8));

				default:
					throw new Exception("Unrecognized TFORM: '" + tform + "'");
			}
		}

		private TypeCode TFORMTYPECODE(string tform)
		{
			char c = Convert.ToChar(tform.Substring(tform.Length - 1));

			switch (c)
			{
				case 'L':
					return TypeCode.Boolean;

				case 'X':
				case 'B':
					return TypeCode.Byte;

				case 'I':
					return TypeCode.Int16;

				case 'J':
				case 'P':
					return TypeCode.Int32;

				case 'K':
				case 'Q':
					return TypeCode.Int64;

				case 'A':
					return TypeCode.Char;

				case 'E':
				case 'C':
					return TypeCode.Single;

				case 'D':
				case 'M':
					return TypeCode.Double;

				default:
					throw new Exception("Unrecognized TFORM: '" + tform + "'");
			}
		}

		private string TYPECODETFORM(TypeCode typecode)
		{
			switch (typecode)
			{
				case TypeCode.Double:
					return "D";
				case TypeCode.UInt64:
				case TypeCode.Int64:
					return "K";
				case TypeCode.Single:
					return "E";
				case TypeCode.UInt32:
				case TypeCode.Int32:
					return "J";
				case TypeCode.UInt16:
				case TypeCode.Int16:
					return "I";
				case TypeCode.Byte:
				case TypeCode.SByte:
					return "B";
				case TypeCode.Boolean:
					return "L";
				case TypeCode.Char:
					return "A";
				default:
					throw new Exception("Unrecognized typecode: '" + typecode.ToString() + "'");
			}
		}

		private string TYPECODESTRING(TypeCode typecode)
		{
			switch (typecode)
			{
				case TypeCode.Double:
					return "DOUBLE";

				case TypeCode.UInt64:
				case TypeCode.Int64:
				case TypeCode.UInt32:
				case TypeCode.Int32:
				case TypeCode.UInt16:
				case TypeCode.Int16:
					return "INTEGER";

				case TypeCode.Single:
					return "SINGLE";

				case TypeCode.Byte:
				case TypeCode.SByte:
					return "BYTE";

				case TypeCode.Boolean:
					return "LOGICAL";

				case TypeCode.Char:
					return "CHAR";

				default:
					throw new Exception("Unrecognized typecode: '" + typecode.ToString() + "'");
			}
		}

		private int TYPECODETONBYTES(TypeCode typecode)
		{
			switch (typecode)
			{
				case TypeCode.Double:
				case TypeCode.UInt64:
				case TypeCode.Int64:
					return 8;

				case TypeCode.UInt32:
				case TypeCode.Int32:
				case TypeCode.Single:
					return 4;

				case TypeCode.UInt16:
				case TypeCode.Int16:
					return 2;

				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Boolean:
				case TypeCode.Char:
					return 1;

				default:
					throw new Exception("Unrecognized typecode: '" + typecode.ToString() + "'");
			}
		}

		private void EATRAWBINTABLEHEADER(ArrayList header)
		{
			//reset
			BITPIX = 0;
			NAXIS = 0;
			NAXIS1 = 0;
			NAXIS2 = 0;
			TFIELDS = 0;

			ArrayList extras = new ArrayList();//for possible extras

			HEADER = new string[(header.Count)];
			string strheaderline;
			int ttypeindex = -1;

			for (int i = 0; i < header.Count; i++)
			{
				strheaderline = (string)header[i];
				HEADER[i] = strheaderline;

				if (BITPIX == 0)
					if (strheaderline.Substring(0, 8).Trim().Equals("BITPIX"))
					{
						BITPIX = Convert.ToInt32(strheaderline.Substring(10, 20));
						continue;
					}
				if (NAXIS == 0)
					if (strheaderline.Substring(0, 8).Trim().Equals("NAXIS"))
					{
						NAXIS = Convert.ToInt32(strheaderline.Substring(10, 20));
						continue;
					}
				if (NAXIS1 == 0)
					if (strheaderline.Substring(0, 8).Trim().Equals("NAXIS1"))
					{
						NAXIS1 = Convert.ToInt32(strheaderline.Substring(10, 20));
						continue;
					}
				if (NAXIS2 == 0)
					if (strheaderline.Substring(0, 8).Trim().Equals("NAXIS2"))
					{
						NAXIS2 = Convert.ToInt32(strheaderline.Substring(10, 20));
						continue;
					}
				if (TFIELDS == 0)
					if (strheaderline.Substring(0, 8).Trim().Equals("TFIELDS"))
					{
						TFIELDS = Convert.ToInt32(strheaderline.Substring(10, 20));
						TTYPES = new string[TFIELDS];
						TFORMS = new string[(TFIELDS)];
						TBYTES = new int[(TFIELDS)];
						TREPEATS = new int[(TFIELDS)];
						TCODES = new TypeCode[(TFIELDS)];
						TUNITS = new string[(TFIELDS)];
						TTYPEISCOMPLEX = new bool[(TFIELDS)];
						TTYPEISHEAPARRAYDESC = new bool[(TFIELDS)];
						HEAPTCODES = new TypeCode[(TFIELDS)];
						TDIMS = new int[(TFIELDS)][];
						TTYPEHEAPARRAYNELSPOS = new int[(TFIELDS)][,];
						continue;
					}

				if (strheaderline.Substring(0, 8).Trim().Equals("TTYPE" + (ttypeindex + 2).ToString()) || strheaderline.Substring(0, 8).Trim().Equals("TFORM" + (ttypeindex + 2).ToString()))
					ttypeindex++;

				if (strheaderline.Substring(0, 8).Trim().Equals("TTYPE" + (ttypeindex + 1).ToString()))
				{
					int f = strheaderline.IndexOf("'");
					int l = strheaderline.LastIndexOf("'");
					TTYPES[ttypeindex] = strheaderline.Substring(f + 1, l - f - 1).Trim();
					continue;
				}

				if (strheaderline.Substring(0, 8).Trim().Equals("TFORM" + (ttypeindex + 1).ToString()))
				{
					int f = strheaderline.IndexOf("'");
					int l = strheaderline.LastIndexOf("'");
					TFORMS[ttypeindex] = strheaderline.Substring(f + 1, l - f - 1).Trim();
					int instances;
					TBYTES[ttypeindex] = TFORMTONBYTES(TFORMS[ttypeindex], out instances);
					TREPEATS[ttypeindex] = instances;
					if (TFORMS[ttypeindex].Contains("Q") || TFORMS[ttypeindex].Contains("P"))//heap form
					{
						TTYPEHEAPARRAYNELSPOS[ttypeindex] = GETHEAPTTYPENELSPOS(ttypeindex);
						TTYPEISHEAPARRAYDESC[ttypeindex] = true;
						if (TFORMS[ttypeindex].Contains("Q"))
						{
							TCODES[ttypeindex] = TFORMTYPECODE("Q");
							HEAPTCODES[ttypeindex] = TFORMTYPECODE(TFORMS[ttypeindex].Substring(TFORMS[ttypeindex].IndexOf("Q") + 1, 1));
						}
						else
						{
							TCODES[ttypeindex] = TFORMTYPECODE("P");
							HEAPTCODES[ttypeindex] = TFORMTYPECODE(TFORMS[ttypeindex].Substring(TFORMS[ttypeindex].IndexOf("P") + 1, 1));
						}
						if (HEAPTCODES[ttypeindex] == TypeCode.Double || HEAPTCODES[ttypeindex] == TypeCode.Single)
							TTYPEISCOMPLEX[ttypeindex] = (TFORMS[ttypeindex].Contains("M") || TFORMS[ttypeindex].Contains("C"));
					}
					else
					{
						TCODES[ttypeindex] = TFORMTYPECODE(TFORMS[ttypeindex]);
						if (TCODES[ttypeindex] == TypeCode.Double || TCODES[ttypeindex] == TypeCode.Single)
							TTYPEISCOMPLEX[ttypeindex] = (TFORMS[ttypeindex].Contains("M") || TFORMS[ttypeindex].Contains("C"));
					}
					continue;
				}

				if (strheaderline.Substring(0, 8).Trim().Equals("TUNIT" + (ttypeindex + 1).ToString()))
				{
					int f = strheaderline.IndexOf("'");
					int l = strheaderline.LastIndexOf("'");
					if (f != -1)
						TUNITS[ttypeindex] = strheaderline.Substring(f + 1, l - f - 1).Trim();
					else
						TUNITS[ttypeindex] = strheaderline.Substring(10, 20).Trim();
					continue;
				}

				if (strheaderline.Substring(0, 8).Trim().Equals("TDIM" + (ttypeindex + 1).ToString()))
				{
					ArrayList dimslist = new ArrayList();
					//TDIMn = '(5,4,3)'
					int f = strheaderline.IndexOf("'");
					int l = strheaderline.LastIndexOf("'");
					string dimline = strheaderline.Substring(f + 1, l - f - 1).Trim();//(5,4,3)
					dimline = dimline.Substring(1);//5,4,3)
					dimline = dimline.Substring(0, dimline.Length - 1);//5,4,3
					int lastcommaindex = 0;
					int nextcommaindex = -1;
					while (dimline.IndexOf(",", lastcommaindex + 1) != -1)
					{
						nextcommaindex = dimline.IndexOf(",", lastcommaindex + 1);
						dimslist.Add(dimline.Substring(lastcommaindex, nextcommaindex - lastcommaindex));
						lastcommaindex = nextcommaindex + 1;
					}
					dimslist.Add(dimline.Substring(lastcommaindex));
					TDIMS[ttypeindex] = new int[(dimslist.Count)];
					for (int ii = 0; ii < dimslist.Count; ii++)
						TDIMS[ttypeindex][ii] = Convert.ToInt32((string)dimslist[i]);
					continue;
				}

				//need to determine if the TypeCode here is supposed to be for signed or unsigned IF the type is an integer (8, 16, 32, or 64 bit)
				//therefore find the TSCALE and TZERO for this entry...if they don't exist then it is signed, if they do exist
				//then it is whatever values they are, for either determined signed or unsigned
				//then set this current tcode[typeindex] to what it should be
				if (strheaderline.Substring(0, 8).Trim().Equals("TZERO" + (ttypeindex + 1).ToString()))
				{
					if (!TTYPEISHEAPARRAYDESC[ttypeindex])
						switch (TCODES[ttypeindex])
						{
							case TypeCode.Byte:
							{
								if (Convert.ToSByte(strheaderline.Substring(10, 20).Trim()) == -128)//then it is a signed
									TCODES[ttypeindex] = TypeCode.SByte;
								break;
							}
							case TypeCode.Int16:
							{
								if (Convert.ToUInt16(strheaderline.Substring(10, 20).Trim()) == 32768)//then it is an unsigned
									TCODES[ttypeindex] = TypeCode.UInt16;
								break;
							}
							case TypeCode.Int32:
							{
								if (Convert.ToUInt32(strheaderline.Substring(10, 20).Trim()) == 2147483648)//then it is an unsigned
									TCODES[ttypeindex] = TypeCode.UInt32;
								break;
							}
							case TypeCode.Int64:
							{
								if (Convert.ToUInt64(strheaderline.Substring(10, 20).Trim()) == 9223372036854775808)//then it is an unsigned
									TCODES[ttypeindex] = TypeCode.UInt64;
								break;
							}
							default:
							{
								throw new Exception("Unrecognized TypeCode in EATRAWBINTABLEHEADER at TZERO analysis");
							}
						}
					else
						switch (HEAPTCODES[ttypeindex])
						{
							case TypeCode.Byte:
							{
								if (Convert.ToSByte(strheaderline.Substring(10, 20).Trim()) == -128)//then it is a signed
									HEAPTCODES[ttypeindex] = TypeCode.SByte;
								break;
							}
							case TypeCode.Int16:
							{
								if (Convert.ToUInt16(strheaderline.Substring(10, 20).Trim()) == 32768)//then it is an unsigned
									HEAPTCODES[ttypeindex] = TypeCode.UInt16;
								break;
							}
							case TypeCode.Int32:
							{
								if (Convert.ToUInt32(strheaderline.Substring(10, 20).Trim()) == 2147483648)//then it is an unsigned
									HEAPTCODES[ttypeindex] = TypeCode.UInt32;
								break;
							}
							case TypeCode.Int64:
							{
								if (Convert.ToUInt64(strheaderline.Substring(10, 20).Trim()) == 9223372036854775808)//then it is an unsigned
									HEAPTCODES[ttypeindex] = TypeCode.UInt64;
								break;
							}
							default:
							{
								throw new Exception("Unrecognized TypeCode in EATRAWBINTABLEHEADER at TZERO analysis");
							}
						}
					continue;
				}

				if (strheaderline.Substring(0, 8).Trim().Equals("TSCAL" + (ttypeindex + 1).ToString()))//don't need to do anything with this
					continue;

				string key = strheaderline.Substring(0, 8).Trim();
				if (key.Length > 0 && key.Substring(0, 1) == "T" && JPMath.IsNumeric(key.Substring(key.Length - 1)))//then likely it is some other T____n field which isn't explicitly coded above...
					continue;

				//should now only be where extra keys might remain...so add them etc
				extras.Add(header[i]);
			}

			if (extras.Count == 0)
				return;

			EXTRAKEYS = new string[(extras.Count)];
			EXTRAKEYVALS = new string[(extras.Count)];
			EXTRAKEYCOMS = new string[(extras.Count)];

			for (int i = 0; i < extras.Count; i++)
			{
				string line = (string)extras[i];
				EXTRAKEYS[i] = line.Substring(0, 8).Trim();

				if (EXTRAKEYS[i] == "COMMENT")
				{
					EXTRAKEYVALS[i] = line.Substring(8, 18);
					EXTRAKEYCOMS[i] = line.Substring(26);
				}
				else
				{
					if (JPMath.IsNumeric(line.Substring(10, 20)))//this has to work if it is supposed to be a numeric value here
						EXTRAKEYVALS[i] = line.Substring(10, 20).Trim();//get rid of leading and trailing white space
					else
					{
						string nock = "'";
						EXTRAKEYVALS[i] = line.Substring(10, 20).Trim();
						EXTRAKEYVALS[i] = EXTRAKEYVALS[i].Trim(nock.ToCharArray());
						EXTRAKEYVALS[i] = EXTRAKEYVALS[i].Trim();
					}
					EXTRAKEYCOMS[i] = line.Substring(32).Trim();
				}
			}
		}

		private Array GETHEAPTTYPE(int ttypeindex, out TypeCode objectTypeCode, out int[] dimNElements)
		{
			objectTypeCode = HEAPTCODES[ttypeindex];

			if (TDIMS[ttypeindex] != null)
				dimNElements = TDIMS[ttypeindex];
			else
			{
				dimNElements = new int[2];
				dimNElements[1] = NAXIS2;
				int max = 0;

				for (int i = 0; i < NAXIS2; i++)
					if (TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i] > max)
						max = TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i];
				dimNElements[0] = max;
				if (TTYPEISCOMPLEX[ttypeindex])
					dimNElements[0] /= 2;
			}

			ParallelOptions opts = new ParallelOptions();
			if (NAXIS2 >= Environment.ProcessorCount)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			switch (objectTypeCode)
			{
				case TypeCode.Double:
				{
					double[][] arrya = new double[NAXIS2][];
					Parallel.For(0, NAXIS2, opts, i =>
					{
						double[] row = new double[(TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i])];
						int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeindex][1, i];
						byte[] dbl = new byte[(8)];

						for (int j = 0; j < row.Length; j++)
						{
							dbl[7] = HEAPDATA[pos];
							dbl[6] = HEAPDATA[pos + 1];
							dbl[5] = HEAPDATA[pos + 2];
							dbl[4] = HEAPDATA[pos + 3];
							dbl[3] = HEAPDATA[pos + 4];
							dbl[2] = HEAPDATA[pos + 5];
							dbl[1] = HEAPDATA[pos + 6];
							dbl[0] = HEAPDATA[pos + 7];
							row[j] = BitConverter.ToDouble(dbl, 0);
							pos += 8;
						}
						arrya[i] = row;
					});
					return arrya;
				}

				case TypeCode.Single:
				{
					float[][] arrya = new float[(NAXIS2)][];
					Parallel.For(0, NAXIS2, opts, i =>
					{
						float[] row = new float[(TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i])];
						int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeindex][1, i];
						byte[] sng = new byte[(4)];

						for (int j = 0; j < row.Length; j++)
						{
							sng[3] = HEAPDATA[pos];
							sng[2] = HEAPDATA[pos + 1];
							sng[1] = HEAPDATA[pos + 2];
							sng[0] = HEAPDATA[pos + 3];
							row[j] = BitConverter.ToSingle(sng, 0);
							pos += 4;
						}
						arrya[i] = row;
					});
					return arrya;
				}

				case (TypeCode.Int64):
				{
					long[][] arrya = new long[NAXIS2][];
					Parallel.For(0, NAXIS2, opts, i =>
					{
						long[] row = new long[(TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i])];
						int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeindex][1, i];
						byte[] i64 = new byte[(8)];

						for (int j = 0; j < row.Length; j++)
						{
							i64[7] = HEAPDATA[pos];
							i64[6] = HEAPDATA[pos + 1];
							i64[5] = HEAPDATA[pos + 2];
							i64[4] = HEAPDATA[pos + 3];
							i64[3] = HEAPDATA[pos + 4];
							i64[2] = HEAPDATA[pos + 5];
							i64[1] = HEAPDATA[pos + 6];
							i64[0] = HEAPDATA[pos + 7];
							row[j] = BitConverter.ToInt64(i64, 0);
							pos += 8;
						}
						arrya[i] = row;
					});
					return arrya;
				}

				case (TypeCode.UInt64):
				{
					ulong[][] arrya = new ulong[(NAXIS2)][];
					Parallel.For(0, NAXIS2, opts, i =>
					{
						ulong[] row = new ulong[(TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i])];
						int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeindex][1, i];
						byte[] ui64 = new byte[8];

						for (int j = 0; j < row.Length; j++)
						{
							ui64[7] = HEAPDATA[pos];
							ui64[6] = HEAPDATA[pos + 1];
							ui64[5] = HEAPDATA[pos + 2];
							ui64[4] = HEAPDATA[pos + 3];
							ui64[3] = HEAPDATA[pos + 4];
							ui64[2] = HEAPDATA[pos + 5];
							ui64[1] = HEAPDATA[pos + 6];
							ui64[0] = HEAPDATA[pos + 7];
							row[j] = MapLongToUlong(BitConverter.ToInt64(ui64, 0));
							pos += 8;
						}
						arrya[i] = row;
					});
					return arrya;
				}

				case TypeCode.Int32:
				{
					int[][] arrya = new int[(NAXIS2)][];
					Parallel.For(0, NAXIS2, opts, i =>
					{
						int[] row = new int[(TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i])];
						int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeindex][1, i];
						byte[] int32 = new byte[4];

						for (int j = 0; j < row.Length; j++)
						{
							int32[3] = HEAPDATA[pos];
							int32[2] = HEAPDATA[pos + 1];
							int32[1] = HEAPDATA[pos + 2];
							int32[0] = HEAPDATA[pos + 3];
							row[j] = BitConverter.ToInt32(int32, 0);
							pos += 4;
						}
						arrya[i] = row;
					});
					return arrya;
				}

				case TypeCode.UInt32:
				{
					uint[][] arrya = new uint[(NAXIS2)][];
					Parallel.For(0, NAXIS2, opts, i =>
					{
						uint[] row = new uint[(TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i])];
						int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeindex][1, i];
						byte[] uint32 = new byte[4];

						for (int j = 0; j < row.Length; j++)
						{
							uint32[3] = HEAPDATA[pos];
							uint32[2] = HEAPDATA[pos + 1];
							uint32[1] = HEAPDATA[pos + 2];
							uint32[0] = HEAPDATA[pos + 3];
							row[j] = MapIntToUint(BitConverter.ToInt32(uint32, 0));
							pos += 4;
						}
						arrya[i] = row;
					});
					return arrya;
				}

				case TypeCode.Int16:
				{
					short[][] arrya = new short[(NAXIS2)][];
					Parallel.For(0, NAXIS2, opts, i =>
					{
						short[] row = new short[(TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i])];
						int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeindex][1, i];
						byte[] int16 = new byte[(2)];

						for (int j = 0; j < row.Length; j++)
						{
							int16[1] = HEAPDATA[pos];
							int16[0] = HEAPDATA[pos + 1];
							row[j] = BitConverter.ToInt16(int16, 0);
							pos += 2;
						}
						arrya[i] = row;
					});
					return arrya;
				}

				case TypeCode.UInt16:
				{
					ushort[][] arrya = new ushort[(NAXIS2)][];
					Parallel.For(0, NAXIS2, opts, i =>
					{
						ushort[] row = new ushort[(TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i])];
						int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeindex][1, i];
						byte[] uint16 = new byte[2];

						for (int j = 0; j < row.Length; j++)
						{
							uint16[1] = HEAPDATA[pos];
							uint16[0] = HEAPDATA[pos + 1];
							row[j] = MapShortToUshort(BitConverter.ToInt16(uint16, 0));
							pos += 2;
						}
						arrya[i] = row;
					});
					return arrya;
				}

				case TypeCode.SByte:
				{
					sbyte[][] arrya = new sbyte[(NAXIS2)][];
					Parallel.For(0, NAXIS2, opts, i =>
					{
						sbyte[] row = new sbyte[(TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i])];
						int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeindex][1, i];

						for (int j = 0; j < row.Length; j++)
							row[j] = (sbyte)HEAPDATA[pos + j];
						arrya[i] = row;
					});
					return arrya;
				}

				case TypeCode.Byte:
				{
					byte[][] arrya = new byte[(NAXIS2)][];
					Parallel.For(0, NAXIS2, opts, i =>
					{
						byte[] row = new byte[(TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i])];
						int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeindex][1, i];

						for (int j = 0; j < row.Length; j++)
							row[j] = (byte)HEAPDATA[pos + j];
						arrya[i] = row;
					});
					return arrya;
				}

				case TypeCode.Boolean:
				{
					bool[][] arrya = new bool[(NAXIS2)][];
					Parallel.For(0, NAXIS2, opts, i =>
					{
						bool[] row = new bool[(TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i])];
						int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeindex][1, i];

						for (int j = 0; j < row.Length; j++)
							row[j] = Convert.ToBoolean(HEAPDATA[pos + j]);
						arrya[i] = row;
					});
					return arrya;
				}

				case TypeCode.Char:
				{
					string[] arrya = new string[(NAXIS2)];
					Parallel.For(0, NAXIS2, opts, i =>
					{
						arrya[i] = System.Text.Encoding.ASCII.GetString(HEAPDATA, TTYPEHEAPARRAYNELSPOS[ttypeindex][1, i], TTYPEHEAPARRAYNELSPOS[ttypeindex][0, i]);
					});
					return arrya;
				}

				default:
				{
					throw new Exception("Unrecognized TypeCode: '" + objectTypeCode.ToString() + "'");
				}
			}
		}

		private int[,] GETHEAPTTYPENELSPOS(int ttypeindex)
		{
			int byteoffset = 0;
			for (int i = 0; i < ttypeindex; i++)
				byteoffset += TBYTES[i];

			int[,] result = new int[2, NAXIS2];

			if (TFORMS[ttypeindex].Contains("P"))
			{
				for (int i = 0; i < NAXIS2; i++)
				{
					int cc = byteoffset + i * NAXIS1;
					byte[] int32 = new byte[4];
					int32[3] = BINTABLE[cc];
					int32[2] = BINTABLE[cc + 1];
					int32[1] = BINTABLE[cc + 2];
					int32[0] = BINTABLE[cc + 3];
					result[0, i] = BitConverter.ToInt32(int32, 0);//nelements
					int32[3] = BINTABLE[cc + 4];
					int32[2] = BINTABLE[cc + 5];
					int32[1] = BINTABLE[cc + 6];
					int32[0] = BINTABLE[cc + 7];
					result[1, i] = BitConverter.ToInt32(int32, 0);//position
				}
			}
			else//"Q"
			{
				for (int i = 0; i < NAXIS2; i++)
				{
					int cc = byteoffset + i * NAXIS1;
					byte[] i64 = new byte[8];
					i64[7] = BINTABLE[cc];
					i64[6] = BINTABLE[cc + 1];
					i64[5] = BINTABLE[cc + 2];
					i64[4] = BINTABLE[cc + 3];
					i64[3] = BINTABLE[cc + 4];
					i64[2] = BINTABLE[cc + 5];
					i64[1] = BINTABLE[cc + 6];
					i64[0] = BINTABLE[cc + 7];
					result[0, i] = (int)BitConverter.ToInt64(i64, 0);//nelements
					i64[7] = BINTABLE[cc + 8];
					i64[6] = BINTABLE[cc + 9];
					i64[5] = BINTABLE[cc + 10];
					i64[4] = BINTABLE[cc + 11];
					i64[3] = BINTABLE[cc + 12];
					i64[2] = BINTABLE[cc + 13];
					i64[1] = BINTABLE[cc + 14];
					i64[0] = BINTABLE[cc + 15];
					result[1, i] = (int)BitConverter.ToInt64(i64, 0);//position
				}
			}
			return result;
		}

		private void REMOVEHEAPTTYPE(int ttypeindex)
		{
			int startpos = TTYPEHEAPARRAYNELSPOS[ttypeindex][1, 0];
			int endpos = TTYPEHEAPARRAYNELSPOS[ttypeindex][1, NAXIS2 - 1];
			if (TTYPEISCOMPLEX[ttypeindex])
				endpos += TTYPEHEAPARRAYNELSPOS[ttypeindex][0, NAXIS2 - 1] * TYPECODETONBYTES(HEAPTCODES[ttypeindex]) * 2;
			else
				endpos += TTYPEHEAPARRAYNELSPOS[ttypeindex][0, NAXIS2 - 1] * TYPECODETONBYTES(HEAPTCODES[ttypeindex]);

			byte[] prepend = new byte[(startpos)];
			for (int i = 0; i < prepend.Length; i++)
				prepend[i] = HEAPDATA[i];

			byte[] append = new byte[(HEAPDATA.Length - endpos)];
			for (int i = 0; i < append.Length; i++)
				append[i] = HEAPDATA[endpos + i];

			HEAPDATA = new byte[(prepend.Length + append.Length)];
			for (int i = 0; i < prepend.Length; i++)
				HEAPDATA[i] = prepend[i];
			for (int i = prepend.Length; i < append.Length + prepend.Length; i++)
				HEAPDATA[i] = prepend[i - prepend.Length];
		}

		#endregion

		#region PROPERTIES
		/// <summary>NumberOfTableEntries reports the number of fields in the extension, i.e. the TFIELDS value.</summary>
		public int NumberOfTableEntriesTFIELDS
		{
			get { return TFIELDS; }
		}

		/// <summary>TableDataTypes reports the number of columns or repeats in each table entry. Variable repeat (heap data) entries only report 1...use GetTTYPERowRepeatsHeapEntry to get the number of repeats for a given row.</summary>
		public int[] TableDataRepeats
		{
			get { return TREPEATS; }
		}

		/// <summary>TableDataLabels reports the name of each table entry, i.e. the TTYPE values.</summary>
		public string[] TableDataLabelsTTYPE
		{
			get { return TTYPES; }
		}

		/// <summary>ExtensionEntryUnits reports the units of each table entry, i.e. the TUNITS values.</summary>
		public string[] ExtensionEntryUnits
		{
			get { return TUNITS; }
		}

		/// <summary>Return the binary table header as an array of Strings for each line of the header.</summary>
		public string[] Header
		{
			get { return HEADER; }
		}

		/// <summary>Return the name of the extension.</summary>
		public string ExtensionNameEXTNAME
		{
			get { return EXTENSIONNAME; }
		}

		/// <summary>Return the width, in bytes, of the table.</summary>
		public int Naxis1
		{
			get { return NAXIS1; }
		}

		/// <summary>Return the height, number of rows, of the table.</summary>
		public int Naxis2
		{
			get { return NAXIS2; }
		}

		/// <summary>Return the BINTABLE data block, excluding header, as a (unsigned) byte array.</summary>
		public byte[] BINTABLEByteArray
		{
			get { return BINTABLE; }
		}
		#endregion

		#region CONSTRUCTORS
		/// <summary>Create an empty FITSBinTable object. TTYPE entries may be added later via SetTTYPEEntries or AddTTYPEEntry. An extension name can be added at writetime.</summary>
		public FITSBinTable()
		{

		}

		/// <summary>Create a FITSBinTable object from an existing extension.</summary>
		/// <param name="fileName">The full path filename.</param>
		/// <param name="extensionName">The BINTABLE EXTNAME name of the extension. If an empty string is passed the first nameless extension will be found, if one exists.</param>
		public FITSBinTable(string fileName, string extensionName)
		{
			FileStream fs = new FileStream(fileName, FileMode.Open);
			bool hasext;
			ArrayList header = null;
			if (!FITSFILEOPS.SCANPRIMARYUNIT(fs, true, ref header, out hasext) || !hasext)
			{
				fs.Close();
				if (!hasext)
					throw new Exception("File '" + fileName + "'  indicates no extensions present.");
				else
					throw new Exception("File '" + fileName + "' not formatted as FITS file.");
			}

			header = new ArrayList();
			long extensionstartposition, extensionendposition, tableendposition, pcount, theap;
			if (!FITSFILEOPS.SEEKEXTENSION(fs, "BINTABLE", extensionName, ref header, out extensionstartposition, out extensionendposition, out tableendposition, out pcount, out theap))
			{
				fs.Close();
				throw new Exception("Could not find BINTABLE with name '" + extensionName + "'");
			}

			FILENAME = fileName;
			EXTENSIONNAME = extensionName;

			BINTABLE = new byte[((int)(tableendposition - fs.Position))];

			fs.Read(BINTABLE, 0, BINTABLE.Length);

			if (pcount != 0)
			{
				fs.Position = fs.Position + theap - BINTABLE.Length;
				HEAPDATA = new byte[((int)(tableendposition + pcount - fs.Position))];
				fs.Read(HEAPDATA, 0, HEAPDATA.Length);
			}

			fs.Close();

			EATRAWBINTABLEHEADER(header);
		}

		#endregion

		#region MEMBERS
		/// <summary>Check if a TTYPE entry exists within the bintable.</summary>
		/// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		public bool TTYPEEntryExists(string ttypeEntry)
		{
			for (int i = 0; i < TTYPES.Length; i++)
				if (TTYPES[i] == ttypeEntry)
					return true;
			return false;
		}

		/// <summary>Return a binary table entry as a double 1-D array, assuming it is a single colunmn entry. If the entry has more than one column, use the overload function to get its dimensions.</summary>
		/// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		public double[] GetTTYPEEntry(string ttypeEntry)
		{
			int ttypeindex = -1;
			for (int i = 0; i < TTYPES.Length; i++)
				if (TTYPES[i] == ttypeEntry)
				{
					ttypeindex = i;
					break;
				}

			if (ttypeindex == -1)
			{
				throw new Exception("Extension Entry TTYPE Label wasn't found: '" + ttypeEntry + "'");
			}

			if (TTYPEISCOMPLEX[ttypeindex] || TTYPEISHEAPARRAYDESC[ttypeindex])
			{
				string type;
				if (TTYPEISCOMPLEX[ttypeindex])
					type = "COMPLEX";
				else
					type = "HEAP ARRAY DESCRIPTOR";

				throw new Exception("Cannot return entry '" + ttypeEntry + "' as a vector because it is " + type + ". Use an overload to get as an Object with dimensions returned to user.");
			}

			int[] dimNElements;
			return GetTTYPEEntry(ttypeEntry, out dimNElements);
		}

		/// <summary>Return a binary table entry as a double 1-D array.</summary>
		/// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		/// <param name="dimNElements">A vector to return the number of elements along each dimension of the Object. 
		/// <br />Contains the TDIM key values for an n &gt; 2 dimensional array, otherwise contains the instances (repeats, i.e. columns) and NAXIS2. Its length gives the rank of the array Object. If rank = 1 then it contains only NAXIS2.</param>
		public double[] GetTTYPEEntry(string ttypeEntry, out int[] dimNElements)
		{
			for (int i = 0; i < TTYPES.Length; i++)
				if (TTYPES[i] == ttypeEntry)
					if (TCODES[i] == TypeCode.Char)
					{
						throw new Exception("Cannot return entry '" + ttypeEntry + "' as double array because it is a char (String) array. Use an overload to get as an Object and then cast the object as a (string[])Object.");
					}

			TypeCode tcode;
			Array obj = GetTTYPEEntry(ttypeEntry, out tcode, out dimNElements);
			int rank = obj.Rank;
			int width, height;
			if (rank == 1)
			{
				width = 1;
				height = obj.Length;
			}
			else
			{
				width = obj.GetLength(0);
				height = obj.GetLength(1);
			}
			double[] result = new double[(width * height)];

			switch (tcode)
			{
				case TypeCode.Double:
				{
					if (rank == 1)
					{
						Parallel.For(0, height, y =>
						{
							result[y] = ((double[])(obj))[y];
						});
					}
					else
					{
						Parallel.For(0, height, y =>
						{
							for (int x = 0; x < width; x++)
								result[y * width + x] = ((double[,])(obj))[x, y];
						});
					}
					break;
				}

				case (TypeCode.Int64):
				{
					if (rank == 1)
					{
						Parallel.For(0, height, y =>
						{
							result[y] = (double)((long[])(obj))[y];
						});
					}
					else
					{
						Parallel.For(0, height, y =>
						{
							for (int x = 0; x < width; x++)
								result[y * width + x] = (double)((long[,])(obj))[x, y];
						});
					}
					break;
				}

				case (TypeCode.UInt64):
				{
					if (rank == 1)
					{
						Parallel.For(0, height, y =>
						{
							result[y] = (double)((ulong[])(obj))[y];
						});
					}
					else
					{
						Parallel.For(0, height, y =>
						{
							for (int x = 0; x < width; x++)
								result[y * width + x] = (double)((ulong[,])(obj))[x, y];
						});
					}
					break;
				}

				case TypeCode.Single:
				{
					if (rank == 1)
					{
						Parallel.For(0, height, y =>
						{
							result[y] = (double)((float[])(obj))[y];
						});
					}
					else
					{
						Parallel.For(0, height, y =>
						{
							for (int x = 0; x < width; x++)
								result[y * width + x] = (double)((float[,])(obj))[x, y];
						});
					}
					break;
				}

				case TypeCode.UInt32:
				{
					if (rank == 1)
					{
						Parallel.For(0, height, y =>
						{
							result[y] = (double)((uint[])(obj))[y];
						});
					}
					else
					{
						Parallel.For(0, height, y =>
						{
							for (int x = 0; x < width; x++)
								result[y * width + x] = (double)((uint[,])(obj))[x, y];
						});
					}
					break;
				}

				case TypeCode.Int32:
				{
					if (rank == 1)
					{
						Parallel.For(0, height, y =>
						{
							result[y] = (double)((int[])(obj))[y];
						});
					}
					else
					{
						Parallel.For(0, height, y =>
						{
							for (int x = 0; x < width; x++)
								result[y * width + x] = (double)((int[,])(obj))[x, y];
						});
					}
					break;
				}

				case TypeCode.UInt16:
				{
					if (rank == 1)
					{
						Parallel.For(0, height, y =>
						{
							result[y] = (double)((ushort[])(obj))[y];
						});
					}
					else
					{
						Parallel.For(0, height, y =>
						{
							for (int x = 0; x < width; x++)
								result[y * width + x] = (double)((ushort[,])(obj))[x, y];
						});
					}
					break;
				}

				case TypeCode.Int16:
				{
					if (rank == 1)
					{
						Parallel.For(0, height, y =>
						{
							result[y] = (double)((short[])(obj))[y];
						});
					}
					else
					{
						Parallel.For(0, height, y =>
						{
							for (int x = 0; x < width; x++)
								result[y * width + x] = (double)((short[,])(obj))[x, y];
						});
					}
					break;
				}

				case TypeCode.Byte:
				{
					if (rank == 1)
					{
						Parallel.For(0, height, y =>
						{
							result[y] = (double)((byte[])(obj))[y];
						});
					}
					else
					{
						Parallel.For(0, height, y =>
						{
							for (int x = 0; x < width; x++)
								result[y * width + x] = (double)((byte[,])(obj))[x, y];
						});
					}
					break;
				}

				case TypeCode.SByte:
				{
					if (rank == 1)
					{
						Parallel.For(0, height, y =>
						{
							result[y] = (double)((sbyte[])(obj))[y];
						});
					}
					else
					{
						Parallel.For(0, height, y =>
						{
							for (int x = 0; x < width; x++)
								result[y * width + x] = (double)((sbyte[,])(obj))[x, y];
						});
					}
					break;
				}

				case TypeCode.Boolean:
				{
					if (rank == 1)
					{
						Parallel.For(0, height, y =>
						{
							result[y] = Convert.ToDouble(((bool[])(obj))[y]);
						});
					}
					else
					{
						Parallel.For(0, height, y =>
						{
							for (int x = 0; x < width; x++)
								result[y * width + x] = Convert.ToDouble(((bool[,])(obj))[x, y]);
						});
					}
					break;
				}

				default:
					throw new Exception("Unrecognized TypeCode: '" + tcode.ToString() + "'");
			}

			return result;
		}

		/// <summary>Return a binary table entry as an Array object. Its type and rank are given to the user. If you just need a double precision array to work on, use the overload for that.</summary>
		/// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		/// <param name="objectTypeCode">The TypeCode precision of the underlying array in the object.</param>
		/// <param name="dimNElements">A vector to return the number of elements along each dimension of the Object. 
		/// <br />Contains the TDIM key values for an n &gt; 2 dimensional array, otherwise contains the instances (repeats, i.e. columns) and NAXIS2. Its length gives the rank of the array Object. If rank = 1 then it contains only NAXIS2.</param>
		public Array GetTTYPEEntry(string ttypeEntry, out TypeCode objectTypeCode, out int[] dimNElements)
		{
			int ttypeindex = -1;
			for (int i = 0; i < TTYPES.Length; i++)
				if (TTYPES[i] == ttypeEntry)
				{
					ttypeindex = i;
					break;
				}

			if (ttypeindex == -1)
			{
				throw new Exception("Extension Entry TTYPE Label wasn't found: '" + ttypeEntry + "'");
			}

			if (TTYPEISHEAPARRAYDESC[ttypeindex])//get from heap
				return GETHEAPTTYPE(ttypeindex, out objectTypeCode, out dimNElements);

			objectTypeCode = TCODES[ttypeindex];

			if (TDIMS[ttypeindex] != null)
				dimNElements = TDIMS[ttypeindex];
			else
				if (TREPEATS[ttypeindex] == 1 || TCODES[ttypeindex] == TypeCode.Char)
				dimNElements = new int[] { NAXIS2 };
			else
				dimNElements = new int[] { TREPEATS[ttypeindex], NAXIS2 };

			int byteoffset = 0;
			for (int i = 0; i < ttypeindex; i++)
				byteoffset += TBYTES[i];

			switch (TCODES[ttypeindex])
			{
				case TypeCode.Double:
				{
					if (TREPEATS[ttypeindex] == 1)
					{
						double[] vector = new double[(NAXIS2)];
						Parallel.For(0, NAXIS2, i =>
						{
							byte[] dbl = new byte[(8)];
							int currentbyte = byteoffset + i * NAXIS1;
							dbl[7] = BINTABLE[currentbyte];
							dbl[6] = BINTABLE[currentbyte + 1];
							dbl[5] = BINTABLE[currentbyte + 2];
							dbl[4] = BINTABLE[currentbyte + 3];
							dbl[3] = BINTABLE[currentbyte + 4];
							dbl[2] = BINTABLE[currentbyte + 5];
							dbl[1] = BINTABLE[currentbyte + 6];
							dbl[0] = BINTABLE[currentbyte + 7];
							vector[i] = BitConverter.ToDouble(dbl, 0);
						});
						return vector;
					}
					else
					{
						double[,] arrya = new double[TREPEATS[ttypeindex], NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							byte[] dbl = new byte[8];
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								dbl[7] = BINTABLE[currentbyte];
								dbl[6] = BINTABLE[currentbyte + 1];
								dbl[5] = BINTABLE[currentbyte + 2];
								dbl[4] = BINTABLE[currentbyte + 3];
								dbl[3] = BINTABLE[currentbyte + 4];
								dbl[2] = BINTABLE[currentbyte + 5];
								dbl[1] = BINTABLE[currentbyte + 6];
								dbl[0] = BINTABLE[currentbyte + 7];
								arrya[j, i] = BitConverter.ToDouble(dbl, 0);
								currentbyte += 8;
							}
						});
						return arrya;
					}
				}

				case TypeCode.Single:
				{
					if (TREPEATS[ttypeindex] == 1)
					{
						float[] vector = new float[(NAXIS2)];
						Parallel.For(0, NAXIS2, i =>
						{
							byte[] sng = new byte[(4)];
							int currentbyte = byteoffset + i * NAXIS1;
							sng[3] = BINTABLE[currentbyte];
							sng[2] = BINTABLE[currentbyte + 1];
							sng[1] = BINTABLE[currentbyte + 2];
							sng[0] = BINTABLE[currentbyte + 3];
							vector[i] = BitConverter.ToSingle(sng, 0);
						});
						return vector;
					}
					else
					{
						float[,] arrya = new float[TREPEATS[ttypeindex], NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							byte[] sng = new byte[(4)];
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{ 
								sng[3] = BINTABLE[currentbyte];
								sng[2] = BINTABLE[currentbyte + 1];
								sng[1] = BINTABLE[currentbyte + 2];
								sng[0] = BINTABLE[currentbyte + 3];
								arrya[j, i] = BitConverter.ToSingle(sng, 0);
								currentbyte += 4;
							}
						});
						return arrya;
					}
				}

				case (TypeCode.Int64):
				{
					if (TREPEATS[ttypeindex] == 1)
					{
						long[] vector = new long[(NAXIS2)];
						Parallel.For(0, NAXIS2, i =>
						{
							byte[] i64 = new byte[(8)];
							int currentbyte = byteoffset + i * NAXIS1;
							i64[7] = BINTABLE[currentbyte];
							i64[6] = BINTABLE[currentbyte + 1];
							i64[5] = BINTABLE[currentbyte + 2];
							i64[4] = BINTABLE[currentbyte + 3];
							i64[3] = BINTABLE[currentbyte + 4];
							i64[2] = BINTABLE[currentbyte + 5];
							i64[1] = BINTABLE[currentbyte + 6];
							i64[0] = BINTABLE[currentbyte + 7];
							vector[i] = BitConverter.ToInt64(i64, 0);
						});
						return vector;
					}
					else
					{
						long[,] arrya = new long[TREPEATS[ttypeindex], NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							byte[] i64 = new byte[(8)];
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								i64[7] = BINTABLE[currentbyte];
								i64[6] = BINTABLE[currentbyte + 1];
								i64[5] = BINTABLE[currentbyte + 2];
								i64[4] = BINTABLE[currentbyte + 3];
								i64[3] = BINTABLE[currentbyte + 4];
								i64[2] = BINTABLE[currentbyte + 5];
								i64[1] = BINTABLE[currentbyte + 6];
								i64[0] = BINTABLE[currentbyte + 7];
								arrya[j, i] = BitConverter.ToInt64(i64, 0);
								currentbyte += 8;
							}
						});
						return arrya;
					}
				}

				case (TypeCode.UInt64):
				{
					if (TREPEATS[ttypeindex] == 1)
					{
						ulong[] vector = new ulong[NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							byte[] ui64 = new byte[(8)];
							int currentbyte = byteoffset + i * NAXIS1;
							ui64[7] = BINTABLE[currentbyte];
							ui64[6] = BINTABLE[currentbyte + 1];
							ui64[5] = BINTABLE[currentbyte + 2];
							ui64[4] = BINTABLE[currentbyte + 3];
							ui64[3] = BINTABLE[currentbyte + 4];
							ui64[2] = BINTABLE[currentbyte + 5];
							ui64[1] = BINTABLE[currentbyte + 6];
							ui64[0] = BINTABLE[currentbyte + 7];
							vector[i] = MapLongToUlong(BitConverter.ToInt64(ui64, 0));
						});
						return vector;
					}
					else
					{
						ulong[,] arrya = new ulong[TREPEATS[ttypeindex], NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							byte[] ui64 = new byte[8];
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								ui64[7] = BINTABLE[currentbyte];
								ui64[6] = BINTABLE[currentbyte + 1];
								ui64[5] = BINTABLE[currentbyte + 2];
								ui64[4] = BINTABLE[currentbyte + 3];
								ui64[3] = BINTABLE[currentbyte + 4];
								ui64[2] = BINTABLE[currentbyte + 5];
								ui64[1] = BINTABLE[currentbyte + 6];
								ui64[0] = BINTABLE[currentbyte + 7];
								arrya[j, i] = MapLongToUlong(BitConverter.ToInt64(ui64, 0));
								currentbyte += 8;
							}
						});
						return arrya;
					}
				}

				case TypeCode.UInt32:
				{
					if (TREPEATS[ttypeindex] == 1)
					{
						uint[] vector = new uint[(NAXIS2)];
						Parallel.For(0, NAXIS2, i =>
						{
							byte[] uint32 = new byte[(4)];
							int currentbyte = byteoffset + i * NAXIS1;
							uint32[3] = BINTABLE[currentbyte];
							uint32[2] = BINTABLE[currentbyte + 1];
							uint32[1] = BINTABLE[currentbyte + 2];
							uint32[0] = BINTABLE[currentbyte + 3];
							vector[i] = MapIntToUint(BitConverter.ToInt32(uint32, 0));
						});
						return vector;
					}
					else
					{
						uint[,] arrya = new uint[TREPEATS[ttypeindex], NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							byte[] uint32 = new byte[(4)];
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								uint32[3] = BINTABLE[currentbyte];
								uint32[2] = BINTABLE[currentbyte + 1];
								uint32[1] = BINTABLE[currentbyte + 2];
								uint32[0] = BINTABLE[currentbyte + 3];
								arrya[j, i] = MapIntToUint(BitConverter.ToInt32(uint32, 0));
								currentbyte += 4;
							}
						});
						return arrya;
					}
				}

				case TypeCode.Int32:
				{
					if (TREPEATS[ttypeindex] == 1)
					{
						int[] vector = new int[(NAXIS2)];
						Parallel.For(0, NAXIS2, i =>
						{
							byte[] int32 = new byte[(4)];
							int currentbyte = byteoffset + i * NAXIS1;
							int32[3] = BINTABLE[currentbyte];
							int32[2] = BINTABLE[currentbyte + 1];
							int32[1] = BINTABLE[currentbyte + 2];
							int32[0] = BINTABLE[currentbyte + 3];
							vector[i] = BitConverter.ToInt32(int32, 0);
						});
						return vector;
					}
					else
					{
						int[,] arrya = new int[TREPEATS[ttypeindex], NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							byte[] int32 = new byte[4];
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								int32[3] = BINTABLE[currentbyte];
								int32[2] = BINTABLE[currentbyte + 1];
								int32[1] = BINTABLE[currentbyte + 2];
								int32[0] = BINTABLE[currentbyte + 3];
								arrya[j, i] = BitConverter.ToInt32(int32, 0);
								currentbyte += 4;
							}
						});
						return arrya;
					}
				}

				case TypeCode.UInt16:
				{
					if (TREPEATS[ttypeindex] == 1)
					{
						ushort[] vector = new ushort[(NAXIS2)];
						Parallel.For(0, NAXIS2, i =>
						{
							byte[] uint16 = new byte[(2)];
							int currentbyte = byteoffset + i * NAXIS1;
							uint16[1] = BINTABLE[currentbyte];
							uint16[0] = BINTABLE[currentbyte + 1];
							vector[i] = MapShortToUshort(BitConverter.ToInt16(uint16, 0));
						});
						return vector;
					}
					else
					{
						ushort[,] arrya = new ushort[TREPEATS[ttypeindex], NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							byte[] uint16 = new byte[(2)];
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								uint16[1] = BINTABLE[currentbyte];
								uint16[0] = BINTABLE[currentbyte + 1];
								arrya[j, i] = MapShortToUshort(BitConverter.ToInt16(uint16, 0));
								currentbyte += 2;
							}
						});
						return arrya;
					}
				}

				case TypeCode.Int16:
				{
					if (TREPEATS[ttypeindex] == 1)
					{
						short[] vector = new short[NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							byte[] int16 = new byte[(2)];
							int currentbyte = byteoffset + i * NAXIS1;
							int16[1] = BINTABLE[currentbyte];
							int16[0] = BINTABLE[currentbyte + 1];
							vector[i] = BitConverter.ToInt16(int16, 0);
						});
						return vector;
					}
					else
					{
						short[,] arrya = new short[TREPEATS[ttypeindex], NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							byte[] int16 = new byte[(2)];
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								int16[1] = BINTABLE[currentbyte];
								int16[0] = BINTABLE[currentbyte + 1];
								arrya[j, i] = BitConverter.ToInt16(int16, 0);
								currentbyte += 2;
							}
						});
						return arrya;
					}
				}

				case TypeCode.Byte:
				{
					if (TREPEATS[ttypeindex] == 1)
					{
						byte[] vector = new byte[NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							vector[i] = (byte)BINTABLE[currentbyte];
						});
						return vector;
					}
					else
					{
						byte[,] arrya = new byte[TREPEATS[ttypeindex], NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								arrya[j, i] = (byte)BINTABLE[currentbyte];
								currentbyte += 1;
							}
						});
						return arrya;
					}
				}

				case TypeCode.SByte:
				{
					if (TREPEATS[ttypeindex] == 1)
					{
						sbyte[] vector = new sbyte[NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							vector[i] = (sbyte)(BINTABLE[currentbyte]);
						});
						return vector;
					}
					else
					{
						sbyte[,] arrya = new sbyte[TREPEATS[ttypeindex], NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								arrya[j, i] = (sbyte)(BINTABLE[currentbyte]);
								currentbyte += 1;
							}
						});
						return arrya;
					}
				}

				case TypeCode.Boolean:
				{
					if (TREPEATS[ttypeindex] == 1)
					{
						bool[] vector = new bool[(NAXIS2)];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							vector[i] = Convert.ToBoolean(BINTABLE[currentbyte]);
						});
						return vector;
					}
					else
					{
						bool[,] arrya = new bool[TREPEATS[ttypeindex], NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								arrya[j, i] = Convert.ToBoolean(BINTABLE[currentbyte]);
								currentbyte += 1;
							}
						});
						return arrya;
					}
				}

				case TypeCode.Char:
				{
					string[] vector = new string[(NAXIS2)];
					Parallel.For(0, NAXIS2, i =>
					{
						int currentbyte = byteoffset + i * NAXIS1;
						byte[] charstr = new byte[(TREPEATS[ttypeindex])];
						for (int j = 0; j < TREPEATS[ttypeindex]; j++)
						{
							charstr[j] = BINTABLE[currentbyte];
							currentbyte += 1;
						}
						vector[i] = System.Text.Encoding.ASCII.GetString(charstr);
					});
					return vector;
				}

				default:
				{
					throw new Exception("Unrecognized TypeCode: '" + TCODES[ttypeindex].ToString() + "'");
				}
			}
		}

		/// <summary>Use this to access individual elements of the table with a String return. Useful for looking at TTYPEs with multiple instances.</summary>
		/// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		/// <param name="rowindex">The row index of the column.</param>
		public string GetTTypeEntryRow(string ttypeEntry, int rowindex)
		{
			int ttypeindex = -1;
			for (int i = 0; i < TTYPES.Length; i++)
				if (TTYPES[i] == ttypeEntry)
				{
					ttypeindex = i;
					break;
				}

			if (ttypeindex == -1)
			{
				throw new Exception("Extension Entry TTYPE Label wasn't found: '" + ttypeEntry + "'");
			}

			if (!TTYPEISHEAPARRAYDESC[ttypeindex])
			{
				int objectArrayRank;
				if (TREPEATS[ttypeindex] == 1)
					objectArrayRank = 1;
				else
					objectArrayRank = 2;

				int byteoffset = 0;
				for (int i = 0; i < ttypeindex; i++)
					byteoffset += TBYTES[i];
				int currentbyte = byteoffset + rowindex * NAXIS1;
				string str = "";

				switch (TCODES[ttypeindex])
				{
					case TypeCode.Double:
					{
						byte[] dbl = new byte[(8)];
						if (objectArrayRank == 1)
						{
							dbl[7] = BINTABLE[currentbyte];
							dbl[6] = BINTABLE[currentbyte + 1];
							dbl[5] = BINTABLE[currentbyte + 2];
							dbl[4] = BINTABLE[currentbyte + 3];
							dbl[3] = BINTABLE[currentbyte + 4];
							dbl[2] = BINTABLE[currentbyte + 5];
							dbl[1] = BINTABLE[currentbyte + 6];
							dbl[0] = BINTABLE[currentbyte + 7];
							return BitConverter.ToDouble(dbl, 0).ToString();
						}
						else
						{
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j * 8;
								dbl[7] = BINTABLE[currentbyte];
								dbl[6] = BINTABLE[currentbyte + 1];
								dbl[5] = BINTABLE[currentbyte + 2];
								dbl[4] = BINTABLE[currentbyte + 3];
								dbl[3] = BINTABLE[currentbyte + 4];
								dbl[2] = BINTABLE[currentbyte + 5];
								dbl[1] = BINTABLE[currentbyte + 6];
								dbl[0] = BINTABLE[currentbyte + 7];
								str += BitConverter.ToDouble(dbl, 0).ToString() + "; ";
							}
							return str;
						}
					}

					case TypeCode.Single:
					{
						byte[] sng = new byte[(4)];
						if (objectArrayRank == 1)
						{
							sng[3] = BINTABLE[currentbyte];
							sng[2] = BINTABLE[currentbyte + 1];
							sng[1] = BINTABLE[currentbyte + 2];
							sng[0] = BINTABLE[currentbyte + 3];
							return BitConverter.ToSingle(sng, 0).ToString();
						}
						else
						{
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j * 4;
								sng[3] = BINTABLE[currentbyte];
								sng[2] = BINTABLE[currentbyte + 1];
								sng[1] = BINTABLE[currentbyte + 2];
								sng[0] = BINTABLE[currentbyte + 3];
								str += BitConverter.ToSingle(sng, 0).ToString() + "; ";
							}
							return str;
						}
					}

					case (TypeCode.Int64):
					{
						byte[] i64 = new byte[(8)];
						if (objectArrayRank == 1)
						{
							i64[7] = BINTABLE[currentbyte];
							i64[6] = BINTABLE[currentbyte + 1];
							i64[5] = BINTABLE[currentbyte + 2];
							i64[4] = BINTABLE[currentbyte + 3];
							i64[3] = BINTABLE[currentbyte + 4];
							i64[2] = BINTABLE[currentbyte + 5];
							i64[1] = BINTABLE[currentbyte + 6];
							i64[0] = BINTABLE[currentbyte + 7];
							return BitConverter.ToInt64(i64, 0).ToString();
						}
						else
						{
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j * 8;
								i64[7] = BINTABLE[currentbyte];
								i64[6] = BINTABLE[currentbyte + 1];
								i64[5] = BINTABLE[currentbyte + 2];
								i64[4] = BINTABLE[currentbyte + 3];
								i64[3] = BINTABLE[currentbyte + 4];
								i64[2] = BINTABLE[currentbyte + 5];
								i64[1] = BINTABLE[currentbyte + 6];
								i64[0] = BINTABLE[currentbyte + 7];
								str += BitConverter.ToInt64(i64, 0).ToString() + "; ";
							}
							return str;
						}
					}

					case (TypeCode.UInt64):
					{
						byte[] ui64 = new byte[(8)];
						if (objectArrayRank == 1)
						{
							ui64[7] = BINTABLE[currentbyte];
							ui64[6] = BINTABLE[currentbyte + 1];
							ui64[5] = BINTABLE[currentbyte + 2];
							ui64[4] = BINTABLE[currentbyte + 3];
							ui64[3] = BINTABLE[currentbyte + 4];
							ui64[2] = BINTABLE[currentbyte + 5];
							ui64[1] = BINTABLE[currentbyte + 6];
							ui64[0] = BINTABLE[currentbyte + 7];
							return MapLongToUlong(BitConverter.ToInt64(ui64, 0)).ToString();
						}
						else
						{
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j * 8;
								ui64[7] = BINTABLE[currentbyte];
								ui64[6] = BINTABLE[currentbyte + 1];
								ui64[5] = BINTABLE[currentbyte + 2];
								ui64[4] = BINTABLE[currentbyte + 3];
								ui64[3] = BINTABLE[currentbyte + 4];
								ui64[2] = BINTABLE[currentbyte + 5];
								ui64[1] = BINTABLE[currentbyte + 6];
								ui64[0] = BINTABLE[currentbyte + 7];
								str += (MapLongToUlong(BitConverter.ToInt64(ui64, 0))).ToString() + "; ";
							}
							return str;
						}
					}

					case TypeCode.UInt32:
					{
						byte[] uint32 = new byte[(4)];
						if (objectArrayRank == 1)
						{
							uint32[3] = BINTABLE[currentbyte];
							uint32[2] = BINTABLE[currentbyte + 1];
							uint32[1] = BINTABLE[currentbyte + 2];
							uint32[0] = BINTABLE[currentbyte + 3];
							return (MapIntToUint(BitConverter.ToInt32(uint32, 0))).ToString();
						}
						else
						{
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j * 4;
								uint32[3] = BINTABLE[currentbyte];
								uint32[2] = BINTABLE[currentbyte + 1];
								uint32[1] = BINTABLE[currentbyte + 2];
								uint32[0] = BINTABLE[currentbyte + 3];
								str += (MapIntToUint(BitConverter.ToInt32(uint32, 0))).ToString() + "; ";
							}
							return str;
						}
					}

					case TypeCode.Int32:
					{
						byte[] int32 = new byte[(4)];
						if (objectArrayRank == 1)
						{
							int32[3] = BINTABLE[currentbyte];
							int32[2] = BINTABLE[currentbyte + 1];
							int32[1] = BINTABLE[currentbyte + 2];
							int32[0] = BINTABLE[currentbyte + 3];
							return BitConverter.ToInt32(int32, 0).ToString();
						}
						else
						{

							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j * 4;
								int32[3] = BINTABLE[currentbyte];
								int32[2] = BINTABLE[currentbyte + 1];
								int32[1] = BINTABLE[currentbyte + 2];
								int32[0] = BINTABLE[currentbyte + 3];
								str += BitConverter.ToInt32(int32, 0).ToString() + "; ";
							}
							return str;
						}
					}

					case TypeCode.UInt16:
					{
						byte[] uint16 = new byte[(2)];
						if (objectArrayRank == 1)
						{
							uint16[1] = BINTABLE[currentbyte];
							uint16[0] = BINTABLE[currentbyte + 1];
							return MapShortToUshort(BitConverter.ToInt16(uint16, 0)).ToString();
						}
						else
						{
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j * 2;
								uint16[1] = BINTABLE[currentbyte];
								uint16[0] = BINTABLE[currentbyte + 1];
								str += (MapShortToUshort(BitConverter.ToInt16(uint16, 0))).ToString() + "; ";
							}
							return str;
						}
					}

					case TypeCode.Int16:
					{
						byte[] int16 = new byte[(2)];
						if (objectArrayRank == 1)
						{

							int16[1] = BINTABLE[currentbyte];
							int16[0] = BINTABLE[currentbyte + 1];
							return BitConverter.ToInt16(int16, 0).ToString();
						}
						else
						{
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j * 2;
								int16[1] = BINTABLE[currentbyte];
								int16[0] = BINTABLE[currentbyte + 1];
								str += BitConverter.ToInt16(int16, 0).ToString() + "; ";
							}
							return str;
						}
					}

					case TypeCode.Byte:
					{
						if (objectArrayRank == 1)
						{
							byte ret = (byte)BINTABLE[currentbyte];
							return ret.ToString();
						}
						else
						{
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j;
								byte ret = (byte)BINTABLE[currentbyte];
								str += ret.ToString() + "; ";
							}
							return str;
						}
					}

					case TypeCode.SByte:
					{
						if (objectArrayRank == 1)
							return ((sbyte)(BINTABLE[currentbyte])).ToString();
						else
						{
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j;
								str += ((sbyte)(BINTABLE[currentbyte])).ToString() + "; ";
							}
							return str;
						}
					}

					case TypeCode.Boolean:
					{
						if (objectArrayRank == 1)
							return Convert.ToBoolean(BINTABLE[currentbyte]).ToString();
						else
						{
							for (int j = 0; j < TREPEATS[ttypeindex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j;
								str += Convert.ToBoolean(BINTABLE[currentbyte]).ToString() + "; ";
							}
							return str;
						}
					}

					case TypeCode.Char:
					{
						return System.Text.Encoding.ASCII.GetString(BINTABLE, currentbyte, TREPEATS[ttypeindex]);
					}

					default:
					{
						throw new Exception("Unrecognized TypeCode: '" + TCODES[ttypeindex].ToString() + "'");
					}
				}
			}
			else
			{
				int currentbyte = TTYPEHEAPARRAYNELSPOS[ttypeindex][1, rowindex];
				string str = "";

				switch (HEAPTCODES[ttypeindex])
				{
					case TypeCode.Double:
					{
						byte[] dbl = new byte[(8)];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeindex][0, rowindex]; j++)
						{
							dbl[7] = HEAPDATA[currentbyte];
							dbl[6] = HEAPDATA[currentbyte + 1];
							dbl[5] = HEAPDATA[currentbyte + 2];
							dbl[4] = HEAPDATA[currentbyte + 3];
							dbl[3] = HEAPDATA[currentbyte + 4];
							dbl[2] = HEAPDATA[currentbyte + 5];
							dbl[1] = HEAPDATA[currentbyte + 6];
							dbl[0] = HEAPDATA[currentbyte + 7];
							str += BitConverter.ToDouble(dbl, 0).ToString() + "; ";
							currentbyte += 8;
						}
						return str;
					}

					case TypeCode.Single:
					{
						byte[] sng = new byte[(4)];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeindex][0, rowindex]; j++)
						{
							sng[3] = HEAPDATA[currentbyte];
							sng[2] = HEAPDATA[currentbyte + 1];
							sng[1] = HEAPDATA[currentbyte + 2];
							sng[0] = HEAPDATA[currentbyte + 3];
							str += BitConverter.ToSingle(sng, 0).ToString() + "; ";
							currentbyte += 4;
						}
						return str;
					}

					case (TypeCode.Int64):
					{
						byte[] i64 = new byte[(8)];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeindex][0, rowindex]; j++)
						{
							i64[7] = HEAPDATA[currentbyte];
							i64[6] = HEAPDATA[currentbyte + 1];
							i64[5] = HEAPDATA[currentbyte + 2];
							i64[4] = HEAPDATA[currentbyte + 3];
							i64[3] = HEAPDATA[currentbyte + 4];
							i64[2] = HEAPDATA[currentbyte + 5];
							i64[1] = HEAPDATA[currentbyte + 6];
							i64[0] = HEAPDATA[currentbyte + 7];
							str += BitConverter.ToInt64(i64, 0).ToString() + "; ";
							currentbyte += 8;
						}
						return str;
					}

					case (TypeCode.UInt64):
					{
						byte[] ui64 = new byte[(8)];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeindex][0, rowindex]; j++)
						{
							ui64[7] = HEAPDATA[currentbyte];
							ui64[6] = HEAPDATA[currentbyte + 1];
							ui64[5] = HEAPDATA[currentbyte + 2];
							ui64[4] = HEAPDATA[currentbyte + 3];
							ui64[3] = HEAPDATA[currentbyte + 4];
							ui64[2] = HEAPDATA[currentbyte + 5];
							ui64[1] = HEAPDATA[currentbyte + 6];
							ui64[0] = HEAPDATA[currentbyte + 7];
							str += MapLongToUlong(BitConverter.ToInt64(ui64, 0)).ToString() + "; ";
							currentbyte += 8;
						}
						return str;
					}

					case TypeCode.Int32:
					{
						byte[] int32 = new byte[(4)];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeindex][0, rowindex]; j++)
						{
							int32[3] = HEAPDATA[currentbyte];
							int32[2] = HEAPDATA[currentbyte + 1];
							int32[1] = HEAPDATA[currentbyte + 2];
							int32[0] = HEAPDATA[currentbyte + 3];
							str += BitConverter.ToInt32(int32, 0).ToString() + "; ";
							currentbyte += 4;
						}
						return str;
					}

					case TypeCode.UInt32:
					{
						byte[] uint32 = new byte[(4)];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeindex][0, rowindex]; j++)
						{
							uint32[3] = HEAPDATA[currentbyte];
							uint32[2] = HEAPDATA[currentbyte + 1];
							uint32[1] = HEAPDATA[currentbyte + 2];
							uint32[0] = HEAPDATA[currentbyte + 3];
							str += MapIntToUint(BitConverter.ToInt32(uint32, 0)).ToString() + "; ";
							currentbyte += 4;
						}
						return str;
					}

					case TypeCode.Int16:
					{
						byte[] int16 = new byte[(2)];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeindex][0, rowindex]; j++)
						{
							int16[1] = HEAPDATA[currentbyte];
							int16[0] = HEAPDATA[currentbyte + 1];
							str += BitConverter.ToInt16(int16, 0).ToString() + "; ";
							currentbyte += 2;
						}
						return str;
					}

					case TypeCode.UInt16:
					{
						byte[] uint16 = new byte[(2)];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeindex][0, rowindex]; j++)
						{
							uint16[1] = HEAPDATA[currentbyte];
							uint16[0] = HEAPDATA[currentbyte + 1];
							str += MapShortToUshort(BitConverter.ToInt16(uint16, 0)).ToString() + "; ";
							currentbyte += 2;
						}
						return str;
					}

					case TypeCode.SByte:
					{
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeindex][0, rowindex]; j++)
							str += ((sbyte)(HEAPDATA[currentbyte + j])).ToString() + "; ";
						return str;
					}

					case TypeCode.Byte:
					{
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeindex][0, rowindex]; j++)
							str += ((byte)HEAPDATA[currentbyte + j]).ToString() + "; ";
						return str;
					}

					case TypeCode.Boolean:
					{
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeindex][0, rowindex]; j++)
							str += Convert.ToBoolean(HEAPDATA[currentbyte + j]).ToString() + "; ";
						return str;
					}

					case TypeCode.Char:
					{
						return System.Text.Encoding.ASCII.GetString(HEAPDATA, TTYPEHEAPARRAYNELSPOS[ttypeindex][1, rowindex], TTYPEHEAPARRAYNELSPOS[ttypeindex][0, rowindex]);
					}

					default:
					{
						throw new Exception("Unrecognized TypeCode: '" + HEAPTCODES[ttypeindex].ToString() + "'");
					}
				}
			}
		}

		/// <summary>Remove one of the entries from the binary table. Inefficient if the table has a very large number of entries with very large number of elements. Operates on heap-stored data if required.</summary>
		/// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		public void RemoveTTYPEEntry(string ttypeEntry)
		{
			int ttypeindex = -1;
			for (int i = 0; i < TTYPES.Length; i++)
				if (TTYPES[i] == ttypeEntry)
				{
					ttypeindex = i;
					break;
				}

			if (ttypeindex == -1)
			{
				throw new Exception("Extension Entry TTYPE wasn't found: '" + ttypeEntry + "'");
			}

			Array[] newEntryDataObjs = new Array[(TFIELDS - 1)];
			string[] newTTYPES = new string[(TFIELDS - 1)];
			string[] newTFORMS = new string[(TFIELDS - 1)];
			string[] newTUNITS = new string[(TFIELDS - 1)];
			int[] newTBYTES = new int[(TFIELDS - 1)];
			int[] newTREPEATS = new int[(TFIELDS - 1)];
			TypeCode[] newTCODES = new TypeCode[(TFIELDS - 1)];
			int[][] newTDIMS = new int[(TFIELDS - 1)][];
			bool[] newTTYPEISCOMPLEX = new bool[(TFIELDS - 1)];
			bool[] newTTYPEISHEAPARRAYDESC = new bool[(TFIELDS - 1)];
			TypeCode[] newHEAPTCODES = new TypeCode[(TFIELDS - 1)];
			int[][,] newTTYPEHEAPARRAYNELSPOS = new int[(TFIELDS - 1)][,];

			int c = 0;
			for (int i = 0; i < TFIELDS; i++)
				if (i == ttypeindex)
				{
					if (TTYPEISHEAPARRAYDESC[ttypeindex])
						REMOVEHEAPTTYPE(ttypeindex);
				}
				else
				{
					TypeCode code;
					int[] dimnelements;
					newEntryDataObjs[c] = this.GetTTYPEEntry(TTYPES[i], out code, out dimnelements);
					newTTYPES[c] = TTYPES[i];
					newTFORMS[c] = TFORMS[i];
					newTUNITS[c] = TUNITS[i];
					newTBYTES[c] = TBYTES[i];
					newTREPEATS[c] = TREPEATS[i];
					newTCODES[c] = TCODES[i];
					newTDIMS[c] = TDIMS[i];
					newTTYPEISCOMPLEX[c] = TTYPEISCOMPLEX[i];
					newTTYPEISHEAPARRAYDESC[c] = TTYPEISHEAPARRAYDESC[i];
					newHEAPTCODES[c] = HEAPTCODES[i];
					newTTYPEHEAPARRAYNELSPOS[c] = TTYPEHEAPARRAYNELSPOS[i];
					c++;
				}

			TFIELDS--;
			TTYPES = newTTYPES;
			TFORMS = newTFORMS;
			TUNITS = newTUNITS;
			TBYTES = newTBYTES;
			TREPEATS = newTREPEATS;
			TCODES = newTCODES;
			TDIMS = newTDIMS;
			TTYPEISCOMPLEX = newTTYPEISCOMPLEX;
			TTYPEISHEAPARRAYDESC = newTTYPEISHEAPARRAYDESC;
			HEAPTCODES = newHEAPTCODES;
			TTYPEHEAPARRAYNELSPOS = newTTYPEHEAPARRAYNELSPOS;

			MAKEBINTABLEBYTEARRAY(newEntryDataObjs);
		}

		// /// <summary>Add a vector or 2D array TTYPE entry of either numeric or string values to the binary table.</summary>
		// /// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		// /// <param name="replaceIfExists">Replace the TTYPE entry if it already exists. If it already exists and the option is given to not replace, then an exception will be thrown.</param>
		// /// <param name="entryUnits">The TUNITS physical units of the values of the array. Pass empty string if not required.</param>
		// /// <param name="entryArray">The vector or 2D array to enter into the table.</param>
		//public void AddTTYPEEntry(string ttypeEntry, bool replaceIfExists, string entryUnits, Array entryArray)
		//{
		//	if (entryArray.Rank >= 3)
		//		throw new Exception("Error: Cannot add an array of rank &gt;= 3 to a FITS binary table.");

		//	int[] dimns = null;
		//	AddTTYPEEntry(ttypeEntry, replaceIfExists, entryUnits, entryArray, dimns, false, false);
		//}

		public enum EntryArrayFormat
		{
			/// <summary>
			/// The entryArray is a 1-dimensional array of numeric or string values, or is a 2-dimensional array of numeric values only. If entryArray is a 1-D array of strings, all strings must be the same length, otherwise specify IsHeapVariableRepeatRows.
			/// </summary>
			Default,

			/// <summary>
			/// The entryArray is a 2-dimensional array of complex (real, imaginary) numeric value pairings, of either single or double floating point precision. The width of the array can be greater than two, but must be an even number given the pairings.
			/// </summary>
			IsComplex,

			/// <summary>
			/// The entryArray is an array containing variable-length vector arrays of numeric values, or it is an array of variable-length strings, either of which must therefore be stored in the heap area.
			/// </summary>
			IsHeapVariableRepeatRows,

			/// <summary>
			/// The entryArray is an array containing variable-length vector arrays of complex value pairings (of either single or double floating point precision) which therefore must be stored in the heap area. The length of each vector of the array must be an even number given the pairings.
			/// </summary>
			IsHeapComplexVariableRepeatRows,

			/// <summary>
			/// The entryArray is a vector or 2D array but which is to be interpreted as an n-dimensional array of rank r &gt;= 3, for which the TDIM values will be provided giving the number of elements along each dimension of the array, which will be written into the header as the TDIMn keys.
			/// <br />The optional argument for tdims *MUST* be provided with this option.
			/// </summary>
			IsNDimensional
		}

		/// <summary>
		/// Add a vector, 2D array, or an array of variable-length arrays, of numeric, complex numeric, string values, or rank &gt;= 3, to the binary table as a TTYPE entry.
		/// <br />The entryArray must be the same neight NAXIS2 as the previous entry additions, if any.
		/// <br />If entryArray is a variable repeat heap array then the entry must be supplied as an array of arrays, or an array of Strings; if complex each subarray must contain an even pairing of values.
		/// <br />If adding a complex number array to the binary table, the entryArray must be either single or double floating point, and must be a factor of two columns repeats where the 1st and odd numbered columns are the spatial part, and the 2nd and even numbered columns are the temporal part.
		/// <br />If entryArray is to be interpreted as rank &gt;= 3, then array dimensions need to be supplied with the optional tdim argument after the EntryArrayFormat.IsNDimensional option. If entries already exist then the user must have formatted the entryArray to match the existing table height NAXIS2.
		/// </summary>
		/// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		/// <param name="replaceIfExists">Replace the TTYPE entry if it already exists. If it already exists and the option is given to not replace, then an exception will be thrown.</param>
		/// <param name="entryUnits">The TUNITS physical units of the values of the array. Pass empty string if not required.</param>
		/// <param name="entryArray">The array to enter into the table.</param>
		///<param name="arrayFormat">Specify entryArray format for non-trivial cases.</param>
		///<param name="tdims">Specify the array dimensions for  rank r &gt;= 3, to be written as the TDIMS keywords.</param>
		// /// <param name="tdims">A vector giving the number of elements along each dimension of the array, to write as the TDIM key for the entry IF the entry is n &gt;= 3 dimensional; pass null if the entry is not n &gt;= 3 dimensional.</param>
		// /// <param name="isComplex">A boolean to set whether the array should be interpreted as complex value pairings.</param>
		// /// <param name="addAsHeapVarRepeatArray">A boolean to set whether to save the array as a variable repeat array in the heap area. If true, the entryArray must be an array of arrays or an array of Strings.</param>
		public void AddTTYPEEntry(string ttypeEntry, bool replaceIfExists, string entryUnits, Array entryArray/*, int[]? tdims, bool isComplex, bool addAsHeapVarRepeatArray*/, EntryArrayFormat arrayFormat = EntryArrayFormat.Default, int[]? tdims = null)
		{
			bool isComplex = false;
			if (arrayFormat == EntryArrayFormat.IsComplex)
				isComplex = true;
			bool addAsHeapVarRepeatArray = false;
			if (arrayFormat == EntryArrayFormat.IsHeapVariableRepeatRows || arrayFormat == EntryArrayFormat.IsHeapComplexVariableRepeatRows)
				addAsHeapVarRepeatArray = true;
			if (arrayFormat == EntryArrayFormat.IsNDimensional && tdims == null)
				throw new Exception("The tdims optional argument must be provided if the array format is n >= 3 dimensional.");

			if (entryArray.Rank >= 3)
				throw new Exception("Error: Cannot add an array of rank &gt;= 3 to a FITS binary table.");

			int ttypeindex = -1;
			if (TTYPES != null)
				for (int i = 0; i < TTYPES.Length; i++)
					if (TTYPES[i] == ttypeEntry)
					{
						ttypeindex = i;
						break;
					}
			if (ttypeindex != -1 && !replaceIfExists)
				throw new Exception("Extension Entry TTYPE '" + ttypeEntry + "' already exists, but was told to not overwrite it.");

			bool isheapvarrepeatString = false;
			if (addAsHeapVarRepeatArray)
				if (Type.GetTypeCode(entryArray.GetType().GetElementType()) == TypeCode.String)
					isheapvarrepeatString = true;

			if (isComplex && isheapvarrepeatString)
				throw new Exception("Adding a char String TTYPE but told it is numerical complex, which doesn't make sense: '" + ttypeEntry + ".");

			if (isComplex && !addAsHeapVarRepeatArray)
			{
				if (entryArray.Rank == 1)
					throw new Exception("Extension Entry TTYPE '" + ttypeEntry + "' is supposed to be complex, but has a rank of 1. A complex array must have a rank of 2 for spatial and temporal pairings.");
				if (Type.GetTypeCode((entryArray.GetType()).GetElementType()) != TypeCode.Double && Type.GetTypeCode((entryArray.GetType()).GetElementType()) != TypeCode.Single)
					throw new Exception("Extension Entry TTYPE '" + ttypeEntry + "' may only be single or double precision floating point if complex, but was " + Type.GetTypeCode((entryArray.GetType()).GetElementType()).ToString());
				if (!JPMath.IsEven(entryArray.Length))
					throw new Exception("Extension Entry TTYPE '" + ttypeEntry + "' is supposed to be complex, but is not an even pairing of spatial and temporal columns.");
			}

			if (isComplex && addAsHeapVarRepeatArray)
			{
				for (int i = 0; i < entryArray.Length; i++)
					if (!JPMath.IsEven(((Array)(entryArray.GetValue(i))).Length))
						throw new Exception("Extension Entry TTYPE '" + ttypeEntry + "' is supposed to be complex, but is not an even pairing of spatial and temporal columns.");

				if (Type.GetTypeCode((entryArray.GetValue(0)).GetType().GetElementType()) != TypeCode.Double && Type.GetTypeCode((entryArray.GetValue(0)).GetType().GetElementType()) != TypeCode.Single)
					throw new Exception("Extension Entry TTYPE '" + ttypeEntry + "' may only be single or double precision floating point if complex, but was " + Type.GetTypeCode((entryArray.GetValue(0)).GetType().GetElementType()).ToString());
			}

			if (addAsHeapVarRepeatArray && !isheapvarrepeatString)
				for (int i = 0; i < entryArray.Length; i++)
					if (((Array)(entryArray.GetValue(i))).Rank != 1)
						throw new Exception("Extension Entry TTYPE '" + ttypeEntry + "' must be an array of rank = 1 arrays. Index '" + i.ToString() + "' is rank = " + ((Array)(entryArray.GetValue(i))).Rank);

			if (!addAsHeapVarRepeatArray && Type.GetTypeCode((entryArray.GetType()).GetElementType()) == TypeCode.String)
				if (entryArray.Rank == 2)
					throw new Exception("Error: Cannot pass a 2d String array '" + ttypeEntry + "' . Only a 1D array of Strings is allowed since a string is already an array.");
				else
				{
					string[] strarr = (string[])entryArray;
					int nels = strarr[0].Length;
					for (int j = 1; j < strarr.Length; j++)
						if (strarr[j].Length != nels)
							throw new Exception("Error: String array entries '" + ttypeEntry + "' are not all the same namber of characters (repeats) long.");
				}

			if (ttypeindex != -1)//then remove it
				this.RemoveTTYPEEntry(ttypeEntry);
			else
				ttypeindex = TFIELDS;//then put the entry at the last column of the table...NB this is a zero-based index...TFIELDS will increment by one below

			//either it was an add to a blank table, or a replacement, or an additional, so these either need set for the first time, or updated
			if (TFIELDS == 0)
				if (entryArray.Rank == 1)//true for heapentry too as array of arrays
					NAXIS2 = entryArray.Length;
				else
					NAXIS2 = entryArray.GetLength(1);
			else if (entryArray.Rank == 1 && entryArray.Length != NAXIS2 || entryArray.Rank > 1 && entryArray.GetLength(1) != NAXIS2)
			{
				int naxis2;
				if (entryArray.Rank == 1)//true for heapentry too as array of arrays
					naxis2 = entryArray.Length;
				else
					naxis2 = entryArray.GetLength(1);
				throw new Exception("Error: Existing NAXIS2 = " + NAXIS2 + "; new entryArray '" + ttypeEntry + "'  NAXIS2 = " + naxis2 + ".");
			}

			TFIELDS++;
			Array[] newEntryDataObjs = new Array[(TFIELDS)];
			string[] newTTYPES = new string[(TFIELDS)];
			string[] newTFORMS = new string[(TFIELDS)];
			string[] newTUNITS = new string[(TFIELDS)];
			int[] newTBYTES = new int[(TFIELDS)];
			int[] newTREPEATS = new int[(TFIELDS)];
			TypeCode[] newTCODES = new TypeCode[TFIELDS];
			int[][] newTDIMS = new int[(TFIELDS)][];
			bool[] newTTYPEISCOMPLEX = new bool[(TFIELDS)];
			bool[] newTTYPEISHEAPARRAYDESC = new bool[(TFIELDS)];
			TypeCode[] newHEAPTCODES = new TypeCode[TFIELDS];
			int[][,] newTTYPEHEAPARRAYNELSPOS = new int[(TFIELDS)][,];

			int c = 0;
			for (int i = 0; i < TFIELDS; i++)
				if (i == ttypeindex)
				{
					int instances = 1;
					if (entryArray.Rank > 1)
						instances = entryArray.GetLength(0);

					if (!addAsHeapVarRepeatArray)
					{
						newTCODES[i] = Type.GetTypeCode((entryArray.GetType()).GetElementType());
						if (newTCODES[i] == TypeCode.String)
						{
							newTCODES[i] = TypeCode.Char;
							instances = ((string[])entryArray)[0].Length;
						}
						newTBYTES[i] = TYPECODETONBYTES(newTCODES[i]) * instances;
						if (isComplex)
							if (newTCODES[i] == TypeCode.Double)
								newTFORMS[i] = (instances / 2).ToString() + "M";
							else
								newTFORMS[i] = (instances / 2).ToString() + "C";
						else
							newTFORMS[i] = instances.ToString() + TYPECODETFORM(newTCODES[i]);
						newTREPEATS[i] = instances;
					}
					else
					{
						newTCODES[i] = TypeCode.Int32;
						newTBYTES[i] = TYPECODETONBYTES(newTCODES[i]) * 2;
						if (isheapvarrepeatString)
							newHEAPTCODES[i] = TypeCode.Char;
						else
							newHEAPTCODES[i] = Type.GetTypeCode((entryArray.GetValue(0)).GetType().GetElementType());
						newTTYPEISHEAPARRAYDESC[i] = addAsHeapVarRepeatArray;
						//newTTYPEHEAPARRAYNELSPOS[i] = null;//this gets set in MAKEBINTABLEBYTEARRAY......
						if (isComplex)
							if (newHEAPTCODES[i] == TypeCode.Double)
								newTFORMS[i] = "PM";
							else
								newTFORMS[i] = "PC";
						else
							newTFORMS[i] = "P" + TYPECODETFORM(newHEAPTCODES[i]);
						newTREPEATS[i] = 1;
						newTFORMS[i] = newTREPEATS[i] + newTFORMS[i];
					}

					newEntryDataObjs[i] = entryArray;
					newTTYPES[i] = ttypeEntry;
					newTUNITS[i] = entryUnits;
					newTDIMS[i] = tdims;
					newTTYPEISCOMPLEX[i] = isComplex;
				}
				else
				{
					TypeCode code;
					int[] dimnelements;
					newEntryDataObjs[i] = this.GetTTYPEEntry(TTYPES[c], out code, out dimnelements);
					newTTYPES[i] = TTYPES[c];
					newTFORMS[i] = TFORMS[c];
					newTUNITS[i] = TUNITS[c];
					newTBYTES[i] = TBYTES[c];
					newTREPEATS[i] = TREPEATS[c];
					newTCODES[i] = TCODES[c];
					newTDIMS[i] = TDIMS[c];
					newTTYPEISCOMPLEX[i] = TTYPEISCOMPLEX[c];
					newTTYPEISHEAPARRAYDESC[i] = TTYPEISHEAPARRAYDESC[c];
					newHEAPTCODES[i] = HEAPTCODES[c];
					newTTYPEHEAPARRAYNELSPOS[i] = TTYPEHEAPARRAYNELSPOS[c];
					c++;
				}

			TTYPES = newTTYPES;
			TFORMS = newTFORMS;
			TUNITS = newTUNITS;
			TBYTES = newTBYTES;
			TREPEATS = newTREPEATS;
			TCODES = newTCODES;
			TDIMS = newTDIMS;
			TTYPEISCOMPLEX = newTTYPEISCOMPLEX;
			TTYPEISHEAPARRAYDESC = newTTYPEISHEAPARRAYDESC;
			HEAPTCODES = newHEAPTCODES;
			TTYPEHEAPARRAYNELSPOS = newTTYPEHEAPARRAYNELSPOS;

			//either it was an add to a blank table, or a replacement, or an additional, so these either need set for the first time, or updated
			BITPIX = 8;
			NAXIS = 2;
			NAXIS1 = 0;
			for (int i = 0; i < TBYTES.Length; i++)
				NAXIS1 += TBYTES[i];

			MAKEBINTABLEBYTEARRAY(newEntryDataObjs);
		}

		/// <summary>Set the bintable full of entries all at once. More efficient than adding a large number of entries once at a time. Useful to use with a brand new and empty FITSBinTable. NOTE: THIS CLEARS ANY EXISTING ENTRIES INCLUDING THE HEAP.
		/// <br />Do not use for n &gt;= 3 dimensional, or complex, or heap variable-repeat entries.</summary>
		/// <param name="ttypeEntries">The names of the binary table extension entries, i.e. the TTYPE values.</param>
		/// <param name="entryUnits">The physical units of the values of the arrays. Pass null if not needed, or with null elements or empty elements where not required, etc.</param>
		/// <param name="entryArrays">An array of vectors or 2D arrays to enter into the table, all of which have the same height NAXIS2.</param>
		public void SetTTYPEEntries(string[] ttypeEntries, string[]? entryUnits, Array[] entryArrays)
		{
			for (int i = 0; i < entryArrays.Length; i++)
				if (entryArrays[i].Rank > 2)
					throw new Exception("Error: Do not use this function to add an n &gt; 2 dimensional array. Use AddTTYPEEntry.");
				else if (entryArrays[i].Rank == 1 && Type.GetTypeCode(entryArrays[i].GetType().GetElementType()) == TypeCode.String)
				{
					string[] strarr = (string[])entryArrays[i];
					int nels = strarr[0].Length;
					for (int j = 1; j < strarr.Length; j++)
						if (strarr[j].Length != nels)
						{
							throw new Exception("Error: String array entries '" + ttypeEntries[i] + "' are not all the same namber of characters (repeats) long. Use AddTTYPEEntry.");
						}
				}

			bool equalnaxis2 = true;
			bool stringarrayrank2 = false;
			int naxis2;
			if (entryArrays[0].Rank == 1)
				naxis2 = entryArrays[0].Length;
			else
				naxis2 = entryArrays[0].GetLength(1);
			for (int i = 1; i < entryArrays.Length; i++)
				if (entryArrays[i].Rank == 1)
				{
					if (entryArrays[i].Length != naxis2)
					{
						equalnaxis2 = false;
						break;
					}
				}
				else
				{
					if (entryArrays[i].GetLength(1) != naxis2)
					{
						equalnaxis2 = false;
						break;
					}

					if (Type.GetTypeCode(entryArrays[i].GetType().GetElementType()) == TypeCode.String)
					{
						stringarrayrank2 = true;
						break;
					}
				}
			if (!equalnaxis2)
			{
				throw new Exception("Error: all entry column heights, NAXIS2s, are not equal. Use an overload to add a variable length array to the heap.");
			}
			if (stringarrayrank2)
			{
				throw new Exception("Error: Cannot pass a 2d String array. Only a 1D array of Strings is allowed.");
			}

			TFIELDS = entryArrays.Length;
			TTYPES = ttypeEntries;
			if (entryUnits != null)
				TUNITS = entryUnits;
			else
				TUNITS = new string[(entryArrays.Length)];
			TCODES = new TypeCode[entryArrays.Length];
			TREPEATS = new int[(entryArrays.Length)];
			TFORMS = new string[(entryArrays.Length)];
			TBYTES = new int[(entryArrays.Length)];
			TTYPEISCOMPLEX = new bool[(entryArrays.Length)];
			TTYPEISHEAPARRAYDESC = new bool[(entryArrays.Length)];
			TDIMS = new int[(entryArrays.Length)][];
			HEAPTCODES = new TypeCode[entryArrays.Length];
			TTYPEHEAPARRAYNELSPOS = new int[(entryArrays.Length)][,];

			for (int i = 0; i < entryArrays.Length; i++)
			{
				TCODES[i] = Type.GetTypeCode((entryArrays[i].GetType()).GetElementType());
				if (TCODES[i] != TypeCode.String)
					if (entryArrays[i].Rank == 1)
						TREPEATS[i] = 1;
					else
						TREPEATS[i] = entryArrays[i].GetLength(0);
				else
				{
					TCODES[i] = TypeCode.Char;
					TREPEATS[i] = ((string[])entryArrays[i])[0].Length;
				}
				TFORMS[i] = TREPEATS[i].ToString() + TYPECODETFORM(TCODES[i]);
				TBYTES[i] = TYPECODETONBYTES(TCODES[i]) * TREPEATS[i];
			}

			//new table, so these either need set for the first time, or updated
			BITPIX = 8;
			NAXIS = 2;
			NAXIS1 = 0;
			for (int i = 0; i < entryArrays.Length; i++)
				NAXIS1 += TBYTES[i];
			if (entryArrays[0].Rank == 1)
				NAXIS2 = entryArrays[0].Length;
			else
				NAXIS2 = entryArrays[0].GetLength(1);

			MAKEBINTABLEBYTEARRAY(entryArrays);
			HEAPDATA = null;
		}

		/// <summary>TableDataTypes reports the .NET typecodes for each entry in the table.</summary>
		public TypeCode GetTableDataTypes(int n)
		{
			if (TTYPEISHEAPARRAYDESC[n])
				return HEAPTCODES[n];
			else
				return TCODES[n];
		}

		/// <summary>Returns wheather the TTYPE entry at the given entry index is a variable repeat array.</summary>
		public bool GetTTYPEIsHeapVariableRepeatEntry(int n)
		{
			return TTYPEISHEAPARRAYDESC[n];
		}

		/// <summary>Returns the number of elements (repeats) for a given heap entry at a given row.</summary>
		public int GetTTYPERowRepeatsHeapEntry(int ttypeindex, int row)
		{
			return TTYPEHEAPARRAYNELSPOS[ttypeindex][0, row];
		}

		/// <summary>Add an extra key to the extension header. If it is to be a COMMENT, just fill the keyValue with eighteen characters, and the keyComment with 54 characters.</summary>
		/// <param name="keyName">The name of the key.</param>
		/// <param name="keyValue">The value of the key. Pass numeric types as a string.</param>
		/// <param name="keyComment">The comment of the key.</param>
		public void AddExtraHeaderKey(string keyName, string keyValue, string keyComment)
		{
			if (EXTRAKEYS == null)
			{
				EXTRAKEYS = new string[] { keyName };
				EXTRAKEYVALS = new string[] { keyValue };
				EXTRAKEYCOMS = new string[] { keyComment };
			}
			else
			{
				string[] newkeys = new string[(EXTRAKEYS.Length + 1)];
				string[] newvals = new string[(EXTRAKEYS.Length + 1)];
				string[] newcoms = new string[(EXTRAKEYS.Length + 1)];

				for (int i = 0; i < EXTRAKEYS.Length; i++)
				{
					newkeys[i] = EXTRAKEYS[i];
					newvals[i] = EXTRAKEYVALS[i];
					newcoms[i] = EXTRAKEYCOMS[i];
				}
				newkeys[EXTRAKEYS.Length] = keyName;
				newvals[EXTRAKEYS.Length] = keyValue;
				newcoms[EXTRAKEYS.Length] = keyComment;

				EXTRAKEYS = newkeys;
				EXTRAKEYVALS = newvals;
				EXTRAKEYCOMS = newcoms;
			}
		}

		/// <summary>Get the value of an extra Key. If the key doesn't exist, an empty String is returned.</summary>
		/// <param name="keyName">The name of the key.</param>
		public string GetExtraHeaderKeyValue(string keyName)
		{
			for (int i = 0; i < EXTRAKEYS.Length; i++)
				if (keyName.Equals(EXTRAKEYS[i]))
					return EXTRAKEYVALS[i];
			return "";
		}

		/// <summary>Remove the extra header key with the given name and value.</summary>
		public void RemoveExtraHeaderKey(string keyName, string keyValue)
		{
			if (EXTRAKEYS == null)
				return;

			int keyindex = -1;

			for (int i = 0; i < EXTRAKEYS.Length; i++)
				if (EXTRAKEYS[i] == keyName && EXTRAKEYVALS[i] == keyValue)
				{
					keyindex = i;
					break;
				}

			if (keyindex == -1)
				return;

			string[] newkeys = new string[(EXTRAKEYS.Length - 1)];
			string[] newvals = new string[(EXTRAKEYS.Length - 1)];
			string[] newcoms = new string[(EXTRAKEYS.Length - 1)];
			int c = 0;
			for (int i = 0; i < EXTRAKEYS.Length; i++)
				if (i == keyindex)
					continue;
				else
				{
					newkeys[c] = EXTRAKEYS[i];
					newvals[c] = EXTRAKEYVALS[i];
					newcoms[c] = EXTRAKEYCOMS[i];
					c++;
				}
			EXTRAKEYS = newkeys;
			EXTRAKEYVALS = newvals;
			EXTRAKEYCOMS = newcoms;
		}

		/// <summary>Clear all extra header keys.</summary>
		public void RemoveAllExtraHeaderKeys()
		{
			EXTRAKEYS = null;
			EXTRAKEYVALS = null;
			EXTRAKEYCOMS = null;
		}

		/// <summary>Write the binary table into a new or existing FITS file. If the binary table already exists in an existing FITS file, it can optionally be replaced.</summary>
		/// <param name="FileName">The full file name to write the binary table into. The file can either be new or already exist.</param>
		/// <param name="ExtensionName">The EXTNAME name of the extension. Can be empty (unnamed) but this is poor practice.</param>
		/// <param name="OverWriteExtensionIfExists">If the binary table already exists it can be overwritten. If it exists and the option is given to not overwrite it, then an exception will be thrown.</param>
		public void Write(string FileName, string ExtensionName, bool OverWriteExtensionIfExists)
		{
			EXTENSIONNAME = ExtensionName;
			FILENAME = FileName;

			if (!File.Exists(FILENAME))//then write a new file, otherwise check the existing file for existing table, etc.
			{
				JPFITS.FITSImage ff = new FITSImage(FILENAME, true);
				ff.WriteImage(TypeCode.Double, true);
			}

			FileStream fs = new FileStream(FILENAME, FileMode.Open);

			bool hasext;
			ArrayList headerret = null;
			if (!FITSFILEOPS.SCANPRIMARYUNIT(fs, true, ref headerret, out hasext))
			{
				fs.Close();
				throw new Exception("File '" + FileName + "' not formatted as FITS file. Use a new file.");
			}
			if (!hasext)
			{
				fs.Position = 0;
				FITSFILEOPS.SCANPRIMARYUNIT(fs, false, ref headerret, out hasext);
				byte[] primarydataarr = new byte[((int)(fs.Length - fs.Position))];
				fs.Read(primarydataarr, 0, primarydataarr.Length);
				fs.Close();

				FITSImage ff = new FITSImage(FILENAME, null, true, false, false, false);
				int n = ff.Header.GetKeyIndex("NAXIS", false);
				if (n == -1)
				{
					throw new Exception("File '" + FileName + "' not formatted as FITS file (NAXIS not present). Use a new file.");
				}
				n = Convert.ToInt32(ff.Header.GetKeyValue("NAXIS"));
				if (n > 0)
				{
					n = ff.Header.GetKeyIndex("NAXIS" + n.ToString(), false);
					if (ff.Header.GetKeyIndex("BZERO", false) > n)
						n = ff.Header.GetKeyIndex("BZERO", false);
					if (ff.Header.GetKeyIndex("BSCALE", false) > n)
						n = ff.Header.GetKeyIndex("BSCALE", false);
				}
				else
					n = ff.Header.GetKeyIndex("NAXIS", false);
				ff.Header.SetKey("EXTEND", "T", "FITS file may contain extensions", true, n + 1);
				string[] HEADER = ff.Header.GetFormattedHeaderBlock(true, false);

				byte[] headarr = new byte[(HEADER.Length * 80)];
				for (int i = 0; i < HEADER.Length; i++)
					for (int j = 0; j < 80; j++)
						headarr[i * 80 + j] = (byte)HEADER[i][j];

				fs = new FileStream(FILENAME, FileMode.Create);
				fs.Write(headarr, 0, headarr.Length);
				fs.Write(primarydataarr, 0, primarydataarr.Length);
				fs.Close();

				fs = new FileStream(FILENAME, FileMode.Open);
				FITSFILEOPS.SCANPRIMARYUNIT(fs, true, ref headerret, out hasext);
			}

			long extensionstartposition, extensionendposition, tableendposition, pcount, theap;
			bool extensionfound = FITSFILEOPS.SEEKEXTENSION(fs, "BINTABLE", EXTENSIONNAME, ref headerret, out extensionstartposition, out extensionendposition, out tableendposition, out pcount, out theap);
			if (extensionfound && !OverWriteExtensionIfExists)
			{
				fs.Close();
				throw new Exception("ExtensionName '" + EXTENSIONNAME + "' already exists and was told to not overwrite it...");
			}

			byte[] arr_prepend;
			byte[] arr_append = null;
			if (extensionfound)
			{
				arr_prepend = new byte[((int)extensionstartposition)];
				fs.Position = 0;
				fs.Read(arr_prepend, 0, arr_prepend.Length);

				if (extensionendposition != fs.Length)//then this was not the end of the file...get the appendage data
				{
					fs.Position = extensionendposition;
					arr_append = new byte[((int)(fs.Length - extensionendposition))];
					fs.Read(arr_append, 0, arr_append.Length);
				}
				fs.Position = extensionstartposition;
			}
			else
			{
				arr_prepend = new byte[((int)fs.Length)];
				fs.Position = 0;
				fs.Read(arr_prepend, 0, arr_prepend.Length);
			}
			fs.Close();

			fs = new FileStream(FILENAME, FileMode.Create);
			if (arr_prepend != null)
				fs.Write(arr_prepend, 0, arr_prepend.Length);

			//format the header for writing
			string[] header = FORMATBINARYTABLEEXTENSIONHEADER();
			byte[] headerdata = new byte[(header.Length * 80)];

			for (int i = 0; i < header.Length; i++)
				for (int j = 0; j < 80; j++)
					headerdata[i * 80 + j] = (byte)header[i][j];

			fs.Write(headerdata, 0, headerdata.Length);
			fs.Write(BINTABLE, 0, BINTABLE.Length);

			if (HEAPDATA != null)
				fs.Write(HEAPDATA, 0, HEAPDATA.Length);
			int Tbytes = BINTABLE.Length;
			if (HEAPDATA != null)
				Tbytes += HEAPDATA.Length;

			int Nfillbytes = (int)(Math.Ceiling((double)(Tbytes) / 2880.0)) * 2880 - Tbytes;
			for (int i = 0; i < Nfillbytes; i++)
				fs.WriteByte(0);
			if (arr_append != null)
				fs.Write(arr_append, 0, arr_append.Length);
			fs.Close();
		}

		#endregion

		#region STATIC

		/// <summary>Returns an array of all binary table extension names in a FITS file. If there are no binary table extensions, returns an empty array.</summary>
		/// <param name="FileName">The full file name to read from disk.</param>
		public static string[] GetAllExtensionNames(string FileName)
		{
			return FITSFILEOPS.GETALLEXTENSIONNAMES(FileName, "BINTABLE");
		}		

		/// <summary>Remove a binary table extension from the given FITS file.</summary>
		/// <param name="FileName">The full-path file name.</param>
		/// <param name="ExtensionName">The name of the binary table extension. If the extension isn't found, an exception is thrown.</param>
		public static void RemoveExtension(string FileName, string ExtensionName)
		{
			FileStream fs = new FileStream(FileName, FileMode.Open);
			bool hasext;
			ArrayList header = null;
			if (!FITSFILEOPS.SCANPRIMARYUNIT(fs, true, ref header, out hasext) || !hasext)
			{
				fs.Close();
				if (!hasext)
					throw new Exception("File '" + FileName + "'  indicates no extensions present.");
				else
					throw new Exception("File '" + FileName + "'  not formatted as FITS file.");
			}

			long extensionstartposition, extensionendposition, tableendposition, pcount, theap;
			bool exists = FITSFILEOPS.SEEKEXTENSION(fs, "BINTABLE", ExtensionName, ref header, out extensionstartposition, out extensionendposition, out tableendposition, out pcount, out theap);
			if (!exists)
			{
				fs.Close();
				throw new Exception("Could not find BINTABLE with name '" + ExtensionName + "'");
			}

			//if here then we found the extension and can excise it given its start and end position
			byte[] arrstart = new byte[((int)extensionstartposition)];
			fs.Position = 0;
			fs.Read(arrstart, 0, arrstart.Length);

			byte[] arrend = new byte[((int)(fs.Length - extensionendposition))];
			fs.Position = extensionendposition;
			fs.Read(arrend, 0, arrend.Length);
			fs.Close();

			fs = new FileStream(FileName, FileMode.Create);
			fs.Write(arrstart, 0, arrstart.Length);
			fs.Write(arrend, 0, arrend.Length);
			fs.Close();
		}

		/// <summary>Checks if the binary extension exists inside the given FITS file.</summary>
		/// <param name="FileName">The full-path file name.</param>
		/// <param name="ExtensionName">The name of the binary table extension.</param>
		public static bool ExtensionExists(string FileName, string ExtensionName)
		{
			FileStream fs = new FileStream(FileName, FileMode.Open);
			bool hasext;
			ArrayList header = null;
			if (!FITSFILEOPS.SCANPRIMARYUNIT(fs, true, ref header, out hasext) || !hasext)
			{
				fs.Close();
				if (!hasext)
					throw new Exception("File '" + FileName + "'  indicates no extensions present.");
				else
					throw new Exception("File '" + FileName + "'  not formatted as FITS file.");
			}

			long extensionstartposition, extensionendposition, tableendposition, pcount, theap;
			bool exists = FITSFILEOPS.SEEKEXTENSION(fs, "BINTABLE", ExtensionName, ref header, out extensionstartposition, out extensionendposition, out tableendposition, out pcount, out theap);
			fs.Close();
			return exists;
		}

		#endregion
	}
}
