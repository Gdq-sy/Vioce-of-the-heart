using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScenePresenter : MonoBehaviour
{
    [Header("UI 引用")]
    public Image background;
    public Image character1;
    public Image character2;

    [Header("动画参数")]
    public float fadeDuration = 0.3f;

    private class Channel
    {
        public Image image;
        public Coroutine routine;
        public string current = "";
        public int requestId = 0;
    }

    private Channel bgChannel;
    private Channel char1Channel;
    private Channel char2Channel;

    // 正在运行的特效协程与最新一次特效的请求 id（供 IsAnimating 判断）
    private Coroutine effectRoutine;
    private int effectRequestId = 0;

    // 保证常驻单例，防止多次加载场景导致出现多个 ScenePresenter 报错
    public static ScenePresenter instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // 注册场景加载完成事件
        SceneManager.sceneLoaded += OnSceneLoaded;

        bgChannel = new Channel { image = background };
        char1Channel = new Channel { image = character1 };
        char2Channel = new Channel { image = character2 };

        HideCharacters();
    }

    /// <summary>
    /// 背景 / 立绘 / 特效的协程是否仍在运行（含 Addressables 加载耗时）。
    /// 自动播放用它判断“动画效果是否全部结束”。
    /// </summary>
    public bool IsAnimating =>
        IsChannelBusy(bgChannel) || IsChannelBusy(char1Channel) || IsChannelBusy(char2Channel)
        || effectRoutine != null;

    static bool IsChannelBusy(Channel ch) => ch != null && ch.routine != null;

    // 当新场景加载完成时触发
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 重新寻找当前场景中的 Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            // 注意：这里的 "Background", "Character1" 必须和 Hierarchy 里的节点名字完全一致
            Transform bgTr = canvas.transform.Find("Background");
            if (bgTr != null)
            {
                background = bgTr.GetComponent<Image>();

                Transform c1Tr = bgTr.Find("Left");
                if (c1Tr != null) character1 = c1Tr.GetComponent<Image>();

                Transform c2Tr = bgTr.Find("Right");
                if (c2Tr != null) character2 = c2Tr.GetComponent<Image>();
            }
        }


        // 关键：必须同步更新 Channel 里的 Image 引用，否则协程还是会去操作旧物体
        if (bgChannel != null) bgChannel.image = background;
        if (char1Channel != null) char1Channel.image = character1;
        if (char2Channel != null) char2Channel.image = character2;

        // 重新绑定后，隐藏立绘并清空当前状态，等待剧情指令
        HideCharacters();
        if (bgChannel != null) bgChannel.current = "";
    }

    private void HideCharacters()
    {
        if (character1 != null) character1.gameObject.SetActive(false);
        if (character2 != null) character2.gameObject.SetActive(false);
    }

    // ---------- 对外接口 ----------

    public void Apply(DialogueRow row)
    {
        // 如果引用为空（可能场景刚切还没绑定好），直接返回
        if (background == null) return;

        ApplyBackground(row.Background);
        ApplyCharacter(char1Channel, row.Character1);
        ApplyCharacter(char2Channel, row.Character2);
        PlayEffect(row.Effect);
    }

    // ---------- 背景 ----------

    void ApplyBackground(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (name == bgChannel.current) return;

        bgChannel.current = name;
        int rid = ++bgChannel.requestId;
        RestartRoutine(bgChannel, BackgroundFlow(name, rid));
    }

    IEnumerator BackgroundFlow(string key, int rid)
    {
        Sprite s = null;
        yield return AssetService.Instance.LoadSprite(key, r => s = r);

        if (rid != bgChannel.requestId) yield break;
        if (s == null) yield break;

        // 增加安全判空
        if (bgChannel.image == null) yield break;

        yield return FadeInImage(bgChannel.image, s, fadeDuration);
    }

    // ---------- 立绘 ----------

    void ApplyCharacter(Channel ch, string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        if (name == "#")
        {
            if (ch.current == "") return;
            ch.current = "";
            int ridHide = ++ch.requestId;
            RestartRoutine(ch, HideCharacterFlow(ch, ridHide));
            return;
        }

        if (name == ch.current) return;

        ch.current = name;
        int rid = ++ch.requestId;
        RestartRoutine(ch, CharacterFlow(ch, name, rid));
    }

    IEnumerator CharacterFlow(Channel ch, string key, int rid)
    {
        Sprite s = null;
        yield return AssetService.Instance.LoadSprite(key, r => s = r);

        if (rid != ch.requestId) yield break;
        if (s == null) yield break;

        // 增加安全判空
        if (ch.image == null) yield break;

        yield return FadeInImage(ch.image, s, fadeDuration);
    }

    IEnumerator HideCharacterFlow(Channel ch, int rid)
    {
        if (ch.image == null) yield break;
        yield return FadeOutImage(ch.image, fadeDuration);
    }

    // ---------- 动画 ----------

    void PlayEffect(string effect)
    {
        if (string.IsNullOrEmpty(effect)) return;

        int rid = ++effectRequestId;
        if (effect == "shake1" && character1 != null && character1.gameObject.activeSelf)
            effectRoutine = StartCoroutine(Shake(character1.rectTransform, 0.3f, 12f, rid));
        else if (effect == "shake2" && character2 != null && character2.gameObject.activeSelf)
            effectRoutine = StartCoroutine(Shake(character2.rectTransform, 0.3f, 12f, rid));
    }

    IEnumerator Shake(RectTransform rt, float duration, float magnitude, int rid)
    {
        if (rt == null) yield break;
        Vector2 origin = rt.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (rt == null) break;   // 立绘随场景重载被销毁
            float x = Mathf.Sin(t * 60f) * magnitude;
            rt.anchoredPosition = origin + new Vector2(x, 0);
            yield return null;
        }
        if (rt != null) rt.anchoredPosition = origin;
        if (rid == effectRequestId) effectRoutine = null;
    }

    // ---------- 淡入淡出 ----------

    IEnumerator FadeInImage(Image img, Sprite sprite, float duration)
    {
        if (img == null) yield break; // 极关键：防止在淡入时物体被销毁导致报错

        img.sprite = sprite;
        img.gameObject.SetActive(true);

        Color c = img.color;
        c.a = 0f;
        img.color = c;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (img == null) yield break; // 循环中也要判空
            c.a = Mathf.Clamp01(t / duration);
            img.color = c;
            yield return null;
        }
        if (img != null)
        {
            c.a = 1f;
            img.color = c;
        }
    }

    IEnumerator FadeOutImage(Image img, float duration)
    {
        if (img == null) yield break;

        Color c = img.color;
        float start = c.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (img == null) yield break; // 循环中判空
            c.a = Mathf.Lerp(start, 0f, t / duration);
            img.color = c;
            yield return null;
        }
        if (img != null) img.gameObject.SetActive(false);
    }

    // ---------- 工具 ----------

    void RestartRoutine(Channel ch, IEnumerator routine)
    {
        if (ch.routine != null) StopCoroutine(ch.routine);
        // 包装一层：协程自然结束时清空引用，IsAnimating 才能正确归零
        ch.routine = StartCoroutine(WrapRoutine(ch, routine));
    }

    IEnumerator WrapRoutine(Channel ch, IEnumerator inner)
    {
        while (inner.MoveNext()) yield return inner.Current;
        ch.routine = null;
    }
}
