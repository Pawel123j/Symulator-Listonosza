using Listonosz.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Listonosz.UI
{
    /// <summary>
    /// Wizualizacja pływającego joysticka dotykowego: rysuje okrąg-bazę w punkcie dotyku i gałkę podążającą
    /// za palcem. Czyta wyłącznie stan z <see cref="MobileInput"/> (nie obsługuje wejścia — tym zajmuje się
    /// <see cref="TouchControls"/>). Canvas budowany w kodzie w przestrzeni pikseli (bez skalera).
    /// </summary>
    public sealed class TouchJoystickVisual : MonoBehaviour
    {
        [Tooltip("Średnica bazy joysticka w pikselach.")]
        [SerializeField] private float baseDiameter = 260f;

        [Tooltip("Średnica gałki w pikselach.")]
        [SerializeField] private float knobDiameter = 120f;

        [Tooltip("Maksymalne wychylenie gałki od środka (piksele).")]
        [SerializeField] private float knobRange = 120f;

        private RawImage _base;
        private RawImage _knob;

        private void Awake() => BuildUi();

        private void Update()
        {
            if (_base == null || _knob == null)
            {
                return;
            }

            bool active = MobileInput.JoystickActive;
            if (_base.enabled != active)
            {
                _base.enabled = active;
                _knob.enabled = active;
            }

            if (!active)
            {
                return;
            }

            _base.rectTransform.anchoredPosition = MobileInput.OriginScreen;

            Vector2 delta = MobileInput.CurrentScreen - MobileInput.OriginScreen;
            if (delta.magnitude > knobRange)
            {
                delta = delta.normalized * knobRange;
            }

            _knob.rectTransform.anchoredPosition = MobileInput.OriginScreen + delta;
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("TouchJoystick_Canvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // pod ekranami dnia (100), nad światem

            Texture2D circle = CreateCircleTexture(128);

            _base = CreateCircle(canvas.transform, "Base", circle, baseDiameter, new Color(1f, 1f, 1f, 0.18f));
            _knob = CreateCircle(canvas.transform, "Knob", circle, knobDiameter, new Color(1f, 1f, 1f, 0.45f));

            _base.enabled = false;
            _knob.enabled = false;
        }

        private static RawImage CreateCircle(Transform parent, string name, Texture2D tex, float diameter, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<RawImage>();
            img.texture = tex;
            img.color = color;
            img.raycastTarget = false;

            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; // zaczepienie w lewym-dolnym rogu => anchoredPosition w pikselach
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(diameter, diameter);

            return img;
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = size * 0.5f;
            Vector2 center = new Vector2(r, r);

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    byte a = (byte)(d <= r ? 255 : 0);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }
}
