using System;

namespace RP_DataSavingSystem
{
    public enum CarControllerType
    {
        Daily4X4 = 0,
        DailyDrivers = 1,
        ExtremeToys = 2,
        StreetRacers = 3,
        SUV = 4,
        Vintage = 5
    }

    public enum CarClass
    {
        D = 0,
        C = 1,
        B = 2,
        A = 3,
        S = 4
    }

    public enum DriveWheel
    {
        FWD = 0,
        RWD = 1,
        AWD = 2
    }

    [Flags]
    public enum CarUnlockOption
    {
        None = 0,
        UnlockWithCoins = 1 << 0,
        UnlockWithSilver = 1 << 1,
        UnlockWithDiamonds = 1 << 2,
        UnlockByWatchingAd = 1 << 3,
        All = UnlockWithCoins | UnlockWithSilver | UnlockWithDiamonds | UnlockByWatchingAd
    }
}
