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

        public static void LoadImage(string urlOrBase64, string pawnId, Action<Texture2D> onComplete)
        {
            // Ensure cache directory exists
            if (!Directory.Exists(CachePath))
            {
                Directory.CreateDirectory(CachePath);
            }

            string filePath = Path.Combine(CachePath, $"{pawnId}.png");

            // Heuristic detection of Base64 (Google API doesn't return data:image prefix usually, just raw base64)
            // But let's check length and content.
            bool isBase64 = IsBase64(urlOrBase64);

            if (isBase64)
            {
                // It's a Base64 string from Gemini
                try 
                {
                    byte[] imageBytes = Convert.FromBase64String(urlOrBase64);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(imageBytes))
                    {
                        // Save to cache for consistency? 
                        // Yes, if we want to reload it later without api call.
                        File.WriteAllBytes(filePath, imageBytes);
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
                // URL (OpenAI) or File Path
                // Check if file exists in cache AND we are not forcing a new generation? 
                // The patching logic (in HarmonyPatches) calls this AFTER generating a NEW url.
                // So if 'url' is provided, we should probably ignore cache if it's a new generation?
                // However, the current logic prefers cache if it exists. 
                // Wait, if we just generated a new image, we want to see THAT one.
                // But HarmonyPatches.cs logic is: unique ID is just pawn.ThingID. 
                // If we generate a new portrait, we likely want to overwrite the old one.
                // But ImageLoader.LoadImage prefers cache. THIS IS A BUG for re-generation.
                // However, for the initial display issue: OpenAI returns a URL.
                
                // If the 'url' argument is actually a URL, we must download it.
                // If we just generated it, we shouldn't read from cache unless we saved it there first.
                // HarmonyPatches passes the NEW url. 
                // If we have a cached file for 'pawnID', LoadImage currently returns THAT instead of downloading the new URL.
                // This means 'Update Portrait' button might just show the old cached image if it exists!
                // But the user's issue is "Image not showing", implies maybe no image at all.
                
                // Let's first fix the Base64 support.
                
                // Note: If url is NOT base64, we assume it's a URL.
                // We should probably NOT verify cache if a URL is explicitly provided?
                // Or maybe we treat 'pawnId' as a cache key.
                
                // For now, let's keep existing cache logic but handle Base64.
                
                if (File.Exists(filePath))
                {
                    // IF the input string LOOKS like a URL (starts with http), maybe we should prioritize downloading it?
                    // But if it's just loading a saved game, we might call LoadImage without a new URL?
                    // Actually HarmonyPatches ONLY calls LoadImage inside the GeneratePortrait callback.
                    // So it's ALWAYS a new image generation event.
                    // So we should FORCE download/processing and overwrite cache.
                    
                    Main.CoroutineRunner.StartCoroutine(DownloadImage(urlOrBase64, filePath, onComplete));
                }
                else
                {
                    Main.CoroutineRunner.StartCoroutine(DownloadImage(urlOrBase64, filePath, onComplete));
                }
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

        private static IEnumerator DownloadImage(string url, string savePath, Action<Texture2D> onComplete)
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
                    
                    // Save to cache
                    try
                    {
                        byte[] bytes = texture.EncodeToPNG();
                        File.WriteAllBytes(savePath, bytes);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[RimPortrait] Failed to save to cache: {ex.Message}");
                    }

                    onComplete?.Invoke(texture);
                }
            }
        }
    }
}
