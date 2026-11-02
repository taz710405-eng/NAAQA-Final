using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections.Generic;

// ===== البريد =====
// كل الرسائل المهمة (مستوى جديد، طلبات، مبيعات، أماكن انفتحت...) والهدية اليومية تستلمها من هنا.
public class MailScreen : MonoBehaviour
{
    static MailScreen instance;
    NaaqaWindow win;
    RectTransform list;
    readonly List<GameObject> rows = new List<GameObject>();

    public static void Show()
    {
        if (instance == null)
        {
            instance = new GameObject("MailScreen").AddComponent<MailScreen>();
            instance.Build();
        }
        instance.win.Open();
        instance.Rebuild();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    void Build()
    {
        win = NaaqaWindow.Create("Mail", "البريد", "mail", new Vector2(1100, 840));
        win.transform.SetParent(transform, false);
        win.onClose = MailBox.MarkAllRead;
        list = win.List(win.body, 12);
    }

    void Rebuild()
    {
        foreach (GameObject g in rows) if (g != null) Destroy(g);
        rows.Clear();

        List<MailBox.Letter> all = MailBox.All;
        win.subtitle.text = all.Count == 0 ? "ما عندك رسائل للحين" : "عندك " + NaaqaUI.Ar(all.Count) + " رسالة";

        foreach (MailBox.Letter l in all)
        {
            MailBox.Letter letter = l;
            bool gift = MailBox.HasGift(l);
            RTLTextMeshPro main, sub, bt;
            Button btn;
            Image row = NaaqaWindow.Row(list, gift ? "trophy" : "mail", null, 104, out main, out sub, out btn, out bt,
                gift ? (l.claimed ? "انستلمت" : "استلم") : null, 200);
            main.text = l.text;
            main.fontSize = 25;
            main.fontStyle = l.read ? FontStyles.Normal : FontStyles.Bold;
            string giftText = "";
            if (gift)
            {
                if (l.giftCoins > 0) giftText += NaaqaUI.Ar(l.giftCoins) + " قرش";
                giftText = "  •  الهدية: " + giftText;
            }
            sub.text = MailBox.Ago(l.time) + giftText;
            if (!l.read) row.color = new Color(1f, 0.75f, 0.4f, 0.1f);
            if (btn != null)
            {
                btn.interactable = !l.claimed;
                btn.onClick.AddListener(() =>
                {
                    if (MailBox.Claim(letter))
                    {
                        win.SetStatus("استلمت الهدية!", true);
                        if (SoundManager.Instance != null) SoundManager.Instance.PlayClick();
                    }
                    Rebuild();
                });
            }
            rows.Add(row.gameObject);
        }

        if (all.Count == 0)
        {
            RTLTextMeshPro empty = NaaqaUI.Text(list, "Empty", "البريد فاضي. أي شي مهم يصير في حوشك بيوصلك هنا", 26, NaaqaUI.Muted, TextAlignmentOptions.Center);
            NaaqaUI.Height(empty, 120);
            rows.Add(empty.gameObject);
        }
    }
}
