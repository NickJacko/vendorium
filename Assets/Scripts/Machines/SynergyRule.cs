using UnityEngine;

namespace Vendorium
{
    [CreateAssetMenu(menuName = "VendoriumData/SynergyRule", fileName = "New_SynergyRule")]
    public class SynergyRule : ScriptableObject
    {
        [Header("Identifikation")]
        public string RuleName;           // z.B. "Frühstücks-Duo"
        [TextArea] public string BonusDescription;

        [Header("Bedingung — BEIDE Maschinen müssen mindestens einen dieser Tags haben")]
        public string[] RequiredTagsA;    // z.B. { "coffee" }
        public string[] RequiredTagsB;    // z.B. { "snack" }

        [Header("Bonus")]
        [Range(1f, 3f)] public float IncomeMultiplier = 1.35f;
        public SynergyEffect SpecialEffect = SynergyEffect.IncomeBonus;

        [Header("Visual")]
        public Color LineColor = new Color(1f, 0.85f, 0f, 0.8f); // Gold
    }
}
