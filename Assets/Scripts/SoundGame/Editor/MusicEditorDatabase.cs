using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pingle.SoundGame.Editor
{
    [System.Serializable]
    public class MusicEditorDatabase
    {
        public string Path => _path;

        private string _path;
        private MusicDatabase _database;
        private bool _isDirty;
        private float _saveTime;

        public MusicEditorDatabase()
        {
            _database = new MusicDatabase();
            _database.Data = new List<MusicData>();
        }

        public void Autosave()
        {
            if (_isDirty && (int)_saveTime < (int)EditorApplication.timeSinceStartup)
            {
                SaveDatabase(_path);
            }
        }

        public void ImportMarkers(string musicName, int maxSamples, List<int> result)
        {
            try
            {
                result.Clear();

                var data = _database.Data.FirstOrDefault(d => d.MusicName == musicName);
                if (data == null) return;

                result.AddRange(data.Markers.Where(m => m > 0 && m <= maxSamples));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void ExportMarkers(string musicName, IReadOnlyList<int> markers)
        {
            if (string.IsNullOrEmpty(musicName)) return;

            try
            {
                var data = _database.Data.FirstOrDefault(d => d.MusicName == musicName);
                if (data == null)
                {
                    data = new MusicData();
                    data.Markers = new List<int>(markers);
                    data.MusicName = musicName;
                    _database.Data.Add(data);
                }
                else
                {
                    data.Markers.Clear();
                    data.Markers.AddRange(markers);
                }

                _isDirty = true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void LoadDatabase(string path)
        {
            if (!File.Exists(path)) return;

            try
            {
                var json = File.ReadAllText(path);
                var database = JsonUtility.FromJson<MusicDatabase>(json);
                if (database.Data == null) return;
                _database.Data.Clear();
                _database.Data.AddRange(database.Data.Where(d => d.Markers != null && !string.IsNullOrEmpty(d.MusicName)));
                _path = path;
                _isDirty = false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void SaveDatabase(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                var json = JsonUtility.ToJson(_database);
                File.WriteAllText(path, json);
                _path = path;
                _isDirty = false;
                _saveTime = (float)EditorApplication.timeSinceStartup;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
