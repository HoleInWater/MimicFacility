using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Characters;
using MimicFacility.Entities;
using MimicFacility.AI.Director;
using MimicFacility.AI.LLM;
using MimicFacility.AI.Persistence;
using MimicFacility.AI.Weapons;
using MimicFacility.AI.Voice;
using MimicFacility.Audio;
using MimicFacility.Facility;
using MimicFacility.Gameplay;
using MimicFacility.Horror;
using MimicFacility.Lore;
using MimicFacility.UI;

namespace MimicFacility.Testing
{
    public class TestSceneBootstrap : MonoBehaviour
    {
        [Header("What To Spawn")]
        [SerializeField] private bool generateMap = true;
        [SerializeField] private bool spawnPlayer = true;
        [SerializeField] private bool spawnDirector = true;
        [SerializeField] private bool spawnEntities = true;
        [SerializeField] private bool spawnGear = true;
        [SerializeField] private bool setupUI = true;
        [SerializeField] private bool setupAudio = true;

        [Header("Entity Counts")]
        [SerializeField] private int mimicCount = 2;
        [SerializeField] private int stalkerCount = 1;
        [SerializeField] private int fraudCount = 1;
        [SerializeField] private int phantomCount = 1;

        [Header("Map Settings")]
        [SerializeField] private int roomCount = 6;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        private TestMapGenerator mapGenerator;
        private GameObject playerObj;

        private void Start()
        {
            StartCoroutine(BootstrapSequence());
        }

        private IEnumerator BootstrapSequence()
        {
            Log("=== MimicFacility Test Bootstrap Starting ===");

            SetupCoreSystems();
            yield return null;

            if (generateMap)
            {
                GenerateTestMap();
                yield return null;
            }

            if (spawnPlayer)
            {
                SpawnTestPlayer();
                yield return null;
            }

            if (spawnDirector)
            {
                SpawnDirectorAI();
                yield return null;
            }

            if (setupAudio)
            {
                SetupAudioSystems();
                yield return null;
            }

            if (setupUI)
            {
                SetupUICanvas();
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            if (spawnEntities)
            {
                SpawnTestEntities();
                yield return null;
            }

            if (spawnGear)
            {
                SpawnTestGear();
                yield return null;
            }

            EnsureNetworkIdentities();

            Log("=== Bootstrap Complete — All Systems Online ===");
            Log($"Rooms: {roomCount} | Mimics: {mimicCount} | Stalkers: {stalkerCount} | Frauds: {fraudCount}");
            Log("Controls: WASD move, Mouse look, E interact, LMB use gear, F flashlight, V push-to-talk, ESC pause");
        }

        private void SetupCoreSystems()
        {
            Log("Setting up core systems...");

            if (GameManager.Instance == null)
            {
                var gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
                Log("  Created GameManager");
            }

            if (SettingsManager.Instance == null)
            {
                var smObj = new GameObject("SettingsManager");
                smObj.AddComponent<SettingsManager>();
                Log("  Created SettingsManager");
            }

            if (FindObjectOfType<FallbackInputManager>() == null && InputManager.Instance == null)
            {
                var imObj = new GameObject("InputManager");
                imObj.AddComponent<FallbackInputManager>();
                Log("  Created FallbackInputManager (old Input system)");
            }

            if (FindObjectOfType<SessionTracker>() == null)
            {
                var stObj = new GameObject("SessionTracker");
                stObj.AddComponent<SessionTracker>();
                Log("  Created SessionTracker");
            }

            if (FindObjectOfType<RoundManager>() == null)
            {
                var rmObj = new GameObject("RoundManager");
                rmObj.AddComponent<RoundManager>();
                Log("  Created RoundManager");
            }

            if (FindObjectOfType<NetworkedGameState>() == null)
            {
                var gsObj = new GameObject("NetworkedGameState");
                gsObj.AddComponent<NetworkedGameState>();
                Log("  Created NetworkedGameState");
            }

            if (FindObjectOfType<VerificationSystem>() == null)
            {
                var vsObj = new GameObject("VerificationSystem");
                vsObj.AddComponent<VerificationSystem>();
                Log("  Created VerificationSystem");
            }

            if (FindObjectOfType<DiagnosticTaskManager>() == null)
            {
                var dtObj = new GameObject("DiagnosticTaskManager");
                dtObj.AddComponent<DiagnosticTaskManager>();
                Log("  Created DiagnosticTaskManager");
            }

            if (FindObjectOfType<LoreDatabase>() == null)
            {
                var ldObj = new GameObject("LoreDatabase");
                ldObj.AddComponent<LoreDatabase>();
                Log("  Created LoreDatabase");
            }
        }

        private void GenerateTestMap()
        {
            Log("Generating test facility...");

            var mapObj = new GameObject("MapGenerator");
            mapGenerator = mapObj.AddComponent<TestMapGenerator>();

            var field = typeof(TestMapGenerator).GetField("roomCount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(mapGenerator, roomCount);

            mapGenerator.GenerateMap();
            Log($"  Generated {roomCount} rooms with corridors, doors, lights, vents, terminals");
        }

        private void SpawnTestPlayer()
        {
            Log("Spawning test player...");

            playerObj = new GameObject("TestPlayer");
            playerObj.tag = "Player";

            var cc = playerObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var camObj = new GameObject("PlayerCamera");
            camObj.transform.SetParent(playerObj.transform);
            camObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.1f;
            camObj.AddComponent<AudioListener>();

            var flashlightObj = new GameObject("Flashlight");
            flashlightObj.transform.SetParent(camObj.transform);
            flashlightObj.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
            var spot = flashlightObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.intensity = 3f;
            spot.range = 20f;
            spot.spotAngle = 35f;
            spot.enabled = false;

            playerObj.AddComponent<AudioSource>();
            playerObj.AddComponent<PlayerCharacter>();
            playerObj.AddComponent<MimicPlayerState>();

            if (mapGenerator != null && mapGenerator.RoomCenters.Count > 0)
                playerObj.transform.position = mapGenerator.RoomCenters[0] + Vector3.up * 1f;
            else
                playerObj.transform.position = Vector3.up * 1f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Log("  Player spawned with CharacterController, Camera, Flashlight, AudioSource");
        }

        private void SpawnDirectorAI()
        {
            Log("Spawning Director AI...");

            var dirObj = new GameObject("DirectorAI");
            dirObj.AddComponent<OllamaClient>();
            dirObj.AddComponent<CorruptionTracker>();
            dirObj.AddComponent<DirectorMemory>();
            dirObj.AddComponent<PersonalWeaponSystem>();
            dirObj.AddComponent<VoiceLearningSystem>();
            dirObj.AddComponent<DirectorAI>();
            dirObj.AddComponent<FacilityControlSystem>();

            var horrorObj = new GameObject("DeviceHorror");
            horrorObj.transform.SetParent(dirObj.transform);
            horrorObj.AddComponent<DeviceHorrorManager>();

            Log("  Director online with LLM client, corruption, weapons, facility control, horror tricks");
        }

        private void SpawnTestEntities()
        {
            Log("Spawning test entities...");
            var centers = mapGenerator != null ? mapGenerator.RoomCenters : new List<Vector3> { Vector3.zero };

            for (int i = 0; i < mimicCount; i++)
            {
                SpawnEntity<MimicBase>("Mimic", centers, i);
            }

            for (int i = 0; i < stalkerCount; i++)
            {
                SpawnEntity<Stalker>("Stalker", centers, mimicCount + i);
            }

            for (int i = 0; i < fraudCount; i++)
            {
                SpawnEntity<Fraud>("Fraud", centers, mimicCount + stalkerCount + i);
            }

            for (int i = 0; i < phantomCount; i++)
            {
                SpawnEntity<Phantom>("Phantom", centers, mimicCount + stalkerCount + fraudCount + i);
            }

            Log($"  Spawned {mimicCount} mimics, {stalkerCount} stalkers, {fraudCount} frauds, {phantomCount} phantoms");
        }

        private void SpawnEntity<T>(string name, List<Vector3> centers, int index) where T : Component
        {
            var obj = new GameObject($"{name}_{index}");

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(obj.transform);
            capsule.transform.localPosition = Vector3.up * 0.5f;
            capsule.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            var renderer = capsule.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));

            if (typeof(T) == typeof(MimicBase))
                renderer.material.color = new Color(0.8f, 0.2f, 0.2f);
            else if (typeof(T) == typeof(Stalker))
                renderer.material.color = new Color(0.1f, 0.1f, 0.1f);
            else if (typeof(T) == typeof(Fraud))
                renderer.material.color = new Color(0.9f, 0.7f, 0.1f);
            else if (typeof(T) == typeof(Phantom))
                renderer.material.color = new Color(0.3f, 0.3f, 0.8f, 0.5f);

            var agent = obj.AddComponent<NavMeshAgent>();
            agent.speed = 3.5f;
            agent.angularSpeed = 360f;
            obj.AddComponent<CapsuleCollider>();
            var rb = obj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            obj.AddComponent<AudioSource>();
            obj.AddComponent<T>();

            Vector3 spawnPos = centers[index % centers.Count]
                + new Vector3(Random.Range(-3f, 3f), 0.5f, Random.Range(-3f, 3f));

            if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                obj.transform.position = hit.position;
            else
                obj.transform.position = spawnPos;
        }

        private void SpawnTestGear()
        {
            Log("Spawning test gear...");

            var gearPoints = FindObjectsOfType<Transform>();
            int gearCount = 0;

            foreach (var t in gearPoints)
            {
                if (t.name != "GearSpawnPoint") continue;
                if (gearCount >= 12) break;

                var gearObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gearObj.transform.position = t.position;
                gearObj.transform.localScale = Vector3.one * 0.3f;

                var renderer = gearObj.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));

                switch (gearCount % 4)
                {
                    case 0:
                        gearObj.name = "Flashlight_Pickup";
                        renderer.material.color = Color.white;
                        renderer.material.SetColor("_EmissionColor", Color.white * 0.3f);
                        renderer.material.EnableKeyword("_EMISSION");
                        break;
                    case 1:
                        gearObj.name = "AudioScanner_Pickup";
                        renderer.material.color = Color.cyan;
                        renderer.material.SetColor("_EmissionColor", Color.cyan * 0.3f);
                        renderer.material.EnableKeyword("_EMISSION");
                        break;
                    case 2:
                        gearObj.name = "ContainmentDevice_Pickup";
                        renderer.material.color = Color.red;
                        renderer.material.SetColor("_EmissionColor", Color.red * 0.3f);
                        renderer.material.EnableKeyword("_EMISSION");
                        break;
                    case 3:
                        gearObj.name = "SignalJammer_Pickup";
                        renderer.material.color = Color.magenta;
                        renderer.material.SetColor("_EmissionColor", Color.magenta * 0.3f);
                        renderer.material.EnableKeyword("_EMISSION");
                        break;
                }

                gearObj.AddComponent<SphereCollider>().isTrigger = true;
                gearObj.AddComponent<Rigidbody>().isKinematic = true;
                gearCount++;
            }

            Log($"  Spawned {gearCount} gear pickups across the facility");
        }

        private void SetupAudioSystems()
        {
            Log("Setting up audio...");

            if (FindObjectOfType<SpatialAudioProcessor>() == null)
            {
                var audioObj = new GameObject("SpatialAudioProcessor");
                audioObj.AddComponent<SpatialAudioProcessor>();
                Log("  Created SpatialAudioProcessor");
            }

            RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.03f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.03f;
        }

        private void SetupUICanvas()
        {
            Log("Setting up UI...");

            if (FindObjectOfType<Canvas>() != null) return;

            var canvasObj = new GameObject("UICanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            Log("  Created UI Canvas");
        }

        private void EnsureNetworkIdentities()
        {
            Log("Adding NetworkIdentity to all NetworkBehaviours...");
            int count = 0;
            foreach (var nb in FindObjectsOfType<NetworkBehaviour>())
            {
                if (nb.GetComponent<NetworkIdentity>() == null)
                {
                    nb.gameObject.AddComponent<NetworkIdentity>();
                    count++;
                }
            }
            Log($"  Added NetworkIdentity to {count} objects");
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.green;

            float y = 10f;
            GUI.Label(new Rect(10, y, 400, 20), "=== MimicFacility Debug ===", style);
            y += 20;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                GUI.Label(new Rect(10, y, 400, 20), $"Phase: {gm.CurrentPhase} | Players: {gm.Players.Count}", style);
                y += 20;
            }

            var director = FindObjectOfType<DirectorAI>();
            if (director != null)
            {
                GUI.Label(new Rect(10, y, 400, 20), $"Director Phase: {director.CurrentPhase}", style);
                y += 20;
            }

            var corruption = FindObjectOfType<CorruptionTracker>();
            if (corruption != null)
            {
                GUI.Label(new Rect(10, y, 400, 20), $"Corruption: {corruption.CorruptionIndex}", style);
                y += 20;
            }

            int entityCount = FindObjectsOfType<MimicBase>().Length
                + FindObjectsOfType<Stalker>().Length
                + FindObjectsOfType<Fraud>().Length
                + FindObjectsOfType<Phantom>().Length;
            GUI.Label(new Rect(10, y, 400, 20), $"Active Entities: {entityCount}", style);
            y += 20;

            if (playerObj != null)
            {
                GUI.Label(new Rect(10, y, 400, 20), $"Player Pos: {playerObj.transform.position:F1}", style);
                y += 20;
            }

            style.fontSize = 12;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(10, y, 500, 20), "WASD:Move  Mouse:Look  E:Interact  LMB:UseGear  F:Light  V:Talk  ESC:Pause", style);
        }

        private void Log(string msg)
        {
            Debug.Log($"[Bootstrap] {msg}");
        }
    }
}
