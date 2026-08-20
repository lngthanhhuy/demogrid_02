using System.IO;
using SenCity.Core.Grid;
using SenCity.Features.FurniturePlacement.Input;
using SenCity.Features.FurniturePlacement.Save;
using SenCity.Features.PetNpc;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SenCity.Features.FurniturePlacement.Editor
{
    public static class SenCityMvpSceneBuilder
    {
        public const int SceneVersion = 5;
        public const string SceneVersionPath = "Assets/_Project/Production/SenCityMvp.version.txt";
        private const string ScenePath = "Assets/_Project/Production/SenCityMvp.unity";
        private const string GridPath = "Assets/_Project/Features/FurniturePlacement/Data/SenCityPlacementGrid.asset";
        private const string CatalogPath = "Assets/_Project/Features/FurniturePlacement/Data/SenCityFurnitureCatalog.asset";
        private const string UiFolder = "Assets/_Project/Art/UI/SenCityFigma";
        private const string RoomImagePath = UiFolder + "/room-interior.png";
        private const string PetImagePath = UiFolder + "/pet-hero.png";
        private const string AvatarImagePath = UiFolder + "/avatar.png";
        private const string SettingsImagePath = UiFolder + "/settings.png";
        private const string FontPath = UiFolder + "/Inter-Variable.ttf";
        private const string RoundSpritePath = UiFolder + "/rounded-ui.png";
        private const string RenderTexturePath = UiFolder + "/StudioRoom.renderTexture";
        private const string CatModelPath = "Assets/_Project/Art/Pets/Cat/Cat.fbx";

        // Landscape UI design reference: 1280 x 720.
        private const float DesignWidth = 1280f;
        private const float DesignHeight = 720f;
        private const float HeaderHeight = 76f;
        private const float ContentPadding = 24f;
        private const float SidePanelWidth = 390f;
        private const float RoomWidth = 818f;
        private const float RoomHeight = 596f;

        private static readonly Color Background = Hex("DBA1BA");
        private static readonly Color TopPanel = Hex("E8B2C9");
        private static readonly Color Cream = Hex("FFE8C9");
        private static readonly Color PalePink = Hex("FFE8E8");
        private static readonly Color TabPink = Hex("FAD6DE");
        private static readonly Color Banner = Hex("FFE0D9");
        private static readonly Color Lime = Hex("B8D96B");
        private static readonly Color LimeText = Hex("4D632E");
        private static readonly Color Ink = Hex("542E42");
        private static readonly Color Muted = Hex("8C596E");
        private static readonly Color AccentText = Hex("854F63");
        private static readonly Color Wood = Hex("CF9969");
        private static readonly Color DarkWood = Hex("8F5C45");
        private static readonly Color MeterTrack = Hex("DBBAC2");
        private static readonly Color Hunger = Hex("D19E8C");

        private static Font uiFont;
        private static Sprite roundedSprite;

        [MenuItem("Tools/SEN CITY/MVP/Build Sen City MVP Scene")]
        public static void BuildMvpScene()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SenCityMvp";
            EnsureUiAssets();

            SenCityGridProfile grid = AssetDatabase.LoadAssetAtPath<SenCityGridProfile>(GridPath);
            FurnitureCatalogDefinition catalog = AssetDatabase.LoadAssetAtPath<FurnitureCatalogDefinition>(CatalogPath);
            if (grid == null || catalog == null)
                throw new FileNotFoundException("SEN CITY placement grid or furniture catalog is missing.");

            GameObject world = new GameObject("World3D");
            CreateLighting(world.transform);
            CreateRoom(world.transform, grid);
            RenderTexture roomTexture = EnsureRenderTexture();
            Camera roomCamera = CreateRoomCamera(world.transform, roomTexture);
            FurniturePlacementRuntime runtime = CreatePlacementSystem(world.transform, grid, catalog, roomCamera);
            PetNpcController petController = CreatePetSystem(world.transform);
            BuildNavigation(world);
            RectTransform interactionRect = BuildUiHierarchy(runtime, catalog, roomTexture, roomCamera, petController);

            FurniturePlacementInputAdapter input = runtime.GetComponent<FurniturePlacementInputAdapter>();
            SetObject(input, "interactionRect", interactionRect);
            SetBool(input, "confirmOnMouseRelease", false);
            RestorePlacementAssetReferences(runtime, grid, catalog);

            ConfigureAndroid();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            SenCityMvpSceneValidator.ValidateActiveScene();
            File.WriteAllText(SceneVersionPath, SceneVersion.ToString());
            AssetDatabase.ImportAsset(SceneVersionPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SenCityMvpSceneBuilder] Built landscape MVP scene at {ScenePath}");
        }

        private static void EnsureUiAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/UI"))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "UI");
            if (!AssetDatabase.IsValidFolder(UiFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art/UI", "SenCityFigma");

            if (!File.Exists(RoundSpritePath))
                CreateRoundedTexture(RoundSpritePath, 64, 16);

            AssetDatabase.Refresh();
            ConfigureSprite(RoomImagePath, Vector4.zero);
            ConfigureSprite(PetImagePath, Vector4.zero);
            ConfigureSprite(AvatarImagePath, Vector4.zero);
            ConfigureSprite(SettingsImagePath, Vector4.zero);
            ConfigureSprite(RoundSpritePath, new Vector4(16f, 16f, 16f, 16f));
            uiFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundSpritePath);
        }

        private static void ConfigureSprite(string path, Vector4 border)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static void CreateRoundedTexture(string path, int size, int radius)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nearestX = Mathf.Clamp(x, radius, size - radius - 1);
                float nearestY = Mathf.Clamp(y, radius, size - radius - 1);
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static RenderTexture EnsureRenderTexture()
        {
            RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (texture == null)
            {
                texture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32)
                {
                    name = "StudioRoom"
                };
                AssetDatabase.CreateAsset(texture, RenderTexturePath);
            }

            if (texture.width != 512 || texture.height != 512)
            {
                texture.Release();
                texture.width = 512;
                texture.height = 512;
            }

            texture.antiAliasing = 1;
            texture.useMipMap = false;
            EditorUtility.SetDirty(texture);
            return texture;
        }

        private static void CreateLighting(Transform parent)
        {
            GameObject lightObject = new GameObject("Sun Warm Directional Light");
            lightObject.transform.SetParent(parent);
            lightObject.transform.rotation = Quaternion.Euler(48f, -34f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.82f, 0.68f);
            light.intensity = 1.25f;

            GameObject fillObject = new GameObject("Room Fill Light");
            fillObject.transform.SetParent(parent);
            fillObject.transform.position = new Vector3(2f, 2.8f, 1.2f);
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(1f, 0.72f, 0.72f);
            fill.intensity = 2.2f;
            fill.range = 8f;
        }

        private static void CreateRoom(Transform parent, SenCityGridProfile grid)
        {
            GameObject room = new GameObject("Studio_01");
            room.transform.SetParent(parent);
            float width = grid.columns * grid.cellSize;
            float depth = grid.rows * grid.cellSize;
            CreateCube(room.transform, "Placement Floor", new Vector3(width * 0.5f, -0.04f, depth * 0.5f), new Vector3(width, 0.08f, depth), Hex("D5A06E"));
            CreateCube(room.transform, "Back Wall", new Vector3(width * 0.5f, 1.35f, depth), new Vector3(width, 2.8f, 0.08f), Hex("F4D7CE"));
            CreateCube(room.transform, "Left Wall", new Vector3(0f, 1.35f, depth * 0.5f), new Vector3(0.08f, 2.8f, depth), Hex("EABFC0"));
            CreateCube(room.transform, "Rug", new Vector3(width * 0.55f, 0.012f, depth * 0.52f), new Vector3(1.7f, 0.025f, 1.45f), Hex("B8C987"));
        }

        private static Camera CreateRoomCamera(Transform parent, RenderTexture target)
        {
            GameObject cameraObject = new GameObject("RoomCamera_RenderTexture");
            cameraObject.transform.SetParent(parent);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(2.15f, 5.5f, -3.8f);
            cameraObject.transform.rotation = Quaternion.Euler(51f, 0f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("D39A68");
            camera.fieldOfView = 42f;
            camera.targetTexture = target;
            return camera;
        }

        private static FurniturePlacementRuntime CreatePlacementSystem(
            Transform parent,
            SenCityGridProfile grid,
            FurnitureCatalogDefinition catalog,
            Camera roomCamera)
        {
            GameObject root = new GameObject("FurniturePlacement");
            root.transform.SetParent(parent);
            Transform placedRoot = new GameObject("PlacedFurniture").transform;
            Transform previewRoot = new GameObject("PlacementPreview").transform;
            placedRoot.SetParent(root.transform);
            previewRoot.SetParent(root.transform);

            FurniturePlacementController controller = root.AddComponent<FurniturePlacementController>();
            FurnitureInventoryRuntime inventory = root.AddComponent<FurnitureInventoryRuntime>();
            FurniturePlacementSaveService save = root.AddComponent<FurniturePlacementSaveService>();
            FurniturePlacementRuntime runtime = root.AddComponent<FurniturePlacementRuntime>();
            FurniturePlacementInputAdapter input = root.AddComponent<FurniturePlacementInputAdapter>();

            SetObject(controller, "gridProfile", grid);
            SetObject(inventory, "catalogAsset", catalog);
            SetObject(runtime, "controller", controller);
            SetObject(runtime, "inventory", inventory);
            SetObject(runtime, "saveService", save);
            SetObject(runtime, "gridProfile", grid);
            SetObject(runtime, "placedRoot", placedRoot);
            SetObject(runtime, "previewRoot", previewRoot);
            SetObject(input, "runtime", runtime);
            SetObject(input, "worldCamera", roomCamera);
            return runtime;
        }

        private static PetNpcController CreatePetSystem(Transform parent)
        {
            GameObject system = new GameObject("PetSystem");
            system.transform.SetParent(parent);
            GameObject pet = new GameObject("Mochi_PetAI");
            pet.transform.SetParent(system.transform);
            pet.transform.position = new Vector3(2.9f, 0f, 2.7f);
            NavMeshAgent agent = pet.AddComponent<NavMeshAgent>();
            agent.speed = 0.65f;
            agent.angularSpeed = 240f;
            agent.radius = 0.18f;
            agent.height = 0.55f;
            PetNpcController controller = pet.AddComponent<PetNpcController>();

            GameObject catAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CatModelPath);
            GameObject visual = catAsset != null ? PrefabUtility.InstantiatePrefab(catAsset) as GameObject : null;
            if (visual == null)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Mochi Cream", Hex("FFDEBD"));
            }
            visual.name = "Mochi Visual";
            visual.transform.SetParent(pet.transform, false);
            visual.transform.localScale = catAsset != null ? Vector3.one * 0.01f : Vector3.one * 0.38f;
            visual.transform.localPosition = catAsset != null ? Vector3.zero : new Vector3(0f, 0.35f, 0f);

            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty patrol = serialized.FindProperty("patrolPoints");
            patrol.arraySize = 4;
            Vector3[] positions =
            {
                new Vector3(0.8f, 0f, 0.9f), new Vector3(3.1f, 0f, 0.9f),
                new Vector3(3.1f, 0f, 3.1f), new Vector3(0.8f, 0f, 3.1f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject point = new GameObject($"PatrolPoint_{i + 1}");
                point.transform.SetParent(system.transform);
                point.transform.position = positions[i];
                patrol.GetArrayElementAtIndex(i).objectReferenceValue = point.transform;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Transform feedPoint = CreateWorldMarker(system.transform, "FeedPoint", new Vector3(0.65f, 0f, 3.25f));
            Transform playPoint = CreateWorldMarker(system.transform, "PlayPoint", new Vector3(2.05f, 0f, 2.05f));
            Transform sleepPoint = CreateWorldMarker(system.transform, "SleepPoint", new Vector3(3.35f, 0f, 3.25f));
            SetObject(controller, "feedPoint", feedPoint);
            SetObject(controller, "playPoint", playPoint);
            SetObject(controller, "sleepPoint", sleepPoint);
            return controller;
        }

        private static Transform CreateWorldMarker(Transform parent, string name, Vector3 position)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent);
            marker.transform.position = position;
            return marker.transform;
        }

        private static void RestorePlacementAssetReferences(
            FurniturePlacementRuntime runtime,
            SenCityGridProfile grid,
            FurnitureCatalogDefinition catalog)
        {
            SetObject(runtime, "gridProfile", grid);
            SetObject(runtime.GetComponent<FurniturePlacementController>(), "gridProfile", grid);
            SetObject(runtime.GetComponent<FurnitureInventoryRuntime>(), "catalogAsset", catalog);
        }

        private static void BuildNavigation(GameObject world)
        {
            NavMeshSurface surface = world.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
        }
        private static RectTransform BuildUiHierarchy(
            FurniturePlacementRuntime runtime,
            FurnitureCatalogDefinition catalog,
            RenderTexture roomTexture,
            Camera roomCamera,
            PetNpcController petController)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            GameObject canvasObject = new GameObject(
                "Canvas_MainStudio",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(DesignWidth, DesignHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject safeArea = CreateRect(canvasObject.transform, "SafeArea");
            Stretch(safeArea.GetComponent<RectTransform>());
            safeArea.AddComponent<SenCitySafeArea>();

            // CanvasScaler is the single source of truth for UI scaling.
            // Do NOT constrain this root to another 16:9 viewport:
            // AspectRatioFitter + SafeArea can create black bars and clip
            // the right-side panel on landscape Android devices.
            GameObject frame = CreateRect(safeArea.transform, "DesignFrame_1280x720");
            Stretch(frame.GetComponent<RectTransform>());

            GameObject screens = CreateRect(frame.transform, "ScreenStack");
            Stretch(screens.GetComponent<RectTransform>());

            GameObject main = BuildMainStudio(screens.transform);
            RectTransform interactionRect;
            GameObject build = BuildBuildDrawer(screens.transform, runtime, catalog, roomTexture, out interactionRect);
            GameObject pet = BuildPetCard(screens.transform);

            GameObject overlays = CreateRect(frame.transform, "OverlayStack");
            Stretch(overlays.GetComponent<RectTransform>());

            GameObject loading = BuildLoadingOverlay(overlays.transform);
            GameObject onboarding = BuildOnboardingOverlay(overlays.transform, out Button startButton);

            GameObject selectionPopup = BuildSelectionPopup(
                overlays.transform,
                out Text selectedItemName,
                out Text selectedItemDetails,
                out Button moveSelected,
                out Button rotateSelected,
                out Button deleteSelected,
                out Button closeSelection);

            GameObject deleteConfirmation = BuildDeleteConfirmation(
                overlays.transform,
                out Button confirmDelete,
                out Button cancelDelete);

            GameObject exitConfirmation = BuildExitConfirmation(
                overlays.transform,
                out Button confirmExit,
                out Button cancelExit);

            GameObject toast = CreatePanel(
                overlays.transform,
                "Overlay_Toast",
                Hex("59313F"),
                430f,
                650f,
                420f,
                44f);

            Text toastText = CreateText(
                toast.transform,
                "Text_ToastMessage",
                string.Empty,
                Color.white,
                11,
                10f,
                4f,
                400f,
                36f,
                TextAnchor.MiddleCenter,
                false);

            Button buildButton = main.transform.Find("Button_BuildShortcut")?.GetComponent<Button>();
            Button focusButton = main.transform.Find("Button_FocusSession")?.GetComponent<Button>();
            Button petButton = main.transform.Find("Button_PetShortcut")?.GetComponent<Button>();
            Button buildBackButton = build.transform.Find("Button_BackToStudio")?.GetComponent<Button>();
            Button petBackButton = pet.transform.Find("Button_BackToStudio")?.GetComponent<Button>();
            Button placeButton = build.transform.Find("Container_InventoryDrawer/Button_ConfirmPlacement")?.GetComponent<Button>();
            Button rotateButton = build.transform.Find("Container_InventoryDrawer/Button_RotatePlacement")?.GetComponent<Button>();
            Button cancelButton = build.transform.Find("Container_InventoryDrawer/Button_CancelPlacement")?.GetComponent<Button>();
            Text placementStatus = build.transform.Find("Container_InventoryDrawer/Text_PlacementStatus")?.GetComponent<Text>();
            Text petState = pet.transform.Find("Text_PetState")?.GetComponent<Text>();

            SenCityRoomCameraFocus cameraFocus = canvasObject.AddComponent<SenCityRoomCameraFocus>();
            SetObject(cameraFocus, "roomCamera", roomCamera);

            SenCityMvpFlowController flow = canvasObject.AddComponent<SenCityMvpFlowController>();
            SetObject(flow, "loadingPanel", loading);
            SetObject(flow, "onboardingPanel", onboarding);
            SetObject(flow, "mainStudioPanel", main);
            SetObject(flow, "buildDrawerPanel", build);
            SetObject(flow, "petCardPanel", pet);
            SetObject(flow, "selectionPanel", selectionPopup);
            SetObject(flow, "deleteConfirmationPanel", deleteConfirmation);
            SetObject(flow, "exitConfirmationPanel", exitConfirmation);
            SetObject(flow, "selectedItemNameText", selectedItemName);
            SetObject(flow, "selectedItemDetailsText", selectedItemDetails);
            SetObject(flow, "buildButton", buildButton);
            SetObject(flow, "focusButton", focusButton);
            SetObject(flow, "petButton", petButton);
            SetObject(flow, "buildBackButton", buildBackButton);
            SetObject(flow, "petBackButton", petBackButton);
            SetObject(flow, "startButton", startButton);
            SetObject(flow, "placeButton", placeButton);
            SetObject(flow, "rotateButton", rotateButton);
            SetObject(flow, "cancelButton", cancelButton);
            SetObject(flow, "moveSelectedButton", moveSelected);
            SetObject(flow, "rotateSelectedButton", rotateSelected);
            SetObject(flow, "deleteSelectedButton", deleteSelected);
            SetObject(flow, "closeSelectionButton", closeSelection);
            SetObject(flow, "confirmDeleteButton", confirmDelete);
            SetObject(flow, "cancelDeleteButton", cancelDelete);
            SetObject(flow, "placementStatusText", placementStatus);
            SetObject(flow, "feedButton", pet.transform.Find("Button_FeedPet")?.GetComponent<Button>());
            SetObject(flow, "playButton", pet.transform.Find("Button_PlayWithPet")?.GetComponent<Button>());
            SetObject(flow, "sleepButton", pet.transform.Find("Button_SleepPet")?.GetComponent<Button>());
            SetObject(flow, "petControllerSource", petController);
            SetObject(flow, "confirmExitButton", confirmExit);
            SetObject(flow, "cancelExitButton", cancelExit);
            SetObject(flow, "toastText", toastText);
            SetObject(flow, "petStateText", petState);
            SetObject(flow, "furnitureRuntime", runtime);
            SetObject(flow, "roomCameraFocus", cameraFocus);

            build.SetActive(false);
            pet.SetActive(false);
            loading.SetActive(true);
            onboarding.SetActive(false);
            selectionPopup.SetActive(false);
            deleteConfirmation.SetActive(false);
            exitConfirmation.SetActive(false);
            toast.SetActive(false);

            if (placeButton != null) placeButton.interactable = false;
            if (rotateButton != null) rotateButton.interactable = false;
            if (cancelButton != null) cancelButton.interactable = false;

            return interactionRect;
        }
        private static GameObject BuildMainStudio(Transform parent)
        {
            GameObject screen = CreatePanel(
                parent,
                "MainStudio",
                Background,
                0f,
                0f,
                DesignWidth,
                DesignHeight,
                false);

            // Header
            CreatePanel(screen.transform, "HeaderBackground", TopPanel, 0f, 0f, DesignWidth, HeaderHeight, false);

            CreateImage(
                screen.transform,
                "Image_Avatar",
                AssetDatabase.LoadAssetAtPath<Sprite>(AvatarImagePath),
                24f,
                18f,
                40f,
                40f);

            CreateText(
                screen.transform,
                "Text_AvatarInitial",
                "S",
                Hex("7A4A57"),
                17,
                24f,
                23f,
                40f,
                30f,
                TextAnchor.MiddleCenter,
                true);

            CreateText(
                screen.transform,
                "Text_StatusTime",
                "09:39 PM     Fri 6 May",
                Hex("73475C"),
                10,
                78f,
                16f,
                180f,
                20f,
                TextAnchor.MiddleLeft,
                true);

            CreateText(
                screen.transform,
                "Text_StudioHeaderTitle",
                "Studio 01",
                Ink,
                18,
                78f,
                37f,
                180f,
                26f,
                TextAnchor.MiddleLeft,
                true);

            CreatePanel(screen.transform, "Panel_CoinCurrency", Hex("FFEDD6"), 900f, 20f, 105f, 34f);
            CreateText(screen.transform, "Text_CoinAmount", "●  9,999", Ink, 11, 908f, 24f, 89f, 26f, TextAnchor.MiddleCenter, true);

            CreatePanel(screen.transform, "Panel_EnergyCurrency", Hex("FFEDD6"), 1015f, 20f, 105f, 34f);
            CreateText(screen.transform, "Text_EnergyAmount", "♧  9,999", Ink, 11, 1023f, 24f, 89f, 26f, TextAnchor.MiddleCenter, true);

            CreateImage(
                screen.transform,
                "Icon_Settings",
                AssetDatabase.LoadAssetAtPath<Sprite>(SettingsImagePath),
                1150f,
                18f,
                40f,
                40f);

            // Main content: large room on the left, information/actions on the right.
            float roomX = ContentPadding;
            float roomY = HeaderHeight + ContentPadding;
            float roomW = RoomWidth;
            float roomH = RoomHeight;

            CreatePanel(screen.transform, "Panel_RoomOuterFrame", DarkWood, roomX, roomY, roomW, roomH);
            CreatePanel(screen.transform, "Panel_RoomWoodFrame", Wood, roomX + 8f, roomY + 8f, roomW - 16f, roomH - 16f);

            CreateImage(
                screen.transform,
                "Image_RoomInterior",
                AssetDatabase.LoadAssetAtPath<Sprite>(RoomImagePath),
                roomX + 20f,
                roomY + 20f,
                roomW - 40f,
                roomH - 40f);

            float infoX = roomX + roomW + ContentPadding;
            float infoW = DesignWidth - infoX - ContentPadding;

            CreatePanel(screen.transform, "Panel_StudioInfo", Cream, infoX, roomY, infoW, roomH);

            CreatePanel(screen.transform, "Panel_FocusStreak", Banner, infoX + 18f, roomY + 18f, infoW - 36f, 36f);
            CreateText(
                screen.transform,
                "Text_FocusStreak",
                "✦  Good work — 60 days focused!  ✦",
                AccentText,
                10,
                infoX + 28f,
                roomY + 24f,
                infoW - 56f,
                24f,
                TextAnchor.MiddleCenter,
                true);

            CreateText(
                screen.transform,
                "Text_StudioTitle",
                "Studio 01",
                Ink,
                19,
                infoX + 20f,
                roomY + 72f,
                infoW - 40f,
                30f,
                TextAnchor.MiddleLeft,
                true);

            CreateText(
                screen.transform,
                "Text_StudioSubtitle",
                "Level 1  ·  Cozy beginnings",
                Muted,
                10,
                infoX + 20f,
                roomY + 103f,
                infoW - 40f,
                20f,
                TextAnchor.MiddleLeft,
                false);

            CreateText(
                screen.transform,
                "Text_MochiSummary",
                "Mochi",
                Ink,
                16,
                infoX + 20f,
                roomY + 150f,
                infoW - 40f,
                24f,
                TextAnchor.MiddleLeft,
                true);

            CreateText(
                screen.transform,
                "Text_MochiSummaryState",
                "Curious · happy · wants to play",
                Muted,
                9,
                infoX + 20f,
                roomY + 177f,
                infoW - 40f,
                20f,
                TextAnchor.MiddleLeft,
                false);

            CreateMeter(screen.transform, "Mood", infoX + 20f, roomY + 218f, 0.86f, Lime);
            CreateMeter(screen.transform, "Energy", infoX + 145f, roomY + 218f, 0.68f, Lime);

            CreateButton(
                screen.transform,
                "Button_BuildShortcut",
                "⌂  Build",
                Lime,
                LimeText,
                11,
                infoX + 20f,
                roomY + 278f,
                infoW - 40f,
                52f);

            CreateButton(
                screen.transform,
                "Button_FocusSession",
                "✦  Focus  ·  Start a calm session",
                Cream,
                AccentText,
                10,
                infoX + 20f,
                roomY + 340f,
                infoW - 40f,
                52f);

            CreateButton(
                screen.transform,
                "Button_PetShortcut",
                "♡  Mochi",
                TabPink,
                Ink,
                11,
                infoX + 20f,
                roomY + 402f,
                infoW - 40f,
                52f);

            CreateText(
                screen.transform,
                "Text_RoomHint",
                "Decorate your room and keep the session calm.",
                Muted,
                9,
                infoX + 20f,
                roomY + 476f,
                infoW - 40f,
                40f,
                TextAnchor.MiddleCenter,
                false);

            return screen;
        }
        private static GameObject BuildBuildDrawer(
            Transform parent,
            FurniturePlacementRuntime runtime,
            FurnitureCatalogDefinition catalog,
            RenderTexture roomTexture,
            out RectTransform interactionRect)
        {
            GameObject screen = CreatePanel(
                parent,
                "BuildDrawer",
                Background,
                0f,
                0f,
                DesignWidth,
                DesignHeight,
                false);

            CreatePanel(screen.transform, "HeaderBackground", TopPanel, 0f, 0f, DesignWidth, HeaderHeight, false);

            CreateText(
                screen.transform,
                "Text_PreviewTitle",
                "Decorate Studio 01",
                Ink,
                20,
                28f,
                17f,
                360f,
                30f,
                TextAnchor.MiddleLeft,
                true);

            CreateText(
                screen.transform,
                "Text_PreviewSubtitle",
                "Preview mode · drag on the room to place",
                Muted,
                10,
                28f,
                46f,
                430f,
                18f,
                TextAnchor.MiddleLeft,
                false);

            Button back = CreateButton(
                screen.transform,
                "Button_BackToStudio",
                "‹  Back",
                Cream,
                AccentText,
                11,
                1130f,
                18f,
                120f,
                40f);
            back.navigation = new Navigation { mode = Navigation.Mode.None };

            float roomX = ContentPadding;
            float roomY = HeaderHeight + ContentPadding;
            float roomW = RoomWidth;
            float roomH = RoomHeight;

            CreatePanel(screen.transform, "Panel_RoomPreview", Hex("F5E0C7"), roomX, roomY, roomW, roomH);
            CreatePanel(screen.transform, "Panel_RoomPreviewBorder", Wood, roomX + 8f, roomY + 8f, roomW - 16f, roomH - 16f);

            RawImage rawImage = CreateRawImage(
                screen.transform,
                "Image_Room3DViewport",
                roomTexture,
                roomX + 20f,
                roomY + 20f,
                roomW - 40f,
                roomH - 40f);

            interactionRect = rawImage.rectTransform;

            float drawerX = roomX + roomW + ContentPadding;
            float drawerW = DesignWidth - drawerX - ContentPadding;

            GameObject drawer = CreatePanel(
                screen.transform,
                "Container_InventoryDrawer",
                PalePink,
                drawerX,
                roomY,
                drawerW,
                roomH);

            CreatePanel(drawer.transform, "Panel_DrawerHandle", Hex("C794A1"), drawerW - 12f, 22f, 5f, 52f);

            CreateText(
                drawer.transform,
                "Text_DrawerTitle",
                "Furniture",
                Ink,
                18,
                20f,
                22f,
                drawerW - 40f,
                28f,
                TextAnchor.MiddleLeft,
                true);

            CreateText(
                drawer.transform,
                "Text_DrawerSubtitle",
                "42 items available",
                Muted,
                10,
                20f,
                51f,
                drawerW - 40f,
                20f,
                TextAnchor.MiddleLeft,
                false);

            float tabGap = 8f;
            float tabW = (drawerW - 40f - tabGap * 2f) / 3f;

            CreateButton(drawer.transform, "Button_FurnitureTab", "Furniture", Lime, LimeText, 9, 20f, 82f, tabW, 34f);
            CreateButton(drawer.transform, "Button_DecorationTab", "Decoration", TabPink, Ink, 9, 20f + tabW + tabGap, 82f, tabW, 34f);
            CreateButton(drawer.transform, "Button_WallTab", "Wall", TabPink, Ink, 9, 20f + (tabW + tabGap) * 2f, 82f, tabW, 34f);

            catalog.TryGetItem("demo_sofa", out FurnitureItemDefinition sofa);
            catalog.TryGetItem("demo_low_table", out FurnitureItemDefinition table);
            catalog.TryGetItem("demo_balcony_planter", out FurnitureItemDefinition plant);

            float cardGap = 10f;
            float cardW = (drawerW - 40f - cardGap * 2f) / 3f;

            CreateItemCard(drawer.transform, runtime, sofa, "Sofa", "◆ 1,200", "▰", 20f, 132f, cardW);
            CreateItemCard(drawer.transform, runtime, table, "Coffee Table", "◆ 680", "○", 20f + cardW + cardGap, 132f, cardW);
            CreateItemCard(drawer.transform, runtime, plant, "Plant", "◆ 420", "Leaf", 20f + (cardW + cardGap) * 2f, 132f, cardW);

            CreateText(
                drawer.transform,
                "Text_PlacementStatus",
                "Select an item, then drag it in the room.",
                Muted,
                9,
                20f,
                290f,
                drawerW - 40f,
                34f,
                TextAnchor.MiddleCenter,
                false);

            CreateButton(
                drawer.transform,
                "Button_ConfirmPlacement",
                "Confirm placement",
                Lime,
                LimeText,
                11,
                20f,
                338f,
                drawerW - 40f,
                48f);

            CreateButton(
                drawer.transform,
                "Button_RotatePlacement",
                "Rotate 90° (R)",
                TabPink,
                Ink,
                9,
                20f,
                400f,
                (drawerW - 50f) * 0.5f,
                38f);

            CreateButton(
                drawer.transform,
                "Button_CancelPlacement",
                "Cancel",
                TabPink,
                Ink,
                10,
                30f + (drawerW - 50f) * 0.5f,
                400f,
                (drawerW - 50f) * 0.5f,
                38f);

            CreateText(
                drawer.transform,
                "Text_TouchHint",
                "Drag • pinch to zoom • green is valid",
                Muted,
                8,
                20f,
                455f,
                drawerW - 40f,
                24f,
                TextAnchor.MiddleCenter,
                false);

            return screen;
        }
        private static GameObject BuildPetCard(Transform parent)
        {
            GameObject screen = CreatePanel(
                parent,
                "PetCard",
                Background,
                0f,
                0f,
                DesignWidth,
                DesignHeight,
                false);

            CreatePanel(screen.transform, "HeaderBackground", TopPanel, 0f, 0f, DesignWidth, HeaderHeight, false);

            CreateText(
                screen.transform,
                "Text_PetCardTitle",
                "Mochi · Pet Companion",
                Ink,
                20,
                28f,
                17f,
                400f,
                30f,
                TextAnchor.MiddleLeft,
                true);

            CreateText(
                screen.transform,
                "Text_PetCardSubtitle",
                "AI companion status and quick actions",
                Muted,
                10,
                28f,
                46f,
                400f,
                18f,
                TextAnchor.MiddleLeft,
                false);

            Button back = CreateButton(
                screen.transform,
                "Button_BackToStudio",
                "Back to Studio",
                Cream,
                AccentText,
                11,
                1080f,
                18f,
                170f,
                40f);
            back.navigation = new Navigation { mode = Navigation.Mode.None };

            float contentY = HeaderHeight + ContentPadding;
            float contentH = DesignHeight - contentY - ContentPadding;

            float heroX = ContentPadding;
            float heroW = 700f;

            CreatePanel(screen.transform, "Panel_PetHero", Cream, heroX, contentY, heroW, contentH);

            CreateImage(
                screen.transform,
                "Image_PetHero",
                AssetDatabase.LoadAssetAtPath<Sprite>(PetImagePath),
                heroX + 110f,
                contentY + 35f,
                480f,
                390f);

            CreateText(
                screen.transform,
                "Text_PetName",
                "Mochi",
                Ink,
                24,
                heroX + 40f,
                contentY + 425f,
                heroW - 80f,
                38f,
                TextAnchor.MiddleCenter,
                true);

            CreateText(
                screen.transform,
                "Text_PetMood",
                "Curious · happy · wants to play",
                Muted,
                11,
                heroX + 40f,
                contentY + 467f,
                heroW - 80f,
                24f,
                TextAnchor.MiddleCenter,
                false);

            float statusX = heroX + heroW + ContentPadding;
            float statusW = DesignWidth - statusX - ContentPadding;

            CreatePanel(screen.transform, "Panel_PetStatus", PalePink, statusX, contentY, statusW, 220f);

            CreateText(
                screen.transform,
                "Text_PetStatusTitle",
                "Mochi today",
                Ink,
                17,
                statusX + 20f,
                contentY + 22f,
                statusW - 40f,
                28f,
                TextAnchor.MiddleLeft,
                true);

            CreateText(
                screen.transform,
                "Text_PetStatusSubtitle",
                "Last interaction · 4 min ago",
                Muted,
                10,
                statusX + 20f,
                contentY + 53f,
                statusW - 40f,
                20f,
                TextAnchor.MiddleLeft,
                false);

            float meterW = (statusW - 60f) / 3f;
            CreateMeter(screen.transform, "Mood", statusX + 20f, contentY + 92f, 0.86f, Lime);
            CreateMeter(screen.transform, "Energy", statusX + 30f + meterW, contentY + 92f, 0.68f, Lime);
            CreateMeter(screen.transform, "Hunger", statusX + 40f + meterW * 2f, contentY + 92f, 0.48f, Hunger);

            CreateText(
                screen.transform,
                "Text_PetActionTitle",
                "What should Mochi do?",
                Ink,
                16,
                statusX,
                contentY + 250f,
                statusW,
                28f,
                TextAnchor.MiddleLeft,
                true);

            CreateButton(screen.transform, "Button_FeedPet", "♡  Feed", Lime, LimeText, 11, statusX, contentY + 300f, statusW, 48f);
            CreateButton(screen.transform, "Button_PlayWithPet", "✦  Play", Lime, LimeText, 11, statusX, contentY + 358f, statusW, 48f);
            CreateButton(screen.transform, "Button_SleepPet", "☾  Sleep", Lime, LimeText, 11, statusX, contentY + 416f, statusW, 48f);

            CreateText(
                screen.transform,
                "Text_PetState",
                "Ready for a calm session.",
                Muted,
                10,
                statusX,
                contentY + 474f,
                statusW,
                24f,
                TextAnchor.MiddleCenter,
                false);

            return screen;
        }
        private static GameObject BuildLoadingOverlay(Transform parent)
        {
            GameObject overlay = CreatePanel(parent, "Overlay_Loading", Background, 0f, 0f, DesignWidth, DesignHeight, false);

            CreateText(
                overlay.transform,
                "Text_BrandLogo",
                "SEN CITY",
                Ink,
                30,
                340f,
                260f,
                600f,
                60f,
                TextAnchor.MiddleCenter,
                true);

            CreateText(
                overlay.transform,
                "Text_LoadingStatus",
                "Preparing your cozy studio…",
                Muted,
                13,
                340f,
                330f,
                600f,
                34f,
                TextAnchor.MiddleCenter,
                false);

            return overlay;
        }
        private static GameObject BuildOnboardingOverlay(Transform parent, out Button startButton)
        {
            GameObject overlay = CreatePanel(parent, "Overlay_Onboarding", Background, 0f, 0f, DesignWidth, DesignHeight, false);

            CreatePanel(
                overlay.transform,
                "Panel_WelcomeCard",
                Cream,
                300f,
                150f,
                680f,
                420f);

            CreateText(
                overlay.transform,
                "Text_OnboardingTitle",
                "Welcome to Sen City",
                Ink,
                30,
                350f,
                220f,
                580f,
                55f,
                TextAnchor.MiddleCenter,
                true);

            CreateText(
                overlay.transform,
                "Text_OnboardingBody",
                "Decorate Studio 01, focus calmly, and care for Mochi.",
                Muted,
                14,
                390f,
                300f,
                500f,
                90f,
                TextAnchor.MiddleCenter,
                false);

            startButton = CreateButton(
                overlay.transform,
                "Button_StartStudio",
                "Start your studio",
                Lime,
                LimeText,
                13,
                490f,
                425f,
                300f,
                58f);

            return overlay;
        }
        private static GameObject BuildSelectionPopup(
            Transform parent,
            out Text itemName,
            out Text itemDetails,
            out Button moveButton,
            out Button rotateButton,
            out Button deleteButton,
            out Button closeButton)
        {
            GameObject shade = CreatePanel(
                parent,
                "Overlay_FurnitureSelection",
                new Color(0.18f, 0.09f, 0.14f, 0.32f),
                0f,
                0f,
                DesignWidth,
                DesignHeight,
                false);

            GameObject card = CreatePanel(
                shade.transform,
                "Panel_FurnitureSelectionCard",
                PalePink,
                360f,
                205f,
                560f,
                310f);

            CreateText(card.transform, "Text_SelectedFurnitureEyebrow", "SELECTED FURNITURE", Muted, 9, 28f, 22f, 500f, 18f, TextAnchor.MiddleLeft, true);
            itemName = CreateText(card.transform, "Text_SelectedFurnitureName", "Furniture", Ink, 22, 28f, 50f, 500f, 36f, TextAnchor.MiddleLeft, true);
            itemDetails = CreateText(card.transform, "Text_SelectedFurnitureDetails", "Grid position • Rotation", Muted, 10, 28f, 92f, 500f, 24f, TextAnchor.MiddleLeft, false);

            moveButton = CreateButton(card.transform, "Button_MoveSelectedFurniture", "Move", Lime, LimeText, 11, 28f, 145f, 150f, 46f);
            rotateButton = CreateButton(card.transform, "Button_RotateSelectedFurniture", "Rotate 90°", Lime, LimeText, 10, 205f, 145f, 150f, 46f);
            deleteButton = CreateButton(card.transform, "Button_DeleteSelectedFurniture", "Delete", TabPink, Ink, 11, 382f, 145f, 150f, 46f);
            closeButton = CreateButton(card.transform, "Button_CloseFurnitureSelection", "Close", Cream, AccentText, 11, 208f, 225f, 144f, 40f);

            return shade;
        }
        private static GameObject BuildDeleteConfirmation(
            Transform parent,
            out Button confirmButton,
            out Button cancelButton)
        {
            GameObject shade = CreatePanel(
                parent,
                "Overlay_DeleteFurnitureConfirmation",
                new Color(0.18f, 0.09f, 0.14f, 0.42f),
                0f,
                0f,
                DesignWidth,
                DesignHeight,
                false);

            GameObject card = CreatePanel(
                shade.transform,
                "Panel_DeleteFurnitureCard",
                Cream,
                390f,
                235f,
                500f,
                250f);

            CreateText(card.transform, "Text_DeleteFurnitureTitle", "Return this item to storage?", Ink, 20, 24f, 28f, 452f, 54f, TextAnchor.MiddleCenter, true);
            CreateText(card.transform, "Text_DeleteFurnitureBody", "Its position will be removed from Studio 01.", Muted, 11, 40f, 92f, 420f, 38f, TextAnchor.MiddleCenter, false);

            cancelButton = CreateButton(card.transform, "Button_CancelDeleteFurniture", "Keep it", TabPink, Ink, 11, 28f, 170f, 210f, 42f);
            confirmButton = CreateButton(card.transform, "Button_ConfirmDeleteFurniture", "Return item", Lime, LimeText, 11, 262f, 170f, 210f, 42f);

            return shade;
        }
        private static GameObject BuildExitConfirmation(
            Transform parent,
            out Button confirmButton,
            out Button cancelButton)
        {
            GameObject shade = CreatePanel(
                parent,
                "Overlay_ExitConfirmation",
                new Color(0.18f, 0.09f, 0.14f, 0.42f),
                0f,
                0f,
                DesignWidth,
                DesignHeight,
                false);

            GameObject card = CreatePanel(
                shade.transform,
                "Panel_ExitConfirmationCard",
                Cream,
                390f,
                245f,
                500f,
                230f);

            CreateText(card.transform, "Text_ExitConfirmationTitle", "Leave Sen City?", Ink, 22, 24f, 28f, 452f, 42f, TextAnchor.MiddleCenter, true);
            CreateText(card.transform, "Text_ExitConfirmationBody", "Your room is saved automatically.", Muted, 11, 40f, 86f, 420f, 30f, TextAnchor.MiddleCenter, false);

            cancelButton = CreateButton(card.transform, "Button_CancelExit", "Stay", TabPink, Ink, 11, 28f, 155f, 210f, 42f);
            confirmButton = CreateButton(card.transform, "Button_ConfirmExit", "Exit", Lime, LimeText, 11, 262f, 155f, 210f, 42f);

            return shade;
        }
        private static void CreateItemCard(
            Transform parent,
            FurniturePlacementRuntime runtime,
            FurnitureItemDefinition item,
            string label,
            string price,
            string glyph,
            float x,
            float y,
            float width)
        {
            string itemToken = ToNameToken(label);

            GameObject card = CreatePanel(
                parent,
                $"ItemCard_{itemToken}",
                TabPink,
                x,
                y,
                width,
                140f);

            CreateText(
                card.transform,
                $"Icon_{itemToken}",
                glyph,
                Ink,
                18,
                8f,
                10f,
                width - 16f,
                30f,
                TextAnchor.MiddleCenter,
                true);

            CreateText(
                card.transform,
                $"Text_{itemToken}Name",
                label,
                Ink,
                9,
                8f,
                45f,
                width - 16f,
                22f,
                TextAnchor.MiddleCenter,
                true);

            CreateText(
                card.transform,
                $"Text_{itemToken}Price",
                price,
                Muted,
                8,
                8f,
                67f,
                width - 16f,
                18f,
                TextAnchor.MiddleCenter,
                false);

            Button select = CreateButton(
                card.transform,
                $"Button_Select{itemToken}",
                "Select",
                Lime,
                LimeText,
                8,
                8f,
                94f,
                width - 16f,
                32f);

            FurnitureInventoryButton inventoryButton = select.gameObject.AddComponent<FurnitureInventoryButton>();
            inventoryButton.Configure(runtime, item, null);
            EditorUtility.SetDirty(inventoryButton);
        }


        private static void CreateMeter(Transform parent, string label, float x, float y, float amount, Color fillColor)
        {
            string meterToken = ToNameToken(label);
            CreateText(parent, $"Text_{meterToken}Label", label, Ink, 10, x, y, 80f, 18f, TextAnchor.MiddleLeft, true);
            CreatePanel(parent, $"Panel_{meterToken}MeterTrack", MeterTrack, x, y + 25f, 80f, 8f);
            CreatePanel(parent, $"Panel_{meterToken}MeterFill", fillColor, x, y + 25f, 80f * amount, 8f);
        }

        private static GameObject CreateRect(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color, float x, float y, float width, float height, bool rounded = true)
        {
            GameObject go = CreateRect(parent, name);
            Image image = go.AddComponent<Image>();
            image.color = color;
            if (rounded && roundedSprite != null)
            {
                image.sprite = roundedSprite;
                image.type = Image.Type.Sliced;
            }
            SetTopLeft(go.GetComponent<RectTransform>(), x, y, width, height);
            return go;
        }

        private static Button CreateButton(Transform parent, string name, string label, Color backgroundColor, Color textColor, int fontSize, float x, float y, float width, float height)
        {
            GameObject go = CreatePanel(parent, name, backgroundColor, x, y, width, height);
            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.12f);
            colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.08f);
            colors.disabledColor = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0.45f);
            button.colors = colors;
            Text text = CreateText(go.transform, ToButtonLabelName(name), label, textColor, fontSize, 0f, 0f, width, height, TextAnchor.MiddleCenter, true);
            text.raycastTarget = false;
            return button;
        }

        private static Text CreateText(Transform parent, string name, string value, Color color, int size, float x, float y, float width, float height, TextAnchor alignment, bool bold)
        {
            GameObject go = CreateRect(parent, name);
            Text text = go.AddComponent<Text>();
            text.text = value;
            text.font = uiFont;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            SetTopLeft(go.GetComponent<RectTransform>(), x, y, width, height);
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, float x, float y, float width, float height)
        {
            GameObject go = CreateRect(parent, name);
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = false;
            SetTopLeft(go.GetComponent<RectTransform>(), x, y, width, height);
            return image;
        }

        private static RawImage CreateRawImage(Transform parent, string name, Texture texture, float x, float y, float width, float height)
        {
            GameObject go = CreateRect(parent, name);
            RawImage image = go.AddComponent<RawImage>();
            image.texture = texture;
            SetTopLeft(go.GetComponent<RectTransform>(), x, y, width, height);
            return image;
        }

        private static string ToButtonLabelName(string buttonName)
        {
            const string prefix = "Button_";
            string token = buttonName.StartsWith(prefix)
                ? buttonName.Substring(prefix.Length)
                : buttonName;
            return "Text_" + token + "Label";
        }

        private static string ToNameToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Item";

            string[] parts = value.Split(' ');
            string token = string.Empty;
            foreach (string part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;
                token += char.ToUpperInvariant(part[0]) + part.Substring(1);
            }

            return string.IsNullOrEmpty(token) ? "Item" : token;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = MakeMaterial(name, color);
            return cube;
        }

        private static Material MakeMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(shader) { name = name, color = color };
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            if (target == null)
                return;
            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                return;
            property.objectReferenceValue = value;
            bool applied = serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            serialized.Update();
            SerializedProperty persistedProperty = serialized.FindProperty(propertyName);
            if (value != null && (persistedProperty == null || persistedProperty.objectReferenceValue == null))
                throw new InvalidDataException($"Could not assign {propertyName} on {target.name} from {AssetDatabase.GetAssetPath(value)} (ApplyModifiedProperties={applied}).");
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            if (target == null)
                return;
            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                return;
            property.boolValue = value;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
        private static void ConfigureAndroid()
        {
            // Android MVP uses landscape only.
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // Keep the project default in landscape as well as the autorotation flags.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Android,
                ScriptingImplementation.IL2CPP);

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.bundleVersion = "0.1.0";
        }

    }
}
