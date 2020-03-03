using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class motor_controller : MonoBehaviour
{
    
    [Header("Virtual Motor rotation")]
    public int inti_sevoAngle;
    public GameObject motor, m_rot;
    public Vector3 rot_vector; //旋轉軸心
    public float angle, diff_angle;
    public ServoController servo;
    private float servo2angle, init_angle;
    private Vector3 init_rot_x, init_forward;
    public GameObject Projector;
    public GameObject zed;
    private Vector3 zed_rot;
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


    // Start is called before the first frame update
    void Start()
    {
        m_rot.transform.position = motor.transform.position + rot_vector * 0.05f;//for visual
        servo2angle = 1024f/360f;

        //init the motor angle
        servo.pitchPos = inti_sevoAngle;
        servo.motor_singal();
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
        
        //motor_control(init_rot_x);
         motor_angle();
        
        //show Projecting_area
        Projecting_area();



        
        /*if(Input.GetKeyDown(KeyCode.Space))
        {
            //set init value
            servo.pitchPos = inti_sevoAngle;
            servo.motor_singal();
            init_angle = (1000 - inti_sevoAngle)/servo2angle;
            angle = init_angle;
            motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);

            init_rot_x = Projector.transform.rotation.eulerAngles;
            zed_rot = zed.transform.rotation.eulerAngles;
        }*/

        
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

    public void motor_angle()
    {
        float theta = 1;
        bool first = true;
        int w = Projector_cam.pixelWidth;
        int h = Projector_cam.pixelHeight;
        
        // only hit the floor
        int layerMask = 1 << 9;

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

            float dot = Vector3.Dot( dir, plane.transform.forward );
            sameDirection = dot >= 0; 
            if(sameDirection) Rot_down = 1; // same as forward dir(blue axix) => rotation down
            else Rot_down = -1;

            while(new_dist <= dist && (first ||(dist - new_dist) >= 0.01f))
            {
                first = false;
                dist = new_dist;
                rot_angle = theta * Rot_down;
                
                f_motor.transform.localRotation = Quaternion.AngleAxis(angle + rot_angle, rot_vector);

                ray = f_Projector.ScreenPointToRay(new Vector3(w/2, h/2, 0));
                if (Physics.Raycast(ray, out hit, 10f, layerMask))
                {
                    new_dist = Vector3.Distance(hit.point, plane.transform.position);
                    //print(dist-new_dist);
                } 
                else break;

                theta++;              
            }

            if(ori_dist - dist > 0.05f)
            {
                print(ori_dist-dist);
                rot_angle = (theta-1) * Rot_down;
                print(angle+" "+rot_angle);
                angle = angle + rot_angle;
                motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);
                f_motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);
                servo.pitchPos = (int)(1000 - angle * servo2angle);
                servo.motor_singal();
            }
            else f_motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);
            
            
            
        }
        else 
            return;
    }

    public void motor_control(Vector3 init_rot_x)
    {
        //print(init_rot_x + " " + Projector.transform.rotation.eulerAngles);
        Vector3 rot_x = Projector.transform.rotation.eulerAngles;
        rot_x = new Vector3(rot_x[0], 0, 0);

        Quaternion init_q = Quaternion.Euler(init_rot_x[0], 0, 0);
        Quaternion q = Quaternion.Euler(rot_x[0], 0, 0);
        float Angle_Diff =  Quaternion.Angle(init_q , q);
        
        // get the signed difference in these angles
        var angleDiff = Mathf.DeltaAngle(init_rot_x[0], rot_x[0]);

        var forwardA = init_q * Vector3.forward;
        var forwardB = q * Vector3.forward;

        // get a numeric angle for each vector, on the X-Z plane (relative to world forward)
        var angleA = Mathf.Atan2(forwardA.y, forwardA.z)*Mathf.Rad2Deg;
        var angleB = Mathf.Atan2(forwardB.y, forwardB.z)*Mathf.Rad2Deg;

        // get the signed difference in these angles
        var angleDiff_ = Mathf.DeltaAngle( angleA, angleB );
        //print(angleDiff_);

        //print(init_rot_x + " " + Projector.transform.rotation.eulerAngles);
        Vector3 zrot_x = zed.transform.rotation.eulerAngles;
        zrot_x = new Vector3(zrot_x[0], 0, 0);
        // get the signed difference in these angles
        var zangleDiff = Mathf.DeltaAngle(zed_rot[0], zrot_x[0]);
        print(zangleDiff);

        
        if(zangleDiff > 0)//-10
            Angle_Diff = - Angle_Diff;
        else
            Angle_Diff = Angle_Diff;

        /*if(Mathf.Abs(zangleDiff)<1) 
            Angle_Diff = 0;
        else zed_rot = zrot_x;*/

        diff_angle = Angle_Diff;
        print(angle);
        angle = angle + diff_angle;
        print(angle);
        
        if(angle > 360f) angle = angle % 360;
        servo.pitchPos = (int)(1000 - angle * servo2angle);
        servo.motor_singal();

        // Sets the transforms rotation to rotate 30 degrees around the y-axis
        motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);

        print(init_rot_x[0] + " " + rot_x[0] + " " + Angle_Diff + " " + angleDiff + " " +  Projector.transform.rotation.eulerAngles[0]);
        
        zed_rot = zrot_x;
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
            DoCreatPloygonMesh(Projecting_points);
        }
        //print(Projecting_points_num);
    }

    public void DoCreatPloygonMesh(Vector3[] s_Vertives)
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
        Projector_area.GetComponent<MeshFilter>().mesh = tMesh;
 
    }
}
