using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServices.GameBasic
{
    public class GameEndedEventArgs : EventArgs
    {
        public bool IsInterrupt { get; private set; }
        public GameResult Result { get; private set; }
        public bool HasWinner { get; private set; }
        public Player Winner { get; private set; }
        public GameEndedEventArgs()
        {
        }
        public GameEndedEventArgs(bool isInterrupt)
        {
            IsInterrupt = isInterrupt;
        }
        public GameEndedEventArgs(Player player)
        {
            Winner = player;
            HasWinner = true;
        }
    }
}
