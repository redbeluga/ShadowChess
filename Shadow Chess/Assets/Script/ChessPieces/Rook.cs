using System;
using System.Collections.Generic;
using UnityEngine;

public class Rook : ChessPiece
{
  [SerializeField] private bool isWhite;
  public override bool IsWhite
  {
    get { return isWhite; }
  }

  public override bool PieceControlsSpot(Vector2Int loc, GameObject[,] boardOfPieces)
  {
    if ((loc.x - CurLoc.x) * (loc.y - CurLoc.y) != 0) return false;
    int dX = MathF.Sign(loc.x - CurLoc.x);
    int dY = MathF.Sign(loc.y - CurLoc.y);
    for (int i = 1; i <= Math.Max(Math.Abs(loc.y - CurLoc.y), Math.Abs(loc.x - CurLoc.x)) - 1; i++)
    {
      // Debug.Log(new Vector2Int(CurLoc.x + i * dX, CurLoc.y + i * dY));
      if (!PlayerScript.Board.EmptySpotOnBoard(
        new Vector2Int(CurLoc.x + i * dX, CurLoc.y + i * dY)))
      {
        return false;
      }
    }
    return true;
  }

  public override int ValidateMove(Vector2Int newLoc)
  {
    if (base.ValidateMove(newLoc) == -1) return -1;

    if ((newLoc.x - CurLoc.x) * (newLoc.y - CurLoc.y) != 0) return -1;
    int dX = MathF.Sign(newLoc.x - CurLoc.x);
    int dY = MathF.Sign(newLoc.y - CurLoc.y);
    for (int i = 1; i <= Math.Max(Math.Abs(newLoc.y - CurLoc.y), Math.Abs(newLoc.x - CurLoc.x)); i++)
    {
      // Debug.Log(new Vector2Int(CurLoc.x + i * dX, CurLoc.y + i * dY));
      if (!PlayerScript.Board.EmptySpotOnBoard(
        new Vector2Int(CurLoc.x + i * dX, CurLoc.y + i * dY)))
      {
        if (i == Math.Max(Math.Abs(newLoc.y - CurLoc.y), Math.Abs(newLoc.x - CurLoc.x)) && !SameTeam(PlayerScript.Board.FilledBoard[newLoc.x, newLoc.y].GetComponent<ChessPiece>())) return 0;
        else return -1;
      }
    }
    return 1;
  }

  public override List<Vector2Int> GetControlledSpots()
  {
    List<Vector2Int> currentPossibleMoves = new List<Vector2Int>();

    for (int i = 0; i < PossibleMoves.Count; i++)
    {
      for (int j = 0; j < 8; j++)
      {
        int moveValidation = SimpleValidateMove(CurLoc + PossibleMoves[i] * j);
        
        if (moveValidation == 0 || moveValidation == 1)
        {
          currentPossibleMoves.Add(CurLoc + PossibleMoves[i] * j);
        }
        if(moveValidation != 1)
        {
          break;
        }
      }
    }
    return currentPossibleMoves;
  }
  
  public override void InitializePossibleMoves()
  {
    PossibleMoves = new List<Vector2Int>{
      new Vector2Int(1, 0),
      new Vector2Int(-1, 0),
      new Vector2Int(0, 1),
      new Vector2Int(0, -1),
    };
  }
}
