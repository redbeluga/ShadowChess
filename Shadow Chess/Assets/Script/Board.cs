using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
public class Board : NetworkBehaviour
{
  [SerializeField] public GameObject blackPawn, blackKnight, blackRook, blackBishop, blackQueen, blackKing;
  [SerializeField] public GameObject whitePawn, whiteKnight, whiteRook, whiteBishop, whiteQueen, whiteKing;
  [SerializeField] public List<GameObject> players;
  [SerializeField] private bool whiteMove = true;
  private Vector3 topLeft, bottomRight;
  private float length, gridLength;
  private Vector3[,] gridCenterLocation = new Vector3[8, 8];
  public GameObject[,] filledBoard = new GameObject[8, 8];
  public List<GameObject> activeWhitePieces, activeBlackPieces, inActiveWhitePieces, inActiveBlackPieces;

  public bool WhiteMove { get => whiteMove; set => whiteMove = value; }

  void Awake()
  {
    // Get the SpriteRenderer component attached to the GameObject
    SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

    // Get the bounds of the sprite in world space
    Bounds spriteBounds = spriteRenderer.bounds;

    // Calculate the top-right and bottom-left corners
    topLeft = new Vector3(spriteBounds.min.x, spriteBounds.max.y, spriteBounds.center.z);
    bottomRight = new Vector3(spriteBounds.max.x, spriteBounds.min.y, spriteBounds.center.z);

    length = bottomRight.x - topLeft.x;
    gridLength = length / 8;

    for (int i = 0; i < 8; i++)
    {
      for (int j = 0; j < 8; j++)
      {
        gridCenterLocation[i, j] = new Vector3(topLeft.x + (i + 0.5f) * gridLength, topLeft.y - (j + 0.5f) * gridLength, spriteBounds.center.x);
      }
    }
  }
  public void Spawn32Pieces()
  {
    for (int i = 0; i < 8; i++)
    {
      ServerCreateChessPiece(whitePawn, new Vector2Int(i, 6));
    }
    ServerCreateChessPiece(whiteRook, new Vector2Int(0, 7));
    ServerCreateChessPiece(whiteRook, new Vector2Int(7, 7));
    ServerCreateChessPiece(whiteKnight, new Vector2Int(1, 7));
    ServerCreateChessPiece(whiteKnight, new Vector2Int(6, 7));
    ServerCreateChessPiece(whiteBishop, new Vector2Int(2, 7));
    ServerCreateChessPiece(whiteBishop, new Vector2Int(5, 7));
    ServerCreateChessPiece(whiteQueen, new Vector2Int(3, 7));
    ServerCreateChessPiece(whiteKing, new Vector2Int(4, 7));

    for (int i = 0; i < 8; i++)
    {
      ServerCreateChessPiece(blackPawn, new Vector2Int(i, 1));
    }
    ServerCreateChessPiece(blackRook, new Vector2Int(0, 0));
    ServerCreateChessPiece(blackRook, new Vector2Int(7, 0));
    ServerCreateChessPiece(blackKnight, new Vector2Int(1, 0));
    ServerCreateChessPiece(blackKnight, new Vector2Int(6, 0));
    ServerCreateChessPiece(blackBishop, new Vector2Int(2, 0));
    ServerCreateChessPiece(blackBishop, new Vector2Int(5, 0));
    ServerCreateChessPiece(blackQueen, new Vector2Int(3, 0));
    ServerCreateChessPiece(blackKing, new Vector2Int(4, 0));
  }
  public bool EmptySpotOnBoard(Vector2Int loc)
  {
    return filledBoard[loc.x, loc.y] == null;
  }
  public GameObject GetPieceOnBoard(Vector2Int loc)
  {
    return filledBoard[loc.x, loc.y];
  }
  public void AddPlayer(GameObject p)
  {
    players.Add(p);
    if (players.Count == 2)
    {
      AssignSides();
      Spawn32Pieces();
    }
  }
  public void AssignSides()
  {
    float randVal = Random.value;
    if (randVal > 0.5)
    {
      (players[0], players[1]) = (players[1], players[0]);
    }
    players[0].GetComponent<Player>().SetWhite(true, players[0]);
    players[1].GetComponent<Player>().SetWhite(false, players[1]);
  }

  [ServerRpc(RequireOwnership = false)]
  public void ServerCreateChessPiece(GameObject chessPiece, Vector2Int loc)
  {
    GameObject newChessPiece = Instantiate(chessPiece, gridCenterLocation[loc.x, loc.y], Quaternion.identity);
    ServerManager.Spawn(newChessPiece);

    filledBoard[loc.x, loc.y] = newChessPiece;
    newChessPiece.GetComponent<ChessPiece>().CurLoc = loc;
    newChessPiece.GetComponent<ChessPiece>().onCreate();

    if (newChessPiece.GetComponent<ChessPiece>().IsWhite)
    {
      activeWhitePieces.Add(newChessPiece);
      newChessPiece.GetComponent<ChessPiece>().Player = players[0];
      players[0].GetComponent<Player>().AddChessPiece(newChessPiece);
    }
    else
    {
      activeBlackPieces.Add(newChessPiece);
      newChessPiece.GetComponent<ChessPiece>().Player = players[1];
      players[1].GetComponent<Player>().AddChessPiece(newChessPiece);
    }
  }

  private void OnDrawGizmos()
  {
    SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
    // Get the bounds of the sprite in world space
    Bounds spriteBounds = spriteRenderer.bounds;

    // Calculate the top-right and bottom-left corners
    topLeft = new Vector3(spriteBounds.min.x, spriteBounds.max.y, spriteBounds.center.z);
    bottomRight = new Vector3(spriteBounds.max.x, spriteBounds.min.y, spriteBounds.center.z);

    // // Optionally, draw lines connecting the corners
    // Gizmos.color = Color.green;
    // Gizmos.DrawLine(bottomRight, topLeft);


    // if (Application.isPlaying)
    // {
    //   // Set the Gizmo color
    //   Gizmos.color = Color.red;
    //   for (int i = 0; i < 8; i++)
    //   {
    //     for (int j = 0; j < 8; j++)
    //     {
    //       Gizmos.DrawCube(gridCenterLocation[i, j], Vector3.one * 0.1f);
    //     }
    //   }
    // }
  }

  public Vector2Int FindClickedSquare(Vector2 mousePosition)
  {
    float closestDistance = Mathf.Infinity;
    Vector2Int clickedLoc = Vector2Int.zero;

    if (mousePosition.y < bottomRight.y || mousePosition.y > topLeft.y || mousePosition.x > bottomRight.x || mousePosition.x < topLeft.x)
    {
      return new Vector2Int(-1, -1);
    }

    for (int i = 0; i < 8; i++)
    {
      for (int j = 0; j < 8; j++)
      {
        float distance = Vector3.Distance(mousePosition, gridCenterLocation[i, j]);
        if (distance < closestDistance)
        {
          closestDistance = distance;
          clickedLoc = new Vector2Int(i, j);
        }
      }
    }
    return clickedLoc;
  }

  public Vector2 CenterOfSquare(Vector2Int clickedSquare)
  {
    return gridCenterLocation[clickedSquare.x, clickedSquare.y];
  }

  [ServerRpc(RequireOwnership = false)]
  public void ServerMovePiece(ChessPiece activeChessPiece, Vector2Int gridLoc)
  {
    // Debug.Log(gridLoc);
    Vector2 loc = CenterOfSquare(gridLoc);
    filledBoard[activeChessPiece.CurLoc.x, activeChessPiece.CurLoc.y] = null;
    filledBoard[gridLoc.x, gridLoc.y] = activeChessPiece.gameObject;
    activeChessPiece.CurLoc = gridLoc;
    ClientMovePiece(activeChessPiece, gridLoc, loc);
  }
  [ObserversRpc]
  public void ClientMovePiece(ChessPiece activeChessPiece, Vector2Int gridLoc, Vector2 loc)
  {
    activeChessPiece.transform.position = loc;
  }
  [ServerRpc(RequireOwnership = false)]
  public void ServerChangeMove()
  {
    whiteMove = !whiteMove;
  }
  [ServerRpc(RequireOwnership = false)]
  public void TakePiece(Vector2Int locToTake){
    GameObject takenChessPiece = filledBoard[locToTake.x, locToTake.y];
    if(takenChessPiece.GetComponent<ChessPiece>().IsWhite){
      // Debug.Log("REmove");
      activeWhitePieces.Remove(takenChessPiece);
      inActiveWhitePieces.Add(takenChessPiece);
    }
    else{
      // Debug.Log("REmove");
      activeBlackPieces.Remove(takenChessPiece);
      inActiveBlackPieces.Add(takenChessPiece);
    }
    takenChessPiece.GetComponent<ChessPiece>().onDisable();
    LocalDisablePiece(takenChessPiece);
  }
  [ObserversRpc]
  public void LocalDisablePiece(GameObject disabledChessPiece){
    disabledChessPiece.SetActive(false);
  }
}
