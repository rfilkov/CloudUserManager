using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System;
using System.IO;
using System.Net;
using System.Net.Security;

public class WebTools 
{

	// calls web service with url, content and headers
	public static WWW CallWebService(string requestUrl, string contentType, byte[] content, Dictionary<string, string> headers, bool bAwaitResponse, bool bStopOnError)
	{
		headers.Add("Content-Type", contentType);
		headers.Add("Content-Length", content.Length.ToString());

		WWW www = new WWW(requestUrl, content, headers);

		while(bAwaitResponse && !www.isDone)
		{
			System.Threading.Thread.Sleep(20);
		}
		
		if (bStopOnError && !string.IsNullOrEmpty(www.error)) 
		{
			throw new Exception(www.error + " - " + requestUrl);
		}

		return www;
	}

	// returns the response status code
	public static int GetStatusCode(WWW request)
	{
		int status = -1;

		if(request != null && request.responseHeaders != null && request.responseHeaders.ContainsKey("STATUS"))
		{
			string statusLine = request.responseHeaders["STATUS"];
			string[] statusComps = statusLine.Split(' ');

			if (statusComps.Length >= 3)
			{
				int.TryParse(statusComps[1], out status);
			}
		}

		return status;
	}

	// checks if the response status is error
	public static bool IsErrorStatus(WWW request)
	{
		int status = GetStatusCode(request);
		return (status >= 300);
	}

	// returns the response status message
	public static string GetStatusMessage(WWW request)
	{
		string message = string.Empty;
		
		if(request != null && request.responseHeaders != null && request.responseHeaders.ContainsKey("STATUS"))
		{
			string statusLine = request.responseHeaders["STATUS"];
			string[] statusComps = statusLine.Split(' ');

			for(int i = 2; i < statusComps.Length; i++)
			{
				message += " " + statusComps[i];
			}
		}
		
		return message.Trim();
	}
	

	// calls web service with url, content and headers
	public static HttpWebResponse DoWebRequest(string requestUrl, string method, string contentType, byte[] content, Dictionary<string, string> headers, bool bAwaitResponse, bool bStopOnError)
	{
		ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
		HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(requestUrl);
		webRequest.ContentType = contentType;
		webRequest.ContentLength = content.Length;

		foreach(string hName in headers.Keys)
		{
			webRequest.Headers.Add(hName, headers[hName]);
		}

		webRequest.Method = !string.IsNullOrEmpty(method) ? method : "POST";
		
		//using (StreamWriter streamWriter = new StreamWriter(webRequest.GetRequestStream()))
		using (Stream stream = webRequest.GetRequestStream())
		{
			stream.Write(content, 0, content.Length);
		}

		HttpWebResponse httpResponse = (HttpWebResponse)webRequest.GetResponse();
		using (StreamReader streamReader = new StreamReader(httpResponse.GetResponseStream()))
		{
			string responseText = streamReader.ReadToEnd();
			//Now you have your response.
			//or false depending on information in the response
			Debug.Log(responseText);            
		}

//		WWW www = new WWW(requestUrl, content, headers);
//		
//		while(bAwaitResponse && !www.isDone)
//		{
//			System.Threading.Thread.Sleep(20);
//		}
//		
//		if (bStopOnError && !string.IsNullOrEmpty(www.error)) 
//		{
//			throw new Exception(www.error + " - " + requestUrl);
//		}

		return httpResponse;
	}

	public static bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) 
	{
		bool isOk = true;

		// If there are errors in the certificate chain, look at each error to determine the cause.
		if (sslPolicyErrors != SslPolicyErrors.None) 
		{
			for (int i = 0; i < chain.ChainStatus.Length; i++) 
			{
				if (chain.ChainStatus [i].Status != X509ChainStatusFlags.RevocationStatusUnknown) 
				{
					chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
					chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
					chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan (0, 1, 0);
					chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
					bool chainIsValid = chain.Build ((X509Certificate2)certificate);

					if (!chainIsValid) 
					{
						isOk = false;
					}
				}
			}
		}
		return isOk;
	}
}
