using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections.Generic;

// ===== الشنطة =====
// الأغراض اللي اشتريتها وما ركّبتها. اضغط على أي غرض وركّبه في الحوش.
public class BagScreen : MonoBehaviour
{
    static BagScreen instance;
    NaaqaWindow win;
    RectTransform grid;
    readonly List<GameObject> cells = new List<GameObject>();

    public static void Show()
    {
        if (instance == null)
        {
            instance = new GameObject("BagScreen").AddComponent<BagScreen>();
            instance.Build();
        }
        instance.win.Open();
        instance.Rebuild();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    void Build()
    {
        win = NaaqaWindow.Create("Bag", "الشنطة", "bag", new Vector2(1240, 800));
        win.transform.SetParent(transform, false);

        RectTransform area = NaaqaUI.Rect("Area", win.body);
        NaaqaUI.Stretch(area, 0, 0, 0, 90);
        grid = win.Grid(area, new Vector2(260, 320), 4);

        RTLTextMeshPro et;
        Button edit = NaaqaUI.MakeButton(win.body, "Edit", "حرّك أغراضك المتركبة", NaaqaUI.ButtonStyle.Secondary, 26, out et);
        NaaqaUI.Place((RectTransform)edit.transform, new Vector2(0.5f, 0), new Vector2(460, 74), new Vector2(0, 0));
        Image ei = NaaqaUI.IconImg(edit.transform, "edit", 54);
        NaaqaUI.Place(ei.rectTransform, new Vector2(1, 0.5f), new Vector2(54, 54), new Vector2(-12, 0));
        et.rectTransform.offsetMax = new Vector2(-70, 0);
        edit.onClick.AddListener(() => { win.Close(); EditMode.Enter(); });
    }

    void Rebuild()
    {
        foreach (GameObject g in cells) if (g != null) Destroy(g);
        cells.Clear();

        InventoryManager inv = InventoryManager.Instance;
        List<string> ids = inv != null ? inv.GetOwnedIds() : new List<string>();
        win.subtitle.text = ids.Count == 0 ? "الشنطة فاضية. اشترِ من متجر السوق الشعبي" : "اضغط على أي غرض وركّبه في حوشك";

        foreach (string id in ids)
        {
            BagItemData d = inv.GetData(id);
            if (d == null) continue;
            string capturedId = id;
            Image tile = NaaqaUI.Tile(grid, "Item_" + id);
            if (d.icon != null)
            {
                Image pic = NaaqaUI.Img(tile.transform, "Pic", d.icon, Color.white);
                pic.preserveAspect = true;
                NaaqaUI.Place(pic.rectTransform, new Vector2(0.5f, 1), new Vector2(190, 136), new Vector2(0, -12));
            }
            float y = -154;
            NaaqaWindow.Line(tile.transform, d.displayName, 26, NaaqaUI.Cream, true, ref y, 40);
            int n = inv.GetCount(id);
            if (n > 1) NaaqaWindow.Line(tile.transform, "عندك " + NaaqaUI.Ar(n), 22, NaaqaUI.Muted, false, ref y, 30);
            // الزر تحت لحاله (ما يطلع على الكلام)
            RTLTextMeshPro bt;
            Button b = NaaqaWindow.BottomButton(tile.transform, "ركّب", NaaqaUI.ButtonStyle.Primary, out bt, 56);
            b.onClick.AddListener(() => Place(capturedId));
            cells.Add(tile.gameObject);
        }
    }

    void Place(string id)
    {
        win.Close();
        if (PlacementManager.Instance != null) PlacementManager.Instance.StartPlacing(id);
    }
}
