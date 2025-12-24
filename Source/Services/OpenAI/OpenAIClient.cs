using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Verse;

namespace RimPortrait.Services.OpenAI
{
    public static class OpenAIClient
    {
        private const string ApiUrl = "https://api.openai.com/v1/images/generations";

        public static IEnumerator GenerateImage(string apiKey, string prompt, Action<string> onUrlReceived)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                Log.Error("[RimPortrait] OpenAI API Key is missing.");
                onUrlReceived?.Invoke(null);
                yield break;
            }

            // Create JSON payload
            // Using DALL-E 3 standard payload
            string jsonPayload = $"{{\"model\": \"dall-e-3\", \"prompt\": \"{EscapeJson(prompt)}\", \"n\": 1, \"size\": \"1024x1024\"}}";

            using (UnityWebRequest uwr = new UnityWebRequest(ApiUrl, "POST"))
            {
                byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonPayload);
                uwr.uploadHandler = new UploadHandlerRaw(jsonToSend);
                uwr.downloadHandler = new DownloadHandlerBuffer();
                
                uwr.SetRequestHeader("Content-Type", "application/json");
                uwr.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                Log.Message($"[RimPortrait] Requesting OpenAI DALL-E 3 Image...");

                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    Log.Error($"[RimPortrait] OpenAI API Error: {uwr.error}\nResponse: {uwr.downloadHandler.text}");
                    onUrlReceived?.Invoke(null);
                }
                else
                {
                    string responseText = uwr.downloadHandler.text;
                    // Log.Message($"[RimPortrait] Response: {responseText}"); // Debug handling

                    // Parse JSON manually or via simple helper to avoid huge dependencies
                    // structure: { "created": ..., "data": [ { "url": "HTTPS..." } ] }
                    
                    string imageUrl = ExtractUrlFromJson(responseText);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        onUrlReceived?.Invoke(imageUrl);
                    }
                    else
                    {
                        Log.Error("[RimPortrait] Failed to parse URL from OpenAI response.");
                        onUrlReceived?.Invoke(null);
                    }
                }
            }
        }

        private static string EscapeJson(string str)
        {
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string ExtractUrlFromJson(string json)
        {
            // Simple string search to avoid heavy JSON libraries if not needed
            // Look for "url": "..."
            string key = "\"url\":";
            int keyIndex = json.IndexOf(key);
            if (keyIndex == -1) return null;

            int start = json.IndexOf("\"", keyIndex + key.Length);
            if (start == -1) return null;
            start++; // move past quote

            int end = json.IndexOf("\"", start);
            if (end == -1) return null;

            return json.Substring(start, end - start);
        }
    }
}
