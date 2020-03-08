using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniOSC;
using OSCsharp.Data;
public class OSCReceiver : UniOSCEventTarget {

    public GameObject sphere;

	// Use this for initialization
	void Start () {
        sphere.transform.localScale = new Vector3 (1f, 1f, 1f);
	}

    public override void OnOSCMessageReceived(UniOSCEventArgs args)
    {
        OscMessage msg = (OscMessage)args.Packet;
        if (msg.Data.Count < 1) return;

        int command = (int)msg.Data[0];
       
        //Change Size
        if (command >= 0)
        {
            float newScale = (float) command / 128f;
            sphere.transform.localScale = new Vector3 (newScale, newScale, newScale);
        }

    }
}
