using GameServices.GameBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServices
{
    public class PlayerService
    {
        public static bool TryCreateAI(AIType type, out Player player)
        {
            player = null;
            switch (type)
            {
                case AIType.Elementary:
                    player = new RandomAIPlayer("Elementary");
                    break;
                case AIType.Intermediate:
                    player = new ScoreAIPlayer("Intermediate");
                    break;
                case AIType.Advanced:
                    player = new ABCutAIPlayer("Advanced");
                    break;
                default: return false;
            }
            return true;
        }

        public static bool TryCreatePlayer(OnlineAccount account, out Player player)
        {
            player = null;
            if (account == null) return false;
            player = new Player(account.NickName)
            {
                Score = account.Score
            };
            return true;
        }

        public static bool TryCreateGuest(string nickname, out Player player)
        {
            player = null;
            var result = SqliteService.GetService().QueryScore($"where nickname='{nickname}' and userid=0");
            if (result == null)
            {
                GameScore storeScore = new GameScore()
                {
                    NickName = nickname,
                    ModifyTime = DateTime.Now
                };
                if (SqliteService.GetService().ExecuteNonQuery(storeScore.GetInsert("scores")) < 1)
                    return false;
                player = new Player()
                {
                    Name = nickname,
                    Score = storeScore
                };
            }
            else
            {
                player = new Player()
                {
                    Name = result.NickName,
                    Score = result
                };
            }
            return true;
        }

    }
}
