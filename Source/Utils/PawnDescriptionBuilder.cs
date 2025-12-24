using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimPortrait
{
    public static class PawnDescriptionBuilder
    {
        public static string GetPawnDescription(Pawn pawn)
        {
            if (pawn == null) return "A high quality portrait of a mysterious figure. Artstation style.";

            StringBuilder sb = new StringBuilder();
            sb.Append("A high quality portrait of a ");

            // 1. Gender & Age
            // Example: "25 year old female"
            if (pawn.ageTracker != null)
            {
                sb.Append($"{pawn.ageTracker.AgeBiologicalYears} year old ");
            }
            else
            {
                sb.Append("adult ");
            }

            if (pawn.gender != Gender.None)
            {
                sb.Append($"{pawn.gender.ToString().ToLower()}, ");
            }
            else
            {
                sb.Append("person, ");
            }

            // 2. Skin Color
            if (pawn.story != null)
            {
                string skinTone = GetColorName(pawn.story.SkinColor, isSkin: true);
                sb.Append($"{skinTone} skin, ");
            }

            // 3. Hair Style & Color
            if (pawn.story != null && pawn.story.hairDef != null)
            {
                string hairColor = GetColorName(pawn.story.HairColor, isSkin: false);
                // Clean up hair label (e.g., "Curly (cut)" -> "Curly")
                string hairStyle = pawn.story.hairDef.label; 
                sb.Append($"{hairColor} {hairStyle} hair, ");
            }
            else
            {
                sb.Append("bald head, ");
            }

            // 4. Apparel
            if (pawn.apparel != null && pawn.apparel.WornApparel.Count > 0)
            {
                List<string> apparelLabels = pawn.apparel.WornApparel
                    .Where(a => a.def != null)
                    .Select(a => a.def.label)
                    .Distinct()
                    .ToList();

                if (apparelLabels.Count > 0)
                {
                    sb.Append("wearing " + string.Join(", ", apparelLabels));
                }
                else
                {
                    sb.Append("wearing simple clothes");
                }
            }
            else
            {
                sb.Append("wearing simple clothes");
            }

            sb.Append(". Artstation style.");

            return sb.ToString();
        }

        private static string GetColorName(Color color, bool isSkin)
        {
            // Define some palette colors for mapping
            // Skin tones
            if (isSkin)
            {
                if (IsClose(color, new Color(0.95f, 0.9f, 0.9f))) return "pale";
                if (IsClose(color, new Color(1f, 0.8f, 0.6f))) return "fair";
                if (IsClose(color, new Color(0.9f, 0.7f, 0.5f))) return "light tan";
                if (IsClose(color, new Color(0.8f, 0.6f, 0.4f))) return "tanned";
                if (IsClose(color, new Color(0.5f, 0.35f, 0.2f))) return "brown";
                if (IsClose(color, new Color(0.3f, 0.2f, 0.1f))) return "dark brown";
                if (IsClose(color, new Color(0.2f, 0.2f, 0.2f))) return "black";
                
                // If it's a fantasy color (aliens etc), fallback to generic nearest color method
            }

            // Generic colors (Hair, etc.)
            Dictionary<string, Color> palette = new Dictionary<string, Color>
            {
                { "white", Color.white },
                { "black", Color.black },
                { "grey", Color.grey },
                { "red", Color.red },
                { "green", Color.green },
                { "blue", Color.blue },
                { "yellow", Color.yellow },
                { "cyan", Color.cyan },
                { "magenta", Color.magenta },
                { "brown", new Color(0.5f, 0.35f, 0.2f) },
                { "blonde", new Color(1f, 0.9f, 0.6f) },
                { "orange", new Color(1f, 0.5f, 0f) },
                { "pink", new Color(1f, 0.7f, 0.8f) },
                { "purple", new Color(0.5f, 0f, 0.5f) },
                { "teal", new Color(0f, 0.5f, 0.5f) }
            };

            string closestName = "colored";
            float minDistance = float.MaxValue;

            foreach (var kvp in palette)
            {
                float dist = ColorDistance(color, kvp.Value);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestName = kvp.Key;
                }
            }

            // If it is skin and we fell through (fantasy colors), return the color name
            if (isSkin && minDistance > 0.3f) 
            {
                // Heuristic: check brightness
                float brightness = color.grayscale;
                if (brightness > 0.8f) return "pale";
                if (brightness < 0.2f) return "dark";
                return closestName;
            }

            return closestName;
        }

        private static bool IsClose(Color a, Color b, float tolerance = 0.15f)
        {
            return ColorDistance(a, b) < tolerance;
        }

        private static float ColorDistance(Color a, Color b)
        {
            return Mathf.Sqrt(
                Mathf.Pow(a.r - b.r, 2) +
                Mathf.Pow(a.g - b.g, 2) +
                Mathf.Pow(a.b - b.b, 2)
            );
        }
    }
}
