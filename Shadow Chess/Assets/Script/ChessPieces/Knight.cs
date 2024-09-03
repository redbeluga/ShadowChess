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
  public override bool CanTake(Vector2Int newLoc)
  {
    throw new NotImplementedException();
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
    if (!Player.GetComponent<Player>().Board.EmptySpotOnBoard(new Vector2Int(newLoc.x, newLoc.y)))
    {
      return 0;
    }
    return 1;
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
