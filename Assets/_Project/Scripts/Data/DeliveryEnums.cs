namespace Listonosz.Data
{
    /// <summary>Rodzaj przesyłki — wpływa (docelowo) na obsługę i wygląd w UI.</summary>
    public enum DeliveryType
    {
        Letter = 0,       // list
        SmallParcel = 1,  // mała paczka
        LargeParcel = 2,  // duża paczka
        Registered = 3    // przesyłka polecona
    }

    /// <summary>Priorytet dostawy — porządkowanie listy zadań / punktacja.</summary>
    public enum DeliveryPriority
    {
        Low = 0,
        Normal = 1,
        High = 2
    }
}
