using Assets.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppOptions : MonoBehaviour
{
    public static float DefaultTimeScale { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        DefaultTimeScale = Time.timeScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            TogglePause();
        if (Input.GetKeyDown(KeyCode.C))
            ToggleSenseCones();
        if (Input.GetKeyDown(KeyCode.Escape))
            ClearSelected();
    }

    public void TogglePause()
    {
        AppState.Paused = !AppState.Paused;
        Time.timeScale = !AppState.Paused ? DefaultTimeScale : 0f;
    }

    public void ToggleSenseCones()
    {
        AppState.SenseConesVisible = !AppState.SenseConesVisible;

        foreach (GameObject cone in AppState.SenseCones)
            cone.SetActive(AppState.SenseConesVisible);
    }

    public void ClearSelected()
    {
        if (AppState.Selected)
            AppState.Selected = null;
    }
}
