using UnityEngine;
using Verse;

namespace RimPortrait
{
    public enum ServiceType
    {
        OpenAI,
        GoogleAI
    }

    public enum GeminiModel
    {
        NanoBanana, // gemini-2.5-flash-image
        NanoBananaPro // gemini-3-pro-image-preview
    }

    public class RimPortraitSettings : ModSettings
    {
        public string apiKey = "";
        public ServiceType serviceType = ServiceType.OpenAI;
        public GeminiModel geminiModel = GeminiModel.NanoBanana;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref apiKey, "apiKey");
            Scribe_Values.Look(ref serviceType, "serviceType", ServiceType.OpenAI);
            Scribe_Values.Look(ref geminiModel, "geminiModel", GeminiModel.NanoBanana);
            base.ExposeData();
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            
            listingStandard.Label("RimPortrait_AIService".Translate());
            if (listingStandard.RadioButton("RimPortrait_Service_OpenAI".Translate(), serviceType == ServiceType.OpenAI))
            {
                serviceType = ServiceType.OpenAI;
            }
            if (listingStandard.RadioButton("RimPortrait_Service_GoogleAI".Translate(), serviceType == ServiceType.GoogleAI))
            {
                serviceType = ServiceType.GoogleAI;
            }

            if (serviceType == ServiceType.GoogleAI)
            {
                listingStandard.Gap();
                listingStandard.Label("RimPortrait_GeminiModel".Translate());
                if (listingStandard.RadioButton("Gemini 2.5 Flash (Nano Banana)", geminiModel == GeminiModel.NanoBanana))
                {
                    geminiModel = GeminiModel.NanoBanana;
                }
                if (listingStandard.RadioButton("Gemini 3 Pro (Nano Banana Pro)", geminiModel == GeminiModel.NanoBananaPro))
                {
                    geminiModel = GeminiModel.NanoBananaPro;
                }
            }
            
            listingStandard.Gap();
            
            listingStandard.Label("RimPortrait_ApiKey".Translate());
            apiKey = listingStandard.TextEntry(apiKey);
            
            listingStandard.End();
        }
    }
}
