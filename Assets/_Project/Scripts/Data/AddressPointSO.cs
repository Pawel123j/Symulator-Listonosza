using UnityEngine;

namespace Listonosz.Data
{
    /// <summary>
    /// Definicja adresu w mieście: identyfikator, nazwa wyświetlana i komórka logiczna gridu.
    /// Stanowi „prawdę" o lokalizacji adresu — zarówno przesyłka (<see cref="DeliverySO"/>), jak i
    /// punkt świata (komponent DeliveryTarget) odwołują się do tego samego adresu, co upraszcza
    /// walidację dostawy w Etapie 4. Konwencja komórki: cell.x ↔ świat X, cell.y ↔ świat Z.
    /// </summary>
    [CreateAssetMenu(fileName = "Address", menuName = "Listonosz/Delivery/Address Point", order = 0)]
    public sealed class AddressPointSO : ScriptableObject
    {
        [Tooltip("Stabilny identyfikator adresu (domyślnie nazwa assetu).")]
        [SerializeField] private string addressId;

        [Tooltip("Nazwa pokazywana w UI, np. „ul. Kwiatowa 12”.")]
        [SerializeField] private string displayName = "ul. Przykładowa 1";

        [Tooltip("Komórka logiczna gridu, w której znajduje się adres.")]
        [SerializeField] private Vector2Int cell;

        /// <summary>Stabilny identyfikator adresu.</summary>
        public string AddressId => addressId;

        /// <summary>Nazwa wyświetlana adresu.</summary>
        public string DisplayName => displayName;

        /// <summary>Komórka logiczna gridu przypisana do adresu.</summary>
        public Vector2Int Cell => cell;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(addressId))
            {
                addressId = name;
            }
        }
    }
}
