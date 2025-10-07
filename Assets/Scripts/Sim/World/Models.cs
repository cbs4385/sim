// Assets/Scripts/Sim/World/Models.cs
// C# 8.0
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public ThingContainerDef container;

        [JsonExtensionData]
        public IDictionary<string, JToken> Extra { get; set; }
    }

    [Serializable]
    public class ThingContainerDef
    {
        public ThingInventoryDef inventory;

        [JsonExtensionData]
        public IDictionary<string, JToken> Extra { get; set; }
    }

    [Serializable]
    public class ThingInventoryDef
    {
        public int slots;
        public int stackSize;
        public List<ThingInventoryContentDef> contents;

        [JsonExtensionData]
        public IDictionary<string, JToken> Extra { get; set; }
    }

    [Serializable]
    public class ThingInventoryContentDef
    {
        public string item;
        public int quantity;

        [JsonExtensionData]
        public IDictionary<string, JToken> Extra { get; set; }
    }

    public abstract class Thing
    {
        private readonly Dictionary<string, float> _attributes;
        private readonly HashSet<string> _tags;

        public string Id { get; }
        public string Type { get; }
        public Vector2Int Cell { get; private set; }
        public IReadOnlyDictionary<string, float> Attributes => _attributes;
        public IReadOnlyCollection<string> Tags => _tags;

        protected Thing(string id, string type, Vector2Int cell, IEnumerable<string> tags, IDictionary<string, float> attributes)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Thing id is required", nameof(id));
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("Thing type is required", nameof(type));

            Id = id;
            Type = type;
            Cell = cell;

            _tags = tags == null
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()), StringComparer.OrdinalIgnoreCase);

            _attributes = attributes == null
                ? new Dictionary<string, float>()
                : new Dictionary<string, float>(attributes, StringComparer.OrdinalIgnoreCase);
        }

        public void MoveTo(Vector2Int cell)
        {
            Cell = cell;
        }

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;

            return _tags.Contains(tag);
        }

        public int GetIntAttr(string name, int fallback = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
                return fallback;

            if (_attributes != null && _attributes.TryGetValue(name, out var value))
                return Mathf.RoundToInt(value);

            return fallback;
        }

        public float GetFloatAttr(string name, float fallback = 0f)
        {
            if (string.IsNullOrWhiteSpace(name))
                return fallback;

            if (_attributes != null && _attributes.TryGetValue(name, out var value))
                return value;

            return fallback;
        }
    }

    public sealed class Item : Thing
    {
        public int MaxStackSize { get; }
        public string IconPath { get; }

        public Item(string id, IEnumerable<string> tags, int maxStackSize, string iconPath, IDictionary<string, float> attributes = null)
            : base(id, "item", Vector2Int.zero, tags, attributes)
        {
            if (maxStackSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxStackSize), "Stack size must be greater than zero.");

            MaxStackSize = maxStackSize;
            IconPath = iconPath ?? string.Empty;
        }
    }

    public sealed class Station : Thing
    {
        public Inventory Inventory { get; private set; }

        public Station(string id, string type, Vector2Int cell, IEnumerable<string> tags, IDictionary<string, float> attributes, Inventory inventory)
            : base(id, type, cell, tags, attributes)
        {
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public void SetInventory(Inventory inventory)
        {
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }
    }

    public sealed class ItemStack
    {
        public Item Item { get; }
        public string ItemId { get; }
        public int Quantity { get; private set; }
        public int MaxStackSize { get; }

        public ItemStack(Item item, int quantity, int maxStackSize)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));

            if (string.IsNullOrWhiteSpace(item.Id))
                throw new ArgumentException("Item id is required.", nameof(item));
            if (maxStackSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxStackSize), "Max stack size must be > 0.");
            if (quantity <= 0 || quantity > maxStackSize)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be between 1 and the max stack size.");

            ItemId = item.Id;
            MaxStackSize = maxStackSize;
            Quantity = quantity;
        }

        internal int Increase(int amount)
        {
            if (amount <= 0)
                return 0;

            var space = MaxStackSize - Quantity;
            if (space <= 0)
                return 0;

            var applied = Mathf.Min(space, amount);
            Quantity += applied;
            return applied;
        }

        internal int Decrease(int amount)
        {
            if (amount <= 0)
                return 0;

            var removed = Mathf.Min(amount, Quantity);
            Quantity -= removed;
            return removed;
        }
    }

    public enum InventoryChangeType
    {
        Added,
        Removed
    }

    public sealed class InventoryChangedEventArgs : EventArgs
    {
        public Inventory Inventory { get; }
        public InventoryChangeType ChangeType { get; }
        public Item Item { get; }
        public string ItemId { get; }
        public int QuantityDelta { get; }

        public int AbsoluteQuantity => Math.Abs(QuantityDelta);

        public InventoryChangedEventArgs(Inventory inventory, InventoryChangeType changeType, Item item, string itemId, int quantityDelta)
        {
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            ChangeType = changeType;
            Item = item;
            ItemId = itemId;
            QuantityDelta = quantityDelta;
        }
    }

    public sealed class Inventory
    {
        private readonly List<ItemStack> _items = new List<ItemStack>();
        private readonly ReadOnlyCollection<ItemStack> _readonlyItems;

        public int Capacity { get; }
        public int StackSizeLimit { get; }
        public IReadOnlyList<ItemStack> Items => _readonlyItems;

        public event EventHandler<InventoryChangedEventArgs> Changed;

        public Inventory(int capacity, int stackSizeLimit)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Inventory capacity must be greater than zero.");
            if (stackSizeLimit <= 0)
                throw new ArgumentOutOfRangeException(nameof(stackSizeLimit), "Stack size limit must be greater than zero.");

            Capacity = capacity;
            StackSizeLimit = stackSizeLimit;
            _readonlyItems = _items.AsReadOnly();
        }

        public bool TryAdd(Item item, int quantity, out int remainder)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

            var limit = DetermineStackLimit(item);
            var remaining = quantity;
            var added = 0;

            foreach (var stack in _items)
            {
                if (!string.Equals(stack.ItemId, item.Id, StringComparison.OrdinalIgnoreCase))
                    continue;

                var applied = stack.Increase(remaining);
                if (applied <= 0)
                    continue;

                remaining -= applied;
                added += applied;

                if (remaining <= 0)
                    break;
            }

            while (remaining > 0)
            {
                if (_items.Count >= Capacity)
                    break;

                var toCreate = Mathf.Min(limit, remaining);
                var newStack = new ItemStack(item, toCreate, limit);
                _items.Add(newStack);
                remaining -= toCreate;
                added += toCreate;
            }

            if (added > 0)
                RaiseChanged(InventoryChangeType.Added, item, item.Id, added);

            remainder = remaining;
            return remainder == 0;
        }

        public bool TryRemove(string itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("Item id is required", nameof(itemId));
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

            var available = GetQuantity(itemId);
            if (available < quantity)
                return false;

            var remaining = quantity;
            Item removedItem = null;

            for (var i = _items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var stack = _items[i];
                if (!string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                    continue;

                removedItem ??= stack.Item;
                var removed = stack.Decrease(remaining);
                remaining -= removed;

                if (stack.Quantity <= 0)
                    _items.RemoveAt(i);
            }

            if (remaining != 0)
                throw new InvalidOperationException("Inventory removal accounting mismatch.");

            RaiseChanged(InventoryChangeType.Removed, removedItem, itemId, -quantity);
            return true;
        }

        public int GetQuantity(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return 0;

            var total = 0;
            foreach (var stack in _items)
            {
                if (string.Equals(stack.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                    total += stack.Quantity;
            }

            return total;
        }

        public int OccupiedSlots => _items.Count;

        private int DetermineStackLimit(Item item)
        {
            if (item == null)
                return StackSizeLimit;

            return Mathf.Min(StackSizeLimit, item.MaxStackSize);
        }

        private void RaiseChanged(InventoryChangeType changeType, Item item, string itemId, int quantityDelta)
        {
            var handler = Changed;
            if (handler == null)
                return;

            var args = new InventoryChangedEventArgs(this, changeType, item, itemId, quantityDelta);
            handler.Invoke(this, args);
        }
    }

    public class Household
    {
        public string Id { get; }
        public string Type { get; }
        public RectInt Bounds { get; }
        public Vector2Int Center { get; }

        public Household(string id, string type, RectInt bounds, Vector2Int center)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id is required", nameof(id));
            Id = id;
            Type = type ?? string.Empty;
            Bounds = bounds;
            Center = center;
        }
    }

    public class JobSite
    {
        public string Id { get; }
        public string Type { get; }
        public RectInt Bounds { get; }
        public Vector2Int Center { get; }
        public List<string> Roles { get; } = new List<string>();

        public JobSite(string id, string type, RectInt bounds, Vector2Int center)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id is required", nameof(id));
            Id = id;
            Type = type ?? string.Empty;
            Bounds = bounds;
            Center = center;
        }
    }

    public class PointOfInterest
    {
        public string Id { get; }
        public string Name { get; }
        public RectInt Bounds { get; }
        public Vector2Int Center { get; }

        public PointOfInterest(string id, string name, RectInt bounds, Vector2Int center)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id is required", nameof(id));
            Id = id;
            Name = string.IsNullOrWhiteSpace(name) ? id : name;
            Bounds = bounds;
            Center = center;
        }
    }

    public class CalendarState
    {
        public int TotalTicks { get; internal set; }
        public int TicksPerDay { get; internal set; }
        public int DayOfYear { get; internal set; }
        public int DayOfSeason { get; internal set; }
        public string Season { get; internal set; } = string.Empty;
        public bool IsDaylight { get; internal set; }
        public float NormalizedTimeOfDay { get; internal set; }
        public string LastHoliday { get; internal set; } = string.Empty;
    }

    public static class WorldState
    {
        public static readonly Dictionary<string, Thing> Things = new Dictionary<string, Thing>();
        public static readonly Dictionary<string, Household> Households = new Dictionary<string, Household>();
        public static readonly Dictionary<string, JobSite> JobSites = new Dictionary<string, JobSite>();
        public static readonly Dictionary<string, PointOfInterest> PointsOfInterest = new Dictionary<string, PointOfInterest>();
        public static readonly CalendarState Calendar = new CalendarState();
        public static Thing Selected;
    }
}
