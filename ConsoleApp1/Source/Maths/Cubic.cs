using System;
using System.Numerics;
using MathNet.Numerics.Interpolation;

public class SplineInterpolator
{
    private IInterpolation spline;

    public SplineInterpolator(Vector2[] points)
    {
        double[] xValues = new double[points.Length];
        double[] yValues = new double[points.Length];
        
        for(int i = 0; i < points.Length; i++)
        {
            xValues[i] = points[i].X;
            yValues[i] = points[i].Y;
        }
        
        spline = CubicSpline.InterpolatePchipSorted(xValues, yValues);
    }

    public double Interpolate(double x)
    {
        return spline.Interpolate(x);
    }
}