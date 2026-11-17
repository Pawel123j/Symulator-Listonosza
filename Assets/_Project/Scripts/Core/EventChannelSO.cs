using System;
using UnityEngine;

namespace Listonosz.Core
{
    /// <summary>
    /// Bazowy kanał zdarzeń oparty na ScriptableObject, przenoszący ładunek typu <typeparamref name="T"/>.
    /// Nadawca woła <see cref="Raise"/>, odbiorcy subskrybują <see cref="OnEventRaised"/> — strony nie znają
    /// się nawzajem, łączy je tylko asset kanału (patrz ADR-0001). Konkretne kanały tworzymy jako klasy
    /// zamykające generyk (np. w warstwie Data), aby uniknąć cyklu zależności Core↔Data.
    /// </summary>
    public abstract class EventChannelSO<T> : ScriptableObject
    {
        /// <summary>Zdarzenie wywoływane przy <see cref="Raise"/>.</summary>
        public event Action<T> OnEventRaised;

        /// <summary>Podnosi zdarzenie z podanym ładunkiem do wszystkich subskrybentów.</summary>
        public void Raise(T payload)
        {
            OnEventRaised?.Invoke(payload);
        }
    }
}
