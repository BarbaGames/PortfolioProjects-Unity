using System.Collections.Generic;

namespace Utils
{
    public class FastPriorityQueue<TNodeType>
    {
        private List<NodeEntry> _heap;
        private Dictionary<TNodeType, int> _indexMap;

        public FastPriorityQueue()
        {
            _heap = new List<NodeEntry>();
            _indexMap = new Dictionary<TNodeType, int>();
        }

        // Constructor with capacity parameter
        public FastPriorityQueue(int capacity)
        {
            _heap = new List<NodeEntry>(capacity);
            _indexMap = new Dictionary<TNodeType, int>(capacity);
        }

        public int Count => _heap.Count;

        // Initialize method for existing instances
        public void Initialize(int capacity)
        {
            if (_heap.Count == 0)
            {
                _heap = new List<NodeEntry>(capacity);
                _indexMap = new Dictionary<TNodeType, int>(capacity);
            }
        }

        // New Contains method
        public bool Contains(TNodeType node)
        {
            return _indexMap.ContainsKey(node);
        }

        public void Enqueue(TNodeType node, int priority)
        {
            if (Contains(node))
            {
                UpdatePriority(node, priority);
                return;
            }

            _heap.Add(new NodeEntry { Node = node, Priority = priority });
            _indexMap[node] = _heap.Count - 1;
            BubbleUp(_heap.Count - 1);
        }

        public TNodeType Dequeue()
        {
            TNodeType result = _heap[0].Node;
            Swap(0, _heap.Count - 1);
            _indexMap.Remove(result);
            _heap.RemoveAt(_heap.Count - 1);

            if (_heap.Count > 0)
                BubbleDown(0);

            return result;
        }

        public void UpdatePriority(TNodeType node, int newPriority)
        {
            if (!_indexMap.TryGetValue(node, out int index)) return;

            int oldPriority = _heap[index].Priority;
            _heap[index] = new NodeEntry { Node = node, Priority = newPriority };

            if (newPriority < oldPriority)
                BubbleUp(index);
            else
                BubbleDown(index);
        }

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (_heap[parentIndex].Priority <= _heap[index].Priority)
                    break;

                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                int leftChild = 2 * index + 1;
                if (leftChild >= _heap.Count) return;

                int rightChild = leftChild + 1;
                int minChild = rightChild < _heap.Count && _heap[rightChild].Priority < _heap[leftChild].Priority
                    ? rightChild
                    : leftChild;

                if (_heap[index].Priority <= _heap[minChild].Priority)
                    return;

                Swap(index, minChild);
                index = minChild;
            }
        }

        private void Swap(int a, int b)
        {
            (_heap[a], _heap[b]) = (_heap[b], _heap[a]);
            _indexMap[_heap[a].Node] = a;
            _indexMap[_heap[b].Node] = b;
        }

        private struct NodeEntry
        {
            public TNodeType Node;
            public int Priority;
        }
    }
}