using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine.EventSystems;
using System.Linq;

public class PersonsManager : MonoBehaviour 
{
    public GameObject personsListPanel;
    public GameObject personDetailsPanel;

	public GameObject personPanelPrefab;

    private RectTransform personsListContent;
    private ModalPanel modalPanel;
    private List<Person> persons;
    private Person selectedPerson;
    private Dictionary<Guid, GameObject> personsPanels = new Dictionary<Guid, GameObject>();

    void Awake()
    {
        modalPanel = ModalPanel.Instance();
        //personPanelPrefab = Resources.LoadAssetAtPath<GameObject>("Assets/GroupManagement/Prefabs/PersonListItem.prefab");

        personsListContent = personsListPanel.FindComponentInChildWithTag<RectTransform>("ListViewContent");
    }

    // Use this for initialization
    void Start()
    {
        StartCoroutine(LoadPersons());
        StartCoroutine(CheckGroupStatus());        
    }

    public void OnAddNewPerson()
    {
        ShowPersonDetail(true);
    }

    public void OnCancelPerson()
    {        
        HidePersonDetails();
        selectedPerson = null;
    }

    public void OnSavePerson()
    {
        string personName = PersonNameInputValue;
        string userData = PersonUserDataInputValue;

        if (personName.Trim().Length == 0)
        {
            modalPanel.ShowMessage("Please enter a name!");
            return;
        }

        if (selectedPerson != null)
        {
            StartCoroutine(UpdatePerson(selectedPerson, personName, userData));
            selectedPerson = null;
        }
        else
        {
            StartCoroutine(CreatePerson(personName));
        }
    }

    public void OnDeletePerson()
    {
        if (selectedPerson != null)
        {
            modalPanel.ShowYesNoDialog(string.Format("Are you sure you want to delete {0}?", selectedPerson.Name), () => {
                StartCoroutine(DeletePerson(selectedPerson));

                selectedPerson = null;
            });
        }
    }

    public void OnReloadPersons()
    {
        StartCoroutine(LoadPersons());
    }

    public void OnTrainGroup()
    {
        StartCoroutine(TrainGroup());
    }

    private IEnumerator TrainGroup()
    {
        AsyncTask<bool> task = new AsyncTask<bool>(() =>
        {
            try
            {
                UserGroupManager groupMgr = UserGroupManager.Instance;

                return groupMgr.StartGroupTraining();
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to process task: " + ex.Message);
                return false;
            }
        });

        bool abort = false;
        modalPanel.ShowProgress("Starting Group Training. Please wait ...", () => abort = true);

        task.Start();
        yield return null;

        while (task.State == TaskState.Running)
            yield return null;

        if (!task.Result)
        {
            modalPanel.ShowMessage("Group training failed. Please, try again later!");
            yield return null;
        }
        else if(!abort)
        {
            task = new AsyncTask<bool>(() =>
            {
                try
                {
                    UserGroupManager groupMgr = UserGroupManager.Instance;

                    bool isTrained = false;
                    int retries = 0;
                    while (!isTrained && retries++ < 3 && !abort)
                    {
                        Thread.Sleep(5000);
                        isTrained = groupMgr.IsGroupTrained();
                    }

                    return isTrained;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to process task: " + ex.Message);
                    return false;
                }
            });

            task.Start();
            yield return null;

            while (task.State == TaskState.Running)
                yield return null;

            if (!abort)
            {
                if (!task.Result)
                {
                    modalPanel.ShowMessage("Group training failed. Please, try again later!");
                }
                else
                {
                    modalPanel.ShowMessage("Group training succeed!");

                    TrainGroupButton.SetActive(false);
                }
            }
        }

        yield return null;
    }

    private IEnumerator CheckGroupStatus()
    {
        AsyncTask<bool> task = new AsyncTask<bool>(() =>
        {
            try
            {
                UserGroupManager groupMgr = UserGroupManager.Instance;

                return groupMgr.IsGroupTrained();
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to process task: " + ex.Message);
                return false;
            }
        });

        task.Start();
        yield return null;

        while (task.State == TaskState.Running)
            yield return null;

        if (!task.Result)
        {
            TrainGroupButton.SetActive(true);
        }

        yield return null;
    }

    private IEnumerator LoadPersons()
    {
        modalPanel.ShowProgress("Loading users. Please wait ...");

        // Clear persons from the list
        if(persons != null) persons.Clear();
        foreach(GameObject panel in personsPanels.Values)
        {
            DestroyPersonPanel(panel);
        }

        personsPanels.Clear();

        AsyncTask<List<Person>> task = new AsyncTask<List<Person>>(() =>
        {
            try
            {
                //Load Persons here
				UserGroupManager groupMgr = UserGroupManager.Instance;

				// wait for the group manager to start
				int waitPeriods = 10;
				while(groupMgr == null && waitPeriods > 0)
				{
					Thread.Sleep(500);
					waitPeriods--;

					groupMgr = UserGroupManager.Instance;
				}

				if(groupMgr != null)
				{
					return groupMgr.GetUsersList();
				}

				return null;
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to process task: " + ex.Message);
                return null;
            }
        });

        task.Start();
        yield return null;

        while (task.State == TaskState.Running)
            yield return null;

        modalPanel.Hide();
		persons = task.Result;

		if(persons != null)
		{
			// sort the person names alphabetically
			persons = persons.OrderBy(p => p.Name).ToList();

			foreach (Person p in persons)
			{
				InstantiatePersonPanel(p);
			}
		}
		else
		{
			Debug.LogError("Error loading users' list. Check the FaceManager- and UserGroupManager-components.");
		}

        yield return null;
    }

    private void InstantiatePersonPanel(Person p)
    {
		if(!personPanelPrefab)
		{
			Debug.LogError("PersonPanel-prefab not set.");
			return;
		}

        GameObject personPanelInstance = Instantiate<GameObject>(personPanelPrefab);

		GameObject personNameObj = personPanelInstance.transform.Find("PersonName").gameObject;
		Text personNameTxt = personNameObj.GetComponent<Text>(); // personPanelInstance.GetComponentInChildren<Text>();
		personNameTxt.text = p.Name;

		GameObject personIDObj = personPanelInstance.transform.Find("PersonID").gameObject;
		Text personIDTxt = personIDObj.GetComponent<Text>();
		personIDTxt.text = "ID: " + p.PersonId.ToString();

        personPanelInstance.transform.SetParent(personsListContent, false);
        AddPersonPanelClickListener(personPanelInstance, p);
        personsPanels.Add(p.PersonId, personPanelInstance);
    }

    private void DestroyPersonPanel(GameObject panel)
    {
        panel.transform.SetParent(null, false);
        Destroy(panel);
    }

    private IEnumerator UpdatePerson(Person p, string name, string userData)
    {
        modalPanel.ShowProgress("Saving data, Please Wait ...");

        AsyncTask<bool> task = new AsyncTask<bool>(() =>
        {
            try
            {
                // update data in the cloud
				UserGroupManager groupMgr = UserGroupManager.Instance;

				if(groupMgr != null && p != null)
				{
					p.Name = name;
                    p.UserData = userData;
					groupMgr.UpdateUserData(p);

					return true;
				}

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to process task: " + ex.Message);
                return false;
            }
        });

        task.Start();
        yield return null;

        while (task.State == TaskState.Running)
            yield return null;

        modalPanel.Hide();

        if (!task.Result)
        {
            modalPanel.ShowMessage("Error saving data!");
        }
        else {
            HidePersonDetails();

            if (p != null)
            {
                GameObject personPanelInstance = personsPanels[p.PersonId];
                Text personName = personPanelInstance.GetComponentInChildren<Text>();
                personName.text = name;
            }
        }  

        yield return null;
    }

    private IEnumerator CreatePerson(string name)
    {
        modalPanel.ShowProgress("Saving data, Please Wait ...");
		Person p = null;

        AsyncTask<bool> task = new AsyncTask<bool>(() =>
        {
            try
            {
                // update data in the cloud
				UserGroupManager groupMgr = UserGroupManager.Instance;

				if(groupMgr != null && persons != null)
				{
					p = groupMgr.AddUserToGroup(name, string.Empty);
					return true;
				}
				
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to process task: " + ex.Message);
                return false;
            }
        });

        task.Start();
        yield return null;

        while (task.State == TaskState.Running)
            yield return null;

        modalPanel.Hide();

        if (!task.Result)
        {
            modalPanel.ShowMessage("Error saving data!");
        }
        else {
            HidePersonDetails();

			if(p != null)
			{
            	persons.Add(p);
				InstantiatePersonPanel(p);
			}
        }

        yield return null;
    }

    private IEnumerator DeletePerson(Person p)
    {
        modalPanel.ShowProgress("Deleting user, Please Wait ...");

        AsyncTask<bool> task = new AsyncTask<bool>(() =>
        {
            try
            {
                // update data in the cloud
				UserGroupManager groupMgr = UserGroupManager.Instance;
				
				if(groupMgr != null && p != null && persons != null)
				{
					groupMgr.DeleteUser(p);
					return true;
				}

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to process task: " + ex.Message);
                return false;
            }
        });

        task.Start();
        yield return null;

        while (task.State == TaskState.Running)
            yield return null;

        modalPanel.Hide();

        if (!task.Result)
        {
            modalPanel.ShowMessage("Error deleting user!");
        }
        else {
            HidePersonDetails();

            if (p != null)
            {
                persons.Remove(p);
                
                GameObject personPanelInstance = personsPanels[p.PersonId];
                DestroyPersonPanel(personPanelInstance);
                personsPanels.Remove(p.PersonId);
            }
        }

        yield return null;
    }

    private void LoadPersonDetails(Person p)
    {
        PersonNameInputValue = p != null ? p.Name : "";
        PersonUserDataInputValue = p != null ? p.UserData : "";
        PersonFaceIdText = p != null && p.PersistedFaceIds != null && p.PersistedFaceIds.Length > 0 ? ("FaceID: " + p.PersistedFaceIds[0].ToString()) : "No Face ID";
        PersonIdText = p != null && p.PersonId != Guid.Empty ? p.PersonId.ToString() : "";
    }

    private string PersonNameInputValue
    {
        get
        {
            InputField personName = FindComponent<InputField>(personDetailsPanel, "InputFieldName");
            if (personName)
                return personName.text;
            else
                return "";
        }
        set
        {
            InputField personName = FindComponent<InputField>(personDetailsPanel, "InputFieldName"); ;
            if(personName)
                personName.text = value;
        }
    }

    private string PersonUserDataInputValue
    {
        get
        {
            InputField personUserData = FindComponent<InputField>(personDetailsPanel, "InputFieldUserData");
            if (personUserData)
                return personUserData.text;
            else
                return "";
        }
        set
        {
            InputField personUserData = FindComponent<InputField>(personDetailsPanel, "InputFieldUserData");
            if (personUserData)
                personUserData.text = value ?? "";
        }
    }

    private string PersonFaceIdText
    {
        set
        {
            Text personFaceID = FindComponent<Text>(personDetailsPanel, "PersonFaceID");
            if (personFaceID)
                personFaceID.text = value ?? "";
        }
    }

    private string PersonIdText
    {
        set
        {
            Text personID = FindComponent<Text>(personDetailsPanel, "TextPersonID");
            if (personID)
                personID.text = value ?? "";
        }
    }

    private GameObject TrainGroupButton
    {
        get
        {
            return personsListPanel.FindChildWithTag("TrainGroupButton");
        }
    }

    private T FindComponent<T>(GameObject parent, string componentName) where T: UnityEngine.Object
    {
        return parent.GetComponentsInChildren<T>().FirstOrDefault(x => x.name == componentName);
    }

    private void AddPersonPanelClickListener(GameObject panel, Person p)
    {
        EventTrigger trigger = panel.GetComponentInParent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((eventData) => { OnPersonClick(p); });
        trigger.delegates.Add(entry);
    }

    private void OnPersonClick(Person p)
    {
        selectedPerson = p;
        ShowPersonDetail();
        LoadPersonDetails(p);
    }

    private void ShowPersonDetail(bool newPerson = false)
    {
        personsListPanel.SetActive(false);
        personDetailsPanel.SetActive(true);

        GameObject deletePersonButton = personDetailsPanel.FindChildWithTag("DeletePersonButton");
        deletePersonButton.SetActive(!newPerson);
    }

    private void HidePersonDetails()
    {
        LoadPersonDetails(null);
        personDetailsPanel.SetActive(false);
        personsListPanel.SetActive(true);        
    }
}
