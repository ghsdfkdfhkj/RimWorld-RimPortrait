using System;
using System.Collections;
using UnityEngine;
using Verse;

namespace RimPortrait
{
    public static class AIClient
    {
        /// <summary>
        /// Generates a portrait using the selected AI service.
        /// </summary>
        /// <param name="prompt">The text prompt describing the portrait.</param>
        /// <param name="onUrlReceived">Callback when the image URL or Base64 string is received. Returns null on failure.</param>
        /// <param name="aspectRatio">Target aspect ratio (Gemini only).</param>
        /// <param name="pawnImageBase64">Optional base64 image of the pawn for reference (Gemini only).</param>
        /// <param name="styleImageBase64">Optional base64 image for style reference (Gemini only).</param>
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
