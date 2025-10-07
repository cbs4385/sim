// Assets/Scripts/Sim/Config/ConfigLoader.cs
// C# 8.0
using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Sim.Config
{
    /// <summary>
    /// Strict JSON loader using Newtonsoft.Json (Unity package "com.unity.nuget.newtonsoft-json").
    /// - No comments, no trailing commas.
    /// - Unknown properties cause failure.
    /// - Throws on any error (no fallbacks).
    /// </summary>
    public static class ConfigLoader
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            // Fail on unknown JSON fields to keep data strict.
            MissingMemberHandling = MissingMemberHandling.Error,
            // Avoid date auto-parsing side effects.
            DateParseHandling = DateParseHandling.None,
            // Ignore $type / $id metadata if present.
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            // Keep default behavior for nulls (caller should validate required fields).
            NullValueHandling = NullValueHandling.Include
        };

        /// <summary>
        /// Loads JSON from an Assets-relative path (e.g., "Assets/Data/items.json").
        /// Throws InvalidDataException on parse/validation errors.
        /// </summary>
        public static T LoadJson<T>(string assetsRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath))
                throw new ArgumentException("Path is required.", nameof(assetsRelativePath));

            string path;
#if UNITY_EDITOR || UNITY_STANDALONE
            if (assetsRelativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                path = Path.Combine(Application.dataPath, assetsRelativePath.Substring("Assets/".Length));
            else
                path = Path.Combine(Application.dataPath, assetsRelativePath.TrimStart('/', '\\'));
#else
            path = Path.Combine(Application.dataPath, assetsRelativePath.TrimStart('/', '\\'));
#endif
            path = Path.GetFullPath(path);

            if (!File.Exists(path))
                throw new FileNotFoundException("Config file not found", path);

            string json = File.ReadAllText(path);

            try
            {
                // NOTE: We intentionally do NOT enable comments or trailing commas.
                // If your file contains these, fix the data; this loader is strict by design.
                var result = JsonConvert.DeserializeObject<T>(json, Settings);
                if (result == null)
                    throw new InvalidDataException("Deserialization produced null: " + path);
                return result;
            }
            catch (JsonReaderException ex)
            {
                throw new InvalidDataException("Failed to parse JSON (syntax error; comments/trailing commas are not allowed): " + path, ex);
            }
            catch (JsonSerializationException ex)
            {
                throw new InvalidDataException("Failed to bind JSON to target type (unknown/missing fields?): " + path, ex);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Unexpected error parsing JSON: " + path, ex);
            }
        }
    }
}
