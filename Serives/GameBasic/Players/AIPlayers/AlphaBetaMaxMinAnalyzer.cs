using System;
using System.Collections.Generic;
using System.Linq;

namespace GameServices.GameBasic
{
    /// <summary>
    /// Alpha-beta cut max-min search algorinthm analyzer
    /// </summary>
    internal class AlphaBetaMaxMinAnalyzer
    {
        int denied = 0;
        int avaliable = 1;
        int minValue = -GlobalLevel.SSS * 4;
        int maxValue = GlobalLevel.SSS * 4;
        int humanID;
        int AIID;

        public AlphaBetaMaxMinAnalyzer(int aiID, int humanID, int denied, int avaliable)
        {
            this.AIID = aiID;
            this.humanID = humanID;
            this.denied = denied;
            this.avaliable = avaliable;
        }

        public Location FindMaxMin(int[,] board, int deep)
        {
            var best = minValue;
            var bestLocations = new List<Location>();
            var locations = GenerateAvaliable(ref board, deep);
            //Init scores
            var scores = new int[board.GetLength(0), board.GetLength(1)];
            FullEvaluate(ref board, ref scores);
            //Start maxValueminValue
            for (int i = 0; i < locations.Count; ++i)
            {
                var loc = locations[i];
                board[loc.X, loc.Y] = AIID;
                int[,] scoresCopy = ArrayHelper.CopyMatrix(ref scores);
                EvaluateLocation(ref board, ref scoresCopy, loc.X, loc.Y);
                var v = Min(ref board, ref scoresCopy, deep - 1, (best > minValue ? best : minValue), maxValue);
                if (v == best) bestLocations.Add(loc);
                if (v > best)
                {
                    best = v;
                    bestLocations.Clear();
                    bestLocations.Add(loc);
                }
                board[loc.X, loc.Y] = avaliable;
            }
            //If best is not found, get best from scores
            if (bestLocations.Count < 1)
            {
                for(int i = 0; i < scores.GetLength(0); ++i)
                {
                    for (int j = 0; j < scores.GetLength(0); ++j)
                    {
                        if(board[i,j] == avaliable)
                        {
                            if (scores[i, j] == best) bestLocations.Add(new Location(i, j));
                            if (scores[i, j] > best)
                            {
                                best = scores[i, j];
                                bestLocations.Clear();
                                bestLocations.Add(new Location(i, j));
                            }
                        }
                    }
                }
            }
            int index = (new Random()).Next(0, bestLocations.Count);
            return bestLocations[index];
        }

        private int Max(ref int[,] board, ref int[,] scores, int deep, int alpha, int beta)
        {
            var v0 = EvaluateScores(ref scores);
            if (deep <= 0 || Ended(board)) return v0;
            var best = minValue;
            var locations = GenerateAvaliable(ref board, deep);
            for (int i = 0; i < locations.Count; ++i)
            {
                var loc = locations[i];
                board[loc.X, loc.Y] = AIID;
                int[,] scoresCopy = ArrayHelper.CopyMatrix(ref scores);
                EvaluateLocation(ref board, ref scoresCopy, loc.X, loc.Y);
                var v = Min(ref board, ref scoresCopy, deep - 1, alpha, (best > beta ? best : beta));
                board[loc.X, loc.Y] = avaliable;
                if (v > best) best = v;
                if (v > alpha) break;//ABcut++;
            }
            return best;
        }

        private int Min(ref int[,] board, ref int[,] scores, int deep, int alpha, int beta)
        {
            var v0 = EvaluateScores(ref scores);
            if (deep <= 0 || Ended(board)) return v0;
            var best = maxValue;
            var locations = GenerateAvaliable(ref board, deep);
            for (int i = 0; i < locations.Count; ++i)
            {
                var loc = locations[i];
                board[loc.X, loc.Y] = humanID;

                //Every time to copy scores,
                //so that evaluate next location can evaluate with origin score.
                //Copy scores and Evaluate location
                int[,] scoresCopy = ArrayHelper.CopyMatrix(ref scores);
                EvaluateLocation(ref board, ref scoresCopy, loc.X, loc.Y);
                var v = Max(ref board, ref scoresCopy, deep - 1, (best < alpha ? best : alpha), beta);
                board[loc.X, loc.Y] = avaliable;
                if (v < best) best = v;
                if (v < beta) break;//Abcut++;
            }
            return best;
        }

        /// <summary>
        /// Calculate total score of exist scores matrix
        /// </summary>
        /// <param name="scores"></param>
        /// <returns></returns>
        private int EvaluateScores(ref int[,] scores)
        {
            int value = 0;
            for (int i = 0; i < scores.GetLength(0); ++i)
            {
                for (int j = 0; j < scores.GetLength(1); ++j)
                {
                    value += scores[i, j];
                }
            }
            return value;
        }

        /// <summary>
        /// Full evaluate board
        /// </summary>
        /// <param name="board"></param>
        /// <param name="scores"></param>
        private void FullEvaluate(ref int[,] board, ref int[,] scores)
        {
            for (int i = 0; i < board.GetLength(0); ++i)
            {
                for (int j = 0; j < board.GetLength(0); ++j)
                {
                    if (board[i, j] != avaliable && board[i, j] != denied)
                    {
                        int lineCount;
                        EvaluateLocation(ref board, ref scores, out lineCount, i, j);
                        scores[i, j] += lineCount;
                    }
                }
            }
        }

        /// <summary>
        /// Evaluate special location
        /// </summary>
        /// <param name="board">Origin board</param>
        /// <param name="scores">Current scores</param>
        /// <param name="lineCount">Line's count</param>
        /// <param name="x">Location's x</param>
        /// <param name="y">Location's y</param>
        private void EvaluateLocation(ref int[,] board, ref int[,] scores, out int lineCount, int x, int y)
        {
            scores[x, y] = GetLocationScore(ref board,  out lineCount, x, y);
        }

        /// <summary>
        /// Evaluate special location
        /// </summary>
        /// <param name="board">Origin board</param>
        /// <param name="scores">Current scores</param>
        /// <param name="x">Location's x</param>
        /// <param name="y">Location's y</param>
        private void EvaluateLocation(ref int[,] board, ref int[,] scores, int x, int y)
        {
            int lineCount;
            scores[x, y] = GetLocationScore(ref board, out lineCount, x, y);
        }

        /// <summary>
        /// Evaluate location's score
        /// </summary>
        /// <param name="board">Origin board</param>
        /// <param name="lineCount"></param>
        /// <param name="x">Location's x</param>
        /// <param name="y">Location's y</param>
        /// <returns></returns>
        private int GetLocationScore(ref int[,] board, out int lineCount, int x, int y)
        {
            lineCount = 0;
            var lines = MapStruct.GetMapStruct().XY3Lines.Where(xy3line => xy3line.IsInLine(x, y));
            int totalScore = 0, sseCount = 0, ooeCount = 0;
            foreach (var line in lines)
            {
                ++lineCount;

                int reachable = 0, selfOccupy = 0, otherOccupy = 0;
                int score = 0;

                var locations = line.ToLocations();
                foreach (var location in locations)
                {
                    if (board[location.X, location.Y] == avaliable) ++reachable;
                    else
                    {
                        if (board[location.X, location.Y] == AIID) ++selfOccupy;
                        else ++otherOccupy;
                    }
                }

                if (reachable == 3) score = GlobalLevel.EEE;
                if (reachable == 0)
                {
                    if (selfOccupy == 0) score = GlobalLevel.OOO;
                    return GlobalLevel.SSS;
                }

                switch (selfOccupy)
                {
                    case 0:
                        if (otherOccupy == 2) ++ooeCount;
                        if (otherOccupy == 1) score = GlobalLevel.OEE;
                        break;
                    case 1:
                        if (otherOccupy == 1) score = GlobalLevel.SOE;
                        if (otherOccupy == 0) score = GlobalLevel.SEE;
                        break;
                    case 2:
                        ++sseCount;
                        break;
                    default: break;
                }
                totalScore += score;
            }

            if (sseCount > 1) totalScore += GlobalLevel.DoubleSSE * (sseCount * (sseCount + 1) / 2);
            else totalScore += GlobalLevel.SSE;

            if (ooeCount > 1) totalScore += GlobalLevel.DoubleOOE * (ooeCount * (ooeCount + 1) / 2);
            else totalScore += GlobalLevel.OOE;

            return totalScore;
        }

        /// <summary>
        /// Check if board is end.
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        private bool Ended(int[,] board)
        {
            int avaliable = 0;
            foreach (var line in MapStruct.GetMapStruct().XY3Lines)
            {
                var locs = line.ToLocations();
                int humanOccupy = 0, aiOccupy = 0;
                foreach (var loc in locs)
                {
                    if (board[loc.X, loc.Y] == humanID) ++humanOccupy;
                    else if (board[loc.X, loc.Y] == AIID) ++aiOccupy;
                    else if (board[loc.X, loc.Y] == avaliable) ++avaliable;
                }
                if (humanOccupy == 3 || aiOccupy == 3) return true;
            }
            if (avaliable == 0) return true;
            return false;
        }

        /// <summary>
        /// Check if board will append winner
        /// </summary>
        /// <param name="board"></param>
        /// <param name="location"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool HasNewWinner(ref int[,] board, Location location, int id)
        {
            var lines = MapStruct.GetMapStruct().XY3Lines.Where(xy3line => xy3line.IsInLine(location.X, location.Y));
            foreach (var line in lines)
            {
                int count = 0;
                var locations = line.ToLocations();
                foreach (var loc in locations)
                {
                    if (board[loc.X, loc.Y] == id) ++count;
                }
                if (count == 3) return true;
            }
            return false;
        }

        /// <summary>
        /// Generate avaliable location for current board
        /// </summary>
        /// <param name="board"></param>
        /// <param name="deep"></param>
        /// <returns></returns>
        private IList<Location> GenerateAvaliable(ref int[,] board, int deep)
        {
            List<Location> three = new List<Location>();
            List<Location> doubleTwo = new List<Location>();
            List<Location> twos = new List<Location>();
            List<Location> remainAvaliables = new List<Location>();
            for (int i = 0; i < board.GetLength(0); ++i)
            {
                for (int j = 0; j < board.GetLength(1); ++j)
                {
                    if (board[i, j] == avaliable)
                    {
                        var scoreCom = EvaluateIfInput(ref board, i, j, AIID);
                        if (scoreCom == 0)
                        {
                            //If current score equal to zero
                            //Means current location don't have chess around it.
                            //Skip it.
                            continue;
                        }
                        var scoreHum = EvaluateIfInput(ref board, i, j, humanID);
                        Location loc = new Location(i, j);
                        if (scoreCom >= OneSidedLevel.SSS)
                            return new Location[] { loc };
                        else if (scoreHum >= OneSidedLevel.SSS)
                            three.Add(loc);
                        else if (scoreCom >= 2 * OneSidedLevel.SSE)
                            doubleTwo.Insert(0, loc);
                        else if (scoreHum >= 2 * OneSidedLevel.SSE)
                            doubleTwo.Add(loc);
                        else if (scoreCom >= OneSidedLevel.SSE)
                            twos.Insert(0, loc);
                        else if (scoreHum >= OneSidedLevel.SSE)
                            twos.Add(loc);
                        else
                            remainAvaliables.Add(loc);
                    }
                }
            }
            if (three.Count > 0) return three;
            if (doubleTwo.Count > 0) return doubleTwo;
            if (twos.Count > 0) return twos;
            return remainAvaliables;
        }

        /// <summary>
        /// Evaluate location's score after input there
        /// </summary>
        /// <param name="board"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        private int EvaluateIfInput(ref int[,] board, int x, int y, int id)
        {
            int score = 0;
            var xy3LineList = MapStruct.GetMapStruct().XY3Lines.Where(xy3Line => xy3Line.IsInLine(x, y));
            foreach (var xy3Line in xy3LineList)
            {
                var locations = xy3Line.ToLocations();
                int reachable = 0, selfOccupy = 0, otherOccupy = 0;
                foreach (var location in locations)
                {
                    int value = board[location.X, location.Y];
                    if (value == avaliable) ++reachable;
                    else
                    {
                        if (value == id) ++selfOccupy;
                        else ++otherOccupy;
                    }
                }
                if (selfOccupy == 2 && otherOccupy == 0) score += OneSidedLevel.SSS;
                else if (selfOccupy == 1 && otherOccupy == 0) score += OneSidedLevel.SSE;
                else if (reachable != 3) score += OneSidedLevel.SEE;
            }
            return score;
        }

        #region Recycling, don't care

        //
        private bool IsEmptyOf(ref int[,] board, XY3Line line)
        {
            return board[line.X1, line.Y1] == avaliable
                && board[line.X2, line.Y2] == avaliable
                && board[line.X3, line.Y3] == avaliable;
        }

        //Old Evaluate
        private int Evaluate(int[,] board)
        {
            int value = 0;
            for (int i = 0; i < board.GetLength(0); ++i)
            {
                for (int j = 0; j < board.GetLength(1); ++j)
                {
                    if (board[i, j] != avaliable && board[i, j] != denied)
                    {
                        value += EvaluateLocationOf(ref board, i, j);
                    }
                }
            }
            return value;
        }
        private int EvaluateLocationOf(ref int[,] board, int x, int y)
        {
            var lines = MapStruct.GetMapStruct().XY3Lines.Where(xy3line => xy3line.IsInLine(x, y));
            int totalScore = 0;
            int sseCount = 0, ooeCount = 0;
            foreach (var line in lines)
            {
                int reachable = 0, selfOccupy = 0, otherOccupy = 0;
                int score = 0;

                var locations = line.ToLocations();
                foreach (var location in locations)
                {
                    if (board[location.X, location.Y] == avaliable) ++reachable;
                    else
                    {
                        if (board[location.X, location.Y] == AIID) ++selfOccupy;
                        else ++otherOccupy;
                    }
                }

                if (reachable == 3) score = GlobalLevel.EEE;
                if (reachable == 0)
                {
                    if (selfOccupy == 0) score = GlobalLevel.OOO;
                    return GlobalLevel.SSS;
                }

                switch (selfOccupy)
                {
                    case 0:
                        if (otherOccupy == 2) ++ooeCount;
                        if (otherOccupy == 1) score = GlobalLevel.OEE;
                        break;
                    case 1:
                        if (otherOccupy == 1) score = GlobalLevel.SOE;
                        if (otherOccupy == 0) score = GlobalLevel.SEE;
                        break;
                    case 2:
                        ++sseCount;
                        break;
                    default: break;
                }
                totalScore += score;
            }
            if (sseCount > 1) totalScore += GlobalLevel.DoubleSSE * (sseCount * (sseCount + 1) / 2);
            else totalScore += GlobalLevel.SSE;
            if (ooeCount > 1)
            {
                totalScore += GlobalLevel.DoubleOOE * (ooeCount * (ooeCount + 1) / 2);
            }
            else totalScore += GlobalLevel.OOE;
            return totalScore;
        }

        //Recycling
        private IList<Location> GenerateAvaliableOld(int[,] board, int deep)
        {
            List<Location> avaliables = new List<Location>();
            for (int i = 0; i < board.GetLength(0); ++i)
            {
                for (int j = 0; j < board.GetLength(1); ++j)
                {
                    if (board[i, j] == avaliable)
                    {
                        var xy3LineList = MapStruct.GetMapStruct().XY3Lines.Where(xy3Line => xy3Line.IsInLine(i, j));
                        foreach (var line in xy3LineList)
                        {
                            if (!IsEmptyOf(ref board, line))
                            {
                                avaliables.Add(new Location(i, j));
                                break;
                            }
                        }
                    }
                }
            }
            return avaliables;
        }

        #endregion

        class GlobalLevel
        {
            public const int SSS = 12000;
            public const int DoubleSSE = 2500;
            public const int SSE = 500;
            public const int SEE = 100;
            public const int EEE = 0;
            public const int SOE = 0;
            public const int OEE = -100;
            public const int OOE = -500;
            public const int DoubleOOE = -2500;
            public const int OOO = -12000;
        }

        class OneSidedLevel
        {
            public const int SSS = 10000;
            public const int SSE = 100;
            public const int SEE = 1;
        }
    }
}
