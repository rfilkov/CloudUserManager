using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class WebTools 
{

	public static WWW CallWebService(string requestUrl, string contentType, byte[] content, Dictionary<string, string> headers)
	{
		headers.Add("Content-Type", contentType);
		headers.Add("Content-Length", content.Length.ToString());
		
		WWW www = new WWW(requestUrl, content, headers);

		while(!www.isDone)
		{
			System.Threading.Thread.Sleep(20);
		}
		
		if (!string.IsNullOrEmpty(www.error)) 
		{
			throw new Exception(www.error + " - " + requestUrl);
		}

		return www;
	}

}
