using System;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using System.CodeDom;
#nullable enable

namespace JPFITS
{
	/// <summary> FITSBinTable class to create, read, interact with, modify components of, and write FITS BINTABLE binary table data extensions.</summary>
	public class FITSBinTable
	{
		#region CONSTRUCTORS
		/// <summary>Create an empty FITSBinTable object. TTYPE entries may be added later via SetTTYPEEntries or AddTTYPEEntry.</summary>
		/// <param name="extensionName">The EXTNAME keyword extension name of the table. Always name your BINTABLE's.</param>
		public FITSBinTable(string extensionName)
		{
			if (extensionName == null || extensionName == "")
				throw new Exception("The EXTNAME extension name of the binary table must have a value.");

			EXTNAME = extensionName;
		}

		/// <summary>Create a FITSBinTable object from an existing extension.</summary>
		/// <param name="fileName">The full path filename.</param>
		/// <param name="extensionName">The BINTABLE EXTNAME name of the extension. If an empty string is passed the first nameless extension will be found, if one exists.</param>
		public FITSBinTable(string fileName, string extensionName)
		{
			FileStream fs = new FileStream(fileName, FileMode.Open);
			ArrayList header = null;
			if (!FITSFILEOPS.ScanPrimaryUnit(fs, true, ref header, out bool hasext) || !hasext)
			{
				fs.Close();
				if (!hasext)
					throw new Exception("File '" + fileName + "'  indicates no extensions present.");
				else
					throw new Exception("File '" + fileName + "' not formatted as FITS file.");
			}

			header = new ArrayList();
			if (!FITSFILEOPS.SeekExtension(fs, "BINTABLE", extensionName, ref header, out _, out _, out long tableendposition, out long pcount, out long theap))
			{
				fs.Close();
				throw new Exception("Could not find BINTABLE with name '" + extensionName + "'");
			}

			FILENAME = fileName;
			EXTNAME = extensionName;

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

		#region PRIVATE CLASS MEMBERS

		private int NAXIS1 = 0, NAXIS2 = 0, TFIELDS = 0;
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
		private string? EXTNAME;
		FITSHeaderKey[]? EXTRAKEYS;
		private byte[]? BINTABLE;//the table in raw byte format read from disk
		private byte[]? HEAPDATA;//the table in raw byte format read from disk			

		private void MAKEBINTABLEBYTEARRAY(Array[] ExtensionEntryData)
		{
			for (int i = 0; i < ExtensionEntryData.Length; i++)
				if (!ExtensionEntryData[i].GetType().IsArray)
					throw new Exception("Error: Object at index '" + i + "' is not an array. Stopping write.");

			MAKEHEAPBYTEARRAY(ExtensionEntryData);//will do nothing if there's no heap data

			int TBytes = NAXIS1 * NAXIS2;
			BINTABLE = new byte[TBytes];			
			bool exception = false;

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
								val = FITSFILEOPS.MapUlongToLong(((ulong[])ExtensionEntryData[j])[i]);
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
									val = FITSFILEOPS.MapUlongToLong(((ulong[,])ExtensionEntryData[j])[ii, i]);
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
								val = FITSFILEOPS.MapUintToInt(((uint[])ExtensionEntryData[j])[i]);
								BINTABLE[cc] = (byte)((val >> 24) & 0xff);
								BINTABLE[cc + 1] = (byte)((val >> 16) & 0xff);
								BINTABLE[cc + 2] = (byte)((val >> 8) & 0xff);
								BINTABLE[cc + 3] = (byte)(val & 0xff);
							}
							else
								for (int ii = 0; ii < TREPEATS[j]; ii++)
								{
									val = FITSFILEOPS.MapUintToInt(((uint[,])ExtensionEntryData[j])[ii, i]);
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
								val = FITSFILEOPS.MapUshortToShort(((ushort[])ExtensionEntryData[j])[i]);
								BINTABLE[cc] = (byte)((val >> 8) & 0xff);
								BINTABLE[cc + 1] = (byte)(val & 0xff);
							}
							else
								for (int ii = 0; ii < TREPEATS[j]; ii++)
								{
									val = FITSFILEOPS.MapUshortToShort(((ushort[,])ExtensionEntryData[j])[ii, i]);
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
							throw new Exception("Data type not recognized for writing as FITS table: '" + TCODES[j].ToString() + "'");
					}
				}
			});				
		}

		private void MAKEHEAPBYTEARRAY(Array[] ExtensionEntryData)
		{
			MAKETTYPEHEAPARRAYNELSPOS(ExtensionEntryData, out long totalbytes);
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
								val = FITSFILEOPS.MapUlongToLong(((ulong[])(((ulong[][])(ExtensionEntryData[i]))[y]))[x]);
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
								val = FITSFILEOPS.MapUintToInt(((uint[])(((uint[][])(ExtensionEntryData[i]))[y]))[x]);
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
								val = FITSFILEOPS.MapUshortToShort(((ushort[])(((ushort[][])(ExtensionEntryData[i]))[y]))[x]);
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
						throw new Exception("Data type not recognized for writing as FITS table: '" + HEAPTCODES[i].ToString() + "'");
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

		private string[] FORMATBINARYTABLEEXTENSIONHEADER(bool keysOnly)
		{
			ArrayList hkeyslist = new ArrayList();

			hkeyslist.Add(new FITSHeaderKey("XTENSION", "BINTABLE", "binary table extension"));
			hkeyslist.Add(new FITSHeaderKey("BITPIX", "8", "8-bit bytes"));
			hkeyslist.Add(new FITSHeaderKey("NAXIS", "2", "2-dimensional binary table"));
			hkeyslist.Add(new FITSHeaderKey("NAXIS1", NAXIS1.ToString(), "width of table in bytes"));
			hkeyslist.Add(new FITSHeaderKey("NAXIS2", NAXIS2.ToString(), "number of rows in table"));
			if (HEAPDATA == null)
				hkeyslist.Add(new FITSHeaderKey("PCOUNT", "0", "size of heap data area (bytes)"));
			else
				hkeyslist.Add(new FITSHeaderKey("PCOUNT", HEAPDATA.Length.ToString(), "size of heap data area (bytes)"));
			hkeyslist.Add(new FITSHeaderKey("GCOUNT", "1", "one data group"));
			hkeyslist.Add(new FITSHeaderKey("TFIELDS", TFIELDS.ToString(), "number of data fields"));
			if (EXTNAME != "")
				hkeyslist.Add(new FITSHeaderKey("EXTNAME", EXTNAME, "name of this binary table extension"));

			//KEY formats
			for (int i = 0; i < TTYPES.Length; i++)
			{
				//TFORM
				if (TTYPEISHEAPARRAYDESC[i])
					if (!TTYPEISCOMPLEX[i])
						hkeyslist.Add(new FITSHeaderKey("TFORM" + (i + 1).ToString(), TFORMS[i], (2 * TYPECODETONBYTES(TCODES[i])).ToString() + "-byte " + TYPECODESTRING(TCODES[i]) + " heap descriptor for " + TYPECODESTRING(HEAPTCODES[i])));
					else
						hkeyslist.Add(new FITSHeaderKey("TFORM" + (i + 1).ToString(), TFORMS[i], (2 * TYPECODETONBYTES(TCODES[i])).ToString() + "-byte " + TYPECODESTRING(TCODES[i]) + " heap descriptor for " + TYPECODESTRING(HEAPTCODES[i]) + " complex pair"));
				else if (TTYPEISCOMPLEX[i])
					hkeyslist.Add(new FITSHeaderKey("TFORM" + (i + 1).ToString(), TFORMS[i], (2 * TYPECODETONBYTES(TCODES[i])).ToString() + "-byte " + TYPECODESTRING(TCODES[i]) + " complex pair"));
				else
					hkeyslist.Add(new FITSHeaderKey("TFORM" + (i + 1).ToString(), TFORMS[i], TYPECODETONBYTES(TCODES[i]).ToString() + "-byte " + TYPECODESTRING(TCODES[i])));

				//TTYPE
				hkeyslist.Add(new FITSHeaderKey("TTYPE" + (i + 1), TTYPES[i], "label for field " + (i + 1).ToString()));

				//TZERO and TSCAL
				if (!TTYPEISHEAPARRAYDESC[i] && TCODES[i] == TypeCode.SByte || HEAPTCODES[i] == TypeCode.SByte)
				{
					hkeyslist.Add(new FITSHeaderKey("TZERO" + (i + 1).ToString(), "-128", "offset for signed 8-bit integers"));
					hkeyslist.Add(new FITSHeaderKey("TSCAL" + (i + 1).ToString(), "1", "data scaling"));
				}
				else if (!TTYPEISHEAPARRAYDESC[i] && TCODES[i] == TypeCode.UInt16 || HEAPTCODES[i] == TypeCode.UInt16)
				{
					hkeyslist.Add(new FITSHeaderKey("TZERO" + (i + 1).ToString(), "32768", "offset for unsigned 16-bit integers"));
					hkeyslist.Add(new FITSHeaderKey("TSCAL" + (i + 1).ToString(), "1", "data scaling"));
				}
				else if (!TTYPEISHEAPARRAYDESC[i] && TCODES[i] == TypeCode.UInt32 || HEAPTCODES[i] == TypeCode.UInt32)
				{
					hkeyslist.Add(new FITSHeaderKey("TZERO" + (i + 1).ToString(), "2147483648", "offset for unsigned 32-bit integers"));
					hkeyslist.Add(new FITSHeaderKey("TSCAL" + (i + 1).ToString(), "1", "data scaling"));
				}
				else if (!TTYPEISHEAPARRAYDESC[i] && TCODES[i] == TypeCode.UInt64 || HEAPTCODES[i] == TypeCode.UInt64)
				{
					hkeyslist.Add(new FITSHeaderKey("TZERO" + (i + 1).ToString(), "9223372036854775808", "offset for unsigned 64-bit integers"));
					hkeyslist.Add(new FITSHeaderKey("TSCAL" + (i + 1).ToString(), "1", "data scaling"));
				}

				//TUNIT
				if (TUNITS != null && TUNITS[i] != null && TUNITS[i] != "")
					hkeyslist.Add(new FITSHeaderKey("TUNIT" + (i + 1).ToString(), TUNITS[i], "physical unit of field"));

				//TDIM
				if (TDIMS[i] != null)//then it is a multi D array, and the dims should exist for this entry
				{
					string dim = "(";
					for (int j = 0; j < TDIMS[i].Length; j++)
						dim += TDIMS[i][j].ToString() + ",";
					dim = dim.Remove(dim.Length - 1) + ")";

					hkeyslist.Add(new FITSHeaderKey("TDIM" + (i + 1).ToString(), dim, "N-dim array dimensions"));
				}
			}

			//EXTRAKEYS
			if (EXTRAKEYS != null)
				for (int i = 0; i < EXTRAKEYS.Length; i++)
					hkeyslist.Add(EXTRAKEYS[i]);

			hkeyslist.Add(new FITSHeaderKey("END", "", ""));

			return (new FITSHeader(hkeyslist)).GetFormattedHeaderBlock(FITSHeader.HeaderUnitType.ExtensionBINTABLE, keysOnly);
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
			NAXIS1 = 0;
			NAXIS2 = 0;
			TFIELDS = 0;

			ArrayList extras = new ArrayList();//for possible extras

			HEADER = new string[header.Count];
			string strheaderline;
			int ttypeindex = -1;

			for (int i = 0; i < header.Count; i++)
			{
				strheaderline = (string)header[i];
				HEADER[i] = strheaderline;

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
						TFORMS = new string[TFIELDS];
						TBYTES = new int[TFIELDS];
						TREPEATS = new int[TFIELDS];
						TCODES = new TypeCode[TFIELDS];
						TUNITS = new string[TFIELDS];
						TTYPEISCOMPLEX = new bool[TFIELDS];
						TTYPEISHEAPARRAYDESC = new bool[TFIELDS];
						HEAPTCODES = new TypeCode[TFIELDS];
						TDIMS = new int[TFIELDS][];
						TTYPEHEAPARRAYNELSPOS = new int[TFIELDS][,];
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
					TBYTES[ttypeindex] = TFORMTONBYTES(TFORMS[ttypeindex], out int instances);
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
					TDIMS[ttypeindex] = new int[dimslist.Count];
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
								throw new Exception("Unrecognized TypeCode in EATRAWBINTABLEHEADER at TZERO analysis");
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
								throw new Exception("Unrecognized TypeCode in EATRAWBINTABLEHEADER at TZERO analysis");
						}
					continue;
				}

				if (strheaderline.Substring(0, 8).Trim().Equals("TSCAL" + (ttypeindex + 1).ToString()))//don't need to do anything with this
					continue;

				string key = strheaderline.Substring(0, 8).Trim();
				if (key.Length > 0 && key.Substring(0, 1) == "T" && JPMath.IsNumeric(key.Substring(key.Length - 1)))//then likely it is some other T____n field which isn't explicitly coded above...
					continue;

				//should now only be where extra keys might remain...so add them etc, but ignore END
				if (strheaderline.Substring(0, 8).Trim() != "END" && strheaderline.Substring(0, 8).Trim() != "PCOUNT" && strheaderline.Substring(0, 8).Trim() != "GCOUNT" && strheaderline.Substring(0, 8).Trim() != "EXTNAME" && strheaderline.Substring(0, 8).Trim() != "XTENSION" && strheaderline.Substring(0, 8).Trim() != "BITPIX" && strheaderline.Substring(0, 8).Trim() != "NAXIS")
					extras.Add(strheaderline);
			}

			if (extras.Count == 0)
				return;

			EXTRAKEYS = new FITSHeaderKey[extras.Count];

			for (int i = 0; i < extras.Count; i++)
				EXTRAKEYS[i] = new FITSHeaderKey((string)extras[i]);
		}

		private Array GETHEAPTTYPE(int ttypeIndex, out TypeCode entryTypeCode, out int[] entryNElements, TTYPEReturn returnType)
		{
			entryTypeCode = HEAPTCODES[ttypeIndex];

			if (TDIMS[ttypeIndex] != null)
				entryNElements = TDIMS[ttypeIndex];
			else
			{
				entryNElements = new int[NAXIS2];
				for (int i = 0; i < NAXIS2; i++)
					entryNElements[i] = TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i];
			}

			ParallelOptions opts = new ParallelOptions();
			if (NAXIS2 >= Environment.ProcessorCount)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;			

			if (returnType == TTYPEReturn.AsDouble)
			{
				switch (entryTypeCode)
				{
					case TypeCode.Double:
					{
						double[][] arrya = new double[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							double[] row = new double[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] dbl = new byte[8];

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
						double[][] arrya = new double[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							double[] row = new double[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] sng = new byte[4];

							for (int j = 0; j < row.Length; j++)
							{
								sng[3] = HEAPDATA[pos];
								sng[2] = HEAPDATA[pos + 1];
								sng[1] = HEAPDATA[pos + 2];
								sng[0] = HEAPDATA[pos + 3];
								row[j] = (double)BitConverter.ToSingle(sng, 0);
								pos += 4;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case (TypeCode.Int64):
					{
						double[][] arrya = new double[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							double[] row = new double[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] i64 = new byte[8];

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
								row[j] = (double)BitConverter.ToInt64(i64, 0);
								pos += 8;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case (TypeCode.UInt64):
					{
						double[][] arrya = new double[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							double[] row = new double[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
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
								row[j] = (double)FITSFILEOPS.MapLongToUlong(BitConverter.ToInt64(ui64, 0));
								pos += 8;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Int32:
					{
						double[][] arrya = new double[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							double[] row = new double[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] int32 = new byte[4];

							for (int j = 0; j < row.Length; j++)
							{
								int32[3] = HEAPDATA[pos];
								int32[2] = HEAPDATA[pos + 1];
								int32[1] = HEAPDATA[pos + 2];
								int32[0] = HEAPDATA[pos + 3];
								row[j] = (double)BitConverter.ToInt32(int32, 0);
								pos += 4;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.UInt32:
					{
						double[][] arrya = new double[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							double[] row = new double[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] uint32 = new byte[4];

							for (int j = 0; j < row.Length; j++)
							{
								uint32[3] = HEAPDATA[pos];
								uint32[2] = HEAPDATA[pos + 1];
								uint32[1] = HEAPDATA[pos + 2];
								uint32[0] = HEAPDATA[pos + 3];
								row[j] = (double)FITSFILEOPS.MapIntToUint(BitConverter.ToInt32(uint32, 0));
								pos += 4;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Int16:
					{
						double[][] arrya = new double[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							double[] row = new double[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] int16 = new byte[2];

							for (int j = 0; j < row.Length; j++)
							{
								int16[1] = HEAPDATA[pos];
								int16[0] = HEAPDATA[pos + 1];
								row[j] = (double)BitConverter.ToInt16(int16, 0);
								pos += 2;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.UInt16:
					{
						double[][] arrya = new double[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							double[] row = new double[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] uint16 = new byte[2];

							for (int j = 0; j < row.Length; j++)
							{
								uint16[1] = HEAPDATA[pos];
								uint16[0] = HEAPDATA[pos + 1];
								row[j] = (double)FITSFILEOPS.MapShortToUshort(BitConverter.ToInt16(uint16, 0));
								pos += 2;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.SByte:
					{
						double[][] arrya = new double[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							double[] row = new double[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];

							for (int j = 0; j < row.Length; j++)
								row[j] = (double)(sbyte)HEAPDATA[pos + j];
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Byte:
					{
						double[][] arrya = new double[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							double[] row = new double[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];

							for (int j = 0; j < row.Length; j++)
								row[j] = (double)(byte)HEAPDATA[pos + j];
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Boolean:
					{
						throw new Exception("Cannot return Boolean as a double.");
					}

					case TypeCode.Char:
					{
						throw new Exception("Cannot return Char as a double.");
					}

					default:
						throw new Exception("Unrecognized TypeCode: '" + entryTypeCode.ToString() + "'");
				}
			}

			else if (returnType == TTYPEReturn.AsString)
			{
				switch (entryTypeCode)
				{
					case TypeCode.Double:
					{
						string[][] arrya = new string[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							string[] row = new string[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] dbl = new byte[8];

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
								row[j] = BitConverter.ToDouble(dbl, 0).ToString();
								pos += 8;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Single:
					{
						string[][] arrya = new string[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							string[] row = new string[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] sng = new byte[4];

							for (int j = 0; j < row.Length; j++)
							{
								sng[3] = HEAPDATA[pos];
								sng[2] = HEAPDATA[pos + 1];
								sng[1] = HEAPDATA[pos + 2];
								sng[0] = HEAPDATA[pos + 3];
								row[j] = BitConverter.ToSingle(sng, 0).ToString();
								pos += 4;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case (TypeCode.Int64):
					{
						string[][] arrya = new string[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							string[] row = new string[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] i64 = new byte[8];

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
								row[j] = BitConverter.ToInt64(i64, 0).ToString();
								pos += 8;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case (TypeCode.UInt64):
					{
						string[][] arrya = new string[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							string[] row = new string[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
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
								row[j] = FITSFILEOPS.MapLongToUlong(BitConverter.ToInt64(ui64, 0)).ToString();
								pos += 8;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Int32:
					{
						string[][] arrya = new string[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							string[] row = new string[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] int32 = new byte[4];

							for (int j = 0; j < row.Length; j++)
							{
								int32[3] = HEAPDATA[pos];
								int32[2] = HEAPDATA[pos + 1];
								int32[1] = HEAPDATA[pos + 2];
								int32[0] = HEAPDATA[pos + 3];
								row[j] = BitConverter.ToInt32(int32, 0).ToString();
								pos += 4;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.UInt32:
					{
						string[][] arrya = new string[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							string[] row = new string[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] uint32 = new byte[4];

							for (int j = 0; j < row.Length; j++)
							{
								uint32[3] = HEAPDATA[pos];
								uint32[2] = HEAPDATA[pos + 1];
								uint32[1] = HEAPDATA[pos + 2];
								uint32[0] = HEAPDATA[pos + 3];
								row[j] = FITSFILEOPS.MapIntToUint(BitConverter.ToInt32(uint32, 0)).ToString();
								pos += 4;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Int16:
					{
						string[][] arrya = new string[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							string[] row = new string[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] int16 = new byte[2];

							for (int j = 0; j < row.Length; j++)
							{
								int16[1] = HEAPDATA[pos];
								int16[0] = HEAPDATA[pos + 1];
								row[j] = BitConverter.ToInt16(int16, 0).ToString();
								pos += 2;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.UInt16:
					{
						string[][] arrya = new string[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							string[] row = new string[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] uint16 = new byte[2];

							for (int j = 0; j < row.Length; j++)
							{
								uint16[1] = HEAPDATA[pos];
								uint16[0] = HEAPDATA[pos + 1];
								row[j] = FITSFILEOPS.MapShortToUshort(BitConverter.ToInt16(uint16, 0)).ToString();
								pos += 2;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.SByte:
					{
						string[][] arrya = new string[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							string[] row = new string[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];

							for (int j = 0; j < row.Length; j++)
								row[j] = ((sbyte)HEAPDATA[pos + j]).ToString();
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Byte:
					{
						string[][] arrya = new string[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							string[] row = new string[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];

							for (int j = 0; j < row.Length; j++)
								row[j] = ((byte)HEAPDATA[pos + j]).ToString();
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Boolean:
					{
						string[][] arrya = new string[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							string[] row = new string[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];

							for (int j = 0; j < row.Length; j++)
								row[j] = Convert.ToBoolean(HEAPDATA[pos + j]).ToString();
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Char:
					{
						string[] arrya = new string[NAXIS2];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							arrya[i] = System.Text.Encoding.ASCII.GetString(HEAPDATA, TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i], TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]);
						});
						return arrya;
					}

					default:
						throw new Exception("Unrecognized TypeCode: '" + entryTypeCode.ToString() + "'");
				}
			}

			else if (returnType == TTYPEReturn.Native)
			{
				switch (entryTypeCode)
				{
					case TypeCode.Double:
					{
						double[][] arrya = new double[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							double[] row = new double[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] dbl = new byte[8];

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
						float[][] arrya = new float[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							float[] row = new float[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] sng = new byte[4];

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
							long[] row = new long[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] i64 = new byte[8];

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
						ulong[][] arrya = new ulong[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							ulong[] row = new ulong[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
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
								row[j] = FITSFILEOPS.MapLongToUlong(BitConverter.ToInt64(ui64, 0));
								pos += 8;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Int32:
					{
						int[][] arrya = new int[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							int[] row = new int[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
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
						uint[][] arrya = new uint[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							uint[] row = new uint[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] uint32 = new byte[4];

							for (int j = 0; j < row.Length; j++)
							{
								uint32[3] = HEAPDATA[pos];
								uint32[2] = HEAPDATA[pos + 1];
								uint32[1] = HEAPDATA[pos + 2];
								uint32[0] = HEAPDATA[pos + 3];
								row[j] = FITSFILEOPS.MapIntToUint(BitConverter.ToInt32(uint32, 0));
								pos += 4;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Int16:
					{
						short[][] arrya = new short[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							short[] row = new short[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] int16 = new byte[2];

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
						ushort[][] arrya = new ushort[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							ushort[] row = new ushort[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];
							byte[] uint16 = new byte[2];

							for (int j = 0; j < row.Length; j++)
							{
								uint16[1] = HEAPDATA[pos];
								uint16[0] = HEAPDATA[pos + 1];
								row[j] = FITSFILEOPS.MapShortToUshort(BitConverter.ToInt16(uint16, 0));
								pos += 2;
							}
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.SByte:
					{
						sbyte[][] arrya = new sbyte[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							sbyte[] row = new sbyte[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];

							for (int j = 0; j < row.Length; j++)
								row[j] = (sbyte)HEAPDATA[pos + j];
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Byte:
					{
						byte[][] arrya = new byte[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							byte[] row = new byte[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];

							for (int j = 0; j < row.Length; j++)
								row[j] = (byte)HEAPDATA[pos + j];
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Boolean:
					{
						bool[][] arrya = new bool[NAXIS2][];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							bool[] row = new bool[TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]];
							int pos = (int)TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i];

							for (int j = 0; j < row.Length; j++)
								row[j] = Convert.ToBoolean(HEAPDATA[pos + j]);
							arrya[i] = row;
						});
						return arrya;
					}

					case TypeCode.Char:
					{
						string[] arrya = new string[NAXIS2];
						Parallel.For(0, NAXIS2, opts, i =>
						{
							arrya[i] = System.Text.Encoding.ASCII.GetString(HEAPDATA, TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, i], TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, i]);
						});
						return arrya;
					}

					default:
						throw new Exception("Unrecognized TypeCode: '" + entryTypeCode.ToString() + "'");
				}
			}

			throw new Exception("Made it to end of GETHEAPTTYPE without returning any data.");
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

			byte[] prepend = new byte[startpos];
			for (int i = 0; i < prepend.Length; i++)
				prepend[i] = HEAPDATA[i];

			byte[] append = new byte[HEAPDATA.Length - endpos];
			for (int i = 0; i < append.Length; i++)
				append[i] = HEAPDATA[endpos + i];

			HEAPDATA = new byte[prepend.Length + append.Length];
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

		/// <summary>TableDataRepeats reports the number of columns or repeats in each table entry. Variable repeat (heap data) entries only report 1...use GetTTYPERowRepeatsHeapEntry to get the number of repeats for a given row.</summary>
		public int[] TableDataRepeats
		{
			get { return TREPEATS; }
		}

		/// <summary>TableDataLabelTTYPEs reports the name of each table entry, i.e. the TTYPE values.</summary>
		public string[] TableDataLabelTTYPEs
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
			get { return FORMATBINARYTABLEEXTENSIONHEADER(true); }
		}

		/// <summary>Accesses the EXTNAME name of the extension. Can be used to set the name.</summary>
		public string ExtensionNameEXTNAME
		{
			get { return EXTNAME; }
			set { EXTNAME = value; }
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

		/// <summary>
		/// Provides options for specifying the nature of the return Array
		/// </summary>
		public enum TTYPEReturn
		{
			/// <summary>
			/// Returns the array in its native Type.
			/// </summary>
			Native,

			/// <summary>
			/// Returns the array as double-precision values. Will throw an error if the TTYPE is a Boolean or Char (for string) TypeCode.
			/// </summary>
			AsDouble,

			/// <summary>
			/// Returns the array values as strings.
			/// </summary>
			AsString
		}

		/// <summary>Return a binary table entry as an Array object. Its type and rank are given to the user. Includes options for specifying the precision of the return.</summary>
		/// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		/// <param name="entryTypeCode">The TypeCode of the underlying data in the TTYPE. NOTE: strings are TypeCode.Char.</param>
		/// <param name="entryNElements">A vector to return the properties of the return Array:
		/// <br />If the return is a numeric vector or vector of strings of all the same length, it contains only one element, being the length of the vector (NAXIS2).
		/// <br />If the return is a numeric 2D array, it contains the width of the array as the first element, and the height of the array (NASIXS2) as the second element.
		/// <br />If the return is from the heap, therefore as a vector of vectors (numeric or string), therefore containing a variable number of elements on each row, then it is a vector containing the number of elements of the vector on each row.
		/// <br />If the ttypeEntry has TDIM keywords for an n &gt;= 3 dimensional array, then it contains the TDIM values for each TDIMn keyword. It therefore should have at least 3 elements.</param>
		/// <param name="returnType">Use this option to force a non-native return for the Array precision. For example, a user may want single or int values returned as all doubles, or all values returned as strings.</param>
		public Array GetTTYPEEntry(string ttypeEntry, out TypeCode entryTypeCode, out int[] entryNElements, TTYPEReturn returnType = TTYPEReturn.Native)
		{
			int ttypeindex = GetTTYPEIndex(ttypeEntry);

			if (returnType == TTYPEReturn.AsDouble)
			{
				if (TTYPEISHEAPARRAYDESC[ttypeindex])//get from heap
					return GETHEAPTTYPE(ttypeindex, out entryTypeCode, out entryNElements, returnType);

				entryTypeCode = TCODES[ttypeindex];

				if (TDIMS[ttypeindex] != null)
					entryNElements = TDIMS[ttypeindex];
				else
					if (TREPEATS[ttypeindex] == 1 || TCODES[ttypeindex] == TypeCode.Char)
					entryNElements = new int[] { NAXIS2 };
				else
					entryNElements = new int[] { TREPEATS[ttypeindex], NAXIS2 };

				int byteoffset = 0;
				for (int i = 0; i < ttypeindex; i++)
					byteoffset += TBYTES[i];

				switch (TCODES[ttypeindex])
				{
					case TypeCode.Double:
					{
						if (TREPEATS[ttypeindex] == 1)
						{
							double[] vector = new double[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] dbl = new byte[8];
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
							double[] vector = new double[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] sng = new byte[4];
								int currentbyte = byteoffset + i * NAXIS1;
								sng[3] = BINTABLE[currentbyte];
								sng[2] = BINTABLE[currentbyte + 1];
								sng[1] = BINTABLE[currentbyte + 2];
								sng[0] = BINTABLE[currentbyte + 3];
								vector[i] = (double)BitConverter.ToSingle(sng, 0);
							});
							return vector;
						}
						else
						{
							double[,] arrya = new double[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								byte[] sng = new byte[4];
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									sng[3] = BINTABLE[currentbyte];
									sng[2] = BINTABLE[currentbyte + 1];
									sng[1] = BINTABLE[currentbyte + 2];
									sng[0] = BINTABLE[currentbyte + 3];
									arrya[j, i] = (double)BitConverter.ToSingle(sng, 0);
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
							double[] vector = new double[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] i64 = new byte[8];
								int currentbyte = byteoffset + i * NAXIS1;
								i64[7] = BINTABLE[currentbyte];
								i64[6] = BINTABLE[currentbyte + 1];
								i64[5] = BINTABLE[currentbyte + 2];
								i64[4] = BINTABLE[currentbyte + 3];
								i64[3] = BINTABLE[currentbyte + 4];
								i64[2] = BINTABLE[currentbyte + 5];
								i64[1] = BINTABLE[currentbyte + 6];
								i64[0] = BINTABLE[currentbyte + 7];
								vector[i] = (double)BitConverter.ToInt64(i64, 0);
							});
							return vector;
						}
						else
						{
							double[,] arrya = new double[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								byte[] i64 = new byte[8];
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
									arrya[j, i] = (double)BitConverter.ToInt64(i64, 0);
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
							double[] vector = new double[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] ui64 = new byte[8];
								int currentbyte = byteoffset + i * NAXIS1;
								ui64[7] = BINTABLE[currentbyte];
								ui64[6] = BINTABLE[currentbyte + 1];
								ui64[5] = BINTABLE[currentbyte + 2];
								ui64[4] = BINTABLE[currentbyte + 3];
								ui64[3] = BINTABLE[currentbyte + 4];
								ui64[2] = BINTABLE[currentbyte + 5];
								ui64[1] = BINTABLE[currentbyte + 6];
								ui64[0] = BINTABLE[currentbyte + 7];
								vector[i] = (double)FITSFILEOPS.MapLongToUlong(BitConverter.ToInt64(ui64, 0));
							});
							return vector;
						}
						else
						{
							double[,] arrya = new double[TREPEATS[ttypeindex], NAXIS2];
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
									arrya[j, i] = (double)FITSFILEOPS.MapLongToUlong(BitConverter.ToInt64(ui64, 0));
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
							double[] vector = new double[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] uint32 = new byte[4];
								int currentbyte = byteoffset + i * NAXIS1;
								uint32[3] = BINTABLE[currentbyte];
								uint32[2] = BINTABLE[currentbyte + 1];
								uint32[1] = BINTABLE[currentbyte + 2];
								uint32[0] = BINTABLE[currentbyte + 3];
								vector[i] = (double)FITSFILEOPS.MapIntToUint(BitConverter.ToInt32(uint32, 0));
							});
							return vector;
						}
						else
						{
							double[,] arrya = new double[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								byte[] uint32 = new byte[4];
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									uint32[3] = BINTABLE[currentbyte];
									uint32[2] = BINTABLE[currentbyte + 1];
									uint32[1] = BINTABLE[currentbyte + 2];
									uint32[0] = BINTABLE[currentbyte + 3];
									arrya[j, i] = (double)FITSFILEOPS.MapIntToUint(BitConverter.ToInt32(uint32, 0));
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
							double[] vector = new double[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] int32 = new byte[4];
								int currentbyte = byteoffset + i * NAXIS1;
								int32[3] = BINTABLE[currentbyte];
								int32[2] = BINTABLE[currentbyte + 1];
								int32[1] = BINTABLE[currentbyte + 2];
								int32[0] = BINTABLE[currentbyte + 3];
								vector[i] = (double)BitConverter.ToInt32(int32, 0);
							});
							return vector;
						}
						else
						{
							double[,] arrya = new double[TREPEATS[ttypeindex], NAXIS2];
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
									arrya[j, i] = (double)BitConverter.ToInt32(int32, 0);
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
							double[] vector = new double[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] uint16 = new byte[2];
								int currentbyte = byteoffset + i * NAXIS1;
								uint16[1] = BINTABLE[currentbyte];
								uint16[0] = BINTABLE[currentbyte + 1];
								vector[i] = (double)FITSFILEOPS.MapShortToUshort(BitConverter.ToInt16(uint16, 0));
							});
							return vector;
						}
						else
						{
							double[,] arrya = new double[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								byte[] uint16 = new byte[2];
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									uint16[1] = BINTABLE[currentbyte];
									uint16[0] = BINTABLE[currentbyte + 1];
									arrya[j, i] = (double)FITSFILEOPS.MapShortToUshort(BitConverter.ToInt16(uint16, 0));
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
							double[] vector = new double[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] int16 = new byte[2];
								int currentbyte = byteoffset + i * NAXIS1;
								int16[1] = BINTABLE[currentbyte];
								int16[0] = BINTABLE[currentbyte + 1];
								vector[i] = (double)BitConverter.ToInt16(int16, 0);
							});
							return vector;
						}
						else
						{
							double[,] arrya = new double[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								byte[] int16 = new byte[2];
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									int16[1] = BINTABLE[currentbyte];
									int16[0] = BINTABLE[currentbyte + 1];
									arrya[j, i] = (double)BitConverter.ToInt16(int16, 0);
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
							double[] vector = new double[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								vector[i] = (double)(byte)BINTABLE[currentbyte];
							});
							return vector;
						}
						else
						{
							double[,] arrya = new double[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									arrya[j, i] = (double)(byte)BINTABLE[currentbyte];
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
							double[] vector = new double[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								vector[i] = (double)(sbyte)(BINTABLE[currentbyte]);
							});
							return vector;
						}
						else
						{
							double[,] arrya = new double[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									arrya[j, i] = (double)(sbyte)(BINTABLE[currentbyte]);
									currentbyte += 1;
								}
							});
							return arrya;
						}
					}

					case TypeCode.Boolean:
					{
						throw new Exception("Cannot convert Boolean to double.");
					}

					case TypeCode.Char:
					{
						throw new Exception("Cannot convert Char to double.");
					}

					default:
						throw new Exception("Unrecognized TypeCode: '" + TCODES[ttypeindex].ToString() + "'");
				}
			}

			else if (returnType == TTYPEReturn.AsString)
			{
				if (TTYPEISHEAPARRAYDESC[ttypeindex])//get from heap
					return GETHEAPTTYPE(ttypeindex, out entryTypeCode, out entryNElements, returnType);

				entryTypeCode = TCODES[ttypeindex];

				if (TDIMS[ttypeindex] != null)
					entryNElements = TDIMS[ttypeindex];
				else
					if (TREPEATS[ttypeindex] == 1 || TCODES[ttypeindex] == TypeCode.Char)
					entryNElements = new int[] { NAXIS2 };
				else
					entryNElements = new int[] { TREPEATS[ttypeindex], NAXIS2 };

				int byteoffset = 0;
				for (int i = 0; i < ttypeindex; i++)
					byteoffset += TBYTES[i];

				switch (TCODES[ttypeindex])
				{
					case TypeCode.Double:
					{
						if (TREPEATS[ttypeindex] == 1)
						{
							string[] vector = new string[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] dbl = new byte[8];
								int currentbyte = byteoffset + i * NAXIS1;
								dbl[7] = BINTABLE[currentbyte];
								dbl[6] = BINTABLE[currentbyte + 1];
								dbl[5] = BINTABLE[currentbyte + 2];
								dbl[4] = BINTABLE[currentbyte + 3];
								dbl[3] = BINTABLE[currentbyte + 4];
								dbl[2] = BINTABLE[currentbyte + 5];
								dbl[1] = BINTABLE[currentbyte + 6];
								dbl[0] = BINTABLE[currentbyte + 7];
								vector[i] = BitConverter.ToDouble(dbl, 0).ToString();
							});
							return vector;
						}
						else
						{
							string[,] arrya = new string[TREPEATS[ttypeindex], NAXIS2];
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
									arrya[j, i] = BitConverter.ToDouble(dbl, 0).ToString();
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
							string[] vector = new string[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] sng = new byte[4];
								int currentbyte = byteoffset + i * NAXIS1;
								sng[3] = BINTABLE[currentbyte];
								sng[2] = BINTABLE[currentbyte + 1];
								sng[1] = BINTABLE[currentbyte + 2];
								sng[0] = BINTABLE[currentbyte + 3];
								vector[i] = BitConverter.ToSingle(sng, 0).ToString();
							});
							return vector;
						}
						else
						{
							string[,] arrya = new string[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								byte[] sng = new byte[4];
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									sng[3] = BINTABLE[currentbyte];
									sng[2] = BINTABLE[currentbyte + 1];
									sng[1] = BINTABLE[currentbyte + 2];
									sng[0] = BINTABLE[currentbyte + 3];
									arrya[j, i] = BitConverter.ToSingle(sng, 0).ToString();
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
							string[] vector = new string[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] i64 = new byte[8];
								int currentbyte = byteoffset + i * NAXIS1;
								i64[7] = BINTABLE[currentbyte];
								i64[6] = BINTABLE[currentbyte + 1];
								i64[5] = BINTABLE[currentbyte + 2];
								i64[4] = BINTABLE[currentbyte + 3];
								i64[3] = BINTABLE[currentbyte + 4];
								i64[2] = BINTABLE[currentbyte + 5];
								i64[1] = BINTABLE[currentbyte + 6];
								i64[0] = BINTABLE[currentbyte + 7];
								vector[i] = BitConverter.ToInt64(i64, 0).ToString();
							});
							return vector;
						}
						else
						{
							string[,] arrya = new string[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								byte[] i64 = new byte[8];
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
									arrya[j, i] = BitConverter.ToInt64(i64, 0).ToString();
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
							string[] vector = new string[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] ui64 = new byte[8];
								int currentbyte = byteoffset + i * NAXIS1;
								ui64[7] = BINTABLE[currentbyte];
								ui64[6] = BINTABLE[currentbyte + 1];
								ui64[5] = BINTABLE[currentbyte + 2];
								ui64[4] = BINTABLE[currentbyte + 3];
								ui64[3] = BINTABLE[currentbyte + 4];
								ui64[2] = BINTABLE[currentbyte + 5];
								ui64[1] = BINTABLE[currentbyte + 6];
								ui64[0] = BINTABLE[currentbyte + 7];
								vector[i] = FITSFILEOPS.MapLongToUlong(BitConverter.ToInt64(ui64, 0)).ToString();
							});
							return vector;
						}
						else
						{
							string[,] arrya = new string[TREPEATS[ttypeindex], NAXIS2];
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
									arrya[j, i] = FITSFILEOPS.MapLongToUlong(BitConverter.ToInt64(ui64, 0)).ToString();
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
							string[] vector = new string[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] uint32 = new byte[4];
								int currentbyte = byteoffset + i * NAXIS1;
								uint32[3] = BINTABLE[currentbyte];
								uint32[2] = BINTABLE[currentbyte + 1];
								uint32[1] = BINTABLE[currentbyte + 2];
								uint32[0] = BINTABLE[currentbyte + 3];
								vector[i] = FITSFILEOPS.MapIntToUint(BitConverter.ToInt32(uint32, 0)).ToString();
							});
							return vector;
						}
						else
						{
							string[,] arrya = new string[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								byte[] uint32 = new byte[4];
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									uint32[3] = BINTABLE[currentbyte];
									uint32[2] = BINTABLE[currentbyte + 1];
									uint32[1] = BINTABLE[currentbyte + 2];
									uint32[0] = BINTABLE[currentbyte + 3];
									arrya[j, i] = FITSFILEOPS.MapIntToUint(BitConverter.ToInt32(uint32, 0)).ToString();
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
							string[] vector = new string[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] int32 = new byte[4];
								int currentbyte = byteoffset + i * NAXIS1;
								int32[3] = BINTABLE[currentbyte];
								int32[2] = BINTABLE[currentbyte + 1];
								int32[1] = BINTABLE[currentbyte + 2];
								int32[0] = BINTABLE[currentbyte + 3];
								vector[i] = BitConverter.ToInt32(int32, 0).ToString();
							});
							return vector;
						}
						else
						{
							string[,] arrya = new string[TREPEATS[ttypeindex], NAXIS2];
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
									arrya[j, i] = BitConverter.ToInt32(int32, 0).ToString();
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
							string[] vector = new string[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] uint16 = new byte[2];
								int currentbyte = byteoffset + i * NAXIS1;
								uint16[1] = BINTABLE[currentbyte];
								uint16[0] = BINTABLE[currentbyte + 1];
								vector[i] = FITSFILEOPS.MapShortToUshort(BitConverter.ToInt16(uint16, 0)).ToString();
							});
							return vector;
						}
						else
						{
							string[,] arrya = new string[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								byte[] uint16 = new byte[2];
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									uint16[1] = BINTABLE[currentbyte];
									uint16[0] = BINTABLE[currentbyte + 1];
									arrya[j, i] = FITSFILEOPS.MapShortToUshort(BitConverter.ToInt16(uint16, 0)).ToString();
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
							string[] vector = new string[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] int16 = new byte[2];
								int currentbyte = byteoffset + i * NAXIS1;
								int16[1] = BINTABLE[currentbyte];
								int16[0] = BINTABLE[currentbyte + 1];
								vector[i] = BitConverter.ToInt16(int16, 0).ToString();
							});
							return vector;
						}
						else
						{
							string[,] arrya = new string[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								byte[] int16 = new byte[2];
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									int16[1] = BINTABLE[currentbyte];
									int16[0] = BINTABLE[currentbyte + 1];
									arrya[j, i] = BitConverter.ToInt16(int16, 0).ToString();
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
							string[] vector = new string[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								vector[i] = ((byte)BINTABLE[currentbyte]).ToString();
							});
							return vector;
						}
						else
						{
							string[,] arrya = new string[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									arrya[j, i] = ((byte)BINTABLE[currentbyte]).ToString();
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
							string[] vector = new string[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								vector[i] = ((sbyte)(BINTABLE[currentbyte])).ToString();
							});
							return vector;
						}
						else
						{
							string[,] arrya = new string[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									arrya[j, i] = ((sbyte)(BINTABLE[currentbyte])).ToString();
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
							string[] vector = new string[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								vector[i] = Convert.ToBoolean(BINTABLE[currentbyte]).ToString();
							});
							return vector;
						}
						else
						{
							string[,] arrya = new string[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									arrya[j, i] = Convert.ToBoolean(BINTABLE[currentbyte]).ToString();
									currentbyte += 1;
								}
							});
							return arrya;
						}
					}

					case TypeCode.Char:
					{
						string[] vector = new string[NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							byte[] charstr = new byte[TREPEATS[ttypeindex]];
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
						throw new Exception("Unrecognized TypeCode: '" + TCODES[ttypeindex].ToString() + "'");
				}
			}

			else if (returnType == TTYPEReturn.Native)
			{
				if (TTYPEISHEAPARRAYDESC[ttypeindex])//get from heap
					return GETHEAPTTYPE(ttypeindex, out entryTypeCode, out entryNElements, returnType);

				entryTypeCode = TCODES[ttypeindex];

				if (TDIMS[ttypeindex] != null)
					entryNElements = TDIMS[ttypeindex];
				else
					if (TREPEATS[ttypeindex] == 1 || TCODES[ttypeindex] == TypeCode.Char)
					entryNElements = new int[] { NAXIS2 };
				else
					entryNElements = new int[] { TREPEATS[ttypeindex], NAXIS2 };

				int byteoffset = 0;
				for (int i = 0; i < ttypeindex; i++)
					byteoffset += TBYTES[i];

				switch (TCODES[ttypeindex])
				{
					case TypeCode.Double:
					{
						if (TREPEATS[ttypeindex] == 1)
						{
							double[] vector = new double[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] dbl = new byte[8];
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
							float[] vector = new float[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] sng = new byte[4];
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
								byte[] sng = new byte[4];
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
							long[] vector = new long[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] i64 = new byte[8];
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
								byte[] i64 = new byte[8];
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
								byte[] ui64 = new byte[8];
								int currentbyte = byteoffset + i * NAXIS1;
								ui64[7] = BINTABLE[currentbyte];
								ui64[6] = BINTABLE[currentbyte + 1];
								ui64[5] = BINTABLE[currentbyte + 2];
								ui64[4] = BINTABLE[currentbyte + 3];
								ui64[3] = BINTABLE[currentbyte + 4];
								ui64[2] = BINTABLE[currentbyte + 5];
								ui64[1] = BINTABLE[currentbyte + 6];
								ui64[0] = BINTABLE[currentbyte + 7];
								vector[i] = FITSFILEOPS.MapLongToUlong(BitConverter.ToInt64(ui64, 0));
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
									arrya[j, i] = FITSFILEOPS.MapLongToUlong(BitConverter.ToInt64(ui64, 0));
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
							uint[] vector = new uint[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] uint32 = new byte[4];
								int currentbyte = byteoffset + i * NAXIS1;
								uint32[3] = BINTABLE[currentbyte];
								uint32[2] = BINTABLE[currentbyte + 1];
								uint32[1] = BINTABLE[currentbyte + 2];
								uint32[0] = BINTABLE[currentbyte + 3];
								vector[i] = FITSFILEOPS.MapIntToUint(BitConverter.ToInt32(uint32, 0));
							});
							return vector;
						}
						else
						{
							uint[,] arrya = new uint[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								byte[] uint32 = new byte[4];
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									uint32[3] = BINTABLE[currentbyte];
									uint32[2] = BINTABLE[currentbyte + 1];
									uint32[1] = BINTABLE[currentbyte + 2];
									uint32[0] = BINTABLE[currentbyte + 3];
									arrya[j, i] = FITSFILEOPS.MapIntToUint(BitConverter.ToInt32(uint32, 0));
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
							int[] vector = new int[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] int32 = new byte[4];
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
							ushort[] vector = new ushort[NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								byte[] uint16 = new byte[2];
								int currentbyte = byteoffset + i * NAXIS1;
								uint16[1] = BINTABLE[currentbyte];
								uint16[0] = BINTABLE[currentbyte + 1];
								vector[i] = FITSFILEOPS.MapShortToUshort(BitConverter.ToInt16(uint16, 0));
							});
							return vector;
						}
						else
						{
							ushort[,] arrya = new ushort[TREPEATS[ttypeindex], NAXIS2];
							Parallel.For(0, NAXIS2, i =>
							{
								int currentbyte = byteoffset + i * NAXIS1;
								byte[] uint16 = new byte[2];
								for (int j = 0; j < TREPEATS[ttypeindex]; j++)
								{
									uint16[1] = BINTABLE[currentbyte];
									uint16[0] = BINTABLE[currentbyte + 1];
									arrya[j, i] = FITSFILEOPS.MapShortToUshort(BitConverter.ToInt16(uint16, 0));
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
								byte[] int16 = new byte[2];
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
								byte[] int16 = new byte[2];
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
							bool[] vector = new bool[NAXIS2];
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
						string[] vector = new string[NAXIS2];
						Parallel.For(0, NAXIS2, i =>
						{
							int currentbyte = byteoffset + i * NAXIS1;
							byte[] charstr = new byte[TREPEATS[ttypeindex]];
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
						throw new Exception("Unrecognized TypeCode: '" + TCODES[ttypeindex].ToString() + "'");
				}
			}

			throw new Exception("Made it to end of GetTTYPEEntry without returning an entry.");
		}

		/// <summary>Use this to access individual elements of the table with a string return of the value. Useful for looking at TTYPEs with multiple elements on a row, or, for extracting values for display purposes, etc.</summary>
		/// <param name="ttypeIndex">The index of the TTYPE value.</param>
		/// <param name="rowindex">The row index of the column.</param>
		public string GetTTypeEntryRow(int ttypeIndex, int rowindex)
		{
			if (!TTYPEISHEAPARRAYDESC[ttypeIndex])
			{
				int objectArrayRank;
				if (TREPEATS[ttypeIndex] == 1)
					objectArrayRank = 1;
				else
					objectArrayRank = 2;

				int byteoffset = 0;
				for (int i = 0; i < ttypeIndex; i++)
					byteoffset += TBYTES[i];
				int currentbyte = byteoffset + rowindex * NAXIS1;
				string str = "";

				switch (TCODES[ttypeIndex])
				{
					case TypeCode.Double:
					{
						byte[] dbl = new byte[8];
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
							for (int j = 0; j < TREPEATS[ttypeIndex]; j++)
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
								str += BitConverter.ToDouble(dbl, 0).ToString() + ", ";
							}
							return str.Substring(0, str.Length - 2);
						}
					}

					case TypeCode.Single:
					{
						byte[] sng = new byte[4];
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
							for (int j = 0; j < TREPEATS[ttypeIndex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j * 4;
								sng[3] = BINTABLE[currentbyte];
								sng[2] = BINTABLE[currentbyte + 1];
								sng[1] = BINTABLE[currentbyte + 2];
								sng[0] = BINTABLE[currentbyte + 3];
								str += BitConverter.ToSingle(sng, 0).ToString() + ", ";
							}
							return str.Substring(0, str.Length - 2);
						}
					}

					case (TypeCode.Int64):
					{
						byte[] i64 = new byte[8];
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
							for (int j = 0; j < TREPEATS[ttypeIndex]; j++)
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
								str += BitConverter.ToInt64(i64, 0).ToString() + ", ";
							}
							return str.Substring(0, str.Length - 2);
						}
					}

					case (TypeCode.UInt64):
					{
						byte[] ui64 = new byte[8];
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
							return FITSFILEOPS.MapLongToUlong(BitConverter.ToInt64(ui64, 0)).ToString();
						}
						else
						{
							for (int j = 0; j < TREPEATS[ttypeIndex]; j++)
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
								str += (FITSFILEOPS.MapLongToUlong(BitConverter.ToInt64(ui64, 0))).ToString() + ", ";
							}
							return str.Substring(0, str.Length - 2);
						}
					}

					case TypeCode.UInt32:
					{
						byte[] uint32 = new byte[4];
						if (objectArrayRank == 1)
						{
							uint32[3] = BINTABLE[currentbyte];
							uint32[2] = BINTABLE[currentbyte + 1];
							uint32[1] = BINTABLE[currentbyte + 2];
							uint32[0] = BINTABLE[currentbyte + 3];
							return (FITSFILEOPS.MapIntToUint(BitConverter.ToInt32(uint32, 0))).ToString();
						}
						else
						{
							for (int j = 0; j < TREPEATS[ttypeIndex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j * 4;
								uint32[3] = BINTABLE[currentbyte];
								uint32[2] = BINTABLE[currentbyte + 1];
								uint32[1] = BINTABLE[currentbyte + 2];
								uint32[0] = BINTABLE[currentbyte + 3];
								str += (FITSFILEOPS.MapIntToUint(BitConverter.ToInt32(uint32, 0))).ToString() + ", ";
							}
							return str.Substring(0, str.Length - 2);
						}
					}

					case TypeCode.Int32:
					{
						byte[] int32 = new byte[4];
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

							for (int j = 0; j < TREPEATS[ttypeIndex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j * 4;
								int32[3] = BINTABLE[currentbyte];
								int32[2] = BINTABLE[currentbyte + 1];
								int32[1] = BINTABLE[currentbyte + 2];
								int32[0] = BINTABLE[currentbyte + 3];
								str += BitConverter.ToInt32(int32, 0).ToString() + ", ";
							}
							return str.Substring(0, str.Length - 2);
						}
					}

					case TypeCode.UInt16:
					{
						byte[] uint16 = new byte[2];
						if (objectArrayRank == 1)
						{
							uint16[1] = BINTABLE[currentbyte];
							uint16[0] = BINTABLE[currentbyte + 1];
							return FITSFILEOPS.MapShortToUshort(BitConverter.ToInt16(uint16, 0)).ToString();
						}
						else
						{
							for (int j = 0; j < TREPEATS[ttypeIndex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j * 2;
								uint16[1] = BINTABLE[currentbyte];
								uint16[0] = BINTABLE[currentbyte + 1];
								str += (FITSFILEOPS.MapShortToUshort(BitConverter.ToInt16(uint16, 0))).ToString() + ", ";
							}
							return str.Substring(0, str.Length - 2);
						}
					}

					case TypeCode.Int16:
					{
						byte[] int16 = new byte[2];
						if (objectArrayRank == 1)
						{

							int16[1] = BINTABLE[currentbyte];
							int16[0] = BINTABLE[currentbyte + 1];
							return BitConverter.ToInt16(int16, 0).ToString();
						}
						else
						{
							for (int j = 0; j < TREPEATS[ttypeIndex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j * 2;
								int16[1] = BINTABLE[currentbyte];
								int16[0] = BINTABLE[currentbyte + 1];
								str += BitConverter.ToInt16(int16, 0).ToString() + ", ";
							}
							return str.Substring(0, str.Length - 2);
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
							for (int j = 0; j < TREPEATS[ttypeIndex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j;
								byte ret = (byte)BINTABLE[currentbyte];
								str += ret.ToString() + ", ";
							}
							return str.Substring(0, str.Length - 2);
						}
					}

					case TypeCode.SByte:
					{
						if (objectArrayRank == 1)
							return ((sbyte)(BINTABLE[currentbyte])).ToString();
						else
						{
							for (int j = 0; j < TREPEATS[ttypeIndex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j;
								str += ((sbyte)(BINTABLE[currentbyte])).ToString() + ", ";
							}
							return str.Substring(0, str.Length - 2);
						}
					}

					case TypeCode.Boolean:
					{
						if (objectArrayRank == 1)
							return Convert.ToBoolean(BINTABLE[currentbyte]).ToString();
						else
						{
							for (int j = 0; j < TREPEATS[ttypeIndex]; j++)
							{
								currentbyte = byteoffset + rowindex * NAXIS1 + j;
								str += Convert.ToBoolean(BINTABLE[currentbyte]).ToString() + ", ";
							}
							return str.Substring(0, str.Length - 2);
						}
					}

					case TypeCode.Char:
					{
						return System.Text.Encoding.ASCII.GetString(BINTABLE, currentbyte, TREPEATS[ttypeIndex]);
					}

					default:
					{
						throw new Exception("Unrecognized TypeCode: '" + TCODES[ttypeIndex].ToString() + "'");
					}
				}
			}
			else
			{
				int currentbyte = TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, rowindex];
				string str = "";

				switch (HEAPTCODES[ttypeIndex])
				{
					case TypeCode.Double:
					{
						byte[] dbl = new byte[8];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, rowindex]; j++)
						{
							dbl[7] = HEAPDATA[currentbyte];
							dbl[6] = HEAPDATA[currentbyte + 1];
							dbl[5] = HEAPDATA[currentbyte + 2];
							dbl[4] = HEAPDATA[currentbyte + 3];
							dbl[3] = HEAPDATA[currentbyte + 4];
							dbl[2] = HEAPDATA[currentbyte + 5];
							dbl[1] = HEAPDATA[currentbyte + 6];
							dbl[0] = HEAPDATA[currentbyte + 7];
							str += BitConverter.ToDouble(dbl, 0).ToString() + ", ";
							currentbyte += 8;
						}
						return str.Substring(0, str.Length - 2);
					}

					case TypeCode.Single:
					{
						byte[] sng = new byte[4];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, rowindex]; j++)
						{
							sng[3] = HEAPDATA[currentbyte];
							sng[2] = HEAPDATA[currentbyte + 1];
							sng[1] = HEAPDATA[currentbyte + 2];
							sng[0] = HEAPDATA[currentbyte + 3];
							str += BitConverter.ToSingle(sng, 0).ToString() + ", ";
							currentbyte += 4;
						}
						return str.Substring(0, str.Length - 2);
					}

					case (TypeCode.Int64):
					{
						byte[] i64 = new byte[8];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, rowindex]; j++)
						{
							i64[7] = HEAPDATA[currentbyte];
							i64[6] = HEAPDATA[currentbyte + 1];
							i64[5] = HEAPDATA[currentbyte + 2];
							i64[4] = HEAPDATA[currentbyte + 3];
							i64[3] = HEAPDATA[currentbyte + 4];
							i64[2] = HEAPDATA[currentbyte + 5];
							i64[1] = HEAPDATA[currentbyte + 6];
							i64[0] = HEAPDATA[currentbyte + 7];
							str += BitConverter.ToInt64(i64, 0).ToString() + ", ";
							currentbyte += 8;
						}
						return str.Substring(0, str.Length - 2);
					}

					case (TypeCode.UInt64):
					{
						byte[] ui64 = new byte[8];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, rowindex]; j++)
						{
							ui64[7] = HEAPDATA[currentbyte];
							ui64[6] = HEAPDATA[currentbyte + 1];
							ui64[5] = HEAPDATA[currentbyte + 2];
							ui64[4] = HEAPDATA[currentbyte + 3];
							ui64[3] = HEAPDATA[currentbyte + 4];
							ui64[2] = HEAPDATA[currentbyte + 5];
							ui64[1] = HEAPDATA[currentbyte + 6];
							ui64[0] = HEAPDATA[currentbyte + 7];
							str += FITSFILEOPS.MapLongToUlong(BitConverter.ToInt64(ui64, 0)).ToString() + ", ";
							currentbyte += 8;
						}
						return str.Substring(0, str.Length - 2);
					}

					case TypeCode.Int32:
					{
						byte[] int32 = new byte[4];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, rowindex]; j++)
						{
							int32[3] = HEAPDATA[currentbyte];
							int32[2] = HEAPDATA[currentbyte + 1];
							int32[1] = HEAPDATA[currentbyte + 2];
							int32[0] = HEAPDATA[currentbyte + 3];
							str += BitConverter.ToInt32(int32, 0).ToString() + ", ";
							currentbyte += 4;
						}
						return str.Substring(0, str.Length - 2);
					}

					case TypeCode.UInt32:
					{
						byte[] uint32 = new byte[4];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, rowindex]; j++)
						{
							uint32[3] = HEAPDATA[currentbyte];
							uint32[2] = HEAPDATA[currentbyte + 1];
							uint32[1] = HEAPDATA[currentbyte + 2];
							uint32[0] = HEAPDATA[currentbyte + 3];
							str += FITSFILEOPS.MapIntToUint(BitConverter.ToInt32(uint32, 0)).ToString() + ", ";
							currentbyte += 4;
						}
						return str.Substring(0, str.Length - 2);
					}

					case TypeCode.Int16:
					{
						byte[] int16 = new byte[2];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, rowindex]; j++)
						{
							int16[1] = HEAPDATA[currentbyte];
							int16[0] = HEAPDATA[currentbyte + 1];
							str += BitConverter.ToInt16(int16, 0).ToString() + ", ";
							currentbyte += 2;
						}
						return str.Substring(0, str.Length - 2);
					}

					case TypeCode.UInt16:
					{
						byte[] uint16 = new byte[2];
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, rowindex]; j++)
						{
							uint16[1] = HEAPDATA[currentbyte];
							uint16[0] = HEAPDATA[currentbyte + 1];
							str += FITSFILEOPS.MapShortToUshort(BitConverter.ToInt16(uint16, 0)).ToString() + ", ";
							currentbyte += 2;
						}
						return str.Substring(0, str.Length - 2);
					}

					case TypeCode.SByte:
					{
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, rowindex]; j++)
							str += ((sbyte)(HEAPDATA[currentbyte + j])).ToString() + ", ";
						return str.Substring(0, str.Length - 2);
					}

					case TypeCode.Byte:
					{
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, rowindex]; j++)
							str += ((byte)HEAPDATA[currentbyte + j]).ToString() + ", ";
						return str.Substring(0, str.Length - 2);
					}

					case TypeCode.Boolean:
					{
						for (int j = 0; j < TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, rowindex]; j++)
							str += Convert.ToBoolean(HEAPDATA[currentbyte + j]).ToString() + ", ";
						return str.Substring(0, str.Length - 2);
					}

					case TypeCode.Char:
					{
						return System.Text.Encoding.ASCII.GetString(HEAPDATA, TTYPEHEAPARRAYNELSPOS[ttypeIndex][1, rowindex], TTYPEHEAPARRAYNELSPOS[ttypeIndex][0, rowindex]);
					}

					default:
					{
						throw new Exception("Unrecognized TypeCode: '" + HEAPTCODES[ttypeIndex].ToString() + "'");
					}
				}
			}
		}

		/// <summary>Use this to access individual elements of the table with a string return of the value. Useful for looking at TTYPEs with multiple elements on a row, or, for extracting values for display purposes, etc.</summary>
		/// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		/// <param name="rowindex">The row index of the column.</param>
		public string GetTTypeEntryRow(string ttypeEntry, int rowindex)
		{
			return GetTTypeEntryRow(GetTTYPEIndex(ttypeEntry), rowindex);
		}

		/// <summary>Remove one of the entries from the binary table. Inefficient if the table has a very large number of entries with very large number of elements. Operates on heap-stored data where required.</summary>
		/// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		public void RemoveTTYPEEntry(string ttypeEntry)
		{
			int ttypeindex = GetTTYPEIndex(ttypeEntry);

			Array[] newEntryDataObjs = new Array[TFIELDS - 1];
			string[] newTTYPES = new string[TFIELDS - 1];
			string[] newTFORMS = new string[TFIELDS - 1];
			string[] newTUNITS = new string[TFIELDS - 1];
			int[] newTBYTES = new int[TFIELDS - 1];
			int[] newTREPEATS = new int[TFIELDS - 1];
			TypeCode[] newTCODES = new TypeCode[TFIELDS - 1];
			int[][] newTDIMS = new int[TFIELDS - 1][];
			bool[] newTTYPEISCOMPLEX = new bool[TFIELDS - 1];
			bool[] newTTYPEISHEAPARRAYDESC = new bool[TFIELDS - 1];
			TypeCode[] newHEAPTCODES = new TypeCode[TFIELDS - 1];
			int[][,] newTTYPEHEAPARRAYNELSPOS = new int[TFIELDS - 1][,];

			int c = 0;
			for (int i = 0; i < TFIELDS; i++)
				if (i == ttypeindex)
				{
					if (TTYPEISHEAPARRAYDESC[ttypeindex])
						REMOVEHEAPTTYPE(ttypeindex);
				}
				else
				{
					newEntryDataObjs[c] = this.GetTTYPEEntry(TTYPES[i], out TypeCode code, out int[] dimnelements, TTYPEReturn.Native);
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

		/// <summary>
		/// Provides options for specifying the nature of the data added to a BINTABLE.
		/// </summary>
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
			/// The entryArray is a vector containing variable-length vectors of numeric values, or it is an array of variable-length strings, either of which must therefore be stored in the heap area.
			/// </summary>
			IsHeapVariableLengthRows,

			/// <summary>
			/// The entryArray is a vector containing variable-length vectors of complex value pairings, of either single or double floating point precision, which therefore must be stored in the heap area. The length of each vector of the rows must be an even number given the pairings.
			/// </summary>
			IsHeapComplexVariableLengthRows,

			/// <summary>
			/// The entryArray is a numeric vector but which is to be interpreted as an n-dimensional array of rank r &gt;= 3, for which the TDIM values will be provided giving the number of elements along each dimension of the array, which will be written into the header as the TDIMn keys.
			/// <br />The optional argument for tdims *MUST* be provided with this option. 
			/// <br />NOTE that the array must be supplied as a vector or 2D array but which is to be understood as a rank r &gt;= 3 array via the TDIM keywords. Do not actually pass a rank r &gt;= 3 array.
			/// </summary>
			IsNDimensional
		}

		/// <summary>
		/// Add a vector, 2D array, or a vector of variable-length vectors, of numeric, complex numeric, string values, or rank &gt;= 3, to the binary table as a TTYPE entry.
		/// <br />The entryArray must be the same neight NAXIS2 as the previous entry additions if any exist.
		/// <br />If entryArray is a variable repeat heap-area array then the entry must be supplied as an vector of vectors, or an vector of strings; if complex each subarray must contain an even pairing of values.
		/// <br />If adding a complex number array to the binary table, the entryArray must be either single or double floating point, and must be a factor of two columns repeats where the 1st and odd numbered columns are the spatial part, and the 2nd and even numbered columns are the temporal part.
		/// <br />If entryArray is to be interpreted as rank &gt;= 3, then array dimensions need to be supplied with the optional tdim argument after the EntryArrayFormat.IsNDimensional option. If entries already exist then the user must have formatted the entryArray to match the existing table height NAXIS2.
		/// </summary>
		/// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		/// <param name="replaceIfExists">Replace the TTYPE entry if it already exists. If it already exists and the option is given to not replace, then an exception will be thrown.</param>
		/// <param name="entryUnits">The TUNITS physical units of the values of the array. Pass empty string if not required.</param>
		/// <param name="entryArray">The array to enter into the table.</param>
		/// <param name="arrayFormat">Specify entryArray format for non-default (trivial) cases.</param>
		/// <param name="tdims">Specify the array dimensions for rank r &gt;= 3, to be written as the TDIM keywords.</param>
		public void AddTTYPEEntry(string ttypeEntry, bool replaceIfExists, string entryUnits, Array entryArray, EntryArrayFormat arrayFormat = EntryArrayFormat.Default, int[]? tdims = null)
		{
			bool isComplex = false;
			if (arrayFormat == EntryArrayFormat.IsComplex)
				isComplex = true;
			bool addAsHeapVarRepeatArray = false;
			if (arrayFormat == EntryArrayFormat.IsHeapVariableLengthRows || arrayFormat == EntryArrayFormat.IsHeapComplexVariableLengthRows)
				addAsHeapVarRepeatArray = true;
			if (arrayFormat == EntryArrayFormat.IsNDimensional && tdims == null)
				throw new Exception("The tdims optional argument must be provided if the array format is n >= 3 dimensional.");

			if (entryArray.Rank >= 3)
				throw new Exception("Error: Cannot add an array of rank &gt;= 3 to a FITS binary table. An rank &gt;= 3 array must be formatted as a vector, and the tdim optional argument supplied to write as the TDIM keywords.");

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
						throw new Exception("Extension Entry TTYPE '" + ttypeEntry + "' must be a vector of vectors to add to the heap area. Index '" + i.ToString() + "' is rank = " + ((Array)(entryArray.GetValue(i))).Rank);

			if (!addAsHeapVarRepeatArray && Type.GetTypeCode((entryArray.GetType()).GetElementType()) == TypeCode.String)
				if (entryArray.Rank == 2)
					throw new Exception("Error: Cannot pass a 2d String array '" + ttypeEntry + "' . Only a 1D array of Strings is allowed since a string is already an array.");
				else
				{
					string[] strarr = (string[])entryArray;
					int nels = strarr[0].Length;
					for (int j = 1; j < strarr.Length; j++)
						if (strarr[j].Length != nels)
							throw new Exception("Error: String array entries '" + ttypeEntry + "' are not all the same namber of characters (repeats) long. Use EntryArrayFormat.IsHeapVariableRepeatRows option to add to the heap.");
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
			Array[] newEntryDataObjs = new Array[TFIELDS];
			string[] newTTYPES = new string[TFIELDS];
			string[] newTFORMS = new string[TFIELDS];
			string[] newTUNITS = new string[TFIELDS];
			int[] newTBYTES = new int[TFIELDS];
			int[] newTREPEATS = new int[TFIELDS];
			TypeCode[] newTCODES = new TypeCode[TFIELDS];
			int[][] newTDIMS = new int[TFIELDS][];
			bool[] newTTYPEISCOMPLEX = new bool[TFIELDS];
			bool[] newTTYPEISHEAPARRAYDESC = new bool[TFIELDS];
			TypeCode[] newHEAPTCODES = new TypeCode[TFIELDS];
			int[][,] newTTYPEHEAPARRAYNELSPOS = new int[TFIELDS][,];

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
					newEntryDataObjs[i] = this.GetTTYPEEntry(TTYPES[c], out TypeCode code, out int[] dimnelements, TTYPEReturn.Native);
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
			NAXIS1 = 0;
			for (int i = 0; i < TBYTES.Length; i++)
				NAXIS1 += TBYTES[i];

			MAKEBINTABLEBYTEARRAY(newEntryDataObjs);
		}

		/// <summary>Set the bintable full of basic entries all at once. More efficient than adding a large number of entries once at a time. Useful to use with a brand new and empty FITSBinTable. NOTE: THIS CLEARS ANY EXISTING ENTRIES INCLUDING THE HEAP.
		/// <br />Do not use for n &gt;= 3 dimensional, or complex, or heap (variable-repeat) entries.</summary>
		/// <param name="ttypeEntries">The names of the binary table extension entries, i.e. the TTYPE values.</param>
		/// <param name="entryUnits">The physical units of the values of the arrays. Pass null if not needed, or with null elements or empty elements where not required.</param>
		/// <param name="entryArrays">An array of vectors or 2D arrays to enter into the table as TTYPEs, all of which have the same height NAXIS2.</param>
		public void SetTTYPEEntries(string[] ttypeEntries, string[]? entryUnits, Array[] entryArrays)
		{
			for (int i = 0; i < entryArrays.Length; i++)
				if (entryArrays[i].Rank > 2)
					throw new Exception("Error: Do not use this function to add an n &gt; 2 dimensional array. Use AddTTYPEEntry with '" + ttypeEntries[i] + "' formatted as a vector, whilst specifying the option to supply the TDIM keywords.");
				else if (entryArrays[i].Rank == 1 && Type.GetTypeCode(entryArrays[i].GetType().GetElementType()) == TypeCode.String)
				{
					string[] strarr = (string[])entryArrays[i];
					int nels = strarr[0].Length;
					for (int j = 1; j < strarr.Length; j++)
						if (strarr[j].Length != nels)
							throw new Exception("Error: String array entries '" + ttypeEntries[i] + "' are not all the same number of characters (repeats) long. Use AddTTYPEEntry.");
				}

			int naxis2;
			if (entryArrays[0].Rank == 1)
				naxis2 = entryArrays[0].Length;
			else
				naxis2 = entryArrays[0].GetLength(1);
			for (int i = 1; i < entryArrays.Length; i++)
				if (entryArrays[i].Rank == 1)
				{
					if (entryArrays[i].Length != naxis2)
						throw new Exception("Error: all entry column heights, NAXIS2s, are not equal. Error detected for '" + ttypeEntries[i] + "'.");
				}
				else
				{
					if (entryArrays[i].GetLength(1) != naxis2)
						throw new Exception("Error: all entry column heights, NAXIS2s, are not equal. Error detected for '" + ttypeEntries[i] + "'.");

					if (Type.GetTypeCode(entryArrays[i].GetType().GetElementType()) == TypeCode.String)
						throw new Exception("Error: Cannot pass a 2d String array. Only a 1D array of Strings is allowed. Error detected for '" + ttypeEntries[i] + "'.");
				}

			TFIELDS = entryArrays.Length;
			TTYPES = ttypeEntries;
			if (entryUnits != null)
				TUNITS = entryUnits;
			else
				TUNITS = new string[entryArrays.Length];
			TCODES = new TypeCode[entryArrays.Length];
			TREPEATS = new int[entryArrays.Length];
			TFORMS = new string[entryArrays.Length];
			TBYTES = new int[entryArrays.Length];
			TTYPEISCOMPLEX = new bool[entryArrays.Length];
			TTYPEISHEAPARRAYDESC = new bool[entryArrays.Length];
			TDIMS = new int[entryArrays.Length][];
			HEAPTCODES = new TypeCode[entryArrays.Length];
			TTYPEHEAPARRAYNELSPOS = new int[entryArrays.Length][,];

			for (int i = 0; i < entryArrays.Length; i++)
			{
				TCODES[i] = Type.GetTypeCode(entryArrays[i].GetType().GetElementType());
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

		/// <summary>Returns the System.TypeCode for an entry in the table. Note that strings entries report as Char.</summary>
		public TypeCode GetTTYPETypeCode(int ttypeindex)
		{
			if (TTYPEISHEAPARRAYDESC[ttypeindex])
				return HEAPTCODES[ttypeindex];
			else
				return TCODES[ttypeindex];
		}

		/// <summary>Returns the System.TypeCode for an entry in the table. Note that strings entries report as Char.</summary>
		/// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		public TypeCode GetTTYPETypeCode(string ttypeEntry)
		{
			return GetTTYPETypeCode(GetTTYPEIndex(ttypeEntry));
		}

		/// <summary>Returns wheather the TTYPE entry at the given entry index is a variable repeat heap area vector of vectors.</summary>
		public bool GetTTYPEIsHeapEntry(int ttypeindex)
		{
			return TTYPEISHEAPARRAYDESC[ttypeindex];
		}

		/// <summary>
		/// Returns wheather the TTYPE entry is a variable repeat heap area vector.
		/// </summary>
		/// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
		public bool GetTTYPEIsHeapEntry(string ttypeEntry)
		{
            return GetTTYPEIsHeapEntry(GetTTYPEIndex(ttypeEntry));
		}

        /// <summary>Returns the number of elements (repeats) for a given heap entry at a given row.</summary>
        /// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
        /// <param name="row">The row of the entry.</param>
        public int GetTTYPEHeapEntryRowLength(string ttypeEntry, int row)
		{
            return TTYPEHEAPARRAYNELSPOS[GetTTYPEIndex(ttypeEntry)][0, row];
		}

        /// <summary>
        /// Returns the index of the TTYPE entry.
        /// </summary>
        /// <param name="ttypeEntry">The name of the binary table extension entry, i.e. the TTYPE value.</param>
        public int GetTTYPEIndex(string ttypeEntry)
		{
            for (int i = 0; i < TTYPES.Length; i++)
                if (TTYPES[i] == ttypeEntry)
					return i;
			
			throw new Exception("Extension Entry TTYPE Label wasn't found: '" + ttypeEntry + "'");
        }

		/// <summary>Add an extra key to the extension header. If the key is intended as a COMMENT, leave the keyValue empty, and place the entire comment in keyComment.</summary>
		/// <param name="keyName">The name of the key. If they key is a comment, either use COMMENT here, or just pass an empty string and put the entire comment line in keyComment.</param>
		/// <param name="keyValue">The value of the key. Pass numeric types as a string. If the key is intended as a comment, pass an empty string.</param>
		/// <param name="keyComment">The header key comment, maximum 80 characters if it is the entire comment line, or maximum 73 characters if keyName is COMMENT; excess elements will be truncated.</param>
		public void AddExtraHeaderKey(string keyName, string keyValue, string keyComment)
		{
			if (EXTRAKEYS == null)
				EXTRAKEYS = new FITSHeaderKey[1] { new FITSHeaderKey(keyName, keyValue, keyComment) };
			else
			{
				FITSHeaderKey[] newkeys = new FITSHeaderKey[EXTRAKEYS.Length + 1];

				for (int i = 0; i < EXTRAKEYS.Length; i++)
					newkeys[i] = EXTRAKEYS[i];

				newkeys[EXTRAKEYS.Length] = new FITSHeaderKey(keyName, keyValue, keyComment);
				EXTRAKEYS = newkeys;
			}
		}

		/// <summary>Get the value of an extra Key. If the key doesn't exist, an empty String is returned.</summary>
		/// <param name="keyName">The name of the key.</param>
		public string GetExtraHeaderKeyValue(string keyName)
		{
			for (int i = 0; i < EXTRAKEYS.Length; i++)
				if (keyName.Equals(EXTRAKEYS[i]))
					return EXTRAKEYS[i].Value;
			return "";
		}

		/// <summary>Remove the extra header key with the given name and value.</summary>
		public void RemoveExtraHeaderKey(string keyName, string keyValue)
		{
			if (EXTRAKEYS == null)
				return;

			int keyindex = -1;

			for (int i = 0; i < EXTRAKEYS.Length; i++)
				if (EXTRAKEYS[i].Name == keyName && EXTRAKEYS[i].Value == keyValue)
				{
					keyindex = i;
					break;
				}

			if (keyindex == -1)
				return;

			FITSHeaderKey[] newkeys = new FITSHeaderKey[EXTRAKEYS.Length - 1];
			int c = 0;
			for (int i = 0; i < EXTRAKEYS.Length; i++)
				if (i == keyindex)
					continue;
				else
				{
					newkeys[c] = EXTRAKEYS[i];
					c++;
				}
			EXTRAKEYS = newkeys;
		}

		/// <summary>Clear all extra header keys.</summary>
		public void RemoveAllExtraHeaderKeys()
		{
			EXTRAKEYS = null;
		}

		/// <summary>Write the binary table into a new or existing FITS file. If the binary table already exists in an existing FITS file, it can optionally be replaced.</summary>
		/// <param name="FileName">The full file name to write the binary table into. The file can either be new or already exist.</param>
		/// <param name="OverWriteExtensionIfExists">If the binary table already exists it can be overwritten. If it exists and the option is given to not overwrite it, then an exception will be thrown.</param>
		public void Write(string FileName, bool OverWriteExtensionIfExists)
		{
			FILENAME = FileName;

			if (!File.Exists(FILENAME))//then write a new file, otherwise check the existing file for existing table, etc.
			{
				JPFITS.FITSImage ff = new FITSImage(FILENAME, true);
				ff.WriteImage(DiskPrecision.Double, true);
			}

			FileStream fs = new FileStream(FILENAME, FileMode.Open);

			ArrayList headerret = null;
			if (!FITSFILEOPS.ScanPrimaryUnit(fs, true, ref headerret, out bool hasext))
			{
				fs.Close();
				throw new Exception("File '" + FileName + "' not formatted as FITS file. Use a new file.");
			}
			if (!hasext)
			{
				fs.Position = 0;
				FITSFILEOPS.ScanPrimaryUnit(fs, false, ref headerret, out hasext);
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
				string[] HEADER = ff.Header.GetFormattedHeaderBlock(FITSHeader.HeaderUnitType.Primary, false);

				byte[] headarr = new byte[HEADER.Length * 80];
				for (int i = 0; i < HEADER.Length; i++)
					for (int j = 0; j < 80; j++)
						headarr[i * 80 + j] = (byte)HEADER[i][j];

				fs = new FileStream(FILENAME, FileMode.Create);
				fs.Write(headarr, 0, headarr.Length);
				fs.Write(primarydataarr, 0, primarydataarr.Length);
				fs.Close();

				fs = new FileStream(FILENAME, FileMode.Open);
				FITSFILEOPS.ScanPrimaryUnit(fs, true, ref headerret, out hasext);
			}

			bool extensionfound = FITSFILEOPS.SeekExtension(fs, "BINTABLE", EXTNAME, ref headerret, out long extensionstartposition, out long extensionendposition, out _, out _, out _);
			if (extensionfound && !OverWriteExtensionIfExists)
			{
				fs.Close();
				throw new Exception("ExtensionName '" + EXTNAME + "' already exists and was told to not overwrite it...");
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
			string[] header = FORMATBINARYTABLEEXTENSIONHEADER(false);
			byte[] headerdata = new byte[header.Length * 80];

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
			return FITSFILEOPS.GetAllExtensionNames(FileName, "BINTABLE");
		}		

		/// <summary>Remove a binary table extension from the given FITS file.</summary>
		/// <param name="FileName">The full-path file name.</param>
		/// <param name="ExtensionName">The name of the binary table extension. If the extension isn't found, an exception is thrown.</param>
		public static void RemoveExtension(string FileName, string ExtensionName)
		{
			FileStream fs = new FileStream(FileName, FileMode.Open);
			ArrayList header = null;
			if (!FITSFILEOPS.ScanPrimaryUnit(fs, true, ref header, out bool hasext) || !hasext)
			{
				fs.Close();
				if (!hasext)
					throw new Exception("File '" + FileName + "'  indicates no extensions present.");
				else
					throw new Exception("File '" + FileName + "'  not formatted as FITS file.");
			}

			bool exists = FITSFILEOPS.SeekExtension(fs, "BINTABLE", ExtensionName, ref header, out long extensionstartposition, out long extensionendposition, out _, out _, out _);
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
			ArrayList header = null;
			if (!FITSFILEOPS.ScanPrimaryUnit(fs, true, ref header, out bool hasext) || !hasext)
			{
				fs.Close();
				if (!hasext)
					throw new Exception("File '" + FileName + "'  indicates no extensions present.");
				else
					throw new Exception("File '" + FileName + "'  not formatted as FITS file.");
			}

			bool exists = FITSFILEOPS.SeekExtension(fs, "BINTABLE", ExtensionName, ref header, out _, out _, out _, out _, out _);
			fs.Close();
			return exists;
		}

		#endregion
	}
}
