using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interaction : MonoBehaviour
{
    public Camera VRCamera,Projector;
    private socket_receive receive_data;
    // Start is called before the first frame update
    void Start()
    {
        receive_data = GameObject.Find("Receive data").GetComponent<socket_receive>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
             RaycastHit hit;
             Ray ray = Projector.ScreenPointToRay(Input.mousePosition);
         
             // do we hit our portal plane?
             if (Physics.Raycast(ray, out hit)) 
             {
                 //Debug.Log(hit.collider.gameObject);
                 
                 var localPoint = hit.textureCoord;
                 // convert the hit texture coordinates into camera coordinates
                 Ray portalRay = VRCamera.ScreenPointToRay(new Vector2(localPoint.x * VRCamera.pixelWidth, localPoint.y * VRCamera.pixelHeight));
                 RaycastHit portalHit;
                 // test these camera coordinates in another raycast test
                 if(Physics.Raycast(portalRay, out portalHit))
                 {
                     Debug.Log(portalHit.collider.gameObject);
                 }
             }
         }
    }
}
