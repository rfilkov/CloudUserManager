using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class TestFaceAPI : MonoBehaviour 
{
	public string faceImageFile;
	public string faceApiKey;

	private string url = "http://images.earthcam.com/ec_metros/ourcams/fridays.jpg";

	private const string ServiceHost = "https://api.projectoxford.ai/face/v1.0";
	private const string DetectQuery = "detect";


//	IEnumerator Start() 
//	{
//		return downloadImage();
//	}
	
	
	void Start() 
	{
		//return downloadImage();

		if(!string.IsNullOrEmpty(faceImageFile) && File.Exists(faceImageFile))
		{
			byte[] imageBytes = File.ReadAllBytes(faceImageFile);
			callFaceDetect(imageBytes);
		}

	}


	IEnumerator downloadImage()
	{
		WWW www = new WWW(url);
		yield return www;
		
		if(string.IsNullOrEmpty(www.error))
		{
			Renderer renderer = GetComponent<Renderer>();
			renderer.material.mainTexture = www.texture;
		}
		else
		{
			Debug.LogError(www.error + " - " + www.url);
		}
	}


	string callFaceDetect(byte[] content)
	{
		string requestUrl = string.Format("{0}/{1}?returnFaceId={2}&returnFaceLandmarks={3}", ServiceHost, DetectQuery, true, false);

//		// Create a Web Form
//		WWWForm form = new WWWForm();
//		form.AddBinaryData("fileUpload", bytes, "image.png", "image/png");
//		
//		// Upload to a cgi script
//		WWW www = new WWW(requestUrl, form);
//		yield return www;

		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", faceApiKey);
		headers.Add("Content-Type", "application/octet-stream");  // application/x-www-form-urlencoded
		headers.Add("Content-Length", content.Length.ToString());

		WWW www = new WWW(requestUrl, content, headers);
		//yield return www;

		// wait for response
		while(!www.isDone)
		{
			System.Threading.Thread.Sleep(20);
		}

		// parse response
		Debug.Log(www.text);

		if (!string.IsNullOrEmpty(www.error)) 
		{
			print(www.error);
		}
		else 
		{
			print("Finished Uploading Screenshot");
		}

		return www.text;
	}

}
