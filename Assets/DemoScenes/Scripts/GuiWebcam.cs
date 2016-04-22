using UnityEngine;
using System.Collections;
using System.Text;

public class GuiWebcam : MonoBehaviour 
{
	[Tooltip("Whether the web-camera output needs to be flipped horizontally or not.")]
	public bool flipHorizontally = false;

	[Tooltip("Selected web-camera name, if any.")]
	public string webcamName;

	// the web-camera texture
	private WebCamTexture webcamTex;

	// whether the output aspect ratio is set
	private bool bTexResolutionSet = false;


	/// <summary>
	/// Gets webcam snapshot.
	/// </summary>
	public Texture2D GetSnapshot()
	{
		Texture2D snap = new Texture2D(webcamTex ? webcamTex.width : 4, webcamTex ? webcamTex.height : 4, TextureFormat.ARGB32, false);

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
			// print available webcams
			StringBuilder sbWebcams = new StringBuilder();
			sbWebcams.Append("Available webcams:").AppendLine();
			
			foreach(WebCamDevice device in devices)
			{
				sbWebcams.Append(device.name).AppendLine();
			}
			
			Debug.Log(sbWebcams.ToString());
			
			if(string.IsNullOrEmpty(webcamName))
			{
				webcamName = devices[0].name;
			}

			// create webcam tex
			webcamTex = new WebCamTexture(webcamName);
			
			GetComponent<GUITexture>().texture = webcamTex;
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
			localScale.x = (float)webcamTex.width * Screen.height / ((float)webcamTex.height * Screen.width)
				* Mathf.Sign(localScale.x);
			transform.localScale = localScale;

			bTexResolutionSet = true;
		}
	}


}
