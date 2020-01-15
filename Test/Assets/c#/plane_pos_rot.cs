using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class plane_pos_rot : MonoBehaviour
{ 
    public GameObject plane_;
    public Vector3 n_vector = Vector3.up;
    private socket_receive receive_data;
    // Start is called before the first frame update
    void Start()
    {
        receive_data = GameObject.Find("Receive data").GetComponent<socket_receive>();
    }

    // Update is called once per frame
    void Update()
    {
        float[] eq = receive_data.plane_equation; 
        
        n_vector = new Vector3(eq[0], eq[1], eq[2]);
        plane_.transform.forward = n_vector*100;
    }
}
