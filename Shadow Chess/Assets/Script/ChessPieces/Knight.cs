using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPiece
{
  [SerializeField] private bool isWhite;
  public override bool IsWhite
  {
    get { return isWhite; }
  }

  public override bool PieceControlsSpot(Vector2Int loc, GameObject[,] boardOfPieces)
  {
    int dx = Mathf.Abs(loc.x - CurLoc.x);
    int dy = Mathf.Abs(loc.y - CurLoc.y);

    // Check for "L" shape movement
    if (!((dx == 2 && dy == 1) || (dx == 1 && dy == 2)))
    {
      return false;
    }
    return true;
  }

  public override int ValidateMove(Vector2Int newLoc)
  {
    if(base.ValidateMove(newLoc) == -1) return -1; 

    int dx = Mathf.Abs(newLoc.x - CurLoc.x);
    int dy = Mathf.Abs(newLoc.y - CurLoc.y);

    // Check for "L" shape movement
    if (!((dx == 2 && dy == 1) || (dx == 1 && dy == 2)))
    {
      return -1;
    }
    if (Board.Instance.EmptySpotOnBoard(new Vector2Int(newLoc.x, newLoc.y)))
    {
      return 1;
    }
    if (!SameTeam(Board.Instance.FilledBoard[newLoc.x, newLoc.y].GetComponent<ChessPiece>()))
    {
      return 0;
    }
    return -1;
  }

  public override List<Vector2Int> GetControlledSpots(){
    List<Vector2Int> currentPossibleMoves = new List<Vector2Int>();
    
    foreach(Vector2Int deltaPos in PossibleMoves){
      int moveValidation = SimpleValidateMove(CurLoc + deltaPos);
      if(moveValidation == 0 || moveValidation == 1){
        currentPossibleMoves.Add(CurLoc + deltaPos);
      }
    }

    return currentPossibleMoves;
  }
  

  public override void InitializePossibleMoves()
  {
    PossibleMoves = new List<Vector2Int>{
        new Vector2Int(1, 2),
        new Vector2Int(1, -2),
        new Vector2Int(-1, 2),
        new Vector2Int(-1, -2),
        new Vector2Int(2, 1),
        new Vector2Int(2, -1),
        new Vector2Int(-2, 1),
        new Vector2Int(-2, -1),
    };
  }
}
