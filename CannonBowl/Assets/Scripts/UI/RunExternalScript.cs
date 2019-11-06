using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RunExternalScript : MonoBehaviour
{
    private Button button;

    public string scriptName;

    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(TaskOnClick);
    }

    void TaskOnClick()
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("gnome-terminal");
        startInfo.Arguments = "-e ./Assets/External_Programs/" + scriptName;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = false;

        Process process = new Process();
        process.StartInfo = startInfo;
        process.Start();
    }
}
