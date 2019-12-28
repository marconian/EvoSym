using Assets.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppOptions : MonoBehaviour
{
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
        if (Input.GetKeyDown(KeyCode.Escape))
            ClearSelected();
    }

    public void TogglePause()
    {
        Paused = !Paused;
        Time.timeScale = !Paused ? DefaultTimeScale : 0f;
    }

    public void ClearSelected()
    {
        if (AppState.Selected)
            AppState.Selected = null;
    }

    public bool Paused { get; private set; }
    public float DefaultTimeScale { get; private set; }
}
