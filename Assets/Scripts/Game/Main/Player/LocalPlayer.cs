using System.Collections.Generic;
using UnityEngine;

public class LocalPlayer : MonoBehaviour
{
    public float movementSpeed = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0);
        this.transform.position += move * this.movementSpeed * Time.deltaTime;
    }

    private void QueueCommand(PlayerCommand cmd)
    {
        if (commandQueue is null)
        {
            this.commandQueue = new Queue<PlayerCommand>();
        }

        this.commandQueue.Enqueue(cmd);
    }

    private Queue<PlayerCommand> commandQueue;
}
