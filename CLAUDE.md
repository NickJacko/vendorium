# VENDORIUM — Projekt-Kontext für Claude

## Was ist Vendorium?
3D First-Person Tycoon-Spiel. Spieler betreibt einen Automatenladen, kauft/platziert Automaten, kassiert Kunden, erweitert den Laden.
Engine: Unity 2023 LTS · Sprache: C# · Ziel: Steam (PC/Mac)
Entwicklungsplan: `VENDORIUM_Entwicklungsplan_v1.0.docx`

## Namespace
Alles im Namespace `Vendorium`. Keine Ausnahmen. Editor-Scripts: `Vendorium.Editor`.

---

## Architektur

### Manager-Pattern
- Alle Manager erben von `Singleton<T>` (`Assets/Scripts/Utils/Singleton.cs`)
- Kommunikation **nur** über `VendoriumEventManager` — nie direkte Manager-zu-Manager-Aufrufe
- `GameManager` hält Referenzen auf alle Sub-Manager als `[SerializeField]`
- `GameManager.Economy`, `GameManager.Customers`, `GameManager.Machines`, `GameManager.UI`, `GameManager.Save`, `GameManager.Audio` als statische Shortcuts

### Player-Prefab Hierarchie
```
Player (Root)
  ├── CharacterController
  ├── AudioSource
  ├── PlayerController.cs
  └── CameraHolder (Y=1.6)
        ├── HeadBobEffect.cs
        └── MainCamera
              ├── Camera (FOV=75, NearClip=0.1)
              └── PlayerInteraction.cs
```

### IInteractable
Alle anklickbaren Objekte implementieren `IInteractable` (`Assets/Scripts/Utils/IInteractable.cs`).
`PlayerInteraction` macht Raycast auf Layer "Interactable" und ruft `Interact(PlayerController)` auf.

### Enums
Alle in `Assets/Scripts/Utils/GameState.cs`: GameState, MachineTrait, MachineState, CustomerType, CustomerState, UIScreen, EventType, TimeOfDay, ...

### Object Pooling
Generischer `ObjectPool<T>` in `Assets/Scripts/Utils/ObjectPool.cs`.
`ParticlePool` für Partikel-Systeme. `CustomerManager` verwaltet den Kunden-Pool intern.

---

## Komplette Script-Übersicht

### Utils
| Datei | Inhalt |
|---|---|
| `GameState.cs` | Alle Enums |
| `IInteractable.cs` | Interface für anklickbare Objekte |
| `Singleton.cs` | Generische Basisklasse |
| `ObjectPool.cs` | Generischer Pool + ParticlePool |
| `PerformanceManager.cs` | FPS-Monitor, Performance-Report, NavMesh-Optimierung |
| `PreBuildChecklist.cs` | Editor-Script (Menü: Vendorium → Pre-Build Checkliste) |

### Managers
| Datei | Inhalt |
|---|---|
| `GameManager.cs` | Orchestrator, GameState-Maschine, globaler Input |
| `SaveManager.cs` | JSON + Backup, AutoSave 60s, 3 Slots |
| `AudioManager.cs` | SFX-Pool (10), Musik-Fade, PlayerPrefs-Lautstärken |
| `AudioData.cs` | ScriptableObject für alle AudioClips |
| `ShopLayoutBuilder.cs` | Laden-Blockout aus Primitives |
| `RoomData.cs` | ScriptableObject für Räume |
| `RoomManager.cs` | Räume freischalten, Persistierung |
| `LockedWall.cs` | IInteractable-Wand, Sink-Animation, NavMesh rebuild |
| `DayNightCycle.cs` | Sonnen-Rotation, Lampen, Neon-Intensität |
| `SceneLoader.cs` | Async-Laden mit Fade-In/Out |

### Economy
| Datei | Inhalt |
|---|---|
| `EconomyManager.cs` | Geld, Tagesstatistiken, 5-Min-Tageszyklus, Tageszeit |
| `CashRegisterManager.cs` | Kassierer-Modus, Warteschlange, Kamera-Wechsel |
| `CashierTrigger.cs` | IInteractable-Zone hinter der Theke |

### Machines
| Datei | Inhalt |
|---|---|
| `MachineData.cs` + `MachineDatabase.cs` | ScriptableObjects |
| `MachineManager.cs` | Registrierung, Radius-Abfragen |
| `VendingMachine.cs` | Income-Loop, Traits, Upgrade, Restock, IInteractable |
| `MachineTriggerZone.cs` | Sphere-Trigger (2m) für Kunden-Erkennung |
| `MachineSalesEffect.cs` | Coin-Partikel + Sound bei Verkauf |
| `PlacementManager.cs` | Ghost-Automat, Grid-Snapping, R=Rotation |
| `SynergyRule.cs` | ScriptableObject für Synergie-Regeln |
| `SynergyManager.cs` | Radius-Scan, LineRenderer, Discovery-Event |

### Customers
| Datei | Inhalt |
|---|---|
| `CustomerData.cs` | ScriptableObject |
| `CustomerManager.cs` | Spawn-Pool, Mundpropaganda, Stammkunden |
| `CustomerController.cs` | NavMeshAgent, 6 States, Präferenz-Kauf, Emotionen |

### Events
| Datei | Inhalt |
|---|---|
| `VendoriumEventManager.cs` | Zentraler Event-Bus (alle C# Events) |
| `EventData.cs` | ScriptableObject für Random Events |
| `RandomEventManager.cs` | 5 Event-Typen, alle 3 Spielminuten |

### Story
| Datei | Inhalt |
|---|---|
| `DialogueData.cs` | ScriptableObject (Lines, Choices) |
| `StoryManager.cs` | Story-Flags, Kapitel-1-Trigger |
| `DialogueSystem.cs` | Typewriter, Portrait, Choices |

### Player
| Datei | Inhalt |
|---|---|
| `PlayerStats.cs` | ScriptableObject |
| `PlayerController.cs` | CharacterController, WASD, MouseLook, Sprung |
| `HeadBobEffect.cs` | Sanfter Kamera-Bob |
| `PlayerInteraction.cs` | Raycast → IInteractable, Outline |

### UI
| Datei | Inhalt |
|---|---|
| `UIManager.cs` | Screen-Stack, Cursor-Steuerung |
| `HUD.cs` | Geld-Flash, Tageszeit-Icon, Tagnummer |
| `InteractionPrompt.cs` | Fade-CanvasGroup, TMP |
| `MachineInspectPanel.cs` | Slide-In, Stock/Upgrade/Restock |
| `ShopScreen.cs` + `MachineCard.cs` | Tab-Menü, Karten-Grid, Kauf-Flow |
| `CashRegisterUI.cs` | Kassier-Panel, Tagesbilanz |
| `ChangeMakingMinigame.cs` | 10s Timer, Münz-Buttons, Bonus |
| `DailyReportPanel.cs` | Tagesbericht, Wochenverlauf-Balken |
| `EventNotificationPanel.cs` | Slide-In Banner von oben |
| `MainMenu.cs` | Logo, Hover-Sounds, Save-Slot-Flow |
| `SaveSlotUI.cs` | 3-Slot-Auswahl |
| `PauseMenu.cs` | Fade-In, Speichern vor Menü-Wechsel |
| `SettingsPanel.cs` | Grafik, Audio, Maus-Sensitivity, PlayerPrefs |

---

## Unity-Setup (manuell nach Klonen)
1. Unity 2023.3 LTS + Unity Hub installieren
2. Neues Projekt: Template = 3D Core, Name = Vendorium, diesen Ordner als Speicherort
3. `Window → Package Manager` → `com.unity.nuget.newtonsoft-json` installieren
4. TextMeshPro: `Window → TextMeshPro → Import TMP Essentials`
5. (Optional) DOTween: `http://dotween.demigiant.com` (für weichere Animationen)
6. GameManager-GameObject in GameScene anlegen, `GameManager.cs` zuweisen
7. ScriptableObjects erstellen: `Assets → Create → VendoriumData → ...`
8. Layer anlegen: Floor, Wall, Machine, Interactable, Player, Customer
9. Tags anlegen: Player, Customer, Interactable, Wall, Floor
10. NavMesh backen: `Window → AI → Navigation → Bake`

## Coding-Konventionen
- Private Felder: `_camelCase`
- Properties/Methoden: `PascalCase`
- Keine Update()-Polling-Schleifen wenn Events reichen
- Object Pooling für Kunden, Partikel und SFX
- Kommentare auf Deutsch (laut Entwicklungsplan)
- `[SerializeField]` statt `public` für Inspector-Felder

## Nächste Schritte
Das Projekt ist **Code-vollständig** (alle 7 Phasen implementiert).
Verbleibende Arbeit ist rein in Unity Editor:
1. Unity-Projekt anlegen und Scripts importieren
2. Szenen aufbauen (GameScene, MainMenu)
3. Prefabs erstellen (Player, Automaten, Kunden)
4. ScriptableObjects befüllen (3 Maschinen: Alte Hilde, Knabberbert, Sprudelmax)
5. UI-Canvas und Panels aufbauen
6. NavMesh backen
7. Sounds importieren (freesound.org)
8. Pre-Build Checkliste laufen lassen: `Vendorium → Pre-Build Checkliste`
