using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Verse;

namespace RimPortrait
{
    public static class ImageLoader
    {
        public static string CachePath => Path.Combine(GenFilePaths.SaveDataFolderPath, "RimPortrait", "Cache");

        public static void LoadImage(string urlOrBase64, Pawn pawn, Action<Texture2D> onComplete, bool forceRefresh = false, string historyBaseName = null)
        {
            // Ensure cache directory exists
            if (!Directory.Exists(CachePath))
            {
                Directory.CreateDirectory(CachePath);
            }

            string pawnId = pawn.ThingID;
            
            // Resolve Filename using DataStore
            var store = Find.World.GetComponent<RimPortraitDataStore>();
            string currentFilename = store?.GetPortraitPath(pawnId);
            
            // Fallback to old format if not in store
            if (string.IsNullOrEmpty(currentFilename))
            {
                currentFilename = $"{pawnId}.png";
            }
            
            string filePath = Path.Combine(CachePath, currentFilename);
            
            if (!forceRefresh && string.IsNullOrEmpty(urlOrBase64))
            {
                // Try to load existing
                if (File.Exists(filePath))
                {
                    Main.CoroutineRunner.StartCoroutine(LoadLocalImage(filePath, onComplete));
                }
                else
                {
                    // If stored filename doesn't exist, check for legacy PawnID.png just in case
                    string legacyPath = Path.Combine(CachePath, $"{pawnId}.png");
                    if (File.Exists(legacyPath))
                    {
                        Main.CoroutineRunner.StartCoroutine(LoadLocalImage(legacyPath, onComplete));
                    }
                    else
                    {
                        onComplete?.Invoke(null);
                    }
                }
                return;
            }

            // Heuristic detection of Base64
            bool isBase64 = IsBase64(urlOrBase64);

            if (isBase64)
            {
                try 
                {
                    byte[] imageBytes = Convert.FromBase64String(urlOrBase64);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(imageBytes))
                    {
                        // Logic for Saving New Image
                        SaveNewImage(pawn, texture, historyBaseName);
                        onComplete?.Invoke(texture);
                    }
                    else
                    {
                         Log.Error("[RimPortrait] Failed to LoadImage from base64 bytes.");
                         onComplete?.Invoke(null);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"[RimPortrait] Exception loading base64 image: {ex.Message}");
                    onComplete?.Invoke(null);
                }
            }
            else
            {
                // URL (OpenAI)
                if (!string.IsNullOrEmpty(urlOrBase64))
                {
                    Main.CoroutineRunner.StartCoroutine(DownloadImage(urlOrBase64, pawn, onComplete, historyBaseName));
                }
                else
                {
                    Log.Warning($"[RimPortrait] No valid image source found for {pawnId} and no cache exists.");
                    onComplete?.Invoke(null);
                }
            }
        }
        
        private static void SaveNewImage(Pawn pawn, Texture2D texture, string baseName)
        {
            try
            {
                // Determine new filename
                string finalFilename;
                
                if (!string.IsNullOrEmpty(baseName))
                {
                    string sanitized = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars()));
                    finalFilename = $"{sanitized}.png";
                    string fullPath = Path.Combine(CachePath, finalFilename);
                    
                    int count = 1;
                    while (File.Exists(fullPath))
                    {
                        finalFilename = $"{sanitized}({count}).png";
                        fullPath = Path.Combine(CachePath, finalFilename);
                        count++;
                    }
                }
                else
                {
                    // Fallback to PawnID if no name provided (shouldn't happen with current logic but safe)
                    finalFilename = $"{pawn.ThingID}.png";
                }

                string savePath = Path.Combine(CachePath, finalFilename);
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(savePath, bytes);
                
                // Update DataStore
                var store = Find.World.GetComponent<RimPortraitDataStore>();
                store?.SetPortraitPath(pawn.ThingID, finalFilename);
                
                Log.Message($"[RimPortrait] Saved portrait: {finalFilename}");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimPortrait] Failed to save image: {ex.Message}");
            }
        }
        
        private static bool IsBase64(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            // Base64 strings are usually long and don't start with http
            if (s.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return false;
            if (s.Length < 100) return false; // Arbitrary min length for an image
            
            // Fast check
            return (s.Length % 4 == 0) && System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,2}$", System.Text.RegularExpressions.RegexOptions.None);
        }

        private static IEnumerator LoadLocalImage(string filePath, Action<Texture2D> onComplete)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture("file://" + filePath))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    Log.Error($"[RimPortrait] Failed to load local image: {uwr.error}");
                }
                else
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                    onComplete?.Invoke(texture);
                }
            }
        }

        private static IEnumerator DownloadImage(string url, Pawn pawn, Action<Texture2D> onComplete, string historyBaseName = null)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    Log.Error($"[RimPortrait] Failed to download image from {url}: {uwr.error}");
                    onComplete?.Invoke(null);
                }
                else
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                    
                    // Save
                    SaveNewImage(pawn, texture, historyBaseName);

                    onComplete?.Invoke(texture);
                }
            }
        }
    }
}
