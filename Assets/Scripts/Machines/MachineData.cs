using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    [CreateAssetMenu(menuName = "VendoriumData/MachineData", fileName = "New_MachineData")]
    public class MachineData : ScriptableObject
    {
        [Header("Identifikation")]
        public string MachineName = "Neuer Automat";
        [TextArea] public string Description;
        public string MachineID;  // Eindeutige ID, z.B. "coffee_alte_hilde"

        [Header("Preise & Wirtschaft")]
        public int PurchasePrice = 500;
        public float BaseIncomePerSale = 2.5f;
        public float BaseSaleInterval = 8f;  // Sekunden zwischen Verkäufen
        public int MaxStock = 20;

        [Header("Upgrade-Stufen (0 = Basis, 4 = Max)")]
        public UpgradeLevel[] UpgradeLevels = new UpgradeLevel[4];

        [Header("Kunden-Präferenzen")]
        public CustomerType[] PrimaryCustomerTypes;
        public string[] SynergyTags;  // z.B. "coffee", "snack", "drink"

        [Header("Persönlichkeit")]
        public MachineTrait PersonalityTrait = MachineTrait.Reliable;

        [Header("Visuals")]
        public Sprite Icon;
        public GameObject Prefab;

        // Berechnet Stats auf einem bestimmten Upgrade-Level
        public float GetIncomeAtLevel(int level)
        {
            float multiplier = 1f;
            for (int i = 0; i < Mathf.Min(level, UpgradeLevels.Length); i++)
                multiplier *= UpgradeLevels[i].IncomeMultiplier;
            return BaseIncomePerSale * multiplier;
        }

        public float GetIntervalAtLevel(int level)
        {
            float multiplier = 1f;
            for (int i = 0; i < Mathf.Min(level, UpgradeLevels.Length); i++)
                multiplier *= UpgradeLevels[i].SaleIntervalMultiplier;
            return BaseSaleInterval * multiplier;
        }

        public int GetUpgradeCost(int targetLevel)
        {
            if (targetLevel <= 0 || targetLevel > UpgradeLevels.Length) return 0;
            return UpgradeLevels[targetLevel - 1].UpgradeCost;
        }
    }

    [Serializable]
    public class UpgradeLevel
    {
        public string UpgradeName;
        public int UpgradeCost;
        [Range(1f, 3f)] public float IncomeMultiplier = 1.2f;
        [Range(0.5f, 1f)] public float SaleIntervalMultiplier = 0.9f; // Unter 1 = schneller
        public int VisualVariantIndex = 0;
        public string UnlockedFeature;  // z.B. "Doppelter Stock"
    }

    [CreateAssetMenu(menuName = "VendoriumData/MachineDatabase", fileName = "MachineDatabase")]
    public class MachineDatabase : ScriptableObject
    {
        public List<MachineData> AllMachines = new List<MachineData>();

        public MachineData GetById(string id)
        {
            foreach (var m in AllMachines)
                if (m != null && m.MachineID == id) return m;
            return null;
        }
    }
}
