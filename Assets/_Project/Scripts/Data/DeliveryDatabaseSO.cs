using System.Collections.Generic;
using UnityEngine;

namespace Listonosz.Data
{
    /// <summary>
    /// Zbiór przesyłek dostępnych w danym „dniu pracy". Pełni rolę edytowalnej listy zadań,
    /// z której systemy rozgrywki (Etap 4+) pobierają dostawy. Czyste dane — bez logiki gry.
    /// </summary>
    [CreateAssetMenu(fileName = "DeliveryDatabase", menuName = "Listonosz/Delivery/Delivery Database", order = 2)]
    public sealed class DeliveryDatabaseSO : ScriptableObject
    {
        [Tooltip("Etykieta dnia, np. „Dzień 1”.")]
        [SerializeField] private string dayLabel = "Dzień 1";

        [Tooltip("Przesyłki do dostarczenia tego dnia.")]
        [SerializeField] private List<DeliverySO> deliveries = new List<DeliverySO>();

        /// <summary>Etykieta dnia.</summary>
        public string DayLabel => dayLabel;

        /// <summary>Lista przesyłek (tylko do odczytu) zaplanowanych na dzień.</summary>
        public IReadOnlyList<DeliverySO> Deliveries => deliveries;

        /// <summary>Liczba przesyłek w bazie.</summary>
        public int Count => deliveries != null ? deliveries.Count : 0;
    }
}
