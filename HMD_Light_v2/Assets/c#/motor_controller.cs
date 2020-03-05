using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kalman;

public class motor_controller : MonoBehaviour
{
    
    [Header("Virtual Motor rotation")]
    public int inti_sevoAngle;
    public GameObject motor, m_rot;
    public Vector3 rot_vector; //旋轉軸心
    public float angle, diff_angle;
    public int default_sevoangle = 750;
    public ServoController servo;
    private float servo2angle, init_angle, angle2servo;
    
     public GameObject HMD;
     //public GameObject debug,debug1;
     IKalmanWrapper kalman;
     

    [Header("Fake Motor rotation")]
    public Camera f_Projector;
    public GameObject f_motor;
    public GameObject plane;

    [Header("OSC Motor control")]
    public GameObject window_top;
    public GameObject window_down;

    [Header("depth camera local_pos")]
    public GameObject depth_child;

    [Header("Projecting area")]
    public Camera Projector_cam;
    public GameObject Projector_area;
    private Vector3[] Projecting_area_points = new Vector3[4];
    
    [Header("Projecting range")]
    public GameObject dist_from_user, Projector_range;
    private Vector3[] Projecting_range_point = new Vector3[4];
    public int Sevo_max_angle = 1000;
    public int Sevo_min_angle = 650;
    private float max_angle, min_angle;//min and max angle the virtual motor can rotation in this HND pose

    [Header("Debug")]
    public bool projecting_state = true;



    void Awake ()
	{
		kalman = new MatrixKalmanWrapper ();
	}

    // Start is called before the first frame update
    void Start()
    {
        m_rot.transform.position = motor.transform.position + rot_vector * 0.05f;//for visual
        servo2angle = 1024f/360f;
        angle2servo = 360f/1024f;

        //init the motor angle
        servo.motor_singal(inti_sevoAngle, 100);
        init_angle = (1000 - inti_sevoAngle)/servo2angle;
        angle = init_angle;
        motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);

        f_motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);

        /*init_rot_x = Projector.transform.rotation.eulerAngles;
        init_forward = Projector.transform.forward;
        zed_rot = zed.transform.rotation.eulerAngles;*/
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            //init_setting();
        } 

        //motor_angle();
        if(projecting_state){
        //show Projecting_range
        projecting_range();

        set_motor_angle();
        }
        //show Projecting_area
        Projecting_area();
        
    }

    public Vector3 set_world_to_localpos(Vector3 pos)
    {
        // y * -1
        pos = new Vector3(pos[0], -pos[1], pos[2]);
        depth_child.transform.localPosition = pos;
        Vector3 position = depth_child.transform.position;
        return position;

        //ex: t.transform.position = set_world_to_localpos(new Vector3(0,0,0));
    }

    
    public void projecting_range()
    {
        //dist_from_user is the child of HMD, dist form HMD is 0.5m
        Vector3 HMD_floor = HMD.transform.position;
        HMD_floor[1] = 0;
        Vector3 HMD_forward = Vector3.ProjectOnPlane(HMD.transform.forward, Vector3.up);
        float dist_need = 0.5f;
        float forward_dist = Vector3.Distance(Vector3.zero, HMD_forward);
        float scale = dist_need/forward_dist;

        dist_from_user.transform.position = HMD_floor + HMD_forward * scale;

        //f_Projector only hit the floor
        int layerMask = 1 << 9 | 1 << 10;
        Vector3[] Projecting_points = new Vector3[4];
        //f_Projector resolution
        int w = f_Projector.pixelWidth-1;
        int h = f_Projector.pixelHeight - 1;

        //min angle sevo can rotate
        min_angle = (1000 - Sevo_min_angle)/servo2angle;
        max_angle = (1000 - Sevo_max_angle)/servo2angle;
        f_motor.transform.localRotation = Quaternion.AngleAxis(min_angle, rot_vector);
        Vector3 screenPos = f_Projector.WorldToScreenPoint(dist_from_user.transform.position);
        RaycastHit hit;
        RaycastHit hit1;
        Ray ray = f_Projector.ScreenPointToRay(new Vector3(0, screenPos[1], 0));
        Ray ray1 = f_Projector.ScreenPointToRay(new Vector3(w, screenPos[1], 0));

        bool hit_floor1 = Physics.Raycast(ray1, out hit1, 10f, layerMask);
        bool hit_floor = Physics.Raycast(ray, out hit, 10f, layerMask) && Physics.Raycast(ray1, out hit1, 10f, layerMask);
        bool inscreen = (screenPos[1]>=0 && screenPos[1]<=h && screenPos[0]>=0 && screenPos[0]<=w);
        //min motor angle
        while(!(hit_floor && inscreen) && min_angle > max_angle)
        {
            min_angle = min_angle - 1;
            f_motor.transform.localRotation = Quaternion.AngleAxis(min_angle, rot_vector);
            screenPos = f_Projector.WorldToScreenPoint(dist_from_user.transform.position);
            ray = f_Projector.ScreenPointToRay(new Vector3(0, screenPos[1], 0));
            ray1 = f_Projector.ScreenPointToRay(new Vector3(w, screenPos[1], 0));

            inscreen = (screenPos[1]>=0 && screenPos[1]<=h && screenPos[0]>=0 && screenPos[0]<=w);
            hit_floor = Physics.Raycast(ray, out hit, 10f, layerMask) && Physics.Raycast(ray1, out hit1, 10f, layerMask);
        }

        if(hit_floor)
        {
            Projecting_points[0] = hit.point;
            Projecting_points[3] = hit1.point;
        }
        //print("screen"+screenPos);
        
        
        
        //max angle sevo can rotate
        max_angle = (1000 - Sevo_max_angle)/servo2angle;
        f_motor.transform.localRotation = Quaternion.AngleAxis(max_angle, rot_vector);

        ray = f_Projector.ScreenPointToRay(new Vector3(w, h, 0));
        ray1 = f_Projector.ScreenPointToRay(new Vector3(0, h, 0));

        hit_floor = Physics.Raycast(ray, out hit, 10f, layerMask) && Physics.Raycast(ray1, out hit1, 10f, layerMask);
        // do we hit our portal plane?
        while(!hit_floor && max_angle < min_angle)
        {
            max_angle = max_angle + 1;
            f_motor.transform.localRotation = Quaternion.AngleAxis(max_angle, rot_vector);
            ray = f_Projector.ScreenPointToRay(new Vector3(w, h, 0));
            ray1 = f_Projector.ScreenPointToRay(new Vector3(0, h, 0));

            hit_floor = Physics.Raycast(ray, out hit, 10f, layerMask) && Physics.Raycast(ray1, out hit1, 10f, layerMask);
        }
        //print((1000 - max_angle * servo2angle)+" "+(1000 - min_angle * servo2angle));
        print(max_angle +" "+ min_angle);
        
        
        if(hit_floor)
        {
            Projecting_points[2] = hit.point;
            Projecting_points[1] = hit1.point;
        }
        
        Projecting_range_point = Projecting_points;

        // for visualzation => plane and projecting area 不重疊
        for(int i = 0; i<4; i++)
            Projecting_points[i][1] = -0.0002f;
        DoCreatPloygonMesh(Projecting_points, true);
    }


    public void set_motor_angle()
    {
        f_motor.transform.localRotation = motor.transform.localRotation;
        //if the plane is inside the projecting range
        bool PlaneInArea = planeInprojectingArea();
        //if not => rotate to default angle 
        if(!PlaneInArea)
        {
            angle = (1000 - default_sevoangle)/servo2angle;
            motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);
            f_motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);
            servo.motor_singal((int)(1000 - angle * servo2angle), 100);
            return;
        }

        float theta = 0;
        bool first = true;
        int w = Projector_cam.pixelWidth;
        int h = Projector_cam.pixelHeight;
        
        // only hit the floor
        int layerMask = 1 << 9 | 1 << 10;

        bool sameDirection = true;
        float Rot_down = 1;
        float rot_angle = 0;

        RaycastHit hit;
        Ray ray = Projector_cam.ScreenPointToRay(new Vector3(w/2, h/2, 0));
        // do we hit floor?
        if (Physics.Raycast(ray, out hit, 10f, layerMask)) 
        {
            Vector3 dir = hit.point - plane.transform.position;
            float dist = Vector3.Distance(hit.point, plane.transform.position);
            float new_dist = dist;
            float ori_dist = dist;//for debug

            Vector3 dir_1 = plane.transform.position - hit.point;
            Vector3 HMD_floor = HMD.transform.position;
            HMD_floor[1] = 0;
            Vector3 dir_2 = HMD_floor - hit.point;
            float dir_angle = Vector3.Angle(dir_2, dir_1);

            if(dir_angle < 90) Rot_down = 1; // dir_angle < 90 => rotation down
            else Rot_down = -1;


            Vector3 HMD_forward = Vector3.ProjectOnPlane(HMD.transform.forward, Vector3.up);
            Vector3 HMD2Pcenter = hit.point - HMD_floor;
            float forward_HMD2Pcenter_angle = Vector3.Angle(HMD_forward, HMD2Pcenter);
            if(dir_angle < 90 && forward_HMD2Pcenter_angle > 90)
            {
                Rot_down = Rot_down*-1;//投影中心和HMD forward 反向
                print("f "+forward_HMD2Pcenter_angle + " " +dir_angle);
            } 
            //print("f "+forward_HMD2Pcenter_angle + " " +dir_angle);
            print("dir "+ dir_angle);
            


            while(first || (new_dist <= dist))//0.01f && (dist - new_dist) >= 0.001f
            {
                first = false;
                theta = theta + 1;
                dist = new_dist;
                rot_angle = theta * Rot_down;// * angle2servo;
                
                f_motor.transform.localRotation = Quaternion.AngleAxis(angle + rot_angle, rot_vector);
                
                ray = f_Projector.ScreenPointToRay(new Vector3(w/2, h/2, 0));
                if (Physics.Raycast(ray, out hit, 10f, layerMask))
                {
                    new_dist = Vector3.Distance(hit.point, plane.transform.position);
                } 
                else break;

            }

            if(theta-1 >= 1)//0.03f
            {
                rot_angle = (theta-1) * Rot_down;// * angle2servo;
                print(angle + " " + rot_angle);
                angle = angle + rot_angle;

                //if(angle >= 0 && angle >= max_angle && angle <= min_angle)
                //if(angle >= 0)
                {//max_angle => sevo angle max (virtual projector angle min)
                    if(angle <= max_angle) angle = max_angle;
                    if(angle >= min_angle) angle = min_angle;
                    if(angle < 0) angle = 0;

                    motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);
                    f_motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);
                    int rot_speed = (int)speed(rot_angle);
                    servo.motor_singal((int)(1000 - angle * servo2angle), rot_speed);
                }
                /*else 
                {
                    angle = angle - rot_angle;
                    print("angle out of range");
                }*/

            }
            else 
            {
                f_motor.transform.localRotation = motor.transform.localRotation;
                print("theta < 1");
            }
  
        }
        else 
        {
            if (Physics.Raycast(ray, out hit))
                Debug.Log(hit.collider.gameObject);
            print("not hit floor");
            print(angle);
            return;
        }
            
    }

    /*public void motor_angle()
    {
        f_motor.transform.localRotation = motor.transform.localRotation;
        float theta = 0;
        bool first = true;
        int w = Projector_cam.pixelWidth;
        int h = Projector_cam.pixelHeight;
        
        // only hit the floor
        int layerMask = 1 << 9 | 1 << 10;

        bool sameDirection = true;
        float Rot_down = 1;
        float rot_angle = 0;

        RaycastHit hit;
        Ray ray = Projector_cam.ScreenPointToRay(new Vector3(w/2, h/2, 0));
        // do we hit floor?
        if (Physics.Raycast(ray, out hit, 10f, layerMask)) 
        {
            Vector3 dir = hit.point - plane.transform.position;
            float dist = Vector3.Distance(hit.point, plane.transform.position);
            float new_dist = dist;
            float ori_dist = dist;//for debug

            //float dot = Vector3.Dot( dir, plane.transform.forward );
            //sameDirection = dot >= 0; 

            //
            Vector3 dir_1 = plane.transform.position - hit.point;
            Vector3 HMD_floor = HMD.transform.position;
            HMD_floor[1] = 0;
            Vector3 dir_2 = HMD_floor - hit.point;
            float dir_angle = Vector3.Angle(dir_2, dir_1);


            //if(sameDirection) Rot_down = 1; // same as forward dir(blue axix) => rotation down
            if(dir_angle < 90) Rot_down = 1; // dir_angle < 90 => rotation down
            else Rot_down = -1;

            while(first || (new_dist <= dist))//0.01f && (dist - new_dist) >= 0.001f
            {
                first = false;
                theta = theta + 1;
                dist = new_dist;
                rot_angle = theta * Rot_down;// * angle2servo;
                
                f_motor.transform.localRotation = Quaternion.AngleAxis(angle + rot_angle, rot_vector);
                
                ray = f_Projector.ScreenPointToRay(new Vector3(w/2, h/2, 0));
                if (Physics.Raycast(ray, out hit, 10f, layerMask))
                {
                    new_dist = Vector3.Distance(hit.point, plane.transform.position);
                } 
                else break;

            }

            /*Vector3 HMD2Plane = plane.transform.position - HMD_floor;
            Vector3 HMD2Center = -dir_2;
            float PC_angle = Vector3.Angle(HMD2Center, HMD2Plane);
            print("plane_HMD_center "+PC_angle);/*

            Vector3 HMD2Plane = Vector3.ProjectOnPlane(plane.transform.position - HMD_floor, Vector3.up);
            Vector3 forward = Vector3.ProjectOnPlane(HMD.transform.forward, Vector3.up);
            float PF_angle = Vector3.Angle(HMD2Plane, forward);
            print("plane_Forward "+PF_angle);

            bool PlaneInArea = planeInprojectingArea();
            //&& PlaneInArea
            if(theta-1 >= 1 )//0.03f
            {
                rot_angle = (theta-1) * Rot_down;// * angle2servo;
                print(angle + " " + rot_angle);
                //print("sevo : " + (servo.pitchPos - (int)(1000 - (angle + rot_angle) * servo2angle)));
                angle = angle + rot_angle;

                motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);
                f_motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);
                int rot_speed = (int)speed(rot_angle);
                servo.motor_singal((int)(1000 - angle * servo2angle), rot_speed);

                /*motor.transform.localRotation = Quaternion.Slerp(motor.transform.localRotation, Quaternion.AngleAxis(angle, rot_vector), 0.35f);
                f_motor.transform.localRotation = Quaternion.Slerp(f_motor.transform.localRotation, Quaternion.AngleAxis(angle, rot_vector), 0.35f);

                float motor_current_angle = motor.transform.localRotation.eulerAngles[0];
                if(motor_current_angle > 360f) motor_current_angle = motor_current_angle % 360;
                print("sevo : " + (servo.pitchPos - (int)(1000 - motor_current_angle * servo2angle)));
                servo.motor_singal((int)(1000 - motor.transform.localRotation.eulerAngles[0] * servo2angle), 100);*/


                /*motor.transform.localRotation = Quaternion.Slerp(motor.transform.localRotation, Quaternion.AngleAxis(angle, rot_vector), 0.35f);
                f_motor.transform.localRotation = Quaternion.Slerp(f_motor.transform.localRotation, Quaternion.AngleAxis(angle, rot_vector), 0.35f);
                servo.motor_singal((int)(1000 - angle * servo2angle), 100);/*
            }
            else 
            {
                f_motor.transform.localRotation = motor.transform.localRotation;
            }
  
        }
        else 
            return;
    }*/

    public float speed(float rot_angle)
    {
        float maxspeed = 100f;
        float minspeed = 20f;
        float max_min_angle = 20;
        float ratio = rot_angle/max_min_angle;
        if(ratio < 0) ratio = -ratio;
        if(ratio > 1) ratio = 1;

        float rot_speed = minspeed + (maxspeed - minspeed)*ratio;

        return rot_speed;
    }

    public bool planeInprojectingArea()
    {
        Vector3[] plane_points = new Vector3[4];
        float y = 0;//Projecting_area_points[0][1];

        GameObject target = new GameObject("corner");
        target.transform.parent = plane.transform;

        //get position of plane corner
        target.transform.localPosition = new Vector3(-5,y,5);
        plane_points[0] = target.transform.position;

        target.transform.localPosition = new Vector3(5,y,0);
        plane_points[1] = target.transform.position;

        target.transform.localPosition = new Vector3(5,y,-5);
        plane_points[2] = target.transform.position;

        target.transform.localPosition = new Vector3(-5,y,-5);
        plane_points[3] = target.transform.position;

        Destroy(target);

        //Projecting_area_points
        int inside_num = 0;
        for(int i = 0; i < 4; i++)
        {
            //bool inside = ContainsPoint(Projecting_area_points,plane_points[i]);
            bool inside = ContainsPoint(Projecting_range_point,plane_points[i]);
            if(inside) inside_num = inside_num + 1;
        }

        print("num "+ inside_num);

        if(inside_num >= 2) return true;
        else return false;

        
    }

    bool ContainsPoint(Vector3[] polyPoints, Vector3 p)
    {
        var j = polyPoints.Length - 1;
        var inside = false;
        for (int i = 0; i < polyPoints.Length; j = i++)
        {
            var pi = polyPoints[i];
            var pj = polyPoints[j];
            if (((pi.z <= p.z && p.z < pj.z) || (pj.z <= p.z && p.z < pi.z)) &&
                (p.x < (pj.x - pi.x) * (p.z - pi.z) / (pj.z - pi.z) + pi.x))
                inside = !inside;
        }
        return inside;
        //https://wiki.unity3d.com/index.php/PolyContainsPoint
    }

    

    public void OSC_motor_control(Vector3 top, Vector3 down)
    {
        Vector3 screenPos = Projector_cam.WorldToScreenPoint(top);
        int projecting_area_top = Projector_cam.pixelHeight - 1;
        if(screenPos.y > projecting_area_top)
        {
            Debug.Log("Rotate motor up");
            servo.motor_up();
        }

        screenPos = Projector_cam.WorldToScreenPoint(down);
        if(screenPos.y < 0)
        {
            Debug.Log("Rotate motor down");
            servo.motor_down();
        }
        //OSC_motor_control(window_top.transform.position, window_down.transform.position);
        //angle = (1000 - servo.pitchPos)/servo2angle;
    }



    public void Projecting_area()
    {
        // Bit shift the index of the layer (9) to get a bit mask
        // This would cast rays only against colliders in layer 8.
        // only hit the floor
        int layerMask = 1 << 9;

        //bottom-left of the screen is (0,0); the right-top is (pixelWidth -1,pixelHeight -1).
        int w = Projector_cam.pixelWidth - 1;
        int h = Projector_cam.pixelHeight - 1;

        Vector3[] Projecting_points = new Vector3[4];
        int Projecting_points_num = 0;

        RaycastHit hit;
        Ray ray = Projector_cam.ScreenPointToRay(new Vector3(0, 0, 0));
        // do we hit our portal plane?
        if (Physics.Raycast(ray, out hit, 10f, layerMask)) 
        {
            Projecting_points[Projecting_points_num] = hit.point;
            Projecting_points_num = Projecting_points_num + 1;
        }

        ray = Projector_cam.ScreenPointToRay(new Vector3(0, h, 0));
        if (Physics.Raycast(ray, out hit, 10f, layerMask)) 
        {
            Projecting_points[Projecting_points_num] = hit.point;
            Projecting_points_num = Projecting_points_num + 1;
        }

        ray = Projector_cam.ScreenPointToRay(new Vector3(w, h, 0));
        if (Physics.Raycast(ray, out hit, 10f, layerMask)) 
        {
            Projecting_points[Projecting_points_num] = hit.point;
            Projecting_points_num = Projecting_points_num + 1;
        }

        ray = Projector_cam.ScreenPointToRay(new Vector3(w, 0, 0));
        if (Physics.Raycast(ray, out hit, 10f, layerMask)) 
        {
            Projecting_points[Projecting_points_num] = hit.point;
            Projecting_points_num = Projecting_points_num + 1;
        }

        if(Projecting_points_num == 4)
        {
            Projecting_area_points = Projecting_points;

            // for visualzation => plane and projecting area 不重疊
            for(int i = 0; i<4; i++)
                Projecting_points[i][1] = -0.0001f;
            DoCreatPloygonMesh(Projecting_points);
        }
        //print(Projecting_points_num);
    }

    public void DoCreatPloygonMesh(Vector3[] s_Vertives, bool range = false)
    {
        //新申请一个Mesh网格
        Mesh tMesh = new Mesh();
 
        //存储所有的顶点
        Vector3[] tVertices = s_Vertives;
 
        //存储画所有三角形的点排序
        List<int> tTriangles = new List<int>();
 
        //根据所有顶点填充点排序
        for (int i = 0; i < tVertices.Length - 1; i++)
        {
            tTriangles.Add(i);
            tTriangles.Add(i + 1);
            tTriangles.Add(tVertices.Length - i - 1);
        }
 
        //赋值多边形顶点
        tMesh.vertices = tVertices;
 
        //赋值三角形点排序
        tMesh.triangles = tTriangles.ToArray();
 
        //重新设置UV，法线
        tMesh.RecalculateBounds();
        tMesh.RecalculateNormals();

        //print(tMesh.normals[0]+" "+tMesh.normals[1]);
 
        //将绘制好的Mesh赋值
        if(range == false)Projector_area.GetComponent<MeshFilter>().mesh = tMesh;
        else Projector_range.GetComponent<MeshFilter>().mesh = tMesh;
 
    }
}
