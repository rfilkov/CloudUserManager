using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class UserRecognizer : MonoBehaviour 
{
	[Tooltip("Image source used for getting face images.")]
	public ImageSourceInterface imageSource;
	
	[Tooltip("Game object used for camera shot rendering.")]
	public GUITexture cameraShot;
	
	[Tooltip("GUI text used for hints and status messages.")]
	public GUIText hintText;

	// internal user recognition state
	private int state = 0;

	// used face colors and color names
	private Color[] faceColors;
	private string[] faceColorNames;
	
	// array of faces
	private Face[] faces = null;

	// array of identification results
	private IdentifyResult[] results = null;

	// GUI background texture
	private Texture2D guiTexBack = null;

	// GUI scroll variable for the results' list
	private Vector2 scroll;
	// selected user face
	private int selected = 0;

	// new user name
	private string userName = string.Empty;


	void Start()
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
		faceColors = FaceManager.GetFaceColors();
		faceColorNames = FaceManager.GetFaceColorNames();

		// initialize gui backfround tex
		guiTexBack = new Texture2D(64, 64, TextureFormat.ARGB32, false);
		Color backColor = (Color)new Color32(32, 32, 32, 220);
		
		for (int y = 0; y < guiTexBack.height; ++y)
		{
			for (int x = 0; x < guiTexBack.width; ++x)
			{
				guiTexBack.SetPixel(x, y, backColor);
			}
		}
		
		guiTexBack.Apply();
		
		if(hintText)
		{
			hintText.text = "Press Space to make a shot, or Ctrl to select a file.";
		}
	}


	void Update () 
	{
		try 
		{
			// check for mouse click
			bool bImportImage = Input.GetKeyDown(KeyCode.LeftControl);
			if(/**Input.GetMouseButtonDown(0) ||*/ Input.GetButtonDown("Jump") || bImportImage)
			{
				DoMouseClick(bImportImage);
			}
			
			// check for Esc, to stop the app
			if(Input.GetKeyDown(KeyCode.Escape))
			{
				Application.Quit();
			}
		} 
		catch (Exception ex) 
		{
			Debug.LogError(ex.Message + '\n' + ex.StackTrace);
			
			if(hintText != null)
			{
				hintText.text = ex.Message;
			}
		}
	}

	void OnGUI()
	{
		// set gui font
		GUI.skin.font = hintText ? hintText.GetComponent<GUIText>().font : GUI.skin.font;

		if(state == 1)
		{
			Rect guiResultRect = new Rect(Screen.width - 180, 0, 170, Screen.height);

			GUIStyle guiStyle = new GUIStyle();
			guiStyle.normal.background = guiTexBack;

			GUILayout.BeginArea(guiResultRect, guiStyle);

			scroll = GUILayout.BeginScrollView(scroll);
			GUILayout.BeginVertical();

			GUIStyle labelStyle = GUI.skin.GetStyle("Label");
			labelStyle.alignment = TextAnchor.UpperCenter;

			if(faces != null && faces.Length > 0)
			{
				GUILayout.Label("Recognized:", labelStyle);

				List<string> alNewFaceNames = new List<string>();
				List<Face> alNewFaces = new List<Face>();

				for(int i = 0; i < faces.Length; i++)
				{
					Face face = faces[i];
					
					Color faceColor = faceColors[i % faceColors.Length];
					string faceColorName = faceColorNames[i % faceColors.Length];
					
					Color guiColor = GUI.color;
					GUI.color = faceColor;

					if(face.Candidate != null && face.Candidate.Person != null)
					{
						//GUILayout.Label(string.Format("{0} face: {1}", faceColorName, face.Candidate.Person.Name));
						GUILayout.Label(string.Format("{0}", face.Candidate.Person.Name));
					}
					else
					{
						alNewFaceNames.Add(string.Format("{0} face", faceColorName));
						alNewFaces.Add(face);
					}

					GUI.color = guiColor;
				}

				if(faces.Length == alNewFaceNames.Count)
				{
					GUILayout.Label("-");
				}

				// horizontal line
				GUILayout.Box(string.Empty, new GUILayoutOption[]{GUILayout.ExpandWidth(true), GUILayout.Height(1)});
				
				if(alNewFaceNames.Count > 0)
				{
					GUILayout.Label("New user:", labelStyle);
					selected = GUILayout.SelectionGrid(selected, alNewFaceNames.ToArray (), 1);
					
					GUILayout.Label("User name:", labelStyle);
					userName = GUILayout.TextField(userName, 128);
					
					if(GUILayout.Button("Add User"))
					{
						if(selected >= 0 && !string.IsNullOrEmpty(userName.Trim()))
						{
							Face face = alNewFaces[selected];
							StartCoroutine(AddUserToGroup(face, userName));
							userName = string.Empty;
						}
					}
				}

				alNewFaceNames.Clear();
				alNewFaces.Clear();
			}

			GUILayout.EndVertical();
			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}
	}


	// invoked on mouse clicks
	private void DoMouseClick(bool bImportImage)
	{
		switch(state)
		{
		case 0:
			if(!bImportImage ? DoCameraShot() : DoImageImport())
			{
				StartCoroutine(DoUserRecognition());
				state = 1;
			}
			break;
			
		case 1:
			if(DoSwitchWebcam())
			{
				state = 0;
			}
			break;
		}
	}
	
	// makes camera shot and displays it on the camera-shot object
	private bool DoCameraShot()
	{
		if(cameraShot && imageSource != null)
		{
			Texture tex = imageSource.GetImage();
			cameraShot.texture = tex;

			if(imageSource != null && imageSource.GetTransform() != null)
				imageSource.GetTransform().gameObject.SetActive(false);
			cameraShot.gameObject.SetActive(true);
			
			Vector3 localScale = cameraShot.transform.localScale;
			localScale.x = (float)tex.width * Screen.height / ((float)tex.height * Screen.width)
				* Mathf.Sign(localScale.x);
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
				cameraShot.texture = tex;

				if(imageSource != null && imageSource.GetTransform() != null)
					imageSource.GetTransform().gameObject.SetActive(false);
				cameraShot.gameObject.SetActive(true);
				
				Vector3 localScale = cameraShot.transform.localScale;
				localScale.x = (float)tex.width * Screen.height / ((float)tex.height * Screen.width)
					* Mathf.Sign(localScale.x);
				cameraShot.transform.localScale = localScale;
				
				return true;
			}
		}
		
		return false;
	}
	
	// switch back the webcam image
	private bool DoSwitchWebcam()
	{
		if(cameraShot && imageSource != null)
		{
			if(imageSource != null && imageSource.GetTransform() != null)
				imageSource.GetTransform().gameObject.SetActive(true);
			cameraShot.gameObject.SetActive(false);
			
			if(hintText)
			{
				hintText.text = "Press Space to make a shot, or Ctrl to select a file.";
			}
			
			return true;
		}
		
		return false;
	}
	
	// performs user recognition
	private IEnumerator DoUserRecognition()
	{
		// get the image to detect
		faces = null;
		Texture2D texCamShot = null;
		
		if(cameraShot)
		{
			texCamShot = (Texture2D)cameraShot.texture;

			if(hintText)
			{
				hintText.text = "Wait...";
			}
		}
		
		yield return null;
		
		try 
		{
			// get the user manager instance
			UserGroupManager userManager = UserGroupManager.Instance;
			
			if(texCamShot && userManager)
			{
				if(userManager.IdentifyUsers(texCamShot, ref faces, ref results))
				{
					// draw face rects
					FaceManager.DrawFaceRects(texCamShot, faces, faceColors);
					
					if(hintText)
					{
						hintText.text = "Press Space or Ctrl to return.";
					}
				}
				else
				{
					if(hintText)
					{
						hintText.text = "No users detected.";
					}
				}
			}
			else
			{
				if(hintText)
				{
					hintText.text = "Check if the FaceManager and UserGroupManagers component exist in the scene.";
				}
			}
		} 
		catch (System.Exception ex) 
		{
			Debug.LogError(ex.Message + '\n' + ex.StackTrace);

			if(hintText)
			{
				hintText.text = ex.Message;
			}
		}
		
		yield return null;
	}
	

	// adds the new user to user group
	private IEnumerator AddUserToGroup(Face face, string userName)
	{
		Texture2D texCamShot = null;
		
		if(cameraShot)
		{
			texCamShot = (Texture2D)cameraShot.texture;
			
			if(hintText)
			{
				hintText.text = "Wait...";
			}
		}
		
		yield return null;
		
		try 
		{
			UserGroupManager userManager = UserGroupManager.Instance;
			
			if(texCamShot && userManager && face != null && userName != string.Empty)
			{
				FaceRectangle faceRect = face.FaceRectangle;
				Person person = userManager.AddUserToGroup(userName, string.Empty, texCamShot, faceRect);

				if(person != null && person.PersistedFaceIds != null && person.PersistedFaceIds.Length > 0)
				{
					Guid faceId = face.FaceId;
					bool bFaceFound = false;

					for(int i = 0; i < faces.Length; i++)
					{
						if(faces[i].FaceId == faceId)
						{
							if(faces[i].Candidate == null)
							{
								faces[i].Candidate = new Candidate();

								faces[i].Candidate.PersonId = person.PersonId;
								faces[i].Candidate.Confidence = 1f;
								faces[i].Candidate.Person = person;
							}

							bFaceFound = true;
							break;
						}
					}

					if(hintText != null)
					{
						hintText.text = string.Format("User '{0}' created successfully.", userName);
					}
				}
				else
				{
					if(hintText != null)
					{
						hintText.text = "Face could not be added.";
					}
				}
			}
//			else
//			{
//				if(hintText)
//				{
//					hintText.text = "Check if the FaceManager and UserGroupManagers component exist in the scene.";
//				}
//			}
		} 
		catch (Exception ex) 
		{
			Debug.LogError(ex.Message + '\n' + ex.StackTrace);
			
			if(hintText != null)
			{
				hintText.text = ex.Message;
			}
		}

		yield return null;
	}

}
