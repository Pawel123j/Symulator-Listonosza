using Listonosz.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Listonosz.UI
{
    /// <summary>
    /// Podpowiedź interakcji: pokazuje akcję dostępną w zasięgu (np. „Dostarcz", „Odbierz przesyłki").
    /// To cienki widok stanu rozgrywki — subskrybuje zdarzenie <see cref="DeliveryService.InteractableChanged"/>
    /// i czyta <see cref="IInteractable.InteractionPrompt"/>. Uwaga: domenowe zdarzenia dostaw nadal lecą
    /// kanałami SO (patrz ADR-0001/0004); tu czytamy stan bezpośrednio, bo to lokalny wskaźnik czasu rzeczywistego.
    /// </summary>
    public sealed class InteractionPromptHud : MonoBehaviour
    {
        [Tooltip("Serwis dostaw (na obiekcie gracza) — źródło bieżącego obiektu w zasięgu.")]
        [SerializeField] private DeliveryService deliveryService;

        private Text _label;

        private void Awake() => BuildUi();

        private void OnEnable()
        {
            if (deliveryService != null)
            {
                deliveryService.InteractableChanged += OnInteractableChanged;
                OnInteractableChanged(deliveryService.CurrentInteractable);
            }
        }

        private void OnDisable()
        {
            if (deliveryService != null)
            {
                deliveryService.InteractableChanged -= OnInteractableChanged;
            }
        }

        private void OnInteractableChanged(IInteractable interactable)
        {
            if (_label == null)
            {
                return;
            }

            _label.text = interactable != null
                ? $"{interactable.InteractionPrompt}\n(dotknij / E)"
                : string.Empty;
        }

        private void BuildUi()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            var canvasGo = new GameObject("InteractionPrompt_Canvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var go = new GameObject("Prompt");
            go.transform.SetParent(canvas.transform, false);

            _label = go.AddComponent<Text>();
            _label.font = font;
            _label.fontSize = 30;
            _label.color = Color.white;
            _label.alignment = TextAnchor.LowerCenter;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.raycastTarget = false;
            _label.supportRichText = false;

            RectTransform rt = _label.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(700f, 120f);
            rt.anchoredPosition = new Vector2(0f, 180f);

            _label.text = string.Empty;
        }
    }
}
