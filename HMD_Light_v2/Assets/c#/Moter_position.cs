using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moter_position : MonoBehaviour
{
    
    public GameObject depth1, depth2, depth3;
    public float t = 10;
    private LineRenderer lineRenderer1,lineRenderer2, lineRenderer3;
    // Start is called before the first frame update
    void Start()
    {
        lineRenderer1 = depth1.GetComponent<LineRenderer>();
        lineRenderer2 = depth2.GetComponent<LineRenderer>();
        //lineRenderer3 = depth3.GetComponent<LineRenderer>();     

        lineRenderer1.startWidth = 0.002f;
        lineRenderer1.endWidth = 0.002f;
        lineRenderer1.SetPosition(0, depth1.transform.position);
        lineRenderer1.SetPosition(1, depth1.transform.position + depth1.transform.up * t);

        lineRenderer2.startWidth = 0.002f;
        lineRenderer2.endWidth = 0.002f;
        lineRenderer2.SetPosition(0, depth2.transform.position);
        lineRenderer2.SetPosition(1, depth2.transform.position + depth2.transform.up * t);

        /*lineRenderer3.startWidth = 0.002f;
        lineRenderer3.endWidth = 0.002f;
        lineRenderer3.SetPosition(0, depth3.transform.position);
        lineRenderer3.SetPosition(1, depth3.transform.position + depth3.transform.up * t);*/
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 p1 = depth1.transform.position;
        Vector3 p2 = depth2.transform.position;

        Vector3 v1 = depth1.transform.up;
        Vector3 v2 = depth1.transform.up;

        Vector3 p = new Vector3(0,0,0);

        bool ret = LineLineIntersection(out p,p1,v1,p2,v2);
        print(ret+" "+p);

        lineRenderer1.SetPosition(1, depth1.transform.position + depth1.transform.up * t);
        lineRenderer2.SetPosition(1, depth2.transform.position + depth2.transform.up * t);
    }

    //Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
	//Note that in 3d, two lines do not intersect most of the time. So if the two lines are not in the 
	//same plane, use ClosestPointsOnTwoLines() instead.
	public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2){
 
		Vector3 lineVec3 = linePoint2 - linePoint1;
		Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
		Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);
 
		float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);
 
		//is coplanar, and not parrallel
		if(Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f)
		{
			float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
			intersection = linePoint1 + (lineVec1 * s);
			return true;
		}
		else
		{
			intersection = Vector3.zero;
			return false;
		}
	}
}
