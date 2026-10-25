using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Screens")]
    public GameObject startScreen;
    public GameObject hud;

    [Header("Windows")]
    public PopupWindow ordersWindow;
    public PopupWindow tasksWindow;
    public PopupWindow upgradeWindow;
    public PopupWindow farmWindow;

    [Header("Overlay")]
    public GameObject dimBackground;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (startScreen != null) startScreen.SetActive(true);
        if (hud != null) hud.SetActive(false);
        CloseAll();
    }

    public void OnStartPressed()
    {
        if (startScreen != null) startScreen.SetActive(false);
        if (hud != null) hud.SetActive(true);
    }

    public void OpenOrders() { Open(ordersWindow); }
    public void OpenTasks() { Open(tasksWindow); }
    public void OpenUpgrade() { Open(upgradeWindow); }
    public void OpenFarm() { Open(farmWindow); }

    void Open(PopupWindow window)
    {
        if (window == null) return;
        CloseAll();
        if (dimBackground != null) dimBackground.SetActive(true);
        window.Show();
    }

    public void CloseAll()
    {
        PopupWindow[] windows = { ordersWindow, tasksWindow, upgradeWindow, farmWindow };
        foreach (PopupWindow w in windows)
        {
            if (w != null) w.Hide();
        }
        if (dimBackground != null) dimBackground.SetActive(false);
    }
}