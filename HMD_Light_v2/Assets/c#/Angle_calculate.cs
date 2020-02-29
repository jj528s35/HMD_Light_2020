using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Angle_calculate : MonoBehaviour
{
    //public float x,y,z;
    //public Vector3 angle;

    public Vector3 f; // from OpenCV
    public Vector3 u; // from OpenCV
    public Vector3 angle1;
    public GameObject depth;

    public bool inv = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        /*float theta = (float)(Mathf.Sqrt(x*x + y*y + z*z)*180/Mathf.PI);
        Vector3 axis = new Vector3 (-x, y, -z);
        Quaternion rot = Quaternion.AngleAxis (theta, axis);
        angle = rot.ToEulerAngles();*/


        // notice that Y coordinates here are inverted to pass from OpenCV right-handed coordinates system to Unity left-handed one
        Quaternion rot1 = Quaternion.LookRotation(new Vector3(f.x, -f.y, f.z), new Vector3(u.x, -u.y, u.z));
        Quaternion q = new Quaternion (-rot1.x, -rot1.z,-rot1.y, rot1.w);
        depth.transform.rotation = rot1;
        angle1 = depth.transform.rotation.eulerAngles;
        if(inv){
            //depth.transform.forward = - depth.transform.forward;
            depth.transform.up = - depth.transform.up;
            depth.transform.right = - depth.transform.right;
        }
    }
}
//https://stackoverflow.com/questions/36561593/opencv-rotation-rodrigues-and-translation-vectors-for-positioning-3d-object-in