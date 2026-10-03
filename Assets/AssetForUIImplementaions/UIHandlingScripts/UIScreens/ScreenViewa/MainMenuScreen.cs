using UnityEngine;
using UnityEngine.UI;

public class MainMenuScreen : MonoBehaviour
{
    [Header("Init")]
    [SerializeField] private bool isInit;

    [Header("Views")]
    [SerializeField] private View garageView;
    private View _view;
    
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button garageButton;


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
        garageButton.onClick.AddListener(OnGarageButtonClicked);
    }

    private void RemoveButtonListeners()
    {
        playButton.onClick.RemoveListener(OnPlayButtonClicked);
        garageButton.onClick.RemoveListener(OnGarageButtonClicked);

    }

    #endregion

    #region Button Actions

    private void OnPlayButtonClicked()
    {
       ViewHandler.Show(garageView);
    }

    private void OnGarageButtonClicked()
    {
        ViewHandler.Show(garageView);
    }

    #endregion

    #region View Refresh

    private void RefreshView()
    {
    }

    #endregion
}
