using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RP_MissionSystem
{
    /// <summary>
    /// Pure view for a single mission. Displays title, progress, claim state and up to N pre-placed reward slots.
    /// Holds no mission state – reads it from <see cref="MissionManager"/> on <see cref="Refresh"/>.
    /// </summary>
    public sealed class MissionCardUI : MonoBehaviour
    {
        /// <summary>One pre-placed reward slot on the card. Unused slots are hidden.</summary>
        [Serializable]
        public struct RewardSlot
        {
            public GameObject root;
            public Image icon;
            public TMP_Text amountText;
        }

        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text progressText;

        [Header("Progress Bar (optional)")]
        [Tooltip("Image with Image Type = Filled. Leave empty if the card has no bar.")]
        [SerializeField] private Image progressFill;

        [Header("Claim")]
        [SerializeField] private Button claimButton;
        [Tooltip("Shown once the reward has been claimed (e.g. 'Claimed' label / tick).")]
        [SerializeField] private GameObject claimedRoot;

        [Header("Rewards")]
        [SerializeField] private RewardSlot[] rewardSlots = Array.Empty<RewardSlot>();

        private MissionDataSO _data;

        public string MissionID => _data != null ? _data.MissionID : null;

        private void Awake() => claimButton.onClick.AddListener(OnClaimClicked);

        private void OnDestroy() => claimButton.onClick.RemoveListener(OnClaimClicked);

        /// <summary>Assigns a mission and draws its static parts (title, rewards) once, then refreshes dynamic parts.</summary>
        public void Bind(MissionDataSO data)
        {
            _data = data;
            titleText.text = data.MissionTitle;
            BindRewards();
            Refresh();
        }

        /// <summary>Redraws progress + claim state. Cheap; called only when the manager reports a change.</summary>
        public void Refresh()
        {
            MissionManager manager = MissionManager.Instance;
            if (_data == null || manager == null)
                return;

            float target = _data.TargetValue;
            float progress = Mathf.Min(manager.GetProgress(_data.MissionID), target);
            MissionState state = manager.GetState(_data.MissionID);

            // Floor progress so "100/100" only appears when truly complete. SetText avoids string allocations.
            progressText.SetText("{0:0}/{1:0}", Mathf.Floor(progress), target);

            if (progressFill != null)
                progressFill.fillAmount = progress / target;

            claimButton.gameObject.SetActive(state != MissionState.Claimed);
            claimButton.interactable = state == MissionState.Claimable;

            if (claimedRoot != null)
                claimedRoot.SetActive(state == MissionState.Claimed);
        }

        private void BindRewards()
        {
            int rewardCount = _data.Rewards.Count;

            if (rewardCount > rewardSlots.Length)
                Debug.LogWarning($"[MissionCardUI] '{_data.name}' has {rewardCount} rewards but card has only {rewardSlots.Length} slots.", this);

            for (int i = 0; i < rewardSlots.Length; i++)
            {
                RewardSlot slot = rewardSlots[i];
                bool used = i < rewardCount;
                slot.root.SetActive(used);

                if (!used)
                    continue;

                MissionReward reward = _data.Rewards[i];
                slot.icon.sprite = reward.icon;
                slot.icon.enabled = reward.icon != null;
                slot.amountText.SetText("{0}", reward.amount);
            }
        }

        private void OnClaimClicked()
        {
            if (_data != null && MissionManager.Instance != null)
                MissionManager.Instance.TryClaim(_data.MissionID);
        }
    }
}
