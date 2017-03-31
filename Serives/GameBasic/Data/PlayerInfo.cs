namespace GameServices.GameBasic
{
    public class PlayerInfo
    {
        public string Name { get; private set; }
        public int TokenID { get; private set; }
        public GameScore Score { get; private set; }
        public System.TimeSpan TimeSpan
        {
            get
            {
                if (stopwatch == null) return new System.TimeSpan();
                return stopwatch.Elapsed;
            }
        }

        public PlayerInfo() { }
        public PlayerInfo(Player player)
        {
            if(player != null)
            {
                Name = player.Name;
                TokenID = player.TokenID;
                Score = player.Score;
            }
        }
        public PlayerInfo(string name, int tokenid, GameScore score)
        {
            Name = name;
            TokenID = tokenid;
            Score = score;
        }


        internal System.Diagnostics.Stopwatch stopwatch;
    }
}
