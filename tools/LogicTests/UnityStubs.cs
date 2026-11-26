// Minimal, deterministic stand-in for the parts of the Unity API used by the day-loop sources.
// It is a LOCAL ADAPTER for headless tests only: no rendering, no physics, no real input devices.
// Lifecycle semantics mirrored from Unity: AddComponent runs Awake + OnEnable immediately,
// Start runs once before the first Update (driven by FakeEngine.Tick), Destroy runs OnDisable +
// OnDestroy, and a destroyed Object compares equal to null (Unity's overloaded ==).
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    public class Object
    {
        internal bool Destroyed;
        public string name { get; set; } = string.Empty;

        public static bool operator ==(Object a, Object b)
        {
            bool aNull = ReferenceEquals(a, null) || a.Destroyed;
            bool bNull = ReferenceEquals(b, null) || b.Destroyed;
            if (aNull || bNull) return aNull && bNull;
            return ReferenceEquals(a, b);
        }

        public static bool operator !=(Object a, Object b) => !(a == b);
        public static implicit operator bool(Object o) => o != null;
        public override bool Equals(object o) => ReferenceEquals(this, o);
        public override int GetHashCode() => base.GetHashCode();

        public static void Destroy(Object o)
        {
            if (o is GameObject go) FakeEngine.DestroyGameObject(go);
            else if (o is Component c) FakeEngine.DestroyComponent(c);
            else if (!ReferenceEquals(o, null)) o.Destroyed = true;
        }

        public static void DestroyImmediate(Object o) => Destroy(o);

        public static void DontDestroyOnLoad(Object o) => FakeEngine.DontDestroyOnLoadCount++;
    }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new T { name = typeof(T).Name };
    }

    public class Component : Object
    {
        public GameObject gameObject { get; internal set; }
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; } = true;
    }

    public class MonoBehaviour : Behaviour
    {
        internal bool Started;
    }

    public sealed class Transform
    {
        public Vector3 position;
    }

    public sealed class GameObject : Object
    {
        internal readonly List<Component> Components = new List<Component>();
        public Transform transform { get; } = new Transform();

        public GameObject() : this("GameObject") { }

        public GameObject(string name)
        {
            this.name = name;
            FakeEngine.Objects.Add(this);
        }

        public T AddComponent<T>() where T : Component, new()
        {
            foreach (RequireComponent req in typeof(T).GetCustomAttributes<RequireComponent>())
            {
                if (GetComponentOfType(req.Type) == null)
                {
                    var dep = (Component)Activator.CreateInstance(req.Type);
                    AttachAndAwake(dep);
                }
            }

            var c = new T();
            AttachAndAwake(c);
            return c;
        }

        private Component GetComponentOfType(Type t)
        {
            foreach (Component c in Components)
            {
                if (!c.Destroyed && t.IsInstanceOfType(c)) return c;
            }

            return null;
        }

        private void AttachAndAwake(Component c)
        {
            c.gameObject = this;
            c.name = name;
            Components.Add(c);
            FakeEngine.Invoke(c, "Awake");
            if (!c.Destroyed && !Destroyed) FakeEngine.Invoke(c, "OnEnable");
        }

        public bool activeSelf { get; private set; } = true;

        /// <summary>Unity semantics: deactivation runs OnDisable, re-activation runs OnEnable.</summary>
        public void SetActive(bool value)
        {
            if (value == activeSelf || Destroyed) return;
            activeSelf = value;
            foreach (Component c in Components.ToArray())
            {
                if (!c.Destroyed) FakeEngine.Invoke(c, value ? "OnEnable" : "OnDisable");
            }
        }

        public T GetComponent<T>() where T : class
        {
            foreach (Component c in Components)
            {
                if (!c.Destroyed && c is T t) return t;
            }

            return null;
        }
    }

    /// <summary>Drives Start/Update and tracks live objects (the "player loop" of the harness).</summary>
    public static class FakeEngine
    {
        internal static readonly List<GameObject> Objects = new List<GameObject>();
        public static int DontDestroyOnLoadCount;
        public static readonly List<string> Logs = new List<string>();

        public static int LiveCount<T>() where T : Component
        {
            int n = 0;
            foreach (GameObject go in Objects)
            {
                if (!go.Destroyed && go.GetComponent<T>() != null) n++;
            }

            return n;
        }

        /// <summary>One frame: deliver pending Start calls, then Update on every live behaviour.</summary>
        public static void Tick(float deltaTime = 0.016f)
        {
            Time.deltaTime = deltaTime;
            var snapshot = new List<Component>();
            foreach (GameObject go in Objects.ToArray())
            {
                if (!go.Destroyed && go.activeSelf) snapshot.AddRange(go.Components);
            }

            foreach (Component c in snapshot)
            {
                if (c is MonoBehaviour mb && !mb.Destroyed && !mb.Started)
                {
                    mb.Started = true;
                    Invoke(mb, "Start");
                }
            }

            foreach (Component c in snapshot)
            {
                if (!c.Destroyed) Invoke(c, "Update");
            }
        }

        /// <summary>Tear down everything (end of a play session / test fixture).</summary>
        public static void Reset()
        {
            foreach (GameObject go in Objects.ToArray()) DestroyGameObject(go);
            Objects.Clear();
            Logs.Clear();
            AudioSource.Played.Clear();
            DontDestroyOnLoadCount = 0;
        }

        internal static void DestroyGameObject(GameObject go)
        {
            if (go.Destroyed) return;
            foreach (Component c in go.Components.ToArray()) DestroyComponent(c);
            go.Destroyed = true;
        }

        /// <summary>Live GameObjects with the given name (e.g. spawned "Confetti").</summary>
        public static List<GameObject> Find(string name)
        {
            var r = new List<GameObject>();
            foreach (GameObject go in Objects) if (!go.Destroyed && go.name == name) r.Add(go);
            return r;
        }

        internal static void DestroyComponent(Component c)
        {
            if (c.Destroyed) return;
            if (c.gameObject == null || c.gameObject.activeSelf) Invoke(c, "OnDisable");
            Invoke(c, "OnDestroy");
            c.Destroyed = true;
        }

        internal static void Invoke(object target, string method)
        {
            MethodInfo m = target.GetType().GetMethod(method,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            try
            {
                m?.Invoke(target, null);
            }
            catch (TargetInvocationException e) when (e.InnerException != null)
            {
                throw e.InnerException;
            }
        }
    }

    public static class Debug
    {
        public static void Log(object message) => FakeEngine.Logs.Add(message?.ToString());
        public static void Log(object message, Object context) => Log(message);
        public static void LogWarning(object message) => Log(message);
        public static void LogWarning(object message, Object context) => Log(message);
    }

    public static class Time
    {
        public static float deltaTime = 0.016f;
        public static float time;
    }

    /// <summary>In-memory PlayerPrefs: isolated synthetic storage, never touches the real registry/files.</summary>
    public static class PlayerPrefs
    {
        private static readonly Dictionary<string, int> Ints = new Dictionary<string, int>();
        public static int GetInt(string key, int defaultValue) => Ints.TryGetValue(key, out int v) ? v : defaultValue;
        public static void SetInt(string key, int value) => Ints[key] = value;
        public static void Save() { }
        public static void DeleteAll() => Ints.Clear();
    }

    public static class Mathf
    {
        public const float Rad2Deg = 57.29578f;
        public static int RoundToInt(float f) => (int)Math.Round(f, MidpointRounding.ToEven);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Clamp(int v, int min, int max) => Math.Clamp(v, min, max);
        public static float Clamp(float v, float min, float max) => Math.Clamp(v, min, max);
        public static float Abs(float f) => Math.Abs(f);
        public const float PI = (float)Math.PI;
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Exp(float f) => (float)Math.Exp(f);
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0f, 0f);
    }

    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public static Vector2Int zero => new Vector2Int(0, 0);
        public bool Equals(Vector2Int o) => x == o.x && y == o.y;
        public override bool Equals(object o) => o is Vector2Int v && Equals(v);
        public override int GetHashCode() => (x * 397) ^ y;
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.Equals(b);
        public static bool operator !=(Vector2Int a, Vector2Int b) => !a.Equals(b);
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public float sqrMagnitude => x * x + y * y + z * z;
        public override string ToString() => $"({x:0.##}, {y:0.##}, {z:0.##})";
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { LoadType = loadType; }
        public RuntimeInitializeLoadType LoadType { get; }
    }

    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string t) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string h) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute { public MinAttribute(float m) { } }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string fileName { get; set; }
        public string menuName { get; set; }
        public int order { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequireComponent : Attribute { public RequireComponent(Type t) { Type = t; } public Type Type { get; } }
}

namespace UnityEngine
{
    // ---- audio / particles / rendering: record-only adapters (nothing is played or drawn) ----

    public sealed class AudioClip : Object
    {
        public int Samples;
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) =>
            new AudioClip { name = name, Samples = lengthSamples };
        public bool SetData(float[] data, int offsetSamples) => true;
    }

    public sealed class AudioSource : Component
    {
        public static readonly List<Vector3> Played = new List<Vector3>();
        public static void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume) => Played.Add(position);
    }

    public sealed class Shader : Object
    {
        public static Shader Find(string name) => null; // no render pipeline in the harness
    }

    public sealed class Material : Object
    {
        public Material(Shader s) { }
    }

    public enum ParticleSystemStopBehavior { StopEmittingAndClear, StopEmitting }
    public enum ParticleSystemStopAction { None, Disable, Destroy, Callback }
    public enum ParticleSystemShapeType { Sphere, Hemisphere, Cone, Box }

    public sealed class ParticleSystemRenderer : Component
    {
        public Material sharedMaterial { get; set; }
    }

    [RequireComponent(typeof(ParticleSystemRenderer))]
    public sealed class ParticleSystem : Component
    {
        public int PlayCount;
        public void Stop(bool withChildren, ParticleSystemStopBehavior b) { }
        public void Play() => PlayCount++;
        public MainModule main => default;
        public EmissionModule emission => default;
        public ShapeModule shape => default;

        public struct MainModule
        {
            public bool playOnAwake { get; set; }
            public float duration { get; set; }
            public bool loop { get; set; }
            public float startLifetime { get; set; }
            public float startSpeed { get; set; }
            public float startSize { get; set; }
            public float gravityModifier { get; set; }
            public int maxParticles { get; set; }
            public ParticleSystemStopAction stopAction { get; set; }
            public MinMaxGradient startColor { get; set; }
        }

        public struct EmissionModule
        {
            public float rateOverTime { get; set; }
            public void SetBursts(Burst[] bursts) { }
        }

        public struct ShapeModule
        {
            public ParticleSystemShapeType shapeType { get; set; }
            public float angle { get; set; }
            public float radius { get; set; }
            public Vector3 rotation { get; set; }
        }

        public struct MinMaxGradient { public MinMaxGradient(Color a, Color b) { } }
        public struct Burst { public Burst(float time, short count) { } }
    }
}

namespace UnityEngine.InputSystem
{
    public enum InputActionType { Value, Button, PassThrough }

    /// <summary>Input action whose "performed" can be fired by the harness (Simulate) — no devices.</summary>
    public sealed class InputAction : IDisposable
    {
        public static readonly List<InputAction> Live = new List<InputAction>();

        public struct CallbackContext { }

        public string name { get; }
        public bool enabled { get; private set; }
        public readonly List<string> Bindings = new List<string>();
        public event Action<CallbackContext> performed;

        public InputAction(string name = null, InputActionType type = InputActionType.Value)
        {
            this.name = name;
            Live.Add(this);
        }

        public void AddBinding(string path) => Bindings.Add(path);
        public void Enable() => enabled = true;
        public void Disable() => enabled = false;
        public void Dispose() { enabled = false; Live.Remove(this); }

        /// <summary>Harness: emulate a press on any enabled action bound to <paramref name="path"/>.</summary>
        public static void Simulate(string path)
        {
            foreach (InputAction a in Live.ToArray())
            {
                if (a.enabled && a.Bindings.Contains(path)) a.performed?.Invoke(default);
            }
        }
    }
}
