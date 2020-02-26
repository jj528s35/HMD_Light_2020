using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniOSC;
using OSCsharp.Data;

public class OSCPoseReceiver : UniOSCEventTarget {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public override void OnOSCMessageReceived(UniOSCEventArgs args)
    {
        OscMessage msg = (OscMessage)args.Packet;
        if (msg.Data.Count < 1) return;

        Quaternion q = new Quaternion((float)msg.Data[0], (float)msg.Data[1], (float)msg.Data[2], (float)msg.Data[3]);

        this.transform.rotation = GyroToUnity(q);
    }

    private Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }
}
