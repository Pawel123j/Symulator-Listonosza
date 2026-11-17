using Listonosz.Core;
using UnityEngine;

namespace Listonosz.Data
{
    /// <summary>
    /// Kanał zdarzeń przenoszący przesyłkę (<see cref="DeliverySO"/>). Tworzymy z niego osobne assety:
    /// jeden na podjęcie, jeden na dostarczenie, jeden na niepowodzenie dostawy. Dzięki temu nadawca
    /// (DeliveryService) i odbiorcy (np. UI) komunikują się wyłącznie przez asset kanału.
    /// </summary>
    [CreateAssetMenu(fileName = "DeliveryEvent", menuName = "Listonosz/Events/Delivery Event Channel", order = 0)]
    public sealed class DeliveryEventChannelSO : EventChannelSO<DeliverySO>
    {
    }
}
