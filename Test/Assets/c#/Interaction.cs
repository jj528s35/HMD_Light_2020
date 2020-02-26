using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kalman;

public class Interaction : MonoBehaviour
{
    public Camera VRCamera,Projector;
    private socket_receive receive_data;
    private int count = 0;
    private bool last_Cube_Touch = false;
    private GameObject Cube;
    public GameObject sphere;
    IKalmanWrapper kalman;
    // Start is called before the first frame update
    void Start()
    {
        receive_data = GameObject.Find("Receive data").GetComponent<socket_receive>();
    }

    void Awake ()
	{
		kalman = new MatrixKalmanWrapper ();
		//kalman = new SimpleKalmanWrapper ();
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


        if(receive_data.touch_points_num > 0)
        {
             bool Cube_Touch = false;
             Vector3[] _pos = new Vector3[receive_data.touch_points_num];

             for(int i = 0 ; i < receive_data.touch_points_num; i++)
             {
                Vector3 pos = receive_data.touch_points[i];
                pos[1] = -pos[1];
                //pos = kalman.Update (pos);
                Vector3 screenPos = Projector.WorldToScreenPoint(pos);
                
                RaycastHit hit;
                Ray ray = Projector.ScreenPointToRay(screenPos);

                /*_pos[i] = receive_data.touch_points[i];
                _pos[i][1] = -_pos[i][1];

                _pos[i] = kalman.Update (_pos[i]);*/
                sphere.transform.position = pos;

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
                        if (portalHit.collider.name =="CUbe_1")
                        {
                            if (last_Cube_Touch == false)
                            {
                                portalHit.collider.gameObject.GetComponent<Renderer> ().material.color =  Color.red;
                                last_Cube_Touch = true;
                                Cube = portalHit.collider.gameObject;
                            }
                            Cube_Touch = true;      
                        }

                    }
                }
                /*else if (Cube_Touch == true)
                {
                    Cube_Touch = false;
                    Cube.GetComponent<Renderer> ().material.color =  Color.yellow;
                    print("2");
                }*/
                
             }

             receive_data.touch_points_num = 0;

            if (last_Cube_Touch == true && Cube_Touch == false)
            {
                last_Cube_Touch = false;
                Cube.GetComponent<Renderer> ().material.color =  Color.yellow;
                print("1");
            }

        }
        
    }
}
