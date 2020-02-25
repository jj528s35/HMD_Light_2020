using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Project_Type : MonoBehaviour
{
    private socket_receive receive_data;
    public GameObject project_on_body;
    public GameObject project_on_floor;

    // Start is called before the first frame update
    void Start()
    {
        receive_data = GameObject.Find("Receive data").GetComponent<socket_receive>();
    }

    // Update is called once per frame
    void Update()
    {
        if(receive_data.Type == 0)//project_on_body
        {
            project_on_floor.SetActive(false);
            project_on_body.SetActive(true);
        }

        if(receive_data.Type == 1)//project_on_body
        {
            project_on_floor.SetActive(true);
            project_on_body.SetActive(false);
        }
    }
}
