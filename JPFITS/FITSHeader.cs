using System;
using System.IO;
using System.Collections;

namespace JPFITS
{
	/// <summary>FITSImageHeader class for managing FITS file Primary Image headers.</summary>
	public class FITSHeader
	{
		private JPFITS.FITSHeaderKey[]? HEADERKEYS;
		private string[]? FORMATTEDHEADER;
		private bool UPDATEDISPLAYHEADER = true;
		private bool ISEXTENSION = false;

		/// <summary>Constructor. Creates an instance of a FITSImageHeader, with options to indicate whether extensions are present, and sets essential keywords for a given image it will be the header for.
		/// <para>If the image is to be an extension, then use GetFormattedHeaderBlock to pull the header out with SIMPLE = T changed to XTENSION = IMAGE for writing.</para></summary>
		/// <param name="mayContainExtensions">If true, heyword EXTEND = T is added, otherwise it is left out.</param>
		/// <param name="image">If image is nullptr, then NAXIS = 0 and there are no NAXISn keywords or BSCALE or BZERO. Otherwise NAXIS, NAXISn, BSCALE and BZERO are set as per the image dimensions.
		/// <para>If the image will be saved at a different precision than double, use SetBITPIXNAXISBSCZ(precision, image) at write time.</para></param>
		public FITSHeader(bool mayContainExtensions, double[,]? image)
		{
			MAKE_DEFAULT_HEADER(mayContainExtensions, image);
		}

		/// <summary>Constructor. Creates an instance of a FITSImageHeader out of a list of header lines. Typically the headerlines would be returned from FITSFILEOPS.SCANPRIMARYUNIT.</summary>
		/// <param name="headerlines">A String list of header lines to be extrated and formatted into keys, values, and comments, or as comment lines.</param>
		/// <param name="populate_nonessential">If false, non-essential key lines will be ignored. Saves a little bit of construction time if you don't need those, but they'll be lost if you re-write the file without them.</param>
		public FITSHeader(ArrayList headerlines, bool populate_nonessential)
		{
			if (populate_nonessential)
				HEADERKEYS = new JPFITS.FITSHeaderKey[headerlines.Count];
			else
			{
				int N = 10;
				if (headerlines.Count < 10)
					N = headerlines.Count;
				HEADERKEYS = new JPFITS.FITSHeaderKey[N];
			}

			for (int i = 0; i < HEADERKEYS.Length; i++)
				HEADERKEYS[i] = new FITSHeaderKey((string)headerlines[i]);
		}

		public FITSHeader(string fileName)
		{
			FileStream fs = new FileStream(fileName, FileMode.Open);
			ArrayList headerlines = new ArrayList();
			bool ext = false;
			FITSFILEOPS.SCANPRIMARYUNIT(fs, false, ref headerlines, ref ext);
			fs.Close();

			HEADERKEYS = new JPFITS.FITSHeaderKey[headerlines.Count];
			for (int i = 0; i < HEADERKEYS.Length; i++)
				HEADERKEYS[i] = new FITSHeaderKey((string)headerlines[i]);
		}

		/// <summary>GetKeyName returns the key of the primary header line at index. Returns empty String if the index exceeds the number of header lines.</summary>
		/// <param name="index">The zero-based line number to get the key name from.</param>
		public string GetKeyName(int index)
		{
			return HEADERKEYS[index].Name;
		}

		/// <summary>GetKeyValue returns the value of the primary header line at index. Returns empty String if the index exceeds the number of header lines.</summary>
		/// <param name="index">The zero-based line number to get the key value from.</param>
		public string GetKeyValue(int index)
		{
			return HEADERKEYS[index].Value;
		}

		/// <summary>GetKeyComment returns the comment of the primary header line at index. Returns empty String if the index exceeds the number of header lines.</summary>
		/// <param name="index">The zero-based line number to get the key comment from.</param>
		public string GetKeyComment(int index)
		{
			return HEADERKEYS[index].Comment;
		}

		/// <summary>GetKeyValue returns the value of the primary header key named Key. Returns empty String if the key is not found.</summary>
		/// <param name="key">The header key to find the value of.</param>
		public string GetKeyValue(string key)
		{
			string result = "";
			bool brek = false;

			for (int i = 0; i < this.Length; i++)
				if (brek)
					break;
				else if (key == HEADERKEYS[i].Name)
				{
					result = HEADERKEYS[i].Value;
					brek = true;
				}

			return result;
		}

		/// <summary>GetKeyComment returns the comment of the primary header key named Key. Returns empty String if the key is not found.</summary>
		/// <param name="key">The header key to find the comment of.</param>
		public string GetKeyComment(string key)
		{
			string result = "";
			bool brek = false;

			for (int i = 0; i < this.Length; i++)
				if (brek)
					break;
				else if (key == HEADERKEYS[i].Name)
				{
					result = HEADERKEYS[i].Comment;
					brek = true;
				}

			return result;
		}

		/// <summary>GetKeyIndex returns the zero-based index in the primary header of the key named Key. Returns -1 if the key is not found.</summary>
		/// <param name="key">The header key to find the index of.</param>
		/// <param name="KeyIsFullLineFormatted">If true then the entire formatted 80-element long line is compared - helpful if multiple keys have the same name or are formatted as comment lines. If false then only the key name is used.</param>
		public int GetKeyIndex(string key, bool KeyIsFullLineFormatted)
		{
			int result = -1;
			bool brek = false;

			if (!KeyIsFullLineFormatted)
			{
				for (int i = 0; i < this.Length; i++)
					if (brek)
						break;
					else if (key == HEADERKEYS[i].Name)
					{
						result = i;
						brek = true;
					}
			}
			else
			{
				for (int i = 0; i < this.Length; i++)
					if (brek)
						break;
					else if (key == this[i].GetFullyFomattedFITSLine())
					{
						result = i;
						brek = true;
					}
			}

			return result;
		}

		/// <summary>GetKeyIndex returns the zero-based index in the primary header of the key with matching value. Returns -1 if the key and value combination is not found.</summary>
		/// <param name="key">The header key to find the index of.</param>
		/// <param name="keyvalue">The header key value to find the index of.</param>
		public int GetKeyIndex(string key, string keyvalue)
		{
			int result = -1;
			bool brek = false;

			for (int i = 0; i < this.Length; i++)
				if (brek)
					break;
				else if (key == HEADERKEYS[i].Name)
					if (keyvalue == HEADERKEYS[i].Value)
					{
						result = i;
						brek = true;
					}

			return result;
		}

		/// <summary>GetKeyIndex returns the zero-based index in the primary header of the key with matching value and comment. Returns -1 if the key, value and comment combination is not found.</summary>
		/// <param name="key">The header key to find the index of.</param>
		/// <param name="keyvalue">The header key value to find the index of.</param>
		/// <param name="keycomment">The header key comment to find the index of.</param>
		public int GetKeyIndex(string key, string keyvalue, string keycomment)
		{
			int result = -1;
			bool brek = false;

			for (int i = 0; i < this.Length; i++)
				if (brek)
					break;
				else if (key == HEADERKEYS[i].Name)
					if (keyvalue == HEADERKEYS[i].Value)
						if (keycomment == HEADERKEYS[i].Comment)
						{
							result = i;
							brek = true;
						}

			return result;
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
		/// <para>If the length of the commentKeyLine is more than 80 elements, the comment will be continued on subsequent lines until depleted.</para>
		/// <para>If the user wishes the line to begin with COMMENT, then write the input commentKeyLine beginning as such.</para>
		/// <para>If the user wishes the line to be blank, then pass commentKeyLine as an empty string or as only containing blanks (whitespace).</para></summary>
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

		/// <summary>Move a fit key line to another position in the header.</summary>
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
		public void RemoveAllKeys(double[,] image)
		{
			MAKE_DEFAULT_HEADER(true, image);
			UPDATEDISPLAYHEADER = true;
		}

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

			//END
			newheaderkeys[newheaderkeys.Length - 1] = new FITSHeaderKey("END");

			HEADERKEYS = newheaderkeys;
			UPDATEDISPLAYHEADER = true;
		}

		/// <summary>This sets the BITPIX, NAXIS, NAXISn, BSCALE and BZERO keywords of the header given the TypeCode and the image. If the image is null then NAXIS = 0 and any NAXISn keywords are removed as well as BSCALE and BZERO.</summary>
		public void SetBITPIXNAXISBSCZ(TypeCode precision, double[,]? image)
		{
			UPDATEDISPLAYHEADER = true;
			if (image == null || image.Length == 0)
			{
				SetKey("BITPIX", "8", false, 0);
				SetKey("NAXIS", "0", false, 0);
				RemoveKey("NAXIS1");
				RemoveKey("NAXIS2");
				RemoveKey("BZERO");
				RemoveKey("BSCALE");
				return;
			}
			else
			{
				SetKey("NAXIS", "2", "Number of image axes", true, 2);
				SetKey("NAXIS1", image.GetLength(0).ToString(), true, 3);
				SetKey("NAXIS2", image.GetLength(1).ToString(), true, 4);
			}

			switch (precision)
			{
				case TypeCode.SByte:
					{
						SetKey("BITPIX", "8", false, 0);
						SetKey("BZERO", "0", "Data Offset; pixel = pixel*BSCALE+BZERO", true, 5);
						SetKey("BSCALE", "1", "Data Scaling; pixel = pixel*BSCALE+BZERO", true, 6);
						break;
					}

				case TypeCode.Byte:
					{
						SetKey("BITPIX", "8", false, 0);
						SetKey("BZERO", "128", "Data Offset; pixel = pixel*BSCALE+BZERO", true, 5);
						SetKey("BSCALE", "1", "Data Scaling; pixel = pixel*BSCALE+BZERO", true, 6);
						break;
					}

				case TypeCode.Int16:
					{
						SetKey("BITPIX", "16", false, 0);
						SetKey("BZERO", "0", "Data Offset; pixel = pixel*BSCALE+BZERO", true, 5);
						SetKey("BSCALE", "1", "Data Scaling; pixel = pixel*BSCALE+BZERO", true, 6);
						break;
					}

				case TypeCode.UInt16:
					{
						SetKey("BITPIX", "16", false, 0);
						SetKey("BZERO", "32768", "Data Offset; pixel = pixel*BSCALE+BZERO", true, 5);
						SetKey("BSCALE", "1", "Data Scaling; pixel = pixel*BSCALE+BZERO", true, 6);
						break;
					}

				case TypeCode.Int32:
					{
						SetKey("BITPIX", "32", false, 0);
						SetKey("BZERO", "0", "Data Offset; pixel = pixel*BSCALE+BZERO", true, 5);
						SetKey("BSCALE", "1", "Data Scaling; pixel = pixel*BSCALE+BZERO", true, 6);
						break;
					}

				case TypeCode.UInt32:
					{
						SetKey("BITPIX", "32", false, 0);
						SetKey("BZERO", "2147483648", "Data Offset; pixel = pixel*BSCALE+BZERO", true, 5);
						SetKey("BSCALE", "1", "Data Scaling; pixel = pixel*BSCALE+BZERO", true, 6);
						break;
					}

				case TypeCode.Int64:
					{
						SetKey("BITPIX", "64", false, 0);
						SetKey("BZERO", "0", "Data Offset; pixel = pixel*BSCALE+BZERO", true, 5);
						SetKey("BSCALE", "1", "Data Scaling; pixel = pixel*BSCALE+BZERO", true, 6);
						break;
					}

				case TypeCode.UInt64:
					{
						SetKey("BITPIX", "64", false, 0);
						SetKey("BZERO", "9223372036854775808", "Data Offset; pixel = pixel*BSCALE+BZERO", true, 5);
						SetKey("BSCALE", "1", "Data Scaling; pixel = pixel*BSCALE+BZERO", true, 6);
						break;
					}

				case TypeCode.Single:
					{
						SetKey("BITPIX", "-32", false, 0);
						SetKey("BZERO", "0", "Data Offset; pixel = pixel*BSCALE+BZERO", true, 5);
						SetKey("BSCALE", "1", "Data Scaling; pixel = pixel*BSCALE+BZERO", true, 6);
						break;
					}

				case TypeCode.Double:
					{
						SetKey("BITPIX", "-64", false, 0);
						SetKey("BZERO", "0", "Data Offset; pixel = pixel*BSCALE+BZERO", true, 5);
						SetKey("BSCALE", "1", "Data Scaling; pixel = pixel*BSCALE+BZERO", true, 6);
						break;
					}

				default:
					{
						throw new Exception("TypeCode '" + precision.ToString() + "' not recognized at SetBITPIXNAXISBSCZ.");
					}
			}
		}

		/// <summary>Returns a formatted header block with the existing keys, and sets the first key to either SIMPLE = T or XTENSION = IMAGE. If a full 2880-multiple block is needed, set keysOnly to false.</summary>
		/// <param name="isExtension">If true then the first keyword is set to XTENSION = IMAGE, otherwise it is SIMPLE = T.</param>
		/// <param name="keysOnly">If true then only the existing keywords are returned formatted, otherwise if you need the entire 2880-multiple block pass false. True typically needed for display, false typically needed for writing.</param>
		public string[] GetFormattedHeaderBlock(bool isExtension, bool keysOnly)
		{
			if (isExtension && !ISEXTENSION)
			{
				UPDATEDISPLAYHEADER = true;
				ISEXTENSION = true;

				this.RemoveKey("EXTEND");

				HEADERKEYS[0].Name = "XTENSION";
				HEADERKEYS[0].Value = "IMAGE";
				HEADERKEYS[0].Comment = "Image extension";

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
			else if (!isExtension && ISEXTENSION)
			{
				UPDATEDISPLAYHEADER = true;
				ISEXTENSION = false;
				HEADERKEYS[0].Name = "SIMPLE";
				HEADERKEYS[0].Value = "T";
				HEADERKEYS[0].Comment = "file conforms to FITS standard.";
			}

			if (!UPDATEDISPLAYHEADER && !keysOnly)
				return FORMATTEDHEADER;

			UPDATEDISPLAYHEADER = false;

			if (keysOnly)
				FORMATTEDHEADER = new string[this.Length];
			else
			{
				int NKeys = this.Length;
				int NCards = (NKeys - 1) / 36;
				FORMATTEDHEADER = new string[(NCards + 1) * 36];
			}

			for (int i = 0; i < this.Length; i++)
				FORMATTEDHEADER[i] = HEADERKEYS[i].GetFullyFomattedFITSLine();

			if (keysOnly)
				return FORMATTEDHEADER;

			string empty = "";

			for (int i = this.Length; i < FORMATTEDHEADER.Length; i++)
				FORMATTEDHEADER[i] = empty.PadLeft(80);

			return FORMATTEDHEADER;
		}

		/// <summary>ValidKeyEdit returns whether the given key is an essential key and shouldn't be user-modified.</summary>
		/// <param name="key">The name of the header key.</param>
		public static bool ValidKeyEdit(string key, bool showMessageBox)
		{
			for (int i = 0; i < INVALIDEDITKEYS.Length; i++)
				if (key == INVALIDEDITKEYS[i])
				{
					if (showMessageBox)
						System.Windows.Forms.MessageBox.Show("Selected key: '" + key + "' is restricted. Operation not allowed.", "Warning...");
					return false;
				}

			return true;
		}

		/// <summary>Returns all of the key names from the header.</summary>
		public string[] GetAllKeyNames()
		{
			string[] names = new string[this.Length];
			for (int i = 0; i < this.Length; i++)
				names[i] = this[i].Name;
			return names;
		}

		/// <summary>Returns the FITSHeaderKey object at a given zero-based index in the header.</summary>
		public FITSHeaderKey this[int i]
		{
			get { return HEADERKEYS[i]; }
			set { HEADERKEYS[i] = value; }
		}

		/// <summary>Returns the number of header key lines in the header, excluding any assumed padding after END key.</summary>
		public int Length
		{
			get { return HEADERKEYS.Length; }
		}		

		private static string[] INVALIDEDITKEYS = { "SIMPLE", "EXTEND", "BITPIX", "NAXIS", "NAXIS1", "NAXIS2", "BZERO", "BSCALE", "END", "PCOUNT", "GCOUNT", "THEAP", "GROUPS", "XTENSION", "TFIELDS" };

		private void MAKE_DEFAULT_HEADER(bool mayContainExtensions, double[,]? image)
		{
			if (mayContainExtensions && image == null)
				HEADERKEYS = new FITSHeaderKey[5];
			else if (mayContainExtensions && image != null)
				HEADERKEYS = new FITSHeaderKey[9];
			else if (!mayContainExtensions && image == null)
				HEADERKEYS = new FITSHeaderKey[4];
			else  //(!mayContainExtensions && image != nullptr)
				HEADERKEYS = new FITSHeaderKey[8];

			long BZERO = 0, BSCALE = 1;
			int BITPIX = -64, NAXIS = 0, NAXIS1 = 0, NAXIS2 = 0;
			if (image != null)
			{
				NAXIS = 2;
				NAXIS1 = image.GetLength(0);
				NAXIS2 = image.GetLength(1);
			}

			int c = 0;
			HEADERKEYS[c++] = new FITSHeaderKey("SIMPLE", "T", "file conforms to FITS standard");
			HEADERKEYS[c++] = new FITSHeaderKey("BITPIX", BITPIX.ToString(), "bits per pixel");
			HEADERKEYS[c++] = new FITSHeaderKey("NAXIS", NAXIS.ToString(), "number of data axes");

			if (image != null)
			{
				HEADERKEYS[c++] = new FITSHeaderKey("NAXIS1", NAXIS1.ToString(), "width - number of data columns");
				HEADERKEYS[c++] = new FITSHeaderKey("NAXIS2", NAXIS2.ToString(), "height - number of data rows");
				HEADERKEYS[c++] = new FITSHeaderKey("BZERO", BZERO.ToString(), "data offset");
				HEADERKEYS[c++] = new FITSHeaderKey("BSCALE", BSCALE.ToString(), "data scaling");
			}
			if (mayContainExtensions)
				HEADERKEYS[c++] = new FITSHeaderKey("EXTEND", "T", "file may contain extensions");

			HEADERKEYS[c++] = new FITSHeaderKey("END");
			UPDATEDISPLAYHEADER = true;
		}
	}
}
