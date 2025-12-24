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

            // Draw button at the top-right of the card rect
            // The rect passed to DrawCharacterCard is the card's rect.
            Rect btnRect = new Rect(rect.width - buttonSize - margin, margin, buttonSize, buttonSize);
             
            if (Widgets.ButtonImage(btnRect, BaseContent.BadTex)) // Placeholder texture
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                string desc = PawnDescriptionBuilder.GetPawnDescription(pawn);
                Log.Message("[RimPortrait] Generating portrait for: " + desc);
                
                AIClient.GeneratePortrait(desc, (url) => {
                    if (string.IsNullOrEmpty(url)) {
                        Log.Error("[RimPortrait] Generated URL is empty.");
                        return;
                    }
                    
                    ImageLoader.LoadImage(url, pawn.ThingID, (texture) => {
                         if (texture != null)
                         {
                             Find.WindowStack.Add(new Dialog_PortraitViewer(texture, pawn.Name.ToStringFull));
                         }
                         else
                         {
                             Log.Error("[RimPortrait] Failed to load generated image.");
                         }
                    });
                });
            }
            TooltipHandler.TipRegion(btnRect, "RimPortrait_Command_Generate_Tooltip".Translate());
        }
    }
}
