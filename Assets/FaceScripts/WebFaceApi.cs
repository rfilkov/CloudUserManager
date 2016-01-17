using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class WebFaceApi
{
	private const string ServiceHost = "https://api.projectoxford.ai/face/v1.0";
	private const string DetectQuery = "detect";
	

	public static Face[] FaceDetect(string apiKey, byte[] imageBytes)
	{
		string requestUrl = string.Format("{0}/{1}?returnFaceId={2}&returnFaceLandmarks={3}&returnFaceAttributes={4}", 
		                                  ServiceHost, DetectQuery, true, false, "age,gender,headPose,smile,facialHair");
		
		Dictionary<string, string> headers = new Dictionary<string, string>();
		headers.Add("ocp-apim-subscription-key", apiKey);
		
		WWW www = WebTools.CallWebService(requestUrl, "application/octet-stream", imageBytes, headers);
		Face[] faces = JsonConvert.DeserializeObject<Face[]>(www.text, jsonSettings);

		return faces;
	}
	
	private static JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
	{
		DateFormatHandling = DateFormatHandling.IsoDateFormat,
		NullValueHandling = NullValueHandling.Ignore,
		ContractResolver = new CamelCasePropertyNamesContractResolver()
	};
	
}
