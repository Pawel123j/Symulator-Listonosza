using System.Collections.Generic;
using Listonosz.Data;
using UnityEngine;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// „Torba" listonosza — lista aktualnie niesionych przesyłek. Czysta pamięć stanu, bez logiki gry;
    /// zdarzenia (podjęcie/dostarczenie) podnosi <see cref="DeliveryService"/>. Opcjonalny limit sztuk.
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [Tooltip("Maksymalna liczba niesionych przesyłek (0 = bez limitu).")]
        [Min(0)]
        [SerializeField] private int maxItems = 0;

        private readonly List<DeliverySO> _carried = new List<DeliverySO>();

        /// <summary>Aktualnie niesione przesyłki (tylko do odczytu).</summary>
        public IReadOnlyList<DeliverySO> Carried => _carried;

        /// <summary>Liczba niesionych przesyłek.</summary>
        public int Count => _carried.Count;

        /// <summary>Czy torba jest pełna (przy ustawionym limicie).</summary>
        public bool IsFull => maxItems > 0 && _carried.Count >= maxItems;

        /// <summary>Czy dana przesyłka jest niesiona.</summary>
        public bool Contains(DeliverySO delivery) => delivery != null && _carried.Contains(delivery);

        /// <summary>Dodaje przesyłkę do torby. Zwraca false, gdy null, duplikat lub torba pełna.</summary>
        public bool TryAdd(DeliverySO delivery)
        {
            if (delivery == null || IsFull || _carried.Contains(delivery))
            {
                return false;
            }

            _carried.Add(delivery);
            return true;
        }

        /// <summary>Usuwa przesyłkę z torby. Zwraca true, jeśli była niesiona.</summary>
        public bool Remove(DeliverySO delivery) => _carried.Remove(delivery);

        /// <summary>Zwraca pierwszą niesioną przesyłkę zaadresowaną pod podany adres lub null.</summary>
        public DeliverySO FindForAddress(AddressPointSO address)
        {
            if (address == null)
            {
                return null;
            }

            for (int i = 0; i < _carried.Count; i++)
            {
                if (_carried[i] != null && _carried[i].Destination == address)
                {
                    return _carried[i];
                }
            }

            return null;
        }
    }
}
