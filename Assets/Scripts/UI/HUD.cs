using System.Collections;
using Scripts.Common;
using TMPro;
using UnityEngine;

namespace Scripts.UI
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text _scoreText;
        
        private int _currentScore;
        private Coroutine _updateScoreCo;
        
        private void Start()
        {
            EventHub.Instance.Subscribe("ScoreChanged", UpdateScore);
        }

        private void UpdateScore()
        {
            if (_updateScoreCo != null)
            {
                StopCoroutine(_updateScoreCo);
                _updateScoreCo = null;
            }
            
            _updateScoreCo = StartCoroutine(UpdateScoreCo());
        }

        private IEnumerator UpdateScoreCo()
        {
            var targetScore  = SC_GameVariables.Instance.Score;
            float displayScore = 0;
            while (_currentScore < targetScore)
            {
                displayScore    = Mathf.Lerp(displayScore, targetScore, SC_GameVariables.Instance.scoreSpeed * Time.deltaTime);
                _currentScore   = Mathf.RoundToInt(displayScore);
                _scoreText.text = _currentScore.ToString();
                yield return null;
            }
        }
    }
}