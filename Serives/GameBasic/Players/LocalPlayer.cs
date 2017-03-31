using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServices.GameBasic
{
    public class LocalPlayer : Player
    {
        public LocalPlayer() { }
        public LocalPlayer(string name) : base(name) { }
        public LocalPlayer(IGameService gameServices, string name = "") : base(gameServices,name) { }

        public override RequestResult HandRequest(RequestType type)
        {
            return base.HandRequest(type);
        }
    }
}
