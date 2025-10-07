// Assets/Scripts/Sim/World/Models.cs
// C# 8.0
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sim.World
{
    [Serializable]
    public class ThingDef
    {
        public string id;
        public string type;
        public List<string> tags;
        public int x;
        public int y;
        public Dictionary<string, float> attributes;
    }

    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public int quantity;
    }

    public class Inventory
    {
        public int Capacity { get; private set; }
        public readonly List<ItemStack> Items = new List<ItemStack>();

        public Inventory(int capacity)
        {
            if (capacity <= 0) throw new ArgumentException("Inventory capacity must be > 0");
            Capacity = capacity;
        }
    }

    public class Thing
    {
        public string Id { get; private set; }
        public string Type { get; private set; }
        public Vector2Int Cell { get; private set; }
        public Dictionary<string, float> Attr { get; private set; }
        public Inventory Inventory { get; set; }

        public Thing(string id, string type, Vector2Int cell, Dictionary<string, float> attr)
        {
            Id = id;
            Type = type;
            Cell = cell;
            Attr = attr ?? new Dictionary<string, float>();
        }

        public int GetIntAttr(string name, int fallback = 0)
        {
            if (Attr != null && Attr.TryGetValue(name, out var v)) return Mathf.RoundToInt(v);
            return fallback;
        }
    }

    public static class WorldState
    {
        public static readonly Dictionary<string, Thing> Things = new Dictionary<string, Thing>();
        public static Thing Selected;
    }
}
