using UnityEngine;

namespace Listonosz.Data
{
    /// <summary>
    /// Definicja pojedynczej przesyłki do dostarczenia: identyfikator, adresat, typ, waga i priorytet.
    /// Komórka docelowa pochodzi z przypisanego adresu (<see cref="AddressPointSO"/>), więc jedna
    /// zmiana adresu propaguje się do wszystkich przesyłek na ten adres.
    /// </summary>
    [CreateAssetMenu(fileName = "Delivery", menuName = "Listonosz/Delivery/Delivery", order = 1)]
    public sealed class DeliverySO : ScriptableObject
    {
        [Header("Identyfikacja")]
        [Tooltip("Stabilny identyfikator przesyłki (domyślnie nazwa assetu).")]
        [SerializeField] private string id;

        [Tooltip("Etykieta pokazywana w UI.")]
        [SerializeField] private string label = "Przesyłka";

        [Header("Adresat")]
        [Tooltip("Docelowy adres. Komórka celu wynika z tego adresu.")]
        [SerializeField] private AddressPointSO destination;

        [Header("Właściwości")]
        [SerializeField] private DeliveryType type = DeliveryType.Letter;

        [Tooltip("Waga w kilogramach.")]
        [Min(0f)]
        [SerializeField] private float weightKg = 0.1f;

        [SerializeField] private DeliveryPriority priority = DeliveryPriority.Normal;

        /// <summary>Stabilny identyfikator przesyłki.</summary>
        public string Id => id;

        /// <summary>Etykieta przesyłki do UI.</summary>
        public string Label => label;

        /// <summary>Docelowy adres przesyłki (może być null, jeśli nieprzypisany).</summary>
        public AddressPointSO Destination => destination;

        /// <summary>Typ przesyłki.</summary>
        public DeliveryType Type => type;

        /// <summary>Waga w kilogramach.</summary>
        public float WeightKg => weightKg;

        /// <summary>Priorytet dostawy.</summary>
        public DeliveryPriority Priority => priority;

        /// <summary>Komórka docelowa wynikająca z adresu (Vector2Int.zero, gdy brak adresu).</summary>
        public Vector2Int TargetCell => destination != null ? destination.Cell : Vector2Int.zero;

        /// <summary>Czy przesyłka ma przypisany adres docelowy.</summary>
        public bool HasDestination => destination != null;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                id = name;
            }
        }
    }
}
