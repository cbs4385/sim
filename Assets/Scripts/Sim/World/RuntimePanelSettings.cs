// Assets/Scripts/Sim/World/RuntimePanelSettings.cs
// C# 8.0
using System.Reflection;
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
        protected void OnEnable()
        {
            PanelSettingsThemeUtility.EnsureThemeAssigned(this);

            var onEnable = typeof(PanelSettings).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);
            onEnable?.Invoke(this, null);
        }
    }
}

