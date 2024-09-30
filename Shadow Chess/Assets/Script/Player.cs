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
    public Board board;
    private bool white = true;
    private Vector2Int kingLoc;
    bool holdingPiece = false;
    private ChessPiece activeChessPieceScript;
    private Vector2 mousePosition;

    public Board Board
    {
        get => board;
        set => board = value;
    }

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
    
    private async void LobbyManager_OnStartGame(SceneLoadEndEventArgs endEventArgs)
    {
        if (endEventArgs.LoadedScenes.Length > 0 && endEventArgs.LoadedScenes[0].name == "Chess" && IsOwner)
        {
            GameObject board = GameObject.Find("Board");

            if (board == null)
            {
                await Task.Delay(100);
                LobbyManager_OnStartGame(endEventArgs);
            }
            else
            {
                this.board = board.GetComponent<Board>();
                ServerAddPlayerToBoard(this);
            }
        }
    }

    [ContextMenu("FindInfo")]
    public void FindInfo()
    {
        Debug.Log(board);
    }

    void Update()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // if (Input.GetKeyDown(KeyCode.A))
        // {
        //   board.PrintControlledBoard();
        // }

        if (Input.GetMouseButtonDown(0)) // 0 is the left mouse button
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
                    holdingPiece = true;
                    activeChessPieceScript =
                        hit.collider.gameObject.transform.parent.gameObject.GetComponent<ChessPiece>();
                    activeChessPieceScript.GetComponentInChildren<SpriteRenderer>().sortingOrder = 3;
                }
            }
        }

        if (holdingPiece)
        {
            activeChessPieceScript.FollowPointer(mousePosition);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerAddPlayerToBoard(Player player)
    {
        if (board == null)
        {
            board = GameObject.Find("Board").GetComponent<Board>();
        }
        
        if (!board.players.Contains(player))
        {
            Debug.Log("adding player");
            board.AddPlayer(player);
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
            p.board.LocalIsWhite = b;
        }

        p.white = b;
        ServerSetWhite(b, p);
    }

    public void LocalMovePiece(Vector2 mousePosition)
    {
        ServerInitiateMovePiece(mousePosition, activeChessPieceScript, white);
        activeChessPieceScript.GetComponentInChildren<SpriteRenderer>().sortingOrder = 1;
        holdingPiece = false;
        activeChessPieceScript = null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerInitiateMovePiece(Vector2 mousePosition, ChessPiece activeChessPieceScript, bool white)
    {
        Vector2Int clickedSquare = Board.OutputCorrectLocation(Board.FindClickedSquare(mousePosition), white);

        int moveValidation = activeChessPieceScript.ValidateMove(clickedSquare);

        if (moveValidation == 0) // take a piece
        {
            board.TakePiece(clickedSquare);
            board.ServerMovePiece(activeChessPieceScript, clickedSquare);
            board.ServerPostMoveHandling();
        }
        else if (moveValidation == 1) // normal move
        {
            activeChessPieceScript.MovedCount++;
            board.ServerMovePiece(activeChessPieceScript, clickedSquare);
            board.ServerPostMoveHandling();
        }
        else if (moveValidation == -2) // en passant
        {
            board.TakePiece(new Vector2Int(clickedSquare.x, activeChessPieceScript.CurLoc.y));
            board.ServerMovePiece(activeChessPieceScript, clickedSquare);
            board.ServerPostMoveHandling();
        }
        else if (moveValidation == 2)
        {
            // castle king
            // handled in king
            board.ServerPostMoveHandling();
        }
        else // move invalid, go back to original spot
        {
            board.ServerMovePiece(activeChessPieceScript, activeChessPieceScript.CurLoc);
        }
    }

    public bool IsMyMove(bool white)
    {
        return board.WhiteMove == white;
    }
}