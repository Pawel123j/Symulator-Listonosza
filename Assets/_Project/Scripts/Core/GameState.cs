namespace Listonosz.Core
{
    /// <summary>
    /// Stany prostej maszyny „dnia pracy": początek dnia (briefing), praca (dostarczanie),
    /// koniec dnia (podsumowanie). Współdzielony kontrakt między logiką (GameLoopManager) a UI.
    /// </summary>
    public enum GameState
    {
        DayStart = 0,
        Working = 1,
        DayEnd = 2
    }
}
