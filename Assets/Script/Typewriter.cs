using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Typewriter
{
    private readonly MonoBehaviour runner;
    private readonly Text target;

    private Coroutine routine;
    private string fullText = "";

    public bool IsTyping { get; private set; }
    public string FullText => fullText;

    public Typewriter(MonoBehaviour runner, Text target)
    {
        this.runner = runner;
        this.target = target;
    }

    /// <summary>开始逐字显示新文本，会中断上一次播放。</summary>
    public void Play(string text)
    {
        Stop();
        fullText = text ?? "";
        IsTyping = true;
        routine = runner.StartCoroutine(Run());
    }

    /// <summary>立刻完成当前打字（点击跳过用）。</summary>
    public void Complete()
    {
        if (!IsTyping) return;
        StopRoutine();
        target.text = fullText;
        IsTyping = false;
    }

    /// <summary>停止打字但不补全。</summary>
    public void Stop()
    {
        StopRoutine();
        IsTyping = false;
    }

    private void StopRoutine()
    {
        if (routine != null)
        {
            runner.StopCoroutine(routine);
            routine = null;
        }
    }

    private IEnumerator Run()
    {
        float i = 0f;
        while (i < fullText.Length)
        {
            target.text = fullText.Substring(0, (int)(i + 1));
            i += Constants.typeSpeed * Time.deltaTime;
            yield return null;
        }
        target.text = fullText;
        IsTyping = false;
        routine = null;
    }
}