using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using RTLTMPro;
using TMPro;
using System.Collections;

// ===== حذف الحساب =====
// يطلع رمز من ٤ أرقام، واللاعب يكتبه عشان يتأكد. بعدها ينحذف حسابه وكل تقدمه، وترجع اللعبة من البداية.
// (يحتاج Supabase/delete_account.sql ينشغّل مرة وحدة في Supabase)
public class DeleteAccountScreen : MonoBehaviour
{
    static DeleteAccountScreen instance;
    NaaqaWindow win;
    RTLTextMeshPro codeText, info;
    TMP_InputField field;
    Button go;
    string code;
    bool busy;

    public static void Show()
    {
        if (instance == null)
        {
            instance = new GameObject("DeleteAccountScreen").AddComponent<DeleteAccountScreen>();
            instance.Build();
        }
        instance.code = Random.Range(1000, 10000).ToString();
        instance.codeText.text = NaaqaUI.Ar(int.Parse(instance.code));
        instance.field.text = "";
        instance.busy = false;
        instance.go.interactable = true;
        instance.win.Open();
    }

    void OnDestroy() { if (win != null) Destroy(win.gameObject); }

    void Build()
    {
        win = NaaqaWindow.Create("DeleteAccount", "حذف الحساب", "delete", new Vector2(900, 720), 790);
        win.transform.SetParent(transform, false);
        win.subtitle.text = "ينحذف حسابك وكل تقدمك: إبلك وقروشك وأحواشك. ما يرجع!";
        win.subtitle.color = NaaqaUI.ErrorColor;

        info = NaaqaUI.Text(win.body, "Info", "عشان تتأكد، اكتب هذا الرمز:", 28, NaaqaUI.Cream, TextAlignmentOptions.Center, true);
        NaaqaUI.Place(info.rectTransform, new Vector2(0.5f, 1), new Vector2(800, 50), new Vector2(0, -10));

        Image pill = NaaqaUI.Img(win.body, "Code", NaaqaUI.Rounded(), new Color(0.05f, 0.03f, 0.02f, 0.9f), 20f);
        NaaqaUI.Place(pill.rectTransform, new Vector2(0.5f, 1), new Vector2(320, 100), new Vector2(0, -74));
        Image line = NaaqaUI.Img(pill.transform, "Line", NaaqaUI.RoundedOutline(), new Color(0.9f, 0.3f, 0.25f, 0.9f), 20f);
        NaaqaUI.Stretch(line.rectTransform);
        codeText = NaaqaUI.Text(pill.transform, "T", "", 56, new Color(1f, 0.55f, 0.45f), TextAlignmentOptions.Center, true);
        NaaqaUI.Stretch(codeText.rectTransform, 0, 0, 0, 4);
        codeText.characterSpacing = 18;

        field = NaaqaUI.Field(win.body, "Input", "اكتب الرمز هنا", false, TMP_InputField.ContentType.IntegerNumber, 84f);
        NaaqaUI.Place((RectTransform)field.transform, new Vector2(0.5f, 1), new Vector2(420, 84), new Vector2(0, -200));
        field.characterLimit = 4;

        RTLTextMeshPro gt;
        go = NaaqaUI.MakeButton(win.body, "Go", "احذف حسابي", NaaqaUI.ButtonStyle.Danger, 30, out gt);
        Image gbg = go.GetComponent<Image>();
        if (gbg != null) gbg.color = new Color(0.75f, 0.16f, 0.12f, 0.95f);
        gt.color = Color.white;
        NaaqaUI.Place((RectTransform)go.transform, new Vector2(0.5f, 0), new Vector2(460, 92), new Vector2(0, 20));
        go.onClick.AddListener(Confirm);
    }

    static string Latin(string s)
    {
        char[] c = (s ?? "").ToCharArray();
        for (int i = 0; i < c.Length; i++)
        {
            if (c[i] >= '٠' && c[i] <= '٩') c[i] = (char)('0' + (c[i] - '٠'));
            if (c[i] >= '۰' && c[i] <= '۹') c[i] = (char)('0' + (c[i] - '۰'));
        }
        return new string(c).Trim();
    }

    void Confirm()
    {
        if (busy) return;
        if (Latin(field.text) != code) { win.SetStatus("الرمز غلط، اكتبه مثل ما هو", false); return; }
        StartCoroutine(Delete());
    }

    IEnumerator Delete()
    {
        busy = true;
        go.interactable = false;
        win.SetStatus("نحذف حسابك...", true);

        AccountManager acc = AccountManager.Instance;
        if (acc != null && acc.IsLoggedIn)
        {
            yield return acc.EnsureFreshToken();
            // نحذف الحساب كامل (الدالة في Supabase)، ولو ما انشغّلت نحذف التقدم بس
            UnityWebRequest req = acc.NewRequest("POST", "/rest/v1/rpc/delete_my_account", "{}");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("ما قدرنا نحذف الحساب كامل (شغّلي Supabase/delete_account.sql): " + req.responseCode + " " + req.downloadHandler.text);
                // نفضّي الحفظ في السحابة (التعديل مسموح دايم)، عشان التقدم ما يرجع لو دخل بنفس الإيميل
                UnityWebRequest wipe = acc.NewRequest("PATCH", "/rest/v1/players?id=eq." + acc.UserId, "{\"save_data\":null}");
                wipe.SetRequestHeader("Prefer", "return=minimal");
                yield return wipe.SendWebRequest();
                if (wipe.result != UnityWebRequest.Result.Success)
                    Debug.LogWarning("ما قدرنا نفضّي الحفظ: " + wipe.responseCode + " " + wipe.downloadHandler.text);
                // ونحاول نحذف الصف كامل (يشتغل لو الملف انشغّل)
                UnityWebRequest del = acc.NewRequest("DELETE", "/rest/v1/players?id=eq." + acc.UserId, null);
                yield return del.SendWebRequest();
            }
        }

        if (SaveManager.Instance != null) SaveManager.Instance.WipeEverything();
        if (acc != null) acc.LogoutNow();
        if (SaveManager.Instance != null) SaveManager.Instance.PrepareFreshStart();

        win.SetStatus("انحذف حسابك. تقدر تسجل بحساب جديد", true);
        yield return new WaitForSecondsRealtime(1.2f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
