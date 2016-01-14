using UnityEngine;
using System.Collections;

public class WebcamTest : MonoBehaviour 
{
	public string deviceName;
	public WebCamTexture webcamTex;

	// For saving to the _savepath
	private string _SavePath = "./";
	private int captureCounter = 0;
	

	void Start () 
	{
		WebCamDevice[] devices = WebCamTexture.devices;
		deviceName = devices[0].name;
		webcamTex = new WebCamTexture(deviceName); //, 400, 300, 12);

		//GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1,1));
		GetComponent<Renderer>().material.mainTexture = webcamTex;

		webcamTex.Play();
	}
	
	void OnGUI() 
	{      
		if (GUI.Button(new Rect(10, 70, 100, 100), "Snapshot"))
		{
			TakeSnapshot();
		}
	}
	
	public void TakeSnapshot()
	{
		Texture2D snap = new Texture2D(webcamTex.width, webcamTex.height, TextureFormat.ARGB32, false);
		snap.SetPixels(webcamTex.GetPixels());
		snap.Apply();

		System.IO.File.WriteAllBytes(_SavePath + "snap-" + captureCounter.ToString() + ".png", snap.EncodeToPNG());

		Texture2D flip = FlipTexture(snap);
		System.IO.File.WriteAllBytes(_SavePath + "flip-" + captureCounter.ToString() + ".png", flip.EncodeToPNG());

		flip.Resize(webcamTex.width / 2, webcamTex.height / 2, TextureFormat.ARGB32, false);
		snap.Apply();
		System.IO.File.WriteAllBytes(_SavePath + "flip2-" + captureCounter.ToString() + ".png", flip.EncodeToPNG());
		
		captureCounter++;
	}


	public Texture2D FlipTexture(Texture2D original)
	{
		Texture2D flipped = new Texture2D(original.width, original.height, TextureFormat.ARGB32, false);
		
		int xN = original.width;
		int yN = original.height;

		for(int i = 0; i < xN; i++)
		{
			for(int j = 0; j < yN; j++) 
			{
				flipped.SetPixel(xN - i - 1, j, original.GetPixel(i,j));
			}
		}

		flipped.Apply();
		
		return flipped;
	}


}
