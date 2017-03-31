using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServices.GameBasic
{
    class OnlinePlayer : Player
    {
        public OnlinePlayer() { }
        public OnlinePlayer(string name) : base(name) { }
        public OnlinePlayer(IGameService gameService, string name = "") : base(gameService,name) { }

        public override RequestResult HandRequest(RequestType type)
        {
            //TODO When is use in online, connect to player and ask for allow
            return base.HandRequest(type);
        }
    }
}
