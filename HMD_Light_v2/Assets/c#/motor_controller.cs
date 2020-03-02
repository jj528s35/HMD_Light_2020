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
    private Vector3 init_rot_x;

    [Header("OSC Motor control")]
    public GameObject window_top;
    public GameObject window_down;

    [Header("depth camera local_pos")]
    public GameObject depth_child;

    [Header("Projecting area")]
    public Camera Projector;
    public GameObject Projector_area;


    // Start is called before the first frame update
    void Start()
    {
        m_rot.transform.position = motor.transform.position + rot_vector * 0.05f;//for visual
        servo2angle = 1024f/360f;

        //init the motor angle
        servo.pitchPos = inti_sevoAngle;
        init_angle = (1000 - inti_sevoAngle)/servo2angle;
        angle = init_angle;
        motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);

        init_rot_x = Projector.transform.rotation.eulerAngles;
        init_rot_x = new Vector3(init_rot_x[0], 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        //OSC_motor_control(window_top.transform.position, window_down.transform.position);
        //angle = (1000 - servo.pitchPos)/servo2angle;

        diff_angle = motor_control(init_rot_x);
        angle = angle + diff_angle;
        servo.pitchPos = (int)(1000 - angle * servo2angle);

        // Sets the transforms rotation to rotate 30 degrees around the y-axis
        motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);
        

        //show Projecting_area
        Projecting_area();

        
        if(Input.GetKeyDown(KeyCode.Space))
        {
            //set init value
            servo.pitchPos = inti_sevoAngle;
            init_angle = (1000 - inti_sevoAngle)/servo2angle;
            angle = init_angle;
            motor.transform.localRotation = Quaternion.AngleAxis(angle, rot_vector);

            init_rot_x = Projector.transform.rotation.eulerAngles;
            init_rot_x = new Vector3(init_rot_x[0], 0, 0);
        }

        
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

    public float motor_control(Vector3 init_rot_x)
    {
        Vector3 rot_x = Projector.transform.rotation.eulerAngles;
        rot_x = new Vector3(rot_x[0], 0, 0);

        Quaternion init_q = Quaternion.Euler(init_rot_x[0], 0, 0);
        Quaternion q = Quaternion.Euler(rot_x[0], 0, 0);
        float AngleDiff =  Quaternion.Angle(init_q , q);
        
        // get the signed difference in these angles
        var angleDiff = Mathf.DeltaAngle(init_rot_x[0], rot_x[0]);

        if(angleDiff > 0)
            AngleDiff = - AngleDiff;

        print(init_rot_x[0] + " " + rot_x[0] + " " + AngleDiff + " " + angleDiff);

        return AngleDiff;
    }

    public void OSC_motor_control(Vector3 top, Vector3 down)
    {
        Vector3 screenPos = Projector.WorldToScreenPoint(top);
        int projecting_area_top = Projector.pixelHeight - 1;
        if(screenPos.y > projecting_area_top)
        {
            Debug.Log("Rotate motor up");
            servo.motor_up();
        }

        screenPos = Projector.WorldToScreenPoint(down);
        if(screenPos.y < 0)
        {
            Debug.Log("Rotate motor down");
            servo.motor_down();
        }
    }



    public void Projecting_area()
    {
        // Bit shift the index of the layer (9) to get a bit mask
        // This would cast rays only against colliders in layer 8.
        // only hit the floor
        int layerMask = 1 << 9;

        //bottom-left of the screen is (0,0); the right-top is (pixelWidth -1,pixelHeight -1).
        int w = Projector.pixelWidth - 1;
        int h = Projector.pixelHeight - 1;

        Vector3[] Projecting_points = new Vector3[4];
        int Projecting_points_num = 0;

        RaycastHit hit;
        Ray ray = Projector.ScreenPointToRay(new Vector3(0, 0, 0));
        // do we hit our portal plane?
        if (Physics.Raycast(ray, out hit, 10f, layerMask)) 
        {
            Projecting_points[Projecting_points_num] = hit.point;
            Projecting_points_num = Projecting_points_num + 1;
        }

        ray = Projector.ScreenPointToRay(new Vector3(0, h, 0));
        if (Physics.Raycast(ray, out hit, 10f, layerMask)) 
        {
            Projecting_points[Projecting_points_num] = hit.point;
            Projecting_points_num = Projecting_points_num + 1;
        }

        ray = Projector.ScreenPointToRay(new Vector3(w, h, 0));
        if (Physics.Raycast(ray, out hit, 10f, layerMask)) 
        {
            Projecting_points[Projecting_points_num] = hit.point;
            Projecting_points_num = Projecting_points_num + 1;
        }

        ray = Projector.ScreenPointToRay(new Vector3(w, 0, 0));
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
