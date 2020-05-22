using System;
using System.Collections.Generic;

namespace Alpha.Dol
{
    public class HexCellPriorityQueue
    {
        private List<HexCell> _list = new List<HexCell>();
        private int _count;
        private int _minimum = Int32.MaxValue;

        public void Enqueue(HexCell cell)
        {
            var priority = cell.SearchPriority;
            while (priority >= _list.Count)
            {
                _list.Add(null);
            }

            cell.NextSearchCell = _list[priority];
            _list[priority] = cell;
            _minimum = Math.Min(_minimum, priority);
            _count++;
        }

        public HexCell Dequeue()
        {
            _count--;
            for (var i = _minimum; i < _list.Count; i++)
            {
                var cell = _list[i];
                if (cell != null)
                {
                    _list[i] = cell.NextSearchCell;
                    return cell;
                }
            }

            return null;
        }

        public void ChangePriority(HexCell cell, int oldPriority)
        {
            if (cell == null || oldPriority > _list.Count) return;
            var cur = _list[oldPriority];
            if (cur != cell)
            {
                while (cur != null && cur.NextSearchCell != cell)
                {
                    cur = cur.NextSearchCell;
                }
            }
            cur.NextSearchCell = cell.NextSearchCell;
            Enqueue(cell);
            _count--;
        }

        public void Clear()
        {
            _list.Clear();
            _count = 0;
            _minimum = Int32.MaxValue;
        }

        public int Count => _count;
    }
}