﻿namespace GameServices.GameBasic
{
    /// <summary>
    /// One line that contains three point than default line
    /// </summary>
    public class XY3Line
    {
        public int X1 { get; set; }
        public int X2 { get; set; }
        public int X3 { get; set; }
        public int Y1 { get; set; }
        public int Y2 { get; set; }
        public int Y3 { get; set; }
        public XY3Line() { }
        public XY3Line(int x1, int y1, int x2, int y2, int x3, int y3)
        {
            X1 = x1;
            X2 = x2;
            X3 = x3;
            Y1 = y1;
            Y2 = y2;
            Y3 = y3;
        }
        
        public bool IsInLine(int x, int y)
        {
            return (X1 == x && Y1 == y) || (X2 == x && Y2 == y) || (X3 == x && Y3 == y);
        }
        public Location[] ToLocations()
        {
            return new Location[] 
            {
                new Location(X1,Y1),
                new Location(X2, Y2),
                new Location(X3,Y3)
            };
        }
    }
}
