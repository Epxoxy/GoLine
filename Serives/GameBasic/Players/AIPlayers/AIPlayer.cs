using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GameServices.GameBasic
{
    
    public class RandomAIPlayer : Player
    {
        internal RandomAIPlayer() : this("")
        {
        }

        internal RandomAIPlayer(string name) : base(name)
        {
            IsAI = true;
        }

        //Allow all request in AI player
        public override RequestResult HandRequest(RequestType type)
        {
            return RequestResult.OK;
        }

        internal override void OnServicesProviderChanging(IGameService oldService, IGameService newService)
        {
            if (oldService != null)
            {
                oldService.ActivePlayerChanged -= OnAcitvePlayerChanged;
            }
            if (newService != null)
            {
                newService.ActivePlayerChanged += OnAcitvePlayerChanged;
            }
        }

        private async void OnAcitvePlayerChanged(object sender, GameEventArgs e)
        {
            if (IsMyTurn)
            {
                bool inputResult = await OnCallInput();
                if (inputResult == false) Debug.LogLine("AI input fail");
            }
        }
        
        internal virtual async Task<bool> OnCallInput()
        {
            if (ServicesProvider == null) return false;
            //Watch time
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Location result = null;
            await Task.Run(() =>
            {
                var availableLocations = ServicesProvider.DataLogger.GetAvailableLocation();
                if (availableLocations.Count > 0)
                {
                    int randomIndex = new Random().Next(0, availableLocations.Count - 1);
                    result = availableLocations[randomIndex];
                }
            });
            if (result == null)
            {
                Debug.LogLine($"AI '{this.Name}' TryRandomInput fail");
                return false;
            }
            var delayTime = Convert.ToInt32(500 - sw.ElapsedMilliseconds);
            if (delayTime > 0) await Task.Delay(delayTime);
            this.Input(result);
            return true;
        }
    }
    
    public class ScoreAIPlayer : RandomAIPlayer
    {
        internal ScoreAIPlayer()
        {
        }

        internal ScoreAIPlayer(string name) : base(name)
        {
        }

        internal override void OnServicesProviderChanging(IGameService oldService, IGameService newService)
        {
            base.OnServicesProviderChanging(oldService, newService);
            if (oldService != null)
            {
                oldService.GameDataUpdated -= OnGameDataUpdated;
                oldService.GameStarting -= OnGameStarting;
            }
            if (newService != null)
            {
                newService.GameDataUpdated += OnGameDataUpdated;
                newService.GameStarting += OnGameStarting;
            }
        }

        private void OnGameStarting(object sender, EventArgs e)
        {
            usingScores = null;
        }

        private void OnGameDataUpdated(object sender, GameEventArgs e)
        {
            if (e.Type == GameEventType.NewInput)
            {
                var spot = e.Spot;
                UpdateAround(spot.X, spot.Y);
            }
            else if (e.Type == GameEventType.Withdraw)
            {
                RecoveryLast();
                Debug.LogLine($"RecoveryLast");
                OutPutScores();
            }
        }

        internal override async Task<bool> OnCallInput()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int i, j;
            bool highestFound = TryFindHighest(out i, out j);
            sw.Stop();
            var delayTime = Convert.ToInt32(500 - sw.ElapsedMilliseconds);
            if (delayTime > 0) await Task.Delay(delayTime);
            if (highestFound)
            {
                this.Input(new Location(j, i));
                Debug.LogLine($"AI '{this.Name}' found highest {j}, {i}");
            }
            else
            {
                return await base.OnCallInput();
            }
            return true;
        }

        public bool TryFindHighest(out int column, out int row)
        {
            int highestScore = 0, highestCount = 0;
            for (int i = 0; i < Scores.GetLength(0); ++i)
            {
                for (int j = 0; j < Scores.GetLength(1); ++j)
                {
                    if (highestScore < Scores[i, j])
                    {
                        highestScore = Scores[i, j];
                        highestCount = 0;
                    }
                    if (highestScore == Scores[i, j])
                    {
                        ++highestCount;
                    }
                }
            }
            if (highestCount > 0 && highestScore > 0)
            {
                Debug.LogLine($"Found highest score is {highestScore}");
                int findIndex = (new Random()).Next(0, highestCount - 1);
                for (int i = 0, index = 0; i < Scores.GetLength(0); ++i)
                {
                    for (int j = 0; j < Scores.GetLength(1); ++j)
                    {
                        if (Scores[i, j] == highestScore)
                        {
                            if (index == findIndex)
                            {
                                Debug.LogLine($"Index is {i}, {j}");
                                column = i;
                                row = j;
                                return true;
                            }
                            ++index;
                        }
                    }
                }
            }
            Debug.LogLine($"Found fail {highestScore}, {highestCount}");
            column = -1; row = -1;
            return false;
        }

        #region Update Score

        internal void UpdateAround(int x, int y)
        {
            AddHistory(Scores);
            var xy3LineList = MapStruct.GetMapStruct().XY3Lines.Where(xy3Line => xy3Line.IsInLine(x, y));
            foreach (var xy3Line in xy3LineList)
            {
                UpdateLineScore(xy3Line, TokenID);
            }
            Debug.LogLine($"Update Around {x}, {y}");
            OutPutScores();
        }

        private void UpdateLineScore(XY3Line xy3Line, int id)
        {
            if (xy3Line == null) return;
            SpotDataLogger datalogger = ServicesProvider.DataLogger;
            if (datalogger == null) return;
            var locations = xy3Line.ToLocations();
            int reachable = 0, selfOccupy = 0, otherOccupy = 0;
            foreach (var location in locations)
            {
                if (datalogger.CanLogAt(location)) ++reachable;
                else
                {
                    if (datalogger.IsOccupiedBy(location, id)) ++selfOccupy;
                    else ++otherOccupy;
                    //TODO make unreachable step's score to zero
                    Scores[location.Y, location.X] = 0;
                }
            }
            //COMMENT DEFINE => ('POS(condition)[num] : possibilities')possibilities of condition is num
            //COMMENT DEFINE => ('COMB<params>')combination of params 
            //POS(Total)[3*3*3] = 27, POS(reachable=3)[1], POS(reachable=0)[2*2*2]=8
            if (reachable == 3 || reachable == 0) return;

            //POS(otherOccupy + selfOccupy)[2] : 1or2
            for (int i = 0; i < locations.Length; ++i)
            {
                if (datalogger.CanLogAt(locations[i]))
                {
                    int score = 0;
                    switch (selfOccupy)
                    {
                        case 0:
                            //POS(COMB<other-other-empty>)[3]
                            if (otherOccupy == 2) score = Level.OOE_LEVEL2;//  High level
                            //POS(COMB<other-empty-empty>)[3]
                            if (otherOccupy == 1) score = Level.OEE_LEVEL5;//
                            break;
                        case 1:
                            //POS(COMB<other-self-empty>)[6]
                            if (otherOccupy == 1) score = Level.SOE_LEVEL6;//
                            //POS(COMB<self-empty-empty>)[3]
                            if (otherOccupy == 0) score = Level.SEE_LEVEL3;//
                            break;
                        case 2:
                            //POS(COMB<self-self-empty>)[3]
                            //TODO highest
                            score = Level.SSE_LEVEL1;
                            break;
                        default: break;
                    }
                    Scores[locations[i].Y, locations[i].X] += score;
                }
            }
        }

        private void AddHistory(int[,] newest)
        {
            int[,] array = new int[newest.GetLength(0), newest.GetLength(1)];
            for (int i = 0; i < newest.GetLength(0); ++i)
                for (int j = 0; j < newest.GetLength(1); ++j)
                    array[i, j] = newest[i, j];
            HistoryScore.Push(array);
        }

        private void RecoveryLast()
        {
            if (HistoryScore.Count < 1) return;
            Scores = HistoryScore.Pop();
        }

        private void OutPutScores()
        {
            Debug.LogLine($"******{this.Name}'s Scores******");
            for (int i = 0; i < Scores.GetLength(0); ++i)
            {
                for (int j = 0; j < Scores.GetLength(1); ++j)
                {
                    Debug.Log($"{Scores[i, j]:000} ");
                }
                Debug.LogLine(" ");
            }
            Debug.LogLine("*******End Scores********");
        }

        Stack<int[,]> HistoryScore = new Stack<int[,]>();
        int[,] scores = new int[,]
        {
            { 2,0,0,4,0,0,2 },
            { 0,0,2,2,2,0,0 },
            { 0,4,0,4,0,4,0 },
            { 2,2,3,0,3,2,2 },
            { 0,4,0,4,0,4,0 },
            { 0,0,2,2,2,0,0 },
            { 2,0,0,4,0,0,2 }
        };
        int[,] usingScores;
        int[,] Scores
        {
            get
            {
                if (usingScores == null)
                {
                    usingScores = new int[scores.GetLength(0), scores.GetLength(1)];
                    for (int i = 0; i < scores.GetLength(0); ++i)
                        for (int j = 0; j < scores.GetLength(1); ++j)
                            usingScores[i, j] = scores[i, j];
                }
                return usingScores;
            }
            set
            {
                usingScores = value;
            }
        }
        #endregion

        /// <summary>
        /// Level for score ai
        /// </summary>
        class Level
        {
            /// <summary>
            /// From(COMB|self-self-empty|),Target(COMB|self-self-self|)
            /// </summary>
            public const int SSE_LEVEL1 = 750;
            /// <summary>
            ///From(COMB|other-other-empty|),Target(COMB|self-other-other|)
            /// </summary>
            public const int OOE_LEVEL2 = 150;
            /// <summary>
            ///From(COMB|self-empty-empty|),Target(COMB|self-self-empty|)
            /// </summary>
            public const int SEE_LEVEL3 = 25;
            /// <summary>
            ///From(COMB|empty-empty-empty|),Target(COMB|self-empty-empty|)
            /// </summary>
            public const int EEE_LEVEL4 = 5;
            /// <summary>
            ///From(COMB|other-empty-empty|),Target(COMB|other-self-empty)
            /// </summary>
            public const int OEE_LEVEL5 = 1;
            /// <summary>
            ///From(COMB|self-other-empty|),Target(COMB|self-self-other|)
            /// </summary>
            public const int SOE_LEVEL6 = 0;
        }
    }

    public class ABCutAIPlayer : RandomAIPlayer
    {
        public int FindDeep { get; private set; } = 12;
        internal ABCutAIPlayer() : base()
        {
        }

        internal ABCutAIPlayer(string name) : base(name)
        {
        }
        
        internal override void OnServicesProviderChanging(IGameService oldService, IGameService newService)
        {
            base.OnServicesProviderChanging(oldService, newService);
            if (oldService != null)
            {
                oldService.GameStarting -= OnGameStarting;
            }
            if (newService != null)
            {
                newService.GameStarting += OnGameStarting;
            }
        }

        internal override Task<bool> OnCallInput()
        {
            return TryInput();
        }

        private void OnGameStarting(object sender, EventArgs e)
        {
            abmmAnalyzer = new AlphaBetaMaxMinAnalyzer(TokenID, NextPlayer.TokenID, SpotDataLogger.UNREACHABLE, SpotDataLogger.REACHABLE);
        }
        
        #region Search & Input

        AlphaBetaMaxMinAnalyzer abmmAnalyzer;
        private async Task<bool> TryInput()
        {
            if (ServicesProvider == null) return false;
            //Watch time
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Location result = null;
            await Task.Run(() =>
            {
                int[,] entry = ServicesProvider.DataLogger.EntryCopy;
                result = abmmAnalyzer.FindMaxMin(entry, FindDeep);
            });
            if(result != null)
            {
                int totalDelay = (new Random().Next(500, 1500));
                var delayTime = Convert.ToInt32(totalDelay - sw.ElapsedMilliseconds);
                if (delayTime > 0) await Task.Delay(delayTime);
                Debug.LogLine($"delayTime {delayTime}");
                if (this.Input(result) == false)
                {
                    return await base.OnCallInput();
                }
                return true;
            }
            else
            {
                return await base.OnCallInput();
            }
        }
        
        #endregion
    }
    

    public enum AIType
    {
        None,
        Elementary,
        Intermediate,
        Advanced
    }
}
