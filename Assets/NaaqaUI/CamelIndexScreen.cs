using UnityEngine;
using UnityEngine.UI;
using RTLTMPro;
using TMPro;
using System.Collections.Generic;

// ===== سجل الإبل =====
// كل السلالات: اللي عندك (كم وحدة، وأسماء إبلك) واللي ما عندك (مقفلة وصورتها ظل).
public class CamelIndexScreen : MonoBehaviour
{
    static CamelIndexScreen instance;
    NaaqaWindow win;
    RectTransform grid;
    readonly List<GameObject> cells = new List<GameObject>();

    public static void Show()
    {
        if (instance == null)
        {
            instance = new GameObject("CamelIndexScreen").AddComponent<CamelIndexScreen>();
            instance.Build();
        }
        instance.win.Open();
        instance.Rebuild();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    void Build()
    {
        win = NaaqaWindow.Create("CamelIndex", "سجل الإبل", "orders", new Vector2(1500, 900));
        win.transform.SetParent(transform, false);
        RectTransform area = NaaqaUI.Rect("Area", win.body);
        NaaqaUI.Stretch(area);
        grid = win.Grid(area, new Vector2(320, 400), 4);
    }

    // أسماء إبلك من هالسلالة
    static List<string> NamesOf(string breedId)
    {
        List<string> l = new List<string>();
        foreach (Camel c in Object.FindObjectsByType<Camel>(FindObjectsInactive.Exclude))
            if (CamelBreeds.BreedOf(c) == breedId) l.Add(CamelProfiles.Get(c).name);
        return l;
    }

    void Rebuild()
    {
        foreach (GameObject g in cells) if (g != null) Destroy(g);
        cells.Clear();

        int found = 0;
        foreach (CamelBreeds.Breed b in CamelBreeds.All) if (CamelBreeds.CountOf(b.id) > 0) found++;
        win.subtitle.text = "اكتشفت " + NaaqaUI.Ar(found) + " من " + NaaqaUI.Ar(CamelBreeds.All.Length) + " سلالات  •  اشترِ الباقي من سوق الإبل";

        // اللي عندك أول، بعدها الباقي من العادية للأسطورية
        List<CamelBreeds.Breed> list = new List<CamelBreeds.Breed>(CamelBreeds.All);
        list.Sort((a, c) =>
        {
            int ha = CamelBreeds.CountOf(a.id) > 0 ? 0 : 1, hc = CamelBreeds.CountOf(c.id) > 0 ? 0 : 1;
            if (ha != hc) return ha - hc;
            return a.rarity != c.rarity ? a.rarity - c.rarity : a.price - c.price;
        });

        foreach (CamelBreeds.Breed b in list) cells.Add(Card(b));
    }

    GameObject Card(CamelBreeds.Breed b)
    {
        int n = CamelBreeds.CountOf(b.id);
        bool have = n > 0;
        Image tile = NaaqaUI.Tile(grid, "Breed_" + b.id, have ? 0.08f : 0.04f);
        if (have)
        {
            Image line = NaaqaUI.Img(tile.transform, "Line", NaaqaUI.RoundedOutline(), b.RarityColor * new Color(1, 1, 1, 0.6f), 22f);
            NaaqaUI.Stretch(line.rectTransform);
        }

        // الصورة (اللي ما عندك تطلع ظل غامق)
        Texture2D tex = Resources.Load<Texture2D>("NaaqaCamels/camel_" + b.id);
        RectTransform pa = NaaqaUI.Rect("Pic", tile.transform);
        pa.anchorMin = new Vector2(0, 1); pa.anchorMax = new Vector2(1, 1);
        pa.pivot = new Vector2(0.5f, 1f);
        pa.sizeDelta = new Vector2(-16, 170);
        pa.anchoredPosition = new Vector2(0, -8);
        if (tex != null)
        {
            RawImage pic = pa.gameObject.AddComponent<RawImage>();
            pic.texture = tex;
            pic.uvRect = new Rect(0f, 0.03f, 1f, 0.87f);
            pic.color = have ? Color.white : new Color(0.05f, 0.03f, 0.02f, 0.85f);
            pic.raycastTarget = false;
        }
        if (!have)
        {
            Image lk = NaaqaUI.IconImg(tile.transform, "lock", 60);
            NaaqaUI.Place(lk.rectTransform, new Vector2(0.5f, 1), new Vector2(60, 60), new Vector2(0, -60));
        }

        // شارة الندرة
        Image rb = NaaqaUI.Img(tile.transform, "Rarity", NaaqaUI.Rounded(), new Color(0.07f, 0.045f, 0.025f, 0.85f), 12f);
        NaaqaUI.Place(rb.rectTransform, new Vector2(1, 1), new Vector2(110, 34), new Vector2(-10, -10));
        RTLTextMeshPro rt = NaaqaUI.Text(rb.transform, "T", b.RarityName, 18, b.RarityColor, TextAlignmentOptions.Center, true);
        NaaqaUI.Stretch(rt.rectTransform, 4, 4, 0, 2);

        float y = -182;
        NaaqaWindow.Line(tile.transform, b.name, 28, have ? NaaqaUI.Cream : NaaqaUI.Muted, true, ref y, 38);
        NaaqaWindow.Line(tile.transform, "شاطرة في " + b.Best + "  •  " + b.color, 18, NaaqaUI.Muted, false, ref y, 28);
        NaaqaWindow.Line(tile.transform, "حليب " + Stars(b.MilkStars) + "  " + (b.RacingEligible ? "سباق " + Stars(b.RaceStars) : "ما تدخل السباق") + "  مزاين " + Stars(b.beauty), 18, NaaqaUI.OrangeLight, false, ref y, 28);

        string foot;
        Color fc;
        if (have)
        {
            List<string> names = NamesOf(b.id);
            string shown = names.Count <= 3 ? string.Join("، ", names) : string.Join("، ", names.GetRange(0, 3)) + " +" + NaaqaUI.Ar(names.Count - 3);
            foot = "عندك " + NaaqaUI.Ar(n) + "\n" + shown;
            fc = NaaqaUI.OkColor;
        }
        else
        {
            foot = "ما عندك • " + NaaqaUI.Ar(b.price) + " قرش\nتفتح بمستوى " + NaaqaUI.Ar(b.unlockLevel);
            fc = NaaqaUI.Muted;
        }
        RTLTextMeshPro ft = NaaqaUI.Text(tile.transform, "Foot", foot, 19, fc, TextAlignmentOptions.Center, have);
        ft.rectTransform.anchorMin = new Vector2(0, 0); ft.rectTransform.anchorMax = new Vector2(1, 0);
        ft.rectTransform.pivot = new Vector2(0.5f, 0f);
        ft.rectTransform.sizeDelta = new Vector2(-20, 62);
        ft.rectTransform.anchoredPosition = new Vector2(0, 10);
        return tile.gameObject;
    }

    static string Stars(int n)
    {
        return NaaqaUI.Ar(n) + "/" + NaaqaUI.Ar(3);   // (النجوم ما هي في الخط العربي)
    }
}
