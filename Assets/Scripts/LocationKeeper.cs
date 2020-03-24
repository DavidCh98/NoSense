﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationKeeper : MonoBehaviour
{
    [SerializeField, Range(1, 15)] private int gridWidth = 1;
    [SerializeField, Range(1, 15)] private int gridDepth = 1;
    [SerializeField] private BoardSpawnScript boardSpawn = null;
    [SerializeField] private bool possibleMove = false;
    [SerializeField] private int number = 0;
    [SerializeField] private bool runCheck = false;
    [SerializeField] private Team team = Team.Black; //Deprecated turn is now available.

    [HideInInspector] public Team turn = Team.White;
    [HideInInspector] public LocationGrid locationGrid = new LocationGrid();
    [HideInInspector] public List<Piece> whiteObjects;
    [HideInInspector] public List<Piece> blackObjects;

    private PossibleMove[] turnMoves;

    private PossibleMove[] possibleMovesWhite; //Deprecated use turnMoves instead.
    private PossibleMove[] possibleMovesBlack; //Deprecated use turnMoves instead.

    private void Awake()
    {
        locationGrid.GenerateGrid(gridWidth, gridDepth);
        if (boardSpawn != null)
        {
            whiteObjects = boardSpawn.GenerateBoard(locationGrid, Team.White);
            blackObjects = boardSpawn.GenerateBoard(locationGrid, Team.Black);
        }
        else Debug.LogWarning("Board Spawn wasn't assigned");
    }

    private void Start()
    {
        turnMoves = CheckMoves(turn);
    }

    private void Update()
    {
        if (possibleMove == true)
        {
            //if (number < turnMoves.Length && number > -1) ExecuteMove(turnMoves[number]);
            ExecuteMove(turnMoves[Random.Range(0, turnMoves.Length)]);
            possibleMove = false;
        }
        if (runCheck == true)
        {
            CheckMoves(Team.White);
            runCheck = false;
        }
    }

    private void ExecuteMove(PossibleMove possibleMove)
    {
        if (possibleMove.strike) DestroyPiece(possibleMove.hitPiece);
        possibleMove.piece.MoveTo(possibleMove.toLocation);
        FollowedUp(possibleMove);
    }

    private bool FollowedUp(PossibleMove possibleMove)
    {
        List<PossibleMove> possibleMoves = new List<PossibleMove>();
        foreach (PossibleMove move in CheckMoves(turn))
        {
            if (move.strike == true && move.piece == possibleMove.piece)
            {
                possibleMoves.Add(move);
            }
        }
        if (possibleMoves.Count > 0)
        {
            turnMoves = possibleMoves.ToArray();
            return true;
        }
        turn = (turn == Team.Black ? Team.White : Team.Black);
        turnMoves = CheckMoves(turn);
        return false;
    }

    private void DestroyPiece(Piece piece)
    {
        piece.location.LeaveLocation();
        (piece.team == Team.Black ? blackObjects : whiteObjects).Remove(piece);
        Destroy(piece.gameObject);
        
    }

    private PossibleMove[] CheckMoves(Team team)
    {
        List<PossibleMove> possibleMoves = new List<PossibleMove>();

        bool strike = false;
        foreach (Piece piece in (team == Team.Black ? blackObjects : whiteObjects))
        {
            piece.possibleMoves.Clear();
            if (piece.isSpecialPiece == false)
            {
                for (int x = -1; x < 2; x += 2)
                {
                    //Check if it's a valid location
                    int tempX = x + piece.location.gridLocation.x;
                    if (ValidGridLocation(tempX, 0))
                    {
                        //Check if it's forward or backward
                        bool forward = (team == Team.Black && x == -1) || (team == Team.White && x == 1) ? true : false;

                        for (int y = -1; y < 2; y += 2)
                        {
                            //Check if it's a valid location
                            int tempY = y + piece.location.gridLocation.y;
                            if (ValidGridLocation(tempX, tempY))
                            {
                                Location tempLocation = locationGrid.locations[tempX, tempY];
                                if (strike == false && tempLocation.isOccupied == false && forward == true)
                                {
                                    PossibleMove possibleMove = new PossibleMove
                                    {
                                        piece = piece,
                                        fromLocation = piece.location,
                                        toLocation = tempLocation
                                    };
                                    possibleMoves.Add(possibleMove);
                                    piece.possibleMoves.Add(possibleMove);
                                }
                                if (tempLocation.isOccupied == true && OccupiedByFriendly(tempX, tempY, team) == false && PositionExistsAndIsFree(x + tempX, y + tempY))
                                {
                                    strike = true;
                                    PossibleMove possibleMove = new PossibleMove
                                    {
                                        piece = piece,
                                        fromLocation = piece.location,
                                        toLocation = locationGrid.locations[x + tempX, y + tempY],
                                        strike = true,
                                        hitPiece = tempLocation.piece
                                    };
                                    possibleMoves.Add(possibleMove);
                                    piece.possibleMoves.Add(possibleMove);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                for (int x = -1; x < 2; x += 2)
                {
                    //Check if it's a valid location
                    int tempX = x + piece.location.gridLocation.x;
                    if (ValidGridLocation(tempX, 0))
                    {
                        for (int y = -1; y < 2; y += 2)
                        {
                            //Check if it's a valid location
                            int tempY = y + piece.location.gridLocation.y;

                            bool striking = false;
                            while (ValidGridLocation(tempX, tempY))
                            {
                                Location tempLocation = locationGrid.locations[tempX, tempY];
                                if (tempLocation.isOccupied == false)
                                {
                                    PossibleMove possibleMove = new PossibleMove
                                    {
                                        piece = piece,
                                        fromLocation = piece.location,
                                        toLocation = tempLocation
                                    };
                                    possibleMoves.Add(possibleMove);
                                    piece.possibleMoves.Add(possibleMove);
                                }
                                if (tempLocation.isOccupied == true)
                                {
                                    if (OccupiedByFriendly(tempX, tempY, team) || striking == true) break;
                                    if (PositionExistsAndIsFree(x + tempX, y + tempY))
                                    {
                                        strike = true;
                                        striking = true;
                                        PossibleMove possibleMove = new PossibleMove
                                        {
                                            piece = piece,
                                            fromLocation = piece.location,
                                            toLocation = locationGrid.locations[x + tempX, y + tempY],
                                            strike = true,
                                            hitPiece = tempLocation.piece
                                        };
                                        possibleMoves.Add(possibleMove);
                                        piece.possibleMoves.Add(possibleMove);
                                    }
                                }

                                tempX += x;
                                tempY += y;
                            }
                        }
                    }
                }
            }
        }
        //Clean if strike is true
        if (strike)
        {
            for (int i = 0; i < possibleMoves.Count; i++)
            {
                if (possibleMoves[i].strike == false)
                {
                    possibleMoves[i].piece.possibleMoves.Remove(possibleMoves[i]);
                    possibleMoves.Remove(possibleMoves[i]);
                    i--;
                }
            }
        }
        //Debug
        //foreach (PossibleMove possibleMove1 in possibleMoves)
        //{
        //    Debug.Log(possibleMove1.fromLocation.gridLocation.ToString() + " " + possibleMove1.toLocation.gridLocation.ToString() + " " + team.ToString());
        //}
        return possibleMoves.ToArray();
    }

    private bool PositionExistsAndIsFree(int x, int y)
    {
        return ValidGridLocation(x, y) ? (locationGrid.locations[x, y].isOccupied ? false : true) : false;
    }

    private bool OccupiedByFriendly(int x, int y, Team team)
    {
        return (team == Team.Black ? blackObjects : whiteObjects).Contains(locationGrid.locations[x, y].piece);
    }

    private bool ValidGridLocation(int x, int y)
    {
        return (x < locationGrid.locations.GetLength(0) && x > -1) && (y < locationGrid.locations.GetLength(1) && y > -1);
    }
}

public class PossibleMove
{
    public Piece piece = null;
    public Location fromLocation = null;
    public Location toLocation = null;
    public bool strike = false;
    public Piece hitPiece;
}