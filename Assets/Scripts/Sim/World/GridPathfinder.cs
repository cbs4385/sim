// Assets/Scripts/Sim/World/GridPathfinder.cs
// C# 8.0
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sim.World
{
    public static class GridPathfinder
    {
        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        public static List<Vector2Int> FindPath(RectInt bounds, Vector2Int start, Vector2Int goal, Func<Vector2Int, bool> isWalkable)
        {
            if (isWalkable == null)
                throw new ArgumentNullException(nameof(isWalkable));

            var path = new List<Vector2Int>();

            if (!bounds.Contains(start) || !bounds.Contains(goal))
                return path;

            var open = new MinHeap<Node>();
            var lookup = new Dictionary<Vector2Int, Node>();
            var closed = new HashSet<Vector2Int>();

            var startNode = new Node(start, 0, Heuristic(start, goal), null);
            open.Push(startNode);
            lookup[start] = startNode;

            while (open.Count > 0)
            {
                var current = open.Pop();

                if (!lookup.TryGetValue(current.Position, out var latest) || !ReferenceEquals(current, latest))
                    continue;

                if (current.Position == goal)
                    return Reconstruct(current);

                closed.Add(current.Position);

                foreach (var dir in Directions)
                {
                    var nextPos = current.Position + dir;
                    if (!bounds.Contains(nextPos))
                        continue;
                    if (closed.Contains(nextPos))
                        continue;
                    if (!isWalkable(nextPos) && nextPos != goal)
                        continue;

                    var gScore = current.G + 1;
                    if (lookup.TryGetValue(nextPos, out var existing) && gScore >= existing.G)
                        continue;

                    var neighbor = new Node(nextPos, gScore, Heuristic(nextPos, goal), current);
                    lookup[nextPos] = neighbor;
                    open.Push(neighbor);
                }
            }

            return path;
        }

        private static List<Vector2Int> Reconstruct(Node node)
        {
            var list = new List<Vector2Int>();
            var current = node;
            while (current != null)
            {
                list.Add(current.Position);
                current = current.Parent;
            }

            list.Reverse();
            return list;
        }

        private static int Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private sealed class Node : IComparable<Node>
        {
            public Vector2Int Position { get; }
            public int G { get; }
            public int H { get; }
            public int F { get; }
            public Node Parent { get; set; }

            public Node(Vector2Int position, int g, int h, Node parent)
            {
                Position = position;
                G = g;
                H = h;
                Parent = parent;
                F = g + h;
            }

            public int CompareTo(Node other)
            {
                if (other == null)
                    return -1;

                var compare = F.CompareTo(other.F);
                if (compare != 0)
                    return compare;

                compare = H.CompareTo(other.H);
                if (compare != 0)
                    return compare;

                compare = G.CompareTo(other.G);
                if (compare != 0)
                    return compare;

                compare = Position.x.CompareTo(other.Position.x);
                if (compare != 0)
                    return compare;

                return Position.y.CompareTo(other.Position.y);
            }
        }

        private sealed class MinHeap<T> where T : IComparable<T>
        {
            private readonly List<T> _items = new List<T>();

            public int Count => _items.Count;

            public void Push(T item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                _items.Add(item);
                BubbleUp(_items.Count - 1);
            }

            public T Pop()
            {
                if (_items.Count == 0)
                    throw new InvalidOperationException("Cannot remove item from an empty heap.");

                var root = _items[0];
                var lastIndex = _items.Count - 1;
                var last = _items[lastIndex];
                _items.RemoveAt(lastIndex);

                if (_items.Count > 0)
                {
                    _items[0] = last;
                    BubbleDown(0);
                }

                return root;
            }

            private void BubbleUp(int index)
            {
                while (index > 0)
                {
                    var parent = (index - 1) / 2;
                    if (_items[index].CompareTo(_items[parent]) >= 0)
                        break;

                    Swap(index, parent);
                    index = parent;
                }
            }

            private void BubbleDown(int index)
            {
                while (true)
                {
                    var left = index * 2 + 1;
                    var right = left + 1;
                    var smallest = index;

                    if (left < _items.Count && _items[left].CompareTo(_items[smallest]) < 0)
                        smallest = left;

                    if (right < _items.Count && _items[right].CompareTo(_items[smallest]) < 0)
                        smallest = right;

                    if (smallest == index)
                        break;

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int a, int b)
            {
                var temp = _items[a];
                _items[a] = _items[b];
                _items[b] = temp;
            }
        }
    }
}
