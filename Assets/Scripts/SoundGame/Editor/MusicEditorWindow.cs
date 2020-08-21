using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;

namespace Pingle.SoundGame.Editor
{
    public class MusicEditorWindow : EditorWindow
    {
        private static readonly string[] AudioFormats = new string[] { ".wav", ".mp3", ".aif", ".ogg" };
        private static readonly float[] TimelineSizes = new float[] { 1f, 5f, 10f, 30f, 60f };
        private static readonly float TimelineSectionWidth = 50;
        private static readonly float ZoomSpeed = 1.5f;

        private MusicEditorResources _resources = new MusicEditorResources();
        private MusicEditorDatabase _database = new MusicEditorDatabase();
        private string _folderPath;
        private string[] _folderItems = new string[0];
        private Vector2 _audioScroll;
        private AudioClip _currentClip;
        private Texture2D _previewTexture;
        private bool _previewOutdated;
        private Rect _previewRect;
        private Rect _timelineRect;
        private Vector2Int _preview;
        private Vector2Int _selection;
        private Vector2 _previewMouseStartPos;
        private bool _loopAudio;
        private int _replaySample;
        private int _startSample;
        private bool _previewMouseDown;
        private int _draggingMouseSample;
        private List<int> _markers = new List<int>();
        private List<int> _beats = new List<int>();
        private float _cursorPos;
        private int _analyzeBPM;
        private float _beatPlacerPeriod;
        private List<int> _beatsPlacerWorkBuffer = new List<int>();
        private List<int> _beatsPlacerResultBuffer = new List<int>();
        private bool _beatPlacerPreview = true;
        private bool _showAllBeats = true;
        private string _currentClipDuration;
        private string _currentClipFrequencyKHZ;
        private Vector2 _databaseScroll;

        [MenuItem("Tools/MusicEditor")]
        private static void OpenWindow()
        {
            GetWindow(typeof(MusicEditorWindow));
        }

        private void OnGUI()
        {
            EditorApplication.update -= _database.Autosave;
            EditorApplication.update += _database.Autosave;

            _resources.Initialize();
            FolderGUI();
            AudioControl();
            AudioInfo();
            AudioPreviewTimeline();
            AudioPreview();
            PreviewScrollBarZoom();
            Markers();
            BeatControl();
            MusicDatabaseControl();
        }

        private void BeatControl()
        {
            GUILayout.BeginHorizontal();

            var sMin = Mathf.Min(_selection.x, _selection.y);
            var sMax = Mathf.Max(_selection.x, _selection.y);

            if (GUILayout.Button("Analyze music", GUILayout.Width(100)))
            {
                _beats.Clear();
                _beats.AddRange(AubioTrack.DetectBeats(_currentClip));
                Repaint();
            }

            _showAllBeats = EditorGUILayout.ToggleLeft("Show", _showAllBeats, GUILayout.Width(50));

            EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(10));

            var placeBeats = GUILayout.Button("Place beats", GUILayout.Width(80)) || Event.current.keyCode == KeyCode.B;
            if (placeBeats && _currentClip != null && sMin != sMax)
            {
                for (var i = _markers.Count - 1; i >= 0; i--)
                    if (_markers[i] >= sMin && _markers[i] <= sMax)
                        _markers.RemoveAt(i);

                ComputePlaceBeats(_beatPlacerPeriod, _beatsPlacerResultBuffer);
                _markers.AddRange(_beatsPlacerResultBuffer);
                _database.ExportMarkers(_currentClip.name, _markers);
                Repaint();
            }

            GUILayout.Label("Period", GUILayout.Width(40));
            _beatPlacerPeriod = Mathf.Max(0, EditorGUILayout.FloatField(_beatPlacerPeriod, GUILayout.Width(25)));

            _beatPlacerPreview = EditorGUILayout.ToggleLeft("Preview", _beatPlacerPreview, GUILayout.Width(60));

            EditorGUILayout.LabelField("", GUI.skin.verticalSlider, GUILayout.Width(10));

            if (GUILayout.Button("Get BPM", GUILayout.Width(80)) && _currentClip != null)
            {
                var selectedBeats = _beats.Where(p => p >= sMin && p <= sMax);
                if (selectedBeats.Any())
                {
                    var delta = selectedBeats.Max() - selectedBeats.Min();
                    var time = Mathf.Max(0.01f, delta / _currentClip.frequency);
                    _analyzeBPM = (int)(selectedBeats.Count() / time * 60);
                }
                else
                {
                    _analyzeBPM = -1;
                }
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField(_analyzeBPM, GUILayout.Width(30));
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();
        }

        private void ComputePlaceBeats(float period, List<int> result)
        {
            var sMin = Mathf.Min(_selection.x, _selection.y);
            var sMax = Mathf.Max(_selection.x, _selection.y);
            result.Clear();

            _beatsPlacerWorkBuffer.Clear();
            _beatsPlacerWorkBuffer.AddRange(_beats.Where(p => p >= sMin && p <= sMax));

            var indexDelta = Mathf.FloorToInt(period) + 1;
            var total = _beatsPlacerWorkBuffer.Count / indexDelta - 1;
            var frac = period % 1;
            if (total <= 0) return;

            for (var i = 0; i < total; i++)
            {
                var left = _beatsPlacerWorkBuffer[i * indexDelta];
                var right = _beatsPlacerWorkBuffer[i * indexDelta + 1];
                var sample = (int)Mathf.Lerp(left, right, frac);
                result.Add(sample);
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
                var startPath = string.IsNullOrEmpty(_folderPath) ? "/" : _folderPath;
                _folderPath = EditorUtility.OpenFolderPanel("Select music folder", startPath, "");
                if (!string.IsNullOrEmpty(_folderPath))
                {
                    _folderItems = Directory.GetFiles(_folderPath)
                        .Select(f => TryGetAudioAssetDatabasePath(f))
                        .Where(f => f != null).ToArray();
                }
                
            }
            EditorGUILayout.EndHorizontal();

            _audioScroll = EditorGUILayout.BeginScrollView(_audioScroll, GUI.skin.box, GUILayout.Height(120));
            EditorGUILayout.BeginVertical();

            foreach (var item in _folderItems)
            {
                if (GUILayout.Button(item, _resources.FileButtonStyle))
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
            _resources.AudioSource.loop = _loopAudio;

            EditorGUI.BeginDisabledGroup(_currentClip == null);
            if (GUILayout.Button("Play", GUILayout.Width(60)))
            {
                if (_selection.x != _selection.y)
                    _replaySample = Mathf.Max(_selection.x, _selection.y);
                else
                    _replaySample = int.MaxValue;
                _startSample = Mathf.Min(_selection.x, _selection.y);
                _resources.AudioSource.Play();
                _resources.AudioSource.timeSamples = _startSample;
            }
                
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(_currentClip == null || !_resources.AudioSource.isPlaying);
            if (GUILayout.Button("Stop", GUILayout.Width(60)))
                _resources.AudioSource.Stop();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void AudioInfo()
        {
            GUILayout.BeginHorizontal();
            
            var channels = _currentClip == null ? 0 : _currentClip.channels;

            GUILayout.Label("Frequency", GUILayout.Width(65));
            EditorGUI.BeginDisabledGroup(true);  GUILayout.TextField(_currentClipFrequencyKHZ, GUILayout.Width(60)); EditorGUI.EndDisabledGroup();

            GUILayout.Label("Length", GUILayout.Width(41));
            EditorGUI.BeginDisabledGroup(true); GUILayout.TextField(_currentClipDuration, GUILayout.Width(40)); EditorGUI.EndDisabledGroup();

            GUILayout.Label("Channels", GUILayout.Width(55));
            EditorGUI.BeginDisabledGroup(true); EditorGUILayout.IntField(channels, GUILayout.Width(15)); EditorGUI.EndDisabledGroup();

            GUILayout.Label("Cursor", GUILayout.Width(41));
            EditorGUI.BeginDisabledGroup(true); EditorGUILayout.FloatField(_cursorPos, GUILayout.Width(45)); EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();
        }

        private float SampleToPreciseTime(int sample)
        {
            if (_currentClip == null) return 0;

            return (sample / (float)_currentClip.frequency);
        }

        private void AudioPreviewTimeline()
        {
            var timelineRect = GUILayoutUtility.GetRect(100, 20);
            if (Event.current.type == EventType.Repaint)
                _timelineRect = timelineRect;

            var frequency = _currentClip == null ? 1 : _currentClip.frequency;
            var sectionCount = _timelineRect.width / TimelineSectionWidth;
            var zoom = _preview.y - _preview.x;
            var windowSize = zoom / (float)frequency;
            var sectionSize = windowSize / sectionCount;
            var resultSize = TimelineSizes.OrderByDescending(k => k).LastOrDefault(k => k > sectionSize);

            var firstSection = (int)(_preview.x / frequency / resultSize);
            var totalSections = (int)((float)zoom / frequency / resultSize) + 2;
            for (var i = 0; i < totalSections; i++)
            {
                var sample = (int)((firstSection + i) * resultSize * frequency);
                var x = SampleToPreviewXLocal(sample);
                
                var lineRect = new Rect(_timelineRect.x + x, _timelineRect.y, 1, _timelineRect.height);
                GUI.DrawTexture(lineRect, _resources.TimelineTexture);

                var textRect = new Rect(_timelineRect.x + x, _timelineRect.y + 3, TimelineSectionWidth, _timelineRect.height);
                var timeText = ((int)((firstSection + i) * resultSize)).ToString();
                GUI.Label(textRect, timeText);
            }

            GUI.DrawTexture(new Rect(_timelineRect.x, _timelineRect.y, _timelineRect.width, 1), _resources.TimelineTexture);
        }

        private void AudioPreview()
        {
            var textureRect = GUILayoutUtility.GetRect(100, 200);
            if (Event.current.type == EventType.Repaint)
                _previewRect = textureRect;

            GUILayoutUtility.GetRect(100, 20);

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
                GUI.DrawTexture(rect, _resources.SelectionBackgroundTexture);
            }

            GUI.DrawTexture(_previewRect, _previewTexture);

            var sliderRect = SampleToPreviewSliderRect(_selection.x);
            GUI.DrawTexture(sliderRect, _resources.SelectionSliderTexture);

            sliderRect = SampleToPreviewSliderRect(_selection.y);
            GUI.DrawTexture(sliderRect, _resources.SelectionSliderTexture);

            if (_currentClip != null && _resources.AudioSource.isPlaying)
            {
                sliderRect = SampleToPreviewSliderRect(_resources.AudioSource.timeSamples);
                GUI.DrawTexture(sliderRect, _resources.PlayingTexture);
                Repaint();

                if (_resources.AudioSource.timeSamples >= _replaySample)
                {
                    if (_loopAudio)
                    {
                        _resources.AudioSource.timeSamples = _startSample;
                    }
                    else
                    {
                        _resources.AudioSource.Stop();
                    }
                }
            }

            if (Event.current.keyCode == KeyCode.A && Event.current.modifiers.HasFlag(EventModifiers.Control)) 
            {
                _selection.x = 0;
                _selection.y = _currentClip == null ? 1 : _currentClip.samples;
                Repaint();
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
                    zoom = Mathf.Max(1, (int)(zoom / ZoomSpeed));
                }
                else
                {
                    zoom = Mathf.Min(sampleCount, (int)(zoom * ZoomSpeed));
                }

                _preview.x = Mathf.Max(0, hoveredSample - (int)(zoom * hoveredPart));
                _preview.y = Mathf.Min(sampleCount, _preview.x + zoom);
                _previewOutdated = true;
                Repaint();
            }

            if (Event.current.modifiers.HasFlag(EventModifiers.Control) && Event.current.keyCode == KeyCode.Z && _selection.x != _selection.y)
            {
                var sMin = Mathf.Min(_selection.x, _selection.y);
                var sMax = Mathf.Max(_selection.x, _selection.y);
                _preview.x = sMin;
                _preview.y = sMax;
                _previewOutdated = true;
                Repaint();
                return;
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
            if (_beatPlacerPreview)
            {
                ComputePlaceBeats(_beatPlacerPeriod, _beatsPlacerResultBuffer);
                foreach (var beat in _beatsPlacerResultBuffer)
                    if (beat >= _preview.x && beat <= _preview.y)
                    {
                        var x = SampleToPreviewXLocal(beat);
                        var rect = new Rect(_previewRect.x + x, _previewRect.y, 1, _previewRect.height);
                        GUI.DrawTexture(rect, _resources.BeatPlacerPreviewTexture);
                    }
            }

            if (_showAllBeats)
            {
                foreach (var beat in _beats)
                {
                    if (beat < _preview.x || beat > _preview.y)
                        continue;

                    var x = SampleToPreviewXLocal(beat);
                    var rect = new Rect(_previewRect.x + x, _previewRect.y, 1, _previewRect.height);
                    GUI.DrawTexture(rect, _resources.BeatMarkerTexture);
                }
            }

            for (var i = _markers.Count - 1; i >= 0; i--)
            {
                var markerPos = _markers[i];
                if (markerPos < _preview.x || markerPos > _preview.y) continue;

                var rect = SampleToPreviewSliderRect(markerPos);
                GUI.DrawTexture(rect, _resources.MarkerTexture);

                rect = SampleToPreviewMarkerRect(markerPos, 10);
                GUI.DrawTexture(rect, _resources.MarkerTexture);
                if (GUI.Button(rect, "", _resources.MarkerButtonStyle))
                {
                    _markers.RemoveAt(i);
                    _database.ExportMarkers(_currentClip.name, _markers);
                    Repaint();
                }
            }

            var mousePos = Event.current.mousePosition;

            if (Event.current.keyCode == KeyCode.Delete)
            {
                var sMin = Mathf.Min(_selection.x, _selection.y);
                var sMax = Mathf.Max(_selection.x, _selection.y);

                var markersCount = _markers.Count;
                for (var i = _markers.Count - 1; i >= 0; i--)
                {
                    if (_markers[i] >= sMin && _markers[i] <= sMax)
                    {
                        _markers.RemoveAt(i);
                    }
                }

                if (markersCount != _markers.Count)
                {
                    _database.ExportMarkers(_currentClip.name, _markers);
                    Repaint();
                }
            }

            if (!_previewRect.Contains(mousePos) || Event.current.button != 1)
            {
                _draggingMouseSample = -1;
                return;
            }

            var hoveredMarkerIndex = FindMarkerAtMousePosition(mousePos);

            if (Event.current.type == EventType.MouseDown)
            {
                if (hoveredMarkerIndex >= 0)
                {
                    _draggingMouseSample = _markers[hoveredMarkerIndex];
                    _cursorPos = SampleToPreciseTime(_draggingMouseSample);
                }
                else
                {
                    _draggingMouseSample = MouseToSamplePos(mousePos);
                    _markers.Add(_draggingMouseSample);
                    _cursorPos = SampleToPreciseTime(_draggingMouseSample);
                    _database.ExportMarkers(_currentClip.name, _markers);
                    Repaint();
                }
                
            }

            if (Event.current.type == EventType.MouseDrag && _draggingMouseSample > 0)
            {
                _markers.Remove(_draggingMouseSample);
                _draggingMouseSample = MouseToSamplePos(mousePos);
                _cursorPos = SampleToPreciseTime(_draggingMouseSample);
                _markers.Add(_draggingMouseSample);
                _database.ExportMarkers(_currentClip.name, _markers);
                Repaint();
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
                _cursorPos = SampleToPreciseTime(_selection.x);
                _previewMouseStartPos = mousePos;
                _previewMouseDown = true;
                Repaint();
            }

            if (_previewMouseDown && Event.current.type == EventType.MouseDrag && (Mathf.Abs(mousePos.x - _previewMouseStartPos.x) > 5))
            {
                _selection.y = MouseToSamplePos(mousePos);
                _cursorPos = SampleToPreciseTime(_selection.y);
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
            _resources.AudioSource.Stop();
            _resources.AudioSource.clip = _currentClip;
            _previewOutdated = true;
            _preview.x = 0;
            _preview.y = _currentClip != null ? _currentClip.samples : 0;
            _selection.x = 0;
            _selection.y = 0;
            _analyzeBPM = -1;
            _beats.Clear();
            _currentClipDuration = _currentClip == null ? "" : new TimeSpan(0, 0, (int)(_currentClip.length)).ToString(@"mm\:ss");
            _currentClipFrequencyKHZ = _currentClip == null ? "" : (_currentClip.frequency / 1000f).ToString() + " kHz";
            _markers.Clear();
            if (_currentClip != null)
                _database.ImportMarkers(_currentClip.name, _currentClip.samples, _markers);
        }

        private void MusicDatabaseControl()
        {
            GUILayout.BeginHorizontal();
            
            GUILayout.Label("Database", GUILayout.Width(60));
            EditorGUI.BeginDisabledGroup(true); GUILayout.TextField(_database.Path); EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Open...", GUILayout.Width(60)))
            {
                var directory = string.IsNullOrEmpty(_database.Path) ? "/" : Path.GetDirectoryName(_database.Path);
                var filePath = EditorUtility.OpenFilePanel("Open database", directory, "json");
                if (!string.IsNullOrEmpty(filePath))
                {
                    _database.LoadDatabase(filePath);
                    if (_currentClip != null)
                        _database.ImportMarkers(_currentClip.name, _currentClip.samples, _markers);
                }
                
            }

            if (GUILayout.Button("Save...", GUILayout.Width(60)))
            {
                var directory = string.IsNullOrEmpty(_database.Path) ? "/" : Path.GetDirectoryName(_database.Path);
                var fileName = string.IsNullOrEmpty(_database.Path) ? "MusicDatabase" : Path.GetFileNameWithoutExtension(_database.Path);
                var filePath = EditorUtility.SaveFilePanel("Save database", directory, fileName, "json");
                if (!string.IsNullOrEmpty(filePath))
                    _database.SaveDatabase(filePath);
            }

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_database.Path));
            if (GUILayout.Button("Reload", GUILayout.Width(60)))
            {
                _database.LoadDatabase(_database.Path);
                if (_currentClip != null)
                    _database.ImportMarkers(_currentClip.name, _currentClip.samples, _markers);
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(81));
            EditorGUILayout.LabelField("Name", GUILayout.Width(200));
            EditorGUILayout.LabelField("Total keys", GUILayout.Width(60));
            GUILayout.EndHorizontal();

            _databaseScroll = GUILayout.BeginScrollView(_databaseScroll, GUI.skin.box, GUILayout.Height(120));
            GUILayout.BeginVertical(GUILayout.Height(10));
            
            MusicData toDelete = null;
            MusicData toLoad = null;
            foreach (var data in _database.Data)
            {
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("X", GUILayout.Width(20)))
                    toDelete = data;
                if (GUILayout.Button("Import", GUILayout.Width(50)))
                    toLoad = data;

                EditorGUILayout.SelectableLabel(data.MusicName, GUILayout.Width(200), GUILayout.Height(20));
                EditorGUILayout.SelectableLabel(data.Markers.Count.ToString(), GUILayout.Width(60), GUILayout.Height(20));
                GUILayout.EndHorizontal();
            }

            if (toDelete != null)
            {
                _database.RemoveEntry(toDelete.MusicName);
                if (_currentClip != null && _currentClip.name == toDelete.MusicName)
                    _markers.Clear();
            }
                

            if (toLoad != null && _currentClip != null)
            {
                _database.ImportMarkers(toLoad.MusicName, _currentClip.samples, _markers);
                _database.ExportMarkers(_currentClip.name, _markers);
            }


            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }
    }
}
