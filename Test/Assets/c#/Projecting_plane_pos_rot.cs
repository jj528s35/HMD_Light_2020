using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kalman;

public class Projecting_plane_pos_rot : MonoBehaviour
{
   /*project on floor*/
    public GameObject plane_;
    public Vector3 n_vector = Vector3.up;
    public Vector3 pos = Vector3.up;
    private socket_receive receive_data;
    public float angle = 0, normal_angle = 0;
    IKalmanWrapper kalman;
    private bool stable = false;

    // Start is called before the first frame update
    void Start()
    {
        receive_data = GameObject.Find("Receive data").GetComponent<socket_receive>();
    }

    void Awake ()
	{
		kalman = new MatrixKalmanWrapper ();
		//kalman = new SimpleKalmanWrapper ();
	}

    // Update is called once per frame
    void FixedUpdate()
    {
        float[] eq = receive_data.plane_equation;
        pos = receive_data.targetpos;
        //y * -1
        pos = new Vector3(pos[0], -pos[1], pos[2]);
        
        n_vector = new Vector3(eq[0], -eq[1], eq[2]);
        if(n_vector[2] > 0) n_vector = n_vector * -1;

        normal_angle = Vector3.Angle(plane_.transform.up , n_vector);
        plane_.transform.up = n_vector*100;

        angle = Vector3.Angle(plane_.transform.right , -1*receive_data.plane_forward_vector);
        int sign = Vector3.Cross(plane_.transform.right, receive_data.plane_forward_vector).z < 0 ? -1 : 1;
        //if (Mathf.Abs(angle) > 20)
        plane_.transform.Rotate(Vector3.up, sign * -angle, Space.Self);
        //plane_.transform.Rotate(Vector3.forward, 180f, Space.Self);

        //plane_.transform.position = pos;
        float dist = Vector3.Distance(plane_.transform.position, pos);
        //print(dist);
        if(dist < 0.05f)
            stable = true;
            if(stable == true)
                transform.position = kalman.Update (pos);
        else if(stable == false)
            transform.position = pos;

        
        //if(dist < 1.5f)
        //transform.position = kalman.Update (pos);

    }

}
