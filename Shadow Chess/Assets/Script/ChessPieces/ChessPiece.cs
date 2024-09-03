using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public abstract class ChessPiece : NetworkBehaviour
{
  private GameObject player;
  private Vector2Int curLoc;
  public abstract bool IsWhite { get; }

  public void FollowPointer(Vector2 mousePosition)
  {
    transform.position = mousePosition;
  }
  public abstract int ValidateMove(Vector2Int newLoc);
  public abstract bool CanTake(Vector2Int newLoc);
  public bool SameTeam(ChessPiece chessPiece){
    return player == chessPiece.player;
  }
  public virtual void onCreate(){

  }
  public Vector2Int CurLoc { get => CurLoc1; set => CurLoc1 = value; }
  public GameObject Player { get => player; set => player = value; }
  public Vector2Int CurLoc1 { get => curLoc; set => curLoc = value; }
}
