using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChangeScene : MonoBehaviour
{
    public string sceneName;
    
    private Button _button;

    // Start is called before the first frame update
    void Start()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(TaskOnClick);
    }

    void TaskOnClick()
    {
        if (_button.name.Equals("RunAsHostButton"))
        {
            Debug.Log("GOING TO HOST SCENE");
            HostAndClientInput.isClient = false;
            Debug.Log(HostAndClientInput.isClient);
        }

        if (_button.name.Equals("ConnectToHostButton"))
        {
            Debug.Log("GOING TO CLIENT SCENE");
            HostAndClientInput.isClient = true;
            HostAndClientInput.hostIp = GameObject.Find("IPInputField").GetComponent<InputField>().text;
            Debug.Log(HostAndClientInput.isClient + "\t" + HostAndClientInput.hostIp);
        }
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
