namespace GameServices.GameBasic
{
    public interface IPlayer
    {
        string Name { get; set; }
        bool IsAI { get; }
        int TokenID { get; }
        GameScore Score { get;}
        IGameService ServicesProvider { get; }
        Player NextPlayer { get; }
        Player FrontPlayer { get; }
        void RegisterService(IGameService servicesProvider);
        bool IsMyTurn { get; }
        bool Input(Location location);
        bool Undo();
        RequestResult HandRequest(RequestType type);
    }
}
