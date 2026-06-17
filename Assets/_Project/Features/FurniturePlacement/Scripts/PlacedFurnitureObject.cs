using SenCity.Core.Grid;
using UnityEngine;

namespace SenCity.Features.FurniturePlacement
{
    public class PlacedFurnitureObject : MonoBehaviour
    {
        [SerializeField] private FurnitureItemDefinition item;
        [SerializeField] private string instanceId;

        private FurnitureInstanceData data;
        private FurnitureSelectionHighlight selectionHighlight;

        public FurnitureItemDefinition Item => item;
        public FurnitureInstanceData Data => data;
        public string InstanceId => data?.InstanceId ?? instanceId;

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

        public void ApplyPose(SenCityGridProfile gridProfile)
        {
            if (data == null || gridProfile == null)
                return;

            transform.position = gridProfile.FootprintCenter(data.OriginCell, data.Footprint, data.RotationDegrees);
            transform.rotation = Quaternion.Euler(0f, GridFootprint.NormalizeRotation(data.RotationDegrees), 0f);
        }

        private void EnsureSelectionHighlight()
        {
            if (selectionHighlight != null)
                return;

            selectionHighlight = GetComponent<FurnitureSelectionHighlight>();
            if (selectionHighlight == null)
                selectionHighlight = gameObject.AddComponent<FurnitureSelectionHighlight>();
        }
    }
}
