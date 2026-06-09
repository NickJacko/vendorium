using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    [CreateAssetMenu(menuName = "VendoriumData/RoomData", fileName = "New_RoomData")]
    public class RoomData : ScriptableObject
    {
        public string RoomID;             // z.B. "hinterraum"
        public string RoomName;           // z.B. "Hinterraum"
        [TextArea] public string Description;
        public int UnlockCost;
        public Vector2Int Size;           // in Metern
        public List<string> ConnectedRoomIDs = new List<string>();
    }
}
