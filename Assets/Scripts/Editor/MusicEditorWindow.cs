using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pingle.EditorTools
{
    public class MusicEditorWindow : EditorWindow
    {
        private static readonly string[] AudioFormats = new string[] { ".wav", ".mp3", ".aif", ".ogg" };

        private string _folderPath;
        private string[] _folderItems = new string[0];
        private Vector2 _audioScroll;
        private AudioClip _currentClip;
        private Texture2D _previewTexture;
        private bool _previewOutdated;
        private Rect _previewRect;
        private Vector2Int _preview;
        private Vector2Int _selection;
        private Texture2D _selectionSliderTexture;
        private Texture2D _selectionBackgroundTexture;
        private Texture2D _playingTexture;
        private Texture2D _markerTexture;
        private AudioSource _audioSource;
        private Vector2 _previewMouseStartPos;
        private bool _loopAudio;
        private int _replaySample;
        private int _startSample;
        private GUIStyle _fileButtonStyle;
        private GUIStyle _markerButtonStyle;
        private bool _previewMouseDown;
        private int _draggingMouseSample;
        private List<int> _markers = new List<int>();

        [MenuItem("Tools/MusicEditor")]
        private static void OpenWindow()
        {
            EditorWindow.GetWindow(typeof(MusicEditorWindow));
        }

        private void OnGUI()
        {
            InitializeResources();
            FolderGUI();
            AudioControl();
            AudioInfo();
            AudioPreview();
            PreviewScrollBarZoom();
            Markers();
        }

        private void InitializeResources()
        {
            if (_selectionSliderTexture == null)
            {
                _selectionSliderTexture = new Texture2D(1, 1);
                _selectionSliderTexture.SetPixel(0, 0, Color.red);
                _selectionSliderTexture.Apply();
            }

            if (_playingTexture == null)
            {
                _playingTexture = new Texture2D(1, 1);
                _playingTexture.SetPixel(0, 0, Color.green);
                _playingTexture.Apply();
            }

            if (_selectionBackgroundTexture == null)
            {
                _selectionBackgroundTexture = new Texture2D(1, 1);
                _selectionBackgroundTexture.SetPixel(0, 0, new Color(0.7f, 0.3f, 0.3f, 0.5f));
                _selectionBackgroundTexture.Apply();
            }

            if (_markerTexture == null)
            {
                _markerTexture = new Texture2D(1, 1);
                _markerTexture.SetPixel(0, 0, Color.yellow);
                _markerTexture.Apply();
            }

            var gm = GameObject.Find("_MusicEditorAudioPlayer");
            _audioSource = gm?.GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = new GameObject("_MusicEditorAudioPlayer").AddComponent<AudioSource>();
            }

            if (_fileButtonStyle == null)
            {
                _fileButtonStyle = new GUIStyle(GUI.skin.GetStyle("Button"));
                _fileButtonStyle.alignment = TextAnchor.MiddleRight;
            }

            if (_markerButtonStyle == null)
            {
                _markerButtonStyle = new GUIStyle();
            }
        }

        private void FolderGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            _folderPath = EditorGUILayout.TextField("Folder: ", _folderPath);
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Open...", GUILayout.Width(100)))
            {
                _folderPath = EditorUtility.OpenFolderPanel("Select music folder", "/Assets", "");
                _folderItems = Directory.GetFiles(_folderPath)
                    .Select(f => TryGetAudioAssetDatabasePath(f))
                    .Where(f => f != null).ToArray();
            }
            EditorGUILayout.EndHorizontal();

            _audioScroll = EditorGUILayout.BeginScrollView(_audioScroll, GUILayout.Height(150));
            EditorGUILayout.BeginVertical();

            foreach (var item in _folderItems)
            {
                if (GUILayout.Button(item, _fileButtonStyle))
                {
                    var newAudio = AssetDatabase.LoadAssetAtPath(item, typeof(AudioClip)) as AudioClip;
                    if (newAudio != null)
                    {
                        _currentClip = newAudio;
                        OnClipChanged();
                    }
                        
                }
            }
                
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void AudioControl()
        {
            EditorGUILayout.BeginHorizontal();
            var clipBak = _currentClip;
            EditorGUILayout.LabelField("Audio clip", GUILayout.Width(65));
            _currentClip = (AudioClip)EditorGUILayout.ObjectField("", _currentClip, typeof(AudioClip), allowSceneObjects: false);
            if (_currentClip != clipBak)
                OnClipChanged();

            _loopAudio = EditorGUILayout.ToggleLeft("Loop", _loopAudio, GUILayout.Width(50));
            _audioSource.loop = _loopAudio;

            EditorGUI.BeginDisabledGroup(_currentClip == null);
            if (GUILayout.Button("Play", GUILayout.Width(60)))
            {
                if (_selection.x != _selection.y)
                    _replaySample = Mathf.Max(_selection.x, _selection.y);
                else
                    _replaySample = int.MaxValue;
                _startSample = Mathf.Min(_selection.x, _selection.y);
                _audioSource.Play();
                _audioSource.timeSamples = _startSample;
            }
                
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(_currentClip == null || !_audioSource.isPlaying);
            if (GUILayout.Button("Stop", GUILayout.Width(60)))
                _audioSource.Stop();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void AudioInfo()
        {
            GUILayout.BeginHorizontal();

            var frequency = _currentClip == null ? "" : (_currentClip.frequency / 1000f).ToString() + " kHz";
            var duration = _currentClip == null ? "" : new TimeSpan(0, 0, (int)(_currentClip.length)).ToString(@"mm\:ss");
            var channels = _currentClip == null ? "" : _currentClip.channels.ToString();

            EditorGUI.BeginDisabledGroup(true);

            GUILayout.Label("Frequency", GUILayout.Width(65));
            GUILayout.TextField(frequency, GUILayout.Width(60));

            GUILayout.Label("Length", GUILayout.Width(41));
            GUILayout.TextField(duration, GUILayout.Width(40));

            GUILayout.Label("Channels", GUILayout.Width(55));
            GUILayout.TextField(channels, GUILayout.Width(15));

            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();
        }

        private void AudioPreview()
        {
            var textureRect = GUILayoutUtility.GetRect(100, 200);
            if (Event.current.type == EventType.Repaint)
                _previewRect = textureRect;

            GUILayoutUtility.GetRect(100, 30);

            if (_previewTexture == null || _previewTexture.width != _previewRect.width || _previewTexture.height != _previewRect.height || _previewOutdated)
            {
                if (_previewTexture == null || _previewTexture.width != _previewRect.width || _previewTexture.height != _previewRect.height)
                {
                    if (_previewTexture == null)
                        _previewTexture = new Texture2D(Mathf.Max(1, (int)_previewRect.width), Mathf.Max(1, (int)_previewRect.height));
                    else
                        _previewTexture.Resize(Mathf.Max(1, (int)_previewRect.width), Mathf.Max(1, (int)_previewRect.height));
                }

                AudioClipPreviewDrawer.DrawPreview(_previewTexture, _currentClip, _preview.x, _preview.y, 0);
                _previewOutdated = false;
            }

            if (_selection.x != _selection.y)
            {
                var startX = SampleToPreviewXLocal(_selection.x);
                var endX = SampleToPreviewXLocal(_selection.y);
                var rect = new Rect(_previewRect.x + startX, _previewRect.y, endX - startX, _previewRect.height);
                GUI.DrawTexture(rect, _selectionBackgroundTexture);
            }

            GUI.DrawTexture(_previewRect, _previewTexture);

            var sliderRect = SampleToPreviewSliderRect(_selection.x);
            GUI.DrawTexture(sliderRect, _selectionSliderTexture);

            sliderRect = SampleToPreviewSliderRect(_selection.y);
            GUI.DrawTexture(sliderRect, _selectionSliderTexture);

            if (_currentClip != null && _audioSource.isPlaying)
            {
                sliderRect = SampleToPreviewSliderRect(_audioSource.timeSamples);
                GUI.DrawTexture(sliderRect, _playingTexture);
                Repaint();

                if (_audioSource.timeSamples >= _replaySample)
                {
                    if (_loopAudio)
                    {
                        _audioSource.timeSamples = _startSample;
                    }
                    else
                    {
                        _audioSource.Stop();
                    }
                }
            }

            PreviewProcessLeftMouseClickEvent();
        }

        private void PreviewScrollBarZoom()
        {
            var zoom = _preview.y - _preview.x;
            var sampleCount = _currentClip == null ? 1 : _currentClip.samples;
            var mousePos = Event.current.mousePosition;

            if (_previewRect.Contains(mousePos) && Event.current.type == EventType.ScrollWheel)
            {
                var hoveredSample = MouseToSamplePos(mousePos);
                var hoveredPart = SampleToPreviewXLocal(hoveredSample) / _previewRect.width;

                if (Event.current.delta.y < 0)
                {
                    zoom = Mathf.Max(1, zoom / 2);
                }
                else
                {
                    zoom = Mathf.Min(sampleCount, zoom * 2);
                }

                _preview.x = Mathf.Max(0, hoveredSample - (int)(zoom * hoveredPart));
                _preview.y = Math.Min(sampleCount, _preview.x + zoom);
                _previewOutdated = true;
                Repaint();
            }

            var previewX = GUILayout.HorizontalScrollbar(_preview.x, zoom, 0, sampleCount);
            if ((int)previewX != _preview.x)
            {
                _preview.x = (int)previewX;
                _preview.y = _preview.x + zoom;
                _previewOutdated = true;
                Repaint();
            }
        }

        private void Markers()
        {
            for (var i = _markers.Count - 1; i >= 0; i--)
            {
                var markerPos = _markers[i];
                if (markerPos < _preview.x || markerPos > _preview.y) continue;

                var rect = SampleToPreviewSliderRect(markerPos);
                GUI.DrawTexture(rect, _markerTexture);

                rect = SampleToPreviewMarkerRect(markerPos, 10);
                GUI.DrawTexture(rect, _markerTexture);
                if (GUI.Button(rect, "", _markerButtonStyle))
                {
                    _markers.RemoveAt(i);
                    TryExportMarkers();
                }
            }

            var mousePos = Event.current.mousePosition;
            if (!_previewRect.Contains(mousePos) || Event.current.button != 1)
            {
                _draggingMouseSample = -1;
                return;
            }

            var hoveredMarkerIndex = FindMarkerAtMousePosition(mousePos);

            if (Event.current.type == EventType.MouseDown)
            {
                if (hoveredMarkerIndex > 0)
                {
                    _draggingMouseSample = _markers[hoveredMarkerIndex];
                }
                else
                {
                    _draggingMouseSample = MouseToSamplePos(mousePos);
                    _markers.Add(_draggingMouseSample);
                    Repaint();
                    TryExportMarkers();
                }
                
            }

            if (Event.current.type == EventType.MouseDrag && _draggingMouseSample > 0)
            {
                _markers.Remove(_draggingMouseSample);
                _draggingMouseSample = MouseToSamplePos(mousePos);
                _markers.Add(_draggingMouseSample);
                Repaint();
                TryExportMarkers();
            }
        }

        private int FindMarkerAtMousePosition(Vector2 mousePos)
        {
            for (var i = _markers.Count - 1; i >= 0; i--)
            {
                var sampleX = _previewRect.x + SampleToPreviewXLocal(_markers[i]);
                if (Mathf.Abs(sampleX - mousePos.x) <= 10) return i;
            }

            return -1;
        }

        private Rect SampleToPreviewSliderRect(int sample)
        {
            var sliderStart = SampleToPreviewXLocal(sample);
            return new Rect(_previewRect.x + sliderStart, _previewRect.y, 1, _previewRect.height + 10);
        }

        private Rect SampleToPreviewMarkerRect(int sample, int markerSize)
        {
            var markerStart = SampleToPreviewXLocal(sample);
            return new Rect(_previewRect.x + markerStart - markerSize / 2, _previewRect.y + _previewRect.height + 10, markerSize, markerSize);
        }

        private void PreviewProcessLeftMouseClickEvent()
        {
            var mousePos = Event.current.mousePosition;
            if (!_previewRect.Contains(mousePos) || Event.current.button != 0)
            {
                _previewMouseDown = false;
                return;
            }

            if (Event.current.type == EventType.MouseDown)
            {
                _selection.x = MouseToSamplePos(mousePos);
                _selection.y = _selection.x;
                _previewMouseStartPos = mousePos;
                _previewMouseDown = true;
                Repaint();
            }

            if (_previewMouseDown && Event.current.type == EventType.MouseDrag && (Mathf.Abs(mousePos.x - _previewMouseStartPos.x) > 5))
            {
                _selection.y = MouseToSamplePos(mousePos);
                Repaint();
            }
        }

        private int MouseToSamplePos(Vector2 mousePos)
        {
            var xNormalized = (mousePos.x - _previewRect.x) / _previewRect.width;
            return _preview.x + (int)((_preview.y - _preview.x) * xNormalized);
        }

        private float SampleToPreviewXLocal(int sample)
        {
            return _previewRect.width * (sample - (float)_preview.x) / (_preview.y - _preview.x);
        }

        private string TryGetAudioAssetDatabasePath(string filePath)
        {
            if (!AudioFormats.Any(format => filePath.EndsWith(format))) return null;

            var dataPath = Application.dataPath;
            var dataIndex = filePath.IndexOf(dataPath);
            if (dataIndex < 0) return null;

            filePath = "Assets/" + filePath.Substring(dataPath.Length);
            return filePath;
        }

        private void OnClipChanged()
        {
            _audioSource.Stop();
            _audioSource.clip = _currentClip;
            _previewOutdated = true;
            _preview.x = 0;
            _preview.y = _currentClip != null ? _currentClip.samples : 0;
            _selection.x = 0;
            _selection.y = 0;
            TryImportMarkers();
        }

        private void TryImportMarkers()
        {
            try
            {
                _markers.Clear();

                if (_currentClip == null) return;

                var audioPath = GetAssetFullPath(_currentClip);
                var jsonPath = GetMarkersJsonPath(audioPath);
                if (!File.Exists(jsonPath)) return;

                var json = File.ReadAllText(jsonPath);
                var data = JsonUtility.FromJson<MusicData>(json);
                _markers.AddRange(data.Markers.Where(m => m > 0 && m <= _currentClip.samples));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void TryExportMarkers()
        {
            if (_currentClip == null) return;

            try
            {
                var data = new MusicData()
                {
                    Markers = _markers
                };

                var audioPath = GetAssetFullPath(_currentClip);
                var jsonPath = GetMarkersJsonPath(audioPath);
                var json = JsonUtility.ToJson(data);
                File.WriteAllText(jsonPath, json);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private string GetMarkersJsonPath(string clipPath)
        {
            var fileName = Path.GetFileName(clipPath);
            var folderPath = Path.GetDirectoryName(clipPath);
            return Path.Combine(folderPath, fileName + ".json");
        }

        private string GetAssetFullPath<T>(T obj) where T : UnityEngine.Object
        {
            var assetPath = AssetDatabase.GetAssetPath(obj);
            var dataPath = Application.dataPath.Replace("Assets", "");
            return Path.Combine(dataPath, assetPath);
        }
    }
}
