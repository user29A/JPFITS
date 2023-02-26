using System;

namespace JPFITS
{
	public class FITSHeaderKey
	{
		private string NAME;
		private string VALUE;
		private string COMMENT;
		private bool LINEISCOMMENTFORMAT;

		/// <summary>
		/// Creates an instance of a FITSHeaderKey, out of a supplied String line. The line may be empty, but not more than 80 elements length. The key will be formatted as a FITS comment line if its internal structure appears to be configured as such.
		/// <br /> Comment lines are indicated either with COMMENT as the first characters, and, or, no equal sign at element position 9, 0-based index 8.
		/// </summary>
		/// <param name="line">The header key line, up to 80 elements in length.</param>
		public FITSHeaderKey(string line)
		{
			if (line.Length > 80)
				throw new Exception("Header line String: \r\r'" + line + "'\r\r is longer than 80 characters. A header line string must contain eighty (or less) elements.");

			line = line.PadRight(80);

			try
			{
				NAME = line.Substring(0, 8).Trim().ToUpper();
				if (NAME == "END")
				{
					LINEISCOMMENTFORMAT = false;
					VALUE = "";
					COMMENT = "";
					return;
				}
				else if (line.Substring(8, 1) != "=")
				{
					LINEISCOMMENTFORMAT = true;
					NAME = "";
					VALUE = "";
					COMMENT = line.Trim();
					return;
				}
				else
				{
					LINEISCOMMENTFORMAT = false;
					if (line.Substring(10, 1) != "'")//must be numeric or T/F then
					{
						if (line.Substring(10, 20).Trim() == "T" || line.Substring(10, 20).Trim() == "F")
							VALUE = line.Substring(10, 20).Trim();
						else
						{
							int slashind = line.IndexOf("/", 10);
							if (slashind == -1)
								VALUE = line.Substring(10).Trim();//get rid of leading and trailing white space
							else
								VALUE = line.Substring(10, slashind - 10 - 1).Trim();
						}
					}
					else
					{
						int indx = line.IndexOf("'");
						if (indx == -1)
						{
							LINEISCOMMENTFORMAT = true;
							NAME = "";
							VALUE = "";
							COMMENT = line.Trim();
							return;
						}
						else
						{
							VALUE = line.Substring(indx + 1, line.LastIndexOf("'") - indx - 1);
							VALUE = VALUE.Trim();
							//if (VALUE.Length > 18)
							//	VALUE = VALUE.Substring(0, 18);
						}
					}

					int ind = line.IndexOf("/", 10);
					if (ind != -1)
						COMMENT = line.Substring(ind + 1).Trim();
					else
						COMMENT = "";
				}
			}
			catch
			{
				throw new Exception("Header line: \r\r'" + line + "'\r\r is very poorly formatted.");
			}
		}

		/// <summary>
		/// Creates an instance of a FITSHeaderKey, out of a supplied key name, key value, and key comment. If name is either "COMMENT" or an empty String the key will be formatted as a FITS comment line using comment.
		/// </summary>
		/// <param name="name">The header key name, maximum 8 characters in length, all caps; excess elements will be truncated. Use COMMENT or pass empty string for a comment-formatted line with the value empty and comment as the comment line.</param>
		/// <param name="value">The header key value, maximum 18 characters in length; excess elements will be truncated. Leave empty if it is a comment line.</param>
		/// <param name="comment">The header key comment, maximum 80 characters if it is the entire comment line, or maximum 73 characters if name is COMMENT; excess elements will be truncated.</param>
		public FITSHeaderKey(string name, string value, string comment)
		{
			NAME = name.Trim().ToUpper();
			VALUE = value.Trim();
			COMMENT = comment.Trim();

			if (NAME.Length > 8)
				NAME = NAME.Substring(0, 8).Trim();
				//throw new Exception("Header line name '" + NAME + "' cannot exceed 8 characters in length; it is " + NAME.Length + " characters long.");

			if (NAME.Equals("COMMENT") || NAME.Length == 0)
			{
				if (VALUE != "")
					throw new Exception("Key value + '" + VALUE + "' should be empty if the line is a comment line.");

				LINEISCOMMENTFORMAT = true;
				COMMENT = NAME + COMMENT;
				NAME = "";

				if (COMMENT.Length > 80)
					COMMENT = COMMENT.Substring(0, 80);
					//throw new Exception("Header line string: \r\r'" + COMMENT + "'\r\r is longer than 80 characters. The string must contain eighty (or less) elements.");
			}
			else
			{
				//if (!ValueIsNumeric() && VALUE.Length > 18)
				//	VALUE = VALUE.Substring(0, 18);
					//throw new Exception("Error: Key value cannot exceed 18 characters. Key value: '" + VALUE + "' is " + VALUE.Length + " characters long.");

				LINEISCOMMENTFORMAT = false;
			}
		}

		/// <summary>The name of the key (elements 1 through 8).</summary>
		public string Name
		{
			get { return NAME; }
			set 
			{
				if (value.Length > 8)
					throw new Exception("Error: Key name '" + value + "' cannot exceed 8 characters in length; it is " + value.Length + " characters long.");
				NAME = value.Trim().ToUpper();
			}
		}

		/// <summary>The value of the key (elements 12 through 30 typically).</summary>
		public string Value
		{
			get { return VALUE; }
			set 
			{
				if (!JPMath.IsNumeric(value) && value.Trim().Length > 18)
					throw new Exception("Error: Key value cannot exceed 18 characters. Key value: '" + value.Trim() + "' is " + value.Trim().Length + " characters long.");
				VALUE = value.Trim();
			}
		}

		/// <summary>The comment of the key (elements 32 through 80 typically). If the key is a comment then Comment contains the entire key line.</summary>
		public string Comment
		{
			get { return COMMENT; }
			set 
			{
				if (!LINEISCOMMENTFORMAT && value.Trim().Length > 48)
					throw new Exception("Error: Key comment value cannot exceed 48 characters. Comment value: '" + value.Trim() + "' is " + value.Trim().Length + " characters long.");
				else if (LINEISCOMMENTFORMAT && (NAME + value.Trim()).Length > 80)
					throw new Exception("Entire comment line cannot be more than 80 characters. Comment value: '" + NAME + value.Trim() + "' is " + (NAME + value.Trim()).Length + " characters long.");
				COMMENT = value.Trim();
			}
		}

		/// <summary>Returns whether the key line is a comment.</summary>
		public bool IsCommentKey
		{
			get { return LINEISCOMMENTFORMAT; }
		}

		/// <summary>Generates and returns a fully formatted FITS key as an 80-element String. Can be used to build a header.</summary>
		public string GetFullyFomattedFITSLine()
		{
			return GETFULLYFORMATTEDFITSLINE();
		}

		/// <summary>Returns whether the key value is numeric.</summary>
		public bool ValueIsNumeric()
		{
			return JPMath.IsNumeric(VALUE);
		}

		/// <summary>Returns the key value as a double. Will throw a conversion exception if the value is not numeric.</summary>
		/// <returns></returns>
		public double ValueAsDouble()
		{
			return Convert.ToDouble(VALUE);
		}

		private string GETFULLYFORMATTEDFITSLINE()
		{
			if (LINEISCOMMENTFORMAT)
				return (NAME + COMMENT).PadRight(80);

			if (NAME.Trim() == "END")
				return NAME.Trim().PadRight(80);

			string key = NAME.PadRight(8);
			key += "= ";
			//key name formatting done

			//do value formatting
			string value;
			if (JPMath.IsNumeric(VALUE))//then we have a numeric key value
			{
				double val = Convert.ToDouble(VALUE);

				if (val == 9223372036854775808)//this is the bzero for unsigned int64...and is so large it will get converted to exponential notation which we do not want for this
					value = "9223372036854775808";
				else if (Math.Abs(val) <= 1e-4 || Math.Abs(val) >= 1e13)
					value = val.ToString("0.000##########e+00");
				else
					value = val.ToString("0.#################");

				if (val == 0)//change the exp to integer 0
					value = "0";

				if (value.Length < 20)
					value = value.PadLeft(20);
			}
			else//else it must be a string
			{
				if (VALUE.Trim() == "T" || VALUE.Trim() == "F")
					value = VALUE.Trim().PadLeft(20);
				else
					value = "'" + VALUE.PadRight(18) + "'";
			}
			//value formatting done

			string comment = COMMENT;

			comment = " / " + comment;//comment formatting done
			string line = key + value + comment;
			if (line.Length > 80)
				line = line.Substring(0, 80);
			else
				line = line.PadRight(80);

			return line;
		}
	}
}
