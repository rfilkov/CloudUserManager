﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;

public class CloudUserManager : MonoBehaviour 
{
	[Tooltip("ID (short name) of the user group, containing the face-identified users. It will be created, if not found.")]
	public string userGroupId = "game-users";

	[Tooltip("Whether group existence should be checked at start.")]
	public bool checkGroupAtStart = true;

	[Tooltip("GUI text used for debug and status messages.")]
	public GUIText debugText;

	// the face manager
	private CloudFaceManager faceManager = null;

	// the person group object
	private PersonGroup personGroup = null;

	private const int threadWaitLoops = 25;  // 25 * 200ms = 5.0s
	private const int threadWaitMs = 200;

	private static CloudUserManager instance = null;
	private bool isInitialized = false;



	/// <summary>
	/// Gets the CloudUserManager instance.
	/// </summary>
	/// <value>The CloudUserManager instance.</value>
	public static CloudUserManager Instance
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
			
			faceManager = CloudFaceManager.Instance;
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
	/// Starts group training.
	/// </summary>
	/// <returns><c>true</c>, if group training was started successfully, <c>false</c> otherwise.</returns>
	public bool StartGroupTraining()
	{
		// create the user-group if needed
		if(!isInitialized)
			isInitialized = GetOrGreateUserGroup();
		if(!isInitialized)
			return false;

		if(faceManager != null)
		{
			return faceManager.TrainPersonGroup(userGroupId);
		}

		return false;
	}


	/// <summary>
	/// Gets the group training status.
	/// </summary>
	/// <returns>The training status (may be null).</returns>
	public TrainingStatus GetTrainingStatus()
	{
		// create the user-group if needed
		if(!isInitialized)
			isInitialized = GetOrGreateUserGroup();
		if(!isInitialized)
			return null;
		
		// get the training status
		TrainingStatus training = null;
		if(faceManager != null)
		{
			training = faceManager.GetPersonGroupTrainingStatus(userGroupId);
		}
		
		return training;
	}
	
	
	/// <summary>
	/// Determines whether the group training is finished.
	/// </summary>
	/// <returns><c>true</c> if the group training is finished; otherwise, <c>false</c>.</returns>
	public bool IsGroupTrained()
	{
		// create the user-group if needed
		if(!isInitialized)
			isInitialized = GetOrGreateUserGroup();
		if(!isInitialized)
			return false;
		
		if(faceManager != null)
		{
			return faceManager.IsPersonGroupTrained(userGroupId);
		}
		
		return false;
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
			//faces = faceManager.DetectFaces(imageBytes);
			AsyncTask<Face[]> task = faceManager.DetectFaces(imageBytes);

			while (task.State == TaskState.Running)
			{
				//yield return null;
				Thread.Sleep(threadWaitMs);
			}

			faces = task.Result;

			// get the training status
			TrainingStatus training = faceManager.GetPersonGroupTrainingStatus(userGroupId);
			bool bEmptyGroup = false;

			if(training != null && training.Status == Status.Failed)
			{
				// check if there are persons in this group
				List<Person> listPersons = GetUsersList();

				if(listPersons.Count > 0)
				{
					// retrain the group
					faceManager.TrainPersonGroup(userGroupId);
				}
				else
				{
					// empty list - always returns 'training failed'
					training.Status = Status.Succeeded;
					bEmptyGroup = true;
				}
			}
			
			float waitTill = Time.realtimeSinceStartup + 5f;
			while((training == null || training.Status != Status.Succeeded) && (Time.realtimeSinceStartup < waitTill))
			{
				// wait for training to succeed
				System.Threading.Thread.Sleep(1000);
				training = faceManager.GetPersonGroupTrainingStatus(userGroupId);
			}

			if(bEmptyGroup)
			{
				// nothing to check
				return true;
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
	/// Gets the user by ID.
	/// </summary>
	/// <returns>The user or null.</returns>
	/// <param name="personId">Person ID</param>
	public Person GetUserById(string personId)
	{
		if(!isInitialized)
			isInitialized = GetOrGreateUserGroup();
		if(!isInitialized)
			return null;

		Person person = null;
		if(faceManager != null && !string.IsNullOrEmpty(userGroupId))
		{
			person = faceManager.GetPerson(userGroupId, personId);
		}
		
		return person;
	}
	
	
	/// <summary>
	/// Adds the user to group.
	/// </summary>
	/// <returns>Person or null.</returns>
	/// <param name="userName">User name.</param>
	/// <param name="userData">User data.</param>
	/// <param name="texImage">Image texture.</param>
	/// <param name="faceRect">Face rectangle.</param>
	public Person AddUserToGroup(string userName, string userData, Texture2D texImage, FaceRectangle faceRect)
	{
		if(texImage == null)
			return null;
		
		byte[] imageBytes = texImage != null ? texImage.EncodeToJPG() : null;
		return AddUserToGroup(userName, userData, imageBytes, faceRect);
	}


	/// <summary>
	/// Adds the user to group.
	/// </summary>
	/// <returns>Person or null.</returns>
	/// <param name="userName">User name.</param>
	/// <param name="userData">User data.</param>
	/// <param name="imageBytes">Image bytes.</param>
	/// <param name="faceRect">Face rectangle.</param>
	public Person AddUserToGroup(string userName, string userData, byte[] imageBytes, FaceRectangle faceRect)
	{
		// create the user-group if needed
		if(!isInitialized)
			isInitialized = GetOrGreateUserGroup();
		if(!isInitialized)
			return null;
		
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
					person.PersistedFaceIds = new Guid[1];
					person.PersistedFaceIds[0] = personFace.PersistedFaceId;

					faceManager.TrainPersonGroup(userGroupId);
				}
			}

			return person;
		}

		return null;
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
