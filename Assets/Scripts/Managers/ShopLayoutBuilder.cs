using UnityEngine;

namespace Vendorium
{
    // Baut beim Start automatisch einen einfachen Laden-Blockout aus Unity Primitives.
    // Im Unity Editor: leeres GameObject "ShopLayout" anlegen, dieses Script zuweisen.
    // Im Play-Mode oder per "Build on Start" wird der Laden generiert.
    public class ShopLayoutBuilder : MonoBehaviour
    {
        // --- Dimensionen ---
        private const float ROOM_WIDTH  = 20f;
        private const float ROOM_DEPTH  = 15f;
        private const float WALL_HEIGHT = 3.5f;
        private const float WALL_THICK  = 0.2f;
        private const float DOOR_WIDTH  = 2f;
        private const float DOOR_HEIGHT = 2.5f;
        private const float CEIL_Y      = WALL_HEIGHT;

        [Header("Einstellungen")]
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool includePlaceholderMachines = true;
        [SerializeField] private int  placeholderMachineCount = 3;

        [Header("Materialien (optional — Unity Default wenn leer)")]
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material ceilingMaterial;
        [SerializeField] private Material machinePlaceholderMaterial;

        // Layer-IDs werden beim Start gesetzt
        private int _floorLayer;
        private int _wallLayer;
        private int _machineLayer;

        private Transform _shopRoot;

        private void Start()
        {
            EnsureLayers();

            if (buildOnStart)
                BuildShop();
        }

        public void BuildShop()
        {
            // Alten Aufbau entfernen falls vorhanden
            if (_shopRoot != null)
                Destroy(_shopRoot.gameObject);

            _shopRoot = new GameObject("ShopLayout").transform;

            BuildFloor();
            BuildCeiling();
            BuildWalls();

            if (includePlaceholderMachines)
                BuildMachinePlaceholders();

            Debug.Log("[ShopLayoutBuilder] Laden aufgebaut.");
        }

        private void BuildFloor()
        {
            var floor = CreatePrimitive(
                "Floor",
                new Vector3(0f, 0f, 0f),
                new Vector3(ROOM_WIDTH, 0.1f, ROOM_DEPTH),
                floorMaterial
            );
            floor.layer = _floorLayer;

            // Physics Material für rutschfreien Boden
            var col = floor.GetComponent<Collider>();
            if (col != null)
            {
                var pm = new PhysicMaterial("Floor_Physics");
                pm.dynamicFriction = 0.6f;
                pm.staticFriction  = 0.6f;
                col.material = pm;
            }
        }

        private void BuildCeiling()
        {
            CreatePrimitive(
                "Ceiling",
                new Vector3(0f, CEIL_Y, 0f),
                new Vector3(ROOM_WIDTH, 0.1f, ROOM_DEPTH),
                ceilingMaterial
            );
        }

        private void BuildWalls()
        {
            float halfW = ROOM_WIDTH  * 0.5f;
            float halfD = ROOM_DEPTH  * 0.5f;
            float wallY = WALL_HEIGHT * 0.5f;

            // Rückwand (hinten)
            CreateWall("Wall_Back",
                new Vector3(0f, wallY, halfD),
                new Vector3(ROOM_WIDTH, WALL_HEIGHT, WALL_THICK));

            // Linke Wand
            CreateWall("Wall_Left",
                new Vector3(-halfW, wallY, 0f),
                new Vector3(WALL_THICK, WALL_HEIGHT, ROOM_DEPTH));

            // Rechte Wand
            CreateWall("Wall_Right",
                new Vector3(halfW, wallY, 0f),
                new Vector3(WALL_THICK, WALL_HEIGHT, ROOM_DEPTH));

            // Vorderwand mit Türöffnung (in zwei Hälften aufgeteilt)
            BuildFrontWallWithDoor(halfD);
        }

        private void BuildFrontWallWithDoor(float halfD)
        {
            float halfW     = ROOM_WIDTH * 0.5f;
            float doorHalfW = DOOR_WIDTH * 0.5f;
            float wallY     = WALL_HEIGHT * 0.5f;

            // Linkes Wandstück neben der Tür
            float leftSegW = halfW - doorHalfW;
            CreateWall("Wall_Front_Left",
                new Vector3(-(doorHalfW + leftSegW * 0.5f), wallY, -halfD),
                new Vector3(leftSegW, WALL_HEIGHT, WALL_THICK));

            // Rechtes Wandstück neben der Tür
            CreateWall("Wall_Front_Right",
                new Vector3(doorHalfW + leftSegW * 0.5f, wallY, -halfD),
                new Vector3(leftSegW, WALL_HEIGHT, WALL_THICK));

            // Sturz über der Tür
            float sturdHeight = WALL_HEIGHT - DOOR_HEIGHT;
            CreateWall("Wall_Front_Doorstep",
                new Vector3(0f, DOOR_HEIGHT + sturdHeight * 0.5f, -halfD),
                new Vector3(DOOR_WIDTH, sturdHeight, WALL_THICK));
        }

        private void BuildMachinePlaceholders()
        {
            // Platzhalter-Automaten an der linken Wand
            float startZ    = (ROOM_DEPTH * 0.5f) - 2f;
            float spacingZ  = 2.5f;
            float xPos      = -(ROOM_WIDTH * 0.5f) + 0.8f;

            for (int i = 0; i < placeholderMachineCount; i++)
            {
                float z = startZ - i * spacingZ;
                var ph = CreatePrimitive(
                    $"MachinePlaceholder_{i + 1}",
                    new Vector3(xPos, 0.75f, z),
                    new Vector3(0.6f, 1.5f, 0.5f),
                    machinePlaceholderMaterial
                );
                ph.layer = _machineLayer;

                // Schlichtes Tag damit PlayerInteraction es erkennt
                ph.tag = "Interactable";
            }
        }

        private GameObject CreateWall(string wallName, Vector3 center, Vector3 size)
        {
            var wall = CreatePrimitive(wallName, center, size, wallMaterial);
            wall.layer = _wallLayer;

            var col = wall.GetComponent<Collider>();
            if (col != null)
            {
                var pm = new PhysicMaterial("Wall_Physics");
                pm.dynamicFriction = 0.4f;
                pm.staticFriction  = 0.4f;
                pm.bounciness      = 0f;
                col.material = pm;
            }

            // Als Static markieren (für Occlusion Culling & Batching)
            wall.isStatic = true;
            return wall;
        }

        private GameObject CreatePrimitive(string objName, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objName;
            go.transform.SetParent(_shopRoot);
            go.transform.position = pos;
            go.transform.localScale = scale;

            if (mat != null)
            {
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                    rend.material = mat;
            }

            return go;
        }

        private void EnsureLayers()
        {
            _floorLayer   = LayerMask.NameToLayer("Floor");
            _wallLayer    = LayerMask.NameToLayer("Wall");
            _machineLayer = LayerMask.NameToLayer("Machine");

            // Fallback auf Default-Layer wenn noch nicht angelegt
            if (_floorLayer   < 0) _floorLayer   = 0;
            if (_wallLayer    < 0) _wallLayer    = 0;
            if (_machineLayer < 0) _machineLayer = 0;
        }
    }
}
