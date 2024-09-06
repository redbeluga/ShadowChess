using System;
using UnityEngine;

public class Bishop : ChessPiece
{
  [SerializeField] private bool isWhite;

  public override bool IsWhite
  {
    get { return isWhite; }
  }

  public override int ValidateMove(Vector2Int newLoc)
  {
    if (Math.Abs(newLoc.x - CurLoc.x) != Math.Abs(newLoc.y - CurLoc.y)) return -1;
    int dX = MathF.Sign(newLoc.x - CurLoc.x);
    int dY = MathF.Sign(newLoc.y - CurLoc.y);
    for (int i = 1; i <= Math.Abs(newLoc.x - CurLoc.x); i++)
    {
      if (!PlayerScript.Board.EmptySpotOnBoard(
        new Vector2Int(CurLoc.x + i * dX, CurLoc.y + i * dY)))
      {
        if (i == Math.Abs(newLoc.x - CurLoc.x) && !SameTeam(PlayerScript.Board.filledBoard[newLoc.x, newLoc.y].GetComponent<ChessPiece>())) return 0;
        else return -1;
      }
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
