using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class fless_test : MonoBehaviour
{
    public bool stop = false;

    RoyaleDotNet.CameraStatus status;
    DataReceiver receiver = new DataReceiver();
    RoyaleDotNet.CameraManager camManager = new RoyaleDotNet.CameraManager();
    List<string> connectedCameras;
    RoyaleDotNet.CameraDevice cam;


    class DataReceiver : RoyaleDotNet.IDepthDataListener
        {
            public void OnNewData (RoyaleDotNet.DepthData data)
            {
                RoyaleDotNet.DepthPoint dp = data.points[ (data.height * data.width) / 2];
                Debug.Log("============================================================\n");
                Debug.Log("Received Frame: " + data.width + "x" + data.height + ", some point: { " + dp.x + ", " + dp.y + ", " + dp.z + " } confidence: " + dp.depthConfidence);
            }
        }
    
    // Start is called before the first frame update
    void Start()
    {
        connectedCameras = camManager.GetConnectedCameraList();
        cam = camManager.CreateCamera (connectedCameras[0]);
        status = cam.Initialize();
    }

    bool first = true;
    // Update is called once per frame
    void Update()
    {
        if(first){
            initfun();
            first = false;
        }

        if(stop)
        {
            stop_camera();
        }
    }

    void initfun(){
        if (connectedCameras.Count == 0)
            {
                Debug.Log ("No connected cameras found.");
                return;
            }

            
            if (RoyaleDotNet.CameraStatus.SUCCESS != status)
            {
                Debug.Log("Failed to initialize camera.");
            }

            status = cam.RegisterDepthDataListener (receiver);
            if (RoyaleDotNet.CameraStatus.SUCCESS != status)
            {
                Debug.Log("Failed to register data listener.");
            }

            Debug.Log("Starting to capture for 10 seconds.");

            status = cam.StartCapture();
            if (RoyaleDotNet.CameraStatus.SUCCESS != status)
            {
                Debug.Log("Failed to start capture.");
            }

            string CurrentUseCase;
            Debug.Log(cam.GetCurrentUseCase (out CurrentUseCase));

    }

    void stop_camera(){
        status = cam.StopCapture();
        if (RoyaleDotNet.CameraStatus.SUCCESS != status)
        {
            Debug.Log("Failed to stop capture.");
        }
    }
}
