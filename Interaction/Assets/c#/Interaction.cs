using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Kalman;

public class Interaction : MonoBehaviour
{
    public Camera VRCamera,Projector;
    private TCP_server receive_data;
    private int count = 0;
    private bool last_Cube_Touch = false;
    private GameObject Cube;
    public GameObject sphere;
    public GameObject depth_child;
    private int disable_count = 0;
    //IKalmanWrapper kalman;
    // Start is called before the first frame update
    void Start()
    {
        receive_data = GameObject.Find("Control").GetComponent<TCP_server>();
    }

    void Awake ()
	{
		//kalman = new MatrixKalmanWrapper ();
	}

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            // Bit shift the index of the layer (10) to get a bit mask
            // This would cast rays only against colliders in layer 10.
            //only hit VR window
            int layerMask = 1 << 10;

             RaycastHit hit;
             Ray ray = Projector.ScreenPointToRay(Input.mousePosition);
         
             // do we hit our portal plane?
             if (Physics.Raycast(ray, out hit, 10f, layerMask)) 
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
            // Bit shift the index of the layer (10) to get a bit mask
            // This would cast rays only against colliders in layer 10.
            //only hit VR window
            int layerMask = 1 << 10;
             
             bool Cube_Touch = false;
             Vector3[] _pos = new Vector3[receive_data.touch_points_num];

             for(int i = 0 ; i < receive_data.touch_points_num; i++)
             {
                //Vector3 pos = receive_data.touch_points[i];
                //pos[1] = -pos[1];
                Vector3 pos = set_local_to_worldpos(receive_data.touch_points[i]);
                //pos = kalman.Update (pos);
                Vector3 screenPos = Projector.WorldToScreenPoint(pos);
                
                RaycastHit hit;
                Ray ray = Projector.ScreenPointToRay(screenPos);

                /*_pos[i] = receive_data.touch_points[i];
                _pos[i][1] = -_pos[i][1];

                _pos[i] = kalman.Update (_pos[i]);*/
                sphere.transform.position = pos;

                // do we hit our portal plane?
                if (Physics.Raycast(ray, out hit, 10f, layerMask)) 
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
                        if (portalHit.collider.name =="Cube")
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

            disable_count = 0;

        }
        else
        {
            if (last_Cube_Touch == true && disable_count > 5)
            {
                last_Cube_Touch = false;
                Cube.GetComponent<Renderer> ().material.color =  Color.yellow;
                print("2");
            }
            disable_count = disable_count + 1;
        }
        
    }

    public Vector3 set_local_to_worldpos(Vector3 pos)
    {
        // y * -1
        pos = new Vector3(pos[0], -pos[1], pos[2]);
        depth_child.transform.localPosition = pos;
        Vector3 position = depth_child.transform.position;
        return position;

        //ex: t.transform.position = set_local_to_worldpos(new Vector3(0,0,0));
    }

    public Vector3 set_world_to_localpos(Vector3 pos)
    {
        // y * -1
        depth_child.transform.position = pos;
        Vector3 position = depth_child.transform.localPosition;
        position = new Vector3(position[0], -position[1], position[2]);//?
        return position;

        //ex: t.transform.position = set_world_to_localpos(new Vector3(0,0,0));
    }
}
//https://answers.unity.com/questions/1018336/is-there-a-way-to-click-on-a-render-texture-to-sel.html
