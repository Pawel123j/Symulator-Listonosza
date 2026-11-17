using UnityEngine;

namespace Listonosz.Grid
{
    /// <summary>
    /// Komponent debugowy rysujący logiczną siatkę w widoku Scene (gizmos). Nie wpływa na grę —
    /// służy wyłącznie weryfikacji konfiguracji gridu. Rysowanie można wyłączyć tu lub w GridConfig.
    /// Cała logika gizmosów jest kompilowana tylko w edytorze (#if UNITY_EDITOR).
    /// </summary>
    public sealed class GridDebugView : MonoBehaviour
    {
        [Tooltip("Konfiguracja siatki do zwizualizowania.")]
        [SerializeField] private GridConfig config;

        [Tooltip("Lokalny wyłącznik rysowania na tym komponencie (poza flagą w GridConfig).")]
        [SerializeField] private bool drawGizmos = true;

        [Tooltip("Podświetl komórkę, nad którą znajduje się ten obiekt (do testów WorldToCell).")]
        [SerializeField] private bool highlightCellUnderTransform = true;

        /// <summary>Przypisana konfiguracja siatki (może być wykorzystana przez inne systemy).</summary>
        public GridConfig Config => config;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawGizmos || config == null || !config.DrawGizmos)
            {
                return;
            }

            var grid = new GridSystem(config);
            DrawGrid(grid);

            if (highlightCellUnderTransform)
            {
                HighlightCell(grid, grid.WorldToCell(transform.position));
            }
        }

        private void DrawGrid(GridSystem grid)
        {
            Gizmos.color = config.GizmoColor;
            Vector2Int dims = grid.Dimensions;

            // Linie biegnące wzdłuż osi Z (stałe X), dla każdej granicy kolumny.
            for (int x = 0; x <= dims.x; x++)
            {
                Vector3 start = grid.CellToWorld(new Vector2Int(x, 0));
                Vector3 end = grid.CellToWorld(new Vector2Int(x, dims.y));
                Gizmos.DrawLine(start, end);
            }

            // Linie biegnące wzdłuż osi X (stałe Z), dla każdej granicy wiersza.
            for (int y = 0; y <= dims.y; y++)
            {
                Vector3 start = grid.CellToWorld(new Vector2Int(0, y));
                Vector3 end = grid.CellToWorld(new Vector2Int(dims.x, y));
                Gizmos.DrawLine(start, end);
            }
        }

        private void HighlightCell(GridSystem grid, Vector2Int cell)
        {
            if (!grid.IsInside(cell))
            {
                return;
            }

            Color c = config.GizmoColor;
            Gizmos.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(c.a + 0.35f));
            Vector3 center = grid.CellToWorldCenter(cell);
            Gizmos.DrawCube(center, new Vector3(grid.CellSize, 0.02f, grid.CellSize));
        }
#endif
    }
}
