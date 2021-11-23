using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
#nullable enable

namespace JPFITS
{
	//<summary>FITSFILEOPS static class for facilitating interaction with FITS binary data on disk through a FileStream.</summary>
	public class FITSFILEOPS
	{
		/// <summary>Scans the primary header unit and data of a FITS file. Returns false if the file is not a FITS file.</summary>
		/// <param name="fs">The FileStream of the FITS file.</param>
		/// <param name="scanpastprimarydata">True to set the FileStream fs position to the end of the data block, otherwise the fs position will be at the end of the primary header block, i.e. at the beginning of the primary data.</param>
		/// <param name="header_return">Returns the header of the extension as an ArrayList with each 80-character header line being a String^ member of this list. Pass nullptr if not required.</param>
		/// <param name="has_extensions">Returns whether or not the FITS file may contain extensions.</param>
		public static bool SCANPRIMARYUNIT(FileStream fs, bool scanpastprimarydata, ref ArrayList? header_return, ref bool has_extensions)
		{
			byte[] c = new byte[2880];
			int naxis = -1, bitpix = -1, Nnaxisn = 1;
			int[] naxisn = new int[0];

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
								FITSformat = true;//if it doesn't exist, then it needs to be added
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
							if (line.Substring(10, 20).Trim() == "T")
							{
								extend = true;
								has_extensions = extend;
								continue;
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

		/// <summary>Find the FITS extension table of the given type and name. Returns false if the XTENSION type of the specified EXTNAME is not found. If extension_name is found the FileStream fs will be placed at the beginning of the extension's main data table block.</summary>
		/// <param name="fs">The FileStream of the FITS file.
		/// <para>If EXTNAME is found the FileStream fs will be placed at the beginning of the extension's main data table block.</para>
		/// <para>If EXTNAME is NOT found it will be at the end of the file.</para></param>
		/// <param name="extension_type">The XTENSION extension type, either: &quot;BINTABLE&quot;, &quot;TABLE&quot;, or &quot;IMAGE&quot;.</param>
		/// <param name="extension_name">The EXTNAME extension name. If the extension is known to have no EXTNAME keyword and name, then pass an empty String and the first nameless extension of the specified type will be seeked.</param>
		/// <param name="header_return">Returns the header of the extension as an ArrayList with each 80-character header line being a String^ member of this list. Pass nullptr if not required.</param>
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
				naxis = 0;
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

		/// <summary>Find the FITS extension table of the given type and name. Returns false if the XTENSION type of the specified EXTNAME is not found. If extension_name is found the FileStream fs will be placed at the beginning of the extension's main data table block.</summary>
		/// <param name="fs">The FileStream of the FITS file.
		/// <para>If EXTNAME is found the FileStream fs will be placed at the beginning of the extension's main data table block.</para>
		/// <para>If EXTNAME is NOT found it will be at the end of the file.</para></param>
		/// <param name="extension_type">The XTENSION extension type, either: &quot;BINTABLE&quot;, &quot;TABLE&quot;, or &quot;IMAGE&quot;.</param>
		/// <param name="extension_number">The ONE-BASED extension number. This can be used when extensions aren't named with the EXTNAME keyword; alternatively if they are named this still returns the XTENSION extension type of the specified number.</param>
		/// <param name="header_return">Returns the header of the extension as an ArrayList with each 80-character header line being a String^ member of this list. Pass nullptr if not required.</param>
		/// <param name="extensionStartPosition">Returns the start position within the FileStream of the extension...i.e. at the block boundary at the start of its header.</param>
		/// <param name="extensionEndPosition">Returns the end position within the FileStream of the extension, including after any heap, rounded up to a multiple of 2880 bytes at the last block boundary.</param>
		/// <param name="tableEndPosition">Returns the end position within the FileStream of the main data table, NOT rounded to a data block boundary.</param>
		/// <param name="pcount">Returns the number of bytes of any remaining fill plus supplemental heap data area after the main table endposition, IF any heap data exists. Does not represent fill bytes after the main table if no heap exists. Does not include fill bytes after the heap.</param>
		/// <param name="theap">Returns the position within the filestream of the beginning of the heap relative to the beginning of the main table. Nominally equal to NAXIS1 * NAXIS2 unless THEAP keyword specifies a larger value.</param>
		public static bool SEEKEXTENSION(FileStream fs, string extension_type, int extension_number, ref ArrayList? header_return, out long extensionStartPosition, out long extensionEndPosition, out long tableEndPosition, out long pcount, out long theap)
		{
			return SEEKEXTENSION(fs, extension_type, "_FINDEXTNUM_" + extension_number.ToString(), ref header_return, out extensionStartPosition, out extensionEndPosition, out tableEndPosition, out pcount, out theap);
		}

		/// <summary>Gets all extension names of a specified extension type in the FITS file.</summary>
		/// <param name="FileName">The full file name to read from disk.</param>
		/// <param name="extension_type">The XTENSION extension type, either: &quot;BINTABLE&quot;, &quot;TABLE&quot;, or &quot;IMAGE&quot;.</param>
		public static string[] GETALLEXTENSIONNAMES(string FileName, string extension_type)
		{
			FileStream fs = new FileStream(FileName, FileMode.Open);
			ArrayList header_return = null;
			bool hasext = false;
			if (!FITSFILEOPS.SCANPRIMARYUNIT(fs, true, ref header_return, ref hasext) || !hasext)
			{
				fs.Close();
				if (!hasext)
					throw new Exception("File '" + FileName + "'  indicates no extensions present.");
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

