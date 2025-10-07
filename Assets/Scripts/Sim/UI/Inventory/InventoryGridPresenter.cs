// Assets/Scripts/Sim/UI/Inventory/InventoryGridPresenter.cs
// C# 8.0
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Sim.World;

namespace Sim.World
{
    public class InventoryGridPresenter
    {
        public VisualElement Root { get; private set; }
        private Inventory _inventory;

        public InventoryGridPresenter()
        {
            Root = new VisualElement();
            Root.style.flexDirection = FlexDirection.Row;
            Root.style.flexWrap = Wrap.Wrap;
            // NOTE: Older UI Toolkit versions don't support style.gap/rowGap/columnGap.
            // We'll space cells using per-slot margins instead.
        }

        public void Bind(Inventory inventory)
        {
            if (_inventory != null)
                _inventory.Changed -= OnInventoryChanged;

            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _inventory.Changed += OnInventoryChanged;
            Rebuild();
        }

        private void OnInventoryChanged(object sender, InventoryChangedEventArgs e)
        {
            Rebuild();
        }

        private void Rebuild()
        {
            Root.Clear();
            if (_inventory == null) throw new InvalidOperationException("Bind() must be called first.");

            for (int i = 0; i < _inventory.Capacity; i++)
            {
                var slot = new VisualElement();
                slot.style.width = 40;
                slot.style.height = 40;
                slot.style.borderTopWidth = 1;
                slot.style.borderLeftWidth = 1;
                slot.style.borderRightWidth = 1;
                slot.style.borderBottomWidth = 1;
                slot.style.justifyContent = Justify.Center;
                slot.style.alignItems = Align.Center;
                // Spacing between cells (fallback for missing style.gap)
                slot.style.marginRight = 4;
                slot.style.marginBottom = 4;

                if (i < _inventory.Items.Count)
                {
                    var stack = _inventory.Items[i];
                    var iconPath = stack?.Item?.IconPath;
                    var spriteKey = string.IsNullOrWhiteSpace(iconPath)
                        ? "Items/" + stack.ItemId
                        : iconPath;
                    var sprite = SpriteResolver.LoadSpriteFromResources(spriteKey);
                    var img = new Image();
                    img.sprite = sprite;
                    img.scaleMode = ScaleMode.ScaleToFit;
                    img.pickingMode = PickingMode.Ignore;
                    img.style.width = 32;
                    img.style.height = 32;
                    slot.Add(img);

                    var qty = new Label(stack.Quantity.ToString());
                    qty.style.position = Position.Absolute;
                    qty.style.bottom = 2;
                    qty.style.right = 4;
                    qty.style.fontSize = 10;
                    slot.Add(qty);
                }

                Root.Add(slot);
            }
        }
    }
}
