using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SenCity.Core.Grid;
using UnityEditor;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement.Editor
{
    public static class FurniturePlacementCatalogTools
    {
        private const string DataFolder = "Assets/_Project/Features/FurniturePlacement/Data";
        private const string CatalogPath = DataFolder + "/SenCityFurnitureCatalog.asset";
        private const long LargeAssetWarningBytes = 10L * 1024L * 1024L;

        private static readonly HashSet<string> HeavyExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".fbx",
            ".obj",
            ".blend",
            ".png",
            ".jpg",
            ".jpeg",
            ".psd",
            ".tga",
            ".wav",
            ".mp3",
            ".mp4",
            ".mov",
            ".zip"
        };

        [MenuItem("Tools/SEN CITY/Furniture Placement/Rebuild Furniture Catalog")]
        public static void RebuildFurnitureCatalog()
        {
            EnsureDataFolder();
            FurnitureCatalogDefinition catalog = AssetDatabase.LoadAssetAtPath<FurnitureCatalogDefinition>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FurnitureCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            List<FurnitureItemDefinition> items = FindFurnitureItems();
            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty itemList = serialized.FindProperty("items");
            itemList.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
                itemList.GetArrayElementAtIndex(i).objectReferenceValue = items[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FurniturePlacementCatalogTools] Rebuilt catalog with {items.Count} furniture items at {CatalogPath}.");
        }

        [MenuItem("Tools/SEN CITY/Furniture Placement/Audit Catalog And Assets")]
        public static void AuditCatalogAndAssets()
        {
            List<string> notes = new List<string>();
            List<string> warnings = new List<string>();
            AuditFurnitureItems(FindFurnitureItems(), notes, warnings);
            AuditHeavyAssets(warnings);

            foreach (string note in notes)
                Debug.Log($"[FurniturePlacementCatalogTools] {note}");

            if (warnings.Count == 0)
            {
                Debug.Log("[FurniturePlacementCatalogTools] Audit passed. No furniture catalog or heavy asset warnings found.");
                return;
            }

            foreach (string warning in warnings)
                Debug.LogWarning($"[FurniturePlacementCatalogTools] {warning}");
        }

        private static void AuditFurnitureItems(
            IReadOnlyList<FurnitureItemDefinition> items,
            List<string> notes,
            List<string> warnings)
        {
            var seenIds = new Dictionary<string, FurnitureItemDefinition>();
            foreach (FurnitureItemDefinition item in items)
            {
                if (item == null)
                    continue;

                if (string.IsNullOrWhiteSpace(item.ItemId))
                    warnings.Add($"{AssetDatabase.GetAssetPath(item)} has an empty item id.");
                else if (seenIds.TryGetValue(item.ItemId, out FurnitureItemDefinition duplicate))
                    warnings.Add($"{AssetDatabase.GetAssetPath(item)} duplicates item id '{item.ItemId}' from {AssetDatabase.GetAssetPath(duplicate)}.");
                else
                    seenIds[item.ItemId] = item;

                GridFootprint footprint = item.Footprint;
                if (footprint.Width <= 0 || footprint.Depth <= 0)
                    warnings.Add($"{AssetDatabase.GetAssetPath(item)} has an invalid footprint.");

                if (item.Prefab == null)
                    notes.Add($"{AssetDatabase.GetAssetPath(item)} has no prefab assigned; runtime will use the cube fallback.");
            }
        }

        private static void AuditHeavyAssets(List<string> warnings)
        {
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !HeavyExtensions.Contains(Path.GetExtension(path)))
                    continue;

                string fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath))
                    continue;

                long byteLength = new FileInfo(fullPath).Length;
                if (byteLength < LargeAssetWarningBytes)
                    continue;

                warnings.Add($"{path} is {FormatBytes(byteLength)}. Confirm Git LFS tracking before pushing.");
            }
        }

        private static List<FurnitureItemDefinition> FindFurnitureItems()
        {
            return AssetDatabase.FindAssets("t:FurnitureItemDefinition", new[] { DataFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<FurnitureItemDefinition>)
                .Where(item => item != null)
                .OrderBy(item => item.ItemId, StringComparer.Ordinal)
                .ToList();
        }

        private static void EnsureDataFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Features/FurniturePlacement"))
                AssetDatabase.CreateFolder("Assets/_Project/Features", "FurniturePlacement");

            if (!AssetDatabase.IsValidFolder(DataFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Features/FurniturePlacement", "Data");
        }

        private static string FormatBytes(long bytes)
        {
            double megabytes = bytes / 1024d / 1024d;
            return $"{megabytes:0.0} MB";
        }
    }
}
