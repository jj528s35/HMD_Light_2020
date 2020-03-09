using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public float viewRange = 30;
    public Chunk chunkPrefab;

    public GameObject UI;

    private int count_type = 0;
    private bool Tree = false, House = false;
    private Chunk.BlockType type = Chunk.BlockType.Dirt;

    public GameObject crosshair;
    public float size = 0.5f;

    private void Start()
    {
        viewRange = 30;
        UpdateWorld();
    }

    void Update ()
    {
        UpdateInput();
        viewRange = 60;
        UpdateWorld();
    }

    void UpdateInput()
    {
        

        if(Input.GetKeyDown(KeyCode.C))//旋鈕
        {
            count_type = count_type + 1;
            if(count_type % 6 == 0)
                type = Chunk.BlockType.Dirt;
            if(count_type % 6 == 1)
                type = Chunk.BlockType.G_D;
            if(count_type % 6 == 2 || count_type % 6 == 4)
                type = Chunk.BlockType.Grass;
            if(count_type % 6 == 3 || count_type % 6 == 5)
                type = Chunk.BlockType.Gravel;

            UI.gameObject.transform.GetChild(count_type % 6).gameObject.GetComponent<Image>().color = Color.yellow;
            UI.gameObject.transform.GetChild((count_type - 1) % 6).gameObject.GetComponent<Image>().color = Color.white;
        }

        // Ray ray = new Ray( targetPosition, direction ); //controll forwrd and pos
        Ray ray_ = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit_;
        if(Physics.Raycast(ray_, out hit_, 25))
        {
            crosshair.transform.position = hit_.point;
            crosshair.transform.up = hit_.normal;
        }

        if(Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) {
            Chunk chunk = hit_.transform.GetComponent<Chunk>();
            if(chunk == null) {
                return;
            }
            if(Input.GetMouseButtonDown(0)) {//build block
                if(count_type % 6 <= 3) creat_block(hit_,  chunk);
                else creat_tree_v2(hit_,  chunk, type);
            } 

            if(Input.GetMouseButtonDown(1)) {//destory block
                destory_block(hit_, chunk);
            }
            
            /*if(Input.GetMouseButtonDown(2))) {
                Vector3 p = hit.point;
                p -= hit.normal / 4;
                selectedInventory = chunk.GetByte(p);
            }*/
			
		}
    }

    void UpdateWorld()
    {
        for (float x = transform.position.x - viewRange; x < transform.position.x + viewRange; x += Chunk.width)
        {
            for (float z = transform.position.z - viewRange; z < transform.position.z + viewRange; z += Chunk.width)
            {
                Vector3 pos = new Vector3(x, -1, z);
                pos.x = Mathf.Floor(pos.x / (float)Chunk.width) * Chunk.width*size;
                pos.z = Mathf.Floor(pos.z / (float)Chunk.width) * Chunk.width*size;

                Chunk chunk = Chunk.GetChunk(pos);
                if (chunk != null) continue;

                if(viewRange == 60) {
                    //chunkPrefab.amplitude = 1;
                }
                chunk = (Chunk)Instantiate(chunkPrefab, pos, Quaternion.identity);
            }
        }
    }

    void creat_block(RaycastHit hit, Chunk chunk_)
    {
        Vector3 p = hit.point;
        p += hit.normal / 4;
        p -= chunk_.transform.position;
        p = p/size;
        chunk_.BuildBlock_v2(type, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1]), Mathf.FloorToInt(p[2]));
    }

    void destory_block(RaycastHit hit, Chunk chunk_)
    {
        Vector3 p = hit.point;
        p -= hit.normal / 4;
        p -= chunk_.transform.position;
        p = p/size;
        chunk_.BuildBlock_v2(Chunk.BlockType.None, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1]), Mathf.FloorToInt(p[2]));
    }

    void creat_tree_v2(RaycastHit hit, Chunk chunk_, Chunk.BlockType type_)
    {
        Vector3 p = hit.point;
        p += hit.normal / 4;
        p -= chunk_.transform.position;
        p = p/size;

        int h = 4;
        for(int i = 0; i < h; i++)
            chunk_.BuildBlock_v2(Chunk.BlockType.Dirt, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1])+i, Mathf.FloorToInt(p[2]));
        
        //leave
        float radius = ((float)h/3)*2;
        Vector3 center = p + new Vector3(0, h-1, 0);
        for(int i = -(int)radius; i < radius; i++)
        {
            for(int j = 0; j < radius; j++)
            {
                for(int k = -(int)radius; k < radius; k++)
                {
                    Vector3 pos = new Vector3(i + center.x, j + center.y, k + center.z); 
                    float dist = Vector3.Distance(center, pos);
                    if(dist < radius) 
                        chunk_.BuildBlock_v2(type_, Mathf.FloorToInt(pos[0]), Mathf.FloorToInt(pos[1]), Mathf.FloorToInt(pos[2]));
                }
            }
        }
    }
    //https://www.youtube.com/watch?v=LyAyY9eP1vE

    

     /*void creat_tree(RaycastHit hit, Chunk chunk_)
    {
        Vector3 p = hit.point;
        p += hit.normal / 4;
        p -= chunk_.transform.position;
        chunk_.BuildBlock_v2(Chunk.BlockType.Dirt, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1]), Mathf.FloorToInt(p[2]));
        chunk_.BuildBlock_v2(Chunk.BlockType.Dirt, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1])+1, Mathf.FloorToInt(p[2]));
        chunk_.BuildBlock_v2(Chunk.BlockType.Dirt, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1])+2, Mathf.FloorToInt(p[2]));

        chunk_.BuildBlock_v2(type, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1])+3, Mathf.FloorToInt(p[2]));
        chunk_.BuildBlock_v2(type, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1])+4, Mathf.FloorToInt(p[2]));
        chunk_.BuildBlock_v2(type, Mathf.FloorToInt(p[0])+1, Mathf.FloorToInt(p[1])+3, Mathf.FloorToInt(p[2]));
        chunk_.BuildBlock_v2(type, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1])+3, Mathf.FloorToInt(p[2])+1);
        chunk_.BuildBlock_v2(type, Mathf.FloorToInt(p[0])-1, Mathf.FloorToInt(p[1])+3, Mathf.FloorToInt(p[2]));
        chunk_.BuildBlock_v2(type, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1])+3, Mathf.FloorToInt(p[2])-1);
    }

    

    void destory_tree(RaycastHit hit, Chunk chunk_)
    {
        Vector3 p = hit.point;
        p -= hit.normal / 4;
        p -= chunk_.transform.position;
        chunk_.BuildBlock_v2(Chunk.BlockType.None, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1]), Mathf.FloorToInt(p[2]));
        chunk_.BuildBlock_v2(Chunk.BlockType.None, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1])+1, Mathf.FloorToInt(p[2]));
        chunk_.BuildBlock_v2(Chunk.BlockType.None, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1])+2, Mathf.FloorToInt(p[2]));

        chunk_.BuildBlock_v2(Chunk.BlockType.None, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1])+3, Mathf.FloorToInt(p[2]));
        chunk_.BuildBlock_v2(Chunk.BlockType.None, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1])+4, Mathf.FloorToInt(p[2]));
        chunk_.BuildBlock_v2(Chunk.BlockType.None, Mathf.FloorToInt(p[0])+1, Mathf.FloorToInt(p[1])+3, Mathf.FloorToInt(p[2]));
        chunk_.BuildBlock_v2(Chunk.BlockType.None, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1])+3, Mathf.FloorToInt(p[2])+1);
        chunk_.BuildBlock_v2(Chunk.BlockType.None, Mathf.FloorToInt(p[0])-1, Mathf.FloorToInt(p[1])+3, Mathf.FloorToInt(p[2]));
        chunk_.BuildBlock_v2(Chunk.BlockType.None, Mathf.FloorToInt(p[0]), Mathf.FloorToInt(p[1])+3, Mathf.FloorToInt(p[2])-1);
    }

   void creat_house(RaycastHit hit, Chunk chunk_)
    {
        Vector3 p = hit.point;
        p += hit.normal / 4;
        p -= chunk_.transform.position;

        int h = 4;
        for(int y = 0; y < h; y++)
            for(int x = -2; x < 3; x++)
                for(int z = -3; z < 3; z++)
                    if(x == -2 || z == -3 || x == 2 || z == 2)
                        chunk_.BuildBlock_v2(Chunk.BlockType.Dirt, Mathf.FloorToInt(p[0])+x, Mathf.FloorToInt(p[1])+y, Mathf.FloorToInt(p[2])+z);
    
         int cell = -2;
         for(int y = h-1; y < 7; y++){
            cell = cell + 1;
            for(int x = -2 + cell; x < 3 - cell; x++)
                for(int z = -3 + -1; z < 3 - -1; z++){
                    if(x == -2 + cell|| z == -3 + -1|| x == 2 - cell|| z == 2 - -1)
                        chunk_.BuildBlock_v2(Chunk.BlockType.Dirt, Mathf.FloorToInt(p[0])+x, Mathf.FloorToInt(p[1])+y, Mathf.FloorToInt(p[2])+z);       

                        if(y == h-1 && (z == -3 + -1 || z == 2 - -1) && (x >= -1 && x <= 1))
                            chunk_.BuildBlock_v2(Chunk.BlockType.None, Mathf.FloorToInt(p[0])+x, Mathf.FloorToInt(p[1])+y, Mathf.FloorToInt(p[2])+z);
                }
        }
        
    }*/
}
//https://github.com/meta-42/Minecraft-Unity
//https://github.com/theWildSushii/Mine-In-Unity/tree/master/Assets/Materials

