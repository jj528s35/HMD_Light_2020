using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniOSC;
using OSCsharp.Data;

public class OSCCommandReceiver : UniOSCEventTarget
{

    public Material[] materialList;
    private int currentMaterial;

	// Use this for initialization
	void Start () {
        currentMaterial = 0;
	}

    public override void OnOSCMessageReceived(UniOSCEventArgs args)
    {
        OscMessage msg = (OscMessage)args.Packet;
        if (msg.Data.Count < 1) return;

        int command = (int)msg.Data[0];
       
        //Change Color
        if (command == 1)
        {
            currentMaterial = (currentMaterial + 1) % materialList.Length;
            GetComponent<Renderer>().material = materialList[currentMaterial];
        }

    }
}
