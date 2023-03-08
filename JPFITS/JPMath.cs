using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
#nullable enable

namespace JPFITS
{
	public enum FitMinimizationType
	{
		/// <summary>
		/// 
		/// </summary>
		LeastSquares,

		/// <summary>
		/// 
		/// </summary>
		ChiSquared,

		/// <summary>
		/// 
		/// </summary>
		Robust,

		/// <summary>
		/// 
		/// </summary>
		CashStatistic
	}

	public enum PointSourceModel
	{
		/// <summary>
		/// G(x,y|p) = p(0) * exp(-((x - p(1))^2 + (y - p(2))^2) / (2*p(3)^2)) + p(4)
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = sigma; p[4] = bias 
		/// </summary>
		CircularGaussian,

		/// <summary>
		/// G(x,y|p) = p(0) * exp(-((x - p(1))*cosd(p(3)) + (y - p(2))*sind(p(3)))^2 / (2*p(4)^2) - (-(x - p(1))*sind(p(3)) + (y - p(2))*cosd(p(3))).^2 / (2*p(5)^2) ) + p(6)
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = phi; p[4] = x-sigma; p[5] = y-sigma; p[6] = bias. 
		/// </summary>
		EllipticalGaussian,

		/// <summary>
		/// M(x,y|p) = p(0) * ( 1 + { (x - p(1))^2 + (y - p(2))^2 } / p(3)^2 )^(-p(4)) + p(5)
		/// </summary>
		CircularMoffat,

		/// <summary>
		/// M(x,y|p) = p(0) * ( 1 + { ((x - p(1))*cosd(p(3)) + (y - p(2))*sind(p(3)))^2 } / p(4)^2 + { (-(x - p(1))*sind(p(3)) + (y - p(2))*cosd(p(3)))^2 } / p(5)^2 )^(-p(6)) + p(7)
		/// </summary>
		EllipticalMoffat
	}

	public enum PointSourceCompoundModel
	{
		/// <summary>
		/// G(x,y|p_n) = Sum[p_n(0) * exp(-((x - p_n(1))^2 + (y - p_n(2))^2) / (2*p_n(3)^2))] + p(4)
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = sigma; p[4] = bias 
		/// </summary>
		CircularGaussian,

		/// <summary>
		/// G(x,y|p_n) =  Sum[p_n(0) * exp(-((x - p_n(1))*cosd(p_n(3)) + (y - p_n(2))*sind(p_n(3)))^2 / (2*p_n(4)^2) - (-(x - p_n(1))*sind(p_n(3)) + (y - p_n(2))*cosd(p_n(3))).^2 / (2*p_n(5)^2) )] + p(6)
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = phi; p[4] = x-sigma; p[5] = y-sigma; p[6] = bias.  
		/// </summary>
		EllipticalGaussian,

		/// <summary>
		/// M(x,y|p_n) = sum[p_n(0) * ( 1 + { (x - p_n(1))^2 + (y - p_n(2))^2 } / p_n(3)^2 )^(-p_n(4))] + p(5)
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = theta; p[4] = beta; p[5] = bias
		/// </summary>
		CircularMoffat,

		/// <summary>
		/// M(x,y|p_n) = sum[p_n(0) * ( 1 + { ((x - p_n(1))*cosd(p_n(3)) + (y - p_n(2))*sind(p_n(3)))^2 } / p_n(4)^2 + { (-(x - p_n(1))*sind(p_n(3)) + (y - p_n(2))*cosd(p_n(3)))^2 } / p_n(5)^2 )^(-p_n(6))] + p_n(7)
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = phi; p[4] = x-theta; p[5] = y-theta; p[6] = beta; p[7] = bias
		/// </summary>
		EllipticalMoffat
	}

	public enum SmoothingMethod
	{
		/// <summary>
		/// simple moving average
		/// </summary>
		Simple,

		/// <summary>
		/// centered moving average
		/// </summary>
		Centered,

		/// <summary>
		/// linear regresion moving average
		/// </summary>
		Linear,

		/// <summary>
		/// exponential moving average
		/// </summary>
		Exponential
	}

	public enum InterpolationType
	{
		/// <summary>
		/// linear interpolation
		/// </summary>
		Linear,

		/// <summary>
		/// cubic spline interpolation
		/// </summary>
		Cublic,

		/// <summary>
		/// monotone cubic spline which preserves monoticity of the data
		/// </summary>
		Monotone,

		/// <summary>
		/// default Catmull-Rom spline
		/// </summary>
		CatmullRom,

		/// <summary>
		/// Akima is a cubic spline which is stable to the outliers, avoiding the oscillations of a cubic spline
		/// </summary>
		Akima
	}

	public class JPMath
	{
		public class PointD
		{
			private double POINTX, POINTY, POINTVAL, POINTXOLD, POINTYOLD;

			public PointD(double x, double y, double value)
			{
				POINTX = x;
				POINTY = y;
				POINTVAL = value;
			}

			public PointD(double x, double y, double value, double x_old, double y_old)
			{
				POINTX = x;
				POINTY = y;
				POINTVAL = value;
				POINTXOLD = x_old;
				POINTYOLD = y_old;
			}

			public double X
			{
				get { return POINTX; }
				set { POINTX = value; }
			}

			public double Y
			{
				get { return POINTY; }
				set { POINTY = value; }
			}

			public double Value
			{
				get { return POINTVAL; }
				set { POINTVAL = value; }
			}

			public double X_Old
			{
				get { return POINTXOLD; }
				set { POINTXOLD = value; }
			}

			public double Y_Old
			{
				get { return POINTYOLD; }
				set { POINTYOLD = value; }
			}

			/// <summary>Computes the distance from the current point to another point.</summary>
			[MethodImpl(256)]
			public double DistanceTo(PointD other_point)
			{
				double dx = POINTX - other_point.X, dy = POINTY - other_point.Y;
				return Math.Sqrt(dx * dx + dy * dy);
			}

			public static bool PointInPoly(PointD P, PointD[] V, int n)
			{
				int wn = 0;    // the  winding number counter

				// loop through all edges of the polygon
				for (int i = 0; i < n; i++)
				{   // edge from V[i] to  V[i+1]
					if (V[i].Y <= P.Y)
					{          // start y <= P.y
						if (V[i + 1].Y > P.Y)      // an upward crossing
							if (ISLEFT(V[i], V[i + 1], P) > 0)  // P left of  edge
								++wn;            // have  a valid up intersect
					}
					else
					{                        // start y > P.y (no test needed)
						if (V[i + 1].Y <= P.Y)     // a downward crossing
							if (ISLEFT(V[i], V[i + 1], P) < 0)  // P right of  edge
								--wn;            // have  a valid down intersect
					}
				}

				if (wn == 0)
					return false;
				return true;
			}

			[MethodImpl(256)]/*256 = agressive inlining*/
			public static double ISLEFT(PointD P0, PointD P1, PointD P2)
			{
				return ((P1.X - P0.X) * (P2.Y - P0.Y) - (P2.X - P0.X) * (P1.Y - P0.Y));
			}

			[MethodImpl(256)]/*256 = agressive inlining*/
			public static bool[,] PolygonInteriorPointsRegion(int regionXSize, int regionYSize, PointD[] polygon, int Xmin, int Ymin, int Xmax, int Ymax)
			{
				if (Xmin < 0)
					throw new Exception("Xmin is less than 0: " + Xmin);
				if (Xmax >= regionXSize)
					throw new Exception(String.Format("Xmax (0-based: {0}) is greater than the x-region size {1} ", Xmax, regionXSize));
				if (Ymin < 0)
					throw new Exception("Ymin is less than 0: " + Ymin);
				if (Ymax >= regionYSize)
					throw new Exception(String.Format("Ymax (0-based: {0}) is greater than the y-region size {1} ", Ymax, regionYSize));

				bool[,] region = new bool[regionXSize, regionYSize];
				Parallel.For(Xmin, Xmax + 1, x =>
				{
					PointD P;
					for (int y = Ymin; y <= Ymax; y++)
					{
						P = new PointD((double)(x), (double)(y), 0);

						int wn = 0;
						for (int i = 0; i < polygon.Length - 1; i++)
							if (polygon[i].Y <= P.Y)
							{
								if (polygon[i + 1].Y > P.Y)
									if (ISLEFT(polygon[i], polygon[i + 1], P) > 0)
										wn++;
							}
							else
								if (polygon[i + 1].Y <= P.Y)
									if (ISLEFT(polygon[i], polygon[i + 1], P) < 0)
										wn--;

						if (wn != 0)
							region[x, y] = true;
					}
				});
				return region;
			}			
		}

		public class Triangle
		{
			public Triangle(PointD point0, PointD point1, PointD point2)
			{
				POINTS = new PointD[3] { point0, point1, point2 };
				SORTTRIANGLE();
				MAKEVERTEXANGLES();
				MAKEFIELDVECTORS();
				TOTALVERTEXPOINTVALUESUM = POINTS[0].Value + POINTS[1].Value + POINTS[2].Value;
			}

			public Triangle(PointD[] points)
			{
				POINTS = new PointD[3] { points[0], points[1], points[2] };
				SORTTRIANGLE();
				MAKEVERTEXANGLES();
				MAKEFIELDVECTORS();
				TOTALVERTEXPOINTVALUESUM = POINTS[0].Value + POINTS[1].Value + POINTS[2].Value;
			}

			public PointD GetVertex(int i)
			{
				return POINTS[i];
			}

			public double GetVertexAngle(int i)
			{
				return VERTEXANGLES[i];
			}

			public PointD[] Points
			{
				get { return POINTS; }
				set { POINTS = value; }
			}

			public double GetSideLength(int i)
			{
				return SIDELENGTHS[i];
			}

			public PointD FieldVector
			{
				get { return FIELDVECTOR; }
			}

			public double FieldVectorRadAngle
			{
				get { return FIELDVECTORRADANGLE; }
			}

			public double VertexPointSum
			{
				get { return TOTALVERTEXPOINTVALUESUM; }
			}

			public double Area_Triangle(double[] x, double[] y)
			{
				double area = 0.5;
				area *= y[0] * (x[1] - x[2]) + y[1] * (x[2] - x[0]) + y[2] * (x[0] - x[1]);
				return Math.Abs(area);
			}

			private PointD[] POINTS;
			private double[] VERTEXANGLES = new double[0];
			private double[] SIDELENGTHS = new double[0];

			[MethodImpl(256)]/*256 = agressive inlining*/
			private void SORTTRIANGLE()
			{
				PointD[] points = new PointD[3] { POINTS[0], POINTS[1], POINTS[2] };
				double D01 = points[0].DistanceTo(points[1]);
				double D02 = points[0].DistanceTo(points[2]);
				double D12 = points[1].DistanceTo(points[2]);
				SIDELENGTHS = new double[3] { D01, D02, D12 };
				int[] distseq = new int[3] { 0, 1, 2 };
				Array.Sort(SIDELENGTHS, distseq);

				int common0 = -1;

				switch (distseq[0])
				{
					case 0:
						if (distseq[1] == 1)
							common0 = 0;
						else
							common0 = 1;
						break;
					case 1:
						if (distseq[1] == 0)
							common0 = 0;
						else
							common0 = 2;
						break;
					case 2:
						if (distseq[1] == 1)
							common0 = 2;
						else
							common0 = 1;
						break;
				}

				int common1 = -1, common2 = -1;

				switch (common0)
				{
					case 0:
						if (distseq[0] == 0)
						{
							common1 = 1;
							common2 = 2;
						}
						else
						{
							common1 = 2;
							common2 = 1;
						}
						break;
					case 1:
						if (distseq[0] == 0)
						{
							common1 = 0;
							common2 = 2;
						}
						else
						{
							common1 = 2;
							common2 = 0;
						}
						break;
					case 2:
						if (distseq[0] == 1)
						{
							common1 = 0;
							common2 = 1;
						}
						else
						{
							common1 = 1;
							common2 = 0;
						}
						break;
				}

				POINTS = new PointD[3] { points[common0], points[common1], points[common2] };
			}

			[MethodImpl(256)]/*256 = agressive inlining*/
			private void MAKEVERTEXANGLES()
			{
				VERTEXANGLES = new double[3];
				double D01 = SIDELENGTHS[0];// POINTS[0].DistanceTo(POINTS[1]);
				double D02 = SIDELENGTHS[1];// POINTS[0].DistanceTo(POINTS[2]);
				double D12 = SIDELENGTHS[2];// POINTS[1].DistanceTo(POINTS[2]);

				//c2 = a2 + b2 − 2ab cos(C)
				VERTEXANGLES[0] = Math.Acos(-(D12 * D12 - D01 * D01 - D02 * D02) / (2 * D01 * D02));
				VERTEXANGLES[1] = Math.Acos(-(D02 * D02 - D01 * D01 - D12 * D12) / (2 * D01 * D12));
				VERTEXANGLES[2] = Math.Acos(-(D01 * D01 - D02 * D02 - D12 * D12) / (2 * D02 * D12));
			}

			[MethodImpl(256)]/*256 = agressive inlining*/
			private void MAKEFIELDVECTORS()
			{
				FIELDVECTOR = new PointD(POINTS[2].X - POINTS[1].X, POINTS[2].Y - POINTS[1].Y, SIDELENGTHS[2]);
				FIELDVECTORRADANGLE = Math.Atan2(FIELDVECTOR.Y, FIELDVECTOR.X);
			}

			private PointD FIELDVECTOR = new PointD(0, 0, 0);
			private double FIELDVECTORRADANGLE;
			private double TOTALVERTEXPOINTVALUESUM;
		}

		#region FITTING

		/// <summary>Determines the non-linear least-squares fit parameters for a positively-oriented Gaussian curve G(x|p)
		/// <br />G(x|p) = p(0) * exp( -(x - p(1))^2 / (2*p(2)^2) ) + p(3)</summary>
		/// <param name="xdata">The x-data grid positions of the Gaussian data. If nullptr is passed a vector will automatically be created of appropriate size, centered on zero.</param>
		/// <param name="Gdata">The values of the data to be fitted to the Gaussian.</param>
		/// <param name="p">The initial and return parameters of the Gaussian. If p is only initialized and input with all zeros, initial estimates will automatically be computed.
		/// <br />p[0] = amplitude; <br />p[1] = x-center; <br />p[2] = sigma; <br />p[3] = bias.</param>
		/// <param name="p_lbnd">The lower-bound on the fit parameters. If nullptr is passed they will automatically be set by the Gdata dimensions with allowance.</param>
		/// <param name="p_ubnd">The upper-bound on the fit parameters. If nullptr is passed they will automatically be set by the Gdata dimensions with allowance.</param>
		/// <param name="p_err">The returned errors on the fitted parameters. Pass nullptr if not required.</param>
		/// <param name="fit_residuals">The returned residuals of the fit: Gdata[x] - fit[x].  Pass nullptr if not required.</param>
		public static void Fit_Gaussian1d(double[] xdata, double[] Gdata, ref double[] p, double[]? p_lbnd, double[]? p_ubnd, ref double[]? p_err, ref double[]? fit_residuals)
		{
			int xw = Gdata.Length;
			double xhw = (double)(xw - 1) / 2.0;

			if (xdata == null)
			{
				xdata = new double[xw];
				for (int i = 0; i < xw; i++)
					xdata[i] = (double)(i) - xhw;
			}

			double[,] x = new double[xw, 1];
			for (int i = 0; i < xw; i++)
				x[i, 0] = xdata[i];

			alglib.ndimensional_pfunc pf = new alglib.ndimensional_pfunc(alglib_Gauss_1d);
			alglib.ndimensional_pgrad pg = new alglib.ndimensional_pgrad(alglib_Gauss_1d_grad);
			//alglib.ndimensional_rep rep;
			object obj = null;
			//double diffstep = 0.0001;
			double epsx = 0.000001;
			int maxits = 0;
			double[] scale;

			double min = Min(Gdata, false);
			double max = Max(Gdata, false);
			double amp = max - min;
			if (amp == 0)
				amp = 1;
			double x0 = xdata[(int)xhw];
			if (x0 == 0)
				x0 = 1;
			double bias = min;
			if (bias == 0)
				bias = 1;

			scale = new double[4] { amp, x0, 2, bias };

			if (p[0] == 0 && p[1] == 0 && p[2] == 0 && p[3] == 0)
			{
				p[0] = amp;
				p[1] = x0;
				p[2] = 2;
				p[3] = bias;
			}

			if (p_lbnd == null)
				p_lbnd = new double[4] { 0, xdata[0], (double)xw / 50, 0 };
			if (p_ubnd == null)
				p_ubnd = new double[4] { 2 * amp, xdata[xw - 1], (double)xw, max };

			alglib.lsfitcreatefg(x, Gdata, p, false, out alglib.lsfitstate state);
			alglib.lsfitsetcond(state, epsx, maxits);
			alglib.lsfitsetscale(state, scale);
			alglib.lsfitsetbc(state, p_lbnd, p_ubnd);
			alglib.lsfitfit(state, pf, pg, null, null);
			alglib.lsfitresults(state, out int info, out p, out alglib.lsfitreport report);

			if (p_err != null)
				for (int i = 0; i < p_err.Length; i++)
					p_err[i] = report.errpar[i];

			if (fit_residuals != null)
			{
				double val = 0;
				double[] X = new double[1];
				for (int i = 0; i < xw; i++)
				{
					X[0] = xdata[i];
					alglib_Gauss_1d(p, X, ref val, obj);
					fit_residuals[i] = Gdata[i] - val;
				}
			}
		}

		/// <summary>Determines the non-linear least-squares fit parameters for a 1D Moffat curve M(x|p)
		/// <br />M(x|p) = p(0) * ( 1 + (x - p(1))^2 / p(2)^2 )^(-p(3)) + p(4)</summary>
		/// <param name="xdata">The x data grid positions of the Moffat data. If nullptr is passed a vector will automatically be created of appropriate size, centered on zero.</param>
		/// <param name="Mdata">The values of the data to be fitted to the Moffat.</param>
		/// <param name="p">The initial and return parameters of the Moffat. If p is only initialized and input with all zeros, initial estimates will automatically be computed.
		/// <br />p[0] = amplitude <br />p[1] = x-center <br />p[2] = theta <br />p[3] = beta <br />p[4] = bias</param>
		/// <param name="p_err">The errors on the fitted parameters. Pass nullptr if not required.</param>
		/// <param name="fit_residuals">The residuals of the fit: Mdata[x] - M(x|p). Pass nullptr if not required.</param>
		public static void Fit_Moffat1d(double[] xdata, double[] Mdata, ref double[] p, double[]? p_lbnd, double[]? p_ubnd, ref double[]? p_err, ref double[]? fit_residuals)
		{
			int xw = Mdata.Length;
			double xhw = (double)(xw - 1) / 2.0;

			if (xdata == null)
			{
				xdata = new double[xw];
				for (int i = 0; i < xdata.GetLength(0); i++)
					xdata[i] = (double)(i) - xhw;
			}

			double[,] x = new double[xw, 1];
			for (int i = 0; i < xw; i++)
				x[i, 0] = xdata[i];

			alglib.ndimensional_pfunc pf = new alglib.ndimensional_pfunc(alglib_Moffat_1d);
			alglib.ndimensional_pgrad pg = new alglib.ndimensional_pgrad(alglib_Moffat_1d_grad);
			//alglib.ndimensional_rep rep;
			object obj = null;
			//double diffstep = 0.0001;
			double epsx = 0.000001;
			int maxits = 0;
			double[] scale;

			double min = Min(Mdata, false);
			double max = Max(Mdata, false);
			double amp = max - min;
			if (amp == 0)
				amp = 1;
			double x0 = xdata[(int)xhw];
			if (x0 == 0)
				x0 = 1;
			double bias = min;
			if (bias == 0)
				bias = 1;

			scale = new double[5] { amp, x0, 2, 2, bias };

			if (p[0] == 0 && p[1] == 0 && p[2] == 0 && p[3] == 0 && p[4] == 0)
			{
				p[0] = amp;
				p[1] = x0;
				p[2] = 2;
				p[3] = 2;
				p[4] = bias;
			}

			if (p_lbnd == null)
				p_lbnd = new double[5] { 0, xdata[0], (double)xw / 500, (double)xw / 500, 0 };
			if (p_ubnd == null)
				p_ubnd = new double[5] { 2 * amp, xdata[xw - 1], (double)xw * 5, (double)xw * 5, max };

			alglib.lsfitcreatefg(x, Mdata, p, false, out alglib.lsfitstate state);
			alglib.lsfitsetcond(state, epsx, maxits);
			alglib.lsfitsetscale(state, scale);
			alglib.lsfitsetbc(state, p_lbnd, p_ubnd);
			alglib.lsfitfit(state, pf, pg, null, null);
			alglib.lsfitresults(state, out int info, out p, out alglib.lsfitreport report);

			if (p_err != null)
				for (int i = 0; i < p_err.Length; i++)
					p_err[i] = report.errpar[i];

			if (fit_residuals != null)
			{
				double val = 0;
				double[] X = new double[1];
				for (int i = 0; i < xw; i++)
				{
					X[0] = xdata[i];
					alglib_Moffat_1d(p, X, ref val, obj);
					fit_residuals[i] = Mdata[i] - val;
				}
			}
		}

		/// <summary>Determines the non-linear least-squares fit parameters for a field of n positively-oriented 2-d Gaussian surfaces G(x,y|p_n)
		/// <br />G(x,y|p_n) = Sum[p_n(0) * exp(-((x - p_n(1))^2 + (y - p_n(2))^2) / (2*p_n(3)^2))] + p(4)
		/// <br />or
		/// <br />G(x,y|p_n) =  Sum[p_n(0) * exp(-((x - p_n(1))*cosd(p_n(3)) + (y - p_n(2))*sind(p_n(3)))^2 / (2*p_n(4)^2) - (-(x - p_n(1))*sind(p_n(3)) + (y - p_n(2))*cosd(p_n(3))).^2 / (2*p_n(5)^2) )] + p(6)
		/// <br />The form of G(x,y|p_n) used is determined by the horizontal length of the parameter vector p</summary>
		/// <param name="xdata">The x-data grid positions of the Gaussian data.</param>
		/// <param name="ydata">The y-data grid positions of the Gaussian data.</param>
		/// <param name="Gdata">The values of the data to be fitted to the Gaussian.</param>
		/// <param name="p">The initial and return parameters of the Gaussian. If p is only initialized and input with all zeros, initial estimates will automatically be computed. Options are:
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = sigma; p[4] = bias
		/// <br />or
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = phi; p[4] = x-sigma; p[5] = y-sigma; p[6] = bias.</param>
		/// <param name="p_LB">The lower bound contraints on the fit parameters.</param>
		/// <param name="p_UB">The upper bound contraints on the fit parameters.</param>
		/// <param name="p_err">The return errors on the fitted parameters. Pass an array of length 0 if not required.</param>
		/// <param name="fit_residuals">The return residuals of the fit: Gdata[x, y] - fit[x, y].  Pass an array of length 0 if not required.</param>
		public static void Fit_Gaussian2d_Compound(int[] xdata, int[] ydata, double[,] Gdata, ref double[,] p, double[,] p_LB, double[,] p_UB, ref double[,] p_err, ref double[,] fit_residuals)
		{
			int N = Gdata.Length;
			double[,] x = new double[N, 2];
			double[] y = new double[N];
			int xw = xdata.Length;
			int yh = ydata.Length;
			int func = p.GetLength(0);
			int count = p.GetLength(1);

			int ii = 0;
			for (int xaxis = 0; xaxis < xw; xaxis++)
				for (int yaxis = 0; yaxis < yh; yaxis++)
				{
					x[ii, 0] = (double)xdata[xaxis];
					x[ii, 1] = (double)ydata[yaxis];
					y[ii] = Gdata[xaxis, yaxis];
					ii++;
				}

			alglib.ndimensional_pfunc pf = new alglib.ndimensional_pfunc(alglib_Gauss_2d_compound);
			alglib.ndimensional_pgrad pg = new alglib.ndimensional_pgrad(alglib_Gauss_2d_compound_grad);
			//alglib.ndimensional_rep rep;
			int[] fcobj = new int[(2)] { func, count };
			double epsx = 0;// 0.00001;
			int maxits = 0;// 5000;//automatic stopping conditions w epsx & maxits = 0
			double[] P = new double[p.Length - count + 1];//remove repeated background, but keep one
			double[] PLB = new double[p.Length - count + 1];//remove repeated background, but keep one
			double[] PUB = new double[p.Length - count + 1];//remove repeated background, but keep one
			double[] scale = new double[p.Length - count + 1];//remove repeated background, but keep one

			int c = 0;
			for (int i = 0; i < count; i++)
				for (int j = 0; j < func - 1; j++)
				{
					P[c] = p[j, i];
					PLB[c] = p_LB[j, i];
					PUB[c] = p_UB[j, i];
					scale[c] = P[c];
					if (scale[c] == 0)
						scale[c] = 1;
					c++;
				}
			P[P.Length - 1] = p[func - 1, count - 1];//background
			PLB[P.Length - 1] = p_LB[func - 1, count - 1];//background
			PUB[P.Length - 1] = p_UB[func - 1, count - 1];//background
			scale[P.Length - 1] = P[P.Length - 1];//background
			if (scale[P.Length - 1] == 0)
				scale[P.Length - 1] = 1;

			alglib.lsfitcreatefg(x, y, P, false, out alglib.lsfitstate state);
			alglib.lsfitsetcond(state, epsx, maxits);
			alglib.lsfitsetscale(state, scale);
			alglib.lsfitsetbc(state, PLB, PUB);
			alglib.lsfitfit(state, pf, pg, null, (object)fcobj);
			alglib.lsfitresults(state, out int info, out P, out alglib.lsfitreport report);

			for (int i = 0; i < count; i++)
				for (int j = 0; j < func - 1; j++)
					p[j, i] = P[j + i * (func - 1)];
			for (int i = 0; i < count; i++)
				p[func - 1, i] = P[P.Length - 1];//background

			if (p_err.Length != 0)
			{
				for (int i = 0; i < count; i++)
					for (int j = 0; j < func - 1; j++)
						p_err[j, i] = report.errpar[j + i * (func - 1)];
				for (int i = 0; i < count; i++)
					p_err[func - 1, i] = report.errpar[P.Length - 1];//background
			}

			if (fit_residuals.Length != 0)
			{
				double val = 0;
				double[] X = new double[2];
				for (int i = 0; i < xw; i++)
					for (int j = 0; j < yh; j++)
					{
						X[0] = (double)xdata[i];
						X[1] = (double)ydata[j];
						alglib_Gauss_2d_compound(P, X, ref val, fcobj);
						fit_residuals[i, j] = Gdata[i, j] - val;
					}
			}
		}

		/// <summary>Determines the non-linear least-squares fit parameters for a 2-d Moffat surface M(x,y|p)
		/// <br />M(x,y|p_n) = sum[p_n(0) * ( 1 + { (x - p_n(1))^2 + (y - p_n(2))^2 } / p_n(3)^2 )^(-p_n(4))] + p(5)
		/// <br />or
		/// <br />M(x,y|p_n) = sum[p_n(0) * ( 1 + { ((x - p_n(1))*cosd(p_n(3)) + (y - p_n(2))*sind(p_n(3)))^2 } / p_n(4)^2 + { (-(x - p_n(1))*sind(p_n(3)) + (y - p_n(2))*cosd(p_n(3)))^2 } / p_n(5)^2 )^(-p_n(6))] + p_n(7)
		/// <br />The form of M(x,y|p_n) used is determined by the length of the parameter vector p</summary>
		/// <param name="xdata">The x-data grid positions of the Moffat data.</param>
		/// <param name="ydata">The y-data grid positions of the Moffat data.</param>
		/// <param name="Mdata">The values of the data to be fitted to the Moffat.</param>
		/// <param name="p">The initial and return parameters of the Moffat. If p is only initialized and input with all zeros, initial estimates will automatically be computed. Options are:
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = theta; p[4] = beta; p[5] = bias
		/// <br />or
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = phi; p[4] = x-theta; p[5] = y-theta; p[6] = beta; p[7] = bias</param>
		/// <param name="p_LB">The lower bound contraints on the fit parameters.</param>
		/// <param name="p_UB">The upper bound contraints on the fit parameters.</param>
		/// <param name="p_err">The return errors on the fitted parameters. Pass an array of length 0 if not required.</param>
		/// <param name="fit_residuals">The return residuals of the fit: Mdata[x, y] - fit[x, y].  Pass an array of length 0 if not required.</param>
		public static void Fit_Moffat2d_Compound(int[] xdata, int[] ydata, double[,] Mdata, ref double[,] p, double[,] p_LB, double[,] p_UB, ref double[,] p_err, ref double[,] fit_residuals)
		{
			int N = Mdata.Length;
			double[,] x = new double[N, 2];
			double[] y = new double[N];
			int xw = xdata.Length;
			int yh = ydata.Length;
			int func = p.GetLength(0);
			int count = p.GetLength(1);

			int ii = 0;
			for (int xaxis = 0; xaxis < xw; xaxis++)
				for (int yaxis = 0; yaxis < yh; yaxis++)
				{
					x[ii, 0] = (double)xdata[xaxis];
					x[ii, 1] = (double)ydata[yaxis];
					y[ii] = Mdata[xaxis, yaxis];
					ii++;
				}

			alglib.ndimensional_pfunc pf = new alglib.ndimensional_pfunc(alglib_Moffat_2d_compound);
			alglib.ndimensional_pgrad pg = new alglib.ndimensional_pgrad(alglib_Moffat_2d_compound_grad);
			//alglib.ndimensional_rep rep;
			int[] fcobj = new int[(2)] { func, count };
			double epsx = 0;// 0.00001;
			int maxits = 0;// 5000;//automatic stopping conditions w epsx & maxits = 0
			double[] P = new double[p.Length - count + 1];//remove repeated background, but keep one
			double[] PLB = new double[p.Length - count + 1];//remove repeated background, but keep one
			double[] PUB = new double[p.Length - count + 1];//remove repeated background, but keep one
			double[] scale = new double[p.Length - count + 1];//remove repeated background, but keep one

			int c = 0;
			for (int i = 0; i < count; i++)
				for (int j = 0; j < func - 1; j++)
				{
					P[c] = p[j, i];
					PLB[c] = p_LB[j, i];
					PUB[c] = p_UB[j, i];
					scale[c] = P[c];
					if (scale[c] <= 0)
						scale[c] = 1;
					c++;
				}
			P[P.Length - 1] = p[func - 1, count - 1];//background
			PLB[P.Length - 1] = p_LB[func - 1, count - 1];//background
			PUB[P.Length - 1] = p_UB[func - 1, count - 1];//background
			scale[P.Length - 1] = P[P.Length - 1];//background
			if (scale[P.Length - 1] <= 0)
				scale[P.Length - 1] = 1;

			alglib.lsfitcreatefg(x, y, P, false, out alglib.lsfitstate state);
			alglib.lsfitsetcond(state, epsx, maxits);
			alglib.lsfitsetscale(state, scale);
			alglib.lsfitsetbc(state, PLB, PUB);
			alglib.lsfitfit(state, pf, pg, null, (object)fcobj);
			alglib.lsfitresults(state, out int info, out P, out alglib.lsfitreport report);

			for (int i = 0; i < count; i++)
				for (int j = 0; j < func - 1; j++)
					p[j, i] = P[j + i * (func - 1)];
			for (int i = 0; i < count; i++)
				p[func - 1, i] = P[P.Length - 1];//background

			if (p_err.Length != 0)
			{
				for (int i = 0; i < count; i++)
					for (int j = 0; j < func - 1; j++)
						p_err[j, i] = report.errpar[j + i * (func - 1)];
				for (int i = 0; i < count; i++)
					p_err[func - 1, i] = report.errpar[P.Length - 1];//background
			}

			if (fit_residuals.Length != 0)
			{
				double val = 0;
				double[] X = new double[2];
				for (int i = 0; i < xw; i++)
					for (int j = 0; j < yh; j++)
					{
						X[0] = (double)xdata[i];
						X[1] = (double)ydata[j];
						alglib_Moffat_2d_compound(P, X, ref val, fcobj);
						fit_residuals[i, j] = Mdata[i, j] - val;
					}
			}
		}		

		public static void Fit_PointSource(PointSourceModel model_name, FitMinimizationType minimization_type, int[]? xdata, int[]? ydata, double[,] source, ref double[]? params_INIT, double[]? params_LB, double[]? params_UB, out double[] p_err, out double[,] fit_residuals, out double chi_sq_norm, out string termination_msg)
		{
			if (IsEven(source.GetLength(0)) || IsEven(source.GetLength(1)))
				throw new Exception("double[,] source must be odd-size");
			if (source.GetLength(0) != source.GetLength(1))
				throw new Exception("double[,] source must be square");

			alglib.ndimensional_grad grad;
			double[] scale;
			double[] XDATA = new double[source.GetLength(0)];
			double[] YDATA = new double[source.GetLength(1)];

			if (xdata == null)
				for (int i = -source.GetLength(0)/2; i <= source.GetLength(0)/2; i++)
					XDATA[i] = (double)i;
			else
				for (int i = 0; i < XDATA.Length; i++)
					XDATA[i] = (double)xdata[i];

			if (ydata == null)
				for (int i = -source.GetLength(1) / 2; i <= source.GetLength(1) / 2; i++)
					YDATA[i] = (double)i;
			else
				for (int i = 0; i < YDATA.Length; i++)
					YDATA[i] = (double)ydata[i];

			//make scale parameters to use below for different parameter lengths
			double max = JPMath.Max(source, out int maxx, out int maxy, false);
			double min = JPMath.Min(source, false);
			double amp = max - min;
			if (amp == 0)
				amp = 1;
			double x0 = XDATA[maxx];
			if (x0 == 0)
				x0 = 1;
			double y0 = YDATA[maxy];
			if (y0 == 0)
				y0 = 1;
			double bias = min;
			if (bias < 1)
				bias = 1;

			if (model_name == PointSourceModel.CircularGaussian || model_name == PointSourceModel.EllipticalGaussian)
			{
				if (model_name == PointSourceModel.CircularGaussian)//G(x,y|p) = p(0) * exp(-((x - p(1))^2 + (y - p(2))^2) / (2*p(3)^2)) + p(4)
				{
					if (params_INIT == null)
						params_INIT = new double[5];
					if (params_INIT.Length != 5)
						throw new Exception(string.Format("Wrong number of parameters in param ({0}) for Circular Gaussian model_name.", params_INIT.Length));

					scale = new double[5] { amp, x0, y0, 2, bias };
					if (params_LB == null)
						params_LB = new double[5] { amp / 2, XDATA[0], YDATA[0], 0.2, min - amp / 3 };
					if (params_UB == null)
						params_UB = new double[5] { 2 * amp, XDATA[XDATA.Length - 1], YDATA[YDATA.Length - 1], 10, min + amp / 3 };
					if (params_INIT[0] == 0 && params_INIT[1] == 0 && params_INIT[2] == 0 && params_INIT[3] == 0 && params_INIT[4] == 0)
					{
						params_INIT[0] = amp;
						params_INIT[1] = x0;
						params_INIT[2] = y0;
						params_INIT[3] = 2;
						params_INIT[4] = bias;
					}
				}
				else if (model_name == PointSourceModel.EllipticalGaussian)//G(x,y|p) = p(0) * exp(-((x - p(1))*cosd(p(3)) + (y - p(2))*sind(p(3)))^2 / (2*p(4)^2) - (-(x - p(1))*sind(p(3)) + (y - p(2))*cosd(p(3))).^2 / (2*p(5)^2) ) + p(6)
				{
					if (params_INIT == null)
						params_INIT = new double[7];
					if (params_INIT.Length != 7)
						throw new Exception(string.Format("Wrong number of parameters in param ({0}) for Elliptical Gaussian model_name.", params_INIT.Length));

					scale = new double[7] { amp, x0, y0, 1, 2, 2, bias };
					if (params_LB == null)
						params_LB = new double[7] { amp / 2, XDATA[0], YDATA[0], -Math.PI, 0.2, 0.2, min - amp / 3 };
					if (params_UB == null)
						params_UB = new double[7] { 2 * amp, XDATA[XDATA.Length - 1], YDATA[YDATA.Length - 1], Math.PI, 10, 10, min + amp / 3 };
					if (params_INIT[0] == 0 && params_INIT[1] == 0 && params_INIT[2] == 0 && params_INIT[3] == 0 && params_INIT[4] == 0 && params_INIT[5] == 0 && params_INIT[6] == 0)
					{
						params_INIT[0] = amp;
						params_INIT[1] = x0;
						params_INIT[2] = y0;
						params_INIT[3] = 0;
						params_INIT[4] = 2;
						params_INIT[5] = 2;
						params_INIT[6] = bias;
					}
				}
				else
					throw new Exception();

				if (minimization_type == FitMinimizationType.LeastSquares)
					grad = new alglib.ndimensional_grad(alglib_Gauss_2d_LM_LS_grad);
				else if (minimization_type == FitMinimizationType.ChiSquared)
					grad = new alglib.ndimensional_grad(alglib_Gauss_2d_LM_LS_CHISQ_grad);
				else if (minimization_type == FitMinimizationType.Robust)
					grad = new alglib.ndimensional_grad(alglib_Gauss_2d_LM_LS_ROBUST_grad);
				else if (minimization_type == FitMinimizationType.CashStatistic)
					grad = new alglib.ndimensional_grad(alglib_Gauss_2d_LM_LS_CSTAT_grad);
				else
					throw new Exception("Fit Type not recognized: '" + minimization_type);
			}
			else if (model_name == PointSourceModel.CircularMoffat || model_name == PointSourceModel.EllipticalMoffat)
			{
				if (model_name == PointSourceModel.CircularMoffat)//M(x,y|p) = p(0) * ( 1 + { (x - p(1))^2 + (y - p(2))^2 } / p(3)^2 )^(-p(4)) + p(5)
				{
					if (params_INIT == null)
						params_INIT = new double[6];
					if (params_INIT.Length != 6)
						throw new Exception(string.Format("Wrong number of parameters in param ({0}) for Circular Moffat model_name.", params_INIT.Length));

					scale = new double[6] { amp, x0, y0, 2, 2, bias };
					if (params_LB == null)
						params_LB = new double[6] { amp / 2, XDATA[0], YDATA[0], 0.2, 1.000001, min - amp / 3 };
					if (params_UB == null || params_UB.Length == 0)
						params_UB = new double[6] { 2 * amp, XDATA[XDATA.Length - 1], YDATA[YDATA.Length - 1], 10, 10, min + amp / 3 };
					if (params_INIT[0] == 0 && params_INIT[1] == 0 && params_INIT[2] == 0 && params_INIT[3] == 0 && params_INIT[4] == 0 && params_INIT[5] == 0)
					{
						params_INIT[0] = amp;
						params_INIT[1] = x0;
						params_INIT[2] = y0;
						params_INIT[3] = 2;
						params_INIT[4] = 2;
						params_INIT[5] = bias;
					}
				}
				else if (model_name == PointSourceModel.EllipticalMoffat)//M(x,y|p) = p(0) * ( 1 + { ((x - p(1))*cosd(p(3)) + (y - p(2))*sind(p(3)))^2 } / p(4)^2 + { (-(x - p(1))*sind(p(3)) + (y - p(2))*cosd(p(3)))^2 } / p(5)^2 )^(-p(6)) + p(7)
				{
					if (params_INIT == null)
						params_INIT = new double[8];
					if (params_INIT.Length != 8)
						throw new Exception(string.Format("Wrong number of parameters in param ({0}) for Elliptical Moffat model_name.", params_INIT.Length));

					scale = new double[8] { amp, x0, y0, 1, 2, 2, 2, bias };
					if (params_LB == null)
						params_LB = new double[8] { amp / 2, XDATA[0], YDATA[0], -Math.PI, 0.2, 0.2, 1.000001, min - amp / 3 };
					if (params_UB == null)
						params_UB = new double[8] { 2 * amp, XDATA[XDATA.Length - 1], YDATA[YDATA.Length - 1], Math.PI, 10, 10, 10, min + amp / 3 };
					if (params_INIT[0] == 0 && params_INIT[1] == 0 && params_INIT[2] == 0 && params_INIT[3] == 0 && params_INIT[4] == 0 && params_INIT[5] == 0 && params_INIT[6] == 0 && params_INIT[7] == 0)
					{
						params_INIT[0] = amp;
						params_INIT[1] = x0;
						params_INIT[2] = y0;
						params_INIT[3] = 0;
						params_INIT[4] = 2;
						params_INIT[5] = 2;
						params_INIT[6] = 2;
						params_INIT[7] = bias;
					}
				}
				else
					throw new Exception();

				if (minimization_type == FitMinimizationType.LeastSquares)
					grad = new alglib.ndimensional_grad(alglib_Moffat_2d_LM_LS_grad);
				else if (minimization_type == FitMinimizationType.ChiSquared)
					grad = new alglib.ndimensional_grad(alglib_Moffat_2d_LM_LS_CHISQ_grad);
				else if (minimization_type == FitMinimizationType.Robust)
					grad = new alglib.ndimensional_grad(alglib_Moffat_2d_LM_LS_ROBUST_grad);
				else if (minimization_type == FitMinimizationType.CashStatistic)
					grad = new alglib.ndimensional_grad(alglib_Moffat_2d_LM_LS_CSTAT_grad);
				else
					throw new Exception("Fit Type not recognized: '" + minimization_type);
			}
			else
				throw new Exception("Fit Model not recognized: '" + model_name);

			double epsx = 1e-9;
			double epsg = 0;
			double epsf = 0;
			int maxits = 0;
			object[] arrays = new object[3];
			arrays[0] = source;
			arrays[1] = XDATA;
			arrays[2] = YDATA;

			alglib.minbccreate(params_INIT, out alglib.minbcstate bcstate);
			alglib.minbcsetcond(bcstate, epsg, epsf, epsx, maxits);
			alglib.minbcsetbc(bcstate, params_LB, params_UB);
			alglib.minbcsetscale(bcstate, scale);
			alglib.minbcoptimize(bcstate, grad, null, arrays);
			alglib.minbcresults(bcstate, out params_INIT, out alglib.minbcreport report);

			switch (report.terminationtype)
			{
				case -8:
				{
					termination_msg = "Internal integrity control detected infinite or NAN values in function or gradient. Abnormal termination signaled.";
					break;
				}
				case -3:
				{
					termination_msg = "Inconsistent constraints.";
					break;
				}
				case 1:
				{
					termination_msg = "Relative function improvement is no more than EpsF.";
					break;
				}
				case 2:
				{
					termination_msg = "Scaled step is no more than EpsX.";
					break;
				}
				case 4:
				{
					termination_msg = "Scaled gradient norm is no more than EpsG.";
					break;
				}
				case 5:
				{
					termination_msg = "MaxIts steps was taken.";
					break;
				}
				case 7:
				{
					termination_msg = "Stopping conditions are too stringent, further improvement is impossible, X contains best point found so far.";
					break;
				}
				case 8:
				{
					termination_msg = "Terminated by user.";
					break;
				}

				default:
				{
					termination_msg = "";
					break;
				}
			}

			if (model_name == PointSourceModel.EllipticalGaussian || model_name == PointSourceModel.CircularGaussian)
				p_err = Gauss_2D_param_err(params_INIT, XDATA, YDATA, source);
			else
				p_err = Moffat_2D_param_err(params_INIT, XDATA, YDATA, source);

			double[,] model;
			if (model_name == PointSourceModel.EllipticalGaussian || model_name == PointSourceModel.CircularGaussian)
				model = Gaussian2d(xdata, ydata, params_INIT, false);
			else
				model = Moffat2d(xdata, ydata, params_INIT, false);

			fit_residuals = new double[XDATA.Length, YDATA.Length];
			for (int i = 0; i < XDATA.Length; i++)
				for (int j = 0; j < YDATA.Length; j++)
					fit_residuals[i, j] = source[i, j] - model[i, j];

			chi_sq_norm = 0;
			for (int i = 0; i < source.GetLength(0); i++)
				for (int j = 0; j < source.GetLength(1); j++)
				{
					if (source[i, j] - params_INIT[params_INIT.Length - 1] == 0)
						chi_sq_norm += fit_residuals[i, j] * fit_residuals[i, j];
					else
						chi_sq_norm += fit_residuals[i, j] * fit_residuals[i, j] / Math.Abs(source[i, j] - params_INIT[params_INIT.Length - 1]);
				}
			chi_sq_norm /= (source.Length - params_INIT.Length);
		}
			

		public static void Fit_PointSource_Compound(PointSourceCompoundModel model_name, FitMinimizationType minimization_type, int[] xdata, int[] ydata, double[,] source, double[] xpositions, double[] ypositions, double position_radius, ref double[,] param, ref double[,] p_err, ref double[,] fit_residuals, out string termination_msg)
		{
			if (param.GetLength(1) != xpositions.Length)
				throw new Exception("Parameter array param not consistent with the number of source xpositions indicated.");

			int func = param.GetLength(0);
			int count = param.GetLength(1);
			alglib.ndimensional_grad grad = new alglib.ndimensional_grad(alglib_Gauss_2d_LM_LS_grad_compound);

			try
			{
				double[] XDATA = new double[xdata.Length];
				double[] YDATA = new double[ydata.Length];
				int[] fcobj = new int[(2)] { func, count };
				double[] P0I = new double[(param.Length - count + 1)];//remove repeated background, but keep one
				double[] PLB = new double[(param.Length - count + 1)];//remove repeated background, but keep one
				double[] PUB = new double[(param.Length - count + 1)];//remove repeated background, but keep one
				double[] scale = new double[(param.Length - count + 1)];//remove repeated background, but keep one

				for (int i = 0; i < XDATA.Length; i++)
					XDATA[i] = (double)(xdata[i]);
				for (int i = 0; i < YDATA.Length; i++)
					YDATA[i] = (double)(ydata[i]);

				JPMath.MinMax(source, out double min, out double max, false);
				double bias = min;
				if (bias < 1)
					bias = 1;
				P0I[P0I.Length - 1] = min;//background
				PLB[P0I.Length - 1] = min - (max - min) / 3;//background
				PUB[P0I.Length - 1] = min + (max - min) / 3;//background
				scale[P0I.Length - 1] = bias;//background

				int funcindex;
				if (model_name == PointSourceCompoundModel.CircularGaussian || model_name == PointSourceCompoundModel.EllipticalGaussian)
				{
					if (func == 5)
					{
						for (int i = 0; i < count; i++)
						{
							funcindex = i * (func - 1);

							//amplitude
							P0I[funcindex] = source[(int)(xpositions[i] - XDATA[0] + 0.5), (int)(ypositions[i] - YDATA[0] + 0.5)] - bias;
							PLB[funcindex] = P0I[funcindex] / 2;
							PUB[funcindex] = 2 * P0I[funcindex];
							scale[funcindex] = P0I[funcindex];

							//x0
							P0I[funcindex + 1] = xpositions[i];
							PLB[funcindex + 1] = xpositions[i] - position_radius;
							PUB[funcindex + 1] = xpositions[i] + position_radius;
							scale[funcindex + 1] = P0I[funcindex + 1];

							//y0
							P0I[funcindex + 2] = ypositions[i];
							PLB[funcindex + 2] = ypositions[i] - position_radius;
							PUB[funcindex + 2] = ypositions[i] + position_radius;
							scale[funcindex + 2] = P0I[funcindex + 2];

							//sigma
							P0I[funcindex + 3] = 2;
							PLB[funcindex + 3] = 2e-1;
							PUB[funcindex + 3] = 10;
							scale[funcindex + 3] = P0I[funcindex + 3];
						}
					}
					else if (func == 7)
					{
						for (int i = 0; i < count; i++)
						{
							funcindex = i * (func - 1);

							//amplitude
							P0I[funcindex] = source[(int)(xpositions[i] - XDATA[0]), (int)(ypositions[i] - YDATA[0])] - bias;
							PLB[funcindex] = P0I[funcindex] / 2;
							PUB[funcindex] = 2 * P0I[funcindex];
							scale[funcindex] = P0I[funcindex];

							//x0
							P0I[funcindex + 1] = xpositions[i];
							PLB[funcindex + 1] = xpositions[i] - position_radius;
							PUB[funcindex + 1] = xpositions[i] + position_radius;
							scale[funcindex + 1] = P0I[funcindex + 1];

							//y0
							P0I[funcindex + 2] = ypositions[i];
							PLB[funcindex + 2] = ypositions[i] - position_radius;
							PUB[funcindex + 2] = ypositions[i] + position_radius;
							scale[funcindex + 2] = P0I[funcindex + 2];

							//phi
							P0I[funcindex + 3] = 0;
							PLB[funcindex + 3] = -Math.PI;
							PUB[funcindex + 3] = Math.PI;
							scale[funcindex + 3] = 1;

							//sigmaX
							P0I[funcindex + 4] = 2;
							PLB[funcindex + 4] = 2e-1;
							PUB[funcindex + 4] = 10;
							scale[funcindex + 4] = P0I[funcindex + 4];

							//sigmaY
							P0I[funcindex + 5] = 2;
							PLB[funcindex + 5] = 2e-1;
							PUB[funcindex + 5] = 10;
							scale[funcindex + 5] = P0I[funcindex + 5];
						}
					}
					else
						throw new Exception("Parameter length does not correspond to either circular (5 params) or elliptical (7 params) Gaussian; params length = " + func);


					if (minimization_type == FitMinimizationType.LeastSquares)
						grad = new alglib.ndimensional_grad(alglib_Gauss_2d_LM_LS_grad_compound);
					else if (minimization_type == FitMinimizationType.ChiSquared)
						grad = new alglib.ndimensional_grad(alglib_Gauss_2d_LM_LS_CHISQ_grad_compound);
					else if (minimization_type == FitMinimizationType.Robust)
						grad = new alglib.ndimensional_grad(alglib_Gauss_2d_LM_LS_ROBUST_grad_compound);
					else if (minimization_type == FitMinimizationType.CashStatistic)
						grad = new alglib.ndimensional_grad(alglib_Gauss_2d_LM_LS_CSTAT_grad_compound);
					else
						throw new Exception("Fit Type not recognized: '" + minimization_type);
				}
				else if (model_name == PointSourceCompoundModel.CircularMoffat || model_name == PointSourceCompoundModel.EllipticalMoffat)
				{
					if (func == 6)
					{
						for (int i = 0; i < count; i++)
						{
							funcindex = i * (func - 1);

							//amplitude
							P0I[funcindex] = source[(int)(xpositions[i] - XDATA[0]), (int)(ypositions[i] - YDATA[0])] - bias;
							PLB[funcindex] = P0I[funcindex] / 2;
							PUB[funcindex] = 2 * P0I[funcindex];
							scale[funcindex] = P0I[funcindex];

							//x0
							P0I[funcindex + 1] = xpositions[i];
							PLB[funcindex + 1] = xpositions[i] - position_radius;
							PUB[funcindex + 1] = xpositions[i] + position_radius;
							scale[funcindex + 1] = P0I[funcindex + 1];

							//y0
							P0I[funcindex + 2] = ypositions[i];
							PLB[funcindex + 2] = ypositions[i] - position_radius;
							PUB[funcindex + 2] = ypositions[i] + position_radius;
							scale[funcindex + 2] = P0I[funcindex + 2];

							//alpha
							P0I[funcindex + 3] = 2;
							PLB[funcindex + 3] = 2e-1;
							PUB[funcindex + 3] = 10;
							scale[funcindex + 3] = P0I[funcindex + 3];

							//beta
							P0I[funcindex + 4] = 2;
							PLB[funcindex + 4] = 1 + 1e-6;
							PUB[funcindex + 4] = 10;
							scale[funcindex + 4] = P0I[funcindex + 4];
						}
					}
					else if (func == 8)
					{
						for (int i = 0; i < count; i++)
						{
							funcindex = i * (func - 1);

							//amplitude
							P0I[funcindex] = source[(int)(xpositions[i] - XDATA[0]), (int)(ypositions[i] - YDATA[0])] - bias;
							PLB[funcindex] = P0I[funcindex] / 2;
							PUB[funcindex] = 2 * P0I[funcindex];
							scale[funcindex] = P0I[funcindex];

							//x0
							P0I[funcindex + 1] = xpositions[i];
							PLB[funcindex + 1] = xpositions[i] - position_radius;
							PUB[funcindex + 1] = xpositions[i] + position_radius;
							scale[funcindex + 1] = P0I[funcindex + 1];

							//y0
							P0I[funcindex + 2] = ypositions[i];
							PLB[funcindex + 2] = ypositions[i] - position_radius;
							PUB[funcindex + 2] = ypositions[i] + position_radius;
							scale[funcindex + 2] = P0I[funcindex + 2];

							//phi
							P0I[funcindex + 3] = 0;
							PLB[funcindex + 3] = -Math.PI;
							PUB[funcindex + 3] = Math.PI;
							scale[funcindex + 3] = 1;

							//alphaX
							P0I[funcindex + 4] = 2;
							PLB[funcindex + 4] = 2e-1;
							PUB[funcindex + 4] = 10;
							scale[funcindex + 4] = P0I[funcindex + 4];

							//alphaY
							P0I[funcindex + 5] = 2;
							PLB[funcindex + 5] = 2e-1;
							PUB[funcindex + 5] = 10;
							scale[funcindex + 5] = P0I[funcindex + 5];

							//beta
							P0I[funcindex + 6] = 2;
							PLB[funcindex + 6] = 1 + 1e-6;
							PUB[funcindex + 6] = 10;
							scale[funcindex + 6] = P0I[funcindex + 6];
						}
					}
					else
						throw new Exception("Parameter length does not correspond to either circular (6 params) or elliptical (8 params) Moffat; params length = " + func);

					if (minimization_type == FitMinimizationType.LeastSquares)
						//grad = new alglib.ndimensional_grad(alglib_Moffat_2d_LM_LS_compound_grad)
						;
					else if (minimization_type == FitMinimizationType.ChiSquared)
						/*grad = new alglib.ndimensional_grad(alglib_Moffat_2d_LM_LS_compound_CHISQ_grad)*/
						;
					else if (minimization_type == FitMinimizationType.Robust)
						/*grad = new alglib.ndimensional_grad(alglib_Moffat_2d_LM_LS_compound_ROBUST_grad)*/
						;
					else if (minimization_type == FitMinimizationType.CashStatistic)
						/*grad = new alglib.ndimensional_grad(alglib_Moffat_2d_LM_LS_compound_CSTAT_grad)*/
						;
					else
						throw new Exception("Fit Type not recognized: '" + minimization_type);
				}
				else
					throw new Exception("Fit Model not recognized: '" + model_name);

				double epsx = 1e-9;
				double epsg = 0;
				double epsf = 0;
				int maxits = 0;
				object[] arrays = new object[4];
				arrays[0] = source;
				arrays[1] = XDATA;
				arrays[2] = YDATA;
				arrays[3] = fcobj;

				alglib.minbccreate(P0I, out alglib.minbcstate bcstate);
				alglib.minbcsetcond(bcstate, epsg, epsf, epsx, maxits);
				alglib.minbcsetbc(bcstate, PLB, PUB);
				alglib.minbcsetscale(bcstate, scale);
				alglib.minbcoptimize(bcstate, grad, null, arrays, alglib.parallel);
				alglib.minbcresults(bcstate, out P0I, out alglib.minbcreport report);

				switch (report.terminationtype)
				{
					case -8:
					{
						termination_msg = "Internal integrity control detected infinite or NAN values in function or gradient. Abnormal termination signaled.";
						break;
					}
					case -3:
					{
						termination_msg = "Inconsistent constraints.";
						break;
					}
					case 1:
					{
						termination_msg = "Relative function improvement is no more than EpsF.";
						break;
					}
					case 2:
					{
						termination_msg = "Scaled step is no more than EpsX.";
						break;
					}
					case 4:
					{
						termination_msg = "Scaled gradient norm is no more than EpsG.";
						break;
					}
					case 5:
					{
						termination_msg = "MaxIts steps was taken.";
						break;
					}
					case 7:
					{
						termination_msg = "Stopping conditions are too stringent, further improvement is impossible, X contains best point found so far.";
						break;
					}
					case 8:
					{
						termination_msg = "Terminated by user.";
						break;
					}
					default:
					{
						termination_msg = "";
						break;
					}
				}

				for (int i = 0; i < count; i++)
					for (int j = 0; j < func - 1; j++)
						param[j, i] = P0I[j + i * (func - 1)];
				for (int i = 0; i < count; i++)
					param[func - 1, i] = P0I[P0I.Length - 1];//background

				/*if (p_err.Length != 0)
				{
					if (model_name == "Gaussian")
						p_err = Gauss_2D_param_err(params, XDATA, YDATA, source);
					else
						p_err = Moffat_2D_param_err(params, XDATA, YDATA, source);
				}*/

				if (fit_residuals.Length != 0)
				{
					double val = 0;
					double[] X = new double[2];
					for (int i = 0; i < XDATA.Length; i++)
						for (int j = 0; j < YDATA.Length; j++)
						{
							X[0] = XDATA[i];
							X[1] = YDATA[j];
							alglib_Gauss_2d_compound(P0I, X, ref val, fcobj);
							fit_residuals[i, j] = source[i, j] - val;
						}
				}
			}
			catch (Exception e)
			{
				throw new Exception(e.Data + "	" + e.InnerException + "	" + e.Message + "	" + e.Source + "	" + e.StackTrace + "	" + e.TargetSite);
			}
		}

		/// <summary>Computes the 2-D transformation elements between intermediate catalogue coordinates and image pixel coordinates.</summary>
		/// <param name="x_intrmdt">The x-axis reference points of which to determine the transformation to.</param>
		/// <param name="y_intrmdt">The y-axis reference points of which to determine the transformation to.</param>
		/// <param name="x_pix">The x-axis points for which to determine the transformation of.</param>
		/// <param name="y_pix">The y-axis points for which to determine the transformation of.</param>
		/// <param name="p">The initial and return parameters of the tranformation. Options are
		/// <br />p[0] = scale; p[1] = phi (radians); p[2] = x-axis pixel coordinate reference; p[3] = y-axis pixel coordinate reference;
		/// <br />or
		/// <br />p[0] = Matrix coeff [0, 0]; p[1] = Matrix coeff [1, 0]; p[2] = Matrix coeff [0, 1]; p[3] = Matrix coeff [1, 1]; p[4] = x-axis pixel coordinate reference; p[5] = y-axis pixel coordinate reference;</param>
		/// <param name="p_lbnd">The lower-bound on the fit parameters.</param>
		/// <param name="p_ubnd">The upper-bound on the fit parameters.</param>
		/// <param name="p_scale">The order of magnitude scale (positive) of the fit parameters.</param>
		public static void Fit_WCSTransform2d(double[] x_intrmdt, double[] y_intrmdt, double[] x_pix, double[] y_pix, ref double[] p, double[] p_lbnd, double[] p_ubnd, double[] p_scale)
		{
			double epsx = 1e-11;
			int maxits = 0;
			alglib.ndimensional_fvec fvec = new alglib.ndimensional_fvec(alglib_WCSTransform2d_fvec);
			alglib.ndimensional_jac jac = new alglib.ndimensional_jac(alglib_WCSTransform2d_jac);
			object[] objj = new object[4];
			objj[0] = (object)x_pix;
			objj[1] = (object)y_pix;
			objj[2] = (object)x_intrmdt;
			objj[3] = (object)y_intrmdt;
			object obj = (object)objj;

			alglib.minlmcreatevj(y_pix.Length, p, out alglib.minlmstate state);
			alglib.minlmsetcond(state, epsx, maxits);
			alglib.minlmsetscale(state, p_scale);
			alglib.minlmsetbc(state, p_lbnd, p_ubnd);
			alglib.minlmoptimize(state, fvec, jac, null, obj);
			alglib.minlmresults(state, out p, out alglib.minlmreport report);
		}

		/// <summary>Computes the 2-D transformation elements between two sets of coordinates.</summary>
		/// <param name="x_ref">The x-axis reference points of which to determine the transformation to.</param>
		/// <param name="y_ref">The y-axis reference points of which to determine the transformation to.</param>
		/// <param name="x_tran">The x-axis points for which to determine the transformation of.</param>
		/// <param name="y_tran">The y-axis points for which to determine the transformation of.</param>
		/// <param name="p">The initial and return parameters of the tranformation. Options are
		/// <br />p[0] = scale; p[1] = phi (radians); p[2] = x-tran pixel coordinate rotation reference; p[3] = y-tran pixel coordinate rotation reference; p[4] = x-tran pixel coordinate shift; p[5] = x-tran pixel coordinate shift;
		/// <br />or
		/// <br />p[0] = Matrix coeff [0, 0]; p[1] = Matrix coeff [1, 0]; p[2] = Matrix coeff [0, 1]; p[3] = Matrix coeff [1, 1]; p[4] = x-tran pixel coordinate rotation reference; p[5] = y-tran pixel coordinate rotation reference; p[6] = x-tran pixel coordinate shift; p[7] = x-tran pixel coordinate shift;</param>
		/// <param name="p_lbnd">The lower-bound on the fit parameters.</param>
		/// <param name="p_ubnd">The upper-bound on the fit parameters.</param>
		/// <param name="p_scale">The order of magnitude scale (positive) of the fit parameters.</param>
		public static void Fit_GeneralTransform2d(double[] x_ref, double[] y_ref, double[] x_tran, double[] y_tran, ref double[] p, double[] p_lbnd, double[] p_ubnd, double[] p_scale)
		{
			double epsx = 1e-11;
			int maxits = 0;
			alglib.ndimensional_fvec fvec = new alglib.ndimensional_fvec(alglib_GeneralTransform2d_fvec);
			object[] objj = new object[4];
			objj[0] = (object)x_tran;
			objj[1] = (object)y_tran;
			objj[2] = (object)x_ref;
			objj[3] = (object)y_ref;
			object obj = (object)objj;

			alglib.minlmcreatev(x_ref.Length, p, 0.000001, out alglib.minlmstate state);
			alglib.minlmsetcond(state, epsx, maxits);
			alglib.minlmsetscale(state, p_scale);
			alglib.minlmsetbc(state, p_lbnd, p_ubnd);
			alglib.minlmoptimize(state, fvec, null, obj);
			alglib.minlmresults(state, out p, out alglib.minlmreport report);
		}

		/// <summary>Fits a polynomial to x, y data.</summary>
		/// <param name="xdata">The x-axis data points.</param>
		/// <param name="ydata">The y-axis data points.</param>
		/// <param name="poly_degree">The degree of polynomial to fit: 1 = linear, 2 = quadratic, etc.</param>
		/// <param name="robust">If true, weights will automatically be determined which supress outliers.</param>
		/// <param name="poly_coeffs">The coefficients of the polynomial ordered by increasing power.</param>
		public static void Fit_Poly1d(double[] xdata, double[] ydata, int poly_degree, bool robust, out double[] poly_coeffs)
		{
			double[] weights = new double[xdata.Length];
			for (int i = 0; i < xdata.Length; i++)
				weights[i] = 1.0;

			double[] xc = new double[0];
			double[] yc = new double[0];
			int[] dc = new int[(0)];

			int m = poly_degree + 1;

			alglib.polynomialfitwc(xdata, ydata, weights, xc, yc, dc, m, out int info, out alglib.barycentricinterpolant p, out alglib.polynomialfitreport rep);
			alglib.polynomialbar2pow(p, out poly_coeffs);

			if (!robust || xdata.Length <= 2)
				return;

			//if robust then determine some weights via the residuals and recalculate solution a few times until some convergence criteria is found
			int iteration_count = 0;
			double sigma = rep.rmserror, yfit, rmsrat = Double.MaxValue;

			while (Math.Abs(rmsrat - 1) > 0.0000001 && iteration_count < 50)
			{
				rmsrat = rep.rmserror;//get the previous rms

				for (int i = 0; i < xdata.Length; i++)
				{
					yfit = alglib.barycentriccalc(p, xdata[i]);
					weights[i] = Math.Exp(-(ydata[i] - yfit) * (ydata[i] - yfit) / (2 * sigma * sigma));
					weights[i] *= weights[i];
				}

				alglib.polynomialfitwc(xdata, ydata, weights, xc, yc, dc, m, out info, out p, out rep);

				sigma = rep.rmserror;
				rmsrat /= rep.rmserror;
				iteration_count++;
			}
			alglib.polynomialbar2pow(p, out poly_coeffs);
		}

		public static double[] Fit_FourierPolynomial(double[] xdata, double[] Fdata, int order)
		{
			double[] p = new double[order * 2 + 2];
			double[] p_lbnd = new double[p.Length];
			double[] p_ubnd = new double[p.Length];
			double[] scale = new double[p.Length];

			try
			{
				double[,] x = new double[Fdata.Length, 1];
				for (int i = 0; i < Fdata.Length; i++)
					x[i, 0] = xdata[i];

				p[p.Length - 2] = JPMath.Mean(Fdata, false);
				p_lbnd[p.Length - 2] = p[p.Length - 2];
				p_ubnd[p.Length - 2] = p[p.Length - 2];
				scale[p.Length - 2] = Math.Abs(p[p.Length - 2]) + 1;

				alglib.ndimensional_pfunc pf = new alglib.ndimensional_pfunc(alglib_Fourier_Polynomial);
				//alglib.ndimensional_rep rep;
				object obj = new double[1] { (double)order };
				//double diffstep = 0;
				double epsx = 0;
				int maxits = 0;

				double amp;
				JPMath.MinMax(Fdata, out double min, out double max, false);
				amp = max - min;
				for (int i = 0; i < p.Length - 2; i++)
				{
					p_lbnd[i] = -amp * 1000;
					p_ubnd[i] = amp * 1000;
					scale[i] = amp + 1;
				}
				double nyq = 0.5 / (xdata[1] - xdata[0]);
				p_ubnd[p.Length - 1] = nyq;
				p_lbnd[p.Length - 1] = nyq / (double)(xdata.Length);

				//MessageBox.Show("ub: " + p_ubnd[p.Length - 1] + "; lb: " + p_lbnd[p.Length - 1]);

				p[p.Length - 1] = nyq / 2;
				scale[p.Length - 1] = p[p.Length - 1];// +1;

				alglib.lsfitcreatef(x, Fdata, p, 0.00001, out alglib.lsfitstate state);
				alglib.lsfitsetcond(state, epsx, maxits);
				alglib.lsfitsetscale(state, scale);
				alglib.lsfitsetbc(state, p_lbnd, p_ubnd);
				alglib.lsfitfit(state, pf, null, obj);

				alglib.lsfitresults(state, out int info, out p, out alglib.lsfitreport report);
				//MessageBox.Show(info.ToString());
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Data + "	" + e.InnerException + "	" + e.Message + "	" + e.Source + "	" + e.StackTrace + "	" + e.TargetSite);
			}

			//MessageBox.Show(p[p.Length - 1] + " ");

			return p;
		}

		public static double[] Gauss_2D_param_err(double[] p, double[] x, double[] y, double[,] Z)
		{
			double[,] hess = new double[p.Length, p.Length];
			double den;

			if (p.Length == 5)
			{
				double p3sq = p[3] * p[3];
				double p3cu = p[3] * p3sq;
				double p3qu = p3sq * p3sq;
				double p3he = p3cu * p3cu;
				double p0sq = p[0] * p[0];
				double twop3sq = 2 * p3sq;
				double p3pe = p3sq * p3cu;
				double twop1 = 2 * p[1];
				double twop2 = 2 * p[2];
				double dx, dxsq, twoxi, twop1mtwoxi, twop1mtwoxisq, dy, dxsqpdysq, exparg, expres, exparg2, expres2, p0expres2, a, dxsqpdysqsq, twoyj, twop2mtwoyj, twop2mtwoyjsq;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					dxsq = dx * dx;
					twoxi = 2 * x[i];
					twop1mtwoxi = twop1 - twoxi;
					twop1mtwoxisq = twop1mtwoxi * twop1mtwoxi;

					for (int j = 0; j < y.Length; j++)
					{
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Z[i, j];

						dy = p[2] - y[j];
						dxsqpdysq = dxsq + dy * dy;
						exparg = -dxsqpdysq / p3sq;
						expres = Math.Exp(exparg);
						exparg2 = -dxsqpdysq / twop3sq;
						expres2 = Math.Exp(exparg2);
						p0expres2 = p[0] * expres2;
						a = p[4] - Z[i, j] + p0expres2;
						dxsqpdysqsq = dxsqpdysq * dxsqpdysq;
						twoyj = 2 * y[j];
						twop2mtwoyj = twop2 - twoyj;
						twop2mtwoyjsq = twop2mtwoyj * twop2mtwoyj;

						hess[0, 0] += (2 * expres) / den;
						hess[1, 1] += (p0sq * expres * twop1mtwoxisq) / (2 * den * p3qu) - (2 * p0expres2 * a) / (den * p3sq) + (p0expres2 * twop1mtwoxisq * a) / (2 * den * p3qu);
						hess[2, 2] += (p0sq * expres * twop2mtwoyjsq) / (2 * den * p3qu) - (2 * p0expres2 * a) / (den * p3sq) + (p0expres2 * twop2mtwoyjsq * a) / (2 * den * p3qu);
						hess[3, 3] += (2 * p0sq * expres * dxsqpdysqsq) / (den * p3he) - (6 * p0expres2 * (dxsqpdysq) * a) / (den * p3qu) + (2 * p0expres2 * dxsqpdysqsq * a) / (den * p3he);
						hess[4, 4] += 2 / den;
						hess[0, 1] += -(expres2 * twop1mtwoxi * a) / (den * p3sq) - (p[0] * expres * twop1mtwoxi) / (den * p3sq);
						hess[0, 2] += -(expres2 * twop2mtwoyj * a) / (den * p3sq) - (p[0] * expres * twop2mtwoyj) / (den * p3sq);
						hess[0, 3] += (2 * expres2 * (dxsqpdysq) * a) / (den * p3cu) + (2 * p[0] * expres * (dxsqpdysq)) / (den * p3cu);
						hess[0, 4] += (2 * expres2) / den;
						hess[1, 2] += (p0sq * expres * twop1mtwoxi * twop2mtwoyj) / (2 * den * p3qu) + (p0expres2 * twop1mtwoxi * twop2mtwoyj * a) / (2 * den * p3qu);
						hess[1, 3] += (2 * p0expres2 * twop1mtwoxi * a) / (den * p3cu) - (p0sq * expres * twop1mtwoxi * (dxsqpdysq)) / (den * p3pe) - (p0expres2 * twop1mtwoxi * (dxsqpdysq) * a) / (den * p3pe);
						hess[1, 4] += -(p0expres2 * twop1mtwoxi) / (den * p3sq);
						hess[2, 3] += (2 * p0expres2 * twop2mtwoyj * a) / (den * p3cu) - (p0sq * expres * twop2mtwoyj * (dxsqpdysq)) / (den * p3pe) - (p0expres2 * twop2mtwoyj * (dxsqpdysq) * a) / (den * p3pe);
						hess[2, 4] += -(p0expres2 * twop2mtwoyj) / (den * p3sq);
						hess[3, 4] += (2 * p0expres2 * (dxsqpdysq)) / (den * p3cu);
					}
				}
				hess[1, 0] = hess[0, 1];
				hess[2, 0] = hess[0, 2];
				hess[3, 0] = hess[0, 3];
				hess[4, 0] = hess[0, 4];
				hess[2, 1] = hess[1, 2];
				hess[3, 1] = hess[1, 3];
				hess[4, 1] = hess[1, 4];
				hess[3, 2] = hess[2, 3];
				hess[4, 2] = hess[2, 4];
				hess[4, 3] = hess[3, 4];
			}
			else if (p.Length == 7)
			{
				double cosp3 = Math.Cos(p[3]);
				double sinp3 = Math.Sin(p[3]);
				double p4sq = p[4] * p[4];
				double p5sq = p[5] * p[5];
				double p0sq = p[0] * p[0];
				double cosp3sq = cosp3 * cosp3;
				double sinp3sq = sinp3 * sinp3;
				double twop0sq = 2 * p0sq;
				double twop0 = 2 * p[0];
				double p4cu = p4sq * p[4];
				double p5cu = p5sq * p[5];
				double p4he = p4cu * p4cu;
				double p5he = p5cu * p5cu;
				double p4qu = p4sq * p4sq;
				double p5qu = p5sq * p5sq;
				double dx, cosp3dx, dy, sinp3dy, a, b, asq, bsq, exarg1, expres1, c, exparg2, expres2, p0expres2, g, twop0sqexpres1, h, k;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					cosp3dx = cosp3 * dx;

					for (int j = 0; j < y.Length; j++)
					{
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Z[i, j];

						dy = p[2] - y[j];
						sinp3dy = sinp3 * dy;
						a = cosp3dx + sinp3dy;
						b = cosp3 * dy - sinp3 * dx;
						asq = a * a;
						bsq = b * b;
						exarg1 = -asq / p4sq - bsq / p5sq;
						expres1 = Math.Exp(exarg1);
						c = (cosp3 * a) / p4sq - (sinp3 * b) / p5sq;
						exparg2 = -asq / (2 * p4sq) - bsq / (2 * p5sq);
						expres2 = Math.Exp(exparg2);
						p0expres2 = p[0] * expres2;
						g = p[6] - Z[i, j] + p0expres2;
						twop0sqexpres1 = twop0sq * expres1;
						h = (cosp3 * b) / p5sq + (sinp3 * a) / p4sq;
						k = (a * b) / p4sq - (a * b) / p5sq;

						hess[0, 0] += (2 * expres1) / den;
						hess[1, 1] += (twop0sqexpres1 * c * c) / den + (2 * p0expres2 * c * c * g) / den - (2 * p0expres2 * (cosp3sq / p4sq + sinp3sq / p5sq) * g) / den;
						hess[2, 2] += (twop0sqexpres1 * h * h) / den + (2 * p0expres2 * h * h * g) / den - (2 * p0expres2 * (cosp3sq / p5sq + sinp3sq / p4sq) * g) / den;
						hess[3, 3] += (twop0sqexpres1 * k * k) / den + (2 * p0expres2 * g * (asq / p4sq - bsq / p4sq - asq / p5sq + bsq / p5sq)) / den + (2 * p0expres2 * k * k * g) / den;
						hess[4, 4] += (twop0sqexpres1 * a * a * a * a) / (den * p4he) - (6 * p0expres2 * asq * g) / (den * p4qu) + (2 * p0expres2 * a * a * a * a * g) / (den * p4he);
						hess[5, 5] += (twop0sqexpres1 * b * b * b * b) / (den * p5he) - (6 * p0expres2 * bsq * g) / (den * p5qu) + (2 * p0expres2 * b * b * b * b * g) / (den * p5he);
						hess[6, 6] += 2 / den;
						hess[0, 1] += -(twop0 * expres1 * c) / den - (2 * expres2 * c * g) / den;
						hess[0, 2] += -(twop0 * expres1 * h) / den - (2 * expres2 * h * g) / den;
						hess[0, 3] += -(twop0 * expres1 * k) / den - (2 * expres2 * k * g) / den;
						hess[0, 4] += (twop0 * expres1 * asq) / (den * p4cu) + (2 * expres2 * asq * g) / (den * p4cu);
						hess[0, 5] += (twop0 * expres1 * bsq) / (den * p5cu) + (2 * expres2 * bsq * g) / (den * p5cu);
						hess[0, 6] += (2 * expres2) / den;
						hess[1, 2] += (twop0sqexpres1 * h * c) / den - (2 * p0expres2 * ((cosp3 * sinp3) / p4sq - (cosp3 * sinp3) / p5sq) * g) / den + (2 * p0expres2 * h * c * g) / den;
						hess[1, 3] += (twop0sqexpres1 * k * c) / den - (2 * p0expres2 * g * ((cosp3 * b) / p4sq - (cosp3 * b) / p5sq - (sinp3 * a) / p4sq + (sinp3 * a) / p5sq)) / den + (2 * p0expres2 * k * c * g) / den;
						hess[1, 4] += (4 * p0expres2 * cosp3 * a * g) / (den * p4cu) - (2 * p0expres2 * asq * c * g) / (den * p4cu) - (twop0sqexpres1 * asq * c) / (den * p4cu);
						hess[1, 5] += -(twop0sqexpres1 * bsq * c) / (den * p5cu) - (4 * p0expres2 * sinp3 * b * g) / (den * p5cu) - (2 * p0expres2 * bsq * c * g) / (den * p5cu);
						hess[1, 6] += -(2 * p0expres2 * c) / den;
						hess[2, 3] += (twop0sqexpres1 * k * h) / den - (2 * p0expres2 * g * ((cosp3 * a) / p4sq - (cosp3 * a) / p5sq + (sinp3 * b) / p4sq - (sinp3 * b) / p5sq)) / den + (2 * p0expres2 * k * h * g) / den;
						hess[2, 4] += (4 * p0expres2 * sinp3 * a * g) / (den * p4cu) - (twop0sqexpres1 * asq * h) / (den * p4cu) - (2 * p0expres2 * asq * h * g) / (den * p4cu);
						hess[2, 5] += (4 * p0expres2 * cosp3 * b * g) / (den * p5cu) - (2 * p0expres2 * bsq * h * g) / (den * p5cu) - (twop0sqexpres1 * bsq * h) / (den * p5cu);
						hess[2, 6] += -(2 * p0expres2 * h) / den;
						hess[3, 4] += (4 * p0expres2 * a * b * g) / (den * p4cu) - (twop0sqexpres1 * asq * k) / (den * p4cu) - (2 * p0expres2 * asq * k * g) / (den * p4cu);
						hess[3, 5] += -(twop0sqexpres1 * bsq * k) / (den * p5cu) - (4 * p0expres2 * a * b * g) / (den * p5cu) - (2 * p0expres2 * bsq * k * g) / (den * p5cu);
						hess[3, 6] += -(2 * p0expres2 * k) / den;
						hess[4, 5] += (twop0sqexpres1 * asq * bsq) / (den * p4cu * p5cu) + (2 * p0expres2 * asq * bsq * g) / (den * p4cu * p5cu);
						hess[4, 6] += (2 * p0expres2 * asq) / (den * p4cu);
						hess[5, 6] += (2 * p0expres2 * bsq) / (den * p5cu);
					}
				}
				hess[1, 0] = hess[0, 1];
				hess[2, 0] = hess[0, 2];
				hess[3, 0] = hess[0, 3];
				hess[4, 0] = hess[0, 4];
				hess[5, 0] = hess[0, 5];
				hess[6, 0] = hess[0, 6];
				hess[2, 1] = hess[1, 2];
				hess[3, 1] = hess[1, 3];
				hess[4, 1] = hess[1, 4];
				hess[5, 1] = hess[1, 5];
				hess[6, 1] = hess[1, 6];
				hess[3, 2] = hess[2, 3];
				hess[4, 2] = hess[2, 4];
				hess[5, 2] = hess[2, 5];
				hess[6, 2] = hess[2, 6];
				hess[4, 3] = hess[3, 4];
				hess[5, 3] = hess[3, 5];
				hess[6, 3] = hess[3, 6];
				hess[5, 4] = hess[4, 5];
				hess[6, 4] = hess[4, 6];
				hess[6, 5] = hess[5, 6];
			}

			alglib.rmatrixinverse(ref hess, out int info, out alglib.matinvreport rep);
			double[] errs = new double[p.Length];
			for (int i = 0; i < p.Length; i++)
				errs[i] = Math.Sqrt(hess[i, i]);

			return errs;
		}

		public static double[] Moffat_2D_param_err(double[] p, double[] x, double[] y, double[,] Z)
		{
			double[,] hess = new double[p.Length, p.Length];
			double den;

			if (p.Length == 6)
			{
				double p3sq = p[3] * p[3];
				double p41 = p[4] + 1;
				double p42 = p[4] + 2;
				double p3qu = p3sq * p3sq;
				double p3pe = p3qu * p[3];
				double p3cu = p3sq * p[3];
				double p3he = p3cu * p3cu;
				double p0sq = p[0] * p[0];
				double p4sq = p[4] * p[4];
				double p0p4 = p[0] * p[4];
				double p0sqp4sq = p0sq * p4sq;
				double twop1 = 2 * p[1];
				double twop2 = 2 * p[2];

				for (int i = 0; i < x.Length; i++)
				{
					double dx = p[1] - x[i];
					double dxsq = dx * dx;
					double twoxi = 2 * x[i];

					for (int j = 0; j < y.Length; j++)
					{
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Z[i, j];

						double dy = p[2] - y[j];
						double dysq = dy * dy;
						double dxsqpdysq = dxsq + dysq;
						double logarg1 = dxsqpdysq / p3sq + 1;
						double logres1 = Math.Log(logarg1);
						double logarg1powp4 = Math.Pow(logarg1, p[4]);
						double logarg1powp41 = logarg1powp4 * logarg1;
						double logarg1powp42 = logarg1powp41 * logarg1;
						double a = p[5] - Z[i, j] + p[0] / logarg1powp4;
						double twoyj = 2 * y[j];
						double twop1mtwoxi = twop1 - twoxi;
						double twop2mtwoyj = twop2 - twoyj;
						double logarg1pow2p4 = logarg1powp4 * logarg1powp4;
						double logarg1pow2p42 = Math.Pow(logarg1, 2 * p[4] + 2);

						hess[0, 0] += (2 / logarg1pow2p4) / den;
						hess[1, 1] += (2 * p0sqp4sq * twop1mtwoxi * twop1mtwoxi) / (den * p3qu * logarg1pow2p42) - (4 * p0p4 * a) / (den * p3sq * logarg1powp41) + (2 * p0p4 * twop1mtwoxi * twop1mtwoxi * p41 * a) / (den * p3qu * logarg1powp42);
						hess[2, 2] += (2 * p0sqp4sq * twop2mtwoyj * twop2mtwoyj) / (den * p3qu * logarg1pow2p42) - (4 * p0p4 * a) / (den * p3sq * logarg1powp41) + (2 * p0p4 * twop2mtwoyj * twop2mtwoyj * p41 * a) / (den * p3qu * logarg1powp42);
						hess[3, 3] += (8 * p0sqp4sq * dxsqpdysq * dxsqpdysq) / (den * p3he * logarg1pow2p42) - (12 * p0p4 * dxsqpdysq * a) / (den * p3qu * logarg1powp41) + (8 * p0p4 * dxsqpdysq * dxsqpdysq * p41 * a) / (den * p3he * logarg1powp42);
						hess[4, 4] += (2 * p0sq * logres1 * logres1 / logarg1pow2p4) / den + (2 * p[0] * logres1 * logres1 * a) / (den * logarg1powp4);
						hess[5, 5] += 2 / den;
						hess[0, 1] += -(2 * p[4] * twop1mtwoxi * a) / (den * p3sq * logarg1powp41) - (2 * p0p4 * twop1mtwoxi) / (den * p3sq * logarg1powp4 * logarg1powp41);
						hess[0, 2] += -(2 * p[4] * twop2mtwoyj * a) / (den * p3sq * logarg1powp41) - (2 * p0p4 * twop2mtwoyj) / (den * p3sq * logarg1powp4 * logarg1powp41);
						hess[0, 3] += (4 * p[4] * dxsqpdysq * a) / (den * p3cu * logarg1powp41) + (4 * p0p4 * dxsqpdysq) / (den * p3cu * logarg1powp4 * logarg1powp41);
						hess[0, 4] += -(2 * p[0] * logres1 / logarg1pow2p4) / den - (2 * logres1 * a) / (den * logarg1powp4);
						hess[0, 5] += 2 / (den * logarg1powp4);
						hess[1, 2] += (2 * p0sqp4sq * twop1mtwoxi * twop2mtwoyj) / (den * p3qu * logarg1pow2p42) + (2 * p0p4 * twop1mtwoxi * twop2mtwoyj * p41 * a) / (den * p3qu * logarg1powp42);
						hess[1, 3] += (4 * p0p4 * twop1mtwoxi * a) / (den * p3cu * logarg1powp41) - (4 * p0sqp4sq * twop1mtwoxi * dxsqpdysq) / (den * p3pe * logarg1pow2p42) - (4 * p0p4 * twop1mtwoxi * dxsqpdysq * p41 * a) / (den * p3pe * logarg1powp42);
						hess[1, 4] += (2 * p0p4 * logres1 * twop1mtwoxi * a) / (den * p3sq * logarg1powp41) - (2 * p[0] * twop1mtwoxi * a) / (den * p3sq * logarg1powp41) + (2 * p0sq * p[4] * logres1 * twop1mtwoxi) / (den * p3sq * logarg1powp4 * logarg1powp41);
						hess[1, 5] += -(2 * p0p4 * twop1mtwoxi) / (den * p3sq * logarg1powp41);
						hess[2, 3] += (4 * p0p4 * twop2mtwoyj * a) / (den * p3cu * logarg1powp41) - (4 * p0sqp4sq * twop2mtwoyj * dxsqpdysq) / (den * p3pe * logarg1pow2p42) - (4 * p0p4 * twop2mtwoyj * dxsqpdysq * p41 * a) / (den * p3pe * logarg1powp42);
						hess[2, 4] += (2 * p0p4 * logres1 * twop2mtwoyj * a) / (den * p3sq * logarg1powp41) - (2 * p[0] * twop2mtwoyj * a) / (den * p3sq * logarg1powp41) + (2 * p0sq * p[4] * logres1 * twop2mtwoyj) / (den * p3sq * logarg1powp4 * logarg1powp41);
						hess[2, 5] += -(2 * p0p4 * twop2mtwoyj) / (den * p3sq * logarg1powp41);
						hess[3, 4] += (4 * p[0] * dxsqpdysq * a) / (den * p3cu * logarg1powp41) - (4 * p0p4 * logres1 * dxsqpdysq * a) / (den * p3cu * logarg1powp41) - (4 * p0sq * p[4] * logres1 * dxsqpdysq) / (den * p3cu * logarg1powp4 * logarg1powp41);
						hess[3, 5] += (4 * p0p4 * dxsqpdysq) / (den * p3cu * logarg1powp41);
						hess[4, 5] += -(2 * p[0] * logres1) / (den * logarg1powp4);
					}
				}
				hess[1, 0] = hess[0, 1];
				hess[2, 0] = hess[0, 2];
				hess[3, 0] = hess[0, 3];
				hess[4, 0] = hess[0, 4];
				hess[5, 0] = hess[0, 5];
				hess[2, 1] = hess[1, 2];
				hess[3, 1] = hess[1, 3];
				hess[4, 1] = hess[1, 4];
				hess[5, 1] = hess[1, 5];
				hess[3, 2] = hess[2, 3];
				hess[4, 2] = hess[2, 4];
				hess[5, 2] = hess[2, 5];
				hess[4, 3] = hess[3, 4];
				hess[5, 3] = hess[3, 5];
				hess[5, 4] = hess[4, 5];
			}
			else if (p.Length == 8)
			{
				double cosp3 = Math.Cos(p[3]);
				double sinp3 = Math.Sin(p[3]);
				double p4sq = p[4] * p[4];
				double p5sq = p[5] * p[5];
				double twocosp3 = 2 * cosp3;
				double twosinp3 = 2 * sinp3;
				double p0sq = p[0] * p[0];
				double p6sq = p[6] * p[6];
				double p0p6 = p[0] * p[6];
				double p0sqp6sq = p0sq * p6sq;
				double p61 = p[6] + 1;
				double twocosp3sq = twocosp3 * twocosp3;
				double twosinp3sq = twosinp3 * twosinp3;
				double p4cu = p4sq * p[4];
				double p4he = p4cu * p4cu;
				double p5cu = p5sq * p[5];
				double p5he = p5cu * p5cu;
				double p4qu = p4sq * p4sq;
				double p5qu = p5sq * p5sq;
				double dx, cosp3dx, sinp3dx, dy, cosp3dxpsinp3dy, cosp3dymsinp3dx, logarg, logres, logargpowp6, logargpow2p6, logargpowp61, logargpowp62, logargpow2p62, a, b, c, d, dsq, f, fsq, g, gsq, h, k;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					cosp3dx = cosp3 * dx;
					sinp3dx = sinp3 * dx;

					for (int j = 0; j < y.Length; j++)
					{
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Z[i, j];

						dy = p[2] - y[j];
						cosp3dxpsinp3dy = cosp3dx + sinp3 * dy;
						cosp3dymsinp3dx = cosp3 * dy - sinp3dx;
						logarg = cosp3dxpsinp3dy * cosp3dxpsinp3dy / p4sq + cosp3dymsinp3dx * cosp3dymsinp3dx / p5sq + 1;
						logres = Math.Log(logarg);
						logargpowp6 = Math.Pow(logarg, p[6]);
						logargpow2p6 = logargpowp6 * logargpowp6;
						logargpowp61 = logargpowp6 * logarg;
						logargpowp62 = logargpowp61 * logarg;
						logargpow2p62 = logargpowp61 * logargpowp61;
						a = p[7] - Z[i, j] + p[0] / logargpowp6;
						b = cosp3dxpsinp3dy * cosp3dxpsinp3dy;
						c = cosp3dymsinp3dx * cosp3dymsinp3dx;
						d = twocosp3 * cosp3dxpsinp3dy / p4sq - twosinp3 * cosp3dymsinp3dx / p5sq;
						dsq = d * d;
						f = twocosp3 * cosp3dymsinp3dx / p5sq + twosinp3 * cosp3dxpsinp3dy / p4sq;
						fsq = f * f;
						g = 2 * cosp3dxpsinp3dy * cosp3dymsinp3dx / p4sq - 2 * cosp3dxpsinp3dy * cosp3dymsinp3dx / p5sq;
						gsq = g * g;
						h = cosp3dxpsinp3dy * cosp3dxpsinp3dy * cosp3dxpsinp3dy * cosp3dxpsinp3dy;
						k = cosp3dymsinp3dx * cosp3dymsinp3dx * cosp3dymsinp3dx * cosp3dymsinp3dx;

						hess[0, 0] += (2 / logargpow2p6) / den;
						hess[1, 1] += (2 * p0sqp6sq * dsq) / (den * logargpow2p62) - (2 * p0p6 * ((twocosp3sq) / p4sq + (twosinp3sq) / p5sq) * (a)) / (den * logargpowp61) + (2 * p0p6 * dsq * p61 * (a)) / (den * logargpowp62);
						hess[2, 2] += (2 * p0sqp6sq * fsq) / (den * logargpow2p62) - (2 * p0p6 * ((twocosp3sq) / p5sq + (twosinp3sq) / p4sq) * (a)) / (den * logargpowp61) + (2 * p0p6 * fsq * p61 * (a)) / (den * logargpowp62);
						hess[3, 3] += (2 * p0sqp6sq * gsq) / (den * logargpow2p62) + (2 * p0p6 * (a) * ((2 * b) / p4sq - (2 * c) / p4sq - (2 * b) / p5sq + (2 * c) / p5sq)) / (den * logargpowp61) + (2 * p0p6 * gsq * p61 * (a)) / (den * logargpowp62);
						hess[4, 4] += (8 * p0sqp6sq * h) / (den * p4he * logargpow2p62) - (12 * p0p6 * b * (a)) / (den * p4qu * logargpowp61) + (8 * p0p6 * h * p61 * (a)) / (den * p4he * logargpowp62);
						hess[5, 5] += (8 * p0sqp6sq * k) / (den * p5he * logargpow2p62) - (12 * p0p6 * c * (a)) / (den * p5qu * logargpowp61) + (8 * p0p6 * k * p61 * (a)) / (den * p5he * logargpowp62);
						hess[6, 6] += (2 * p0sq * logres * logres / logargpow2p6) / den + (2 * p[0] * logres * logres * (a)) / (den * logargpowp6);
						hess[7, 7] += 2 / den;
						hess[0, 1] += -(2 * p[6] * d * (a)) / (den * logargpowp61) - (2 * p0p6 * d) / (den * logargpowp61 * logargpowp6);
						hess[0, 2] += -(2 * p[6] * (f) * (a)) / (den * logargpowp61) - (2 * p0p6 * (f)) / (den * logargpowp61 * logargpowp6);
						hess[0, 3] += -(2 * p[6] * (g) * (a)) / (den * logargpowp61) - (2 * p0p6 * (g)) / (den * logargpowp61 * logargpowp6);
						hess[0, 4] += (4 * p[6] * b * (a)) / (den * p4cu * logargpowp61) + (4 * p0p6 * b) / (den * p4cu * logargpowp61 * logargpowp6);
						hess[0, 5] += (4 * p[6] * c * (a)) / (den * p5cu * logargpowp61) + (4 * p0p6 * c) / (den * p5cu * logargpowp61 * logargpowp6);
						hess[0, 6] += -(2 * logres * (a)) / (den * logargpowp6) - (2 * p[0] * logres / logargpow2p6) / den;
						hess[0, 7] += 2 / (den * logargpowp6);
						hess[1, 2] += (2 * p0sqp6sq * d * (f)) / (den * logargpow2p62) - (2 * p0p6 * ((twocosp3 * sinp3) / p4sq - (twocosp3 * sinp3) / p5sq) * (a)) / (den * logargpowp61) + (2 * p0p6 * d * (f) * p61 * (a)) / (den * logargpowp62);
						hess[1, 3] += (2 * p0sqp6sq * (g) * d) / (den * logargpow2p62) - (2 * p0p6 * (a) * ((twocosp3 * cosp3dymsinp3dx) / p4sq - (twocosp3 * cosp3dymsinp3dx) / p5sq - (twosinp3 * cosp3dxpsinp3dy) / p4sq + (twosinp3 * cosp3dxpsinp3dy) / p5sq)) / (den * logargpowp61) + (2 * p0p6 * (g) * d * p61 * (a)) / (den * logargpowp62);
						hess[1, 4] += (8 * p0p6 * cosp3 * cosp3dxpsinp3dy * (a)) / (den * p4cu * logargpowp61) - (4 * p0sqp6sq * b * d) / (den * p4cu * logargpow2p62) - (4 * p0p6 * b * d * p61 * (a)) / (den * p4cu * logargpowp62);
						hess[1, 5] += -(4 * p0sqp6sq * c * d) / (den * p5cu * logargpow2p62) - (8 * p0p6 * sinp3 * cosp3dymsinp3dx * (a)) / (den * p5cu * logargpowp61) - (4 * p0p6 * c * d * p61 * (a)) / (den * p5cu * logargpowp62);
						hess[1, 6] += (2 * p0p6 * logres * d * (a)) / (den * logargpowp61) - (2 * p[0] * d * (a)) / (den * logargpowp61) + (2 * p0sq * p[6] * logres * d) / (den * logargpowp61 * logargpowp6);
						hess[1, 7] += -(2 * p0p6 * d) / (den * logargpowp61);
						hess[2, 3] += (2 * p0sqp6sq * (g) * (f)) / (den * logargpow2p62) - (2 * p0p6 * (a) * ((twocosp3 * cosp3dxpsinp3dy) / p4sq - (twocosp3 * cosp3dxpsinp3dy) / p5sq + (twosinp3 * cosp3dymsinp3dx) / p4sq - (twosinp3 * cosp3dymsinp3dx) / p5sq)) / (den * logargpowp61) + (2 * p0p6 * (g) * (f) * p61 * (a)) / (den * logargpowp62);
						hess[2, 4] += (8 * p0p6 * sinp3 * cosp3dxpsinp3dy * (a)) / (den * p4cu * logargpowp61) - (4 * p0sqp6sq * b * (f)) / (den * p4cu * logargpow2p62) - (4 * p0p6 * b * (f) * p61 * (a)) / (den * p4cu * logargpowp62);
						hess[2, 5] += (8 * p0p6 * cosp3 * cosp3dymsinp3dx * (a)) / (den * p5cu * logargpowp61) - (4 * p0sqp6sq * c * (f)) / (den * p5cu * logargpow2p62) - (4 * p0p6 * c * (f) * p61 * (a)) / (den * p5cu * logargpowp62);
						hess[2, 6] += (2 * p0p6 * logres * (f) * (a)) / (den * logargpowp61) - (2 * p[0] * (f) * (a)) / (den * logargpowp61) + (2 * p0sq * p[6] * logres * (f)) / (den * logargpowp61 * logargpowp6);
						hess[2, 7] += -(2 * p0p6 * (f)) / (den * logargpowp61);
						hess[3, 4] += (8 * p0p6 * cosp3dxpsinp3dy * cosp3dymsinp3dx * (a)) / (den * p4cu * logargpowp61) - (4 * p0sqp6sq * b * (g)) / (den * p4cu * logargpow2p62) - (4 * p0p6 * b * (g) * p61 * (a)) / (den * p4cu * logargpowp62);
						hess[3, 5] += -(4 * p0sqp6sq * c * (g)) / (den * p5cu * logargpow2p62) - (8 * p0p6 * cosp3dxpsinp3dy * cosp3dymsinp3dx * (a)) / (den * p5cu * logargpowp61) - (4 * p0p6 * c * (g) * p61 * (a)) / (den * p5cu * logargpowp62);
						hess[3, 6] += (2 * p0p6 * logres * (g) * (a)) / (den * logargpowp61) - (2 * p[0] * (g) * (a)) / (den * logargpowp61) + (2 * p0sq * p[6] * logres * (g)) / (den * logargpowp61 * logargpowp6);
						hess[3, 7] += -(2 * p0p6 * (g)) / (den * logargpowp61);
						hess[4, 5] += (8 * p0sqp6sq * b * c) / (den * p4cu * p5cu * logargpow2p62) + (8 * p0p6 * b * c * p61 * (a)) / (den * p4cu * p5cu * logargpowp62);
						hess[4, 6] += (4 * p[0] * b * (a)) / (den * p4cu * logargpowp61) - (4 * p0p6 * logres * b * (a)) / (den * p4cu * logargpowp61) - (4 * p0sq * p[6] * logres * b) / (den * p4cu * logargpowp61 * logargpowp6);
						hess[4, 7] += (4 * p0p6 * b) / (den * p4cu * logargpowp61);
						hess[5, 6] += (4 * p[0] * c * (a)) / (den * p5cu * logargpowp61) - (4 * p0p6 * logres * c * (a)) / (den * p5cu * logargpowp61) - (4 * p0sq * p[6] * logres * c) / (den * p5cu * logargpowp61 * logargpowp6);
						hess[5, 7] += (4 * p0p6 * c) / (den * p5cu * logargpowp61);
						hess[6, 7] += -(2 * p[0] * logres) / (den * logargpowp6);
					}
				}
				hess[1, 0] = hess[0, 1];
				hess[2, 0] = hess[0, 2];
				hess[3, 0] = hess[0, 3];
				hess[4, 0] = hess[0, 4];
				hess[5, 0] = hess[0, 5];
				hess[6, 0] = hess[0, 6];
				hess[7, 0] = hess[0, 7];
				hess[2, 1] = hess[1, 2];
				hess[3, 1] = hess[1, 3];
				hess[4, 1] = hess[1, 4];
				hess[5, 1] = hess[1, 5];
				hess[6, 1] = hess[1, 6];
				hess[7, 1] = hess[1, 7];
				hess[3, 2] = hess[2, 3];
				hess[4, 2] = hess[2, 4];
				hess[5, 2] = hess[2, 5];
				hess[6, 2] = hess[2, 6];
				hess[7, 2] = hess[2, 7];
				hess[4, 3] = hess[3, 4];
				hess[5, 3] = hess[3, 5];
				hess[6, 3] = hess[3, 6];
				hess[7, 3] = hess[3, 7];
				hess[5, 4] = hess[4, 5];
				hess[6, 4] = hess[4, 6];
				hess[7, 4] = hess[4, 7];
				hess[6, 5] = hess[5, 6];
				hess[7, 5] = hess[5, 7];
				hess[7, 6] = hess[6, 7];
			}

			alglib.rmatrixinverse(ref hess, out int info, out alglib.matinvreport rep);
			double[] errs = new double[p.Length];
			for (int i = 0; i < p.Length; i++)
				errs[i] = Math.Sqrt(hess[i, i]);

			return errs;
		}

		/// <summary>Computes the normalized radial profile.</summary>
		/// <param name="Rdata">The 2D profile to create the radial plot from. Maximum value must be the center pixel, and Rdata array [x, y] size must be odd and square. If the size Rdata is less than 33x33 elements, the profile is spline-interpolated by a factor of 5.</param>
		/// <param name="xdata">The abscissa (horizontal) values for the Rdata array.</param>
		/// <param name="ydata">The ordinate (vertical) values for the Rdata array.</param>
		/// <param name="axis_scale">The unit scale per pixel of the axes, assuming both axes are equal. Pass 0 or 1 for no scale, or any other value greater than zero for scaling.</param>
		/// <param name="radial_x">The radial profile radius values (returned).</param>
		/// <param name="radial_y">The radial profile values (returned).</param>
		public static void Radial_Profile_Normalized(double[,] Rdata, int[] xdata, int[] ydata, double axis_scale, out double[] radial_x, out double[] radial_y)
		{
			if (Rdata.GetLength(0) != Rdata.GetLength(1))
				throw new Exception("Error: Rdata array must be square.");

			if (Rdata.GetLength(0) < 5)
				throw new Exception("Error: Rdata array must be at least 5x5 pixels...");

			if (IsEven(Rdata.GetLength(0)))
				throw new Exception("Error: Rdata array not odd-size number of square elements. Cannot solve radial fit.");

			int Rdata_HW = (Rdata.GetLength(0) - 1) / 2;

			double center_val = JPMath.Max(Rdata, out int xx, out int yy, false);

			if (xx != Rdata_HW || yy != Rdata_HW)
				throw new Exception("Error: Rdata maximum of array not at the center of the array. Cannot solve radial fit.");

			double[] xdata_dbl = new double[xdata.Length];
			double[] ydata_dbl = new double[ydata.Length];
			for (int i = 0; i < xdata.Length; i++)
			{
				xdata_dbl[i] = (double)(xdata[i]);
				ydata_dbl[i] = (double)(ydata[i]);
			}

			double[,] interp_Rdata = Rdata;
			if (Rdata_HW <= 15)
			{
				int interp_delta = 5;
				interp_Rdata = JPMath.Interpolate2d(xdata_dbl, ydata_dbl, interp_Rdata, interp_delta, interp_delta, out xdata_dbl, out ydata_dbl, true);
			}

			ArrayList distances_LIST = new ArrayList();
			ArrayList values_LIST = new ArrayList();
			double Rdata_HW_sq = (double)(Rdata_HW * Rdata_HW);
			double X0 = (double)xdata[Rdata_HW];
			double Y0 = (double)ydata[Rdata_HW];

			for (int x = 0; x < xdata_dbl.Length; x++)
			{
				double dx_sq = (xdata_dbl[x] - X0);
				dx_sq *= dx_sq;
				for (int y = 0; y < ydata_dbl.Length; y++)
				{
					double dy = ydata_dbl[y] - Y0;
					double d_sq = dx_sq + dy * dy;
					if (d_sq > Rdata_HW_sq)
						continue;

					distances_LIST.Add(d_sq);
					values_LIST.Add(interp_Rdata[x, y]);
				}
			}

			double[] distances_sq = new double[distances_LIST.Count];
			double[] values = new double[distances_LIST.Count];
			for (int q = 0; q < distances_sq.Length; q++)
			{
				distances_sq[q] = (double)distances_LIST[q];
				values[q] = (double)values_LIST[q];
			}
			Array.Sort(distances_sq, values);
			values = JPMath.VectorDivScalar(values, center_val, false);//normalize to max count for radial profile plot

			ArrayList r_binnedlist = new ArrayList();
			ArrayList v_binnedlist = new ArrayList();
			double d0 = distances_sq[0];
			for (int i = 0; i < distances_sq.Length; i++)
			{
				int dcounter = 0;
				double val = 0;
				while ((i + dcounter) < distances_sq.Length && d0 == distances_sq[i + dcounter])
				{
					val += values[i + dcounter];
					dcounter++;
				}
				r_binnedlist.Add(d0);
				v_binnedlist.Add(val / ((double)(dcounter)));
				if ((i + dcounter) < distances_sq.Length)
					d0 = distances_sq[i + dcounter];
				i += dcounter - 1;
			}

			if (axis_scale == 0)
				axis_scale = 1;

			radial_x = new double[r_binnedlist.Count];
			radial_y = new double[r_binnedlist.Count];

			for (int q = 0; q < radial_x.Length; q++)
			{
				radial_x[q] = Math.Sqrt((double)r_binnedlist[q]) * axis_scale;
				radial_y[q] = (double)v_binnedlist[q];
			}
		}

		public static double QuadFit3PtsCenterPos(double[] x, double[] y)
		{
			double x1 = x[0];
			double x2 = x[1];
			double x3 = x[2];
			double y1 = y[0];
			double y2 = y[1];
			double y3 = y[2];

			double det = x1 * x1 * (x2 - x3) - x2 * x2 * (x1 - x3) + x3 * x3 * (x1 - x2);
			double a1 = (y1 * (x2 - x3) - y2 * (x1 - x3) + y3 * (x1 - x2)) / det;
			double b1 = (x1 * x1 * (y2 - y3) - x2 * x2 * (y1 - y3) + x3 * x3 * (y1 - y2)) / det;
			double c1 = (x1 * x1 * (x2 * y3 - x3 * y2) - x2 * x2 * (x1 * y3 - x3 * y1) + x3 * x3 * (x1 * y2 - x2 * y1)) / det;

			double X0 = -0.5 * (b1 / a1);

			return X0;
		}

		public static double[] QuadFit3PtsParams(double[] x, double[] y)
		{
			double[] result = new double[3];
			double x1 = x[0];
			double x2 = x[1];
			double x3 = x[2];
			double y1 = y[0];
			double y2 = y[1];
			double y3 = y[2];

			double det = x1 * x1 * (x2 - x3) - x2 * x2 * (x1 - x3) + x3 * x3 * (x1 - x2);
			result[0] = (y1 * (x2 - x3) - y2 * (x1 - x3) + y3 * (x1 - x2)) / det;
			result[1] = (x1 * x1 * (y2 - y3) - x2 * x2 * (y1 - y3) + x3 * x3 * (y1 - y2)) / det;
			result[2] = (x1 * x1 * (x2 * y3 - x3 * y2) - x2 * x2 * (x1 * y3 - x3 * y1) + x3 * x3 * (x1 * y2 - x2 * y1)) / det;

			return result;
		}		

		/// <summary>Smooths a data series with optional methods.</summary>
		/// <param name="data">The data to smooth.</param>
		/// <param name="kernelsize">For simple or linear must be an integer; for centered must be an odd integer; for expoenential must be greater than zero and less than or equal to 1.</param>
		/// <param name="do_parallel">Optionally perform array operations in parallel. False when parallelizing upstream.</param>
		/// <param name="method">The mathematical method to use for smoothing.</param>
		public static double[] Smooth(double[] data, double kernelsize, bool do_parallel, SmoothingMethod method)
		{
			double[] result = new double[data.Length];

			try
			{
				if (method != SmoothingMethod.Centered)
				{
					data.CopyTo(result, 0);
					alglib.xparams param = new alglib.xparams(0);
					if (do_parallel)
						param = alglib.parallel;

					if (method == SmoothingMethod.Simple)
					{
						if (!IsInteger(kernelsize))
							throw new Exception("Specified smoothing kernel size '" + kernelsize + "' not an integer.");
						alglib.filtersma(ref result, (int)kernelsize, param);
					}
					else if (method == SmoothingMethod.Linear)
					{
						if (!IsInteger(kernelsize))
							throw new Exception("Specified smoothing kernel size '" + kernelsize + "' not an integer.");
						alglib.filterlrma(ref result, (int)kernelsize, param);
					}
					else if (method == SmoothingMethod.Exponential)
					{
						if (kernelsize <= 0 || kernelsize > 1)
							throw new Exception("Specified smoothing kernel '" + kernelsize + "' not a decimal value greater than zero and less than or equal to 1.");
						alglib.filterema(ref result, kernelsize, param);
					}
				}
				else// method == "centered"
				{
					Math.DivRem((int)kernelsize, 2, out int rem);
					if (kernelsize == 1)
						return data;
					else if (rem == 0 || !IsInteger(kernelsize))
						throw new Exception("Centered smoothing kernel '" + kernelsize + "' not an odd integer.");

					int kernelHW = ((int)kernelsize - 1) / 2;
					ParallelOptions opts = new ParallelOptions();
					if (do_parallel)
						opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
					else
						opts.MaxDegreeOfParallelism = 1;

					Parallel.For(0, data.Length, opts, i =>
					{
						double val = 0;
						int kern = i - kernelHW;

						if (kern < 0)
						{
							kern = kern + kernelHW;
							for (int j = i - kern; j <= i + kern; j++)
								val += data[j];
							result[i] = val / (2 * kern + 1);
							return;// continue; //return is continue for Parallel.For methods
						}

						kern = i + kernelHW;
						if (kern > data.Length - 1)
						{
							kern = -(kern - kernelHW - data.Length + 1);
							for (int j = i - kern; j <= i + kern; j++)
								val += data[j];
							result[i] = val / (2 * kern + 1);
							return;// continue; //return is continue for Parallel.For methods
						}

						for (int j = i - kernelHW; j <= i + kernelHW; j++)
							val += data[j];
						result[i] = val / kernelsize;
					});
				}
			}
			catch (Exception e)
			{
				throw new Exception(e.Data + "	" + e.InnerException + "	" + e.Message + "	" + e.Source + "	" + e.StackTrace + "	" + e.TargetSite);
			}
			return result;
		}
		
		/// <summary>Returns an interpolation of the specified data at the given interpolation points.</summary>
		/// <param name="xdata">The x-positions of the ydata points to interpolate.</param>
		/// <param name="ydata">The y-values of the data to interpolate.</param>
		/// <param name="xinterp">The x-positions at which to interpolate y-values.</param>
		/// <param name="style">The type of interpolation to compute.</param>
		public static double[] Interpolate1d(double[] xdata, double[] ydata, double[] xinterp, InterpolationType style, bool do_parallel = false)
		{
			double[] result = new double[xinterp.Length];

			alglib.xparams param = new alglib.xparams(0);
			if (do_parallel)
				param = alglib.parallel;

			//void *x = 0;
			alglib.spline1dinterpolant sp = new alglib.spline1dinterpolant(/*x*/);

			if (style == InterpolationType.Linear)
				alglib.spline1dbuildlinear(xdata, ydata, out sp, param);
			else if (style == InterpolationType.Cublic)
				alglib.spline1dbuildcubic(xdata, ydata, out sp, param);
			else if (style == InterpolationType.Monotone)
				alglib.spline1dbuildmonotone(xdata, ydata, out sp, param);
			else if (style == InterpolationType.CatmullRom)
				alglib.spline1dbuildcatmullrom(xdata, ydata, out sp, param);
			else if (style == InterpolationType.Akima)
				alglib.spline1dbuildakima(xdata, ydata, out sp, param);

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, xinterp.Length, opts, i =>
			{
				result[i] = alglib.spline1dcalc(sp, xinterp[i]);
			});

			return result;
		}

		/// <summary>Returns an interpolation of the specified surface data at the given interpolation points with bicubic spline.</summary>
		/// <param name="xdata">The x-positions of the surface points to interpolate. If nullptr is passed a vector will automatically be created of appropriate length.</param>
		/// <param name="ydata">The y-positions of the surface points to interpolate. If nullptr is passed a vector will automatically be created of appropriate length.</param>
		/// <param name="surfdata">The surface data to interpolate.</param>
		/// <param name="xinterpdelta_inv">The inverse of the interpolation delta.  That is, &quot;10&quot; means the grid will interpolated at 1/10th grid scale.</param>
		/// <param name="yinterpdelta_inv">The inverse of the interpolation delta.  That is, &quot;10&quot; means the grid will interpolated at 1/10th grid scale.</param>
		/// <param name="xinterp">The returned interpolated xdata vector. Pass nullptr if not required. If required, must be initialized as an xdata->Length*xinterpdelta_inv length vector.</param>
		/// <param name="yinterp">The returned interpolated ydata vector. Pass nullptr if not required. If required, must be initialized as an ydata->Length*yinterpdelta_inv length vector.</param>
		public static double[,] Interpolate2d(double[] xdata, double[] ydata, double[,] surfdata, int xinterpdelta_inv, int yinterpdelta_inv, out double[] xinterp, out double[] yinterp, bool do_parallel = false)
		{
			int xw = surfdata.GetLength(0);
			int yh = surfdata.GetLength(1);
			double[,] result = new double[xw * xinterpdelta_inv, yh * yinterpdelta_inv];

			double[] surfdata_vec = new double[surfdata.Length];
			for (int j = 0; j < yh; j++)
				for (int i = 0; i < xw; i++)
					surfdata_vec[j * xw + i] = surfdata[i, j];

			if (xdata == null)
			{
				xdata = new double[xw];
				for (int i = 0; i < xw; i++)
					xdata[i] = (double)(i);
			}
			if (ydata == null)
			{
				ydata = new double[yh];
				for (int i = 0; i < yh; i++)
					ydata[i] = (double)(i);
			}

			alglib.xparams param = new alglib.xparams(0);
			if (do_parallel)
				param = alglib.parallel;

			alglib.spline2dbuildbicubicv(xdata, xw, ydata, yh, surfdata_vec, 1, out alglib.spline2dinterpolant s, param);

			double[] xinter = new double[xw * xinterpdelta_inv];
			for (int i = 0; i < xw * xinterpdelta_inv; i++)
				xinter[i] = xdata[0] + (double)(i) / (double)(xinterpdelta_inv);
			double[] yinter = new double[yh * yinterpdelta_inv];
			for (int j = 0; j < yh * yinterpdelta_inv; j++)
				yinter[j] = ydata[0] + (double)(j) / (double)(yinterpdelta_inv);

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, xw * xinterpdelta_inv, opts, i =>
			{
				for (int j = 0; j < yh * yinterpdelta_inv; j++)
					result[i, j] = alglib.spline2dcalc(s, xinter[i], yinter[j]);
			});

			xinterp = xinter;
			yinterp = yinter;
			return result;
		}

		#endregion

		#region ALGLIB FUNCTIONS		

		public static void alglib_Gauss_1d(double[] p, double[] x, ref double val, object obj)
		{
			val = p[0] * Math.Exp(-((x[0] - p[1]) * (x[0] - p[1])) / (2 * p[2] * p[2])) + p[3];
		}

		public static void alglib_Gauss_1d_grad(double[] p, double[] x, ref double val, double[] grad, object obj)
		{
			val = p[0] * Math.Exp(-((x[0] - p[1]) * (x[0] - p[1])) / (2 * p[2] * p[2])) + p[3];
			grad[0] = Math.Exp(-((x[0] - p[1]) * (x[0] - p[1])) / (2 * p[2] * p[2]));
			grad[1] = -(p[0] * Math.Exp(-(p[1] - x[0]) * (p[1] - x[0]) / (2 * p[2] * p[2])) * (2 * p[1] - 2 * x[0])) / (2 * p[2] * p[2]);
			grad[2] = (p[0] * Math.Exp(-(p[1] - x[0]) * (p[1] - x[0]) / (2 * p[2] * p[2])) * (p[1] - x[0]) * (p[1] - x[0])) / (p[2] * p[2] * p[2]);
			grad[3] = 1;
		}

		/// <summary>Calculates a single point of a 2-d Gaussian surface G(x,y|p)
		/// <br />G(x,y|p) = p(0)*exp(-((x-p(1))^2 + (y - p(2))^2)/(2*p(3)^2)) + p(4)
		/// <br />or
		/// <br />G(x,y|p) = p(0)*exp(-((x-p(1))*cos(p(3)) + (y-p(2))*sin(p(3)))^2 / (2*p(4)^2) - (-(x-p(1))*sin(p(3)) + (y-p(2))*cos(p(3))).^2 / (2*p(5)^2) ) + p(6)
		/// <br />where x[0] is a position on X-axis x, and x[1] is a position on Y-axis y.
		/// <br />The form of G(x,y|p) used is determined by the length of the parmater vector p</summary>
		/// <param name="p">The initial parameters of the Gaussian fit.  Options are:
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = sigma; p[4] = bias
		/// <br />or
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = theta; p[4] = x-sigma; p[5] = y-sigma; p[6] = bias</param>
		/// <param name="x">The x,y position to calculate the value val of the Gaussian G(x,y|p): x[0] = x, x[1] = y</param>
		/// <param name="val">The calculated value of the Gaussian.</param>
		/// <param name="obj">obj.</param>
		public static void alglib_Gauss_2d(double[] p, double[] x, ref double val, object obj)
		{
			if (p.Length == 5)
				val = p[0] * Math.Exp(-((x[0] - p[1]) * (x[0] - p[1]) + (x[1] - p[2]) * (x[1] - p[2])) / (2 * p[3] * p[3])) + p[4];
			if (p.Length == 7)
				val = p[0] * Math.Exp(-Math.Pow((x[0] - p[1]) * Math.Cos(p[3]) + (x[1] - p[2]) * Math.Sin(p[3]), 2) / (2 * p[4] * p[4]) - Math.Pow(-(x[0] - p[1]) * Math.Sin(p[3]) + (x[1] - p[2]) * Math.Cos(p[3]), 2) / (2 * p[5] * p[5])) + p[6];
		}

		public static void alglib_Gauss_2d_grad(double[] p, double[] x, ref double val, double[] grad, object obj)
		{
			if (p.Length == 5)
			{
				val = p[0] * Math.Exp(-((x[0] - p[1]) * (x[0] - p[1]) + (x[1] - p[2]) * (x[1] - p[2])) / (2 * p[3] * p[3])) + p[4];
				grad[0] = Math.Exp(-((p[1] - x[0]) * (p[1] - x[0]) + (p[2] - x[1]) * (p[2] - x[1])) / (2 * p[3] * p[3]));
				grad[1] = -(p[0] * Math.Exp(-((p[1] - x[0]) * (p[1] - x[0]) + (p[2] - x[1]) * (p[2] - x[1])) / (2 * p[3] * p[3])) * (2 * p[1] - 2 * x[0])) / (2 * p[3] * p[3]);
				grad[2] = -(p[0] * Math.Exp(-((p[1] - x[0]) * (p[1] - x[0]) + (p[2] - x[1]) * (p[2] - x[1])) / (2 * p[3] * p[3])) * (2 * p[2] - 2 * x[1])) / (2 * p[3] * p[3]);
				grad[3] = (p[0] * Math.Exp(-((p[1] - x[0]) * (p[1] - x[0]) + (p[2] - x[1]) * (p[2] - x[1])) / (2 * p[3] * p[3])) * ((p[1] - x[0]) * (p[1] - x[0]) + (p[2] - x[1]) * (p[2] - x[1]))) / (p[3] * p[3] * p[3]);
				grad[4] = 1;
			}
			if (p.Length == 7)
			{
				val = p[0] * Math.Exp(-Math.Pow((x[0] - p[1]) * Math.Cos(p[3]) + (x[1] - p[2]) * Math.Sin(p[3]), 2) / (2 * p[4] * p[4]) - Math.Pow(-(x[0] - p[1]) * Math.Sin(p[3]) + (x[1] - p[2]) * Math.Cos(p[3]), 2) / (2 * p[5] * p[5])) + p[6];
				grad[0] = Math.Exp(-Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (2 * p[4] * p[4]) - Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (2 * p[5] * p[5]));
				grad[1] = -p[0] * Math.Exp(-Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (2 * p[4] * p[4]) - Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (2 * p[5] * p[5])) * ((Math.Cos(p[3]) * (Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]))) / (p[4] * p[4]) - (Math.Sin(p[3]) * (Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]))) / (p[5] * p[5]));
				grad[2] = -p[0] * Math.Exp(-Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (2 * p[4] * p[4]) - Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (2 * p[5] * p[5])) * ((Math.Cos(p[3]) * (Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]))) / (p[5] * p[5]) + (Math.Sin(p[3]) * (Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]))) / (p[4] * p[4]));
				grad[3] = -p[0] * Math.Exp(-Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (2 * p[4] * p[4]) - Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (2 * p[5] * p[5])) * (((Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1])) * (Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]))) / (p[4] * p[4]) - ((Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1])) * (Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]))) / (p[5] * p[5]));
				grad[4] = (p[0] * Math.Exp(-Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (2 * p[4] * p[4]) - Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (2 * p[5] * p[5])) * Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2)) / (p[4] * p[4] * p[4]);
				grad[5] = (p[0] * Math.Exp(-Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (2 * p[4] * p[4]) - Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (2 * p[5] * p[5])) * Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2)) / (p[5] * p[5] * p[5]);
				grad[6] = 1;
			}
		}

		public static void alglib_Gauss_2d_LM_LS_grad(double[] p, ref double f, double[] grad, object obj)
		{
			f = 0;
			for (int i = 0; i < grad.Length; i++)
				grad[i] = 0;
			double[,] Z = (double[,])(((object[])obj)[0]);
			double[] x = (double[])(((object[])obj)[1]);
			double[] y = (double[])(((object[])obj)[2]);

			if (p.Length == 5)
			{
				double p3sq = p[3] * p[3];
				double twop3sq = 2 * p3sq;
				double p3cu = p3sq * p[3];
				double twop1 = 2 * p[1];
				double twop2 = 2 * p[2];
				double dx, dxsq, dy, dxsqpdysq, expres, exparg, p0expres, a;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					dxsq = dx * dx;

					for (int j = 0; j < y.Length; j++)
					{
						dy = p[2] - y[j];
						dxsqpdysq = dxsq + dy * dy;
						exparg = -dxsqpdysq / twop3sq;
						expres = Math.Exp(exparg);
						p0expres = p[0] * expres;
						a = p[4] - Z[i, j] + p0expres;

						f += a * a;

						grad[0] += 2 * expres * a;
						grad[1] += -(p0expres * (twop1 - 2 * x[i]) * a) / p3sq;
						grad[2] += -(p0expres * (twop2 - 2 * y[j]) * a) / p3sq;
						grad[3] += (2 * p0expres * dxsqpdysq * a) / p3cu;
						grad[4] += 2 * a;
					}
				}
				return;
			}

			if (p.Length == 7)
			{
				double cosp3 = Math.Cos(p[3]);
				double sinp3 = Math.Sin(p[3]);
				double p4sq = p[4] * p[4];
				double twop4sq = 2 * p4sq;
				double p5sq = p[5] * p[5];
				double twop5sq = 2 * p5sq;
				double p4cu = p4sq * p[4];
				double p5cu = p5sq * p[5];
				double dx, cosp3dx, sinp3dx, dy, sinp3dy, a, cosp3dy, b, c, exparg, asq, bsq, expexparg, p0expexparg, twop0expexparg;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					cosp3dx = cosp3 * dx;
					sinp3dx = sinp3 * dx;

					for (int j = 0; j < y.Length; j++)
					{
						dy = p[2] - y[j];
						sinp3dy = sinp3 * dy;
						a = cosp3dx + sinp3dy;
						cosp3dy = cosp3 * dy;
						b = cosp3dy - sinp3dx;
						exparg = -a * a / twop4sq - b * b / twop5sq;
						asq = a * a;
						bsq = b * b;
						expexparg = Math.Exp(exparg);
						p0expexparg = p[0] * expexparg;
						c = p[6] - Z[i, j] + p0expexparg;
						twop0expexparg = 2 * p0expexparg;

						f += c * c;

						grad[0] += 2 * expexparg * c;
						grad[1] += -twop0expexparg * ((cosp3 * a) / p4sq - (sinp3 * b) / p5sq) * c;
						grad[2] += -twop0expexparg * ((cosp3 * b) / p5sq + (sinp3 * a) / p4sq) * c;
						grad[3] += -twop0expexparg * ((a * b) / p4sq - (a * b) / p5sq) * c;
						grad[4] += (twop0expexparg * asq * c) / p4cu;
						grad[5] += (twop0expexparg * bsq * c) / p5cu;
						grad[6] += 2 * p[6] - 2 * Z[i, j] + twop0expexparg;
					}
				}
			}
		}

		public static void alglib_Gauss_2d_LM_LS_grad_compound(double[] p, ref double f, double[] grad, object obj)
		{
			f = 0;
			for (int i = 0; i < grad.Length; i++)
				grad[i] = 0;
			double[] g = new double[grad.Length];
			double[,] Z = (double[,])(((object[])obj)[0]);
			double[] x = (double[])(((object[])obj)[1]);
			double[] y = (double[])(((object[])obj)[2]);
			int func = ((int[])(((object[])obj)[3]))[0];
			int count = ((int[])(((object[])obj)[3]))[1];
			int pindex;

			if (func == 5)
			{
				double[] p3sq = new double[count];// = p[3] * p[3];
				double[] twop3sq = new double[count];// = 2 * p3sq;
				double[] p3cu = new double[count];// = p3sq * p[3];
				double[] twop1 = new double[count]; // = 2 * p[1];
				double[] twop2 = new double[count];// = 2 * p[2];
				for (int k = 0; k < count; k++)
				{
					pindex = k * (func - 1);
					p3sq[k] = p[pindex + 3] * p[pindex + 3];
					twop3sq[k] = 2 * p3sq[k];
					p3cu[k] = p3sq[k] * p[pindex + 3];
					twop1[k] = 2 * p[pindex + 1];
					twop2[k] = 2 * p[pindex + 2];
				}

				double dy, dxsqpdysq, expres, exparg, p0expres, a;
				double[] dxsq = new double[count];

				for (int i = 0; i < x.Length; i++)
				{
					for (int k = 0; k < count; k++)
					{
						pindex = k * (func - 1);

						dxsq[k] = p[pindex + 1] - x[i];
						dxsq[k] *= dxsq[k];
					}

					for (int j = 0; j < y.Length; j++)
					{
						p0expres = 0;
						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							dy = p[pindex + 2] - y[j];
							dxsqpdysq = dxsq[k] + dy * dy;
							exparg = -dxsqpdysq / twop3sq[k];
							expres = Math.Exp(exparg);

							p0expres += p[pindex + 0] * expres;
							g[pindex + 0] = expres;
							g[pindex + 1] = -((twop1[k] - 2 * x[i])) / p3sq[k] * p[pindex + 0] * expres;
							g[pindex + 2] = -((twop2[k] - 2 * y[j])) / p3sq[k] * p[pindex + 0] * expres;
							g[pindex + 3] = (dxsqpdysq) / p3cu[k] * p[pindex + 0] * expres;
						}

						a = p[p.Length - 1] - Z[i, j] + p0expres;

						f += a * a;
						grad[grad.Length - 1] += 2 * a;
						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							grad[pindex + 0] += 2 * g[pindex + 0] * a;
							grad[pindex + 1] += g[pindex + 1] * a;
							grad[pindex + 2] += g[pindex + 2] * a;
							grad[pindex + 3] += 2 * g[pindex + 3] * a;
						}
					}
				}
				return;
			}

			if (func == 7)
			{
				double[] cosp3 = new double[count];// = Math.Cos(p[3]);
				double[] sinp3 = new double[count];// = Math.Sin(p[3]);
				double[] p4sq = new double[count];// = p[4] * p[4];
				double[] twop4sq = new double[count];// = 2 * p4sq;
				double[] p5sq = new double[count];// = p[5] * p[5];
				double[] twop5sq = new double[count];// = 2 * p5sq;
				double[] p4cu = new double[count];// = p4sq * p[4];
				double[] p5cu = new double[count];// = p5sq * p[5];
				for (int k = 0; k < count; k++)
				{
					pindex = k * (func - 1);
					cosp3[k] = Math.Cos(p[pindex + 3]);
					sinp3[k] = Math.Sin(p[pindex + 3]);
					p4sq[k] = p[pindex + 4] * p[pindex + 4];
					twop4sq[k] = 2 * p4sq[k];
					p5sq[k] = p[pindex + 5] * p[pindex + 5];
					twop5sq[k] = 2 * p5sq[k];
					p4cu[k] = p4sq[k] * p[pindex + 4];
					p5cu[k] = p5sq[k] * p[pindex + 5];
				}

				double dx, cosp3dx, sinp3dx, dy, sinp3dy, a, cosp3dy, b, c, exparg, asq, bsq, expexparg, p0expexparg, twop0expexparg;

				for (int i = 0; i < x.Length; i++)
				{
					for (int j = 0; j < y.Length; j++)
					{
						p0expexparg = 0;
						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							dx = p[pindex + 1] - x[i];
							cosp3dx = cosp3[k] * dx;
							sinp3dx = sinp3[k] * dx;
							dy = p[pindex + 2] - y[j];
							sinp3dy = sinp3[k] * dy;
							a = cosp3dx + sinp3dy;
							cosp3dy = cosp3[k] * dy;
							b = cosp3dy - sinp3dx;
							exparg = -a * a / twop4sq[k] - b * b / twop5sq[k];
							asq = a * a;
							bsq = b * b;
							expexparg = Math.Exp(exparg);
							p0expexparg += p[pindex + 0] * expexparg;

							g[pindex + 0] = 2 * expexparg;
							g[pindex + 1] = ((cosp3[k] * a) / p4sq[k] - (sinp3[k] * b) / p5sq[k]) * 2 * p[pindex + 0] * expexparg;
							g[pindex + 2] = ((cosp3[k] * b) / p5sq[k] + (sinp3[k] * a) / p4sq[k]) * 2 * p[pindex + 0] * expexparg;
							g[pindex + 3] = ((a * b) / p4sq[k] - (a * b) / p5sq[k]) * 2 * p[pindex + 0] * expexparg;
							g[pindex + 4] = (asq) / p4cu[k] * 2 * p[pindex + 0] * expexparg;
							g[pindex + 5] = (bsq) / p5cu[k] * 2 * p[pindex + 0] * expexparg;
						}

						twop0expexparg = 2 * p0expexparg;
						c = p[p.Length - 1] - Z[i, j] + p0expexparg;

						f += c * c;
						grad[grad.Length - 1] += 2 * c;

						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							grad[pindex + 0] += g[pindex + 0] * c;
							grad[pindex + 1] += -g[pindex + 1] * c;
							grad[pindex + 2] += -g[pindex + 2] * c;
							grad[pindex + 3] += -g[pindex + 3] * c;
							grad[pindex + 4] += (c) * g[pindex + 4];
							grad[pindex + 5] += (c) * g[pindex + 5];
						}
					}
				}
			}
		}

		public static void alglib_Gauss_2d_LM_LS_CHISQ_grad(double[] p, ref double f, double[] grad, object obj)
		{
			f = 0;
			for (int i = 0; i < grad.Length; i++)
				grad[i] = 0;
			double[,] Z = (double[,])(((object[])obj)[0]);
			double[] x = (double[])(((object[])obj)[1]);
			double[] y = (double[])(((object[])obj)[2]);
			double den;

			if (p.Length == 5)
			{
				double p3sq = p[3] * p[3];
				double p3cu = p[3] * p3sq;
				double twop3sq = 2 * p3sq;
				double twop1 = 2 * p[1];
				double twop2 = 2 * p[2];

				for (int i = 0; i < x.Length; i++)
				{
					double dx = p[1] - x[i];
					double dxsq = dx * dx;
					double twoxi = 2 * x[i];

					for (int j = 0; j < y.Length; j++)
					{
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Z[i, j];

						double dy = p[2] - y[j];
						double dysq = dy * dy;
						double exparg = (dxsq + dysq) / twop3sq;
						double expmexparg = Math.Exp(-exparg);
						double p0expmexparg = p[0] * expmexparg;
						double a = p[4] - Z[i, j] + p0expmexparg;

						f += a * a / den;

						grad[0] += (2 * expmexparg * a) / den;
						grad[1] += -(p0expmexparg * (twop1 - twoxi) * a) / (den * p3sq);
						grad[2] += -(p0expmexparg * (twop2 - 2 * y[j]) * a) / (den * p3sq);
						grad[3] += (2 * p0expmexparg * (dxsq + dysq) * a) / (den * p3cu);
						grad[4] += (2 * p[4] - 2 * Z[i, j] + 2 * p0expmexparg) / den;
					}
				}
				return;
			}

			if (p.Length == 7)
			{
				double cosp3 = Math.Cos(p[3]);
				double sinp3 = Math.Sin(p[3]);
				double p4sq = p[4] * p[4];
				double p5sq = p[5] * p[5];
				double twop4sq = 2 * p4sq;
				double twop5sq = 2 * p5sq;
				double p4cu = p4sq * p[4];
				double p5cu = p5sq * p[5];
				double twop6 = 2 * p[6];
				double dx, cosp3dx, dy, sinp3dy, cosp3dxsinp3dy, cosp3dxsinp3dysq, cosp3dysinp3dx, cosp3dysinp3dxsq, exparg, expres, a, p0expres, twop0expres;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					cosp3dx = cosp3 * dx;

					for (int j = 0; j < y.Length; j++)
					{
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Z[i, j];

						dy = p[2] - y[j];
						sinp3dy = sinp3 * dy;
						cosp3dxsinp3dy = cosp3dx + sinp3dy;
						cosp3dxsinp3dysq = cosp3dxsinp3dy * cosp3dxsinp3dy;
						cosp3dysinp3dx = cosp3 * dy - sinp3 * dx;
						cosp3dysinp3dxsq = cosp3dysinp3dx * cosp3dysinp3dx;
						exparg = -cosp3dxsinp3dysq / twop4sq - cosp3dysinp3dxsq / twop5sq;
						expres = Math.Exp(exparg);
						a = p[6] - Z[i, j] + p[0] * expres;
						p0expres = p[0] * expres;
						twop0expres = 2 * p0expres;

						f += a * a / den;

						grad[0] += (2 * expres * a) / den;
						grad[1] += -(twop0expres * ((cosp3 * cosp3dxsinp3dy) / p4sq - (sinp3 * cosp3dysinp3dx) / p5sq) * a) / den;
						grad[2] += -(twop0expres * ((cosp3 * cosp3dysinp3dx) / p5sq + (sinp3 * cosp3dxsinp3dy) / p4sq) * a) / den;
						grad[3] += -(twop0expres * ((cosp3dxsinp3dy * cosp3dysinp3dx) / p4sq - (cosp3dxsinp3dy * cosp3dysinp3dx) / p5sq) * a) / den;
						grad[4] += (twop0expres * cosp3dxsinp3dysq * a) / (den * p4cu);
						grad[5] += (twop0expres * cosp3dysinp3dxsq * a) / (den * p5cu);
						grad[6] += (twop6 - 2 * Z[i, j] + twop0expres) / den;
					}
				}
			}
		}

		public static void alglib_Gauss_2d_LM_LS_CHISQ_grad_compound(double[] p, ref double f, double[] grad, object obj)
		{
			f = 0;
			for (int i = 0; i < grad.Length; i++)
				grad[i] = 0;
			double[] g = new double[grad.Length];
			double[,] Z = (double[,])(((object[])obj)[0]);
			double[] x = (double[])(((object[])obj)[1]);
			double[] y = (double[])(((object[])obj)[2]);
			int func = ((int[])(((object[])obj)[3]))[0];
			int count = ((int[])(((object[])obj)[3]))[1];
			int pindex;

			if (func == 5)
			{
				double[] p3sq = new double[count];// = p[3] * p[3];
				double[] twop3sq = new double[count];// = 2 * p3sq;
				double[] p3cu = new double[count];// = p3sq * p[3];
				double[] twop1 = new double[count]; // = 2 * p[1];
				double[] twop2 = new double[count];// = 2 * p[2];
				for (int k = 0; k < count; k++)
				{
					pindex = k * (func - 1);
					p3sq[k] = p[pindex + 3] * p[pindex + 3];
					twop3sq[k] = 2 * p3sq[k];
					p3cu[k] = p3sq[k] * p[pindex + 3];
					twop1[k] = 2 * p[pindex + 1];
					twop2[k] = 2 * p[pindex + 2];
				}

				double dy, dxsqpdysq, expres, exparg, p0expres, a, den;
				double[] dxsq = new double[count];

				for (int i = 0; i < x.Length; i++)
				{
					for (int k = 0; k < count; k++)
					{
						pindex = k * (func - 1);

						dxsq[k] = p[pindex + 1] - x[i];
						dxsq[k] *= dxsq[k];
					}

					for (int j = 0; j < y.Length; j++)
					{
						p0expres = 0;
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Z[i, j];

						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							dy = p[pindex + 2] - y[j];
							dxsqpdysq = dxsq[k] + dy * dy;
							exparg = -dxsqpdysq / twop3sq[k];
							expres = Math.Exp(exparg);

							p0expres += p[pindex + 0] * expres;
							g[pindex + 0] = expres;
							g[pindex + 1] = -((twop1[k] - 2 * x[i])) / p3sq[k] * p[pindex + 0] * expres;
							g[pindex + 2] = -((twop2[k] - 2 * y[j])) / p3sq[k] * p[pindex + 0] * expres;
							g[pindex + 3] = (dxsqpdysq) / p3cu[k] * p[pindex + 0] * expres;
						}

						a = p[p.Length - 1] - Z[i, j] + p0expres;

						f += a * a / den;
						grad[grad.Length - 1] += 2 * a / den;
						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							grad[pindex + 0] += 2 * g[pindex + 0] * a / den;
							grad[pindex + 1] += g[pindex + 1] * a / den;
							grad[pindex + 2] += g[pindex + 2] * a / den;
							grad[pindex + 3] += 2 * g[pindex + 3] * a / den;
						}
					}
				}
				return;
			}

			if (func == 7)
			{
				double[] cosp3 = new double[count];// = Math.Cos(p[3]);
				double[] sinp3 = new double[count];// = Math.Sin(p[3]);
				double[] p4sq = new double[count];// = p[4] * p[4];
				double[] twop4sq = new double[count];// = 2 * p4sq;
				double[] p5sq = new double[count];// = p[5] * p[5];
				double[] twop5sq = new double[count];// = 2 * p5sq;
				double[] p4cu = new double[count];// = p4sq * p[4];
				double[] p5cu = new double[count];// = p5sq * p[5];
				for (int k = 0; k < count; k++)
				{
					pindex = k * (func - 1);
					cosp3[k] = Math.Cos(p[pindex + 3]);
					sinp3[k] = Math.Sin(p[pindex + 3]);
					p4sq[k] = p[pindex + 4] * p[pindex + 4];
					twop4sq[k] = 2 * p4sq[k];
					p5sq[k] = p[pindex + 5] * p[pindex + 5];
					twop5sq[k] = 2 * p5sq[k];
					p4cu[k] = p4sq[k] * p[pindex + 4];
					p5cu[k] = p5sq[k] * p[pindex + 5];
				}

				double dx, cosp3dx, sinp3dx, dy, sinp3dy, a, cosp3dy, b, c, exparg, asq, bsq, expexparg, p0expexparg, twop0expexparg, den;

				for (int i = 0; i < x.Length; i++)
				{
					for (int j = 0; j < y.Length; j++)
					{
						p0expexparg = 0;
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Z[i, j];

						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							dx = p[pindex + 1] - x[i];
							cosp3dx = cosp3[k] * dx;
							sinp3dx = sinp3[k] * dx;
							dy = p[pindex + 2] - y[j];
							sinp3dy = sinp3[k] * dy;
							a = cosp3dx + sinp3dy;
							cosp3dy = cosp3[k] * dy;
							b = cosp3dy - sinp3dx;
							exparg = -a * a / twop4sq[k] - b * b / twop5sq[k];
							asq = a * a;
							bsq = b * b;
							expexparg = Math.Exp(exparg);
							p0expexparg += p[pindex + 0] * expexparg;

							g[pindex + 0] = 2 * expexparg;
							g[pindex + 1] = ((cosp3[k] * a) / p4sq[k] - (sinp3[k] * b) / p5sq[k]) * 2 * p[pindex + 0] * expexparg;
							g[pindex + 2] = ((cosp3[k] * b) / p5sq[k] + (sinp3[k] * a) / p4sq[k]) * 2 * p[pindex + 0] * expexparg;
							g[pindex + 3] = ((a * b) / p4sq[k] - (a * b) / p5sq[k]) * 2 * p[pindex + 0] * expexparg;
							g[pindex + 4] = (asq) / p4cu[k] * 2 * p[pindex + 0] * expexparg;
							g[pindex + 5] = (bsq) / p5cu[k] * 2 * p[pindex + 0] * expexparg;
						}

						twop0expexparg = 2 * p0expexparg;
						c = p[p.Length - 1] - Z[i, j] + p0expexparg;

						f += c * c / den;
						grad[grad.Length - 1] += 2 * c / den;

						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							grad[pindex + 0] += g[pindex + 0] * c / den;
							grad[pindex + 1] += -g[pindex + 1] * c / den;
							grad[pindex + 2] += -g[pindex + 2] * c / den;
							grad[pindex + 3] += -g[pindex + 3] * c / den;
							grad[pindex + 4] += (c) * g[pindex + 4] / den;
							grad[pindex + 5] += (c) * g[pindex + 5] / den;
						}
					}
				}
			}
		}

		public static void alglib_Gauss_2d_LM_LS_ROBUST_grad(double[] p, ref double f, double[] grad, object obj)
		{
			f = 0;
			for (int i = 0; i < grad.Length; i++)
				grad[i] = 0;
			double[,] Z = (double[,])(((object[])obj)[0]);
			double[] x = (double[])(((object[])obj)[1]);
			double[] y = (double[])(((object[])obj)[2]);
			double den;

			if (p.Length == 5)
			{
				double p3sq = p[3] * p[3];
				double twop3sq = 2 * p3sq;
				double p3cu = p3sq * p[3];
				double twop1 = 2 * p[1];
				double twop2 = 2 * p[2];
				double dx, dxsq, dy, dysq, dxsqdysq, exparg, expres, p0expres, a;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					dxsq = dx * dx;

					for (int j = 0; j < y.Length; j++)
					{
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Math.Sqrt(Z[i, j]);

						dy = p[2] - y[j];
						dysq = dy * dy;
						dxsqdysq = dxsq + dysq;
						exparg = -dxsqdysq / twop3sq;
						expres = Math.Exp(exparg);
						p0expres = p[0] * expres;
						a = p[4] - Z[i, j] + p0expres;

						f += a * a / den;

						grad[0] += (2 * expres * a) / den;
						grad[1] += -(p0expres * (twop1 - 2 * x[i]) * a) / (den * p3sq);
						grad[2] += -(p0expres * (twop2 - 2 * y[j]) * a) / (den * p3sq);
						grad[3] += (2 * p0expres * dxsqdysq * a) / (den * p3cu);
						grad[4] += (2 * p[4] - 2 * Z[i, j] + 2 * p0expres) / den;
					}
				}
				return;
			}

			if (p.Length == 7)
			{
				double cosp3 = Math.Cos(p[3]);
				double sinp3 = Math.Sin(p[3]);
				double p4sq = p[4] * p[4];
				double twop4sq = 2 * p4sq;
				double p5sq = p[5] * p[5];
				double twop5sq = 2 * p5sq;
				double p4cu = p[4] * p4sq;
				double p5cu = p[5] * p5sq;
				double dx, cosp3dx, dy, sinp3dy, a, b, asq, bsq, exparg, expres, c, p0expres, twop0expres;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					cosp3dx = cosp3 * dx;

					for (int j = 0; j < y.Length; j++)
					{
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Math.Sqrt(Z[i, j]);

						dy = p[2] - y[j];
						sinp3dy = sinp3 * dy;
						a = cosp3dx + sinp3dy;
						b = cosp3 * dy - sinp3 * dx;
						asq = a * a;
						bsq = b * b;
						exparg = -asq / twop4sq - bsq / twop5sq;
						expres = Math.Exp(exparg);
						c = p[6] - Z[i, j] + p[0] * expres;
						p0expres = p[0] * expres;
						twop0expres = 2 * p0expres;

						f += c * c / den;

						grad[0] += (2 * expres * c) / den;
						grad[1] += -(twop0expres * ((cosp3 * a) / p4sq - (sinp3 * b) / p5sq) * c) / den;
						grad[2] += -(twop0expres * ((cosp3 * b) / p5sq + (sinp3 * a) / p4sq) * c) / den;
						grad[3] += -(twop0expres * ((a * b) / p4sq - (a * b) / p5sq) * c) / den;
						grad[4] += (twop0expres * asq * c) / (den * p4cu);
						grad[5] += (twop0expres * bsq * c) / (den * p5cu);
						grad[6] += (2 * p[6] - 2 * Z[i, j] + twop0expres) / den;
					}
				}
			}
		}

		public static void alglib_Gauss_2d_LM_LS_ROBUST_grad_compound(double[] p, ref double f, double[] grad, object obj)
		{
			f = 0;
			for (int i = 0; i < grad.Length; i++)
				grad[i] = 0;
			double[] g = new double[grad.Length];
			double[,] Z = (double[,])(((object[])obj)[0]);
			double[] x = (double[])(((object[])obj)[1]);
			double[] y = (double[])(((object[])obj)[2]);
			int func = ((int[])(((object[])obj)[3]))[0];
			int count = ((int[])(((object[])obj)[3]))[1];
			int pindex;

			if (func == 5)
			{
				double[] p3sq = new double[count];// = p[3] * p[3];
				double[] twop3sq = new double[count];// = 2 * p3sq;
				double[] p3cu = new double[count];// = p3sq * p[3];
				double[] twop1 = new double[count]; // = 2 * p[1];
				double[] twop2 = new double[count];// = 2 * p[2];
				for (int k = 0; k < count; k++)
				{
					pindex = k * (func - 1);
					p3sq[k] = p[pindex + 3] * p[pindex + 3];
					twop3sq[k] = 2 * p3sq[k];
					p3cu[k] = p3sq[k] * p[pindex + 3];
					twop1[k] = 2 * p[pindex + 1];
					twop2[k] = 2 * p[pindex + 2];
				}

				double dy, dxsqpdysq, expres, exparg, p0expres, a, den;
				double[] dxsq = new double[count];

				for (int i = 0; i < x.Length; i++)
				{
					for (int k = 0; k < count; k++)
					{
						pindex = k * (func - 1);

						dxsq[k] = p[pindex + 1] - x[i];
						dxsq[k] *= dxsq[k];
					}

					for (int j = 0; j < y.Length; j++)
					{
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Math.Sqrt(Z[i, j]);

						p0expres = 0;
						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							dy = p[pindex + 2] - y[j];
							dxsqpdysq = dxsq[k] + dy * dy;
							exparg = -dxsqpdysq / twop3sq[k];
							expres = Math.Exp(exparg);

							p0expres += p[pindex + 0] * expres;
							g[pindex + 0] = expres;
							g[pindex + 1] = -((twop1[k] - 2 * x[i])) / p3sq[k] * p[pindex + 0] * expres;
							g[pindex + 2] = -((twop2[k] - 2 * y[j])) / p3sq[k] * p[pindex + 0] * expres;
							g[pindex + 3] = (dxsqpdysq) / p3cu[k] * p[pindex + 0] * expres;
						}

						a = p[p.Length - 1] - Z[i, j] + p0expres;

						f += a * a / den;
						grad[grad.Length - 1] += 2 * a / den;
						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							grad[pindex + 0] += 2 * g[pindex + 0] * a / den;
							grad[pindex + 1] += g[pindex + 1] * a / den;
							grad[pindex + 2] += g[pindex + 2] * a / den;
							grad[pindex + 3] += 2 * g[pindex + 3] * a / den;
						}
					}
				}
				return;
			}

			if (func == 7)
			{
				double[] cosp3 = new double[count];// = Math.Cos(p[3]);
				double[] sinp3 = new double[count];// = Math.Sin(p[3]);
				double[] p4sq = new double[count];// = p[4] * p[4];
				double[] twop4sq = new double[count];// = 2 * p4sq;
				double[] p5sq = new double[count];// = p[5] * p[5];
				double[] twop5sq = new double[count];// = 2 * p5sq;
				double[] p4cu = new double[count];// = p4sq * p[4];
				double[] p5cu = new double[count];// = p5sq * p[5];
				for (int k = 0; k < count; k++)
				{
					pindex = k * (func - 1);
					cosp3[k] = Math.Cos(p[pindex + 3]);
					sinp3[k] = Math.Sin(p[pindex + 3]);
					p4sq[k] = p[pindex + 4] * p[pindex + 4];
					twop4sq[k] = 2 * p4sq[k];
					p5sq[k] = p[pindex + 5] * p[pindex + 5];
					twop5sq[k] = 2 * p5sq[k];
					p4cu[k] = p4sq[k] * p[pindex + 4];
					p5cu[k] = p5sq[k] * p[pindex + 5];
				}

				double dx, cosp3dx, sinp3dx, dy, sinp3dy, a, cosp3dy, b, c, exparg, asq, bsq, expexparg, p0expexparg, twop0expexparg, den;

				for (int i = 0; i < x.Length; i++)
				{
					for (int j = 0; j < y.Length; j++)
					{
						p0expexparg = 0;
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Math.Sqrt(Z[i, j]);

						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							dx = p[pindex + 1] - x[i];
							cosp3dx = cosp3[k] * dx;
							sinp3dx = sinp3[k] * dx;
							dy = p[pindex + 2] - y[j];
							sinp3dy = sinp3[k] * dy;
							a = cosp3dx + sinp3dy;
							cosp3dy = cosp3[k] * dy;
							b = cosp3dy - sinp3dx;
							exparg = -a * a / twop4sq[k] - b * b / twop5sq[k];
							asq = a * a;
							bsq = b * b;
							expexparg = Math.Exp(exparg);
							p0expexparg += p[pindex + 0] * expexparg;

							g[pindex + 0] = 2 * expexparg;
							g[pindex + 1] = ((cosp3[k] * a) / p4sq[k] - (sinp3[k] * b) / p5sq[k]) * 2 * p[pindex + 0] * expexparg;
							g[pindex + 2] = ((cosp3[k] * b) / p5sq[k] + (sinp3[k] * a) / p4sq[k]) * 2 * p[pindex + 0] * expexparg;
							g[pindex + 3] = ((a * b) / p4sq[k] - (a * b) / p5sq[k]) * 2 * p[pindex + 0] * expexparg;
							g[pindex + 4] = (asq) / p4cu[k] * 2 * p[pindex + 0] * expexparg;
							g[pindex + 5] = (bsq) / p5cu[k] * 2 * p[pindex + 0] * expexparg;
						}

						twop0expexparg = 2 * p0expexparg;
						c = p[p.Length - 1] - Z[i, j] + p0expexparg;

						f += c * c / den;
						grad[grad.Length - 1] += 2 * c / den;

						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							grad[pindex + 0] += g[pindex + 0] * c / den;
							grad[pindex + 1] += -g[pindex + 1] * c / den;
							grad[pindex + 2] += -g[pindex + 2] * c / den;
							grad[pindex + 3] += -g[pindex + 3] * c / den;
							grad[pindex + 4] += (c) * g[pindex + 4] / den;
							grad[pindex + 5] += (c) * g[pindex + 5] / den;
						}
					}
				}
			}
		}

		public static void alglib_Gauss_2d_LM_LS_CSTAT_grad(double[] p, ref double f, double[] grad, object obj)
		{
			f = 0;
			for (int i = 0; i < grad.Length; i++)
				grad[i] = 0;
			double[,] Z = (double[,])(((object[])obj)[0]);
			double[] x = (double[])(((object[])obj)[1]);
			double[] y = (double[])(((object[])obj)[2]);

			if (p.Length == 5)
			{
				double p3sq = p[3] * p[3];
				double twop3sq = 2 * p3sq;
				double p3cu = p3sq * p[3];
				double twop1 = 2 * p[1];
				double twop2 = 2 * p[2];
				double dx, dxsq, twoxi, dy, dysq, dxsqdysq, exparg, expres, p0expres, a;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					dxsq = dx * dx;
					twoxi = 2 * x[i];

					for (int j = 0; j < y.Length; j++)
					{
						dy = p[2] - y[j];
						dysq = dy * dy;
						dxsqdysq = dxsq + dysq;
						exparg = -dxsqdysq / twop3sq;
						expres = Math.Exp(exparg);
						p0expres = p[0] * expres;
						a = p[4] + p0expres;

						f += 2 * p[4] - 2 * Z[i, j] * Math.Log(a) + 2 * p0expres;

						grad[0] += 2 * expres - (2 * Z[i, j] * expres) / (a);
						grad[1] += (Z[i, j] * p0expres * (twop1 - twoxi)) / (p3sq * (a)) - (p0expres * (twop1 - twoxi)) / p3sq;
						grad[2] += (Z[i, j] * p0expres * (twop2 - 2 * y[j])) / (p3sq * (a)) - (p0expres * (twop2 - 2 * y[j])) / p3sq;
						grad[3] += (2 * p0expres * dxsqdysq) / p3cu - (2 * Z[i, j] * p0expres * dxsqdysq) / (p3cu * (a));
						grad[4] += 2 - (2 * Z[i, j]) / (a);
					}
				}
				return;
			}

			if (p.Length == 7)
			{
				double p4sq = p[4] * p[4];
				double p5sq = p[5] * p[5];
				double cosp3 = Math.Cos(p[3]);
				double sinp3 = Math.Sin(p[3]);
				double twop4sq = 2 * p4sq;
				double twop5sq = 2 * p5sq;
				double p4cu = p4sq * p[4];
				double p5cu = p5sq * p[5];
				double dy, dx, cosp3dx, sinp3dx, sinp3dy, a, b, c, d, g, ab, h, cosp3dy, asq, bsq, exparg, expres, p0expres, twop0expres, twoZijp0expres, twoZij, cosp3a, cosp3b, sinp3b, sinp3a;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					cosp3dx = cosp3 * dx;
					sinp3dx = sinp3 * dx;

					for (int j = 0; j < y.Length; j++)
					{
						dy = p[2] - y[j];
						sinp3dy = sinp3 * dy;
						a = cosp3dx + sinp3dy;
						cosp3dy = cosp3 * dy;
						b = cosp3dy - sinp3dx;
						asq = a * a;
						bsq = b * b;
						exparg = -asq / twop4sq - bsq / twop5sq;
						expres = Math.Exp(exparg);
						c = p[6] + p[0] * expres;
						p0expres = p[0] * expres;
						twoZijp0expres = 2 * Z[i, j] * p0expres;
						twop0expres = 2 * p0expres;
						twoZij = 2 * Z[i, j];
						cosp3a = cosp3 * a;
						cosp3b = cosp3 * b;
						sinp3b = sinp3 * b;
						sinp3a = sinp3 * a;
						d = cosp3a / p4sq - sinp3b / p5sq;
						g = cosp3b / p5sq + sinp3a / p4sq;
						ab = a * b;
						h = ab / p4sq - ab / p5sq;

						f += 2 * p[6] - twoZij * Math.Log(c) + twop0expres;

						grad[0] += 2 * expres - (twoZij * expres) / (c);
						grad[1] += (twoZijp0expres * d) / (c) - twop0expres * d;
						grad[2] += (twoZijp0expres * g) / (c) - twop0expres * g;
						grad[3] += (twoZijp0expres * h) / (c) - twop0expres * h;
						grad[4] += (twop0expres * asq) / p4cu - (twoZijp0expres * asq) / (p4cu * (c));
						grad[5] += (twop0expres * bsq) / p5cu - (twoZijp0expres * bsq) / (p5cu * (c));
						grad[6] += 2 - (twoZij) / (c);
					}
				}
			}
		}

		public static void alglib_Gauss_2d_LM_LS_CSTAT_grad_compound(double[] p, ref double f, double[] grad, object obj)
		{
			f = 0;
			for (int i = 0; i < grad.Length; i++)
				grad[i] = 0;
			double[,] Z = (double[,])(((object[])obj)[0]);
			double[] x = (double[])(((object[])obj)[1]);
			double[] y = (double[])(((object[])obj)[2]);
			int func = ((int[])(((object[])obj)[3]))[0];
			int count = ((int[])(((object[])obj)[3]))[1];
			int pindex;

			if (func == 5)
			{
				double[] p3sq = new double[count];// = p[3] * p[3];
				double[] twop3sq = new double[count];// = 2 * p3sq;
				double[] p3cu = new double[count];// = p3sq * p[3];
				double[] twop1 = new double[count]; // = 2 * p[1];
				double[] twop2 = new double[count];// = 2 * p[2];
				for (int k = 0; k < count; k++)
				{
					pindex = k * (func - 1);
					p3sq[k] = p[pindex + 3] * p[pindex + 3];
					twop3sq[k] = 2 * p3sq[k];
					p3cu[k] = p3sq[k] * p[pindex + 3];
					twop1[k] = 2 * p[pindex + 1];
					twop2[k] = 2 * p[pindex + 2];
				}

				double twoxi, exparg, p0expres, a, sumexpress;
				double[] dxsq = new double[count];
				double[] dysq = new double[count];
				double[] dxsqdysq = new double[count];
				double[] expres = new double[count];

				for (int i = 0; i < x.Length; i++)
				{
					for (int k = 0; k < count; k++)
					{
						pindex = k * (func - 1);

						dxsq[k] = p[pindex + 1] - x[i];
						dxsq[k] *= dxsq[k];
					}
					twoxi = 2 * x[i];

					for (int j = 0; j < y.Length; j++)
					{
						p0expres = 0;
						sumexpress = 0;
						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							dysq[k] = p[pindex + 2] - y[j];
							dysq[k] *= dysq[k];
							dxsqdysq[k] = dxsq[k] + dysq[k];
							exparg = -dxsqdysq[k] / twop3sq[k];
							expres[k] = Math.Exp(exparg);

							sumexpress += expres[k];
							p0expres += p[pindex + 0] * expres[k];
						}
						a = p[p.Length - 1] + p0expres;
						f += 2 * a - 2 * Z[i, j] * Math.Log(a);
						grad[grad.Length - 1] += 2 - (2 * Z[i, j]) / (a);

						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							grad[pindex + 0] += 2 * expres[k] - (2 * Z[i, j] * expres[k]) / (a);
							grad[pindex + 1] += (Z[i, j] * p[pindex + 0] * expres[k] * (twop1[k] - twoxi)) / (p3sq[k] * (a)) - (p[pindex + 0] * expres[k] * (twop1[k] - twoxi)) / p3sq[k];
							grad[pindex + 2] += (Z[i, j] * p[pindex + 0] * expres[k] * (twop2[k] - 2 * y[j])) / (p3sq[k] * (a)) - (p[pindex + 0] * expres[k] * (twop2[k] - 2 * y[j])) / p3sq[k];
							grad[pindex + 3] += (2 * p[pindex + 0] * expres[k] * dxsqdysq[k]) / p3cu[k] - (2 * Z[i, j] * p[pindex + 0] * expres[k] * dxsqdysq[k]) / (p3cu[k] * (a));
						}
					}
				}
				return;
			}

			if (func == 7)
			{
				double[] p4sq = new double[count];// p[4] * p[4];
				double[] p5sq = new double[count];// p[5] * p[5];
				double[] cosp3 = new double[count];//  Math.Cos(p[3]);
				double[] sinp3 = new double[count];// Math.Sin(p[3]);
				double[] twop4sq = new double[count];// 2 * p4sq;
				double[] twop5sq = new double[count];// 2 * p5sq;
				double[] p4cu = new double[count];//  p4sq * p[4];
				double[] p5cu = new double[count];// p5sq * p[5];
				for (int k = 0; k < count; k++)
				{
					pindex = k * (func - 1);

					p4sq[k] = p[pindex + 4] * p[pindex + 4];
					p5sq[k] = p[pindex + 5] * p[pindex + 5];
					cosp3[k] = Math.Cos(p[pindex + 3]);
					sinp3[k] = Math.Sin(p[pindex + 3]);
					twop4sq[k] = 2 * p4sq[k];
					twop5sq[k] = 2 * p5sq[k];
					p4cu[k] = p4sq[k] * p[pindex + 4];
					p5cu[k] = p5sq[k] * p[pindex + 5];
				}

				double dy, sinp3dy, a, b, c, ab, cosp3dy, exparg, p0expres, twop0expres, twoZijp0expres, twoZij, cosp3a, cosp3b, sinp3b, sinp3a;
				double[] dx = new double[count];
				double[] cosp3dx = new double[count];
				double[] sinp3dx = new double[count];
				double[] expres = new double[count];
				double[] d = new double[count];
				double[] g = new double[count];
				double[] h = new double[count];
				double[] asq = new double[count];
				double[] bsq = new double[count];

				for (int i = 0; i < x.Length; i++)
				{
					for (int k = 0; k < count; k++)
					{
						pindex = k * (func - 1);

						dx[k] = p[pindex + 1] - x[i];
						cosp3dx[k] = cosp3[k] * dx[k];
						sinp3dx[k] = sinp3[k] * dx[k];
					}

					for (int j = 0; j < y.Length; j++)
					{
						twoZij = 2 * Z[i, j];
						p0expres = 0;

						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							dy = p[pindex + 2] - y[j];
							sinp3dy = sinp3[k] * dy;
							a = cosp3dx[k] + sinp3dy;
							cosp3dy = cosp3[k] * dy;
							b = cosp3dy - sinp3dx[k];
							asq[k] = a * a;
							bsq[k] = b * b;
							exparg = -asq[k] / twop4sq[k] - bsq[k] / twop5sq[k];
							expres[k] = Math.Exp(exparg);
							p0expres += p[pindex + 0] * expres[k];
							cosp3a = cosp3[k] * a;
							cosp3b = cosp3[k] * b;
							sinp3b = sinp3[k] * b;
							sinp3a = sinp3[k] * a;
							d[k] = cosp3a / p4sq[k] - sinp3b / p5sq[k];
							g[k] = cosp3b / p5sq[k] + sinp3a / p4sq[k];
							ab = a * b;
							h[k] = ab / p4sq[k] - ab / p5sq[k];
						}
						twop0expres = 2 * p0expres;
						twoZijp0expres = twoZij * p0expres;
						c = p[p.Length - 1] + p0expres;

						f += 2 * c - twoZij * Math.Log(c);
						grad[grad.Length - 1] += 2 - (twoZij) / (c);

						for (int k = 0; k < count; k++)
						{
							pindex = k * (func - 1);

							grad[pindex + 0] += 2 * expres[k] - (twoZij * expres[k]) / (c);
							grad[pindex + 1] += (twoZij * p[pindex + 0] * expres[k] * d[k]) / (c) - 2 * p[pindex + 0] * expres[k] * d[k];
							grad[pindex + 2] += (twoZij * p[pindex + 0] * expres[k] * g[k]) / (c) - 2 * p[pindex + 0] * expres[k] * g[k];
							grad[pindex + 3] += (twoZij * p[pindex + 0] * expres[k] * h[k]) / (c) - 2 * p[pindex + 0] * expres[k] * h[k];
							grad[pindex + 4] += (2 * p[pindex + 0] * expres[k] * asq[k]) / p4cu[k] - (twoZij * p[pindex + 0] * expres[k] * asq[k]) / (p4cu[k] * (c));
							grad[pindex + 5] += (2 * p[pindex + 0] * expres[k] * bsq[k]) / p5cu[k] - (twoZij * p[pindex + 0] * expres[k] * bsq[k]) / (p5cu[k] * (c));
						}
					}
				}
			}
		}

		public static void alglib_Moffat_2d_LM_LS_grad(double[] p, ref double f, double[] grad, object obj)
		{
			f = 0;
			for (int i = 0; i < grad.Length; i++)
				grad[i] = 0;
			double[,] Z = (double[,])(((object[])obj)[0]);
			double[] x = (double[])(((object[])obj)[1]);
			double[] y = (double[])(((object[])obj)[2]);

			if (p.Length == 6)
			{
				double p3sq = p[3] * p[3];
				double p41 = p[4] + 1;
				double twop1 = 2 * p[1];
				double twop2 = 2 * p[2];
				double p0p4 = p[0] * p[4];
				double p3cu = p3sq * p[3];
				double dx, dxsq, twoxi, twop1mtwoxi, dy, dysq, dxsqdysq, a, loga, apowp4, apowp41, b;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					dxsq = dx * dx;
					twoxi = 2 * x[i];
					twop1mtwoxi = twop1 - twoxi;

					for (int j = 0; j < y.Length; j++)
					{
						dy = p[2] - y[j];
						dysq = dy * dy;
						dxsqdysq = dxsq + dysq;
						a = dxsqdysq / p3sq + 1;
						loga = Math.Log(a);
						apowp4 = Math.Pow(a, p[4]);
						apowp41 = Math.Pow(a, p41);
						b = p[5] - Z[i, j] + p[0] / apowp4;

						f += b * b;

						grad[0] += (2 * b) / apowp4;
						grad[1] += -(2 * p0p4 * twop1mtwoxi * b) / (p3sq * apowp41);
						grad[2] += -(2 * p0p4 * (twop2 - 2 * y[j]) * b) / (p3sq * apowp41);
						grad[3] += (4 * p0p4 * dxsqdysq * b) / (p3cu * apowp41);
						grad[4] += -(2 * p[0] * loga * b) / apowp4;
						grad[5] += 2 * p[5] - 2 * Z[i, j] + (2 * p[0]) / apowp4;
					}
				}
				return;
			}

			if (p.Length == 8)
			{
				double cosp3 = Math.Cos(p[3]);
				double sinp3 = Math.Sin(p[3]);
				double p4sq = p[4] * p[4];
				double p5sq = p[5] * p[5];
				double p6p1 = p[6] + 1;
				double p0p6 = p[0] * p[6];
				double p4cu = p[4] * p4sq;
				double p5cu = p[5] * p5sq;
				double twop0p6 = 2 * p0p6;
				double fourp0p6 = 4 * p0p6;
				double twocosp3 = 2 * cosp3;
				double twosinp3 = 2 * sinp3;
				double twop7 = 2 * p[7];
				double twop0 = 2 * p[0];
				double dx, cosp3dx, sinp3dx, dy, sinp3dy, a, b, c, d, g, h, asq, cosp3dy, bsq, logc, p0logc;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					cosp3dx = cosp3 * dx;
					sinp3dx = sinp3 * dx;

					for (int j = 0; j < y.Length; j++)
					{
						dy = p[2] - y[j];
						sinp3dy = sinp3 * dy;
						a = cosp3dx + sinp3dy;
						asq = a * a;
						cosp3dy = cosp3 * dy;
						b = cosp3dy - sinp3dx;
						bsq = b * b;
						c = asq / p4sq + bsq / p5sq + 1;
						logc = Math.Log(c);
						d = Math.Pow(c, p6p1);
						g = Math.Pow(c, p[6]);
						h = p[7] - Z[i, j] + p[0] / g;
						p0logc = p[0] * logc;

						f += h * h;

						grad[0] += (2 * (h)) / g;
						grad[1] += -(twop0p6 * ((twocosp3 * a) / p4sq - (twosinp3 * b) / p5sq) * (h)) / d;
						grad[2] += -(twop0p6 * ((twocosp3 * b) / p5sq + (twosinp3 * a) / p4sq) * (h)) / d;
						grad[3] += -(twop0p6 * ((2 * a * b) / p4sq - (2 * a * b) / p5sq) * (h)) / d;
						grad[4] += (fourp0p6 * asq * (h)) / (p4cu * d);
						grad[5] += (fourp0p6 * bsq * (h)) / (p5cu * d);
						grad[6] += -(2 * p0logc * (h)) / g;
						grad[7] += twop7 - 2 * Z[i, j] + twop0 / g;
					}
				}
			}
		}

		public static void alglib_Moffat_2d_LM_LS_CHISQ_grad(double[] p, ref double f, double[] grad, object obj)
		{
			f = 0;
			for (int i = 0; i < grad.Length; i++)
				grad[i] = 0;
			double[,] Z = (double[,])(((object[])obj)[0]);
			double[] x = (double[])(((object[])obj)[1]);
			double[] y = (double[])(((object[])obj)[2]);
			double den;

			if (p.Length == 6)
			{
				double p3sq = p[3] * p[3];
				double p41 = p[4] + 1;
				double twop1 = 2 * p[1];
				double twop2 = 2 * p[2];
				double p0p4 = p[0] * p[4];
				double p3cu = p3sq * p[3];
				double dx, dxsq, twoxi, twop1mtwoxi, dy, dysq, dxsqdysq, a, loga, apowp4, apowp41, b;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					dxsq = dx * dx;
					twoxi = 2 * x[i];
					twop1mtwoxi = twop1 - twoxi;

					for (int j = 0; j < y.Length; j++)
					{
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Z[i, j];

						dy = p[2] - y[j];
						dysq = dy * dy;
						dxsqdysq = dxsq + dysq;
						a = dxsqdysq / p3sq + 1;
						loga = Math.Log(a);
						apowp4 = Math.Pow(a, p[4]);
						apowp41 = Math.Pow(a, p41);
						b = p[5] - Z[i, j] + p[0] / apowp4;

						f += b * b / den;

						grad[0] += (2 * b) / (den * apowp4);
						grad[1] += -(2 * p0p4 * twop1mtwoxi * b) / (den * p3sq * apowp41);
						grad[2] += -(2 * p0p4 * (twop2 - 2 * y[j]) * b) / (den * p3sq * apowp41);
						grad[3] += (4 * p0p4 * dxsqdysq * b) / (den * p3cu * apowp41);
						grad[4] += -(2 * p[0] * loga * b) / (den * apowp4);
						grad[5] += (2 * p[5] - 2 * Z[i, j] + (2 * p[0]) / apowp4) / den;
					}
				}
				return;
			}

			if (p.Length == 8)
			{
				double cosp3 = Math.Cos(p[3]);
				double sinp3 = Math.Sin(p[3]);
				double p4sq = p[4] * p[4];
				double p5sq = p[5] * p[5];
				double p6p1 = p[6] + 1;
				double p0p6 = p[0] * p[6];
				double p4cu = p[4] * p4sq;
				double p5cu = p[5] * p5sq;
				double twop0p6 = 2 * p0p6;
				double fourp0p6 = 4 * p0p6;
				double twocosp3 = 2 * cosp3;
				double twosinp3 = 2 * sinp3;
				double twop7 = 2 * p[7];
				double twop0 = 2 * p[0];
				double dx, cosp3dx, sinp3dx, dy, sinp3dy, a, b, c, d, g, h, asq, cosp3dy, bsq, logc, p0logc;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					cosp3dx = cosp3 * dx;
					sinp3dx = sinp3 * dx;

					for (int j = 0; j < y.Length; j++)
					{
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Z[i, j];

						dy = p[2] - y[j];
						sinp3dy = sinp3 * dy;
						a = cosp3dx + sinp3dy;
						asq = a * a;
						cosp3dy = cosp3 * dy;
						b = cosp3dy - sinp3dx;
						bsq = b * b;
						c = asq / p4sq + bsq / p5sq + 1;
						logc = Math.Log(c);
						d = Math.Pow(c, p6p1);
						g = Math.Pow(c, p[6]);
						h = p[7] - Z[i, j] + p[0] / g;
						p0logc = p[0] * logc;

						f += h * h / den;

						grad[0] += (2 * (h)) / (den * g);
						grad[1] += -(twop0p6 * ((twocosp3 * a) / p4sq - (twosinp3 * b) / p5sq) * (h)) / (den * d);
						grad[2] += -(twop0p6 * ((twocosp3 * b) / p5sq + (twosinp3 * a) / p4sq) * (h)) / (den * d);
						grad[3] += -(twop0p6 * ((2 * a * b) / p4sq - (2 * a * b) / p5sq) * (h)) / (den * d);
						grad[4] += (fourp0p6 * asq * (h)) / (den * p4cu * d);
						grad[5] += (fourp0p6 * bsq * (h)) / (den * p5cu * d);
						grad[6] += -(2 * p0logc * (h)) / (den * g);
						grad[7] += (twop7 - 2 * Z[i, j] + twop0 / g) / den;
					}
				}
			}
		}

		public static void alglib_Moffat_2d_LM_LS_ROBUST_grad(double[] p, ref double f, double[] grad, object obj)
		{
			f = 0;
			for (int i = 0; i < grad.Length; i++)
				grad[i] = 0;
			double[,] Z = (double[,])(((object[])obj)[0]);
			double[] x = (double[])(((object[])obj)[1]);
			double[] y = (double[])(((object[])obj)[2]);
			double den;

			if (p.Length == 6)
			{
				double p3sq = p[3] * p[3];
				double p41 = p[4] + 1;
				double twop1 = 2 * p[1];
				double twop2 = 2 * p[2];
				double p0p4 = p[0] * p[4];
				double p3cu = p3sq * p[3];
				double dx, dxsq, twoxi, twop1mtwoxi, dy, dysq, dxsqdysq, a, loga, apowp4, apowp41, b;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					dxsq = dx * dx;
					twoxi = 2 * x[i];
					twop1mtwoxi = twop1 - twoxi;

					for (int j = 0; j < y.Length; j++)
					{
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Math.Sqrt(Z[i, j]);

						dy = p[2] - y[j];
						dysq = dy * dy;
						dxsqdysq = dxsq + dysq;
						a = dxsqdysq / p3sq + 1;
						loga = Math.Log(a);
						apowp4 = Math.Pow(a, p[4]);
						apowp41 = Math.Pow(a, p41);
						b = p[5] - Z[i, j] + p[0] / apowp4;

						f += b * b / den;

						grad[0] += (2 * b) / (den * apowp4);
						grad[1] += -(2 * p0p4 * twop1mtwoxi * b) / (den * p3sq * apowp41);
						grad[2] += -(2 * p0p4 * (twop2 - 2 * y[j]) * b) / (den * p3sq * apowp41);
						grad[3] += (4 * p0p4 * dxsqdysq * b) / (den * p3cu * apowp41);
						grad[4] += -(2 * p[0] * loga * b) / (den * apowp4);
						grad[5] += (2 * p[5] - 2 * Z[i, j] + (2 * p[0]) / apowp4) / den;
					}
				}
				return;
			}

			if (p.Length == 8)
			{
				double cosp3 = Math.Cos(p[3]);
				double sinp3 = Math.Sin(p[3]);
				double p4sq = p[4] * p[4];
				double p5sq = p[5] * p[5];
				double p6p1 = p[6] + 1;
				double p0p6 = p[0] * p[6];
				double p4cu = p[4] * p4sq;
				double p5cu = p[5] * p5sq;
				double twop0p6 = 2 * p0p6;
				double fourp0p6 = 4 * p0p6;
				double twocosp3 = 2 * cosp3;
				double twosinp3 = 2 * sinp3;
				double twop7 = 2 * p[7];
				double twop0 = 2 * p[0];
				double dx, cosp3dx, sinp3dx, dy, sinp3dy, a, b, c, d, g, h, asq, cosp3dy, bsq, logc, p0logc;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					cosp3dx = cosp3 * dx;
					sinp3dx = sinp3 * dx;

					for (int j = 0; j < y.Length; j++)
					{
						if (Z[i, j] < 1)
							den = 1;
						else
							den = Math.Sqrt(Z[i, j]);

						dy = p[2] - y[j];
						sinp3dy = sinp3 * dy;
						a = cosp3dx + sinp3dy;
						asq = a * a;
						cosp3dy = cosp3 * dy;
						b = cosp3dy - sinp3dx;
						bsq = b * b;
						c = asq / p4sq + bsq / p5sq + 1;
						logc = Math.Log(c);
						d = Math.Pow(c, p6p1);
						g = Math.Pow(c, p[6]);
						h = p[7] - Z[i, j] + p[0] / g;
						p0logc = p[0] * logc;

						f += h * h / den;

						grad[0] += (2 * (h)) / (den * g);
						grad[1] += -(twop0p6 * ((twocosp3 * a) / p4sq - (twosinp3 * b) / p5sq) * (h)) / (den * d);
						grad[2] += -(twop0p6 * ((twocosp3 * b) / p5sq + (twosinp3 * a) / p4sq) * (h)) / (den * d);
						grad[3] += -(twop0p6 * ((2 * a * b) / p4sq - (2 * a * b) / p5sq) * (h)) / (den * d);
						grad[4] += (fourp0p6 * asq * (h)) / (den * p4cu * d);
						grad[5] += (fourp0p6 * bsq * (h)) / (den * p5cu * d);
						grad[6] += -(2 * p0logc * (h)) / (den * g);
						grad[7] += (twop7 - 2 * Z[i, j] + twop0 / g) / den;
					}
				}
			}
		}

		public static void alglib_Moffat_2d_LM_LS_CSTAT_grad(double[] p, ref double f, double[] grad, object obj)
		{
			f = 0;
			for (int i = 0; i < grad.Length; i++)
				grad[i] = 0;
			double[,] Z = (double[,])(((object[])obj)[0]);
			double[] x = (double[])(((object[])obj)[1]);
			double[] y = (double[])(((object[])obj)[2]);

			if (p.Length == 6)
			{
				double p3sq = p[3] * p[3];
				double p41 = p[4] + 1;
				double twop1 = 2 * p[1];
				double twop2 = 2 * p[2];
				double p0p4 = p[0] * p[4];
				double p3cu = p3sq * p[3];
				double dx, dxsq, twoxi, twop1mtwoxi, dy, dysq, dxsqdysq, a, loga, apowp4, apowp41, b, d;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					dxsq = dx * dx;
					twoxi = 2 * x[i];
					twop1mtwoxi = twop1 - twoxi;

					for (int j = 0; j < y.Length; j++)
					{
						dy = p[2] - y[j];
						dysq = dy * dy;
						dxsqdysq = dxsq + dysq;
						a = dxsqdysq / p3sq + 1;
						loga = Math.Log(a);
						apowp4 = Math.Pow(a, p[4]);
						apowp41 = Math.Pow(a, p41);
						b = p[5] - Z[i, j] + p[0] / apowp4;
						d = p[5] + p[0] / apowp4;

						f += 2 * p[5] - 2 * Z[i, j] * Math.Log(d) + (2 * p[0]) / apowp4;

						grad[0] += 2 / apowp4 - (2 * Z[i, j]) / (d * apowp4);
						grad[1] += (2 * Z[i, j] * p0p4 * twop1mtwoxi) / (p3sq * d * apowp41) - (2 * p0p4 * twop1mtwoxi) / (p3sq * apowp41);
						grad[2] += (2 * Z[i, j] * p0p4 * (twop2 - 2 * y[j])) / (p3sq * d * apowp41) - (2 * p0p4 * (twop2 - 2 * y[j])) / (p3sq * apowp41);
						grad[3] += (4 * p0p4 * dxsqdysq) / (p3cu * apowp41) - (4 * Z[i, j] * p0p4 * dxsqdysq) / (p3cu * d * apowp41);
						grad[4] += (2 * Z[i, j] * p[0] * loga) / (d * apowp4) - (2 * p[0] * loga) / apowp4;
						grad[5] += 2 - (2 * Z[i, j]) / d;
					}
				}
				return;
			}

			if (p.Length == 8)
			{
				double cosp3 = Math.Cos(p[3]);
				double sinp3 = Math.Sin(p[3]);
				double p4sq = p[4] * p[4];
				double p5sq = p[5] * p[5];
				double p61 = p[6] + 1;
				double p4cu = p[4] * p4sq;
				double p5cu = p[5] * p5sq;
				double p0p6 = p[0] * p[6];
				double dx, cosp3dx, sinp3dx, dy, sinp3dy, cosp3dy, cosp3dxsinp3dy, cosp3dysinp3dx, cosp3dxsinp3dysq, cosp3dysinp3dxsq, a, loga, apowp6, apowp61, b, twoZij;

				for (int i = 0; i < x.Length; i++)
				{
					dx = p[1] - x[i];
					cosp3dx = cosp3 * dx;
					sinp3dx = sinp3 * dx;

					for (int j = 0; j < y.Length; j++)
					{
						dy = p[2] - y[j];
						sinp3dy = sinp3 * dy;
						cosp3dy = cosp3 * dy;
						cosp3dxsinp3dy = cosp3dx + sinp3dy;
						cosp3dysinp3dx = cosp3dy - sinp3dx;
						cosp3dxsinp3dysq = cosp3dxsinp3dy * cosp3dxsinp3dy;
						cosp3dysinp3dxsq = cosp3dysinp3dx * cosp3dysinp3dx;
						a = cosp3dxsinp3dysq / p4sq + cosp3dysinp3dxsq / p5sq + 1;
						loga = Math.Log(a);
						apowp6 = Math.Pow(a, p[6]);
						apowp61 = apowp6 * a;
						b = p[7] + p[0] / apowp6;
						twoZij = 2 * Z[i, j];

						f += 2 * p[7] - twoZij * Math.Log(b) + (2 * p[0]) / apowp6;

						grad[0] += 2 / apowp6 - (twoZij) / ((b) * apowp6);
						grad[1] += (twoZij * p0p6 * ((2 * cosp3 * cosp3dxsinp3dy) / p4sq - (2 * sinp3 * cosp3dysinp3dx) / p5sq)) / ((b) * apowp61) - (2 * p0p6 * ((2 * cosp3 * cosp3dxsinp3dy) / p4sq - (2 * sinp3 * cosp3dysinp3dx) / p5sq)) / apowp61;
						grad[2] += (twoZij * p0p6 * ((2 * cosp3 * cosp3dysinp3dx) / p5sq + (2 * sinp3 * cosp3dxsinp3dy) / p4sq)) / ((b) * apowp61) - (2 * p0p6 * ((2 * cosp3 * cosp3dysinp3dx) / p5sq + (2 * sinp3 * cosp3dxsinp3dy) / p4sq)) / apowp61;
						grad[3] += (twoZij * p0p6 * ((2 * cosp3dxsinp3dy * cosp3dysinp3dx) / p4sq - (2 * cosp3dxsinp3dy * cosp3dysinp3dx) / p5sq)) / ((b) * apowp61) - (2 * p0p6 * ((2 * cosp3dxsinp3dy * cosp3dysinp3dx) / p4sq - (2 * cosp3dxsinp3dy * cosp3dysinp3dx) / p5sq)) / apowp61;
						grad[4] += (4 * p0p6 * cosp3dxsinp3dysq) / (p4cu * apowp61) - (4 * Z[i, j] * p0p6 * cosp3dxsinp3dysq) / (p4cu * (b) * apowp61);
						grad[5] += (4 * p0p6 * cosp3dysinp3dxsq) / (p5cu * apowp61) - (4 * Z[i, j] * p0p6 * cosp3dysinp3dxsq) / (p5cu * (b) * apowp61);
						grad[6] += (twoZij * p[0] * loga) / ((b) * apowp6) - (2 * p[0] * loga) / apowp6;
						grad[7] += 2 - (twoZij) / (b);
					}
				}
			}
		}

		/// <summary>Calculates a single point of a 2-d Moffat surface M(x,y|p)
		/// <br />M(x,y|p) = p(0) * ( 1 + { (x-p(1))^2 + (y-p(2))^2 } / p(3)^2 ) ^ (-p(4)) + p(5)
		/// <br />or
		/// <br />M(x,y|p) = p(0) * ( 1 + { ((x-p(1))*cos(p(3)) + (y-p(2))*sin(p(3)))^2 } / p(4)^2 + { (-(x-p(1))*sin(p(3)) + (y-p(2))*cos(p(3)))^2 } / p(5)^2 ) ^ (-p(6)) + p(7)
		/// <br />where x[0] is a position on X-axis x, and x[1] is a position on Y-axis y.
		/// <br />The form of M(x,y|p) used is determined by the length of the parmater vector "p"</summary>
		/// <param name="p">The initial parameters of the Moffat fit.  Options are:
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = theta; p[4] = beta; p[5] = bias
		/// <br />or
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = phi; p[4] = x-theta; p[5] = y-theta; p[6] = beta; p[7] = bias</param>
		/// <param name="x">The x,y position to calculate the value val of the Moffat M(x,y|p): x[0] = x, x[1] = y</param>
		/// <param name="val">The calculated value of the Moffat.</param>
		/// <param name="obj">obj.</param>
		public static void alglib_Moffat_2d(double[] p, double[] x, ref double val, object obj)
		{
			if (p.Length == 6)
				val = p[0] * Math.Pow(1.0 + ((x[0] - p[1]) * (x[0] - p[1]) + (x[1] - p[2]) * (x[1] - p[2])) / p[3] / p[3], -p[4]) + p[5];
			if (p.Length == 8)
				val = p[0] * Math.Pow(1.0 + (Math.Pow((x[0] - p[1]) * Math.Cos(p[3]) + (x[1] - p[2]) * Math.Sin(p[3]), 2)) / (p[4] * p[4]) + (Math.Pow(-(x[0] - p[1]) * Math.Sin(p[3]) + (x[1] - p[2]) * Math.Cos(p[3]), 2)) / (p[5] * p[5]), -p[6]) + p[7];
		}

		public static void alglib_Moffat_2d_grad(double[] p, double[] x, ref double val, double[] grad, object obj)
		{
			if (p.Length == 6)
			{
				val = p[0] * Math.Pow(1.0 + ((x[0] - p[1]) * (x[0] - p[1]) + (x[1] - p[2]) * (x[1] - p[2])) / p[3] / p[3], -p[4]) + p[5];
				grad[0] = Math.Pow(((p[1] - x[0]) * (p[1] - x[0]) + (p[2] - x[1]) * (p[2] - x[1])) / (p[3] * p[3]) + 1, -p[4]);
				grad[1] = -(p[0] * p[4] * (2 * p[1] - 2 * x[0])) / (p[3] * p[3] * Math.Pow(((p[1] - x[0]) * (p[1] - x[0]) + (p[2] - x[1]) * (p[2] - x[1])) / (p[3] * p[3]) + 1, (p[4] + 1)));
				grad[2] = -(p[0] * p[4] * (2 * p[2] - 2 * x[1])) / (p[3] * p[3] * Math.Pow(((p[1] - x[0]) * (p[1] - x[0]) + (p[2] - x[1]) * (p[2] - x[1])) / (p[3] * p[3]) + 1, (p[4] + 1)));
				grad[3] = (2 * p[0] * p[4] * ((p[1] - x[0]) * (p[1] - x[0]) + (p[2] - x[1]) * (p[2] - x[1]))) / (p[3] * p[3] * p[3] * Math.Pow(((p[1] - x[0]) * (p[1] - x[0]) + (p[2] - x[1]) * (p[2] - x[1])) / (p[3] * p[3]) + 1, (p[4] + 1)));
				grad[4] = -(p[0] * Math.Log(((p[1] - x[0]) * (p[1] - x[0]) + (p[2] - x[1]) * (p[2] - x[1])) / (p[3] * p[3]) + 1)) / Math.Pow(((p[1] - x[0]) * (p[1] - x[0]) + (p[2] - x[1]) * (p[2] - x[1])) / (p[3] * p[3]) + 1, p[4]);
				grad[5] = 1;
			}
			if (p.Length == 8)
			{
				val = p[0] * Math.Pow(1.0 + (Math.Pow((x[0] - p[1]) * Math.Cos(p[3]) + (x[1] - p[2]) * Math.Sin(p[3]), 2)) / (p[4] * p[4]) + (Math.Pow(-(x[0] - p[1]) * Math.Sin(p[3]) + (x[1] - p[2]) * Math.Cos(p[3]), 2)) / (p[5] * p[5]), -p[6]) + p[7];
				grad[0] = Math.Pow(Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (p[4] * p[4]) + Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (p[5] * p[5]) + 1, -p[6]);
				grad[1] = -(p[0] * p[6] * ((2 * Math.Cos(p[3]) * (Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]))) / (p[4] * p[4]) - (2 * Math.Sin(p[3]) * (Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]))) / (p[5] * p[5]))) / Math.Pow(Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (p[4] * p[4]) + Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (p[5] * p[5]) + 1, (p[6] + 1));
				grad[2] = -(p[0] * p[6] * ((2 * Math.Cos(p[3]) * (Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]))) / (p[5] * p[5]) + (2 * Math.Sin(p[3]) * (Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]))) / (p[4] * p[4]))) / Math.Pow(Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (p[4] * p[4]) + Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (p[5] * p[5]) + 1, (p[6] + 1));
				grad[3] = -(p[0] * p[6] * ((2 * (Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1])) * (Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]))) / (p[4] * p[4]) - (2 * (Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1])) * (Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]))) / (p[5] * p[5]))) / Math.Pow(Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (p[4] * p[4]) + Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (p[5] * p[5]) + 1, (p[6] + 1));
				grad[4] = (2 * p[0] * p[6] * Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2)) / (p[4] * p[4] * p[4] * Math.Pow(Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (p[4] * p[4]) + Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (p[5] * p[5]) + 1, (p[6] + 1)));
				grad[5] = (2 * p[0] * p[6] * Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2)) / (p[5] * p[5] * p[5] * Math.Pow(Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (p[4] * p[4]) + Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (p[5] * p[5]) + 1, (p[6] + 1)));
				grad[6] = -(p[0] * Math.Log(Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (p[4] * p[4]) + Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (p[5] * p[5]) + 1)) / Math.Pow(Math.Pow(Math.Cos(p[3]) * (p[1] - x[0]) + Math.Sin(p[3]) * (p[2] - x[1]), 2) / (p[4] * p[4]) + Math.Pow(Math.Cos(p[3]) * (p[2] - x[1]) - Math.Sin(p[3]) * (p[1] - x[0]), 2) / (p[5] * p[5]) + 1, p[6]);
				grad[7] = 1;
			}
		}

		public static void alglib_Gauss_2d_compound(double[] p, double[] x, ref double val, object obj)
		{
			int func = ((int[])obj)[0];
			int count = ((int[])obj)[1];

			val = p[p.Length - 1];//background added in once

			if (func == 5)
				for (int i = 0; i < count; i++)
				{
					int index = i * (func - 1);
					val += p[0 + index] * Math.Exp(-((x[0] - p[1 + index]) * (x[0] - p[1 + index]) + (x[1] - p[2 + index]) * (x[1] - p[2 + index])) / (2 * p[3 + index] * p[3 + index]));
				}
			else
				for (int i = 0; i < count; i++)
				{
					int index = i * (func - 1);
					val += p[0 + index] * Math.Exp(-Math.Pow((x[0] - p[1 + index]) * Math.Cos(p[3 + index]) + (x[1] - p[2 + index]) * Math.Sin(p[3 + index]), 2) / (2 * p[4 + index] * p[4 + index]) - Math.Pow(-(x[0] - p[1 + index]) * Math.Sin(p[3 + index]) + (x[1] - p[2 + index]) * Math.Cos(p[3 + index]), 2) / (2 * p[5 + index] * p[5 + index]));
				}
		}

		public static void alglib_Gauss_2d_compound_grad(double[] p, double[] x, ref double val, double[] grad, object obj)
		{
			int func = ((int[])obj)[0];
			int count = ((int[])obj)[1];

			val = p[p.Length - 1];//background added in once
			grad[p.Length - 1] = 1;//background added in once

			if (func == 5)
				for (int i = 0; i < count; i++)
				{
					int index = i * (func - 1);
					val += p[0 + index] * Math.Exp(-((x[0] - p[1 + index]) * (x[0] - p[1 + index]) + (x[1] - p[2 + index]) * (x[1] - p[2 + index])) / (2 * p[3 + index] * p[3 + index]));
					grad[0 + index] = Math.Exp(-((p[1 + index] - x[0]) * (p[1 + index] - x[0]) + (p[2 + index] - x[1]) * (p[2 + index] - x[1])) / (2 * p[3 + index] * p[3 + index]));
					grad[1 + index] = -(p[0 + index] * Math.Exp(-((p[1 + index] - x[0]) * (p[1 + index] - x[0]) + (p[2 + index] - x[1]) * (p[2 + index] - x[1])) / (2 * p[3 + index] * p[3 + index])) * (2 * p[1 + index] - 2 * x[0])) / (2 * p[3 + index] * p[3 + index]);
					grad[2 + index] = -(p[0 + index] * Math.Exp(-((p[1 + index] - x[0]) * (p[1 + index] - x[0]) + (p[2 + index] - x[1]) * (p[2 + index] - x[1])) / (2 * p[3 + index] * p[3 + index])) * (2 * p[2 + index] - 2 * x[1])) / (2 * p[3 + index] * p[3 + index]);
					grad[3 + index] = (p[0 + index] * Math.Exp(-((p[1 + index] - x[0]) * (p[1 + index] - x[0]) + (p[2 + index] - x[1]) * (p[2 + index] - x[1])) / (2 * p[3 + index] * p[3 + index])) * ((p[1 + index] - x[0]) * (p[1 + index] - x[0]) + (p[2 + index] - x[1]) * (p[2 + index] - x[1]))) / (p[3 + index] * p[3 + index] * p[3 + index]);
				}
			else
				for (int i = 0; i < count; i++)
				{
					int index = i * (func - 1);
					val += p[0 + index] * Math.Exp(-Math.Pow((x[0] - p[1 + index]) * Math.Cos(p[3 + index]) + (x[1] - p[2 + index]) * Math.Sin(p[3 + index]), 2) / (2 * p[4 + index] * p[4 + index]) - Math.Pow(-(x[0] - p[1 + index]) * Math.Sin(p[3 + index]) + (x[1] - p[2 + index]) * Math.Cos(p[3 + index]), 2) / (2 * p[5 + index] * p[5 + index]));
					grad[0 + index] = Math.Exp(-Math.Pow(Math.Cos(p[3 + index]) * (p[1 + index] - x[0]) + Math.Sin(p[3 + index]) * (p[2 + index] - x[1]), 2) / (2 * p[4 + index] * p[4 + index]) - Math.Pow(Math.Cos(p[3 + index]) * (p[2 + index] - x[1]) - Math.Sin(p[3 + index]) * (p[1 + index] - x[0]), 2) / (2 * p[5 + index] * p[5 + index]));
					grad[1 + index] = -p[0 + index] * Math.Exp(-Math.Pow(Math.Cos(p[3 + index]) * (p[1 + index] - x[0]) + Math.Sin(p[3 + index]) * (p[2 + index] - x[1]), 2) / (2 * p[4 + index] * p[4 + index]) - Math.Pow(Math.Cos(p[3 + index]) * (p[2 + index] - x[1]) - Math.Sin(p[3 + index]) * (p[1 + index] - x[0]), 2) / (2 * p[5 + index] * p[5 + index])) * ((Math.Cos(p[3 + index]) * (Math.Cos(p[3 + index]) * (p[1 + index] - x[0]) + Math.Sin(p[3 + index]) * (p[2 + index] - x[1]))) / (p[4 + index] * p[4 + index]) - (Math.Sin(p[3 + index]) * (Math.Cos(p[3 + index]) * (p[2 + index] - x[1]) - Math.Sin(p[3 + index]) * (p[1 + index] - x[0]))) / (p[5 + index] * p[5 + index]));
					grad[2 + index] = -p[0 + index] * Math.Exp(-Math.Pow(Math.Cos(p[3 + index]) * (p[1 + index] - x[0]) + Math.Sin(p[3 + index]) * (p[2 + index] - x[1]), 2) / (2 * p[4 + index] * p[4 + index]) - Math.Pow(Math.Cos(p[3 + index]) * (p[2 + index] - x[1]) - Math.Sin(p[3 + index]) * (p[1 + index] - x[0]), 2) / (2 * p[5 + index] * p[5 + index])) * ((Math.Cos(p[3 + index]) * (Math.Cos(p[3 + index]) * (p[2 + index] - x[1]) - Math.Sin(p[3 + index]) * (p[1 + index] - x[0]))) / (p[5 + index] * p[5 + index]) + (Math.Sin(p[3 + index]) * (Math.Cos(p[3 + index]) * (p[1 + index] - x[0]) + Math.Sin(p[3 + index]) * (p[2 + index] - x[1]))) / (p[4 + index] * p[4 + index]));
					grad[3 + index] = -p[0 + index] * Math.Exp(-Math.Pow(Math.Cos(p[3 + index]) * (p[1 + index] - x[0]) + Math.Sin(p[3 + index]) * (p[2 + index] - x[1]), 2) / (2 * p[4 + index] * p[4 + index]) - Math.Pow(Math.Cos(p[3 + index]) * (p[2 + index] - x[1]) - Math.Sin(p[3 + index]) * (p[1 + index] - x[0]), 2) / (2 * p[5 + index] * p[5 + index])) * (((Math.Cos(p[3 + index]) * (p[1 + index] - x[0]) + Math.Sin(p[3 + index]) * (p[2 + index] - x[1])) * (Math.Cos(p[3 + index]) * (p[2 + index] - x[1]) - Math.Sin(p[3 + index]) * (p[1 + index] - x[0]))) / (p[4 + index] * p[4 + index]) - ((Math.Cos(p[3 + index]) * (p[1 + index] - x[0]) + Math.Sin(p[3 + index]) * (p[2 + index] - x[1])) * (Math.Cos(p[3 + index]) * (p[2 + index] - x[1]) - Math.Sin(p[3 + index]) * (p[1 + index] - x[0]))) / (p[5 + index] * p[5 + index]));
					grad[4 + index] = (p[0 + index] * Math.Exp(-Math.Pow(Math.Cos(p[3 + index]) * (p[1 + index] - x[0]) + Math.Sin(p[3 + index]) * (p[2 + index] - x[1]), 2) / (2 * p[4 + index] * p[4 + index]) - Math.Pow(Math.Cos(p[3 + index]) * (p[2 + index] - x[1]) - Math.Sin(p[3 + index]) * (p[1 + index] - x[0]), 2) / (2 * p[5 + index] * p[5 + index])) * Math.Pow(Math.Cos(p[3 + index]) * (p[1 + index] - x[0]) + Math.Sin(p[3 + index]) * (p[2 + index] - x[1]), 2)) / (p[4 + index] * p[4 + index] * p[4 + index]);
					grad[5 + index] = (p[0 + index] * Math.Exp(-Math.Pow(Math.Cos(p[3 + index]) * (p[1 + index] - x[0]) + Math.Sin(p[3 + index]) * (p[2 + index] - x[1]), 2) / (2 * p[4 + index] * p[4 + index]) - Math.Pow(Math.Cos(p[3 + index]) * (p[2 + index] - x[1]) - Math.Sin(p[3 + index]) * (p[1 + index] - x[0]), 2) / (2 * p[5 + index] * p[5 + index])) * Math.Pow(Math.Cos(p[3 + index]) * (p[2 + index] - x[1]) - Math.Sin(p[3 + index]) * (p[1 + index] - x[0]), 2)) / (p[5 + index] * p[5 + index] * p[5 + index]);
				}
		}

		public static void alglib_Moffat_1d(double[] p, double[] x, ref double val, object obj)
		{
			val = p[0] * Math.Pow(1.0 + ((x[0] - p[1]) * (x[0] - p[1])) / p[2] / p[2], -p[3]) + p[4];
		}

		public static void alglib_Moffat_1d_grad(double[] p, double[] x, ref double val, double[] grad, object obj)
		{
			val = p[0] * Math.Pow(1.0 + ((x[0] - p[1]) * (x[0] - p[1])) / p[2] / p[2], -p[3]) + p[4];
			grad[0] = Math.Pow((p[1] - x[0]) * (p[1] - x[0]) / (p[2] * p[2]) + 1, -p[3]);
			grad[1] = -(p[0] * p[3] * (2 * p[1] - 2 * x[0])) / (p[2] * p[2] * Math.Pow((p[1] - x[0]) * (p[1] - x[0]) / (p[2] * p[2]) + 1, (p[3] + 1)));
			grad[2] = (2 * p[0] * p[3] * (p[1] - x[0]) * (p[1] - x[0])) / (p[2] * p[2] * p[2] * Math.Pow((p[1] - x[0]) * (p[1] - x[0]) / (p[2] * p[2]) + 1, (p[3] + 1)));
			grad[3] = -(p[0] * Math.Log((p[1] - x[0]) * (p[1] - x[0]) / (p[2] * p[2]) + 1)) / Math.Pow((p[1] - x[0]) * (p[1] - x[0]) / (p[2] * p[2]) + 1, p[3]);
			grad[4] = 1;
		}

		public static void alglib_Moffat_2d_compound(double[] p, double[] x, ref double val, object obj)
		{
			int func = ((int[])obj)[0];
			int count = ((int[])obj)[1];

			val = p[p.Length - 1];//background added in once

			if (func == 6)
				for (int i = 0; i < count; i++)
				{
					int index = i * (func - 1);
					val += p[0 + index] * Math.Pow(1.0 + ((x[0] - p[1 + index]) * (x[0] - p[1 + index]) + (x[1] - p[2 + index]) * (x[1] - p[2 + index])) / p[3 + index] / p[3 + index], -p[4 + index]);
				}
			if (func == 8)
				for (int i = 0; i < count; i++)
				{
					int index = i * (func - 1);
					val += p[0 + index] * Math.Pow(1.0 + (Math.Pow((x[0] - p[1 + index]) * Math.Cos(p[3 + index]) + (x[1] - p[2 + index]) * Math.Sin(p[3 + index]), 2)) / (p[4 + index] * p[4 + index]) + (Math.Pow(-(x[0] - p[1 + index]) * Math.Sin(p[3 + index]) + (x[1] - p[2 + index]) * Math.Cos(p[3 + index]), 2)) / (p[5 + index] * p[5 + index]), -p[6 + index]);
				}
		}

		public static void alglib_Moffat_2d_compound_grad(double[] p, double[] x, ref double val, double[] grad, object obj)
		{
			int func = ((int[])obj)[0];
			int count = ((int[])obj)[1];

			val = p[p.Length - 1];//background added in once
			grad[p.Length - 1] = 1;//background added in once

			if (func == 6)
			{
				for (int i = 0; i < count; i++)
				{
					int index = i * (func - 1);
					val += p[index + 0] * Math.Pow(1.0 + ((x[0] - p[index + 1]) * (x[0] - p[index + 1]) + (x[1] - p[index + 2]) * (x[1] - p[index + 2])) / p[index + 3] / p[index + 3], -p[index + 4]);
					grad[0 + index] = Math.Pow(((p[index + 1] - x[0]) * (p[index + 1] - x[0]) + (p[index + 2] - x[1]) * (p[index + 2] - x[1])) / (p[index + 3] * p[index + 3]) + 1, -p[index + 4]);
					grad[1 + index] = -(p[index + 0] * p[index + 4] * (2 * p[index + 1] - 2 * x[0])) / (p[index + 3] * p[index + 3] * Math.Pow(((p[index + 1] - x[0]) * (p[index + 1] - x[0]) + (p[index + 2] - x[1]) * (p[index + 2] - x[1])) / (p[index + 3] * p[index + 3]) + 1, (p[index + 4] + 1)));
					grad[2 + index] = -(p[index + 0] * p[index + 4] * (2 * p[index + 2] - 2 * x[1])) / (p[index + 3] * p[index + 3] * Math.Pow(((p[index + 1] - x[0]) * (p[index + 1] - x[0]) + (p[index + 2] - x[1]) * (p[index + 2] - x[1])) / (p[index + 3] * p[index + 3]) + 1, (p[index + 4] + 1)));
					grad[3 + index] = (2 * p[index + 0] * p[index + 4] * ((p[index + 1] - x[0]) * (p[index + 1] - x[0]) + (p[index + 2] - x[1]) * (p[index + 2] - x[1]))) / (p[index + 3] * p[index + 3] * p[index + 3] * Math.Pow(((p[index + 1] - x[0]) * (p[index + 1] - x[0]) + (p[index + 2] - x[1]) * (p[index + 2] - x[1])) / (p[index + 3] * p[index + 3]) + 1, (p[index + 4] + 1)));
					grad[4 + index] = -(p[index + 0] * Math.Log(((p[index + 1] - x[0]) * (p[index + 1] - x[0]) + (p[index + 2] - x[1]) * (p[index + 2] - x[1])) / (p[index + 3] * p[index + 3]) + 1)) / Math.Pow(((p[index + 1] - x[0]) * (p[index + 1] - x[0]) + (p[index + 2] - x[1]) * (p[index + 2] - x[1])) / (p[index + 3] * p[index + 3]) + 1, p[index + 4]);
				}
			}
			if (func == 8)
			{
				for (int i = 0; i < count; i++)
				{
					int index = i * (func - 1);
					val += p[index + 0] * Math.Pow(1.0 + (Math.Pow((x[0] - p[index + 1]) * Math.Cos(p[index + 3]) + (x[1] - p[index + 2]) * Math.Sin(p[index + 3]), 2)) / (p[index + 4] * p[index + 4]) + (Math.Pow(-(x[0] - p[index + 1]) * Math.Sin(p[index + 3]) + (x[1] - p[index + 2]) * Math.Cos(p[index + 3]), 2)) / (p[index + 5] * p[index + 5]), -p[index + 6]);
					grad[0 + index] = Math.Pow(Math.Pow(Math.Cos(p[index + 3]) * (p[index + 1] - x[0]) + Math.Sin(p[index + 3]) * (p[index + 2] - x[1]), 2) / (p[index + 4] * p[index + 4]) + Math.Pow(Math.Cos(p[index + 3]) * (p[index + 2] - x[1]) - Math.Sin(p[index + 3]) * (p[index + 1] - x[0]), 2) / (p[index + 5] * p[index + 5]) + 1, -p[index + 6]);
					grad[1 + index] = -(p[index + 0] * p[index + 6] * ((2 * Math.Cos(p[index + 3]) * (Math.Cos(p[index + 3]) * (p[index + 1] - x[0]) + Math.Sin(p[index + 3]) * (p[index + 2] - x[1]))) / (p[index + 4] * p[index + 4]) - (2 * Math.Sin(p[index + 3]) * (Math.Cos(p[index + 3]) * (p[index + 2] - x[1]) - Math.Sin(p[index + 3]) * (p[index + 1] - x[0]))) / (p[index + 5] * p[index + 5]))) / Math.Pow(Math.Pow(Math.Cos(p[index + 3]) * (p[index + 1] - x[0]) + Math.Sin(p[index + 3]) * (p[index + 2] - x[1]), 2) / (p[index + 4] * p[index + 4]) + Math.Pow(Math.Cos(p[index + 3]) * (p[index + 2] - x[1]) - Math.Sin(p[index + 3]) * (p[index + 1] - x[0]), 2) / (p[index + 5] * p[index + 5]) + 1, (p[index + 6] + 1));
					grad[2 + index] = -(p[index + 0] * p[index + 6] * ((2 * Math.Cos(p[index + 3]) * (Math.Cos(p[index + 3]) * (p[index + 2] - x[1]) - Math.Sin(p[index + 3]) * (p[index + 1] - x[0]))) / (p[index + 5] * p[index + 5]) + (2 * Math.Sin(p[index + 3]) * (Math.Cos(p[index + 3]) * (p[index + 1] - x[0]) + Math.Sin(p[index + 3]) * (p[index + 2] - x[1]))) / (p[index + 4] * p[index + 4]))) / Math.Pow(Math.Pow(Math.Cos(p[index + 3]) * (p[index + 1] - x[0]) + Math.Sin(p[index + 3]) * (p[index + 2] - x[1]), 2) / (p[index + 4] * p[index + 4]) + Math.Pow(Math.Cos(p[index + 3]) * (p[index + 2] - x[1]) - Math.Sin(p[index + 3]) * (p[index + 1] - x[0]), 2) / (p[index + 5] * p[index + 5]) + 1, (p[index + 6] + 1));
					grad[3 + index] = -(p[index + 0] * p[index + 6] * ((2 * (Math.Cos(p[index + 3]) * (p[index + 1] - x[0]) + Math.Sin(p[index + 3]) * (p[index + 2] - x[1])) * (Math.Cos(p[index + 3]) * (p[index + 2] - x[1]) - Math.Sin(p[index + 3]) * (p[index + 1] - x[0]))) / (p[index + 4] * p[index + 4]) - (2 * (Math.Cos(p[index + 3]) * (p[index + 1] - x[0]) + Math.Sin(p[index + 3]) * (p[index + 2] - x[1])) * (Math.Cos(p[index + 3]) * (p[index + 2] - x[1]) - Math.Sin(p[index + 3]) * (p[index + 1] - x[0]))) / (p[index + 5] * p[index + 5]))) / Math.Pow(Math.Pow(Math.Cos(p[index + 3]) * (p[index + 1] - x[0]) + Math.Sin(p[index + 3]) * (p[index + 2] - x[1]), 2) / (p[index + 4] * p[index + 4]) + Math.Pow(Math.Cos(p[index + 3]) * (p[index + 2] - x[1]) - Math.Sin(p[index + 3]) * (p[index + 1] - x[0]), 2) / (p[index + 5] * p[index + 5]) + 1, (p[index + 6] + 1));
					grad[4 + index] = (2 * p[index + 0] * p[index + 6] * Math.Pow(Math.Cos(p[index + 3]) * (p[index + 1] - x[0]) + Math.Sin(p[index + 3]) * (p[index + 2] - x[1]), 2)) / (p[index + 4] * p[index + 4] * p[index + 4] * Math.Pow(Math.Pow(Math.Cos(p[index + 3]) * (p[index + 1] - x[0]) + Math.Sin(p[index + 3]) * (p[index + 2] - x[1]), 2) / (p[index + 4] * p[index + 4]) + Math.Pow(Math.Cos(p[index + 3]) * (p[index + 2] - x[1]) - Math.Sin(p[index + 3]) * (p[index + 1] - x[0]), 2) / (p[index + 5] * p[index + 5]) + 1, (p[index + 6] + 1)));
					grad[5 + index] = (2 * p[index + 0] * p[index + 6] * Math.Pow(Math.Cos(p[index + 3]) * (p[index + 2] - x[1]) - Math.Sin(p[index + 3]) * (p[index + 1] - x[0]), 2)) / (p[index + 5] * p[index + 5] * p[index + 5] * Math.Pow(Math.Pow(Math.Cos(p[index + 3]) * (p[index + 1] - x[0]) + Math.Sin(p[index + 3]) * (p[index + 2] - x[1]), 2) / (p[index + 4] * p[index + 4]) + Math.Pow(Math.Cos(p[index + 3]) * (p[index + 2] - x[1]) - Math.Sin(p[index + 3]) * (p[index + 1] - x[0]), 2) / (p[index + 5] * p[index + 5]) + 1, (p[index + 6] + 1)));
					grad[6 + index] = -(p[index + 0] * Math.Log(Math.Pow(Math.Cos(p[index + 3]) * (p[index + 1] - x[0]) + Math.Sin(p[index + 3]) * (p[index + 2] - x[1]), 2) / (p[index + 4] * p[index + 4]) + Math.Pow(Math.Cos(p[index + 3]) * (p[index + 2] - x[1]) - Math.Sin(p[index + 3]) * (p[index + 1] - x[0]), 2) / (p[index + 5] * p[index + 5]) + 1)) / Math.Pow(Math.Pow(Math.Cos(p[index + 3]) * (p[index + 1] - x[0]) + Math.Sin(p[index + 3]) * (p[index + 2] - x[1]), 2) / (p[index + 4] * p[index + 4]) + Math.Pow(Math.Cos(p[index + 3]) * (p[index + 2] - x[1]) - Math.Sin(p[index + 3]) * (p[index + 1] - x[0]), 2) / (p[index + 5] * p[index + 5]) + 1, p[index + 6]);
				}
			}
		}

		public static void alglib_WCSTransform2d_fvec(double[] p, double[] f, object obj)
		{
			object[] objj = (object[])obj;
			double[] x_pix = (double[])objj[0];
			double[] y_pix = (double[])objj[1];
			double[] x_intrmdt = (double[])objj[2];
			double[] y_intrmdt = (double[])objj[3];

			double xres, yres;

			if (p.Length == 4)
			{
				double cosp1 = Math.Cos(p[1]);
				double sinp1 = Math.Sin(p[1]);
				double xpiximp2, ypiximp3;

				for (int i = 0; i < x_intrmdt.Length; i++)
				{
					xpiximp2 = x_pix[i] - p[2];
					ypiximp3 = y_pix[i] - p[3];

					xres = p[0] * (cosp1 * xpiximp2 - sinp1 * ypiximp3) - x_intrmdt[i];
					yres = p[0] * (sinp1 * xpiximp2 + cosp1 * ypiximp3) - y_intrmdt[i];

					f[i] = Math.Sqrt(xres * xres + yres * yres);
				}
				return;
			}

			if (p.Length == 6)
			{
				double xpiximp4, ypiximp5;

				for (int i = 0; i < x_pix.Length; i++)
				{
					xpiximp4 = x_pix[i] - p[4];
					ypiximp5 = y_pix[i] - p[5];

					xres = p[0] * xpiximp4 + p[1] * ypiximp5 - x_intrmdt[i];
					yres = p[2] * xpiximp4 + p[3] * ypiximp5 - y_intrmdt[i];

					f[i] = Math.Sqrt(xres * xres + yres * yres);
				}
			}
		}

		public static void alglib_WCSTransform2d_jac(double[] p, double[] f, double[,] jac, object obj)
		{
			object[] objj = (object[])obj;
			double[] x_pix = (double[])objj[0];
			double[] y_pix = (double[])objj[1];
			double[] x_intrmdt = (double[])objj[2];
			double[] y_intrmdt = (double[])objj[3];

			alglib_WCSTransform2d_fvec(p, f, obj);

			if (p.Length == 4)
			{
				double cosp1 = Math.Cos(p[1]);
				double sinp1 = Math.Sin(p[1]);
				double p2mxpixi, p3mypixi, cosp1p2mxpixisinp1p3mypixi, twox_intrmdtip0cosp1p2mxpixisinp1p3mypixisqy_intrmdtip0cosp1p3mypixisinp1p2mxpixisqroot, x_intrmdtip0cosp1p2mxpixisinp1p3mypixisqy_intrmdtip0cosp1p3mypixisinp1p2mxpixisq, cosp1p3mypixisinp1p2mxpixi, x_intrmdtip0cosp1p2mxpixisinp1p3mypixi, y_intrmdtip0cosp1p3mypixisinp1p2mxpixi, x_intrmdtip0cosp1p2mxpixisinp1p3mypixisq, y_intrmdtip0cosp1p3mypixisinp1p2mxpixisq;

				for (int i = 0; i < x_intrmdt.Length; i++)
				{
					p2mxpixi = p[2] - x_pix[i];
					p3mypixi = p[3] - y_pix[i];
					cosp1p2mxpixisinp1p3mypixi = cosp1 * p2mxpixi - sinp1 * p3mypixi;
					cosp1p3mypixisinp1p2mxpixi = cosp1 * p3mypixi + sinp1 * p2mxpixi;
					x_intrmdtip0cosp1p2mxpixisinp1p3mypixi = x_intrmdt[i] + p[0] * cosp1p2mxpixisinp1p3mypixi;
					y_intrmdtip0cosp1p3mypixisinp1p2mxpixi = y_intrmdt[i] + p[0] * cosp1p3mypixisinp1p2mxpixi;
					x_intrmdtip0cosp1p2mxpixisinp1p3mypixisq = x_intrmdtip0cosp1p2mxpixisinp1p3mypixi * x_intrmdtip0cosp1p2mxpixisinp1p3mypixi;
					y_intrmdtip0cosp1p3mypixisinp1p2mxpixisq = y_intrmdtip0cosp1p3mypixisinp1p2mxpixi * y_intrmdtip0cosp1p3mypixisinp1p2mxpixi;
					x_intrmdtip0cosp1p2mxpixisinp1p3mypixisqy_intrmdtip0cosp1p3mypixisinp1p2mxpixisq = x_intrmdtip0cosp1p2mxpixisinp1p3mypixisq + y_intrmdtip0cosp1p3mypixisinp1p2mxpixisq;
					twox_intrmdtip0cosp1p2mxpixisinp1p3mypixisqy_intrmdtip0cosp1p3mypixisinp1p2mxpixisqroot = 2 * Math.Sqrt(x_intrmdtip0cosp1p2mxpixisinp1p3mypixisqy_intrmdtip0cosp1p3mypixisinp1p2mxpixisq);

					jac[i, 0] = (2 * cosp1p2mxpixisinp1p3mypixi * x_intrmdtip0cosp1p2mxpixisinp1p3mypixi + 2 * cosp1p3mypixisinp1p2mxpixi * y_intrmdtip0cosp1p3mypixisinp1p2mxpixi) / twox_intrmdtip0cosp1p2mxpixisinp1p3mypixisqy_intrmdtip0cosp1p3mypixisinp1p2mxpixisqroot;
					jac[i, 1] = -(2 * p[0] * cosp1p3mypixisinp1p2mxpixi * x_intrmdtip0cosp1p2mxpixisinp1p3mypixi - 2 * p[0] * cosp1p2mxpixisinp1p3mypixi * y_intrmdtip0cosp1p3mypixisinp1p2mxpixi) / twox_intrmdtip0cosp1p2mxpixisinp1p3mypixisqy_intrmdtip0cosp1p3mypixisinp1p2mxpixisqroot;
					jac[i, 2] = (2 * p[0] * cosp1 * x_intrmdtip0cosp1p2mxpixisinp1p3mypixi + 2 * p[0] * sinp1 * y_intrmdtip0cosp1p3mypixisinp1p2mxpixi) / twox_intrmdtip0cosp1p2mxpixisinp1p3mypixisqy_intrmdtip0cosp1p3mypixisinp1p2mxpixisqroot;
					jac[i, 3] = (2 * p[0] * cosp1 * y_intrmdtip0cosp1p3mypixisinp1p2mxpixi - 2 * p[0] * sinp1 * x_intrmdtip0cosp1p2mxpixisinp1p3mypixi) / twox_intrmdtip0cosp1p2mxpixisinp1p3mypixisqy_intrmdtip0cosp1p3mypixisinp1p2mxpixisqroot;
				}
				return;
			}

			if (p.Length == 6)
			{
				double p4mx_pixi, p5my_pixi, p0p4mx_pixi, rootx_intrmdtip0p4mx_pixip1p5my_pixisqy_intrmdtip2p4mx_pixip3p5my_pixisq, x_intrmdtip0p4mx_pixip1p5my_pixisqy_intrmdtip2p4mx_pixip3p5my_pixisq, p1p5my_pixi, p2p4mx_pixi, p3p5my_pixi, x_intrmdtip0p4mx_pixip1p5my_pixi, y_intrmdtip2p4mx_pixip3p5my_pixi, x_intrmdtip0p4mx_pixip1p5my_pixisq, y_intrmdtip2p4mx_pixip3p5my_pixisq;

				for (int i = 0; i < x_pix.Length; i++)
				{
					p4mx_pixi = p[4] - x_pix[i];
					p5my_pixi = p[5] - y_pix[i];
					p0p4mx_pixi = p[0] * p4mx_pixi;
					p1p5my_pixi = p[1] * p5my_pixi;
					p2p4mx_pixi = p[2] * p4mx_pixi;
					p3p5my_pixi = p[3] * p5my_pixi;
					x_intrmdtip0p4mx_pixip1p5my_pixi = x_intrmdt[i] + p0p4mx_pixi + p1p5my_pixi;
					y_intrmdtip2p4mx_pixip3p5my_pixi = y_intrmdt[i] + p2p4mx_pixi + p3p5my_pixi;
					x_intrmdtip0p4mx_pixip1p5my_pixisq = x_intrmdtip0p4mx_pixip1p5my_pixi * x_intrmdtip0p4mx_pixip1p5my_pixi;
					y_intrmdtip2p4mx_pixip3p5my_pixisq = y_intrmdtip2p4mx_pixip3p5my_pixi * y_intrmdtip2p4mx_pixip3p5my_pixi;
					x_intrmdtip0p4mx_pixip1p5my_pixisqy_intrmdtip2p4mx_pixip3p5my_pixisq = x_intrmdtip0p4mx_pixip1p5my_pixisq + y_intrmdtip2p4mx_pixip3p5my_pixisq;
					rootx_intrmdtip0p4mx_pixip1p5my_pixisqy_intrmdtip2p4mx_pixip3p5my_pixisq = Math.Sqrt(x_intrmdtip0p4mx_pixip1p5my_pixisqy_intrmdtip2p4mx_pixip3p5my_pixisq);

					jac[i, 0] = (p4mx_pixi * x_intrmdtip0p4mx_pixip1p5my_pixi) / rootx_intrmdtip0p4mx_pixip1p5my_pixisqy_intrmdtip2p4mx_pixip3p5my_pixisq;
					jac[i, 1] = (p5my_pixi * x_intrmdtip0p4mx_pixip1p5my_pixi) / rootx_intrmdtip0p4mx_pixip1p5my_pixisqy_intrmdtip2p4mx_pixip3p5my_pixisq;
					jac[i, 2] = (p4mx_pixi * y_intrmdtip2p4mx_pixip3p5my_pixi) / rootx_intrmdtip0p4mx_pixip1p5my_pixisqy_intrmdtip2p4mx_pixip3p5my_pixisq;
					jac[i, 3] = (p5my_pixi * y_intrmdtip2p4mx_pixip3p5my_pixi) / rootx_intrmdtip0p4mx_pixip1p5my_pixisqy_intrmdtip2p4mx_pixip3p5my_pixisq;
					jac[i, 4] = (2 * p[0] * x_intrmdtip0p4mx_pixip1p5my_pixi + 2 * p[2] * y_intrmdtip2p4mx_pixip3p5my_pixi) / (2 * rootx_intrmdtip0p4mx_pixip1p5my_pixisqy_intrmdtip2p4mx_pixip3p5my_pixisq);
					jac[i, 5] = (2 * p[1] * x_intrmdtip0p4mx_pixip1p5my_pixi + 2 * p[3] * y_intrmdtip2p4mx_pixip3p5my_pixi) / (2 * rootx_intrmdtip0p4mx_pixip1p5my_pixisqy_intrmdtip2p4mx_pixip3p5my_pixisq);
				}
			}
		}

		public static void alglib_GeneralTransform2d_fvec(double[] p, double[] f, object obj)
		{
			object[] objj = (object[])obj;
			double[] x_tran = (double[])objj[0];
			double[] y_tran = (double[])objj[1];
			double[] x_ref = (double[])objj[2];
			double[] y_ref = (double[])objj[3];

			if (p.Length == 6)
			{
				double xres, yres;

				for (int i = 0; i < x_ref.Length; i++)
				{
					xres = p[0] * (Math.Cos(p[1]) * (x_tran[i] - p[2]) - Math.Sin(p[1]) * (y_tran[i] - p[3])) + p[2] + p[4] - x_ref[i];
					yres = p[0] * (Math.Sin(p[1]) * (x_tran[i] - p[2]) + Math.Cos(p[1]) * (y_tran[i] - p[3])) + p[3] + p[5] - y_ref[i];

					f[i] = Math.Sqrt(xres * xres + yres * yres);
				}
				return;
			}

			if (p.Length == 8)
			{
				double xres, yres;

				for (int i = 0; i < x_tran.Length; i++)
				{
					xres = p[0] * (x_tran[i] - p[4]) + p[1] * (y_tran[i] - p[5]) + p[4] + p[6] - x_ref[i];
					yres = p[2] * (x_tran[i] - p[4]) + p[3] * (y_tran[i] - p[5]) + p[5] + p[7] - y_ref[i];

					f[i] = Math.Sqrt(xres * xres + yres * yres);
				}
			}
		}

		public static void alglib_Fourier_Polynomial(double[] p, double[] x, ref double val, object obj)
		{
			int order = (int)((double[])obj)[0];

			double x0omega = x[0] * p[p.Length - 1] / 2 / Math.PI;

			val = p[p.Length - 2];//mean added in once

			for (int i = 0; i < order; i++)
				val += p[i * 2] * Math.Cos((double)(i + 1) * x0omega) + p[i * 2 + 1] * Math.Sin((double)(i + 1) * x0omega);
		}

		#endregion

		#region FIND AND REPLACE

		/// <summary>Returns an array with the indeces at which the 2D data array satisfies the matching style for the given value.
		/// <br />The return array is an n x 2 array giving the row [n, 0] and column [n, 1] indices of the match.</summary>
		/// <param name="data">The data array to check for matches.</param>
		/// <param name="val">The value with which to check for a match in the data array.</param>
		/// <param name="style">The matching style can be &lt;, &lt;=, ==, &gt;=, &gt;, !=.</param>
		public static void Find(double[,] data, double val, string style, bool do_parallel, out int[] xIndices, out int[] yIndices)
		{
			ArrayList ptslist = new ArrayList();

			int method = 0;
			if (style.Equals("<"))
				method = 0;
			else if (style.Equals("<="))
				method = 1;
			else if (style.Equals("==") || style.Equals("="))
				method = 2;
			else if (style.Equals(">="))
				method = 3;
			else if (style.Equals(">"))
				method = 4;
			else if (style.Equals("!="))
				method = 5;
			else
				throw new Exception("Error:  Search style '" + style + "' not meaningful.");

			if (Double.IsNaN(val))
				method = 6;

			if (Double.IsPositiveInfinity(val))
				method = 7;

			if (Double.IsNegativeInfinity(val))
				method = 8;

			ParallelOptions opt = new ParallelOptions();
			if (do_parallel)
				opt.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opt.MaxDegreeOfParallelism = 1;

			object locker = new object();

			Parallel.For(0, data.GetLength(0), opt, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
				{
					switch (method)
					{
						case 0://<
						{
							if (data[i, j] < val)
							{
								lock (locker)
								{
									ptslist.Add(i);
									ptslist.Add(j);
								}
							}
							break;
						}
						case 1://<=
						{
							if (data[i, j] <= val)
							{
								lock (locker)
								{
									ptslist.Add(i);
									ptslist.Add(j);
								}
							}
							break;
						}
						case 2://== || =
						{
							if (data[i, j] == val)
							{
								lock (locker)
								{
									ptslist.Add(i);
									ptslist.Add(j);
								}
							}
							break;
						}
						case 3://>=
						{
							if (data[i, j] >= val)
							{
								lock (locker)
								{
									ptslist.Add(i);
									ptslist.Add(j);
								}
							}
							break;
						}
						case 4://>
						{
							if (data[i, j] > val)
							{
								lock (locker)
								{
									ptslist.Add(i);
									ptslist.Add(j);
								}
							}
							break;
						}
						case 5://!=
						{
							if (data[i, j] != val)
							{
								lock (locker)
								{
									ptslist.Add(i);
									ptslist.Add(j);
								}
							}
							break;
						}
						case 6://= NaN
						{
							if (Double.IsNaN(data[i, j]))
							{
								lock (locker)
								{
									ptslist.Add(i);
									ptslist.Add(j);
								}
							}
							break;
						}
						case 7://= +ve inf
						{
							if (Double.IsPositiveInfinity(data[i, j]))
							{
								lock (locker)
								{
									ptslist.Add(i);
									ptslist.Add(j);
								}
							}
							break;
						}
						case 8://= -ve inf
						{
							if (Double.IsNegativeInfinity(data[i, j]))
							{
								lock (locker)
								{
									ptslist.Add(i);
									ptslist.Add(j);
								}
							}
							break;
						}
					}
				}
			});

			int N = ptslist.Count / 2;
			xIndices = new int[N];
			yIndices = new int[N];

			for (int i = 0; i < N; i++)
			{
				xIndices[i] = System.Convert.ToInt32(ptslist[i * 2]);//x
				yIndices[i] = System.Convert.ToInt32(ptslist[i * 2 + 1]);//y
			}
		}

		/// <summary>Returns an array with the indeces at which the 1D data array satisfies the matching style for the given value.</summary>
		/// <param name="data">The data array to check for matches.</param>
		/// <param name="val">The value with which to check for a match in the data array.</param>
		/// <param name="style">The matching style can be &lt;, &lt;=, ==, &gt;=, &gt;, !=.</param>
		public static int[] Find(double[] data, double val, string style)
		{
			ArrayList ptslist = new ArrayList();

			int method = 0;
			if (style.Equals("<"))
				method = 0;
			else if (style.Equals("<="))
				method = 1;
			else if (style.Equals("==") || style.Equals("="))
				method = 2;
			else if (style.Equals(">="))
				method = 3;
			else if (style.Equals(">"))
				method = 4;
			else if (style.Equals("!="))
				method = 5;
			else
				throw new Exception("Error:  Search style '" + style + "' not meaningful.");

			for (int i = 0; i < data.Length; i++)
			{
				switch (method)
				{
					case 0://<
					{
						if (data[i] < val)
							ptslist.Add(i);
						break;
					}
					case 1://<=
					{
						if (data[i] <= val)
							ptslist.Add(i);
						break;
					}
					case 2://==
					{
						if (data[i] == val)
							ptslist.Add(i);
						break;
					}
					case 3://>=
					{
						if (data[i] >= val)
							ptslist.Add(i);
						break;
					}
					case 4://>
					{
						if (data[i] > val)
							ptslist.Add(i);
						break;
					}
					case 5://!=
					{
						if (data[i] != val)
							ptslist.Add(i);
						break;
					}
				}
			}

			int N = ptslist.Count;
			int[] result = new int[N];

			for (int i = 0; i < N; i++)
				result[i] = System.Convert.ToInt32(ptslist[i]);

			return result;
		}

		/// <summary>Returns an array with the indeces at which the 1D data array satisfies the matching style for the given value.</summary>
		/// <param name="data">The data array to check for matches.</param>
		/// <param name="val">The value with which to check for a match in the data array.</param>
		/// <param name="style">The matching style can be &lt;, &lt;=, ==, &gt;=, &gt;, !=.</param>
		/// <param name="startindex">The starting index at which to begin checking for matches.</param>
		public static int[] Find(double[] data, double val, string style, int startindex)
		{
			if (startindex < 0)
				startindex = 0;

			ArrayList ptslist = new ArrayList();

			int method = 0;
			if (style.Equals("<"))
				method = 0;
			else if (style.Equals("<="))
				method = 1;
			else if (style.Equals("==") || style.Equals("="))
				method = 2;
			else if (style.Equals(">="))
				method = 3;
			else if (style.Equals(">"))
				method = 4;
			else if (style.Equals("!="))
				method = 5;
			else
				throw new Exception("Error:  Search style '" + style + "' not meaningful.");

			for (int i = startindex; i < data.Length; i++)
			{
				switch (method)
				{
					case 0://<
					{
						if (data[i] < val)
							ptslist.Add(i);
						break;
					}
					case 1://<=
					{
						if (data[i] <= val)
							ptslist.Add(i);
						break;
					}
					case 2://==
					{
						if (data[i] == val)
							ptslist.Add(i);
						break;
					}
					case 3://>=
					{
						if (data[i] >= val)
							ptslist.Add(i);
						break;
					}
					case 4://>
					{
						if (data[i] > val)
							ptslist.Add(i);
						break;
					}
					case 5://!=
					{
						if (data[i] != val)
							ptslist.Add(i);
						break;
					}
				}
			}

			int N = ptslist.Count;
			int[] result = new int[N];

			for (int i = 0; i < N; i++)
				result[i] = System.Convert.ToInt32(ptslist[i]);

			return result;
		}

		/// <summary>Returns an array with the indeces at which the 1D data array satisfies the matching style for the given value.</summary>
		/// <param name="data">The data array to check for matches.</param>
		/// <param name="val">The value with which to check for a match in the data array.</param>
		/// <param name="style">The matching style can be &lt;, &lt;=, ==, &gt;=, &gt;, !=.</param>
		/// <param name="startindex">The starting index at which to begin checking for matches.</param>
		/// <param name="endindex">The ending index at which to stop checking for matches.</param>
		public static int[] Find(double[] data, double val, string style, int startindex, int endindex)
		{
			if (startindex < 0)
				startindex = 0;
			if (endindex > data.Length)
				endindex = data.Length - 1;

			ArrayList ptslist = new ArrayList();

			int method = 0;
			if (style.Equals("<"))
				method = 0;
			else if (style.Equals("<="))
				method = 1;
			else if (style.Equals("==") || style.Equals("="))
				method = 2;
			else if (style.Equals(">="))
				method = 3;
			else if (style.Equals(">"))
				method = 4;
			else if (style.Equals("!="))
				method = 5;
			else
				throw new Exception("Error:  Search style '" + style + "' not meaningful.");

			for (int i = startindex; i <= endindex; i++)
			{
				switch (method)
				{
					case 0://<
					{
						if (data[i] < val)
							ptslist.Add(i);
						break;
					}
					case 1://<=
					{
						if (data[i] <= val)
							ptslist.Add(i);
						break;
					}
					case 2://==
					{
						if (data[i] == val)
							ptslist.Add(i);
						break;
					}
					case 3://>=
					{
						if (data[i] >= val)
							ptslist.Add(i);
						break;
					}
					case 4://>
					{
						if (data[i] > val)
							ptslist.Add(i);
						break;
					}
					case 5://!=
					{
						if (data[i] != val)
							ptslist.Add(i);
						break;
					}
				}
			}

			int N = ptslist.Count;
			int[] result = new int[N];

			for (int i = 0; i < N; i++)
				result[i] = System.Convert.ToInt32(ptslist[i]);

			return result;
		}

		/// <summary>Returns either the first or last index in the data array that satisfies the match.</summary>
		/// <param name="data">The data array to check for matches.</param>
		/// <param name="val">The value with which to check for a match in the data array.</param>
		/// <param name="style">The matching style can be &lt;, &lt;=, ==, &gt;=, &gt;, !=.</param>
		/// <param name="return_first_true_last_false">Return first index of the match (true) or the last index (false).</param>
		public static int Find(double[] data, double val, string style, bool return_first_true_last_false)
		{
			int method = 0;
			if (style == "<")
				method = 0;
			else if (style == "<=")
				method = 1;
			else if (style == "==" || style == "=")
				method = 2;
			else if (style == ">=")
				method = 3;
			else if (style == ">")
				method = 4;
			else if (style == "!=")
				method = 5;
			else
				throw new Exception("Error:  Search style '" + style + "' not meaningful.");

			int index = -1;

			if (return_first_true_last_false)
				for (int i = 0; i < data.Length; i++)
				{
					switch (method)
					{
						case 0://<
						{
							if (data[i] < val)
								index = i;
							break;
						}
						case 1://<=
						{
							if (data[i] <= val)
								index = i;
							break;
						}
						case 2://==
						{
							if (data[i] == val)
								index = i;
							break;
						}
						case 3://>=
						{
							if (data[i] >= val)
								index = i;
							break;
						}
						case 4://>
						{
							if (data[i] > val)
								index = i;
							break;
						}
						case 5://!=
						{
							if (data[i] != val)
								index = i;
							break;
						}
					}
					if (index != -1)
						break;
				}
			else
				for (int i = data.Length - 1; i >= 0; i--)
				{
					switch (method)
					{
						case 0://<
						{
							if (data[i] < val)
								index = i;
							break;
						}
						case 1://<=
						{
							if (data[i] <= val)
								index = i;
							break;
						}
						case 2://==
						{
							if (data[i] == val)
								index = i;
							break;
						}
						case 3://>=
						{
							if (data[i] >= val)
								index = i;
							break;
						}
						case 4://>
						{
							if (data[i] > val)
								index = i;
							break;
						}
						case 5://!=
						{
							if (data[i] != val)
								index = i;
							break;
						}
					}
					if (index != -1)
						break;
				}

			return index;
		}

		/// <summary>Returns a new array with all values at the given indeces replaced with the given value.</summary>
		/// <param name="data">A 2-D double array.</param>
		/// <param name="xcoords">The horizontal indexes of the values to be replaced.</param>
		/// /// <param name="ycoords">The vertical indexes of the values to be replaced.</param>
		/// <param name="val">The value with which to replace at the given indices.</param>
		public static double[,] Replace(double[,] data, int[] xcoords, int[] ycoords, double val, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			Array.Copy(data, result, data.Length);
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, xcoords.Length, opts, i =>
			{
				result[xcoords[i], ycoords[i]] = val;
			});

			return result;
		}

		/// <summary>Returns a new array with all values at the given indeces replaced with the given value.</summary>
		/// <param name="data">A 2-D double array.</param>
		/// <param name="coords">An array giving the indices at which to replace the values.</param>
		/// <param name="val">The value with which to replace at the given indices.</param>
		public static double[] Replace(double[] data, int[] coords, double val)
		{
			double[] result = data;
			for (int i = 0; i < coords.Length; i++)
				result[coords[i]] = val;

			return result;
		}

		#endregion			

		#region ARRAY OPERATORS
		
		public static double[] VectorDivScalar(double[] vector, double scalar, bool do_parallel = false)
		{
			double[] result = new double[vector.Length];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;


			Parallel.For(0, vector.Length, opts, i =>
			{
				result[i] = vector[i] / scalar;
			});

			return result;
		}

		public static double[] VectorDivVector(double[] vector1, double[] vector2, bool do_parallel = false)
		{
			double[] result = new double[vector1.Length];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, vector1.Length, opts, i =>
			{
				result[i] = vector1[i] / vector2[i];
			});

			return result;
		}

		public static double[,] MatrixDivScalar(double[,] matrix, double scalar, bool do_parallel = false)
		{
			double[,] result = new double[matrix.GetLength(0), matrix.GetLength(1)];
			scalar = 1 / scalar;

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, matrix.GetLength(0), opts, i =>
			{
				for (int j = 0; j < matrix.GetLength(1); j++)
					result[i, j] = matrix[i, j] * scalar;
			});

			return result;
		}

		public static double[,] MatrixDivMatrix(double[,] matrix1, double[,] matrix2, bool do_parallel = false)
		{
			double[,] result = new double[matrix1.GetLength(0), matrix1.GetLength(1)];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, matrix1.GetLength(0), opts, i =>
			{
				for (int j = 0; j < matrix1.GetLength(1); j++)
					result[i, j] = matrix1[i, j] / matrix2[i, j];
			});

			return result;
		}

		public static double[] VectorMultScalar(double[] vector, double scalar, bool do_parallel = false)
		{
			double[] result = new double[vector.Length];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, vector.Length, opts, i =>
			{
				result[i] = vector[i] * scalar;
			});

			return result;
		}

		public static double[] VectorMultVector(double[] vector1, double[] vector2, bool do_parallel = false)
		{
			double[] result = new double[vector1.Length];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, vector1.Length, opts, i =>
			{
				result[i] = vector1[i] * vector2[i];
			});

			return result;
		}

		public static double[,] MatrixMultScalar(double[,] matrix, double scalar, bool do_parallel = false)
		{
			double[,] result = new double[matrix.GetLength(0), matrix.GetLength(1)];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, matrix.GetLength(0), opts, i =>
			{
				for (int j = 0; j < matrix.GetLength(1); j++)
					result[i, j] = matrix[i, j] * scalar;
			});

			return result;
		}

		public static double[,] MatrixMultMatrix(double[,] matrix1, double[,] matrix2, bool do_parallel = false)
		{
			double[,] result = new double[matrix1.GetLength(0), matrix1.GetLength(1)];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, matrix1.GetLength(0), opts, i =>
			{
				for (int j = 0; j < matrix1.GetLength(1); j++)
					result[i, j] = matrix1[i, j] * matrix2[i, j];
			});

			return result;
		}

		public static double[] VectorAddScalar(double[] vector, double scalar, bool do_parallel = false)
		{
			double[] result = new double[vector.Length];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, vector.Length, opts, i =>
			{
				result[i] = vector[i] + scalar;
			});

			return result;
		}

		public static double[] VectorAddVector(double[] vector1, double[] vector2, bool do_parallel = false)
		{
			double[] result = new double[vector1.Length];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, vector1.Length, opts, i =>
			{
				result[i] = vector1[i] + vector2[i];
			});

			return result;
		}

		public static double[,] MatrixAddScalar(double[,] matrix, double scalar, bool do_parallel = false)
		{
			double[,] result = new double[matrix.GetLength(0), matrix.GetLength(1)];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, matrix.GetLength(0), opts, i =>
			{
				for (int j = 0; j < matrix.GetLength(1); j++)
					result[i, j] = matrix[i, j] + scalar;
			});

			return result;
		}

		public static double[,] MatrixAddMatrix(double[,] matrix1, double[,] matrix2, bool do_parallel = false)
		{
			double[,] result = new double[matrix1.GetLength(0), matrix1.GetLength(1)];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, matrix1.GetLength(0), opts, i =>
			{
				for (int j = 0; j < matrix1.GetLength(1); j++)
					result[i, j] = matrix1[i, j] + matrix2[i, j];
			});

			return result;
		}

		public static double[] VectorSubScalar(double[] vector, double scalar, bool do_parallel = false)
		{
			double[] result = new double[vector.Length];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, vector.Length, opts, i =>
			{
				result[i] = vector[i] - scalar;
			});

			return result;
		}

		public static double[] VectorSubVector(double[] vector1, double[] vector2, bool do_parallel = false)
		{
			double[] result = new double[vector1.Length];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, vector1.Length, opts, i =>
			{
				result[i] = vector1[i] - vector2[i];
			});

			return result;
		}

		public static double[,] MatrixSubScalar(double[,] matrix, double scalar, bool do_parallel = false)
		{
			double[,] result = new double[matrix.GetLength(0), matrix.GetLength(1)];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, matrix.GetLength(0), opts, i =>
			{
				for (int j = 0; j < matrix.GetLength(1); j++)
					result[i, j] = matrix[i, j] - scalar;
			});

			return result;
		}

		public static double[,] MatrixSubMatrix(double[,] matrix1, double[,] matrix2, bool do_parallel = false)
		{
			double[,] result = new double[matrix1.GetLength(0), matrix1.GetLength(1)];


			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, matrix1.GetLength(0), opts, i =>
			{
				for (int j = 0; j < matrix1.GetLength(1); j++)
					result[i, j] = matrix1[i, j] - matrix2[i, j];
			});

			return result;
		}

		#endregion

		#region ARRAY OPERATIONS

		/// <summary>Identifies and removes hot pixels from an image. The algorithm is not a simple find and replace, but assesses whether a pixel is part of a source with legitimate high values or is a solitary or paired high value which is simply hot.</summary>
		/// <param name="image">An image array with hot pixels.</param>
		/// <param name="countThreshold">The pixel value above which a pixel might be considered to be hot.</param>
		/// <param name="Nhot">The maximum number of hot pixels in a hot pixel cluster. Recommend 1, 2, sometimes 3 if hot pixels cluster in 3's.</param>
		/// <param name="doParallel">Perform array scan with parallelism.</param>
		public static double[,] DeSpeckle(double[,] image, double countThreshold, int Nhot, bool doParallel)
		{
			ParallelOptions opts = new ParallelOptions();
			if (doParallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			double[,] result = new double[image.GetLength(0), image.GetLength(1)];
			for (int y = 0; y < image.GetLength(1); y++)
			{
				result[0, y] = image[0, y];
				result[image.GetLength(0) - 1, y] = image[image.GetLength(0) - 1, y];
			}
			for (int x = 0; x < image.GetLength(0); x++)
			{
				result[x, 0] = image[x, 0];
				result[x, image.GetLength(1) - 1] = image[x, image.GetLength(1) - 1];
			}

			Parallel.For(1, image.GetLength(1) - 1, opts, y =>
			{
				int npix = 0;
				for (int x = 1; x < image.GetLength(0) - 1; x++)
				{
					result[x, y] = image[x, y];
					npix = 0;

					if (image[x, y] > countThreshold)
					{
						npix++;

						if (image[x - 1, y] > countThreshold)
							npix++;
						if (image[x + 1, y] > countThreshold)
							npix++;
						if (image[x - 1, y - 1] > countThreshold)
							npix++;
						if (image[x, y - 1] > countThreshold)
							npix++;
						if (image[x + 1, y - 1] > countThreshold)
							npix++;
						if (image[x - 1, y + 1] > countThreshold)
							npix++;
						if (image[x, y + 1] > countThreshold)
							npix++;
						if (image[x + 1, y + 1] > countThreshold)
							npix++;

						if (npix <= Nhot)
							result[x, y] = JPMath.Median(new double[8] { image[x - 1, y], image[x + 1, y], image[x - 1, y - 1], image[x, y - 1], image[x + 1, y - 1], image[x - 1, y + 1], image[x, y + 1], image[x + 1, y + 1] });
					}
				}
			});

			return result;
		}

		/// <summary>Determines the cross correlation lags between two images using the image-reduction-to-vector method.</summary>
		/// <param name="reference">The reference data array against which to create the cross correlation.</param>
		/// <param name="COMPARISON">The comparison data array with which to create the cross correlation.</param>
		/// <param name="autoDeBias_refX">Option to automatically de-gradient the reference image along the x-dimension (horizontal degradient).</param>
		/// <param name="autoDeBias_refY">Option to automatically de-gradient the reference image along the y-dimension (vertical degradient).</param>
		/// <param name="autoDeBias_COMX">Option to automatically de-gradient the comparison image along the x-dimension (horizontal degradient).</param>
		/// <param name="autoDeBias_COMY">Option to automatically de-gradient the comparison image along the y-dimension (vertical degradient).</param>
		/// <param name="autoHanning_ref">Option to automatically Hanning-window the reference image.</param>
		/// <param name="autoHanning_COM">Option to automatically Hanning-window the comparison image.</param>
		/// <param name="xshift">The sub-integer x-shift of the comparison with respect to the reference, passed by reference.</param>
		/// <param name="yshift">The sub-integer y-shift of the comparison with respect to the reference, passed by reference.</param>
		/// <param name="do_parallel">Optionally perform all array operations in parallel. False when parallelizing upstream.</param>
		public static void XCorrImageLagShifts(double[,] reference, double[,] COMPARISON, bool autoDeBias_refX, bool autoDeBias_refY, bool autoDeBias_COMX, bool autoDeBias_COMY, bool autoHanning_ref, bool autoHanning_COM, out double xshift, out double yshift, bool do_parallel = false)
		{
			double[,] refref = new double[reference.GetLength(0), reference.GetLength(1)];
			double[,] COMCOM = new double[COMPARISON.GetLength(0), COMPARISON.GetLength(1)];
			Array.Copy(reference, refref, reference.LongLength);
			Array.Copy(COMPARISON, COMCOM, COMCOM.LongLength);

			if (autoDeBias_refX)
				refref = DeGradient(refref, 0, do_parallel);
			if (autoDeBias_refY)
				refref = DeGradient(refref, 1, do_parallel);
			if (autoDeBias_COMX)
				COMCOM = DeGradient(COMCOM, 0, do_parallel);
			if (autoDeBias_COMY)
				COMCOM = DeGradient(COMCOM, 1, do_parallel);
			if (autoHanning_ref)
				refref = Hanning(refref, do_parallel);
			if (autoHanning_COM)
				COMCOM = Hanning(COMCOM, do_parallel);

			double[] Href = Sum(refref, 1, do_parallel);
			double[] Vref = Sum(refref, 0, do_parallel);
			double[] HCOM = Sum(COMCOM, 1, do_parallel);
			double[] VCOM = Sum(COMCOM, 0, do_parallel);
			double meanref = Median(Href);
			double meanCOM = Median(HCOM);
			Href = VectorSubScalar(Href, meanref, do_parallel);
			Vref = VectorSubScalar(Vref, meanref, do_parallel);
			HCOM = VectorSubScalar(HCOM, meanCOM, do_parallel);
			VCOM = VectorSubScalar(VCOM, meanCOM, do_parallel);

			double[] Hcorr_Amp = XCorr(Href, HCOM, out double[] Hcorr_Lag, do_parallel);
			double[] Vcorr_Amp = XCorr(Vref, VCOM, out double[] Vcorr_Lag, do_parallel);

			double maxH = Max(Hcorr_Amp, out int maxHAmpindex, do_parallel);
			double xmax = Hcorr_Lag[maxHAmpindex];
			double maxV = Max(Vcorr_Amp, out int maxVAmpindex, do_parallel);
			double ymax = Vcorr_Lag[maxVAmpindex];

			double[] Hx = new double[3] { (double)(xmax - 1), (double)(xmax), (double)(xmax + 1) };
			double[] Hy = new double[3] { Hcorr_Amp[maxHAmpindex - 1], Hcorr_Amp[maxHAmpindex], Hcorr_Amp[maxHAmpindex + 1] };
			double[] Vx = new double[3] { (double)(ymax - 1), (double)(ymax), (double)(ymax + 1) };
			double[] Vy = new double[3] { Vcorr_Amp[maxVAmpindex - 1], Vcorr_Amp[maxVAmpindex], Vcorr_Amp[maxVAmpindex + 1] };

			xshift = QuadFit3PtsCenterPos(Hx, Hy);
			yshift = QuadFit3PtsCenterPos(Vx, Vy);
		}

		/// <summary>Determines the cross correlation lags between two images using the image-reduction-to-vector method where the reference image has already been reduced to X and Y vectors.</summary>
		/// <param name="referenceX">The reference horizontal vector against which to create the cross correlation.</param>
		/// <param name="referenceY">The reference vertical vector against which to create the cross correlation.</param>
		/// <param name="COMPARISON">The comparison data array with which to create the cross correlation.</param>
		/// <param name="autoDeBias_COMX">Option to automatically de-gradient the comparison image along the x-dimension (horizontal degradient).</param>
		/// <param name="autoDeBias_COMY">Option to automatically de-gradient the comparison image along the y-dimension (vertical degradient).</param>
		/// <param name="xshift">The x-shift of the comparison with respect to the reference.</param>
		/// <param name="yshift">The y-shift of the comparison with respect to the reference.</param>
		/// <param name="do_parallel">Optionally perform all array operations in parallel.</param>
		public static void XCorrImageLagShifts(double[] referenceX, double[] referenceY, double[,] COMPARISON, bool autoDeBias_COMX, bool autoDeBias_COMY, bool autoHanning_COM, out double xshift, out double yshift, bool do_parallel = false)
		{
			double[,] COMCOM = new double[COMPARISON.GetLength(0), COMPARISON.GetLength(1)];
			Array.Copy(COMPARISON, COMCOM, COMCOM.LongLength);

			if (autoDeBias_COMX)
				COMCOM = DeGradient(COMCOM, 0, do_parallel);
			if (autoDeBias_COMY)
				COMCOM = DeGradient(COMCOM, 1, do_parallel);
			if (autoHanning_COM)
				COMCOM = Hanning(COMCOM, do_parallel);

			double[] HCOM = Sum(COMCOM, 1, do_parallel);
			double[] VCOM = Sum(COMCOM, 0, do_parallel);
			double meanCOM = Median(HCOM);
			HCOM = VectorSubScalar(HCOM, meanCOM, false);
			VCOM = VectorSubScalar(VCOM, meanCOM, false);

			double[] Hcorr_Amp = XCorr(referenceX, HCOM, out double[] Hcorr_Lag, do_parallel);
			double maxH = Max(Hcorr_Amp, out int maxHAmpindex, false);
			double xmax = Hcorr_Lag[maxHAmpindex];

			double[] Vcorr_Amp = XCorr(referenceY, VCOM, out double[] Vcorr_Lag, do_parallel);
			double maxV = Max(Vcorr_Amp, out int maxVAmpindex, false);
			double ymax = Vcorr_Lag[maxVAmpindex];

			double[] Hx = new double[3] { (double)(xmax - 1), (double)(xmax), (double)(xmax + 1) };
			double[] Hy = new double[3] { Hcorr_Amp[maxHAmpindex - 1], Hcorr_Amp[maxHAmpindex], Hcorr_Amp[maxHAmpindex + 1] };
			double[] Vx = new double[3] { (double)(ymax - 1), (double)(ymax), (double)(ymax + 1) };
			double[] Vy = new double[3] { Vcorr_Amp[maxVAmpindex - 1], Vcorr_Amp[maxVAmpindex], Vcorr_Amp[maxVAmpindex + 1] };

			xshift = QuadFit3PtsCenterPos(Hx, Hy);
			yshift = QuadFit3PtsCenterPos(Vx, Vy);

			//MessageBox.Show(xshift + " " + xmax);
			//MessageBox.Show(yshift + " " + ymax);

			//Plotter plot = new Plotter();
			//plot.jpChart1.PlotXYData(Hcorr_Lag, Hcorr_Amp, "t", "t", "t", System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine, "t");
			//plot.ShowDialog();
        }

		/// <summary>Returns the 2-D array with gradients removed from a specified dimension.</summary>
		/// <param name="data">The data array to degradient.</param>
		/// <param name="dim">The dimension to degradient: 0 = x, 1 = y.</param>
		public static double[,] DeGradient(double[,] data, int dim, bool do_parallel = false)
		{
			int width = data.GetLength(0);
			int height = data.GetLength(1);
			double[,] result = new double[width, height];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			if (dim == 0)
			{
				Parallel.For(0, width, opts, ii =>
				{
					double[] column = new double[height];
					for (int jj = 0; jj < height; jj++)
					{
						column[jj] = data[ii, jj];
						result[ii, jj] = data[ii, jj];
					}

					double med = Median(column);
					for (int kk = 0; kk < height; kk++)
						result[ii, kk] -= med;
				});
			}
			else// (dim == 1)
			{
				Parallel.For(0, height, opts, ii =>
				{
					double[] row = new double[width];
					for (int jj = 0; jj < width; jj++)
					{
						row[jj] = data[jj, ii];
						result[jj, ii] = data[jj, ii];
					}

					double med = Median(row);
					for (int jj = 0; jj < width; jj++)
						result[jj, ii] -= med;
				});
			}

			return result;
		}

		public static double[,] MedianFilter(double[,] data, int kernelHalfWidth, bool do_parallel = false)
		{
			int szx = data.GetLength(0);
			int szy = data.GetLength(1);
			double[,] result = new double[szx, szy];
			szx -= kernelHalfWidth;
			szy -= kernelHalfWidth;
			int Nkernpix = (kernelHalfWidth * 2 + 1) * (kernelHalfWidth * 2 + 1);

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(kernelHalfWidth, szx, opts, x =>
			{
				double[] kernel = new double[Nkernpix];
				int kxmax = x + kernelHalfWidth;
				for (int y = kernelHalfWidth; y < szy; y++)
				{
					int i = 0;
					int kymax = y + kernelHalfWidth;
					for (int kx = x - kernelHalfWidth; kx <= kxmax; kx++)
					{
						for (int ky = y - kernelHalfWidth; ky <= kymax; ky++)
						{
							kernel[i] = data[kx, ky];
							i++;
						}
					}
					result[x, y] = Median(kernel);
				}
			});

			return result;
		}

		public static double[,] ShiftArrayInt(double[,] data, int xshift, int yshift, bool do_parallel = false)
		{
			int width = data.GetLength(0);
			int height = data.GetLength(1);
			double[,] arr = new double[width, height];

			if (xshift == 0 && yshift == 0)
				return data;

			int istart = 0;
			int iend = width;
			//if shift is +ve, then it is shifting right, so need blank columns at left,
			if (xshift > 0)
			{
				istart = xshift;
				iend = width;
			}
			//if shift is -ve, then it is shifting left, so need blank columns at right,
			if (xshift < 0)
			{
				istart = 0;
				iend = width + xshift;
			}

			int jstart = 0;
			int jend = height;
			//if shift is +ve, then it is shifting down, so need blank columns at top,
			if (yshift > 0)
			{
				jstart = yshift;
				jend = height;
			}
			//if shift is -ve, then it is shifting up, so need blank columns at bottom,
			if (yshift < 0)
			{
				jstart = 0;
				jend = height + yshift;
			}

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(istart, iend, opts, i =>
			{
				for (int j = jstart; j < jend; j++)
					arr[i, j] = data[i - xshift, j - yshift];
			});

			return arr;
		}

		[MethodImpl(256)]
		public static double InterpolateBiLinear(double[,] data, int width, int height, double x, double y)
		{
			int xoldfloor = (int)(x);
			int yoldfloor = (int)(y);
			if (xoldfloor < 0 || yoldfloor < 0)
				return 0;

			int xoldciel = (int)(x + 1);
			int yoldciel = (int)(y + 1);
			if (xoldciel >= width || yoldciel >= height)
				return 0;

			double deltaX = x - (double)xoldfloor;
			double onemdeltaX = 1 - deltaX;
			double deltaY = y - (double)yoldfloor;

			//linearly interpolate horizontally between top neighbours
			double pixTop = onemdeltaX * data[xoldfloor, yoldfloor] + deltaX * data[xoldciel, yoldfloor];

			// linearly interpolate horizontally between bottom neighbours
			double pixBottom = onemdeltaX * data[xoldfloor, yoldciel] + deltaX * data[xoldciel, yoldciel];

			// linearly interpolate vertically between top and bottom interpolated results
			return (1 - deltaY) * pixTop + deltaY * pixBottom;
		}

		[MethodImpl(256)]
		public static double InterpolateLanczos(double[,] data, int width, int height, double x, double y, int n)
		{
			double xoldfloor = Math.Floor(x);
			if (xoldfloor <= n || xoldfloor >= width - n)
				return 0;
			double xoldfloorminisx = xoldfloor - x;

			double yoldfloor = Math.Floor(y);
			if (yoldfloor <= n || yoldfloor >= height - n)
				return 0;
			double yoldfloorminisy = yoldfloor - y;

			double w = 0, val = 0, Lx = 0, Ly = 0;
			for (int i = -n + 1; i <= n; i++)
			{
				Lx = Lanczos((double)(i) + xoldfloorminisx, n);
				for (int j = -n + 1; j <= n; j++)
				{
					Ly = Lx * Lanczos((double)(j) + yoldfloorminisy, n);
					w += Ly;
					val += data[(int)xoldfloor + i, (int)yoldfloor + j] * Ly;
				}
			}
			return val / w;
		}

		[MethodImpl(256)]
		public static double Lanczos(double x, int n)
		{
			if (Math.Abs(x) > n)
				return 0;
			else
				return sinc_n(x) * sinc_n(x / (double)(n));
		}

		//normalized sinc function
		[MethodImpl(256)]
		public static double sinc_n(double x)
		{
			if (x == 0)
				return 1;
			else
				return Math.Sin(Math.PI * x) / Math.PI / x;
		}

		//un-normalized sinc function
		[MethodImpl(256)]
		public static double sinc_un(double x)
		{
			if (x == 0)
				return 1;
			else
				return Math.Sin(x) / x;
		}

		/// <summary>Rotates an array about its center.</summary>
		/// <param name="data">The array to rotate.</param>
		/// <param name="radians">The angle to rotate the array, positive counter-clockwise.</param>
		/// <param name="x_center">The zero-based rotation center on the x-axis to rotate the array about. Pass Double.MaxValue for array center.</param>
		/// <param name="y_center">The zero-based rotation center on the y-axis to rotate the array about. Pass Double.MaxValue for array center.</param>
		/// <param name="interp_style">&quot;nearest&quot; nearest neighbor pixel<br />&quot;bilinear&quot; 2x2 bilinear interpolation<br />&quot;lanc_n&quot; Lanczos interpolation of order n = 3, 4, 5</param>
		/// <param name="xshift">The amount to shift the image on the x-axis.</param>
		/// <param name="yshift">The amount to shift the image on the y-axis.</param>
		/// <param name="do_parallel">Perform operation with parallelism.</param>
		public static double[,] RotateShiftArray(double[,] data, double radians, double x_center, double y_center, string interp_style, double xshift, double yshift, bool do_parallel = false)
		{
			int width = data.GetLength(0);
			int height = data.GetLength(1);
			double xmid = (double)(width / 2 - 1);//zero based
			double ymid = (double)(height / 2 - 1);//zero based
			if (x_center != Double.MaxValue)
				xmid = x_center;
			if (y_center != Double.MaxValue)
				ymid = y_center;
			double[,] arr = new double[data.GetLength(0), data.GetLength(1)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

            double cosrad = Math.Cos(radians), sinrad = Math.Sin(radians);
			double MyshiftMymid = -yshift - ymid;
			double MxshiftMxmid = -xshift - xmid;

            if (interp_style.ToLower() == "nearest")
			{
				Parallel.For(0, width, opts, x =>
				{
					int xold, yold;
					double xMxmidPxshift = (double)x + MxshiftMxmid;
					double yPyshiftMymid;

                    for (int y = 0; y < height; y++)
					{
						yPyshiftMymid = (double)y + MyshiftMymid;
                        xold = (int)Math.Round(xMxmidPxshift * cosrad - yPyshiftMymid * sinrad + xmid);
						yold = (int)Math.Round(xMxmidPxshift * sinrad + yPyshiftMymid * cosrad + ymid);
						if (xold >= 0 && xold < width && yold >= 0 && yold < height)
							arr[x, y] = data[xold, yold];
					}
				});
			}

			if (interp_style.ToLower() == "bilinear")
			{
				Parallel.For(0, width, opts, x =>
				{
					double xold, yold;
                    double xMxmidPxshift = (double)x + MxshiftMxmid;
                    double yPyshiftMymid;

                    for (int y = 0; y < height; y++)
					{
                        yPyshiftMymid = (double)y + MyshiftMymid;
                        xold = xMxmidPxshift * cosrad - yPyshiftMymid * sinrad + xmid;
						yold = xMxmidPxshift * sinrad + yPyshiftMymid * cosrad + ymid;
						arr[x, y] = InterpolateBiLinear(data, width, height, xold, yold);
					}
				});
			}

			if (interp_style.Contains("lanc"))
			{
				string sn = interp_style.Substring(interp_style.Length - 1);
				if (!JPMath.IsNumeric(sn))
					throw new Exception("Lanczos order " + sn + " indeterminable. ");
				int n = Convert.ToInt32(sn);
				if (n < 3 || n > 5)
					throw new Exception("Lanczos order " + n + " not computable. Allowed n = 3, 4, 5. ");

				Parallel.For(0, width, opts, x =>
				{
					double xold, yold;
                    double xMxmidPxshift = (double)x + MxshiftMxmid;
                    double yPyshiftMymid;

                    for (int y = n; y < height - n; y++)
					{
                        yPyshiftMymid = (double)y + MyshiftMymid;
                        xold = xMxmidPxshift * cosrad - yPyshiftMymid * sinrad + xmid;
						yold = xMxmidPxshift * sinrad + yPyshiftMymid * cosrad + ymid;
						arr[x, y] = InterpolateLanczos(data, width, height, xold, yold, n);
					}
				});
			}

			return arr;
		}

		/// <summary>Returns the dot-product of two equal-length vectors.</summary>
		public static double VectorDotProdVector(double[] vector1, double[] vector2, bool do_parallel = false)
		{
			double[] result = new double[vector1.Length];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, vector1.Length, opts, i =>
			{
				result[i] = vector1[i] * vector2[i];
			});

			if (do_parallel)
				return result.AsParallel().Sum();
			else
				return result.Sum();
		}

		/// <summary>Returns the cross correlation of two equal-length vectors and its lags.</summary>
		/// <param name="reference">The reference data array against which to create the cross correlation.</param>
		/// <param name="relative">The comparison data array with which to create the cross correlation.</param>
		/// <param name="lags">An array passed (arrays pass by reference) to populate the cross correlation lags.</param>
		public static double[] XCorr(double[] reference, double[] relative, out double[] lags, bool do_parallel = false)
		{
			if (reference.Length != relative.Length)
				throw new Exception("Error in JPMath.XCorr, \"reference\" and \"relative\" must be equal length.");

			int L = reference.Length;
			int xcl = 2 * L - 1;
			double[] result = new double[2 * L - 1];
			double[] loclags = new double[2 * L - 1];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, xcl, opts, i =>
			{
				if (i < L)
					for (int j = 0; j < i + 1; j++)
						result[i] += reference[L - 1 - i + j] * relative[j];
				else
					for (int j = 0; j < 2 * L - i - 1; j++)
						result[i] += reference[j] * relative[i - L + j + 1];

				loclags[i] = -L + 1 + i;
			});

			lags = new double[2 * L - 1];
			loclags.CopyTo(lags, 0);
			return result;
		}

		/// <summary>Returns the cross correlation of two equal-length vectors and its lags over a range of lags centered on zero lag. Only the lag range is returned.</summary>
		/// <param name="reference">The reference data array against which to create the cross correlation.</param>
		/// <param name="relative">The comparison data array with which to create the cross correlation. Must be equal in length to the reference array.</param>
		/// <param name="lags">An array passed (arrays pass by reference) to populate the cross correlation lags. Only the +- lang range is returned.</param>
		/// <param name="maxlag">The +- maximum lag displacement between the vectors. Only the +- lag range is returned. 0 returns the dot product of the vectors. Must be positive or zero and smaller than the length of the input vectors.</param>
		public static double[] XCorr(double[] reference, double[] relative, out double[] lags, int maxlag)
		{
			if (maxlag < 0)
				throw new Exception("Error in JPMath.XCorr, \"maxlag\" cannot be less than zero.");
			if (maxlag >= reference.Length)
				throw new Exception("Error in JPMath.XCorr, \"maxlag\" cannot be greater than or equal to the length of the input vectors.");
			if (reference.Length != relative.Length)
				throw new Exception("Error in JPMath.XCorr, \"reference\" and \"relative\" must be equal length.");

			int L = reference.Length;
			int xcm = L - 1;
			double[] result = new double[2 * maxlag + 1];
			lags = new double[2 * maxlag + 1];

			for (int i = xcm - maxlag; i <= xcm + maxlag; i++)
			{
				if (i < L)
					for (int j = 0; j < i + 1; j++)
						result[i - xcm + maxlag] += reference[L - 1 - i + j] * relative[j];
				else
					for (int j = 0; j < 2 * L - i - 1; j++)
						result[i - xcm + maxlag] += reference[j] * relative[i - L + j + 1];

				lags[i - xcm + maxlag] = -L + 1 + i;
			}

			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		/// <param name="Nx"></param>
		/// <returns></returns>
		public static double[] Bin(double[] data, int Nx)
		{
			if (Nx > data.GetLength(0))
				Nx = data.GetLength(0);
			int Lx = (int)System.Math.Floor((double)data.GetLength(0) / (double)Nx);

			double[] result = new double[Lx];

			for (int i = 0; i < Lx; i++)
			{
				double s = 0;
				for (int k = i * Nx; k < i * Nx + Nx; k++)
					s = s + data[k];

				result[i] = s;
			}

			return result;
		}

		public static double[,] Bin(double[,] data, int Nx, int Ny, bool do_parallel = false)//remainders are dropped
		{
			if (Nx > data.GetLength(0))
				Nx = data.GetLength(0);
			if (Ny > data.GetLength(1))
				Ny = data.GetLength(1);

			int Lx = (int)System.Math.Floor((double)data.GetLength(0) / (double)Nx);
			int Ly = (int)System.Math.Floor((double)data.GetLength(1) / (double)Ny);

			double[,] result = new double[Lx, Ly];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, Lx, opts, i =>
			{
				for (int j = 0; j < Ly; j++)
				{
					double s = 0;
					for (int k = i * Nx; k < i * Nx + Nx; k++)
						for (int l = j * Ny; l < j * Ny + Ny; l++)
							s = s + data[k, l];

					result[i, j] = s;
				}
			});

			return result;
		}

		public static uint[,] Bin(uint[,] data, int Nx, int Ny, bool do_parallel = false)//remainders are dropped
		{
			if (Nx > data.GetLength(0))
				Nx = data.GetLength(0);
			if (Ny > data.GetLength(1))
				Ny = data.GetLength(1);

			int Lx = (int)System.Math.Floor((double)data.GetLength(0) / (double)Nx);
			int Ly = (int)System.Math.Floor((double)data.GetLength(1) / (double)Ny);
			uint[,] result = new uint[Lx, Ly];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, Lx, opts, i =>
			{
				for (int j = 0; j < Ly; j++)
				{
					uint s = 0;
					for (int k = i * Nx; k < i * Nx + Nx; k++)
						for (int l = j * Ny; l < j * Ny + Ny; l++)
							s = s + data[k, l];

					result[i, j] = s;
				}
			});

			return result;
		}

		public static double[,] Pad(double[,] data, int[] padding, bool do_parallel = false)
		{
			int width = data.GetLength(0), height = data.GetLength(1);
			double[,] result = new double[width + padding[0] + padding[1], height + padding[2] + padding[3]];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, width, opts, x =>
			{
				for (int y = 0; y < height; y++)
					result[x + padding[0], y + padding[2]] = data[x, y];
			});

			return result;
		}

		public static double[,] Crop(double[,] data, int[] cropping, bool do_parallel = false)
		{
			double[,] result = new double[cropping[1] - cropping[0] + 1, cropping[3] - cropping[2] + 1];
			int width = result.GetLength(0), height = result.GetLength(1);

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, width, opts, x =>
			{
				for (int y = 0; y < height; y++)
					result[x, y] = data[x + cropping[0], y + cropping[2]];
			});

			return result;
		}

		/// <summary>
		/// Excise a vertical or horizontal strip from an array.
		/// </summary>
		/// <param name="data">The array.</param>
		/// <param name="column">True if a column is to be excised, false if a row.</param>
		/// <param name="X0">The coordinate of the center of the strip to be excised.</param>
		/// <param name="halfWidth">A strip +- the halfWidth will be excised.</param>
		/// <param name="do_parallel">Perform operation with parallelism over the array operations.</param>
		public static double[,] Excise(double[,] data, bool column, int X0, int halfWidth, bool do_parallel = false)
		{
			double[,] result;
			int[] xrange;
			int[] yrange;
			int hwt2p1 = halfWidth * 2 + 1;

			if (column)
			{
				result = new double[data.GetLength(0) - (halfWidth * 2 + 1), data.GetLength(1)];
				yrange = new int[result.GetLength(1)];
				for (int i = 0; i < yrange.Length; i++)
					yrange[i] = i;

				xrange = new int[result.GetLength(0)];
				for (int i = 0; i < data.GetLength(0); i++)
					if (i < X0 - halfWidth)
						xrange[i] = i;
					else if (i > X0 + halfWidth)
						xrange[i - hwt2p1] = i;
			}
			else
			{
				result = new double[data.GetLength(0), data.GetLength(1) - hwt2p1];
				xrange = new int[result.GetLength(0)];
				for (int i = 0; i < xrange.Length; i++)
					xrange[i] = i;

				yrange = new int[result.GetLength(1)];
				for (int i = 0; i < data.GetLength(1); i++)
					if (i < X0 - halfWidth)
						yrange[i] = i;
					else if (i > X0 + halfWidth)
						yrange[i - hwt2p1] = i;
			}

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, result.GetLength(0), opts, x =>
			{
				for (int y = 0; y < result.GetLength(1); y++)
					result[x, y] = data[xrange[x], yrange[y]];
			});

			return result;
		}

		/// <summary>Convolves a kernel array into a primary array.  The kernel must have a an odd-numbered with and height.</summary>
		/// <param name="primary">The primary array into which the kernel is convolved.</param>
		/// <param name="kernel">The kernel with which the primary array is convolved.</param>
		public static double[,] MatrixConvolveMatrix(double[,] primary, double[,] kernel, bool do_parallel = false)
		{
			int KFWX = kernel.GetLength(0);
			int KFWY = kernel.GetLength(1);
			int KHWX = (KFWX - 1) / 2;
			int KHWY = (KFWY - 1) / 2;
			int PFWX = primary.GetLength(0);
			int PFWY = primary.GetLength(1);
			int PMX = PFWX - KHWX, PMY = PFWY - KHWY;

			double[,] result = new double[PFWX, PFWY];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(KHWX, PMX, opts, x =>
			{
				for (int y = KHWY; y < PMY; y++)
				{
					int ky = y - KHWY;
					for (int xx = 0; xx < KFWX; xx++)
					{
						int kx = x - KHWX + xx;
						for (int yy = 0; yy < KFWY; yy++)
							result[x, y] += kernel[xx, yy] * primary[kx, ky + yy];
					}
				}
			});

			return result;
		}

		#endregion

		#region ARRAY STATISTICS

		/// <summary>
		/// Multiples together all elements of an array.
		/// </summary>
		/// <param name="data"></param>
		public static long Product(int[] data)
		{
			long product = 1;
			for (int i = 0; i < data.Length; i++)
				product *= data[i];
			
			return product;
		}

		/// <summary>Returns the sum over all elements in the data array.</summary>
		/// <param name="vectorOrArray">A vecor or 2-D array.</param>
		public static double Sum(Array vectorOrArray, bool do_parallel = false)
		{
			TypeCode type = Type.GetTypeCode((vectorOrArray.GetType()).GetElementType());
			int rank = vectorOrArray.Rank;
			double res = 0;
			int naxis0 = -1, naxis1 = -1;
			if (rank == 1)
				naxis0 = vectorOrArray.Length;
			else
			{
				naxis0 = vectorOrArray.GetLength(0);
				naxis1 = vectorOrArray.GetLength(1);
			}

			object locker = new object();
			var rangePartitioner = Partitioner.Create(0, naxis0);
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			switch (type)
			{
				case TypeCode.Double:
				{
					if (rank == 1)
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								sum += ((double[])vectorOrArray)[x];

							lock (locker)
							{
								res += sum;
							}
						});
					else //rank == 2
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								for (int y = 0; y < naxis1; y++)
									sum += ((double[,])vectorOrArray)[x, y];

							lock (locker)
							{
								res += sum;
							}
						});
					return res;
				}

				case TypeCode.Single:
				{
					if (rank == 1)
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								sum += ((float[])vectorOrArray)[x];

							lock (locker)
							{
								res += sum;
							}
						});
					else //rank == 2
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								for (int y = 0; y < naxis1; y++)
									sum += ((float[,])vectorOrArray)[x, y];

							lock (locker)
							{
								res += sum;
							}
						});
					return res;
				}

				case TypeCode.UInt64:
				{
					if (rank == 1)
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								sum += ((ulong[])vectorOrArray)[x];

							lock (locker)
							{
								res += sum;
							}
						});
					else //rank == 2
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								for (int y = 0; y < naxis1; y++)
									sum += ((ulong[,])vectorOrArray)[x, y];

							lock (locker)
							{
								res += sum;
							}
						});
					return res;
				}

				case TypeCode.Int64:
				{
					if (rank == 1)
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								sum += ((long[])vectorOrArray)[x];

							lock (locker)
							{
								res += sum;
							}
						});
					else //rank == 2
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								for (int y = 0; y < naxis1; y++)
									sum += ((long[,])vectorOrArray)[x, y];

							lock (locker)
							{
								res += sum;
							}
						});
					return res;
				}

				case TypeCode.UInt32:
				{
					if (rank == 1)
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								sum += ((uint[])vectorOrArray)[x];

							lock (locker)
							{
								res += sum;
							}
						});
					else //rank == 2
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								for (int y = 0; y < naxis1; y++)
									sum += ((uint[,])vectorOrArray)[x, y];

							lock (locker)
							{
								res += sum;
							}
						});
					return res;
				}

				case TypeCode.Int32:
				{
					if (rank == 1)
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								sum += ((int[])vectorOrArray)[x];

							lock (locker)
							{
								res += sum;
							}
						});
					else //rank == 2
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								for (int y = 0; y < naxis1; y++)
									sum += ((int[,])vectorOrArray)[x, y];

							lock (locker)
							{
								res += sum;
							}
						});
					return res;
				}

				case TypeCode.UInt16:
				{
					if (rank == 1)
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								sum += ((ushort[])vectorOrArray)[x];

							lock (locker)
							{
								res += sum;
							}
						});
					else //rank == 2
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								for (int y = 0; y < naxis1; y++)
									sum += ((ushort[,])vectorOrArray)[x, y];

							lock (locker)
							{
								res += sum;
							}
						});
					return res;
				}

				case TypeCode.Int16:
				{
					if (rank == 1)
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								sum += ((short[])vectorOrArray)[x];

							lock (locker)
							{
								res += sum;
							}
						});
					else //rank == 2
						Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
						{
							double sum = 0;
							for (int x = range.Item1; x < range.Item2; x++)
								for (int y = 0; y < naxis1; y++)
									sum += ((short[,])vectorOrArray)[x, y];

							lock (locker)
							{
								res += sum;
							}
						});
					return res;
				}

				default:
				{
					throw new Exception("Typecode '" + type.ToString() + "' not supported for Sum.");
				}
			}
		}

		/// <summary>Sum a 2-D array along one dimension, resulting in a 1-D vector array.</summary>
		/// <param name="data">A 2-D double array.</param>
		/// <param name="dim">The dimension along which to sum.  
		/// <br />0 (zero) sums along the horizontal axis, resulting in a vertical vector.
		/// <br />1 (one) sums along the vertical axis, resulting in a horizontal vector.</param>
		public static double[] Sum(double[,] data, int dim, bool do_parallel = false)
		{
			int d = 1;
			if (dim == 1)
				d = 0;

			double[] result = new double[data.GetLength(d)];

			ParallelOptions parallel_options = new ParallelOptions();
			if (do_parallel)
				parallel_options.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				parallel_options.MaxDegreeOfParallelism = 1;

			if (dim == 0)//collapses array horizontally, i.e., makes a 'vertical' vector
			{
				Parallel.For(0, data.GetLength(1), parallel_options, j =>
				{
                    double S = 0;
					for (int i = 0; i < data.GetLength(0); i++)
						S += data[i, j];

					result[j] = S;
				});
			}

			if (dim == 1)//collpases array vertically...i.e., makes a 'horizontal' vector
			{
				Parallel.For(0, data.GetLength(1), parallel_options, i =>
				{
                    double S = 0;
					for (int j = 0; j < data.GetLength(1); j++)
						S += data[i, j];

					result[i] = S;
				});
			}

			return result;
		}

		/// <summary>Sum a 2-D array along one dimension, resulting in a 1-D vector array.</summary>
		/// <param name="data">A 2-D int array.</param>
		/// <param name="dim">The dimension along which to sum.  
		/// <br />0 (zero) sums along the horizontal axis, resulting in a vertical vector.
		/// <br />1 (one) sums along the vertical axis, resulting in a horizontal vector.</param>
		public static int[] Sum(int[,] data, int dim, bool do_parallel = false)
		{
			int d = 1;
			if (dim == 1)
				d = 0;

			int[] result = new int[data.GetLength(d)];

			ParallelOptions parallel_options = new ParallelOptions();
			if (do_parallel)
				parallel_options.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				parallel_options.MaxDegreeOfParallelism = 1;

			ArrayList list = new ArrayList();

			if (dim == 0)//collapses array horizontally, i.e., makes a 'vertical' vector
			{
				Parallel.For(0, data.GetLength(1), parallel_options, j =>
				{
                    int S = 0;
					for (int i = 0; i < data.GetLength(0); i++)
						S += data[i, j];

					result[j] = S;
				});
			}

			if (dim == 1)//collpases array vertically...i.e., makes a 'horizontal' vector
			{
				Parallel.For(0, data.GetLength(1), parallel_options, i =>
				{
                    int S = 0;
					for (int j = 0; j < data.GetLength(1); j++)
						S += data[i, j];

					result[i] = S;
				});
			}

			return result;
		}

		/// <summary>Average a 2-D array along one dimension, resulting in a 1-D vector array.</summary>
		/// <param name="data">A 2-D double array.</param>
		/// <param name="dim">The dimension along which to average.  
		/// <br />0 (zero) averages along the horizontal axis, resulting in a vertical vector.
		/// <br />1 (one) averages along the vertical axis, resulting in a horizontal vector.</param>
		public static double[] Mean(double[,] data, int dim, bool do_parallel = false)
		{
			int d = 1;
			if (dim == 1)
				d = 0;

			double dimOppL = (double)data.GetLength(dim);
			double[] result = new double[data.GetLength(d)];

			ParallelOptions parallel_options = new ParallelOptions();
			if (do_parallel)
				parallel_options.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				parallel_options.MaxDegreeOfParallelism = 1;

			ArrayList list = new ArrayList();

			if (dim == 0)//collapses array horizontally, i.e., makes a 'vertical' vector
			{
				Parallel.For(0, data.GetLength(1), parallel_options, j =>
				{
                    double S = 0;
					for (int i = 0; i < data.GetLength(0); i++)
						S += data[i, j];

					result[j] = S / dimOppL;
				});
			}

			if (dim == 1)//collpases array vertically...i.e., makes a 'horizontal' vector
			{
				Parallel.For(0, data.GetLength(1), parallel_options, i =>
				{
                    double S = 0;
					for (int j = 0; j < data.GetLength(1); j++)
						S += data[i, j];

					result[i] = S / dimOppL;
				});
			}

			return result;
		}

		/// <summary>Returns the mean over all elements in the data array.</summary>
		/// <param name="vectorOrArray">A vecor or 2-D array.</param>
		public static double Mean(Array vectorOrArray, bool do_parallel = false)
		{
			return Sum(vectorOrArray, do_parallel) / (double)((vectorOrArray).Length);
		}

		/// <summary>Average a 2D array along one dimension, resulting in a 1D vector array.</summary>
		/// <param name="data">A 2D double array.</param>
		/// <param name="dim">The dimension along which to average.  
		/// <br />0 (zero) averages along the horizontal axis, resulting in a vertical vector.
		/// <br />1 (one) averages along the vertical axis, resulting in a horizontal vector.</param>
		public static double[] Stdv(double[,] data, int dim, bool do_parallel = false)
		{
			int d = 1;
			if (dim == 1)
				d = 0;

			ParallelOptions parallel_options = new ParallelOptions();
			if (do_parallel)
				parallel_options.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				parallel_options.MaxDegreeOfParallelism = 1;

			double[] mean = JPMath.Mean(data, dim, true);
			double diml = data.GetLength(dim) - 1.0;
			double[] result = new double[data.GetLength(d)];

			if (dim == 0)//collapses array horizontally, i.e., makes a 'vertical' vector
			{

				Parallel.For(0, data.GetLength(1), parallel_options, j =>
				{
                    double std = 0;
					for (int i = 0; i < data.GetLength(0); i++)
						std += (data[i, j] - mean[j]) * (data[i, j] - mean[j]);

					result[j] = Math.Sqrt(std / diml);
				});
			}

			if (dim == 1)//collpases array vertically, i.e., makes a 'horizontal' vector
			{
				Parallel.For(0, data.GetLength(1), parallel_options, i =>
				{
                    double std = 0;
					for (int j = 0; j < data.GetLength(1); j++)
						std += (data[i, j] - mean[i]) * (data[i, j] - mean[i]);

					result[i] = Math.Sqrt(std / diml);
				});
			}

			return result;
		}

		/// <summary>Returns the minima and their indices along a given dimension of the data array.
		/// <br /> If dim = 0, the minima are row-wise.
		/// <br /> If dim = 1, the minima are column-wise.</summary>
		/// <param name="data">A 2-D double array.</param>
		/// <param name="dim">The dimension along which to reduce to minimums:  0 is x (rows), 1 is y (columns).</param>
		/// <param name="indices">An array passed to populate the indices at which the minima appear along the dimension.</param>
		public static double[] Min(double[,] data, int dim, out int[] indices, bool do_parallel = false)
		{
			int d = 1;
			if (dim == 1)
				d = 0;

			double[] result = new double[data.GetLength(d)];
			int[] locinds = new int[data.GetLength(d)];

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			if (dim == 0)//collapses array horizontally, i.e., makes a 'vertical' vector
			{
				var rangePartitioner = Partitioner.Create(0, data.GetLength(1));

				Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
				{
					for (int j = range.Item1; j < range.Item2; j++)
					{
                        double min = System.Double.MaxValue;
						for (int i = 0; i < data.GetLength(0); i++)
							if (data[i, j] < min)
							{
								min = data[i, j];
								result[j] = min;
								locinds[j] = i;
							}
					}
				});
			}

			if (dim == 1)//collpases array vertically...i.e., makes a 'horizontal' vector
			{
				var rangePartitioner = Partitioner.Create(0, data.GetLength(0));

				Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
				{
					for (int i = range.Item1; i < range.Item2; i++)
					{
                        double min = System.Double.MaxValue;
						for (int j = 0; j < data.GetLength(1); j++)
							if (data[i, j] < min)
							{
								min = data[i, j];
								result[i] = min;
								locinds[i] = j;
							}
					}
				});
			}

			indices = locinds;
			return result;
		}

		/// <summary>Returns the global minimum and its [x, y] index in the 2-D array data.</summary>
		public static double Min(double[,] data, out int x, out int y, bool do_parallel = false)
		{
			double min = System.Double.MaxValue;
			int locx = -1, locy = -1;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
					if (data[i, j] < min)
					{
						lock (locker)
						{
							if (data[i, j] < min)
							{
								min = data[i, j];
								locx = i;
								locy = j;
							}
						}
					}
			});

			x = locx;
			y = locy;
			return min;
		}

		public static double Min(double[,] data, bool do_parallel = false)
		{
			double min = System.Double.MaxValue;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
					if (data[i, j] < min)
					{
						lock (locker)
						{
							if (data[i, j] < min)
								min = data[i, j];
						}
					}
			});
			return min;
		}

		/// <summary>Returns the global minimum and its index in the 1-D array data.</summary>
		public static double Min(double[] data, out int index, bool do_parallel = false)
		{
			double min = System.Double.MaxValue;
			int locx = -1;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.Length, opts, i =>
			{
				if (data[i] < min)
				{
					lock (locker)
					{
						if (data[i] < min)
						{
							min = data[i];
							locx = i;
						}
					}
				}
			});

			index = locx;
			return min;
		}

		public static double Min(double[] data, bool do_parallel = false)
		{
			double min = System.Double.MaxValue;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.Length, opts, i =>
			{
				if (data[i] < min)
				{
					lock (locker)
					{
						if (data[i] < min)
							min = data[i];
					}
				}
			});

			return min;
		}

		/// <summary>Returns the global minimum and maximum of the 2-D array data.</summary>
		public static void MinMax(double[,] data, out double min, out double max, bool do_parallel = false)
		{
			double locmin = System.Double.MaxValue;
			double locmax = System.Double.MinValue;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
				{
					if (data[i, j] < locmin)
					{
						lock (locker)
						{
							if (data[i, j] < locmin)
								locmin = data[i, j];
						}
					}
					if (data[i, j] > locmax)
					{
						lock (locker)
						{
							if (data[i, j] > locmax)
								locmax = data[i, j];
						}
					}
				}
			});

			min = locmin;
			max = locmax;
		}

		/// <summary>Returns the global minimum and maximum of the 1-D array data.</summary>
		public static void MinMax(double[] data, out double min, out double max, bool do_parallel = false)
		{
			double locmin = System.Double.MaxValue;
			double locmax = System.Double.MinValue;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				if (data[i] < locmin)
				{
					lock (locker)
					{
						if (data[i] < locmin)
							locmin = data[i];
					}
				}
				if (data[i] > locmax)
				{
					lock (locker)
					{
						if (data[i] > locmax)
							locmax = data[i];
					}
				}
			});

			min = locmin;
			max = locmax;
		}

		/// <summary>Returns the maxima and their indices along a given dimension of the data array.
		/// <br /> If dim = 0, the maxima are row-wise.
		/// <br /> If dim = 1, the maxima are column-wise.</summary>
		/// <param name="data">A 2-D double array.</param>
		/// <param name="dim">The dimension along which to reduce to maximums:  0 is x (rows), 1 is y (columns).</param>
		/// <param name="indices">An array passed to populate the indices at which the maxima appear along the dimension.</param>
		public static double[] Max(double[,] data, int dim, out int[] indices, bool do_parallel = false)
		{
			int d = 1;
			if (dim == 1)
				d = 0;

			double[] result = new double[data.GetLength(d)];
			int[] locinds = new int[data.GetLength(d)];
			double max = System.Double.MinValue;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			if (dim == 0)//collapses array horizontally, i.e., makes a 'vertical' vector
			{
				var rangePartitioner = Partitioner.Create(0, data.GetLength(1));

				Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
				{
					for (int j = range.Item1; j < range.Item2; j++)
					{
						max = System.Double.MinValue;
						for (int i = 0; i < data.GetLength(0); i++)
							if (data[i, j] > max)
							{
								max = data[i, j];
								result[j] = max;
								locinds[j] = i;
							}
					}
				});
			}

			if (dim == 1)//collpases array vertically...i.e., makes a 'horizontal' vector
			{
				var rangePartitioner = Partitioner.Create(0, data.GetLength(0));

				Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
				{
					for (int i = range.Item1; i < range.Item2; i++)
					{
						max = System.Double.MinValue;
						for (int j = 0; j < data.GetLength(1); j++)
							if (data[i, j] > max)
							{
								max = data[i, j];
								result[i] = max;
								locinds[i] = j;
							}
					}
				});
			}

			indices = locinds;
			return result;
		}

		/// <summary>Returns the global maximum and determines its [x, y] index in the 2-D array data.</summary>
		public static double Max(double[,] data, out int x, out int y, bool do_parallel = false)
		{
			double max = System.Double.MinValue;
			int locx = -1, locy = -1;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
					if (data[i, j] > max)
					{
						lock (locker)
						{
							if (data[i, j] > max)
							{
								max = data[i, j];
								locx = i;
								locy = j;
							}
						}
					}
			});

			x = locx;
			y = locy;
			return max;
		}

		public static double Max(double[] data, bool do_parallel = false)
		{
			double max = System.Double.MinValue;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.Length, opts, i =>
			{
				if (data[i] > max)
				{
					lock (locker)
					{
						if (data[i] > max)
							max = data[i];
					}
				}
			});

			return max;
		}

		public static double Max(double[,] data, bool do_parallel = false)
		{
			double max = System.Double.MinValue;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
					if (data[i, j] > max)
					{
						lock (locker)
						{
							if (data[i, j] > max)
								max = data[i, j];
						}
					}
			});

			return max;
		}

		/// <summary>Returns the global maximum and its index in the 1-D array data.</summary>
		public static double Max(double[] data, out int index, bool do_parallel = false)
		{
			double max = System.Double.MinValue;
			int locx = -1;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.Length, opts, i =>
			{
				if (data[i] > max)
				{
					lock (locker)
					{
						if (data[i] > max)
						{
							max = data[i];
							locx = i;
						}
					}
				}
			});

			index = locx;
			return max;
		}

		/// <summary>Returns the global maximum and its index within a subsection of the 1-D array data.</summary>
		/// <param name="data">A 1-D double array.</param>
		/// <param name="startIndex">The start index at which to begin checking for a maximum value.</param>
		/// <param name="endIndex">The end index within which to check for a maximum value.</param>
		/// <param name="maxIndex">The index in the array at which the maximum value occurs.</param>
		public static double Max(double[] data, int startIndex, int endIndex, out int maxIndex, bool do_parallel = false)
		{
			if (startIndex < 0)
				startIndex = 0;
			if (endIndex > data.Length)
				endIndex = data.Length - 1;

			double max = System.Double.MinValue;
			int locx = -1;
			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(startIndex, endIndex + 1, opts, i =>
			{
				if (data[i] > max)
				{
					lock (locker)
					{
						if (data[i] > max)
						{
							max = data[i];
							locx = i;
						}
					}
				}
			});

			maxIndex = locx;
			return max;
		}

		/// <summary>Returns the global maximum of the data array and its [x, y] indices.</summary>
		public static uint Max(uint[,] data, out int x, out int y, bool do_parallel = false)
		{
			uint max = System.UInt32.MinValue;
			int locx = -1, locy = -1;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
					if (data[i, j] > max)
					{
						lock (locker)
						{
							if (data[i, j] > max)
							{
								max = data[i, j];
								locx = i;
								locy = j;
							}
						}
					}
			});

			x = locx;
			y = locy;
			return max;
		}

		/// <summary>Returns the global minimum and its indeces within a subsection of the 1-D array data.</summary>
		/// <param name="data">A 1-D double array.</param>
		/// <param name="startIndex">The start index at which to begin checking for a minimum value.</param>
		/// <param name="endIndex">The end index within which to check for a minimum value.</param>
		/// <param name="minIndex">The index in the array at which the minimum value occurs.</param>
		public static double Min(double[] data, int startIndex, int endIndex, out int minIndex, bool do_parallel = false)
		{
			if (startIndex < 0)
				startIndex = 0;
			if (endIndex > data.Length)
				endIndex = data.Length - 1;

			double min = System.Double.MaxValue;
			int locx = -1;
			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(startIndex, endIndex + 1, opts, i =>
			{
				if (data[i] < min)
				{
					lock (locker)
					{
						if (data[i] < min)
						{
							min = data[i];
							locx = i;
						}
					}
				}
			});

			minIndex = locx;
			return min;
		}

		/// <summary>Returns the global maximum of the data array.</summary>
		/// <param name="data">A 1-D int array.</param>
		/// <param name="index">The index at which the maximum occurs in the array.</param>
		public static int Max(int[] data, out int index, bool do_parallel = false)
		{
			int max = System.Int32.MinValue;
			int locx = -1;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.Length, opts, i =>
			{
				if (data[i] > max)
				{
					lock (locker)
					{
						if (data[i] > max)
						{
							max = data[i];
							locx = i;
						}
					}
				}
			});

			index = locx;
			return max;
		}

		public static int Max(int[] data, bool do_parallel = false)
		{
			int max = System.Int32.MinValue;

			object locker = new object();
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.Length, opts, i =>
			{
				if (data[i] > max)
				{
					lock (locker)
					{
						if (data[i] > max)
						{
							max = data[i];
						}
					}
				}
			});

			return max;
		}

		/// <summary>Returns the standard deviation of all elements in the data array.</summary>
		public static double Stdv(double[,] data, bool do_parallel = false)
		{
			double SUM = 0, STD = 0;
			object locker = new object();
			var rangePartitioner = Partitioner.Create(0, data.GetLength(0));
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
			{
				double sum = 0;

				for (int i = range.Item1; i < range.Item2; i++)
					for (int j = 0; j < data.GetLength(1); j++)
					{
						sum += data[i, j];
					}

				lock (locker)
				{
					SUM += sum;
				}
			});

			double MEAN = SUM / (double)data.Length;

			Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
			{
				double std = 0, delta;
				for (int i = range.Item1; i < range.Item2; i++)
					for (int j = 0; j < data.GetLength(1); j++)
					{
						delta = (data[i, j] - MEAN);
						std += delta * delta; ;
					}

				lock (locker)
				{
					STD += std;
				}
			});
			STD = Math.Sqrt(STD / ((double)data.Length - 1.0));

			return STD;
		}

		/// <summary>Returns the standard deviation of all elements in the data array.</summary>
		/// <param name="data">A 2-D double array.</param>
		/// <param name="known_mean">If the mean of the data is already known, then save compute time by not having to calculate it first before the stdv is calculated.</param>
		public static double Stdv(double[,] data, double known_mean, bool do_parallel = false)
		{
			double STD = 0;
			object locker = new object();
			var rangePartitioner = Partitioner.Create(0, data.GetLength(0));
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
			{
				double std = 0, delta;
				for (int i = range.Item1; i < range.Item2; i++)
					for (int j = 0; j < data.GetLength(1); j++)
					{
						delta = (data[i, j] - known_mean);
						std += delta * delta;
					}

				lock (locker)
				{
					STD += std;
				}
			});
			STD = Math.Sqrt(STD / ((double)data.Length - 1.0));

			return STD;
		}

		/// <summary>Returns the standard deviation of all elements in the data array.</summary>
		public static double Stdv(double[] data, bool do_parallel = false)
		{
			double SUM = 0, STD = 0;
			object locker = new object();
			var rangePartitioner = Partitioner.Create(0, data.Length);
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
			{
				double sum = 0;

				for (int i = range.Item1; i < range.Item2; i++)
					sum += data[i];

				lock (locker)
				{
					SUM += sum;
				}
			});

			double MEAN = SUM / (double)data.Length;

			Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
			{
				double std = 0, delta;
				for (int i = range.Item1; i < range.Item2; i++)
				{
					delta = (data[i] - MEAN);
					std += delta * delta;
				}

				lock (locker)
				{
					STD += std;
				}
			});
			STD = Math.Sqrt(STD / ((double)data.Length - 1.0));

			return STD;
		}

		/// <summary>Returns the standard deviation of all elements in the data array.</summary>
		/// <param name="data">A 1-D double array.</param>
		/// <param name="known_mean">If the mean of the data is already known, then save compute time by not having to calculate it first before the stdv is calculated.</param>
		public static double Stdv(double[] data, double known_mean, bool do_parallel = false)
		{
			double STD = 0;
			object locker = new object();
			var rangePartitioner = Partitioner.Create(0, data.Length);
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.ForEach(rangePartitioner, opts, (range, loopState) =>
			{
				double std = 0, delta;
				for (int i = range.Item1; i < range.Item2; i++)
				{
					delta = (data[i] - known_mean);
					std += delta * delta;
				}

				lock (locker)
				{
					STD += std;
				}
			});
			STD = Math.Sqrt(STD / ((double)data.Length - 1.0));

			return STD;
		}

		public static double Mean_RobustClipped(double[] data, double sigma)
		{
			double[] clipper = data;
			double m = Mean(clipper, true);
			double s = Stdv(clipper, true);

			int[] pts = Find(Abs(VectorSubScalar(clipper, m, true), true), sigma * s, ">");

			while (pts.Length > 0)
			{
				clipper = Replace(clipper, pts, Median(clipper));
				m = Mean(clipper, true);
				s = Stdv(clipper, true);
				pts = Find(Abs(VectorSubScalar(clipper, m, true), true), sigma * s, ">");
			}
			return m;
		}

		public static double Mean_RobustClipped(double[,] data, double sigma)
		{
			double[,] clipper = data;
			double m = Mean(clipper, true);
			double s = Stdv(clipper, true);

			Find(Abs(MatrixSubScalar(clipper, m, true), true), sigma * s, ">", true, out int[] xinds, out int[] yinds);

			while (xinds.Length > 0)
			{
				clipper = Replace(clipper, xinds, yinds, Median(clipper), true);
				m = Mean(clipper, true);
				s = Stdv(clipper, true);
				Find(Abs(MatrixSubScalar(clipper, m, true), true), sigma * s, ">", true, out xinds, out yinds);
			}
			return m;
		}		

		public static double Median(Array vectorOrArray)
		{
			double[] arr = new double[vectorOrArray.Length];

			switch (Type.GetTypeCode((vectorOrArray.GetType()).GetElementType()))
			{
				case TypeCode.Double:
				{
					if (vectorOrArray.Rank == 2)
						for (int i = 0; i < vectorOrArray.GetLength(0); i++)
							for (int j = 0; j < vectorOrArray.GetLength(1); j++)
								arr[vectorOrArray.GetLength(1) * i + j] = ((double[,])vectorOrArray)[i, j];
					else
						for (int i = 0; i < vectorOrArray.Length; i++)
							arr[i] = ((double[])vectorOrArray)[i];

					break;
				}

				case TypeCode.Single:
				{
					if (vectorOrArray.Rank == 2)
						for (int i = 0; i < vectorOrArray.GetLength(0); i++)
							for (int j = 0; j < vectorOrArray.GetLength(1); j++)
								arr[vectorOrArray.GetLength(1) * i + j] = ((float[,])vectorOrArray)[i, j];
					else
						for (int i = 0; i < vectorOrArray.Length; i++)
							arr[i] = ((float[])vectorOrArray)[i];

					break;
				}

				case TypeCode.UInt64:
				{
					if (vectorOrArray.Rank == 2)
						for (int i = 0; i < vectorOrArray.GetLength(0); i++)
							for (int j = 0; j < vectorOrArray.GetLength(1); j++)
								arr[vectorOrArray.GetLength(1) * i + j] = ((ulong[,])vectorOrArray)[i, j];
					else
						for (int i = 0; i < vectorOrArray.Length; i++)
							arr[i] = ((ulong[])vectorOrArray)[i];

					break;
				}

				case TypeCode.Int64:
				{
					if (vectorOrArray.Rank == 2)
						for (int i = 0; i < vectorOrArray.GetLength(0); i++)
							for (int j = 0; j < vectorOrArray.GetLength(1); j++)
								arr[vectorOrArray.GetLength(1) * i + j] = ((long[,])vectorOrArray)[i, j];
					else
						for (int i = 0; i < vectorOrArray.Length; i++)
							arr[i] = ((long[])vectorOrArray)[i];

					break;
				}

				case TypeCode.UInt32:
				{
					if (vectorOrArray.Rank == 2)
						for (int i = 0; i < vectorOrArray.GetLength(0); i++)
							for (int j = 0; j < vectorOrArray.GetLength(1); j++)
								arr[vectorOrArray.GetLength(1) * i + j] = ((uint[,])vectorOrArray)[i, j];
					else
						for (int i = 0; i < vectorOrArray.Length; i++)
							arr[i] = ((uint[])vectorOrArray)[i];

					break;
				}

				case TypeCode.Int32:
				{
					if (vectorOrArray.Rank == 2)
						for (int i = 0; i < vectorOrArray.GetLength(0); i++)
							for (int j = 0; j < vectorOrArray.GetLength(1); j++)
								arr[vectorOrArray.GetLength(1) * i + j] = ((int[,])vectorOrArray)[i, j];
					else
						for (int i = 0; i < vectorOrArray.Length; i++)
							arr[i] = ((int[])vectorOrArray)[i];

					break;
				}

				case TypeCode.UInt16:
				{
					if (vectorOrArray.Rank == 2)
						for (int i = 0; i < vectorOrArray.GetLength(0); i++)
							for (int j = 0; j < vectorOrArray.GetLength(1); j++)
								arr[vectorOrArray.GetLength(1) * i + j] = ((ushort[,])vectorOrArray)[i, j];
					else
						for (int i = 0; i < vectorOrArray.Length; i++)
							arr[i] = ((ushort[])vectorOrArray)[i];

					break;
				}

				case TypeCode.Int16:
				{
					if (vectorOrArray.Rank == 2)
						for (int i = 0; i < vectorOrArray.GetLength(0); i++)
							for (int j = 0; j < vectorOrArray.GetLength(1); j++)
								arr[vectorOrArray.GetLength(1) * i + j] = ((short[,])vectorOrArray)[i, j];
					else
						for (int i = 0; i < vectorOrArray.Length; i++)
							arr[i] = ((short[])vectorOrArray)[i];

					break;
				}

				default:
					throw new Exception("Typecode '" + Type.GetTypeCode((vectorOrArray.GetType()).GetElementType()).ToString() + "' not supported for Median.");
			}

			if (JPMath.IsEven(arr.Length))
			{
				int kth1 = arr.Length / 2;
				int kth2 = (arr.Length - 1) / 2;

				return (arr[nth_element(arr, kth1)] + arr[nth_element(arr, kth2)]) / 2;
			}
			else
				return arr[nth_element(arr, arr.Length / 2)];
		}

		[MethodImpl(256)]/*256 = agressive inlining*/
		private static unsafe int nth_element(double[] data, int nth_element)
		{
			int middle, ll, hh;

			int low = 0;
			int high = data.Length - 1;

			fixed (double* arrptr = data)
			{
				for (; ; )
				{
					if (high <= low)
						return nth_element;

					if (high == low + 1)
					{
						if (data[low] > data[high])
							SwapElements(arrptr + low, arrptr + high);
						return nth_element;
					}

					middle = (low + high) / 2;
					if (data[middle] > data[high])
						SwapElements(arrptr + middle, arrptr + high);

					if (data[low] > data[high])
						SwapElements(arrptr + low, arrptr + high);

					if (data[middle] > data[low])
						SwapElements(arrptr + middle, arrptr + low);

					SwapElements(arrptr + middle, arrptr + low + 1);

					ll = low + 1;
					hh = high;
					for (; ; )
					{
						do ll++; while (data[low] > data[ll]);
						do hh--; while (data[hh] > data[low]);

						if (hh < ll)
							break;

						SwapElements(arrptr + ll, arrptr + hh);
					}

					SwapElements(arrptr + low, arrptr + hh);

					if (hh <= nth_element)
						low = ll;
					if (hh >= nth_element)
						high = hh - 1;
				}
			}
		}

		[MethodImpl(256)]/*256 = agressive inlining*/
		private static unsafe void SwapElements(double* p, double* q)
		{
			double temp = *p;
			*p = *q;
			*q = temp;
		}

		///// <summary>Returns the median of all elements in the data array.</summary>
		//[MethodImpl(256)]/*256 = agressive inlining*/
		//public static double Median(double[,] data)
		//{
		//	double[] arr = new double[data.Length];
		//	for (int i = 0; i < data.GetLength(0); i++)
		//		for (int j = 0; j < data.GetLength(1); j++)
		//			arr[data.GetLength(1) * i + j] = data[i, j];

		//	return Median(arr);
		//}


		//[MethodImpl(256)]/*256 = agressive inlining*/
		//public static unsafe double Median(double[] data)
		//{
		//	double[] arr = new double[data.Length];
		//	for (int i = 0; i < data.Length; i++)
		//		arr[i] = data[i];

		//	int middle, ll, hh;

		//	int low = 0;
		//	int high = arr.Length - 1;
		//	int median = (low + high) / 2;

		//	fixed (double* arrptr = arr)
		//	{
		//		for (; ; )
		//		{
		//			if (high <= low)
		//				return arr[median];

		//			if (high == low + 1)
		//			{
		//				if (arr[low] > arr[high])
		//					SwapElements(arrptr + low, arrptr + high);
		//				return arr[median];
		//			}

		//			middle = (low + high) / 2;
		//			if (arr[middle] > arr[high])
		//				SwapElements(arrptr + middle, arrptr + high);

		//			if (arr[low] > arr[high])
		//				SwapElements(arrptr + low, arrptr + high);

		//			if (arr[middle] > arr[low])
		//				SwapElements(arrptr + middle, arrptr + low);

		//			SwapElements(arrptr + middle, arrptr + low + 1);

		//			ll = low + 1;
		//			hh = high;
		//			for (; ; )
		//			{
		//				do ll++; while (arr[low] > arr[ll]);
		//				do hh--; while (arr[hh] > arr[low]);

		//				if (hh < ll)
		//					break;

		//				SwapElements(arrptr + ll, arrptr + hh);
		//			}

		//			SwapElements(arrptr + low, arrptr + hh);

		//			if (hh <= median)
		//				low = ll;
		//			if (hh >= median)
		//				high = hh - 1;
		//		}
		//	}
		//}


		public static double[] Histogram_IntegerStep(double[] values, double step, out double[] bincenters)
		{
			Array.Sort(values);
			int NDivs = (int)((values[values.Length - 1] - values[0]) / step) + 1;
			double[] posts = new double[NDivs + 1];
			for (int i = 0; i < posts.Length; i++)
				posts[i] = values[0] + step * (double)i;

			double[] histogram = new double[NDivs];
			bincenters = new double[NDivs];
			for (int j = 0; j < bincenters.Length; j++)
				bincenters[j] = (posts[j] + posts[j + 1]) / 2;

			int ind = 0;
			for (int j = 0; j < values.Length; j++)
				if (values[j] >= posts[ind] && values[j] < posts[ind + 1])
					histogram[ind]++;
				else
					histogram[++ind]++;

			return histogram;
		}

		public static double[] Histogram_IntegerDivisions(double[] values, int NDivs, out double[] bincenters)
		{
			Array.Sort(values);
			double[] posts = new double[NDivs + 1];//histogram subsection bounds
			double step = (values[values.Length - 1] - values[0]) / (double)NDivs;
			for (int i = 0; i <= posts.Length; i++)
				posts[i] = values[0] + step * (double)(i);

			double[] histogram = new double[NDivs];
			bincenters = new double[NDivs];
			for (int j = 0; j < bincenters.Length; j++)
				bincenters[j] = (posts[j] + posts[j + 1]) / 2;

			int ind = 0;
			for (int j = 0; j < values.Length; j++)
				if (values[j] >= posts[ind] && values[j] < posts[ind + 1])
					histogram[ind]++;
				else
					histogram[++ind]++;

			return histogram;
		}

		#endregion

		#region ARRAY ELEMENT OPS

		/// <summary>Returns the absolute values of all elements in the data array.</summary>
		public static double[] Abs(double[] data, bool do_parallel = false)
		{
			double[] result = new double[(data.Length)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.Length, opts, i =>
			{
				result[i] = System.Math.Abs(data[i]);
			});

			return result;
		}

		/// <summary>Returns the absolute values of all elements in the data array.</summary>
		public static double[,] Abs(double[,] data, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
					result[i, j] = System.Math.Abs(data[i, j]);
			});

			return result;
		}

		/// <summary>Returns the rounded values of all elements in the data array.</summary>
		/// <param name="data">A 2-D double array.</param>
		/// <param name="digits">The number of digits to which to round the data values.</param>
		public static double[,] Round(double[,] data, int digits, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
					result[i, j] = System.Math.Round(data[i, j], digits);
			});

			return result;
		}

		public static double[,] Floor(double[,] data, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
					result[i, j] = System.Math.Floor(data[i, j]);
			});

			return result;
		}

		/// <summary>Returns an array with all data values less than <i>clip_floor</i> replaced with <i>clip_floor</i>.</summary>
		/// <param name="data">A 2-D double array.</param>
		/// <param name="clip_floor">The value below which all data elements will be replaced with.</param>
		public static double[,] Floor(double[,] data, double clip_floor, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
				{
					if (data[i, j] < clip_floor)
						result[i, j] = clip_floor;
					else
						result[i, j] = data[i, j];
				}
			});

			return result;
		}

		public static double[,] Ceil(double[,] data, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
					result[i, j] = System.Math.Ceiling(data[i, j]);
			});

			return result;
		}

		public static double[,] Power(double[,] data, double exponent, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
					result[i, j] = System.Math.Pow(data[i, j], exponent);
			});

			return result;
		}

		public static double[,] Sqrt(double[,] data, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
					result[i, j] = System.Math.Sqrt(data[i, j]);
			});

			return result;
		}

		public static double[,] Log(double[,] data, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
				{
					if (data[i, j] <= 0)
						result[i, j] = 0;
					else
						result[i, j] = System.Math.Log10(data[i, j]);
				}
			});

			return result;
		}

		/// <summary>Return the custom-base logarithm of an array.</summary>
		/// <param name="data">A 2-D double array.</param>
		/// <param name="logbase">The base for the logarithm.</param>
		public static double[,] Log(double[,] data, double logbase, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
				{
					if (data[i, j] <= 0)
						result[i, j] = 0;
					else
						result[i, j] = System.Math.Log(data[i, j], logbase);
				}
			});

			return result;
		}

		/// <summary>Return the natural logarithm of an array.</summary>
		public static double[,] Ln(double[,] data, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
				{
					if (data[i, j] <= 0)
						result[i, j] = 0;
					else
						result[i, j] = System.Math.Log(data[i, j]);
				}
			});

			return result;
		}

		public static double[,] Exp(double[,] data, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
					result[i, j] = System.Math.Exp(data[i, j]);
			});

			return result;
		}

		public static double[,] Exp(double[,] data, double logbase, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, data.GetLength(0), opts, i =>
			{
				for (int j = 0; j < data.GetLength(1); j++)
					result[i, j] = System.Math.Pow(logbase, data[i, j]);
			});

			return result;
		}

		/// <summary>
		/// Radial Velocity due to earth’s rotation and orbital motion referred to solar system barycenter, in direction of target. Positive away; negative towards. Accurate to ~1 m/s.
		/// </summary>
		/// <param name="julianDate">Geocentric Julian Date of the observation</param>
		/// <param name="longitude">West longitude of observatory in degrees</param>
		/// <param name="latitude">Latitude of observatory in degrees</param>
		/// <param name="rightAscension">Right Ascension of target in degrees</param>
		/// <param name="declination">Declination of target in degrees</param>
		public static double RadialVelocityCorrection(double julianDate, double longitude, double latitude, double rightAscension, double declination)
		{
			//earth equatorial rotational linear velocity in m / s, based on spherical
			//earth using quadratic-mean(polar - equatorial) radius; can be improved to
			//take into account non-sphericity and geographical elevations, but these
			//are 2nd order corrections
			double vrot_eq = 465.1;			

			//astronomical unit(m)
			double au = 1.49597870e11;

			//latitude in radians:
			latitude = latitude * Math.PI / 180;			

			//declination in radians:
			declination = declination * Math.PI / 180;

			//west longitude of observatory in hours:
			double L = longitude / 15;

			//Greenwhich Mean Sidereal Time at JD:
			double GMST = Math.IEEERemainder(18.697374558 + 24.06570982441908 * (julianDate - 2451545.0), 24);

			//Local Sidereal Time at JD and longitude:
			double LST = GMST - L;

			//local hour angle of target:
			double ha = LST - rightAscension / 15;
			ha = ha * Math.PI / 12;//radians

			//decimal day number from J2000.0 UT 12hr:
			double n = julianDate - 2451545.0;

			//mean anomaly, in radians, at day number n:
			double g = Math.IEEERemainder((357.528 + .9856003 * n) * Math.PI / 180, 2 * Math.PI);

			//mean longitude, in radians, at n:
			L = Math.IEEERemainder((280.46 + .9856474 * n) * Math.PI / 180, 2 * Math.PI);
			
			//ecliptic longitude, in radians, at n:
			double lam = L + 1.915 * Math.PI / 180 * Math.Sin(g) + .020 * Math.PI / 180 * Math.Sin(2 * g);
			
			//ecliptic obliquity, in radians, at n:
			double eps = 23.439 * Math.PI / 180 - .0000004 * Math.PI / 180 * n;

			//distance of earth from sun in au’s at JD:
			double R = 1.00014 - 0.01671 * Math.Cos(g) - 0.00014 * Math.Cos(2 * g);

			//rectangular coordinates of earth wrt solar system barycenter, in au's:
			double X = -R * Math.Cos(lam);
			double Y = -R * Math.Cos(eps) * Math.Sin(lam);
			double Z = -R * Math.Sin(eps) * Math.Sin(lam);
			
			//first deriv's of XYZ above, wrt time in days (au/d). Note: deriv’s of XYZ w d/ dt eps ~0:
			double Xdot = .0172 * Math.Sin(lam);
			double Ydot = -.0158 * Math.Cos(lam);
			double Zdot = -.0068 * Math.Cos(lam);
			
			//rv in direction of target due to earth's rotational motion, +ve away, m/s
			double rv_rot = Math.Cos(latitude) * Math.Cos(declination) * Math.Sin(ha) * vrot_eq;

			//right ascension in radians:
			rightAscension = rightAscension * Math.PI / 180;

			//rv due to earth's orbital motion, +ve away au/d:
			double rv_orb = -Xdot * Math.Cos(rightAscension) * Math.Cos(declination) - Ydot * Math.Sin(rightAscension) * Math.Cos(declination) - Zdot * Math.Sin(declination);
			rv_orb = rv_orb * au / 86400;// convert to m/ s

			//Radial Velocity due to earth’s rotation and orbital motion referred to barycenter, in direction of target, +ve away.
			double RVC = rv_rot + rv_orb;

			return RVC;
		}

		/// <summary>
		/// Airmass of target: Young, A. T. 1994. Air mass and refraction. Applied Optics. 33:1108–1110
		/// </summary>
		/// <param name="julianDate">Geocentric Julian Date of the observation</param>
		/// <param name="longitude">West longitude of observatory in degrees</param>
		/// <param name="latitude">Latitude of observatory in degrees</param>
		/// <param name="rightAscension">Right Ascension of target in degrees</param>
		/// <param name="declination">Declination of target in degrees</param>
		public static double AirMass(double julianDate, double longitude, double latitude, double rightAscension, double declination)
		{
			//west longitude of observatory in hours:
			double L = longitude / 15;
			
			//Greenwhich Mean Sidereal Time at JD:
			double GMST = Math.IEEERemainder(18.697374558 + 24.06570982441908 * (julianDate - 2451545.0), 24);
			
			//Local Sidereal Time at JD and longitude:
			double LST = GMST - L;
			
			//local hour angle of target:
			double ha = LST - rightAscension / 15;
			ha = ha * Math.PI / 12;//radians
			
			//latitude in radians:
			double lat = latitude * Math.PI / 180;

			//declination in radians:
			double dec = declination * Math.PI / 180;

			//altitude:
			double alt = Math.Asin(Math.Sin(lat) * Math.Sin(dec) + Math.Cos(lat) * Math.Cos(dec) * Math.Cos(ha));
			
			//true zenith angle; don’t care about stuff below horizon:
			double zt = (Math.PI / 2 - alt);
			if (zt > Math.PI / 2 - Math.PI / 2 / 50)
				zt = Math.PI / 2 - Math.PI / 2 / 50;

			//Airmass of target: Young, A.T. 1994.Air mass and refraction.Applied Optics. 33:1108–1110.
			double A = (1.002432 * Math.Pow(Math.Cos(zt), 2) + 0.148386 * Math.Cos(zt) + 0.0096467) / (Math.Pow(Math.Cos(zt), 3) + 0.149864 * Math.Pow(Math.Cos(zt), 2) + 0.0102963 * Math.Cos(zt) + 0.000303978);

			return A;
		}

		/// <summary>Computes the Barycentric Julian Day Correction given a Julian Date and sky pointing coordinates. Accurate to ~1s.</summary>
		/// <param name="julianDate">The Julian Date.</param>
		/// <param name="RightAscension_deg">The right ascension in degrees.</param>
		/// <param name="Declination_deg">The declination in degrees.</param>
		/// <param name="returnCorrectionOnly">Return only the correction values (true), or return the Julian Dates with the correction applied (false) so that they are Barycentric values.</param>
		public static double BarycentricJulianDayCorrection(double julianDate, double RightAscension_deg, double Declination_deg, bool returnCorrectionOnly)
		{
			double cs = 173.14463348;// speed of light (au/d)
			double n, g, L, lam, eps, R, X, Y, Z, BJDC;

			// exact decimal day number from J2000.0 UT 12hr:
			n = julianDate - 2451545.0;
			
			// mean anomaly, in radians, at day number n:
			g = Math.IEEERemainder((357.528 + .9856003 * n) * Math.PI / 180, 2 * Math.PI);
			
			// mean longitude, in radians, at n:
			L = Math.IEEERemainder((280.46 + .9856474 * n) * Math.PI / 180, 2 * Math.PI);
			
			// ecliptic longitude, in radians, at n:
			lam = L + 1.915 * Math.PI / 180 * Math.Sin(g) + .020 * Math.PI / 180 * Math.Sin(2 * g);
			
			// ecliptic obliquity, in radians, at n:
			eps = 23.439 * Math.PI / 180 - .0000004 * Math.PI / 180 * n;
			
			// distance of earth from sun in au’s at JD:
			R = 1.00014 - 0.01671 * Math.Cos(g) - 0.00014 * Math.Cos(2 * g);
			
			// rectangular coordinates of earth wrt solar system barycenter, in au's:
			X = -R * Math.Cos(lam);
			Y = -R * Math.Cos(eps) * Math.Sin(lam);
			Z = -R * Math.Sin(eps) * Math.Sin(lam);

			BJDC = 1 / cs * (X * Math.Cos(RightAscension_deg) * Math.Cos(Declination_deg) + Y * Math.Sin(RightAscension_deg) * Math.Cos(Declination_deg) + Z * Math.Sin(Declination_deg));

			if (returnCorrectionOnly)
				return BJDC;
			else
				return julianDate + BJDC;
		}

		/// <summary>Computes the Barycentric Julian Day Correction given a Julian Date and sky pointing coordinates. Accurate to ~1s.</summary>
		/// <param name="julianDate">A vector of Julian Dates.</param>
		/// <param name="RightAscension_deg">The right ascension in degrees.</param>
		/// <param name="Declination_deg">The declination in degrees.</param>
		/// <param name="returnCorrectionOnly">Return only the correction values (true), or return the Julian Dates with the correction applied (false) so that they are Barycentric values.</param>
		public static double[] BarycentricJulianDayCorrection(double[] julianDate, double RightAscension_deg, double Declination_deg, bool returnCorrectionOnly)
		{
			double[] result = new double[julianDate.Length];

			for (int i = 0; i < julianDate.Length; i++)
				result[i] = BarycentricJulianDayCorrection(julianDate[i], RightAscension_deg, Declination_deg, returnCorrectionOnly);

			return result;
		}

		/// <summary>Computes the Barycentric Julian Day Correction given a Julian Date and sky pointing coordinates. Accurate to ~1s.</summary>
		/// <param name="julianDate">A vector of Julian Dates.</param>
		/// <param name="RightAscension_deg">A vector of right ascension in degrees.</param>
		/// <param name="Declination_deg">A vector of declination in degrees.</param>
		/// <param name="returnCorrectionOnly">Return only the correction values (true), or return the Julian Dates with the correction applied (false) so that they are Barycentric values.</param>
		public static double[] BarycentricJulianDayCorrection(double[] julianDate, double[] RightAscension_deg, double[] Declination_deg, bool returnCorrectionOnly)
		{
			double[] result = new double[julianDate.Length];

			for (int i = 0; i < julianDate.Length; i++)
				result[i] = BarycentricJulianDayCorrection(julianDate[i], RightAscension_deg[i], Declination_deg[i], returnCorrectionOnly);

			return result;
		}

		/// <summary>
		/// Convert Calendar Date post-1583 A.D. and Univeral Time to Julian Day Number
		/// </summary>
		/// <param name="date">Big-endian year, month, day string = '2000-03-20' or '2000:03:20' for example</param>
		/// <param name="utime">Universal time string = '04-15-16' or '04:15:16' for example</param>
		public static double[] DateToJD(string[] date, string[] utime)
		{
			double[] result = new double[date.Length];
			double year = 0;
			for (int i = 0; i < date.Length; i++)
				result[i] = DateToJD(date[i], utime[i], out year);

			return result;
		}

		/// <summary>
		/// Convert Calendar Date post-1583 A.D. and Univeral Time to Julian Day Number
		/// </summary>
		/// <param name="date">Big-endian year, month, day string = '2000-03-20' or '2000:03:20' for example</param>
		/// <param name="utime">Universal time string = '04-15-16' or '04:15:16' for example</param>
		/// <param name="yearpointyear">Get the year time as a year.year float value</param>
		public static double[] DateToJD(string[] date, string[] utime, out double[] yearpointyear)
		{
			double[] result = new double[date.Length];
			yearpointyear = new double[date.Length];
			double year = 0;
			for (int i = 0; i < date.Length; i++)
			{
				result[i] = DateToJD(date[i], utime[i], out year);
				yearpointyear[i] = year;
			}

			return result;
		}

		/// <summary>
		/// Convert Calendar Date post-1583 A.D. and Univeral Time to Julian Day Number
		/// </summary>
		/// <param name="date">Big-endian year, month, day string = '2000-03-20' or '2000:03:20' for example</param>
		/// <param name="utime">Universal time string = '04-15-16' or '04:15:16' for example</param>
		/// <param name="yearpointyear">Get the year time as a year.year float value</param>
		public static double DateToJD(string date, string utime, out double yearpointyear)
		{
			//%#days in each month for a LEAP year
			double[] monthLYdays = new double[12] { 0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30 };

			//%#days in each month for a NON-LEAP year
			double[] monthCYdays = new double[12] { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30 };

			//% reference Julian Date on 1583-01-01 @ 0.0hrs
			double JD1583UT00 = 2299238.5;

			//% get year, month, day
			string[] splitdate = date.Split(new string[] { "-", ":" }, StringSplitOptions.RemoveEmptyEntries);
			double year = Convert.ToDouble(splitdate[0]);
			double month = Convert.ToDouble(splitdate[1]);
			double day = Convert.ToDouble(splitdate[2]);

			//count the number of leap years and years since J2000
			double Nleapyears = 0, Ncommonyears = 0, yeardays = 365;
			double[] monthdays = monthCYdays;
			if (YearIsLeap(year))
			{
				monthdays = monthLYdays;
				yeardays = 366;
			}
			for (int i = 1583; i < year; i++)
			{
				if (YearIsLeap((double)i))
					Nleapyears++;
				else
					Ncommonyears++;
			}

			double Ndays = 366 * Nleapyears + 365 * Ncommonyears + day - 1;
			yearpointyear = year + (day - 1) / yeardays;
			for (int i = 0; i < month; i++)
			{
				Ndays += monthdays[i];
				yearpointyear += (monthdays[i] / yeardays);
			}

			string[] splittime = utime.Split(new string[] { "-", ":" }, StringSplitOptions.RemoveEmptyEntries);
			double t = Convert.ToDouble(splittime[0]) / 24 + Convert.ToDouble(splittime[1]) / 24 / 60 + Convert.ToDouble(splittime[2]) / 24 / 3600;
			yearpointyear += (t / yeardays);

			return JD1583UT00 + Ndays + t;
		}

		/// <summary>
		/// Convert Calendar Date post-1583 A.D. and Univeral Time to Julian Day Number
		/// </summary>
		/// <param name="date">Big-endian year, month, day string = '2000-03-20' or '2000:03:20' for example</param>
		/// <param name="utime">Universal time string = '04-15-16' or '04:15:16' for example</param>
		public static double DateToJD(string date, string utime)
		{
			return DateToJD(date, utime, out double outyear);
		}

		/// <summary>
		/// Check if year post-1583 A.D. is a leap year
		/// </summary>
		/// <param name="year"></param>
		public static bool YearIsLeap(double year)
		{
			if (Math.IEEERemainder(year, 4) != 0)
				return false;
			else if (Math.IEEERemainder(year, 100) != 0)
				return true;
			else if (Math.IEEERemainder(year, 400) != 0)
				return false;
			else
				return true;
		}

		#endregion

		#region MATH

		/// <summary>Returns the angle between -PI to +PI radians following the CAST convention given the run (x) and rise (y) of the direction vector.</summary>
		/// <param name="run">The run (signed horizontal amplitude) of the vector.</param>
		/// <param name="rise">The rise (signed vertical amplitude) of the vector.</param>
		public static double aTanAbsoluteAngle(double run, double rise)
		{
			return Math.Atan2(rise, run);
		}

		/// <summary>Returns true if an integer is even, false if odd.</summary>
		public static bool IsEven(int x)
		{
			return (System.Math.IEEERemainder((double)(x), 2) == 0);
		}

		/// <summary>Returns true if a String can convert to a number, false if it can not.</summary>
		public static bool IsNumeric(string x)
		{
			try
			{
				Convert.ToDouble(x);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>Returns true if a number is an integer, false if it is not.</summary>
		public static bool IsInteger(double x)
		{
			if (Math.Truncate(x) == x)
				return true;
			else
				return false;
		}

		/// <summary>Returns true if a string number is an integer, false if it is not.</summary>
		public static bool IsInteger(string x)
		{
			return IsInteger(Convert.ToDouble(x));
		}

		#endregion

		#region FUNCTIONS

		public static double[] CosineBell(int length)
		{
			double[] result = new double[length];
			double L = (double)length;

			for (int i = 0; i < (int)L; i++)
				result[i] = -(1 + Math.Cos(((double)i / (L - 1)) * 2 * Math.PI)) * .5 + 1;

			return result;
		}

		public static double[] Hanning(double[] data)
		{
			double[] result = new double[(data.Length)];
			double L = (double)data.Length;
			double bell;
			for (int i = 0; i < (int)L; i++)
			{
				bell = -(1 + Math.Cos(((double)i / (L - 1)) * 2 * Math.PI)) * .5 + 1;
				result[i] = data[i] * bell;
			}

			return result;
		}

		/// <summary>
		/// Returns an input data array multiplied by the Hanning Function (Cosine Bell)
		/// </summary>
		/// <param name="data">The ipnut array.</param>
		/// <param name="do_parallel">Perform array operations with parallelism.</param>
		public static double[,] Hanning(double[,] data, bool do_parallel = false)
		{
			double[,] result = new double[data.GetLength(0), data.GetLength(1)];
			double HL = (double)data.GetLength(0);
			double VL = (double)data.GetLength(1);

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			if (HL > 1 && VL > 1)
			{
				Parallel.For(0, data.GetLength(0), opts, x =>
				{
					double bell;
					for (int y = 0; y < data.GetLength(1); y++)
					{
						bell = -(1 + Math.Cos(((double)(x) / (HL - 1)) * 2 * Math.PI)) * .5 + 1;
						bell *= (-(1 + Math.Cos(((double)(y) / (VL - 1)) * 2 * Math.PI)) * .5 + 1);
						result[x, y] = data[x, y] * bell;
					}
				});
			}

			if (HL == 1)
			{
				Parallel.For(0, data.GetLength(1), opts, y =>
				{
					double bell = (-(1 + Math.Cos(((double)(y) / (VL - 1)) * 2 * Math.PI)) * .5 + 1);
					result[0, y] = data[0, y] * bell;
				});
			}

			if (VL == 1)
			{
				Parallel.For(0, data.GetLength(0), opts, x =>
				{
					double bell = -(1 + Math.Cos(((double)(x) / (HL - 1)) * 2 * Math.PI)) * .5 + 1;
					result[x, 0] = data[x, 0] * bell;
				});
			}

			return result;
		}

		/// <summary>Returns a circular Gaussian centered on a central pixel in a 2-D array with a given amplitude and Full Width Half Maximum.</summary>
		/// <param name="Amplitude">The amplitude of the Gaussian.</param>
		/// <param name="FWHM">The Full Width Half Maximum of the Gaussian.</param>
		/// <param name="HalfWidth">The half-width or square-radius of the return array at which the Gaussian is calculated.</param>
		public static double[,] Gaussian(double Amplitude, double FWHM, int HalfWidth, bool do_parallel = false)
		{
			double sig = FWHM / (2 * System.Math.Sqrt(2 * System.Math.Log(2)));
			int width = HalfWidth * 2 + 1;
			double[,] result = new double[width, width];
			double twosigsq = 2 * sig * sig;
			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			Parallel.For(0, width, opts, i =>
			{
				double imid = (double)(i) - HalfWidth;
				for (int j = 0; j < width; j++)
				{
					double jmid = (double)(j) - HalfWidth;
					result[i, j] = Amplitude * System.Math.Exp(-(imid * imid + jmid * jmid) / twosigsq);
				}
			});

			return result;
		}

		public static double[,] Sersic(double Effective_Radius_Re_PIXELS, double Effective_Radius_Io, double SersicFactor_n, int CalculationRadius_NRe)
		{
			int width = (int)(CalculationRadius_NRe * Effective_Radius_Re_PIXELS) * 2 + 1;
			double[,] result = new double[width, width];
			int hw = (width - 1) / 2;

			/*for (int x = -hw; x <= hw; x++)
				for (int y = -hw; y <= hw; y++)
				{
					double r_sq = x * x + y * y;
					result[x, y] = 0;
				}*/

			return result;
		}

		/// <summary>Computes the elements for a Gaussian curve G(x|p)
		/// <br />G(x|p) = p(0) * exp( -((x - p(1))^2) / (2*p(2)^2) ) + p(3)</summary>
		/// <param name="xdata">The x-data grid positions of the Gaussian data. If nullptr is passed a vector will be created of appropriate size, centered on zero.</param>
		/// <param name="G">The values of the data to be computed for the Gaussian.</param>
		/// <param name="p">The parameters of the Gaussian.
		/// <br />p[0] = amplitude; <br />p[1] = x-center; <br />p[2] = sigma; <br />p[3] = bias</param>
		public static void Gaussian1d(double[] xdata, ref double[] G, double[] p)
		{
			int xw = G.Length;
			double xhw = (double)(xw - 1) / 2.0;

			if (xdata == null)
			{
				xdata = new double[xw];
				for (int i = 0; i < xw; i++)
					xdata[i] = (double)(i) - xhw;
			}

			for (int i = 0; i < xw; i++)
				G[i] = p[0] * Math.Exp(-((xdata[i] - p[1]) * (xdata[i] - p[1])) / (2 * p[2] * p[2])) + p[3];
		}

		/// <summary>Computes the elements for a Moffat curve M(x|p)
		/// <br />M(x|p) = p(0) * ( 1 + (x - p(1))^2 / p(2)^2 )^(-p(3)) + p(4)</summary>
		/// <param name="xdata">The x-data grid positions of the Moffat data. If nullptr is passed a vector will be created of appropriate size, centered on zero.</param>
		/// <param name="M">The values of the data to be computed for the Moffat.</param>
		/// <param name="p">The parameters of the Moffat: <br />p[0] = amplitude; <br />p[1] = x-center; <br />p[2] = theta; <br />p[3] = beta; <br />p[4] = bias</param>
		public static void Moffat1d(double[] xdata, ref double[] M, double[] p)
		{
			int xw = M.Length;
			double xhw = (double)(xw - 1) / 2.0;

			if (xdata == null)
			{
				xdata = new double[xw];
				for (int i = 0; i < xdata.GetLength(0); i++)
					xdata[i] = (double)(i) - xhw;
			}

			for (int i = 0; i < xw; i++)
				M[i] = p[0] * Math.Pow(1.0 + ((xdata[i] - p[1]) * (xdata[i] - p[1])) / p[2] / p[2], -p[3]) + p[4];
		}

		/// <summary>Computes the elements for a 2-d Gaussian surface G(x,y|p)
		/// <br />G(x,y|p) = p(0)*exp( -((x - p(1))^2 + (y - p(2))^2) / (2*p(3)^2) ) + p(4)
		/// <br />or
		/// <br />G(x,y|p) = p(0)*exp( -((x - p(1))*cosd(p(3)) + (y - p(2))*sind(p(3)))^2 / (2*p(4)^2) - (-(x - p(1))*sind(p(3)) + (y - p(2))*cosd(p(3))).^2 / (2*p(5)^2) ) + p(6)
		/// <br />The form of G(x,y|p) used is determined by the length of the parameter vector p</summary>
		/// <param name="xdata">The x-data grid positions of the Gaussian data.</param>
		/// <param name="ydata">The y-data grid positions of the Gaussian data.</param>
		/// <param name="p">The parameters of the Gaussian.  Options are:
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = sigma; p[4] = bias
		/// <br />or
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = phi; p[4] = x-sigma; p[5] = y-sigma; p[6] = bias</param>
		public static double[,] Gaussian2d(int[] xdata, int[] ydata, double[] p, bool do_parallel = false)
		{
			double[,] G = new double[xdata.Length, ydata.Length];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			if (p.Length == 5)
			{
				Parallel.For(0, G.GetLength(0), opts, x =>
				{
					for (int y = 0; y < G.GetLength(1); y++)
						G[x, y] = p[0] * Math.Exp(-(((double)xdata[x] - p[1]) * ((double)xdata[x] - p[1]) + ((double)ydata[y] - p[2]) * ((double)ydata[y] - p[2])) / (2 * p[3] * p[3])) + p[4];
				});
			}

			if (p.Length == 7)
			{
				Parallel.For(0, G.GetLength(0), opts, x =>
				{
					for (int y = 0; y < G.GetLength(1); y++)
						G[x, y] = p[0] * Math.Exp(-Math.Pow(((double)xdata[x] - p[1]) * Math.Cos(p[3]) + ((double)ydata[y] - p[2]) * Math.Sin(p[3]), 2) / (2 * p[4] * p[4]) - Math.Pow(-((double)xdata[x] - p[1]) * Math.Sin(p[3]) + ((double)ydata[y] - p[2]) * Math.Cos(p[3]), 2) / (2 * p[5] * p[5])) + p[6];
				});
			}

			return G;
		}

		/// <summary>Computes the elements for a 2-d Moffat surface M(x,y|p)
		/// <br />M(x,y|p) = p(0) * ( 1 + { (x-p(1))^2 + (y-p(2))^2 } / p(3)^2 ) ^ (-p(4)) + p(5)
		/// <br />or
		/// <br />M(x,y|p) = p(0) * ( 1 + { ((x-p(1))*cosd(p(3)) + (y-p(2))*sind(p(3)))^2 } / p(4)^2 + { (-(x-p(1))*sind(p(3)) + (y-p(2))*cosd(p(3)))^2 } / p(5)^2 ) ^ (-p(6)) + p(7)
		/// <br />The form of M(x,y|p) used is determined by the length of the parameter vector p</summary>
		/// <param name="xdata">The x-data grid positions of the Moffat data.</param>
		/// <param name="ydata">The y-data grid positions of the Moffat data.</param>
		/// <param name="p">The parameters of the Moffat. Options are:
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = theta; p[4] = beta; p[5] = bias;
		/// <br />or
		/// <br />p[0] = amplitude; p[1] = x-center; p[2] = y-center; p[3] = phi; p[4] = x-theta; p[5] = y-theta; p[6] = beta; p[7] = bias;</param>
		public static double[,] Moffat2d(int[] xdata, int[] ydata, double[] p, bool do_parallel = false)
		{
			double[,] M = new double[xdata.Length, ydata.Length];

			ParallelOptions opts = new ParallelOptions();
			if (do_parallel)
				opts.MaxDegreeOfParallelism = Environment.ProcessorCount;
			else
				opts.MaxDegreeOfParallelism = 1;

			if (p.Length == 6)
			{
				Parallel.For(0, M.GetLength(0), opts, i =>
				{
					for (int j = 0; j < M.GetLength(1); j++)
						M[i, j] = p[0] * Math.Pow(1.0 + (((double)xdata[i] - p[1]) * ((double)xdata[i] - p[1]) + ((double)ydata[j] - p[2]) * ((double)ydata[j] - p[2])) / p[3] / p[3], -p[4]) + p[5];
				});
			}

			if (p.Length == 8)
			{
				Parallel.For(0, M.GetLength(0), opts, i =>
				{
					for (int j = 0; j < M.GetLength(1); j++)
						M[i, j] = p[0] * Math.Pow(1.0 + (Math.Pow(((double)xdata[i] - p[1]) * Math.Cos(p[3]) + ((double)ydata[j] - p[2]) * Math.Sin(p[3]), 2)) / (p[4] * p[4]) + (Math.Pow(-((double)xdata[i] - p[1]) * Math.Sin(p[3]) + ((double)ydata[j] - p[2]) * Math.Cos(p[3]), 2)) / (p[5] * p[5]), -p[6]) + p[7];
				});
			}

			return M;
		}

		public static double[] FourierPolynomial(double[] xdata, double[] p)
		{
			double[] result = new double[xdata.Length];

			for (int x = 0; x < xdata.Length; x++)
			{
				result[x] = p[p.Length - 2];

				for (int i = 0; i < (p.Length - 2) / 2; i++)
					result[x] += p[i * 2] * Math.Cos((double)(i + 1) * xdata[x] * p[p.Length - 1] / 2 / Math.PI) + p[i * 2 + 1] * Math.Sin((double)(i + 1) * xdata[x] * p[p.Length - 1] / 2 / Math.PI);
			}
			return result;
		}

		#endregion

	}
}

