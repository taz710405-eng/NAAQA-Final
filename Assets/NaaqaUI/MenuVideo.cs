using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;

// ===== فيديو خلفية القوائم (شاشة البداية وتسجيل الدخول) =====
// الفيديو في Assets/StreamingAssets/naaqa_menu.mp4 (يشتغل على الويب والجوال والكمبيوتر)
// وإلى ما يجهز يطلع لون غامق (بدون صورة ثابتة)
public class MenuVideo : MonoBehaviour
{
    public static MenuVideo Instance;

    public const string FileName = "naaqa_menu.mp4";

    private VideoPlayer player;
    private RenderTexture rt;
    private bool prepared = false;
    private readonly List<RawImage> targets = new List<RawImage>();

    static MenuVideo Get()
    {
        if (Instance != null) return Instance;
        GameObject g = new GameObject("MenuVideo");
        DontDestroyOnLoad(g);
        Instance = g.AddComponent<MenuVideo>();
        return Instance;
    }

    // يحط الفيديو في الصورة، ويشغله لو كان واقف
    public static void Attach(RawImage img)
    {
        if (img == null) return;
        MenuVideo v = Get();
        if (!v.targets.Contains(img)) v.targets.Add(img);
        // ما فيه صورة ثابتة: لون صحراوي غامق لين توصل أول لقطة من الفيديو
        img.texture = v.prepared ? (Texture)v.rt : null;
        img.color = v.prepared ? Color.white : new Color32(46, 26, 12, 255);
        v.Play();
    }

    // لما تختفي الشاشة نوقف الفيديو عشان ما ياكل من أداء اللعبة
    public static void Detach(RawImage img)
    {
        if (Instance == null) return;
        Instance.targets.Remove(img);
        Instance.targets.RemoveAll(t => t == null);
        if (Instance.targets.Count == 0 && Instance.player != null) Instance.player.Pause();
    }

    void Awake()
    {

        rt = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        rt.name = "MenuVideoRT";

        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.isLooping = true;
        player.skipOnDrop = true;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = rt;
        player.source = VideoSource.Url;
        player.url = System.IO.Path.Combine(Application.streamingAssetsPath, FileName);
        player.prepareCompleted += OnPrepared;
        player.errorReceived += (vp, msg) => Debug.LogWarning("فيديو الخلفية ما اشتغل: " + msg);
        player.Prepare();
    }

    void OnPrepared(VideoPlayer vp)
    {
        // نشغّله، ونبدّل الصورة بالفيديو أول ما توصل أول لقطة (عشان ما يطلع أسود)
        vp.sendFrameReadyEvents = true;
        vp.frameReady += OnFirstFrame;
        vp.Play();
    }

    void OnFirstFrame(VideoPlayer vp, long frame)
    {
        vp.frameReady -= OnFirstFrame;
        vp.sendFrameReadyEvents = false;
        prepared = true;

        targets.RemoveAll(t => t == null);
        foreach (RawImage img in targets) { img.texture = rt; img.color = Color.white; }
        if (targets.Count == 0) vp.Pause();
    }

    void Play()
    {
        if (player == null || !player.isPrepared) return;
        if (!player.isPlaying) player.Play();
    }
}
