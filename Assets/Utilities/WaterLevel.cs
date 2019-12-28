using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterLevel : MonoBehaviour
{
    public float UpperLimit = 2f;
    public float LowerLimit = -.5f;
    public float Step = .001f;
    private bool State { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        Vector3 position = transform.position;
        position.y = UpperLimit;
        transform.position = position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 position = transform.position;
        if (State)
        {
            position.y += Step * Time.deltaTime;
            if (position.y > UpperLimit)
            {
                position.y = UpperLimit;
                State = false;
            }
        }
        else
        {
            position.y -= Step * Time.deltaTime;
            if (position.y < LowerLimit)
            {
                position.y = LowerLimit;
                State = true;
            }
        }
        transform.position = position;
    }
}
