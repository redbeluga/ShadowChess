using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    public static InGameUI Instance { get; private set; }

    [SerializeField] TextMeshProUGUI playerNameDisplay, opponentNameDisplay;
    [SerializeField] private Color currentMoveColor, nextMoveColor;
    [SerializeField] private Image playerTurnContainer, opponentTurnContainer;
    [SerializeField] private Transform playerCapturedPieces, opponentCapturedPieces;
    [SerializeField] private GameObject displayPiecePrefab;

    private int playerCapturedPiecesCount, opponentCapturedPiecesCount;
    [SerializeField] private float spacing;

    private void Awake()
    {
        Instance = this;
        opponentCapturedPiecesCount = playerCapturedPiecesCount = 0;
        Hide();
    }

    public void ChangeTurns(bool myTurn)
    {
        if (myTurn)
        {
            playerTurnContainer.color = nextMoveColor;
            playerNameDisplay.color = Color.white;

            opponentTurnContainer.color = currentMoveColor;
            opponentNameDisplay.color = Color.black;
        }
        else
        {
            playerTurnContainer.color = currentMoveColor;
            playerNameDisplay.color = Color.black;

            opponentTurnContainer.color = nextMoveColor;
            opponentNameDisplay.color = Color.white;
        }
    }

    public void TakePiece(GameObject takenPiece, bool takenPieceIsWhite)
    {
        GameObject displayPiece = Instantiate(displayPiecePrefab,
            takenPieceIsWhite == Board.Instance.LocalIsWhite ? opponentCapturedPieces : playerCapturedPieces);
        displayPiece.GetComponent<Image>().sprite = takenPiece.GetComponentInChildren<SpriteRenderer>().sprite;

        RectTransform displayPieceRect = displayPiece.GetComponent<RectTransform>();

        int column = (takenPieceIsWhite == Board.Instance.LocalIsWhite
            ? playerCapturedPiecesCount
            : opponentCapturedPiecesCount) % 7;
        int row = (takenPieceIsWhite == Board.Instance.LocalIsWhite
            ? playerCapturedPiecesCount
            : opponentCapturedPiecesCount) / 7;

        displayPieceRect.anchoredPosition = new Vector2(column * (displayPieceRect.sizeDelta.x * 2 / 3), row);
        playerCapturedPiecesCount += takenPieceIsWhite == Board.Instance.LocalIsWhite ? 1 : 0;
        opponentCapturedPiecesCount += takenPieceIsWhite == Board.Instance.LocalIsWhite ? 0 : 1;
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show(string playerName, string opponentName, bool isMyTurn)
    {
        playerNameDisplay.text = playerName;
        opponentNameDisplay.text = opponentName;
        gameObject.SetActive(true);
        ChangeTurns(isMyTurn);
    }
}