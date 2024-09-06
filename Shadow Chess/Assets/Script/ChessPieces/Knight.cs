using System;
using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPiece
{
  [SerializeField] private bool isWhite;
  public override bool IsWhite
  {
    get { return isWhite; }
  }

  public override int ValidateMove(Vector2Int newLoc)
  {
    if (
      Math.Abs(newLoc.x - CurLoc.x) > 2 && Math.Abs(newLoc.y - CurLoc.y) > 2 ||
      Math.Abs(newLoc.x - CurLoc.x) == 1 && Math.Abs(newLoc.y - CurLoc.y) != 2 ||
      Math.Abs(newLoc.x - CurLoc.x) == 2 && Math.Abs(newLoc.y - CurLoc.y) != 1
      )
    {
      return -1;
    }
    if (PlayerScript.Board.EmptySpotOnBoard(new Vector2Int(newLoc.x, newLoc.y)))
    {
      return 1;
    }
    if (!SameTeam(PlayerScript.Board.filledBoard[newLoc.x, newLoc.y].GetComponent<ChessPiece>()))
    {
      return 0;
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
