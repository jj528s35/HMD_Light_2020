using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraSetup : MonoBehaviour
{
    Camera mainCamera;
    public float fx,fy,cx,cy;
    public int width, height;
 
    public float f = 35.0f; // f can be arbitrary, as long as sensor_size is resized to to make ax,ay consistient
 
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = gameObject.GetComponent<Camera>();
        changeCameraParam();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void changeCameraParam()
    {
        //string path = "Assets/Resources/Intrinsic.txt";
        float sizeX, sizeY;
        float shiftX, shiftY;
 
        /*string[] lines = File.ReadAllLines(path);
        string[] parameters = lines[1].Split(' ');
        string[] resolution = lines[3].Split(' ');
 
        ax = float.Parse(parameters[0]);
        ay = float.Parse(parameters[1]);
        x0 = float.Parse(parameters[2]);
        y0 = float.Parse(parameters[3]);
 
        width = int.Parse(resolution[0]);
        height = int.Parse(resolution[1]);*/
 
        sizeX = f * width / fx;
        sizeY = f * height / fy;
 
        //PlayerSettings.defaultScreenWidth = width;
        //PlayerSettings.defaultScreenHeight = height;
 
        shiftX = -(cx - width / 2.0f) / width;
        shiftY = (cy - height / 2.0f) / height;
 
        mainCamera.sensorSize = new Vector2(sizeX, sizeY);     // in mm, mx = 1000/x, my = 1000/y
        mainCamera.focalLength = f;                            // in mm, ax = f * mx, ay = f * my
        mainCamera.lensShift = new Vector2(shiftX, shiftY);    // W/2,H/w for (0,0), 1.0 shift in full W/H in image plane
 
    }
}

//https://forum.unity.com/threads/how-to-use-opencv-camera-calibration-to-set-physical-camera-parameters.704120/
