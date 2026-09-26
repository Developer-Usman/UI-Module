using System;

namespace RP_DataSavingSystem
{
    [Serializable]
    public sealed class CarCurrencyBalance
    {
        public CarCurrencyType type;
        public int amount;

        public CarCurrencyBalance(CarCurrencyType type, int amount = 0)
        {
            this.type = type;
            this.amount = amount;
        }
    }
}
