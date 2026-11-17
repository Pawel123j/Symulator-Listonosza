using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// Sterowanie dotykowe (mobile) czytane bezpośrednio z <see cref="Touchscreen"/> — bez EventSystem
    /// ani On-Screen Controls, więc jest odporne i proste. Lewa połowa ekranu = pływający joystick ruchu,
    /// prawa połowa = tap interakcji. Wynik trafia do <see cref="MobileInput"/>, skąd czytają go gracz,
    /// serwis dostaw i wizualizacja. Działa równolegle z klawiaturą/padem (do testów w edytorze).
    /// </summary>
    public sealed class TouchControls : MonoBehaviour
    {
        [Tooltip("Promień (w pikselach), przy którym wychylenie joysticka daje pełną prędkość.")]
        [Min(20f)]
        [SerializeField] private float joystickRadiusPixels = 140f;

        private int _moveTouchId = -1;
        private Vector2 _moveOrigin;

        private void OnDisable() => ResetMove();

        private void Update()
        {
            Touchscreen ts = Touchscreen.current;
            if (ts == null)
            {
                return; // brak ekranu dotykowego (np. desktop bez symulacji) — gra steruje się klawiaturą
            }

            bool moveTouchAlive = false;
            float halfWidth = Screen.width * 0.5f;

            foreach (TouchControl touch in ts.touches)
            {
                UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.None)
                {
                    continue;
                }

                int id = touch.touchId.ReadValue();
                Vector2 pos = touch.position.ReadValue();

                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    if (pos.x < halfWidth && _moveTouchId == -1)
                    {
                        _moveTouchId = id;
                        _moveOrigin = pos;
                        MobileInput.JoystickActive = true;
                        MobileInput.OriginScreen = pos;
                        MobileInput.CurrentScreen = pos;
                    }
                    else if (pos.x >= halfWidth)
                    {
                        MobileInput.QueueInteract();
                    }
                }

                if (id == _moveTouchId)
                {
                    bool active = phase == UnityEngine.InputSystem.TouchPhase.Began
                                  || phase == UnityEngine.InputSystem.TouchPhase.Moved
                                  || phase == UnityEngine.InputSystem.TouchPhase.Stationary;

                    if (active)
                    {
                        moveTouchAlive = true;
                        Vector2 delta = pos - _moveOrigin;
                        Vector2 v = delta / joystickRadiusPixels;
                        if (v.sqrMagnitude > 1f)
                        {
                            v.Normalize();
                        }

                        MobileInput.Move = v;
                        MobileInput.CurrentScreen = pos;
                    }
                    else
                    {
                        ResetMove();
                    }
                }
            }

            if (_moveTouchId != -1 && !moveTouchAlive)
            {
                ResetMove();
            }
        }

        private void ResetMove()
        {
            _moveTouchId = -1;
            MobileInput.Move = Vector2.zero;
            MobileInput.JoystickActive = false;
        }
    }
}
