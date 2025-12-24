using System;
using UnityEngine;
using Verse;

namespace RimPortrait
{
    public class Dialog_GenerationConfig : Window
    {
        private string prompt;
        private string selectedAspectRatio = "1:1";
        private string selectedStyle = "Default";
        private Pawn pawn;
        
        // Gemini Aspect Ratios
        private static readonly string[] AspectRatios = { "1:1", "3:4", "4:3", "9:16", "16:9" };
        
        // Art Styles
        private static readonly string[] ArtStyles = { "Artstation", "Anime", "Oil Painting", "Watercolor", "Cyberpunk", "Realistic", "Sketch", "Pixel Art" };

        // Compositions
        private static readonly string[] Compositions = { "Portrait", "Waist-up", "Full Body", "Cinematic", "Isometric" };

        private string selectedComposition = "Portrait";


        public override Vector2 InitialSize => new Vector2(500f, 650f); // Increased height for composition

        public Dialog_GenerationConfig(Pawn pawn, string initialPrompt)
        {
            this.pawn = pawn;
            this.prompt = initialPrompt;
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("RimPortrait_Config_Title".Translate());
            Text.Font = GameFont.Small;
            listing.Gap();

            // Prompt Section
            listing.Label("RimPortrait_Config_Prompt".Translate());
            prompt = listing.TextEntry(prompt, 5); 
            listing.Gap();
            
            // Art Style Section
            // Art Style Section
            listing.Label("RimPortrait_Config_Style".Translate() + ": " + selectedStyle); 
            
            Rect styleRect = listing.GetRect(60f); // 2 rows of buttons
            float widthPerStyle = styleRect.width / 4f; 
            
            for (int i = 0; i < ArtStyles.Length; i++)
            {
                int row = i / 4;
                int col = i % 4;
                Rect btnRect = new Rect(styleRect.x + (col * widthPerStyle), styleRect.y + (row * 30f), widthPerStyle - 5f, 30f);
                if (Widgets.ButtonText(btnRect, ArtStyles[i]))
                {
                    if (selectedStyle != ArtStyles[i])
                    {
                        selectedStyle = ArtStyles[i];
                        UpdatePromptStyle(selectedStyle);
                    }
                }
            }
            listing.Gap(40f);

            // Composition Section
            listing.Label("RimPortrait_Config_Composition".Translate() + ": " + selectedComposition);
            Rect compRect = listing.GetRect(30f);
            float widthPerComp = compRect.width / Compositions.Length;

            for (int i = 0; i < Compositions.Length; i++)
            {
                Rect btnRect = new Rect(compRect.x + (i * widthPerComp), compRect.y, widthPerComp - 5f, 30f);
                if (Widgets.ButtonText(btnRect, Compositions[i]))
                {
                    if (selectedComposition != Compositions[i])
                    {
                        selectedComposition = Compositions[i];
                        UpdatePromptComposition(selectedComposition);
                    }
                }
            }
            listing.Gap(30f);

            // Aspect Ratio Section
            listing.Label("RimPortrait_Config_AspectRatio".Translate() + ": " + selectedAspectRatio);
            
            Rect ratioRect = listing.GetRect(30f);
            float widthPerBtn = ratioRect.width / AspectRatios.Length;
            
            for (int i = 0; i < AspectRatios.Length; i++)
            {
                Rect btnRect = new Rect(ratioRect.x + (i * widthPerBtn), ratioRect.y, widthPerBtn - 5f, 30f);
                if (Widgets.ButtonText(btnRect, AspectRatios[i]))
                {
                    selectedAspectRatio = AspectRatios[i];
                }
            }
            listing.Gap(30f);

            // Generate Button
            if (listing.ButtonText("RimPortrait_Command_Generate".Translate()))
            {
                Generate();
            }

            // Close/Cancel
            if (listing.ButtonText("CloseButton".Translate()))
            {
                Close();
            }

            listing.End();
        }

        private void Generate()
        {
            string finalPrompt = prompt;
            // Style is already in the prompt now
            
            Log.Message($"[RimPortrait] Starting generation with prompt: {finalPrompt} | Ratio: {selectedAspectRatio}");
            
            AIClient.GeneratePortrait(finalPrompt, (url) => {
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
            }, selectedAspectRatio);
            
            Close();
        }


        private void UpdatePromptComposition(string newComp)
        {
            string prefix = ", ";
            
            // Remove known compositions
            foreach (string comp in Compositions)
            {
                string pattern = $"{prefix}{comp}";
                if (prompt.EndsWith(pattern))
                {
                    prompt = prompt.Substring(0, prompt.Length - pattern.Length);
                    break;
                }
                // Also check if it's potentially followed by style (complex case not fully handled but basic suffix logic works if order is strictly Composition then Style? Or Style then Composition?)
                // Current logic appends. So order depends on user click order. 
                // To be robust, we need to regex or search anywhere? 
                // For simplicity: Simple suffix replacement. If user mixes clicks, prompt might get ", Style, Comp".
                // If I click Style then Comp: "..., Style, Comp".
                // If I click Comp then Style: "..., Comp, Style".
                // Deleting via suffix matches only the LAST element.
                // This is a limitation. But simple.
                // Let's improve robustness: Replace specific substring anywhere.
                
                string search = $", {comp}";
                int idx = prompt.IndexOf(search);
                if (idx != -1)
                {
                    // Check if it matches a full word boundary if possible, but here they are specific keys.
                    // Just remove it.
                     prompt = prompt.Remove(idx, search.Length);
                }
            }
            
            // Append
            prompt += $", {newComp}";
        }

        private void UpdatePromptStyle(string newStyle)
        {
            string suffix = " art style";
            
            // Remove any known style suffix logic updated to 'search and remove' for robustness
            foreach (string style in ArtStyles)
            {
                if (style == "Default") continue; // Should be removed from array anyway
                
                string search = $", {style}{suffix}";
                int idx = prompt.IndexOf(search);
                if (idx != -1)
                {
                     prompt = prompt.Remove(idx, search.Length);
                }
            }

            prompt += $", {newStyle}{suffix}";
        }

        private void Close()
        {
            base.Close();
        }
    }
}
