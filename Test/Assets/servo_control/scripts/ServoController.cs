using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO.Ports;
using UnityEngine.UI;
using UniOSC;
using OSCsharp.Data;

public class ServoController : UniOSCEventDispatcher
{
	[Header("communincation")]
	public CHANNEL NowConnection = CHANNEL.SERIAL;
	public string comportNumber;

	[Header("servo")]
	public float speed;
	public int pitchPos = 1000;
	public int pitchSpeed = 100;
	public int pitchInterval = 5;
	public int yawPos = 85;
	public int yawInterval = 5;

	[Header("UI")]
	public Text pitchText;
	public Text yawText;

	[Header("test")]
	public float testPitchTime = 10f;
	public float testYawTime = 10;

	private int maxPitchPos = 1000;
	private int minPitchPos = 650;
	private int maxYawPos = 180;
	private int minYawPos = 0;
	private SerialPort serial;

	private float lastSendTime;
	private float respondTime = 0.05f;

	public enum CHANNEL
	{
		SERIAL,
		OSC
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

	// Use this for initialization
	void Start () {
		if(NowConnection == CHANNEL.SERIAL)
		{
			serial = new SerialPort(comportNumber, 57600);
			serial.Open();
		}
	}
	
	// Update is called once per frame
	void Update () {

		if (Input.GetKeyDown(KeyCode.UpArrow))
		{
			pitchPos += pitchInterval;
			if(pitchPos > maxPitchPos) pitchPos = maxPitchPos;
			SendSingal();
		}
		else if(Input.GetKeyDown(KeyCode.DownArrow))
		{
			pitchPos -= pitchInterval;
			if (pitchPos < minPitchPos) pitchPos = minPitchPos;
			SendSingal();
		}
		else if(Input.GetKeyDown(KeyCode.LeftArrow))
		{
			yawPos -= yawInterval;
			if (yawPos < minYawPos) yawPos = minYawPos;
			SendSingal();
		}
		else if(Input.GetKeyDown(KeyCode.RightArrow))
		{
			yawPos += yawInterval;
			if (yawPos > maxYawPos) yawPos = maxYawPos;
			SendSingal();
		}
		else if(Input.GetKeyDown(KeyCode.Z))
		{
			StartCoroutine(TestPitchAndYawMotion());
		}else if(Input.GetKeyDown(KeyCode.X))
		{
			StartCoroutine(TestPitchMotion());
		}
		else if(Input.GetKeyDown(KeyCode.C))
		{
			StartCoroutine(TestYawMotion());
		}
	}

	IEnumerator TestPitchMotion()
	{
		Debug.Log("testing pitch motion");
		float testingTimes = testPitchTime / respondTime;
		int pitchSteps = (maxPitchPos - minPitchPos) / (int) testingTimes;
		for (int p = maxPitchPos; p>minPitchPos;p -= pitchSteps)
		{
			pitchPos = p;
			SendSingal();
			yield return new WaitForSeconds(respondTime);
		}

		for(int p = minPitchPos; p<maxPitchPos;p+=pitchSteps)
		{
			pitchPos = p;
			SendSingal();
			yield return new WaitForSeconds(respondTime);
		}

		Debug.Log("tested pitch motion");
		yield break;
	}

	IEnumerator TestYawMotion()
	{
		Debug.Log("test yaw motion");
		float testingTimes = testYawTime / respondTime;
		int yawSteps = 2 * (maxYawPos - minYawPos) / (int)testingTimes;
		for(int y = 90;y<maxYawPos;y+=yawSteps)
		{
			yawPos = y;
			SendSingal();
			yield return new WaitForSeconds(respondTime);
		}
		for(int y=maxYawPos;y>minYawPos;y-=yawSteps)
		{
			yawPos = y;
			SendSingal();
			yield return new WaitForSeconds(respondTime);
		}
		for(int y=minYawPos;y<90;y+=yawSteps)
		{
			yawPos = y;
			SendSingal();
			yield return new WaitForSeconds(respondTime);
		}

		Debug.Log("tested yaw motion");
		yield break;
	}

	IEnumerator TestPitchAndYawMotion()
	{
		Debug.Log("testing pitch and yaw motion");
		float testingTimes = testPitchTime / respondTime;
		int pitchSteps = (maxPitchPos - minPitchPos) / (int)testingTimes;
		for (int p = maxPitchPos; p > minPitchPos; p -= pitchSteps)
		{
			pitchPos = p;
			SendSingal();
			yield return new WaitForSeconds(respondTime);
		}

		for (int p = minPitchPos; p < maxPitchPos; p += pitchSteps)
		{
			pitchPos = p;
			SendSingal();
			yield return new WaitForSeconds(respondTime);
		}

		testingTimes = testYawTime / respondTime;
		int yawSteps = 2 * (maxYawPos - minYawPos) / (int)testingTimes;
		for (int y = 90; y < maxYawPos; y += yawSteps)
		{
			yawPos = y;
			SendSingal();
			yield return new WaitForSeconds(respondTime);
		}
		for (int y = maxYawPos; y > minYawPos; y -= yawSteps)
		{
			yawPos = y;
			SendSingal();
			yield return new WaitForSeconds(respondTime);
		}
		for (int y = minYawPos; y < 90; y += yawSteps)
		{
			yawPos = y;
			SendSingal();
			yield return new WaitForSeconds(respondTime);
		}

		Debug.Log("tested pitch and yaw motion");
		yield break;
	}

	void SendSingal()
	{
		if(NowConnection == CHANNEL.SERIAL)
		{
			if (!serial.IsOpen) return;

			if (Time.time - lastSendTime > respondTime)
			{
				string singal = pitchPos.ToString("0") + " " + pitchSpeed.ToString("0") + " " + yawPos.ToString("0");
				serial.Write(singal);
				Debug.Log("send singal: " + singal);
				lastSendTime = Time.time;
			}
		}
		else if(NowConnection == CHANNEL.OSC)
		{
			//Here we update the data with the new value
			if (_OSCeArg.Packet is OscMessage)
			{
				//Message
				OscMessage msg = ((OscMessage)_OSCeArg.Packet);
				msg.UpdateDataAt(0, pitchPos);
				msg.UpdateDataAt(1, pitchSpeed);
				msg.UpdateDataAt(2, yawPos);

			}
			else if (_OSCeArg.Packet is OscBundle)
			{
				//Bundle
				foreach (OscMessage msg2 in ((OscBundle)_OSCeArg.Packet).Messages)
				{
					msg2.UpdateDataAt(0, pitchPos);
					msg2.UpdateDataAt(1, pitchSpeed);
					msg2.UpdateDataAt(2, yawPos);
				}
			}

			//Here we trigger the sending method 
			_SendOSCMessage(_OSCeArg);
		}
		if (pitchText != null) pitchText.text = pitchPos.ToString("0");
		if (yawText != null) yawText.text = yawPos.ToString("0");
	}

	public void motor_up()
	{
		pitchPos += pitchInterval;
		if(pitchPos > maxPitchPos) pitchPos = maxPitchPos;
		SendSingal();
	}

	public void motor_down()
	{
		pitchPos -= pitchInterval;
		if (pitchPos < minPitchPos) pitchPos = minPitchPos;
		SendSingal();
	}

	public void motor_left()
	{
		yawPos -= yawInterval;
		if (yawPos < minYawPos) yawPos = minYawPos;
		SendSingal();
	}

	public void motor_right()
	{
		yawPos += yawInterval;
		if (yawPos > maxYawPos) yawPos = maxYawPos;
		SendSingal();
	}

	public void motor_init(int yaw,int pitch)
	{
		yawPos = yaw;
		//pitchPos = pitch;
		if (yawPos > maxYawPos) yawPos = maxYawPos;
		//if (pitchPos < minPitchPos) pitchPos = minPitchPos;
		SendSingal();
	}
}
