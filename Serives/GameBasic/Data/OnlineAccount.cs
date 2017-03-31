namespace GameServices
{
    public class OnlineAccount
    {
        public int UserID { get; set; }
        public string NickName { get; set; }
        public string UserName { get; set; }
        public AccountState State { get; set; }
        public GameBasic.GameScore Score { get; set; }
    }

    public enum AccountState
    {
        None,
        Offline,
        Online
    }
}
