#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine.Rendering.Universal;

namespace Vendorium.Editor
{
    // Baut das komplette Projekt auf — einmal ausführen über:
    // Menü: Vendorium → Komplett-Setup ausführen
    //
    // Was dieser Wizard erstellt:
    //  1. Alle ScriptableObject-Assets (MachineData, PlayerStats, AudioData, usw.)
    //  2. GameScene vollständig aufgebaut (Laden, Player, GameManager, Licht, UI)
    //  3. MainMenu-Szene
    //  4. Build Settings mit beiden Szenen
    public static class VendoriumAutoSetup
    {
        private const string GAME_SCENE_NAME  = "GameScene";
        private const string MENU_SCENE_NAME  = "MainMenu";
        private const string SCENES_PATH      = "Assets/Scenes";
        private const string SO_MACHINES_PATH = "Assets/ScriptableObjects/MachineData";
        private const string SO_CUSTOMER_PATH = "Assets/ScriptableObjects/CustomerData";
        private const string SO_ROOT_PATH     = "Assets/ScriptableObjects";

        [MenuItem("Vendorium/Komplett-Setup ausführen", priority = 1)]
        public static void RunFullSetup()
        {
            int step = 0;
            int total = 6;

            EditorUtility.DisplayProgressBar("Vendorium Setup", "Starte...", 0f);

            try
            {
                step++; EditorUtility.DisplayProgressBar("Vendorium Setup", "URP Render Pipeline einrichten...", (float)step / total);
                SetupURPPipeline();

                step++; EditorUtility.DisplayProgressBar("Vendorium Setup", "ScriptableObjects erstellen...", (float)step / total);
                CreateAllScriptableObjects();

                step++; EditorUtility.DisplayProgressBar("Vendorium Setup", "GameScene aufbauen...", (float)step / total);
                BuildGameScene();

                step++; EditorUtility.DisplayProgressBar("Vendorium Setup", "MainMenu-Szene aufbauen...", (float)step / total);
                BuildMainMenuScene();

                step++; EditorUtility.DisplayProgressBar("Vendorium Setup", "Build Settings konfigurieren...", (float)step / total);
                ConfigureBuildSettings();

                step++; EditorUtility.DisplayProgressBar("Vendorium Setup", "Fertig!", 1f);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog(
                "Vendorium Setup abgeschlossen",
                "Alles wurde erfolgreich erstellt!\n\n" +
                "Noch 2 manuelle Schritte:\n" +
                "1. NavMesh backen:\n   Window → AI → Navigation → Bake\n" +
                "2. Sounds unter Assets/Audio/ importieren\n\n" +
                "URP, Szenen und ScriptableObjects sind bereits fertig!",
                "Super!"
            );

            Debug.Log("[VendoriumAutoSetup] Setup abgeschlossen.");
        }

        // ----------------------------------------------------------------
        // URP RENDER PIPELINE
        // ----------------------------------------------------------------

        private static void SetupURPPipeline()
        {
            EnsureDirectory("Assets/Settings");

            const string rendererPath = "Assets/Settings/URP_ForwardRenderer.asset";
            const string urpAssetPath = "Assets/Settings/URP_PipelineAsset.asset";
            const string volumePath   = "Assets/Settings/URP_GlobalVolume.asset";

            // Renderer Data
            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
                Debug.Log("[Setup] URP ForwardRenderer erstellt.");
            }

            // Pipeline Asset
            UniversalRenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpAssetPath);
            if (urpAsset == null)
            {
                urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                // Komfortable Defaults: MSAA 2x, Schatten bei 50m, HDR an
                urpAsset.msaaSampleCount = 2;
                urpAsset.shadowDistance  = 50f;
                urpAsset.supportsHDR     = true;
                AssetDatabase.CreateAsset(urpAsset, urpAssetPath);
                Debug.Log("[Setup] URP Pipeline Asset erstellt.");
            }

            // Allen Quality-Levels zuweisen + als Default setzen
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urpAsset;
            }
            QualitySettings.SetQualityLevel(3, false); // Zurück auf "High"

            // Post-Processing Volume Profile
            VolumeProfile volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumePath);
            if (volumeProfile == null)
            {
                volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                // Bloom & Tonemapping für den Laden-Look
                volumeProfile.Add<Bloom>(overrides: true);
                volumeProfile.Add<Tonemapping>(overrides: true);
                volumeProfile.Add<ColorAdjustments>(overrides: true);
                AssetDatabase.CreateAsset(volumeProfile, volumePath);
                Debug.Log("[Setup] Post-Processing Volume Profile erstellt.");
            }

            EditorUtility.SetDirty(urpAsset);
            AssetDatabase.SaveAssets();
        }

        // ----------------------------------------------------------------
        // SCRIPTABLE OBJECTS
        // ----------------------------------------------------------------

        private static void CreateAllScriptableObjects()
        {
            EnsureDirectory(SO_MACHINES_PATH);
            EnsureDirectory(SO_CUSTOMER_PATH);

            CreateMachineData("AlteHilde",
                "Alter Kaffeeautomat",
                "coffee_alte_hilde",
                "Zuverlässig, etwas langsam – aber nie leer.",
                500, 2.5f, 8f, 20,
                MachineTrait.Reliable,
                new[] { "coffee" },
                new[] { CustomerType.Bueroangestellter });

            CreateMachineData("Knabberbert",
                "Snackautomat Knabberbert",
                "snack_knabberbert",
                "Beliebt bei Schülern. Immer ausverkauft.",
                300, 1.5f, 5f, 30,
                MachineTrait.Magnetic,
                new[] { "snack" },
                new[] { CustomerType.Schueler, CustomerType.Jugendlicher });

            CreateMachineData("Sprudelmax",
                "Getränkeautomat Sprudelmax",
                "drink_sprudelmax",
                "Kalt, sprudelnd, profitabel.",
                400, 2.0f, 6f, 25,
                MachineTrait.Fast,
                new[] { "drink" },
                new[] { CustomerType.Schueler, CustomerType.Bueroangestellter });

            CreateMachineDatabase();
            CreatePlayerStats();
            CreateAudioData();
            CreateCustomerData();
            CreateSynergyRules();
        }

        private static void CreateMachineData(string assetName, string machineName, string machineID,
            string description, int price, float income, float interval, int maxStock,
            MachineTrait trait, string[] synergyTags, CustomerType[] customerTypes)
        {
            string path = $"{SO_MACHINES_PATH}/{assetName}.asset";
            if (AssetExists(path)) return;

            var data = ScriptableObject.CreateInstance<MachineData>();
            data.MachineName         = machineName;
            data.MachineID           = machineID;
            data.Description         = description;
            data.PurchasePrice       = price;
            data.BaseIncomePerSale   = income;
            data.BaseSaleInterval    = interval;
            data.MaxStock            = maxStock;
            data.PersonalityTrait    = trait;
            data.SynergyTags         = synergyTags;
            data.PrimaryCustomerTypes = customerTypes;
            data.UpgradeLevels       = new UpgradeLevel[]
            {
                new UpgradeLevel { UpgradeName = "Wartung",        UpgradeCost = (int)(price * 0.4f), IncomeMultiplier = 1.2f, SaleIntervalMultiplier = 0.95f },
                new UpgradeLevel { UpgradeName = "Modernisierung", UpgradeCost = (int)(price * 0.7f), IncomeMultiplier = 1.3f, SaleIntervalMultiplier = 0.9f  },
                new UpgradeLevel { UpgradeName = "Upgrade",        UpgradeCost = (int)(price * 1.0f), IncomeMultiplier = 1.4f, SaleIntervalMultiplier = 0.85f },
                new UpgradeLevel { UpgradeName = "Vollausbau",     UpgradeCost = (int)(price * 1.5f), IncomeMultiplier = 1.5f, SaleIntervalMultiplier = 0.8f  }
            };

            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[Setup] MachineData erstellt: {assetName}");
        }

        private static void CreateMachineDatabase()
        {
            string path = $"{SO_ROOT_PATH}/MachineDatabase.asset";
            if (AssetExists(path)) return;

            var db = ScriptableObject.CreateInstance<MachineDatabase>();
            var hilde  = AssetDatabase.LoadAssetAtPath<MachineData>($"{SO_MACHINES_PATH}/AlteHilde.asset");
            var knabb  = AssetDatabase.LoadAssetAtPath<MachineData>($"{SO_MACHINES_PATH}/Knabberbert.asset");
            var sprud  = AssetDatabase.LoadAssetAtPath<MachineData>($"{SO_MACHINES_PATH}/Sprudelmax.asset");

            if (hilde  != null) db.AllMachines.Add(hilde);
            if (knabb  != null) db.AllMachines.Add(knabb);
            if (sprud  != null) db.AllMachines.Add(sprud);

            AssetDatabase.CreateAsset(db, path);
            Debug.Log("[Setup] MachineDatabase erstellt.");
        }

        private static void CreatePlayerStats()
        {
            string path = $"{SO_ROOT_PATH}/PlayerStats.asset";
            if (AssetExists(path)) return;

            var ps = ScriptableObject.CreateInstance<PlayerStats>();
            ps.WalkSpeed        = 4f;
            ps.RunSpeed         = 7f;
            ps.JumpHeight       = 1.2f;
            ps.MouseSensitivity = 2f;
            ps.MinVerticalAngle = -80f;
            ps.MaxVerticalAngle = 80f;
            ps.InteractionRange = 2.5f;
            ps.BobFrequency     = 2f;
            ps.BobAmplitude     = 0.05f;

            AssetDatabase.CreateAsset(ps, path);
            Debug.Log("[Setup] PlayerStats erstellt.");
        }

        private static void CreateAudioData()
        {
            string path = $"{SO_ROOT_PATH}/AudioData.asset";
            if (AssetExists(path)) return;

            var ad = ScriptableObject.CreateInstance<AudioData>();
            AssetDatabase.CreateAsset(ad, path);
            Debug.Log("[Setup] AudioData erstellt (Sounds müssen noch importiert werden).");
        }

        private static void CreateCustomerData()
        {
            EnsureDirectory(SO_CUSTOMER_PATH);

            CreateCustomerDataAsset("Schueler", "Schüler", CustomerType.Schueler,
                1.8f, 0.75f, 3, 45f, 8, new[] { "snack", "drink" });

            CreateCustomerDataAsset("Bueroangestellter", "Büroangestellter", CustomerType.Bueroangestellter,
                1.4f, 0.6f, 2, 60f, 5, new[] { "coffee", "drink" });
        }

        private static void CreateCustomerDataAsset(string assetName, string typeName,
            CustomerType type, float speed, float buyChance, int maxBuys, float stayTime,
            int weight, string[] tags)
        {
            string path = $"{SO_CUSTOMER_PATH}/{assetName}.asset";
            if (AssetExists(path)) return;

            var cd = ScriptableObject.CreateInstance<CustomerData>();
            cd.CustomerTypeName     = typeName;
            cd.Type                 = type;
            cd.WalkSpeed            = speed;
            cd.PurchaseProbability  = buyChance;
            cd.MaxPurchasesPerVisit = maxBuys;
            cd.StayDuration         = stayTime;
            cd.SpawnWeight          = weight;
            cd.PreferredMachineTags = tags;

            AssetDatabase.CreateAsset(cd, path);
            Debug.Log($"[Setup] CustomerData erstellt: {assetName}");
        }

        private static void CreateSynergyRules()
        {
            string dir = "Assets/ScriptableObjects/SynergyRules";
            EnsureDirectory(dir);

            CreateSynergyRule(dir, "Fruehstuecksduo",
                "Frühstücks-Duo", "Kaffee + Snack = +35% Einnahmen",
                new[] { "coffee" }, new[] { "snack" }, 1.35f,
                new Color(1f, 0.85f, 0f));

            CreateSynergyRule(dir, "Durstloescher",
                "Durstlöscher-Kombo", "Snack + Getränk = +25% Einnahmen",
                new[] { "snack" }, new[] { "drink" }, 1.25f,
                new Color(0.2f, 0.8f, 1f));
        }

        private static void CreateSynergyRule(string dir, string assetName, string ruleName,
            string desc, string[] tagsA, string[] tagsB, float multiplier, Color color)
        {
            string path = $"{dir}/{assetName}.asset";
            if (AssetExists(path)) return;

            var rule = ScriptableObject.CreateInstance<SynergyRule>();
            rule.RuleName          = ruleName;
            rule.BonusDescription  = desc;
            rule.RequiredTagsA     = tagsA;
            rule.RequiredTagsB     = tagsB;
            rule.IncomeMultiplier  = multiplier;
            rule.LineColor         = color;

            AssetDatabase.CreateAsset(rule, path);
        }

        // ----------------------------------------------------------------
        // GAMESCENE
        // ----------------------------------------------------------------

        private static void BuildGameScene()
        {
            EnsureDirectory(SCENES_PATH);
            string scenePath = $"{SCENES_PATH}/{GAME_SCENE_NAME}.unity";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- Licht ----
            var sunGO  = new GameObject("Sun");
            var sun    = sunGO.AddComponent<Light>();
            sun.type   = LightType.Directional;
            sun.color  = new Color(1f, 0.96f, 0.84f);
            sun.intensity = 1.0f;
            sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sunGO.AddComponent<DayNightCycle>();
            sunGO.AddComponent<UniversalAdditionalLightData>();

            // ---- GameManager (persistiert über alle Scenes) ----
            var gmGO = new GameObject("GameManager");
            gmGO.AddComponent<GameManager>();
            gmGO.AddComponent<VendoriumEventManager>();

            var economyGO = new GameObject("EconomyManager");
            economyGO.AddComponent<EconomyManager>();
            economyGO.transform.SetParent(gmGO.transform);

            var custGO = new GameObject("CustomerManager");
            custGO.AddComponent<CustomerManager>();
            custGO.transform.SetParent(gmGO.transform);

            var machGO = new GameObject("MachineManager");
            var mm = machGO.AddComponent<MachineManager>();
            machGO.transform.SetParent(gmGO.transform);

            // MachineDatabase zuweisen
            var db = AssetDatabase.LoadAssetAtPath<MachineDatabase>($"{SO_ROOT_PATH}/MachineDatabase.asset");
            if (db != null) SerializedObjectHelper.SetField(mm, "machineDatabase", db);

            var uiMgrGO = new GameObject("UIManager");
            uiMgrGO.AddComponent<UIManager>();
            uiMgrGO.transform.SetParent(gmGO.transform);

            var savGO = new GameObject("SaveManager");
            savGO.AddComponent<SaveManager>();
            savGO.transform.SetParent(gmGO.transform);

            var audGO = new GameObject("AudioManager");
            var audioMgr = audGO.AddComponent<AudioManager>();
            audGO.transform.SetParent(gmGO.transform);
            var ad = AssetDatabase.LoadAssetAtPath<AudioData>($"{SO_ROOT_PATH}/AudioData.asset");
            if (ad != null) SerializedObjectHelper.SetField(audioMgr, "audioData", ad);

            var perfGO = new GameObject("PerformanceManager");
            perfGO.AddComponent<PerformanceManager>();
            perfGO.transform.SetParent(gmGO.transform);

            var synGO = new GameObject("SynergyManager");
            synGO.AddComponent<SynergyManager>();
            synGO.transform.SetParent(gmGO.transform);

            var randEvGO = new GameObject("RandomEventManager");
            randEvGO.AddComponent<RandomEventManager>();
            randEvGO.transform.SetParent(gmGO.transform);

            var storyGO = new GameObject("StoryManager");
            storyGO.AddComponent<StoryManager>();
            storyGO.transform.SetParent(gmGO.transform);

            var dlgGO = new GameObject("DialogueSystem");
            dlgGO.AddComponent<DialogueSystem>();
            dlgGO.transform.SetParent(gmGO.transform);

            var sceneLoaderGO = new GameObject("SceneLoader");
            sceneLoaderGO.AddComponent<SceneLoader>();
            sceneLoaderGO.transform.SetParent(gmGO.transform);

            var placementGO = new GameObject("PlacementManager");
            placementGO.AddComponent<PlacementManager>();

            var cashRegGO = new GameObject("CashRegisterManager");
            cashRegGO.AddComponent<CashRegisterManager>();
            cashRegGO.transform.SetParent(gmGO.transform);

            // ---- Laden-Layout ----
            var shopBuilderGO = new GameObject("ShopLayoutSetup");
            var shopBuilder = shopBuilderGO.AddComponent<ShopLayoutBuilder>();
            SerializedObjectHelper.SetField(shopBuilder, "buildOnStart", true);
            SerializedObjectHelper.SetField(shopBuilder, "includePlaceholderMachines", true);
            SerializedObjectHelper.SetField(shopBuilder, "placeholderMachineCount", 3);

            // NavMesh-Surface — wird auf dem Boden des Ladens gebacken
            var navMeshGO = new GameObject("NavMeshSurface");
            var navSurface = navMeshGO.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.All;
            navSurface.useGeometry    = NavMeshCollectGeometry.PhysicsColliders;
            navSurface.layerMask      = LayerMask.GetMask("Floor", "Default");

            // Post-Processing Global Volume
            var volumeGO = new GameObject("GlobalVolume");
            var volume   = volumeGO.AddComponent<Volume>();
            volume.isGlobal   = true;
            volume.priority   = 0;
            var volProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/URP_GlobalVolume.asset");
            if (volProfile != null) volume.sharedProfile = volProfile;

            // ---- Player ----
            var playerGO = new GameObject("Player");
            playerGO.tag = "Player";
            playerGO.layer = LayerMask.NameToLayer("Player") >= 0 ? LayerMask.NameToLayer("Player") : 0;

            var cc = playerGO.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var playerAudio = playerGO.AddComponent<AudioSource>();
            playerAudio.playOnAwake = false;

            var pc = playerGO.AddComponent<PlayerController>();
            var playerStats = AssetDatabase.LoadAssetAtPath<PlayerStats>($"{SO_ROOT_PATH}/PlayerStats.asset");
            if (playerStats != null) SerializedObjectHelper.SetField(pc, "stats", playerStats);

            playerGO.transform.position = new Vector3(0f, 0f, -6f);

            // CameraHolder
            var camHolderGO = new GameObject("CameraHolder");
            camHolderGO.transform.SetParent(playerGO.transform, false);
            camHolderGO.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var headBob = camHolderGO.AddComponent<HeadBobEffect>();
            SerializedObjectHelper.SetField(headBob, "player", pc);

            // MainCamera
            var camGO = new GameObject("MainCamera");
            camGO.tag = "MainCamera";
            camGO.transform.SetParent(camHolderGO.transform, false);
            var cam = camGO.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.1f;
            camGO.AddComponent<AudioListener>();
            var camData = camGO.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            var playerInteraction = camGO.AddComponent<PlayerInteraction>();
            SerializedObjectHelper.SetField(playerInteraction, "interactableLayers",
                LayerMask.GetMask("Interactable", "Default"));

            // CameraHolder-Referenz in PlayerController setzen
            SerializedObjectHelper.SetField(pc, "cameraHolder", camHolderGO.transform);
            SerializedObjectHelper.SetField(pc, "audioSource", playerAudio);

            // ---- Kunden-Spawn-Punkt ----
            var spawnGO = new GameObject("CustomerSpawnPoint");
            spawnGO.transform.position = new Vector3(0f, 0f, -7.5f);

            // ---- UI Canvas ----
            BuildGameSceneUI(gmGO, playerGO);

            // ---- EventSystem ----
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();

            // Szene speichern
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[Setup] GameScene erstellt: {scenePath}");
        }

        private static void BuildGameSceneUI(GameObject gmGO, GameObject playerGO)
        {
            // Haupt-Canvas
            var canvasGO = new GameObject("GameCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // --- HUD ---
            var hudGO = CreatePanel(canvasGO.transform, "HUD");
            var hud = hudGO.AddComponent<HUD>();

            // Geld-Text oben rechts
            var moneyText = CreateTMPText(hudGO.transform, "MoneyText", "1000,00 €",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -10f), 24);
            SerializedObjectHelper.SetField(hud, "moneyText", moneyText);

            // Tag-Text
            var dayText = CreateTMPText(hudGO.transform, "DayText", "Tag 1",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -40f), 18);
            SerializedObjectHelper.SetField(hud, "dayText", dayText);

            // Tageszeit
            var todText = CreateTMPText(hudGO.transform, "TimeOfDayText", "Morgen",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -65f), 16);
            SerializedObjectHelper.SetField(hud, "timeOfDayText", todText);

            // Tagesumsatz
            var incText = CreateTMPText(hudGO.transform, "TodayIncomeText", "Heute: 0,00 €",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -90f), 14);
            SerializedObjectHelper.SetField(hud, "todayIncomeText", incText);

            // --- Interaction Prompt unten mittig ---
            var promptGO = new GameObject("InteractionPrompt");
            promptGO.transform.SetParent(canvasGO.transform, false);
            var promptCG = promptGO.AddComponent<CanvasGroup>();
            promptCG.alpha = 0f;
            var promptText = CreateTMPText(promptGO.transform, "PromptText", "[E] Interagieren",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), 20);
            var prompt = promptGO.AddComponent<InteractionPrompt>();
            SerializedObjectHelper.SetField(prompt, "promptText", promptText);

            // --- Leere Panel-Slots (werden mit MachineInspect usw. befüllt) ---
            CreateEmptyPanel(canvasGO.transform, "MachineInspectPanel").AddComponent<MachineInspectPanel>();
            CreateEmptyPanel(canvasGO.transform, "ShopScreen").AddComponent<ShopScreen>();
            CreateEmptyPanel(canvasGO.transform, "CashRegisterPanel").AddComponent<CashRegisterUI>();
            CreateEmptyPanel(canvasGO.transform, "DailyReportPanel").AddComponent<DailyReportPanel>();
            var pauseGO = CreateEmptyPanel(canvasGO.transform, "PauseMenu");
            pauseGO.AddComponent<PauseMenu>();
            CreateEmptyPanel(canvasGO.transform, "EventNotificationPanel").AddComponent<EventNotificationPanel>();
            CreateEmptyPanel(canvasGO.transform, "DialoguePanel").AddComponent<DialogueSystem>();

            // UIManager befüllen
            var uiMgr = Object.FindObjectOfType<UIManager>();
            if (uiMgr != null)
            {
                SerializedObjectHelper.SetField(uiMgr, "hudScreen",              hudGO);
                SerializedObjectHelper.SetField(uiMgr, "pauseMenuScreen",        pauseGO);
                SerializedObjectHelper.SetField(uiMgr, "eventNotificationPanel", canvasGO.transform.Find("EventNotificationPanel")?.gameObject);
            }
        }

        // ----------------------------------------------------------------
        // MAINMENU-SZENE
        // ----------------------------------------------------------------

        private static void BuildMainMenuScene()
        {
            string scenePath = $"{SCENES_PATH}/{MENU_SCENE_NAME}.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Kamera mit URP-Komponente
            var camGO = new GameObject("MainCamera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<UniversalAdditionalCameraData>();

            // Licht
            var lightGO = new GameObject("DirectionalLight");
            var light   = lightGO.AddComponent<Light>();
            light.type  = LightType.Directional;
            lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            lightGO.AddComponent<UniversalAdditionalLightData>();

            // Managers die auch im Menü gebraucht werden
            var sceneLoaderGO = new GameObject("SceneLoader");
            sceneLoaderGO.AddComponent<SceneLoader>();

            var audioMgrGO = new GameObject("AudioManager");
            audioMgrGO.AddComponent<AudioManager>();

            // Post-Processing
            var menuVolumeGO = new GameObject("GlobalVolume");
            var menuVolume   = menuVolumeGO.AddComponent<Volume>();
            menuVolume.isGlobal = true;
            var menuProfile  = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/URP_GlobalVolume.asset");
            if (menuProfile != null) menuVolume.sharedProfile = menuProfile;

            // UI Canvas
            var canvasGO = new GameObject("MenuCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // Logo-Text
            var logoText = CreateTMPText(canvasGO.transform, "Logo", "VENDORIUM",
                new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), Vector2.zero, 72);
            logoText.color = new Color(1f, 0.85f, 0.1f);
            logoText.fontStyle = FontStyles.Bold;

            // Buttons
            float startY = -50f;
            float spacing = -65f;
            string[] btnNames   = { "BtnNewGame", "BtnContinue", "BtnSettings", "BtnCredits", "BtnQuit" };
            string[] btnLabels  = { "Neues Spiel", "Fortsetzen", "Einstellungen", "Credits", "Beenden" };

            for (int i = 0; i < btnNames.Length; i++)
            {
                var btnGO = new GameObject(btnNames[i]);
                btnGO.transform.SetParent(canvasGO.transform, false);
                var rect = btnGO.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, startY + i * spacing);
                rect.sizeDelta = new Vector2(250f, 55f);

                var img = btnGO.AddComponent<Image>();
                img.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
                var btn = btnGO.AddComponent<Button>();

                var lblGO  = new GameObject("Label");
                lblGO.transform.SetParent(btnGO.transform, false);
                var lbl    = lblGO.AddComponent<TextMeshProUGUI>();
                lbl.text   = btnLabels[i];
                lbl.fontSize = 22;
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.color  = Color.white;
                var lblRect = lbl.GetComponent<RectTransform>();
                lblRect.anchorMin = Vector2.zero;
                lblRect.anchorMax = Vector2.one;
                lblRect.sizeDelta = Vector2.zero;
            }

            var mainMenu  = canvasGO.AddComponent<MainMenu>();
            var settingsP = CreateEmptyPanel(canvasGO.transform, "SettingsPanel");
            settingsP.AddComponent<SettingsPanel>();

            // EventSystem
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[Setup] MainMenu-Szene erstellt: {scenePath}");
        }

        // ----------------------------------------------------------------
        // BUILD SETTINGS
        // ----------------------------------------------------------------

        private static void ConfigureBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene($"{SCENES_PATH}/{MENU_SCENE_NAME}.unity", true),
                new EditorBuildSettingsScene($"{SCENES_PATH}/{GAME_SCENE_NAME}.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[Setup] Build Settings konfiguriert (MainMenu + GameScene).");
        }

        // ----------------------------------------------------------------
        // HILFSMETHODEN
        // ----------------------------------------------------------------

        private static GameObject CreatePanel(Transform parent, string name)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            return go;
        }

        private static GameObject CreateEmptyPanel(Transform parent, string name)
        {
            var go = CreatePanel(parent, name);
            go.SetActive(false);
            return go;
        }

        private static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, float fontSize)
        {
            var go   = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp  = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineRight;
            var rect = tmp.GetComponent<RectTransform>();
            rect.anchorMin        = anchorMin;
            rect.anchorMax        = anchorMax;
            rect.pivot            = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta        = new Vector2(300f, 30f);
            return tmp;
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static bool AssetExists(string path) =>
            AssetDatabase.LoadAssetAtPath<Object>(path) != null;
    }

    // Hilfklasse für SerializedObject-Feldsets
    public static class SerializedObjectHelper
    {
        public static void SetField(Object target, string fieldName, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                // Versuche mit m_ Prefix (Unity-Konvention)
                prop = so.FindProperty("m_" + fieldName);
            }
            if (prop == null) return;

            switch (value)
            {
                case bool b:        prop.boolValue       = b; break;
                case int i:         prop.intValue        = i; break;
                case float f:       prop.floatValue      = f; break;
                case string s:      prop.stringValue     = s; break;
                case Object obj:    prop.objectReferenceValue = obj; break;
                case Vector2 v2:    prop.vector2Value    = v2; break;
                case Vector3 v3:    prop.vector3Value    = v3; break;
                case LayerMask lm:  prop.intValue        = lm; break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
