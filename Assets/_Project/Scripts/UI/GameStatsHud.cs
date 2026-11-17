using Listonosz.Core;
using Listonosz.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Listonosz.UI
{
    /// <summary>
    /// Runtime HUD for score, best score, time remaining, and delivery combo.
    /// </summary>
    public sealed class GameStatsHud : MonoBehaviour
    {
        [SerializeField] private GameLoopManager loop;

        private Text _text;

        private void Awake() => BuildUi();

        private void Update()
        {
            if (_text == null || loop == null)
            {
                return;
            }

            if (loop.State != GameState.Working)
            {
                if (_text.enabled)
                {
                    _text.enabled = false;
                }

                return;
            }

            if (!_text.enabled)
            {
                _text.enabled = true;
            }

            int seconds = Mathf.CeilToInt(loop.TimeRemaining);
            int minutes = seconds / 60;
            int rest = seconds % 60;
            string combo = loop.Combo > 1 ? $"\nCombo x{loop.Combo}" : string.Empty;

            _text.color = seconds <= 15 ? new Color(1f, 0.5f, 0.45f) : Color.white;
            _text.text = $"Wynik: {loop.Score}\nRekord: {loop.BestScore}\nCzas: {minutes}:{rest:00}{combo}";
        }

        private void BuildUi()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            var canvasGo = new GameObject("Stats_Canvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var go = new GameObject("Stats");
            go.transform.SetParent(canvas.transform, false);

            _text = go.AddComponent<Text>();
            _text.font = font;
            _text.fontSize = 30;
            _text.color = Color.white;
            _text.alignment = TextAnchor.UpperRight;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;
            _text.supportRichText = false;

            RectTransform rt = _text.rectTransform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(460f, 140f);
            rt.anchoredPosition = new Vector2(-24f, -20f);

            _text.enabled = false;
        }
    }
}
