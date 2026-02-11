using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace RimPortrait
{
    public class RimPortraitDataStore : WorldComponent
    {
        // Dictionary<PawnID(string), Filename>
        private Dictionary<string, string> pawnPortraitPaths = new Dictionary<string, string>();
        private List<string> tmpKeys;
        private List<string> tmpValues;

        public RimPortraitDataStore(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pawnPortraitPaths, "pawnPortraitPaths", LookMode.Value, LookMode.Value, ref tmpKeys, ref tmpValues);
        }

        public string GetPortraitPath(string pawnId)
        {
            if (pawnPortraitPaths.TryGetValue(pawnId, out string path))
            {
                return path;
            }
            return null;
        }

        public void SetPortraitPath(string pawnId, string path)
        {
            if (pawnPortraitPaths.ContainsKey(pawnId))
            {
                pawnPortraitPaths[pawnId] = path;
            }
            else
            {
                pawnPortraitPaths.Add(pawnId, path);
            }
        }
    }
}
