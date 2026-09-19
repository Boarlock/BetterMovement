using HarmonyLib;
using UnityEngine;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace BetterMovement
{
    [HarmonyPatch(typeof(Pawn), "TicksPerMove", new[] { typeof(bool) })]
    public static class TicksPerMovePatch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 1; i < codes.Count - 2; i++)
            {
                if (codes[i - 1].opcode == OpCodes.Ldloc_0 && codes[i].opcode == OpCodes.Ldc_R4 && codes[i + 1].opcode == OpCodes.Div && codes[i + 2].opcode == OpCodes.Stloc_1)
                {
                    codes[i] = CodeInstruction.LoadArgument(0);
                    codes[i + 1] = CodeInstruction.Call(typeof(TicksPerMovePatch), nameof(SpeedFactorCalculator));

                    break;
                }
            }
            return codes;
        }

        public static readonly HashSet<Pawn> OverencumberedPawns = new HashSet<Pawn>();

        public static float SpeedFactorCalculator(float currentSpeed, Pawn pawn)
        {

            if (pawn == null)
                return currentSpeed / 60f;

            if (pawn.IsAnimal)
            {
                OverencumberedPawns.Remove(pawn);
                return currentSpeed / 60f;
            }

            if (pawn.Downed && pawn.health.CanCrawl)
            {
                OverencumberedPawns.Remove(pawn);
                return currentSpeed / 60f;
            }

            if (BetterMovementSettings.combatOverride && pawn.mindState != null && !pawn.mindState.CombatantRecently)
            {
                OverencumberedPawns.Remove(pawn);
                return currentSpeed / 60f;
            }

            float normalSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);
            float vanillaSpeedFactor = currentSpeed / normalSpeed;
            float desiredSpeedFactor = 1f;

            float minimumLoad = BetterMovementSettings.minimumLoad;
            float bodyMass = pawn.BodySize * 70f;
            float movementCapacity = bodyMass * 0.30f;
            float carriedMass = MassUtility.GearMass(pawn) + MassUtility.InventoryMass(pawn) + CarriedThingMass(pawn);
            float encumbrance = carriedMass / movementCapacity;

            if (carriedMass <= minimumLoad)
            {
                OverencumberedPawns.Remove(pawn);
            }
            else if (encumbrance <= 1f)
            {
                float minimumEncumbrance = minimumLoad / movementCapacity;
                float normalized = Mathf.InverseLerp(minimumEncumbrance, 1f, encumbrance);

                desiredSpeedFactor = 1f - (0.5f * Mathf.Pow(normalized, 1.5f));
                OverencumberedPawns.Remove(pawn);
            }
            else
            {
                float excess = encumbrance - 1f;
                desiredSpeedFactor = 0.15f + 0.35f * Mathf.Exp(-2.0f * excess);

                OverencumberedPawns.Add(pawn);
            }

            desiredSpeedFactor = Mathf.Max(desiredSpeedFactor, 0.15f);

            float finalSpeedFactor = Mathf.Min(vanillaSpeedFactor, desiredSpeedFactor);

            return (normalSpeed * finalSpeedFactor) / 60f;
        }

        private static float CarriedThingMass(Pawn pawn)
        {
            Thing carriedThing = pawn.carryTracker.CarriedThing;

            if (carriedThing == null)
                return 0f;

            return carriedThing.stackCount * carriedThing.GetStatValue(StatDefOf.Mass);
        }
    }
}
