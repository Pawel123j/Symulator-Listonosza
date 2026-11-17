using UnityEngine;

namespace Listonosz.Grid
{
    /// <summary>
    /// Czysta logika mapowania świat↔komórka. Nie jest komponentem sceny i nie wpływa na ruch —
    /// udostępnia wyłącznie konwersje na potrzeby logiki (adresy, cele dostaw, occupancy).
    /// Konwencja: <c>cell.x</c> ↔ oś świata X, <c>cell.y</c> ↔ oś świata Z; siatka na płaszczyźnie XZ.
    /// Klasa jest niezależna od ScriptableObject (konstruktor wartościowy), więc łatwo ją testować.
    /// </summary>
    public sealed class GridSystem
    {
        /// <summary>Pozycja świata rogu komórki (0,0).</summary>
        public Vector3 Origin { get; }

        /// <summary>Długość boku komórki w jednostkach świata.</summary>
        public float CellSize { get; }

        /// <summary>Liczba komórek w osiach X i Z.</summary>
        public Vector2Int Dimensions { get; }

        public GridSystem(Vector3 origin, float cellSize, Vector2Int dimensions)
        {
            Origin = origin;
            CellSize = Mathf.Max(0.01f, cellSize);
            Dimensions = new Vector2Int(Mathf.Max(1, dimensions.x), Mathf.Max(1, dimensions.y));
        }

        /// <summary>Konstruktor wygodny: pobiera wartości z <see cref="GridConfig"/>.</summary>
        public GridSystem(GridConfig config)
            : this(
                (config ?? throw new System.ArgumentNullException(nameof(config))).Origin,
                config.CellSize,
                config.Dimensions)
        {
        }

        /// <summary>Mapuje pozycję świata na indeks komórki (zaokrąglenie w dół; oś Y jest ignorowana).</summary>
        public Vector2Int WorldToCell(Vector3 worldPosition)
        {
            int x = Mathf.FloorToInt((worldPosition.x - Origin.x) / CellSize);
            int y = Mathf.FloorToInt((worldPosition.z - Origin.z) / CellSize);
            return new Vector2Int(x, y);
        }

        /// <summary>Zwraca pozycję świata rogu komórki (najmniejsze X,Z) na wysokości <see cref="Origin"/>.Y.</summary>
        public Vector3 CellToWorld(Vector2Int cell)
        {
            float x = Origin.x + cell.x * CellSize;
            float z = Origin.z + cell.y * CellSize;
            return new Vector3(x, Origin.y, z);
        }

        /// <summary>Zwraca pozycję świata środka komórki.</summary>
        public Vector3 CellToWorldCenter(Vector2Int cell)
        {
            float half = CellSize * 0.5f;
            Vector3 corner = CellToWorld(cell);
            return new Vector3(corner.x + half, corner.y, corner.z + half);
        }

        /// <summary>Czy komórka mieści się w zadeklarowanych wymiarach siatki.</summary>
        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Dimensions.x
                && cell.y >= 0 && cell.y < Dimensions.y;
        }

        /// <summary>Przycina komórkę do granic siatki.</summary>
        public Vector2Int ClampCell(Vector2Int cell)
        {
            int x = Mathf.Clamp(cell.x, 0, Dimensions.x - 1);
            int y = Mathf.Clamp(cell.y, 0, Dimensions.y - 1);
            return new Vector2Int(x, y);
        }
    }
}
