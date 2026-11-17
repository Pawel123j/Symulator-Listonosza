using Listonosz.Data;
using Listonosz.Grid;
using UnityEngine;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// „Juice" dostawy: przy ukończeniu (kanał OnDeliveryCompleted) wystrzeliwuje confetti i odtwarza
    /// krótki dźwięk nad celem. Dźwięk generowany proceduralnie (bez assetów). Pozycję celu wylicza
    /// z adresu przesyłki i configu gridu.
    /// </summary>
    public sealed class DeliveryFeedback : MonoBehaviour
    {
        [SerializeField] private DeliveryEventChannelSO onCompleted;
        [SerializeField] private GridConfig gridConfig;

        private GridSystem _grid;
        private AudioClip _ding;
        private Material _particleMat;

        private void Awake()
        {
            if (gridConfig != null)
            {
                _grid = new GridSystem(gridConfig);
            }

            _ding = MakeDing();
            _particleMat = MakeParticleMaterial();
        }

        private void OnEnable()
        {
            if (onCompleted != null)
            {
                onCompleted.OnEventRaised += OnDone;
            }
        }

        private void OnDisable()
        {
            if (onCompleted != null)
            {
                onCompleted.OnEventRaised -= OnDone;
            }
        }

        private void OnDone(DeliverySO d)
        {
            Vector3 pos = (_grid != null && d != null && d.Destination != null)
                ? _grid.CellToWorldCenter(d.Destination.Cell)
                : transform.position;
            pos.y += 0.7f;

            SpawnConfetti(pos);

            if (_ding != null)
            {
                AudioSource.PlayClipAtPoint(_ding, pos, 0.7f);
            }
        }

        private void SpawnConfetti(Vector3 pos)
        {
            var go = new GameObject("Confetti");
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // ParticleSystem auto-gra na Awake — zatrzymaj przed konfiguracją

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 1.1f;
            main.startSpeed = 4.5f;
            main.startSize = 0.16f;
            main.gravityModifier = 1.5f;
            main.maxParticles = 90;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.4f, 0.55f), new Color(0.3f, 0.7f, 1f));

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)55) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 42f;
            shape.radius = 0.12f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (_particleMat != null)
            {
                renderer.sharedMaterial = _particleMat;
            }

            ps.Play();
        }

        private static Material MakeParticleMaterial()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s == null)
            {
                s = Shader.Find("Sprites/Default");
            }

            return s != null ? new Material(s) : null;
        }

        private static AudioClip MakeDing()
        {
            const int sampleRate = 44100;
            const float duration = 0.25f;
            int n = (int)(sampleRate * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 11f);
                float wave = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.4f;
                data[i] = wave * env * 0.4f;
            }

            AudioClip clip = AudioClip.Create("Ding", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
