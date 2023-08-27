using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameBoard
{
    #region Variables

    private int height = 0;
    public int Height { get { return height; } }

    private int width = 0;
    public int Width { get { return width; } }
  
    private SC_Gem[,]       _allGems;
    private int[,]          _logicalBoard;
    private HashSet<SC_Gem> _currentMatches = new HashSet<SC_Gem>();
    public  HashSet<SC_Gem> CurrentMatches { get { return _currentMatches; } }

    private int[,] temporaryRefillBoard;
    private int[,] minimalMatchesBoard;
    
    Dictionary<GlobalEnums.GemType, int> _matchedTypeCount = new(); 
    #endregion

    public GameBoard(int width, int height)
    {
        this.height          = height;
        this.width           = width;
        _allGems              = new SC_Gem[this.width, this.height];
        _logicalBoard         = new int[this.width, this.height];
        temporaryRefillBoard = new int[this.width, this.height];
        minimalMatchesBoard  = new int[this.width, this.height];
    }
    public bool MatchesAt(Vector2Int positionToCheck, int gemToCheck)
    {
        if (positionToCheck.x > 1)
        {
            if (_logicalBoard[positionToCheck.x - 1, positionToCheck.y] == gemToCheck &&
                _logicalBoard[positionToCheck.x - 2, positionToCheck.y] == gemToCheck)
                return true;
        }

        if (positionToCheck.y > 1)
        {
            if (_logicalBoard[positionToCheck.x, positionToCheck.y - 1] == gemToCheck &&
                _logicalBoard[positionToCheck.x, positionToCheck.y - 2] == gemToCheck)
                return true;
        }

        return false;
    }

    public void SetGem(int x, int y, SC_Gem gem)
    {
        _allGems[x, y] = gem;
        _logicalBoard[x, y] = gem == null ? -1 : (int)gem.type;
    }
    public SC_Gem GetGem(int x,int y)
    {
       return _allGems[x, y];
    }

    /// <summary>
    /// Find all the matched gems on the board.
    /// Add them into the current matches list.
    /// </summary>
    public void FindAllMatches()
    {
        _currentMatches.Clear();
        _matchedTypeCount.Clear();

        // find all the matches from the logical board
        var             matches        = GetAllMatchesFromThisBoard(_logicalBoard, width, height); 
        (int x , int y) unflattedIndex = (0, 0);
        SC_Gem currentGem;
        
        // then fill all the matches to the current matches list
        foreach (var match in matches)
        {
            unflattedIndex     = UnFlattedIndex(match);
            currentGem         = _allGems[unflattedIndex.x, unflattedIndex.y];
            currentGem.isMatch = true;
            _currentMatches.Add(currentGem);
            if (_matchedTypeCount.ContainsKey(currentGem.type))
            {
                _matchedTypeCount[currentGem.type]++;
            }
            else
            {
                _matchedTypeCount.Add(currentGem.type, 1);
            }
        }

        CheckForBombs();
    }
    
    /// <summary>
    /// This method is used to check if there are any matches on the temporary board. 
    /// </summary>
    /// <param name="temporaryBoard"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    private HashSet<int> GetAllMatchesFromThisBoard(int[,] temporaryBoard, int width, int height)
    {
        var result = new HashSet<int>();

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                FindMatchAt(temporaryBoard, i, j, width, height, ref result);
            }
        }
        
        return result;
    }

    /// <summary>
    /// Find matches at a specific position on the board
    /// </summary>
    /// <returns></returns>
    private HashSet<int> FindMatchAt(int[,] temporaryBoard, int x, int y, int width, int height, ref HashSet<int> result)
    {
        if (result == null)
        {
            result = new HashSet<int>();
        }
        
        if (temporaryBoard[x, y] < 0) return result;
        
        // Horizontal gems
        if (x > 0 && x < width - 1)
        {
            int left = temporaryBoard[x - 1, y];
            int right = temporaryBoard[x + 1, y];
            
            if (left < 0 || right < 0) return result;
            
            if (left == temporaryBoard[x, y] && right == temporaryBoard[x, y])
            {
                result.Add(FlattedIndex(x, y));
                result.Add(FlattedIndex(x - 1, y));
                result.Add(FlattedIndex(x + 1, y));
            }
        }
        
        // Vertical gems
        if (y > 0 && y < height - 1)
        {
            int above = temporaryBoard[x, y - 1];
            int below = temporaryBoard[x, y + 1];
            
            if (above < 0 || below < 0) return result;
            
            if (above == temporaryBoard[x, y] && below == temporaryBoard[x, y])
            {
                result.Add(FlattedIndex(x, y));
                result.Add(FlattedIndex(x, y - 1));
                result.Add(FlattedIndex(x, y + 1));
            }
        }

        return result;
    }

    private void CheckForBombs()
    {
        List<(Vector2Int pos, int blastSize)> bombAreas = new();
        foreach (var gem in _currentMatches)
        {
            int x = gem.posIndex.x;
            int y = gem.posIndex.y;

            if (gem.posIndex.x > 0)
            {
                if (_logicalBoard[x - 1, y] == (int)GlobalEnums.GemType.bomb)
                    bombAreas.Add((new Vector2Int(x - 1, y), _allGems[x - 1, y].blastSize));
            }

            if (gem.posIndex.x + 1 < width)
            {
                if (_allGems[x + 1, y] != null && _allGems[x + 1, y].type == GlobalEnums.GemType.bomb)
                    bombAreas.Add((new Vector2Int(x + 1, y), _allGems[x + 1, y].blastSize));
            }

            if (gem.posIndex.y > 0)
            {
                if (_allGems[x, y - 1] != null && _allGems[x, y - 1].type == GlobalEnums.GemType.bomb)
                    bombAreas.Add((new Vector2Int(x, y - 1), _allGems[x, y - 1].blastSize));
            }

            if (gem.posIndex.y + 1 < height)
            {
                if (_allGems[x, y + 1] != null && _allGems[x, y + 1].type == GlobalEnums.GemType.bomb)
                    bombAreas.Add((new Vector2Int(x, y + 1), _allGems[x, y + 1].blastSize));
            }
        }

        foreach (var area in bombAreas)
        {
            MarkBombArea(area.pos, area.blastSize);
        }
    }

    private void MarkBombArea(Vector2Int bombPos, int blastSize)
    {
        for (int x = bombPos.x - blastSize; x <= bombPos.x + blastSize; x++)
        {
            for (int y = bombPos.y - blastSize; y <= bombPos.y + blastSize; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    if (_allGems[x, y] != null)
                    {
                        _allGems[x, y].isMatch = true;
                        _currentMatches.Add(_allGems[x, y]);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Try to refill the game board with the minimal matches. Ideally 0 match.
    /// This function can be called from <see cref="SC_GameLogic"/>> after destroy all the matched gems.
    /// </summary>
    /// <returns></returns>
    public int[,] RefillGameBoard()
    {
        int minimalMatches             = int.MaxValue;
        int emptySlotCount             = 0;
        
        // clone the logical board to the temporary board
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                temporaryRefillBoard[x, y] = _logicalBoard[x, y];
                minimalMatchesBoard[x, y]  = _logicalBoard[x, y];
                if (_logicalBoard[x, y] < 0)
                {
                    emptySlotCount++;
                }
            }
        }
        
        // generate all possible gems
        List<int> allPossibleGems = new();
        int[] gemTypes = new int[SC_GameVariables.Instance.gems.Length];
        for (int i = 0; i < gemTypes.Length; i++)
        {
            gemTypes[i] = (int)SC_GameVariables.Instance.gems[i].type;
        }
        GenerateAllPossibleGems(emptySlotCount, 0, gemTypes, ref allPossibleGems);
        
        // try to refill the board with all possible gems
        int count = allPossibleGems.Count;
        for (int i = 0; i < count; i++)
        {
            int[,] newBoard = new int[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++) 
                {
                    if (temporaryRefillBoard[x, y] < 0)
                    {
                        newBoard[x, y] = allPossibleGems[i];
                        i++;
                    }
                    else
                    {
                        newBoard[x, y] = temporaryRefillBoard[x, y];
                    }
                }
            }
            
            var matches = GetAllMatchesFromThisBoard(newBoard, width, height);
            if (matches.Count == 0)
            {
                // if we find the perfect board, we can stop the loop
                minimalMatchesBoard = newBoard;
                break;
            }
            
            if (matches.Count < minimalMatches)
            {
                // otherwise we keep the best board
                minimalMatches      = matches.Count;
                minimalMatchesBoard = newBoard;
            }
        }
        
        return minimalMatchesBoard;
    }

    /// <summary>
    /// Run a recursive function to generate all possible gems
    /// </summary>
    private void GenerateAllPossibleGems(int emptySlotCount, int index, int[] gemTypes, ref List<int> spawnedGems)
    {
        if (index == emptySlotCount || spawnedGems.Count == emptySlotCount * 1000)
        {
            return;
        }
        
        for (int i = 0; i < gemTypes.Length; i++)
        {
            spawnedGems.Add(gemTypes[i]);
            GenerateAllPossibleGems(emptySlotCount, index + 1, gemTypes, ref spawnedGems);
        }
    }
    
    private int FlattedIndex(int x, int y)
    {
        return x + y * width;
    }
    
    private (int x, int y) UnFlattedIndex(int index)
    {
        return (index % width, index / width);
    }
}

