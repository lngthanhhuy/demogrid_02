using System;
using System.IO;
using SenCity.Features.FurniturePlacement;
using UnityEngine;

namespace SenCity.Core.Save
{
    public class FurniturePlacementSaveService : MonoBehaviour
    {
        [SerializeField] private string fileName = "sen_city_room_layout.json";

        public string SavePath => Path.Combine(Application.persistentDataPath, fileName);

        public bool Save(FurnitureRoomLayoutSnapshot roomLayout, FurnitureInventorySnapshot inventory)
        {
            try
            {
                var payload = new FurniturePlacementSavePayload
                {
                    roomLayout = roomLayout ?? new FurnitureRoomLayoutSnapshot(),
                    inventory = inventory ?? new FurnitureInventorySnapshot(),
                    savedAtUtc = DateTime.UtcNow.ToString("O")
                };
                File.WriteAllText(SavePath, JsonUtility.ToJson(payload, true));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FurniturePlacementSaveService] Save failed: {ex.Message}");
                return false;
            }
        }

        public bool TryLoad(out FurniturePlacementSavePayload payload)
        {
            payload = null;
            try
            {
                if (!File.Exists(SavePath))
                    return false;

                payload = JsonUtility.FromJson<FurniturePlacementSavePayload>(File.ReadAllText(SavePath));
                return payload != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FurniturePlacementSaveService] Load failed: {ex.Message}");
                return false;
            }
        }
    }

    [Serializable]
    public class FurniturePlacementSavePayload
    {
        public string savedAtUtc;
        public FurnitureRoomLayoutSnapshot roomLayout = new FurnitureRoomLayoutSnapshot();
        public FurnitureInventorySnapshot inventory = new FurnitureInventorySnapshot();
    }
}
