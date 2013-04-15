using UnityEngine;
using System.Collections;
using JSONEncoderDecoder;
using System.Threading;

public class PubnubTest : MonoBehaviour {
	
	string Pubnub_PublishKey = "demo";
    string Pubnub_SubscribeKey = "demo";
    string Pubnub_SecretKey = "";
	string Pubnub_Channel = "unitydemo";
			
	string lastCommand = "";
	string changeColour = "";
	string currentStatus = "";
	string currentRotation = "stop";
	
	Thread UpdateCatThread;
	
	PubnubThreads pubNubThreads;
	
	bool playSound = false;
	
	// ****************************************************************************************************
	// Overloaded MonoBehaviour Functions
	// ****************************************************************************************************
	#region MonoBehaviour_Functions
	void Start ()
	{	
		pubNubThreads = new PubnubThreads(Pubnub_SubscribeKey, Pubnub_PublishKey);
		
		// Subscribe to this SmartPoint's PubNub Channel
		pubNubThreads.Subscribe(Pubnub_Channel, DoSomething);
	}
	
	void Update ()
	{
		// Keep the cube rotating if needs be
		switch (currentRotation)
		{
		case "left":
			this.transform.Rotate(0, 20 * Time.deltaTime, 0);
			break;
		case "right":
			this.transform.Rotate(0, -20 * Time.deltaTime, 0);
			break;
		case "up":
			this.transform.Rotate(20 * Time.deltaTime, 0, 0);
			break;
		case "down":
			this.transform.Rotate(-20 * Time.deltaTime, 0, 0);
			break;
		case "reset":
			this.transform.rotation = Quaternion.identity;
			this.renderer.material.color = Color.white;
			currentRotation = "stop";
			break;
		}
		
		// Change the colour - can't do this in the callback as need reference to the renderer
		// which can only be had in the main thread
		switch (changeColour)
		{
		case "red":
			this.renderer.material.color = Color.red;
			changeColour = "";
			break;
		case "green":
			this.renderer.material.color = Color.green;
			changeColour = "";
			break;
		case "blue":
			this.renderer.material.color = Color.blue;
			changeColour = "";
			break;
		case "white":
			this.renderer.material.color = Color.white;
			changeColour = "";
			break;
		}
		
		if (playSound)
		{
			this.audio.Play();
			playSound = false;
		}
	}
	
	void OnGUI()
	{
		GUI.Label (new Rect(10, 10, 300, 40), "Latest Command : " + lastCommand);
	}
	
	void OnDestroy()
	{
		Debug.Log ("Killing all threads");
		pubNubThreads.KillAll ();
	}
	// ****************************************************************************************************
	#endregion

	
	
	// ****************************************************************************************************
	// Callback Functions
	// ****************************************************************************************************	
	#region Callback_Functions
	void DoSomething (string theMessage)
	{
		// Do something with a command coming in from the PubNub Channel
		switch (theMessage.ToLower ().Trim ())
		{
		case "left":
		case "right":
		case "up":
		case "down":
		case "stop":
		case "reset":
			currentRotation = lastCommand = theMessage;
			break;
		case "red":
		case "green":
		case "blue":
		case "white":
			changeColour = lastCommand = theMessage;
			break;
		case "meow":
			lastCommand = theMessage;
			playSound = true;
			break;
		}
		
		Debug.Log (theMessage);
	}
	
	void PublishMessage(string theMessage)
	{
		// Any messages from the publish process
		Debug.Log (theMessage);
	}
	#endregion
}
