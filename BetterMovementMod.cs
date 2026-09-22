using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace BetterMovement
{
    public class BetterMovementMod : Mod
    {
        public static bool VehicleFrameworkActive { get; private set; }
        public BetterMovementMod(ModContentPack content) : base(content)
        {
            VehicleFrameworkActive = LoadedModManager.RunningModsListForReading.Any(mod => mod.PackageIdPlayerFacing == "SmashPhil.VehicleFramework");

            if (!VehicleFrameworkActive)
            {
                var harmony = new Harmony("b0arl0ck.bettermovement");
                harmony.PatchAll();
            }
            else
            {
                PatchWithVehicleFrameworkHarmony();
            }

            Log.Message("[BetterMovement] Initialization completed.");
        }

        private static void PatchWithVehicleFrameworkHarmony()
        {
            var vfPatcherType = AccessTools.TypeByName("SmashTools.Patching.HarmonyPatcher");
            var harmonyProperty = vfPatcherType.GetProperty("Harmony", BindingFlags.Static | BindingFlags.NonPublic);
            var vfHarmonyObject = harmonyProperty.GetValue(null);

            var vfHarmonyType = vfHarmonyObject.GetType();
            var vfHarmonyMethodType = vfHarmonyType.Assembly.GetType("HarmonyLib.HarmonyMethod");

            var ticksPerMove = AccessTools.Method(typeof(Pawn), "TicksPerMove", new[] { typeof(bool) });

            var transpilerMethod = typeof(TicksPerMovePatch).GetMethod(nameof(TicksPerMovePatch.Transpiler), BindingFlags.Public | BindingFlags.Static);
            var vfTranspiler = Activator.CreateInstance(vfHarmonyMethodType, transpilerMethod);

            var patchMethod = vfHarmonyType.GetMethod("Patch", new[]
            {
                    typeof(MethodBase),
                    vfHarmonyMethodType,
                    vfHarmonyMethodType,
                    vfHarmonyMethodType,
                    vfHarmonyMethodType
            });

            patchMethod.Invoke(vfHarmonyObject, new object[]
            {
                    ticksPerMove,
                    null!,
                    null!,
                    vfTranspiler,
                    null!
            });
        
        }

        public override string SettingsCategory() => "Better Movement";
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();

            listing.Begin(inRect);

            listing.Gap(8f);

            listing.CheckboxLabeled("Turn off alerts for Better Movement", ref BetterMovementSettings.alertOverride);

            listing.Gap(8f);

            listing.CheckboxLabeled("Turn off Better Movement outside of combat", ref BetterMovementSettings.combatOverride);

            listing.Gap(12f);

            if (listing.ButtonTextLabeled("Minimum load before movement penalty", $"{BetterMovementSettings.minimumLoad:F1} kg"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("9.1 kg", () => BetterMovementSettings.minimumLoad = 9.1f),

                    new FloatMenuOption("13.6 kg", () => BetterMovementSettings.minimumLoad = 13.6f),

                    new FloatMenuOption("18.1 kg", () => BetterMovementSettings.minimumLoad = 18.1f)
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

        public static bool alertOverride = false;
        public static bool combatOverride = false;
        public static float minimumLoad = 13.6f;

        // Persist settings across game sessions.
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref alertOverride, "alertOverride", false);
            Scribe_Values.Look(ref combatOverride, "combatOverride", false);
            Scribe_Values.Look(ref minimumLoad, "minimumLoad", 13.6f);
        }
    }
}
