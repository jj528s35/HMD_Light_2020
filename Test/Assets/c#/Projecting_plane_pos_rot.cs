using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projecting_plane_pos_rot : MonoBehaviour
{
    public GameObject plane_;
    public Vector3 n_vector = Vector3.up;
    public Vector3 pos = Vector3.up;
    private socket_receive receive_data;
    public float angle = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        receive_data = GameObject.Find("Receive data").GetComponent<socket_receive>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float[] eq = receive_data.plane_equation;
        pos = receive_data.targetpos;
        
        n_vector = new Vector3(eq[0], -eq[1], eq[2]);
        if(n_vector[2] < 0) n_vector = n_vector * -1;
        //y * -1
        pos = new Vector3(pos[0], -pos[1], pos[2]);

        //plane_.transform.forward = receive_data.plane_forward_vector*100;
        plane_.transform.up = n_vector*100;

        angle = Vector3.Angle(plane_.transform.right , receive_data.plane_forward_vector);
        int sign = Vector3.Cross(plane_.transform.right, receive_data.plane_forward_vector).z < 0 ? -1 : 1;
        plane_.transform.Rotate(Vector3.up, sign * -angle, Space.Self);

        //Quaternion rotation = Quaternion.LookRotation(receive_data.plane_forward_vector*100, n_vector*100);
        //plane_.transform.rotation = rotation;

        plane_.transform.position = pos;
    }

}
