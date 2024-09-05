using System;
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
    if (MathF.Sign(CurLoc.y - newLoc.y) != validDirection) return -1;

    if (newLoc.x == CurLoc.x && Player.GetComponent<Player>().Board.EmptySpotOnBoard(newLoc))
    {
      if(Math.Abs(newLoc.y - CurLoc.y) == 1){
        return 1;
      }
      else if(Math.Abs(newLoc.y - CurLoc.y) == 2 && MovedCount == 0 && Player.GetComponent<Player>().Board.EmptySpotOnBoard(new Vector2Int(CurLoc.x, CurLoc.y - validDirection))){
        return 1;
      }
      else{
        return -1;
      }
    }
    else
    {
      if (CanTake(newLoc))
      {
        return 0;
      }
      else
      {
        return -1;
      }
    }
  }
  public override bool CanTake(Vector2Int newLoc)
  {
    if (!Player.GetComponent<Player>().Board.EmptySpotOnBoard(newLoc))
    {
      if (!SameTeam(Player.GetComponent<Player>().Board.GetPieceOnBoard(newLoc).GetComponent<ChessPiece>()))
      {
        return true;
      }
    }
    else if (!Player.GetComponent<Player>().Board.EmptySpotOnBoard(new Vector2Int(newLoc.x, CurLoc.y)))
    {
      if (!SameTeam(Player.GetComponent<Player>().Board.GetPieceOnBoard(new Vector2Int(newLoc.x, CurLoc.y)).GetComponent<ChessPiece>()))
      {
        return true;
      }
    }
    return false;
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
