// Assets/Scripts/Sim/World/ItemDatabase.cs
// C# 8.0
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sim.Config;
using Sim.Logging;

namespace Sim.World
{
    public interface IItemDatabase
    {
        void Load(string itemsPath);
        bool TryGet(string itemId, out Item item);
        IReadOnlyDictionary<string, Item> Items { get; }
    }

    public sealed class ItemDatabase : IItemDatabase
    {
        private readonly Dictionary<string, Item> _items = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);
        private readonly IWorldLogger _logger;

        public ItemDatabase(IWorldLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IReadOnlyDictionary<string, Item> Items => _items;

        public void Load(string itemsPath)
        {
            if (string.IsNullOrWhiteSpace(itemsPath))
                throw new ArgumentException("Items path is required.", nameof(itemsPath));

            var defs = ConfigLoader.LoadJson<List<ItemDefinitionData>>(itemsPath);
            if (defs == null)
                throw new InvalidDataException($"No item data found in {itemsPath}.");

            _items.Clear();

            foreach (var def in defs)
            {
                if (def == null)
                    continue;

                if (string.IsNullOrWhiteSpace(def.id))
                    throw new InvalidDataException($"Item definition missing id in {itemsPath}.");
                if (def.stackSize <= 0)
                    throw new InvalidDataException($"Item '{def.id}' missing stackSize in {itemsPath}.");

                var item = new Item(def.id, def.tags, def.stackSize, def.icon);
                _items[item.Id] = item;
                _logger.World($"Loaded item '{item.Id}' (stackSize={item.MaxStackSize}).");
            }

            _logger.World($"Loaded {_items.Count} items from {itemsPath}.");
        }

        public bool TryGet(string itemId, out Item item)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                item = null;
                return false;
            }

            return _items.TryGetValue(itemId, out item);
        }

        private sealed class ItemDefinitionData
        {
            public string id;
            public List<string> tags;
            public int stackSize;
            public PriceData price;
            public List<string> gifts;
            public List<string> effects;
            public string icon;

            [JsonExtensionData]
            public IDictionary<string, JToken> Extra { get; set; }
        }

        private sealed class PriceData
        {
            public float buy;
            public float sell;

            [JsonExtensionData]
            public IDictionary<string, JToken> Extra { get; set; }
        }
    }
}
