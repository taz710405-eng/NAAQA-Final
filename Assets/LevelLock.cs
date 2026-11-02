using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;

// كرت في المتجر يفتح من مستوى معيّن
public class LevelLock : MonoBehaviour
{
    public int level = 2;

    ShopItemView view;
    bool wasLocked = false;

    void Awake() { view = GetComponent<ShopItemView>(); }

    void OnEnable() { wasLocked = false; Apply(); }

    void Update() { Apply(); }

    void Apply()
    {
        if (view == null || view.buyButton == null) return;
        int lvl = LevelManager.Instance != null ? LevelManager.Instance.Level : 1;
        bool locked = lvl < level;

        if (locked)
        {
            view.buyButton.interactable = false;
            if (!wasLocked && view.buyButtonText != null)
            {
                view.buyButtonText.text = "المستوى " + NaaqaUI.Ar(level);
                view.buyButtonText.ForceMeshUpdate();
            }
        }
        else if (wasLocked)
        {
            view.buyButton.interactable = true;
            view.RefreshTexts();
        }
        wasLocked = locked;
    }
}
