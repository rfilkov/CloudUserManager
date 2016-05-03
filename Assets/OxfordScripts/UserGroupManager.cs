using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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
			if(faceManager != null)
			{
				if(string.IsNullOrEmpty(faceManager.faceSubscriptionKey))
				{
					throw new Exception("Please set your face-subscription key.");
				}
			}
			else
			{
				throw new Exception("FaceManager-component not found.");
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
	/// Gets the list of users in this group.
	/// </summary>
	/// <returns>The users list.</returns>
	public List<Person> GetUsersList()
	{
		if(!isInitialized)
			isInitialized = GetOrGreateUserGroup();
		if(!isInitialized)
			return null;
		
		if(faceManager != null && !string.IsNullOrEmpty(userGroupId))
		{
			Person[] persons = faceManager.ListPersonsInGroup(userGroupId);
			
			if(persons != null)
			{
				return new List<Person>(persons);
			}
		}
		
		return null;
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
		
		byte[] imageBytes = texImage != null ? texImage.EncodeToJPG() : null;
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

				PersonFace personFace = null;
				if(imageBytes != null)
				{
					personFace = faceManager.AddFaceToPerson(userGroupId, person.PersonId.ToString (), string.Empty, faceRect, imageBytes);
				}

				if(personFace != null)
				{
					faceManager.TrainPersonGroup(userGroupId);
					return personFace.PersistedFaceId.ToString();
				}
			}
		}

		return string.Empty;
	}


	/// <summary>
	/// Adds the user to group.
	/// </summary>
	/// <returns>Person or null.</returns>
	/// <param name="userName">User name.</param>
	/// <param name="userData">User data.</param>
	public Person AddUserToGroup(string userName, string userData)
	{
		// create the user-group if needed
		if(!isInitialized)
			isInitialized = GetOrGreateUserGroup();
		if(!isInitialized)
			return null;

		Person person = null;
		if(faceManager != null)
		{
			// add person
			person = faceManager.AddPersonToGroup(userGroupId, userName, userData);
		}
		
		return person;
	}


	/// <summary>
	/// Adds the face to user.
	/// </summary>
	/// <returns>User face ID.</returns>
	/// <param name="person">Person.</param>
	/// <param name="texImage">Image texture.</param>
	/// <param name="faceRect">Face rectangle.</param>
	public string AddFaceToUser(Person person, Texture2D texImage, FaceRectangle faceRect)
	{
		if(texImage == null)
			return string.Empty;
		
		byte[] imageBytes = texImage != null ? texImage.EncodeToJPG() : null;
		return AddFaceToUser(person, imageBytes, faceRect);
	}


	/// <summary>
	/// Adds face to the user.
	/// </summary>
	/// <returns>User face ID.</returns>
	/// <param name="person">Person.</param>
	/// <param name="imageBytes">Image bytes.</param>
	/// <param name="faceRect">Face rectangle.</param>
	public string AddFaceToUser(Person person, byte[] imageBytes, FaceRectangle faceRect)
	{
		// create the user-group if needed
		if(!isInitialized)
			isInitialized = GetOrGreateUserGroup();
		if(!isInitialized)
			return string.Empty;
		
		if(faceManager != null && person != null && imageBytes != null)
		{
			PersonFace personFace = faceManager.AddFaceToPerson(userGroupId, person.PersonId.ToString (), string.Empty, faceRect, imageBytes);

			if(personFace != null)
			{
				faceManager.TrainPersonGroup(userGroupId);
				return personFace.PersistedFaceId.ToString();
			}
		}
		
		return string.Empty;
	}
	
	
	/// <summary>
	/// Updates the person's name or userData field.
	/// </summary>
	/// <param name="person">Person to be updated.</param>
	public void UpdateUserData(Person person)
	{
		if(!isInitialized)
			isInitialized = GetOrGreateUserGroup();
		if(!isInitialized)
			return;
		
		if(faceManager != null && !string.IsNullOrEmpty(userGroupId) && person != null)
		{
			faceManager.UpdatePersonData(userGroupId, person);
		}
	}
	
	
	/// <summary>
	/// Deletes existing person from a person group. Persisted face images of the person will also be deleted. 
	/// </summary>
	/// <param name="person">Person to be deleted.</param>
	public void DeleteUser(Person person)
	{
		if(!isInitialized)
			isInitialized = GetOrGreateUserGroup();
		if(!isInitialized)
			return;
		
		if(faceManager != null && !string.IsNullOrEmpty(userGroupId) && person != null)
		{
			faceManager.DeletePerson(userGroupId, person.PersonId.ToString());
			faceManager.TrainPersonGroup(userGroupId);
		}
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
