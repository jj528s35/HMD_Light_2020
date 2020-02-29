using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraSetup : MonoBehaviour
{
    Camera mainCamera;
    public float fx,fy,cx,cy;
    public int width, height;
 
    public float f = 35.0f; // f can be arbitrary, as long as sensor_size is resized to to make ax,ay consistient
    public float _f = 15.53f;
 
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
        shiftY = -(cy - height / 2.0f) / height;
 
        mainCamera.sensorSize = new Vector2(sizeX, sizeY);     // in mm, mx = 1000/x, my = 1000/y
        mainCamera.focalLength = f;                            // in mm, ax = f * mx, ay = f * my
        mainCamera.lensShift = new Vector2(shiftX, shiftY);    // W/2,H/w for (0,0), 1.0 shift in full W/H in image plane

        mainCamera.focalLength = _f; 

        mainCamera.lensShift = new Vector2(-0.005f, 0.1f);     
 
    }

    
   

/*
    private Matrix4x4 LoadProjectionMatrix(float fx, float fy, float cx, float cy)
    {
        // https://github.com/kylemcdonald/ofxCv/blob/88620c51198fc3992fdfb5c0404c37da5855e1e1/libs/ofxCv/src/Calibration.cpp
        float w = mainCamera.pixelWidth;
        float h = mainCamera.pixelHeight;
        float nearDist = mainCamera.nearClipPlane;
        float farDist = mainCamera.farClipPlane;

        return MakeFrustumMatrix(
            nearDist * (-cx) / fx, nearDist * (w - cx) / fx,
            nearDist * (cy) / fy, nearDist * (cy - h) / fy,
            nearDist, farDist);
    }
    private Matrix4x4 MakeFrustumMatrix(float left, float right,
                                        float bottom, float top,
                                        float zNear, float zFar)
    {
        // https://github.com/openframeworks/openFrameworks/blob/master/libs/openFrameworks/math/ofMatrix4x4.cpp
        // note transpose of ofMatrix4x4 wr.t OpenGL documentation, since the OSG use post multiplication rather than pre.
        // NB this has been transposed here from the original openframeworks code

        float A = (right + left) / (right - left);
        float B = (top + bottom) / (top - bottom);
        float C = -(zFar + zNear) / (zFar - zNear);
        float D = -2.0f * zFar * zNear / (zFar - zNear);

        var persp = new Matrix4x4();
        persp[0, 0] = 2.0f * zNear / (right - left);
        persp[1, 1] = 2.0f * zNear / (top - bottom);
        persp[2, 0] = A;
        persp[2, 1] = B;
        persp[2, 2] = C;
        persp[2, 3] = -1.0f;
        persp[3, 2] = D;

        var rhsToLhs = new Matrix4x4();
        rhsToLhs[0, 0] = 1.0f;
        rhsToLhs[1, 1] = -1.0f; // Flip Y (RHS -> LHS)
        rhsToLhs[2, 2] = 1.0f;
        rhsToLhs[3, 3] = 1.0f;

        return rhsToLhs * persp.transpose; // see comment above
    }

    private void CreateIntrinsicGuess()
    {
        double height = (double)1080;
        double width = (double) 1920;

        // from https://docs.google.com/spreadsheet/ccc?key=0AuC4NW61c3-cdDFhb1JxWUFIVWpEdXhabFNjdDJLZXc#gid=0
        // taken from http://www.neilmendoza.com/projector-field-view-calculator/
        float hfov = 172.4510499f;
        float vfov = 166.6213173f;

        double _fx = (double)((float)width / (2.0f * Mathf.Tan(0.5f * hfov * Mathf.Deg2Rad)));
        double _fy = (double)((float)height / (2.0f * Mathf.Tan(0.5f * vfov * Mathf.Deg2Rad)));

        double _cy = height / 2.0;
        double _cx = width / 2.0;

        /*var intrinsics = new IntrinsicCameraParameters(); 
        intrinsics.IntrinsicMatrix[0, 0] = fx;
        intrinsics.IntrinsicMatrix[0, 2] = cx;
        intrinsics.IntrinsicMatrix[1, 1] = fy;
        intrinsics.IntrinsicMatrix[1, 2] = cy;
        intrinsics.IntrinsicMatrix[2, 2] = 1;

        return intrinsics;
        print(_fy+" "+_fx+" "+_cx+" "+_cy);
    }*/
}

//https://forum.unity.com/threads/how-to-use-opencv-camera-calibration-to-set-physical-camera-parameters.704120/
