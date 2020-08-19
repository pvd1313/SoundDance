using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace Pingle.SoundGame.Editor
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
            var beatsPath = Path.GetTempFileName();

            try
            {
                WavExporter.ExportClip(audioClip, wavPath);

                if (ExportBeats(exePath, wavPath, beatsPath))
                {
                    var beats = ParseBeats(beatsPath);

                    File.Delete(wavPath);
                    File.Delete(beatsPath);

                    return beats;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            File.Delete(wavPath);
            File.Delete(beatsPath);

            return EmptyList;
        }

        private static bool ExportBeats(string aubioPath, string inputFile, string outputFile)
        {
            var process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $@"/C {aubioPath} >> {outputFile} -t 1 -T samples -i ""{inputFile}""";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();

            process.WaitForExit(3000);

            if (process.HasExited && process.ExitCode == 0)
            {
                return true;
            }            

            if (!process.HasExited)
                Debug.LogError("AubioTrack timed out.");

            if (process.HasExited && process.ExitCode != 0)
                Debug.LogError("AubioTrack exited with code " + process.ExitCode);

            process.Close();

            return false;
        }

        private static List<int> ParseBeats(string fileName)
        {
            var text = File.ReadAllText(fileName);
            var samples = text.Split(NewLineSeparator, StringSplitOptions.RemoveEmptyEntries).Select(l => int.Parse(l));

            return samples.ToList();
        }

    }
}
