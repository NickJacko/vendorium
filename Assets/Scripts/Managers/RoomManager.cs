using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    public class RoomManager : Singleton<RoomManager>
    {
        [SerializeField] private List<RoomData> allRooms = new List<RoomData>();

        private HashSet<string> _unlockedRooms = new HashSet<string>();

        // Startraum ist immer freigeschaltet
        private void Start()
        {
            _unlockedRooms.Add("start_room");
        }

        public bool IsUnlocked(string roomId) => _unlockedRooms.Contains(roomId);

        public bool UnlockRoom(string roomId)
        {
            var data = GetRoomData(roomId);
            if (data == null)
            {
                Debug.LogWarning($"[RoomManager] Raum '{roomId}' nicht in der Datenbank.");
                return false;
            }

            if (IsUnlocked(roomId)) return true;

            if (!EconomyManager.Instance.SpendMoney(data.UnlockCost, $"Raum freischalten: {data.RoomName}"))
                return false;

            _unlockedRooms.Add(roomId);
            VendoriumEventManager.Instance?.TriggerRoomUnlocked(roomId);
            Debug.Log($"[RoomManager] Raum freigeschaltet: {data.RoomName}");
            return true;
        }

        public RoomData GetRoomData(string roomId)
        {
            foreach (var r in allRooms)
                if (r != null && r.RoomID == roomId) return r;
            return null;
        }

        public List<string> GetUnlockedRooms() => new List<string>(_unlockedRooms);

        public void LoadUnlockedRooms(List<string> rooms)
        {
            _unlockedRooms.Clear();
            foreach (var r in rooms) _unlockedRooms.Add(r);
        }
    }
}
