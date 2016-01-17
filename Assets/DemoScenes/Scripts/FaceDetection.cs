using UnityEngine;
using System.Collections;
using System.IO;

public class FaceDetection : MonoBehaviour 
{
	public WebcamSource webcamSource;
	public Renderer cameraShot;
	public Texture initialTexture;

	public GUIText hintText;
	public GUIText resultText;
	public string faceSubscriptionKey;

	private Color[] faceColors;
	private string[] faceColorNames;


	void Start () 
	{
		// init face colors
		faceColors = new Color[5];
		faceColors[0] = Color.green;
		faceColors[1] = Color.magenta;
		faceColors[2] = Color.red;
		faceColors[3] = Color.yellow;
		faceColors[4] = Color.cyan;

		faceColorNames = new string[5];
		faceColorNames[0] = "green";
		faceColorNames[1] = "magenta";
		faceColorNames[2] = "red";
		faceColorNames[3] = "yellow";
		faceColorNames[4] = "cyan";

		if(string.IsNullOrEmpty(faceSubscriptionKey))
		{
			string sErrorText = "Please set your face-subscription key.";
			SetHintText(sErrorText);
		}
		else
		{
			SetHintText("Click on the camera image to make a shot");
		}
	}
	
	void Update () 
	{
		// check for mouse click
		if (Input.GetMouseButtonDown(0))
		{
			DoMouseClick();
		}

	}

	// mouse click
	private void DoMouseClick()
	{
		RaycastHit hit;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		
		if(Physics.Raycast(ray, out hit))
		{
			GameObject selected = hit.transform.gameObject;
			
			if(selected)
			{
				if(webcamSource && selected == webcamSource.gameObject)
				{
					DoCameraShot();
					StartCoroutine(DoFaceDetection());
				}
				else if(cameraShot && selected == cameraShot.gameObject)
				{
					DoImageImport();
					StartCoroutine(DoFaceDetection());
				}
			}
		}
		
	}
	
	// camera shot step
	private void DoCameraShot()
	{
		if(cameraShot && webcamSource)
		{
			cameraShot.GetComponent<Renderer>().material.mainTexture = webcamSource.GetSnapshot();

			if(resultText)
			{
				resultText.GetComponent<GUIText>().text = "";
			}
		}
	}

	// image import
	private void DoImageImport()
	{
		string filePath = UnityEditor.EditorUtility.OpenFilePanel("Open image file", "", "jpg");
		
		if(!string.IsNullOrEmpty(filePath))
		{
			byte[] fileBytes = File.ReadAllBytes(filePath);
			
			Texture2D tex = new Texture2D(2, 2);
			tex.LoadImage(fileBytes);

			if(cameraShot)
			{
				cameraShot.GetComponent<Renderer>().material.mainTexture = tex;
			}
			
			if(resultText)
			{
				resultText.GetComponent<GUIText>().text = "";
			}
		}
	}

	// face detection
	private IEnumerator DoFaceDetection()
	{
		if(cameraShot && !string.IsNullOrEmpty(faceSubscriptionKey))
		{
			SetHintText("Wait...");

			Texture2D texCamShot = (Texture2D)cameraShot.GetComponent<Renderer>().material.mainTexture;
			yield return null;

			if(texCamShot)
			{
				byte[] imageBytes = texCamShot.EncodeToJPG();
				Face[] faces = WebFaceApi.FaceDetect(faceSubscriptionKey, imageBytes);

				SetResultText(faces);
				DrawFaceRects(faces, texCamShot);
			}
			
			SetHintText("Click on the camera image to make a shot");
		}

		yield return null;
	}

	// draw face rectangles
	private void DrawFaceRects(Face[] faces, Texture2D tex)
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

	// display results
	private void SetResultText(Face[] faces)
	{
		System.Text.StringBuilder sbResult = new System.Text.StringBuilder();

		if(faces == null || faces.Length == 0)
		{
			sbResult.Append("No face detected.");
		}

		for(int i = 0; i < faces.Length; i++)
		{
			Face face = faces[i];
			string faceColorName = faceColorNames[i % faceColors.Length];

			sbResult.Append(string.Format("Face-{0}: {1}", faceColorName, face.FaceId)).AppendLine();
			sbResult.Append(string.Format("    Gender: {0}", face.FaceAttributes.Gender)).AppendLine();
			sbResult.Append(string.Format("    Age: {0}", face.FaceAttributes.Age)).AppendLine();
			sbResult.Append(string.Format("    Smile: {0:F0}%", face.FaceAttributes.Smile * 100f)).AppendLine().AppendLine();

//			sbResult.Append(string.Format("    Beard: {0}", face.FaceAttributes.FacialHair.Beard)).AppendLine();
//			sbResult.Append(string.Format("    Moustache: {0}", face.FaceAttributes.FacialHair.Moustache)).AppendLine();
//			sbResult.Append(string.Format("    Sideburns: {0}", face.FaceAttributes.FacialHair.Sideburns)).AppendLine().AppendLine();
		}

		if(resultText)
		{
			resultText.GetComponent<GUIText>().text = sbResult.ToString();
		}
		else
		{
			Debug.Log(sbResult.ToString());
		}
	}


	private void SetHintText(string sHintText)
	{
		if(hintText)
		{
			hintText.GetComponent<GUIText>().text = sHintText;
		}
		else
		{
			Debug.Log(sHintText);
		}
	}

}
