using UnityEngine;

namespace Vendorium
{
    [CreateAssetMenu(menuName = "VendoriumData/PlayerStats", fileName = "PlayerStats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Bewegung")]
        public float WalkSpeed = 4f;
        public float RunSpeed = 7f;
        public float JumpHeight = 1.2f;

        [Header("Kamera")]
        [Range(0.1f, 10f)] public float MouseSensitivity = 2f;
        public float MinVerticalAngle = -80f;
        public float MaxVerticalAngle = 80f;

        [Header("Interaktion")]
        public float InteractionRange = 2.5f;
        public LayerMask InteractableLayers;

        [Header("Head Bob")]
        public float BobFrequency = 2f;
        public float BobAmplitude = 0.05f;
    }
}
