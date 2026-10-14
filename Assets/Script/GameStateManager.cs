using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [SerializeField] private int currentIndex = 0;

    //改成可读可写
    public int CurrentIndex
    {
        get { return currentIndex; }
        set { currentIndex = Mathf.Max(0, value); }  // 防止负数
    }

    public void add(int index)
    {
        CurrentIndex += index;   // 走 setter，自动校验
    }

    public void Reload(int index)
    {
        SceneManager.LoadScene(1);
        CurrentIndex = index - 1;
    }
}