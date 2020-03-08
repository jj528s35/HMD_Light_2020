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

    [Header("socket receive interaction")]
    public Vector3[] touch_points = new Vector3[2];
    public int touch_points_num = 1; 

    [Header("socket send")]
    public int fps = 45;
    public Vector3[] window_corner = new Vector3[4];


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

}
