using UnityEngine;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// Pomost wejścia dotykowego: <see cref="TouchControls"/> tu zapisuje stan (wektor ruchu, żądanie
    /// interakcji, dane wizualizacji joysticka), a <see cref="PlayerController"/> / <see cref="DeliveryService"/>
    /// i widżety UI tylko odczytują. Dzięki temu sterowanie dotykowe nie tworzy sztywnych referencji
    /// między komponentami — to cienka, współdzielona granica wejścia.
    /// </summary>
    public static class MobileInput
    {
        /// <summary>Wektor ruchu z joysticka dotykowego (znormalizowany do długości &lt;= 1).</summary>
        public static Vector2 Move;

        /// <summary>Czy joystick jest aktualnie aktywny (do wizualizacji).</summary>
        public static bool JoystickActive;

        /// <summary>Punkt zaczepienia joysticka w pikselach ekranu (origin dotyku).</summary>
        public static Vector2 OriginScreen;

        /// <summary>Aktualna pozycja palca w pikselach ekranu.</summary>
        public static Vector2 CurrentScreen;

        private static bool _interactQueued;

        /// <summary>Zgłasza żądanie interakcji (tap). Pobierane raz przez <see cref="ConsumeInteract"/>.</summary>
        public static void QueueInteract() => _interactQueued = true;

        /// <summary>Zwraca true raz, jeśli było żądanie interakcji, i je czyści.</summary>
        public static bool ConsumeInteract()
        {
            if (!_interactQueued)
            {
                return false;
            }

            _interactQueued = false;
            return true;
        }

        /// <summary>Czyści cały stan wejścia dotykowego.</summary>
        public static void Reset()
        {
            Move = Vector2.zero;
            JoystickActive = false;
            _interactQueued = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => Reset();
    }
}
