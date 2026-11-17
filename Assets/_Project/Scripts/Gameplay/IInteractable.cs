using UnityEngine;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// Obiekt świata, z którym gracz może wejść w interakcję w zasięgu (punkt odbioru, cel dostawy).
    /// <see cref="DeliveryService"/> wyszukuje najbliższy interaktywny obiekt i — po naciśnięciu przycisku —
    /// rozstrzyga akcję na podstawie jego konkretnego typu.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Pozycja świata, względem której liczony jest zasięg interakcji.</summary>
        Vector3 InteractionWorldPosition { get; }

        /// <summary>Promień, w którym interakcja jest możliwa (jednostki świata).</summary>
        float InteractionRadius { get; }

        /// <summary>Krótki opis akcji do UI (np. „Odbierz", „Dostarcz").</summary>
        string InteractionPrompt { get; }
    }
}
