using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServices.GameBasic
{
    public struct Spot
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Data { get; set; }
        public Spot(int x, int y): this(x,y,0)
        {
            X = x;
            Y = y;
        }
        public Spot(int x, int y, int data)
        {
            X = x;
            Y = y;
            Data = data;
        }

        public bool Equals(Spot step)
        {
            return step.X == this.X && step.Y == this.Y && step.Data == this.Data;
        }

        public override string ToString()
        {
            return $"{X}, {Y}, {Data}";
        }
    }
}
