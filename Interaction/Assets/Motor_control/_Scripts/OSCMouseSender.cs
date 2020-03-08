using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniOSC;
using OSCsharp.Data;

public class OSCMouseSender : UniOSCEventDispatcher{
	
	// Update is called once per frame
	void Update () {

        if (Input.GetMouseButtonDown(0))
        {
            sendMouseState(0, Input.mousePosition);
        }

        else if (Input.GetMouseButton(0))
        {
            sendMouseState(1, Input.mousePosition);
        }

        else if (Input.GetMouseButtonUp(0))
        {
            sendMouseState(2, Input.mousePosition);
        }

	}

    public override void OnEnable()
    {
        base.OnEnable();

        //Initial the Data
        ClearData();

        AppendData(0);
        AppendData(0.1f);
        AppendData(0.2f);
    }

    private void sendMouseState(int state, Vector2 pos)
    {
        //Here we update the data with the new value
        if (_OSCeArg.Packet is OscMessage)
        {
            //Message
            OscMessage msg = ((OscMessage)_OSCeArg.Packet);
            msg.UpdateDataAt(0, state);
            msg.UpdateDataAt(1, pos.x);
            msg.UpdateDataAt(2, pos.y);

        }
        else if (_OSCeArg.Packet is OscBundle)
        {
            //Bundle
            foreach (OscMessage msg2 in ((OscBundle)_OSCeArg.Packet).Messages)
            {
                msg2.UpdateDataAt(0, state);
                msg2.UpdateDataAt(1, pos.x);
                msg2.UpdateDataAt(2, pos.y);
            }
        }

        //Here we trigger the sending method 
        _SendOSCMessage(_OSCeArg);
    }
}
