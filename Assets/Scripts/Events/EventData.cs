using UnityEngine;

namespace Vendorium
{
    [CreateAssetMenu(menuName = "VendoriumData/EventData", fileName = "New_EventData")]
    public class EventData : ScriptableObject
    {
        [Header("Identifikation")]
        public string EventName;
        [TextArea] public string Description;
        public EventType Type = EventType.Positive;
        public Sprite Icon;

        [Header("Trigger")]
        [Range(0f, 1f)] public float Probability = 0.3f;

        [Header("Dauer (Spielminuten)")]
        public float Duration = 3f;

        [Header("Effekte")]
        [Range(0.1f, 5f)] public float IncomeMultiplier = 1f;
        [Range(0.1f, 5f)] public float SpawnRateMultiplier = 1f;
        public int SpecificCustomerCount = 0;   // für Schulausflug (20 Schüler)
        public CustomerType SpecificCustomerType = CustomerType.Schueler;
        public float ReputationChange = 0f;
        public bool PowerOutage = false;
    }
}
