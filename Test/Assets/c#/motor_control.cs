using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class motor_control : MonoBehaviour
{
    private socket_receive receive_data;
    private ServoController servo_control;
    public Camera cam;
    public float rx = 0.25f;
    public float ry = 0.1f;
    public int yaw = 85;
    public int pitch = 1000;

    void Start()
    {
        receive_data = GameObject.Find("Receive data").GetComponent<socket_receive>();
        servo_control = GetComponent<ServoController>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
            servo_control.motor_init(yaw, pitch);
        }

        float leftmost = cam.pixelWidth*rx;
        float rightmost = cam.pixelWidth*(1-rx);
        float bottom = cam.pixelWidth*ry;
        float top = cam.pixelWidth*(1-ry);

        /*project on body*/
        if (receive_data.Type == 0)
        {
            int num = receive_data.target_plane_points_num;
            Vector3[] plane_points = receive_data.target_plane;
            int idx = (int)Mathf.Round(num/2 + 0.5f);
            Vector3 pos = plane_points[idx];
            pos[1] = -pos[1];

            Vector3 screenPos = cam.WorldToScreenPoint(pos);
            // Debug.Log("target is " + screenPos.x + " pixels from the left " + screenPos.y);

            if(screenPos.x > rightmost)
            {
                 Debug.Log("Rotate motor left");
                 servo_control.motor_left();
            }
            else if(screenPos.x < leftmost)
            {
                 Debug.Log("Rotate motor right");
                 servo_control.motor_right();
            }
            else if(screenPos.y > top)
            {
                 Debug.Log("Rotate motor down");
                 servo_control.motor_down();
            }
            else if(screenPos.y < bottom)
            {
                 Debug.Log("Rotate motor up");
                 servo_control.motor_up();
            }

        }
        else if (receive_data.Type == 1)/*project on floor*/
        {
            Vector3 pos = receive_data.targetpos;
            //y * -1
            pos = new Vector3(pos[0], -pos[1], pos[2]);

            Vector3 screenPos = cam.WorldToScreenPoint(pos);
            // Debug.Log("target is " + screenPos.x + " pixels from the left " + screenPos.y);
            //print(cam.pixelWidth+" "+cam.pixelHeight);
            if(screenPos.x > rightmost)
            {
                 Debug.Log("Rotate motor left");
                 servo_control.motor_left();
            }
            else if(screenPos.x < leftmost)
            {
                 Debug.Log("Rotate motor right");
                 servo_control.motor_right();
            }
            else if(screenPos.y > top)
            {
                 Debug.Log("Rotate motor down");
                 //servo_control.motor_down();
            }
            else if(screenPos.y < bottom)
            {
                 Debug.Log("Rotate motor up");
                 //servo_control.motor_up();
            }
        }
        
    }
}
