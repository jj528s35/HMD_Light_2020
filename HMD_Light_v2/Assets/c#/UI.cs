using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public InputField shiftx;
    public InputField shifty;
    public GameObject setlenshift;
    public Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("press 'S' to set camera len shift");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            setlenshift.SetActive(true);
        }
    }

    public void onButtonClick() 
    {
        setlenshift.SetActive(false);
    }


    public void setx ()
     {
         if (shiftx.text.Length > 0)
         {
            float x = float.Parse(shiftx.text);
    
            cam.lensShift = new Vector2(x, cam.lensShift[1]); 
         }
     }

     public void sety ()
     {
         if (shifty.text.Length > 0)
         {
            float y = float.Parse(shifty.text);
    
            cam.lensShift = new Vector2(cam.lensShift[0], y); 
         }
     }
}
