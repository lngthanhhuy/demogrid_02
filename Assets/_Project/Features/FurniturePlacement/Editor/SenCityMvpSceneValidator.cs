using System;
using System.Collections.Generic;
using System.Reflection;
using SenCity.Features.FurniturePlacement.Input;
using SenCity.Features.PetNpc;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SenCity.Features.FurniturePlacement.Editor
{
    public static class SenCityMvpSceneValidator
    {
        [MenuItem("Tools/SEN CITY/MVP/Validate Active MVP Scene")]
        public static void ValidateActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            var errors = new List<string>();

            RequireRoot(scene, "World3D", errors);
            RequireRoot(scene, "EventSystem", errors);
            GameObject canvas = RequireRoot(scene, "Canvas_UI", errors);

            SenCityMvpFlowController flow = canvas != null
                ? canvas.GetComponent<SenCityMvpFlowController>()
                : null;
            SenCityRoomCameraFocus cameraFocus = canvas != null
                ? canvas.GetComponent<SenCityRoomCameraFocus>()
                : null;
            if (flow == null)
                errors.Add("Canvas_UI is missing SenCityMvpFlowController.");
            if (cameraFocus == null)
                errors.Add("Canvas_UI is missing SenCityRoomCameraFocus.");

            if (flow != null)
            {
                RequireReferences(flow, errors,
                    "loadingPanel", "onboardingPanel", "mainStudioPanel", "buildDrawerPanel", "petCardPanel",
                    "selectionPanel", "deleteConfirmationPanel", "exitConfirmationPanel",
                    "buildButton", "petButton", "buildBackButton", "petBackButton", "startButton",
                    "placeButton", "rotateButton", "cancelButton",
                    "moveSelectedButton", "rotateSelectedButton", "deleteSelectedButton", "closeSelectionButton",
                    "confirmDeleteButton", "cancelDeleteButton", "placementStatusText",
                    "feedButton", "playButton", "sleepButton", "petControllerSource",
                    "confirmExitButton", "cancelExitButton", "toastText", "petStateText",
                    "furnitureRuntime", "roomCameraFocus");
            }

            FurniturePlacementRuntime runtime = FindInScene<FurniturePlacementRuntime>(scene);
            FurniturePlacementController placementController = FindInScene<FurniturePlacementController>(scene);
            FurnitureInventoryRuntime inventory = FindInScene<FurnitureInventoryRuntime>(scene);
            FurniturePlacementInputAdapter input = FindInScene<FurniturePlacementInputAdapter>(scene);
            PetNpcController pet = FindInScene<PetNpcController>(scene);
            if (runtime == null)
                errors.Add("FurniturePlacementRuntime is missing.");
            if (placementController == null)
                errors.Add("FurniturePlacementController is missing.");
            if (inventory == null)
                errors.Add("FurnitureInventoryRuntime is missing.");
            if (input == null)
                errors.Add("FurniturePlacementInputAdapter is missing.");
            if (pet == null)
                errors.Add("PetNpcController is missing.");

            if (runtime != null)
                RequireReferences(runtime, errors, "controller", "inventory", "saveService", "gridProfile", "placedRoot", "previewRoot");
            if (placementController != null)
                RequireReferences(placementController, errors, "gridProfile");
            if (inventory != null)
                RequireReferences(inventory, errors, "catalogAsset");
            if (input != null)
            {
                RequireReferences(input, errors, "runtime", "worldCamera", "interactionRect");
                SerializedProperty confirmOnRelease = new SerializedObject(input).FindProperty("confirmOnMouseRelease");
                if (confirmOnRelease == null || confirmOnRelease.boolValue)
                    errors.Add("Input must keep the preview active until the explicit Confirm button is tapped.");
            }

            if (pet != null)
                RequireReferences(pet, errors, "feedPoint", "playPoint", "sleepPoint");

            foreach (FurnitureInventoryButton inventoryButton in FindAllInScene<FurnitureInventoryButton>(scene))
                RequireReferences(inventoryButton, errors, "runtime", "item");

            if (errors.Count > 0)
                throw new BuildFailedException("SEN CITY MVP scene validation failed:\n- " + string.Join("\n- ", errors));

            Debug.Log("[SenCityMvpSceneValidator] PASS — hierarchy, Inspector references, placement controls, camera focus and PET commands are connected.");
        }

        private static GameObject RequireRoot(Scene scene, string name, ICollection<string> errors)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
            }

            errors.Add($"Missing scene root: {name}.");
            return null;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }

            return null;
        }

        private static IEnumerable<T> FindAllInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T component in root.GetComponentsInChildren<T>(true))
                    yield return component;
            }
        }

        private static void RequireReferences(UnityEngine.Object target, ICollection<string> errors, params string[] propertyNames)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.Update();
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                FieldInfo field = target.GetType().GetField(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                UnityEngine.Object directValue = field?.GetValue(target) as UnityEngine.Object;
                if (property == null)
                    errors.Add($"{target.name} ({target.GetType().Name}).{propertyName} is not serialized.");
                else if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                    errors.Add($"{target.name} ({target.GetType().Name}).{propertyName} is not assigned in Inspector (direct value: {(directValue == null ? "null" : directValue.name)}).");
            }
        }
    }
}
