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

        public static float SpeedFactorCalculator(float currentSpeed, Pawn pawn)
        {

            if (pawn == null || pawn.IsAnimal)
                return currentSpeed / 60f;

            if (pawn.Downed && pawn.health.CanCrawl)
                return currentSpeed / 60f;

            if (BetterMovementMod.settings!.combatOverride && pawn.mindState != null && !pawn.mindState.CombatantRecently)
                return currentSpeed / 60f;

            float normalSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);
            float vanillaSpeedFactor = currentSpeed / normalSpeed;
            float desiredSpeedFactor = 1f;

            float minimumLoad = BetterMovementMod.settings!.minimumLoad;
            float bodyMass = pawn.BodySize * 70f;
            float movementCapacity = bodyMass * 0.30f;
            float carriedMass = MassUtility.GearMass(pawn) + MassUtility.InventoryMass(pawn);
            float encumbrance = carriedMass / movementCapacity;

            if (carriedMass <= minimumLoad)
            {
                // No penalty
            }
            else if (encumbrance <= 1f)
            {
                float minimumEncumbrance = minimumLoad / movementCapacity;
                float normalized = Mathf.InverseLerp(minimumEncumbrance, 1f, encumbrance);

                desiredSpeedFactor = 1f - (0.5f * Mathf.Pow(normalized, 1.5f));
            }
            else
            {
                float excess = encumbrance - 1f;
                desiredSpeedFactor = 0.15f + 0.35f * Mathf.Exp(-2.0f * excess);
            }

            desiredSpeedFactor = Mathf.Max(desiredSpeedFactor, 0.15f);

            float finalSpeedFactor = Mathf.Min(vanillaSpeedFactor, desiredSpeedFactor);

            Log.Message(
                $"[BetterMovement] Pawn={pawn.LabelShort} " +
                $"Current={currentSpeed:F2} " +
                $"Normal={normalSpeed:F2} " +
                $"VanillaFactor={vanillaSpeedFactor:F3} " +
                $"Encumbrance={encumbrance:P1} " +
                $"DesiredFactor={desiredSpeedFactor:F3} " +
                $"FinalFactor={finalSpeedFactor:F3}");

            return (normalSpeed * finalSpeedFactor) / 60f;
        }
    }
}
