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
    }
}
