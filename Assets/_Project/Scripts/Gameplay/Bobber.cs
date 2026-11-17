using UnityEngine;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// Delikatna animacja „życia": kołysanie w pionie (bob), opcjonalny dryf w poziomie i przechył (sway).
    /// Używane do chmurek (dryf + bob) i koron drzew (bob + sway). Czysta matematyka transformacji,
    /// losowa faza na starcie, aby obiekty nie ruszały się zgodnie.
    /// </summary>
    public sealed class Bobber : MonoBehaviour
    {
        public float bobAmplitude = 0.2f;
        public float bobSpeed = 1.5f;
        public float swayDegrees = 0f;
        public float driftAmplitude = 0f;
        public float driftSpeed = 0.4f;
        public float phase = 0f;

        private Vector3 _startPos;
        private Quaternion _startRot;

        private void Awake()
        {
            _startPos = transform.localPosition;
            _startRot = transform.localRotation;
            if (Mathf.Approximately(phase, 0f))
            {
                phase = Random.value * 6.2831853f;
            }
        }

        private void Update()
        {
            float t = Time.time * bobSpeed + phase;

            Vector3 p = _startPos;
            p.y += Mathf.Sin(t) * bobAmplitude;
            if (driftAmplitude > 0f)
            {
                p.x += Mathf.Sin(Time.time * driftSpeed + phase) * driftAmplitude;
            }

            transform.localPosition = p;

            if (swayDegrees > 0f)
            {
                transform.localRotation = _startRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t) * swayDegrees);
            }
        }
    }
}
