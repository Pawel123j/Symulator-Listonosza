// Regression checks for the day loop, driven only through public entry points of the game code
// (GameManager.Instance, GameLoopManager.StartDay/BeginWork/EndDay + the "Advance" input,
// MobileInput.QueueInteract + a frame tick for DeliveryService). Unity itself is replaced by the
// local adapter in UnityStubs.cs. All data is synthetic (parcels P1/P2, addresses A/B).
//
// Run:  dotnet run --project tools/LogicTests      -> exit code 0 when every check passes.
using System;
using System.Collections.Generic;
using System.Reflection;
using Listonosz.Core;
using Listonosz.Data;
using Listonosz.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

internal static class Program
{
    private static int _failures;
    private static int _checks;

    private static int Main()
    {
        Run("A1 GameManager: two starts in one session (no teardown) keep a single instance", A1_TwoBootstrapsSameSession);
        Run("A2 GameManager: start, teardown, start again -> fresh, identical initial state", A2_TwoPlaySessions);
        Run("A3 GameManager: scene-placed duplicate is destroyed", A3_ScenePlacedDuplicate);
        Run("B1 Fixture: two starts of the same playground fixture -> same initial day state (best score persists)", B1_FixtureTwoStarts);
        Run("B2 Fixture: a tap pending when run 1 stops does not act in run 2 (auto-start, spawn at pickup)", B2_PendingTapDoesNotLeak);
        Run("C1 DayEnd(goal reached) -> Advance -> new round", C1_DayEndByGoalToNewRound);
        Run("C2 DayEnd(timeout, parcel carried) -> Advance -> new round", C2_DayEndByTimeoutToNewRound);
        Run("C3 Previously supported: autoStartWork skips the briefing on every new round", C3_AutoStartWork);

        Console.WriteLine();
        Console.WriteLine(_failures == 0
            ? $"PASSED: {_checks} checks"
            : $"FAILED: {_failures} of {_checks} checks");
        return _failures == 0 ? 0 : 1;
    }

    // ------------------------------------------------------------------ A: GameManager

    private static void A1_TwoBootstrapsSameSession()
    {
        EnterPlayMode();
        GameManager first = GameManager.Instance;
        EnterPlayMode(); // second start of the same fixture while the first one is still alive

        Eq("live GameManager objects", 1, FakeEngine.LiveCount<GameManager>());
        Eq("Instance is the first one", true, ReferenceEquals(first, GameManager.Instance));
        Eq("init log lines", 1, CountLogs("GameManager zainicjalizowany"));
        ExitPlayMode();
    }

    private static void A2_TwoPlaySessions()
    {
        EnterPlayMode();
        GameManager first = GameManager.Instance;
        int logsRun1 = CountLogs("GameManager zainicjalizowany");
        int liveRun1 = FakeEngine.LiveCount<GameManager>();
        ExitPlayMode();

        Eq("Instance after teardown is null", true, GameManager.Instance == null);

        EnterPlayMode();
        Eq("run 2: live GameManager objects", liveRun1, FakeEngine.LiveCount<GameManager>());
        Eq("run 2: init log lines", logsRun1, CountLogs("GameManager zainicjalizowany"));
        Eq("run 2: Instance is not the destroyed one", false, ReferenceEquals(first, GameManager.Instance));
        Eq("run 2: Instance is alive", true, GameManager.Instance != null);
        ExitPlayMode();
    }

    private static void A3_ScenePlacedDuplicate()
    {
        EnterPlayMode();
        GameManager boot = GameManager.Instance;
        var dup = new GameObject("GameManager (scene)").AddComponent<GameManager>();

        Eq("duplicate destroyed", true, dup == null);
        Eq("Instance unchanged", true, ReferenceEquals(boot, GameManager.Instance));
        Eq("live GameManager objects", 1, FakeEngine.LiveCount<GameManager>());
        ExitPlayMode();
    }

    // ------------------------------------------------------------------ B: fixture started twice

    private static void B1_FixtureTwoStarts()
    {
        PlayerPrefs.DeleteAll();

        // Run 1: play one full day, leave a tap queued and parcels in the bag at exit.
        EnterPlayMode();
        Fixture f1 = Fixture.Build();
        FakeEngine.Tick(0f);
        Snapshot s1 = Snapshot.Of(f1);
        PlayFullDay(f1);
        Eq("run 1 best score after the day", 305, f1.Loop.BestScore);
        Press(); // DayEnd -> DayStart
        Press(); // DayStart -> Working
        Interact(f1, f1.PickupPos);
        MobileInput.QueueInteract(); // pending tap left behind when play stops
        ExitPlayMode();

        Eq("registry empty after teardown", 0, InteractableRegistry.All.Count);

        // Run 2: same fixture again.
        EnterPlayMode();
        Fixture f2 = Fixture.Build();
        FakeEngine.Tick(0f);
        Snapshot s2 = Snapshot.Of(f2);

        Eq("initial State", s1.State, s2.State);
        Eq("initial State is DayStart", GameState.DayStart, s2.State);
        Eq("initial Completed", s1.Completed, s2.Completed);
        Eq("initial Score", s1.Score, s2.Score);
        Eq("initial Combo", s1.Combo, s2.Combo);
        Eq("initial TimeRemaining", s1.Time, s2.Time);
        Eq("initial bag", s1.Bag, s2.Bag);
        Eq("state events at start", "DayStart", string.Join(",", f2.States));
        Eq("BestScore run 1 (empty prefs)", 0, s1.Best);
        Eq("BestScore run 2 (persisted by design)", 305, s2.Best);
        Eq("registry holds only run-2 interactables", 3, InteractableRegistry.All.Count);
        ExitPlayMode();
    }

    private static void B2_PendingTapDoesNotLeak()
    {
        PlayerPrefs.DeleteAll();
        EnterPlayMode();
        Fixture f1 = Fixture.Build(autoStartWork: true);
        FakeEngine.Tick(0f);
        MobileInput.QueueInteract(); // tap registered in the last frame before play stops
        ExitPlayMode();

        EnterPlayMode();
        Fixture f2 = Fixture.Build(autoStartWork: true);
        f2.Player.transform.position = f2.PickupPos; // spawn inside the pickup radius
        FakeEngine.Tick(0f); // first frame: Start (-> Working) then Update
        FakeEngine.Tick(0f);
        Eq("run 2 bag after first frames (no input given)", 0, f2.Inventory.Count);
        Eq("run 2 pickup events", 0, f2.Picked.Count);
        ExitPlayMode();
    }

    // ------------------------------------------------------------------ C: DayEnd -> new round

    private static void C1_DayEndByGoalToNewRound()
    {
        PlayerPrefs.DeleteAll();
        EnterPlayMode();
        Fixture f = Fixture.Build();
        FakeEngine.Tick(0f);

        PlayFullDay(f);
        Eq("day 1 ended", GameState.DayEnd, f.Loop.State);
        Eq("day 1 score (100+15) + (150+15+25)", 305, f.Loop.Score);
        Eq("day 1 stars (goal met, full time left)", 3, f.Loop.StarRating);

        Press(); // single Advance in DayEnd
        Eq("one press -> DayStart (briefing not skipped)", GameState.DayStart, f.Loop.State);
        Eq("Completed reset", 0, f.Loop.CompletedCount);
        Eq("Score reset", 0, f.Loop.Score);
        Eq("Combo reset", 0, f.Loop.Combo);
        Eq("TimeRemaining reset", 60f, f.Loop.TimeRemaining);
        Eq("StarRating reset", 0, f.Loop.StarRating);
        Eq("BestScore kept", 305, f.Loop.BestScore);
        Eq("service CompletedCount reset", 0, f.Service.CompletedCount);
        Eq("bag empty", 0, f.Inventory.Count);
        Eq("state events", "DayStart,Working,DayEnd,DayStart", string.Join(",", f.States));

        int pickedBefore = f.Picked.Count;
        Interact(f, f.PickupPos);
        Eq("no pickup during briefing", pickedBefore, f.Picked.Count);

        Press(); // DayStart -> Working
        Interact(f, f.PickupPos);
        Eq("same parcels available again", "P1,P2", Labels(f.Inventory.Carried));
        Interact(f, f.TargetAPos);
        Eq("day 2 first delivery scores like day 1 (combo restarted)", 115, f.Loop.Score);
        ExitPlayMode();
    }

    private static void C2_DayEndByTimeoutToNewRound()
    {
        PlayerPrefs.DeleteAll();
        EnterPlayMode();
        Fixture f = Fixture.Build();
        FakeEngine.Tick(0f);

        Press(); // Working
        Interact(f, f.PickupPos);
        Interact(f, f.TargetAPos); // P1 delivered, P2 still in the bag
        FakeEngine.Tick(61f);      // time runs out

        Eq("day 1 ended by timeout", GameState.DayEnd, f.Loop.State);
        Eq("TimeRemaining clamped to 0", 0f, f.Loop.TimeRemaining);
        Eq("carried P2 failed exactly once", "P2", Labels(f.Failed));
        Eq("day 1 score", 115, f.Loop.Score);
        Eq("day 1 stars (50%)", 1, f.Loop.StarRating);

        Press(); // DayEnd -> DayStart
        Eq("DayStart", GameState.DayStart, f.Loop.State);
        Eq("TimeRemaining back to full", 60f, f.Loop.TimeRemaining);
        Eq("Score reset", 0, f.Loop.Score);
        Eq("no extra failures on reset", 1, f.Failed.Count);

        Press(); // Working
        FakeEngine.Tick(1f);
        Eq("still Working after 1 s (timer restarted, no instant DayEnd)", GameState.Working, f.Loop.State);
        Eq("TimeRemaining 59", 59f, f.Loop.TimeRemaining);
        Interact(f, f.PickupPos);
        Eq("both parcels (delivered and failed) available again", "P1,P2", Labels(f.Inventory.Carried));
        ExitPlayMode();
    }

    private static void C3_AutoStartWork()
    {
        PlayerPrefs.DeleteAll();
        EnterPlayMode();
        Fixture f = Fixture.Build(autoStartWork: true);
        FakeEngine.Tick(0f);
        Eq("auto start -> Working", GameState.Working, f.Loop.State);
        Interact(f, f.PickupPos);
        Interact(f, f.TargetAPos);
        Interact(f, f.TargetBPos);
        Eq("DayEnd", GameState.DayEnd, f.Loop.State);
        Press();
        Eq("new round goes straight to Working", GameState.Working, f.Loop.State);
        Eq("state events", "DayStart,Working,DayEnd,DayStart,Working", string.Join(",", f.States));
        Interact(f, f.PickupPos);
        Eq("pickup works in auto-started round", 2, f.Inventory.Count);
        ExitPlayMode();
    }

    // ------------------------------------------------------------------ helpers

    private static void PlayFullDay(Fixture f)
    {
        Press(); // DayStart -> Working
        Interact(f, f.PickupPos);
        Interact(f, f.TargetAPos);
        Interact(f, f.TargetBPos);
    }

    private static void Press() => InputAction.Simulate("<Keyboard>/enter");

    private static void Interact(Fixture f, Vector3 where)
    {
        f.Player.transform.position = where;
        FakeEngine.Tick(0f); // refresh interactable in range
        MobileInput.QueueInteract();
        FakeEngine.Tick(0f); // DeliveryService consumes the tap
    }

    /// <summary>Unity's play-mode entry: invoke [RuntimeInitializeOnLoadMethod] hooks in load order.
    /// Statics are NOT reset between sessions here — the stricter "domain reload disabled" case.</summary>
    private static void EnterPlayMode()
    {
        foreach (RuntimeInitializeLoadType phase in new[] { RuntimeInitializeLoadType.SubsystemRegistration, RuntimeInitializeLoadType.BeforeSceneLoad })
        {
            foreach (Type t in typeof(GameManager).Assembly.GetTypes())
            {
                foreach (MethodInfo m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var attr = m.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
                    if (attr != null && attr.LoadType == phase) m.Invoke(null, null);
                }
            }
        }
    }

    private static void ExitPlayMode() => FakeEngine.Reset();

    private static int CountLogs(string fragment)
    {
        int n = 0;
        foreach (string l in FakeEngine.Logs) if (l != null && l.Contains(fragment)) n++;
        return n;
    }

    private static string Labels(IEnumerable<DeliverySO> items)
    {
        var l = new List<string>();
        foreach (DeliverySO d in items) l.Add(d.Label);
        return string.Join(",", l);
    }

    private static void Run(string name, Action test)
    {
        Console.WriteLine($"== {name}");
        try { test(); }
        catch (Exception e) { _failures++; _checks++; Console.WriteLine($"   EXCEPTION {e.GetType().Name}: {e.Message}"); FakeEngine.Reset(); }
    }

    private static void Eq<T>(string what, T expected, T actual)
    {
        _checks++;
        bool ok = EqualityComparer<T>.Default.Equals(expected, actual);
        if (!ok) _failures++;
        Console.WriteLine($"   [{(ok ? "ok" : "FAIL")}] {what}: expected={expected} observed={actual}");
    }

    private struct Snapshot
    {
        public GameState State; public int Completed, Score, Combo, Best, Bag; public float Time;

        public static Snapshot Of(Fixture f) => new Snapshot
        {
            State = f.Loop.State, Completed = f.Loop.CompletedCount, Score = f.Loop.Score, Combo = f.Loop.Combo,
            Best = f.Loop.BestScore, Bag = f.Inventory.Count, Time = f.Loop.TimeRemaining,
        };
    }

    /// <summary>Synthetic playground: pickup at x=0, target A at x=10, target B at x=20, 60 s day, goal = all (2).</summary>
    private sealed class Fixture
    {
        public GameObject Player;
        public PlayerInventory Inventory;
        public DeliveryService Service;
        public GameLoopManager Loop;
        public readonly List<string> States = new List<string>();
        public readonly List<DeliverySO> Picked = new List<DeliverySO>();
        public readonly List<DeliverySO> Failed = new List<DeliverySO>();
        public Vector3 PickupPos = new Vector3(0f, 0f, 0f);
        public Vector3 TargetAPos = new Vector3(10f, 0f, 0f);
        public Vector3 TargetBPos = new Vector3(20f, 0f, 0f);

        public static Fixture Build(bool autoStartWork = false)
        {
            var f = new Fixture();
            var chPicked = ScriptableObject.CreateInstance<DeliveryEventChannelSO>();
            var chDone = ScriptableObject.CreateInstance<DeliveryEventChannelSO>();
            var chFail = ScriptableObject.CreateInstance<DeliveryEventChannelSO>();
            var chState = ScriptableObject.CreateInstance<GameStateEventChannelSO>();
            chPicked.OnEventRaised += d => f.Picked.Add(d);
            chFail.OnEventRaised += d => f.Failed.Add(d);
            chState.OnEventRaised += s => f.States.Add(s.ToString());

            var addrA = Address("A", "ul. Testowa 1");
            var addrB = Address("B", "ul. Testowa 2");
            var p1 = Parcel("P1", addrA, DeliveryPriority.Normal);
            var p2 = Parcel("P2", addrB, DeliveryPriority.High);
            var db = ScriptableObject.CreateInstance<DeliveryDatabaseSO>();
            Set(db, "dayLabel", "Dzień testowy");
            Set(db, "deliveries", new List<DeliverySO> { p1, p2 });

            // Player: inventory + service. Serialized refs are set before Awake/OnEnable matter,
            // mirroring the scene asset; channels are only read at runtime so late assignment is fine.
            f.Player = new GameObject("Player");
            f.Service = f.Player.AddComponent<DeliveryService>();
            f.Inventory = f.Player.GetComponent<PlayerInventory>();
            Set(f.Service, "onPickedUp", chPicked);
            Set(f.Service, "onCompleted", chDone);
            Set(f.Service, "onFailed", chFail);
            Set(f.Service, "logActions", false);

            Place<DeliveryPickupPoint>("Pickup", f.PickupPos, c => Set(c, "database", db));
            Place<DeliveryTarget>("TargetA", f.TargetAPos, c => Set(c, "address", addrA));
            Place<DeliveryTarget>("TargetB", f.TargetBPos, c => Set(c, "address", addrB));

            // GameLoopManager subscribes to channels in OnEnable, so wire fields before enabling:
            var loopGo = new GameObject("GameLoop");
            f.Loop = AddWired<GameLoopManager>(loopGo, l =>
            {
                Set(l, "dayDatabase", db);
                Set(l, "dailyGoal", 0);
                Set(l, "dayDurationSeconds", 60f);
                Set(l, "autoStartWork", autoStartWork);
                Set(l, "onCompleted", chDone);
                Set(l, "onStateChanged", chState);
                Set(l, "deliveryService", f.Service);
            });
            return f;
        }

        private static T AddWired<T>(GameObject go, Action<T> wire) where T : MonoBehaviour, new()
        {
            T c = go.AddComponent<T>();
            // Unity deserializes fields before Awake/OnEnable; emulate by re-running OnEnable after wiring.
            FakeEngine_Invoke(c, "OnDisable");
            wire(c);
            FakeEngine_Invoke(c, "OnEnable");
            return c;
        }

        private static void Place<T>(string name, Vector3 pos, Action<T> wire) where T : MonoBehaviour, new()
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            wire(go.AddComponent<T>());
        }

        private static AddressPointSO Address(string id, string display)
        {
            var a = ScriptableObject.CreateInstance<AddressPointSO>();
            Set(a, "addressId", id);
            Set(a, "displayName", display);
            return a;
        }

        private static DeliverySO Parcel(string label, AddressPointSO to, DeliveryPriority prio)
        {
            var d = ScriptableObject.CreateInstance<DeliverySO>();
            Set(d, "id", label);
            Set(d, "label", label);
            Set(d, "destination", to);
            Set(d, "priority", prio);
            return d;
        }

        private static void FakeEngine_Invoke(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(target, null);

        /// <summary>Inspector/serialization adapter: assigns a [SerializeField] private field.</summary>
        private static void Set(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                           ?? throw new MissingFieldException(target.GetType().Name, field);
            fi.SetValue(target, value);
        }
    }
}
