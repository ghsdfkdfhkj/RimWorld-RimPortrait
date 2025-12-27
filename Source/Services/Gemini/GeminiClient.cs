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

        public static IEnumerator GenerateImage(string apiKey, string prompt, string aspectRatio, Action<string> onUrlReceived, string pawnImageBase64 = null, string styleImageBase64 = null)
        {
            var settings = RimPortraitMod.settings;
            string url = settings.geminiModel == GeminiModel.NanoBananaPro ? ApiUrlPro : ApiUrlFlash;
            
            // Construct JSON payload
            // We need to build the 'parts' array manually to include images if present.
            StringBuilder partsJson = new StringBuilder();
            partsJson.Append("{\"text\": \"" + EscapeJson(prompt) + "\"}");

            if (!string.IsNullOrEmpty(pawnImageBase64))
            {
                partsJson.Append(", {\"inlineData\": {\"mimeType\": \"image/png\", \"data\": \"" + pawnImageBase64 + "\"}}");
            }
            if (!string.IsNullOrEmpty(styleImageBase64))
            {
                 partsJson.Append(", {\"inlineData\": {\"mimeType\": \"image/png\", \"data\": \"" + styleImageBase64 + "\"}}");
            }

            string jsonPayload = "{\"contents\": [{ \"parts\": [ " + partsJson.ToString() + " ] }], \"generationConfig\": { \"imageConfig\": { \"aspectRatio\": \"" + aspectRatio + "\" } } }";

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
