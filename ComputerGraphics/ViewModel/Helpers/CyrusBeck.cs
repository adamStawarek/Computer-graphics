using System;
using System.Collections.Generic;
using System.Drawing;

namespace ImageEditor.ViewModel.Helpers
{
    public class CyrusBeck
    {
        public struct Polygon
        {
            public int nPoints;
            public List<Point> v;
        }
        public static bool CBClip(Point p1, Point p2, Point[] n, Polygon polygon, bool visible, Point rp, Point q)
        {
            var dirV = new Point(); // vectors
            var F = new Point();

            // start largest at smallest legal value and smallest 
            // at largest legal value
            double t1 = 0;
            double t2 = 1;
            // compute the direction vector
            dirV.X = p2.X - p1.X;
            dirV.Y = p2.Y - p1.Y;

            var i = 0;
            while ((i < polygon.nPoints) && visible)
            {
                F.X = p1.X - polygon.v[i].X;
                F.Y = p1.Y - polygon.v[i].Y;
                var num = DotProduct(n[i], new Point(F.X, F.Y));
                var den = DotProduct(n[i], new Point(dirV.X, dirV.Y));

                if (Math.Abs(den) < 0.01) // Parallel or Point
                {
                    if (num > 0.0F)
                    {
                        visible = false; //   Parallel and outside or point (p1 == p2) and outside
                    }
                }
                else
                {
                    var t = -(num / den);
                    if (den < 0.0F) // entering
                    {
                        if (t <= 1.0F)
                        {
                            if (t > t1)
                            {
                                t1 = t;
                            }
                        }
                    }
                    else if (t >= 0.0F) //exiting
                    {
                        if (t < t2)
                        {
                            t2 = t;
                        }
                    }
                }
                i++;
            }
            if (t1 <= t2)
            {
                rp.X = (int)(p1.X + t1 * dirV.X);
                rp.Y = (int)(p1.Y + t1 * dirV.Y);
                q.X = (int)(p1.X + t2 * dirV.X);
                q.Y = (int)(p1.Y + t2 * dirV.Y);
            }
            else
            {
                visible = false;
            }

            return visible;
        }



        // compute the outer normals.  
        // note that this requires that the polygon be convex
        // to always work
        public static Point[] CalcNormals(List<Point> points)//points are polygon points
        {
            var normals = new Point[points.Count];

            var v = new Point();
            for (int i = 0; i < points.Count; i++)
            {
                var j = (i + 1) % points.Count;
                var k = (i + 2) % points.Count;
                // make vector be -1/mI + 1J
                normals[i].X = -(points[j].Y - points[i].Y) / (points[j].X - points[i].X);
                normals[i].Y = 1;
                v.X = points[k].X - points[i].X;
                v.Y = points[k].Y - points[i].Y;
                if (DotProduct(normals[i], new Point(v.X, v.Y)) > 0F) // inner normal
                {
                    normals[i].X *= -1;
                    normals[i].Y = -1;
                }
            }

            return normals;
        }

        public static double DotProduct(Point v1, Point v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }
    }
}
