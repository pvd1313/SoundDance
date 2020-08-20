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
        [SerializeField] private List<AudioClip> _availableMusic;

        private List<int> _musicKeys;
        private int _nextMovementSample;
        private int _nextMovementNumber;
        private MovementData _nextMovement;
        private bool _nextMovementPressed;
        private int _pressDurationSamples;
        private int _previewMaxTimeSamples;
        private bool _gameStarted;


        private void TryStartGame(AudioClip audioClip)
        {
            if (_audioDatabase == null || _audioSource == null || audioClip == null || _spriteRenderer == null)
                return;

            _audioSource.Stop();
            _nextMovementNumber = -1;
            _nextMovementSample = -1;
            _nextMovementPressed = true;
            _audioSource.clip = audioClip;

            var musicDatabase = JsonUtility.FromJson(_audioDatabase.text, typeof(MusicDatabase)) as MusicDatabase;
            if (musicDatabase == null) 
                return;

            var musicData = musicDatabase.Data.FirstOrDefault(d => d.MusicName == _audioSource.clip.name);
            if (musicData == null || musicData.Markers == null || musicData.Markers.Count == 0)
                return;

            _pressDurationSamples = (int)(_pressDuration * _audioSource.clip.frequency);
            _previewMaxTimeSamples = (int)(_previewMaxTime * _audioSource.clip.frequency);

            _musicKeys = new List<int>(musicData.Markers.OrderBy(k => k));

            _gameStarted = true;

            _audioSource.Play();
        }

        private void Update()
        {
            if (!_gameStarted) return;

            if (_audioSource.timeSamples > _nextMovementSample + _pressDurationSamples || _nextMovementSample == -1)
            {
                _nextMovementNumber ++;

                if (!_nextMovementPressed)
                {
                    DoFail("Not pressed");
                }

                _nextMovementPressed = false;

                if (_nextMovementNumber >= _musicKeys.Count)
                {
                    _gameStarted = false;
                    Debug.LogError("GAME FINISH");
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

            TryInputKey();

            var color = _spriteRenderer.color;
            transform.localScale = Vector3.one + Vector3.one * progress * 0.5f;
            color.a = _nextMovementPressed ? 0.1f : progress;
            _spriteRenderer.color = color;
        }

        private void TryInputKey()
        {
            if (!Input.anyKeyDown) return;

            if (_nextMovementPressed)
            {
                DoFail("Tried to press key twice");
                return;
            }

            if (!Input.GetKeyDown(_nextMovement.Key))
            {
                DoFail("Pressed wrong key");
                return;
            }

            if (Mathf.Abs(_nextMovementSample - _audioSource.timeSamples) > _pressDurationSamples)
            {
                DoFail("Press invalid timing");
            }

            _nextMovementPressed = true;
        }

        private void DoFail(string reason)
        {
            Debug.LogError($"{reason} at {_audioSource.time:F1}");
        }

        private void OnGUI()
        {
            var currentClipName = _audioSource?.clip?.name;

            for (var i = 0; i < _availableMusic.Count; i++)
            {
                var rect = new Rect(10, 10 + i * 30, 180, 30);
                var clip = _availableMusic[i];
                if (GUI.Button(rect, clip.name == currentClipName ? (">>>" + clip.name) : clip.name))
                {
                    TryStartGame(clip);
                }
            }

            if (_audioSource != null && _audioSource.isPlaying)
            {
                GUI.TextArea(new Rect(10, Screen.height - 30, 50, 20), _audioSource.time.ToString("F1"));
                GUI.TextArea(new Rect(60, Screen.height - 30, 10, 20), "/");
                GUI.TextArea(new Rect(70, Screen.height - 30, 50, 20), _audioSource.clip.length.ToString("F1"));
            }
                
        }
    }
}
