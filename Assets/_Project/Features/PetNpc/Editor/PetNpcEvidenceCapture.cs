using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SenCity.Features.PetNpc.Editor
{
    public static class PetNpcEvidenceCapture
    {
        private const string DemoScene = "Assets/_Project/Features/PetNpc/Scenes/PetNpcDemo.unity";
        private const string ModelPath = "Assets/_Project/Art/Pets/Cat/Cat.fbx";
        private static string OutputRoot => Path.GetFullPath("C:/Users/Thanh Huy/.codex/visualizations/2026/08/05/019fcfe9-51c4-7d51-84ec-b26cdf780305");

        [MenuItem("SenCity/Pet NPC/Capture Evidence")]
        public static void CaptureEvidence()
        {
            CaptureEvidenceFor(DemoScene, "Cat NPC", "pet_demo_scene.png", "pet_navmesh_agent.png", "pet_skeleton_import.png");
        }

        public static void CaptureDogEvidence()
        {
            CaptureEvidenceFor("Assets/_Project/Features/PetNpc/Scenes/DogNpcDemo.unity", "Dog NPC", "dog_demo_scene.png", "dog_navmesh_agent.png", "dog_skeleton_import.png");
        }

        private static void CaptureEvidenceFor(string scenePath, string npcName, string demoFile, string navmeshFile, string skeletonFile)
        {
            Directory.CreateDirectory(OutputRoot);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            CaptureScene(npcName, demoFile, true, false);
            CaptureScene(npcName, navmeshFile, true, false);
            CaptureScene(npcName, skeletonFile, false, true);
            Debug.Log($"Pet evidence captured to {OutputRoot}");
        }

        public static void CaptureSkeletonEvidence()
        {
            Directory.CreateDirectory(OutputRoot);
            EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Single);
            CaptureScene("Cat NPC", "pet_skeleton_import.png", false, true);
            Debug.Log($"Skeleton evidence captured to {OutputRoot}");
        }

        private static void CaptureScene(string npcName, string fileName, bool showNavMesh, bool showSkeleton)
        {
            var cat = GameObject.Find(npcName);
            if (!cat) throw new UnityException($"{npcName} not found in demo scene.");

            var temporary = new GameObject("__EvidenceTemporary");
            var cameraObject = new GameObject("Evidence Camera");
            cameraObject.transform.SetParent(temporary.transform);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.08f, 0.11f, 1f);
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            Bounds bounds = GetBounds(cat);
            Vector3 target = bounds.center + Vector3.up * (showSkeleton ? 0f : 0.15f);
            float extent = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float distance = showSkeleton ? Mathf.Max(1.2f, extent * 2.4f) : Mathf.Max(4.5f, extent * 5.5f);
            Vector3 direction = new Vector3(0.65f, 0.45f, -0.8f).normalized;
            camera.transform.position = target + direction * distance;
            LookAt(camera.transform, target);

            var lightObject = new GameObject("Evidence Light");
            lightObject.transform.SetParent(temporary.transform);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            if (showNavMesh) CreateNavMeshOverlay(temporary.transform);

            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            image.Apply();
            if (showSkeleton) DrawSkeletonOnImage(image, camera, cat.transform);
            File.WriteAllBytes(Path.Combine(OutputRoot, fileName), image.EncodeToPNG());
            Object.DestroyImmediate(image);
            RenderTexture.active = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(temporary);
        }

        private static void CreateNavMeshOverlay(Transform parent)
        {
            var tri = NavMesh.CalculateTriangulation();
            if (tri.vertices.Length == 0) return;
            var mesh = new Mesh { name = "Evidence NavMesh" };
            var vertices = new Vector3[tri.vertices.Length];
            for (int i = 0; i < tri.vertices.Length; i++) vertices[i] = tri.vertices[i] + Vector3.up * 0.035f;
            mesh.vertices = vertices;
            mesh.triangles = tri.indices;
            var go = new GameObject("Baked NavMesh (Evidence)");
            go.transform.SetParent(parent);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.1f, 0.85f, 0.2f, 0.42f) };
            renderer.sharedMaterial = material;
        }

        private static void CreateSkeletonOverlay(Transform root, Transform parent)
        {
            var material = new Material(Shader.Find("Unlit/Color")) { color = Color.red };
            foreach (var bone in root.GetComponentsInChildren<Transform>(true))
            {
                if (bone == root || !bone.parent || !bone.parent.IsChildOf(root)) continue;
                var lineObject = new GameObject("Bone");
                lineObject.transform.SetParent(parent);
                var line = lineObject.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.SetPosition(0, bone.position);
                line.SetPosition(1, bone.parent.position);
                line.startWidth = 0.025f;
                line.endWidth = 0.012f;
                line.material = material;
                line.useWorldSpace = true;
            }
        }

        private static void DrawSkeletonOnImage(Texture2D image, Camera camera, Transform root)
        {
            foreach (var bone in root.GetComponentsInChildren<Transform>(true))
            {
                if (bone == root || !bone.parent || !bone.parent.IsChildOf(root)) continue;
                Vector3 a = camera.WorldToScreenPoint(bone.position);
                Vector3 b = camera.WorldToScreenPoint(bone.parent.position);
                if (a.z <= 0f || b.z <= 0f) continue;
                DrawLine(image, (int)a.x, (int)a.y, (int)b.x, (int)b.y, Color.red, 3);
            }
            image.Apply();
        }

        private static void DrawLine(Texture2D image, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            while (true)
            {
                for (int ox = -thickness; ox <= thickness; ox++)
                    for (int oy = -thickness; oy <= thickness; oy++)
                        if (x0 + ox >= 0 && x0 + ox < image.width && y0 + oy >= 0 && y0 + oy < image.height)
                            image.SetPixel(x0 + ox, y0 + oy, color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * error;
                if (e2 >= dy) { error += dy; x0 += sx; }
                if (e2 <= dx) { error += dx; y0 += sy; }
            }
        }

        private static Bounds GetBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void LookAt(Transform source, Vector3 target)
        {
            source.rotation = Quaternion.LookRotation(target - source.position, Vector3.up);
        }
    }
}
