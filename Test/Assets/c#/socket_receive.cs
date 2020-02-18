using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class socket_receive : MonoBehaviour
{
    
    private TcpListener tcpListener;
    private Thread tcpListenerThread;
    private TcpClient connectedTcpClient;

    [Header("socket receive")]
    public float[] plane_equation = new float[4];
    public Vector3 targetpos;
    public Vector3 plane_forward_vector;
    public Vector3[] sample_points = new Vector3[3];
    public Vector3[] plane_points = new Vector3[4];
    public int plane_points_num = 4;
    public Vector3 plane_center;

    [Header("socket receive project on body")]
    public Vector3[] target_plane = new Vector3[25];
    public int target_plane_points_num = 25;


    [Header("socket send")]
    public int fps = 45;

    /*[Header("Projecting plane")]
    public GameObject plane_;*/

    public enum ReceiveType
    {
        None,
        plane_eq_type,
        sample_points_type,
        plane_points_type,
        plane_center_type,
        targetpos_type,
        plane_forward_Type,
        target_plane_Type
    };
    
    // Start is called before the first frame update
    void Start()
    {
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
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

        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            int fps = 5;
            SendMessage("1 "+ fps);
        }

        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            SendMessage("2 stop!");
        }
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
        
        if(dataType == (int)ReceiveType.plane_eq_type)
        {
            int plane_eq_num = 4;
            if (values.Length == plane_eq_num + 1)
            {
                float a= float.Parse(values[1]);
                float b = float.Parse(values[2]);
                float c = float.Parse(values[3]);
                float d = float.Parse(values[4]);
                plane_equation = new float[] { a, b, c, d };

                //projecting_plane_rot(plane_equation);
                //Test TCP time
                //SendMessage("1 ");
            }
            else
            {
                Debug.LogFormat("reveice plane equation, data length: {0}", values.Length);
                //Debug.LogFormat("plane equation format is wrong: {0}", data);
            }
        }
        else if(dataType == (int) ReceiveType.sample_points_type)
        {
            //Debug.LogFormat("reveice touch state, data length: {0}", values.Length);
            int sample_points_num = 3;
            
            if ((values.Length-2) == sample_points_num*3)
            {
                for (int i = 0; i < sample_points_num; i++)
                {
                    float x = float.Parse(values[i*3 + 1]);
                    float y = float.Parse(values[i*3 + 2]);
                    float z = float.Parse(values[i*3 + 3]);
                    sample_points[i] = new Vector3(x, y, z);
                    
                }
            }
            else
            {
                Debug.LogFormat("reveice sample_points, data length: {0}", values.Length);
                //Debug.LogFormat("sample_points format is wrong: {0}", data);
            }
        }
        else if(dataType == (int) ReceiveType.plane_points_type)
        {
            //Debug.LogFormat("reveice touch state, data length: {0}", values.Length);
            plane_points_num = int.Parse(values[1]);
            
            if ((values.Length-3) == plane_points_num*3)
            {
                for (int i = 0; i < plane_points_num; i++)
                {
                    float x = float.Parse(values[i*3 + 2]);
                    float y = float.Parse(values[i*3 + 3]);
                    float z = float.Parse(values[i*3 + 4]);
                    plane_points[i] = new Vector3(x, y, z);
                    
                }
            }
            else
            {
                Debug.LogFormat("reveice plane_points, data length: {0}", values.Length);
                //Debug.LogFormat("plane_points format is wrong: {0}", data);
            }
        }
        else if(dataType == (int)ReceiveType.plane_center_type)
        {
            int center_num = 3;
            if (values.Length == center_num + 1)
            {
                float a= float.Parse(values[1]);
                float b = float.Parse(values[2]);
                float c = float.Parse(values[3]);
                //plane_center = new Vector3(a, b, c);
            }
            else
            {
                Debug.LogFormat("reveice plane_center, data length: {0}", values.Length);
                //Debug.LogFormat("plane_center format is wrong: {0}", data);
            }
        }
        else if(dataType == (int)ReceiveType.targetpos_type)
        {
            int center_num = 3;
            if (values.Length == center_num + 1)
            {
                float a= float.Parse(values[1]);
                float b = float.Parse(values[2]);
                float c = float.Parse(values[3]);
                targetpos = new Vector3(a, b, c);

                //projecting_plane_pos(targetpos);
            }
            else
            {
                Debug.LogFormat("reveice target_pos, data length: {0}", values.Length);
                //Debug.LogFormat("plane_center format is wrong: {0}", data);
            }
        }
        else if(dataType == (int)ReceiveType.plane_forward_Type)
        {
            int center_num = 3;
            if (values.Length == center_num + 1)
            {
                float a= float.Parse(values[1]);
                float b = float.Parse(values[2]);
                float c = float.Parse(values[3]);
                plane_forward_vector = new Vector3(a, b, c);
                
            }
            else
            {
                Debug.LogFormat("reveice plane_forward_vector, data length: {0}", values.Length);
                //Debug.LogFormat("plane_center format is wrong: {0}", data);
            }
        }
        else if(dataType == (int) ReceiveType.target_plane_Type)
        {
            // Debug.LogFormat("reveice touch state, data length: {0}", values.Length);
            target_plane_points_num = int.Parse(values[1]);
            
            if ((values.Length-3) == target_plane_points_num*3)
            {
                for (int i = 0; i < target_plane_points_num; i++)
                {
                    float x = float.Parse(values[i*3 + 2]);
                    float y = float.Parse(values[i*3 + 3]);
                    float z = float.Parse(values[i*3 + 4]);
                    target_plane[i] = new Vector3(x, y, z);
                    
                }
            }
            else
            {
                Debug.LogFormat("target_plane_points, data length: {0}", values.Length);
                //Debug.LogFormat("plane_points format is wrong: {0}", data);
            }
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

    /*Assign plane pos and rot
    private void projecting_plane_pos(Vector3 _targetpos)
    {
        Vector3 pos = _targetpos;
        //y * -1
        pos = new Vector3(pos[0], -pos[1], pos[2]);
        plane_.transform.position = pos;
    }

    private void projecting_plane_rot(float[] _eq)
    {
        Vector3 n_vector = new Vector3(_eq[0], -_eq[1], _eq[2]);
        if(n_vector[2] < 0) n_vector = n_vector * -1;
        
        //y * -1
        plane_.transform.up = n_vector*100;
    }*/

}
