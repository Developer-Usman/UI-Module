using UnityEngine;
using UnityEngine.UI;

public class GarageScreen : MonoBehaviour
{
    [Header("Init")]
    [SerializeField] private bool isInit;

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

    }

    private void RemoveButtonListeners()
    {

    }

    #endregion

    #region Button Actions

    #endregion

    #region View Refresh

    private void RefreshView()
    {
    }

    #endregion
}
