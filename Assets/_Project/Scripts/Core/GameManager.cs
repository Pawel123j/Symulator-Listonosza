using UnityEngine;

namespace Listonosz.Core
{
    /// <summary>
    /// Centralny punkt wejścia gry (composition root / serwis-szkielet).
    /// Tworzy się automatycznie przy starcie odtwarzania, przeżywa zmiany scen i będzie
    /// hostem dla kolejnych serwisów (dostawy, pętla dnia). Na Etapie 0 pełni wyłącznie
    /// rolę szkieletu potwierdzającego poprawny bootstrap projektu.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        /// <summary>Jedyna instancja menedżera w czasie działania gry (singleton-lite).</summary>
        public static GameManager Instance { get; private set; }

        /// <summary>
        /// Automatyczny bootstrap: instancja powstaje przed załadowaniem sceny, więc nie
        /// wymaga ręcznego dodawania obiektu na scenie. Dzięki temu gra startuje poprawnie
        /// z dowolnej sceny (w tym z Bootstrap.unity).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
            {
                return;
            }

            var go = new GameObject(nameof(GameManager));
            go.AddComponent<GameManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("[Listonosz] GameManager zainicjalizowany (Etap 0 — szkielet).");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
