using System.Collections.Generic;
using SenCity.Core.Grid;
using SenCity.Features.FurniturePlacement.Input;
using SenCity.Features.FurniturePlacement.Save;
using SenCity.Core.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SenCity.Features.FurniturePlacement.Editor
{
    public static class FurniturePlacementPrototypeBuilder
    {
        private const string DataFolder = "Assets/_Project/Features/FurniturePlacement/Data";
        private const string SceneFolder = "Assets/_Project/Features/FurniturePlacement/Scenes";
        private const string GridProfilePath = DataFolder + "/SenCityPlacementGrid.asset";
        private const string CatalogPath = DataFolder + "/SenCityFurnitureCatalog.asset";
        private const string ChairPath = DataFolder + "/Demo_WoodChair.asset";
        private const string TablePath = DataFolder + "/Demo_LowTable.asset";
        private const string PlanterPath = DataFolder + "/Demo_BalconyPlanter.asset";
        private const string ScenePath = SceneFolder + "/SenCityFurniturePlacementPrototype.unity";

        [MenuItem("Tools/SEN CITY/Furniture Placement/Rebuild Prototype Scene")]
        public static void RebuildPrototypeScene()
        {
            EnsureFolders();
            SenCityGridProfile gridProfile = EnsureGridProfile();
            var items = new List<FurnitureItemDefinition>
            {
                EnsureFurnitureItem(ChairPath, "demo_wood_chair", "Ghe go", FurnitureCategory.Chair, 2, 2, 8),
                EnsureFurnitureItem(TablePath, "demo_low_table", "Ban tra", FurnitureCategory.Table, 4, 3, 4),
                EnsureFurnitureItem(PlanterPath, "demo_balcony_planter", "Chau cay ban cong", FurnitureCategory.Farming, 3, 2, 6)
            };
            FurnitureCatalogDefinition catalog = EnsureCatalog(items);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SenCityFurniturePlacementPrototype";

            CreateLighting();
            CreateCamera();
            CreateGround(gridProfile);
            FurniturePlacementRuntime runtime = CreateRuntime(gridProfile, catalog, items);
            CreateHud(runtime, items);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FurniturePlacementPrototypeBuilder] Rebuilt prototype scene at {ScenePath}");
        }

        private static FurnitureCatalogDefinition EnsureCatalog(IReadOnlyList<FurnitureItemDefinition> items)
        {
            FurnitureCatalogDefinition catalog = AssetDatabase.LoadAssetAtPath<FurnitureCatalogDefinition>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FurnitureCatalogDefinition>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty itemList = serialized.FindProperty("items");
            itemList.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
                itemList.GetArrayElementAtIndex(i).objectReferenceValue = items[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project/Features/FurniturePlacement", "Data");
            EnsureFolder("Assets/_Project/Features/FurniturePlacement", "Scenes");
        }

        private static void EnsureFolder(string parent, string folder)
        {
            string path = $"{parent}/{folder}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folder);
        }

        private static SenCityGridProfile EnsureGridProfile()
        {
            SenCityGridProfile profile = AssetDatabase.LoadAssetAtPath<SenCityGridProfile>(GridProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<SenCityGridProfile>();
                AssetDatabase.CreateAsset(profile, GridProfilePath);
            }

            profile.cellSize = 0.2f;
            profile.columns = 20;
            profile.rows = 20;
            profile.origin = Vector3.zero;
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static FurnitureItemDefinition EnsureFurnitureItem(
            string path,
            string itemId,
            string displayName,
            FurnitureCategory category,
            int width,
            int depth,
            int quantity)
        {
            FurnitureItemDefinition item = AssetDatabase.LoadAssetAtPath<FurnitureItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<FurnitureItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }

            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("itemId").stringValue = itemId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("category").enumValueIndex = (int)category;
            serialized.FindProperty("prefab").objectReferenceValue = null;
            serialized.FindProperty("icon").objectReferenceValue = null;
            SerializedProperty footprint = serialized.FindProperty("footprint");
            footprint.FindPropertyRelative("width").intValue = width;
            footprint.FindPropertyRelative("depth").intValue = depth;
            serialized.FindProperty("startingQuantity").intValue = quantity;
            serialized.FindProperty("canStore").boolValue = true;
            serialized.FindProperty("lockedBySystem").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        private static void CreateLighting()
        {
            var lightObject = new GameObject("Sun Warm Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.82f, 0.58f);
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -34f, 0f);
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Prototype Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(2.3f, 4.4f, -4.2f);
            cameraObject.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.Skybox;
        }

        private static void CreateGround(SenCityGridProfile gridProfile)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Balcony Placement Floor";
            ground.transform.position = new Vector3(
                gridProfile.columns * gridProfile.cellSize * 0.5f,
                -0.025f,
                gridProfile.rows * gridProfile.cellSize * 0.5f);
            ground.transform.localScale = new Vector3(
                gridProfile.columns * gridProfile.cellSize,
                0.05f,
                gridProfile.rows * gridProfile.cellSize);
            Renderer renderer = ground.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial("Prototype Warm Wood", new Color(0.64f, 0.42f, 0.25f));
        }

        private static FurniturePlacementRuntime CreateRuntime(
            SenCityGridProfile gridProfile,
            FurnitureCatalogDefinition catalog,
            IReadOnlyList<FurnitureItemDefinition> items)
        {
            var root = new GameObject("Furniture Placement Runtime");
            var placedRoot = new GameObject("Placed Furniture Root").transform;
            var previewRoot = new GameObject("Preview Ghost Root").transform;
            placedRoot.SetParent(root.transform);
            previewRoot.SetParent(root.transform);

            FurniturePlacementController controller = root.AddComponent<FurniturePlacementController>();
            FurnitureInventoryRuntime inventory = root.AddComponent<FurnitureInventoryRuntime>();
            FurniturePlacementRuntime runtime = root.AddComponent<FurniturePlacementRuntime>();
            root.AddComponent<FurniturePlacementInputAdapter>();
            FurniturePlacementSaveService saveService = root.AddComponent<FurniturePlacementSaveService>();

            SetObjectReference(controller, "gridProfile", gridProfile);
            SetObjectReference(inventory, "catalogAsset", catalog);
            SetInventoryCatalog(inventory, items);
            SetObjectReference(runtime, "controller", controller);
            SetObjectReference(runtime, "inventory", inventory);
            SetObjectReference(runtime, "saveService", saveService);
            SetObjectReference(runtime, "gridProfile", gridProfile);
            SetObjectReference(runtime, "placedRoot", placedRoot);
            SetObjectReference(runtime, "previewRoot", previewRoot);

            return runtime;
        }

        private static void CreateHud(FurniturePlacementRuntime runtime, IReadOnlyList<FurnitureItemDefinition> items)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            var canvasObject = new GameObject("Furniture Placement HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Transform inventoryPanel = CreatePanel(canvasObject.transform, "Inventory Panel", new Vector2(170f, 260f), new Vector2(95f, -155f));
            for (int i = 0; i < items.Count; i++)
                CreateInventoryButton(inventoryPanel, runtime, items[i], defaultFont, i);

            SenCityToastPresenter toast = CreateToast(canvasObject.transform, defaultFont);
            SenCityConfirmDialog dialog = CreateConfirmDialog(canvasObject.transform, defaultFont);
            FurniturePlacementHud hud = canvasObject.AddComponent<FurniturePlacementHud>();
            SetObjectReference(hud, "runtime", runtime);
            SetObjectReference(hud, "toastPresenter", toast);
            SetObjectReference(hud, "confirmDialog", dialog);
            SetObjectReference(hud, "moveButton", CreateCommandButton(canvasObject.transform, "Move Button", "Move", defaultFont, new Vector2(-420f, 54f)));
            SetObjectReference(hud, "rotateButton", CreateCommandButton(canvasObject.transform, "Rotate Button", "Rotate", defaultFont, new Vector2(-280f, 54f)));
            SetObjectReference(hud, "confirmButton", CreateCommandButton(canvasObject.transform, "Confirm Button", "Confirm", defaultFont, new Vector2(-140f, 54f)));
            SetObjectReference(hud, "cancelButton", CreateCommandButton(canvasObject.transform, "Cancel Button", "Cancel", defaultFont, new Vector2(0f, 54f)));
            SetObjectReference(hud, "storeButton", CreateCommandButton(canvasObject.transform, "Store Button", "Store", defaultFont, new Vector2(140f, 54f)));
            SetObjectReference(hud, "closeButton", CreateCommandButton(canvasObject.transform, "Close Selection Button", "Close", defaultFont, new Vector2(280f, 54f)));
            SetObjectReference(hud, "saveButton", CreateCommandButton(canvasObject.transform, "Save Button", "Save", defaultFont, new Vector2(420f, 54f)));
            SetObjectReference(hud, "loadButton", CreateCommandButton(canvasObject.transform, "Load Button", "Load", defaultFont, new Vector2(560f, 54f)));
            SetObjectReference(hud, "selectedItemNameText", CreateText(canvasObject.transform, "Selected Item Text", "No selection", defaultFont, new Vector2(180f, 28f), new Vector2(122f, 100f)));
        }

        private static Transform CreatePanel(Transform parent, string name, Vector2 size, Vector2 anchoredPosition)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.18f, 0.13f, 0.1f, 0.78f);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return panel.transform;
        }

        private static Button CreateInventoryButton(Transform parent, FurniturePlacementRuntime runtime, FurnitureItemDefinition item, Font font, int index)
        {
            Button button = CreateButton(parent, $"Inventory {item.DisplayName}", item.DisplayName, font, new Vector2(0f, -30f - index * 58f));
            FurnitureInventoryButton inventoryButton = button.gameObject.AddComponent<FurnitureInventoryButton>();
            Text label = button.GetComponentInChildren<Text>();
            inventoryButton.Configure(runtime, item, label);
            return button;
        }

        private static Button CreateCommandButton(Transform parent, string name, string label, Font font, Vector2 anchoredPosition)
        {
            Button button = CreateButton(parent, name, label, font, anchoredPosition);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            return button;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 anchoredPosition)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.86f, 0.55f, 0.28f, 0.92f);
            Button button = buttonObject.AddComponent<Button>();
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(132f, 42f);
            rect.anchoredPosition = anchoredPosition;

            Text text = CreateText(buttonObject.transform, "Label", label, font, rect.sizeDelta, Vector2.zero);
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private static Text CreateText(Transform parent, string name, string label, Font font, Vector2 size, Vector2 anchoredPosition)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.text = label;
            text.font = font;
            text.fontSize = 16;
            text.color = new Color(1f, 0.94f, 0.84f);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return text;
        }

        private static SenCityToastPresenter CreateToast(Transform parent, Font font)
        {
            Transform panel = CreatePanel(parent, "Toast", new Vector2(360f, 58f), new Vector2(0f, -74f));
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            Text message = CreateText(panel, "Message", string.Empty, font, rect.sizeDelta, Vector2.zero);
            message.alignment = TextAnchor.MiddleCenter;
            CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();
            SenCityToastPresenter toast = panel.gameObject.AddComponent<SenCityToastPresenter>();
            SetObjectReference(toast, "canvasGroup", group);
            SetObjectReference(toast, "messageText", message);
            return toast;
        }

        private static SenCityConfirmDialog CreateConfirmDialog(Transform parent, Font font)
        {
            Transform panel = CreatePanel(parent, "Store Confirm Dialog", new Vector2(360f, 150f), Vector2.zero);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            Text message = CreateText(panel, "Message", "Cat vao Kho?", font, new Vector2(320f, 56f), new Vector2(0f, 36f));
            message.alignment = TextAnchor.MiddleCenter;
            Button confirm = CreateButton(panel, "Confirm", "OK", font, new Vector2(-72f, -38f));
            Button cancel = CreateButton(panel, "Cancel", "Cancel", font, new Vector2(72f, -38f));
            CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();
            SenCityConfirmDialog dialog = panel.gameObject.AddComponent<SenCityConfirmDialog>();
            SetObjectReference(dialog, "canvasGroup", group);
            SetObjectReference(dialog, "messageText", message);
            SetObjectReference(dialog, "confirmButton", confirm);
            SetObjectReference(dialog, "cancelButton", cancel);
            return dialog;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = new Material(shader);

            material.name = name;
            material.color = color;
            return material;
        }

        private static void SetInventoryCatalog(FurnitureInventoryRuntime inventory, IReadOnlyList<FurnitureItemDefinition> items)
        {
            SerializedObject serialized = new SerializedObject(inventory);
            SerializedProperty catalog = serialized.FindProperty("catalog");
            catalog.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
                catalog.GetArrayElementAtIndex(i).objectReferenceValue = items[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
