using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BtnClick : MonoBehaviour {

    public OSCSender oscSender;
    
    public void StartVibrate()
    {
        oscSender.GetComponent<OSCSender>().triggerVibrate = true;
    }
    
    public void StopVibrate()
    {
        oscSender.GetComponent<OSCSender>().triggerVibrate = false;
    }


}
