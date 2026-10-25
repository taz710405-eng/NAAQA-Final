using UnityEngine;
using RTLTMPro;

public class MilkManager : MonoBehaviour
{
    public static MilkManager Instance;
    public RTLTextMeshPro milkText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateMilkText();
    }

    public void UpdateMilkText()
    {
        if (milkText == null)
        {
            Debug.LogError("MilkText غير مربوط في Inspector!", this);
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance غير موجود!");
            return;
        }

        int count = GameManager.Instance.milkCount;
        milkText.text = ToArabicNumbers(count);
        milkText.ForceMeshUpdate();

        Debug.Log("تم تحديث عداد الحليب إلى: " + count);
    }

    private string ToArabicNumbers(int number)
    {
        char[] chars = number.ToString().ToCharArray();

        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '0' && chars[i] <= '9')
                chars[i] = (char)('\u0660' + (chars[i] - '0'));
        }

        return new string(chars);
    }

    public void ProduceMilk(int amount)
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.AddMilk(amount);
        UpdateMilkText();
    }

    public bool CollectMilk()
    {
        if (GameManager.Instance == null) return false;

        if (GameManager.Instance.milkCount <= 0)
            return false;

        GameManager.Instance.milkCount--;
        UpdateMilkText();
        return true;
    }
}