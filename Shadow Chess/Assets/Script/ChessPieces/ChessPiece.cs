using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public abstract class ChessPiece : NetworkBehaviour
{
  private GameObject player;
  private Player playerScript;
  private Vector2Int curLoc;
  private int movedCount;
  public abstract bool IsWhite { get; }

  public void FollowPointer(Vector2 mousePosition)
  {
    transform.position = mousePosition;
  }
  public abstract int ValidateMove(Vector2Int newLoc);
  public bool SameTeam(ChessPiece chessPiece)
  {
    return player == chessPiece.player;
  }

  // public abstract bool isAttacking(Vector2Int curLoc);
  public virtual void onCreate()
  {
    MovedCount = 0;
    playerScript = player.GetComponent<Player>();
  }
  public virtual void onDisable()
  {
    curLoc = new Vector2Int(-1, -1);
  }
  public Vector2Int CurLoc { get => CurLoc1; set => CurLoc1 = value; }
  public GameObject Player { get => player; set => player = value; }
  public Vector2Int CurLoc1 { get => curLoc; set => curLoc = value; }
  public Player PlayerScript { get => playerScript; set => playerScript = value; }
  public int MovedCount { get => movedCount; set => movedCount = value; }
}
