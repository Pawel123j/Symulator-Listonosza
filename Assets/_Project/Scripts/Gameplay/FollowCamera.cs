using UnityEngine;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// Prosta kamera podążająca za graczem (płynny lerp + stały offset, patrzy na cel). Dla mobile/feel
    /// daje widok trzecioosobowy/zza-pleców i jest bazą dla ruchu względem kamery w <see cref="PlayerController"/>.
    /// Umieść na obiekcie kamery i przypisz <see cref="target"/> = transform gracza.
    /// </summary>
    public sealed class FollowCamera : MonoBehaviour
    {
        [Tooltip("Cel podążania (transform gracza).")]
        [SerializeField] private Transform target;

        [Tooltip("Offset kamery względem celu (świat).")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -9f);

        [Tooltip("Szybkość wygładzania podążania (większe = ciaśniej).")]
        [Min(0f)]
        [SerializeField] private float followSharpness = 10f;

        [Tooltip("Wysokość punktu, na który patrzy kamera, ponad celem.")]
        [SerializeField] private float lookAtHeight = 1f;

        public void SetTarget(Transform newTarget) => target = newTarget;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desired = target.position + offset;
            float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
            transform.LookAt(target.position + Vector3.up * lookAtHeight);
        }
    }
}
