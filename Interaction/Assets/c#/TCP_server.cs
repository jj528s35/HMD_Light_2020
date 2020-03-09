using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TCP_server : MonoBehaviour
{
    
    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    private TcpClient connectedTcpClient;
    private motor_controller motor_control;
    private Interaction Interaction;

    [Header("socket receive interaction")]
    public Vector3[] touch_points = new Vector3[2];
    public int touch_points_num = 1; 

    [Header("socket send")]
    public int fps = 45;
    public Vector3[] window_corner = new Vector3[4];
    public GameObject plane;
    private int count = 0;
    private bool Close_CLient = false;

    //[Header("Debug")]
    //public Camera depth;


    public enum ReceiveType
    {
        None,
        feet_touch_Type
    };
    
    // Start is called before the first frame update
    void Start()
    {
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
        Debug.Log("Q : close client");

        motor_control = GameObject.Find("Control").GetComponent<motor_controller>();
        Interaction = GameObject.Find("Control").GetComponent<Interaction>();
    }

     void OnDiaable()
    {
        SendMessage("2 stop!");
        tcpListenerThread.Abort();
        tcpListenerThread.Join();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            SendMessage("0 Hi, this is server!");
        }

        if(Input.GetKeyDown(KeyCode.Q))
        {
            SendMessage("2 stop!");
            Close_CLient = true;
        }

        if(count % 6 == 0 && Close_CLient == false)
        {
            //Plane_corner();
            string window_corner_str = "3 " + window_in_depth_camera_coord();
            SendMessage(window_corner_str);
            
            //Debug
            /*string s = "";
            for(int i = 0; i < 4; i ++)
            {
                Vector3 v = depth.WorldToScreenPoint(motor_control.plane_points[i]);
                s = s + " " +v[0] + " " + (171 - v[1]);//unity screen (0,0) is buttom left
            }
            print(s);*/
        }
        count = count + 1;


    }

    private void ListenForIncommingRequests()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), 7777);
            tcpListener.Start();
            Debug.Log("Server is listening");
            Byte[] bytes = new Byte[1024];
            while(true)
            {
                using (connectedTcpClient = tcpListener.AcceptTcpClient())
                {
                    using (NetworkStream stram = connectedTcpClient.GetStream())
                    {
                        int length;
                        string clietMessage = "";
                        while ((length = stram.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            var incommingData = new byte[length];
                            Array.Copy(bytes, 0, incommingData, 0, length);
                            clietMessage = Encoding.ASCII.GetString(incommingData);
                            ParseData(clietMessage);
                            // Debug.Log("client message received as: " + clietMessage);
                        }
                    }
                }
            }
        }catch(SocketException e)
        {
            Debug.Log("Socket exception " + e.ToString());
        }catch(ThreadAbortException abortException)
        {
            Debug.Log(abortException);
        }
    }


    private void ParseData(string data)
    {
        string[] values = data.Split(' ');
        int dataType = int.Parse(values[0]);
        
        if(dataType == (int)ReceiveType.feet_touch_Type)
        {
            touch_points_num = int.Parse(values[1]);
            
            if ((values.Length-3) == touch_points_num*3)
            {
                for (int i = 0; i < touch_points_num; i++)
                {
                    float x = float.Parse(values[i*3 + 2]);
                    float y = float.Parse(values[i*3 + 3]);
                    float z = float.Parse(values[i*3 + 4]);
                    touch_points[i] = new Vector3(x, y, z);
                    
                }
            }
            else
            {
                Debug.LogFormat("reveice feet_touch_Type, data length: {0}", values.Length);
                //Debug.LogFormat("feet_touch_Type format is wrong: {0}", data);
            }
            //Debug.LogFormat("reveice feet_touch_Type, data length: {0}", values.Length);
        }
        
    }


    private void SendMessage(string msg)
    {
        if(connectedTcpClient == null)
        {
            return;
        }

        try
        {
            NetworkStream stream = connectedTcpClient.GetStream();
            if (stream.CanWrite)
            {
                byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(msg);
                stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                Debug.Log("Server sent his message - should be received by client");
            }
        }catch(SocketException e)
        {
            Debug.Log(e.ToString());
        }
    }

    public string window_in_depth_camera_coord()
    {
        Vector3[] Plane_corner = new Vector3[4];
        string str = "";
        for(int i = 0 ; i < 4; i++)
        {
            Plane_corner[i] = Interaction.set_world_to_localpos( motor_control.Projecting_area_points[i]);
            str = str + Plane_corner[i][0].ToString() + " " + Plane_corner[i][1].ToString() + " " + Plane_corner[i][2].ToString() + " ";
        }

        return str;
        
    }

    public void Plane_corner()
    {
        float y = 0;//Projecting_area_points[0][1];

        GameObject target = new GameObject("corner");
        target.transform.parent = plane.transform;

        //get position of plane corner
        target.transform.localPosition = new Vector3(-5,y,5);
        window_corner[0] = target.transform.position;

        target.transform.localPosition = new Vector3(5,y,5);
        window_corner[1] = target.transform.position;

        target.transform.localPosition = new Vector3(5,y,-5);
        window_corner[2] = target.transform.position;

        target.transform.localPosition = new Vector3(-5,y,-5);
        window_corner[3] = target.transform.position;

        Destroy(target);
    }

}
