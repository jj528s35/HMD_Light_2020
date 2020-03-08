using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projecting_plane_pos_rot : MonoBehaviour
{
   /*project on floor*/
    public GameObject plane_, depth_child;
    public Vector3 n_vector = Vector3.up;
    public Vector3 pos = Vector3.up;
    private socket_receive receive_data;
    public float angle = 0, normal_angle = 0,forward_angle = 0;


    // Start is called before the first frame update
    void Start()
    {
        receive_data = GameObject.Find("Receive data").GetComponent<socket_receive>();
    }

  
    // Update is called once per frame
    void FixedUpdate()
    {
        //if (receive_data.Type == 1)/*project on floor*/
        {
            float[] eq = receive_data.plane_equation;
            pos = receive_data.targetpos;
            //y * -1
            pos = new Vector3(pos[0], -pos[1], pos[2]);

            float dist = Vector3.Distance(plane_.transform.position, pos);
            
            n_vector = new Vector3(eq[0], -eq[1], eq[2]);
            if(n_vector[2] > 0) n_vector = n_vector * -1;

            //forward_angle = Vector3.Angle(plane_.transform.right , receive_data.plane_forward_vector);
            normal_angle = Vector3.Angle(plane_.transform.up , n_vector);
            plane_.transform.up = n_vector*100;

            depth_child.transform.localPosition = pos;
            transform.position = depth_child.transform.position;
            
            Vector3 forward_vector = receive_data.plane_forward_vector;

            angle = Vector3.Angle(plane_.transform.right , forward_vector);
            int sign = Vector3.Cross(plane_.transform.right, forward_vector).z < 0 ? -1 : 1;
            {
                plane_.transform.Rotate(Vector3.up, sign * -angle-90, Space.Self);
            }
            forward_angle = Vector3.Angle(plane_.transform.right , forward_vector);

        }
    }

}
