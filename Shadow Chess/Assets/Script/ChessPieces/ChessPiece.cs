using System;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public abstract class ChessPiece : NetworkBehaviour
{
  private Player player;
  private List<Vector2Int> possibleMoves; 
  private SpriteRenderer spriteRenderer;
  private Player playerScript;
  [SerializeField] private Vector2Int curLoc;
  private int movedCount;
  public abstract bool IsWhite { get; }

  public void FollowPointer(Vector2 mousePosition)
  {
    transform.position = mousePosition;
  }

  private void Awake()
  {
    spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    DisableSprite();
  }

  public void EnableSprite()
  {
    spriteRenderer.enabled = true;
  }
  
  public void DisableSprite()
  {
    spriteRenderer.enabled = false;
  }

  public virtual int SimpleValidateMove(Vector2Int newLoc)
  {
    if (!Board.LocInBounds(newLoc)) return -1;

    if (Board.Instance.EmptySpotOnBoard(newLoc))
    {
      if (SameTeam(Board.Instance.FilledBoard[newLoc.x, newLoc.y].GetComponent<ChessPiece>()))
      {
        return -1;
      }

      return 0;
    }

    return 1;
  }

  public virtual int ValidateMove(Vector2Int newLoc)
  {
    if (!playerScript.IsMyMove(playerScript.White) || !Board.LocInBounds(newLoc) || newLoc == curLoc)
    {
      return -1;
    }

    return 1;
  }

  public bool SameTeam(ChessPiece chessPiece)
  {
    return player == chessPiece.player;
  }

  // public abstract bool isAttacking(Vector2Int curLoc);
  public virtual void OnCreate()
  {
    MovedCount = 0;
    playerScript = player.GetComponent<Player>();
    InitializePossibleMoves();
  }
  public virtual void onDisable()
  {
    curLoc = new Vector2Int(-1, -1);
  }

  public abstract List<Vector2Int> GetControlledSpots();
  public Vector2Int CurLoc { get => CurLoc1; set => CurLoc1 = value; }
  public Player Player { get => player; set => player = value; }
  public Vector2Int CurLoc1 { get => curLoc; set => curLoc = value; }
  public Player PlayerScript { get => playerScript; set => playerScript = value; }
  public int MovedCount { get => movedCount; set => movedCount = value; }
  public List<Vector2Int> PossibleMoves { get => possibleMoves; set => possibleMoves = value; }

  public abstract bool PieceControlsSpot(Vector2Int loc, GameObject[,] boardOfPieces);
  // public abstract bool PieceControlsSpot(Vector2Int loc, string[,] boardOfPieces, Vector2Int curLoc);

  public abstract void InitializePossibleMoves();
}
