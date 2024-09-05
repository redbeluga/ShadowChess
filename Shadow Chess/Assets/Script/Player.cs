using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
public class Player : NetworkBehaviour
{
  [SerializeField] private GameObject boardObject;
  private Board board;
  public bool white = true;
  [SerializeField] private List<GameObject> chessPieces = new List<GameObject>();
  bool holdingPiece = false;
  // private GameObject activeChessPiece;
  private ChessPiece activeChessPieceScript;
  private Vector2 mousePosition;

  public Board Board { get => board; set => board = value; }
  public bool White { get => white; set => white = value; }

  public override void OnStartClient()
  {
    base.OnStartClient();
    if (IsOwner)
    {
      board = GameObject.FindGameObjectWithTag("Board").GetComponent<Board>();
      ServerAddPlayerToBoard(gameObject);
    }
    else
    {
      GetComponent<Player>().enabled = false;
    }
  }

  [ServerRpc(RequireOwnership = false)]
  private void ServerAddPlayerToBoard(GameObject gameObject)
  {
    // ClientAddPlayerToBoard(gameObject);
    if (board == null)
    {
      board = GameObject.FindGameObjectWithTag("Board").GetComponent<Board>();
    }
    board.AddPlayer(gameObject);
  }

  [ObserversRpc]
  public void SetWhite(bool b, GameObject p)
  {
    if (b)
    {
      p.name = "White";
    }
    else
    {
      p.name = "Black";
    }
    if (IsOwner)
    {
      p.name += " Active";
    }
    p.GetComponent<Player>().white = b;
  }


  // Update is called once per frame
  void Update()
  {
    mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    if (Input.GetKeyDown(KeyCode.B))
    {
      if (white)
      {
        Vector2Int clickedSquare = Board.FindClickedSquare(mousePosition);
        board.ServerCreateChessPiece(Board.whitePawn, clickedSquare);
      }
      else
      {
        Vector2Int clickedSquare = Board.FindClickedSquare(mousePosition);
        board.ServerCreateChessPiece(Board.blackPawn, clickedSquare);
      }
    }

    if (Input.GetMouseButtonDown(0)) // 0 is the left mouse button
    {
      if (holdingPiece) // drop piece
      {
        LocalMovePiece(mousePosition);
      }
      else // get piece
      {
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
        if (hit.collider != null && chessPieces.Contains(hit.collider.gameObject.transform.parent.gameObject))
        {
          holdingPiece = true;
          activeChessPieceScript = hit.collider.gameObject.transform.parent.gameObject.GetComponent<ChessPiece>();
          activeChessPieceScript.GetComponentInChildren<SpriteRenderer>().sortingOrder = 2;
        }
      }
    }

    if (holdingPiece)
    {
      activeChessPieceScript.FollowPointer(mousePosition);
    }
  }
  [ObserversRpc]
  public void AddChessPiece(GameObject newChessPiece)
  {
    if (IsOwner)
    {
      chessPieces.Add(newChessPiece);
      newChessPiece.GetComponent<ChessPiece>().Player = gameObject;
    }
  }

  public void LocalMovePiece(Vector2 mousePosition){
    ServerInitiateMovePiece(mousePosition, activeChessPieceScript, white);
    activeChessPieceScript.GetComponentInChildren<SpriteRenderer>().sortingOrder = 1;
    holdingPiece = false;
    activeChessPieceScript = null;
  }

  [ServerRpc (RequireOwnership = false)]
  public void ServerInitiateMovePiece(Vector2 mousePosition, ChessPiece activeChessPieceScript, bool white)
  {
    Vector2Int clickedSquare = Board.FindClickedSquare(mousePosition);

    int moveValidation;
    if (clickedSquare == new Vector2Int(-1, -1) || !IsMyMove(white)) moveValidation = -1;
    else moveValidation = activeChessPieceScript.ValidateMove(clickedSquare);

    if (moveValidation == -1) // doesn't work
    {
      board.ServerMovePiece(activeChessPieceScript, activeChessPieceScript.CurLoc);
    }
    else if (moveValidation == 1) // works
    {
      activeChessPieceScript.MovedCount++;
      board.ServerMovePiece(activeChessPieceScript, clickedSquare);
      board.ServerChangeMove();
    }
    else
    { // handle take piece
      board.TakePiece(clickedSquare);
      board.ServerMovePiece(activeChessPieceScript, clickedSquare);
      board.ServerChangeMove();
    }
  }
  private bool IsMyMove(bool white)
  {
    return board.WhiteMove == white;
  }
}
