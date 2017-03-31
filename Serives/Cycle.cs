using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Services.GameBasic;

namespace Services
{
    public class PlayerInfoLogger
    {
        private Dictionary<IPlayer, PlayerInfo> infoDictionary;
        internal Dictionary<IPlayer, PlayerInfo> PlayersInfo
        {
            get
            {
                return infoDictionary;
            }
            private set
            {
                infoDictionary = value;
            }
        }
        private CycleIndexer cycleIndexer;
        internal CycleIndexer CycleIndexer
        {
            get { return cycleIndexer; }
            private set { cycleIndexer = value; }
        }

        internal PlayerInfoLogger(Dictionary<IPlayer, PlayerInfo>  playersInfo)
        {
            PlayersInfo = playersInfo;
            CycleIndexer = new CycleIndexer(PlayersInfo);
        }

        internal int GetIndexOf(IPlayer player)
        {
            for (int index = 0; index < PlayersInfo.Count; ++index)
            {
                if (PlayersInfo.Skip(index).First().Key == player)
                {
                    return index;
                }
            }
            return -1;
        }
    }
}
