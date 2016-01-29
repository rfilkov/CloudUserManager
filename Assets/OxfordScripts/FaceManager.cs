using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Net;
using System.IO;


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
		
		HttpWebResponse response = WebTools.DoWebRequest(requestUrl, "POST", "application/octet-stream", imageBytes, headers, true, false);
		
		Face[] faces = null;
		if(!WebTools.IsErrorStatus(response))
		{
			StreamReader reader = new StreamReader(response.GetResponseStream());
			faces = JsonConvert.DeserializeObject<Face[]>(reader.ReadToEnd(), jsonSettings);
		}
		else
		{
			ProcessFaceError(response);
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
	/// <param name="groupId">Person-group ID.</param>
	/// <param name="name">Group name (max 128 chars).</param>
	/// <param name="userData">User data (max 16K).</param>
	public bool CreatePersonGroup(string groupId, string groupName, string userData)
	{
		if(string.IsNullOrEmpty(faceSubscriptionKey))
		{
			throw new Exception("The face-subscription key is not set.");
		}
		
		string requestUrl = string.Format("{0}/persongroups/{1}", ServiceHost, groupId);
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", faceSubscriptionKey);

		string sJsonContent = JsonConvert.SerializeObject(new { name = groupName, userData = userData }, jsonSettings);
		byte[] btContent = Encoding.UTF8.GetBytes(sJsonContent);
		HttpWebResponse response = WebTools.DoWebRequest(requestUrl, "PUT", "application/json", btContent, headers, true, false);
		
		if(WebTools.IsErrorStatus(response))
		{
			ProcessFaceError(response);
			return false;
		}
		
		return true;
	}
	

	/// <summary>
	/// Lists the people in a person-group.
	/// </summary>
	/// <returns>The people in group.</returns>
	/// <param name="groupId">Person-group ID.</param>
	public Person[] ListPersonsInGroup(string groupId)
	{
		if(string.IsNullOrEmpty(faceSubscriptionKey))
		{
			throw new Exception("The face-subscription key is not set.");
		}
		
		string requestUrl = string.Format("{0}/persongroups/{1}/persons", ServiceHost, groupId);
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", faceSubscriptionKey);
		
		HttpWebResponse response = WebTools.DoWebRequest(requestUrl, "GET", "application/json", null, headers, true, false);

		Person[] persons = null;
		if(!WebTools.IsErrorStatus(response))
		{
			StreamReader reader = new StreamReader(response.GetResponseStream());
			persons = JsonConvert.DeserializeObject<Person[]>(reader.ReadToEnd(), jsonSettings);
		}
		else
		{
			ProcessFaceError(response);
		}

		return persons;
	}
	

	/// <summary>
	/// Adds the person to a group.
	/// </summary>
	/// <returns>The person to group.</returns>
	/// <param name="groupId">Person-group ID.</param>
	/// <param name="personName">Person name (max 128 chars).</param>
	/// <param name="userData">User data (max 16K).</param>
	public Person AddPersonToGroup(string groupId, string personName, string userData)
	{
		if(string.IsNullOrEmpty(faceSubscriptionKey))
		{
			throw new Exception("The face-subscription key is not set.");
		}
		
		string requestUrl = string.Format("{0}/persongroups/{1}/persons", ServiceHost, groupId);
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", faceSubscriptionKey);
		
		string sJsonContent = JsonConvert.SerializeObject(new { name = personName, userData = userData }, jsonSettings);
		byte[] btContent = Encoding.UTF8.GetBytes(sJsonContent);
		HttpWebResponse response = WebTools.DoWebRequest(requestUrl, "POST", "application/json", btContent, headers, true, false);
		
		Person person = null;
		if(!WebTools.IsErrorStatus(response))
		{
			StreamReader reader = new StreamReader(response.GetResponseStream());
			person = JsonConvert.DeserializeObject<Person>(reader.ReadToEnd(), jsonSettings);

			if(person.PersonId != null)
			{
				person.Name = personName;
				person.UserData = userData;
			}
		}
		else
		{
			ProcessFaceError(response);
		}

		return person;
	}
	

	/// <summary>
	/// Adds the face to a person in a person-group.
	/// </summary>
	/// <returns>The persisted face (only faceId is set).</returns>
	/// <param name="groupId">Person-group ID.</param>
	/// <param name="personId">Person ID.</param>
	/// <param name="userData">User data.</param>
	/// <param name="faceRect">Face rect or null.</param>
	/// <param name="imageBytes">Image bytes.</param>
	public PersonFace AddFaceToPerson(string groupId, string personId, string userData, FaceRectangle faceRect, Texture2D texImage)
	{
		if(texImage == null)
			return null;
		
		byte[] imageBytes = texImage.EncodeToJPG();
		return AddFaceToPerson(groupId, personId, userData, faceRect, imageBytes);
	}
	
	
	/// <summary>
	/// Adds the face to a person in a person-group.
	/// </summary>
	/// <returns>The persisted face (only faceId is set).</returns>
	/// <param name="groupId">Person-group ID.</param>
	/// <param name="personId">Person ID.</param>
	/// <param name="userData">User data.</param>
	/// <param name="faceRect">Face rect or null.</param>
	/// <param name="imageBytes">Image bytes.</param>
	public PersonFace AddFaceToPerson(string groupId, string personId, string userData, FaceRectangle faceRect, byte[] imageBytes)
	{
		if(string.IsNullOrEmpty(faceSubscriptionKey))
		{
			throw new Exception("The face-subscription key is not set.");
		}

		string sFaceRect = faceRect != null ? string.Format("{0},{1},{2},{3}", faceRect.Left, faceRect.Top, faceRect.Width, faceRect.Height) : string.Empty;
		string requestUrl = string.Format("{0}/persongroups/{1}/persons/{2}/persistedFaces?userData={3}&targetFace={4}", ServiceHost, groupId, personId, userData, sFaceRect);
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", faceSubscriptionKey);
		
		HttpWebResponse response = WebTools.DoWebRequest(requestUrl, "POST", "application/octet-stream", imageBytes, headers, true, false);
		
		PersonFace face = null;
		if(!WebTools.IsErrorStatus(response))
		{
			StreamReader reader = new StreamReader(response.GetResponseStream());
			face = JsonConvert.DeserializeObject<PersonFace>(reader.ReadToEnd(), jsonSettings);
		}
		else
		{
			ProcessFaceError(response);
		}
		
		return face;
	}


	/// <summary>
	/// Trains the person-group.
	/// </summary>
	/// <returns><c>true</c>, if person-group training was successfully started, <c>false</c> otherwise.</returns>
	/// <param name="groupId">Group identifier.</param>
	public bool TrainPersonGroup(string groupId)
	{
		if(string.IsNullOrEmpty(faceSubscriptionKey))
		{
			throw new Exception("The face-subscription key is not set.");
		}
		
		string requestUrl = string.Format("{0}/persongroups/{1}/train", ServiceHost, groupId);
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", faceSubscriptionKey);
		
		HttpWebResponse response = WebTools.DoWebRequest(requestUrl, "POST", "", null, headers, true, false);
		
		if(WebTools.IsErrorStatus(response))
		{
			ProcessFaceError(response);
			return false;
		}
		
		return true;
	}
	

	/// <summary>
	/// Determines whether the person-group's training is finished.
	/// </summary>
	/// <returns><c>true</c> if the person-group's training is finished; otherwise, <c>false</c>.</returns>
	/// <param name="groupId">Person-group ID.</param>
	public bool IsPersonGroupTrained(string groupId)
	{
		if(string.IsNullOrEmpty(faceSubscriptionKey))
		{
			throw new Exception("The face-subscription key is not set.");
		}
		
		string requestUrl = string.Format("{0}/persongroups/{1}/training", ServiceHost, groupId);
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", faceSubscriptionKey);
		
		HttpWebResponse response = WebTools.DoWebRequest(requestUrl, "GET", "", null, headers, true, false);
		
		TrainingStatus status = null;
		if(!WebTools.IsErrorStatus(response))
		{
			StreamReader reader = new StreamReader(response.GetResponseStream());
			status = JsonConvert.DeserializeObject<TrainingStatus>(reader.ReadToEnd(), jsonSettings);
		}
		else
		{
			ProcessFaceError(response);
		}

		bool bSuccess = (status != null && status.Status == Status.Succeeded);

		return bSuccess;
	}
	
	


	// --------------------------------------------------------------------------------- //
	
	// processes the error status in response
	private void ProcessFaceError(HttpWebResponse response)
	{
		StreamReader reader = new StreamReader(response.GetResponseStream());
		string responseText = reader.ReadToEnd();

		ClientError ex = JsonConvert.DeserializeObject<ClientError>(responseText);
		if (ex.error != null && ex.error.code != null)
		{
			string sErrorMsg = !string.IsNullOrEmpty(ex.error.code) && ex.error.code != "Unspecified" ?
				ex.error.code + " - " + ex.error.message : ex.error.message;
			throw new System.Exception(sErrorMsg);
		}
		else
		{
			ServiceError serviceEx = JsonConvert.DeserializeObject<ServiceError>(responseText);
			if (serviceEx != null && serviceEx.statusCode != null)
			{
				string sErrorMsg = !string.IsNullOrEmpty(serviceEx.statusCode) && serviceEx.statusCode != "Unspecified" ?
					serviceEx.statusCode + " - " + serviceEx.message : serviceEx.message;
				throw new System.Exception(sErrorMsg);
			}
			else
			{
				throw new System.Exception("Error " + WebTools.GetStatusCode(response) + ": " + WebTools.GetStatusMessage(response) + "; Url: " + response.ResponseUri);
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
