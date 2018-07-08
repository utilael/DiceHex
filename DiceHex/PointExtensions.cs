using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace DiceHex
{
    class PointExtensions
    {
        public static Point add(Point h, Point a, int multiply = 1)
        {
            return new Point(h.X + (a.X * multiply), h.Y + (a.Y * multiply));
        }

        public static Point[] add(Point[] h, Point a)
        {
            Point[] p = new Point[h.Count()];
            for (int i = 0; i < h.Count(); i++)
            {
                p[i].X = h[i].X + a.X;
                p[i].Y = h[i].Y + a.Y;
            }
            return p;
        }

        public static bool isAdjacent(Point a, Point b)
        {
            if ((Math.Abs(a.X - b.X) == 1 && a.Y == b.Y) || (Math.Abs(a.Y - b.Y) == 1 && a.X == b.X))
                return true;

            if (a.X % 2 == 0)
                if (Math.Abs(a.X - b.X) == 1 && a.Y - b.Y == 1)
                    return true;
                else
                if (Math.Abs(a.X - b.X) == 1 && b.Y - a.Y == 1)
                    return true;

            return false;
        }

        public static Point[] getAdjacent(Point a, Point max)
        {
            List<Point> returnPoints = new List<Point>();

            if (a.X != 0)
                returnPoints.Add(new Point(a.X - 1, a.Y));
            if (a.X != max.X - 1)
                returnPoints.Add(new Point(a.X + 1, a.Y));
            if (a.Y != 0)
                returnPoints.Add(new Point(a.X, a.Y - 1));
            if (a.Y != max.Y - 1)
                returnPoints.Add(new Point(a.X, a.Y + 1));

            if (a.X % 2 == 0)
            {
                if (a.Y != 0)
                {
                    if (a.X != 0)
                        returnPoints.Add(new Point(a.X - 1, a.Y - 1));
                    if (a.X != max.X - 1)
                        returnPoints.Add(new Point(a.X + 1, a.Y - 1));
                }
            }
            else
            {
                if (a.Y != max.Y - 1)
                {
                    if (a.X != 0)
                        returnPoints.Add(new Point(a.X - 1, a.Y + 1));
                    if (a.X != max.X - 1)
                        returnPoints.Add(new Point(a.X + 1, a.Y + 1));
                }
            }

            return returnPoints.ToArray<Point>();
        }

        public static Point[] getOuterAdjacent(Point a, Point max)
        {
            List<Point> returnPoints = new List<Point>();
            List<Point> adjacents = getAdjacent(a, max).ToList<Point>();
            foreach (Point p in adjacents)
            {
                foreach (Point _p in getAdjacent(p, max))
                {
                    if (!adjacents.Contains(_p) && !a.Equals(_p))
                        returnPoints.Add(_p);
                }

            }

            return returnPoints.ToArray<Point>();
        }

        public static Point[][] getLineAdjacent(Point a, Point max)
        {
            List<Point[]> returnPoints = new List<Point[]>();
            List<Point> upPoints = new List<Point>();
            List<Point> dnPoints = new List<Point>();
            List<Point> luPoints = new List<Point>();
            List<Point> ruPoints = new List<Point>();
            List<Point> ldPoints = new List<Point>();
            List<Point> rdPoints = new List<Point>();

            for (int i = a.Y + 1; i < max.Y; i++)
                upPoints.Add(new Point(a.X, i));
            for (int i = a.Y - 1; i >= 0; i--)
                dnPoints.Add(new Point(a.X, i));

            int x = a.X;
            int y = a.Y;
            x++;
            if (x % 2 == 0)
                y++;
            while (x < max.X && y < max.Y)
            {
                rdPoints.Add(new Point(x, y));
                x++;
                if (x % 2 == 0)
                    y++;
            }

            x = a.X;
            y = a.Y;
            x++;
            if (x % 2 == 1)
                y--;
            while (x < max.X && y >= 0)
            {
                ruPoints.Add(new Point(x, y));
                x++;
                if (x % 2 == 1)
                    y--;
            }

            x = a.X;
            y = a.Y;
            x--;
            if (x % 2 == 1)
                y--;
            while (x >= 0 && y >= 0)
            {
                luPoints.Add(new Point(x, y));
                x--;
                if (x % 2 == 1)
                    y--;
            }

            x = a.X;
            y = a.Y;
            x--;
            if (x % 2 == 0)
                y++;
            while (x >= 0 && y < max.Y)
            {
                ldPoints.Add(new Point(x, y));
                x--;
                if (x % 2 == 0)
                    y++;
            }

            returnPoints.Add(upPoints.ToArray<Point>()); returnPoints.Add(dnPoints.ToArray<Point>());
            returnPoints.Add(luPoints.ToArray<Point>()); returnPoints.Add(ruPoints.ToArray<Point>());
            returnPoints.Add(ldPoints.ToArray<Point>()); returnPoints.Add(rdPoints.ToArray<Point>());

            return returnPoints.ToArray<Point[]>();
        }
    }
}
