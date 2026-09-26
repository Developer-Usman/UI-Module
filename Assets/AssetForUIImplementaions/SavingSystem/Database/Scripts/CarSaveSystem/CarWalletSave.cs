using System;
using System.Collections.Generic;

namespace RP_DataSavingSystem
{
    [Serializable]
    public sealed class CarWalletSave
    {
        public List<CarCurrencyBalance> balances = new List<CarCurrencyBalance>
        {
            new CarCurrencyBalance(CarCurrencyType.Coins),
            new CarCurrencyBalance(CarCurrencyType.Silver),
            new CarCurrencyBalance(CarCurrencyType.Diamonds)
        };
    }
}
