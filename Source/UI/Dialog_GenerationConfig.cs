using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace RimPortrait
{
    public class Dialog_GenerationConfig : Window
    {
        private string prompt;
        private string selectedAspectRatio = "1:1";
        private string selectedStyle = "Rimworld";
        private string selectedComposition = "Portrait";
        private Pawn pawn;
        
        // Gemini Aspect Ratios
        private static readonly string[] AspectRatios = { "1:1", "3:4", "4:3", "9:16", "16:9" };
        
        // Art Styles
        private static readonly string[] ArtStyles = { "Rimworld", "Artstation", "Anime", "Oil Painting", "Watercolor", "Realistic", "Sketch", "Pixel Art" };

        // Compositions
        private static readonly string[] Compositions = { "Portrait", "Waist-up", "Full Body", "Cinematic", "Isometric" };

        // Accordion States
        private bool artStyleExpanded = false;
        private bool compositionExpanded = false;
        private bool aspectRatioExpanded = false;
        
        // Image Input States
        // Image Input States
        private bool usePawnAppearance = true;
        private string selectedStyleImagePath = null;
        private Texture2D cachedStyleTexture = null;

        private Vector2 scrollPosition;
        private float viewHeight = 1000f; // Initial height, updates dynamically

        public override Vector2 InitialSize => new Vector2(500f, 750f); 

        public Dialog_GenerationConfig(Pawn pawn, string initialPrompt)
        {
            this.pawn = pawn;
            this.prompt = initialPrompt;
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = false;

            // Apply default prompt modifiers
            UpdatePromptStyle(selectedStyle);
            UpdatePromptComposition(selectedComposition);
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Bottom area for buttons
            float btnHeight = 30f;
            float btnGap = 10f;
            float bottomMargin = 10f;
            // Layout: Generate Button, then Close Button, with gaps
            float bottomAreaHeight = (btnHeight * 2) + (btnGap * 2) + bottomMargin;

            // Scroll View Area
            Rect outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - bottomAreaHeight);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            
            Listing_Standard listing = new Listing_Standard();
            // Use infinite height for the listing so it doesn't clip content based on the previous frame's height
            listing.Begin(new Rect(0f, 0f, viewRect.width, float.MaxValue));

            Text.Font = GameFont.Medium;
            listing.Label("RimPortrait_Config_Title".Translate());
            Text.Font = GameFont.Small;
            listing.Gap();

            // Prompt Section
            listing.Label("RimPortrait_Config_Prompt".Translate());
            prompt = listing.TextEntry(prompt, 5); 
            listing.Gap();
            
            // Image Reference Section
            // Image Reference Section
            listing.Gap(5f);
            listing.Label("RimPortrait_Config_ImageReferences".Translate());
            listing.CheckboxLabeled("RimPortrait_Config_UsePawnAppearance".Translate(), ref usePawnAppearance, "RimPortrait_Config_UsePawnAppearanceDesc".Translate());
            
            listing.Gap(5f);
            listing.Label("RimPortrait_Config_StyleImageReference".Translate());

            Rect styleRow = listing.GetRect(30f);
            if (Widgets.ButtonText(new Rect(styleRow.x, styleRow.y, 150f, 30f), "RimPortrait_Config_SelectStyleImage".Translate()))
            {
                Find.WindowStack.Add(new Dialog_StyleImageSelector((path) => {
                    selectedStyleImagePath = path;
                    // Load preview
                    if (cachedStyleTexture != null) UnityEngine.Object.Destroy(cachedStyleTexture);
                    cachedStyleTexture = null;
                    
                    if (File.Exists(path)) {
                        try {
                            byte[] data = File.ReadAllBytes(path);
                            cachedStyleTexture = new Texture2D(2, 2);
                            cachedStyleTexture.LoadImage(data);
                        } catch {}
                    }
                }));
            }
            
            if (Widgets.ButtonText(new Rect(styleRow.x + 160f, styleRow.y, 120f, 30f), "RimPortrait_Config_ClearSelection".Translate()))
            {
                selectedStyleImagePath = null;
                if (cachedStyleTexture != null) UnityEngine.Object.Destroy(cachedStyleTexture);
                cachedStyleTexture = null;
            }
            
            if (!string.IsNullOrEmpty(selectedStyleImagePath))
            {
                listing.Gap(5f);
                listing.Label("RimPortrait_Config_SelectedImage".Translate(Path.GetFileName(selectedStyleImagePath)));
                
                if (cachedStyleTexture != null)
                {
                    Rect previewRect = listing.GetRect(150f);
                    // Center the image
                    float aspect = (float)cachedStyleTexture.width / cachedStyleTexture.height;
                    float h = 150f;
                    float w = h * aspect;
                    if (w > previewRect.width) { w = previewRect.width; h = w / aspect; }
                    
                    Rect imgRect = new Rect(previewRect.x, previewRect.y, w, h);
                    GUI.DrawTexture(imgRect, cachedStyleTexture, ScaleMode.ScaleToFit);
                }
            }
            listing.Gap();

            Color darkHeaderColor = new Color(0.12f, 0.12f, 0.12f);
            Color darkHeaderBorderColor = new Color(0.35f, 0.35f, 0.35f);

            // --- Art Style Section (Accordion) ---
            // --- Art Style Section (Accordion) ---
            bool styleDisabled = !string.IsNullOrEmpty(selectedStyleImagePath);
            string styleHeader = "RimPortrait_Config_Style".Translate();
            if (styleDisabled) styleHeader += " " + "RimPortrait_Config_StyleDisabledByImage".Translate();

            DrawAccordionSection(listing, styleHeader, selectedStyle, ref artStyleExpanded, darkHeaderColor, darkHeaderBorderColor, () => {
                DrawOptionGrid(listing, ArtStyles, selectedStyle, (val) => {
                    selectedStyle = val;
                    UpdatePromptStyle(selectedStyle);
                });
            }, disabled: styleDisabled);

            // --- Composition Section (Accordion) ---
            DrawAccordionSection(listing, "RimPortrait_Config_Composition".Translate(), selectedComposition, ref compositionExpanded, darkHeaderColor, darkHeaderBorderColor, () => {
                 DrawOptionGrid(listing, Compositions, selectedComposition, (val) => {
                     if (selectedComposition != val)
                     {
                         selectedComposition = val;
                         UpdatePromptComposition(selectedComposition);
                     }
                 });
            });

            // --- Aspect Ratio Section (Accordion) ---
            DrawAccordionSection(listing, "RimPortrait_Config_AspectRatio".Translate(), selectedAspectRatio, ref aspectRatioExpanded, darkHeaderColor, darkHeaderBorderColor, () => {
                 DrawOptionGrid(listing, AspectRatios, selectedAspectRatio, (val) => selectedAspectRatio = val);
            });

            listing.Gap(20f); // Extra space at bottom of scroll content

            viewHeight = listing.CurHeight; // Update view height for next frame
            listing.End();
            Widgets.EndScrollView();

            // --- Fixed Bottom Buttons ---
            float yPos = inRect.yMax - bottomAreaHeight + btnGap;
            
            Rect btnRectGenerate = new Rect(inRect.x, yPos, inRect.width, btnHeight);
            if (Widgets.ButtonText(btnRectGenerate, "RimPortrait_Command_Generate".Translate()))
            {
                Generate();
            }

            yPos += btnHeight + btnGap;
            Rect btnRectClose = new Rect(inRect.x, yPos, inRect.width, btnHeight);
            if (Widgets.ButtonText(btnRectClose, "CloseButton".Translate()))
            {
                Close();
            }
        }

        private void DrawAccordionSection(Listing_Standard listing, string label, string currentVal, ref bool expanded, Color bgColor, Color borderColor, Action drawContents, bool disabled = false)
        {
            // If disabled, collapse
            if (disabled && expanded) expanded = false;

            string headerText = $"{label}: {currentVal} {(expanded ? "▲" : "▼")}";
            Rect headerRect = listing.GetRect(30f);
            
            // Gray out if disabled
            if (disabled)
            {
                GUI.color = Color.gray;
                Widgets.DrawBoxSolidWithOutline(headerRect, bgColor * 0.7f, borderColor * 0.7f);
            }
            else
            {
                Widgets.DrawBoxSolidWithOutline(headerRect, bgColor, borderColor);
            }
            
            if (!disabled && Widgets.ButtonInvisible(headerRect))
            {
                expanded = !expanded;
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
            }
            
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = headerRect;
            labelRect.xMin += 10f;
            Widgets.Label(labelRect, headerText);
            Text.Anchor = TextAnchor.UpperLeft;
            
            if (disabled) GUI.color = Color.white;

            if (expanded)
            {
                listing.Gap(5f);
                drawContents();
                listing.Gap(10f);
            }
            else
            {
                listing.Gap(5f); // Small gap even if closed
            }
        }

        private void DrawOptionGrid(Listing_Standard listing, string[] options, string currentVal, Action<string> onSelect)
        {
            float rowHeight = 30f;
            int cols = 4; // Adjust columns based on space? 4 is good for small items.
            int rows = Mathf.CeilToInt((float)options.Length / cols);
            
            Rect gridRect = listing.GetRect(rows * rowHeight);
            float colWidth = gridRect.width / cols;

            for (int i = 0; i < options.Length; i++)
            {
                int r = i / cols;
                int c = i % cols;
                Rect itemRect = new Rect(gridRect.x + (c * colWidth), gridRect.y + (r * rowHeight), colWidth - 4f, rowHeight - 4f);
                
                bool isSelected = options[i] == currentVal;
                Widgets.DrawOptionBackground(itemRect, isSelected);

                if (Widgets.ButtonInvisible(itemRect))
                {
                    onSelect(options[i]);
                    SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                }

                Text.Anchor = TextAnchor.MiddleLeft;
                Rect txtRect = itemRect;
                txtRect.xMin += 10f;
                // Highlight color improvement
                if (isSelected) GUI.color = Color.cyan;
                Widgets.Label(txtRect, options[i]);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void Generate()
        {
            string finalPrompt = prompt;
            string pawnBase64 = null;
            string styleBase64 = null;

            // Capture Pawn Appearance
            if (usePawnAppearance)
            {
                 RenderTexture rt = PortraitsCache.Get(pawn, new Vector2(1024f, 1024f), Rot4.South, new Vector3(0f, 0f, 0.1f), 1f);
                 pawnBase64 = PortraitUtils.RenderTextureToBase64(rt);
            }

            // Handle Style Image Local
            if (!string.IsNullOrEmpty(selectedStyleImagePath) && File.Exists(selectedStyleImagePath))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(selectedStyleImagePath);
                    styleBase64 = System.Convert.ToBase64String(bytes);
                    
                    // Absolute Style Reference Logic
                    // 1. Remove text-based style if present to avoid conflict
                    string suffix = " art style";
                    foreach (string style in ArtStyles)
                    {
                        string search = $", {style}{suffix}";
                        int idx = finalPrompt.IndexOf(search);
                        if (idx != -1) finalPrompt = finalPrompt.Remove(idx, search.Length);
                    }
                    
                    // 2. Append Strong Instruction
                    // Note: Gemini receiving order is [Text, PawnImage (optional), StyleImage (optional)]
                    // If pawnBase64 is present, StyleImage is the 2nd image.
                    // If pawnBase64 is null, StyleImage is the 1st image.
                    
                    string imageRefText = !string.IsNullOrEmpty(pawnBase64) ? "second image" : "provided image";
                    finalPrompt += $". STRONGLY FOLLOW THE STYLE OF THE {imageRefText.ToUpper()}. Replicate the art style of the {imageRefText} exactly, ignoring any other style descriptions.";
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[RimPortrait] Failed to load style image: {ex.Message}");
                }
            }
            
            Log.Message($"[RimPortrait] Starting generation with prompt: {finalPrompt} | Ratio: {selectedAspectRatio}");
            
            AIClient.GeneratePortrait(finalPrompt, HandleUrlReceived, selectedAspectRatio, pawnBase64, styleBase64);
            
            Close();
        }


        

        
        private void HandleUrlReceived(string url)
        {
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
                
                string search = $", {comp}";
                int idx = prompt.IndexOf(search);
                if (idx != -1)
                {
                     prompt = prompt.Remove(idx, search.Length);
                }
            }
            
            // Append
            prompt += $", {newComp}";
        }

        private void UpdatePromptStyle(string newStyle)
        {
            string suffix = " art style";
            
            // Remove any known style
            foreach (string style in ArtStyles)
            {
                if (style == "Default") continue; 
                
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
