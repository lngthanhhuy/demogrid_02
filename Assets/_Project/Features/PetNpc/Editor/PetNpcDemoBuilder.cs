using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using SenCity.Features.PetNpc;

namespace SenCity.Features.PetNpc.Editor
{
    public static class PetNpcDemoBuilder
    {
        private const string SourceScene = "Assets/_Project/Features/FurniturePlacement/Scenes/SenCityFurniturePlacementPrototype.unity";
        private const string CatModel = "Assets/_Project/Art/Pets/Cat/Cat.fbx";
        private const string DogModel = "Assets/_Project/Art/Pets/Dog/dog.fbx";

        [MenuItem("SenCity/Pet NPC/Create Demo")]
        public static void CreateDemo()
        {
            BuildDemo(CatModel, "Assets/_Project/Features/PetNpc/Scenes/PetNpcDemo.unity", "Cat NPC");
        }

        [MenuItem("SenCity/Pet NPC/Create Dog Demo")]
        public static void CreateDogDemo()
        {
            BuildDemo(DogModel, "Assets/_Project/Features/PetNpc/Scenes/DogNpcDemo.unity", "Dog NPC");
        }

        private static void BuildDemo(string modelPath, string demoScene, string npcName)
        {
            EnsureFolder("Assets/_Project/Features/PetNpc/Scenes");
            var scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene, demoScene);

            var surfaceObject = new GameObject("Pet NPC NavMesh Surface");
            var surface = surfaceObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;

            var pointsRoot = new GameObject("Pet Patrol Points");
            Vector3[] positions = { new(-2f, 0.2f, -2f), new(2f, 0.2f, -2f), new(2f, 0.2f, 2f), new(-2f, 0.2f, 2f) };
            var points = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var point = new GameObject($"Patrol Point {i + 1}").transform;
                point.SetParent(pointsRoot.transform);
                point.position = positions[i];
                points[i] = point;
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (!model) throw new UnityException($"Missing pet model: {modelPath}");
            var pet = (GameObject)PrefabUtility.InstantiatePrefab(model);
            pet.name = npcName;
            pet.transform.position = new Vector3(0f, 0.2f, -2f);
            var agent = pet.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.2f;
            agent.speed = 1.6f;
            agent.angularSpeed = 360f;
            agent.stoppingDistance = 0.2f;
            agent.baseOffset = 0f;
            var collider = pet.GetComponent<Collider>() ?? pet.AddComponent<CapsuleCollider>();
            if (collider is CapsuleCollider capsule) { capsule.height = 1.1f; capsule.radius = 0.3f; capsule.center = new Vector3(0f, 0.55f, 0f); }
            var controller = pet.AddComponent<PetNpcController>();
            SerializedObject so = new(controller);
            so.FindProperty("patrolPoints").arraySize = points.Length;
            for (int i = 0; i < points.Length; i++) so.FindProperty("patrolPoints").GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            surface.BuildNavMesh();
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = pet;
            Debug.Log($"Pet NPC demo created: {npcName} patrols the furniture placement scene.");
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
