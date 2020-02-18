using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class plane_pos_rot : MonoBehaviour
{ 
    public GameObject plane_;
    public Vector3 n_vector = Vector3.up;
    private socket_receive receive_data;
    // Start is called before the first frame update
    void Start()
    {
        receive_data = GameObject.Find("Receive data").GetComponent<socket_receive>();
    }

    // Update is called once per frame
    void Update()
    {
        /* depth plane mask visualiation
        int num = receive_data.plane_points_num;
        Vector3[] plane_points = receive_data.plane_points;

        DoCreatPloygonMesh(plane_points);*/


        int num = receive_data.target_plane_points_num;
        Vector3[] plane_points = receive_data.target_plane;

        DoCreatPloygonMesh2(plane_points, num);

        //DoCreatPloygonMesh(plane_points);
    }

    public void DoCreatPloygonMesh2(Vector3[] s_Vertives, int num)
    {
        //新申请一个Mesh网格
        Mesh tMesh = new Mesh();
 
        //存储所有的顶点
        Vector3[] tVertices = s_Vertives;
 
        //存储画所有三角形的点排序
        List<int> tTriangles = new List<int>();
 
        //根据所有顶点填充点排序
        for (int i = 0; i < num - 6; i++)
        {
            tTriangles.Add(i);
            tTriangles.Add(i + 1);
            tTriangles.Add(i + 6);

            // lower left triangle
            tTriangles.Add(i);
            tTriangles.Add(i + 5);
            tTriangles.Add(i + 6);
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
        GetComponent<MeshFilter>().mesh = tMesh;
 
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
        GetComponent<MeshFilter>().mesh = tMesh;
 
    }
}
