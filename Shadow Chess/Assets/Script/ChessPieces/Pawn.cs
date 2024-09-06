using System;
using Unity.VisualScripting;
using UnityEngine;

public class Pawn : ChessPiece
{
  [SerializeField] private bool isWhite;
  public int validDirection;
  public override bool IsWhite
  {
    get { return isWhite; }
  }
  public override void onCreate()
  {
    base.onCreate();
    if (isWhite)
    {
      validDirection = 1;
    }
    else
    {
      validDirection = -1;
    }
  }
  public override int ValidateMove(Vector2Int newLoc)
  {
    Debug.Log(PlayerScript.gameObject);
    if (MathF.Sign(CurLoc.y - newLoc.y) != validDirection) return -1;

    if (newLoc.x == CurLoc.x && PlayerScript.Board.EmptySpotOnBoard(newLoc))
    {
      if (Math.Abs(newLoc.y - CurLoc.y) == 1)
      {
        return 1;
      }
      else if (Math.Abs(newLoc.y - CurLoc.y) == 2 && MovedCount == 0 && PlayerScript.Board.EmptySpotOnBoard(new Vector2Int(CurLoc.x, CurLoc.y - validDirection)))
      {
        return 1;
      }
      else
      {
        return -1;
      }
    }
    else
    {
      return CanTake(newLoc);
    }
  }
  public int CanTake(Vector2Int newLoc)
  {
    if (newLoc.y == CurLoc.y - validDirection && Math.Abs(newLoc.x - CurLoc.x) == 1)
    {
      if (!PlayerScript.Board.EmptySpotOnBoard(newLoc))
      {
        return 0;
      }
      if (PlayerScript.Board.EmptySpotOnBoard(newLoc) && !PlayerScript.Board.EmptySpotOnBoard(new Vector2Int(newLoc.x, CurLoc.y)))
      {
        ChessPiece pieceAtNewLoc = PlayerScript.Board.filledBoard[newLoc.x, CurLoc.y].GetComponent<ChessPiece>();

        if (pieceAtNewLoc.MovedCount == 1 && !SameTeam(pieceAtNewLoc) && pieceAtNewLoc is Pawn) return -2;
      }
    }
    return -1;
  }

  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {

  }
}
