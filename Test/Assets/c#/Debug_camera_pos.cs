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
        print(Vector3.Distance(cam1.transform.position,cam2.transform.position)*1000);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            print((cam1.transform.position - cam2.transform.position)*100 + "cm");
        }
    }
}
