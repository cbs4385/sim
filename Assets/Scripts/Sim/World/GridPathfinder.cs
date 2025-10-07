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

            var open = new List<Node>();
            var lookup = new Dictionary<Vector2Int, Node>();
            var closed = new HashSet<Vector2Int>();

            var startNode = new Node(start, 0, Heuristic(start, goal), null);
            open.Add(startNode);
            lookup[start] = startNode;

            while (open.Count > 0)
            {
                open.Sort((a, b) => a.F.CompareTo(b.F));
                var current = open[0];
                open.RemoveAt(0);

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
                    if (!lookup.TryGetValue(nextPos, out var neighbor))
                    {
                        neighbor = new Node(nextPos, gScore, Heuristic(nextPos, goal), current);
                        lookup[nextPos] = neighbor;
                        open.Add(neighbor);
                    }
                    else if (gScore < neighbor.G)
                    {
                        neighbor.G = gScore;
                        neighbor.Parent = current;
                        neighbor.UpdateF();
                        if (!open.Contains(neighbor))
                            open.Add(neighbor);
                    }
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

        private sealed class Node
        {
            public Vector2Int Position { get; }
            public int G { get; set; }
            public int H { get; }
            public int F { get; private set; }
            public Node Parent { get; set; }

            public Node(Vector2Int position, int g, int h, Node parent)
            {
                Position = position;
                G = g;
                H = h;
                Parent = parent;
                F = g + h;
            }

            public void UpdateF()
            {
                F = G + H;
            }
        }
    }
}
