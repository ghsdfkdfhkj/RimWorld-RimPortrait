using System;
using System.Collections;
using UnityEngine;
using Verse;

namespace RimPortrait
{
    // Stub class to be expanded in next steps
    public static class AIClient
    {
        public static void GeneratePortrait(string prompt, Action<string> onUrlReceived)
        {
            var settings = RimPortraitMod.settings;
            string apiKey = settings.apiKey;

            if (string.IsNullOrEmpty(apiKey))
            {
                Log.Error("[RimPortrait] No API Key found in settings.");
                onUrlReceived?.Invoke(null);
                return;
            }

            if (settings.serviceType == ServiceType.OpenAI)
            {
                Main.CoroutineRunner.StartCoroutine(Services.OpenAI.OpenAIClient.GenerateImage(apiKey, prompt, onUrlReceived));
            }
            else if (settings.serviceType == ServiceType.GoogleAI)
            {
                Main.CoroutineRunner.StartCoroutine(Services.Gemini.GeminiClient.GenerateImage(apiKey, prompt, onUrlReceived));
            }
            else
            {
                Log.Error("[RimPortrait] Unknown Service Type.");
                onUrlReceived?.Invoke(null);
            }
        }
    }
}
