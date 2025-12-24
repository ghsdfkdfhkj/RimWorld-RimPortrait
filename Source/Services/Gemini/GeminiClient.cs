using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Verse;

namespace RimPortrait.Services.Gemini
{
    public static class GeminiClient
    {
        private const string ApiUrlFlash = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-image:generateContent";
        private const string ApiUrlPro = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3-pro-image-preview:generateContent";

        public static IEnumerator GenerateImage(string apiKey, string prompt, Action<string> onUrlReceived)
        {
            var settings = RimPortraitMod.settings;
            string url = settings.geminiModel == GeminiModel.NanoBananaPro ? ApiUrlPro : ApiUrlFlash;
            
            // Construct JSON payload
            // Note: "aspectRatio" is 1:1 by default if omitted, but let's be explicit if needed. 
            // For now, we'll stick to a simple default.
            string jsonPayload = "{\"contents\": [{ \"parts\": [ {\"text\": \"" + EscapeJson(prompt) + "\"} ] }], \"generationConfig\": { \"imageConfig\": { \"aspectRatio\": \"1:1\" } } }";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-goog-api-key", apiKey);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Log.Error($"[RimPortrait] Gemini API Error: {request.error}\nResponse: {request.downloadHandler.text}");
                    onUrlReceived?.Invoke(null);
                }
                else
                {
                    string responseText = request.downloadHandler.text;
                    string base64Image = PortraitUtils.ExtractBase64FromGoogle(responseText);
                    onUrlReceived?.Invoke(base64Image);
                }
            }
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
