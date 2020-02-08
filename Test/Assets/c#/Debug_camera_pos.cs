using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug_camera_pos : MonoBehaviour
{
    public GameObject cam1, cam2;
    // Start is called before the first frame update
    void Start()
    {
        print((cam1.transform.position - cam2.transform.position)*100 + "cm");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
