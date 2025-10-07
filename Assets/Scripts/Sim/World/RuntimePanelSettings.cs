// Assets/Scripts/Sim/World/RuntimePanelSettings.cs
// C# 8.0
using UnityEngine;
using UnityEngine.UIElements;

namespace Sim.World
{
    /// <summary>
    /// PanelSettings instance that guarantees a theme is assigned before the base
    /// PanelSettings initialization runs. This suppresses warnings that are logged when
    /// Unity creates a theme-less PanelSettings via ScriptableObject.CreateInstance.
    /// </summary>
    public sealed class RuntimePanelSettings : PanelSettings
    {
        protected override void OnEnable()
        {
            PanelSettingsThemeUtility.EnsureThemeAssigned(this);
            base.OnEnable();
        }
    }
}

