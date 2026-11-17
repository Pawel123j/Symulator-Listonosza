using Listonosz.Core;
using Listonosz.Data;
using Listonosz.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Listonosz.UI
{
    /// <summary>
    /// Full-screen day start and day summary overlay. It listens to ScriptableObject channels
    /// and displays progress, score, stars, and best score.
    /// </summary>
    public sealed class DayScreenHud : MonoBehaviour
    {
        [Header("Kanaly zdarzen")]
        [SerializeField] private GameStateEventChannelSO onStateChanged;
        [SerializeField] private DeliveryEventChannelSO onCompleted;
        [SerializeField] private DeliveryEventChannelSO onFailed;

        [Header("Wyswietlanie")]
        [Tooltip("Cel dnia do pokazania na ekranach (0 = nie pokazuj liczby celu).")]
        [Min(0)]
        [SerializeField] private int dailyGoalDisplay = 0;

        [Tooltip("Opcjonalnie: petla dnia - pokazuje wynik, gwiazdki i rekord.")]
        [SerializeField] private GameLoopManager loop;

        private GameState _state = GameState.Working;
        private int _completed;
        private int _failed;

        private GameObject _overlay;
        private Text _text;

        private void Awake() => BuildUi();

        private void OnEnable()
        {
            if (onStateChanged != null)
            {
                onStateChanged.OnEventRaised += HandleState;
            }

            if (onCompleted != null)
            {
                onCompleted.OnEventRaised += HandleCompleted;
            }

            if (onFailed != null)
            {
                onFailed.OnEventRaised += HandleFailed;
            }

            UpdateOverlay();
        }

        private void OnDisable()
        {
            if (onStateChanged != null)
            {
                onStateChanged.OnEventRaised -= HandleState;
            }

            if (onCompleted != null)
            {
                onCompleted.OnEventRaised -= HandleCompleted;
            }

            if (onFailed != null)
            {
                onFailed.OnEventRaised -= HandleFailed;
            }
        }

        private void HandleState(GameState state)
        {
            if (state == GameState.DayStart)
            {
                _completed = 0;
                _failed = 0;
            }

            _state = state;
            UpdateOverlay();
        }

        private void HandleCompleted(DeliverySO _)
        {
            _completed++;
            UpdateOverlay();
        }

        private void HandleFailed(DeliverySO _)
        {
            _failed++;
            UpdateOverlay();
        }

        private void UpdateOverlay()
        {
            if (_overlay == null || _text == null)
            {
                return;
            }

            switch (_state)
            {
                case GameState.DayStart:
                    _overlay.SetActive(true);
                    _text.text = "DZIEN PRACY\n\n"
                                 + (dailyGoalDisplay > 0 ? $"Dostarcz {dailyGoalDisplay} przesylek.\n\n" : string.Empty)
                                 + "Dotknij ekran / [Enter], aby rozpoczac.";
                    break;

                case GameState.Working:
                    _overlay.SetActive(false);
                    break;

                case GameState.DayEnd:
                    _overlay.SetActive(true);
                    string delivered = dailyGoalDisplay > 0
                        ? $"Dostarczone: {_completed} / {dailyGoalDisplay}"
                        : $"Dostarczone: {_completed}";
                    string scoreLine = loop != null
                        ? $"\nWYNIK: {loop.Score}\nGWIAZDKI: {Stars(loop.StarRating)}\nREKORD: {loop.BestScore}"
                        : string.Empty;
                    _text.text = $"KONIEC DNIA\n\n{delivered}\nNieudane: {_failed}{scoreLine}\n\n"
                                 + "Dotknij ekran / [Enter], aby zaczac nowy dzien.";
                    break;
            }
        }

        private static string Stars(int count)
        {
            switch (Mathf.Clamp(count, 0, 3))
            {
                case 3: return "***";
                case 2: return "**";
                case 1: return "*";
                default: return "-";
            }
        }

        private void BuildUi()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            var canvasGo = new GameObject("DayScreen_Canvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _overlay = new GameObject("Overlay");
            _overlay.transform.SetParent(canvas.transform, false);

            var bg = _overlay.AddComponent<RawImage>();
            bg.color = new Color(0.02f, 0.05f, 0.08f, 0.72f);
            bg.raycastTarget = false;
            StretchFull(bg.rectTransform);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_overlay.transform, false);

            _text = textGo.AddComponent<Text>();
            _text.font = font;
            _text.fontSize = 40;
            _text.color = Color.white;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;
            _text.supportRichText = false;
            StretchFull(_text.rectTransform);

            _overlay.SetActive(false);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
