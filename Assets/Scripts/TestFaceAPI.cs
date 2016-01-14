using UnityEngine;
using System.Collections;

public class TestFaceAPI : MonoBehaviour 
{
	public string url = "http://images.earthcam.com/ec_metros/ourcams/fridays.jpg";

	IEnumerator Start() 
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

	void Update () 
	{
	
	}
}
