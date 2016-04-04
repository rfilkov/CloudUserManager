using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine.EventSystems;

public class PersonsManager : MonoBehaviour {

    public GameObject personsListPanel;
    public GameObject personDetailsPanel;

    private RectTransform personsListContent;
    private ModalPanel modalPanel;
    private GameObject personPanelPrefab;
    private List<Person> persons;
    private Person selectedPerson;
    private Dictionary<Guid, GameObject> personsPanels = new Dictionary<Guid, GameObject>();

    void Awake()
    {
        modalPanel = ModalPanel.Instance();
        personPanelPrefab = Resources.LoadAssetAtPath<GameObject>("Assets/GroupManagement/Prefabs/PersonListItem.prefab");

        personsListContent = personsListPanel.FindComponentInChildWithTag<RectTransform>("ListViewContent");
    }

    // Use this for initialization
    void Start()
    {
        StartCoroutine(LoadPersons());
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

        if(personName.Trim().Length == 0)
        {
            modalPanel.ShowMessage("Please enter a name!");
            return;
        }

        if (selectedPerson != null)
        {
            StartCoroutine(UpdatePerson(selectedPerson, personName));
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
            StartCoroutine(DeletePerson(selectedPerson));

            selectedPerson = null;
        }
    }

    public void OnReloadPersons()
    {
        StartCoroutine(LoadPersons());
    }

    private IEnumerator LoadPersons()
    {
        modalPanel.ShowProgress("Loading Players, Please Wait ...");

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

//                Thread.Sleep(3000);
//
//                return new List<Person>()
//                {
//                    new Person { Name = "Andy Murray", PersonId = Guid.NewGuid() },
//                    new Person { Name = "Roger Federer", PersonId = Guid.NewGuid() },
//                    new Person { Name = "Novak Djokovic", PersonId = Guid.NewGuid() },
//                };
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
			foreach (Person p in persons)
			{
				InstantiatePersonPanel(p);
			}
		}
		else
		{
			Debug.LogError("Error loading persons.");
		}

        yield return null;
    }

    private void InstantiatePersonPanel(Person p)
    {
        GameObject personPanelInstance = Instantiate<GameObject>(personPanelPrefab);

		GameObject personNameObj = personPanelInstance.transform.Find("PersonName").gameObject;
		Text personNameTxt = personNameObj.GetComponent<Text>(); // personPanelInstance.GetComponentInChildren<Text>();
		personNameTxt.text = p.Name;

		GameObject personGroupObj = personPanelInstance.transform.Find("PersonGroup").gameObject;
		Text personGroupTxt = personGroupObj.GetComponent<Text>();
		personGroupTxt.text = "ID: " + p.PersonId.ToString();

        personPanelInstance.transform.SetParent(personsListContent, false);
        AddPersonPanelClickListener(personPanelInstance, p);
        personsPanels.Add(p.PersonId, personPanelInstance);
    }

    private void DestroyPersonPanel(GameObject panel)
    {
        panel.transform.SetParent(null, false);
        Destroy(panel);
    }

    private IEnumerator UpdatePerson(Person p, string name)
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
					groupMgr.UpdateUserData(p);

					return true;
				}

                //Thread.Sleep(2000);

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

            //Person p = persons.Find(x => x.PersonId == id);
            if (p != null)
            {
                //p.Name = name;
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
				
                //Thread.Sleep(2000);

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

            //Person p = new Person { Name = name, PersonId = Guid.NewGuid() };
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
        //TO DO: Ask first ... 

        modalPanel.ShowProgress("Deleting player, Please Wait ...");

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
				
                //Thread.Sleep(2000);

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
            modalPanel.ShowMessage("Error deleting player!");
        }
        else {
            HidePersonDetails();

            //Person p = persons.Find(x => x.PersonId == id);
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
    }

    private string PersonNameInputValue
    {
        get
        {
            InputField personName = personDetailsPanel.GetComponentInChildren<InputField>();
            return personName.text;
        }
        set
        {
            InputField personName = personDetailsPanel.GetComponentInChildren<InputField>();
            personName.text = value;
        }
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
