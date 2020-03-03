using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebuggerController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UnityEngine.UI.Text txt = this.GetComponentInChildren<Canvas>().GetComponentInChildren<UnityEngine.UI.Text>();
        txt.text = $"Total Players: {playerCount}";
    }

    public static int playerCount = 0;
}
