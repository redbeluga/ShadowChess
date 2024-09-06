using System;
using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
  [SerializeField] private bool isWhite;

  public override bool IsWhite
  {
    get { return isWhite; }
  }
  public bool CanTake(Vector2Int newLoc)
  {
    throw new NotImplementedException();
  }

  public override int ValidateMove(Vector2Int newLoc)
  {
    if (Math.Abs(newLoc.x - CurLoc.x) != Math.Abs(newLoc.y - CurLoc.y) && (newLoc.x - CurLoc.x) * (newLoc.y - CurLoc.y) != 0) return -1;
    int dX = newLoc.x - CurLoc.x;
    int dY = newLoc.y - CurLoc.y;
    if (new Vector2(dX, dY).magnitude != 1 && Math.Abs(dX * dY) != 1) return -1;
    if (!PlayerScript.Board.EmptySpotOnBoard(new Vector2Int(newLoc.x, newLoc.y)))
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
