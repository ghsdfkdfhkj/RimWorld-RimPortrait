using UnityEngine;
using Verse;

namespace RimPortrait
{
    public class RimPortraitMod : Mod
    {
        public static RimPortraitSettings settings;

        public RimPortraitMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<RimPortraitSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "RimPortrait";
        }
    }
}
