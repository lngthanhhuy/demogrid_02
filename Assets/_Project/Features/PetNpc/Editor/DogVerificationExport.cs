using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SenCity.Features.PetNpc.Editor
{
    public static class DogVerificationExport
    {
        private const string DogAsset = "Assets/_Project/Art/Pets/Dog/dog.fbx";
        private const string DogScene = "Assets/_Project/Features/PetNpc/Scenes/DogNpcDemo.unity";
        private static readonly string OutputRoot = "C:/Users/Thanh Huy/.codex/visualizations/2026/08/05/019fcfe9-51c4-7d51-84ec-b26cdf780305";

        [MenuItem("SenCity/Pet NPC/Export Dog Verification")]
        public static void Export()
        {
            Directory.CreateDirectory(OutputRoot);
            var importer = AssetImporter.GetAtPath(DogAsset) as ModelImporter;
            var loaded = AssetDatabase.LoadAllAssetsAtPath(DogAsset);
            var clips = loaded.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).Select(c => c.name).ToArray();
            var avatar = loaded.OfType<Avatar>().FirstOrDefault();

            EditorSceneManager.OpenScene(DogScene, OpenSceneMode.Single);
            var dog = GameObject.Find("Dog NPC");
            var agent = dog ? dog.GetComponent<NavMeshAgent>() : null;
            var animator = dog ? dog.GetComponentInChildren<Animator>() : null;
            var controller = animator ? animator.runtimeAnimatorController : null;
            var tri = NavMesh.CalculateTriangulation();
            var patrolRoot = GameObject.Find("Pet Patrol Points");
            int patrolCount = patrolRoot ? patrolRoot.transform.childCount : 0;

            var lines = new List<string>
            {
                "DOG UNITY VERIFICATION",
                $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"Asset: {DogAsset}",
                $"Scene: {DogScene}",
                "",
                "[RIG / MODEL IMPORTER]",
                $"Import Animation: {importer?.importAnimation}",
                $"Animation Type: {importer?.animationType}",
                $"Avatar Setup: {importer?.avatarSetup}",
                $"Avatar Found: {avatar != null}",
                $"Scale: {importer?.globalScale}",
                $"Animation Clips: {clips.Length}",
                $"Clip Names: {(clips.Length == 0 ? "(none detected)" : string.Join(", ", clips))}",
                "",
                "[ANIMATOR]",
                $"Animator Found: {animator != null}",
                $"Controller: {(controller ? controller.name : "(none)")}",
                $"Avatar: {(animator && animator.avatar ? animator.avatar.name : "(none)")}",
                $"Apply Root Motion: {(animator ? animator.applyRootMotion.ToString() : "N/A")}",
                $"Controller Clips: {(controller ? controller.animationClips.Length.ToString() : "0")}",
                "",
                "[NAVMESH AGENT]",
                $"Agent Found: {agent != null}",
                $"Radius: {(agent ? agent.radius.ToString("0.###") : "N/A")}",
                $"Height: {(agent ? agent.height.ToString("0.###") : "N/A")}",
                $"Speed: {(agent ? agent.speed.ToString("0.###") : "N/A")}",
                $"Angular Speed: {(agent ? agent.angularSpeed.ToString("0.###") : "N/A")}",
                $"Stopping Distance: {(agent ? agent.stoppingDistance.ToString("0.###") : "N/A")}",
                $"Patrol Points: {patrolCount}",
                $"NavMesh Vertices: {tri.vertices.Length}",
                $"NavMesh Triangles: {tri.indices.Length / 3}",
                "",
                "[REMAINING VALIDATION]",
                "Play Mode movement: NOT VERIFIED",
                "Inspector screenshots: exported as evidence cards",
                "Final integrated product shot: NOT VERIFIED"
            };

            string logPath = Path.Combine(OutputRoot, "dog_verification.log");
            File.WriteAllLines(logPath, lines);
            File.WriteAllText(Path.Combine(OutputRoot, "dog_verification.json"), JsonUtility.ToJson(new VerificationData(importer, avatar, clips, dog, agent, animator, controller, patrolCount, tri), true));
            Debug.Log(string.Join("\n", lines));
        }

        [Serializable]
        private sealed class VerificationData
        {
            public string asset = DogAsset;
            public string scene = DogScene;
            public bool importAnimation;
            public string animationType;
            public string avatarSetup;
            public bool avatarFound;
            public float globalScale;
            public string[] clips;
            public bool animatorFound;
            public string controller;
            public string animatorAvatar;
            public bool applyRootMotion;
            public int controllerClipCount;
            public bool agentFound;
            public float agentRadius;
            public float agentHeight;
            public float agentSpeed;
            public float agentAngularSpeed;
            public float stoppingDistance;
            public int patrolPoints;
            public int navMeshVertices;
            public int navMeshTriangles;

            public VerificationData(ModelImporter importer, Avatar avatar, string[] clipNames, GameObject dog, NavMeshAgent agent, Animator animator, RuntimeAnimatorController controller, int patrolCount, NavMeshTriangulation tri)
            {
                importAnimation = importer && importer.importAnimation;
                animationType = importer ? importer.animationType.ToString() : "Unknown";
                avatarSetup = importer ? importer.avatarSetup.ToString() : "Unknown";
                avatarFound = avatar != null;
                globalScale = importer ? importer.globalScale : 0f;
                clips = clipNames;
                animatorFound = animator != null;
                this.controller = controller ? controller.name : "";
                animatorAvatar = animator && animator.avatar ? animator.avatar.name : "";
                applyRootMotion = animator && animator.applyRootMotion;
                controllerClipCount = controller ? controller.animationClips.Length : 0;
                agentFound = agent != null;
                agentRadius = agent ? agent.radius : 0f;
                agentHeight = agent ? agent.height : 0f;
                agentSpeed = agent ? agent.speed : 0f;
                agentAngularSpeed = agent ? agent.angularSpeed : 0f;
                stoppingDistance = agent ? agent.stoppingDistance : 0f;
                patrolPoints = patrolCount;
                navMeshVertices = tri.vertices.Length;
                navMeshTriangles = tri.indices.Length / 3;
            }
        }
    }
}
