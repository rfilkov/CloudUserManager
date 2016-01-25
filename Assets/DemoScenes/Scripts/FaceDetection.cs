using UnityEngine;
using System.Collections;
using System.IO;

public class FaceDetection : MonoBehaviour 
{
	public WebcamSource webcamSource;
	public Renderer cameraShot;
	public Texture initialTexture;
	public GUIText hintText;

	private Color[] faceColors;
	private string[] faceColorNames;

	private Face[] faces;
	private Vector2 scroll;


	void Start () 
	{
		// init face colors
		faceColors = new Color[5];
		faceColors[0] = Color.green;
		faceColors[1] = Color.yellow;
		faceColors[2] = Color.cyan;
		faceColors[3] = Color.magenta;
		faceColors[4] = Color.red;

		faceColorNames = new string[5];
		faceColorNames[0] = "Green";
		faceColorNames[1] = "Yellow";
		faceColorNames[2] = "Cyan";
		faceColorNames[3] = "Magenta";
		faceColorNames[4] = "Red";

		SetHintText("Click on the camera image to make a shot");
	}
	
	void Update () 
	{
		// check for mouse click
		if (Input.GetMouseButtonDown(0))
		{
			DoMouseClick();
		}
	
		if(Input.GetButton("Jump"))
		{
			bool bRes = FaceManager.Instance.CreatePersonGroup("testusers", "Test Users", "group=test");
			Debug.Log(bRes);
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
					if(DoCameraShot())
					{
						StartCoroutine(DoFaceDetection());
					}
				}
				else if(cameraShot && selected == cameraShot.gameObject)
				{
					if(DoImageImport())
					{
						StartCoroutine(DoFaceDetection());
					}
				}
			}
		}
		
	}
	
	// camera shot step
	private bool DoCameraShot()
	{
		if(cameraShot && webcamSource)
		{
			Texture tex = webcamSource.GetSnapshot();
			cameraShot.GetComponent<Renderer>().material.mainTexture = tex;

			Vector3 localScale = cameraShot.transform.localScale;
			localScale.x = (float)tex.width / (float)tex.height * Mathf.Sign(localScale.x);
			cameraShot.transform.localScale = localScale;

			return true;
		}

		return false;
	}

	// image import
	private bool DoImageImport()
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

				Vector3 localScale = cameraShot.transform.localScale;
				localScale.x = (float)tex.width / (float)tex.height * Mathf.Sign(localScale.x);
				cameraShot.transform.localScale = localScale;

				return true;
			}
		}

		return false;
	}

	// face detection
	private IEnumerator DoFaceDetection()
	{
		// get the image to detect
		faces = null;
		Texture2D texCamShot = null;

		if(cameraShot)
		{
			texCamShot = (Texture2D)cameraShot.GetComponent<Renderer>().material.mainTexture;
		}

		yield return null;
		
		try 
		{
			// get the face manager instance
			FaceManager faceManager = FaceManager.Instance;

			if(texCamShot && faceManager)
			{
				SetHintText("Wait...");
				faces = faceManager.DetectFaces(texCamShot);
				
				if(faces != null && faces.Length > 0)
				{
					faceManager.DrawFaceRects(texCamShot, faces, faceColors);
					SetHintText("Click on the camera image to make a shot");
				}
				else
				{
					SetHintText("No face(s) detected.");
				}
			}
		} 
		catch (System.Exception ex) 
		{
			Debug.LogError(ex.Message + '\n' + ex.StackTrace);
			SetHintText(ex.Message);
		}

		yield return null;
	}

	void OnGUI()
	{
		if(faces != null && faces.Length > 0)
		{
			Rect guiResultRect = new Rect(Screen.width / 2, 20, Screen.width / 2, Screen.height - 20);
			GUILayout.BeginArea(guiResultRect);
			scroll = GUILayout.BeginScrollView(scroll);
			
			for(int i = 0; i < faces.Length; i++)
			{
				Face face = faces[i];

				Color faceColor = faceColors[i % faceColors.Length];
				string faceColorName = faceColorNames[i % faceColors.Length];

				Color guiColor = GUI.color;
				GUI.color = faceColor;

				System.Text.StringBuilder sbResult = new System.Text.StringBuilder();
				sbResult.Append(string.Format("{0} face:", faceColorName, face.FaceId)).AppendLine();
				sbResult.Append(string.Format("    Gender: {0}", face.FaceAttributes.Gender)).AppendLine();
				sbResult.Append(string.Format("    Age: {0}", face.FaceAttributes.Age)).AppendLine();
				sbResult.Append(string.Format("    Smile: {0:F0}%", face.FaceAttributes.Smile * 100f)).AppendLine().AppendLine();
				
//				sbResult.Append(string.Format("    Beard: {0}", face.FaceAttributes.FacialHair.Beard)).AppendLine();
//				sbResult.Append(string.Format("    Moustache: {0}", face.FaceAttributes.FacialHair.Moustache)).AppendLine();
//				sbResult.Append(string.Format("    Sideburns: {0}", face.FaceAttributes.FacialHair.Sideburns)).AppendLine().AppendLine();

				GUILayout.Label(sbResult.ToString());

				GUI.color = guiColor;
			}
			
			GUILayout.EndScrollView();
			GUILayout.EndArea();
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
