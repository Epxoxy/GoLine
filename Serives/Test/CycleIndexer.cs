using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServices
{
    public class CycleIndexer
    {
        private int pointAt;
        public int PointAt
        {
            get
            {
                if(pointAt < 0 && Collection.Count > 0) pointAt = 0;
                return pointAt;
            }
            private set
            {
                pointAt = value;
            }
        }
        public int NextIndex
        {
            get
            {
                if (!IsInRange(PointAt)) return -1;
                if (PointAt == (Collection.Count - 1)) return 0;
                return PointAt + 1;
            }
        }
        public int FrontIndex
        {
            get
            {
                if (!IsInRange(PointAt)) return -1;
                if (PointAt == 0) return Collection.Count - 1;
                return PointAt - 1;
            }
        }
        private int startIndex;
        public int StartIndex
        {
            get
            {
                return startIndex;
            }
            set
            {
                if (IsInRange(value) && startIndex != value)
                {
                    startIndex = value;
                }
            }
        }
        public int Count
        {
            get
            {
                if (Collection == null) return 0;
                return Collection.Count;
            }
        }
        private ICollection Collection { get; set; }

        private bool IsInRange(int index, int range = 0)
        {
            if (Collection == null || Collection.Count < 1) return false;
            return index > -1 && index < Collection.Count;
        }

        public bool MoveToStart()
        {
            if (IsInRange(StartIndex))
            {
                PointAt = StartIndex;
                GameBasic.Debug.LogLine($"CycleIndexer MoveToStart -> {StartIndex}");
                return true;
            }
            return false;
        }

        public bool MoveToNext()
        {
            int index = NextIndex;
            if (index < 0) return false;
            PointAt = index;
            GameBasic.Debug.LogLine($"CycleIndexer MoveToNext -> {index}");
            return true;
        }
        public bool MoveFront()
        {
            int index = FrontIndex;
            if (index < 0) return false;
            PointAt = index;
            GameBasic.Debug.LogLine($"CycleIndexer MoveToFront -> {index}");
            return true;
        }
        public void Reset()
        {
            if (IsInRange(StartIndex)) PointAt = StartIndex;
            else PointAt = -1;
        }

        public CycleIndexer(ICollection collection)
        {
            Collection = collection;
        }
    }
}
