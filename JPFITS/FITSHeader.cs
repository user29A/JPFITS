using System;
using System.IO;
using System.Collections;
#nullable enable

namespace JPFITS
{
	/// <summary>FITSImageHeader class for managing FITS file Primary Image headers.</summary>
	public class FITSHeader
	{
		#region CONSTRUCTORS
		/// <summary>Constructor. Creates an instance of a FITSHeader, with options to indicate whether extensions are present, and sets essential keywords for a given image it will be the header for.
		/// <br />If the image is to be an extension, then use GetFormattedHeaderBlock to pull the header out with SIMPLE = T changed to XTENSION = IMAGE for writing.</summary>
		/// <param name="mayContainExtensions">If true, heyword EXTEND = T is added, otherwise it is left out.</param>
		/// <param name="image">If image is null, then NAXIS = 0 and there are no NAXISn keywords or BSCALE or BZERO. Otherwise NAXIS, NAXISn, BSCALE and BZERO are set as per the image dimensions.
		/// <br />If the image will be saved at a different precision than double, use SetBITPIXNAXISBSCZ(precision, image) at write time.</param>
		public FITSHeader(bool mayContainExtensions, double[,]? image)
		{
			MAKE_DEFAULT_HEADER(mayContainExtensions, image);
		}

		/// <summary>Constructor. Creates an instance of a FITSImageHeader out of an array list of string header lines. Typically the headerlines would be returned from FITSFILEOPS.SCANPRIMARYUNIT.</summary>
		/// <param name="headerlines">A String list of header lines to be extrated and formatted into keys, values, and comments, or as comment lines, as FITSHeaderKeys</param>
		/// <param name="populate_nonessential">If false, non-essential key lines will be ignored. Saves a little bit of construction time if you don't need those, but they'll be lost if you re-write the file without them.</param>
		public FITSHeader(ArrayList headerlines, bool populate_nonessential)
		{
			if (populate_nonessential)
				HEADERKEYS = new JPFITS.FITSHeaderKey[headerlines.Count];
			else
				HEADERKEYS = new JPFITS.FITSHeaderKey[0];

			for (int i = 0; i < HEADERKEYS.Length; i++)
				HEADERKEYS[i] = new FITSHeaderKey((string)headerlines[i]);
		}

		/// <summary>
		/// Constructor. Creates an instance of a FITSImageHeader out of an array of string header lines.
		/// </summary>
		/// <param name="headerlines">An array of strings to turn into FITSHeaderKeys.</param>
		public FITSHeader(string[] headerlines)
		{
			HEADERKEYS = new JPFITS.FITSHeaderKey[headerlines.Length];

			for (int i = 0; i < HEADERKEYS.Length; i++)
				HEADERKEYS[i] = new FITSHeaderKey(headerlines[i]);
		}

		/// <summary>
		/// Constructor. Creates an instance of a FITSImageHeader out of an array of FITSHeaderKeys.
		/// </summary>
		/// <param name="headerKeys">An array of keys.</param>
		public FITSHeader(FITSHeaderKey[] headerKeys)
		{
			HEADERKEYS = headerKeys;
		}

		/// <summary>
		/// Constructor. Creates an instance of a FITSImageHeader out of an array list of FITSHeaderKeys.
		/// </summary>
		/// <param name="FITSheaderKeys">An array list of FITSHeaderKeys</param>
		public FITSHeader(ArrayList FITSheaderKeys)
		{
			HEADERKEYS = new FITSHeaderKey[FITSheaderKeys.Count];
			for (int i = 0; i < HEADERKEYS.Length; i++)
				HEADERKEYS[i] = (FITSHeaderKey)FITSheaderKeys[i];
		}

		/// <summary>
		/// Make a header object from an existing FITS file primary header.
		/// </summary>
		/// <param name="fileName">The file from which to get the primary header unit and make this header from.</param>
		public FITSHeader(string fileName)
		{
			FileStream fs = new FileStream(fileName, FileMode.Open);
			ArrayList headerlines = new ArrayList();
			FITSFILEOPS.ScanPrimaryUnit(fs, false, ref headerlines, out bool ext);
			fs.Close();

			HEADERKEYS = new JPFITS.FITSHeaderKey[headerlines.Count];
			for (int i = 0; i < HEADERKEYS.Length; i++)
				HEADERKEYS[i] = new FITSHeaderKey((string)headerlines[i]);
		}

		/// <summary>
		/// Make a HITSHeader object from an existing one. Creates a new exact copy.
		/// </summary>
		/// <param name="header">A FITSHeader object.</param>
		public FITSHeader(FITSHeader header)
		{
			HEADERKEYS = new JPFITS.FITSHeaderKey[header.Length];
			for (int i = 0; i < HEADERKEYS.Length; i++)
				HEADERKEYS[i] = new FITSHeaderKey(header[i].GetFullyFomattedFITSLine());
		}

		#endregion

		#region PROPERTIES
		/// <summary>Returns the FITSHeaderKey object at a given zero-based index in the header.</summary>
		public FITSHeaderKey this[int i]
		{
			get { return HEADERKEYS[i]; }
			set { HEADERKEYS[i] = value; }
		}

		/// <summary>Returns the number of header key lines in the header, excluding any padding after END key.</summary>
		public int Length
		{
			get { return HEADERKEYS.Length; }
		}

		#endregion

		#region MEMBERS

		/// <summary>Copies a header from another FITSImageHeader into this one. Restricted keywords are neither copied nor overwritten.</summary>
		public void CopyHeaderFrom(JPFITS.FITSHeader sourceHeader)
		{
			bool[] oktocopy = new bool[sourceHeader.Length];
			int ntocopy = 0;

			for (int i = 0; i < sourceHeader.Length; i++)
				if (ValidKeyEdit(sourceHeader.GetKeyName(i), false))
				{
					ntocopy++;
					oktocopy[i] = true;
				}

			JPFITS.FITSHeaderKey[] newheaderkeys = new JPFITS.FITSHeaderKey[this.Length + ntocopy];

			for (int i = 0; i < this.Length - 1; i++)//leave off END for now
				newheaderkeys[i] = HEADERKEYS[i];

			int c = 0;
			for (int i = 0; i < sourceHeader.Length; i++)
				if (oktocopy[i])
				{
					newheaderkeys[this.Length - 1 + c] = sourceHeader[i];
					c++;
				}

			newheaderkeys[newheaderkeys.Length - 1] = new FITSHeaderKey("END");

			HEADERKEYS = newheaderkeys;
			UPDATEDISPLAYHEADER = true;
		}

		public enum HeaderUnitType
		{
			/// <summary>
			/// The header is for the Primary Unit of the FITS file.
			/// </summary>
			Primary,

			/// <summary>
			/// The header is for an IMAGE extension.
			/// </summary>
			ExtensionIMAGE,

			/// <summary>
			/// The header is for a BINTABLE extension.
			/// </summary>
			ExtensionBINTABLE
		}

		/// <summary>Returns a formatted header block with the existing keys, and sets the first key to either SIMPLE = T or XTENSION = IMAGE or BINTABLE. If a full 2880-multiple block is needed, set keysOnly to false.</summary>
		/// <param name="headerType">The type of header, either primary, image extension, or binary table extension.</param>
		/// <param name="keysOnly">If true then only the existing keywords are returned formatted, otherwise if you need the entire 2880-multiple block pass false. True typically needed for display, false typically needed for writing.</param>
		public string[] GetFormattedHeaderBlock(HeaderUnitType headerType, bool keysOnly)
		{
			if (headerType != HeaderUnitType.Primary && !ISEXTENSION)
			{
				UPDATEDISPLAYHEADER = true;
				ISEXTENSION = true;

				this.RemoveKey("EXTEND");//if the header was primary, then it may have contained this keyword, but it doesn't need to anymore now that it is an extension header

				if (headerType == HeaderUnitType.ExtensionIMAGE)
					HEADERKEYS[0] = new FITSHeaderKey("XTENSION", "IMAGE", "image extension");
				else//must be binary table
					HEADERKEYS[0] = new FITSHeaderKey("XTENSION", "BINTABLE", "binary table extension");

				bool pcountkey = false, gcountkey = false;
				for (int i = 0; i < this.Length; i++)
					if (!pcountkey || !gcountkey)
					{
						if (HEADERKEYS[i].Name.Trim() == "PCOUNT")
							pcountkey = true;
						if (HEADERKEYS[i].Name.Trim() == "GCOUNT")
							pcountkey = true;
					}
				if (!pcountkey && !gcountkey)//they would BOTH not be present if things are being done correctly...need to add them
				{
					int naxis = -1;
					for (int i = 0; i < this.Length; i++)
						if (HEADERKEYS[i].Name.Trim() == "NAXIS")
						{
							naxis = Convert.ToInt32(HEADERKEYS[i].Value.Trim());
							break;
						}
					int naxisNindex = -1;
					for (int i = 0; i < this.Length; i++)
						if (HEADERKEYS[i].Name.Trim() == "NAXIS" + naxis.ToString())
						{
							naxisNindex = i;
							break;
						}

					JPFITS.FITSHeaderKey[] keys = new JPFITS.FITSHeaderKey[this.Length + 2];

					for (int i = 0; i <= naxisNindex; i++)
						keys[i] = HEADERKEYS[i];

					keys[naxisNindex + 1] = new FITSHeaderKey("PCOUNT", "0", "number of bytes in heap area");
					keys[naxisNindex + 2] = new FITSHeaderKey("GCOUNT", "1", "single data table");

					for (int i = naxisNindex + 3; i < keys.Length; i++)
						keys[i] = HEADERKEYS[i - 2];

					HEADERKEYS = keys;
				}
			}
			else if (headerType == HeaderUnitType.Primary && ISEXTENSION)
			{
				UPDATEDISPLAYHEADER = true;
				ISEXTENSION = false;
				HEADERKEYS[0] = new FITSHeaderKey("SIMPLE", "T", "file conforms to FITS standard");
			}

			if (!UPDATEDISPLAYHEADER && !keysOnly)
				return FORMATTEDHEADER;

			UPDATEDISPLAYHEADER = false;

			if (keysOnly)
				FORMATTEDHEADER = new string[this.Length];
			else
			{
				int NKeys = this.Length;
				int NCards = (NKeys - 1) / 36 + 1;
				FORMATTEDHEADER = new string[NCards * 36];
			}

			for (int i = 0; i < this.Length; i++)
				FORMATTEDHEADER[i] = HEADERKEYS[i].GetFullyFomattedFITSLine();

			if (keysOnly)
				return FORMATTEDHEADER;

			for (int i = this.Length; i < FORMATTEDHEADER.Length; i++)
				FORMATTEDHEADER[i] = "".PadLeft(80);

			return FORMATTEDHEADER;
		}		

		/// <summary>Returns all of the key names from the header.</summary>
		public string[] GetAllKeyNames()
		{
			string[] names = new string[this.Length];
			for (int i = 0; i < this.Length; i++)
				names[i] = this[i].Name;
			return names;
		}

		#endregion

		#region HEADER KEY INTERACTION
		/// <summary>GetKeyName returns the key of the primary header line at index. Throws an exception if the index exceeds the number of header lines.</summary>
		/// <param name="index">The zero-based line number to get the key name from.</param>
		public string GetKeyName(int index)
		{
			return HEADERKEYS[index].Name;
		}

		/// <summary>GetKeyValue returns the value of the primary header line at index. Throws an exception if the index exceeds the number of header lines.</summary>
		/// <param name="index">The zero-based line number to get the key value from.</param>
		public string GetKeyValue(int index)
		{
			return HEADERKEYS[index].Value;
		}

		/// <summary>GetKeyComment returns the comment of the primary header line at index. Throws an exception if the index exceeds the number of header lines.</summary>
		/// <param name="index">The zero-based line number to get the key comment from.</param>
		public string GetKeyComment(int index)
		{
			return HEADERKEYS[index].Comment;
		}

		/// <summary>GetKeyValue returns the value of the primary header key named Key. Returns empty String if the key is not found.</summary>
		/// <param name="key">The header key to find the value of.</param>
		public string GetKeyValue(string key)
		{
			for (int i = 0; i < this.Length; i++)
				if (key == HEADERKEYS[i].Name)
					return HEADERKEYS[i].Value;

			return "";
		}

		/// <summary>GetKeyComment returns the comment of the primary header key named Key. Returns empty String if the key is not found.</summary>
		/// <param name="key">The header key to find the comment of.</param>
		public string GetKeyComment(string key)
		{
			for (int i = 0; i < this.Length; i++)
				if (key == HEADERKEYS[i].Name)
					return HEADERKEYS[i].Comment;

			return "";
		}

		/// <summary>GetKeyIndex returns the zero-based index in the primary header of the key named Key. Returns -1 if the key is not found.</summary>
		/// <param name="key">The header key to find the index of.</param>
		/// <param name="KeyIsFullLineFormatted">If true then the entire formatted 80-element long line is compared - helpful if multiple keys have the same name or are formatted as comment lines. If false then only the key name is used.</param>
		public int GetKeyIndex(string key, bool KeyIsFullLineFormatted)
		{
			if (!KeyIsFullLineFormatted)
			{
				for (int i = 0; i < this.Length; i++)
					if (key == HEADERKEYS[i].Name)
						return i;
			}
			else
			{
				for (int i = 0; i < this.Length; i++)
					if (key == this[i].GetFullyFomattedFITSLine())
						return i;
			}

			return -1;
		}

		/// <summary>GetKeyIndex returns the zero-based index in the primary header of the key with matching value. Returns -1 if the key and value combination is not found.</summary>
		/// <param name="key">The header key to find the index of.</param>
		/// <param name="keyvalue">The header key value to find the index of.</param>
		public int GetKeyIndex(string key, string keyvalue)
		{
			for (int i = 0; i < this.Length; i++)
				if (key == HEADERKEYS[i].Name)
					if (keyvalue == HEADERKEYS[i].Value)
						return i;

			return -1;
		}

		/// <summary>GetKeyIndex returns the zero-based index in the primary header of the key with matching value and comment. Returns -1 if the key, value and comment combination is not found.</summary>
		/// <param name="key">The header key to find the index of.</param>
		/// <param name="keyvalue">The header key value to find the index of.</param>
		/// <param name="keycomment">The header key comment to find the index of.</param>
		public int GetKeyIndex(string key, string keyvalue, string keycomment)
		{
			for (int i = 0; i < this.Length; i++)
				if (key == HEADERKEYS[i].Name)
					if (keyvalue == HEADERKEYS[i].Value)
						if (keycomment == HEADERKEYS[i].Comment)
							return i;

			return -1;
		}

		/// <summary>SetKey sets the value of the key. If the key already exists then the value will be replaced but the comment will remain the same.</summary>
		/// <param name="Key">The header key to access.</param>
		/// <param name="Value">The header key value to set.</param>
		/// <param name="AddIfNotFound">Optionally add the key to the header if it isn't found.</param>
		/// <param name="AddAtIndex">If the key wasn't found, add at this zero-based index. Use -1 to append to the end of the header (before END key).</param>
		public void SetKey(string Key, string Value, bool AddIfNotFound, int AddAtIndex)
		{
			UPDATEDISPLAYHEADER = true;
			Key = Key.ToUpper();
			for (int i = 0; i < this.Length; i++)
				if (Key == HEADERKEYS[i].Name)
				{
					HEADERKEYS[i].Value = Value;
					return;
				}
			if (AddIfNotFound)
				AddKey(Key, Value, "", AddAtIndex);
		}

		/// <summary>SetKey sets the value and comment of the key. If the key already exists then the value and comment will be replaced.</summary>
		/// <param name="Key">The header key to access.</param>
		/// <param name="Value">The header key value to set.</param>
		/// <param name="Comment">The header key comment to set.</param>
		/// <param name="AddIfNotFound">Optionally add the key to the header if it isn't found.</param>
		/// <param name="AddAtIndex">If the key wasn't found, add at this zero-based index. Use -1 to append to the end of the header (before END key).</param>
		public void SetKey(string Key, string Value, string Comment, bool AddIfNotFound, int AddAtIndex)
		{
			UPDATEDISPLAYHEADER = true;
			Key = Key.ToUpper();
			for (int i = 0; i < this.Length; i++)
				if (Key == HEADERKEYS[i].Name)
				{
					HEADERKEYS[i].Value = Value;
					HEADERKEYS[i].Comment = Comment;
					return;
				}
			if (AddIfNotFound)
				AddKey(Key, Value, Comment, AddAtIndex);
		}

		/// <summary>SetKey sets the key, value and comment of the key at the given header index. This will overwrite whatever key exists at that index.</summary>
		/// <param name="index">The 0-based index of the header key to access. If the index does not occur within the header, then nothing happens.</param>
		/// <param name="Key">The header key to set.</param>
		/// <param name="Value">The header key value to set.</param>
		/// <param name="Comment">The header key comment to set.</param>
		public void SetKey(int index, string Key, string Value, string Comment)
		{
			HEADERKEYS[index] = new FITSHeaderKey(Key, Value, Comment);
			UPDATEDISPLAYHEADER = true;
		}

		/// <summary>AddKey adds a new key to the primary header.</summary>
		/// <param name = "keyLine">The header key line to add.</param>
		/// <param name="keyIndex">Add at this zero-based index. Use -1 to append to the end of the header (before END key).</param>
		public void AddKey(JPFITS.FITSHeaderKey keyLine, int keyIndex)
		{
			if (keyLine.IsCommentKey)
				this.AddCommentKeyLine(keyLine.Comment, keyIndex);
			else
				this.AddKey(keyLine.Name, keyLine.Value, keyLine.Comment, keyIndex);
		}

		/// <summary>AddKey adds a new key with value and comment to the primary header.</summary>
		/// <param name="NewKey">The header key to add.</param>
		/// <param name="NewValue">The header key value to add.</param>
		/// <param name="NewComment">The header key comment to add.</param>
		/// <param name="KeyIndex">Add at this zero-based index. Use -1 to append to the end of the header (before END key).</param>
		public void AddKey(string NewKey, string NewValue, string NewComment, int KeyIndex)
		{
			int c = 0;
			int L = this.Length;
			if (KeyIndex < 0 || KeyIndex >= L)
				KeyIndex = L - 1;//add to end of header (before END)

			JPFITS.FITSHeaderKey[] keys = new JPFITS.FITSHeaderKey[L + 1];

			for (int i = 0; i < L + 1; i++)
			{
				if (i == KeyIndex)
				{
					keys[i] = new FITSHeaderKey(NewKey, NewValue, NewComment);
					continue;
				}
				keys[i] = HEADERKEYS[c];
				c++;
			}

			HEADERKEYS = keys;
			UPDATEDISPLAYHEADER = true;
		}

		/// <summary>AddCommentKey adds a new key line formatted as a comment.
		/// <br />If the length of the commentKeyLine is more than 80 elements, the comment will be continued on subsequent lines until depleted.
		/// <br />If the user wishes the line to begin with COMMENT, then write the input commentKeyLine beginning as such.
		/// <br />If the user wishes the line to be blank, then pass commentKeyLine as an empty string or as only containing blanks (whitespace).</summary>
		/// <param name="commentKeyLine">The comment line.</param>
		/// <param name="keyIndex">Insert at this zero-based index. Use -1 to append to the end of the header (before END key). If keyIndex exceeds the header, the line is appended to the end of the header (before END key).</param>
		public void AddCommentKeyLine(string commentKeyLine, int keyIndex)
		{
			commentKeyLine = commentKeyLine.PadRight(80);//pad since might just be empty intended as blank line
			int nels = commentKeyLine.Length;
			int Nnewlines = (int)Math.Ceiling((double)(nels) / 80);
			string[] strnewlines = new string[Nnewlines];

			for (int i = 0; i < Nnewlines; i++)
				if ((i + 1) * 80 > nels)
					strnewlines[i] = commentKeyLine.Substring(i * 80);
				else
					strnewlines[i] = commentKeyLine.Substring(i * 80, 80);

			if (keyIndex < 0 || keyIndex >= this.Length)
				keyIndex = this.Length - 1;//add to end of header (before END)

			JPFITS.FITSHeaderKey[] keys = new JPFITS.FITSHeaderKey[this.Length + Nnewlines];

			int c = 0;
			nels = 0;
			for (int i = 0; i < keys.Length; i++)
			{
				if (i >= keyIndex && i < keyIndex + Nnewlines)
					keys[i] = new FITSHeaderKey(strnewlines[nels++]);
				else
				{
					keys[i] = HEADERKEYS[c];
					c++;
				}
			}

			HEADERKEYS = keys;
			UPDATEDISPLAYHEADER = true;
		}

		/// <summary>Move a key line to another position in the header.</summary>
		/// <param name="currentIndex">The key at this zero-based index will be moved.</param>
		/// /// <param name="newIndex">The key at currentIndex index will be moved to this zero-based index position.</param>
		public void MoveKey(int currentIndex, int newIndex)
		{
			if (newIndex < 0 || newIndex >= this.Length - 1)
				return;
			if (currentIndex < 0 || currentIndex >= this.Length - 1)
				return;
			if (currentIndex == newIndex)
				return;

			FITSHeaderKey tempkey = HEADERKEYS[currentIndex];
			JPFITS.FITSHeaderKey[] keys = new JPFITS.FITSHeaderKey[this.Length];

			int c = 0;
			for (int i = 0; i < this.Length; i++)
			{
				if (i == currentIndex)
					c++;

				if (i == newIndex)
				{
					keys[i] = tempkey;
					continue;
				}

				keys[i] = HEADERKEYS[c];
				c++;
			}

			HEADERKEYS = keys;
			UPDATEDISPLAYHEADER = true;
		}

		/// <summary>RemoveKey removes the key at the given index from the primary header.</summary>
		/// <param name="KeyIndex">The zero-based index of the key to remove. If the index is outside of the range of the header nothing happens.</param>
		public void RemoveKey(int KeyIndex)
		{
			if (KeyIndex < 0 || KeyIndex > this.Length - 1)
				return;

			int c = -1;
			JPFITS.FITSHeaderKey[] keys = new JPFITS.FITSHeaderKey[this.Length - 1];

			for (int i = 0; i < this.Length; i++)
			{
				if (i == KeyIndex)
					continue;
				c++;
				keys[c] = HEADERKEYS[i];
			}
			HEADERKEYS = keys;
			UPDATEDISPLAYHEADER = true;
		}

		/// <summary>RemoveKey removes the given key from the primary header. If there is more than one key with the given name, only the first occurence will be removed.</summary>
		/// <param name="Key">The name of the header key to remove.</param>
		public void RemoveKey(string Key)
		{
			RemoveKey(GetKeyIndex(Key, false));
		}

		/// <summary>RemoveKey removes the given key with matching value from the primary header. If there is more than one key with the given name and value, only the first occurence will be removed.</summary>
		/// <param name="Key">The name of the header key to remove.</param>
		/// <param name="Value">The corresponding header key value to remove.</param>
		public void RemoveKey(string Key, string Value)
		{
			RemoveKey(GetKeyIndex(Key, Value));
		}

		public void RemoveKeys(string[] keyNames)
		{
			for (int i = 0; i < keyNames.Length; i++)
				RemoveKey(keyNames[i]);
		}

		/// <summary>RemoveAllKeys clears all keys from the primary header. Essential keywords will remain. image is supplied to re-create essential keywords, or pass nullptr to set NAXIS = 0.</summary>
		public void RemoveAllKeys(double[,]? image)
		{
			MAKE_DEFAULT_HEADER(true, image);
			UPDATEDISPLAYHEADER = true;
		}

		#endregion

		#region STATIC HEADER INTERACTION
		public static DiskPrecision GetHeaderTypeCode(FITSHeader header)
		{
			int BITPIX = Convert.ToInt32(header.GetKeyValue("BITPIX"));

			int BZERO;
			string bzero = header.GetKeyValue("BZERO");
			if (bzero == "")
				BZERO = 0;
			else
				BZERO = Convert.ToInt32(bzero);

			switch (BITPIX)
			{
				case 8:
				{
					if (BZERO == -128)
						return DiskPrecision.SByte;
					else
						return DiskPrecision.Byte;
				}

				case 16:
				{
					if (BZERO == 0)
						return DiskPrecision.Int16;
					else
						return DiskPrecision.UInt16;
				}

				case 32:
				{
					if (BZERO == 0)
						return DiskPrecision.Int32;
					else
						return DiskPrecision.UInt32;
				}

				case 64:
				{
					if (BZERO == 0)
						return DiskPrecision.Int64;
					else
						return DiskPrecision.UInt64;
				}

				case -32:
					return DiskPrecision.Single;

				case -64:
					return DiskPrecision.Double;

				default:
					throw new Exception(String.Format("Problem with BITPIX {0} or BZERO {1}", BITPIX, BZERO));
			}
		}

		private static string[] INVALIDEDITKEYS = { "SIMPLE", "EXTEND", "BITPIX", "NAXIS", "BZERO", "BSCALE", "END", "PCOUNT", "GCOUNT", "THEAP", "GROUPS", "XTENSION", "TFIELDS", "TUNIT", "TFORM", "TTYPE", "TZERO", "TSCAL" };

		/// <summary>Returns whether the given key is NOT an essential key and therefore is OKAY to modify, copy, etc.</summary>
		/// <param name="key">The name of the header key.</param>
		public static bool ValidKeyEdit(string key, bool showMessageBox)
		{
			for (int i = 0; i < INVALIDEDITKEYS.Length; i++)
				if (key.Contains(INVALIDEDITKEYS[i]))
				{
					if (showMessageBox)
						System.Windows.Forms.MessageBox.Show("Selected key: '" + key + "' is restricted. Operation not allowed.", "Warning...");
					return false;
				}

			return true;
		}

		#endregion

		#region PRIVATE MEMBERS

		private FITSHeaderKey[]? HEADERKEYS;
		private string[]? FORMATTEDHEADER;
		private bool UPDATEDISPLAYHEADER = true;
		private bool ISEXTENSION = false;		

		/// <summary>
		/// Make a header with essential keywords.
		/// </summary>
		/// <param name="mayContainExtensions">Include the EXTEND keyword with value T</param>
		/// <param name="dataUnit">The data unit array. Pass null for NAXIS = 0.</param>
		private void MAKE_DEFAULT_HEADER(bool mayContainExtensions, Array? dataUnit)
		{
			FITSFILEOPS.GetBitpixNaxisnBscaleBzero(DiskPrecision.Double, dataUnit, out int BITPIX, out int[] NAXISN, out double BSCALE, out double BZERO);

			if (mayContainExtensions && dataUnit == null)
				HEADERKEYS = new FITSHeaderKey[5];
			else if (mayContainExtensions && dataUnit != null)
				HEADERKEYS = new FITSHeaderKey[7 + NAXISN.Length];
			else if (!mayContainExtensions && dataUnit == null)
				HEADERKEYS = new FITSHeaderKey[4];
			else if (!mayContainExtensions && dataUnit != null)
				HEADERKEYS = new FITSHeaderKey[6 + NAXISN.Length];

			int c = 0;
			HEADERKEYS[c++] = new FITSHeaderKey("SIMPLE", "T", "file conforms to FITS standard");
			HEADERKEYS[c++] = new FITSHeaderKey("BITPIX", BITPIX.ToString(), "bits per pixel");
			HEADERKEYS[c++] = new FITSHeaderKey("NAXIS", NAXISN.Length.ToString(), "number of data axes");

			if (dataUnit != null)
			{
				for (int i = 0; i < dataUnit.Rank; i++)
					HEADERKEYS[c++] = new FITSHeaderKey("NAXIS" + (i + 1).ToString(), dataUnit.GetLength(i).ToString(), "number of elements on this axis");

				HEADERKEYS[c++] = new FITSHeaderKey("BZERO", BZERO.ToString(), "data offset");
				HEADERKEYS[c++] = new FITSHeaderKey("BSCALE", BSCALE.ToString(), "data scaling");
			}
			if (mayContainExtensions)
				HEADERKEYS[c++] = new FITSHeaderKey("EXTEND", "T", "file may contain extensions");

			HEADERKEYS[c++] = new FITSHeaderKey("END");
			UPDATEDISPLAYHEADER = true;
		}

		#endregion

	}
}
