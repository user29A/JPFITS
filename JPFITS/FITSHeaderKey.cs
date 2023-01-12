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
		/// </summary>
		/// <param name="line">The header key line,up to 80 elements in length.</param>
		public FITSHeaderKey(string line)
		{
			if (line.Length > 80)
			{
				throw new Exception("Header line String: \r\r'" + line + "'\r\r is is longer than 80 characters. The String must contain eighty (or less) elements.");
			}
			else if (line.Length < 80)
				line = line.PadRight(80);

			try
			{
				NAME = line.Substring(0, 8).Trim();
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
					if (JPMath.IsNumeric(line.Substring(10, 20)))//this has to work if it is supposed to be a numeric value here
						VALUE = line.Substring(10, 20).Trim();//get rid of leading and trailing white space
					else if (line.Substring(10, 20).Trim() == "T" || line.Substring(10, 20).Trim() == "F")
						VALUE = line.Substring(10, 20).Trim();
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
						}
					}

					int ind = line.IndexOf("/");
					if (ind != -1)
						COMMENT = line.Substring(ind + 1).Trim();
					else
						COMMENT = "";
				}
			}
			catch
			{
				throw new Exception("Header line: \r\r'" + line + "'\r\r is very poorly formatted. Continuing should allow the file to be processed but the header will not likely contain whatever was intended.");
			}
		}

		/// <summary>
		/// Creates an instance of a FITSHeaderKey, out of a supplied key name, key value, and key comment. If name is either "COMMENT" or an empty String the key will be formatted as a FITS comment line using value and comment as the comment.
		/// </summary>
		/// <param name="name">The header key name.</param>
		/// <param name="value">The header key value.</param>
		/// <param name="comment">The header key comment.</param>
		public FITSHeaderKey(string name, string value, string comment)
		{
			if (name.Length > 8)
			{
				throw new Exception("Header line name '" + name + "' cannot exceed 8 characters in length; it is " + name.Length + " characters long.");
			}

			if (name.Trim().Equals("COMMENT") || name.Length == 0)
			{
				LINEISCOMMENTFORMAT = true;
				NAME = "";
				VALUE = "";
				COMMENT = name + value + comment;
			}
			else
			{
				LINEISCOMMENTFORMAT = false;
				NAME = name;
				VALUE = value;
				COMMENT = comment;
			}
		}

		/// <summary>The name of the key (elements 1 through 8).</summary>
		public string Name
		{
			get { return NAME; }
			set { NAME = value; }
		}

		/// <summary>The value of the key (elements 11 through 30 typically).</summary>
		public string Value
		{
			get { return VALUE; }
			set { VALUE = value; }
		}

		/// <summary>The comment of the key (elements 34 through 80 typically). If the key is a comment then Comment contains the entire key line.</summary>
		public string Comment
		{
			get { return COMMENT; }
			set { COMMENT = value; }
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
				if (VALUE != "")
				{
					throw new Exception("Error: Line was indicated as comment but the value field is not empty. key MAY equal COMMENT and comment string MUST be supplied in comment, and value must be empty if it is a comment line.");
				}
				else
					if ((NAME.Length + COMMENT.Length) > 80)
				{
					throw new Exception("Error: key name and comment are more than 80 elements: '" + NAME + COMMENT + "' are " + (NAME.Length + COMMENT.Length) + " elements.");
				}
				else
					return (NAME + COMMENT).PadRight(80);

			if (NAME.Length > 8)
			{
				throw new Exception("Error: key name was supplied with more than 8 characters: '" + NAME + "' is " + NAME.Length + " elements.");
			}

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
				else if (Math.Abs(val) <= 1e-6 || Math.Abs(val) >= 1e13)
					value = val.ToString("0.00###########e+00");
				else
					value = val.ToString("G");
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
