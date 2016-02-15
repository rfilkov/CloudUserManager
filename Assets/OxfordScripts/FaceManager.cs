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
	[Tooltip("Subscription key for Face API.")]
	public string faceSubscriptionKey;
	
	[Tooltip("Subscription key for Emotion API.")]
	public string emotionSubscriptionKey;

	private const string FaceServiceHost = "https://api.projectoxford.ai/face/v1.0";
	private const string EmotionServiceHost = "https://api.projectoxford.ai/emotion/v1.0";

	private static FaceManager instance = null;
	private bool isInitialized = false;


	void Awake() 
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
		                                  FaceServiceHost, true, false, "age,gender,smile,headPose");
		
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


	/// <summary>
	/// Recognizes the emotions.
	/// </summary>
	/// <returns>The array of recognized emotions.</returns>
	/// <param name="texImage">Image texture.</param>
	/// <param name="faceRects">Detected face rectangles, or null.</param>
	public Emotion[] RecognizeEmotions(Texture2D texImage, FaceRectangle[] faceRects)
	{
		if(texImage == null)
			return null;
		
		byte[] imageBytes = texImage.EncodeToJPG();
		return RecognizeEmotions(imageBytes, faceRects);
	}


	/// <summary>
	/// Recognizes the emotions.
	/// </summary>
	/// <returns>The array of recognized emotions.</returns>
	/// <param name="imageBytes">Image bytes.</param>
	/// <param name="faceRects">Detected face rectangles, or null.</param>
	public Emotion[] RecognizeEmotions(byte[] imageBytes, FaceRectangle[] faceRects)
	{
		if(string.IsNullOrEmpty(emotionSubscriptionKey))
		{
			throw new Exception("The emotion-subscription key is not set.");
		}
		
		StringBuilder faceRectsStr = new StringBuilder();
		if(faceRects != null)
		{
			foreach(FaceRectangle rect in faceRects)
			{
				faceRectsStr.AppendFormat("{0},{1},{2},{3};", rect.Left, rect.Top, rect.Width, rect.Height);
			}
			
			faceRectsStr.Remove(faceRectsStr.Length - 1, 1); // drop the last semicolon
		}
		
		string requestUrl = string.Format("{0}/recognize??faceRectangles={1}", EmotionServiceHost, faceRectsStr);
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", emotionSubscriptionKey);
		
		HttpWebResponse response = WebTools.DoWebRequest(requestUrl, "POST", "application/octet-stream", imageBytes, headers, true, false);
		
		Emotion[] emotions = null;
		if(!WebTools.IsErrorStatus(response))
		{
			StreamReader reader = new StreamReader(response.GetResponseStream());
			emotions = JsonConvert.DeserializeObject<Emotion[]>(reader.ReadToEnd(), jsonSettings);
		}
		else
		{
			ProcessFaceError(response);
		}
		
		return emotions;
	}
	
	
	/// <summary>
	/// Matches the recognized emotions to faces.
	/// </summary>
	/// <returns>The number of matched emotions.</returns>
	/// <param name="faces">Array of detected Faces.</param>
	/// <param name="emotions">Array of recognized Emotions.</param>
	public int MatchEmotionsToFaces(ref Face[] faces, ref Emotion[] emotions)
	{
		int matched = 0;
		if(faces == null || emotions == null)
			return matched;
		
		foreach(Emotion emot in emotions)
		{
			FaceRectangle emotRect = emot.FaceRectangle;
			
			for(int i = 0; i < faces.Length; i++)
			{
				if(Mathf.Abs(emotRect.Left - faces[i].FaceRectangle.Left) <= 2 &&
				   Mathf.Abs(emotRect.Top - faces[i].FaceRectangle.Top) <= 2)
				{
					faces[i].Emotion = emot;
					matched++;
					break;
				}
			}
		}
		
		return matched;
	}
	
	
	/// <summary>
	/// Gets the emotion scores as string.
	/// </summary>
	/// <returns>The emotion as string.</returns>
	/// <param name="emotion">Emotion.</param>
	public static string GetEmotionScoresAsString(Emotion emotion)
	{
		if(emotion == null || emotion.Scores == null)
			return string.Empty;
		
		Scores es = emotion.Scores; 
		StringBuilder emotStr = new StringBuilder();
		
		if(es.Anger >= 0.01f) emotStr.AppendFormat(" {0:F0}% angry,", es.Anger * 100f);
		if(es.Contempt >= 0.01f) emotStr.AppendFormat(" {0:F0}% contemptuous,", es.Contempt * 100f);
		if(es.Disgust >= 0.01f) emotStr.AppendFormat(" {0:F0}% disgusted,", es.Disgust * 100f);
		if(es.Fear >= 0.01f) emotStr.AppendFormat(" {0:F0}% scared,", es.Fear * 100f);
		if(es.Happiness >= 0.01f) emotStr.AppendFormat(" {0:F0}% happy,", es.Happiness * 100f);
		if(es.Neutral >= 0.01f) emotStr.AppendFormat(" {0:F0}% neutral,", es.Neutral * 100f);
		if(es.Sadness >= 0.01f) emotStr.AppendFormat(" {0:F0}% sad,", es.Sadness * 100f);
		if(es.Surprise >= 0.01f) emotStr.AppendFormat(" {0:F0}% surprised,", es.Surprise * 100f);
		
		if(emotStr.Length > 0)
		{
			emotStr.Remove(0, 1);
			emotStr.Remove(emotStr.Length - 1, 1);
		}
		
		return emotStr.ToString();
	}
	
	
	/// <summary>
	/// Gets the emotion scores as list of strings.
	/// </summary>
	/// <returns>The emotion as string.</returns>
	/// <param name="emotion">Emotion.</param>
	public static List<string> GetEmotionScoresList(Emotion emotion)
	{
		List<string> alScores = new List<string>();
		if(emotion == null || emotion.Scores == null)
			return alScores;
		
		Scores es = emotion.Scores; 
		
		if(es.Anger >= 0.01f) alScores.Add(string.Format("{0:F0}% angry", es.Anger * 100f));
		if(es.Contempt >= 0.01f) alScores.Add(string.Format("{0:F0}% contemptuous", es.Contempt * 100f));
		if(es.Disgust >= 0.01f) alScores.Add(string.Format("{0:F0}% disgusted,", es.Disgust * 100f));
		if(es.Fear >= 0.01f) alScores.Add(string.Format("{0:F0}% scared", es.Fear * 100f));
		if(es.Happiness >= 0.01f) alScores.Add(string.Format("{0:F0}% happy", es.Happiness * 100f));
		if(es.Neutral >= 0.01f) alScores.Add(string.Format("{0:F0}% neutral", es.Neutral * 100f));
		if(es.Sadness >= 0.01f) alScores.Add(string.Format("{0:F0}% sad", es.Sadness * 100f));
		if(es.Surprise >= 0.01f) alScores.Add(string.Format("{0:F0}% surprised", es.Surprise * 100f));
		
		return alScores;
	}


	/// <summary>
	/// Gets the standard face colors.
	/// </summary>
	/// <returns>The face colors.</returns>
	public static Color[] GetFaceColors()
	{
		Color[] faceColors = new Color[5];

		faceColors[0] = Color.green;
		faceColors[1] = Color.yellow;
		faceColors[2] = Color.cyan;
		faceColors[3] = Color.magenta;
		faceColors[4] = Color.red;

		return faceColors;
	}


	/// <summary>
	/// Gets the standard face color names.
	/// </summary>
	/// <returns>The face color names.</returns>
	public static string[] GetFaceColorNames()
	{
		string[] faceColorNames = new string[5];

		faceColorNames[0] = "Green";
		faceColorNames[1] = "Yellow";
		faceColorNames[2] = "Cyan";
		faceColorNames[3] = "Magenta";
		faceColorNames[4] = "Red";

		return faceColorNames;
	}
	
	
	// draw face rectangles
	/// <summary>
	/// Draws the face rectacgles in the given texture.
	/// </summary>
	/// <param name="faces">List of faces.</param>
	/// <param name="tex">Tex.</param>
	public static void DrawFaceRects(Texture2D tex, Face[] faces, Color[] faceColors)
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
		
		string requestUrl = string.Format("{0}/persongroups/{1}", FaceServiceHost, groupId);
		
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
	/// Gets the person group.
	/// </summary>
	/// <returns>The person group.</returns>
	/// <param name="groupId">Group ID.</param>
	public PersonGroup GetPersonGroup(string groupId)
	{
		if(string.IsNullOrEmpty(faceSubscriptionKey))
		{
			throw new Exception("The face-subscription key is not set.");
		}
		
		string requestUrl = string.Format("{0}/persongroups/{1}", FaceServiceHost, groupId);
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", faceSubscriptionKey);
		
		HttpWebResponse response = WebTools.DoWebRequest(requestUrl, "GET", "application/json", null, headers, true, false);
		
		PersonGroup group = null;
		if(!WebTools.IsErrorStatus(response))
		{
			StreamReader reader = new StreamReader(response.GetResponseStream());
			group = JsonConvert.DeserializeObject<PersonGroup>(reader.ReadToEnd(), jsonSettings);
		}
		else
		{
			ProcessFaceError(response);
		}
		
		return group;
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
		
		string requestUrl = string.Format("{0}/persongroups/{1}/persons", FaceServiceHost, groupId);
		
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
		
		string requestUrl = string.Format("{0}/persongroups/{1}/persons", FaceServiceHost, groupId);
		
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

			//if(person.PersonId != null)
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
	/// Gets the person data.
	/// </summary>
	/// <returns>The person data.</returns>
	/// <param name="groupId">Group ID.</param>
	/// <param name="personId">Person ID.</param>
	public Person GetPerson(string groupId, string personId)
	{
		if(string.IsNullOrEmpty(faceSubscriptionKey))
		{
			throw new Exception("The face-subscription key is not set.");
		}
		
		string requestUrl = string.Format("{0}/persongroups/{1}/persons/{2}", FaceServiceHost, groupId, personId);
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", faceSubscriptionKey);
		
		HttpWebResponse response = WebTools.DoWebRequest(requestUrl, "GET", "application/json", null, headers, true, false);
		
		Person person = null;
		if(!WebTools.IsErrorStatus(response))
		{
			StreamReader reader = new StreamReader(response.GetResponseStream());
			person = JsonConvert.DeserializeObject<Person>(reader.ReadToEnd(), jsonSettings);
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
		string requestUrl = string.Format("{0}/persongroups/{1}/persons/{2}/persistedFaces?userData={3}&targetFace={4}", FaceServiceHost, groupId, personId, userData, sFaceRect);
		
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
		
		string requestUrl = string.Format("{0}/persongroups/{1}/train", FaceServiceHost, groupId);
		
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
		
		string requestUrl = string.Format("{0}/persongroups/{1}/training", FaceServiceHost, groupId);
		
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
	

	/// <summary>
	/// Identifies the given faces.
	/// </summary>
	/// <returns>Array of identification results.</returns>
	/// <param name="groupId">Group ID.</param>
	/// <param name="faces">Array of detected faces.</param>
	/// <param name="maxCandidates">Maximum allowed candidates pro face.</param>
	public IdentifyResult[] IdentifyFaces(string groupId, ref Face[] faces, int maxCandidates)
	{
		if(string.IsNullOrEmpty(faceSubscriptionKey))
		{
			throw new Exception("The face-subscription key is not set.");
		}

		Guid[] faceIds = new Guid[faces.Length];
		for(int i = 0; i < faces.Length; i++)
		{
			faceIds[i] = faces[i].FaceId;
		}

		if(maxCandidates <= 0)
		{
			maxCandidates = 1;
		}
		
		string requestUrl = string.Format("{0}/identify", FaceServiceHost);
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", faceSubscriptionKey);
		
		string sJsonContent = JsonConvert.SerializeObject(new { personGroupId = groupId, faceIds = faceIds, maxNumOfCandidatesReturned = maxCandidates }, jsonSettings);
		byte[] btContent = Encoding.UTF8.GetBytes(sJsonContent);
		HttpWebResponse response = WebTools.DoWebRequest(requestUrl, "POST", "application/json", btContent, headers, true, false);
		
		IdentifyResult[] results = null;
		if(!WebTools.IsErrorStatus(response))
		{
			StreamReader reader = new StreamReader(response.GetResponseStream());
			results = JsonConvert.DeserializeObject<IdentifyResult[]>(reader.ReadToEnd(), jsonSettings);
		}
		else
		{
			ProcessFaceError(response);
		}
		
		return results;
	}
	

	/// <summary>
	/// Matchs the identity candidates to faces.
	/// </summary>
	/// <returns>The number of matched identities.</returns>
	/// <param name="faces">Array of detected faces.</param>
	/// <param name="identities">Array of recognized identities.</param>
	public int MatchCandidatesToFaces(ref Face[] faces, ref IdentifyResult[] identities, string groupId)
	{
		int matched = 0;
		if(faces == null || identities == null)
			return matched;

		// clear face identities
		for(int i = 0; i < faces.Length; i++)
		{
			faces[i].Candidate = null;
		}

		foreach(IdentifyResult ident in identities)
		{
			Guid faceId = ident.FaceId;
			
			for(int i = 0; i < faces.Length; i++)
			{
				if(faces[i].FaceId == faceId)
				{
					if(ident.Candidates != null && ident.Candidates.Length > 0)
					{
						faces[i].Candidate = ident.Candidates[0];

						if(faces[i].Candidate != null)
						{
							faces[i].Candidate.Person = GetPerson(groupId, faces[i].Candidate.PersonId.ToString());
						}
					}

					matched++;
					break;
				}
			}
		}
		
		return matched;
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
			string sErrorMsg = !string.IsNullOrEmpty(ex.error.message) ? ex.error.message : ex.error.code;
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
