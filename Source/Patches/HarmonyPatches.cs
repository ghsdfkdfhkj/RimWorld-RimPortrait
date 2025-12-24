using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using System;

namespace RimPortrait
{
    [HarmonyPatch(typeof(CharacterCardUtility), "DrawCharacterCard")]
    public static class CharacterCardUtility_Patch
    {
        public static void Postfix(Rect rect, Pawn pawn)
        {
            if (pawn == null || !pawn.IsColonist) return;
            
            float buttonSize = 24f;
            float margin = 18f;

            // Draw button below the top-right buttons (Rename/Banish)
            // Typically those are at y ~ 18. Let's push this one down.
            // Previous 'margin' was 18f. Let's try y = 18 + 24 + 10 = 52f.
            Rect btnRect = new Rect(rect.width - buttonSize - margin, margin + buttonSize + 10f, buttonSize, buttonSize);
             
            if (Widgets.ButtonImage(btnRect, BaseContent.BadTex)) // Placeholder texture
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                string desc = PawnDescriptionBuilder.GetPawnDescription(pawn);
                
                // Open Configuration Dialog
                Find.WindowStack.Add(new Dialog_GenerationConfig(pawn, desc));
            }
            TooltipHandler.TipRegion(btnRect, "RimPortrait_Command_Generate_Tooltip".Translate());
        }
    }
}
