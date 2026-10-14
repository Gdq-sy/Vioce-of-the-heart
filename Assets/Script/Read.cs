using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Read : MonoBehaviour
{
    public TextAsset csvFile;
    public Text naem;
    public Text dialog;
    public GameObject targetObject;
    public Button save;
    public Button load;
    public Save_LoadManager saveLoadManager;
    public Image Background;
    public Image Character1;
    public Image Character2;
    public AudioSource BackgroundMusic;
    public AudioSource CharacterAudio1;
    public AudioSource CharacterAudio2;

    private int n = 0;

    private List<string> col1List = new List<string>();
    private List<string> col2List = new List<string>();
    private List<string> col3List = new List<string>();
    private List<string> col4List = new List<string>();
    private List<string> col5List = new List<string>();
    private List<string> col6List = new List<string>();
    private List<string> col7List = new List<string>();

    // 打字机状态
    private bool isTyping = false;
    private string currentText = "";

    void Start()
    {
        ReadCSV();
        save.onClick.AddListener(OnSave);
        load.onClick.AddListener(OnLoad);
        Constants.Click = true;
        if (GameStateManager.Instance.CurrentIndex >= 0 && GameStateManager.Instance.CurrentIndex < col1List.Count)
        {
            targetObject.SetActive(true);
            naem.text = col2List[GameStateManager.Instance.CurrentIndex];
            currentText = col3List[GameStateManager.Instance.CurrentIndex];
            isTyping = true;
            StartCoroutine(TypeText());
        }
    }
    void Update()
    {
        IsTypes();

    }
    //打字判断
    void IsTypes()
    {
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
        if (Input.GetMouseButtonUp(0) && Constants.Click)
        {
            //检查文本框是否被隐藏
            if (targetObject.activeSelf == false)
            {
                n++;
                if (n >= 1)
                {
                    targetObject.SetActive(true); // 显示文本框
                    n = 0;
                }
                return;
            }
            // 如果正在打字，立即完成
            if (isTyping)
            {
                StopAllCoroutines();
                dialog.text = currentText;
                isTyping = false;
                return;
            }
            Debug.Log(GameStateManager.Instance.CurrentIndex);
            // 否则前进到下一行
            GameStateManager.Instance.add(1);
            if (GameStateManager.Instance.CurrentIndex < col1List.Count)
            {
                naem.text = col2List[GameStateManager.Instance.CurrentIndex];
                currentText = col3List[GameStateManager.Instance.CurrentIndex];
                isTyping = true;
                StartCoroutine(TypeText());
            }
            else
            {
                Debug.Log("已到最后一行，点击重置");
            }
        }
    }
    // 打字机协程
    IEnumerator TypeText()
    {
        float i = 0;
        while (i < currentText.Length)
        {
            dialog.text = currentText.Substring(0, (int)(i + 1));
            i += Constants.typeSpeed * Time.deltaTime;
            yield return null;
        }
        dialog.text = currentText;
        isTyping = false;
    }
    void ReadCSV()
    {
        if (csvFile == null)
        {
            Debug.LogError("CSV file not assigned!");
            return;
        }

        string[] lines = csvFile.text.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] columns = line.Split(',');
            if (columns.Length >= 7)
            {
                col1List.Add(columns[0].Trim());
                col2List.Add(columns[1].Trim());
                col3List.Add(columns[2].Trim());
                col4List.Add(columns[3].Trim());
                col5List.Add(columns[4].Trim());
                col6List.Add(columns[5].Trim());
                col7List.Add(columns[6].Trim());
            }
        }
    }
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
        if (Constants.Click && GameStateManager.Instance.CurrentIndex >= 0 && GameStateManager.Instance.CurrentIndex < col1List.Count)
        {
            Constants.issave = true;

            string time = System.DateTime.Now.ToString("yyyy-MM-dd-HH:mm");
            string content = dialog.text;
            string StringNumber = col1List[GameStateManager.Instance.CurrentIndex];
            int number = int.Parse(StringNumber);
            StartCoroutine(CaptureAndSave(number, time, content));
        }
    }
    void OnLoad()
    {
        if (Constants.Click)
        {
            Constants.issave = false;
            saveLoadManager.saveload(0, null, null, null);
            Debug.Log("点击了加载");
        }
    }
}
