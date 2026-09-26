using System.Collections.Generic;
using UnityEngine;

namespace RP_DataSavingSystem
{
    [CreateAssetMenu(menuName = "Car Database/Game Database")]
    public sealed class CarGameDatabase : ScriptableObject
    {
        public List<CarDefinition> cars = new List<CarDefinition>();

        private Dictionary<string, CarDefinition> carLookup;

        public void BuildLookup()
        {
            int capacity = cars != null ? cars.Count : 0;
            carLookup = new Dictionary<string, CarDefinition>(capacity);

            if (cars == null)
                return;

            for (int i = 0; i < cars.Count; i++)
            {
                CarDefinition car = cars[i];
                if (car == null || string.IsNullOrWhiteSpace(car.carId) || carLookup.ContainsKey(car.carId))
                    continue;

                carLookup.Add(car.carId, car);
            }
        }

        public CarDefinition GetCarDefinition(string carId)
        {
            if (string.IsNullOrEmpty(carId))
                return null;

            if (carLookup == null)
                BuildLookup();

            return carLookup.TryGetValue(carId, out CarDefinition car) ? car : null;
        }
    }
}
