#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Vendorium.Editor
{
    // Erstellt Platzhalter-Prefabs für Automaten, Kunden und den Player.
    // Menü: Vendorium → Prefabs erstellen
    // Diese Prefabs sind rein visuell (farbige Primitives).
    // Später durch echte 3D-Modelle ersetzen.
    public static class VendoriumPrefabBuilder
    {
        private const string PREFAB_MACHINES  = "Assets/Prefabs/Machines";
        private const string PREFAB_CUSTOMERS = "Assets/Prefabs/Customers";
        private const string PREFAB_PLAYER    = "Assets/Prefabs/Player";

        [MenuItem("Vendorium/Prefabs erstellen", priority = 2)]
        public static void BuildAllPrefabs()
        {
            EnsureDirectory(PREFAB_MACHINES);
            EnsureDirectory(PREFAB_CUSTOMERS);
            EnsureDirectory(PREFAB_PLAYER);

            CreateMachinePrefab("AlteHilde_Prefab",     new Color(0.55f, 0.35f, 0.15f), "coffee_alte_hilde");
            CreateMachinePrefab("Knabberbert_Prefab",   new Color(1.0f,  0.6f,  0.1f),  "snack_knabberbert");
            CreateMachinePrefab("Sprudelmax_Prefab",    new Color(0.2f,  0.5f,  0.9f),  "drink_sprudelmax");
            CreateCustomerPrefab("Schueler_Prefab",     new Color(0.3f,  0.7f,  0.3f),  CustomerType.Schueler);
            CreateCustomerPrefab("Bueroangestellter_Prefab", new Color(0.5f, 0.5f, 0.8f), CustomerType.Bueroangestellter);
            CreatePlayerPrefab();

            // Prefab-Referenzen in ScriptableObjects eintragen
            LinkPrefabsToScriptableObjects();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Prefabs erstellt",
                "Alle Platzhalter-Prefabs wurden erstellt.\n\n" +
                "Die Prefabs sind farbige Würfel/Kapseln.\n" +
                "Ersetze sie später durch echte 3D-Modelle.",
                "OK");
        }

        // ----------------------------------------------------------------
        // AUTOMAT-PREFAB
        // Aufbau: Root → BoxCollider (Body) + MachineTriggerZone + VendingMachine
        //          └── VisualCube (MeshRenderer)
        //          └── SaleEffectPoint (für Coin-Partikel)
        // ----------------------------------------------------------------
        private static void CreateMachinePrefab(string prefabName, Color color, string machineID)
        {
            string path = $"{PREFAB_MACHINES}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            // Root
            var root = new GameObject(prefabName);
            root.tag   = "Interactable";
            root.layer = LayerMask.NameToLayer("Machine") >= 0 ? LayerMask.NameToLayer("Machine") : 0;

            // Visueller Würfel — Automat-Körper
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale    = new Vector3(0.8f, 1.6f, 0.5f);
            body.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            // Farbe setzen
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            body.GetComponent<Renderer>().sharedMaterial = mat;
            AssetDatabase.CreateAsset(mat, $"{PREFAB_MACHINES}/{prefabName}_Mat.mat");

            // Display-Würfel (helleres Panel oben)
            var display = GameObject.CreatePrimitive(PrimitiveType.Cube);
            display.name = "Display";
            display.transform.SetParent(root.transform, false);
            display.transform.localScale    = new Vector3(0.6f, 0.35f, 0.05f);
            display.transform.localPosition = new Vector3(0f, 1.25f, 0.28f);
            var dispMat = new Material(mat);
            dispMat.color = Color.black;
            display.GetComponent<Renderer>().sharedMaterial = dispMat;
            AssetDatabase.CreateAsset(dispMat, $"{PREFAB_MACHINES}/{prefabName}_Display_Mat.mat");

            // Root-Collider entfernen (nur Body-Collider benutzen)
            // BoxCollider für Physik auf Root
            var rootCol = root.AddComponent<BoxCollider>();
            rootCol.size   = new Vector3(0.8f, 1.6f, 0.5f);
            rootCol.center = new Vector3(0f, 0.8f, 0f);

            // Trigger-Zone für Kunden (2m Radius)
            var triggerGO = new GameObject("TriggerZone");
            triggerGO.transform.SetParent(root.transform, false);
            triggerGO.layer = LayerMask.NameToLayer("Interactable") >= 0 ? LayerMask.NameToLayer("Interactable") : 0;
            var trigger = triggerGO.AddComponent<SphereCollider>();
            trigger.radius    = 2f;
            trigger.isTrigger = true;
            triggerGO.AddComponent<MachineTriggerZone>();

            // Coin-Partikel Spawn-Punkt
            var salePoint = new GameObject("SaleEffectPoint");
            salePoint.transform.SetParent(root.transform, false);
            salePoint.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            salePoint.AddComponent<MachineSalesEffect>();

            // VendingMachine-Component
            var vm = root.AddComponent<VendingMachine>();
            var machineData = AssetDatabase.LoadAssetAtPath<MachineData>(
                $"Assets/ScriptableObjects/MachineData/{prefabName.Replace("_Prefab","")}.asset");
            if (machineData != null)
                SerializedObjectHelper.SetField(vm, "data", machineData);

            // Prefab speichern
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[Prefabs] {prefabName} erstellt.");
        }

        // ----------------------------------------------------------------
        // KUNDEN-PREFAB
        // Aufbau: Kapsel + NavMeshAgent + CustomerController
        // ----------------------------------------------------------------
        private static void CreateCustomerPrefab(string prefabName, Color color, CustomerType type)
        {
            string path = $"{PREFAB_CUSTOMERS}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var root = new GameObject(prefabName);
            root.tag   = "Customer";
            root.layer = LayerMask.NameToLayer("Customer") >= 0 ? LayerMask.NameToLayer("Customer") : 0;

            // Kapsel-Körper
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = color;
            body.GetComponent<Renderer>().sharedMaterial = mat;
            AssetDatabase.CreateAsset(mat, $"{PREFAB_CUSTOMERS}/{prefabName}_Mat.mat");

            // CapsuleCollider am Root
            var col = root.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.3f;
            col.center = new Vector3(0f, 1f, 0f);

            // NavMeshAgent
            var agent = root.AddComponent<NavMeshAgent>();
            agent.height     = 2f;
            agent.radius     = 0.3f;
            agent.speed      = 1.5f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.5f;

            // AudioSource für Kunden-Sounds
            var audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.minDistance  = 1f;
            audio.maxDistance  = 10f;

            // CustomerController
            var ctrl = root.AddComponent<CustomerController>();
            var custData = AssetDatabase.LoadAssetAtPath<CustomerData>(
                $"Assets/ScriptableObjects/CustomerData/{type}.asset");
            if (custData != null)
                SerializedObjectHelper.SetField(ctrl, "data", custData);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[Prefabs] {prefabName} erstellt.");
        }

        // ----------------------------------------------------------------
        // PLAYER-PREFAB
        // Exakte Hierarchie laut CLAUDE.md:
        //   Player → CharacterController + PlayerController
        //     └── CameraHolder (Y=1.6) → HeadBobEffect
        //           └── MainCamera → Camera (FOV=75) + PlayerInteraction
        // ----------------------------------------------------------------
        private static void CreatePlayerPrefab()
        {
            string path = $"{PREFAB_PLAYER}/Player.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var playerGO = new GameObject("Player");
            playerGO.tag = "Player";
            playerGO.layer = LayerMask.NameToLayer("Player") >= 0 ? LayerMask.NameToLayer("Player") : 0;

            var cc = playerGO.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var audio = playerGO.AddComponent<AudioSource>();
            audio.playOnAwake = false;

            var pc = playerGO.AddComponent<PlayerController>();
            var stats = AssetDatabase.LoadAssetAtPath<PlayerStats>("Assets/ScriptableObjects/PlayerStats.asset");
            if (stats != null) SerializedObjectHelper.SetField(pc, "stats", stats);

            // CameraHolder
            var holderGO = new GameObject("CameraHolder");
            holderGO.transform.SetParent(playerGO.transform, false);
            holderGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var hb = holderGO.AddComponent<HeadBobEffect>();
            SerializedObjectHelper.SetField(hb, "player", pc);
            if (stats != null) SerializedObjectHelper.SetField(hb, "stats", stats);

            // MainCamera
            var camGO = new GameObject("MainCamera");
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(holderGO.transform, false);
            var cam = camGO.AddComponent<Camera>();
            cam.fieldOfView   = 75f;
            cam.nearClipPlane = 0.1f;
            camGO.AddComponent<AudioListener>();

            var pi = camGO.AddComponent<PlayerInteraction>();
            if (stats != null) SerializedObjectHelper.SetField(pi, "stats", stats);
            SerializedObjectHelper.SetField(pi, "interactableLayers", LayerMask.GetMask("Interactable", "Default"));

            // Referenzen in PlayerController
            SerializedObjectHelper.SetField(pc, "cameraHolder", holderGO.transform);
            SerializedObjectHelper.SetField(pc, "audioSource", audio);

            PrefabUtility.SaveAsPrefabAsset(playerGO, path);
            Object.DestroyImmediate(playerGO);
            Debug.Log("[Prefabs] Player-Prefab erstellt.");
        }

        // ----------------------------------------------------------------
        // PREFAB-REFERENZEN IN SCRIPTABLEOBJECTS EINTRAGEN
        // ----------------------------------------------------------------
        private static void LinkPrefabsToScriptableObjects()
        {
            LinkMachinePrefab("AlteHilde",          "AlteHilde_Prefab");
            LinkMachinePrefab("Knabberbert",        "Knabberbert_Prefab");
            LinkMachinePrefab("Sprudelmax",         "Sprudelmax_Prefab");
            LinkCustomerPrefab("Schueler",          "Schueler_Prefab");
            LinkCustomerPrefab("Bueroangestellter", "Bueroangestellter_Prefab");
        }

        private static void LinkMachinePrefab(string soName, string prefabName)
        {
            var so = AssetDatabase.LoadAssetAtPath<MachineData>(
                $"Assets/ScriptableObjects/MachineData/{soName}.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PREFAB_MACHINES}/{prefabName}.prefab");
            if (so != null && prefab != null)
            {
                so.Prefab = prefab;
                EditorUtility.SetDirty(so);
            }
        }

        private static void LinkCustomerPrefab(string soName, string prefabName)
        {
            var so = AssetDatabase.LoadAssetAtPath<CustomerData>(
                $"Assets/ScriptableObjects/CustomerData/{soName}.asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"{PREFAB_CUSTOMERS}/{prefabName}.prefab");
            if (so != null && prefab != null)
            {
                so.Prefab = prefab;
                EditorUtility.SetDirty(so);
            }
        }

        // ----------------------------------------------------------------

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
#endif
