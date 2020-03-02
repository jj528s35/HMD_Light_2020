using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniOSC;
using OSCsharp.Data;

public class OSCTouchReceiver : UniOSCEventTarget{

    public GameObject cursor;

    private const float X_MAX = 1000.0f;
    private const float Y_MAX = 2000.0f;

    public override void OnOSCMessageReceived(UniOSCEventArgs args)
    {
        OscMessage msg = (OscMessage)args.Packet;
        if (msg.Data.Count < 1) return;

        Vector2 phonePos = Vector2.zero;
        phonePos.x = (float)msg.Data[0];
        phonePos.y = (float)msg.Data[1];

        cursor.transform.localPosition = new Vector3(phonePos.x / X_MAX - 0.5f, 1.0f, phonePos.y / Y_MAX - 0.5f);
    }
}
