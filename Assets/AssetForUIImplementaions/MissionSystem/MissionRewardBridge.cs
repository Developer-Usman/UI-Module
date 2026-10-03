using RP_DataSavingSystem;
using UnityEngine;

namespace RP_MissionSystem
{
    /// <summary>
    /// Adapter between the mission system and the game's wallet. Keeps <see cref="MissionManager"/>
    /// unaware of any economy code (Dependency Inversion). Put it on the same persistent GameObject
    /// as the MissionManager so rewards are never missed.
    /// </summary>
    public sealed class MissionRewardBridge : MonoBehaviour
    {
        private void OnEnable() => MissionEvents.OnRewardClaimed += HandleRewardClaimed;

        private void OnDisable() => MissionEvents.OnRewardClaimed -= HandleRewardClaimed;

        private static void HandleRewardClaimed(RewardType type, int amount)
        {
            if (!TryMapCurrency(type, out CarCurrencyType currency))
            {
                // TODO: route XP to the player progression system once it exists.
                Debug.Log($"[MissionRewardBridge] {type} x{amount} claimed (no receiver yet).");
                return;
            }

            CarSaveSystem saveSystem = CarSaveSystem.Instance;
            if (saveSystem == null)
            {
                Debug.LogError($"[MissionRewardBridge] CarSaveSystem missing – {type} x{amount} could not be granted.");
                return;
            }

            if (CarCurrencyService.Instance != null)
                CarCurrencyService.Instance.Add(currency, amount);
            else
                saveSystem.AddCurrency(currency, amount);

            // The wallet only flushes on pause/quit; save now so the claim and the currency persist together.
            saveSystem.SaveNow();
        }

        private static bool TryMapCurrency(RewardType type, out CarCurrencyType currency)
        {
            switch (type)
            {
                case RewardType.Gold:    currency = CarCurrencyType.Coins;    return true;
                case RewardType.Silver:  currency = CarCurrencyType.Silver;   return true;
                case RewardType.Diamond: currency = CarCurrencyType.Diamonds; return true;
                default:                 currency = default;                  return false;
            }
        }
    }
}
