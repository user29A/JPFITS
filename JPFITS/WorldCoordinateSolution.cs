using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
#nullable enable

namespace JPFITS
{
	/// <summary>WorldCoordinateSolution class for creating, interacting with, and solving the paramaters for World Coordinate Solutions for the FITS image standard.</summary>
	public class WorldCoordinateSolution
	{
		public enum WCSType
		{
			/// <summary>
			/// Tangent-plane Gnomic projection.
			/// </summary>
			TAN
		}

		#region CONSTRUCTORS

		/// <summary>Default constructor.</summary>
		public WorldCoordinateSolution()
		{

		}

		/// <summary>Constructor based on an existing FITS primary image header which contains FITS standard keywords for a WCS solution. If a WCS solution is not present in the header, then the Exists property will be false.</summary>
		public WorldCoordinateSolution(JPFITS.FITSHeader header)
		{
			EATHEADERFORWCS(header);
		}

		/// <summary>
		/// Constructor based on basic parameters of a World Coordinate Solution
		/// </summary>
		/// <param name="cdelt">The plate scale in arcseconds per pixel.</param>
		/// <param name="crpix1">The coordinate reference pixel on the x-axis.</param>
		/// <param name="crpix2">The coordinate reference pixel on the y-axis.</param>
		/// <param name="zeroBasedCRpix">True if the CRPIX values are from zero-based indexing. If true will increment CRPIXn internally by 1.</param>
		/// <param name="crvalRA">The coordinate reference value on the RA axis.</param>
		/// <param name="crvalDec">The coordinate reference value on the dec axis.</param>
		/// <param name="rotation">The field rotation, in degrees.</param>
		public WorldCoordinateSolution(double cdelt, double crpix1, double crpix2, bool zeroBasedCRpix, double crvalRA, double crvalDec, double rotation)
		{
			CTYPEN = new string[2];
			CTYPEN[0] = "RA---TAN";
			CTYPEN[1] = "DEC--TAN";

			CDELTN = new double[2];
			CDELTN[0] = cdelt;
			CDELTN[1] = cdelt;

			CRPIXN = new double[2];
			CRPIXN[0] = crpix1;
			CRPIXN[1] = crpix2;
			if (zeroBasedCRpix)
			{
				CRPIXN[0]++;
				CRPIXN[1]++;
			}

			CRVALN = new double[2];
			CRVALN[0] = crvalRA;
			CRVALN[1] = crvalDec;

			CROTAN = new double[2];
			CROTAN[0] = rotation;
			CROTAN[1] = rotation;

			rotation *= Math.PI / 180;
			CDMATRIX = new double[2, 2];
			CDMATRIX[0, 0] = -cdelt / 3600 * Math.Cos(rotation);
			CDMATRIX[1, 0] = cdelt / 3600 * Math.Sin(rotation);
			CDMATRIX[0, 1] = -cdelt / 3600 * Math.Sin(rotation);
			CDMATRIX[1, 1] = -cdelt / 3600 * Math.Cos(rotation);

			CD1_1 = CDMATRIX[0, 0];
			CD1_2 = CDMATRIX[1, 0];
			CD2_1 = CDMATRIX[0, 1];
			CD2_2 = CDMATRIX[1, 1];			

			SET_CDMATRIXINV();

			WCSEXISTS = true;
		}

		#endregion

		#region PRIVATE

		private double[,]? CDMATRIX;
		private double[,]? CDMATRIXINV;
		private double[]? CVAL1;
		private double[]? CVAL2;
		private double[]? DVAL1;
		private double[]? DVAL2;
		private double[]? CPIX1;
		private double[]? CPIX2;
		private double[]? CRVALN;
		private double[]? CRPIXN;
		private double[]? CDELTN;
		private double[]? CROTAN;
		private string[]? CTYPEN;
		private double CD1_1, CD1_2, CD2_1, CD2_2, CPIX1RM, CPIX1RS, CVAL1RM, CVAL1RS, CPIX2RM, CPIX2RS, CVAL2RM, CVAL2RS, CPIXRM, CPIXRS, CVALRM, CVALRS, CCVALD1, CCVALD2;
		private String? CCVALS1;
		private String? CCVALS2;
		private bool WCSEXISTS = false;
		private PointF[][]? RAS_LINES;
		private PointF[][]? DEC_LINES;
		string[]? RAS_LABELS;
		string[]? DEC_LABELS;
		private PointF[]? RAS_LABEL_LOCATIONS;
		private PointF[]? DEC_LABEL_LOCATIONS;
		bool VALIDWCSGRIDLINES = false;
		private string[,]? WCS_TABLE;

		private void GENERATE_WCS_TABLE()
		{
			WCS_TABLE = new string[7, CVAL1.Length + 1];

			WCS_TABLE[0, 0] = "Source Pixel X-Centroid (pixels)";
			WCS_TABLE[1, 0] = "Source Pixel Y-Centroid (pixels)";
			WCS_TABLE[2, 0] = "Source Catalogue RA (degrees)";
			WCS_TABLE[3, 0] = "Source Catalogue Decl (degrees)";
			WCS_TABLE[4, 0] = "WCS RA Residual (arcsec)";
			WCS_TABLE[5, 0] = "WCS Decl Residual (arcsec)";
			WCS_TABLE[6, 0] = "WCS Residual (arcsec)";

			for (int i = 0; i < CVAL1.Length; i++)
			{
				WCS_TABLE[0, i + 1] = CPIX1[i].ToString();
				WCS_TABLE[1, i + 1] = CPIX2[i].ToString();
				WCS_TABLE[2, i + 1] = CVAL1[i].ToString();
				WCS_TABLE[3, i + 1] = CVAL2[i].ToString();
				WCS_TABLE[4, i + 1] = DVAL1[i].ToString();
				WCS_TABLE[5, i + 1] = DVAL2[i].ToString();
				WCS_TABLE[6, i + 1] = Math.Sqrt(DVAL1[i] * DVAL1[i] + DVAL2[i] * DVAL2[i]).ToString();
			}
		}

		private void SET_CDMATRIXINV()
		{
			CDMATRIXINV = new double[2, 2];
			double det = 1 / ((CDMATRIX[0, 0] * CDMATRIX[1, 1] - CDMATRIX[1, 0] * CDMATRIX[0, 1]) * Math.PI / 180);
			CDMATRIXINV[0, 0] = det * CDMATRIX[1, 1];
			CDMATRIXINV[1, 0] = -det * CDMATRIX[1, 0];
			CDMATRIXINV[0, 1] = -det * CDMATRIX[0, 1];
			CDMATRIXINV[1, 1] = det * CDMATRIX[0, 0];
		}

		private void EATHEADERFORWCS(JPFITS.FITSHeader header)
		{
			CD1_1 = (double)header.GetKeyIndex("CD1_1", false);
			if (CD1_1 < 0)
			{
				WCSEXISTS = false;
				return;
			}
			CD1_1 = Convert.ToDouble(header[(int)Math.Round(CD1_1)].Value);

			CTYPEN = new string[2];
			CRPIXN = new double[2];
			CRVALN = new double[2];

			Parallel.Invoke(
				() => CTYPEN[0] = header.GetKeyValue("CTYPE1"),
				() => CTYPEN[1] = header.GetKeyValue("CTYPE2"),
				() => CD1_2 = Convert.ToDouble(header.GetKeyValue("CD1_2")),
				() => CD2_1 = Convert.ToDouble(header.GetKeyValue("CD2_1")),
				() => CD2_2 = Convert.ToDouble(header.GetKeyValue("CD2_2")),
				() => CRPIXN[0] = Convert.ToDouble(header.GetKeyValue("CRPIX1")),
				() => CRPIXN[1] = Convert.ToDouble(header.GetKeyValue("CRPIX2")),
				() => CRVALN[0] = Convert.ToDouble(header.GetKeyValue("CRVAL1")),
				() => CRVALN[1] = Convert.ToDouble(header.GetKeyValue("CRVAL2")));

			CDMATRIX = new double[2, 2];
			CDMATRIXINV = new double[2, 2];
			CDMATRIX[0, 0] = CD1_1;
			CDMATRIX[1, 0] = CD1_2;
			CDMATRIX[0, 1] = CD2_1;
			CDMATRIX[1, 1] = CD2_2;
			SET_CDMATRIXINV();

			CDELTN = new double[2];
			CDELTN[0] = Math.Sqrt(CD1_1 * CD1_1 + CD1_2 * CD1_2) * 3600;
			CDELTN[1] = Math.Sqrt(CD2_1 * CD2_1 + CD2_2 * CD2_2) * 3600;

			CROTAN = new double[(2)];
			CROTAN[0] = Math.Atan2(CD1_2, -CD1_1) * 180 / Math.PI;
			CROTAN[1] = Math.Atan2(-CD2_1, -CD2_2) * 180 / Math.PI;

			WCSEXISTS = true;

			//optionally populate this?
			Parallel.Invoke(
				() =>
				{
					int ind = header.GetKeyIndex("CCVALD1", false);
					if (ind != -1)
						CCVALD1 = Convert.ToDouble(header[ind].Value);
				},
				() =>
				{
					int ind = header.GetKeyIndex("CCVALD2", false);
					if (ind != -1)
						CCVALD2 = Convert.ToDouble(header[ind].Value);
				},
				() =>
				{
					int ind = header.GetKeyIndex("CCVALS1", false);
					if (ind != -1)
						CCVALS1 = header.GetKeyValue("CCVALS1");
				},
				() =>
				{
					int ind = header.GetKeyIndex("CCVALS2", false);
					if (ind != -1)
						CCVALS2 = header.GetKeyValue("CCVALS2");
				},
				() =>
				{
					int ind = header.GetKeyIndex("CPIX1RM", false);
					if (ind != -1)
						CPIX1RM = Convert.ToDouble(header[ind].Value);
				},
				() =>
				{
					int ind = header.GetKeyIndex("CPIX1RS", false);
					if (ind != -1)
						CPIX1RS = Convert.ToDouble(header[ind].Value);
				},
				() =>
				{
					int ind = header.GetKeyIndex("CVAL1RM", false);
					if (ind != -1)
						CVAL1RM = Convert.ToDouble(header[ind].Value);
				},
				() =>
				{
					int ind = header.GetKeyIndex("CVAL1RS", false);
					if (ind != -1)
						CVAL1RS = Convert.ToDouble(header[ind].Value);
				},
				() =>
				{
					int ind = header.GetKeyIndex("CPIX2RM", false);
					if (ind != -1)
						CPIX2RM = Convert.ToDouble(header[ind].Value);
				},
				() =>
				{
					int ind = header.GetKeyIndex("CPIX2RS", false);
					if (ind != -1)
						CPIX2RS = Convert.ToDouble(header[ind].Value);
				},
				() =>
				{
					int ind = header.GetKeyIndex("CVAL2RM", false);
					if (ind != -1)
						CVAL2RM = Convert.ToDouble(header[ind].Value);
				},
				() =>
				{
					int ind = header.GetKeyIndex("CVAL2RS", false);
					if (ind != -1)
						CVAL2RS = Convert.ToDouble(header[ind].Value);
				},
				() =>
				{
					int ind = header.GetKeyIndex("CPIXRM", false);
					if (ind != -1)
						CPIXRM = Convert.ToDouble(header[ind].Value);
				},
				() =>
				{
					int ind = header.GetKeyIndex("CPIXRS", false);
					if (ind != -1)
						CPIXRS = Convert.ToDouble(header[ind].Value);
				},
				() =>
				{
					int ind = header.GetKeyIndex("CVALRM", false);
					if (ind != -1)
						CVALRM = Convert.ToDouble(header[ind].Value);
				},
				() =>
				{
					int ind = header.GetKeyIndex("CVALRS", false);
					if (ind != -1)
						CVALRS = Convert.ToDouble(header[ind].Value);
				}
			);

			int num = 0, key = 0;
			while (key != -1)
			{
				num++;
				key = header.GetKeyIndex("WCP1_" + num.ToString("000"), false);
			}
			num--;
			CPIX1 = new double[(num)];
			CPIX2 = new double[(num)];
			CVAL1 = new double[(num)];
			CVAL2 = new double[(num)];
			DVAL1 = new double[(num)];
			DVAL2 = new double[(num)];

			Parallel.For(1, CPIX1.Length + 1, i =>
			{
				int ind = header.GetKeyIndex("WCP1_" + i.ToString("000"), false);
				if (ind != -1)
					CPIX1[i - 1] = Convert.ToDouble(header[ind].Value);

				ind = header.GetKeyIndex("WCP2_" + i.ToString("000"), false);
				if (ind != -1)
					CPIX2[i - 1] = Convert.ToDouble(header[ind].Value);

				ind = header.GetKeyIndex("WCV1_" + i.ToString("000"), false);
				if (ind != -1)
					CVAL1[i - 1] = Convert.ToDouble(header[ind].Value);

				ind = header.GetKeyIndex("WCV2_" + i.ToString("000"), false);
				if (ind != -1)
					CVAL2[i - 1] = Convert.ToDouble(header[ind].Value);

				ind = header.GetKeyIndex("WCD1_" + i.ToString("000"), false);
				if (ind != -1)
					DVAL1[i - 1] = Convert.ToDouble(header[ind].Value);

				ind = header.GetKeyIndex("WCD2_" + i.ToString("000"), false);
				if (ind != -1)
					DVAL2[i - 1] = Convert.ToDouble(header[ind].Value);
			});
		}

		#endregion

		#region PROPERTIES

		/// <summary>Returns an array of PointF locations created within Grid_MakeWCSGrid for printing labels to the window.</summary>
		public PointF[] Grid_RightAscensionLabelLocations
		{
			get { return RAS_LABEL_LOCATIONS; }
		}

		/// <summary>Returns an array of PointF locations created within Grid_MakeWCSGrid for printing labels to the window.</summary>
		public PointF[] Grid_DeclinationLabelLocations
		{
			get { return DEC_LABEL_LOCATIONS; }
		}

		/// <summary>Returns an array of Right Ascension sexagesimal values for each grid line from Grid_RightAscensionPoints.</summary>
		public string[] Grid_RightAscensionLabels
		{
			get { return RAS_LABELS; }
		}

		/// <summary>Returns an array of Declination sexagesimal values for each grid line from Grid_DeclinationPoints.</summary>
		public string[] Grid_DeclinationLabels
		{
			get { return DEC_LABELS; }
		}

		/// <summary>Returns an array of arrays of PointF's each of which represent grid lines at intervals of Right Ascension.</summary>
		public PointF[][] Grid_RightAscensionPoints
		{
			get { return RAS_LINES; }
		}

		/// <summary>Returns an array of arrays of PointF's each of which represent grid lines at intervals of Declination.</summary>
		public PointF[][] Grid_DeclinationPoints
		{
			get { return DEC_LINES; }
		}

		/// <summary>Gets or Sets the column-major CD matrix for this class instance.</summary>
		public double[,] CD_Matrix
		{
			get { return CDMATRIX; }
			set
			{
				CDMATRIX = value;
				CD1_1 = CDMATRIX[0, 0];
				CD1_2 = CDMATRIX[1, 0];
				CD2_1 = CDMATRIX[0, 1];
				CD2_2 = CDMATRIX[1, 1];
				SET_CDMATRIXINV();
			}
		}

		/// <summary>Gets the inverse of the CD matrix.</summary>
		public double[,] CD_Matrix_Inverse
		{
			get { return CDMATRIXINV; }
		}

		/// <summary>Gets the mean of the residuals of the WCS solution in pixel units; should be very small as per least squares minimization.</summary>
		public double WCSFitResidual_MeanPix
		{
			get { return CPIXRM; }
		}

		/// <summary>Gets the standard deviation of the residuals of the WCS solution in pixel units; gives the average WCS solution error in pixels.</summary>
		public double WCSFitResidual_StdvPix
		{
			get { return CPIXRS; }
		}

		/// <summary>Gets the mean of the residuals of the WCS solution in arcseconds; should be very small as per least squares minimization.</summary>
		public double WCSFitResidual_MeanSky
		{
			get { return CVALRM; }
		}

		/// <summary>Gets the standard deviation of the residuals of the WCS solution in arcseconds; gives the average WCS solution error in arcseconds.</summary>
		public double WCSFitResidual_StdvSky
		{
			get { return CVALRS; }
		}

		public string[,] WCS_Table
		{
			get 
			{
				GENERATE_WCS_TABLE();
				return WCS_TABLE; 
			}
		}

		#endregion

		#region MEMBERS

		/// <summary>
		/// Useful when an image containing a WCS solution is binned - the WCS parameters can be binned so that the transformation remains consistent with the new image scale and dimensions.
		/// </summary>
		/// <param name="binning">The binning factor, equal for both image axes.</param>
		/// <param name="header">Optionally provide a header to update the keywords. Pass null if no header present or necessary.</param>
		public void Bin(int binning, JPFITS.FITSHeader? header)
		{
			this.SetCDi_j(1, 1, this.GetCDi_j(1, 1) * binning);
			this.SetCDi_j(1, 2, this.GetCDi_j(1, 2) * binning);
			this.SetCDi_j(2, 1, this.GetCDi_j(2, 1) * binning);
			this.SetCDi_j(2, 2, this.GetCDi_j(2, 2) * binning);
			this.SetCRPIXn(1, this.GetCRPIXn(1) / binning);
			this.SetCRPIXn(2, this.GetCRPIXn(2) / binning);
			this.CDELTN[0] *= binning;
			this.CDELTN[1] *= binning;

			if (header == null)
				return;

			header.SetKey("CD1_1", this.GetCDi_j(1, 1).ToString(), true, -1);
			header.SetKey("CD1_2", this.GetCDi_j(1, 2).ToString(), true, -1);
			header.SetKey("CD2_1", this.GetCDi_j(2, 1).ToString(), true, -1);
			header.SetKey("CD2_2", this.GetCDi_j(2, 2).ToString(), true, -1);
			header.SetKey("CRPIX1", this.GetCRPIXn(1).ToString(), true, -1);
			header.SetKey("CRPIX2", this.GetCRPIXn(2).ToString(), true, -1);
			header.SetKey("CDELT1", this.GetCDELTn(1).ToString(), true, -1);
			header.SetKey("CDELT2", this.GetCDELTn(2).ToString(), true, -1);
		}

		/// <summary>
		/// Useful when an image containing a WCS is snipped or cut to a smaller region - the WCS parameters can be adjusted so that the transformation remains consistent with the new image size.
		/// </summary>
		/// <param name="cutregion_xmin">The minimum x-value of the snipped or cut region.</param>
		/// <param name="cutregion_ymin">The minimum y-value of the snipped or cut region.</param>
		/// <param name="header">Optionally provide a header to update the keywords. Pass null if no header present or necessary.</param> 
		public void Cut(double cutregion_xmin, double cutregion_ymin, JPFITS.FITSHeader? header)
		{
			this.SetCRPIXn(1, this.GetCRPIXn(1) - cutregion_xmin);
			this.SetCRPIXn(2, this.GetCRPIXn(2) - cutregion_ymin);

			if (header == null)
				return;

			header.SetKey("CRPIX1", this.GetCRPIXn(1).ToString(), true, -1);
			header.SetKey("CRPIX2", this.GetCRPIXn(2).ToString(), true, -1);
		}

		///// <summary>
		///// Rotate the CD parameters of a WCS solution by some angle.
		///// </summary>
		///// <param name="rotation">The rotation, in degrees.</param>
		///// <param name="header">Optionally provide a header to update the keywords. Pass null if no header present or necessary.</param>
		//public void Rotate(double rotation, FITSHeader? header)
		//{
		//	CDMATRIX[0, 0] = -CDELTN[0] / 3600 * Math.Cos((CROTAN[0] + rotation) * Math.PI / 180);
		//	CDMATRIX[1, 0] = CDELTN[1] / 3600 * Math.Sin((CROTAN[1] + rotation) * Math.PI / 180);
		//	CDMATRIX[0, 1] = -CDELTN[0] / 3600 * Math.Sin((CROTAN[0] + rotation) * Math.PI / 180);
		//	CDMATRIX[1, 1] = -CDELTN[1] / 3600 * Math.Cos((CROTAN[1] + rotation) * Math.PI / 180);

		//	CD1_1 = CDMATRIX[0, 0];
		//	CD1_2 = CDMATRIX[1, 0];
		//	CD2_1 = CDMATRIX[0, 1];
		//	CD2_2 = CDMATRIX[1, 1];

		//	CROTAN[0] += rotation;
		//	CROTAN[1] += rotation;

		//	SET_CDMATRIXINV();
		//}

		/// <summary>Recomputes the existing grid so that it can be updated for new display settings.</summary>
		public void Grid_Refresh()
		{
			VALIDWCSGRIDLINES = false;
		}

		/// <summary>Gets the one-based row-major element from the CD matrix CDi_j[int i, int j], where i is the row index, and j is the column index.</summary>
		public double GetCDi_j(int i, int j)
		{
			return CDMATRIX[j - 1, i - 1];
		}

		/// <summary>Sets the one-based row-major element from the CD matrix CDi_j[int i, int j], where i is the row index, and j is the column index.</summary>
		public void SetCDi_j(int i, int j, double val)
		{
			CDMATRIX[j - 1, i - 1] = val;
			CD1_1 = CDMATRIX[0, 0];
			CD1_2 = CDMATRIX[1, 0];
			CD2_1 = CDMATRIX[0, 1];
			CD2_2 = CDMATRIX[1, 1];
			SET_CDMATRIXINV();
		}

		public double[] GetWCSResiduals(int coordinate_Axis)
		{
			if (coordinate_Axis == 1)
				return DVAL1;
			if (coordinate_Axis == 2)
				return DVAL2;

			throw new Exception("coordinate_Axis '" + coordinate_Axis + "' not either 1 or 2");
		}

		/// <summary>Gets the array of coordinate values on one-based axis i used for this World Coordinate Solution.</summary>
		/// <param name="coordinate_Axis"></param>
		/// <param name="asPixelLocations"></param>
		/// <param name="returnZeroBasedPixels"></param>
		public double[] GetCVALValues(int coordinate_Axis, bool asPixelLocations, bool returnZeroBasedPixels)
		{
			if (!asPixelLocations)
			{
				if (coordinate_Axis == 1)
					return CVAL1;
				if (coordinate_Axis == 2)
					return CVAL2;
			}
			else
			{
				this.Get_Pixels(CVAL1, CVAL2, "TAN", out double[] x, out double[] y, returnZeroBasedPixels);

				if (coordinate_Axis == 1)
					return x;
				if (coordinate_Axis == 2)
					return y;
			}

			throw new Exception("coordinate_Axis '" + coordinate_Axis + "' not either 1 or 2");
		}

		/// <summary>Gets the array of one-based coordinate pixels on one-based axis n (Coordinate_Pixels[n]) used for this World Coordinate Solution.</summary>
		/// <param name="coordinate_Axis"></param>
		/// <param name="returnZeroBasedPixels"></param>
		public double[] GetCPIXPixels(int coordinate_Axis, bool returnZeroBasedPixels)
		{
			if (!returnZeroBasedPixels)
			{
				if (coordinate_Axis == 1)
					return CPIX1;
				if (coordinate_Axis == 2)
					return CPIX2;
			}
			else
			{
				if (coordinate_Axis == 1)
				{
					double[] x = new double[CPIX1.Length];
					for (int i = 0; i < x.Length; i++)
						x[i] = CPIX1[i] - 1;
					return x;
				}

				if (coordinate_Axis == 2)
				{
					double[] y = new double[CPIX2.Length];
					for (int i = 0; i < y.Length; i++)
						y[i] = CPIX2[i] - 1;
					return y;
				}	
			}

			throw new Exception("coordinate_Axis '" + coordinate_Axis + "' not either 1 or 2");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cpix1"></param>
		/// <param name="cpix2"></param>
		/// <param name="cpixAreZeroBased"></param>
		public void SetCPIXPixels(double[] cpix1, double[] cpix2, bool cpixAreZeroBased)
		{
			CPIX1 = new double[cpix1.Length];
			CPIX2 = new double[cpix2.Length];
			double zb = 0;
			if (cpixAreZeroBased)
				zb = 1;

			for (int i = 0; i < cpix1.Length; i++)
			{
				CPIX1[i] = cpix1[i] + zb;
				CPIX2[i] = cpix2[i] + zb;
			}
		}

		/// <summary>Gets the Coordinate Reference Value for the one-based axis n: CRVALn[int n].</summary>
		public double GetCRVALn(int coordinate_Axis)
		{
			return CRVALN[coordinate_Axis - 1];
		}

		/// <summary>Sets the Coordinate Reference Value for the one-based axis n: CRVALn[int n].</summary>
		public void SetCRVALn(int coordinate_Axis, double val)
		{
			CRVALN[coordinate_Axis - 1] = val;
		}

		/// <summary>Gets the one-based Coordinate Reference Pixel for the one-based axis n: CRPIXn[int n].</summary>
		public double GetCRPIXn(int coordinate_Axis)
		{
			return CRPIXN[coordinate_Axis - 1];
		}

		/// <summary>Sets the one-based Coordinate Reference Pixel for the one-based axis n: CRPIXn[int n].</summary>
		public void SetCRPIXn(int coordinate_Axis, double val)
		{
			CRPIXN[coordinate_Axis - 1] = val;
		}

		/// <summary>Gets the world coordinate solution plate scale (arcseconds per pixel) for one-based axis n: CDELTn(int n).</summary>
		public double GetCDELTn(int coordinate_Axis)
		{
			return CDELTN[coordinate_Axis - 1];
		}

		/// <summary>Gets the world coordinate solution field rotation (degrees) for one-based axis n: WCSROTn[int n].</summary>
		public double GetCROTAn(int coordinate_Axis)
		{
			return CROTAN[coordinate_Axis - 1];
		}

		/// <summary>Gets the world coordinate solution type for one-based axis n: CTYPEn[int n].</summary>
		public string GetCTYPEn(int coordinate_Axis)
		{
			return CTYPEN[coordinate_Axis - 1];
		}		

		/// <summary>Solves the projection parameters for a given list of pixel and coordinate values. Pass nullptr for FITS if writing WCS parameters to a primary header not required.</summary>
		/// <param name="WCS_Type">The world coordinate solution type. For example: TAN, for tangent-plane or Gnomic projection. Only TAN is currently supported.</param>
		/// <param name="X_pix">An array of the image x-axis pixel locations.</param>
		/// <param name="Y_pix">An array of the image y-axis pixel locations.</param>
		/// <param name="zero_based_pixels">A boolean to indicate if the X_Pix and Y_Pix are zero-based coordinates. They will be converted to one-based if true.</param>
		/// <param name="cval1">An array of coordinate values in degrees on coordinate axis 1.</param>
		/// <param name="cval2">An array of coordinate values in degrees on coordinate axis 2.</param>
		/// <param name="header">An FITSImageHeader instance to write the solution into. Pass null if not required.</param>
		/// <param name="verbose">Copy all WCS diagnostic data into the header in addition to essential WCS keywords.</param>
		public void Solve_WCS(WCSType WCS_Type, double[] X_pix, double[] Y_pix, bool zero_based_pixels, double[] cval1, double[] cval2, FITSHeader? header, bool verbose = false/*, WorldCoordinateSolution? INIT = null*/)
		{
			//should first do a check of WCS type to make sure it is valid
			//if (WCS_Type != "TAN")
				//throw new Exception("Solution type: '" + WCS_Type + "' is invalid. Valid solution types are: 'TAN'");

			VALIDWCSGRIDLINES = false;

			CTYPEN = new string[2];
			CTYPEN[0] = "RA---" + WCS_Type;
			CTYPEN[1] = "DEC--" + WCS_Type;

			CPIX1 = new double[X_pix.Length];
			CPIX2 = new double[X_pix.Length];
			CVAL1 = new double[X_pix.Length];
			CVAL2 = new double[X_pix.Length];
			DVAL1 = new double[X_pix.Length];
			DVAL2 = new double[X_pix.Length];
			for (int i = 0; i < X_pix.Length; i++)
			{
				CPIX1[i] = X_pix[i];
				CPIX2[i] = Y_pix[i];
				if (zero_based_pixels)
				{
					CPIX1[i]++;
					CPIX2[i]++;
				}
				CVAL1[i] = cval1[i];
				CVAL2[i] = cval2[i];
			}
			CRPIXN = new double[2];
			CRVALN = new double[2];
			CRPIXN[0] = JPMath.Mean(CPIX1, true);//fix this in the fit boundaries? - NO...let it get the best one...but they should be close
			CRPIXN[1] = JPMath.Mean(CPIX2, true);//fix this in the fit boundaries? - NO...let it get the best one...but they should be close
			CRVALN[0] = JPMath.Mean(CVAL1, true);//these are fixed as the coordinate reference value
			CRVALN[1] = JPMath.Mean(CVAL2, true);//these are fixed as the coordinate reference value

			double[] X_intrmdt = new double[CPIX1.Length];//intermediate coords (degrees)
			double[] Y_intrmdt = new double[CPIX1.Length];//intermediate coords (degrees)
			double a0 = CRVALN[0] * Math.PI / 180, d0 = CRVALN[1] * Math.PI / 180, a, d;
			for (int i = 0; i < CPIX1.Length; i++)
			{
				a = CVAL1[i] * Math.PI / 180;//radians
				d = CVAL2[i] * Math.PI / 180;//radians

				//for tangent plane Gnomic
				if (WCS_Type == WCSType.TAN)
				{
					X_intrmdt[i] = Math.Cos(d) * Math.Sin(a - a0) / (Math.Cos(d0) * Math.Cos(d) * Math.Cos(a - a0) + Math.Sin(d0) * Math.Sin(d));
					Y_intrmdt[i] = (Math.Cos(d0) * Math.Sin(d) - Math.Cos(d) * Math.Sin(d0) * Math.Cos(a - a0)) / (Math.Cos(d0) * Math.Cos(d) * Math.Cos(a - a0) + Math.Sin(d0) * Math.Sin(d));
				}
			}

			double[] P0 = new double[6] { 0, 0, 0, 0, CRPIXN[0], CRPIXN[1] };
			double[] plb = new double[6] { -0.1, -0.1, -0.1, -0.1, JPMath.Min(CPIX1, false), JPMath.Min(CPIX2, false) };
			double[] pub = new double[6] { 0.1, 0.1, 0.1, 0.1, JPMath.Max(CPIX1, false), JPMath.Max(CPIX2, false) };
			double[] scale = new double[6] { 4e-6, 4e-6, 4e-6, 4e-6, CRPIXN[0], CRPIXN[1] };

			//if (INIT != null)
			//{
			//	P0 = new double[6] { INIT.GetCDi_j(1, 1), INIT.GetCDi_j(1, 2), INIT.GetCDi_j(2, 1), INIT.GetCDi_j(2, 2), INIT.GetCRPIXn(1), INIT.GetCRPIXn(2) };
			//	//plb = new double[6] { P0[0] - Math.Abs(P0[0]), P0[1] - Math.Abs(P0[1]), P0[2] - Math.Abs(P0[2]), P0[3] - Math.Abs(P0[3]), INIT.GetCRPIXn(1) - 5, INIT.GetCRPIXn(2) - 5};
			//	//pub = new double[6] { P0[0] + Math.Abs(P0[0]), P0[1] + Math.Abs(P0[1]), P0[2] + Math.Abs(P0[2]), P0[3] + Math.Abs(P0[3]), INIT.GetCRPIXn(1) + 5, INIT.GetCRPIXn(2) + 5 };

			//	CRVALN[0] = INIT.GetCRVALn(1);
			//	CRVALN[1] = INIT.GetCRVALn(2);
			//}

			JPMath.Fit_WCSTransform2d(X_intrmdt, Y_intrmdt, CPIX1, CPIX2, ref P0, plb, pub, scale);

			CDMATRIX = new double[2, 2];
			CDMATRIX[0, 0] = P0[0] * 180 / Math.PI;
			CDMATRIX[1, 0] = P0[1] * 180 / Math.PI;
			CDMATRIX[0, 1] = P0[2] * 180 / Math.PI;
			CDMATRIX[1, 1] = P0[3] * 180 / Math.PI;
			CRPIXN[0] = P0[4];
			CRPIXN[1] = P0[5];
			CD1_1 = CDMATRIX[0, 0];
			CD1_2 = CDMATRIX[1, 0];
			CD2_1 = CDMATRIX[0, 1];
			CD2_2 = CDMATRIX[1, 1];

			CDELTN = new double[2];
			CDELTN[0] = Math.Sqrt(CD1_1 * CD1_1 + CD1_2 * CD1_2) * 3600;
			CDELTN[1] = Math.Sqrt(CD2_1 * CD2_1 + CD2_2 * CD2_2) * 3600;

			CROTAN = new double[2];
			CROTAN[0] = Math.Atan2(CD1_2, -CD1_1) * 180 / Math.PI;
			CROTAN[1] = Math.Atan2(-CD2_1, -CD2_2) * 180 / Math.PI;

			SET_CDMATRIXINV();

			double[] dxpix = new double[CPIX1.Length];
			double[] dypix = new double[CPIX1.Length];
			for (int i = 0; i < CPIX1.Length; i++)
			{
				this.Get_Pixel(CVAL1[i], CVAL2[i], "TAN", out double xpix, out double ypix, false);
				dxpix[i] = xpix - CPIX1[i];
				dypix[i] = ypix - CPIX2[i];

				DVAL1[i] = dxpix[i] * CDELTN[0];
				DVAL2[i] = dypix[i] * CDELTN[1];
			}

			CPIX1RM = JPMath.Mean(dxpix, true);
			CPIX1RS = JPMath.Stdv(dxpix, true);
			CVAL1RM = CPIX1RM * CDELTN[0];
			CVAL1RS = CPIX1RS * CDELTN[0];

			CPIX2RM = JPMath.Mean(dypix, true);
			CPIX2RS = JPMath.Stdv(dypix, true);
			CVAL2RM = CPIX2RM * CDELTN[1];
			CVAL2RS = CPIX2RS * CDELTN[1];

			CPIXRM = Math.Sqrt(CPIX1RM * CPIX1RM + CPIX2RM * CPIX2RM);
			CPIXRS = Math.Sqrt(CPIX1RS * CPIX1RS + CPIX2RS * CPIX2RS);
			CVALRM = Math.Sqrt(CVAL1RM * CVAL1RM + CVAL2RM * CVAL2RM);
			CVALRS = Math.Sqrt(CVAL1RS * CVAL1RS + CVAL2RS * CVAL2RS);

			WCSEXISTS = true;

			if (header == null)
				return;

			double width = Convert.ToDouble(header.GetKeyValue("NAXIS1"));
			double height = Convert.ToDouble(header.GetKeyValue("NAXIS2"));
			Get_Coordinate(width / 2, height / 2, false, "TAN", out double ccvald1, out double ccvald2, out string ccvals1, out string ccvals2);
			CCVALD1 = ccvald1;
			CCVALD2 = ccvald2;
			CCVALS1 = ccvals1;
			CCVALS2 = ccvals2;

			ClearWCS(header);
			this.CopyTo(header, verbose);
		}

		/// <summary>Gets the image [x, y] pixel position for a given world coordinate in degrees at cval1 and cval2.</summary>
		/// <param name="cval1">A coordinate values in degrees on coordinate axis 1 (i.e. right ascension).</param>
		/// <param name="cval2">A coordinate values in degrees on coordinate axis 2 (i.e. declination).</param>
		/// <param name="WCS_Type">The type of WCS solution: "TAN" for tangent-plane or Gnomic projection. Only "TAN" supported at this time.</param>
		/// <param name="X_pix">The x-pixel position of the sky coordinate.</param>
		/// <param name="Y_pix">The y-pixel position of the sky coordinate.</param>
		/// <param name="return_zero_based_pixels">If the pixels for the image should be interpreted as zero-based, pass true.</param>
		public void Get_Pixel(double cval1, double cval2, string WCS_Type, out double X_pix, out double Y_pix, bool return_zero_based_pixels)
		{
			double a0 = CRVALN[0] * Math.PI / 180, d0 = CRVALN[1] * Math.PI / 180;
			double a = cval1 * Math.PI / 180, d = cval2 * Math.PI / 180;//radians
			double X_intrmdt = Math.Cos(d) * Math.Sin(a - a0) / (Math.Cos(d0) * Math.Cos(d) * Math.Cos(a - a0) + Math.Sin(d0) * Math.Sin(d));
			double Y_intrmdt = (Math.Cos(d0) * Math.Sin(d) - Math.Cos(d) * Math.Sin(d0) * Math.Cos(a - a0)) / (Math.Sin(d0) * Math.Sin(d) + Math.Cos(d0) * Math.Cos(d) * Math.Cos(a - a0));
			X_pix = CDMATRIXINV[0, 0] * X_intrmdt + CDMATRIXINV[1, 0] * Y_intrmdt + CRPIXN[0];
			Y_pix = CDMATRIXINV[0, 1] * X_intrmdt + CDMATRIXINV[1, 1] * Y_intrmdt + CRPIXN[1];
			if (return_zero_based_pixels)
			{
				X_pix--;
				Y_pix--;
			}
		}

		/// <summary>Gets arrays of image [x, y] pixel positions for a list of given world coordinates in degrees at cval1 and cval2.</summary>
		/// <param name="cval1">An array of coordinate values in degrees on coordinate axis 1.</param>
		/// <param name="cval2">An array of coordinate values in degrees on coordinate axis 2.</param>
		/// <param name="WCS_Type">The type of WCS solution: "TAN" for tangent-plane or Gnomic projection. Only "TAN" supported at this time.</param>
		/// <param name="X_pix">An array of the image x-axis pixel locations.</param>
		/// <param name="Y_pix">An array of the image y-axis pixel locations.</param>
		/// <param name="return_zero_based_pixels">If the pixels for the image should be interpreted as zero-based, pass true.</param>
		public void Get_Pixels(double[] cval1, double[] cval2, string WCS_Type, out double[] X_pix, out double[] Y_pix, bool return_zero_based_pixels)
		{
			X_pix = new double[cval1.Length];
			Y_pix = new double[cval1.Length];

			for (int i = 0; i < cval1.Length; i++)
			{
				this.Get_Pixel(cval1[i], cval2[i], WCS_Type, out double xpix, out double ypix, return_zero_based_pixels);
				X_pix[i] = xpix;
				Y_pix[i] = ypix;
			}
		}

		/// <summary>Gets the cval1 and cval2 world coordinate in degrees for a given image [x, y] pixel position.</summary>
		/// <param name="X_pix">The x-pixel position of the sky coordinates.</param>
		/// <param name="Y_pix">The y-pixel position of the sky coordinates.</param>
		/// <param name="zero_based_pixels">True if the pixels coordinates for the image are zero-based.</param>
		/// <param name="WCS_Type">The type of WCS solution: "TAN" for tangent-plane or Gnomic projection. Only "TAN" supported at this time.</param>
		/// <param name="cval1">A coordinate value in degrees on coordinats axis 1 (i.e. right ascension).</param>
		/// <param name="cval2">A coordinate value in degrees on coordinats axis 2 (i.e. declination).</param>
		public void Get_Coordinate(double X_pix, double Y_pix, bool zero_based_pixels, string WCS_Type, out double cval1, out double cval2)
		{
			Get_Coordinate(X_pix, Y_pix, zero_based_pixels, WCS_Type, out cval1, out cval2, out string sx1, out string sx2);
		}

		/// <summary>Gets the cval1 and cval2 world coordinate in sexagesimal for a given image [x, y] pixel position.</summary>
		public void Get_Coordinate(double X_pix, double Y_pix, bool zero_based_pixels, string WCS_Type, out string cval1_sxgsml, out string cval2_sxgsml)
		{
			Get_Coordinate(X_pix, Y_pix, zero_based_pixels, WCS_Type, out double cv1, out double cv2, out cval1_sxgsml, out cval2_sxgsml);
		}

		/// <summary>Gets the cval1 and cval2 world coordinate in degrees and sexagesimal for a given image [x, y] pixel position.</summary>
		public void Get_Coordinate(double X_pix, double Y_pix, bool zero_based_pixels, string WCS_Type, out double cval1, out double cval2, out string cval1_sxgsml, out string cval2_sxgsml)
		{
			if (zero_based_pixels)
			{
				X_pix++;
				Y_pix++;
			}
			double X_intrmdt = CDMATRIX[0, 0] * (X_pix - CRPIXN[0]) * Math.PI / 180 + CDMATRIX[1, 0] * (Y_pix - CRPIXN[1]) * Math.PI / 180;
			double Y_intrmdt = CDMATRIX[0, 1] * (X_pix - CRPIXN[0]) * Math.PI / 180 + CDMATRIX[1, 1] * (Y_pix - CRPIXN[1]) * Math.PI / 180;
			double a = CRVALN[0] * Math.PI / 180 + Math.Atan(X_intrmdt / (Math.Cos(CRVALN[1] * Math.PI / 180) - Y_intrmdt * Math.Sin(CRVALN[1] * Math.PI / 180)));
			double d = Math.Asin((Math.Sin(CRVALN[1] * Math.PI / 180) + Y_intrmdt * Math.Cos(CRVALN[1] * Math.PI / 180)) / Math.Sqrt(1 + X_intrmdt * X_intrmdt + Y_intrmdt * Y_intrmdt));
			a = a * 180 / Math.PI;
			d = d * 180 / Math.PI;

			if (a < 0)
				a += 360;

			cval1 = a;
			cval2 = d;

			double h = Math.Floor(a / 360 * 24);
			double m = Math.Floor((a / 360 * 24 - h) * 60);
			double s = Math.Round((a / 360 * 24 - h - m / 60) * 3600, 2);

			double decdeg = Math.Abs(d);
			double deg = Math.Floor(decdeg);
			double am = Math.Floor((decdeg - deg) * 60);
			double ars = Math.Round((decdeg - deg - am / 60) * 3600, 2);

			String sign = "+";
			if (d < 0)
				sign = "-";

			cval1_sxgsml = h.ToString("00") + ":" + m.ToString("00") + ":" + s.ToString("00.00");
			cval2_sxgsml = sign + deg.ToString("00") + ":" + am.ToString("00") + ":" + ars.ToString("00.00");
		}

		/// <summary>Gets arrays of cval1 and cval2 world coordinates in degrees for a list of given image [x, y] pixel positions.</summary>
		public void Get_Coordinates(double[] X_pix, double[] Y_pix, bool zero_based_pixels, string WCS_Type, out double[] cval1, out double[] cval2)
		{
			string[] sx1 = new string[(X_pix.Length)];
			string[] sx2 = new string[(X_pix.Length)];

			Get_Coordinates(X_pix, Y_pix, zero_based_pixels, WCS_Type, out cval1, out cval2, out sx1, out sx2);
		}

		/// <summary>Gets arrays of cval1 and cval2 world coordinates in sexagesimal for a list of given image [x, y] pixel positions.</summary>
		public void Get_Coordinates(double[] X_pix, double[] Y_pix, bool zero_based_pixels, string WCS_Type, out string[] cval1_sxgsml, out string[] cval2_sxgsml)
		{
			double[] cv1 = new double[(X_pix.Length)];
			double[] cv2 = new double[(X_pix.Length)];

			Get_Coordinates(X_pix, Y_pix, zero_based_pixels, WCS_Type, out cv1, out cv2, out cval1_sxgsml, out cval2_sxgsml);
		}

		/// <summary>Gets arrays of cval1 and cval2 world coordinates in degrees and sexagesimal for a list of given image [x, y] pixel positions.</summary>
		public void Get_Coordinates(double[] X_pix, double[] Y_pix, bool zero_based_pixels, string WCS_Type, out double[] cval1, out double[] cval2, out string[] cval1_sxgsml, out string[] cval2_sxgsml)
		{
			cval1 = new double[X_pix.Length];
			cval2 = new double[X_pix.Length];
			cval1_sxgsml = new string[X_pix.Length];
			cval2_sxgsml = new string[X_pix.Length];

			for (int i = 0; i < cval1.Length; i++)
			{
				this.Get_Coordinate(X_pix[i], Y_pix[i], zero_based_pixels, WCS_Type, out double radeg, out double decdeg, out string rasx, out string decsx);
				cval1[i] = radeg;
				cval2[i] = decdeg;
				cval1_sxgsml[i] = rasx;
				cval2_sxgsml[i] = decsx;
			}
		}

		/// <summary>Copy WCS parameters from another WCS solution into the current instance.</summary>
		public void CopyFrom(JPFITS.WorldCoordinateSolution wcs_source)
		{
			try
			{
				WCSEXISTS = true;

				this.CD_Matrix = wcs_source.CD_Matrix;

				this.CTYPEN = new string[2];
				this.CTYPEN[0] = wcs_source.GetCTYPEn(1);
				this.CTYPEN[1] = wcs_source.GetCTYPEn(2);

				this.CRPIXN = new double[2];
				this.CRPIXN[0] = wcs_source.GetCRPIXn(1);
				this.CRPIXN[1] = wcs_source.GetCRPIXn(2);

				this.CRVALN = new double[2];
				this.CRVALN[0] = wcs_source.GetCRVALn(1);
				this.CRVALN[1] = wcs_source.GetCRVALn(2);

				this.CDELTN = new double[2];
				this.CDELTN[0] = wcs_source.GetCDELTn(1);
				this.CDELTN[1] = wcs_source.GetCDELTn(2);

				this.CROTAN = new double[2];
				this.CROTAN[0] = wcs_source.GetCROTAn(1);
				this.CROTAN[1] = wcs_source.GetCROTAn(2);

				this.CPIX1 = wcs_source.GetCPIXPixels(1, false);
				this.CPIX2 = wcs_source.GetCPIXPixels(2, false);
				this.CVAL1 = wcs_source.GetCVALValues(1, false, false);
				this.CVAL2 = wcs_source.GetCVALValues(2, false, false);

				this.CCVALD1 = wcs_source.CCVALD1;
				this.CCVALD2 = wcs_source.CCVALD2;
				this.CCVALS1 = wcs_source.CCVALS1;
				this.CCVALS2 = wcs_source.CCVALS2;

				this.CPIX1RM = wcs_source.CPIX1RM;
				this.CPIX1RS = wcs_source.CPIX1RS;
				this.CVAL1RM = wcs_source.CVAL1RM;
				this.CVAL1RS = wcs_source.CVAL1RS;
				this.CPIX2RM = wcs_source.CPIX2RM;
				this.CPIX2RS = wcs_source.CPIX2RS;
				this.CVAL2RM = wcs_source.CVAL2RM;
				this.CVAL2RS = wcs_source.CVAL2RS;
				this.CPIXRM = wcs_source.CPIXRM;
				this.CPIXRS = wcs_source.CPIXRS;
				this.CVALRM = wcs_source.CVALRM;
				this.CVALRS = wcs_source.CVALRS;
			}
			catch (Exception e)
			{
				throw new Exception(e.Data + "	" + e.InnerException + "	" + e.Message + "	" + e.Source + "	" + e.StackTrace + "	" + e.TargetSite);
			}
		}

		/// <summary>Copy WCS parameters from the current instance into a FITSHeader.</summary>
		/// <param name="header">The JPFITSHeader to copy the WCS parameters into.</param>
		/// <param name="verbose">Copy all WCS diagnostic data into the header in addition to essential WCS keywords.</param>
		public void CopyTo(JPFITS.FITSHeader header, bool verbose = false)
		{
			try
			{
				header.SetKey("CTYPE1", CTYPEN[0], "WCS type of horizontal coordinate transformation", true, -1);
				header.SetKey("CTYPE2", CTYPEN[1], "WCS type of vertical coordinate transformation", true, -1);
				header.SetKey("CRPIX1", CRPIXN[0].ToString("F5"), "WCS coordinate reference pixel on axis 1", true, -1);
				header.SetKey("CRPIX2", CRPIXN[1].ToString("F5"), "WCS coordinate reference pixel on axis 2", true, -1);
				header.SetKey("CRVAL1", CRVALN[0].ToString("F8"), "WCS coordinate reference value on axis 1 (deg)", true, -1);
				header.SetKey("CRVAL2", CRVALN[1].ToString("F8"), "WCS coordinate reference value on axis 2 (deg)", true, -1);
				header.SetKey("CD1_1", CDMATRIX[0, 0].ToString("0.0#########e+00"), "WCS rotation and scaling matrix", true, -1);
				header.SetKey("CD1_2", CDMATRIX[1, 0].ToString("0.0#########e+00"), "WCS rotation and scaling matrix", true, -1);
				header.SetKey("CD2_1", CDMATRIX[0, 1].ToString("0.0#########e+00"), "WCS rotation and scaling matrix", true, -1);
				header.SetKey("CD2_2", CDMATRIX[1, 1].ToString("0.0#########e+00"), "WCS rotation and scaling matrix", true, -1);
				header.SetKey("CDELT1", CDELTN[0].ToString("F8"), "WCS plate scale on axis 1 (arcsec per pixel)", true, -1);
				header.SetKey("CDELT2", CDELTN[1].ToString("F8"), "WCS plate Scale on axis 2 (arcsec per pixel)", true, -1);
				header.SetKey("CROTA1", CROTAN[0].ToString("F8"), "WCS field rotation angle on axis 1 (degrees)", true, -1);
				header.SetKey("CROTA2", CROTAN[1].ToString("F8"), "WCS field rotation angle on axis 2 (degrees)", true, -1);
				header.SetKey("CCVALS1", CCVALS1, "WCS field center on axis 1 (sexagesimal h m s)", true, -1);
				header.SetKey("CCVALS2", CCVALS2, "WCS field center on axis 2 (sexagesimal d am as)", true, -1);
				header.SetKey("CVALRM", CVALRM.ToString("G"), "Mean of WCS residuals (arcsec)", true, -1);
				header.SetKey("CVALRS", CVALRS.ToString("G"), "Standard dev of WCS residuals (arcsec)", true, -1);

				if (!verbose)
					return;

				header.SetKey("CCVALD1", CCVALD1.ToString("F8"), "WCS field center on axis 1 (degrees)", true, -1);
				header.SetKey("CCVALD2", CCVALD2.ToString("F8"), "WCS field center on axis 2 (degrees)", true, -1);				
				header.SetKey("CPIX1RM", CPIX1RM.ToString("G"), "Mean of WCS residuals on axis 1 (pixels)", true, -1);
				header.SetKey("CPIX1RS", CPIX1RS.ToString("G"), "Standard dev of WCS residuals on axis 1 (pixels)", true, -1);
				header.SetKey("CVAL1RM", CVAL1RM.ToString("G"), "Mean of WCS residuals on axis 1 (arcsec)", true, -1);
				header.SetKey("CVAL1RS", CVAL1RS.ToString("G"), "Standard dev of WCS residuals on axis 1 (arcsec)", true, -1);
				header.SetKey("CPIX2RM", CPIX2RM.ToString("G"), "Mean of WCS residuals on axis 2 (pixels)", true, -1);
				header.SetKey("CPIX2RS", CPIX2RS.ToString("G"), "Standard dev of WCS residuals on axis 2 (pixels)", true, -1);
				header.SetKey("CVAL2RM", CVAL2RM.ToString("G"), "Mean of WCS residuals on axis 2 (arcsec)", true, -1);
				header.SetKey("CVAL2RS", CVAL2RS.ToString("G"), "Standard dev of WCS residuals on axis 2 (arcsec)", true, -1);
				header.SetKey("CPIXRM", CPIXRM.ToString("G"), "Mean of WCS residuals (pixels)", true, -1);
				header.SetKey("CPIXRS", CPIXRS.ToString("G"), "Standard dev of WCS residuals (pixels)", true, -1);				

				int key = 0, num = 1;
				while (key != -1)
				{
					key = header.GetKeyIndex("WCD1_" + num.ToString("000"), false);
					header.RemoveKey(key);
					key = header.GetKeyIndex("WCD2_" + num.ToString("000"), false);
					header.RemoveKey(key);
					key = header.GetKeyIndex("WCP1_" + num.ToString("000"), false);
					header.RemoveKey(key);
					key = header.GetKeyIndex("WCP2_" + num.ToString("000"), false);
					header.RemoveKey(key);
					key = header.GetKeyIndex("WCV1_" + num.ToString("000"), false);
					header.RemoveKey(key);
					key = header.GetKeyIndex("WCV2_" + num.ToString("000"), false);
					header.RemoveKey(key);
					num++;
				}

				num = 1;
				for (int i = 0; i < CPIX1.Length; i++)
				{
					header.SetKey("WCP1_" + num.ToString("000"), CPIX1[i].ToString("F5"), "PSE coordinate pixel on axis 1", true, -1);
					header.SetKey("WCP2_" + num.ToString("000"), CPIX2[i].ToString("F5"), "PSE coordinate pixel on axis 2", true, -1);
					header.SetKey("WCV1_" + num.ToString("000"), CVAL1[i].ToString("F8"), "CAT coordinate value on axis 1 (degrees)", true, -1);
					header.SetKey("WCV2_" + num.ToString("000"), CVAL2[i].ToString("F8"), "CAT coordinate value on axis 2 (degrees)", true, -1);
					header.SetKey("WCD1_" + num.ToString("000"), DVAL1[i].ToString("F8"), "WCS coordinate delta on axis 1 (arcsec)", true, -1);
					header.SetKey("WCD2_" + num.ToString("000"), DVAL2[i].ToString("F8"), "WCS coordinate delta on axis 2 (arcsec)", true, -1);
					num++;
				}
			}
			catch (Exception e)
			{
				throw new Exception(e.Data + "	" + e.InnerException + "	" + e.Message + "	" + e.Source + "	" + e.StackTrace + "	" + e.TargetSite);
			}
		}

		/// <summary>
		/// Clear all WCS parameters from this instance.
		/// </summary>
		public void ClearWCS()
		{
			CDMATRIX = new double[0, 0];
			CDMATRIXINV = new double[0, 0];

			CD1_1 = 0;
			CD1_2 = 0;
			CD2_1 = 0;
			CD2_2 = 0;

			CTYPEN = new string[0];
			CRPIXN = new double[0];
			CRVALN = new double[0];
			CDELTN = new double[0];
			CROTAN = new double[0];

			CPIX1RM = 0;
			CPIX1RS = 0;
			CVAL1RM = 0;
			CVAL1RS = 0;
			CPIX2RM = 0;
			CPIX2RS = 0;
			CVAL2RM = 0;
			CVAL2RS = 0;
			CPIXRM = 0;
			CPIXRS = 0;
			CVALRM = 0;
			CVALRS = 0;
			CCVALD1 = 0;
			CCVALD2 = 0;
			CCVALS1 = "";
			CCVALS2 = "";

			WCSEXISTS = false;
		}

		/// <summary>Checks if a WCS solution has been computed for this instance.</summary>
		public bool Exists()
		{
			return WCSEXISTS;
		}

		/// <summary>
		/// Draws a WCS grid onto a PictureBox control.
		/// </summary>
		/// <param name="pictureBox">The picture box.</param>
		/// <param name="e">The PaintEventArgs from the PictureBox's Paint callback.</param>
		public void Grid_DrawWCSGrid(System.Windows.Forms.PictureBox pictureBox, PaintEventArgs e)
		{
			if (!WCSEXISTS)
				return;

			System.Drawing.Drawing2D.LinearGradientBrush lgbr;
			Pen RApen;
			for (int i = 0; i < this.Grid_RightAscensionPoints.Length; i++)
			{
				float xbb = this.Grid_RightAscensionPoints[i][0].X;
				float xtt = this.Grid_RightAscensionPoints[i][this.Grid_RightAscensionPoints[i].Length - 1].X;
				float ybb = this.Grid_RightAscensionPoints[i][0].Y;
				float ytt = this.Grid_RightAscensionPoints[i][this.Grid_RightAscensionPoints[i].Length - 1].Y;

				lgbr = new System.Drawing.Drawing2D.LinearGradientBrush(this.Grid_RightAscensionPoints[i][0], this.Grid_RightAscensionPoints[i][this.Grid_RightAscensionPoints[i].Length - 1], Color.Red, Color.Blue);
				RApen = new System.Drawing.Pen(lgbr);

				e.Graphics.DrawCurve(RApen, this.Grid_RightAscensionPoints[i]);
			}

			Pen IMAGEWINDOWPEN = new Pen(Color.FromArgb(175, Color.Green));
			for (int i = 0; i < this.Grid_DeclinationPoints.Length; i++)
				e.Graphics.DrawCurve(IMAGEWINDOWPEN, this.Grid_DeclinationPoints[i]);

			Brush IMAGEWINDOWBRUSH = new SolidBrush(Color.FromArgb(175, Color.Red));
			for (int i = 0; i < this.Grid_RightAscensionLabels.Length; i++)
			{
				PointF pnt = this.Grid_RightAscensionLabelLocations[i];
				float angle = -(float)(this.GetCROTAn(1) - 90);
				if (this.GetCROTAn(1) < 0)
					angle += 180;
				DrawRotatedTextAt(e.Graphics, angle, this.Grid_RightAscensionLabels[i], pnt, new Font("Microsoft Sans Serif", 14.0f, FontStyle.Bold), IMAGEWINDOWBRUSH);
			}

			IMAGEWINDOWBRUSH = new SolidBrush(Color.FromArgb(175, Color.Green));
			for (int i = 0; i < this.Grid_DeclinationLabels.Length; i++)
			{
				PointF pnt = this.Grid_DeclinationLabelLocations[i];
				float angle = -(float)(this.GetCROTAn(1));
				if (this.GetCROTAn(1) < -90)
					angle += 180;
				DrawRotatedTextAt(e.Graphics, angle, this.Grid_DeclinationLabels[i], pnt, new Font("Microsoft Sans Serif", 14.0f, FontStyle.Bold), IMAGEWINDOWBRUSH);
			}
		}

		private void DrawRotatedTextAt(Graphics gr, float angle, string txt, PointF point, Font the_font, Brush the_brush)
		{
			// Save the graphics state.
			GraphicsState state = gr.Save();
			gr.ResetTransform();

			// Rotate.
			gr.RotateTransform(angle);

			// Translate to desired position. Be sure to append
			// the rotation so it occurs after the rotation.
			gr.TranslateTransform(point.X, point.Y, MatrixOrder.Append);

			// Draw the text at the origin.
			gr.DrawString(txt, the_font, the_brush, 0, 0);

			// Restore the graphics state.
			gr.Restore(state);
		}

		/// <summary>Generates arrays of PointF arrays which represent grid lines in Right Ascension and Declination.
		/// <br />The lines are accessed via Grid_RightAscensionPoints and Grid_DeclinationPoints.
		/// <br />The sexagesimal labels for each grid line are accessed via Grid_RightAscensionLabels and Grid_DeclinationLabels.</summary>
		/// <param name="wcsImage_width">Image width, number of pixels.</param>
		/// <param name="wcsImage_height">Image height, number of pixels.</param>
		/// <param name="displayWindow_width">The display window width in display pixels.</param>
		/// <param name="displayWindow_height">The display window height in display pixels.</param>
		/// <param name="NapproximateIntervals">Approximate number of intervals in the grid. Suggest 7. Minimum 3.</param>
		public void Grid_MakeWCSGrid(int wcsImage_width, int wcsImage_height, int displayWindow_width, int displayWindow_height, int NapproximateIntervals)
		{
			if (NapproximateIntervals < 3)
				throw new Exception("Minimum number of grid intervals is 3...you passed: " + NapproximateIntervals);

			if (VALIDWCSGRIDLINES)
				return;
			VALIDWCSGRIDLINES = true;

			try
			{
				float xscale = (float)displayWindow_width / (float)wcsImage_width;
				float yscale = (float)displayWindow_height / (float)wcsImage_height;
				string sexx, sexy;

				Get_Coordinate((double)wcsImage_width / 2, (double)wcsImage_height / 2, false, "TAN", out double fieldcenterRA, out double fieldcenterDEC);

				bool poleinsidepos = false;
				bool poleinsideneg = false;
				Get_Pixel(0, 90, "TAN", out double x, out double y, true);
				if (x > 0 && y > 0 && x < wcsImage_width && y < wcsImage_height)
					poleinsidepos = true;
				if (!poleinsidepos)
				{
					Get_Pixel(0, -90, "TAN", out x, out y, true);
					if (x > 0 && y > 0 && x < wcsImage_width && y < wcsImage_height)
						poleinsideneg = true;
				}

				//top left, top middle, top right, middle left, middle right, bottom left, bottom middle, bottom right
				double[] crosscornersX = new double[8] { 1, wcsImage_width / 2, wcsImage_width, 1, wcsImage_width, 1, wcsImage_width / 2, wcsImage_width };
				double[] crosscornersY = new double[8] { 1, 1, 1, wcsImage_height / 2, wcsImage_height / 2, wcsImage_height, wcsImage_height, wcsImage_height };
				Get_Coordinates(crosscornersX, crosscornersY, false, "TAN", out double[] crosscornersRA, out double[] crosscornersDE);

				double despan = 0;
				JPMath.MinMax(crosscornersDE, out double minDE, out double maxDE, false);
				if (!poleinsidepos && !poleinsideneg)
					despan = maxDE - minDE;
				else if (poleinsidepos)
					despan = 90 - minDE;
				else if (poleinsideneg)
					despan = 90 + maxDE;

				double raspan = 0;
				if (poleinsidepos || poleinsideneg)
					raspan = 360;
				else//calculate RA sweep
				{
					for (int i = 0; i < crosscornersRA.Length - 1; i++)
						for (int j = i + 1; j < crosscornersRA.Length; j++)
						{
							double localspan;

							if (crosscornersRA[i] > crosscornersRA[j])
								localspan = crosscornersRA[i] - crosscornersRA[j];
							else
								localspan = crosscornersRA[j] - crosscornersRA[i];

							if (localspan <= 180 && localspan > raspan)
								raspan = localspan;
							else if (localspan > 180)
							{
								localspan = 360 - localspan;
								if (localspan > raspan)
									raspan = localspan;
							}
						}
					raspan *= 1.05;//to get total field coverage due to feild center being offset usually...this should be enough
					if (raspan > 360)
						raspan = 360;
				}

				//what is the closest gradient to get Nintervals intervals on the dec axis
				double decintervaldeg = despan / NapproximateIntervals;
				double decintervalarm = despan / NapproximateIntervals * 60;
				double decintervalars = despan / NapproximateIntervals * 3600;

				bool decdeg = false, decarm = false, decars = false;

				if (decintervalars <= 30)
					decars = true;
				else if (decintervalarm <= 30)
					decarm = true;
				else
					decdeg = true;

				double decgrad = 0;
				if (decdeg)
					decgrad = ROUNDTOHUMANINTERVAL(decintervaldeg);
				else if (decarm)
					decgrad = ROUNDTOHUMANINTERVAL(decintervalarm) / 60;
				else if (decars)
					decgrad = ROUNDTOHUMANINTERVAL(decintervalars) / 3600;
				fieldcenterDEC = Math.Round(fieldcenterDEC / decgrad) * decgrad;

				int nDECintervalsHW = (int)Math.Ceiling(despan / 2 / decgrad);
				DEC_LINES = new PointF[nDECintervalsHW * 2 + 1][];
				DEC_LABELS = new string[DEC_LINES.Length];
				DEC_LABEL_LOCATIONS = new PointF[DEC_LINES.Length];
				int nDECsweeppointsHW = 15;
				double decsweepinterval = raspan / ((double)nDECsweeppointsHW * 2 + 1);

				for (int i = -nDECintervalsHW; i <= nDECintervalsHW; i++)
				{
					DEC_LINES[i + nDECintervalsHW] = new PointF[nDECsweeppointsHW * 2 + 1];
					double dec = fieldcenterDEC + (double)(i) * decgrad;
					if (dec > 90)
						dec = 90 - (dec - 90);
					else if (dec < -90)
						dec = -90 - (90 + dec);
					DegreeElementstoSexagesimalElements(0, dec, out sexx, out sexy, ":", 0);
					DEC_LABELS[i + nDECintervalsHW] = sexy;

					for (int j = -nDECsweeppointsHW; j <= nDECsweeppointsHW; j++)
					{
						Get_Pixel(fieldcenterRA + (double)j * decsweepinterval, fieldcenterDEC + (double)(i) * decgrad, "TAN", out x, out y, true);
						DEC_LINES[i + nDECintervalsHW][j + nDECsweeppointsHW] = new PointF((float)x, (float)y);
					}
				}

				//what is the closest gradient to get NRAintervals intervals on the RA axis
				double raintervalhrs = raspan / NapproximateIntervals / 15;
				double raintervalmin = raspan / NapproximateIntervals * 60 / 15;
				double raintervalsec = raspan / NapproximateIntervals * 3600 / 15;

				bool rahrs = false, ramin = false, rasec = false;

				if (raintervalsec <= 30)
					rasec = true;
				else if (raintervalmin <= 30)
					ramin = true;
				else
					rahrs = true;

				double ragrad = 0;
				if (rahrs)
					ragrad = ROUNDTOHUMANINTERVAL(raintervalhrs) * 15;
				else if (ramin)
					ragrad = ROUNDTOHUMANINTERVAL(raintervalmin) / 60 * 15;
				else if (rasec)
					ragrad = ROUNDTOHUMANINTERVAL(raintervalsec) / 3600 * 15;
				fieldcenterRA = Math.Round(fieldcenterRA / ragrad) * ragrad;

				int nRAintervalsHW = (int)Math.Ceiling(raspan / 2 / ragrad);
				RAS_LINES = new PointF[nRAintervalsHW * 2 + 1][];
				RAS_LABELS = new string[RAS_LINES.Length];
				RAS_LABEL_LOCATIONS = new PointF[RAS_LINES.Length];
				int nRAsweeppointsHW = 15;
				double rasweepinterval = despan / (double)nRAsweeppointsHW;

				for (int i = -nRAintervalsHW; i <= nRAintervalsHW; i++)
				{
					RAS_LINES[i + nRAintervalsHW] = new PointF[nRAsweeppointsHW * 2 + 1];
					double ra = fieldcenterRA + (double)(i) * ragrad;
					if (ra < 0)
						ra += 360;
					DegreeElementstoSexagesimalElements(ra, 0, out sexx, out sexy, ":", 0);
					RAS_LABELS[i + nRAintervalsHW] = sexx;

					for (int j = -nRAsweeppointsHW; j <= nRAsweeppointsHW; j++)
					{
						Get_Pixel(fieldcenterRA + (double)(i) * ragrad, fieldcenterDEC + (double)j * rasweepinterval, "TAN", out x, out y, true);
						RAS_LINES[i + nRAintervalsHW][j + nRAsweeppointsHW] = new PointF((float)x, (float)y);
					}
				}

				//adjust line points for scale
				for (int i = 0; i < RAS_LINES.Length; i++)
					for (int j = 0; j < RAS_LINES[i].Length; j++)
						RAS_LINES[i][j] = new PointF(RAS_LINES[i][j].X * xscale, RAS_LINES[i][j].Y * yscale);
				for (int i = 0; i < DEC_LINES.Length; i++)
					for (int j = 0; j < DEC_LINES[i].Length; j++)
						DEC_LINES[i][j] = new PointF(DEC_LINES[i][j].X * xscale, DEC_LINES[i][j].Y * yscale);

				int divfact = 2;
				if (poleinsidepos || poleinsideneg)
					divfact = 3;
				for (int i = 0; i < this.Grid_RightAscensionLabels.Length; i++)
				{
					int yy = this.Grid_RightAscensionPoints[i].Length / divfact;
					PointF pnt = new PointF(this.Grid_RightAscensionPoints[i][yy].X, this.Grid_RightAscensionPoints[i][yy].Y);
					RAS_LABEL_LOCATIONS[i] = pnt;
				}
				for (int i = 0; i < this.Grid_DeclinationLabels.Length; i++)
				{
					int yy = this.Grid_DeclinationPoints[i].Length / 2;
					PointF pnt = new PointF(this.Grid_DeclinationPoints[i][yy].X, this.Grid_DeclinationPoints[i][yy].Y);
					DEC_LABEL_LOCATIONS[i] = pnt;
				}
			}
			catch (Exception ee)
			{
				MessageBox.Show(ee.Data + "	" + ee.InnerException + "	" + ee.Message + "	" + ee.Source + "	" + ee.StackTrace + "	" + ee.TargetSite);
			}
		}

		private double ROUNDTOHUMANINTERVAL(double value)
		{
			if (value <= 1)
				return 1;
			else if (value > 1 && value <= 2)
			{
				if (value < 1.25)
					return 1;
				else
					return 2;
			}
			else if (value > 2 && value <= 5)
			{
				if (value < 3.5)
					return 2;
				else
					return 5;
			}
			else if (value > 5 && value <= 10)
			{
				if (value < 7)
					return 5;
				else
					return 10;
			}
			else 
				return 30;
		}

		//public void Plot_PlotWCSQuiver(System.Windows.Forms.PictureBox pictureBox, PaintEventArgs e, double residualScaleFactor, int imagepixels_width, int imagepixels_height, int windowpixels_width, int windowpixels_height)
		//{
		//	float xsc = (float)windowpixels_width / (float)imagepixels_width;
		//	float ysc = (float)windowpixels_height / (float)imagepixels_height;

		//	AdjustableArrowCap bigArrow = new AdjustableArrowCap(4, 4);
		//	Pen p = new Pen(Color.LimeGreen);
		//	p.CustomEndCap = bigArrow;

		//	for (int i = 0; i < CPIX1.Length; i++)
		//	{
		//		double xend = CPIX1[i] + DVAL1[i] * residualScaleFactor;
		//		double yend = CPIX2[i] + DVAL2[i] * residualScaleFactor;
		//		e.Graphics.DrawLine(p, ((float)(CPIX1[i] - 1) + 0.5f) * xsc, ((float)(CPIX2[i] - 1) + 0.5f) * ysc, ((float)(xend - 1) + 0.5f) * xsc, ((float)(yend - 1) + 0.5f) * ysc);
		//	}

		//	double[] cv1 = this.GetCVALValues(1, true, true);
		//	double[] cv2 = this.GetCVALValues(2, true, true);
		//	Rectangle[] MARKCOORDRECTS = new Rectangle[cv1.Length];
		//	for (int i = 0; i < MARKCOORDRECTS.Length; i++)
		//		MARKCOORDRECTS[i] = new Rectangle((int)(((float)(cv1[i]) + 1) * xsc - 6), (int)(((float)(cv2[i]) + 1) * ysc - 6), 13, 13);
		//	p = new Pen(Color.Violet);
		//	p.Width = 2;
		//	for (int i = 0; i < MARKCOORDRECTS.Length; i++)
		//		e.Graphics.DrawEllipse(p, MARKCOORDRECTS[i]);
		//
		////also plot the cpix points
		//}

		#endregion

		#region STATIC MEMBERS
		/// <summary>Checks if a WCS solution exists based on the existence of the CTYPE keywords in the primary header of the given FITS object.</summary>
		/// <param name="header">The header to scan for complete FITS standard WCS keywords.</param>
		/// <param name="wcs_CTYPEN">The WCS solution type CTYPE to check for. Typically both axes of an image utilize the same projection type.</param>
		public static bool Exists(FITSHeader header, WCSType wcs_CTYPEN)
		{
			//for (int i = 1; i <= 2; i++)
			//	if (!header.GetKeyValue("CTYPE" + i.ToString()).Contains(wcs_CTYPEN[i - 1]))
			//		return false;

			if (header.GetKeyIndex("CTYPE1", false) == -1)
				return false;

			return true;
		}

		/// <summary>
		/// Clears the WCS keywords from the header.
		/// </summary>
		public static void ClearWCS(JPFITS.FITSHeader header)
		{
			header.RemoveKey("CTYPE1");
			header.RemoveKey("CTYPE2");
			header.RemoveKey("CRPIX1");
			header.RemoveKey("CRPIX2");
			header.RemoveKey("CRVAL1");
			header.RemoveKey("CRVAL2");
			header.RemoveKey("CD1_1");
			header.RemoveKey("CD1_2");
			header.RemoveKey("CD2_1");
			header.RemoveKey("CD2_2");
			header.RemoveKey("CDELT1");
			header.RemoveKey("CDELT2");
			header.RemoveKey("CROTA1");
			header.RemoveKey("CROTA2");
			header.RemoveKey("CCVALD1");
			header.RemoveKey("CCVALD2");
			header.RemoveKey("CCVALS1");
			header.RemoveKey("CCVALS2");
			header.RemoveKey("CPIX1RM");
			header.RemoveKey("CPIX1RS");
			header.RemoveKey("CVAL1RM");
			header.RemoveKey("CVAL1RS");
			header.RemoveKey("CPIX2RM");
			header.RemoveKey("CPIX2RS");
			header.RemoveKey("CVAL2RM");
			header.RemoveKey("CVAL2RS");
			header.RemoveKey("CPIXRM");
			header.RemoveKey("CPIXRS");
			header.RemoveKey("CVALRM");
			header.RemoveKey("CVALRS");

			int key = 0, num = 1;
			while (key != -1)
			{
				key = header.GetKeyIndex("WCD1_" + num.ToString("000"), false);
				header.RemoveKey(key);
				key = header.GetKeyIndex("WCD2_" + num.ToString("000"), false);
				header.RemoveKey(key);
				key = header.GetKeyIndex("WCP1_" + num.ToString("000"), false);
				header.RemoveKey(key);
				key = header.GetKeyIndex("WCP2_" + num.ToString("000"), false);
				header.RemoveKey(key);
				key = header.GetKeyIndex("WCV1_" + num.ToString("000"), false);
				header.RemoveKey(key);
				key = header.GetKeyIndex("WCV2_" + num.ToString("000"), false);
				header.RemoveKey(key);
				num++;
			}
		}

		/// <summary>Convert sexagesimal coordinate elements to degree units, with possibly arbitrary scale delimitters.</summary>
		/// <param name="ra_sexa">The right ascension in sexagesimal format.</param>
		/// <param name="dec_sexa">The declination in sexagesimal format.</param>
		/// <param name="delimit">If the scale delimiter is known then pass it (fast), otherwise it will be arbitrarily determined at each scale separation by passing an empty string (slower).</param>
		/// <param name="ra_deg">Return parameter for right ascension in degrees.</param>
		/// <param name="dec_deg">Return parameter for declination in degrees.</param>
		public static void SexagesimalElementsToDegreeElements(string ra_sexa, string dec_sexa, string delimit, out double ra_deg, out double dec_deg)
		{
			double h, m, s, d, am, ars;

			ra_sexa = ra_sexa.Trim();
			dec_sexa = dec_sexa.Trim();

			if (delimit == "")
			{
				int startindex = 0;
				int lastindex = 1;//lastindex can start at 1 since RA must have at least a single digit

				String currchar;

				while (true)//raH
					if (JPMath.IsNumeric(ra_sexa.Substring(lastindex, 1)))
						lastindex++;
					else//it must be a delimiter, therefore the space between startindex and lastindex (not including last index) must be numeric
					{
						h = Convert.ToDouble(ra_sexa.Substring(startindex, lastindex - startindex));
						break;
					}

				lastindex++;//increase last index one step past where it would have been a single element delimitter
				startindex = lastindex;//the start index can start at the same element

				while (true)//raM
				{
					if (JPMath.IsNumeric(ra_sexa.Substring(lastindex, 1)))
						lastindex++;
					else//it must be a delimiter, therefore the space between startindex and lastindex (not including last index) must be numeric
					{
						m = Convert.ToDouble(ra_sexa.Substring(startindex, lastindex - startindex));
						break;
					}
				}

				lastindex++;//increase last index one step past where it would have been a single element delimitter
				startindex = lastindex;//the start index can start at the same element

				while (true)//raS
				{
					currchar = ra_sexa.Substring(lastindex, 1);
					if ((currchar == "." || JPMath.IsNumeric(currchar)) && lastindex < ra_sexa.Length - 1)
						lastindex++;
					else//it must be a delimiter
					{
						s = Convert.ToDouble(ra_sexa.Substring(startindex, lastindex - startindex));
						break;
					}
				}

				lastindex = 0;//these indices start at zero now
				startindex = lastindex;

				while (true)//decD
				{
					currchar = dec_sexa.Substring(lastindex, 1);
					if (currchar == "+" || currchar == "-" || JPMath.IsNumeric(currchar))
						lastindex++;
					else//it must be a delimiter
					{
						d = Convert.ToDouble(dec_sexa.Substring(startindex, lastindex - startindex));
						break;
					}
				}

				lastindex++;//increase last index one step past where it would have been a single element delimitter
				startindex = lastindex;//the start index can start at the same element

				while (true)//decAM
				{
					if (JPMath.IsNumeric(dec_sexa.Substring(lastindex, 1)))
						lastindex++;
					else//it must be a delimiter, therefore the space between startindex and lastindex (not including last index) must be numeric
					{
						am = Convert.ToDouble(dec_sexa.Substring(startindex, lastindex - startindex));
						break;
					}
				}

				lastindex++;//increase last index one step past where it would have been a single element delimitter
				startindex = lastindex;//the start index can start at the same element

				while (true)//decAS
				{
					currchar = dec_sexa.Substring(lastindex, 1);
					if ((currchar == "." || JPMath.IsNumeric(currchar)) && lastindex < dec_sexa.Length - 1)
						lastindex++;
					else//it must be a delimiter or end of line
					{
						ars = Convert.ToDouble(dec_sexa.Substring(startindex, lastindex - startindex));
						break;
					}
				}
			}
			else
			{
				int lastind = 0;
				int nextind = ra_sexa.IndexOf(delimit, lastind);
				h = Convert.ToDouble(ra_sexa.Substring(lastind, nextind - lastind));
				lastind = nextind + 1;
				nextind = ra_sexa.IndexOf(delimit, lastind);
				m = Convert.ToDouble(ra_sexa.Substring(lastind, nextind - lastind));
				lastind = nextind + 1;
				s = Convert.ToDouble(ra_sexa.Substring(lastind));

				lastind = 0;
				nextind = dec_sexa.IndexOf(delimit, lastind);
				d = Convert.ToDouble(dec_sexa.Substring(lastind, nextind - lastind));
				lastind = nextind + 1;
				nextind = dec_sexa.IndexOf(delimit, lastind);
				am = Convert.ToDouble(dec_sexa.Substring(lastind, nextind - lastind));
				lastind = nextind + 1;
				ars = Convert.ToDouble(dec_sexa.Substring(lastind));
			}

			ra_deg = h / 24 * 360 + m / 60 / 24 * 360 + s / 3600 / 24 * 360;
			dec_deg = Math.Abs(d) + am / 60 + ars / 3600;
			if (d < 0)
				dec_deg *= -1;
		}

		/// <summary>Convert sexigesimal coordinates found in the first two columns of a String line into degree coordinate units.</summary>
		/// <param name="line">A String line whose first two columns contain sexagesimal coordinates.</param>
		/// <param name="ra_deg">Return parameter for right ascension in degrees.</param>
		/// <param name="dec_deg">Return parameter for declination in degrees.</param>
		public static void SexagesimalLineToDegreeElements(string line, out double ra_deg, out double dec_deg)
		{
			line = line.Trim();//clean leading and trailing white space
			int startindex = 0;
			int lastindex = 1;//lastindex can start at 1 since RA must have at least a single digit
			double raH, raM, raS, decD, decAM, decAS;
			String currchar;

			while (true)//raH
				if (JPMath.IsNumeric(line.Substring(lastindex, 1)))
					lastindex++;
				else//it must be a delimiter, therefore the space between startindex and lastindex (not including last index) must be numeric
				{
					raH = Convert.ToDouble(line.Substring(startindex, lastindex - startindex));
					break;
				}

			lastindex++;//increase last index one step past where it would have been a single element delimitter
			startindex = lastindex;//the start index can start at the same element

			while (true)//raM
			{
				if (JPMath.IsNumeric(line.Substring(lastindex, 1)))
					lastindex++;
				else//it must be a delimiter, therefore the space between startindex and lastindex (not including last index) must be numeric
				{
					raM = Convert.ToDouble(line.Substring(startindex, lastindex - startindex));
					break;
				}
			}

			lastindex++;//increase last index one step past where it would have been a single element delimitter
			startindex = lastindex;//the start index can start at the same element

			while (true)//raS
			{
				currchar = line.Substring(lastindex, 1);
				if (currchar == "." || JPMath.IsNumeric(currchar))
					lastindex++;
				else//it must be a delimiter
				{
					raS = Convert.ToDouble(line.Substring(startindex, lastindex - startindex));
					break;
				}
			}

			//now repeat the previous for the second coordinate, usually dec, which may have a + or - sign
			line = line.Substring(lastindex + 1).Trim();//reset the line to start with dec
			lastindex = 0;//these indices start at zero now
			startindex = lastindex;

			while (true)//decD
			{
				currchar = line.Substring(lastindex, 1);
				if (currchar == "+" || currchar == "-" || JPMath.IsNumeric(currchar))
					lastindex++;
				else//it must be a delimiter
				{
					decD = Convert.ToDouble(line.Substring(startindex, lastindex - startindex));
					break;
				}
			}

			lastindex++;//increase last index one step past where it would have been a single element delimitter
			startindex = lastindex;//the start index can start at the same element

			while (true)//decAM
			{
				if (JPMath.IsNumeric(line.Substring(lastindex, 1)))
					lastindex++;
				else//it must be a delimiter, therefore the space between startindex and lastindex (not including last index) must be numeric
				{
					decAM = Convert.ToDouble(line.Substring(startindex, lastindex - startindex));
					break;
				}
			}

			lastindex++;//increase last index one step past where it would have been a single element delimitter
			startindex = lastindex;//the start index can start at the same element

			while (true)//decAS
			{
				currchar = line.Substring(lastindex, 1);
				if ((currchar == "." || JPMath.IsNumeric(currchar)) && lastindex < line.Length - 1)
					lastindex++;
				else//it must be a delimiter or end of line
				{
					decAS = Convert.ToDouble(line.Substring(startindex, lastindex - startindex));
					break;
				}
			}

			ra_deg = raH / 24 * 360 + raM / 60 / 24 * 360 + raS / 3600 / 24 * 360;
			dec_deg = Math.Abs(decD) + decAM / 60 + decAS / 3600;
			if (decD < 0)
				dec_deg *= -1;
		}

		/// <summary>Open a file whose first two columns are sexagesimal entries with any formatting, and optionally save the file as a new file with a SaveFileDialog and return the coordinate columns as arrays in degree units.</summary>
		/// <param name="file">The full path of the textual file to open, to scan the first two columns for sexagesimal entries.</param>
		/// <param name="saveDegreeFile">True to be given a SaveFileDialog to save a new file with just the two coordinate columns in degree units; False to not save a file.</param>
		/// <param name="raDeg">A declared array for the RA coordinates in degrees. The array will be initialized internally to the appropriate size. The user may not require this array, but it still must be supplied.</param>
		/// <param name="decDeg">A declared array for the Declination coordinates in degrees. The array will be initialized internally to the appropriate size. The user may not require this array, but it still must be supplied.</param>
		public static void SexagesimalFileToDegreeFile(string file, bool saveDegreeFile, out double[] raDeg, out double[] decDeg)
		{
			int Nlines = 0;

			StreamReader sr = new StreamReader(file);
			while (!sr.EndOfStream)
			{
				sr.ReadLine();
				Nlines++;
			}
			sr.Close();
			sr = new StreamReader(file);

			raDeg = new double[(Nlines)];
			decDeg = new double[(Nlines)];

			for (int j = 0; j < Nlines; j++)
			{
				SexagesimalLineToDegreeElements(sr.ReadLine(), out double ra, out double dec);
				raDeg[j] = ra;
				decDeg[j] = dec;
			}

			sr.Close();

			if (!saveDegreeFile)
				return;

			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "Tab Delimited Text|*.txt";
			sfd.Title = "Specify file to save sexagesimal coordinate pairs coverted to degrees...";
			sfd.InitialDirectory = file.Substring(0, file.LastIndexOf("\\"));
			if (sfd.ShowDialog() == DialogResult.Cancel)
				return;

			String delimit = "\t";

			StreamWriter sw = new StreamWriter(sfd.FileName);
			for (int i = 0; i < Nlines; i++)
				sw.WriteLine(raDeg[i] + delimit + decDeg[i]);
			sw.Close();
		}

		/// <summary>Convert degree coordinate elements to sexagesimal format.</summary>
		/// <param name="ra_deg">Right ascension in degrees.</param>
		/// <param name="dec_deg">Declination in degrees.</param>
		/// <param name="ra_sexa">The right ascension in sexagesimal format, returned as a String.</param>
		/// <param name="dec_sexa">The declination in sexagesimal format, returned as a String.</param>
		/// <param name="delimitter">The scale delimiter; if an empty String is passed then a colon will be used.</param>
		/// <param name="decimals">The number of decimal places for the smallest scale element. Pass -1 for all decimals.</param>
		public static void DegreeElementstoSexagesimalElements(double ra_deg, double dec_deg, out string ra_sexa, out string dec_sexa, string delimitter, int decimals)
		{
			if (delimitter == "")
				delimitter = ":";

			double h = Math.Floor(ra_deg / 360 * 24);
			double m = Math.Floor((ra_deg / 360 * 24 - h) * 60);
			double s;
			if (decimals == -1)
				s = (ra_deg / 360 * 24 - h - m / 60) * 3600;
			else
				s = Math.Round((ra_deg / 360 * 24 - h - m / 60) * 3600, decimals);
			
			if (s == 60)
			{
				s = 0;
				m += 1;
			}

			double decdeg = Math.Abs(dec_deg);
			double deg = Math.Floor(decdeg);
			double am = Math.Floor((decdeg - deg) * 60);
			double ars;
			if (decimals == -1)
				ars = (decdeg - deg - am / 60) * 3600;
			else
				ars = Math.Round((decdeg - deg - am / 60) * 3600, decimals);

			if (ars == 60)
			{
				ars = 0;
				am += 1;
			}

			String sign = "+";
			if (dec_deg < 0)
				sign = "-";

			ra_sexa = h.ToString("00") + delimitter + m.ToString("00") + delimitter + s.ToString("00.############");
			dec_sexa = sign + deg.ToString("00") + delimitter + am.ToString("00") + delimitter + ars.ToString("00.############");
		}

		/// <summary>Convert degree coordinates found in the first two columns of a String line into sexigesimal coordinate format.</summary>
		/// <param name="line">A String line whose first two columns contain degree unit coordinates.</param>
		/// <param name="ra_sexa">The right ascension in sexagesimal format, returned as a String.</param>
		/// <param name="dec_sexa">The declination in sexagesimal format, returned as a String.</param>
		/// <param name="delimitter">The scale delimiter; if an empty String is passed then a colon will be used.</param>
		/// <param name="decimals">The number of decimal places for the smallest scale element. Pass -1 for all decimals.</param>
		public static void DegreeLineToSexagesimalElements(string line, out string ra_sexa, out string dec_sexa, string delimitter, int decimals)
		{
			line = line.Trim();//clean leading and trailing white space
			int startindex = 0;
			int lastindex = 1;//lastindex can start at 1 since RA must have at least a single digit
			double raD, decD;
			String currchar;

			while (true)//raD
			{
				currchar = line.Substring(lastindex, 1);
				if (currchar == "." || JPMath.IsNumeric(line.Substring(lastindex, 1)))
					lastindex++;
				else//it must be a delimiter, therefore the space between startindex and lastindex (not including last index) must be numeric
				{
					raD = Convert.ToDouble(line.Substring(startindex, lastindex - startindex));
					break;
				}
			}

			//now repeat the previous for the second coordinate, usually dec, which may have a + or - sign
			line = line.Substring(lastindex + 1).Trim();//reset the line to start with dec
			lastindex = 0;//these indices start at zero now
			startindex = lastindex;

			while (true)//decD
			{
				currchar = line.Substring(lastindex, 1);
				if ((currchar == "+" || currchar == "-" || currchar == "." || JPMath.IsNumeric(line.Substring(lastindex, 1))) && lastindex < line.Length - 1)
					lastindex++;
				else//it must be a delimiter, therefore the space between startindex and lastindex (not including last index) must be numeric
				{
					decD = Convert.ToDouble(line.Substring(startindex, lastindex - startindex));
					break;
				}
			}

			DegreeElementstoSexagesimalElements(raD, decD, out ra_sexa, out dec_sexa, delimitter, decimals);
		}

		/// <summary>Open a file whose first two columns are degree entries, and optionally save the file as a new file with a SaveFileDialog and return the coordinate columns as arrays in sexigesimal coordinate format.</summary>
		/// <param name="file">The full path of the textual file to open, to scan the first two columns for degree unit entries.</param>
		/// <param name="saveSexigesimalFile">True to be given a SaveFileDialog to save a new file with just the two coordinate columns; False to not save a file.</param>
		/// <param name="raSexagesimal">An array for the RA coordinates in sexigesimal format. The array will be initialized internally to the appropriate size. The user may not require this array, but it still must be supplied.</param>
		/// <param name="decSexagesimal">An array for the Declination coordinates in sexigesimal format. The array will be initialized internally to the appropriate size. The user may not require this array, but it still must be supplied.</param>
		/// <param name="delimitter">The scale delimitter; if an empty String is passed then a colon will be used.</param>
		/// <param name="decimals">The number of decimal places for the smallest scale element. Pass -1 for all decimals.</param>
		public static void DegreeFileToSexagesimalFile(string file, bool saveSexigesimalFile, out string[] raSexagesimal, out string[] decSexagesimal, string delimitter, int decimals)
		{
			int Nlines = 0;

			StreamReader sr = new StreamReader(file);
			while (!sr.EndOfStream)
			{
				sr.ReadLine();
				Nlines++;
			}
			sr.Close();
			sr = new StreamReader(file);

			raSexagesimal = new string[(Nlines)];
			decSexagesimal = new string[(Nlines)];

			for (int j = 0; j < Nlines; j++)
			{
				DegreeLineToSexagesimalElements(sr.ReadLine(), out string ra, out string dec, delimitter, decimals);
				raSexagesimal[j] = ra;
				decSexagesimal[j] = dec;
			}

			sr.Close();

			if (!saveSexigesimalFile)
				return;

			SaveFileDialog sfd = new SaveFileDialog();
			sfd.Filter = "Tab Delimited Text|*.txt";
			sfd.Title = "Specify file to save sexagesimal coordinate pairs coverted to degrees...";
			sfd.InitialDirectory = file.Substring(0, file.LastIndexOf("\\"));
			if (sfd.ShowDialog() == DialogResult.Cancel)
				return;

			String delimit = "\t"; ;

			StreamWriter sw = new StreamWriter(sfd.FileName);
			for (int i = 0; i < Nlines; i++)
				sw.WriteLine(raSexagesimal[i] + delimit + decSexagesimal[i]);
			sw.Close();
		}

		#endregion
	}
}

