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
        if (Instance != null&& Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    [SerializeField] private int currentIndex = 0;


    public int CurrentIndex => currentIndex;
    
    public void add(int index)
    {
        currentIndex += index;
    }
    public void relode(int index)
    {
        SceneManager.LoadScene(1);
        currentIndex = index - 1;
    }
}
