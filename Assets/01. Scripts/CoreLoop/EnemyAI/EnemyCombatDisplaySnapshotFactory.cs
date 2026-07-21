using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal static class EnemyCombatDisplaySnapshotFactory
    {
        private static readonly IReadOnlyList<EnemyInferenceDisplayEntry> NoInferences =
            Array.AsReadOnly(Array.Empty<EnemyInferenceDisplayEntry>());

        public static EnemyCombatDisplaySnapshot Create(
            CoreLoopBattle battle,
            string profileKey)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (string.IsNullOrEmpty(profileKey))
            {
                return CreateUnavailable();
            }

            EnemyCombatProfile profile =
                EnemyCombatProfileCatalog.Default.GetByKey(profileKey);
            switch (profile.Grade)
            {
                case EnemyGrade.Normal:
                    return CreateNormal(battle, profile);
                case EnemyGrade.Elite:
                    return CreateElite(battle, profile);
                case EnemyGrade.Boss:
                    return CreateBoss(battle, profile);
                default:
                    throw new ArgumentOutOfRangeException(nameof(profile.Grade));
            }
        }

        private static EnemyCombatDisplaySnapshot CreateUnavailable()
        {
            return new EnemyCombatDisplaySnapshot(
                null,
                "UNPROFILED ENEMY",
                null,
                "PROFILE DATA UNAVAILABLE",
                null,
                NoInferences,
                null,
                null,
                null,
                null);
        }

        private static EnemyCombatDisplaySnapshot CreateNormal(
            CoreLoopBattle battle,
            EnemyCombatProfile profile)
        {
            IReadOnlyList<EnemyNumberInference> inferences =
                EnemyObservationFactory.CreateNumberInferences(battle);
            var entries = new List<EnemyInferenceDisplayEntry>(
                Math.Min(3, inferences.Count));
            for (int i = 0; i < inferences.Count && i < 3; i++)
            {
                EnemyNumberInference inference = inferences[i];
                entries.Add(new EnemyInferenceDisplayEntry(
                    inference.Number,
                    inference.ProbabilityPercent));
            }

            return CreateProfileSnapshot(
                profile,
                entries,
                null,
                null,
                null,
                null);
        }

        private static EnemyCombatDisplaySnapshot CreateElite(
            CoreLoopBattle battle,
            EnemyCombatProfile profile)
        {
            EnemyInferenceDisplayModel inference =
                EnemyInferenceDisplayModel.CreateForElite(
                    EnemyObservationFactory.CreateNumberInferences(battle));
            var entries = new List<EnemyInferenceDisplayEntry>(
                inference.TopNumbers.Count);
            foreach (int number in inference.TopNumbers)
            {
                entries.Add(new EnemyInferenceDisplayEntry(number, null));
            }

            return CreateProfileSnapshot(
                profile,
                entries,
                inference.Confidence,
                null,
                null,
                null);
        }

        private static EnemyCombatDisplaySnapshot CreateBoss(
            CoreLoopBattle battle,
            EnemyCombatProfile profile)
        {
            BossCombatDisplayModel display = null;
            if (battle.EnemyBehaviorPolicy is FinalBossEnemyPolicy bossPolicy)
            {
                display = bossPolicy.CurrentDisplay;
            }

            if (display == null)
            {
                display = BossCombatDisplayModel.Create(
                    EnemyObservationFactory.Create(battle, decisionSeed: 0),
                    BossTelegraphedAction.None);
            }

            return CreateProfileSnapshot(
                profile,
                NoInferences,
                display.Confidence,
                display.Phase,
                display.InferenceDirection,
                display.TelegraphedAction);
        }

        private static EnemyCombatDisplaySnapshot CreateProfileSnapshot(
            EnemyCombatProfile profile,
            IEnumerable<EnemyInferenceDisplayEntry> inferenceEntries,
            EnemyInferenceConfidence? confidence,
            FinalBossPhase? bossPhase,
            BossInferenceDirection? bossInferenceDirection,
            BossTelegraphedAction? bossTelegraphedAction)
        {
            return new EnemyCombatDisplaySnapshot(
                profile.Key,
                profile.DisplayName,
                profile.Grade,
                profile.Summary,
                profile.PlayerInformationMode,
                inferenceEntries,
                confidence,
                bossPhase,
                bossInferenceDirection,
                bossTelegraphedAction);
        }
    }
}
