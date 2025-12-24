using UnityEngine;
using Verse;

namespace RimPortrait
{
    public class Dialog_PortraitViewer : Window
    {
        private Texture2D portraitTexture;
        private string pawnName;

        public override Vector2 InitialSize => new Vector2(550f, 600f);

        public Dialog_PortraitViewer(Texture2D texture, string pawnName)
        {
            this.portraitTexture = texture;
            this.pawnName = pawnName;
            this.forcePause = false; // Don't pause game
            this.draggable = true;
            this.resizeable = true;
            this.doCloseX = true;
            this.doCloseButton = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Title
            Rect titleRect = new Rect(0f, 0f, inRect.width, 30f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "RimPortrait_Viewer_Title".Translate(pawnName));
            Text.Font = GameFont.Small;

            // Image area
            Rect imageRect = new Rect(0f, 40f, inRect.width, inRect.height - 80f);
            
            if (portraitTexture != null)
            {
                // Maintain aspect ratio
                float aspect = (float)portraitTexture.width / portraitTexture.height;
                float viewAspect = imageRect.width / imageRect.height;

                Rect drawRect;
                if (aspect > viewAspect)
                {
                    // Width constrained
                    float h = imageRect.width / aspect;
                    drawRect = new Rect(imageRect.x, imageRect.y + (imageRect.height - h) / 2f, imageRect.width, h);
                }
                else
                {
                    // Height constrained
                    float w = imageRect.height * aspect;
                    drawRect = new Rect(imageRect.x + (imageRect.width - w) / 2f, imageRect.y, w, imageRect.height);
                }

                GUI.DrawTexture(drawRect, portraitTexture);
            }
            else
            {
                Widgets.Label(imageRect, "RimPortrait_Viewer_Loading".Translate());
            }
        }
    }
}
