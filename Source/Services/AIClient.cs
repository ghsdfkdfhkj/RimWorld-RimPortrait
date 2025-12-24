using System;
using System.Collections;
using UnityEngine;
using Verse;

namespace RimPortrait
{
    // Stub class to be expanded in next steps
    public static class AIClient
    {
        public static void GeneratePortrait(string prompt, Action<string> onUrlReceived, string aspectRatio = "1:1")
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
                // OpenAI DALL-E 3 supports "1024x1024", "1024x1792", etc.
                // We'll need to map "1:1" to "1024x1024".
                // For now, let's just pass the prompt. Aspect ratio support for OpenAI can be a todo.
                Main.CoroutineRunner.StartCoroutine(Services.OpenAI.OpenAIClient.GenerateImage(apiKey, prompt, onUrlReceived));
            }
            else if (settings.serviceType == ServiceType.GoogleAI)
            {
                Main.CoroutineRunner.StartCoroutine(Services.Gemini.GeminiClient.GenerateImage(apiKey, prompt, aspectRatio, onUrlReceived));
            }
            else
            {
                Log.Error("[RimPortrait] Unknown Service Type.");
                onUrlReceived?.Invoke(null);
            }
        }
    }
}
