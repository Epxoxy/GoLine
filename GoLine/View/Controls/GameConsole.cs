using System.Windows.Media;
using System;
using GameServices.GameBasic;

namespace GoLine
{
    public class GameConsole : IGameBoard<int, Spot>
    {
        public GameConsole()
        {
        }

        public void Clear()
        {
        }
        public bool Hand(int t1, Spot t2)
        {
            Debug.Log(t1 + " -> " + t2.X + "," + t2.Y);
            return true;
        }

        public bool UndoHand(int t1, Spot t2)
        {
            Debug.Log("Undo " + t1 + " -> " + t2.X + "," + t2.Y);
            return true;
        }
    }
}
