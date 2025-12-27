using System;
using System.Collections;
using UnityEngine;
using Verse;

namespace RimPortrait
{
    // Stub class to be expanded in next steps
    public static class AIClient
    {
        public static void GeneratePortrait(string prompt, Action<string> onUrlReceived, string aspectRatio = "1:1", string pawnImageBase64 = null, string styleImageBase64 = null)
        {
            var settings = RimPortraitMod.settings;
            string apiKey = settings.GetCurrentServiceKey();

            if (string.IsNullOrEmpty(apiKey))
            {
                Log.Error("[RimPortrait] No API Key found in settings.");
                onUrlReceived?.Invoke(null);
                return;
            }

            if (settings.serviceType == ServiceType.OpenAI)
            {
                 if (!string.IsNullOrEmpty(pawnImageBase64) || !string.IsNullOrEmpty(styleImageBase64))
                 {
                     Log.Warning("[RimPortrait] Image input is currently only supported for Google Gemini. Ignoring images for OpenAI.");
                 }
                Main.CoroutineRunner.StartCoroutine(Services.OpenAI.OpenAIClient.GenerateImage(apiKey, prompt, onUrlReceived));
            }
            else if (settings.serviceType == ServiceType.GoogleAI)
            {
                Main.CoroutineRunner.StartCoroutine(Services.Gemini.GeminiClient.GenerateImage(apiKey, prompt, aspectRatio, onUrlReceived, pawnImageBase64, styleImageBase64));
            }
            else
            {
                Log.Error("[RimPortrait] Unknown Service Type.");
                onUrlReceived?.Invoke(null);
            }
        }
    }
}
