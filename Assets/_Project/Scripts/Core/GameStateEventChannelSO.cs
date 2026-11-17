using UnityEngine;

namespace Listonosz.Core
{
    /// <summary>
    /// Kanał zdarzeń przenoszący zmianę stanu gry (<see cref="GameState"/>). Pozwala UI i innym systemom
    /// reagować na start/koniec dnia bez znajomości GameLoopManager. Leży w Core, bo ładunek (enum) też
    /// jest w Core — dzięki temu nie wprowadza zależności od warstwy Data.
    /// </summary>
    [CreateAssetMenu(fileName = "GameStateEvent", menuName = "Listonosz/Events/Game State Event Channel", order = 1)]
    public sealed class GameStateEventChannelSO : EventChannelSO<GameState>
    {
    }
}
