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

    void Awake()
    {
        modalPanel = ModalPanel.Instance();
        personPanelPrefab = Resources.LoadAssetAtPath<GameObject>("Assets/Scenes/Prefabs/PersonListItem.prefab");

        personsListContent = personsListPanel.FindComponentInChildWithTag<RectTransform>("ListViewContent");
    }

    // Use this for initialization
    void Start()
    {
        StartCoroutine(LoadingPersons());
    }

    public void OnAddNewPerson()
    {
        ShowPersonDetail(true);
    }

    public void OnCancelPerson()
    {        
        HidePersonDetails();
    }

    public void OnSavePerson()
    {
        HidePersonDetails();
    }

    public void OnDeletePerson()
    {

    }

    private IEnumerator LoadingPersons()
    {
        modalPanel.Show("Loading Players, Please Wait ...");

        AsyncTask<List<Person>> task = new AsyncTask<List<Person>>(() =>
        {
            try
            {
                //Load Persons here

                Thread.Sleep(3000);

                return new List<Person>()
                {
                    new Person { Name = "Andy Murray" },
                    new Person { Name = "Roger Federer" },
                    new Person { Name = "Novak Djokovic" },
                };
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to process task: " + ex.Message);
                return new List<Person>();
            }
        });

        task.Start();

        yield return null;

        while (task.State == TaskState.Running)
            yield return null;

        modalPanel.Hide();


        personsListContent.sizeDelta = new Vector2(personsListContent.sizeDelta.x, personsListContent.sizeDelta.y * task.Result.Count);

        foreach (Person p in task.Result)
        {
            GameObject personPanelInstance = Instantiate<GameObject>(personPanelPrefab);
            Text personName = personPanelInstance.GetComponentInChildren<Text>();
            personName.text = p.Name;
            personPanelInstance.transform.SetParent(personsListContent, false);
            AddPersonPanelClickListener(personPanelInstance, p);
        }

        yield return null;
    }

    private void LoadPersonDetails(Person p)
    {
        InputField personName = personDetailsPanel.GetComponentInChildren<InputField>();
        personName.text = p != null ? p.Name : "";
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
