using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class Board : NetworkBehaviour
{
    [SerializeField] private bool whiteMove = true;
    [SerializeField] private bool whiteKingCheck = false;
    [SerializeField] private bool blackKingCheck = false;
    [SerializeField] private bool whiteKingCheckmate = false;
    [SerializeField] private bool blackKingCheckmate = false;

    [SerializeField] public GameObject blackPawn, blackKnight, blackRook, blackBishop, blackQueen, blackKing;
    [SerializeField] public GameObject whitePawn, whiteKnight, whiteRook, whiteBishop, whiteQueen, whiteKing;

    [SerializeField] private List<GameObject> players;

    private List<GameObject> activeWhitePieces, activeBlackPieces, inActiveWhitePieces, inActiveBlackPieces;

    private Vector3 topLeft, bottomRight;
    private float length, gridLength;
    private Vector3[,] gridCenterLocation = new Vector3[8, 8];

    private GameObject[,] filledBoard = new GameObject[8, 8];
    private bool[,] whiteControlledBoard = new bool[8, 8]; 
    private bool[,] blackControlledBoard = new bool[8, 8];

    public bool WhiteMove
    {
        get => whiteMove;
        set => whiteMove = value;
    }

    public GameObject[,] FilledBoard
    {
        get => filledBoard;
        set => filledBoard = value;
    }

    // public string[,] FilledBoardNames { get => filledBoardNames; set => filledBoardNames = value; }
    public bool WhiteKingCheck
    {
        get => whiteKingCheck;
        set => whiteKingCheck = value;
    }

    public bool BlackKingCheck
    {
        get => blackKingCheck;
        set => blackKingCheck = value;
    }

    public static bool LocInBounds(Vector2Int newLoc)
    {
        return (newLoc.x >= 0 && newLoc.x <= 7 && newLoc.y >= 0 && newLoc.y <= 7);
    }

    void Awake()
    {
        // Get the SpriteRenderer component attached to the GameObject
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        activeWhitePieces = new List<GameObject>();
        activeBlackPieces = new List<GameObject>();
        inActiveWhitePieces = new List<GameObject>();
        inActiveBlackPieces = new List<GameObject>();


        // Get the bounds of the sprite in world space
        Bounds spriteBounds = spriteRenderer.bounds;

        // Calculate the top-right and bottom-left corners
        topLeft = new Vector3(spriteBounds.min.x, spriteBounds.max.y, spriteBounds.center.z);
        bottomRight = new Vector3(spriteBounds.max.x, spriteBounds.min.y, spriteBounds.center.z);

        length = bottomRight.x - topLeft.x;
        gridLength = length / 8;

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                gridCenterLocation[i, j] = new Vector3(topLeft.x + (i + 0.5f) * gridLength,
                    topLeft.y - (j + 0.5f) * gridLength, spriteBounds.center.x);
            }
        }
    }

    public void Spawn32Pieces()
    {
        for (int i = 0; i < 8; i++)
        {
            ServerCreateChessPiece(whitePawn, new Vector2Int(i, 6));
        }

        ServerCreateChessPiece(whiteKing, new Vector2Int(4, 7));
        ServerCreateChessPiece(whiteRook, new Vector2Int(0, 7));
        ServerCreateChessPiece(whiteRook, new Vector2Int(7, 7));
        ServerCreateChessPiece(whiteKnight, new Vector2Int(1, 7));
        ServerCreateChessPiece(whiteKnight, new Vector2Int(6, 7));
        ServerCreateChessPiece(whiteBishop, new Vector2Int(2, 7));
        ServerCreateChessPiece(whiteBishop, new Vector2Int(5, 7));
        ServerCreateChessPiece(whiteQueen, new Vector2Int(3, 7));

        for (int i = 0; i < 8; i++)
        {
            ServerCreateChessPiece(blackPawn, new Vector2Int(i, 1));
        }

        ServerCreateChessPiece(blackKing, new Vector2Int(4, 0));
        ServerCreateChessPiece(blackRook, new Vector2Int(0, 0));
        ServerCreateChessPiece(blackRook, new Vector2Int(7, 0));
        ServerCreateChessPiece(blackKnight, new Vector2Int(1, 0));
        ServerCreateChessPiece(blackKnight, new Vector2Int(6, 0));
        ServerCreateChessPiece(blackBishop, new Vector2Int(2, 0));
        ServerCreateChessPiece(blackBishop, new Vector2Int(5, 0));
        ServerCreateChessPiece(blackQueen, new Vector2Int(3, 0));
    }

    public bool EmptySpotOnBoard(Vector2Int loc)
    {
        return FilledBoard[loc.x, loc.y] == null;
    }

    public GameObject GetPieceOnBoard(Vector2Int loc)
    {
        return FilledBoard[loc.x, loc.y];
    }

    public void AddPlayer(GameObject p)
    {
        players.Add(p);
        if (players.Count == 2)
        {
            AssignSides();
            Spawn32Pieces();
            SweepControl(true);
            SweepControl(false);
        }
    }

    public void AssignSides()
    {
        float randVal = Random.value;
        if (randVal > 0.5)
        {
            (players[0], players[1]) = (players[1], players[0]);
        }

        players[0].GetComponent<Player>().SetWhite(true, players[0]);
        players[1].GetComponent<Player>().SetWhite(false, players[1]);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerCreateChessPiece(GameObject chessPiece, Vector2Int loc)
    {
        GameObject newChessPiece = Instantiate(chessPiece, gridCenterLocation[loc.x, loc.y], Quaternion.identity);
        ServerManager.Spawn(newChessPiece);

        FilledBoard[loc.x, loc.y] = newChessPiece;
        // filledBoardNames[loc.x, loc.y] = newChessPiece.gameObject.name;
        newChessPiece.GetComponent<ChessPiece>().CurLoc = loc;

        if (newChessPiece.GetComponent<ChessPiece>().IsWhite)
        {
            activeWhitePieces.Add(newChessPiece);
            newChessPiece.GetComponent<ChessPiece>().Player = players[0];
            players[0].GetComponent<Player>().AddChessPiece(newChessPiece);
        }
        else
        {
            activeBlackPieces.Add(newChessPiece);
            newChessPiece.GetComponent<ChessPiece>().Player = players[1];
            players[1].GetComponent<Player>().AddChessPiece(newChessPiece);
        }

        newChessPiece.GetComponent<ChessPiece>().OnCreate();
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    bool whiteControl = SpotUnderControl(new Vector2Int(i, j), true);
                    bool blackControl = SpotUnderControl(new Vector2Int(i, j), false);
                    // // if (whiteControl && blackControl)
                    // // {
                    // //     Gizmos.color = Color.blue;
                    // // }
                    // else if (whiteControl)
                    // {
                    //     Gizmos.color = Color.white;
                    // }
                    // else if (blackControl)
                    // {
                    //     Gizmos.color = Color.black;
                    // }
                    // else
                    // {
                    //     Gizmos.color = Color.red;
                    // }
                    if (whiteControl) Gizmos.color = Color.white;
                    else Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(CenterOfSquare(new Vector2Int(i ,j)), 0.15f);
                }
            }
        }
    }

    public Vector2Int FindClickedSquare(Vector2 mousePosition)
    {
        float closestDistance = Mathf.Infinity;
        Vector2Int clickedLoc = Vector2Int.zero;

        if (mousePosition.y < bottomRight.y || mousePosition.y > topLeft.y || mousePosition.x > bottomRight.x ||
            mousePosition.x < topLeft.x)
        {
            return new Vector2Int(-1, -1);
        }

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                float distance = Vector3.Distance(mousePosition, gridCenterLocation[i, j]);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    clickedLoc = new Vector2Int(i, j);
                }
            }
        }

        return clickedLoc;
    }

    public Vector2 CenterOfSquare(Vector2Int clickedSquare)
    {
        return gridCenterLocation[clickedSquare.x, clickedSquare.y];
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerMovePiece(ChessPiece activeChessPiece, Vector2Int gridLoc)
    {
        // Debug.Log(gridLoc);
        Vector2 loc = CenterOfSquare(gridLoc);
        FilledBoard[activeChessPiece.CurLoc.x, activeChessPiece.CurLoc.y] = null;
        // FilledBoardNames[activeChessPiece.CurLoc.x, activeChessPiece.CurLoc.y] = null;
        FilledBoard[gridLoc.x, gridLoc.y] = activeChessPiece.gameObject;
        // FilledBoardNames[gridLoc.x, gridLoc.y] = activeChessPiece.gameObject.name;
        activeChessPiece.CurLoc = gridLoc;

        if (activeChessPiece is King)
        {
            ((King)activeChessPiece).UpdateLoc();
        }

        ClientMovePiece(activeChessPiece, gridLoc, loc);
    }

    [ObserversRpc]
    public void ClientMovePiece(ChessPiece activeChessPiece, Vector2Int gridLoc, Vector2 loc)
    {
        activeChessPiece.transform.position = loc;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerPostMoveHandling()
    {
        whiteMove = !whiteMove;
        
        SweepControl(true);
        SweepControl(false);

        // whiteKingCheck = KingUnderCheck(true);
        // blackKingCheck = KingUnderCheck(false);
        //
        // whiteKingCheckmate = KingUnderCheckmate(true);
        // blackKingCheckmate = KingUnderCheckmate(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakePiece(Vector2Int locToTake)
    {
        GameObject takenChessPiece = FilledBoard[locToTake.x, locToTake.y];
        if (takenChessPiece.GetComponent<ChessPiece>().IsWhite)
        {
            // Debug.Log("REmove");
            activeWhitePieces.Remove(takenChessPiece);
            inActiveWhitePieces.Add(takenChessPiece);
        }
        else
        {
            // Debug.Log("REmove");
            activeBlackPieces.Remove(takenChessPiece);
            inActiveBlackPieces.Add(takenChessPiece);
        }

        takenChessPiece.GetComponent<ChessPiece>().onDisable();
        LocalDisablePiece(takenChessPiece);
    }

    [ObserversRpc]
    public void LocalDisablePiece(GameObject disabledChessPiece)
    {
        disabledChessPiece.SetActive(false);
    }

    public void CastleKing(ChessPiece kingPiece, ChessPiece rookPiece, Vector2Int kingNewLoc, Vector2Int rookNewloc)
    {
        ServerMovePiece(kingPiece, kingNewLoc);
        ServerMovePiece(rookPiece, rookNewloc);
    }

    public bool AllEmptySpacesBetween(ChessPiece A, ChessPiece B)
    {
        int minX = Mathf.Min(A.CurLoc.x, B.CurLoc.x);
        int minY = Mathf.Min(A.CurLoc.y, B.CurLoc.y);
        int maxX = Mathf.Max(A.CurLoc.x, B.CurLoc.x);
        int maxY = Mathf.Max(A.CurLoc.y, B.CurLoc.y);
        int dx = (maxX - minX) == 0 ? 0 : (int)Mathf.Sign(maxX - minX);
        int dy = (maxY - minY) == 0 ? 0 : (int)Mathf.Sign(maxY - minY);
        Debug.Log(minX + " " + minY + " " + maxX + " " + maxY);
        for (int x = minX + dx, y = minY + dy; x < maxX || y < maxY; x += dx, y += dy)
        {
            Debug.Log(x + " " + y);
            if (!EmptySpotOnBoard(new Vector2Int(x, y))) return false;
        }

        return true;
    }

    // public bool KingUnderCheckNextMove(bool whiteKing, ChessPiece movedChessPiece, Vector2Int newLoc)
    // {
    //     // Sim Move
    //     GameObject pieceOnNewLoc = filledBoard[newLoc.x, newLoc.y];
    //     Vector2Int startLoc = movedChessPiece.CurLoc;
    //     filledBoard[startLoc.x, startLoc.y] = null;
    //     filledBoard[newLoc.x, newLoc.y] = movedChessPiece.gameObject;
    //     movedChessPiece.CurLoc = newLoc;
    //     if (movedChessPiece is King)
    //     {
    //         ((King)movedChessPiece).UpdateLoc();
    //     }
    //
    //     if (pieceOnNewLoc != null)
    //     {
    //         if (whiteKing)
    //         {
    //             activeBlackPieces.Remove(pieceOnNewLoc);
    //         }
    //         else
    //         {
    //             activeWhitePieces.Remove(pieceOnNewLoc);
    //         }
    //     }
    //
    //     // bool kingUnderCheck = KingUnderCheck(whiteKing);
    //
    //     // Undo sim move
    //     filledBoard[startLoc.x, startLoc.y] = movedChessPiece.gameObject;
    //     filledBoard[newLoc.x, newLoc.y] = pieceOnNewLoc;
    //     movedChessPiece.CurLoc = startLoc;
    //     if (movedChessPiece is King)
    //     {
    //         ((King)movedChessPiece).UpdateLoc();
    //     }
    //
    //     if (pieceOnNewLoc != null)
    //     {
    //         if (whiteKing)
    //         {
    //             activeBlackPieces.Add(pieceOnNewLoc);
    //         }
    //         else
    //         {
    //             activeWhitePieces.Add(pieceOnNewLoc);
    //         }
    //     }
    //
    //     // return kingUnderCheck;
    // }
    
    // public bool KingUnderCheck(bool whiteKing)
    // {
    //     Vector2Int kingLoc =
    //         whiteKing ? players[0].GetComponent<Player>().KingLoc : players[1].GetComponent<Player>().KingLoc;
    //     List<GameObject> activePieces = whiteKing ? activeBlackPieces : activeWhitePieces;
    //     
    //
    //     foreach (GameObject g in activePieces)
    //     {
    //         ChessPiece checkedScript = g.GetComponent<ChessPiece>();
    //         if (checkedScript.PieceControlsSpot(kingLoc, filledBoard))
    //         {
    //             return true;
    //         }
    //     }
    //
    //     return false;
    // }
    //
    // public bool KingUnderCheckmate(bool whiteKing)
    // {
    //     if (whiteKing && !whiteKingCheck || !whiteKing && !blackKingCheck) return false;
    //
    //     foreach (GameObject piece in whiteKing ? activeWhitePieces : activeBlackPieces)
    //     {
    //         ChessPiece pieceScript = piece.GetComponent<ChessPiece>();
    //         foreach (Vector2Int newLoc in pieceScript.GetControlledSpots())
    //         {
    //             if (!KingUnderCheckNextMove(whiteKing, pieceScript, newLoc))
    //             {
    //                 return false;
    //             }
    //         }
    //     }
    //
    //     return true;
    // }

    public bool SpotUnderControl(Vector2Int loc, bool white)
    {
        return white ? whiteControlledBoard[loc.x, loc.y] : blackControlledBoard[loc.x, loc.y];
    }
    
    [ServerRpc (RequireOwnership = false)]
    public void SweepControl(bool white) // for vision
    {
        List<GameObject> activePieces = white ? new List<GameObject>(activeWhitePieces) : new List<GameObject>(activeBlackPieces);
        if (white)
        {
            Debug.Log("Sweeping White");
            whiteControlledBoard = new bool[8, 8];
            Debug.Log(activeWhitePieces.Count);
            
            foreach (GameObject piece in activePieces)
            {
                ChessPiece pieceScript = piece.GetComponent<ChessPiece>();
                // Debug.Log(pieceScript.gameObject.name);
                whiteControlledBoard[pieceScript.CurLoc.x, pieceScript.CurLoc.y] = true;
                foreach (Vector2Int newLoc in pieceScript.GetControlledSpots())
                {
                    whiteControlledBoard[newLoc.x, newLoc.y] = true;
                }
            }  
        }
        else
        {
            Debug.Log("Sweeping Black");
            blackControlledBoard = new bool[8, 8];
            Debug.Log(activeBlackPieces.Count);
            
            foreach (GameObject piece in activePieces)
            {
                ChessPiece pieceScript = piece.GetComponent<ChessPiece>();
                blackControlledBoard[pieceScript.CurLoc.x, pieceScript.CurLoc.y] = true;
                // Debug.Log(pieceScript.gameObject.name);
                foreach (Vector2Int newLoc in pieceScript.GetControlledSpots())
                {
                    blackControlledBoard[newLoc.x, newLoc.y] = true;
                }
            }  
        }
    }

}
