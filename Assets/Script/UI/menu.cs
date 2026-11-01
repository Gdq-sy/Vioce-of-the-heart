using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class menu : MonoBehaviour
{
    public Button start;
    public Button load;
    public GameObject Obload;
    public Button CG;
    public GameObject ObCG;
    public Button exit;
    public Button set;
    public GameObject Obset;

    private void Start()
    {
        start.onClick.AddListener(begin);
        load.onClick.AddListener(Load);
        exit.onClick.AddListener(Exit);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    private void begin()
    {
        GameStateManager.Instance.Reload(1);
        SceneManager.LoadScene(1);
    }
    private void Load()
    {
        if (Obload != null)
        {
            Obload.SetActive(true);
        }
        Constants.issave = false;
    }
    private  void Exit()
    {
        Application.Quit();
    }
}
