using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniOSC;
using OSCsharp.Data;

public class OSCSender : UniOSCEventDispatcher{
	
	public bool triggerVibrate = false;
	public int vibrateFreq = 0;
	public int vibrateTime = 0;
	// Update is called once per frame
	void Update () {

        if (triggerVibrate)
        {
            sendVibrateState(1, vibrateFreq, vibrateTime);
        }

	}

    public override void OnEnable()
    {
        base.OnEnable();

        //Initial the Data
        ClearData();

        AppendData(0);
		AppendData(0);
		AppendData(0);
    }

    private void sendVibrateState(int state, int freq, int time)
    {
        //Here we update the data with the new value
        if (_OSCeArg.Packet is OscMessage)
        {
            //Message
            OscMessage msg = ((OscMessage)_OSCeArg.Packet);
            msg.UpdateDataAt(0, state);
		    msg.UpdateDataAt(1, changeFreqToWavelength(freq));
			msg.UpdateDataAt(2, time);

        }
        else if (_OSCeArg.Packet is OscBundle)
        {
            //Bundle
            foreach (OscMessage msg2 in ((OscBundle)_OSCeArg.Packet).Messages)
            {
                msg2.UpdateDataAt(0, state);
				msg2.UpdateDataAt(1, changeFreqToWavelength(freq));
				msg2.UpdateDataAt(2, time);
            }
        }

        //Here we trigger the sending method 
        _SendOSCMessage(_OSCeArg);
    }

    private int changeFreqToWavelength(int freq) {
        return (1000/freq/2);
    }
}