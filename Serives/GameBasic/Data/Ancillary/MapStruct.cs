using System.Collections.Generic;

namespace GameServices.GameBasic
{
    public class MapStruct
    {
        private static MapStruct mapStruct;
        public static MapStruct GetMapStruct()
        {
            if (mapStruct == null) mapStruct = new MapStruct();
            return mapStruct;
        }

        public int[,] DefaultEntry
        {
            get
            {
                return new int[,]
                {
                    { 1,0,0,1,0,0,1 },
                    { 0,0,1,1,1,0,0 },
                    { 0,1,0,1,0,1,0 },
                    { 1,1,1,0,1,1,1 },
                    { 0,1,0,1,0,1,0 },
                    { 0,0,1,1,1,0,0 },
                    { 1,0,0,1,0,0,1 }
                };
            }
        }

        private MapStruct() { }

        public IEnumerable<XY3Line> XY3Lines => XY3LineList;

        private List<XY3Line> xy3LineList;
        private List<XY3Line> XY3LineList
        {
            get
            {
                if (xy3LineList == null)
                {
                    int[,] linesArray = new int[,]
                    { //Horizontal
                        {0,0, 3,0, 6,0 },{0,3, 1,3, 2,3 },{0,6, 3,6, 6,6 },{1,2, 3,2, 5,2 },
                        {1,4, 3,4, 5,4 },{2,1, 3,1, 4,1 },{2,5, 3,5, 4,5 },{4,3, 5,3, 6,3 },
                        //Vertical
                        {0,0, 0,3, 0,6 },{1,2, 1,3, 1,4 },{3,0, 3,1, 3,2 },
                        {3,4, 3,5, 3,6 },{5,2, 5,3, 5,4 },{6,0, 6,3, 6,6 },
                        //Declining
                        {1,2, 2,3, 3,4 },{1,4, 2,5, 3,6 },{3,0, 4,1, 5,2 },{3,2, 4,3, 5,4 },//-1
                        {1,2, 2,1, 3,0 },{1,4, 2,3, 3,2 },{3,4, 4,3, 5,2 },{3,6, 4,5, 5,4 },//1
                    };
                    xy3LineList = new List<XY3Line>();
                    for (int i = 0; i < linesArray.GetLength(0); ++i)
                    {
                        XY3Line xy3Line = new XY3Line()
                        {
                            X1 = linesArray[i, 0],
                            Y1 = linesArray[i, 1],
                            X2 = linesArray[i, 2],
                            Y2 = linesArray[i, 3],
                            X3 = linesArray[i, 4],
                            Y3 = linesArray[i, 5]
                        };
                        xy3LineList.Add(xy3Line);
                    }
                }
                return xy3LineList;
            }
        }

        int[,] scores = new int[,]
       {
            { 2,0,0,4,0,0,2 },
            { 0,0,2,2,2,0,0 },
            { 0,4,0,4,0,4,0 },
            { 2,2,3,0,3,2,2 },
            { 0,4,0,4,0,4,0 },
            { 0,0,2,2,2,0,0 },
            { 2,0,0,4,0,0,2 }
       };
    }
}
