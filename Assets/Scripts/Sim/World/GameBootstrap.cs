// Assets/Scripts/Sim/World/GameBootstrap.cs
// C# 8.0
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Sim.Config;
using Sim.Logging;

namespace Sim.World
{
    public class GameBootstrap : MonoBehaviour
    {
        private VisualElement _root;
        private InventoryGridPresenter _inventoryUI;

        public string DemoSettingsPath = "Assets/Data/demo.settings.json";
        public string ItemsPath = "Assets/Data/goap/items.json";

        void Awake()
        {
            Application.targetFrameRate = 60;
            Log.InitLogs();
            Log.World("GameBootstrap.Awake()");

            ContentValidator.ValidateAll();

            // Create a UI Document at runtime so no inspector references are required.
            var uiDoc = gameObject.AddComponent<UIDocument>();
            var panelSettings = Resources.Load<PanelSettings>("PanelSettings/DefaultPanelSettings");
            if (panelSettings == null)
            {
                // Create a minimal panel settings on the fly if none exist in Resources
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            }
            uiDoc.panelSettings = panelSettings;

            // Build UI tree in code
            _root = new VisualElement();
            _root.style.flexDirection = FlexDirection.Column;
            _root.style.paddingLeft = 8;
            _root.style.paddingTop = 8;
            uiDoc.rootVisualElement.Add(_root);

            var header = new Label("Inventory");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            _root.Add(header);

            // Inventory grid presenter
            _inventoryUI = new InventoryGridPresenter();
            _root.Add(_inventoryUI.Root);

            LoadWorld();
        }

        private void LoadWorld()
        {
            Log.World("LoadWorld() begin.");
            // Load placements (strict)
            var json = File.ReadAllText(DemoSettingsPath);
            // demo.settings.json is not a strict schema here; do a very light filter for pantry Things
            var asText = json;

            // naive extract: find objects with "type": "pantry"
            // In a real system you'd model full schema; for this bootstrap we load only what we need.
            var pantryThings = new List<Thing>();
            foreach (var line in asText.Split('\n'))
            {
                // no-op here; we will parse below using a minimal JSON approach.
            }

            // Better: use a very small parser for list of things
            // Load using a fallback class that only cares about "things"
            var root = Sim.Config.ConfigLoader.LoadJson<TempRoot>(DemoSettingsPath);
            if (root.placements == null || root.placements.things == null)
                throw new InvalidDataException("demo.settings.json missing placements.things");

            foreach (var td in root.placements.things)
            {
                if (td == null || string.IsNullOrEmpty(td.type)) continue;
                if (string.Equals(td.type, "pantry", StringComparison.OrdinalIgnoreCase))
                {
                    var t = new Thing(td.id ?? Guid.NewGuid().ToString("N"), td.type, new Vector2Int(td.x, td.y), td.attributes);
                    var cap = t.GetIntAttr("inventory_capacity", 0);
                    if (cap <= 0) throw new InvalidDataException("Pantry missing attributes.inventory_capacity in demo.settings.json");
                    t.Inventory = new Inventory(cap);
                    WorldState.Things[t.Id] = t;
                }
            }

            if (WorldState.Things.Count == 0)
                throw new InvalidDataException("No pantry found in demo.settings.json (type='pantry').");

            // Select the first pantry and bind UI
            WorldState.Selected = WorldState.Things.Values.First();
            _inventoryUI.Bind(WorldState.Selected.Inventory);

            Log.World($"Selected pantry {WorldState.Selected.Id} capacity={WorldState.Selected.Inventory.Capacity}");
        }

        [Serializable]
        private class TempRoot
        {
            public TempPlacement placements;
        }

        [Serializable]
        private class TempPlacement
        {
            public List<Sim.World.ThingDef> things;
        }
    }
}
