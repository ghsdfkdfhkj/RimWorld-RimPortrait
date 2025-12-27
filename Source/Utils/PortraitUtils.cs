using UnityEngine;
using System;
using Verse;

namespace RimPortrait
{
    public static class PortraitUtils
    {
        public static string ExtractBase64FromGoogle(string jsonResponse)
        {
            // Expected JSON structure for Gemini Image Generation:
            // {
            //   "candidates": [
            //     {
            //       "content": {
            //         "parts": [
            //           {
            //             "inlineData": {
            //               "mimeType": "image/png",
            //               "data": "BASE64_STRING_HERE"
            //             }
            //           }
            //         ]
            //       }
            //     }
            //   ]
            // }

            try
            {
                // Simple string parsing to find "data": "..."
                // This is fragile but avoids external JSON library dependencies if not strictly present.
                // It searches for the "inlineData" block and then the "data" field within it.
                
                string searchKey = "\"data\": \"";
                int dataIndex = jsonResponse.IndexOf(searchKey);
                if (dataIndex == -1)
                {
                    // Fallback/Check for error logic or different format
                    Log.Error($"[RimPortrait] Could not find 'data' field in Google response: {jsonResponse}");
                    return null;
                }

                int start = dataIndex + searchKey.Length;
                int end = jsonResponse.IndexOf("\"", start);
                
                if (end == -1)
                {
                    Log.Error("[RimPortrait] Malformed JSON in Google response.");
                    return null;
                }

                return jsonResponse.Substring(start, end - start);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimPortrait] Error parsing Google response: {ex.Message}");
                return null;
            }
        }


        public static string TextureToBase64(Texture2D texture)
        {
            if (texture == null) return null;
            try
            {
                byte[] bytes = texture.EncodeToPNG();
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                Log.Error($"[RimPortrait] Failed to encode texture to Base64: {ex.Message}");
                return null;
            }
        }

        public static string RenderTextureToBase64(RenderTexture rt)
        {
            if (rt == null) return null;
            
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new UnityEngine.Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            string base64 = TextureToBase64(tex);
            UnityEngine.Object.Destroy(tex); // Cleanup
            return base64;
        }
    }
}
