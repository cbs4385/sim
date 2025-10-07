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
            Root.style.justifyContent = Justify.FlexStart;
            Root.style.alignContent = Align.FlexStart;
            Root.style.alignItems = Align.FlexStart;
            Root.style.flexGrow = 0f;
            Root.style.flexShrink = 0f;
            Root.style.paddingTop = 4;
            Root.style.paddingLeft = 4;
            Root.style.paddingRight = 4;
            Root.style.paddingBottom = 4;
            Root.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
            Root.style.borderTopWidth = 1f;
            Root.style.borderRightWidth = 1f;
            Root.style.borderBottomWidth = 1f;
            Root.style.borderLeftWidth = 1f;
            Root.style.borderTopColor = new Color(1f, 1f, 1f, 0.2f);
            Root.style.borderRightColor = new Color(1f, 1f, 1f, 0.2f);
            Root.style.borderBottomColor = new Color(1f, 1f, 1f, 0.2f);
            Root.style.borderLeftColor = new Color(1f, 1f, 1f, 0.2f);
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
                slot.style.flexGrow = 0f;
                slot.style.flexShrink = 0f;
                slot.style.borderTopWidth = 1;
                slot.style.borderLeftWidth = 1;
                slot.style.borderRightWidth = 1;
                slot.style.borderBottomWidth = 1;
                slot.style.borderTopColor = new Color(1f, 1f, 1f, 0.15f);
                slot.style.borderRightColor = new Color(1f, 1f, 1f, 0.15f);
                slot.style.borderBottomColor = new Color(1f, 1f, 1f, 0.15f);
                slot.style.borderLeftColor = new Color(1f, 1f, 1f, 0.15f);
                slot.style.backgroundColor = new Color(0f, 0f, 0f, 0.4f);
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
                    qty.style.fontSize = 10;
                    qty.style.color = Color.white;
                    qty.style.unityFontStyleAndWeight = FontStyle.Bold;
                    qty.pickingMode = PickingMode.Ignore;

                    var qtyContainer = new VisualElement();
                    qtyContainer.style.position = Position.Absolute;
                    qtyContainer.style.bottom = 2;
                    qtyContainer.style.right = 2;
                    qtyContainer.style.paddingLeft = 4;
                    qtyContainer.style.paddingRight = 4;
                    qtyContainer.style.paddingTop = 1;
                    qtyContainer.style.paddingBottom = 1;
                    qtyContainer.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
                    qtyContainer.style.borderTopLeftRadius = 3;
                    qtyContainer.style.borderBottomRightRadius = 3;
                    qtyContainer.pickingMode = PickingMode.Ignore;
                    qtyContainer.Add(qty);

                    slot.Add(qtyContainer);
                }

                Root.Add(slot);
            }
        }
    }
}
