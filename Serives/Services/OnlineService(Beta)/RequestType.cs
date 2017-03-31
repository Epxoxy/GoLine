using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServices.GameBasic
{
    public enum RequestType
    {
        SelfFist,
        SelfUndo,
        SelfRedo,
        Tie,
        GiveUp,
        Restart
    }
}
