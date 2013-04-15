// ******************************************************************************************************************************************************
// Copyright Imersia Ltd, February 2013
// Version 130316-01
//
// For information on the Pubnub REST API, go here: http://www.pubnub.com/tutorial/http-rest-push-api
// ******************************************************************************************************************************************************

using System;
using System.Collections;
using JSONEncoderDecoder;
using System.Xml;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using UnityEngine;

public delegate void stringCallback(string message);				// The callback function

public class PubnubThreads
{
    private string PubnubURL = "http://pubsub.pubnub.com/";			// URL for the Pubnub REST API
    private Hashtable threadPool = new Hashtable();					// A pool of threads, one for each channel

    private string pubnubSubKey = "";
    private string pubnubPubKey = "";
	private string pubnubSecretKey = "";
	
	public bool stopped = false;

    // ****************************************************************************************************
    // Constructors
    // ****************************************************************************************************
    public PubnubThreads(string subscribeKey, string publishKey)
	{
        pubnubSubKey = subscribeKey;
        pubnubPubKey = publishKey;
	}
	public PubnubThreads(string subscribeKey, string publishKey, string secretKey)
    {
        pubnubSubKey = subscribeKey;
        pubnubPubKey = publishKey;
		pubnubSecretKey = secretKey;
    }
    // ****************************************************************************************************



    // ****************************************************************************************************
    // Destructor
    // ****************************************************************************************************
    ~PubnubThreads()
    {
        KillAll();
    }
    // ****************************************************************************************************



    // ****************************************************************************************************
    // Determine if we have Internet access of not
    // ****************************************************************************************************
    public static bool HasConnection()
    {
        try
        {
            System.Net.IPHostEntry i = System.Net.Dns.GetHostEntry("www.google.com");
            return true;
        }
        catch
        {
            return false;
        }
    }
    // ****************************************************************************************************



    // ****************************************************************************************************
    // Return a text-based status of the given channel
    // ****************************************************************************************************
    public string Status(string channel)
    {
        if (HasConnection())
        {
            Thread oThread = (Thread)threadPool[channel];
			if (oThread != null)
			{
	            if (oThread.IsAlive)
	                return "OK";
	            else
	                return "Thread Error";
			}
			else
				return "";
        }
        else
        {
            return "Offline";
        }
    }
    // ****************************************************************************************************
   


    // ****************************************************************************************************
    // Publish a text, Array or Hashtable message to the Pubnub Channel
    // ****************************************************************************************************
    public void Publish(string channel, object theMessage, stringCallback theCallback)
    {
        Thread oThread = new Thread(new ThreadStart(() => { PublishThread(channel, theMessage, theCallback); }));
        oThread.Start();
    }
    private void PublishThread(string channel, object theMessage, stringCallback theCallback)
    {
        string output = "";
        string queryString = "";

        try
        {
            queryString = "publish/" + pubnubPubKey + "/" + pubnubSubKey + "/0/" + channel + "/0/";
            if (theMessage is string)
            {
                queryString += "\"" + (string)theMessage + "\"";
            }
            else if ((theMessage is ArrayList) || (theMessage is Hashtable))
            {
                queryString += JSON.JsonEncode(theMessage);
            }
            else // Probably a number
            {
                queryString += theMessage.ToString();
            }

            WebRequest objRequest = (HttpWebRequest)WebRequest.Create(PubnubURL + queryString);

            WebResponse objResponse = (WebResponse)objRequest.GetResponse();
            using (StreamReader sr = new StreamReader(objResponse.GetResponseStream()))
            {
                output += sr.ReadToEnd();
                sr.Close();
            }
        }
        catch (Exception e)
        {
            output = "error";
        }

        theCallback(output);
    }
    // ****************************************************************************************************



    // ****************************************************************************************************
    // Subscribe to a Pubnub Channel
    // ****************************************************************************************************
    public void Subscribe(string channel, stringCallback theCallback)
    {
        if (!threadPool.ContainsKey(channel))
        {
            Thread oThread = new Thread(new ThreadStart(() => { SubscribeThread(channel, theCallback); }));
			oThread.IsBackground = true;
            threadPool.Add(channel, oThread);
            oThread.Start();
        }
        else
        {
            Thread oThread = (Thread)threadPool[channel];
            if (!oThread.IsAlive)
            {
                threadPool.Remove(channel);
                oThread = new Thread(new ThreadStart(() => { SubscribeThread(channel, theCallback); }));
				oThread.IsBackground = true;
                threadPool.Add(channel, oThread);
                oThread.Start();
            }
        }
    }

    private void SubscribeThread(string channel, stringCallback theCallback)
    {
        string output = "";
        string queryString = "";
        string timeToken = "0";
		Debug.Log ("Thread " + channel + " started");

        while (!stopped && threadPool.ContainsKey (channel))
        {
            try
            {
                // Create the Query URL
                queryString = "subscribe/" + pubnubSubKey + "/" + channel + "/0/" + timeToken;
                WebRequest objRequest = (HttpWebRequest)WebRequest.Create(PubnubURL + queryString);
			
				objRequest.Timeout = 10000;
			
                // Send the URL and get the Response
                WebResponse objResponse = (WebResponse)objRequest.GetResponse();

                // Get the Response.  Note that this will pause here if no new messages are waiting, and keep
                // the http channel open.
                output = "";
                using (StreamReader sr = new StreamReader(objResponse.GetResponseStream()))
                {
                    output += sr.ReadToEnd();
                    sr.Close();
                }

                // Convert it to a form we can work with, namely an Array with the first element an Array of messages
                // And the second element the timeToken
                ArrayList outputArray = (ArrayList)JSON.JsonDecode(output);
                if (outputArray != null)
                {
                    // The timeToken, used to make sure we get new messages next time around
                    timeToken = (string)outputArray[1];

                    // The messages
                    ArrayList messageArray = (ArrayList)outputArray[0];

                    // Call the Callback function for each message, turning it into text on the way if necessary
                    foreach (object message in messageArray)
                    {
                        if (message is string)
                        {
                            theCallback((string)message);
                        }
                        else if ((message is Hashtable) || (message is ArrayList))
                        {
                            theCallback(JSON.JsonEncode(message));
                        }
                        else // Probably a number
                        {
                            theCallback(message.ToString());
                        }
                    }
                }
            }
			catch (WebException w)
			{
				//Debug.Log ("Time out");
			}
            catch (Exception e)
            {
                //theCallback(e.Message);
			}

            // Give some other threads a chance
            Thread.Sleep(100);
        }
		Debug.Log ("Thread " + channel + " stopped");
    }
    // ****************************************************************************************************



    // ****************************************************************************************************
    // Kill the thread so no more messages are received from this channel
    // ****************************************************************************************************	
    public void Unsubscribe(string channel)
    {
        if (threadPool.ContainsKey(channel))
        {
            Thread oThread = (Thread)threadPool[channel];
            threadPool.Remove(channel);
        }
    }
    // ****************************************************************************************************



    // ****************************************************************************************************
    // Kill all the Threads - call this when the Application Quits to make sure there are no odd threads
    // lying around.
    // ****************************************************************************************************
    public void KillAll()
    {
		stopped = true;
		
		bool allStopped = false;
		
		while (!allStopped)
		{
			allStopped = true;
			foreach (string theKey in threadPool.Keys)
	        {
	            Thread oThread = (Thread)threadPool[theKey];
	            if (oThread.IsAlive) allStopped = false;
	        }
		}
		
        threadPool.Clear();
    }
    // ****************************************************************************************************
}
