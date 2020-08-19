using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace Pingle.EditorTools
{
    public static class AubioTrack
    {
        private const string AubioPath = "Plugins/Aubio/AubioTrack.exe";

        private static readonly IReadOnlyList<int> EmptyList = new List<int>();
        private static readonly string[] NewLineSeparator = { Environment.NewLine };

        public static IReadOnlyList<int> DetectBeats(AudioClip audioClip)
        {
            var exePath = Path.Combine(Application.dataPath, AubioPath);
            if (!File.Exists(exePath))
            {
                Debug.LogError($"{exePath} was not found.");
                return EmptyList;
            }

            var wavPath = Path.GetTempFileName();

            try
            {
                WavSaver.Save(wavPath, audioClip);
                var beatsText = ImportBeats(exePath, wavPath);
                if (!string.IsNullOrEmpty(beatsText))
                {
                    return ParseBeats(beatsText, audioClip);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            File.Delete(wavPath);

            return EmptyList;
        }

        private static string ImportBeats(string aubioPath, string inputFile)
        {
            var process = new Process();
            process.StartInfo.FileName = aubioPath;
            process.StartInfo.Arguments = $@"-t 1 -i ""{inputFile}""";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();

            process.WaitForExit(3000);

            if (process.HasExited && process.ExitCode == 0)
            {
                return process.StandardOutput.ReadToEnd();
            }            

            if (!process.HasExited)
                Debug.LogError("AubioTrack timed out.");

            if (process.HasExited && process.ExitCode != 0)
                Debug.LogError("AubioTrack exited with code " + process.ExitCode);

            process.Close();

            return null;
        }

        private static List<int> ParseBeats(string text, AudioClip audioClip)
        {
            var result = new List<int>();
            var clipLength = audioClip.length;
            var sampleCount = audioClip.samples;

            var samples = text.Split(NewLineSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => (int)(sampleCount * (float.Parse(l) / clipLength)));

            return samples.ToList();
        }
    }
}
