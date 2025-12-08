using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ButtonController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.tabKey.wasPressedThisFrame)
        {
            SwithToTitle();
        }
    }

    public void SwithToTitle()
    {
        SceneManager.LoadScene("Title");
    }
    public void SwithToMain()
    {
        SceneManager.LoadScene("Main");
    }
    public void SwithToSetting()
    {
        SceneManager.LoadScene("Setting");
    }

    public void QuitApplication()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

}
