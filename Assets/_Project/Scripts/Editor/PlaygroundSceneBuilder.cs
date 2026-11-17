using Listonosz.Core;
using Listonosz.Data;
using Listonosz.Gameplay;
using Listonosz.Grid;
using Listonosz.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Listonosz.EditorTools
{
    /// <summary>
    /// Generator grywalnej sceny testowej. Działa dwuetapowo, bo Unity nie potrafi w jednym wywołaniu
    /// utworzyć assetu ScriptableObject i zapisać scenę, która go referuje (AssetDatabase nie zdąży go
    /// zarejestrować — referencja scena->asset zapisuje się jako null). Dlatego:
    ///   Krok 1 (BuildAssets) tworzy assety (pola wpieczone PRZED CreateAsset),
    ///   Krok 2 (BuildScene) ładuje je z dysku i buduje + wiąże scenę.
    /// W GUI „Zbuduj scenę testową" odpala Krok 1, a Krok 2 w następnym ticku edytora (delayCall).
    /// </summary>
    public static class PlaygroundSceneBuilder
    {
        private const string GenDir = "Assets/_Project/Settings/Generated";
        private const string ScenePath = "Assets/_Project/Scenes/Playground.unity";

        /// <summary>Adres miasteczka: klucz nazw (Addr_/Del_/Cel_), nazwa, komórka, typ, priorytet, waga.</summary>
        internal readonly struct TownAddress
        {
            public readonly string Key;
            public readonly string Display;
            public readonly Vector2Int Cell;
            public readonly DeliveryType Type;
            public readonly DeliveryPriority Prio;
            public readonly float Weight;

            public TownAddress(string key, string display, Vector2Int cell, DeliveryType type, DeliveryPriority prio, float weight)
            {
                Key = key;
                Display = display;
                Cell = cell;
                Type = type;
                Prio = prio;
                Weight = weight;
            }
        }

        /// <summary>Wspólna tabela adresów — używana przez generator assetów, sceny i oprawę.</summary>
        internal static readonly TownAddress[] Town =
        {
            new TownAddress("Kwiatowa", "ul. Kwiatowa 12", new Vector2Int(10, 6), DeliveryType.Registered, DeliveryPriority.High, 0.1f),
            new TownAddress("Lipowa", "ul. Lipowa 5", new Vector2Int(14, 11), DeliveryType.SmallParcel, DeliveryPriority.Normal, 1.5f),
            new TownAddress("Polna", "ul. Polna 3", new Vector2Int(7, 14), DeliveryType.LargeParcel, DeliveryPriority.Low, 4f),
            new TownAddress("Sloneczna", "ul. Sloneczna 7", new Vector2Int(16, 3), DeliveryType.SmallParcel, DeliveryPriority.Normal, 1.2f),
            new TownAddress("Morska", "ul. Morska 2", new Vector2Int(4, 12), DeliveryType.Registered, DeliveryPriority.High, 0.2f),
            new TownAddress("Lesna", "ul. Lesna 9", new Vector2Int(17, 8), DeliveryType.LargeParcel, DeliveryPriority.Low, 5f),
            new TownAddress("Parkowa", "ul. Parkowa 1", new Vector2Int(11, 17), DeliveryType.Letter, DeliveryPriority.Normal, 0.1f),
        };

        private const int DailyGoal = 5;

        private static string LabelFor(DeliveryType type)
        {
            switch (type)
            {
                case DeliveryType.Registered: return "List polecony";
                case DeliveryType.SmallParcel: return "Paczka";
                case DeliveryType.LargeParcel: return "Duza paczka";
                default: return "List";
            }
        }

        [MenuItem("Listonosz/Zbuduj scenę testową")]
        public static void Build()
        {
            BuildAssets();
            EditorApplication.delayCall += BuildScene;
        }

        [MenuItem("Listonosz/Krok 1 — utwórz assety")]
        public static void BuildAssets()
        {
            EnsureFolder(GenDir);

            MakeAsset<GridConfig>("GridConfig", so =>
            {
                so.FindProperty("cellSize").floatValue = 2f;
                so.FindProperty("dimensions").vector2IntValue = new Vector2Int(20, 20);
                so.FindProperty("origin").vector3Value = Vector3.zero;
                so.FindProperty("drawGizmos").boolValue = true;
            });

            MakeAsset<DeliveryEventChannelSO>("DE_PickedUp", null);
            MakeAsset<DeliveryEventChannelSO>("DE_Completed", null);
            MakeAsset<DeliveryEventChannelSO>("DE_Failed", null);
            MakeAsset<GameStateEventChannelSO>("GS_State", null);

            var deliveries = new System.Collections.Generic.List<DeliverySO>();
            foreach (TownAddress a in Town)
            {
                AddressPointSO addr = MakeAddress("Addr_" + a.Key, a.Display, a.Cell);
                deliveries.Add(MakeDelivery("Del_" + a.Key, LabelFor(a.Type), addr, a.Type, a.Weight, a.Prio));
            }

            MakeAsset<DeliveryDatabaseSO>("Day01", so =>
            {
                so.FindProperty("dayLabel").stringValue = "Dzien 1";
                SerializedProperty list = so.FindProperty("deliveries");
                list.arraySize = deliveries.Count;
                for (int i = 0; i < deliveries.Count; i++)
                {
                    list.GetArrayElementAtIndex(i).objectReferenceValue = deliveries[i];
                }
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Listonosz] Krok 1: assety utworzone w " + GenDir);
        }

        [MenuItem("Listonosz/Krok 2 — zbuduj scenę")]
        public static void BuildScene()
        {
            GridConfig grid = Load<GridConfig>("GridConfig");
            DeliveryEventChannelSO chPicked = Load<DeliveryEventChannelSO>("DE_PickedUp");
            DeliveryEventChannelSO chDone = Load<DeliveryEventChannelSO>("DE_Completed");
            DeliveryEventChannelSO chFail = Load<DeliveryEventChannelSO>("DE_Failed");
            GameStateEventChannelSO chState = Load<GameStateEventChannelSO>("GS_State");
            DeliveryDatabaseSO db = Load<DeliveryDatabaseSO>("Day01");

            if (grid == null || db == null || chPicked == null || chDone == null || chFail == null || chState == null)
            {
                Debug.LogError("[Listonosz] Brak assetow - najpierw uruchom Krok 1 (utworz assety).");
                return;
            }

            var addresses = new System.Collections.Generic.List<AddressPointSO>();
            foreach (TownAddress a in Town)
            {
                AddressPointSO addr = Load<AddressPointSO>("Addr_" + a.Key);
                if (addr == null)
                {
                    Debug.LogError("[Listonosz] Brak assetow - najpierw uruchom Krok 1 (utworz assety).");
                    return;
                }

                addresses.Add(addr);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var gridSys = new GridSystem(grid);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(20f, 0f, 20f);
            ground.transform.localScale = new Vector3(4f, 1f, 4f);

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            Object.DestroyImmediate(player.GetComponent<Collider>());
            player.AddComponent<CharacterController>();
            PlayerController pc = player.AddComponent<PlayerController>();
            player.AddComponent<PlayerInventory>();
            DeliveryService svc = player.AddComponent<DeliveryService>();
            player.AddComponent<TouchControls>();
            Vector3 playerCell = gridSys.CellToWorldCenter(new Vector2Int(4, 4));
            player.transform.position = new Vector3(playerCell.x, 1f, playerCell.z);

            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            FollowCamera follow = cam.gameObject.AddComponent<FollowCamera>();
            cam.transform.position = player.transform.position + new Vector3(0f, 12f, -9f);

            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pickup.name = "PunktOdbioru_Urzad";
            Vector3 pickupCell = gridSys.CellToWorldCenter(new Vector2Int(6, 4));
            pickup.transform.position = new Vector3(pickupCell.x, 0.5f, pickupCell.z);
            DeliveryPickupPoint pickupComp = pickup.AddComponent<DeliveryPickupPoint>();

            for (int i = 0; i < Town.Length; i++)
            {
                MakeTarget("Cel_" + Town[i].Key, addresses[i], grid, gridSys);
            }

            var loopGo = new GameObject("GameLoop");
            GameLoopManager loop = loopGo.AddComponent<GameLoopManager>();

            var hudGo = new GameObject("HUD");
            DeliveryHud deliveryHud = hudGo.AddComponent<DeliveryHud>();
            DayScreenHud dayHud = hudGo.AddComponent<DayScreenHud>();
            InteractionPromptHud promptHud = hudGo.AddComponent<InteractionPromptHud>();
            GameStatsHud statsHud = hudGo.AddComponent<GameStatsHud>();
            hudGo.AddComponent<TouchJoystickVisual>();

            // Systemy czasu rzeczywistego: znacznik celu + juice dostawy.
            var systemsGo = new GameObject("Systemy");
            TargetBeacon beacon = systemsGo.AddComponent<TargetBeacon>();
            DeliveryFeedback feedback = systemsGo.AddComponent<DeliveryFeedback>();

            var gridDebugGo = new GameObject("GridDebug");
            GridDebugView gridDebug = gridDebugGo.AddComponent<GridDebugView>();

            SetRef(pc, "gridConfig", grid);
            SetRef(pc, "cameraTransform", cam.transform);

            SetRef(svc, "onPickedUp", chPicked);
            SetRef(svc, "onCompleted", chDone);
            SetRef(svc, "onFailed", chFail);

            SetRef(pickupComp, "database", db);

            SetRef(loop, "dayDatabase", db);
            SetInt(loop, "dailyGoal", DailyGoal);
            SetRef(loop, "onCompleted", chDone);
            SetRef(loop, "onStateChanged", chState);
            SetRef(loop, "deliveryService", svc);

            SetRef(deliveryHud, "onPickedUp", chPicked);
            SetRef(deliveryHud, "onCompleted", chDone);
            SetRef(deliveryHud, "onFailed", chFail);
            SetRef(deliveryHud, "player", player.transform);
            SetRef(deliveryHud, "gridConfig", grid);

            SetRef(dayHud, "onStateChanged", chState);
            SetRef(dayHud, "onCompleted", chDone);
            SetRef(dayHud, "onFailed", chFail);
            SetInt(dayHud, "dailyGoalDisplay", DailyGoal);

            SetRef(promptHud, "deliveryService", svc);

            SetRef(follow, "target", player.transform);
            SetRef(gridDebug, "config", grid);

            SetRef(statsHud, "loop", loop);
            SetRef(dayHud, "loop", loop);

            SetRef(beacon, "onPickedUp", chPicked);
            SetRef(beacon, "onCompleted", chDone);
            SetRef(beacon, "onFailed", chFail);
            SetRef(beacon, "player", player.transform);
            SetRef(beacon, "gridConfig", grid);

            SetRef(feedback, "onCompleted", chDone);
            SetRef(feedback, "gridConfig", grid);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!buildScenes.Exists(s => s.path == ScenePath))
            {
                buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            Debug.Log($"[Listonosz] Krok 2: scena zbudowana: {ScenePath}. Naciśnij Play (cel dnia: 3 dostawy).");
        }

        // ---------- Helpery tworzenia assetów ----------

        private static AddressPointSO MakeAddress(string file, string displayName, Vector2Int cell)
        {
            return MakeAsset<AddressPointSO>(file, so =>
            {
                so.FindProperty("addressId").stringValue = file;
                so.FindProperty("displayName").stringValue = displayName;
                so.FindProperty("cell").vector2IntValue = cell;
            });
        }

        private static DeliverySO MakeDelivery(string file, string label, AddressPointSO dest, DeliveryType type, float weight, DeliveryPriority prio)
        {
            return MakeAsset<DeliverySO>(file, so =>
            {
                so.FindProperty("id").stringValue = file;
                so.FindProperty("label").stringValue = label;
                so.FindProperty("destination").objectReferenceValue = dest;
                so.FindProperty("type").enumValueIndex = (int)type;
                so.FindProperty("weightKg").floatValue = weight;
                so.FindProperty("priority").enumValueIndex = (int)prio;
            });
        }

        private static DeliveryTarget MakeTarget(string name, AddressPointSO address, GridConfig grid, GridSystem gridSys)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Vector3 c = gridSys.CellToWorldCenter(address.Cell);
            go.transform.position = new Vector3(c.x, 0.5f, c.z);
            DeliveryTarget t = go.AddComponent<DeliveryTarget>();
            SetRef(t, "address", address);
            SetRef(t, "gridConfig", grid);
            return t;
        }

        private static T MakeAsset<T>(string fileNameNoExt, System.Action<SerializedObject> configure) where T : ScriptableObject
        {
            string path = $"{GenDir}/{fileNameNoExt}.asset";
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            T instance = ScriptableObject.CreateInstance<T>();
            if (configure != null)
            {
                var so = new SerializedObject(instance);
                configure(so);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.CreateAsset(instance, path);
            return instance;
        }

        private static T Load<T>(string fileNameNoExt) where T : ScriptableObject
        {
            return AssetDatabase.LoadAssetAtPath<T>($"{GenDir}/{fileNameNoExt}.asset");
        }

        // ---------- Helpery niskopoziomowe ----------

        private static void SetField(Object target, string fieldName, object value)
        {
            System.Reflection.FieldInfo field = null;
            for (System.Type t = target.GetType(); t != null && field == null; t = t.BaseType)
            {
                field = t.GetField(fieldName, System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            }

            if (field == null)
            {
                Debug.LogError($"[Listonosz] Brak pola '{fieldName}' w {target.GetType().Name}.");
                return;
            }

            field.SetValue(target, value);
            EditorUtility.SetDirty(target);
        }

        private static void SetRef(Object target, string fieldName, Object value) => SetField(target, fieldName, value);

        private static void SetInt(Object target, string fieldName, int value) => SetField(target, fieldName, value);

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
