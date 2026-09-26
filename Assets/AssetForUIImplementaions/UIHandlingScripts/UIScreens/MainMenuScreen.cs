using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuScreen : MonoBehaviour
{
    [Header("Init")]
    [SerializeField] private bool isInit;

    [Header("Links")]
    [SerializeField] private string rateUsUrl = "https://www.google.com";
    [SerializeField] private string privacyPolicyUrl = "https://www.freeprivacypolicy.com/live/4317c7b8-b2ca-425b-b28c-1dc12580ddfb";

    [Header("Views")]
    [SerializeField] private View modeView;
    [SerializeField] private View settingsView;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button rateUsButton;
    [SerializeField] private Button privacyPolicyButton;

    private View _view;

    private void Awake()
    {
        _view = GetComponent<View>();
        _view.OnInitEvent.AddListener(OnViewInit);
        _view.OnShowEvent.AddListener(OnViewShow);
        _view.OnHideEvent.AddListener(OnViewHide);

        RegisterButtonListeners();
    }

    private void OnDestroy()
    {
        _view.OnInitEvent.RemoveListener(OnViewInit);
        _view.OnShowEvent.RemoveListener(OnViewShow);
        _view.OnHideEvent.RemoveListener(OnViewHide);

        RemoveButtonListeners();
    }

    #region View Lifecycle

    private void OnViewInit()
    {
        Init();
    }

    private void Init()
    {
        if (isInit)
            return;

        isInit = true;
    }

    private void OnViewShow()
    {
        RefreshView();
    }

    private void OnViewHide()
    {
    }

    #endregion

    #region Button Listeners

    private void RegisterButtonListeners()
    {
        playButton.onClick.AddListener(OnPlayButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        rateUsButton.onClick.AddListener(OnRateUsButtonClicked);
        privacyPolicyButton.onClick.AddListener(OnPrivacyPolicyButtonClicked);
    }

    private void RemoveButtonListeners()
    {
        playButton.onClick.RemoveListener(OnPlayButtonClicked);
        settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
        rateUsButton.onClick.RemoveListener(OnRateUsButtonClicked);
        privacyPolicyButton.onClick.RemoveListener(OnPrivacyPolicyButtonClicked);
    }

    #endregion

    #region Button Actions

    private void OnPlayButtonClicked()
    {
        ViewHandler.Show(modeView);
    }

    private void OnSettingsButtonClicked()
    {
        ViewHandler.Show(settingsView);
    }

    private void OnRateUsButtonClicked()
    {
        Application.OpenURL(rateUsUrl);
    }

    private void OnPrivacyPolicyButtonClicked()
    {
        Application.OpenURL(privacyPolicyUrl);
    }

    #endregion

    #region View Refresh

    private void RefreshView()
    {
    }

    #endregion
}
