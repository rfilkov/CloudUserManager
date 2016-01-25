using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Text;
using System;


public class FaceManager : MonoBehaviour 
{

	public string faceSubscriptionKey;

	private const string ServiceHost = "https://api.projectoxford.ai/face/v1.0";
	private static FaceManager instance = null;
	private bool isInitialized = false;


	void Start () 
	{
		instance = this;

		if(string.IsNullOrEmpty(faceSubscriptionKey))
		{
			throw new Exception("Please set your face-subscription key.");
		}

		isInitialized = true;
	}

	/// <summary>
	/// Gets the FaceManager instance.
	/// </summary>
	/// <value>The FaceManager instance.</value>
	public static FaceManager Instance
	{
		get
		{
			return instance;
		}
	}


	/// <summary>
	/// Determines whether the FaceManager is initialized.
	/// </summary>
	/// <returns><c>true</c> if the FaceManager is initialized; otherwise, <c>false</c>.</returns>
	public bool IsInitialized()
	{
		return isInitialized;
	}


	/// <summary>
	/// Detects the faces in the given image.
	/// </summary>
	/// <returns>List of detected faces.</returns>
	/// <param name="texImage">Image texture.</param>
	public Face[] DetectFaces(Texture2D texImage)
	{
		if(texImage == null)
			return null;

		byte[] imageBytes = texImage.EncodeToJPG();
		return DetectFaces(imageBytes);
	}
	
	
	/// <summary>
	/// Detects the faces in the given image.
	/// </summary>
	/// <returns>List of detected faces.</returns>
	/// <param name="imageBytes">Image bytes.</param>
	public Face[] DetectFaces(byte[] imageBytes)
	{
		if(string.IsNullOrEmpty(faceSubscriptionKey))
		{
			throw new Exception("The face-subscription key is not set.");
		}

		string requestUrl = string.Format("{0}/detect?returnFaceId={1}&returnFaceLandmarks={2}&returnFaceAttributes={3}", 
		                                  ServiceHost, true, false, "age,gender,smile,headPose");
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", faceSubscriptionKey);
		
		WWW www = WebTools.CallWebService(requestUrl, "application/octet-stream", imageBytes, headers, true, false);
		
		Face[] faces = null;
		if(!WebTools.IsErrorStatus(www))
		{
			faces = JsonConvert.DeserializeObject<Face[]>(www.text, jsonSettings);
		}
		else
		{
			ProcessFaceError(www);
		}
		
		return faces;
	}


	// draw face rectangles
	/// <summary>
	/// Draws the face rectacgles in the given texture.
	/// </summary>
	/// <param name="faces">List of faces.</param>
	/// <param name="tex">Tex.</param>
	public void DrawFaceRects(Texture2D tex, Face[] faces, Color[] faceColors)
	{
		for(int i = 0; i < faces.Length; i++)
		{
			Face face = faces[i];
			Color faceColor = faceColors[i % faceColors.Length];
			
			FaceRectangle rect = face.FaceRectangle;
			TexTools.DrawRect(tex, rect.Left, rect.Top, rect.Width, rect.Height, faceColor);
		}
		
		tex.Apply();
	}
	

	/// <summary>
	/// Creates a person group.
	/// </summary>
	/// <returns><c>true</c>, if person group was created, <c>false</c> otherwise.</returns>
	/// <param name="pgId">Group identifier.</param>
	/// <param name="name">Group name.</param>
	/// <param name="userData">User data.</param>
	public bool CreatePersonGroup(string pgId, string groupName, string userData)
	{
		if(string.IsNullOrEmpty(faceSubscriptionKey))
		{
			throw new Exception("The face-subscription key is not set.");
		}
		
		string requestUrl = string.Format("{0}/persongroups/{1}", ServiceHost, pgId);
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", faceSubscriptionKey);

		string sJsonContent = JsonConvert.SerializeObject(new { name = name, userData = userData }, jsonSettings);
		byte[] btContent = Encoding.UTF8.GetBytes(sJsonContent);
		var www = WebTools.DoWebRequest(requestUrl, "PUT", "application/json", btContent, headers, true, false);
		
//		if(WebTools.IsErrorStatus(www))
//		{
//			ProcessFaceError(www);
//			return false;
//		}
		
		return true;
	}
	

	// processes the error status in response
	private void ProcessFaceError(WWW www)
	{
		ClientError ex = JsonConvert.DeserializeObject<ClientError>(www.text);
		
		if (ex.error != null && ex.error.code != null)
		{
			string sErrorMsg = !string.IsNullOrEmpty(ex.error.code) && ex.error.code != "Unspecified" ?
				ex.error.code + " - " + ex.error.message : ex.error.message;
			throw new System.Exception(sErrorMsg);
		}
		else
		{
			ServiceError serviceEx = JsonConvert.DeserializeObject<ServiceError>(www.text);
			
			if (serviceEx != null && serviceEx.statusCode != null)
			{
				string sErrorMsg = !string.IsNullOrEmpty(serviceEx.statusCode) && serviceEx.statusCode != "Unspecified" ?
					serviceEx.statusCode + " - " + serviceEx.message : serviceEx.message;
				throw new System.Exception(sErrorMsg);
			}
			else
			{
				throw new System.Exception("Error " + WebTools.GetStatusCode(www) + ": " + WebTools.GetStatusMessage(www) + "; Url: " + www.url);
			}
		}
	}
	
	
	private JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
	{
		DateFormatHandling = DateFormatHandling.IsoDateFormat,
		NullValueHandling = NullValueHandling.Ignore,
		ContractResolver = new CamelCasePropertyNamesContractResolver()
	};


}
