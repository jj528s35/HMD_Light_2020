using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projecting_plane_pos_rot : MonoBehaviour
{
    public GameObject plane_;
    public GameObject sphere;
    public Vector3 n_vector = Vector3.up;
    public Vector3 pos = Vector3.up;
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
        pos = receive_data.targetpos;
        
        n_vector = new Vector3(eq[0], -eq[1], eq[2]);
        //y * -1
        pos = new Vector3(pos[0], -pos[1], pos[2]);
        plane_.transform.up = n_vector*100;
        plane_.transform.position = pos;
        //sphere.transform.position = n_vector*100;
    }
}
