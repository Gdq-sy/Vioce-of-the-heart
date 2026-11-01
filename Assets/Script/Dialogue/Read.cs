using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Read : MonoBehaviour
{
    [Header("UI 引用")]
    public Text naem;
    public Text dialog;
    public GameObject targetObject;
    public GameObject exit;
    [Header("退出按钮")]
    public Button menuButton; // 标题菜单按钮
    public Button exitButton; // 退出按钮

    [Header("自动和跳过")]
    public Button auto;
    public Button skip;

    [Header("存档相关")]
    public Button save;
    public Button load;
    public Save_LoadManager saveLoadManager;

    [Header("数据源")]
    public TextAsset csvFile;

    private readonly DialogueData data = new DialogueData();
    private Typewriter typewriter;

    // auto / skip 按钮点击会同帧穿透到 Update 的 HandleClick，用该标志吞掉这一次推进（沿用 Open / Exit 的模式）
    private static bool blockNextClick = false;

    // 自动播放状态
    private bool autoActive = false;
    private Coroutine autoRoutine;
    private int autoToken = 0;
    private Color autoBaseColor = Color.white;

    // 自动播放激活时的按钮反馈色
    private static readonly Color AutoOnColor = new Color(0.56f, 0.93f, 0.56f, 1f);

    private int hiddenClickBuffer = 0;

    void Start()
    {
        typewriter = new Typewriter(this, dialog);

        data.Load(csvFile);

        save.onClick.AddListener(OnSave);
        load.onClick.AddListener(OnLoad);
        auto.onClick.AddListener(OnAuto);
        skip.onClick.AddListener(OnSkip);
        menuButton.onClick.AddListener(() => SceneManager.LoadScene(0));
        exitButton.onClick.AddListener(() => Application.Quit());
        Constants.Click = true;

        int index = GameStateManager.Instance.CurrentIndex;
        if (data.HasIndex(index))
        {
            targetObject.SetActive(true);
            ShowLine(index);
        }
    }

    void Update()
    {
        HandleClick();
        menu();
    }
    // ---------- 退出 UI ----------
    void menu()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && SceneManager.GetActiveScene().name != "menu")
        {
            exit.SetActive(true);       // 退出 UI
            blockNextClick = false;
            Constants.Click = false;
        }
    }

    // ---------- 输入处理 ----------

    void HandleClick()
    {
        // 自动 / 跳过按钮点击时用来屏蔽本次“推进对话”
        if (blockNextClick)
        {
            blockNextClick = false;
            return;
        }
        // Open / Exit 按钮点击时用来屏蔽本次“推进对话”
        if (Open.BlockNextClick)
        {
            Open.BlockNextClick = false;
            Constants.Click = false;
            return;
        }
        if (Exit.BlockNextClick)
        {
            Exit.BlockNextClick = false;
            Constants.Click = true;
            return;
        }

        if (!Input.GetMouseButtonUp(0) || !Constants.Click) return;

        // 文本框被隐藏时，点击只负责恢复显示
        if (!targetObject.activeSelf)
        {
            hiddenClickBuffer++;
            if (hiddenClickBuffer >= 1)
            {
                targetObject.SetActive(true);
                hiddenClickBuffer = 0;
            }
            return;
        }

        // 正在打字：点击立刻补全
        if (typewriter.IsTyping)
        {
            typewriter.Complete();
            return;
        }

        // 否则推进到下一行
        AdvanceLine();
    }

    void AdvanceLine()
    {
        //Debug.Log(GameStateManager.Instance.CurrentIndex);

        GameStateManager.Instance.add(1);

        int index = GameStateManager.Instance.CurrentIndex;
        if (data.HasIndex(index))
        {
            ShowLine(index);
        }
        else
        {
            Debug.Log("已到最后一行，点击重置");
            GameStateManager.Instance.Reload(1);
        }
    }

    void ShowLine(int index)
    {
        DialogueRow row = data.GetRow(index);
        naem.text = row.Name;
        ScenePresenter.instance.Apply(data.GetRowWithInheritedVisuals(index));
        AudioPresenter.instance?.Apply(row);
        typewriter.Play(row.Dialog);

        // 自动播放开着时，为新的一行重启等待（token+1 使旧协程作废）
        if (autoActive) StartAutoRoutine();
    }

    // ---------- 自动播放 ----------

    void OnAuto()
    {
        // 屏蔽本次按钮点击
        blockNextClick = true;
        SetAuto(!autoActive);
    }

    /// <summary>
    /// 等待本行“全部呈现结束”（打字 + 动画 + 语音），停顿 autoDelay 秒后推进下一行。
    /// 手动点击 / 跳过 / 自动推进都会经过 ShowLine 重新启动本协程，token 保证旧协程不会二次推进。
    /// </summary>
    void StartAutoRoutine()
    {
        if (autoRoutine != null) StopCoroutine(autoRoutine);
        autoToken++;
        autoRoutine = StartCoroutine(AutoAdvance(autoToken));
    }

    IEnumerator AutoAdvance(int token)
    {
        while (true)
        {
            // 等待本行呈现结束；存读档面板打开或文本框隐藏（截图等）时挂起等待
            while (!LineFullyPresented() || !Constants.Click || !targetObject.activeSelf)
            {
                yield return null;
            }

            yield return new WaitForSeconds(Constants.autoDelay);

            if (token != autoToken) yield break;                       // 期间行已被推进/跳过：本协程作废
            if (!Constants.Click || !targetObject.activeSelf) continue; // 停顿期间面板被打开：回到等待
            if (!LineFullyPresented()) continue;                        // 停顿期间又有新效果/语音：回到等待

            int next = GameStateManager.Instance.CurrentIndex + 1;
            if (!data.HasIndex(next))
            {
                Debug.Log("自动播放到达最后一行，自动关闭");
                SetAuto(false);
                yield break;
            }

            AdvanceLine();
            yield break;   // 推进后由 ShowLine 启动新一轮协程
        }
    }

    /// <summary>本行打字、动画、语音是否全部结束。</summary>
    bool LineFullyPresented()
    {
        if (typewriter.IsTyping) return false;
        if (ScenePresenter.instance != null && ScenePresenter.instance.IsAnimating) return false;
        if (AudioPresenter.instance != null && AudioPresenter.instance.IsVoicePlaying) return false;
        return true;
    }

    void SetAuto(bool on)
    {
        autoActive = on;
        if (auto != null && auto.image != null)
        {
            auto.image.color = on ? AutoOnColor : autoBaseColor;
        }

        if (on)
        {
            StartAutoRoutine();
        }
        else if (autoRoutine != null)
        {
            StopCoroutine(autoRoutine);
            autoRoutine = null;
        }
    }

    // ---------- 跳过 ----------

    void OnSkip()
    {
        // 屏蔽本次按钮点
        blockNextClick = true;

        // 存读档面板打开时不允许跳过
        if (!Constants.Click) return;

        int next = GameStateManager.Instance.CurrentIndex + 1;
        int target = data.FindNextBackgroundIndex(next);
        if (target < 0)
        {
            Debug.Log("后面没有新的背景图，跳过无效");
            return;
        }

        Debug.Log($"跳过：{next - 1} -> {target}（下一背景图）");
        GameStateManager.Instance.CurrentIndex = target;
        // ShowLine 内部会触发新背景淡入、重开打字；自动播放开着时也会重启等待
        ShowLine(target);
    }

    // ---------- 存档 / 读档 ----------

    IEnumerator CaptureAndSave(int number, string time, string content)
    {
        targetObject.SetActive(false);
        yield return new WaitForEndOfFrame();

        Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshot.Apply();
        byte[] bytes = screenshot.EncodeToPNG();
        Destroy(screenshot);

        targetObject.SetActive(true);
        saveLoadManager.saveload(number, time, content, bytes);
    }

    void OnSave()
    {
        int index = GameStateManager.Instance.CurrentIndex;
        if (!Constants.Click || !data.HasIndex(index)) return;

        Constants.issave = true;

        string time = System.DateTime.Now.ToString("yyyy-MM-dd-HH:mm");
        string content = dialog.text;
        int number = data.GetNumberInt(index);

        StartCoroutine(CaptureAndSave(number, time, content));
    }

    void OnLoad()
    {
        if (!Constants.Click) return;
        Constants.issave = false;
        saveLoadManager.saveload(0, null, null, null);
        Debug.Log("点击了加载");
    }
}
