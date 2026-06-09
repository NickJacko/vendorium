using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    [CreateAssetMenu(menuName = "VendoriumData/CustomerData", fileName = "New_CustomerData")]
    public class CustomerData : ScriptableObject
    {
        [Header("Identifikation")]
        public string CustomerTypeName = "Unbekannter Typ";
        public CustomerType Type = CustomerType.Schueler;

        [Header("Bewegung")]
        [Range(1.0f, 2.5f)] public float WalkSpeed = 1.5f;

        [Header("Kauf-Verhalten")]
        public string[] PreferredMachineTags;  // z.B. "snack", "drink"
        [Range(0f, 1f)] public float PurchaseProbability = 0.7f;
        [Range(1, 5)] public int MaxPurchasesPerVisit = 2;
        public float StayDuration = 60f;  // Sekunden im Laden

        [Header("Spawn-Häufigkeit")]
        [Range(1, 10)] public int SpawnWeight = 5;  // Höher = häufiger

        [Header("Visuals")]
        public GameObject Prefab;
        public Color CharacterColor = Color.white;
    }
}
