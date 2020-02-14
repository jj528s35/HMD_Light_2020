using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RGB_camera_set : MonoBehaviour
{
    public float fx,fy,cx,cy;
    public float resolutionX, resolutionY;

    
    // Start is called before the first frame update
    void Start()
    {
        Camera cam = gameObject.GetComponent<Camera>();

        // STEP 2 : set virtual camera's frustrum (Unity) to match physical camera's parameters
        // from OpenCV (calibration parameters Fx and Fy = focal lengths in pixels)
        // image resolution from OpenCV
        float vfov =  2.0f * Mathf.Atan(0.5f * resolutionY / fy) * Mathf.Rad2Deg; // virtual camera (pinhole type) vertical field of view
         // TODO get reference one way or another
        cam.fieldOfView = vfov;
        cam.aspect = resolutionX / resolutionY; // you could set a viewport rect with proper aspect as well... I would prefer the viewport approach

        
        Vector3 pos = transform.position;
        print(pos);
         // from OpenCV (calibration parameters Cx and Cy = optical center shifts from image center in pixels)
        Vector3 imageCenter = new Vector3(0.5f, 0.5f, pos.z); // in viewport coordinates
        Vector3 opticalCenter = new Vector3(0.5f + cx / resolutionX, 0.5f + cy / resolutionY, pos.z); // in viewport coordinates
        pos += cam.ViewportToWorldPoint(imageCenter) - cam.ViewportToWorldPoint(opticalCenter); // position is set as if physical camera's optical axis went exactly through image center
        print(pos);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
