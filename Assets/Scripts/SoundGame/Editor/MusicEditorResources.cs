using System.Collections.Generic;
using UnityEngine;

namespace Pingle.SoundGame.Editor
{
    [System.Serializable]
    public class MusicEditorResources : ISerializationCallbackReceiver
    {
        public Texture2D SelectionSliderTexture => _selectionSliderTexture;
        public Texture2D SelectionBackgroundTexture => _selectionBackgroundTexture;
        public Texture2D PlayingTexture => _playingTexture;
        public Texture2D MarkerTexture => _markerTexture;
        public Texture2D TimelineTexture => _timelineTexture;
        public Texture2D BeatMarkerTexture => _beatMarkerTexture;
        public Texture2D BeatPlacerPreviewTexture => _beatPlacerPreviewTexture;
        public GUIStyle FileButtonStyle => _fileButtonStyle;
        public GUIStyle MarkerButtonStyle => _markerButtonStyle;
        public AudioSource AudioSource => _audioSource;

        private Texture2D _selectionSliderTexture;
        private Texture2D _selectionBackgroundTexture;
        private Texture2D _playingTexture;
        private Texture2D _markerTexture;
        private Texture2D _timelineTexture;
        private Texture2D _beatMarkerTexture;
        private Texture2D _beatPlacerPreviewTexture;
        private GUIStyle _fileButtonStyle;
        private GUIStyle _markerButtonStyle;
        private AudioSource _audioSource;

        private bool _initialized;

        public void Initialize()
        {
            if (_initialized) return;

            _initialized = true;

            var gm = GameObject.Find("_MusicEditorAudioPlayer");
            _audioSource = gm?.GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                gm = new GameObject("_MusicEditorAudioPlayer");
                gm.hideFlags |= HideFlags.HideAndDontSave;
                _audioSource = gm.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }

            _fileButtonStyle = new GUIStyle(GUI.skin.GetStyle("Button"));
            _fileButtonStyle.alignment = TextAnchor.MiddleRight;

            _markerButtonStyle = new GUIStyle();

            _beatPlacerPreviewTexture = CreatePixelColorTexture(0, 1, 1, 1f);
            _beatMarkerTexture = CreatePixelColorTexture(0.8f, 0.8f, 0.8f, 0.3f);
            _timelineTexture = CreatePixelColorTexture(Color.gray);
            _markerTexture = CreatePixelColorTexture(Color.yellow);
            _selectionBackgroundTexture = CreatePixelColorTexture(0.7f, 0.3f, 0.3f, 0.5f);
            _playingTexture = CreatePixelColorTexture(Color.green);
            _selectionSliderTexture = CreatePixelColorTexture(Color.red);
        }

        public void OnAfterDeserialize()
        {
            _initialized = false;
        }

        public void OnBeforeSerialize() { }

        private Texture2D CreatePixelColorTexture(float r, float g, float b, float a) => CreatePixelColorTexture(new Color(r, g, b, a));

        private Texture2D CreatePixelColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();

            return texture;
        }
    }
}
