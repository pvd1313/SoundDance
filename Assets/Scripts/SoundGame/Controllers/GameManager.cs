using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pingle.SoundGame
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private TextAsset _audioDatabase;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private List<MovementData> _moves;
        [SerializeField] private float _pressDuration;
        [SerializeField] private float _previewMaxTime;

        private List<int> _musicKeys;
        private int _nextMovementSample = -1;
        private int _nextMovementNumber = -1;
        private MovementData _nextMovement;
        private bool _nextMovementPressed = true;
        private int _pressDurationSamples;
        private int _previewMaxTimeSamples;


        private void Start()
        {
            enabled = false;

            if (_audioDatabase == null || _audioSource == null || _audioSource.clip == null || _spriteRenderer == null)
                return;

            var musicDatabase = JsonUtility.FromJson(_audioDatabase.text, typeof(MusicDatabase)) as MusicDatabase;
            if (musicDatabase == null) 
                return;

            var musicData = musicDatabase.Data.FirstOrDefault(d => d.MusicName == _audioSource.clip.name);
            if (musicData == null || musicData.Markers == null || musicData.Markers.Count == 0)
                return;

            _pressDurationSamples = (int)(_pressDuration * _audioSource.clip.frequency);
            _previewMaxTimeSamples = (int)(_previewMaxTime * _audioSource.clip.frequency);

            _musicKeys = new List<int>(musicData.Markers.OrderBy(k => k));

            enabled = true;

            _audioSource.Play();
        }

        private void Update()
        {
            if (_audioSource.timeSamples > _nextMovementSample + _pressDurationSamples || _nextMovementSample == -1)
            {
                _nextMovementNumber ++;

                if (!_nextMovementPressed)
                {
                    DoFail();
                }

                _nextMovementPressed = false;

                if (_nextMovementNumber >= _musicKeys.Count)
                {
                    enabled = false;
                    return;
                }

                var nextMove = _moves[Random.Range(0, _moves.Count)];
                while (_nextMovement == nextMove)
                    nextMove = _moves[Random.Range(0, _moves.Count)];

                _nextMovement = nextMove;
                _spriteRenderer.sprite = nextMove.Icon;
                _nextMovementSample = _musicKeys[_nextMovementNumber];
            }

            var progress = 1 - Mathf.Clamp01((_nextMovementSample - _audioSource.timeSamples) / (float) _previewMaxTimeSamples);

            if (Input.anyKeyDown)
            {
                if (Input.GetKeyDown(_nextMovement.Key) && !_nextMovementPressed && 
                    Mathf.Abs(_nextMovementSample - _audioSource.timeSamples) <= _pressDurationSamples)
                {
                    _nextMovementPressed = true;
                }
                else
                {
                    DoFail();
                }
            }
                

            var color = _spriteRenderer.color;
            transform.localScale = Vector3.one + Vector3.one * progress * 0.5f;
            color.a = _nextMovementPressed ? 0.1f : progress;
            _spriteRenderer.color = color;
        }

        private void DoFail()
        {
            Debug.LogError("Invalid!");
        }
    }
}
