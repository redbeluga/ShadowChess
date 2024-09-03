using System;
using UnityEngine;

public class Rook : ChessPiece
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
    if ((newLoc.x - CurLoc.x) * (newLoc.y - CurLoc.y) != 0) return -1;
    int dX = MathF.Sign(newLoc.x - CurLoc.x);
    int dY = MathF.Sign(newLoc.y - CurLoc.y);
    for (int i = 1; i <= Math.Max(Math.Abs(newLoc.y - CurLoc.y), Math.Abs(newLoc.x - CurLoc.x)); i++)
    {
      Debug.Log(new Vector2Int(CurLoc.x + i * dX, CurLoc.y + i * dY));
      if (!Player.GetComponent<Player>().Board.EmptySpotOnBoard(
        new Vector2Int(CurLoc.x + i * dX, CurLoc.y + i * dY)))
      {
        if (i == Math.Abs(newLoc.x - CurLoc.x)) return 0;
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
