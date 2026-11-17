using System.Collections.Generic;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// Lekki rejestr aktywnych obiektów interaktywnych. Obiekty rejestrują się w OnEnable i wyrejestrowują
    /// w OnDisable, dzięki czemu <see cref="DeliveryService"/> nie musi co klatkę przeszukiwać sceny
    /// (FindObjectsOfType) ani trzymać sztywnych referencji do konkretnych celów.
    /// </summary>
    public static class InteractableRegistry
    {
        private static readonly List<IInteractable> Items = new List<IInteractable>();

        /// <summary>Aktualnie zarejestrowane obiekty interaktywne (tylko do odczytu).</summary>
        public static IReadOnlyList<IInteractable> All => Items;

        public static void Register(IInteractable interactable)
        {
            if (interactable != null && !Items.Contains(interactable))
            {
                Items.Add(interactable);
            }
        }

        public static void Unregister(IInteractable interactable)
        {
            Items.Remove(interactable);
        }
    }
}
