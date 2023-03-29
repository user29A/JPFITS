using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Collections;
using System.Net;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;
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

		public static void Help()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("\"-maglimit\" Magnitude limit below which to flag bright sources and save to output table. Default is 100, to pass all, given that there is no such low magnitude." + Environment.NewLine);
			sb.AppendLine("\"-buffer\" Tolerance buffer around image field, in arcminutes. This field can be negative, if one wishes to mitigate image padding in the query." + Environment.NewLine);
			sb.AppendLine("\"-offsetra\" Offset the center of the query region in right ascension. Units in arcminutes." + Environment.NewLine);
			sb.AppendLine("\"-offsetdec\" Offset the center of the query region in declination. Units in arcminutes." + Environment.NewLine);
			sb.AppendLine("\"-shape\" Shape of field to query: \"rectangle\" (default) or \"circle\". Circle may only be used if pixwidth and pixheight are equal. Rectangle query uses a polygon query with corners defined by an ad-hoc WCS given the supplied field parameters, whereas circle uses a radius." + Environment.NewLine);
			sb.AppendLine("\"-rotation\" Field rotation, applicable to a rectangle query. Raises an exception if used for a circle query." + Environment.NewLine);
			sb.AppendLine("\"-catalogue\" Catalogue or Service to query. Valid options are currently: \"GaiaDR3\" (default)." + Environment.NewLine);
			sb.AppendLine("\"-filter\" Filter of the catalogue to sort on. Options are: for GaiaDR3: \"rp\", \"bp\", \"g\" (default)." + Environment.NewLine);
			sb.AppendLine("\"-forcenew\" Force new astroquery. The raw query is saved with a filename based on a hash of the astroquery parameters, and therefore should be unique for unique queries, and the same for the same queries. The exception is for the \"entries\" option which cannot be hashed non-randomly. Therefore if everything else stays the same except for \"entries\", one would need to force a new query." + Environment.NewLine);
			sb.AppendLine("\"-imageout\" Output an image field plot with maglimit sources marked." + Environment.NewLine);
			sb.AppendLine("\"-imageshow\" Show the image field plot." + Environment.NewLine);
			sb.AppendLine("\"-outdir\" Directory to save files. By default files are saved in the current working directory." + Environment.NewLine);
			sb.AppendLine("\"-outname\" The name to use for output files. If not supplied a settings-consistent but random hash will be used for output file names. Existing filenames will not be overwritten." + Environment.NewLine);
			sb.AppendLine("\"-overwrite\" Overwrite the output table if one is produced, instead of appending an instance number." + Environment.NewLine);
			sb.AppendLine("\"-fitsout\" Output results table format as FITS binary table (instead of csv)." + Environment.NewLine);
			sb.AppendLine("\"-rmvrawquery\" Remove the raw query folder and its contents after running. This will force future astroqueries." + Environment.NewLine);
			sb.AppendLine("\"-nquery\" Number of brightest sources in the filter to retreive from the query service. Pass 0 to retreive all sources. Default 500." + Environment.NewLine);
			sb.AppendLine("\"-pmepoch\" Pass the year.year value of the observation to update the RA and Dec entries of the table with their proper motion adjustments, given the catalogue reference epoch. Only entries in the query which have valid proper motion entries will be saved to output." + Environment.NewLine);
			sb.AppendLine("\"-pmlimit\" Limit the output to proper motions whose absolute value are less than pmlimit. Milliarcseconds per year." + Environment.NewLine);
			sb.AppendLine("\"-entries\" A commaspace \", \" separated list of source columns to request from the query. Pass entries=\"all\" to get everything from the query source. Default is for GaiaDR3, entries=\"ref_epoch, ra, ra_err, dec, dec_err, pmra, pmra_err, pmdec, pmdec_err, pm, phot_bp_mean_mag, phot_g_mean_mag, phot_rp_mean_mag\". Thus, if entries is supplied, it appends additional entries to the default. For example if you additionally wanted the absolute value of proper motion errors then passing entries=\"pm_error\" would append \", pm_error\" to the string." + Environment.NewLine);
			sb.AppendLine("\"-notableout\" Do not write an output file even when sources have been found. Useful if only wanting to view images but do not want to fill up a directory with table results." + Environment.NewLine);
			sb.AppendLine("\"-silent\" Do not output process milestones to command window. Default false." + Environment.NewLine);

			Clipboard.SetText(sb.ToString());

			MessageBox.Show(sb.ToString());
		}

		/// <summary>
		/// Perform a query with no user interface dialog form. Will throw an informative exception if something goes wrong. Returns a string which is the filename of the catalogue data downloaded. If nothing was found the string will be empty.
		/// </summary>
		/// <param name="ra">The right acension of the field center.</param>
		/// <param name="dec">The declination of the field center.</param>
		/// <param name="scale">The plate scale in arcseconds per pixel.</param>
		/// <param name="pixwidth">The number of horizontal pixels of the image.</param>
		/// <param name="pixheight">The number of vertical pixels of the image</param>
		/// <param name="optArgs">Optional arguments list. Possible arguments can be found here: https://github.com/user29A/AstraCarta/wiki, or with AstraCarta.Help call.
		/// <br />Be sure to pass the switch "-argument"; for example do not pass "buffer", but pass "-buffer", etc.
		/// <br />Arguments and their values must be consecutive, for example: optArgs.Add("-buffer); optArgs.Add(2); and so on.
		/// <br />Boolean arguments do not require a value, and their presence indicates true. For example the presence of optArgs.Add("-fitsout") equates to true for writing the file as a FITS bintable.</param>
		public static string Query(double ra, double dec, double scale, int pixwidth, int pixheight, ArrayList? optArgs = null)
		{
			try
			{
				if (optArgs == null)
					optArgs = new ArrayList();

				int n;

				double maglimit = 100;
				n = optArgs.IndexOf("-maglimit");
				if (n != -1)
					maglimit = Convert.ToDouble(optArgs[n + 1]);

				double buffer = 0;
				n = optArgs.IndexOf("-buffer");
				if (n != -1)
					buffer = Convert.ToDouble(optArgs[n + 1]); //arcminutes

				double offsetra = 0;
				n = optArgs.IndexOf("-offsetra");
				if (n != -1)
					offsetra = Convert.ToDouble(optArgs[n + 1]); //arcminutes
				ra += (offsetra / 60);

				double offsetdec = 0;
				n = optArgs.IndexOf("-offsetdec");
				if (n != -1)
					offsetdec = Convert.ToDouble(optArgs[n + 1]); //arcminutes
				dec += (offsetdec / 60);

				string shape = "rectangle";
				int shapenum = 1;
				n = optArgs.IndexOf("-shape");
				if (n != -1)
					if ((string)optArgs[n + 1] != "circle" && (string)optArgs[n + 1] != "rectangle")
						throw new Exception("shape may only be \"circle\" or \"rectangle\"");
					else
						shape = (string)optArgs[n + 1];
				if (shape == "circle")
					if (pixwidth != pixheight)
						throw new Exception("shape may only be \"circle\" if pixwidth and pixheight are equal");
					else
						shapenum = 2;
				double radius = scale / 3600 * pixwidth / 2 + buffer / 60;// degrees
				double radiuspix = radius / (scale / 3600);

				double rotation = 0;
				n = optArgs.IndexOf("-rotation");
				if (n != -1)
					if (shape == "circle")
						throw new Exception("rotation doesn't make sense with a cirlce query.");
					else
						rotation = Convert.ToDouble(optArgs[n + 1]) * Math.PI / 180;// radians

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
				n = optArgs.IndexOf("-catalogue");
				if (n != -1)
					catalogue = (string)optArgs[n + 1];
				if (catalogue != "GaiaDR3")
					throw new Exception("Catalogue " + catalogue + " not valid.");

				string filter = "g";
				int filternum = 3;
				n = optArgs.IndexOf("-filter");
				if (n != -1)
					filter = (string)optArgs[n + 1];
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
				n = optArgs.IndexOf("-imageout");
				if (n != -1)
					imageout = (bool)optArgs[n + 1];

				string outformat = ".csv";
				bool fitsout = false;
				n = optArgs.IndexOf("-fitsout");
				if (n != -1)
					fitsout = true;
				if (fitsout == true)
					outformat = ".fit";

				bool imageshow = false;
				n = optArgs.IndexOf("-imageshow");
				if (n != -1)
					imageshow = true;

				string outdir = Directory.GetCurrentDirectory();
				n = optArgs.IndexOf("-outdir");
				if (n != -1)
					outdir = (string)optArgs[n + 1];
				if (!Directory.Exists(outdir))
					Directory.CreateDirectory(outdir);

				string rawoutdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AstraCarta", "AstraCartaRawQueries");
				if (!Directory.Exists(rawoutdir))
					Directory.CreateDirectory(rawoutdir);

				bool forcenew = false;
				n = optArgs.IndexOf("-forcenew");
				if (n != -1)
					forcenew = true;

				int nquery = 500;
				n = optArgs.IndexOf("-nquery");
				if (n != -1)
					nquery = Convert.ToInt32(optArgs[n + 1]);

				double pmepoch = 0;
				n = optArgs.IndexOf("-pmepoch");
				if (n != -1)
					pmepoch = Convert.ToDouble(optArgs[n + 1]);

				double pmlimit = Double.MaxValue;
				n = optArgs.IndexOf("-pmlimit");
				if (n != -1)
					if (pmepoch == 0)
						throw new Exception("pmlimit is only valid if pmepoch is specified");
					else
						pmlimit = Convert.ToDouble(optArgs[n + 1]);

				string entries = "ref_epoch, ra, ra_error, dec, dec_error, pmra, pmra_error, pmdec, pmdec_error, pm, phot_bp_mean_mag, phot_g_mean_mag, phot_rp_mean_mag";
				n = optArgs.IndexOf("-entries");
				if (n != -1)
					if ((string)optArgs[n + 1] == "all")
						entries = "*";
					else
						entries += " " + (string)optArgs[n + 1];

				bool notableout = false;
				n = optArgs.IndexOf("-notableout");
				if (n != -1)
					notableout = true;

				bool rmvrawquery = false;
				n = optArgs.IndexOf("-rmvrawquery");
				if (n != -1)
					rmvrawquery = true;

				double rawqueryfilenamehash, fileoutfilenamehash;
				if (shape == "circle")
				{
					rawqueryfilenamehash = (double)Tuple.Create(ra, dec, nquery, cataloguenum, filternum, radius * 3600, shapenum).GetHashCode() + Int32.MaxValue + 1;
					fileoutfilenamehash = (double)Tuple.Create(rawqueryfilenamehash, maglimit, pmepoch, pmlimit).GetHashCode() + Int32.MaxValue + 1;
				}
				else
				{
					rawqueryfilenamehash = (double)Tuple.Create(ra, dec, nquery, cataloguenum, filternum, rotation, ra_topleft, dec_topleft).GetHashCode();
					rawqueryfilenamehash = (double)Tuple.Create(rawqueryfilenamehash, ra_topright, dec_topright, ra_bottomright, dec_bottomright, ra_bottomleft, dec_bottomleft, shapenum).GetHashCode() + Int32.MaxValue + 1;
					fileoutfilenamehash = (double)(new double[] { rawqueryfilenamehash, maglimit, pmepoch, pmlimit }).GetHashCode() + Int32.MaxValue + 1;
				}

				string outname = fileoutfilenamehash.ToString();
				n = optArgs.IndexOf("-outname");
				if (n != -1)
					outname = (string)optArgs[n + 1];

				bool overwrite = false;
				n = optArgs.IndexOf("-overwrite");
				if (n != -1)
					overwrite = true;

				#pragma warning disable CS0219 // Variable is assigned but its value is never used
				bool silent = false;
				#pragma warning restore CS0219 // Variable is assigned but its value is never used
				n = optArgs.IndexOf("-silent");
				if (n != -1)
					silent = true;

				string rawqueryfilename = Path.Combine(rawoutdir, rawqueryfilenamehash.ToString() + ".csv");
				string resultsfilename = Path.Combine(outdir, outname + outformat);
				string imagefilename = Path.Combine(outdir, outname + ".jpg");

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
							jobstr += string.Format("POLYGON('ICRS',{0},{1},{2},{3},{4},{5},{6},{7}))", ra_topleft, dec_topleft, ra_topright, dec_topright, ra_bottomright, dec_bottomright, ra_bottomleft, dec_bottomleft) + Environment.NewLine;
						else
							jobstr += string.Format("CIRCLE('ICRS',{0},{1},{2}))", ra, dec, radius) + Environment.NewLine;

						jobstr += "ORDER by gaiadr3.gaia_source." + filter + " ASC";

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
						string content = "";
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
								if (pmepoch != 0)//update ra & dec
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
					fbt.SetTTYPEEntries(ttypes, null, table);
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
				MessageBox.Show(ex.Message + Environment.NewLine + ex.TargetSite + Environment.NewLine + ex.StackTrace + Environment.NewLine + ex.Source + Environment.NewLine + ex.InnerException);
				return "";
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
		/// <br />Be sure to pass the switch "-argument"; for example do not pass "buffer", but pass "-buffer", etc.
		/// <br />Arguments and their values must be consecutive, for example: optArgs.Add("-buffer); optArgs.Add(2); and so on.
		/// <br />Boolean arguments do not require a value, and their presence indicates true. For example the presence of optArgs.Add("-fitsout") equates to true for writing the file as a FITS bintable.</param>
		public AstraCarta(double ra, double dec, double scale, int pixwidth, int pixheight, ArrayList? optArgs = null)
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

			int n;

			BufferTextBox.Text = "";
			n = optArgs.IndexOf("-buffer");
			if (n != -1)
				BufferTextBox.Text = Convert.ToDouble(optArgs[n + 1]).ToString();

			RAOffsetTextBox.Text = "";
			n = optArgs.IndexOf("-offsetra");
			if (n != -1)
				RAOffsetTextBox.Text = Convert.ToDouble(optArgs[n + 1]).ToString();

			DecOffsetTextBox.Text = "";
			n = optArgs.IndexOf("-offsetdec");
			if (n != -1)
				DecOffsetTextBox.Text = Convert.ToDouble(optArgs[n + 1]).ToString();

			NameTextBox.Text = "";
			n = optArgs.IndexOf("-outname");
			if (n != -1)
				NameTextBox.Text = string.Join("_", ((string)optArgs[n + 1]).Split(Path.GetInvalidFileNameChars())) + "_AstraCarta";

			CatalogueDrop.SelectedIndex = 0;//GaiaDR3
			n = optArgs.IndexOf("-catalogue");
			if (n != -1)
				if ((string)optArgs[n + 1] == "GaiaDR3")
					CatalogueDrop.SelectedIndex = 0;
				else
					throw new Exception("Catalogue: '" + (string)optArgs[n + 1] + "' not recognized...");

			FilterDrop.SelectedIndex = 1;//GaiaDR3 g
			n = optArgs.IndexOf("-filter");
			if (n != -1)
				if (CatalogueDrop.SelectedIndex == 0)//gaiadr3
				{
					if ((string)optArgs[n + 1] == "bp")
						FilterDrop.SelectedIndex = 0;
					else if ((string)optArgs[n + 1] == "g")
						FilterDrop.SelectedIndex = 1;
					else if ((string)optArgs[n + 1] == "rp")
						FilterDrop.SelectedIndex = 2;
					else
						throw new Exception("Filter: '" + (string)optArgs[n + 1] + "' not recognized for catalogue '" + CatalogueDrop.SelectedItem.ToString() + "'");
				}
				else
					throw new Exception("Filter: '" + (string)optArgs[n + 1] + "' not recognized...");

			MagLimitTextBox.Text = "";
			n = optArgs.IndexOf("-maglimit");
			if (n != -1)
				MagLimitTextBox.Text = Convert.ToDouble(optArgs[n + 1]).ToString();

			ShapeDrop.SelectedIndex = 0;
			n = optArgs.IndexOf("-shape");
			if (n != -1)
				if ((string)optArgs[n + 1] == "circle")
					ShapeDrop.SelectedIndex = 1;
				else if ((string)optArgs[n + 1] == "rectangle" || (string)optArgs[n + 1] == "square")
					ShapeDrop.SelectedIndex = 0;
				else
					throw new Exception("Shape: '" + (string)optArgs[n + 1] + "' not recognized.");

			RotationTextBox.Text = "";
			n = optArgs.IndexOf("-rotation");
			if (n != -1)
				RotationTextBox.Text = Convert.ToDouble(optArgs[n + 1]).ToString();

			NQueryTextBox.Text = "500";
			n = optArgs.IndexOf("-nquery");
			if (n != -1)
				NQueryTextBox.Text = Convert.ToDouble(optArgs[n + 1]).ToString();

			PMEpochTextBox.Text = "";
			n = optArgs.IndexOf("-pmepoch");
			if (n != -1)
				PMEpochTextBox.Text = Convert.ToDouble(optArgs[n + 1]).ToString();

			PMLimitTextBox.Text = "";
			n = optArgs.IndexOf("-pmlimit");
			if (n != -1)
				PMLimitTextBox.Text = Convert.ToDouble(optArgs[n + 1]).ToString();

			DirectoryTextBox.Text = "";
			n = optArgs.IndexOf("-outdir");
			if (n != -1)
				DirectoryTextBox.Text = (string)optArgs[n + 1];

			EntriesTextBox.Text = "";
			n = optArgs.IndexOf("-entries");
			if (n != -1)
				EntriesTextBox.Text = (string)optArgs[n + 1];

			SaveTableChck.Checked = true;
			n = optArgs.IndexOf("-notable");
			if (n != -1)
				SaveTableChck.Checked = false;

			FITSTableChck.Checked = false;
			n = optArgs.IndexOf("-fitsout");
			if (n != -1)
				FITSTableChck.Checked = true;

			ShowImageChck.Checked = false;
			n = optArgs.IndexOf("-imageshow");
			if (n != -1)
				ShowImageChck.Checked = true;

			SaveImageChck.Checked = false;
			n = optArgs.IndexOf("-outimage");
			if (n != -1)
				SaveImageChck.Checked = true;

			ForceNewChck.Checked = false;
			n = optArgs.IndexOf("-forcenew");
			if (n != -1)
				ForceNewChck.Checked = true;

			RemoveRawQueryChck.Checked = false;
			n = optArgs.IndexOf("-rmvrawquery");
			if (n != -1)
				RemoveRawQueryChck.Checked = true;

			SilentChck.Checked = false;
			n = optArgs.IndexOf("-silent");
			if (n != -1)
				SilentChck.Checked = true;

			OverwriteChck.Checked = false;
			n = optArgs.IndexOf("-overwrite");
			if (n != -1)
				OverwriteChck.Checked = true;
		}

		private void ExecuteBtn_Click(object sender, EventArgs e)
		{
			ArrayList optArgs = new ArrayList();
			if (BufferTextBox.Text != "")
			{
				optArgs.Add("-buffer");
				optArgs.Add(Convert.ToDouble(BufferTextBox.Text));
			}
			if (RAOffsetTextBox.Text != "")
			{
				optArgs.Add("-offsetra");
				optArgs.Add(Convert.ToDouble(RAOffsetTextBox.Text));
			}
			if (DecOffsetTextBox.Text != "")
			{
				optArgs.Add("-offsetdec");
				optArgs.Add(Convert.ToDouble(DecOffsetTextBox.Text));
			}
			if (NameTextBox.Text != "")
			{
				optArgs.Add("-outname");
				optArgs.Add(NameTextBox.Text);
			}
			optArgs.Add("-catalogue");
			optArgs.Add(CatalogueDrop.SelectedItem.ToString());
			optArgs.Add("-filter");
			optArgs.Add(FilterDrop.SelectedItem.ToString());
			if (MagLimitTextBox.Text != "")
			{
				optArgs.Add("-maglimit");
				optArgs.Add(Convert.ToDouble(MagLimitTextBox.Text));
			}
			optArgs.Add("-shape");
			optArgs.Add(ShapeDrop.SelectedItem.ToString());
			if (ShapeDrop.SelectedItem.ToString() == "rectangle")
				if (RotationTextBox.Text != "")
				{
					optArgs.Add("-rotation");
					optArgs.Add(Convert.ToDouble(RotationTextBox.Text));
				}
			optArgs.Add("-nquery");
			optArgs.Add(NQueryTextBox.Text);
			if (PMEpochTextBox.Text != "")
			{
				optArgs.Add("-pmepoch");
				optArgs.Add(Convert.ToDouble(PMEpochTextBox.Text));
			}
			if (PMLimitTextBox.Text != "")
			{
				optArgs.Add("-pmlimit");
				optArgs.Add(Convert.ToDouble(PMLimitTextBox.Text));
			}
			if (DirectoryTextBox.Text != "")
			{
				optArgs.Add("-outdir");
				optArgs.Add(DirectoryTextBox.Text);
			}
			optArgs.Add("-entries");
			optArgs.Add(EntriesTextBox.Text);
			if (!SaveTableChck.Checked)
				optArgs.Add("-notable");
			if (FITSTableChck.Checked)
				optArgs.Add("-fitsout");
			if (ShowImageChck.Checked)
				optArgs.Add("-imageshow");
			if (SaveImageChck.Checked)
				optArgs.Add("-outimage");
			if (ForceNewChck.Checked)
				optArgs.Add("-forcenew");
			if (RemoveRawQueryChck.Checked)
				optArgs.Add("-rmvrawquery");
			if (SilentChck.Checked)
				optArgs.Add("-silent");
			if (OverwriteChck.Checked)
				optArgs.Add("-overwrite");

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
			ArrayList optArgs = (ArrayList)((object[])e.Argument)[5];

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
			AstraCarta.Help();
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
