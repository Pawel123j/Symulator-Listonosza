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

        /// <summary>
        /// Liczba przesyłek w bazie: unikalne, niepuste wpisy. Puste pozycje listy (null) i powtórzenia
        /// tej samej przesyłki nie są liczone — gracz może odebrać i dostarczyć każdą przesyłkę najwyżej
        /// raz dziennie, więc tylko takie wpisy są osiągalnym celem dnia (GameLoopManager.Goal przy dailyGoal = 0).
        /// </summary>
        public int Count
        {
            get
            {
                if (deliveries == null)
                {
                    return 0;
                }

                int count = 0;
                for (int i = 0; i < deliveries.Count; i++)
                {
                    DeliverySO d = deliveries[i];
                    if (d != null && deliveries.IndexOf(d) == i)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
