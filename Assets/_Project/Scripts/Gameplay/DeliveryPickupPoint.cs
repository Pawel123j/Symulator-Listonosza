using System.Collections.Generic;
using Listonosz.Data;
using UnityEngine;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// Punkt odbioru przesyłek (np. urząd/baza). W interakcji wydaje graczowi przesyłki z przypisanej
    /// bazy dnia (<see cref="DeliveryDatabaseSO"/>). Sama logika dodania do torby i podniesienia zdarzeń
    /// należy do <see cref="DeliveryService"/> — tu tylko dane i rejestracja jako obiekt interaktywny.
    /// </summary>
    public sealed class DeliveryPickupPoint : MonoBehaviour, IInteractable
    {
        [Tooltip("Baza przesyłek wydawanych w tym punkcie.")]
        [SerializeField] private DeliveryDatabaseSO database;

        [Min(0.1f)]
        [SerializeField] private float interactionRadius = 2.5f;

        /// <summary>Przesyłki do wydania w tym punkcie (z bazy dnia).</summary>
        public IReadOnlyList<DeliverySO> Deliveries =>
            database != null ? database.Deliveries : System.Array.Empty<DeliverySO>();

        public Vector3 InteractionWorldPosition => transform.position;
        public float InteractionRadius => interactionRadius;
        public string InteractionPrompt => "Odbierz przesyłki";

        private void OnEnable() => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Unregister(this);

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
#endif
    }
}
