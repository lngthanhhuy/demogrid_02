using SenCity.Core.Grid;
using UnityEngine;
using UnityEngine.AI;

namespace SenCity.Features.FurniturePlacement
{
    public class PlacedFurnitureObject : MonoBehaviour
    {
        [SerializeField] private FurnitureItemDefinition item;
        [SerializeField] private string instanceId;

        private FurnitureInstanceData data;
        private FurnitureSelectionHighlight selectionHighlight;
        private Renderer[] placementRenderers;
        private Collider[] placementColliders;
        private NavMeshObstacle[] placementObstacles;
        private bool[] rendererEnabledStates;
        private bool[] colliderEnabledStates;
        private bool[] obstacleEnabledStates;
        private bool placementVisible = true;

        public FurnitureItemDefinition Item => item;
        public FurnitureInstanceData Data => data;
        public string InstanceId => data?.InstanceId ?? instanceId;
        public bool IsSelected => selectionHighlight != null && selectionHighlight.IsSelected;
        public bool IsHovered => selectionHighlight != null && selectionHighlight.IsHovered;

        public void Initialize(FurnitureItemDefinition item, FurnitureInstanceData data, SenCityGridProfile gridProfile)
        {
            this.item = item;
            this.data = data;
            instanceId = data?.InstanceId;
            EnsureSelectionHighlight();
            SetSelected(false);
            ApplyPose(gridProfile);
        }

        public void SetSelected(bool selected)
        {
            EnsureSelectionHighlight();
            selectionHighlight.SetSelected(selected);
        }

        public void SetHovered(bool hovered)
        {
            EnsureSelectionHighlight();
            selectionHighlight.SetHovered(hovered);
        }

        public void ApplyPose(SenCityGridProfile gridProfile)
        {
            if (data == null || gridProfile == null)
                return;

            transform.position = gridProfile.FootprintCenter(data.OriginCell, data.Footprint, data.RotationDegrees);
            transform.rotation = Quaternion.Euler(0f, GridFootprint.NormalizeRotation(data.RotationDegrees), 0f);
        }

        public void SetPlacementVisible(bool visible)
        {
            if (placementVisible == visible)
                return;

            CachePlacementComponents();
            placementVisible = visible;
            for (int i = 0; i < placementRenderers.Length; i++)
            {
                if (placementRenderers[i] != null)
                    placementRenderers[i].enabled = visible && rendererEnabledStates[i];
            }

            for (int i = 0; i < placementColliders.Length; i++)
            {
                if (placementColliders[i] != null)
                    placementColliders[i].enabled = visible && colliderEnabledStates[i];
            }

            for (int i = 0; i < placementObstacles.Length; i++)
            {
                if (placementObstacles[i] != null)
                    placementObstacles[i].enabled = visible && obstacleEnabledStates[i];
            }
        }

        private void EnsureSelectionHighlight()
        {
            if (selectionHighlight != null)
                return;

            selectionHighlight = GetComponent<FurnitureSelectionHighlight>();
            if (selectionHighlight == null)
                selectionHighlight = gameObject.AddComponent<FurnitureSelectionHighlight>();
        }

        private void CachePlacementComponents()
        {
            if (placementRenderers != null)
                return;

            placementRenderers = GetComponentsInChildren<Renderer>(true);
            placementColliders = GetComponentsInChildren<Collider>(true);
            placementObstacles = GetComponentsInChildren<NavMeshObstacle>(true);
            rendererEnabledStates = new bool[placementRenderers.Length];
            colliderEnabledStates = new bool[placementColliders.Length];
            obstacleEnabledStates = new bool[placementObstacles.Length];

            for (int i = 0; i < placementRenderers.Length; i++)
                rendererEnabledStates[i] = placementRenderers[i] != null && placementRenderers[i].enabled;
            for (int i = 0; i < placementColliders.Length; i++)
                colliderEnabledStates[i] = placementColliders[i] != null && placementColliders[i].enabled;
            for (int i = 0; i < placementObstacles.Length; i++)
                obstacleEnabledStates[i] = placementObstacles[i] != null && placementObstacles[i].enabled;
        }
    }
}
