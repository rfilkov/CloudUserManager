using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;

public class ModalPanel : MonoBehaviour {

    public Text messageText;
    public Button abortButton;
    public GameObject modalPanelObject;

    private static ModalPanel modalPanel;

    public static ModalPanel Instance()
    {
        if (!modalPanel)
        {
            modalPanel = FindObjectOfType(typeof(ModalPanel)) as ModalPanel;
            if (!modalPanel)
                Debug.LogError("There needs to be one active ModalPanel script on a GameObject in your scene.");
        }

        return modalPanel;
    }

    public void ShowProgress(string message = null, UnityAction abortEvent = null)
    {
        Show("Abort", message, abortEvent);
    }

    public void ShowMessage(string message, UnityAction closeEvent = null)
    {
        Show("Close", message, closeEvent);
    }

    private void Show(string buttonLabel, string message = null, UnityAction abortEvent = null)
    {
        modalPanelObject.SetActive(true);

        abortButton.GetComponentInChildren<Text>().text = buttonLabel;
        abortButton.onClick.RemoveAllListeners();
        abortButton.onClick.AddListener(Hide);
        if (abortEvent != null)
            abortButton.onClick.AddListener(abortEvent);

        if (message != null)
        {
            messageText.text = message;
        }

        abortButton.gameObject.SetActive(true);
    }

    public void Hide()
    {
        modalPanelObject.SetActive(false);
    }
}
