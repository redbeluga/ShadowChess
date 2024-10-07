using System.Collections.Generic;
using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;
using FishNet.Object;
using Task = System.Threading.Tasks.Task;

public class Player : NetworkBehaviour
{
    [SerializeField] private GameObject boardObject;
    [SerializeField] private List<GameObject> chessPieces = new List<GameObject>();
    private bool white = true;
    private Vector2Int kingLoc;
    bool holdingPiece = false;
    private ChessPiece activeChessPieceScript;
    private Vector2 mousePosition;

    public bool White
    {
        get => white;
        set => white = value;
    }

    public Vector2Int KingLoc
    {
        get => kingLoc;
        set => kingLoc = value;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
        {
            GetComponent<Player>().enabled = false;
        }
        else
        {
            InstanceFinder.SceneManager.OnLoadEnd += LobbyManager_OnStartGame;
        }
    }
    
    private void LobbyManager_OnStartGame(SceneLoadEndEventArgs endEventArgs)
    {
        if (endEventArgs.LoadedScenes.Length > 0 && endEventArgs.LoadedScenes[0].name == "Chess" && IsOwner)
        {
            LobbyManager.Instance.InGame = true;
            ServerAddPlayerToBoard(this);
        }
    }

    [ContextMenu("FindInfo")]
    public void FindInfo()
    {
        Debug.Log(Board.Instance);
    }

    void Update()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Board.Instance != null && Input.GetMouseButtonDown(0) && !Board.Instance.GameOver) // 0 is the left mouse button
        {
            if (holdingPiece) // drop piece
            {
                LocalMovePiece(mousePosition);
            }
            else // get piece
            {
                RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
                if (hit.collider != null && chessPieces.Contains(hit.collider.gameObject.transform.parent.gameObject))
                {
                    PickUpPiece(hit);
                }
            }
        }

        if (holdingPiece)
        {
            activeChessPieceScript.FollowPointer(mousePosition);
        }
    }

    private void PickUpPiece(RaycastHit2D hit)
    {
        holdingPiece = true;
        activeChessPieceScript =
            hit.collider.gameObject.transform.parent.gameObject.GetComponent<ChessPiece>();
        activeChessPieceScript.GetComponentInChildren<SpriteRenderer>().sortingOrder = 3;
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerAddPlayerToBoard(Player player)
    {
        if (Board.Instance == null)
        {
            Debug.Log("Board instance is null");
        }
        
        if (!Board.Instance.players.Contains(player))
        {
            Debug.Log("adding player");
            Board.Instance.AddPlayer(player);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerSetWhite(bool b, Player p)
    {
        p.white = b;
    }

    [ObserversRpc]
    public void AddChessPiece(GameObject newChessPiece)
    {
        if (IsOwner)
        {
            chessPieces.Add(newChessPiece);
            newChessPiece.GetComponent<ChessPiece>().Player = this;
        }
    }

    [ObserversRpc]
    public void SetWhite(bool b, Player p)
    {
        if (b)
        {
            p.gameObject.name = "White";
        }
        else
        {
            p.gameObject.name = "Black";
        }

        if (IsOwner)
        {
            p.gameObject.name += " Active";
            Board.Instance.LocalIsWhite = b;
        }

        p.white = b;
        ServerSetWhite(b, p);
    }

    public void LocalMovePiece(Vector2 mousePosition)
    {
        ServerInitiateMovePiece(mousePosition, activeChessPieceScript, white);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerInitiateMovePiece(Vector2 mousePosition, ChessPiece activeChessPieceScript, bool white)
    {
        Vector2Int clickedSquare = Board.Instance.OutputCorrectLocation(Board.Instance.FindClickedSquare(mousePosition), white);

        int moveValidation = activeChessPieceScript.ValidateMove(clickedSquare);

        if (moveValidation == 0) // take a piece
        {
            Board.Instance.TakePiece(clickedSquare);
            Board.Instance.ServerMovePiece(activeChessPieceScript, clickedSquare);
            Board.Instance.ServerPostMoveHandling();
        }
        else if (moveValidation == 1) // normal move
        {
            activeChessPieceScript.MovedCount++;
            Board.Instance.ServerMovePiece(activeChessPieceScript, clickedSquare);
            Board.Instance.ServerPostMoveHandling();
        }
        else if (moveValidation == -2) // en passant
        {
            Board.Instance.TakePiece(new Vector2Int(clickedSquare.x, activeChessPieceScript.CurLoc.y));
            Board.Instance.ServerMovePiece(activeChessPieceScript, clickedSquare);
            Board.Instance.ServerPostMoveHandling();
        }
        else if (moveValidation == 2)
        {
            // castle king
            // handled in king
            Board.Instance.ServerPostMoveHandling();
        }
        else // move invalid, go back to original spot
        {
            Board.Instance.ServerMovePiece(activeChessPieceScript, activeChessPieceScript.CurLoc);
        }

        ServerSetSpriteLayer(activeChessPieceScript.gameObject, 1, white);
    }

    [ServerRpc (RequireOwnership = false)]
    private void ServerSetSpriteLayer(GameObject chessPiece, int layer, bool white)
    {
        LocalSetSpriteLayer(chessPiece, layer, white);
    }
    
    [ObserversRpc]
    private void LocalSetSpriteLayer(GameObject chessPiece, int layer, bool white)
    {
        if (Board.Instance.LocalIsWhite == white)
        {
            holdingPiece = false;
            activeChessPieceScript = null;
            chessPiece.GetComponentInChildren<SpriteRenderer>().sortingOrder = layer;
        }
    }

    public bool IsMyMove(bool white)
    {
        return Board.Instance.WhiteMove == white;
    }
}