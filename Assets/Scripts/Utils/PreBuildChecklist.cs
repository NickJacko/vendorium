#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vendorium.Editor
{
    // Editor-Script: vor jedem Build automatisch ausführen (oder manuell über Menü).
    // Prüft: Szenen in Build Settings, fehlende Referenzen, AudioClips, Versionsnummer.
    public class PreBuildChecklist : EditorWindow
    {
        private List<CheckItem> _results = new List<CheckItem>();
        private Vector2 _scroll;

        [MenuItem("Vendorium/Pre-Build Checkliste")]
        public static void ShowWindow()
        {
            var win = GetWindow<PreBuildChecklist>("Pre-Build Checkliste");
            win.RunChecks();
        }

        private void OnGUI()
        {
            GUILayout.Label("VENDORIUM — Pre-Build Checkliste", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (GUILayout.Button("Erneut prüfen"))
                RunChecks();

            GUILayout.Space(5);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var item in _results)
            {
                Color old = GUI.color;
                GUI.color = item.Passed ? Color.green : (item.IsWarning ? Color.yellow : Color.red);
                GUILayout.Label($"[{(item.Passed ? "OK" : item.IsWarning ? "!!" : "FAIL")}] {item.Description}");
                GUI.color = old;

                if (!item.Passed && !string.IsNullOrEmpty(item.Detail))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox(item.Detail, item.IsWarning ? MessageType.Warning : MessageType.Error);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            int failures = _results.FindAll(r => !r.Passed && !r.IsWarning).Count;
            int warnings = _results.FindAll(r => !r.Passed &&  r.IsWarning).Count;

            GUILayout.Label($"Ergebnis: {failures} Fehler, {warnings} Warnungen", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(failures > 0);
            if (GUILayout.Button("Build starten (Player Settings öffnen)"))
                EditorApplication.ExecuteMenuItem("File/Build Settings...");
            EditorGUI.EndDisabledGroup();
        }

        private void RunChecks()
        {
            _results.Clear();

            CheckBuildScenes();
            CheckVersionNumber();
            CheckMissingReferences();
            CheckAudioClips();
            CheckScriptableObjects();
            CheckPlayerSettings();

            Repaint();
        }

        private void CheckBuildScenes()
        {
            string[] required = { "MainMenu", "GameScene" };
            var scenes = EditorBuildSettings.scenes;

            foreach (var name in required)
            {
                bool found = false;
                foreach (var scene in scenes)
                    if (Path.GetFileNameWithoutExtension(scene.path) == name) { found = true; break; }

                _results.Add(new CheckItem
                {
                    Description = $"Szene '{name}' in Build Settings",
                    Passed = found,
                    Detail = found ? "" : $"Szene '{name}' fehlt in File → Build Settings → Scenes In Build."
                });
            }
        }

        private void CheckVersionNumber()
        {
            string version = PlayerSettings.bundleVersion;
            bool valid = !string.IsNullOrEmpty(version) && version != "0.1" && version != "1.0";

            _results.Add(new CheckItem
            {
                Description = $"Versionsnummer gesetzt: {version}",
                Passed = true, // nur Warnung
                IsWarning = !valid,
                Detail = valid ? "" : "Versionsnummer in Player Settings → Version prüfen."
            });
        }

        private void CheckMissingReferences()
        {
            int missingCount = 0;

            // Alle Prefabs im Projekt prüfen
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
            foreach (var guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                foreach (var comp in prefab.GetComponentsInChildren<Component>(true))
                {
                    if (comp == null) { missingCount++; break; }
                }
            }

            _results.Add(new CheckItem
            {
                Description = "Keine fehlenden Komponenten in Prefabs",
                Passed = missingCount == 0,
                Detail = missingCount > 0
                    ? $"{missingCount} Prefab(s) mit Missing-Komponenten gefunden."
                    : ""
            });
        }

        private void CheckAudioClips()
        {
            var audioDatas = AssetDatabase.FindAssets("t:AudioData");
            bool allFound = true;

            foreach (var guid in audioDatas)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ad = AssetDatabase.LoadAssetAtPath<AudioData>(path);
                if (ad == null) continue;

                // Kritische Felder prüfen
                if (ad.FootstepsConcrete == null || ad.FootstepsConcrete.Count == 0)
                {
                    allFound = false;
                    break;
                }
            }

            _results.Add(new CheckItem
            {
                Description = "AudioData — Footstep-Sounds zugewiesen",
                Passed = allFound,
                IsWarning = !allFound,
                Detail = allFound ? "" : "AudioData ScriptableObject: FootstepsConcrete Liste ist leer."
            });
        }

        private void CheckScriptableObjects()
        {
            // MachineDatabase prüfen
            var dbs = AssetDatabase.FindAssets("t:MachineDatabase");
            bool dbFound = dbs.Length > 0;

            _results.Add(new CheckItem
            {
                Description = "MachineDatabase Asset vorhanden",
                Passed = dbFound,
                Detail = dbFound ? "" : "MachineDatabase ScriptableObject fehlt. Erstellen: Assets → Create → VendoriumData → MachineDatabase."
            });

            // PlayerStats prüfen
            var ps = AssetDatabase.FindAssets("t:PlayerStats");
            _results.Add(new CheckItem
            {
                Description = "PlayerStats Asset vorhanden",
                Passed = ps.Length > 0,
                Detail = ps.Length == 0 ? "PlayerStats ScriptableObject fehlt." : ""
            });
        }

        private void CheckPlayerSettings()
        {
            // IL2CPP
            var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            bool isIL2CPP = backend == ScriptingImplementation.IL2CPP;

            _results.Add(new CheckItem
            {
                Description = $"Scripting Backend: {backend}",
                Passed = true,
                IsWarning = !isIL2CPP,
                Detail = isIL2CPP ? "" : "IL2CPP empfohlen für Release-Build (Player Settings → Scripting Backend)."
            });

            // Firmen-/Produktname
            _results.Add(new CheckItem
            {
                Description = $"Produktname: {PlayerSettings.productName}",
                Passed = PlayerSettings.productName == "Vendorium",
                IsWarning = PlayerSettings.productName != "Vendorium",
                Detail = ""
            });
        }
    }

    public class CheckItem
    {
        public string Description;
        public bool Passed;
        public bool IsWarning;
        public string Detail;
    }
}
#endif
