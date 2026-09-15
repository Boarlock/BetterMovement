using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BetterMovement
{
    public class BetterMovementMod : Mod
    {
        public BetterMovementMod(ModContentPack content) : base(content)
        {

            settings = GetSettings<BetterMovementSettings>();

            var harmony = new Harmony("b0arl0ck.bettermovement");
            harmony.PatchAll();

            Log.Message($"[BetterMovement] Initialization completed.");
        }

        public static BetterMovementSettings? settings;
        public override string SettingsCategory() => "Better Movement";
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Gap(8f);

            listing.CheckboxLabeled("Turn off Better Movement outside of combat", ref settings!.combatOverride);

            listing.Gap(12f);

            if (listing.ButtonTextLabeled("Minimum load before movement penalty", $"{settings!.minimumLoad:F1} kg"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("9.1 kg", () => settings!.minimumLoad = 9.1f),

                    new FloatMenuOption("13.6 kg", () => settings!.minimumLoad = 13.6f),

                    new FloatMenuOption("18.1 kg", () => settings!.minimumLoad = 18.1f)
                };

                Find.WindowStack.Add(new FloatMenu(options));
            }

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }

    public class BetterMovementSettings : ModSettings
    {
        // Enable or disable combat override.
        public bool combatOverride = false;
        public float minimumLoad = 13.6f;

        // Persist the override setting across game sessions.
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref combatOverride, "subOverrideDuration", false);
            Scribe_Values.Look(ref minimumLoad, "minimumLoad", 13.6f);
        }
    }
}
