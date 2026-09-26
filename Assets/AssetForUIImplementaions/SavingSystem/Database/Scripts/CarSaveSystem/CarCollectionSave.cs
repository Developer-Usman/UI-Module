using System;
using System.Collections.Generic;

namespace RP_DataSavingSystem
{
    [Serializable]
    public sealed class CarCollectionSave
    {
        public List<CarOwnedState> cars = new List<CarOwnedState>();
    }
}
