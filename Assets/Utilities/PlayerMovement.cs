using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody player;
    public Vector3 offset;
    public Vector3 ground;

    float North { get { return (player.position.x + 50) / 100; } }
    float South { get { return 1 - North; } }
    float East { get { return (player.position.z + 50) / 100; } }
    float West { get { return 1 - East; } }
 
    bool DirectionNorth { get; set; } = true;
    bool DirectionEast { get; set; } = true;

    // Start is called before the first frame update
    private void Start()
    {
        offset.x = 400f;
        offset.z = 100f;

        UpdatePositionalForce();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Wall")
        {
            UpdatePositionalForce();
        }
    }

    private void UpdatePositionalForce()
    {
        Debug.Log($"n:{North};e:{East};s:{South};w:{West};");

        float xSpeed = Random.Range(400f, 2000f);
        float zSpeed = Random.Range(400f, 2000f);
        float flip = 0.1f;

        if (DirectionNorth)
        {
            offset.x = player.position.x - xSpeed;
            if (North < flip)
                DirectionNorth = false;
        }
        else
        {
            offset.x = player.position.x + xSpeed;
            if (South < flip)
                DirectionNorth = true;
        }

        if (DirectionEast)
        {
            offset.z = player.position.z - zSpeed;
            if (East < flip)
                DirectionEast = false;
        }
        else
        {
            offset.z = player.position.z + zSpeed;
            if (West < flip)
                DirectionEast = true;
        }

        player.AddForce(offset);
    }
}
