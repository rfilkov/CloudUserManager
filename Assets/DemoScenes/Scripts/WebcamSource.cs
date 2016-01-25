using UnityEngine;
using System.Collections;

public class WebcamSource : MonoBehaviour 
{
	public bool flipHorizontally = false;

	public string webcamName;
	public WebCamTexture webcamTex;


	private bool bTexResolutionSet = false;


	/// <summary>
	/// Gets webcam snapshot.
	/// </summary>
	public Texture2D GetSnapshot()
	{
		Texture2D snap = new Texture2D(webcamTex.width, webcamTex.height, TextureFormat.ARGB32, false);

		if(webcamTex)
		{
			snap.SetPixels(webcamTex.GetPixels());
			snap.Apply();
			
			if(flipHorizontally)
			{
				snap = TexTools.FlipTexture(snap);
			}
		}

		return snap;
	}
	

	void Start () 
	{
		WebCamDevice[] devices = WebCamTexture.devices;

		if(devices != null && devices.Length > 0)
		{
			webcamName = devices[0].name;
			webcamTex = new WebCamTexture(webcamName);
			
			GetComponent<Renderer>().material.mainTexture = webcamTex;
			bTexResolutionSet = false;

		}

		if(flipHorizontally)
		{
			Vector3 scale = transform.localScale;
			transform.localScale = new Vector3(-scale.x, scale.y, scale.z);
		}

		if(webcamTex && !string.IsNullOrEmpty(webcamTex.deviceName))
		{
			webcamTex.Play();
		}
	}


	void Update()
	{
		if(!bTexResolutionSet && webcamTex != null && webcamTex.isPlaying)
		{
			Vector3 localScale = transform.localScale;
			localScale.x = (float)webcamTex.width / (float)webcamTex.height * Mathf.Sign(localScale.x);
			transform.localScale = localScale;

			bTexResolutionSet = true;
		}
	}


}
