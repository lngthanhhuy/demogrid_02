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
        public const int SceneVersion = 4;
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
            Debug.Log($"[SenCityMvpSceneBuilder] Built portrait MVP scene at {ScenePath}");
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

            GameObject canvasObject = new GameObject("Canvas_UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            GameObject safeArea = CreateRect(canvasObject.transform, "SafeArea");
            Stretch(safeArea.GetComponent<RectTransform>());
            safeArea.AddComponent<SenCitySafeArea>();

            GameObject frame = CreateRect(safeArea.transform, "DesignFrame_390x844");
            Stretch(frame.GetComponent<RectTransform>());
            AspectRatioFitter fitter = frame.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 390f / 844f;
            frame.AddComponent<RectMask2D>();

            GameObject screens = CreateRect(frame.transform, "Screens");
            Stretch(screens.GetComponent<RectTransform>());
            GameObject main = BuildMainStudio(screens.transform);
            RectTransform interactionRect;
            GameObject build = BuildBuildDrawer(screens.transform, runtime, catalog, roomTexture, out interactionRect);
            GameObject pet = BuildPetCard(screens.transform);

            GameObject overlays = CreateRect(frame.transform, "Overlays");
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
            GameObject toast = CreatePanel(overlays.transform, "Toast", Hex("59313F"), 45f, 774f, 300f, 44f);
            Text toastText = CreateText(toast.transform, "Message", string.Empty, Color.white, 11, 10f, 4f, 280f, 36f, TextAnchor.MiddleCenter, false);

            Button buildButton = main.transform.Find("BuildShortcut")?.GetComponent<Button>();
            Button focusButton = main.transform.Find("FocusButton")?.GetComponent<Button>();
            Button petButton = main.transform.Find("PetShortcut")?.GetComponent<Button>();
            Button buildBackButton = build.transform.Find("BackButton")?.GetComponent<Button>();
            Button petBackButton = pet.transform.Find("BackToStudio")?.GetComponent<Button>();
            Button placeButton = build.transform.Find("Drawer/PlaceSelectedItem")?.GetComponent<Button>();
            Button rotateButton = build.transform.Find("Drawer/Rotate")?.GetComponent<Button>();
            Button cancelButton = build.transform.Find("Drawer/Cancel")?.GetComponent<Button>();
            Text placementStatus = build.transform.Find("Drawer/PlacementStatus")?.GetComponent<Text>();
            Text petState = pet.transform.Find("PetState")?.GetComponent<Text>();

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
            SetObject(flow, "feedButton", pet.transform.Find("Feed")?.GetComponent<Button>());
            SetObject(flow, "playButton", pet.transform.Find("Play")?.GetComponent<Button>());
            SetObject(flow, "sleepButton", pet.transform.Find("Sleep")?.GetComponent<Button>());
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
            GameObject screen = CreatePanel(parent, "MainStudioScreen", Background, 0f, 0f, 390f, 844f, false);
            CreatePanel(screen.transform, "TopGradientPanel", TopPanel, 0f, 0f, 390f, 150f, false);
            CreateText(screen.transform, "StatusTime", "09:39 PM     Fri 6 May", Hex("73475C"), 11, 22f, 15f, 180f, 22f, TextAnchor.MiddleLeft, true);
            CreateImage(screen.transform, "Avatar", AssetDatabase.LoadAssetAtPath<Sprite>(AvatarImagePath), 22f, 50f, 40f, 40f);
            CreateText(screen.transform, "AvatarInitial", "S", Hex("7A4A57"), 17, 22f, 55f, 40f, 30f, TextAnchor.MiddleCenter, true);
            CreatePanel(screen.transform, "CoinPill", Hex("FFEDD6"), 126f, 52f, 102f, 34f);
            CreateText(screen.transform, "CoinText", "●  9,999", Ink, 12, 136f, 56f, 82f, 26f, TextAnchor.MiddleCenter, true);
            CreatePanel(screen.transform, "EnergyPill", Hex("FFEDD6"), 237f, 52f, 102f, 34f);
            CreateText(screen.transform, "EnergyText", "♧  9,999", Ink, 12, 247f, 56f, 82f, 26f, TextAnchor.MiddleCenter, true);
            CreateImage(screen.transform, "Settings", AssetDatabase.LoadAssetAtPath<Sprite>(SettingsImagePath), 347f, 51f, 36f, 36f);
            CreatePanel(screen.transform, "FocusBanner", Banner, 28f, 112f, 334f, 28f);
            CreateText(screen.transform, "FocusBannerText", "✦  Good work — 60 days focused!  ✦", AccentText, 11, 40f, 114f, 310f, 24f, TextAnchor.MiddleCenter, true);
            CreateText(screen.transform, "StudioTitle", "Studio 01", Ink, 20, 28f, 168f, 334f, 30f, TextAnchor.MiddleLeft, true);
            CreateText(screen.transform, "StudioSubtitle", "Level 1  ·  Cozy beginnings", Muted, 11, 28f, 197f, 334f, 20f, TextAnchor.MiddleLeft, false);
            CreatePanel(screen.transform, "RoomOuterFrame", DarkWood, 25f, 232f, 340f, 430f);
            CreatePanel(screen.transform, "RoomWoodFrame", Wood, 32f, 239f, 326f, 416f);
            CreateImage(screen.transform, "RoomInterior", AssetDatabase.LoadAssetAtPath<Sprite>(RoomImagePath), 43f, 250f, 304f, 394f);
            CreateButton(screen.transform, "BuildShortcut", "⌂\nHome", Cream, AccentText, 10, 28f, 688f, 64f, 54f);
            CreateButton(screen.transform, "FocusButton", "✦  Focus\nStart a calm session", Lime, LimeText, 12, 103f, 685f, 184f, 62f);
            CreateButton(screen.transform, "PetShortcut", "Paw\nPet", Cream, AccentText, 10, 296f, 688f, 66f, 54f);
            return screen;
        }

        private static GameObject BuildBuildDrawer(
            Transform parent,
            FurniturePlacementRuntime runtime,
            FurnitureCatalogDefinition catalog,
            RenderTexture roomTexture,
            out RectTransform interactionRect)
        {
            GameObject screen = CreatePanel(parent, "BuildDrawerScreen", Background, 0f, 0f, 390f, 844f, false);
            CreatePanel(screen.transform, "RoomPreview", Hex("F5E0C7"), 28f, 28f, 334f, 390f);
            CreatePanel(screen.transform, "RoomPreviewBorder", Wood, 38f, 38f, 314f, 370f);
            CreateText(screen.transform, "PreviewTitle", "Studio 01", Ink, 18, 50f, 52f, 240f, 28f, TextAnchor.MiddleLeft, true);
            CreateText(screen.transform, "PreviewSubtitle", "Preview mode · drag on the room to place", Muted, 10, 50f, 82f, 280f, 20f, TextAnchor.MiddleLeft, false);
            Button back = CreateButton(screen.transform, "BackButton", "‹", Cream, AccentText, 22, 306f, 50f, 34f, 34f);
            back.navigation = new Navigation { mode = Navigation.Mode.None };
            RawImage rawImage = CreateRawImage(screen.transform, "Room3DViewport", roomTexture, 48f, 108f, 294f, 288f);
            interactionRect = rawImage.rectTransform;

            GameObject drawer = CreatePanel(screen.transform, "Drawer", PalePink, 0f, 365f, 390f, 479f);
            CreatePanel(drawer.transform, "Handle", Hex("C794A1"), 169f, 17f, 52f, 5f);
            CreateText(drawer.transform, "Title", "Decorate your Studio", Ink, 19, 26f, 37f, 334f, 30f, TextAnchor.MiddleLeft, true);
            CreateText(drawer.transform, "Subtitle", "42 items available", Muted, 11, 26f, 67f, 334f, 18f, TextAnchor.MiddleLeft, false);
            CreateButton(drawer.transform, "FurnitureTab", "Furniture", Lime, LimeText, 11, 26f, 104f, 105f, 34f);
            CreateButton(drawer.transform, "DecorationTab", "Decoration", TabPink, Ink, 10, 139f, 104f, 105f, 34f);
            CreateButton(drawer.transform, "WallTab", "Wall", TabPink, Ink, 11, 252f, 104f, 105f, 34f);

            catalog.TryGetItem("demo_sofa", out FurnitureItemDefinition sofa);
            catalog.TryGetItem("demo_low_table", out FurnitureItemDefinition table);
            catalog.TryGetItem("demo_balcony_planter", out FurnitureItemDefinition plant);
            CreateItemCard(drawer.transform, runtime, sofa, "Sofa", "◆ 1,200", "▰", 26f);
            CreateItemCard(drawer.transform, runtime, table, "Coffee Table", "◆ 680", "○", 133f);
            CreateItemCard(drawer.transform, runtime, plant, "Plant", "◆ 420", "Leaf", 240f);
            CreateText(drawer.transform, "PlacementStatus", "Tap a card or select furniture in the room.", Muted, 10, 36f, 302f, 318f, 18f, TextAnchor.MiddleCenter, false);
            CreateButton(drawer.transform, "PlaceSelectedItem", "Confirm placement", Lime, LimeText, 12, 96f, 329f, 198f, 48f);
            CreateButton(drawer.transform, "Rotate", "Rotate 90° (R)", TabPink, Ink, 9, 26f, 392f, 92f, 34f);
            CreateButton(drawer.transform, "Cancel", "Cancel", TabPink, Ink, 10, 272f, 392f, 92f, 34f);
            CreateText(drawer.transform, "TouchHint", "Drag • pinch to zoom • green is valid", Muted, 8, 116f, 398f, 160f, 24f, TextAnchor.MiddleCenter, false);
            return screen;
        }

        private static GameObject BuildPetCard(Transform parent)
        {
            GameObject screen = CreatePanel(parent, "PetCardScreen", Background, 0f, 0f, 390f, 844f, false);
            CreateText(screen.transform, "Title", "Your little neighbor", Ink, 20, 28f, 25f, 334f, 32f, TextAnchor.MiddleLeft, true);
            CreateText(screen.transform, "Subtitle", "AI companion status and quick actions", Muted, 11, 28f, 55f, 334f, 22f, TextAnchor.MiddleLeft, false);
            CreatePanel(screen.transform, "PetHeroCard", Cream, 24f, 100f, 342f, 232f);
            CreateImage(screen.transform, "PetHero", AssetDatabase.LoadAssetAtPath<Sprite>(PetImagePath), 109f, 121f, 172f, 172f);
            CreateText(screen.transform, "PetName", "Mochi", Ink, 18, 130f, 270f, 130f, 28f, TextAnchor.MiddleCenter, true);
            CreateText(screen.transform, "PetMood", "Curious · happy · wants to play", Muted, 11, 70f, 298f, 250f, 20f, TextAnchor.MiddleCenter, false);

            CreatePanel(screen.transform, "StatusCard", PalePink, 24f, 355f, 342f, 144f);
            CreateText(screen.transform, "StatusTitle", "Mochi today", Ink, 15, 44f, 371f, 280f, 24f, TextAnchor.MiddleLeft, true);
            CreateText(screen.transform, "StatusSubtitle", "Last interaction · 4 min ago", Muted, 10, 44f, 397f, 280f, 18f, TextAnchor.MiddleLeft, false);
            CreateMeter(screen.transform, "Mood", 44f, 428f, 0.86f, Lime);
            CreateMeter(screen.transform, "Energy", 151f, 428f, 0.68f, Lime);
            CreateMeter(screen.transform, "Hunger", 258f, 428f, 0.48f, Hunger);
            CreateText(screen.transform, "ActionTitle", "What should Mochi do?", Ink, 15, 28f, 538f, 334f, 26f, TextAnchor.MiddleLeft, true);
            CreateText(screen.transform, "PetState", "Ready for a calm session.", Muted, 10, 28f, 652f, 334f, 24f, TextAnchor.MiddleCenter, false);
            CreateButton(screen.transform, "Feed", "♡\nFeed", Lime, LimeText, 11, 28f, 575f, 88f, 70f);
            CreateButton(screen.transform, "Play", "✦\nPlay", Lime, LimeText, 11, 126f, 575f, 88f, 70f);
            CreateButton(screen.transform, "Sleep", "☾\nSleep", Lime, LimeText, 11, 224f, 575f, 88f, 70f);
            CreateButton(screen.transform, "BackToStudio", "Back to Studio", TabPink, Ink, 13, 93f, 692f, 204f, 50f);
            return screen;
        }

        private static GameObject BuildLoadingOverlay(Transform parent)
        {
            GameObject overlay = CreatePanel(parent, "LoadingPanel", Background, 0f, 0f, 390f, 844f, false);
            CreateText(overlay.transform, "Logo", "SEN CITY", Ink, 24, 45f, 330f, 300f, 50f, TextAnchor.MiddleCenter, true);
            CreateText(overlay.transform, "Loading", "Preparing your cozy studio…", Muted, 12, 45f, 390f, 300f, 30f, TextAnchor.MiddleCenter, false);
            return overlay;
        }

        private static GameObject BuildOnboardingOverlay(Transform parent, out Button startButton)
        {
            GameObject overlay = CreatePanel(parent, "OnboardingPanel", Background, 0f, 0f, 390f, 844f, false);
            CreatePanel(overlay.transform, "WelcomeCard", Cream, 28f, 210f, 334f, 360f);
            CreateText(overlay.transform, "Title", "Welcome to Sen City", Ink, 24, 48f, 260f, 294f, 50f, TextAnchor.MiddleCenter, true);
            CreateText(overlay.transform, "Body", "Decorate Studio 01, focus calmly, and care for Mochi.", Muted, 13, 58f, 330f, 274f, 90f, TextAnchor.MiddleCenter, false);
            startButton = CreateButton(overlay.transform, "Start", "Start your studio", Lime, LimeText, 13, 96f, 470f, 198f, 54f);
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
            GameObject shade = CreatePanel(parent, "FurnitureSelectionPopup", new Color(0.18f, 0.09f, 0.14f, 0.32f), 0f, 0f, 390f, 844f, false);
            GameObject card = CreatePanel(shade.transform, "Card", PalePink, 36f, 244f, 318f, 256f);
            CreateText(card.transform, "Eyebrow", "SELECTED FURNITURE", Muted, 9, 24f, 20f, 240f, 18f, TextAnchor.MiddleLeft, true);
            itemName = CreateText(card.transform, "ItemName", "Furniture", Ink, 20, 24f, 45f, 240f, 32f, TextAnchor.MiddleLeft, true);
            itemDetails = CreateText(card.transform, "ItemDetails", "Grid position • Rotation", Muted, 10, 24f, 79f, 270f, 24f, TextAnchor.MiddleLeft, false);
            moveButton = CreateButton(card.transform, "Move", "Move", Lime, LimeText, 11, 24f, 122f, 82f, 42f);
            rotateButton = CreateButton(card.transform, "Rotate", "Rotate 90°", Lime, LimeText, 10, 118f, 122f, 82f, 42f);
            deleteButton = CreateButton(card.transform, "Delete", "Delete", TabPink, Ink, 11, 212f, 122f, 82f, 42f);
            closeButton = CreateButton(card.transform, "Close", "Close", Cream, AccentText, 11, 87f, 187f, 144f, 40f);
            return shade;
        }

        private static GameObject BuildDeleteConfirmation(
            Transform parent,
            out Button confirmButton,
            out Button cancelButton)
        {
            GameObject shade = CreatePanel(parent, "DeleteFurnitureConfirmation", new Color(0.18f, 0.09f, 0.14f, 0.42f), 0f, 0f, 390f, 844f, false);
            GameObject card = CreatePanel(shade.transform, "Card", Cream, 40f, 292f, 310f, 200f);
            CreateText(card.transform, "Title", "Return this item to storage?", Ink, 18, 24f, 25f, 262f, 54f, TextAnchor.MiddleCenter, true);
            CreateText(card.transform, "Body", "Its position will be removed from Studio 01.", Muted, 11, 30f, 82f, 250f, 38f, TextAnchor.MiddleCenter, false);
            cancelButton = CreateButton(card.transform, "Cancel", "Keep it", TabPink, Ink, 11, 24f, 139f, 120f, 40f);
            confirmButton = CreateButton(card.transform, "Confirm", "Return item", Lime, LimeText, 11, 166f, 139f, 120f, 40f);
            return shade;
        }

        private static GameObject BuildExitConfirmation(
            Transform parent,
            out Button confirmButton,
            out Button cancelButton)
        {
            GameObject shade = CreatePanel(parent, "ExitConfirmation", new Color(0.18f, 0.09f, 0.14f, 0.42f), 0f, 0f, 390f, 844f, false);
            GameObject card = CreatePanel(shade.transform, "Card", Cream, 40f, 300f, 310f, 188f);
            CreateText(card.transform, "Title", "Leave Sen City?", Ink, 20, 24f, 29f, 262f, 40f, TextAnchor.MiddleCenter, true);
            CreateText(card.transform, "Body", "Your room is saved automatically.", Muted, 11, 30f, 76f, 250f, 30f, TextAnchor.MiddleCenter, false);
            cancelButton = CreateButton(card.transform, "Cancel", "Stay", TabPink, Ink, 11, 24f, 126f, 120f, 40f);
            confirmButton = CreateButton(card.transform, "Confirm", "Exit", Lime, LimeText, 11, 166f, 126f, 120f, 40f);
            return shade;
        }

        private static void CreateItemCard(
            Transform parent,
            FurniturePlacementRuntime runtime,
            FurnitureItemDefinition item,
            string label,
            string price,
            string glyph,
            float x)
        {
            GameObject card = CreatePanel(parent, $"ItemCard_{label.Replace(" ", string.Empty)}", TabPink, x, 162f, 101f, 135f);
            CreateText(card.transform, "Icon", glyph, Ink, 22, 10f, 9f, 81f, 32f, TextAnchor.MiddleCenter, true);
            CreateText(card.transform, "Name", label, Ink, 10, 11f, 49f, 79f, 18f, TextAnchor.MiddleLeft, true);
            CreateText(card.transform, "Price", price, Muted, 9, 11f, 75f, 79f, 16f, TextAnchor.MiddleLeft, false);
            Button select = CreateButton(card.transform, "Select", "Select", Lime, LimeText, 9, 11f, 103f, 79f, 22f);
            FurnitureInventoryButton inventoryButton = select.gameObject.AddComponent<FurnitureInventoryButton>();
            inventoryButton.Configure(runtime, item, null);
            EditorUtility.SetDirty(inventoryButton);
        }

        private static void CreateMeter(Transform parent, string label, float x, float y, float amount, Color fillColor)
        {
            CreateText(parent, $"{label}Label", label, Ink, 10, x, y, 80f, 18f, TextAnchor.MiddleLeft, true);
            CreatePanel(parent, $"{label}Track", MeterTrack, x, y + 25f, 80f, 8f);
            CreatePanel(parent, $"{label}Fill", fillColor, x, y + 25f, 80f * amount, 8f);
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
            Text text = CreateText(go.transform, "Text", label, textColor, fontSize, 0f, 0f, width, height, TextAnchor.MiddleCenter, true);
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
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.bundleVersion = "0.1.0";
        }
    }
}
