using UnityEngine;
using System.Collections;
using System.IO;

public class CloudFaceDetection : MonoBehaviour 
{
	[Tooltip("Image source used for getting face images.")]
	public ImageSourceInterface imageSource;

	[Tooltip("Game object used for camera shot rendering.")]
	public Renderer cameraShot;

	[Tooltip("Whether to recognize the emotions of the detected faces, or not.")]
	public bool recognizeEmotions = false;
	
	[Tooltip("GUI text used for hints and status messages.")]
	public GUIText hintText;

	// used face colors and color names
	private Color[] faceColors;
	private string[] faceColorNames;

	// list of detected faces
	private Face[] faces;

	// GUI scroll variable for the results' list
	private Vector2 scroll;


	void Start () 
	{
		// get the first image source among mono behaviors
		MonoBehaviour[] monoScripts = FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];
		
		foreach(MonoBehaviour monoScript in monoScripts)
		{
			if(typeof(ImageSourceInterface).IsAssignableFrom(monoScript.GetType()) &&
			   monoScript.enabled)
			{
				imageSource = (ImageSourceInterface)monoScript;
				break;
			}
		}
		
		// init face colors
		faceColors = CloudFaceManager.GetFaceColors();
		faceColorNames = CloudFaceManager.GetFaceColorNames();

		bool bErrorFound = false;
		if(CloudFaceManager.Instance)
		{
			if(string.IsNullOrEmpty(CloudFaceManager.Instance.faceSubscriptionKey))
			{
				SetHintText("Please set your face-subscription key.");
				bErrorFound = true;
			}
			else if(recognizeEmotions && string.IsNullOrEmpty(CloudFaceManager.Instance.emotionSubscriptionKey))
			{
				SetHintText("Please set your emotion-subscription key.");
				bErrorFound = true;
			}
		}
		else
		{
			SetHintText("CloudFaceManager-component not found.");
			bErrorFound = true;
		}

		if(!bErrorFound)
		{
			SetHintText("Click on the camera image to make a shot");
		}
	}
	
	void Update () 
	{
		// check for mouse click
		if(Input.GetMouseButtonDown(0))
		{
			DoMouseClick();
		}

		// check for Esc, to stop the app
		if(Input.GetKey(KeyCode.Escape))
		{
			Application.Quit();
		}
	}

	// invoked on mouse clicks
	private void DoMouseClick()
	{
		RaycastHit hit;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		
		if(Physics.Raycast(ray, out hit))
		{
			GameObject selected = hit.transform.gameObject;
			
			if(selected)
			{
				if(imageSource != null && imageSource.GetTransform() != null && 
				   selected == imageSource.GetTransform().gameObject)
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
	
	// makes camera shot and displays it on the camera-shot object
	private bool DoCameraShot()
	{
		if(cameraShot && imageSource != null)
		{
			Texture tex = imageSource.GetImage();
			cameraShot.GetComponent<Renderer>().material.mainTexture = tex;

			Vector3 localScale = cameraShot.transform.localScale;
			localScale.x = (float)tex.width / (float)tex.height * Mathf.Sign(localScale.x);
			cameraShot.transform.localScale = localScale;

			return true;
		}

		return false;
	}

	// imports image and displays it on the camera-shot object
	private bool DoImageImport()
	{
#if UNITY_EDITOR
		string filePath = UnityEditor.EditorUtility.OpenFilePanel("Open image file", "", "jpg");  // string.Empty; // 
#else
		string filePath = string.Empty;
#endif
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

	// performs face detection
	private IEnumerator DoFaceDetection()
	{
		// get the image to detect
		faces = null;
		Texture2D texCamShot = null;

		if(cameraShot)
		{
			texCamShot = (Texture2D)cameraShot.GetComponent<Renderer>().material.mainTexture;
			SetHintText("Wait...");
		}

//		yield return null;
//		
//		try 
//		{
			// get the face manager instance
			CloudFaceManager faceManager = CloudFaceManager.Instance;

			if(texCamShot && faceManager)
			{
				//faces = faceManager.DetectFaces(texCamShot);
				AsyncTask<Face[]> taskFace = faceManager.DetectFaces(texCamShot);

				while (taskFace.State == TaskState.Running)
				{
					yield return null;
				}

				if(string.IsNullOrEmpty(taskFace.ErrorMessage))
				{
					faces = taskFace.Result;

					if(faces != null && faces.Length > 0)
					{
						// stick to detected face rectangles
						FaceRectangle[] faceRects = new FaceRectangle[faces.Length];

						for(int i = 0; i < faces.Length; i++)
						{
							faceRects[i] = faces[i].FaceRectangle;
						}

						// get the emotions of the faces
						if(recognizeEmotions)
						{
							//Emotion[] emotions = faceManager.RecognizeEmotions(texCamShot, faceRects);
							AsyncTask<Emotion[]> taskEmot = faceManager.RecognizeEmotions(texCamShot, faceRects);
							
							while (taskEmot.State == TaskState.Running)
							{
								yield return null;
							}
							
							if(string.IsNullOrEmpty(taskEmot.ErrorMessage))
							{
								Emotion[] emotions = taskEmot.Result;
								int matched = faceManager.MatchEmotionsToFaces(ref faces, ref emotions);

								if(matched != faces.Length)
								{
									Debug.Log(string.Format("Matched {0}/{1} emotions to {2} faces.", matched, emotions.Length, faces.Length));
								}
							}
							else
							{
								SetHintText(taskEmot.ErrorMessage);
							}
						}

						CloudFaceManager.DrawFaceRects(texCamShot, faces, faceColors);
						SetHintText("Click on the camera image to make a shot");
					}
					else
					{
						SetHintText("No faces detected.");
					}
				}
				else
				{
					SetHintText(taskFace.ErrorMessage);
				}
			}
			else
			{
				SetHintText("Check if the FaceManager component exists in the scene.");
			}
//		} 
//		catch (System.Exception ex) 
//		{
//			Debug.LogError(ex.Message + '\n' + ex.StackTrace);
//			SetHintText(ex.Message);
//		}
//
//		yield return null;
	}

	void OnGUI()
	{
		// set gui font
		GUI.skin.font = hintText ? hintText.GetComponent<GUIText>().font : GUI.skin.font;

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
				sbResult.Append(string.Format("    Smile: {0:F0}%", face.FaceAttributes.Smile * 100f)).AppendLine();
				if(recognizeEmotions)
					sbResult.Append(string.Format("    Emotion: {0}", CloudFaceManager.GetEmotionScoresAsString(face.Emotion))).AppendLine();
				sbResult.AppendLine();
				
				GUILayout.Label(sbResult.ToString());

				GUI.color = guiColor;
			}
			
			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}
	}


	// displays hint or status text
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
