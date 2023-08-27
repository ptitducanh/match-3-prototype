using System;
using System.Collections;
using System.Collections.Generic;
using Scripts.Common;
using UnityEngine;
using UnityEngine.Serialization;

public class SC_GameVariables : Singleton<SC_GameVariables>
{
    public GameObject bgTilePrefabs;
    public SC_Gem     bomb;
    public SC_Gem[]   gems;
    public float      bonusAmount = 0.5f;
    public float      bombChance  = 2f;
    public int        dropHeight  = 0;
    public float      gemAcceleration;
    public float      scoreSpeed = 5;

    [SerializeField] public Dictionary<GlobalEnums.GemType, SC_Gem> gemsDictionary = new ();
    
    [HideInInspector] public int Score = 0;

    [HideInInspector] public int rowsSize = 7;
    [HideInInspector] public int colsSize = 7;

    private void Start()
    {
        foreach (var gem in gems)
        {
            gemsDictionary.Add(gem.type, gem);
        }
    }
}