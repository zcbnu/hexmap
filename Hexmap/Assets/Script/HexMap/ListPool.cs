using System.Collections.Generic;

namespace Alpha.Dol
{
    public class ListPool<T>
    {
        private static Stack<List<T>> _stack = new Stack<List<T>>();
        public static List<T> Get()
        {
            if (_stack.Count > 0)
            {
                return _stack.Pop();
            }
            return new List<T>();
        }

        public static void Put(List<T> list)
        {
            list.Clear();
            _stack.Push(list);
        }
    }
}