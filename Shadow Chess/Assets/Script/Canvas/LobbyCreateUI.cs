using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour, UI_Instance
{
    public static LobbyCreateUI Instance { get; private set; }


    [SerializeField] private Button createButton;
    [SerializeField] private Button lobbyNameButton;
    [SerializeField] private Button publicPrivateButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI publicPrivateText;


    private string lobbyName;
    private bool isPrivate;

    private void Awake()
    {
        Instance = this;

        createButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.CreateLobby(
                lobbyName,
                isPrivate
            );
            Hide();
        });

        lobbyNameButton.onClick.AddListener(() =>
        {
            UI_InputWindow.Show_Static("Lobby Name", "Save",
                "abcdefghijklmnopqrstuvxywzABCDEFGHIJKLMNOPQRSTUVXYWZ .,-'", 20,
                () =>
                {
                    // Cancel
                },
                (string lobbyName) =>
                {
                    Debug.Log(lobbyName);
                    this.lobbyName = lobbyName;
                    UpdateText();
                },
                this);
            Hide();
        });

        publicPrivateButton.onClick.AddListener(() =>
        {
            isPrivate = !isPrivate;
            UpdateText();
        });

        Hide();
    }

    private void UpdateText()
    {
        lobbyNameText.text = lobbyName;
        publicPrivateText.text = isPrivate ? "Private" : "Public";
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);

        lobbyName = LobbyManager.Instance.PlayerName + "'s Match";
        isPrivate = false;

        UpdateText();
    }
}