using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is an individual gem on the game board
/// </summary>
public class SC_Gem : MonoBehaviour
{
    [HideInInspector] public Vector2Int posIndex;
    [HideInInspector] public float      delayFallingTime;

    private Vector2 _firstTouchPosition;
    private Vector2 _finalTouchPosition;
    private bool    _isSelected;
    private float   _swipeAngle = 0;
    private SC_Gem  _otherGem;

    public  GlobalEnums.GemType type;
    public  bool                isMatch = false;
    private Vector2Int          previousPos;
    public  GameObject          destroyEffect;
    public  int                 scoreValue = 10;

    public  int          blastSize = 1;
    private SC_GameLogic _scGameLogic;
    private float        _currentVelocity;
    private Transform    _transform;

    private void Awake()
    {
        _transform = transform;
    }

    void Update()
    {
        if (delayFallingTime > 0)
        {
            delayFallingTime -= Time.deltaTime;
            return;
        }
        // if the game isn't at the designed position, move it there
        if (Vector2.Distance(transform.position, posIndex) > 0.01f)
        {
            var position  = _transform.position;
            var direction = (posIndex - (Vector2)position).normalized;                   // get the direction
            _currentVelocity    += SC_GameVariables.Instance.gemAcceleration * Time.deltaTime; // calculate the velocity base on the acceleration
            position            += (Vector3)(direction * (_currentVelocity * Time.deltaTime)); // calculate the new position
            _transform.position =  position;                                                   // set the new position
        }
        else
        {
            // reset the velocity if the gem is at the designed position
            _currentVelocity = 0; 
            // if it is. Snap it to the exact position and update the game board
            transform.position = new Vector3(posIndex.x, posIndex.y, 0);
        }
    }

    #region public methods

    public void SetupGem(SC_GameLogic scGameLogic, Vector2Int position)
    {
        posIndex     = position;
        _scGameLogic = scGameLogic;
    }

    #endregion

    #region event handlers

    private void OnMouseDown()
    {
        // if player is allowed to move the gems, get the first touch position
        if (_scGameLogic.CurrentState == GlobalEnums.GameState.move)
        {
            _firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _isSelected         = true;
        }
    }

    private void OnMouseUp()
    {
        if (_isSelected)
        {
            _isSelected = false;
            if (_scGameLogic.CurrentState == GlobalEnums.GameState.move)
            {
                _finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                CalculateAngle();
            }
        }
    }

    #endregion

    #region private methods

    private void CalculateAngle()
    {
        _swipeAngle = Mathf.Atan2(_finalTouchPosition.y - _firstTouchPosition.y, _finalTouchPosition.x - _firstTouchPosition.x);
        _swipeAngle = _swipeAngle * 180 / Mathf.PI;

        if (Vector3.Distance(_firstTouchPosition, _finalTouchPosition) > .5f)
            MovePieces();
    }

    private void MovePieces()
    {
        previousPos = posIndex;

        if (_swipeAngle < 45 && _swipeAngle > -45 && posIndex.x < SC_GameVariables.Instance.rowsSize - 1)
        {
            _otherGem = _scGameLogic.GetGem(posIndex.x + 1, posIndex.y);
            _otherGem.posIndex.x--;
            posIndex.x++;
        }
        else if (_swipeAngle > 45 && _swipeAngle <= 135 && posIndex.y < SC_GameVariables.Instance.colsSize - 1)
        {
            _otherGem = _scGameLogic.GetGem(posIndex.x, posIndex.y + 1);
            _otherGem.posIndex.y--;
            posIndex.y++;
        }
        else if (_swipeAngle < -45 && _swipeAngle >= -135 && posIndex.y > 0)
        {
            _otherGem = _scGameLogic.GetGem(posIndex.x, posIndex.y - 1);
            _otherGem.posIndex.y++;
            posIndex.y--;
        }
        else if (_swipeAngle > 135 || _swipeAngle < -135 && posIndex.x > 0)
        {
            _otherGem = _scGameLogic.GetGem(posIndex.x - 1, posIndex.y);
            _otherGem.posIndex.x++;
            posIndex.x--;
        }

        _scGameLogic.SetGem(posIndex.x, posIndex.y, this);
        _scGameLogic.SetGem(_otherGem.posIndex.x, _otherGem.posIndex.y, _otherGem);

        StartCoroutine(CheckMoveCo());
    }


    private IEnumerator CheckMoveCo()
    {
        _scGameLogic.SetState(GlobalEnums.GameState.wait);

        yield return new WaitForSeconds(.5f);
        _scGameLogic.FindAllMatches();

        if (_otherGem != null)
        {
            if (isMatch == false && _otherGem.isMatch == false)
            {
                _otherGem.posIndex = posIndex;
                posIndex           = previousPos;

                _scGameLogic.SetGem(posIndex.x, posIndex.y, this);
                _scGameLogic.SetGem(_otherGem.posIndex.x, _otherGem.posIndex.y, _otherGem);

                yield return new WaitForSeconds(.5f);
                _scGameLogic.SetState(GlobalEnums.GameState.move);
            }
            else
            {
                _scGameLogic.DestroyMatches();
            }
        }
    }

    #endregion
}