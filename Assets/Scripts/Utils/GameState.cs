namespace Vendorium
{
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        CashierMode,
        ShopMode,
        PlacementMode,
        Dialogue,
        GameOver
    }

    public enum MachineTrait
    {
        Reliable,   // Konstante Einnahmen ohne Überraschungen
        Moody,      // 20% Chance auf doppeltes Income, 10% Chance kein Verkauf
        Generous,   // Höherer Income-Bonus, aber teuer im Unterhalt
        Magnetic,   // Zieht mehr Kunden an (höhere Spawn-Rate in der Nähe)
        Fast        // Verkauft schneller (kürzeres Intervall)
    }

    public enum MachineState
    {
        Active,
        Empty,
        Broken,
        Upgrading
    }

    public enum CustomerType
    {
        Schueler,
        Bueroangestellter,
        Rentner,
        Jugendlicher,
        Tourist,
        VIP,
        Stammkunde
    }

    public enum CustomerState
    {
        Entering,
        Browsing,
        WalkingToMachine,
        WaitingInQueue,
        Buying,
        Leaving,
        Satisfied,
        Annoyed
    }

    public enum CustomerEmotion
    {
        Happy,
        Neutral,
        Annoyed
    }

    public enum UIScreen
    {
        None,
        HUD,
        MainMenu,
        PauseMenu,
        ShopScreen,
        MachineInspect,
        CashRegister,
        DailyReport,
        Settings,
        Credits,
        Dialogue,
        EventNotification
    }

    public enum EventType
    {
        Positive,
        Negative,
        Special,
        Neutral
    }

    public enum EventEffect
    {
        IncomeMultiplier,
        SpawnRateMultiplier,
        SpecificMachineBonus,
        ReputationChange,
        PowerOutage,
        ViralPost,
        HealthInspection
    }

    public enum SynergyEffect
    {
        DoubleChance,
        CustomerMagnet,
        RareDropChance,
        IncomeBonus
    }

    public enum TimeOfDay
    {
        Frueh,         // 06:00 - 09:00
        Morgen,        // 09:00 - 12:00
        Mittag,        // 12:00 - 15:00
        Nachmittag,    // 15:00 - 18:00
        Abend,         // 18:00 - 21:00
        Nacht          // 21:00 - 06:00
    }
}
