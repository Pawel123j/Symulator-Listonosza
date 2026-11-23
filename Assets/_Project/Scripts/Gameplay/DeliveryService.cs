using System;
using System.Collections.Generic;
using Listonosz.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Listonosz.Gameplay
{
    /// <summary>
    /// Serwis dostarczania: wykrywa najbliższy obiekt interaktywny w zasięgu i — po naciśnięciu przycisku
    /// akcji — odbiera przesyłki (punkt odbioru) albo dostarcza je (cel dostawy). Waliduje, że gracz niesie
    /// przesyłkę zaadresowaną pod dany cel. Komunikaty do innych systemów wysyła wyłącznie przez kanały SO
    /// (podjęcie / dostarczenie / niepowodzenie) — patrz ADR-0001.
    /// </summary>
    [RequireComponent(typeof(PlayerInventory))]
    public sealed class DeliveryService : MonoBehaviour
    {
        [Header("Kanały zdarzeń (ScriptableObject)")]
        [SerializeField] private DeliveryEventChannelSO onPickedUp;
        [SerializeField] private DeliveryEventChannelSO onCompleted;
        [SerializeField] private DeliveryEventChannelSO onFailed;

        [Header("Debug")]
        [Tooltip("Loguj akcje interakcji do Console.")]
        [SerializeField] private bool logActions = true;

        internal bool InteractionsEnabled { get; set; } = true;

        internal void ResetForNewDay()
        {
            _completed.Clear();
            _inventory.Clear();
        }

        private PlayerInventory _inventory;
        private InputAction _interactAction;
        private readonly HashSet<DeliverySO> _completed = new HashSet<DeliverySO>();

        /// <summary>Obiekt interaktywny aktualnie w zasięgu (null, gdy brak). Do podświetlenia/UI.</summary>
        public IInteractable CurrentInteractable { get; private set; }

        /// <summary>Zgłaszane przy zmianie obiektu w zasięgu (na potrzeby podpowiedzi interakcji).</summary>
        public event Action<IInteractable> InteractableChanged;

        /// <summary>Liczba dostaw zaliczonych dotychczas.</summary>
        public int CompletedCount => _completed.Count;

        private void Awake()
        {
            _inventory = GetComponent<PlayerInventory>();

            _interactAction = new InputAction("Interact", InputActionType.Button);
            _interactAction.AddBinding("<Keyboard>/e");
            _interactAction.AddBinding("<Gamepad>/buttonSouth");
            _interactAction.performed += OnInteractPerformed;
        }

        private void OnEnable() => _interactAction?.Enable();

        private void OnDisable() => _interactAction?.Disable();

        private void OnDestroy()
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractPerformed;
                _interactAction.Dispose();
            }
        }

        private void Update()
        {
            RefreshCurrentInteractable();

            // Interakcja dotykowa (tap w prawą połowę ekranu) — równolegle do przycisku E / pada.
            if (MobileInput.ConsumeInteract())
            {
                TryInteract();
            }
        }

        private void RefreshCurrentInteractable()
        {
            IInteractable best = null;
            float bestSqr = float.MaxValue;
            Vector3 playerPos = transform.position;

            IReadOnlyList<IInteractable> all = InteractableRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                IInteractable candidate = all[i];
                if (candidate == null)
                {
                    continue;
                }

                float sqr = (candidate.InteractionWorldPosition - playerPos).sqrMagnitude;
                float radius = candidate.InteractionRadius;
                if (sqr <= radius * radius && sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }

            if (!ReferenceEquals(best, CurrentInteractable))
            {
                CurrentInteractable = best;
                InteractableChanged?.Invoke(best);
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext _) => TryInteract();

        private void TryInteract()
        {
            if (!InteractionsEnabled)
            {
                return;
            }

            switch (CurrentInteractable)
            {
                case DeliveryPickupPoint pickup:
                    HandlePickup(pickup);
                    break;
                case DeliveryTarget target:
                    HandleDelivery(target);
                    break;
            }
        }

        private void HandlePickup(DeliveryPickupPoint pickup)
        {
            int added = 0;
            IReadOnlyList<DeliverySO> available = pickup.Deliveries;
            for (int i = 0; i < available.Count; i++)
            {
                DeliverySO delivery = available[i];
                if (delivery == null || _completed.Contains(delivery) || _inventory.Contains(delivery))
                {
                    continue;
                }

                if (_inventory.TryAdd(delivery))
                {
                    added++;
                    onPickedUp?.Raise(delivery);
                }
                else if (_inventory.IsFull)
                {
                    Log("Torba pełna — nie udało się odebrać wszystkich przesyłek.");
                    break;
                }
            }

            Log(added > 0
                ? $"Odebrano {added} przesyłek (w torbie: {_inventory.Count})."
                : "Brak nowych przesyłek do odebrania.");
        }

        private void HandleDelivery(DeliveryTarget target)
        {
            if (!target.HasAddress)
            {
                Log("Cel dostawy bez przypisanego adresu.");
                return;
            }

            DeliverySO match = _inventory.FindForAddress(target.Address);
            if (match != null)
            {
                _inventory.Remove(match);
                _completed.Add(match);
                onCompleted?.Raise(match);
                Log($"Dostarczono '{match.Label}' pod {target.Address.DisplayName}. (zaliczone: {_completed.Count})");
                return;
            }

            // Brak pasującej przesyłki to nie porażka — tylko informacja zwrotna.
            Log($"Nie masz przesyłki na adres {target.Address.DisplayName}.");
        }

        /// <summary>Oznacza przesyłkę jako niedostarczoną (usuwa z torby, podnosi kanał porażki).
        /// Wykorzystywane m.in. przy podsumowaniu dnia (Etap 6).</summary>
        public void FailDelivery(DeliverySO delivery)
        {
            if (delivery == null)
            {
                return;
            }

            _inventory.Remove(delivery);
            onFailed?.Raise(delivery);
            Log($"Niepowodzenie dostawy: '{delivery.Label}'.");
        }

        /// <summary>Oznacza wszystkie niesione przesyłki jako niedostarczone (np. koniec dnia).</summary>
        public void FailAllCarried()
        {
            var snapshot = new List<DeliverySO>(_inventory.Carried);
            for (int i = 0; i < snapshot.Count; i++)
            {
                FailDelivery(snapshot[i]);
            }
        }

        private void Log(string message)
        {
            if (logActions)
            {
                Debug.Log($"[Listonosz] {message}", this);
            }
        }
    }
}
