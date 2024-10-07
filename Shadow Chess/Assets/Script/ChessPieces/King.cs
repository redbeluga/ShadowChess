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

    public override void OnCreate()
    {
        base.OnCreate();
        UpdateLoc();
    }

    public void UpdateLoc()
    {
        PlayerScript.KingLoc = CurLoc;
    }

    public override int SimpleValidateMove(Vector2Int newLoc)
    {
        if (base.SimpleValidateMove(newLoc) == -1) return -1;
        if (Math.Abs(newLoc.x - CurLoc.x) == 2)
        {
            if (MovedCount == 0 && CheckCastlePiece(newLoc, false))
            {
                return 1;
            }
            return -1;
        }
        return 1;
    }

    public override bool PieceControlsSpot(Vector2Int loc, GameObject[,] boardOfPieces)
    {
        return Math.Abs(loc.x - CurLoc.x) <= 1 && Math.Abs(loc.y - CurLoc.y) <= 1;
    }

    public override int ValidateMove(Vector2Int newLoc)
    {
        if (base.ValidateMove(newLoc) == -1) return -1;

        if (Math.Abs(newLoc.x - CurLoc.x) != Math.Abs(newLoc.y - CurLoc.y) &&
            (newLoc.x - CurLoc.x) * (newLoc.y - CurLoc.y) != 0) return -1;
        int dX = newLoc.x - CurLoc.x;
        int dY = newLoc.y - CurLoc.y;
        if (MovedCount == 0 && CheckCastlePiece(newLoc, true))
        {
            return 2;
        }

        if (new Vector2(dX, dY).magnitude != 1 && Math.Abs(dX * dY) != 1) return -1;
        if (Board.Instance.EmptySpotOnBoard(new Vector2Int(newLoc.x, newLoc.y))) return 1;
        if (!SameTeam(Board.Instance.FilledBoard[newLoc.x, newLoc.y].GetComponent<ChessPiece>())) return 0;
        return -1;
    }

    private bool CheckCastlePiece(Vector2Int kingNewLoc, bool shouldCastle)
    {
        if (kingNewLoc.y != CurLoc.y || kingNewLoc.x - 2 < 0 || kingNewLoc.x - 1 > 7)
        {
            return false;
        }

        ChessPiece rookPiece;
        Vector2Int rookNewLoc;
        if (kingNewLoc.x - CurLoc.x == 2 &&
            Board.Instance.FilledBoard[kingNewLoc.x + 1, kingNewLoc.y].GetComponent<ChessPiece>() is Rook)
        {
            rookPiece = Board.Instance.FilledBoard[kingNewLoc.x + 1, kingNewLoc.y].GetComponent<ChessPiece>();
            rookNewLoc = new Vector2Int(CurLoc.x + 1, CurLoc.y);
        }
        else if (kingNewLoc.x - CurLoc.x == -2 &&
                 Board.Instance.FilledBoard[kingNewLoc.x - 2, kingNewLoc.y].GetComponent<ChessPiece>() is Rook)
        {
            rookPiece = Board.Instance.FilledBoard[kingNewLoc.x - 2, kingNewLoc.y].GetComponent<ChessPiece>();
            rookNewLoc = new Vector2Int(CurLoc.x - 1, CurLoc.y);
        }
        else
        {
            return false;
        }

        if (!Board.Instance.AllEmptySpacesBetween(rookPiece, this))
        {
            // Debug.Log("dont work");
            return false;
        }

        if(shouldCastle) Board.Instance.CastleKing(this, rookPiece, kingNewLoc, rookNewLoc);
        return true;
    }

    public bool validSpaceBetweenForCastle(ChessPiece rookPiece)
    {
        int minX = Mathf.Min(rookPiece.CurLoc.x, CurLoc.x);
        int minY = Mathf.Min(rookPiece.CurLoc.y, CurLoc.y);
        int maxX = Mathf.Max(rookPiece.CurLoc.x, CurLoc.x);
        int maxY = Mathf.Max(rookPiece.CurLoc.y, CurLoc.y);
        int dx = (maxX - minX) == 0 ? 0 : (int)Mathf.Sign(maxX - minX);
        int dy = (maxY - minY) == 0 ? 0 : (int)Mathf.Sign(maxY - minY);
        Debug.Log(minX + " " + minY + " " + maxX + " " + maxY);
        for (int x = minX + dx, y = minY + dy; x < maxX || y < maxY; x += dx, y += dy)
        {
            if (!Board.Instance.EmptySpotOnBoard(new Vector2Int(x, y))) return false; // see if opp team has control
        }

        return true;
    }

    public override List<Vector2Int> GetControlledSpots()
    {
        List<Vector2Int> currentPossibleMoves = new List<Vector2Int>();

        for (int i = 0; i < PossibleMoves.Count; i++)
        {
            int moveValidation = SimpleValidateMove(CurLoc + PossibleMoves[i]);

            if (moveValidation == 0 || moveValidation == 1)
            {
                currentPossibleMoves.Add(CurLoc + PossibleMoves[i]);
            }
        }

        return currentPossibleMoves;
    }

    public override void InitializePossibleMoves()
    {
        PossibleMoves = new List<Vector2Int>
        {
            new Vector2Int(1, 0),
            new Vector2Int(1, 1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, -1),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0),
        };
    }
}