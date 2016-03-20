﻿using UnityEngine;
using System.Collections;
using System;

public class UserGroupManager : MonoBehaviour 
{
	[Tooltip("ID (short name) of the user group, containing the face-identified users. It will be created, if not found.")]
	public string userGroupId = "game-users";

	[Tooltip("Whether group existence should be checked at start.")]
	public bool checkGroupAtStart = true;

	[Tooltip("GUI text used for debug and status messages.")]
	public GUIText debugText;

	// the face manager
	private FaceManager faceManager = null;

	// the person group object
	private PersonGroup personGroup = null;

	private static UserGroupManager instance = null;
	private bool isInitialized = false;



	/// <summary>
	/// Gets the UserGroupManager instance.
	/// </summary>
	/// <value>The UserGroupManager instance.</value>
	public static UserGroupManager Instance
	{
		get
		{
			return instance;
		}
	}
	
	
	/// <summary>
	/// Determines whether the UserGroupManager is initialized.
	/// </summary>
	/// <returns><c>true</c> if the UserGroupManager is initialized; otherwise, <c>false</c>.</returns>
	public bool IsInitialized()
	{
		return isInitialized;
	}
	
	
	void Start () 
	{
		try 
		{
			instance = this;
			
			if(string.IsNullOrEmpty(userGroupId))
			{
				throw new Exception("Please set the user-group name.");
			}
			
			faceManager = FaceManager.Instance;
			if(faceManager == null || !faceManager.IsInitialized())
			{
				throw new Exception("FaceManager not found or not initialized.");
			}

			// get the user group info
			isInitialized = checkGroupAtStart ? GetOrGreateUserGroup() : false;
		} 
		catch (Exception ex) 
		{
			Debug.LogError(ex.Message + '\n' + ex.StackTrace);

			if(debugText != null)
			{
				debugText.text = ex.Message;
			}
		}
	}
	
	void Update () 
	{
	}


	/// <summary>
	/// Identifies the users on the image.
	/// </summary>
	/// <returns><c>true</c>, if identification completed successfully, <c>false</c> otherwise.</returns>
	/// <param name="texImage">Image texture.</param>
	/// <param name="faces">Array of faces.</param>
	/// <param name="results">Array of identification results.</param>
	public bool IdentifyUsers(Texture2D texImage, ref Face[] faces, ref IdentifyResult[] results)
	{
		if(texImage == null)
			return false;
		
		byte[] imageBytes = texImage.EncodeToJPG();
		return IdentifyUsers(imageBytes, ref faces, ref results);
	}


	/// <summary>
	/// Identifies the users on the image.
	/// </summary>
	/// <returns><c>true</c>, if identification completed successfully, <c>false</c> otherwise.</returns>
	/// <param name="imageBytes">Image bytes.</param>
	/// <param name="faces">Array of faces.</param>
	/// <param name="results">Array of identification results.</param>
	public bool IdentifyUsers(byte[] imageBytes, ref Face[] faces, ref IdentifyResult[] results)
	{
		// create the user-group if needed
		if(!isInitialized)
			isInitialized = GetOrGreateUserGroup();
		if(!isInitialized)
			return false;

		// detect and identify user faces
		faces = null;
		results = null;

		if(faceManager != null)
		{
			faces = faceManager.DetectFaces(imageBytes);

			// wait for the group to finish training
			float waitTill = Time.time + 5f;
			while(!faceManager.IsPersonGroupTrained(userGroupId) && (Time.time < waitTill))
			{
				System.Threading.Thread.Sleep(250);
			}
			
			if(faces != null && faces.Length > 0)
			{
				results = faceManager.IdentifyFaces(userGroupId, ref faces, 1);
				faceManager.MatchCandidatesToFaces(ref faces, ref results, userGroupId);
				return true;
			}
		}

		return false;
	}


	/// <summary>
	/// Adds the user to group.
	/// </summary>
	/// <returns>User face ID.</returns>
	/// <param name="userName">User name.</param>
	/// <param name="userData">User data.</param>
	/// <param name="texImage">Image texture.</param>
	/// <param name="faceRect">Face rectangle.</param>
	public string AddUserToGroup(string userName, string userData, Texture2D texImage, FaceRectangle faceRect)
	{
		if(texImage == null)
			return string.Empty;
		
		byte[] imageBytes = texImage.EncodeToJPG();
		return AddUserToGroup(userName, userData, imageBytes, faceRect);
	}

	/// <summary>
	/// Adds the user to group.
	/// </summary>
	/// <returns>User face ID.</returns>
	/// <param name="userName">User name.</param>
	/// <param name="userData">User data.</param>
	/// <param name="imageBytes">Image bytes.</param>
	/// <param name="faceRect">Face rectangle.</param>
	public string AddUserToGroup(string userName, string userData, byte[] imageBytes, FaceRectangle faceRect)
	{
		// create the user-group if needed
		if(!isInitialized)
			isInitialized = GetOrGreateUserGroup();
		if(!isInitialized)
			return string.Empty;
		
		if(faceManager != null)
		{
			// add person
			Person person = faceManager.AddPersonToGroup(userGroupId, userName, userData);

			if(person != null)
			{
//				if(faceRect != null)
//				{
//					faceRect.Left -= 10;
//					faceRect.Top -= 10;
//					faceRect.Width += 20;
//					faceRect.Height += 20;
//				}

				PersonFace personFace = faceManager.AddFaceToPerson(userGroupId, person.PersonId.ToString (), string.Empty, faceRect, imageBytes);

				if(personFace != null)
				{
					faceManager.TrainPersonGroup(userGroupId);
					return personFace.PersistedFaceId.ToString();
				}
			}
		}

		return string.Empty;
	}


	// gets the person group info
	private bool GetOrGreateUserGroup()
	{
		if(!string.IsNullOrEmpty(userGroupId) && faceManager != null)
		{
			try 
			{
				personGroup = faceManager.GetPersonGroup(userGroupId);
			} 
			catch (Exception ex) 
			{
				Debug.Log(ex.Message);
				Debug.Log("Trying to create user-group '" + userGroupId + "'...");

				if(faceManager.CreatePersonGroup(userGroupId, userGroupId, string.Empty))
				{
					faceManager.TrainPersonGroup(userGroupId);
					personGroup = faceManager.GetPersonGroup(userGroupId);
				}

				Debug.Log("User-group '" + userGroupId + "' created.");
			}
			
			return (personGroup != null);
		}

		return false;
	}

}