using RimWorld;
using Verse;

namespace BetterMovement
{
    public class BetterMovementAlert : Alert
    {
        public BetterMovementAlert()
        {
            defaultLabel = $"Overencumbered pawn";
            defaultPriority = AlertPriority.Medium;
        }

        // This method controls when the alert actually shows up.
        public override AlertReport GetReport()
        {

            if (BetterMovementSettings.alertOverride)
                return AlertReport.Inactive;

            Map currentMap = Find.CurrentMap;

            if (currentMap == null)
                return AlertReport.Inactive;

            foreach (Pawn pawn in currentMap.mapPawns.FreeColonistsSpawned)
            {
                if (TicksPerMovePatch.OverencumberedPawns.Contains(pawn))
                {
                    // Returning a specific pawn and centers the camera on them when clicked.
                    return AlertReport.CulpritIs(pawn);
                }
            }
            
            return AlertReport.Inactive;
        }

        public override TaggedString GetExplanation()
        {
            return $"A colonist is carrying too much weight and is overencumbered.";
        }
    }
}