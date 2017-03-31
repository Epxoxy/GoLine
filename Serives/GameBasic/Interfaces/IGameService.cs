using System;
using System.Collections.Generic;

namespace GameServices.GameBasic
{
    public interface IGameService
    {
        bool IsGameStarted { get;}
        event EventHandler GameStarting;
        event EventHandler<GameEventArgs> GameDataUpdated;
        event EventHandler<GameEndedEventArgs> GameEnded;
        event EventHandler<GameEventArgs> ActivePlayerChanged;
        SpotDataLogger DataLogger { get;}
        IList<PlayerInfo> PlayersInfo { get; }
        Player CurrentPlayer { get; }
        Player Winner { get; }
        PlayerInfo GetInfoOf(Player player);
        bool JoinPlayer(Player player);
        void LeavePlayer(Player player);
        void SetFirstPlayer(Player player);
        bool HandInput(Player player, Location location, ActionType type);
        bool StartNewGame();
        void EndGame();
    }
}
