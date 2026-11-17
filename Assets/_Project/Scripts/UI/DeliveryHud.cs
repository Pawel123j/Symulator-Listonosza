using System.Collections.Generic;
using System.Text;
using Listonosz.Data;
using Listonosz.Grid;
using UnityEngine;
using UnityEngine.UI;

namespace Listonosz.UI
{
    /// <summary>
    /// HUD dostaw: licznik dostarczonych / do dostarczenia oraz lista aktywnych przesyłek.
    /// Stan utrzymuje WYŁĄCZNIE na podstawie kanałów zdarzeń SO (podjęcie/dostarczenie/niepowodzenie) —
    /// nie zna DeliveryService ani logiki gry (patrz ADR-0001). Cały interfejs budowany jest w kodzie.
    /// Opcjonalny wskaźnik kierunku do najbliższego celu wymaga (z natury) danych przestrzennych:
    /// przypisz <see cref="player"/> i <see cref="gridConfig"/>, aby go włączyć.
    /// </summary>
    public sealed class DeliveryHud : MonoBehaviour
    {
        [Header("Kanały zdarzeń (ScriptableObject)")]
        [SerializeField] private DeliveryEventChannelSO onPickedUp;
        [SerializeField] private DeliveryEventChannelSO onCompleted;
        [SerializeField] private DeliveryEventChannelSO onFailed;

        [Header("Wskaźnik kierunku (opcjonalny)")]
        [Tooltip("Transform gracza — wymagany do wskaźnika najbliższego celu.")]
        [SerializeField] private Transform player;

        [Tooltip("Config gridu do zamiany komórki adresu na pozycję świata.")]
        [SerializeField] private GridConfig gridConfig;

        private readonly List<DeliverySO> _active = new List<DeliverySO>();
        private int _completedCount;
        private int _failedCount;

        private Text _counterText;
        private Text _listText;
        private Text _directionText;
        private GridSystem _grid;
        private readonly StringBuilder _sb = new StringBuilder(256);

        private void Awake()
        {
            BuildUi();

            if (gridConfig != null)
            {
                _grid = new GridSystem(gridConfig);
            }
        }

        private void OnEnable()
        {
            Subscribe(onPickedUp, HandlePickedUp);
            Subscribe(onCompleted, HandleCompleted);
            Subscribe(onFailed, HandleFailed);
            RefreshTexts();
        }

        private void OnDisable()
        {
            Unsubscribe(onPickedUp, HandlePickedUp);
            Unsubscribe(onCompleted, HandleCompleted);
            Unsubscribe(onFailed, HandleFailed);
        }

        private void Update()
        {
            UpdateDirection();
        }

        // --- Reakcje na kanały -------------------------------------------------

        private void HandlePickedUp(DeliverySO delivery)
        {
            if (delivery != null && !_active.Contains(delivery))
            {
                _active.Add(delivery);
                RefreshTexts();
            }
        }

        private void HandleCompleted(DeliverySO delivery)
        {
            _active.Remove(delivery);
            _completedCount++;
            RefreshTexts();
        }

        private void HandleFailed(DeliverySO delivery)
        {
            _active.Remove(delivery);
            _failedCount++;
            RefreshTexts();
        }

        // --- Render tekstu -----------------------------------------------------

        private void RefreshTexts()
        {
            if (_counterText != null)
            {
                _counterText.text = _failedCount > 0
                    ? $"Dostarczone: {_completedCount}    Do dostarczenia: {_active.Count}    Nieudane: {_failedCount}"
                    : $"Dostarczone: {_completedCount}    Do dostarczenia: {_active.Count}";
            }

            if (_listText == null)
            {
                return;
            }

            _sb.Clear();
            _sb.AppendLine("Aktywne dostawy:");
            if (_active.Count == 0)
            {
                _sb.AppendLine("  (brak — odbierz przesyłki w punkcie odbioru)");
            }
            else
            {
                for (int i = 0; i < _active.Count; i++)
                {
                    DeliverySO d = _active[i];
                    if (d == null)
                    {
                        continue;
                    }

                    string addr = d.Destination != null ? d.Destination.DisplayName : "— brak adresu —";
                    _sb.Append("  • ").Append(d.Label).Append("  →  ").Append(addr)
                       .Append("  [").Append(d.Type).Append(']').AppendLine();
                }
            }

            _listText.text = _sb.ToString();
        }

        private void UpdateDirection()
        {
            if (_directionText == null)
            {
                return;
            }

            if (player == null || _grid == null || _active.Count == 0)
            {
                _directionText.text = string.Empty;
                return;
            }

            DeliverySO nearest = null;
            Vector3 nearestPos = default;
            float bestSqr = float.MaxValue;
            Vector3 playerPos = player.position;

            for (int i = 0; i < _active.Count; i++)
            {
                DeliverySO d = _active[i];
                if (d == null || d.Destination == null)
                {
                    continue;
                }

                Vector3 pos = _grid.CellToWorldCenter(d.Destination.Cell);
                float sqr = (pos - playerPos).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    nearest = d;
                    nearestPos = pos;
                }
            }

            if (nearest == null)
            {
                _directionText.text = string.Empty;
                return;
            }

            Vector3 delta = nearestPos - playerPos;
            float distance = new Vector2(delta.x, delta.z).magnitude;
            _directionText.text =
                $"Najbliższy cel:\n{nearest.Destination.DisplayName}\n{Compass(delta)}  {distance:0} m";
        }

        /// <summary>Kierunek świata jako litera kompasu (N = +Z, E = +X).</summary>
        private static string Compass(Vector3 deltaXz)
        {
            float angle = Mathf.Atan2(deltaXz.x, deltaXz.z) * Mathf.Rad2Deg; // 0 = +Z
            if (angle < 0f)
            {
                angle += 360f;
            }

            string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            int index = Mathf.RoundToInt(angle / 45f) % 8;
            return dirs[index];
        }

        // --- Subskrypcja kanałów ----------------------------------------------

        private static void Subscribe(DeliveryEventChannelSO channel, System.Action<DeliverySO> handler)
        {
            if (channel != null)
            {
                channel.OnEventRaised += handler;
            }
        }

        private static void Unsubscribe(DeliveryEventChannelSO channel, System.Action<DeliverySO> handler)
        {
            if (channel != null)
            {
                channel.OnEventRaised -= handler;
            }
        }

        // --- Budowa UI w kodzie ------------------------------------------------

        private void BuildUi()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            var canvasGo = new GameObject("DeliveryHUD_Canvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            _counterText = CreateText(canvas.transform, font, "Counter",
                new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(900f, 40f), 28, TextAnchor.UpperLeft);

            _listText = CreateText(canvas.transform, font, "ActiveList",
                new Vector2(0f, 1f), new Vector2(20f, -70f), new Vector2(700f, 600f), 22, TextAnchor.UpperLeft);

            _directionText = CreateText(canvas.transform, font, "Direction",
                new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(420f, 120f), 24, TextAnchor.UpperRight);
        }

        private static Text CreateText(Transform parent, Font font, string name,
            Vector2 anchor, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = false;

            RectTransform rt = text.rectTransform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPosition;

            return text;
        }
    }
}
