using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Save_LoadManager : MonoBehaviour 
{
    public static bool issave;
    // 待保存的数据（临时存储，等玩家选择存档位）
    private int pendingNumber;
    private string pendingTime;
    private string pendingDialog;
    private byte[] pendingScreenshot;
    private bool hasPendingSave = false;

    public TextMeshProUGUI PanelText;
    string save = "保存游戏";
    string load = "加载游戏";
    public Button[] SaveLoadButtons;
    public TextMeshProUGUI[] times;
    public RawImage[] sceneshots;
    public Text[] dialogs;
    public Button previousButton;
    public Button nextButton;
    public Text pageText;
    public int MaxPage = 6;
    int page = 1;

    void Start()
    {
        UpdateSaveLoadButtons();
        for (int i = 0; i < SaveLoadButtons.Length; i++)
        {
            int serial = i + 1;
            SaveLoadButtons[i].onClick.AddListener(() => OnSaveLoadButtonClick(serial, page));
        }

        previousButton.onClick.AddListener(() => Page(false));
        nextButton.onClick.AddListener(() => Page(true));
    }

    public void saveload(int number, string time, string dialog, byte[] screenshot)
    {
        if (issave)
        {
            // 缓存待保存的数据，等待玩家选择存档槽位
            pendingNumber = number;
            pendingTime = time;
            pendingDialog = dialog;
            pendingScreenshot = screenshot;
            hasPendingSave = true;
            Debug.Log("已缓存存档数据，等待选择存档槽位");
        }
        // 无论保存还是加载，都更新PanelText
        PanelText.text = issave ? save : load;
    }
    //检测页码
    void Page(bool isnext)
    {
        if (page <= MaxPage && page >= 1)
        {
            if (isnext)
            {
                page++;
            }
            else
            {
                page--;
            }
        }
        if (page < 1)
        {
            page = MaxPage;
        }
        if (page > MaxPage)
        {
            page = 1;
        }
        UpdateSaveLoadButtons();
        pageText.text = "第" + page + "页";
        Debug.Log(page);
    }

    void OnSaveLoadButtonClick(int serial, int clickedPage)
    {
        int index = (clickedPage - 1) * 9 + serial;
        Debug.Log($"点击了 {index} 号按钮");

        if (issave && hasPendingSave)
        {
            Save(index, pendingTime, pendingDialog, pendingScreenshot);
            hasPendingSave = false;
        }
        else if (!issave)
        {
            Load(index);
        }
    }

    //更新所有按钮的显示
    void UpdateSaveLoadButtons()
    {
        PanelText.text = issave ? save : load;
        for (int i = 0; i < SaveLoadButtons.Length; i++)
        {
            int index = (page - 1) * SaveLoadButtons.Length + i + 1;
            string folderPath = Application.persistentDataPath + $"/data_{index}";

            if (Directory.Exists(folderPath) && File.Exists($"{folderPath}/data.json"))
            {
                string jsonData = File.ReadAllText($"{folderPath}/data.json");
                // 简单解析 JSON 字段
                string time = ExtractJsonString(jsonData, "time");
                string dialog = ExtractJsonString(jsonData, "dialog");

                if (times[i] != null) times[i].text = time;
                if (dialogs[i] != null) dialogs[i].text = dialog;

                // 加载截图
                string screenshotPath = $"{folderPath}/screenshot.png";
                if (sceneshots[i] != null && File.Exists(screenshotPath))
                {
                    byte[] bytes = File.ReadAllBytes(screenshotPath);
                    Texture2D tex = new Texture2D(2, 2);
                    //if (sceneshots[i].texture != null)
                    //{
                    //    Destroy(sceneshots[i].texture);
                    //}
                    tex.LoadImage(bytes);
                    sceneshots[i].texture = tex;
                }
            }
            else
            {
                if (times[i] != null) times[i].text = "";
                if (dialogs[i] != null) dialogs[i].text = "";
                if (sceneshots[i] != null) sceneshots[i].texture = null;
            }
        }
    }
    //解析JSON字段
    string ExtractJsonString(string json, string key)
    {
        int start = json.IndexOf($"\"{key}\"");
        if (start == -1) return "";
        int colon = json.IndexOf(":", start);
        int valueStart = json.IndexOf("\"", colon) + 1;
        int valueEnd = json.IndexOf("\"", valueStart);
        if (valueEnd == -1) return "";
        return json.Substring(valueStart, valueEnd - valueStart);
    }
    //存档逻辑
    void Save(int index, string time, string dialogContent, byte[] screenshot)
    {
        Debug.Log($"保存数据到 data_{index}");
        Debug.Log(time);
        Debug.Log(index);
        Debug.Log(dialogContent);

        string folderPath = Application.persistentDataPath + $"/data_{index}";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            Debug.Log($"创建文件夹: {folderPath}");
        }

        if (screenshot != null)
        {
            string screenshotPath = $"{folderPath}/screenshot.png";
            File.WriteAllBytes(screenshotPath, screenshot);
            Debug.Log($"截图已保存: {screenshotPath}");
        }

        string jsonData = $"{{\n  \"number\": {pendingNumber},\n  \"time\": \"{time}\",\n  \"dialog\": \"{EscapeJson(dialogContent)}\"\n}}";
        string dataPath = $"{folderPath}/data.json";
        File.WriteAllText(dataPath, jsonData);
        UpdateSaveLoadButtons();
        Debug.Log($"游戏数据已保存: {dataPath}");
    }
    //对话解析
    string EscapeJson(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
    //加载逻辑
    void Load(int index)
    {
        Debug.Log($"加载 data_{index}");
        UpdateSaveLoadButtons();
        string folderPath = Application.persistentDataPath + $"/data_{index}";
        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"存档文件夹不存在: {folderPath}");
            return;
        }

        string dataPath = $"{folderPath}/data.json";
        if (File.Exists(dataPath))
        {
            string jsonData = File.ReadAllText(dataPath);
            Debug.Log($"读取存档数据: {jsonData}");
        }
    }
}
