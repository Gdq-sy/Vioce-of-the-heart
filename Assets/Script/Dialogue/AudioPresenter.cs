using System.Collections;
using UnityEngine;

/// <summary>
/// 听觉呈现器：与 ScenePresenter 对等的音频通道，挂在 GameManager 上。
/// 由剧本行的 背景音乐 / 位置 / 音频 列驱动，列为空则什么都不做。
/// BGM 循环播放、不阻塞推进；语音播完前 IsVoicePlaying == true，供自动播放等待。
/// </summary>
public class AudioPresenter : MonoBehaviour
{
    public static AudioPresenter instance;

    [Header("音频源引用（BGM 循环，语音一次性）")]
    public AudioSource bgmSource;
    public AudioSource voiceSource;

    private string currentBgm = "";
    private Coroutine bgmRoutine;
    private int voiceRequestId = 0;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>本行语音是否仍在播（自动播放等待条件之一）。</summary>
    public bool IsVoicePlaying => voiceSource != null && voiceSource.isPlaying;

    /// <summary>每行调用：按列驱动；空列静默跳过。</summary>
    public void Apply(DialogueRow row)
    {
        if (!string.IsNullOrEmpty(row.Bgm)) PlayBgm(row.Bgm);

        if (!string.IsNullOrEmpty(row.Audio)) PlayVoice(row.Audio);
        else StopVoice();
    }

    // ---------- BGM（循环，永不阻塞推进） ----------

    public void PlayBgm(string key)
    {
        if (key == currentBgm) return;   // 同名不重播
        currentBgm = key;

        if (bgmRoutine != null)
        {
            StopCoroutine(bgmRoutine);
            bgmRoutine = null;
        }
        bgmRoutine = StartCoroutine(BgmFlow(key));
    }

    IEnumerator BgmFlow(string key)
    {
        AudioClip clip = null;
        yield return AssetService.Instance.LoadClip(key, r => clip = r);
        if (clip == null || bgmSource == null) yield break;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // ---------- 语音（参与自动播放等待） ----------

    public void PlayVoice(string key)
    {
        if (voiceSource == null) return;
        int rid = ++voiceRequestId;
        StartCoroutine(VoiceFlow(key, rid));
    }

    IEnumerator VoiceFlow(string key, int rid)
    {
        AudioClip clip = null;
        yield return AssetService.Instance.LoadClip(key, r => clip = r);

        if (rid != voiceRequestId) yield break;   // 期间行已推进，丢弃过期语音
        if (clip == null || voiceSource == null) yield break;

        voiceSource.clip = clip;
        voiceSource.Play();
    }

    public void StopVoice()
    {
        voiceRequestId++;   // 作废还在加载中的旧语音
        if (voiceSource == null) return;
        voiceSource.Stop();
        voiceSource.clip = null;
    }
}
