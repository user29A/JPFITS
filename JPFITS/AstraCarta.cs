/*  
    JPFITS: object-oriented FITS file interaction
    Copyright (C) 2023  Joseph E. Postma

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

	joepostma@live.ca
*/

using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;
using System.Globalization;
using System.Collections.Specialized;
using System.Linq;
#nullable enable

namespace JPFITS
{
	/// <summary>
	/// Provides query access to the Gaia Archive, for downloading catalogue regions given an RA, DEC, and field scale and size, etc. Used by WCSAutoSolver.
	/// </summary>
	public partial class AstraCarta : Form
	{
		string OUTFILE = "";
		bool OPENFROMARGS = false;
		bool EXECUTEONSHOW = false;
		bool PERFORMEXECUTE = false;
		bool CANCELLED = false;
		bool ERROR = false;
		bool CLOSEONCOMPLETE = false;

		public string Result_Filename
		{
			get { return OUTFILE; }
		}

		public bool ExecuteOnShow
		{
			get { return EXECUTEONSHOW; }
			set { EXECUTEONSHOW = value; }
		}

		public bool CloseOnComplete
		{
			get { return CloseOnCompleteChck.Checked; }
			set 
			{
				CloseOnCompleteChck.Checked = value;
			}
		}

		public static void Help(bool showmessagebox)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("\"maglimit\" Magnitude limit below which to flag bright sources and save to output table; less bright sources will be ignored." + Environment.NewLine);
			sb.AppendLine("\"buffer\" Tolerance buffer around image field, in arcminutes. This field can be negative, if one wishes to mitigate image padding." + Environment.NewLine);
			sb.AppendLine("\"offsetra\" Offset the center of the query region in right ascension. Units in arcminutes." + Environment.NewLine);
			sb.AppendLine("\"offsetdec\" Offset the center of the query region in declination. Units in arcminutes." + Environment.NewLine);
			sb.AppendLine("\"shape\" Shape of field to query: \"rectangle\" (default) or \"circle\". Rectangle query uses a polygon query with corners defined by an ad-hoc WCS given the supplied field parameters, whereas circle uses a radius." + Environment.NewLine);
			sb.AppendLine("\"rotation\" Field rotation, applicable to a rectangle query." + Environment.NewLine);
			sb.AppendLine("\"catalogue\" Catalogue or Service to query. Valid options are currently: \"GaiaDR3\" (default)." + Environment.NewLine);
			sb.AppendLine("\"filter\" Filter of the catalogue to sort on. Options are: for GaiaDR3: \"rp\", \"bp\", \"g\" (default)." + Environment.NewLine);
			sb.AppendLine("\"forcenew\" Force new astroquery. The raw query is saved with a filename based on a hash of the astroquery parameters, and therefore should be unique for unique queries, and the same for the same queries. The exception is for the \"entries\" option which cannot be hashed non-randomly. Therefore if everything else stays the same except for \"entries\", one would need to force a new query." + Environment.NewLine);
			sb.AppendLine("\"imageout\" Output an image field plot with maglimit sources marked." + Environment.NewLine);
			sb.AppendLine("\"imageshow\" Show the image field plot." + Environment.NewLine);
			sb.AppendLine("\"rawoutdir\" Directory to save raw querry files. By default raw files are saved in the user's appdata folder." + Environment.NewLine);
			sb.AppendLine("\"outdir\" Directory to save files. By default files are saved in the current working directory." + Environment.NewLine);
			sb.AppendLine("\"outname\" The name to use for output files. If not supplied a settings-consistent but random hash will be used for output file names. Existing filenames will not be overwritten." + Environment.NewLine);
			sb.AppendLine("\"overwrite\" Overwrite the output table if one is produced, instead of appending an instance number." + Environment.NewLine);
			sb.AppendLine("\"fitsout\" Output results table format as FITS binary table (instead of csv)." + Environment.NewLine);
			sb.AppendLine("\"rmvrawquery\" Remove the raw query folder and its contents after running. This will force future astroqueries." + Environment.NewLine);
			sb.AppendLine("\"nquery\" Number of brightest sources in the filter to retreive from the query service. Pass 0 to retreive all sources. Default 500." + Environment.NewLine);
			sb.AppendLine("\"pmepoch\" Pass the year.year value or the big-endian year string ex. 2013-06-12 of the observation to update the RA and Dec entries of the table with their proper motion adjustments, given the catalogue reference epoch. Only entries in the query which have valid proper motion entries will be saved to output." + Environment.NewLine);
			sb.AppendLine("\"pmlimit\" Limit the output to proper motions whose absolute value are less than pmlimit. Only valid if pmepoch is specified. Milliarcseconds per year." + Environment.NewLine);
			sb.AppendLine("\"entries\" A commaspace \", \" separated list of source columns to request from the query. Pass entries=\"all\" to get everything from the query source. Default is for GaiaDR3, entries=\"ref_epoch, ra, ra_err, dec, dec_err, pmra, pmra_err, pmdec, pmdec_err, pm, phot_bp_mean_mag, phot_g_mean_mag, phot_rp_mean_mag\". Thus, if entries is supplied, it appends additional entries to the default. For example if you additionally wanted the absolute value of proper motion errors then passing entries=\"pm_error\" would append \", pm_error\" to the string." + Environment.NewLine);
			sb.AppendLine("\"notableout\" Do not write an output file even when sources have been found. Useful if only wanting to view images but do not want to fill up a directory with table results." + Environment.NewLine);
			sb.AppendLine("\"silent\" Do not output process milestones to command window. Default false." + Environment.NewLine);

			Clipboard.SetText(sb.ToString());

			if (showmessagebox)
				MessageBox.Show(sb.ToString());
		}

        /// <summary>
        /// Perform a query with no user interface dialog form. Will throw an informative exception if something goes wrong. Returns a string which is the filename of the catalogue data downloaded. If nothing was found the string will be empty.
        /// </summary>
        /// <param name="RA">Right Ascension: degree.degree format; sexagesimal hh:mm:ss.s format (any delimiter acceptable); or the keyword of the FITS image header which contains the value in either format.</param>
        /// <param name="DEC">Declination: degree.degree format; sexagesimal dd:am:as.s format (any delimiter acceptable); or the keyword of the FITS image header which contains the value in either format.</param>
        /// <param name="SCALE">The plate scale in arcseconds per pixel, or the keyword of the FITS image header which contains the value.</param>
        /// <param name="fimg">The reference FITS image from which values may be extracted from the Header. Pass null if not required; pixwidth and pixheight must then be supplied.</param>
		/// <param name="pixwidth">The number of horizontal pixels of the image. Pass null if fimg is not null.</param>
		/// <param name="pixheight">The number of vertical pixels of the image. Pass null if fimg is not null.</param>
		/// <param name="ra">Give the RA back as its determined double value.</param>
		/// <param name="dec">Give the DEC back as its determined double value.</param>
		/// <param name="scale">Give the SCALE back as its determined double value.</param>
        /// <param name="args">Optional arguments list. Possible arguments can be found here: https://github.com/user29A/AstraCarta/wiki, or with AstraCarta.Help call.</param>
        public static string Query(string RA, string DEC, string SCALE, FITSImage? fimg, int? pixwidth, int? pixheight, out double ra, out double dec, out double scale, NameValueCollection? args = null)
		{
			WorldCoordinateSolution.RADecParse(RA, DEC, fimg, out ra, out dec);

            if (JPMath.IsNumeric(SCALE))
                scale = Convert.ToDouble(SCALE);
            else
            {
                if (fimg == null)
                    throw new Exception("FITSImage fimg in AstraCarta SCALE is null. Cannot proceed.");

                try
                {
                    scale = Convert.ToDouble(fimg.Header.GetKeyValue(SCALE));
                }
                catch
                {
                    throw new Exception("Cannot make sense of scale '" + SCALE + "'. Stopping AstraCarta.");
                }
            }

			if (args != null)
				if (!string.IsNullOrEmpty(args["pmepoch"]))
                    if (!JPMath.IsNumeric(args["pmepoch"]))
					{
						if (args["pmepoch"] == "DATE-OBS")
						{
                            if (fimg == null)
                                throw new Exception("FITSImage fimg in AstraCarta pmepoch is null. Cannot proceed.");

                            args["pmepoch"] = fimg.Header.GetKeyValue("DATE-OBS");
							if (string.IsNullOrEmpty(args["pmepoch"]))
								throw new Exception("pmepoch in AstraCarta, DATE-OBS keyword returned nothing from the FITS image header. Stopping AstraCarta");
                            if (!JPMath.IsNumeric(args["pmepoch"].Substring(0, 4)) || !JPMath.IsNumeric(args["pmepoch"].Substring(5, 2)) || !JPMath.IsNumeric(args["pmepoch"].Substring(8, 2)) || args["pmepoch"].Substring(4, 1) != "-" || args["pmepoch"].Substring(7, 1) != "-")
                                throw new Exception("'pmepoch=" + args["pmepoch"] + "' in AstraCarta invalid. Stopping AstraCarta");
                        }
						else if (!JPMath.IsNumeric(args["pmepoch"].Substring(0,4)) || !JPMath.IsNumeric(args["pmepoch"].Substring(5, 2)) || !JPMath.IsNumeric(args["pmepoch"].Substring(8, 2)) || args["pmepoch"].Substring(4, 1) != "-" || args["pmepoch"].Substring(7, 1) != "-")
							throw new Exception("'pmepoch=" + args["pmepoch"] + "' in AstraCarta invalid. Stopping AstraCarta");
					}

			if (fimg != null)
				return Query(ra, dec, scale, fimg.Width, fimg.Height, args);
			else if (pixwidth != null && pixheight != null) 
				return Query(ra, dec, scale, (int)pixwidth, (int)pixheight, args);
			else
                throw new Exception("AstraCarta.Query requires fimg or pixwidth & pixheight to not be null. Cannot proceed.");
        }

        /// <summary>
        /// Perform a query with no user interface dialog form. Will throw an informative exception if something goes wrong. Returns a string which is the filename of the catalogue data downloaded. If nothing was found the string will be empty.
        /// </summary>
        /// <param name="ra">The right acension of the field center. Degrees.</param>
        /// <param name="dec">The declination of the field center. Degrees.</param>
        /// <param name="scale">The plate scale in arcseconds per pixel.</param>
        /// <param name="pixwidth">The number of horizontal pixels of the image.</param>
        /// <param name="pixheight">The number of vertical pixels of the image</param>
        /// <param name="args">Optional arguments list. Possible arguments can be found here: https://github.com/user29A/AstraCarta/wiki, or with AstraCarta.Help call.</param>
        public static string Query(double ra, double dec, double scale, int pixwidth, int pixheight, NameValueCollection? args = null)
		{
            if (scale <= 0)
                throw new Exception("scale: '" + scale + "' not sensible for AstraCarta");
			if (pixwidth <= 0)
				throw new Exception("pixwidth: '" + pixwidth + "' not sensible for AstraCarta");
            if (pixheight <= 0)
                throw new Exception("pixheight: '" + pixheight + "' not sensible for AstraCarta");

            double RAorig = ra;
			double decorig = dec;

			try
			{
				if (args == null)
					args = new NameValueCollection();

				double maglimit = Double.MaxValue;
				if (!string.IsNullOrEmpty(args["maglimit"]))
					maglimit = Convert.ToDouble(args["maglimit"]);

				double buffer = 0;
				if (!string.IsNullOrEmpty(args["buffer"]))
					buffer = Convert.ToDouble(args["buffer"]); //arcminutes

				double offsetra = 0;
				if (!string.IsNullOrEmpty(args["offsetra"]))
					offsetra = Convert.ToDouble(args["offsetra"]); //arcminutes
				ra += (offsetra / 60);

				double offsetdec = 0;
				if (!string.IsNullOrEmpty(args["offsetdec"]))
					offsetdec = Convert.ToDouble(args["offsetdec"]); //arcminutes
				dec += (offsetdec / 60);

				string shape = "rectangle";
				int shapenum = 1;
				if (!string.IsNullOrEmpty(args["shape"]))
					if (args["shape"] != "circle" && args["shape"] != "rectangle")
						throw new Exception("shape may only be \"circle\" or \"rectangle\"");
					else
						shape = args["shape"];
				if (shape == "circle")
						shapenum = 2;
				double radius = scale / 3600 * pixwidth / 2 + buffer / 60;// degrees
				double radiuspix = radius / (scale / 3600);

				double rotation = 0;
				if (!string.IsNullOrEmpty(args["rotation"]))
					rotation = Convert.ToDouble(args["rotation"]) * Math.PI / 180;// radians

				double cd11 = -scale / 3600 * Math.Cos(rotation);
				double cd12 = scale / 3600 * Math.Sin(rotation);
				double cd21 = -scale / 3600 * Math.Sin(rotation);
				double cd22 = -scale / 3600 * Math.Cos(rotation);
				double crval1 = ra;
				double crval2 = dec;
				double crpix1 = pixwidth / 2;
				double crpix2 = pixheight / 2;
				double det = 1 / ((cd11 * cd22 - cd12 * cd21) * Math.PI / 180);
				double cdinv11 = det * cd22;
				double cdinv12 = -det * cd12;
				double cdinv21 = -det * cd21;
				double cdinv22 = det * cd11;
				double xpix_topleft = 0, ypix_topleft = 0, dec_topleft = 0, ra_topleft = 0, xpix_topright = 0, ypix_topright = 0, ra_topright = 0, dec_topright = 0, xpix_bottomright = 0, ypix_bottomright = 0, ra_bottomright = 0, dec_bottomright = 0, xpix_bottomleft = 0, ypix_bottomleft = 0, ra_bottomleft = 0, dec_bottomleft = 0;

				if (shape == "rectangle")
				{
					// top left
					xpix_topleft = 1 - (buffer / 60 * 3600 / scale);
					ypix_topleft = 1 - (buffer / 60 * 3600 / scale);
					double X_intrmdt = cd11 * (xpix_topleft - crpix1) * Math.PI / 180 + cd12 * (ypix_topleft - crpix2) * Math.PI / 180;
					double Y_intrmdt = cd21 * (xpix_topleft - crpix1) * Math.PI / 180 + cd22 * (ypix_topleft - crpix2) * Math.PI / 180;
					ra_topleft = (crval1 * Math.PI / 180 + Math.Atan(X_intrmdt / (Math.Cos(crval2 * Math.PI / 180) - Y_intrmdt * Math.Sin(crval2 * Math.PI / 180)))) * 180 / Math.PI;
					dec_topleft = (Math.Asin((Math.Sin(crval2 * Math.PI / 180) + Y_intrmdt * Math.Cos(crval2 * Math.PI / 180)) / Math.Sqrt(1 + X_intrmdt * X_intrmdt + Y_intrmdt * Y_intrmdt))) * 180 / Math.PI;
					if (ra_topleft < 0)
						ra_topleft += 360;

					// top right
					xpix_topright = pixwidth + (buffer / 60 * 3600 / scale);
					ypix_topright = 1 - (buffer / 60 * 3600 / scale);
					X_intrmdt = cd11 * (xpix_topright - crpix1) * Math.PI / 180 + cd12 * (ypix_topright - crpix2) * Math.PI / 180;
					Y_intrmdt = cd21 * (xpix_topright - crpix1) * Math.PI / 180 + cd22 * (ypix_topright - crpix2) * Math.PI / 180;
					ra_topright = (crval1 * Math.PI / 180 + Math.Atan(X_intrmdt / (Math.Cos(crval2 * Math.PI / 180) - Y_intrmdt * Math.Sin(crval2 * Math.PI / 180)))) * 180 / Math.PI;
					dec_topright = (Math.Asin((Math.Sin(crval2 * Math.PI / 180) + Y_intrmdt * Math.Cos(crval2 * Math.PI / 180)) / Math.Sqrt(1 + X_intrmdt * X_intrmdt + Y_intrmdt * Y_intrmdt))) * 180 / Math.PI;
					if (ra_topright < 0)
						ra_topright += 360;

					// bottom right
					xpix_bottomright = pixwidth + (buffer / 60 * 3600 / scale);
					ypix_bottomright = pixheight + (buffer / 60 * 3600 / scale);
					X_intrmdt = cd11 * (xpix_bottomright - crpix1) * Math.PI / 180 + cd12 * (ypix_bottomright - crpix2) * Math.PI / 180;
					Y_intrmdt = cd21 * (xpix_bottomright - crpix1) * Math.PI / 180 + cd22 * (ypix_bottomright - crpix2) * Math.PI / 180;
					ra_bottomright = (crval1 * Math.PI / 180 + Math.Atan(X_intrmdt / (Math.Cos(crval2 * Math.PI / 180) - Y_intrmdt * Math.Sin(crval2 * Math.PI / 180)))) * 180 / Math.PI;
					dec_bottomright = (Math.Asin((Math.Sin(crval2 * Math.PI / 180) + Y_intrmdt * Math.Cos(crval2 * Math.PI / 180)) / Math.Sqrt(1 + X_intrmdt * X_intrmdt + Y_intrmdt * Y_intrmdt))) * 180 / Math.PI;
					if (ra_bottomright < 0)
						ra_bottomright += 360;

					// bottom left
					xpix_bottomleft = 1 - (buffer / 60 * 3600 / scale);
					ypix_bottomleft = pixheight + (buffer / 60 * 3600 / scale);
					X_intrmdt = cd11 * (xpix_bottomleft - crpix1) * Math.PI / 180 + cd12 * (ypix_bottomleft - crpix2) * Math.PI / 180;
					Y_intrmdt = cd21 * (xpix_bottomleft - crpix1) * Math.PI / 180 + cd22 * (ypix_bottomleft - crpix2) * Math.PI / 180;
					ra_bottomleft = (crval1 * Math.PI / 180 + Math.Atan(X_intrmdt / (Math.Cos(crval2 * Math.PI / 180) - Y_intrmdt * Math.Sin(crval2 * Math.PI / 180)))) * 180 / Math.PI;
					dec_bottomleft = (Math.Asin((Math.Sin(crval2 * Math.PI / 180) + Y_intrmdt * Math.Cos(crval2 * Math.PI / 180)) / Math.Sqrt(1 + X_intrmdt * X_intrmdt + Y_intrmdt * Y_intrmdt))) * 180 / Math.PI;
					if (ra_bottomleft < 0)
						ra_bottomleft += 360;
				}

				string catalogue = "GaiaDR3";
				int cataloguenum = 1;
				if (!string.IsNullOrEmpty(args["catalogue"]))
					catalogue = args["catalogue"];
				if (catalogue != "GaiaDR3")
					throw new Exception("Catalogue " + catalogue + " not valid.");

				string filter = "g";
				int filternum = 3;
				if (!string.IsNullOrEmpty(args["filter"]))
					filter = args["filter"];
				if (catalogue == "GaiaDR3")
					if (filter == "bp" || filter == "b")
					{
						filter = "phot_bp_mean_mag";
						filternum = 1;
					}
					else if (filter == "rp" || filter == "r")
					{
						filter = "phot_rp_mean_mag";
						filternum = 2;
					}
					else if (filter == "g")
					{
						filter = "phot_g_mean_mag";
						filternum = 3;
					}
					else
						throw new Exception("Filter " + filter + " not valid for Catalogue " + catalogue);

				bool imageout = false;
				if (!string.IsNullOrEmpty(args["imageout"]))
					imageout = Convert.ToBoolean(args["imageout"]);

				string outformat = ".csv";
				bool fitsout = false;
				if (!string.IsNullOrEmpty(args["fitsout"]))
					fitsout = Convert.ToBoolean(args["fitsout"]);
				if (fitsout == true)
					outformat = ".fit";

				bool imageshow = false;
				if (!string.IsNullOrEmpty(args["imageshow"]))
					imageshow = Convert.ToBoolean(args["imageshow"]);

				string outdir = Directory.GetCurrentDirectory();
				if (!string.IsNullOrEmpty(args["outdir"]))
					outdir = args["outdir"];
				if (!Directory.Exists(outdir))
					Directory.CreateDirectory(outdir);

				string rawoutdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AstraCarta", "AstraCartaRawQueries");
				if (!string.IsNullOrEmpty(args["rawoutdir"]))
					rawoutdir = args["rawoutdir"];
				if (!Directory.Exists(rawoutdir))
					Directory.CreateDirectory(rawoutdir);

				bool forcenew = false;
				if (!string.IsNullOrEmpty(args["forcenew"]))
					forcenew = Convert.ToBoolean(args["forcenew"]);

				int nquery = 500;
				if (!string.IsNullOrEmpty(args["nquery"]))
					nquery = Convert.ToInt32(args["nquery"]);

				double pmepoch = Double.MaxValue;
				if (!string.IsNullOrEmpty(args["pmepoch"]))
					try
					{
                        pmepoch = Convert.ToDouble(args["pmepoch"]);
                    }
					catch
					{
						try
						{
							JPMath.DateToJD(args["pmepoch"], "00:00:00", out pmepoch);
						}
						catch
						{
                            throw new Exception("pmepoch: '" + args["pmepoch"] + "' is not valid in AstraCarta");
                        }
					}					

				double pmlimit = Double.MaxValue;
				if (!string.IsNullOrEmpty(args["pmlimit"]))
					if (pmepoch == Double.MaxValue)
                        throw new Exception("pmlimit only valid if pmepoch is specified in AstraCarta");
					else
						pmlimit = Convert.ToDouble(args["pmlimit"]);

				string entries = "source_id, ref_epoch, ra, ra_error, dec, dec_error, pmra, pmra_error, pmdec, pmdec_error, pm, " + filter;
				if (!string.IsNullOrEmpty(args["entries"]))
					if (args["entries"] == "all")
						entries = "*";
					else
						entries += ", " + args["entries"];

				bool notableout = false;
				if (!string.IsNullOrEmpty(args["notableout"]))
					notableout = Convert.ToBoolean(args["notableout"]);

				bool rmvrawquery = false;
				if (!string.IsNullOrEmpty(args["rmvrawquery"]))
					rmvrawquery = Convert.ToBoolean(args["rmvrawquery"]);

				ulong rawqueryfilenamehash, fileoutfilenamehash;
				if (shape == "circle")
				{
					rawqueryfilenamehash = JPMath.MakeUniqueHashCode(new double[] { ra, dec, nquery, cataloguenum, filternum, radius * 3600, shapenum }, entries);
					fileoutfilenamehash = JPMath.MakeUniqueHashCode(new double[] { rawqueryfilenamehash, maglimit, pmepoch, pmlimit });
				}
				else
				{
					rawqueryfilenamehash = JPMath.MakeUniqueHashCode(new double[] { ra, dec, nquery, cataloguenum, filternum, rotation, ra_topleft, dec_topleft, ra_topright, dec_topright, ra_bottomright, dec_bottomright, ra_bottomleft, dec_bottomleft, shapenum }, entries);
					fileoutfilenamehash = JPMath.MakeUniqueHashCode(new double[] { rawqueryfilenamehash, maglimit, pmepoch, pmlimit });
				}

				string outname = fileoutfilenamehash.ToString();
				if (!string.IsNullOrEmpty(args["outname"]))
					outname = args["outname"];

				bool overwrite = false;
				if (!string.IsNullOrEmpty(args["overwrite"]))
					overwrite = Convert.ToBoolean(args["overwrite"]);
				else if (args.AllKeys.Contains("overwrite"))
					overwrite = true;

				#pragma warning disable CS0219 // Variable is assigned but its value is never used
				bool silent = false;
				#pragma warning restore CS0219 // Variable is assigned but its value is never used
				if (!string.IsNullOrEmpty(args["silent"]))
					silent = Convert.ToBoolean(args["silent"]);
				else if (args.AllKeys.Contains("silent"))
					silent = true;

				string rawqueryfilename = Path.Combine(rawoutdir, rawqueryfilenamehash.ToString() + ".csv");
				string resultsfilename = Path.Combine(outdir, outname + outformat);
				string imagefilename = Path.Combine(outdir, outname + "ACQuery" + ".jpg");

				if (!overwrite)
					if (File.Exists(resultsfilename) || File.Exists(imagefilename))
					{
						int f = 1;
						resultsfilename = Path.Combine(outdir, outname + string.Format(" ({0})", f) + outformat);
						imagefilename = Path.Combine(outdir, outname + string.Format(" ({0})", f) + ".jpg");

						while (File.Exists(resultsfilename) || File.Exists(imagefilename))
						{
							f += 1;
							resultsfilename = Path.Combine(outdir, outname + string.Format(" ({0})", f) + outformat);
							imagefilename = Path.Combine(outdir, outname + string.Format(" ({0})", f) + ".jpg");
						}
					}

				if (!File.Exists(rawqueryfilename) || forcenew) //query new table download
				{
					string jobstr;
					if (catalogue == "GaiaDR3")
					{
						jobstr = "REQUEST=doQuery&LANG=ADQL&FORMAT=csv&QUERY=";

						if (nquery == 0)
							jobstr += "SELECT " + entries + " FROM gaiadr3.gaia_source" + Environment.NewLine;
						else
							jobstr += string.Format("SELECT TOP {0}", nquery) + " " + entries + " FROM gaiadr3.gaia_source" + Environment.NewLine;

						jobstr += "WHERE 1=CONTAINS(POINT('ICRS', gaiadr3.gaia_source.ra,gaiadr3.gaia_source.dec),";

						if (shape == "rectangle")
							jobstr += string.Format(CultureInfo.GetCultureInfo("en-US").NumberFormat, "POLYGON('ICRS',{0},{1},{2},{3},{4},{5},{6},{7}))", ra_topleft, dec_topleft, ra_topright, dec_topright, ra_bottomright, dec_bottomright, ra_bottomleft, dec_bottomleft) + Environment.NewLine;
						else
							jobstr += string.Format(CultureInfo.GetCultureInfo("en-US").NumberFormat, "CIRCLE('ICRS',{0},{1},{2}))", ra, dec, radius) + Environment.NewLine;

						jobstr += "ORDER by gaiadr3.gaia_source." + filter + " ASC";

						string content = "";
						try
						{
							WebResponse response = default;
							HttpWebRequest tapQueryRequest = HttpWebRequest.CreateHttp("https://gea.esac.esa.int/tap-server/tap/sync?");
							tapQueryRequest.CookieContainer = new CookieContainer();
							tapQueryRequest.Method = "POST";
							tapQueryRequest.ContentType = "application/x-www-form-urlencoded";
							byte[] paramsStream = Encoding.UTF8.GetBytes(jobstr);
							tapQueryRequest.ContentLength = paramsStream.Length;
							var requestStream = tapQueryRequest.GetRequestStream();
							requestStream.Write(paramsStream, 0, paramsStream.Length);
							requestStream.Close();
							response = tapQueryRequest.GetResponse();
							using (var dataStream = response.GetResponseStream())
							{
								content = new StreamReader(dataStream, Encoding.UTF8).ReadToEnd();
							}
							response.Close();
							response = default;

							string[] lines = content.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

							using (var stream = File.CreateText(rawqueryfilename))
							{
								for (int i = 0; i < lines.Length - 1; i++)//last line always empty so ignore
									stream.WriteLine(lines[i]);
							}
						}
						catch (Exception ex)
						{
                            throw new Exception(ex.Message + Environment.NewLine + ex.TargetSite + Environment.NewLine + ex.StackTrace + Environment.NewLine + ex.Source + Environment.NewLine + ex.InnerException + Environment.NewLine + Environment.NewLine + ex.Data + Environment.NewLine + "jobstr: '" + jobstr + "'" + Environment.NewLine + "content: '" + content + "'");
						}
					}
				}

				int rowswritten = 0;
				StreamReader querystream = new StreamReader(rawqueryfilename);
				string header = querystream.ReadLine();
				string rawquery = querystream.ReadToEnd();
				querystream.Close();
				string[] ttypes = header.Split(',');
				string[] rows = rawquery.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

				using (var resultsstream = File.CreateText(resultsfilename))
				{					
					resultsstream.WriteLine(header);

					foreach (string row in rows)
					{
						if (row.Trim().Length == 0)//last row is always blank
							continue;

						string[] values = row.Split(',');

						if (values[Array.IndexOf(ttypes, filter)] != "")
							if (Convert.ToDouble(values[Array.IndexOf(ttypes, filter)]) <= maglimit)
								if (pmepoch != Double.MaxValue)//update ra & dec
								{
									if (values[Array.IndexOf(ttypes, "pm")] != "" && Convert.ToDouble(values[Array.IndexOf(ttypes, "pm")]) < pmlimit)
									{
										double dt = pmepoch - Convert.ToDouble(values[Array.IndexOf(ttypes, "ref_epoch")]);
										values[Array.IndexOf(ttypes, "ref_epoch")] = (Convert.ToDouble(values[Array.IndexOf(ttypes, "ref_epoch")]) + dt).ToString();
										values[Array.IndexOf(ttypes, "ra")] = (Convert.ToDouble(values[Array.IndexOf(ttypes, "ra")]) + dt * Convert.ToDouble(values[Array.IndexOf(ttypes, "pmra")]) / 3600 / 1000).ToString();
										values[Array.IndexOf(ttypes, "dec")] = (Convert.ToDouble(values[Array.IndexOf(ttypes, "dec")]) + dt * Convert.ToDouble(values[Array.IndexOf(ttypes, "pmdec")]) / 3600 / 1000).ToString();
										string line = "";
										for (int i = 0; i < values.Length; i++)
											line += values[i] + ",";
										line = line.Substring(0, line.Length - 1);
										resultsstream.WriteLine(line);
										rowswritten++;
									}
								}
								else
								{
									string line = "";
									for (int i = 0; i < values.Length; i++)
										line += values[i] + ",";
									line = line.Substring(0, line.Length - 1);
									resultsstream.WriteLine(line);
									rowswritten++;
								}
					}
				}

				if (rmvrawquery)
					File.Delete(rawqueryfilename);

				if (rowswritten == 0)
				{
					File.Delete(resultsfilename);
					return "";
				}

				Plotter plot = new Plotter("AstraCarta", true, true);
				plot.ChartGraph.ChartAreas[0].AxisY.IsReversed = true; 
				plot.Width = 500;
				plot.Height = 500;
				plot.Text = "AstraCarta";

				if (imageshow || imageout)
				{
					double[] x = new double[rowswritten];
					double[] y = new double[rowswritten];
					double a0 = crval1 * Math.PI / 180;
					double d0 = crval2 * Math.PI / 180;

					StreamReader resultsstream = new StreamReader(resultsfilename);
					header = resultsstream.ReadLine();
					string results = resultsstream.ReadToEnd();
					resultsstream.Close();
					ttypes = header.Split(',');
					rows = results.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

					for (int i = 0; i < rowswritten; i++)
					{
						string[] values = rows[i].Split(',');

						ra = Convert.ToDouble(values[Array.IndexOf(ttypes, "ra")]);
						dec = Convert.ToDouble(values[Array.IndexOf(ttypes, "dec")]);
						double a = ra * Math.PI / 180;
						double d = dec * Math.PI / 180;
						double X_intrmdt = Math.Cos(d) * Math.Sin(a - a0) / (Math.Cos(d0) * Math.Cos(d) * Math.Cos(a - a0) + Math.Sin(d0) * Math.Sin(d));
						double Y_intrmdt = (Math.Cos(d0) * Math.Sin(d) - Math.Cos(d) * Math.Sin(d0) * Math.Cos(a - a0)) / (Math.Sin(d0) * Math.Sin(d) + Math.Cos(d0) * Math.Cos(d) * Math.Cos(a - a0));
						x[i] = cdinv11 * X_intrmdt + cdinv12 * Y_intrmdt + crpix1;
						y[i] = cdinv21 * X_intrmdt + cdinv22 * Y_intrmdt + crpix2;
					}
					
					plot.ChartGraph.PlotXYData(x, y, "Found " + rowswritten + " Sources", "Horizontal Image Axis (Pixels)", "Vertical Image Axis (Pixels)", JPChart.SeriesType.Point, "astracarta", System.Drawing.Color.Blue);
					double xlimmin = 1;
					double xlimmax = pixwidth;
					double ylimmin = 1;
					double ylimmax = pixheight;

					if (shape == "rectangle")
					{
						if (buffer > 0)
						{
							plot.ChartGraph.AddXYData(new double[] { 1, 1, pixwidth, pixwidth, 1 }, new double[] { 1, pixheight, pixheight, 1, 1 }, JPChart.SeriesType.Line, "imagebox", System.Drawing.Color.Black);
							xlimmin = xpix_topleft;
							xlimmax = xpix_topright;
							ylimmin = ypix_topleft;
							ylimmax = ypix_bottomleft;
						}
						else if (buffer < 0)
							plot.ChartGraph.AddXYData(new double[] { xpix_topleft, xpix_topright, xpix_bottomright, xpix_bottomleft, xpix_topleft }, new double[] { ypix_topleft, ypix_topright, ypix_bottomright, ypix_bottomleft, ypix_topleft }, JPChart.SeriesType.Line, "imagebox", System.Drawing.Color.Black);
					}
					else if (shape == "circle")
					{
						double rimage = 0;
						if (buffer >= 0)
						{
							rimage = crpix1;
							if (buffer > 0)
							{
								xlimmin -= (radiuspix - rimage);
								xlimmax += (radiuspix - rimage);
								ylimmin -= (radiuspix - rimage);
								ylimmax += (radiuspix - rimage);
							}
                        
						}
						else if (buffer < 0)
							rimage = radiuspix;

						double[] theta = new double[361];
						double[] xc = new double[361];
						double[] yc = new double[361];							

						for (int i = 0; i < theta.Length; i++)
						{
							theta[i] = i;
							xc[i] = rimage * Math.Sin(theta[i] * Math.PI / 180) + crpix1;
							yc[i] = rimage * Math.Cos(theta[i] * Math.PI / 180) + crpix2;
						}

						plot.ChartGraph.AddXYData(xc, yc, JPChart.SeriesType.Line, "imagecircle", System.Drawing.Color.Black);
					}

					plot.ChartGraph.SetAxesLimits(Math.Floor(xlimmin), Math.Ceiling(xlimmax), Math.Floor(ylimmin), Math.Ceiling(ylimmax));
					//plot.jpChart1.ChartAreas[0].AxisY.Crossing = Math.Ceiling(ylimmax);
					//plot.jpChart1.Series[0].XAxisType = AxisType.Secondary;

					if (imageout)
						plot.ChartGraph.SaveImage(imagefilename, ChartImageFormat.Jpeg);
				}

				if (notableout)
					File.Delete(resultsfilename);
				else if (fitsout)
				{
					StreamReader resultsstream = new StreamReader(resultsfilename);
					header = resultsstream.ReadLine();
					string results = resultsstream.ReadToEnd();
					resultsstream.Close();
					ttypes = header.Split(',');
					rows = results.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

					double[][] table = new double[ttypes.Length][];
					for (int i = 0; i < ttypes.Length; i++)
						table[i] = new double[rowswritten];

					for (int i = 0; i < rowswritten; i++)
					{
						string[] rowentries = rows[i].Split(',');

						for (int j = 0; j < rowentries.Length; j++)
							if (rowentries[j].Trim() == "")
								table[j][i] = Double.NaN;
							else
								table[j][i] = Convert.ToDouble(rowentries[j]);
					}

					FITSBinTable fbt = new FITSBinTable(catalogue);
					fbt.SetFields(ttypes, null, table);
					fbt.AddExtraHeaderKey("RA", RAorig.ToString(), "Right Ascension of query center");
					fbt.AddExtraHeaderKey("DEC", decorig.ToString(), "Declination of query center");
					WorldCoordinateSolution.DegreeElementstoSexagesimalElements(RAorig, decorig, out string rasex, out string decsex, ":", 4);
					fbt.AddExtraHeaderKey("RASEX", rasex, "Right Ascension of query center");
					fbt.AddExtraHeaderKey("DECSEX", decsex, "Declination of query center");
					File.Delete(resultsfilename);
					resultsfilename = resultsfilename.Split('.')[0];
					resultsfilename += ".fit";
					fbt.Write(resultsfilename, true);
				}

				if (imageshow)
					plot.ShowDialog();

				if (notableout)
					return "";

				return resultsfilename;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message + Environment.NewLine + ex.TargetSite + Environment.NewLine + ex.StackTrace + Environment.NewLine + ex.Source + Environment.NewLine + ex.InnerException);
				//return "";
			}
		}

		/// <summary>
		/// Construct AstraCarta. Useful for building queries with a user interface for option settings.
		/// </summary>
		public AstraCarta()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Construct AstraCarta with keys and their values. Useful for repeating/specifying user interface option settings.
		/// </summary>
		/// <param name="ra">The right acension of the field center.</param>
		/// <param name="dec">The declination of the field center.</param>
		/// <param name="scale">The plate scale in arcseconds per pixel.</param>
		/// <param name="pixwidth">The number of horizontal pixels of the image.</param>
		/// <param name="pixheight">The number of vertical pixels of the image</param>
		/// <param name="optArgs">Optional arguments list. Possible arguments can be found here: https://github.com/user29A/AstraCarta/wiki, or with AstraCarta.Help call.
		/// <br />Boolean arguments do not require a value, and their presence indicates true. For example the presence of optArgs.Add("fitsout") equates to true for writing the file as a FITS bintable.</param>
		public AstraCarta(double ra, double dec, double scale, int pixwidth, int pixheight, NameValueCollection? optArgs = null)
		{
			InitializeComponent();

			OPENFROMARGS = true;

			RATextBox.Text = ra.ToString();
			DecTextBox.Text = dec.ToString();
			ScaleTextBox.Text = scale.ToString();
			WidthTextBox.Text = pixwidth.ToString();
			HeightTextBox.Text = pixheight.ToString();

			if (optArgs == null)
				return;

			BufferTextBox.Text = "";
			if (!string.IsNullOrEmpty(optArgs["buffer"]))
				BufferTextBox.Text = Convert.ToDouble(optArgs["buffer"]).ToString();

			RAOffsetTextBox.Text = "";
			if (!string.IsNullOrEmpty(optArgs["offsetra"]))
				RAOffsetTextBox.Text = Convert.ToDouble(optArgs["offsetra"]).ToString();

			DecOffsetTextBox.Text = "";
			if (!string.IsNullOrEmpty(optArgs["offsetdec"]))
				DecOffsetTextBox.Text = Convert.ToDouble(optArgs["offsetdec"]).ToString();

			NameTextBox.Text = "";
			if (!string.IsNullOrEmpty(optArgs["outname"]))
				NameTextBox.Text = string.Join("_", (optArgs["outname"]).Split(Path.GetInvalidFileNameChars())) + "_AstraCarta";

			CatalogueDrop.SelectedIndex = 0;//GaiaDR3
			if (!string.IsNullOrEmpty(optArgs["catalogue"]))
				if (optArgs["catalogue"] == "GaiaDR3")
					CatalogueDrop.SelectedIndex = 0;
				else
					throw new Exception("Catalogue: '" + (string)optArgs["catalogue"] + "' not recognized...");

			FilterDrop.SelectedIndex = 1;//GaiaDR3 g
			if (!string.IsNullOrEmpty(optArgs["filter"]))
				if (CatalogueDrop.SelectedIndex == 0)//gaiadr3
				{
					if (optArgs["filter"] == "bp")
						FilterDrop.SelectedIndex = 0;
					else if (optArgs["filter"] == "g")
						FilterDrop.SelectedIndex = 1;
					else if (optArgs["filter"] == "rp")
						FilterDrop.SelectedIndex = 2;
					else
						throw new Exception("Filter: '" + optArgs["filter"] + "' not recognized for catalogue '" + CatalogueDrop.SelectedItem.ToString() + "'");
				}
				else
					throw new Exception("Filter: '" + optArgs["filter"] + "' not recognized...");

			MagLimitTextBox.Text = "";
			if (!string.IsNullOrEmpty(optArgs["maglimit"]))
				MagLimitTextBox.Text = Convert.ToDouble(optArgs["maglimit"]).ToString();

			ShapeDrop.SelectedIndex = 0;
			if (!string.IsNullOrEmpty(optArgs["shape"]))
				if (optArgs["shape"] == "circle")
					ShapeDrop.SelectedIndex = 1;
				else if (optArgs["shape"] == "rectangle" || optArgs["shape"] == "square")
					ShapeDrop.SelectedIndex = 0;
				else
					throw new Exception("Shape: '" + optArgs["shape"] + "' not recognized.");

			RotationTextBox.Text = "";
			if (!string.IsNullOrEmpty(optArgs["rotation"]))
				RotationTextBox.Text = Convert.ToDouble(optArgs["rotation"]).ToString();

			NQueryTextBox.Text = "500";
			if (!string.IsNullOrEmpty(optArgs["nquery"]))
				NQueryTextBox.Text = Convert.ToDouble(optArgs["nquery"]).ToString();

			PMEpochTextBox.Text = "";
			if (!string.IsNullOrEmpty(optArgs["pmepoch"]))
				PMEpochTextBox.Text = Convert.ToDouble(optArgs["pmepoch"]).ToString();

			PMLimitTextBox.Text = "";
			if (!string.IsNullOrEmpty(optArgs["pmlimit"]))
				PMLimitTextBox.Text = Convert.ToDouble(optArgs["pmlimit"]).ToString();

			DirectoryTextBox.Text = "";
			if (!string.IsNullOrEmpty(optArgs["outdir"]))
				DirectoryTextBox.Text = optArgs["outdir"];

			EntriesTextBox.Text = "";
			if (!string.IsNullOrEmpty(optArgs["entries"]))
				EntriesTextBox.Text = optArgs["entries"];

			SaveTableChck.Checked = true;
			if (!string.IsNullOrEmpty(optArgs["notable"]))
				SaveTableChck.Checked = false;

			FITSTableChck.Checked = false;
			if (!string.IsNullOrEmpty(optArgs["fitsout"]))
				FITSTableChck.Checked = true;

			ShowImageChck.Checked = false;
			if (!string.IsNullOrEmpty(optArgs["imageshow"]))
				ShowImageChck.Checked = true;

			SaveImageChck.Checked = false;
			if (!string.IsNullOrEmpty(optArgs["outimage"]))
				SaveImageChck.Checked = true;

			ForceNewChck.Checked = false;
			if (!string.IsNullOrEmpty(optArgs["forcenew"]))
				ForceNewChck.Checked = true;

			RemoveRawQueryChck.Checked = false;
			if (!string.IsNullOrEmpty(optArgs["rmvrawquery"]))
				RemoveRawQueryChck.Checked = true;

			SilentChck.Checked = false;
			if (!string.IsNullOrEmpty(optArgs["silent"]))
				SilentChck.Checked = true;

			OverwriteChck.Checked = false;
			if (!string.IsNullOrEmpty(optArgs["overwrite"]))
				OverwriteChck.Checked = true;
		}

		private void ExecuteBtn_Click(object sender, EventArgs e)
		{
			NameValueCollection optArgs = new NameValueCollection();
			if (BufferTextBox.Text != "")
				optArgs.Add("buffer", BufferTextBox.Text);
			if (RAOffsetTextBox.Text != "")
				optArgs.Add("offsetra", RAOffsetTextBox.Text);
			if (DecOffsetTextBox.Text != "")
				optArgs.Add("offsetdec", DecOffsetTextBox.Text);
			if (NameTextBox.Text != "")
				optArgs.Add("outname", NameTextBox.Text);
			optArgs.Add("catalogue", CatalogueDrop.SelectedItem.ToString());
			optArgs.Add("filter", FilterDrop.SelectedItem.ToString());
			if (MagLimitTextBox.Text != "")
				optArgs.Add("maglimit", MagLimitTextBox.Text);
			optArgs.Add("shape", ShapeDrop.SelectedItem.ToString());
			if (ShapeDrop.SelectedItem.ToString() == "rectangle")
				if (RotationTextBox.Text != "")
					optArgs.Add("rotation", RotationTextBox.Text);
			optArgs.Add("nquery", NQueryTextBox.Text);
			if (PMEpochTextBox.Text != "")
				optArgs.Add("pmepoch", PMEpochTextBox.Text);
			if (PMLimitTextBox.Text != "")
				optArgs.Add("pmlimit", PMLimitTextBox.Text);
			if (DirectoryTextBox.Text != "")
				optArgs.Add("outdir", DirectoryTextBox.Text);
			optArgs.Add("entries", EntriesTextBox.Text);
			if (!SaveTableChck.Checked)
				optArgs.Add("notable", "true");
			if (FITSTableChck.Checked)
				optArgs.Add("fitsout", "true");
			if (ShowImageChck.Checked)
				optArgs.Add("imageshow", "true");
			if (SaveImageChck.Checked)
				optArgs.Add("outimage", "true");
			if (ForceNewChck.Checked)
				optArgs.Add("forcenew", "true");
			if (RemoveRawQueryChck.Checked)
				optArgs.Add("rmvrawquery", "true");
			if (SilentChck.Checked)
				optArgs.Add("silent", "true");
			if (OverwriteChck.Checked)
				optArgs.Add("overwrite", "true");

			double ra = Convert.ToDouble(RATextBox.Text);
			double dec = Convert.ToDouble(DecTextBox.Text);
			double scale = Convert.ToDouble(ScaleTextBox.Text);
			int pixwidth = Convert.ToInt32(WidthTextBox.Text);
			int pixheight = Convert.ToInt32(HeightTextBox.Text);

			BGWrkr.RunWorkerAsync(new object[] { ra, dec, scale, pixwidth, pixheight, optArgs });
		}

		private void BGWrkr_DoWork(object sender, DoWorkEventArgs e)
		{
			double ra = (double)((object[])e.Argument)[0];
			double dec = (double)((object[])e.Argument)[1];
			double scale = (double)((object[])e.Argument)[2];
			int pixwidth = (int)((object[])e.Argument)[3];
			int pixheight = (int)((object[])e.Argument)[4];
			NameValueCollection optArgs = (NameValueCollection)((object[])e.Argument)[5];

			BGWrkr.ReportProgress(0, "Calling AstraCarta.Query(ra, dec, scale, pixwidth, pixheight, optArgs)" + Environment.NewLine);

			OUTFILE = AstraCarta.Query(ra, dec, scale, pixwidth, pixheight, optArgs);

			if (CANCELLED)
				return;

			if (OUTFILE == "")
				;
			else
			{
				BGWrkr.ReportProgress(0, OUTFILE + Environment.NewLine);
			}		
		}

		private void BGWrkr_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			MessageTextBox.AppendText((string)e.UserState);
		}

		private void BGWrkr_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (ExecuteBtn.Text.Contains("Cancel"))
				ExecuteBtn.Text = "Execute";
			if (!ExecuteBtn.Enabled)
				ExecuteBtn.Enabled = true;

			if (CANCELLED)
			{
				PERFORMEXECUTE = false;
				MessageTextBox.AppendText("Cancelled...\r\n");
				return;
			}

			if (ERROR)
			{
				PERFORMEXECUTE = false;
				ERROR = false;
				return;
			}

			if (PERFORMEXECUTE)
			{
				PERFORMEXECUTE = false;

				ExecuteBtn.PerformClick();
				return;
			}

			if (CLOSEONCOMPLETE)
				this.Close();
		}

		private void AstraCarta_Load(object sender, EventArgs e)
		{
			if (!OPENFROMARGS)
			{
				CatalogueDrop.SelectedIndex = Convert.ToInt32(REG.GetReg("AstraCarta", "CatalogueDrop"));
				ShapeDrop.SelectedIndex = Convert.ToInt32(REG.GetReg("AstraCarta", "ShapeDrop"));
				SaveTableChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "SaveTableChck"));
				FITSTableChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "FITSTableChck"));
				ShowImageChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "ShowImageChck"));
				SaveImageChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "SaveImageChck"));
				ForceNewChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "ForceNewChck"));
				RemoveRawQueryChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "RemoveRawQueryChck"));
				SilentChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "SilentChck"));
				OverwriteChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "OverwriteChck"));
				CloseOnCompleteChck.Checked = Convert.ToBoolean(REG.GetReg("AstraCarta", "CloseOnCompleteChck"));
				DirectoryTextBox.Text = (string)REG.GetReg("AstraCarta", "DirectoryTextBox");
				RATextBox.Text = (string)REG.GetReg("AstraCarta", "RATextBox");
				DecTextBox.Text = (string)REG.GetReg("AstraCarta", "DecTextBox");
				ScaleTextBox.Text = (string)REG.GetReg("AstraCarta", "ScaleTextBox");
				WidthTextBox.Text = (string)REG.GetReg("AstraCarta", "WidthTextBox");
				HeightTextBox.Text = (string)REG.GetReg("AstraCarta", "HeightTextBox");
				BufferTextBox.Text = (string)REG.GetReg("AstraCarta", "BufferTextBox");
				MagLimitTextBox.Text = (string)REG.GetReg("AstraCarta", "MagLimitTextBox");
				RotationTextBox.Text = (string)REG.GetReg("AstraCarta", "RotationTextBox");
				PMEpochTextBox.Text = (string)REG.GetReg("AstraCarta", "PMEpochTextBox");
				PMLimitTextBox.Text = (string)REG.GetReg("AstraCarta", "PMLimitTextBox");
				NQueryTextBox.Text = (string)REG.GetReg("AstraCarta", "NQueryTextBox");
			}

			if (EXECUTEONSHOW)
				ExecuteBtn.PerformClick();
		}

		private void CatalogueDrop_SelectedIndexChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "CatalogueDrop", CatalogueDrop.SelectedIndex);

			if (CatalogueDrop.SelectedItem.ToString() == "GaiaDR3")
			{
				//EntriesTextBox.Text = "ref_epoch, ra, ra_error, dec, dec_error, pmra, pmra_error, pmdec, pmdec_error, pm, phot_bp_mean_mag, phot_g_mean_mag, phot_rp_mean_mag";
				FilterDrop.Items.Clear();
				FilterDrop.Items.AddRange(new string[] { "bp", "g", "rp" });
			}

			FilterDrop.SelectedIndex = Convert.ToInt32(REG.GetReg("AstraCarta", "FilterDrop" + CatalogueDrop.SelectedItem.ToString()));
		}

		private void FilterDrop_SelectedIndexChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "FilterDrop" + CatalogueDrop.SelectedItem.ToString(), FilterDrop.SelectedIndex);
		}

		private void ShapeDrop_SelectedIndexChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "ShapeDrop", ShapeDrop.SelectedIndex);
		}

		private void SaveTableChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "SaveTableChck", SaveTableChck.Checked);

			if (SaveTableChck.Checked)
				FITSTableChck.Enabled = true;
			else
				FITSTableChck.Enabled = false;

			if (!SaveTableChck.Checked && !SaveImageChck.Checked)
				OverwriteChck.Enabled = false;
			else
				OverwriteChck.Enabled = true;
		}

		private void FITSTableChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "FITSTableChck", FITSTableChck.Checked);
		}

		private void ShowImageChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "ShowImageChck", ShowImageChck.Checked);
		}

		private void SaveImageChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "SaveImageChck", SaveImageChck.Checked);

			if (!SaveTableChck.Checked && !SaveImageChck.Checked)
				OverwriteChck.Enabled = false;
			else
				OverwriteChck.Enabled = true;
		}

		private void ForceNewChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "ForceNewChck", ForceNewChck.Checked);
		}

		private void RemoveRawQueryChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "RemoveRawQueryChck", RemoveRawQueryChck.Checked);
		}

		private void SilentChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "SilentChck", SilentChck.Checked);
		}

		private void OverwriteChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "OverwriteChck", OverwriteChck.Checked);
		}

		private void DirectoryTextBox_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog fbd = new FolderBrowserDialog();
			fbd.SelectedPath = (string)REG.GetReg("AstraCarta", "DirectoryTextBox");
			fbd.Description = "Please select the directory for saving outputs.";
			if (fbd.ShowDialog() == DialogResult.Cancel)
				return;
			DirectoryTextBox.Text = fbd.SelectedPath;
			REG.SetReg("AstraCarta", "DirectoryTextBox", DirectoryTextBox.Text);
		}

		private void CloseOnCompleteChck_CheckedChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", "CloseOnCompleteChck", CloseOnCompleteChck.Checked);

			CLOSEONCOMPLETE = CloseOnCompleteChck.Checked;
		}

		private void NumericTextBox_TextChanged(object sender, EventArgs e)
		{
			REG.SetReg("AstraCarta", ((TextBox)sender).Name, ((TextBox)sender).Text);			
		}

		private void AstraCarta_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
				this.Close();
		}

		private void AstraCarta_HelpRequested(object sender, HelpEventArgs hlpevent)
		{
			AstraCarta.Help(false);
		}

		private void LoadMenuBtn_Click(object sender, EventArgs e)
		{

		}

		private void SaveMenuBtn_Click(object sender, EventArgs e)
		{
			//SaveFileDialog sfd = new SaveFileDialog
			//{
			//	Filter = "FITS|*.fit",
			//	InitialDirectory = Environment.CurrentDirectory
			//};

			//if (sfd.ShowDialog() == DialogResult.Cancel)
			//	return;

			//FITSBinTable fbt = new FITSBinTable("astracarta");

		}
	}
}
