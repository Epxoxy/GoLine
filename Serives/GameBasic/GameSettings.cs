using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServices.GameBasic
{
    public class GameSettings
    {
        public GameMode Mode { get; set; }
        public int MaxStep { get; } = 12;
        public int PlayerLimits { get; } = 2;
    }

}
