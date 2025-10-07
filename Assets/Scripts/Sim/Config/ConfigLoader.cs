// Assets/Scripts/Sim/Config/ConfigLoader.cs
// C# 8.0
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        /// Loads JSON from an Assets-relative path (e.g., "Assets/Data/goap/items.json").
        /// Throws InvalidDataException on parse/validation errors.
        /// </summary>
        public static T LoadJson<T>(string assetsRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath))
                throw new ArgumentException("Path is required.", nameof(assetsRelativePath));

            var (relativePath, absolutePath) = ResolveAssetsPath(assetsRelativePath);

            if (!File.Exists(absolutePath))
                throw new FileNotFoundException("Config file not found", absolutePath);

            using var reader = CreateJsonReader(absolutePath);
            var serializer = JsonSerializer.Create(Settings);

            try
            {
                var result = serializer.Deserialize<T>(reader);
                if (result == null)
                    throw new InvalidDataException($"Deserialization produced null: {relativePath}");

                EnsureStreamFullyConsumed(reader, relativePath);
                return result;
            }
            catch (JsonReaderException ex)
            {
                throw BuildInvalidDataException(relativePath, "Failed to parse JSON (syntax error; comments/trailing commas are not allowed)", ex);
            }
            catch (JsonSerializationException ex)
            {
                throw BuildInvalidDataException(relativePath, "Failed to bind JSON to target type (unknown/missing fields?)", ex);
            }
        }

        public static JToken LoadJToken(string assetsRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath))
                throw new ArgumentException("Path is required.", nameof(assetsRelativePath));

            var (relativePath, absolutePath) = ResolveAssetsPath(assetsRelativePath);

            if (!File.Exists(absolutePath))
                throw new FileNotFoundException("Config file not found", absolutePath);

            using var reader = CreateJsonReader(absolutePath);

            try
            {
                var token = JToken.ReadFrom(reader);
                EnsureStreamFullyConsumed(reader, relativePath);
                return token;
            }
            catch (JsonReaderException ex)
            {
                throw BuildInvalidDataException(relativePath, "Failed to parse JSON (syntax error; comments/trailing commas are not allowed)", ex);
            }
        }

        public static string GetAbsolutePath(string assetsRelativePath)
        {
            if (string.IsNullOrWhiteSpace(assetsRelativePath))
                throw new ArgumentException("Path is required.", nameof(assetsRelativePath));

            var (_, absolutePath) = ResolveAssetsPath(assetsRelativePath);
            return absolutePath;
        }

        private static (string relativePath, string absolutePath) ResolveAssetsPath(string assetsRelativePath)
        {
            var trimmed = assetsRelativePath.Trim();
            var normalized = trimmed.Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal))
                normalized = "Assets/" + normalized.TrimStart('/');

            var absolute = Path.Combine(Application.dataPath, normalized.Substring("Assets/".Length));
            absolute = Path.GetFullPath(absolute);
            return (normalized, absolute);
        }

        private static JsonTextReader CreateJsonReader(string absolutePath)
        {
            var stream = File.OpenText(absolutePath);
            var reader = new JsonTextReader(stream)
            {
                CloseInput = true,
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Double
            };
            return reader;
        }

        private static void EnsureStreamFullyConsumed(JsonTextReader reader, string relativePath)
        {
            if (reader == null) return;
            if (reader.Read())
                throw new InvalidDataException($"Unexpected trailing content in JSON: {relativePath}");
        }

        private static InvalidDataException BuildInvalidDataException(string relativePath, string context, Exception inner)
        {
            return new InvalidDataException($"{context}: {relativePath}\n{inner.Message}", inner);
        }

    }
}
