using System;
using System.IO;
using UnityEngine;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Game
{
    /// <summary>
    /// The one save slot: a <see cref="RunSnapshot"/> as JSON in the player's data folder. Core decides what is saved
    /// and whether a save still fits the content; this only reads and writes the file.
    /// </summary>
    static class SaveFile
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "expedition.json");

        public static bool Exists => File.Exists(FilePath);

        /// <summary>Writes to a temporary file first, so a crash mid-write leaves the last good save in place.</summary>
        public static void Write(RunSnapshot snapshot)
        {
            try
            {
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(snapshot, prettyPrint: true));
                File.Copy(temp, FilePath, overwrite: true);
                File.Delete(temp);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"[Save] Couldn't write {FilePath}: {e.Message}");
            }
        }

        /// <summary>The saved run, or null with the reason the file couldn't be read.</summary>
        public static RunSnapshot Read(out string problem)
        {
            problem = null;
            try
            {
                var snapshot = JsonUtility.FromJson<RunSnapshot>(File.ReadAllText(FilePath));
                if (snapshot == null) problem = "The save file is empty.";
                return snapshot;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                problem = "The save file couldn't be read.";
                Debug.LogError($"[Save] Couldn't read {FilePath}: {e.Message}");
                return null;
            }
        }

        public static void Delete()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"[Save] Couldn't delete {FilePath}: {e.Message}");
            }
        }
    }
}
