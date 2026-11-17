using Listonosz.Grid;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// Swobodny ruch listonosza na płaszczyźnie XZ (new Input System, CharacterController).
    /// Ruch jest płynny i NIE jest snapowany do gridu. Niezależnie od ruchu komponent raportuje
    /// aktualną komórkę logiczną (przez <see cref="GridSystem"/>) — patrz ADR-0002 i ADR-0003.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Ruch")]
        [Tooltip("Prędkość bazowa w jednostkach/s.")]
        [SerializeField] private float moveSpeed = 5f;

        [Tooltip("Mnożnik prędkości przy sprincie.")]
        [SerializeField] private float sprintMultiplier = 1.7f;

        [Tooltip("Szybkość obrotu w stronę kierunku ruchu (stopnie/s).")]
        [SerializeField] private float rotationSpeed = 720f;

        [Tooltip("Przyspieszenie grawitacji (ujemne) — utrzymuje gracza na podłożu.")]
        [SerializeField] private float gravity = -20f;

        [Header("Kamera")]
        [Tooltip("Ruch względem kierunku patrzenia kamery (zalecane dla widoku 3D/mobile).")]
        [SerializeField] private bool cameraRelative = true;

        [Tooltip("Transform kamery dla ruchu względnego (jeśli pusty — użyje Camera.main).")]
        [SerializeField] private Transform cameraTransform;

        [Header("Grid (raportowanie komórki — bez wpływu na ruch)")]
        [SerializeField] private GridConfig gridConfig;

        [Tooltip("Loguj zmianę komórki do Console (debug).")]
        [SerializeField] private bool logCellChanges = true;

        private CharacterController _controller;
        private GridSystem _grid;
        private InputAction _moveAction;
        private InputAction _sprintAction;
        private float _verticalVelocity;
        private Vector2Int _currentCell;
        private bool _hasCell;

        /// <summary>Ostatnio wyznaczona komórka gridu, w której znajduje się gracz.</summary>
        public Vector2Int CurrentCell => _currentCell;

        /// <summary>Czy gracz znajduje się wewnątrz zadeklarowanej siatki.</summary>
        public bool HasValidCell => _hasCell && _grid != null && _grid.IsInside(_currentCell);

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (gridConfig != null)
            {
                _grid = new GridSystem(gridConfig);
            }
            else
            {
                Debug.LogWarning("[Listonosz] PlayerController: brak przypisanego GridConfig — raportowanie komórki wyłączone.", this);
            }

            SetupInput();
        }

        private void SetupInput()
        {
            _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            _moveAction.AddBinding("<Gamepad>/leftStick");

            _sprintAction = new InputAction("Sprint", InputActionType.Button);
            _sprintAction.AddBinding("<Keyboard>/leftShift");
            _sprintAction.AddBinding("<Gamepad>/leftStickPress");
        }

        private void OnEnable()
        {
            _moveAction?.Enable();
            _sprintAction?.Enable();
        }

        private void OnDisable()
        {
            _moveAction?.Disable();
            _sprintAction?.Disable();
        }

        private void OnDestroy()
        {
            _moveAction?.Dispose();
            _sprintAction?.Dispose();
        }

        private void Update()
        {
            // Łączymy wejście klawiatura/pad z dotykowym (MobileInput) — działają równolegle.
            Vector2 input = _moveAction.ReadValue<Vector2>() + MobileInput.Move;
            if (input.sqrMagnitude > 1f)
            {
                input = input.normalized;
            }

            // Domyślnie input.x -> świat X, input.y -> świat Z. W trybie względnym do kamery
            // przeliczamy wejście na płaszczyznę XZ wg kierunku patrzenia kamery.
            Vector3 planar;
            Transform cam = cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);
            if (cameraRelative && cam != null)
            {
                Vector3 forward = cam.forward;
                forward.y = 0f;
                forward.Normalize();
                Vector3 right = cam.right;
                right.y = 0f;
                right.Normalize();
                planar = right * input.x + forward * input.y;
            }
            else
            {
                planar = new Vector3(input.x, 0f, input.y);
            }

            if (planar.sqrMagnitude > 1f)
            {
                planar.Normalize();
            }

            float speed = moveSpeed * (_sprintAction.IsPressed() ? sprintMultiplier : 1f);
            Vector3 horizontal = planar * speed;

            // Grawitacja: dociśnij do podłoża, gdy ugruntowany; w przeciwnym razie narastaj.
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 velocity = horizontal + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);

            // Obrót płynnie w kierunku ruchu (tylko gdy jest wejście).
            if (planar.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(planar, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
            }

            ReportCell();
        }

        private void ReportCell()
        {
            if (_grid == null)
            {
                return;
            }

            Vector2Int cell = _grid.WorldToCell(transform.position);
            if (_hasCell && cell == _currentCell)
            {
                return;
            }

            _currentCell = cell;
            _hasCell = true;

            if (logCellChanges)
            {
                string suffix = _grid.IsInside(cell) ? string.Empty : " (poza siatką)";
                Debug.Log($"[Listonosz] Gracz w komórce {cell}{suffix}.", this);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (gridConfig == null)
            {
                return;
            }

            GridSystem grid = _grid ?? new GridSystem(gridConfig);
            Vector2Int cell = grid.WorldToCell(transform.position);
            if (!grid.IsInside(cell))
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.6f);
            Gizmos.DrawCube(grid.CellToWorldCenter(cell), new Vector3(grid.CellSize, 0.05f, grid.CellSize));
        }
#endif
    }
}
