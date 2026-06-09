#if UNITY_EDITOR
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vendorium.Editor
{
    // NavMesh für die GameScene backen — einmalig nach dem ersten Setup.
    // Menü: Vendorium → NavMesh backen
    public static class VendoriumNavMeshBaker
    {
        [MenuItem("Vendorium/NavMesh backen", priority = 3)]
        public static void BakeNavMesh()
        {
            const string scenePath = "Assets/Scenes/GameScene.unity";

            // Szene öffnen falls noch nicht geöffnet
            var scene = SceneManager.GetActiveScene();
            bool needsOpen = scene.path != scenePath;

            if (needsOpen)
            {
                bool save = EditorUtility.DisplayDialog(
                    "NavMesh backen",
                    "Die GameScene wird geöffnet um den NavMesh zu backen.\n" +
                    "Ungespeicherte Änderungen in der aktuellen Szene gehen verloren.\n\nFortfahren?",
                    "Ja, backen", "Abbrechen");

                if (!save) return;

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // Alle NavMeshSurface-Komponenten in der Szene finden und backen
            var surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);

            if (surfaces.Length == 0)
            {
                EditorUtility.DisplayDialog("Kein NavMesh",
                    "Keine NavMeshSurface-Komponente in der Szene gefunden.\n" +
                    "Führe zuerst 'Vendorium → Komplett-Setup ausführen' aus.",
                    "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("NavMesh", "Backe NavMesh...", 0f);

            try
            {
                for (int i = 0; i < surfaces.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("NavMesh",
                        $"Surface {i + 1}/{surfaces.Length}...",
                        (float)i / surfaces.Length);
                    surfaces[i].BuildNavMesh();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("NavMesh fertig",
                $"{surfaces.Length} NavMesh-Surface(s) gebacken und Szene gespeichert.\n\n" +
                "Das Spiel ist jetzt spielbereit!",
                "Super!");

            Debug.Log("[VendoriumNavMeshBaker] NavMesh erfolgreich gebacken.");
        }
    }
}
#endif
