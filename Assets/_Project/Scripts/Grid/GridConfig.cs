using UnityEngine;

namespace Listonosz.Grid
{
    /// <summary>
    /// Konfiguracja logicznej siatki: punkt zaczepienia (origin), rozmiar komórki i wymiary.
    /// Siatka leży na płaszczyźnie świata XZ (oś Y to wysokość). Dane są czysto logiczne —
    /// nie wpływają na ruch gracza; służą mapowaniu pozycji świata na komórki (adresy,
    /// cele dostaw, occupancy). Tworzona jako asset przez menu Create.
    /// </summary>
    [CreateAssetMenu(fileName = "GridConfig", menuName = "Listonosz/Grid/Grid Config", order = 0)]
    public sealed class GridConfig : ScriptableObject
    {
        [Header("Geometria siatki")]
        [Tooltip("Pozycja świata rogu komórki (0,0) — róg o najmniejszych X i Z. Y to wysokość siatki.")]
        [SerializeField] private Vector3 origin = Vector3.zero;

        [Tooltip("Długość boku komórki w jednostkach świata.")]
        [Min(0.01f)]
        [SerializeField] private float cellSize = 1f;

        [Tooltip("Liczba komórek wzdłuż osi X (kolumny) oraz Z (wiersze).")]
        [SerializeField] private Vector2Int dimensions = new Vector2Int(20, 20);

        [Header("Debug (gizmos)")]
        [Tooltip("Czy w ogóle rysować siatkę gizmosami (globalna flaga dla configu).")]
        [SerializeField] private bool drawGizmos = true;

        [SerializeField] private Color gizmoColor = new Color(0.25f, 0.9f, 1f, 0.5f);

        /// <summary>Pozycja świata rogu komórki (0,0).</summary>
        public Vector3 Origin => origin;

        /// <summary>Długość boku komórki w jednostkach świata (zawsze &gt;= 0.01).</summary>
        public float CellSize => Mathf.Max(0.01f, cellSize);

        /// <summary>Liczba komórek w osiach X i Z (zawsze &gt;= 1).</summary>
        public Vector2Int Dimensions => new Vector2Int(Mathf.Max(1, dimensions.x), Mathf.Max(1, dimensions.y));

        /// <summary>Czy rysować siatkę gizmosami.</summary>
        public bool DrawGizmos => drawGizmos;

        /// <summary>Kolor gizmosów siatki.</summary>
        public Color GizmoColor => gizmoColor;

        private void OnValidate()
        {
            if (cellSize < 0.01f)
            {
                cellSize = 0.01f;
            }

            if (dimensions.x < 1)
            {
                dimensions.x = 1;
            }

            if (dimensions.y < 1)
            {
                dimensions.y = 1;
            }
        }
    }
}
