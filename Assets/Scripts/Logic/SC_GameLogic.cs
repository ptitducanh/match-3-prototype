using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Scripts.Common;
using TMPro;
using UnityEngine;

public class SC_GameLogic : MonoBehaviour
{
    private Dictionary<string, GameObject> unityObjects;
    private int score = 0;
    private float displayScore = 0;
    private GameBoard gameBoard;
    private GlobalEnums.GameState currentState = GlobalEnums.GameState.move;
    public GlobalEnums.GameState CurrentState { get { return currentState; } }

    #region MonoBehaviour
    private void Start()
    {
        Init();
    }

    #endregion

    #region Logic
    private void Init()
    {
        unityObjects = new Dictionary<string, GameObject>();
        GameObject[] _obj = GameObject.FindGameObjectsWithTag("UnityObject");
        foreach (GameObject g in _obj)
            unityObjects.Add(g.name,g);

        gameBoard = new GameBoard(SC_GameVariables.Instance.colsSize, SC_GameVariables.Instance.rowsSize);
        Setup();
    }
    private void Setup()
    {
        for (int x = 0; x < gameBoard.Width; x++)
            for (int y = 0; y < gameBoard.Height; y++)
            {
                Vector2    _pos    = new Vector2(x, y);
                GameObject _bgTile = ObjectPool.Instance.Get(SC_GameVariables.Instance.bgTilePrefabs.name);
                
                var bgTransform = _bgTile.transform;
                bgTransform.SetParent(unityObjects["GemsHolder"].transform);
                bgTransform.localPosition = new Vector3(_pos.x, _pos.y, 0f);

                int _gemToUse = Random.Range(0, SC_GameVariables.Instance.gems.Length);

                int iterations = 0;
                while (gameBoard.MatchesAt(new Vector2Int(x, y), (int)SC_GameVariables.Instance.gems[_gemToUse].type) && iterations < 100)
                {
                    _gemToUse = Random.Range(0, SC_GameVariables.Instance.gems.Length);
                    iterations++;
                }
                SpawnGem(new Vector2Int(x, y), SC_GameVariables.Instance.gems[_gemToUse]);
            }
    }
    
    private void SpawnGem(Vector2Int _Position, SC_Gem _GemToSpawn, float delayFallingTime = 0)
    {
        if (Random.Range(0, 100f) < SC_GameVariables.Instance.bombChance)
            _GemToSpawn = SC_GameVariables.Instance.bomb;

        // Get the gem from the pool. And put it into the gem holder
        SC_Gem _gem = ObjectPool.Instance.Get(_GemToSpawn.name).GetComponent<SC_Gem>();
        var gemTransform = _gem.transform;
        gemTransform.position = new Vector3(_Position.x, _Position.y + SC_GameVariables.Instance.dropHeight, 0f);
        gemTransform.SetParent(unityObjects["GemsHolder"].transform);
        
        // add the gem into game board logically
        gameBoard.SetGem(_Position.x,_Position.y, _gem);
        _gem.SetupGem(this,_Position);
        _gem.delayFallingTime = delayFallingTime;
    }
    public void SetGem(int x,int y, SC_Gem gem)
    {
        gameBoard.SetGem(x,y, gem);
    }
    
    public SC_Gem GetGem(int x, int y)
    {
        return gameBoard.GetGem(x, y);
    }
    
    public void SetState(GlobalEnums.GameState currentState)
    {
        this.currentState = currentState;
    }
    
    public void DestroyMatches()
    {
        var currentMatches = gameBoard.CurrentMatches;

        foreach (var gem in currentMatches)
        {
            if(gem == null) continue;
            
            CalculateGameScore(gem);
            DestroyMatchedGemsAt(gem.posIndex);
        }

        StartCoroutine(DecreaseRowCo());
    }
    
    /// <summary>
    /// Move all the gem down to the lowest position
    /// </summary>
    /// <returns></returns>
    private IEnumerator DecreaseRowCo()
    {
        yield return new WaitForSeconds(.2f);

        int nullCounter = 0;
        int deepestRow = -1;
        float delayToRefill = 0;
        for (int x = 0; x < gameBoard.Width; x++)
        {
            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem curGem = gameBoard.GetGem(x, y);
                if (curGem == null)
                {
                    nullCounter++;
                    if (deepestRow < 0)
                        deepestRow = y;
                }
                else if (nullCounter > 0)
                {
                    curGem.posIndex.y       -= nullCounter;
                    curGem.delayFallingTime =  (curGem.posIndex.y - deepestRow) * 0.1f;
                    delayToRefill           =  Mathf.Max(delayToRefill, curGem.delayFallingTime);
                    SetGem(x, y - nullCounter, curGem);
                    SetGem(x, y, null);
                }
            }
            nullCounter = 0;
        }

        Debug.Log($"Delay to refill: {delayToRefill}");
        StartCoroutine(FilledBoardCo(delayToRefill * 2));
    }

    /// <summary>
    /// Calculate the score of the game. Depends on which gem is matched.
    /// </summary>
    /// <param name="gemToCheck"></param>
    private void CalculateGameScore(SC_Gem gemToCheck)
    {
        SC_GameVariables.Instance.Score += gemToCheck.scoreValue;
        EventHub.Instance.Publish("ScoreChanged");
    }
    
    private void DestroyMatchedGemsAt(Vector2Int _Pos)
    {
        SC_Gem _curGem = gameBoard.GetGem(_Pos.x,_Pos.y);
        if (_curGem != null)
        {
            var effect = ObjectPool.Instance.Get(_curGem.destroyEffect.name);
            effect.transform.position = new Vector3(_Pos.x, _Pos.y, 0f);
            
            ObjectPool.Instance.Return(_curGem.gameObject);
            SetGem(_Pos.x,_Pos.y, null);
        }
    }

    /// <summary>
    /// Refill the game board. Also destroy all the new matches if there's any.
    /// </summary>
    /// <param name="delayToRefill"></param>
    /// <returns></returns>
    private IEnumerator FilledBoardCo(float delayToRefill)
    {
        yield return new WaitForSeconds(delayToRefill);
        RefillBoard();
        yield return new WaitForSeconds(0.5f);
        FindAllMatches();
        if (gameBoard.CurrentMatches.Count > 0)
        {
            yield return new WaitForSeconds(0.5f);
            DestroyMatches();
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            currentState = GlobalEnums.GameState.move;
        }
    }
    
    /// <summary>
    /// Refill the game board with new gems. Just that.
    /// </summary>
    private void RefillBoard()
    {
        var newBoard   = gameBoard.RefillGameBoard();
        var deepestRow = -1;
        for (int x = 0; x < gameBoard.Width; x++)
        {
            for (int y = 0; y < gameBoard.Height; y++)
            {
                SC_Gem _curGem = gameBoard.GetGem(x,y);
                if (_curGem == null)
                {
                    if (deepestRow < 0)
                    {
                        deepestRow = y;
                    }
                    SpawnGem(new Vector2Int(x, y), SC_GameVariables.Instance.gemsDictionary[(GlobalEnums.GemType)newBoard[x, y]], (y - deepestRow) * 0.1f);
                }
            }
        }
    }
    
    public void FindAllMatches()
    {
        gameBoard.FindAllMatches();
    }

    #endregion
}
