using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance {get; private set;}

    [SerializeField] private TextMeshProUGUI winnerDisplayText;
    [SerializeField] private TextMeshProUGUI reason;
    [SerializeField] private Button backToLobbyButton;

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
        
        backToLobbyButton.onClick.AddListener(() =>
        {
            Hide();
            LobbyManager.Instance.LeaveLobby(true);
        });
    }

    public void Show(bool winner, string reason)
    {
        // blockerPanel.SetActive(true);
        winnerDisplayText.text = winner ? "White Won" : "Black Won";
        this.reason.text = reason;
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        // blockerPanel.SetActive(false);
        gameObject.SetActive(false);
    }
}
