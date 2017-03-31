using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServices.GameBasic
{
    public class GameEventArgs : EventArgs
    {
        public GameEventType Type { get; private set; }
        public Player Player { get; private set; }
        public Spot Spot { get; private set; }
        public GameEventArgs()
        {

        }
        public GameEventArgs(Player player)
        {
            Player = player;
        }
        public GameEventArgs(GameEventType type, Player player)
        {
            Type = type;
            Player = player;
        }
        public GameEventArgs(GameEventType type, Player player, Spot spot)
        {
            Type = type;
            Player = player;
            Spot = spot;
        }
    }


}
