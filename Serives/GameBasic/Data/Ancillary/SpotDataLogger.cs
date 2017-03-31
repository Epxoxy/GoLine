using System;
using System.Collections.Generic;

namespace GameServices.GameBasic
{
    public class SpotDataLogger
    {
        public int ReachableCount { get; private set; }
        public bool IsFill => ReachableCount <= 0;
        internal const int UNREACHABLE = 0;
        internal const int REACHABLE = 1;
        private int[,] defaultEntry;
        private int[,] entry;
        private int[,] Entry
        {
            get
            {
                return entry;
            }
            set
            {
                if(entry != value)
                {
                    entry = value;
                    if (entry == null) return;
                    int reachableCount = 0;
                    for (int i = 0; i < entry.GetLength(0); ++i)
                    {
                        for (int j = 0; j < entry.GetLength(0); ++j)
                        {
                            if (entry[i, j] == REACHABLE) ++reachableCount;
                        }
                    }
                    if (ReachableCount != reachableCount)
                        ReachableCount = reachableCount;
                }
            }
        }
        public int[,] EntryCopy
        {
            get
            {
                if (Entry == null) return null;
                return ArrayHelper.CopyMatrix(Entry);
            }
        }
        
        public Dictionary<int, int> LogCountor { get; private set; }
        private Stack<Spot> UndoStack;
        private Stack<Spot> RedoStack;

        public SpotDataLogger(int[,] defaultEntry)
        {
            CustomContract.Requires<ArgumentException> (defaultEntry != null, 
                $"{nameof(defaultEntry)} of {nameof(SpotDataLogger)} can not be null");
            this.defaultEntry = defaultEntry;
            Initilize();
        }

        private void Initilize()
        {
            if (LogCountor != null && LogCountor.Count > 0) return;
            Entry = ArrayHelper.CopyMatrix(ref defaultEntry);
            LogCountor = new Dictionary<int, int>();
            UndoStack = new Stack<Spot>();
            RedoStack = new Stack<Spot>();
        }

        public bool IsInRange(int x, int y)
        {
            return x < Entry.GetLength(0) && y < Entry.GetLength(1);
        }
        public bool CanLogAt(int x, int y)
        {
            if (!IsInRange(x, y)) return false;
            return Entry[x, y] == REACHABLE;
        }
        public bool IsOccupiedBy(int x, int y, int id)
        {
            if (!IsInRange(x, y)) return false;
            return Entry[x, y] == id;
        }

        public bool IsInRange(Location location)
        {
            return IsInRange(location.X, location.Y);
        }
        public bool CanLogAt(Location location)
        {
            return CanLogAt(location.X, location.Y);
        }
        public bool IsOccupiedBy(Location location, int id)
        {
            return IsOccupiedBy(location.X, location.Y, id);
        }

        #region Log & RevokeLog

        private void RevokeOccupy(int x, int y)
        {
            if (!IsInRange(x, y) || Entry[x, y] == UNREACHABLE) return;
            if (Entry[x, y] == REACHABLE) return;
            int id = Entry[x, y];
            Entry[x, y] = REACHABLE;
            ++ReachableCount;
            --LogCountor[id];
        }
        private void SetOccupy(int x, int y, int id)
        {
            if (IsFill || !IsInRange(x, y)) return;
            if (Entry[x, y] == UNREACHABLE || Entry[x, y] == id) return;
            Entry[x, y] = id;
            --ReachableCount;
            if (!LogCountor.ContainsKey(id)) LogCountor.Add(id, 1);
            else ++LogCountor[id];
        }

        //Internal method
        internal int GetValuIn(int x, int y)
        {
            if (!IsInRange(x, y)) return -1;
            return Entry[x, y];
        }
        private void RevokeLog(Location location)
        {
            RevokeOccupy(location.X, location.Y);
            PrintEntry();
        }
        internal void Log(Location location, int id)
        {
            if (Entry == null) return;
            SetOccupy(location.X, location.Y, id);
            PushUndoStack(new Spot(location.X, location.Y, id));
            PrintEntry();
        }

        #endregion

        private void PrintEntry()
        {
            for (int i = 0; i < Entry.GetLength(0); ++i)
            {
                for (int j = 0; j < Entry.GetLength(0); ++j)
                {
                    System.Diagnostics.Debug.Write(Entry[i, j]);
                }
                System.Diagnostics.Debug.WriteLine("");
            }
        }

        #region Undo & Redo

        private void PushUndoStack(Spot spot)
        {
            UndoStack.Push(spot);
            RedoStack.Clear();
        }

        public bool CanUndo => UndoStack.Count > 0;
        public bool CanRedo => RedoStack.Count > 0;

        internal Spot Undo()
        {
            if (!CanUndo) return new Spot();
            Spot spot;
            spot = UndoStack.Pop();
            RevokeOccupy(spot.X, spot.Y);
            RedoStack.Push(spot);
            return spot;
        }
        internal Spot Redo()
        {
            if (!CanRedo) return new Spot();
            Spot spot = RedoStack.Pop();
            SetOccupy(spot.X, spot.Y, spot.Data);
            UndoStack.Push(spot);
            return spot;
        }

        internal bool GetRedoTop(out Spot topSpot)
        {
            if (CanRedo)
            {
                topSpot = RedoStack.Peek();
                return true;
            }
            topSpot = new Spot();
            return false;
        }
        internal bool GetUndoTop(out Spot topSpot)
        {
            if (CanUndo)
            {
                topSpot = UndoStack.Peek();
                return true;
            }
            topSpot = new Spot();
            return false;
        }

        #endregion

        public int CountOccupiedOf(int id, params Location[] locations)
        {
            int count = 0;
            foreach (var location in locations)
            {
                if (IsOccupiedBy(location.X, location.Y, id))
                {
                    ++count;
                }
            }
            return count;
        }

        public int CountOf(int id)
        {
            if (LogCountor.ContainsKey(id)) return LogCountor[id];
            return 0;
        }

        public IList<Location> GetAvailableLocation()
        {
            List<Location> list = new List<Location>();
            for(int i = 0; i < Entry.GetLength(0); ++i)
            {
                for (int j = 0; j < Entry.GetLength(0); ++j)
                {
                    if (CanLogAt(i, j)) list.Add(new Location(i, j));
                }
            }
            return list;
        }

        internal void Reset()
        {
            Initilize();
        }
    }
}
