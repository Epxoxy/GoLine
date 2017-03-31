using System;
using System.Collections.Generic;
using System.Linq;
using GameServices.GameBasic;

namespace GameServices
{
    public class GameService : IGameService
    {
        #region --------Private Member--------

        private System.Diagnostics.Stopwatch stopwatch;
        private Dictionary<int, Player> playersDictionary;
        private Dictionary<int, PlayerInfo> infoDictionary;

        #endregion

        #region --------Property--------

        public GameMode CurrentMode { get; set; }
        public MapStruct MapStruct => MapStruct.GetMapStruct();
        public SpotDataLogger DataLogger { get; private set; }
        public TimeSpan Elapsed => stopwatch == null ? new TimeSpan() : stopwatch.Elapsed;
        public bool IsGameStarted { get; private set; }
        public bool IsAttached { get; private set; }
        public GameSettings Settings { get; set; }
        
        public IList<PlayerInfo> PlayersInfo => infoDictionary.Values.ToList();
        public Player StartPlayer { get; private set; }
        public Player CurrentPlayer { get; private set; }
        public Player NextPlayer => CurrentPlayer.NextPlayer;
        public Player FrontPlayer => CurrentPlayer.FrontPlayer;
        public Player LastPlayer { get; private set; }
        public Player Winner { get; private set; }

        #endregion

        #region --------Event--------

        public event EventHandler GameStarting;
        public event EventHandler<GameEndedEventArgs> GameEnded;
        public event EventHandler<GameEventArgs> GameDataUpdated;
        public event EventHandler<GameEventArgs> ActivePlayerChanged;

        #endregion

        #region --------Constructor--------

        public GameService()
        {
            Debug.LogLine("GameMachines invoke!");
            DataLogger = new SpotDataLogger(MapStruct.DefaultEntry);
            Settings = new GameSettings();
        }

        #endregion

        #region --------Private Method--------

        /// <summary>
        /// Try to active a player as current player
        /// </summary>
        /// <param name="player">New activing player</param>
        private void TryActivePlayer(Player player)
        {
            if (player != null && CurrentPlayer != player)
            {
                //Ensure old current player's stopwatch start
                if (CurrentPlayer != null)
                    PauseStopwatch(ref infoDictionary[CurrentPlayer.TokenID].stopwatch);
                //Set player as current actived
                CurrentPlayer = player;
                //Ensure new current player's stopwatch start
                EnsureStopwatch(ref infoDictionary[player.TokenID].stopwatch);
                //Raise active player changed event handler
                ActivePlayerChanged?.Invoke(this, new GameEventArgs(player));
                Debug.LogLine($"Active {CurrentPlayer.Name}");
            }
        }
        
        private bool HasNewWinner(Location location, int id)
        {
            var lines = MapStruct.XY3Lines.Where(xy3line => xy3line.IsInLine(location.X, location.Y));
            foreach(var line in lines)
            {
                if (DataLogger.CountOccupiedOf(id, line.ToLocations()) == 3)
                {
                    return true;
                }
            }
            return false;
        }

        private void ResetData()
        {
            Winner = null;
            ResetTotalStopwatch();
            DataLogger.Reset();
            ClearEachStopwatch();
        }

        #region --------Stopwatch--------

        private void EnsureStopwatch(ref System.Diagnostics.Stopwatch stopwatch)
        {
            if (stopwatch == null)
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
            stopwatch.Start();
        }

        private void PauseStopwatch(ref System.Diagnostics.Stopwatch stopwatch)
        {
            if (stopwatch != null)
                stopwatch.Stop();
        }
        
        private void StopEachStopwatch()
        {
            if (infoDictionary == null) return;
            foreach (var info in infoDictionary)
            {
                if (info.Value.stopwatch != null)
                {
                    info.Value.stopwatch.Stop();
                }
            }
        }

        private void ClearEachStopwatch()
        {
            if (infoDictionary == null) return;
            foreach (var info in infoDictionary)
            {
                if (info.Value.stopwatch != null)
                {
                    info.Value.stopwatch.Stop();
                    info.Value.stopwatch = null;
                }
            }
        }

        private void ResetAllStopwatch()
        {
            if (infoDictionary == null) return;
            foreach (var info in infoDictionary)
            {
                if (info.Value.stopwatch != null)
                    info.Value.stopwatch.Reset();
            }
        }
        
        private void StartTotalStopwatch()
        {
            if (stopwatch == null)
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
            else stopwatch.Reset();
            stopwatch.Start();
        }
        private void StopTotalStopwatch()
        {
            if (stopwatch != null)
            {
                stopwatch.Stop();
            }
            StopEachStopwatch();
        }
        private void ResetTotalStopwatch()
        {
            if (stopwatch != null)
            {
                stopwatch.Reset();
            }
            ResetAllStopwatch();
        }

        #endregion

        #endregion

        #region --------Implement IGameMachines--------

        #region --------Player Operation--------

        public bool JoinPlayer(Player player)
        {
            if (player == null) return false;
            if (!IsAttached) Attach();
            if (!playersDictionary.ContainsValue(player))
            {
                if (playersDictionary.Count >= Settings.PlayerLimits)
                {
                    return false;
                }
                else
                {
                    player.TokenID = playersDictionary.Count + 2;
                    if(playersDictionary.Count > 0)
                    {
                        var firstPlayer = playersDictionary.First().Value;
                        var lastPlayer = playersDictionary.Last().Value;
                        lastPlayer.NextPlayer = player;
                        player.FrontPlayer = lastPlayer;
                        player.NextPlayer = firstPlayer;
                    }
                    playersDictionary.Add(player.TokenID, player);
                    infoDictionary.Add(player.TokenID, new PlayerInfo(player));
                    Debug.LogLine($"Player '{player.Name}' (TokenID : {player.TokenID}) joined.");
                }
            }
            player.RegisterService(this);//Ensure register services
            return true;
        }

        public void LeavePlayer(Player player)
        {
            if (player == null || playersDictionary.Count < 1) return;
            if (playersDictionary.ContainsValue(player))
            {
                int tokenid = player.TokenID;
                player.TokenID = -1;
                playersDictionary.Remove(tokenid);
                infoDictionary.Remove(tokenid);

                var frontPlayer = player.FrontPlayer.NextPlayer;

                player.FrontPlayer.NextPlayer = player.NextPlayer;
                if (CurrentPlayer == player) TryActivePlayer(player.NextPlayer);
                Debug.LogLine($"Player '{player.Name}' leave.");
            }
            else
            {
                Debug.LogLine($"Player '{player.Name}' leave fail.");
            }
        }

        public void SetFirstPlayer(Player player)
        {
            if (player == null || playersDictionary.Count < 1 ||!playersDictionary.ContainsValue(player)) return;
            StartPlayer = player;
            Debug.LogLine($"Player '{player.Name}' is set to first.");
        }

        public PlayerInfo GetInfoOf(Player player)
        {
            int tokenid = player.TokenID;
            if (infoDictionary.ContainsKey(tokenid)) return infoDictionary[tokenid];
            return null;
        }

        #endregion

        /// <summary>
        /// Attach game service.
        /// Game service will init data
        /// </summary>
        public void Attach()
        {
            if (IsAttached) return;
            Detach(true);//Ensure detach old
            //Initilize all data logger
            playersDictionary = new Dictionary<int, Player>();
            infoDictionary = new Dictionary<int, PlayerInfo>();
            Debug.LogLine("Attaching game services.");
            IsAttached = true;
        }

        /// <summary>
        /// Detach game service.
        /// Game service will clear data.
        /// </summary>
        public void Detach(bool internalInvoke = false )
        {
            if (IsGameStarted)
            {
                EndGame();
            }
            ResetData();
            if (playersDictionary != null && playersDictionary.Count > 0)
            {
                foreach (var playerValue in playersDictionary)
                {
                    playerValue.Value.RegisterService(null);
                    playerValue.Value.TokenID = -1;
                }
                playersDictionary.Clear();
            }
            if (!internalInvoke) CurrentMode = GameMode.NoSpecail;
            IsAttached = false;
            Debug.LogLine("Detached game services.");
        }

        /// <summary>
        /// Start a new game
        /// </summary>
        /// <returns></returns>
        public bool StartNewGame()
        {
            Debug.LogLine("Try StartNewGame.");
            if (IsAttached && !IsGameStarted && CurrentMode != GameMode.NoSpecail)
            {
                if (playersDictionary.Count < 2) return false;
                ResetData();
                IsGameStarted = true;
                GameStarting?.Invoke(this, EventArgs.Empty);
                StartTotalStopwatch();
                TryActivePlayer(StartPlayer);
                Debug.LogLine("Game is started.");
                return true;
            }
            return false;
        }

        public void EndGame()
        {
            if (IsGameStarted)
            {
                IsGameStarted = false;
                CurrentPlayer = null;
                StopTotalStopwatch();
                GameEnded?.Invoke(this, new GameEndedEventArgs(true));
                Debug.LogLine("Game is ended.");
            }
        }
        
        private void EndGameWithArgs(GameEndedEventArgs args)
        {
            if (IsGameStarted)
            {
                IsGameStarted = false;
                CurrentPlayer = null;
                StopTotalStopwatch();
                GameEnded?.Invoke(this, args);
                Debug.LogLine("Game is ended.");
            }
        }
        
        public bool HandInput(Player player, Location location, ActionType type)
        {
            //Return while player is not active or can't input new data
            if (!IsGameStarted || player == null || playersDictionary.Count < 1) return false;
            if (CurrentPlayer == null || CurrentPlayer != player || DataLogger.IsFill) return false;
            //Hand data by check action type and invoke method
            switch (type)
            {
                case ActionType.New: return HandNew(player, location);
                case ActionType.Undo: return BackTo(player, location);
                case ActionType.Redo: return ForwardTo(player, location);
                default:return false;
            }
        }

        //
        private bool VerifyFirst(Player player, Location location)
        {
            if(DataLogger.CountOf(player.TokenID) == 0)
            {
                if (location.X == 0 && location.Y == 0) return false;
            }
            return true;
        }

        private bool HandNew(Player player, Location location)
        {
            //Check game started or can't input new data
            if (!IsGameStarted || DataLogger.IsFill) return false;
            //Check player
            if (player == null || CurrentPlayer == null || CurrentPlayer != player) return false;
            //Check if can player input
            if (DataLogger.CountOf(player.TokenID) >= Settings.MaxStep)
            {
                Debug.LogLine($"Locations overflow, player : '{player.Name}'.");
                return false;
            }
            //Verify data
            if (!DataLogger.CanLogAt(location))
            {
                Debug.LogLine($"'{player.Name}' 's input Validate fail {location.X}, {location.Y}.");
                return false;
            }
            //Get current player info
            LastPlayer = player;
            DataLogger.Log(location, player.TokenID);
            Debug.LogLine($"'{player.Name}' inputed {location.X}, {location.Y}.");
            //Raise EventHandler
            GameDataUpdated?.Invoke(this, new GameEventArgs(GameEventType.NewInput, player, new Spot(location.X, location.Y, player.TokenID)));
            //Check winner & Raise eventhandler
            if (HasNewWinner(location, player.TokenID))
            {
                Debug.LogLine($"Winner append : '{ player.Name}'.");
                Winner = player;
                UpdateScore();
                this.EndGameWithArgs(new GameEndedEventArgs(player));
            }
            else if (DataLogger.IsFill)
            {
                Debug.LogLine($"ReachableCount zero : '{player.Name}'.");
                UpdateScore();
                this.EndGameWithArgs(new GameEndedEventArgs());
            }
            else
            {
                this.TryActivePlayer(player.NextPlayer);
            }
            return true;
        }
        private bool BackTo(Player player, Location location)
        {
            //Check location exist in undo stack
            //TODO
            if (!IsGameStarted || playersDictionary.Count < 1) return false;
            if (player.IsAI) return false;//Disable AI go back
            Debug.LogLine($"*****{player.Name} request withdraw.*****");
            bool isTargetFound = false;
            Spot topUndoSpot;
            while (DataLogger.GetUndoTop(out topUndoSpot) && !isTargetFound)
            {
                Player pointPlayer;
                if(topUndoSpot.Data == player.TokenID)
                {
                    pointPlayer = player;
                    if (topUndoSpot.X == location.X && topUndoSpot.Y == location.Y)
                    {
                        isTargetFound = true;
                    }
                    this.DataLogger.Undo();
                    Debug.LogLine($"{pointPlayer.Name} revoke location -> {topUndoSpot.X }, {topUndoSpot.Y}.");
                }
                else
                {
                    //Apply for other player's agress
                    pointPlayer = playersDictionary.First(item => { return item.Key == topUndoSpot.Data; }).Value;
                    //TODO Hand receive message to ensure is undo enable
                    if (pointPlayer.HandRequest(RequestType.SelfUndo) != RequestResult.OK)
                    {
                        Debug.LogLine($"Undo fail, player '{pointPlayer.Name}' not allow");
                        return false;
                    }
                    this.DataLogger.Undo();
                    Debug.LogLine($"{pointPlayer.Name} allow revoke location -> {topUndoSpot.X }, {topUndoSpot.Y}.");
                }
                GameDataUpdated?.Invoke(this, new GameEventArgs(GameEventType.Withdraw,pointPlayer, topUndoSpot));
            }
            return isTargetFound;
        }

        #endregion

        #region --------Sql operate--------

        private void UpdateScore()
        {
            if(CurrentMode == GameMode.PVE || CurrentMode == GameMode.Online)
            {
                int winscore = 1, drawnscore = 0, failscore = -1;
                var builder = new System.Text.StringBuilder();
                var sqliteService = SqliteService.GetService();
                foreach (var playerPair in playersDictionary)
                {
                    var player = playerPair.Value;
                    if (player.IsAI) continue;
                    string cmdString = string.Empty;
                    if (Winner == null)
                    {
                        sqliteService.UpdateScore(player.Score.ScoreID, drawnscore, GameResult.Drawn);
                    }
                    else if (Winner == player)
                    {
                        sqliteService.UpdateScore(player.Score.ScoreID, winscore, GameResult.Win);
                    }
                    else
                    {
                        sqliteService.UpdateScore(player.Score.ScoreID, failscore, GameResult.Fail);
                    }
                    player.Score = sqliteService.GetScoreOfUser(player.Score.ScoreID);
                }
                /* Use online database version
                var sqlserverService = SqlServerService.GetService();
                if (sqlserverService.HasConnected)
                    sqlserverService.TrySync();
                    */
            }
        }

        #endregion

        #region --------Recycling--------

        private int GetIndexOf(Dictionary<int, Player> dictionary, Player player)
        {
            for (int index = 0; index < dictionary.Count; ++index)
            {
                if (dictionary.Skip(index).First().Value == player)
                {
                    return index;
                }
            }
            return -1;
        }
        private bool ForwardTo(Player player, Location location)
        {
            //Check location exist in redo stack
            //TODO
            if (!IsGameStarted || playersDictionary.Count < 1) return false;
            Debug.LogLine($"*****{player.Name} request redo.*****");
            bool isTargetFound = false;
            Spot topRedoSpot;
            while (DataLogger.GetRedoTop(out topRedoSpot) && !isTargetFound)
            {
                if (topRedoSpot.X == location.X && topRedoSpot.Y == location.Y)
                {
                    isTargetFound = true;
                }
                Player pointPlayer;
                if (topRedoSpot.Data == player.TokenID)
                {
                    this.DataLogger.Redo();
                    pointPlayer = player;
                    Debug.LogLine($"{pointPlayer.Name} relog location -> {topRedoSpot.X }, {topRedoSpot.Y}.");
                }
                else
                {
                    //Ask for other player's agress
                    pointPlayer = playersDictionary.First(item => { return item.Key == topRedoSpot.Data; }).Value;

                    if (pointPlayer.HandRequest(RequestType.SelfRedo) != RequestResult.OK)
                    {
                        Debug.LogLine($"Redo fail, player '{pointPlayer.Name}' not allow");
                        return false;
                    }
                    this.DataLogger.Redo();
                    Debug.LogLine($"{pointPlayer.Name} allow relog location -> {topRedoSpot.X }, {topRedoSpot.Y}.");
                }
                GameDataUpdated?.Invoke(this, new GameEventArgs(GameEventType.Redo, pointPlayer, topRedoSpot));
            }
            return isTargetFound;
        }

        #endregion
    }
}
