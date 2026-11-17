using Listonosz.Data;
using Listonosz.Grid;
using UnityEngine;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// Punkt świata (skrzynka / dom) reprezentujący adres dostawy. Łączy obiekt sceny z danymi
    /// adresu (<see cref="AddressPointSO"/>) i udostępnia go jako cel interakcji. Walidację „czy gracz
    /// niesie przesyłkę na ten adres" i podniesienie zdarzeń wykonuje <see cref="DeliveryService"/>.
    /// </summary>
    public sealed class DeliveryTarget : MonoBehaviour, IInteractable
    {
        [Tooltip("Adres przypisany do tego punktu świata.")]
        [SerializeField] private AddressPointSO address;

        [Min(0.1f)]
        [SerializeField] private float interactionRadius = 2f;

        [Tooltip("Opcjonalnie: config gridu do wizualizacji/weryfikacji komórki adresu w edytorze.")]
        [SerializeField] private GridConfig gridConfig;

        /// <summary>Adres przypisany do punktu.</summary>
        public AddressPointSO Address => address;

        /// <summary>Komórka logiczna adresu (Vector2Int.zero, gdy brak adresu).</summary>
        public Vector2Int Cell => address != null ? address.Cell : Vector2Int.zero;

        /// <summary>Czy punkt ma przypisany adres.</summary>
        public bool HasAddress => address != null;

        public Vector3 InteractionWorldPosition => transform.position;
        public float InteractionRadius => interactionRadius;
        public string InteractionPrompt => "Dostarcz";

        private void OnEnable() => InteractableRegistry.Register(this);
        private void OnDisable() => InteractableRegistry.Unregister(this);

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (address == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, interactionRadius);

            if (gridConfig == null)
            {
                return;
            }

            var grid = new GridSystem(gridConfig);
            Vector3 cellCenter = grid.CellToWorldCenter(address.Cell);

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
            Gizmos.DrawCube(cellCenter, new Vector3(grid.CellSize, 0.05f, grid.CellSize));

            // Ostrzeżenie wizualne: obiekt nie leży w komórce swojego adresu.
            if (grid.WorldToCell(transform.position) != address.Cell)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, cellCenter);
            }
        }
#endif
    }
}
