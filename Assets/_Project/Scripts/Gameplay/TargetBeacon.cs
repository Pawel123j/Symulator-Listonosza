using System.Collections.Generic;
using Listonosz.Data;
using Listonosz.Grid;
using UnityEngine;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// Świecący, bujający się znacznik nad najbliższym celem aktualnie niesionej przesyłki. Śledzi aktywne
    /// dostawy przez kanały SO (podjęcie dodaje, ukończenie/niepowodzenie usuwa) i pozycjonuje marker nad
    /// komórką adresu. Czytelność trasy — gracz od razu wie, dokąd iść. Marker budowany w kodzie (Play).
    /// </summary>
    public sealed class TargetBeacon : MonoBehaviour
    {
        [SerializeField] private DeliveryEventChannelSO onPickedUp;
        [SerializeField] private DeliveryEventChannelSO onCompleted;
        [SerializeField] private DeliveryEventChannelSO onFailed;
        [SerializeField] private Transform player;
        [SerializeField] private GridConfig gridConfig;
        [SerializeField] private Color color = new Color(1f, 0.85f, 0.2f);

        private readonly List<DeliverySO> _active = new List<DeliverySO>();
        private GridSystem _grid;
        private GameObject _marker;

        private void Awake()
        {
            if (gridConfig != null)
            {
                _grid = new GridSystem(gridConfig);
            }

            BuildMarker();
        }

        private void OnEnable()
        {
            Sub(onPickedUp, OnPicked);
            Sub(onCompleted, OnRemoved);
            Sub(onFailed, OnRemoved);
        }

        private void OnDisable()
        {
            Unsub(onPickedUp, OnPicked);
            Unsub(onCompleted, OnRemoved);
            Unsub(onFailed, OnRemoved);
        }

        private void OnPicked(DeliverySO d)
        {
            if (d != null && !_active.Contains(d))
            {
                _active.Add(d);
            }
        }

        private void OnRemoved(DeliverySO d) => _active.Remove(d);

        private void Update()
        {
            if (_marker == null)
            {
                return;
            }

            if (_grid == null || player == null || _active.Count == 0)
            {
                Hide();
                return;
            }

            DeliverySO best = null;
            Vector3 bestPos = default;
            float bestSqr = float.MaxValue;
            Vector3 pp = player.position;

            for (int i = 0; i < _active.Count; i++)
            {
                DeliverySO d = _active[i];
                if (d == null || d.Destination == null)
                {
                    continue;
                }

                Vector3 wp = _grid.CellToWorldCenter(d.Destination.Cell);
                float sq = (wp - pp).sqrMagnitude;
                if (sq < bestSqr)
                {
                    bestSqr = sq;
                    best = d;
                    bestPos = wp;
                }
            }

            if (best == null)
            {
                Hide();
                return;
            }

            if (!_marker.activeSelf)
            {
                _marker.SetActive(true);
            }

            float bob = Mathf.Sin(Time.time * 3f) * 0.25f;
            _marker.transform.position = new Vector3(bestPos.x, 3.3f + bob, bestPos.z);
            _marker.transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        }

        private void Hide()
        {
            if (_marker.activeSelf)
            {
                _marker.SetActive(false);
            }
        }

        private void BuildMarker()
        {
            _marker = new GameObject("Beacon");
            _marker.transform.SetParent(transform, false);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.4f);

            GameObject diamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diamond.name = "Beacon_diamond";
            diamond.transform.SetParent(_marker.transform, false);
            DestroyImmediate(diamond.GetComponent<Collider>());
            diamond.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            diamond.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            diamond.GetComponent<Renderer>().sharedMaterial = mat;

            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "Beacon_beam";
            beam.transform.SetParent(_marker.transform, false);
            DestroyImmediate(beam.GetComponent<Collider>());
            beam.transform.localScale = new Vector3(0.08f, 1.6f, 0.08f);
            beam.transform.localPosition = new Vector3(0f, -1.8f, 0f);
            beam.GetComponent<Renderer>().sharedMaterial = mat;

            _marker.SetActive(false);
        }

        private static void Sub(DeliveryEventChannelSO c, System.Action<DeliverySO> h)
        {
            if (c != null)
            {
                c.OnEventRaised += h;
            }
        }

        private static void Unsub(DeliveryEventChannelSO c, System.Action<DeliverySO> h)
        {
            if (c != null)
            {
                c.OnEventRaised -= h;
            }
        }
    }
}
