using Listonosz.Core;
using Listonosz.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// Pętla „dnia pracy" jako maszyna stanów (DayStart → Working → DayEnd). Prowadzi czas pracy i wynik
    /// (punkty wg priorytetu przesyłki + bonus za szybkość), kończy dzień po osiągnięciu celu lub gdy
    /// skończy się czas. Zmiany stanu rozgłasza kanałem SO. Niedostarczone na koniec dnia → nieudane.
    /// </summary>
    public sealed class GameLoopManager : MonoBehaviour
    {
        private const string BestScoreKey = "Listonosz.BestScore";

        [Header("Konfiguracja dnia")]
        [Tooltip("Baza przesyłek na dany dzień (źródło celu, gdy dailyGoal = 0).")]
        [SerializeField] private DeliveryDatabaseSO dayDatabase;

        [Tooltip("Liczba dostaw do zaliczenia (0 = wszystkie z bazy dnia).")]
        [Min(0)]
        [SerializeField] private int dailyGoal = 0;

        [Tooltip("Czas pracy w sekundach.")]
        [Min(10f)]
        [SerializeField] private float dayDurationSeconds = 300f;

        [Tooltip("Czy od razu przejść do pracy, pomijając ekran startu.")]
        [SerializeField] private bool autoStartWork = false;

        [Header("Kanały")]
        [SerializeField] private DeliveryEventChannelSO onCompleted;
        [SerializeField] private GameStateEventChannelSO onStateChanged;

        [Header("Powiązania (opcjonalne)")]
        [Tooltip("Serwis dostaw — na koniec dnia oznacza niesione przesyłki jako nieudane.")]
        [SerializeField] private DeliveryService deliveryService;

        private GameState _state = GameState.DayStart;
        private int _completed;
        private int _score;
        private int _combo;
        private int _bestScore;
        private float _timeRemaining;
        private InputAction _advanceAction;

        /// <summary>Bieżący stan dnia.</summary>
        public GameState State => _state;

        /// <summary>Cel dnia (liczba dostaw); przy dailyGoal = 0 to liczność bazy dnia.</summary>
        public int Goal => dailyGoal > 0 ? dailyGoal : (dayDatabase != null ? dayDatabase.Count : 0);

        /// <summary>Liczba zaliczonych dostaw w bieżącym dniu.</summary>
        public int CompletedCount => _completed;

        /// <summary>Aktualny wynik punktowy.</summary>
        public int Score => _score;

        public int BestScore => _bestScore;

        public int Combo => _combo;

        /// <summary>Pozostały czas pracy w sekundach.</summary>
        public float TimeRemaining => _timeRemaining;

        /// <summary>Całkowity czas dnia w sekundach.</summary>
        public float DayDuration => dayDurationSeconds;

        public int StarRating
        {
            get
            {
                if (Goal <= 0 || _completed <= 0)
                {
                    return 0;
                }

                float completion = _completed / (float)Goal;
                if (completion >= 1f && _timeRemaining >= dayDurationSeconds * 0.33f)
                {
                    return 3;
                }

                if (completion >= 0.8f)
                {
                    return 2;
                }

                return 1;
            }
        }

        private void Awake()
        {
            _bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);

            _advanceAction = new InputAction("Advance", InputActionType.Button);
            _advanceAction.AddBinding("<Keyboard>/enter");
            _advanceAction.AddBinding("<Keyboard>/numpadEnter");
            _advanceAction.AddBinding("<Gamepad>/start");
            _advanceAction.AddBinding("<Pointer>/press"); // tap/klik — ekrany dnia na mobile
            _advanceAction.performed += OnAdvance;
        }

        private void OnEnable()
        {
            if (onCompleted != null)
            {
                onCompleted.OnEventRaised += HandleCompleted;
            }

            _advanceAction?.Enable();
        }

        private void OnDisable()
        {
            if (onCompleted != null)
            {
                onCompleted.OnEventRaised -= HandleCompleted;
            }

            _advanceAction?.Disable();
        }

        private void OnDestroy()
        {
            if (_advanceAction != null)
            {
                _advanceAction.performed -= OnAdvance;
                _advanceAction.Dispose();
            }
        }

        private void Start()
        {
            StartDay();
        }

        private void Update()
        {
            if (_state != GameState.Working)
            {
                return;
            }

            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                EndDay();
            }
        }

        /// <summary>Rozpoczyna nowy dzień: zeruje licznik, wynik i czas, wchodzi w stan DayStart.</summary>
        public void StartDay()
        {
            _completed = 0;
            _score = 0;
            _combo = 0;
            _timeRemaining = dayDurationSeconds;
            SetState(GameState.DayStart);

            string day = dayDatabase != null ? dayDatabase.DayLabel : "?";
            Debug.Log($"[Listonosz] Start dnia '{day}' - cel: {Goal} dostaw, czas: {dayDurationSeconds:0}s.", this);

            if (autoStartWork)
            {
                BeginWork();
            }
        }

        /// <summary>Przechodzi z briefingu do pracy (uruchamia stoper).</summary>
        public void BeginWork()
        {
            if (_state != GameState.DayStart)
            {
                return;
            }

            _timeRemaining = dayDurationSeconds;
            SetState(GameState.Working);
        }

        /// <summary>Kończy dzień: niedostarczone oznacza jako nieudane i pokazuje podsumowanie.</summary>
        public void EndDay()
        {
            if (_state == GameState.DayEnd)
            {
                return;
            }

            if (deliveryService != null)
            {
                deliveryService.FailAllCarried();
            }

            if (_score > _bestScore)
            {
                _bestScore = _score;
                PlayerPrefs.SetInt(BestScoreKey, _bestScore);
                PlayerPrefs.Save();
            }

            SetState(GameState.DayEnd);
            Debug.Log($"[Listonosz] Koniec dnia. Zaliczone: {_completed}/{Goal}. Wynik: {_score}.", this);
        }

        private void HandleCompleted(DeliverySO delivery)
        {
            if (_state != GameState.Working)
            {
                return;
            }

            _completed++;
            _combo++;

            int baseScore = ScoreFor(delivery);
            int timeBonus = Mathf.RoundToInt(_timeRemaining * 0.25f);
            int comboBonus = Mathf.Min((_combo - 1) * 25, 150);
            _score += baseScore + timeBonus + comboBonus;

            if (Goal > 0 && _completed >= Goal)
            {
                EndDay();
            }
        }

        private static int ScoreFor(DeliverySO delivery)
        {
            if (delivery == null)
            {
                return 100;
            }

            switch (delivery.Priority)
            {
                case DeliveryPriority.High: return 150;
                case DeliveryPriority.Low: return 70;
                default: return 100;
            }
        }

        private void OnAdvance(InputAction.CallbackContext _)
        {
            switch (_state)
            {
                case GameState.DayStart:
                    BeginWork();
                    break;
                case GameState.DayEnd:
                    StartDay(); // restart pętli — nowy dzień
                    break;
            }
        }

        private void SetState(GameState next)
        {
            _state = next;
            onStateChanged?.Raise(next);
        }
    }
}
