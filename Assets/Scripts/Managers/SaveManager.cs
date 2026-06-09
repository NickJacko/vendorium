using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

namespace Vendorium
{
    public class SaveManager : Singleton<SaveManager>
    {
        private const int SAVE_SLOT_COUNT = 3;
        private const float AUTO_SAVE_INTERVAL = 60f;
        private const string SAVE_FILE_PREFIX = "vendorium_save_slot";
        private const string BACKUP_SUFFIX = "_backup";

        private string SaveDirectory => Application.persistentDataPath;

        private void Start()
        {
            StartCoroutine(AutoSaveRoutine());
        }

        // --- Speichern ---

        public void SaveGame(int slot = 0)
        {
            if (slot < 0 || slot >= SAVE_SLOT_COUNT)
            {
                Debug.LogError($"[SaveManager] Ungültiger Slot: {slot}");
                return;
            }

            SaveData data = CollectSaveData();
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            string path = GetSavePath(slot);

            // Backup der alten Datei erstellen
            if (File.Exists(path))
                File.Copy(path, path + BACKUP_SUFFIX, overwrite: true);

            try
            {
                File.WriteAllText(path, json);
                Debug.Log($"[SaveManager] Spiel gespeichert in Slot {slot}: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Fehler beim Speichern: {e.Message}");
            }
        }

        // --- Laden ---

        public bool LoadGame(int slot = 0)
        {
            string path = GetSavePath(slot);

            if (!File.Exists(path))
            {
                Debug.Log($"[SaveManager] Kein Speicherstand in Slot {slot} gefunden.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonConvert.DeserializeObject<SaveData>(json);
                ApplySaveData(data);
                Debug.Log($"[SaveManager] Spiel geladen aus Slot {slot}.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Speicherstand korrupt: {e.Message}. Versuche Backup...");
                return LoadBackup(slot);
            }
        }

        private bool LoadBackup(int slot)
        {
            string backupPath = GetSavePath(slot) + BACKUP_SUFFIX;

            if (!File.Exists(backupPath)) return false;

            try
            {
                string json = File.ReadAllText(backupPath);
                SaveData data = JsonConvert.DeserializeObject<SaveData>(json);
                ApplySaveData(data);
                Debug.Log($"[SaveManager] Backup geladen für Slot {slot}.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Auch Backup korrupt: {e.Message}");
                return false;
            }
        }

        // --- Slot-Info ---

        public bool SlotExists(int slot) => File.Exists(GetSavePath(slot));

        public SaveSlotInfo GetSlotInfo(int slot)
        {
            string path = GetSavePath(slot);
            if (!File.Exists(path)) return null;

            try
            {
                string json = File.ReadAllText(path);
                SaveData data = JsonConvert.DeserializeObject<SaveData>(json);
                return new SaveSlotInfo
                {
                    Slot = slot,
                    Tag = data.CurrentDay,
                    Geld = data.PlayerMoney,
                    AutomatenAnzahl = data.PlacedMachines?.Count ?? 0,
                    Speicherdatum = File.GetLastWriteTime(path)
                };
            }
            catch { return null; }
        }

        public void DeleteSave(int slot)
        {
            string path = GetSavePath(slot);
            if (File.Exists(path)) File.Delete(path);
            string backup = path + BACKUP_SUFFIX;
            if (File.Exists(backup)) File.Delete(backup);
            Debug.Log($"[SaveManager] Slot {slot} gelöscht.");
        }

        // --- Daten zusammenstellen ---

        private SaveData CollectSaveData()
        {
            var data = new SaveData();

            // Wirtschaft
            if (EconomyManager.Instance != null)
            {
                data.PlayerMoney = EconomyManager.Instance.GetCurrentMoney();
                data.CurrentDay = EconomyManager.Instance.CurrentDay;
                data.DayTime = EconomyManager.Instance.DayTimeNormalized;
            }

            // Platzierte Maschinen
            if (MachineManager.Instance != null)
            {
                data.PlacedMachines = new List<MachineSaveData>();
                foreach (var machine in MachineManager.Instance.PlacedMachines)
                {
                    if (machine == null) continue;
                    // VendingMachine.GetSaveData() wird implementiert wenn VendingMachine fertig ist
                }
            }

            // Story-Flags und Einstellungen
            data.SavedAt = DateTime.Now;

            return data;
        }

        private void ApplySaveData(SaveData data)
        {
            if (data == null) return;

            // Wirtschaft wiederherstellen
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.SetMoneyDirect(data.PlayerMoney);

            // Maschinen spawnen
            if (MachineManager.Instance != null && data.PlacedMachines != null)
            {
                foreach (var machineSave in data.PlacedMachines)
                {
                    // Wird vollständig implementiert wenn VendingMachine fertig ist
                    Debug.Log($"[SaveManager] Lade Maschine: {machineSave.MachineId}");
                }
            }
        }

        private IEnumerator AutoSaveRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(AUTO_SAVE_INTERVAL);
                SaveGame(0);
                Debug.Log("[SaveManager] AutoSave ausgeführt.");
            }
        }

        private string GetSavePath(int slot) =>
            Path.Combine(SaveDirectory, $"{SAVE_FILE_PREFIX}{slot}.json");
    }

    // --- Datenhüllen ---

    [Serializable]
    public class SaveData
    {
        public decimal PlayerMoney;
        public int CurrentDay;
        public float DayTime;
        public List<MachineSaveData> PlacedMachines = new List<MachineSaveData>();
        public List<string> UnlockedRooms = new List<string>();
        public List<string> DiscoveredSynergies = new List<string>();
        public Dictionary<string, bool> StoryFlags = new Dictionary<string, bool>();
        public PlayerPreferencesData Preferences = new PlayerPreferencesData();
        public DateTime SavedAt;
    }

    [Serializable]
    public class MachineSaveData
    {
        public string MachineId;
        public Vector3 Position;
        public Quaternion Rotation;
        public int UpgradeLevel;
        public int CurrentStock;
        public MachineState State;
    }

    [Serializable]
    public class PlayerPreferencesData
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 0.7f;
        public float SFXVolume = 1f;
        public float AmbientVolume = 0.5f;
        public float MouseSensitivity = 2f;
        public bool FullScreen = true;
        public int ResolutionIndex = 0;
        public int QualityLevel = 2;
    }

    [Serializable]
    public class SaveSlotInfo
    {
        public int Slot;
        public int Tag;
        public decimal Geld;
        public int AutomatenAnzahl;
        public DateTime Speicherdatum;
    }
}
