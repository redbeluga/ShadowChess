using System;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    [SerializeField] private bool isWhite;
    public int validDirection;

    public override bool IsWhite
    {
        get { return isWhite; }
    }

    public override void OnCreate()
    {
        if (isWhite)
        {
            validDirection = 1;
        }
        else
        {
            validDirection = -1;
        }

        base.OnCreate();
    }

    public override int SimpleValidateMove(Vector2Int newLoc)
    {
        if (base.SimpleValidateMove(newLoc) == -1) return -1;
        if (Math.Abs(newLoc.y-CurLoc.y) == 2 && MovedCount == 0 && Board.Instance.EmptySpotOnBoard(newLoc)) return 1;
        if (Math.Abs(newLoc.y-CurLoc.y) == 1 && newLoc.x == CurLoc.x && Board.Instance.EmptySpotOnBoard(newLoc)) return 1;
        if (CanTake(newLoc) != -1) return 1;
        return -1;
    }

    public override int ValidateMove(Vector2Int newLoc)
    {
        if (base.ValidateMove(newLoc) == -1) return -1;

        if (MathF.Sign(CurLoc.y - newLoc.y) != validDirection) return -1;

        if (newLoc.x == CurLoc.x && Board.Instance.EmptySpotOnBoard(newLoc))
        {
            if (Math.Abs(newLoc.y - CurLoc.y) == 1)
            {
                return 1;
            }
            else if (Math.Abs(newLoc.y - CurLoc.y) == 2 && MovedCount == 0 &&
                     Board.Instance.EmptySpotOnBoard(new Vector2Int(CurLoc.x, CurLoc.y - validDirection)))
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
            if (!Board.Instance.EmptySpotOnBoard(newLoc) &&
                !SameTeam(Board.Instance.FilledBoard[newLoc.x, newLoc.y].GetComponent<ChessPiece>()))
            {
                return 0;
            }

            if (Board.Instance.EmptySpotOnBoard(newLoc) &&
                !Board.Instance.EmptySpotOnBoard(new Vector2Int(newLoc.x, CurLoc.y)))
            {
                ChessPiece pieceAtNewLoc =
                    Board.Instance.FilledBoard[newLoc.x, CurLoc.y].GetComponent<ChessPiece>();

                if (pieceAtNewLoc.MovedCount == 1 && !SameTeam(pieceAtNewLoc) && pieceAtNewLoc is Pawn) return -2;
            }
        }

        return -1;
    }

    public override bool PieceControlsSpot(Vector2Int loc, GameObject[,] boardOfPieces)
    {
        if (loc.y == CurLoc.y - validDirection && Math.Abs(loc.x - CurLoc.x) == 1)
        {
            if (!Board.Instance.EmptySpotOnBoard(loc))
            {
                return true;
            }
        }

        return false;
    }

    public override List<Vector2Int> GetControlledSpots()
    {
        List<Vector2Int> currentPossibleMoves = new List<Vector2Int>();

        for (int i = 0; i < PossibleMoves.Count; i++)
        {
            int moveValidation = SimpleValidateMove(CurLoc + PossibleMoves[i]);
            if (moveValidation == 1 || moveValidation == 0 || moveValidation == -2)
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
            new Vector2Int(0, -validDirection),
            new Vector2Int(1, -validDirection),
            new Vector2Int(-1, -validDirection),
            new Vector2Int(0, -validDirection * 2)
        };
    }
}