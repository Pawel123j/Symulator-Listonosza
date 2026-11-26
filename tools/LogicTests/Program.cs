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
        Run("D1 Similar labels: parcel for 'ul. Kwiatowa 1' is not accepted at 'ul. Kwiatowa 12'", D1_SimilarLabels);
        Run("D2 Identical label and id on two address assets: binding is by asset, not by text", D2_IdenticalLabels);
        Run("D3 Carrying parcels for both similar addresses: each target takes only its own", D3_BothCarried);
        Run("E1 Database: every entry bound to an existing address (typical)", E1_DatabaseTypical);
        Run("E2 Database: empty slot in the list does not count as a parcel", E2_DatabaseNullSlot);
        Run("E3 Database: the same parcel listed twice counts once", E3_DatabaseDuplicate);
        Run("E4 Database: parcel without an address is listed but can never be delivered", E4_UnboundParcel);
        Run("R1 Return to pickup with everything still carried: nothing new, bag unchanged", R1_ReturnNothingDelivered);
        Run("R2 Return to pickup after one delivery: delivered parcel is not issued again", R2_ReturnAfterDelivery);
        Run("R3 Bag limit 1: return after delivering issues the next parcel", R3_LimitedBagRefill);
        Run("R4 Bag limit 1: return while full issues nothing", R4_LimitedBagFull);
        Run("F1 Feedback: correct point -> one confetti + one sound over the address cell", F1_CorrectPoint);
        Run("F2 Feedback: wrong point -> no effects, no events, only an info log", F2_WrongPoint);
        Run("F3 Feedback: last delivery (ends the day) still celebrated; nothing after DayEnd", F3_LastDeliveryAndAfter);

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

    // ------------------------------------------------------------------ D: similar address labels

    private static void D1_SimilarLabels()
    {
        EnterPlayMode();
        Fixture f = Fixture.Build(day: x => new List<DeliverySO> { x.P1 }); // only P1 -> A "ul. Kwiatowa 1"
        FakeEngine.Tick(0f);
        Press();
        Interact(f, f.PickupPos);
        Interact(f, f.TargetBPos); // B = "ul. Kwiatowa 12" (A's label is its prefix)
        Eq("at B: nothing delivered", 0, f.Completed.Count);
        Eq("at B: P1 still in the bag", "P1", Labels(f.Inventory.Carried));
        Interact(f, f.TargetAPos);
        Eq("at A: P1 delivered", "P1", Labels(f.Completed));
        Eq("day complete (goal 1)", GameState.DayEnd, f.Loop.State);
        ExitPlayMode();
    }

    private static void D2_IdenticalLabels()
    {
        EnterPlayMode();
        Fixture f = Fixture.Build(
            day: x => new List<DeliverySO> { x.P1 },
            addresses: x =>
            {
                Fixture.Set(x.AddrB, "displayName", "ul. Kwiatowa 1");
                Fixture.Set(x.AddrB, "addressId", "A");
            });
        FakeEngine.Tick(0f);
        Eq("labels really identical", f.AddrA.DisplayName + "|" + f.AddrA.AddressId, f.AddrB.DisplayName + "|" + f.AddrB.AddressId);
        Press();
        Interact(f, f.PickupPos);
        Interact(f, f.TargetBPos);
        Eq("at B (same text, other asset): nothing delivered", 0, f.Completed.Count);
        Interact(f, f.TargetAPos);
        Eq("at A: P1 delivered", "P1", Labels(f.Completed));
        ExitPlayMode();
    }

    private static void D3_BothCarried()
    {
        EnterPlayMode();
        Fixture f = Fixture.Build();
        FakeEngine.Tick(0f);
        Press();
        Interact(f, f.PickupPos);
        Interact(f, f.TargetBPos);
        Eq("at B: P2 delivered, P1 kept", "P2|P1", Labels(f.Completed) + "|" + Labels(f.Inventory.Carried));
        Interact(f, f.TargetAPos);
        Eq("at A: P1 delivered", "P2,P1", Labels(f.Completed));
        ExitPlayMode();
    }

    // ------------------------------------------------------------------ E: database <-> addresses

    private static void E1_DatabaseTypical()
    {
        EnterPlayMode();
        Fixture f = Fixture.Build();
        FakeEngine.Tick(0f);
        Eq("Goal = parcels in the day", 2, f.Loop.Goal);
        PlayFullDay(f);
        Eq("day completed by goal", GameState.DayEnd, f.Loop.State);
        Eq("stars", 3, f.Loop.StarRating);
        ExitPlayMode();
    }

    private static void E2_DatabaseNullSlot()
    {
        EnterPlayMode();
        Fixture f = Fixture.Build(day: x => new List<DeliverySO> { x.P1, null, x.P2 });
        FakeEngine.Tick(0f);
        Eq("Goal counts real parcels only", 2, f.Loop.Goal);
        PlayFullDay(f);
        Eq("both parcels delivered", "P1,P2", Labels(f.Completed));
        Eq("day completed by goal", GameState.DayEnd, f.Loop.State);
        Eq("stars", 3, f.Loop.StarRating);
        ExitPlayMode();
    }

    private static void E3_DatabaseDuplicate()
    {
        EnterPlayMode();
        Fixture f = Fixture.Build(day: x => new List<DeliverySO> { x.P1, x.P2, x.P2 });
        FakeEngine.Tick(0f);
        Eq("Goal counts P2 once", 2, f.Loop.Goal);
        PlayFullDay(f);
        Eq("pickup gave P2 once", 2, f.Picked.Count);
        Eq("day completed by goal", GameState.DayEnd, f.Loop.State);
        ExitPlayMode();
    }

    private static void E4_UnboundParcel()
    {
        EnterPlayMode();
        Fixture f = Fixture.Build(day: x => new List<DeliverySO> { x.P1, Fixture.Parcel("P3", null, DeliveryPriority.Normal) });
        FakeEngine.Tick(0f);
        Eq("unbound parcel is still a listed parcel (documented: destination may be null)", 2, f.Loop.Goal);
        Press();
        Interact(f, f.PickupPos);
        Interact(f, f.TargetAPos);
        Interact(f, f.TargetBPos);
        Eq("only P1 delivered", "P1", Labels(f.Completed));
        Eq("P3 stays in the bag", "P3", Labels(f.Inventory.Carried));
        FakeEngine.Tick(61f);
        Eq("P3 failed at day end", "P3", Labels(f.Failed));
        ExitPlayMode();
    }

    // ------------------------------------------------------------------ R: return to the pickup point

    private static Fixture StartWorking(int maxItems)
    {
        EnterPlayMode();
        Fixture f = Fixture.Build();
        Fixture.Set(f.Inventory, "maxItems", maxItems);
        FakeEngine.Tick(0f);
        Press(); // Working
        return f;
    }

    private static void R1_ReturnNothingDelivered()
    {
        Fixture f = StartWorking(0);
        Interact(f, f.PickupPos);
        Eq("first visit: bag", "P1,P2", Labels(f.Inventory.Carried));
        Interact(f, new Vector3(100f, 0f, 0f)); // walk away (nothing in range)
        Interact(f, f.PickupPos);
        Eq("return: bag unchanged", "P1,P2", Labels(f.Inventory.Carried));
        Eq("return: no new pickup events", 2, f.Picked.Count);
        ExitPlayMode();
    }

    private static void R2_ReturnAfterDelivery()
    {
        Fixture f = StartWorking(0);
        Interact(f, f.PickupPos);
        Interact(f, f.TargetAPos); // P1 delivered
        Interact(f, f.PickupPos);
        Eq("return: bag holds only the undelivered parcel", "P2", Labels(f.Inventory.Carried));
        Eq("return: no new pickup events", 2, f.Picked.Count);
        Interact(f, f.TargetAPos);
        Eq("no second delivery of P1", "P1", Labels(f.Completed));
        ExitPlayMode();
    }

    private static void R3_LimitedBagRefill()
    {
        Fixture f = StartWorking(1);
        Interact(f, f.PickupPos);
        Eq("first visit (limit 1): bag", "P1", Labels(f.Inventory.Carried));
        Interact(f, f.TargetAPos);
        Interact(f, f.PickupPos);
        Eq("return: next parcel issued", "P2", Labels(f.Inventory.Carried));
        Eq("pickup events in total", "P1,P2", Labels(f.Picked));
        Interact(f, f.TargetBPos);
        Eq("day completed", GameState.DayEnd, f.Loop.State);
        ExitPlayMode();
    }

    private static void R4_LimitedBagFull()
    {
        Fixture f = StartWorking(1);
        Interact(f, f.PickupPos);
        Interact(f, f.PickupPos); // return without delivering
        Eq("bag still P1 only", "P1", Labels(f.Inventory.Carried));
        Eq("pickup events", "P1", Labels(f.Picked));
        ExitPlayMode();
    }

    // ------------------------------------------------------------------ F: feedback after delivery

    private static Fixture StartWorkingLogged()
    {
        Fixture f = StartWorking(0);
        Fixture.Set(f.Service, "logActions", true);
        Interact(f, f.PickupPos);
        FakeEngine.Logs.Clear();
        return f;
    }

    private static string Effects() =>
        $"confetti={FakeEngine.Find("Confetti").Count} sounds={AudioSource.Played.Count}";

    private static void F1_CorrectPoint()
    {
        Fixture f = StartWorkingLogged();
        Interact(f, f.TargetAPos);
        Eq("effects", "confetti=1 sounds=1", Effects());
        Eq("confetti over cell (10,0) center + 0.7 m", "(10.5, 0.7, 0.5)", FakeEngine.Find("Confetti")[0].transform.position.ToString());
        Eq("sound at the same point", "(10.5, 0.7, 0.5)", AudioSource.Played[0].ToString());
        Eq("completed event", "P1", Labels(f.Completed));
        Eq("info log", "[Listonosz] Dostarczono 'P1' pod ul. Kwiatowa 1. (zaliczone: 1)", string.Join(" | ", FakeEngine.Logs));
        ExitPlayMode();
    }

    private static void F2_WrongPoint()
    {
        Fixture f = StartWorkingLogged();
        Interact(f, f.TargetAPos); // deliver P1 so only P2 (for B) is carried...
        FakeEngine.Logs.Clear();
        Interact(f, f.TargetAPos); // ...and come back to A: wrong point for P2
        Eq("effects (only the earlier P1 delivery)", "confetti=1 sounds=1", Effects());
        Eq("no completed/failed event", "P1|", Labels(f.Completed) + "|" + Labels(f.Failed));
        Eq("bag unchanged", "P2", Labels(f.Inventory.Carried));
        Eq("score unchanged (P1 only)", 115, f.Loop.Score);
        Eq("info log", "[Listonosz] Nie masz przesyłki na adres ul. Kwiatowa 1.", string.Join(" | ", FakeEngine.Logs));
        ExitPlayMode();
    }

    private static void F3_LastDeliveryAndAfter()
    {
        Fixture f = StartWorkingLogged();
        Interact(f, f.TargetAPos);
        Interact(f, f.TargetBPos);
        Eq("day ended by the last delivery", GameState.DayEnd, f.Loop.State);
        Eq("effects for both deliveries", "confetti=2 sounds=2", Effects());
        Eq("second confetti over cell (20,0)", "(20.5, 0.7, 0.5)", FakeEngine.Find("Confetti")[1].transform.position.ToString());
        FakeEngine.Logs.Clear();
        Interact(f, f.TargetBPos); // after DayEnd interactions are off
        Eq("after DayEnd: no new effects", "confetti=2 sounds=2", Effects());
        Eq("after DayEnd: no log", 0, FakeEngine.Logs.Count);
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

    /// <summary>Event channel assets. Shared between scene loads when passed to Build (assets outlive scenes).</summary>
    private sealed class Channels
    {
        public readonly DeliveryEventChannelSO Picked = ScriptableObject.CreateInstance<DeliveryEventChannelSO>();
        public readonly DeliveryEventChannelSO Done = ScriptableObject.CreateInstance<DeliveryEventChannelSO>();
        public readonly DeliveryEventChannelSO Fail = ScriptableObject.CreateInstance<DeliveryEventChannelSO>();
        public readonly GameStateEventChannelSO State = ScriptableObject.CreateInstance<GameStateEventChannelSO>();

        /// <summary>Observation only: game components (not test listeners) currently subscribed to the channel.</summary>
        public static int ComponentListeners(object channel)
        {
            FieldInfo fi = null;
            for (Type t = channel.GetType(); t != null && fi == null; t = t.BaseType)
                fi = t.GetField("OnEventRaised", BindingFlags.Instance | BindingFlags.NonPublic);
            var d = fi?.GetValue(channel) as Delegate;
            int n = 0;
            if (d != null) foreach (Delegate h in d.GetInvocationList()) if (h.Target is Component) n++;
            return n;
        }

        public string Census() =>
            $"picked={ComponentListeners(Picked)} completed={ComponentListeners(Done)} failed={ComponentListeners(Fail)} state={ComponentListeners(State)}";
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

        public DeliverySO P1, P2;
        public AddressPointSO AddrA, AddrB;
        public readonly List<DeliverySO> Completed = new List<DeliverySO>();
        public DeliveryFeedback Feedback;
        public Channels Ch;

        /// <summary><paramref name="day"/> (optional) builds the day list from (fixture with P1/P2/AddrA/AddrB set).</summary>
        public static Fixture Build(bool autoStartWork = false, Func<Fixture, List<DeliverySO>> day = null,
            Action<Fixture> addresses = null, Channels channels = null)
        {
            var f = new Fixture();
            f.Ch = channels ?? new Channels();
            var chPicked = f.Ch.Picked;
            var chDone = f.Ch.Done;
            var chFail = f.Ch.Fail;
            var chState = f.Ch.State;
            chPicked.OnEventRaised += d => f.Picked.Add(d);
            chFail.OnEventRaised += d => f.Failed.Add(d);
            chState.OnEventRaised += s => f.States.Add(s.ToString());

            chDone.OnEventRaised += d => f.Completed.Add(d);
            var addrA = f.AddrA = Address("A", "ul. Kwiatowa 1");
            var addrB = f.AddrB = Address("B", "ul. Kwiatowa 12");
            Set(addrA, "cell", new Vector2Int(10, 0)); // grid: origin 0, cell 1 m -> target A sits in cell (10,0)
            Set(addrB, "cell", new Vector2Int(20, 0));
            addresses?.Invoke(f);
            var p1 = f.P1 = Parcel("P1", addrA, DeliveryPriority.Normal);
            var p2 = f.P2 = Parcel("P2", addrB, DeliveryPriority.High);
            var db = ScriptableObject.CreateInstance<DeliveryDatabaseSO>();
            Set(db, "dayLabel", "Dzień testowy");
            Set(db, "deliveries", day != null ? day(f) : new List<DeliverySO> { p1, p2 });

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

            var grid = ScriptableObject.CreateInstance<Listonosz.Grid.GridConfig>();
            Set(grid, "cellSize", 1f);
            var systemsGo = new GameObject("Systemy");
            f.Feedback = AddWired<DeliveryFeedback>(systemsGo, c =>
            {
                Set(c, "onCompleted", chDone);
                Set(c, "gridConfig", grid);
            });
            FakeEngine_Invoke(f.Feedback, "Awake"); // grid is read in Awake
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

        internal static AddressPointSO Address(string id, string display)
        {
            var a = ScriptableObject.CreateInstance<AddressPointSO>();
            Set(a, "addressId", id);
            Set(a, "displayName", display);
            return a;
        }

        internal static DeliverySO Parcel(string label, AddressPointSO to, DeliveryPriority prio)
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
        internal static void Set(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                           ?? throw new MissingFieldException(target.GetType().Name, field);
            fi.SetValue(target, value);
        }
    }
}
