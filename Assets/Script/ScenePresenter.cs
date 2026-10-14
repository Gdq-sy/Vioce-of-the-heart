using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 表现层：负责背景、立绘、动画的显示。
/// 只通过 AssetService 拿 Sprite，不直接接触 Addressables。
/// </summary>
public class ScenePresenter : MonoBehaviour
{
    [Header("UI 引用")]
    public Image background;
    public Image character1;
    public Image character2;

    [Header("动画参数")]
    public float fadeDuration = 0.3f;

    [Header("Addressables 地址后缀")]
    public string backgroundbackfix = "/Backgrounds";
    public string characterbackfix = "/Characters";

    private class Channel
    {
        public Image image;
        public Coroutine routine;
        public string current = "";  // 当前显示的资源名；"" 表示隐藏
        public int requestId = 0;    // 请求版本号，用于丢弃过期请求
    }

    private Channel bgChannel;
    private Channel char1Channel;
    private Channel char2Channel;

    void Awake()
    {
        bgChannel = new Channel { image = background };
        char1Channel = new Channel { image = character1 };
        char2Channel = new Channel { image = character2 };

        // 初始隐藏立绘，避免出现空白框
        if (character1 != null) character1.gameObject.SetActive(false);
        if (character2 != null) character2.gameObject.SetActive(false);
    }

    // ---------- 对外接口 ----------

    public void Apply(DialogueRow row)
    {
        ApplyBackground(row.Background);
        ApplyCharacter(char1Channel, row.Character1);
        ApplyCharacter(char2Channel, row.Character2);
        PlayEffect(row.Effect);
    }

    // ---------- 背景 ----------

    void ApplyBackground(string name)
    {
        if (string.IsNullOrEmpty(name)) return;    // 空 = 不变
        if (name == bgChannel.current) return;     // 同名 = 不重切

        bgChannel.current = name;
        int rid = ++bgChannel.requestId;
        RestartRoutine(bgChannel, BackgroundFlow(name , rid));//+ backgroundbackfix
    }

    IEnumerator BackgroundFlow(string key, int rid)
    {
        Sprite s = null;
        yield return AssetService.Instance.LoadSprite(key, r => s = r);

        if (rid != bgChannel.requestId) yield break;  // 已被新请求取代
        if (s == null) yield break;

        yield return FadeInImage(bgChannel.image, s, fadeDuration);
    }

    // ---------- 立绘 ----------

    void ApplyCharacter(Channel ch, string name)
    {
        if (string.IsNullOrEmpty(name)) return;   // 空 = 不变

        if (name == "#")                          // "#" = 隐藏
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
        RestartRoutine(ch, CharacterFlow(ch, name, rid));// + characterbackfix
    }

    IEnumerator CharacterFlow(Channel ch, string key, int rid)
    {
        Sprite s = null;
        yield return AssetService.Instance.LoadSprite(key, r => s = r);

        if (rid != ch.requestId) yield break;
        if (s == null) yield break;

        yield return FadeInImage(ch.image, s, fadeDuration);
    }

    IEnumerator HideCharacterFlow(Channel ch, int rid)
    {
        yield return FadeOutImage(ch.image, fadeDuration);
    }

    // ---------- 动画 ----------

    void PlayEffect(string effect)
    {
        if (string.IsNullOrEmpty(effect)) return;

        if (effect == "shake1" && character1.gameObject.activeSelf)
            StartCoroutine(Shake(character1.rectTransform, 0.3f, 12f));
        else if (effect == "shake2" && character2.gameObject.activeSelf)
            StartCoroutine(Shake(character2.rectTransform, 0.3f, 12f));
    }

    IEnumerator Shake(RectTransform rt, float duration, float magnitude)
    {
        Vector2 origin = rt.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float x = Mathf.Sin(t * 60f) * magnitude;
            rt.anchoredPosition = origin + new Vector2(x, 0);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    // ---------- 淡入淡出 ----------

    IEnumerator FadeInImage(Image img, Sprite sprite, float duration)
    {
        img.sprite = sprite;
        img.gameObject.SetActive(true);

        Color c = img.color;
        c.a = 0f;
        img.color = c;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / duration);
            img.color = c;
            yield return null;
        }
        c.a = 1f;
        img.color = c;
    }

    IEnumerator FadeOutImage(Image img, float duration)
    {
        Color c = img.color;
        float start = c.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(start, 0f, t / duration);
            img.color = c;
            yield return null;
        }
        img.gameObject.SetActive(false);
    }

    // ---------- 工具 ----------

    void RestartRoutine(Channel ch, IEnumerator routine)
    {
        if (ch.routine != null) StopCoroutine(ch.routine);
        ch.routine = StartCoroutine(routine);
    }
}