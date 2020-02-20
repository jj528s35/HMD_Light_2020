using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class motor_control : MonoBehaviour
{
    private socket_receive receive_data;
    public Camera cam;
    void Start()
    {
        receive_data = GameObject.Find("Receive data").GetComponent<socket_receive>();
    }

    // Update is called once per frame
    void Update()
    {
        /*project on body*/
        if (receive_data.Type == 0)
        {
            int num = receive_data.target_plane_points_num;
            Vector3[] plane_points = receive_data.target_plane;
            int idx = (int)Mathf.Round(num/2 + 0.5f);
            Vector3 pos = plane_points[idx];

            Vector3 screenPos = cam.WorldToScreenPoint(pos);
            // Debug.Log("target is " + screenPos.x + " pixels from the left " + screenPos.y);

            if(screenPos.x > 1600)
            {
                 Debug.Log("Rotate motor left");
            }
            else if(screenPos.x < 300)
            {
                 Debug.Log("Rotate motor right");
            }

        }
        else if (receive_data.Type == 1)/*project on floor*/
        {
            Vector3 pos = receive_data.targetpos;
            //y * -1
            pos = new Vector3(pos[0], -pos[1], pos[2]);

            Vector3 screenPos = cam.WorldToScreenPoint(pos);
            // Debug.Log("target is " + screenPos.x + " pixels from the left " + screenPos.y);
            if(screenPos.x > 1600)
            {
                 Debug.Log("Rotate motor left");
            }
            else if(screenPos.x < 300)
            {
                 Debug.Log("Rotate motor right");
            }
        }
        
    }
}
