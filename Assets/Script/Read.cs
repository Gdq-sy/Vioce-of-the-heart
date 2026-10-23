using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Read : MonoBehaviour
{
    [Header("UI 引用")]
    public Text naem;
    public Text dialog;
    public GameObject targetObject;

    [Header("存档相关")]
    public Button save;
    public Button load;
    public Save_LoadManager saveLoadManager;

    [Header("数据源")]
    public TextAsset csvFile;

    // 以下字段当前逻辑未使用，保留以免场景引用丢失
    [Header("备用资源引用")]
    public Image Background;
    public Image Character1;
    public Image Character2;
    public AudioSource BackgroundMusic;
    public AudioSource CharacterAudio1;
    public AudioSource CharacterAudio2;

    private readonly DialogueData data = new DialogueData();
    private Typewriter typewriter;

    // 旧代码中的 n：文本框被隐藏时的点击缓冲
    private int hiddenClickBuffer = 0;

    void Start()
    {
        typewriter = new Typewriter(this, dialog);

        data.Load(csvFile);

        save.onClick.AddListener(OnSave);
        load.onClick.AddListener(OnLoad);
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
    }

    // ---------- 输入处理 ----------

    void HandleClick()
    {
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
        Debug.Log(GameStateManager.Instance.CurrentIndex);

        GameStateManager.Instance.add(1);

        int index = GameStateManager.Instance.CurrentIndex;
        if (data.HasIndex(index))
        {
            ShowLine(index);
        }
        else
        {
            Debug.Log("已到最后一行，点击重置");
        }
    }

    void ShowLine(int index)
    {
        DialogueRow row = data.GetRow(index);
        naem.text = row.Name;
        //ScenePresenter.instance.Apply(row);
        // 改成一行
        ScenePresenter.instance.Apply(data.GetRowWithInheritedVisuals(index));
        typewriter.Play(row.Dialog);
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