using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SenCity.Core.DamageVisual.Editor
{
    public static class DamageVisualPetSleepingMatBuilder
    {
        private const string SourceFbxPath = "Assets/Pet_Sleeping_Mat_Oval/fn_furn_pet_sleeping_mat_oval_01.fbx";
        private const string SourceTexturePath = "Assets/Pet_Sleeping_Mat_Oval/fn_furn_pet_sleeping_mat_oval_01_BC.png";

        private const string ArtFolder = "Assets/_Project/Art/Furniture/PetSleepingMatOval";
        private const string DataFolder = "Assets/_Project/Features/FurniturePlacement/Data";
        private const string SceneFolder = "Assets/_Project/Features/FurniturePlacement/Scenes";

        private const string MaterialPath = ArtFolder + "/mat_pet_sleeping_mat_oval.mat";
        private const string ProfilePath = DataFolder + "/DamageVisualProfile_PetSleepingMatOval.asset";
        private const string PrefabPath = ArtFolder + "/fn_furn_pet_sleeping_mat_oval_01.prefab";
        private const string DemoScenePath = SceneFolder + "/DamageVisual_PetSleepingMatDemo.unity";

        private const string RootObjectName = "[FURNITURE - LOT 2] Pet Sleeping Mat Oval";
        private const string ModelChildName = "Model";
        private const string EffectAnchorName = "VFX_Damage";

        [MenuItem("Tools/SEN CITY/Damage Visual/Build Pet Sleeping Mat Oval Prefab")]
        public static void BuildPrefabOnly()
        {
            if (!TryLoadSourceAssets(out GameObject sourceFbx, out Texture2D sourceTexture))
                return;

            EnsureFolders();
            Material material = EnsureMaterial(sourceTexture);
            DamageVisualProfile profile = EnsureProfile();
            GameObject prefab = EnsurePrefab(sourceFbx, material, profile);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[DamageVisualPetSleepingMatBuilder] Prefab built at {PrefabPath}");
        }

        [MenuItem("Tools/SEN CITY/Damage Visual/Build Pet Sleeping Mat Oval Demo")]
        public static void BuildDemo()
        {
            if (!TryLoadSourceAssets(out GameObject sourceFbx, out Texture2D sourceTexture))
                return;

            EnsureFolders();
            Material material = EnsureMaterial(sourceTexture);
            DamageVisualProfile profile = EnsureProfile();
            GameObject prefab = EnsurePrefab(sourceFbx, material, profile);
            EnsureDemoScene(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log(
                "[DamageVisualPetSleepingMatBuilder] Build complete.\n" +
                $"Material: {MaterialPath}\n" +
                $"Profile: {ProfilePath}\n" +
                $"Prefab: {PrefabPath}\n" +
                $"Scene: {DemoScenePath}");
        }

        private static bool TryLoadSourceAssets(out GameObject sourceFbx, out Texture2D sourceTexture)
        {
            sourceFbx = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbxPath);
            sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SourceTexturePath);

            if (sourceFbx == null)
            {
                EditorUtility.DisplayDialog(
                    "Damage Visual Builder",
                    $"Missing source FBX:\n{SourceFbxPath}",
                    "OK");
                return false;
            }

            if (sourceTexture == null)
            {
                EditorUtility.DisplayDialog(
                    "Damage Visual Builder",
                    $"Missing base color texture:\n{SourceTexturePath}",
                    "OK");
                return false;
            }

            return true;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project/Art/Furniture", "PetSleepingMatOval");
            EnsureFolder("Assets/_Project/Features/FurniturePlacement", "Data");
            EnsureFolder("Assets/_Project/Features/FurniturePlacement", "Scenes");
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string path = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        private static Material EnsureMaterial(Texture2D baseColorTexture)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = "mat_pet_sleeping_mat_oval" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", baseColorTexture);
            else if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", baseColorTexture);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static DamageVisualProfile EnsureProfile()
        {
            DamageVisualProfile profile = AssetDatabase.LoadAssetAtPath<DamageVisualProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<DamageVisualProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            SerializedObject serialized = new SerializedObject(profile);
            ConfigureStateSettings(serialized.FindProperty("normal"), useTintFallback: false, tintColor: Color.white);
            ConfigureStateSettings(
                serialized.FindProperty("damaged"),
                useTintFallback: true,
                tintColor: new Color(0.85f, 0.65f, 0.45f, 1f));
            ConfigureStateSettings(
                serialized.FindProperty("destroyed"),
                useTintFallback: true,
                tintColor: new Color(0.35f, 0.25f, 0.2f, 1f));
            serialized.FindProperty("transitionDuration").floatValue = 0.25f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigureStateSettings(
            SerializedProperty stateProperty,
            bool useTintFallback,
            Color tintColor)
        {
            stateProperty.FindPropertyRelative("textureOverride").objectReferenceValue = null;
            stateProperty.FindPropertyRelative("damageMask").objectReferenceValue = null;
            stateProperty.FindPropertyRelative("useTintFallback").boolValue = useTintFallback;
            stateProperty.FindPropertyRelative("tintColor").colorValue = tintColor;
        }

        private static GameObject EnsurePrefab(
            GameObject sourceFbx,
            Material material,
            DamageVisualProfile profile)
        {
            GameObject root = new GameObject(RootObjectName);

            GameObject modelInstance = PrefabUtility.InstantiatePrefab(sourceFbx, root.transform) as GameObject;
            if (modelInstance == null)
                throw new UnityException("Failed to instantiate source FBX as nested model child.");

            modelInstance.name = ModelChildName;
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>(true);
            ApplyMaterial(renderers, material);

            Transform effectAnchor = CreateEffectAnchor(root.transform, renderers);

            BoxCollider interactionCollider = root.AddComponent<BoxCollider>();
            FitBoxCollider(interactionCollider, renderers, root.transform);

            DamageVisualPrefabSetup prefabSetup = root.AddComponent<DamageVisualPrefabSetup>();
            prefabSetup.Configure(
                modelInstance.transform,
                material,
                renderers,
                interactionCollider,
                effectAnchor);

            ConfigureDamageComponents(root, profile, renderers);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Transform CreateEffectAnchor(Transform root, Renderer[] renderers)
        {
            GameObject anchorObject = new GameObject(EffectAnchorName);
            anchorObject.transform.SetParent(root, false);

            if (renderers != null && renderers.Length > 0)
            {
                Bounds bounds = CalculateCombinedBounds(renderers);
                Vector3 localPosition = root.InverseTransformPoint(bounds.center);
                localPosition.y = root.InverseTransformPoint(bounds.max).y + 0.02f;
                anchorObject.transform.localPosition = localPosition;
            }

            return anchorObject.transform;
        }

        private static void ApplyMaterial(Renderer[] renderers, Material material)
        {
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = material;

                renderer.sharedMaterials = materials;
            }
        }

        private static void ConfigureDamageComponents(
            GameObject root,
            DamageVisualProfile profile,
            Renderer[] renderers)
        {
            DamageState damageState = root.AddComponent<DamageState>();
            DamageVisualApplier visualApplier = root.AddComponent<DamageVisualApplier>();
            DamageStateVisualBinder binder = root.AddComponent<DamageStateVisualBinder>();

            SerializedObject damageStateObject = new SerializedObject(damageState);
            SerializedProperty thresholds = damageStateObject.FindProperty("thresholds");
            thresholds.FindPropertyRelative("damagedThreshold").floatValue = 25f;
            thresholds.FindPropertyRelative("destroyedThreshold").floatValue = 75f;
            damageStateObject.FindProperty("currentDamage").floatValue = 0f;
            damageStateObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject applierObject = new SerializedObject(visualApplier);
            applierObject.FindProperty("profile").objectReferenceValue = profile;
            SerializedProperty rendererProperty = applierObject.FindProperty("targetRenderers");
            rendererProperty.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++)
                rendererProperty.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            applierObject.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject binderObject = new SerializedObject(binder);
            binderObject.FindProperty("damageState").objectReferenceValue = damageState;
            binderObject.FindProperty("visualApplier").objectReferenceValue = visualApplier;
            binderObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(damageState);
            EditorUtility.SetDirty(visualApplier);
            EditorUtility.SetDirty(binder);
        }

        private static void FitBoxCollider(BoxCollider collider, Renderer[] renderers, Transform root)
        {
            if (renderers == null || renderers.Length == 0)
                return;

            Bounds worldBounds = CalculateCombinedBounds(renderers);
            collider.center = root.InverseTransformPoint(worldBounds.center);
            Vector3 worldSize = worldBounds.size;
            Vector3 localSize = new Vector3(
                Mathf.Abs(root.InverseTransformVector(new Vector3(worldSize.x, 0f, 0f)).x),
                Mathf.Abs(root.InverseTransformVector(new Vector3(0f, worldSize.y, 0f)).y),
                Mathf.Abs(root.InverseTransformVector(new Vector3(0f, 0f, worldSize.z)).z));
            collider.size = localSize;
        }

        private static Bounds CalculateCombinedBounds(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void EnsureDemoScene(GameObject prefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "DamageVisual_PetSleepingMatDemo";

            CreateLighting();
            CreateCamera();
            CreateGround();

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance != null)
            {
                instance.transform.position = Vector3.zero;
                instance.transform.rotation = Quaternion.identity;
            }

            CreateSceneDebugController(instance);

            EditorSceneManager.SaveScene(scene, DemoScenePath);
        }

        private static void CreateSceneDebugController(GameObject damageTarget)
        {
            if (damageTarget == null)
                return;

            DamageState damageState = damageTarget.GetComponent<DamageState>();
            if (damageState == null)
                return;

            GameObject debugObject = new GameObject("Damage Debug Runtime");
            DamageStateDebugController debugController = debugObject.AddComponent<DamageStateDebugController>();

            SerializedObject debugObjectSerialized = new SerializedObject(debugController);
            debugObjectSerialized.FindProperty("damageState").objectReferenceValue = damageState;
            debugObjectSerialized.FindProperty("damagePerApply").floatValue = 10f;
            debugObjectSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Sun Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.88f);
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Demo Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1.2f, -2.2f);
            cameraObject.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.Skybox;
        }

        private static void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Demo Ground";
            ground.transform.position = new Vector3(0f, -0.01f, 0f);
            ground.transform.localScale = new Vector3(0.4f, 1f, 0.4f);

            Renderer renderer = ground.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material groundMaterial = new Material(shader)
            {
                color = new Color(0.55f, 0.55f, 0.55f)
            };
            renderer.sharedMaterial = groundMaterial;
        }
    }
}
