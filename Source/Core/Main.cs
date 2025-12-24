using Verse;
using UnityEngine;
using HarmonyLib;

namespace RimPortrait
{
    [StaticConstructorOnStartup]
    public static class Main
    {
        public static MonoBehaviour CoroutineRunner;

        static Main()
        {
            Log.Message("RimIllustrator Loaded");
            
            // Harmony Initialization
            var harmony = new Harmony("com.rimportrait.ai");
            harmony.PatchAll();

            GameObject go = new GameObject("RimPortrait_CoroutineRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            CoroutineRunner = go.AddComponent<PortraitHelper>();
        }
    }

    public class PortraitHelper : MonoBehaviour { }
}
