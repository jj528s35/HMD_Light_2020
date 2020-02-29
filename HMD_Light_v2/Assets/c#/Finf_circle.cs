using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Finf_circle : MonoBehaviour
{
    public GameObject d1,d2,d3;
    public GameObject visual,plane;
    private Vector3 A,B,C;
    private LineRenderer lineRenderer1,lineRenderer2, lineRenderer3;
    // Start is called before the first frame update
    void Start()
    {
        A = d1.transform.position;
        B = d2.transform.position;
        C = d3.transform.position;

        Vector3 v_AB = A - B;
        Vector3 v_BC = B - C;

        PlaneFrom3Points(out Vector3 n, out Vector3 planePoint, A, B, C);
        plane.transform.up = n;

        Vector3 p1 = (A + B)/2; 
        Vector3 v1 = -Vector3.Cross(v_AB,n);

        Vector3 p2 = (B + C)/2; 
        Vector3 v2 = -Vector3.Cross(v_BC,n);


        bool ret = ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, p1, v1, p2, v2);
        Vector3 p = (closestPointLine1+closestPointLine2)/2;
        visual.transform.position = p;



        lineRenderer1 = d1.GetComponent<LineRenderer>();
        lineRenderer2 = d2.GetComponent<LineRenderer>();  
        lineRenderer3 = d3.GetComponent<LineRenderer>();    

        lineRenderer1.startWidth = 0.002f;
        lineRenderer1.endWidth = 0.002f;
        lineRenderer1.SetPosition(0, p1);
        lineRenderer1.SetPosition(1, p1 + v1 * 5);

        lineRenderer2.startWidth = 0.002f;
        lineRenderer2.endWidth = 0.002f;
        lineRenderer2.SetPosition(0, p2);
        lineRenderer2.SetPosition(1, p2 + v2 * 5);

        lineRenderer3.startWidth = 0.002f;
        lineRenderer3.endWidth = 0.002f;
        
        lineRenderer3.SetPosition(0, p);
        lineRenderer3.SetPosition(1, p + n * 5);

        

    }

    // Update is called once per frame
    void Update()
    {
        A = d1.transform.position;
        B = d2.transform.position;
        C = d3.transform.position;

        Vector3 v_AB = A - B;
        Vector3 v_BC = B - C;

        PlaneFrom3Points(out Vector3 n, out Vector3 planePoint, A, B, C);
        plane.transform.up = n;

        Vector3 p1 = (A + B)/2; 
        Vector3 v1 = -Vector3.Cross(v_AB,n);

        Vector3 p2 = (B + C)/2; 
        Vector3 v2 = -Vector3.Cross(v_BC,n);


        bool ret = ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, p1, v1, p2, v2);
        Vector3 p = (closestPointLine1+closestPointLine2)/2;
        visual.transform.position = p;



        lineRenderer1 = d1.GetComponent<LineRenderer>();
        lineRenderer2 = d2.GetComponent<LineRenderer>();  
        lineRenderer3 = d3.GetComponent<LineRenderer>();    

        lineRenderer1.startWidth = 0.002f;
        lineRenderer1.endWidth = 0.002f;
        lineRenderer1.SetPosition(0, p1);
        lineRenderer1.SetPosition(1, p1 + v1 * 5);

        lineRenderer2.startWidth = 0.002f;
        lineRenderer2.endWidth = 0.002f;
        lineRenderer2.SetPosition(0, p2);
        lineRenderer2.SetPosition(1, p2 + v2 * 5);

        lineRenderer3.startWidth = 0.002f;
        lineRenderer3.endWidth = 0.002f;
        
        lineRenderer3.SetPosition(0, p);
        lineRenderer3.SetPosition(1, p + n * 5);
    }


    //Convert a plane defined by 3 points to a plane defined by a vector and a point. 
	//The plane point is the middle of the triangle defined by the 3 points.
	public static void PlaneFrom3Points(out Vector3 planeNormal, out Vector3 planePoint, Vector3 pointA, Vector3 pointB, Vector3 pointC){
 
		planeNormal = Vector3.zero;
		planePoint = Vector3.zero;
 
		//Make two vectors from the 3 input points, originating from point A
		Vector3 AB = pointB - pointA;
		Vector3 AC = pointC - pointA;
 
		//Calculate the normal
		planeNormal = Vector3.Normalize(Vector3.Cross(AB, AC));
 
		//Get the points in the middle AB and AC
		Vector3 middleAB = pointA + (AB / 2.0f);
		Vector3 middleAC = pointA + (AC / 2.0f);
 
		//Get vectors from the middle of AB and AC to the point which is not on that line.
		Vector3 middleABtoC = pointC - middleAB;
		Vector3 middleACtoB = pointB - middleAC;
 
		//Calculate the intersection between the two lines. This will be the center 
		//of the triangle defined by the 3 points.
		//We could use LineLineIntersection instead of ClosestPointsOnTwoLines but due to rounding errors 
		//this sometimes doesn't work.
		Vector3 temp;
		ClosestPointsOnTwoLines(out planePoint, out temp, middleAB, middleABtoC, middleAC, middleACtoB);
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

    //Two non-parallel lines which may or may not touch each other have a point on each line which are closest
	//to each other. This function finds those two points. If the lines are not parallel, the function 
	//outputs true, otherwise false.
	public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2){
 
		closestPointLine1 = Vector3.zero;
		closestPointLine2 = Vector3.zero;
 
		float a = Vector3.Dot(lineVec1, lineVec1);
		float b = Vector3.Dot(lineVec1, lineVec2);
		float e = Vector3.Dot(lineVec2, lineVec2);
 
		float d = a*e - b*b;
 
		//lines are not parallel
		if(d != 0.0f){
 
			Vector3 r = linePoint1 - linePoint2;
			float c = Vector3.Dot(lineVec1, r);
			float f = Vector3.Dot(lineVec2, r);
 
			float s = (b*f - c*e) / d;
			float t = (a*f - c*b) / d;
 
			closestPointLine1 = linePoint1 + lineVec1 * s;
			closestPointLine2 = linePoint2 + lineVec2 * t;
 
			return true;
		}
 
		else{
			return false;
		}
	}	
}
