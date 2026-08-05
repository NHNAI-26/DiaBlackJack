using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.GameScene
{
    internal enum SpeechPriority
    {
        RoundStart = 100,
        BattleStart = 200,
        Action = 300,
        Damage = 400,
        LowSoul = 500,
        Terminal = 600,
    }

    internal static class SpeechCueKeys
    {
        public const string BattleStart = "combat.battle_start";
        public const string RoundStart = "combat.round_start";
        public const string ActionHit = "combat.action.hit";
        public const string ActionStand = "combat.action.stand";
        public const string ActionChange = "combat.action.change";
        public const string ActionUseCard = "combat.action.use_card";
        public const string ActionDemonContract =
            "combat.action.demon_contract";
        public const string ActionRevolverBefore =
            "combat.action.revolver.before";
        public const string ActionRevolverHit =
            "combat.action.revolver.hit";
        public const string ActionRevolverMiss =
            "combat.action.revolver.miss";
        public const string ActionKnifeBefore =
            "combat.action.knife.before";
        public const string ActionKnifeBust =
            "combat.action.knife.bust";
        public const string ActionKnifeNoBust =
            "combat.action.knife.no_bust";
        public const string ActionHammerBefore =
            "combat.action.hammer.before";
        public const string ActionHammerBust =
            "combat.action.hammer.bust";
        public const string ActionHammerNoBust =
            "combat.action.hammer.no_bust";
        public const string ReactionPlayerManualCard =
            "combat.reaction.player.manual_card";
        public const string ReactionPlayerDemonContract =
            "combat.reaction.player.demon_contract";
        public const string ReactionPlayerAutomaticCard =
            "combat.reaction.player.automatic_card";
        public const string DamageCard = "combat.damage.card";
        public const string DamageRound = "combat.damage.round";
        public const string DamageOther = "combat.damage.other";
        public const string LowSoul = "combat.low_soul";
        public const string Victory = "combat.victory";
        public const string Defeat = "combat.defeat";

        public const string ShopGreeting = "shop.greeting";
        public const string ShopPurchaseSuccess = "shop.purchase_success";
        public const string ShopInsufficientGold = "shop.insufficient_gold";
        public const string ShopSoldOut = "shop.sold_out";
        public const string ShopUnavailable = "shop.unavailable";
        public const string ShopLighterSuccess = "shop.lighter_success";
        public const string ShopWhiskeySuccess = "shop.whiskey_success";
        public const string ShopFarewell = "shop.farewell";

        private static readonly IReadOnlyList<string> EnemyKeys =
            Array.AsReadOnly(new[]
            {
                BattleStart,
                RoundStart,
                ActionHit,
                ActionStand,
                ActionChange,
                ActionUseCard,
                ActionDemonContract,
                ReactionPlayerManualCard,
                ReactionPlayerDemonContract,
                ReactionPlayerAutomaticCard,
                DamageCard,
                DamageRound,
                DamageOther,
                LowSoul,
                Victory,
                Defeat,
            });

        private static readonly IReadOnlyList<string> ShopKeys =
            Array.AsReadOnly(new[]
            {
                ShopGreeting,
                ShopPurchaseSuccess,
                ShopInsufficientGold,
                ShopSoldOut,
                ShopUnavailable,
                ShopLighterSuccess,
                ShopWhiskeySuccess,
                ShopFarewell,
            });

        public static IReadOnlyList<string> RequiredEnemyKeys => EnemyKeys;

        public static IReadOnlyList<string> RequiredShopKeys => ShopKeys;

        public static string GetAutomaticCardAction(string definitionKey)
        {
            return "combat.action.automatic." + definitionKey;
        }

        public static string GetDemonContractAction(string definitionKey)
        {
            return "combat.action.demon_contract." + definitionKey;
        }

        internal static IReadOnlyList<string> GetRequiredEnemyKeys(
            EnemyCombatProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var keys = new List<string>(EnemyKeys);
            var unique = new HashSet<string>(keys, StringComparer.Ordinal);
            bool hasRevolver = false;
            bool hasKnife = false;
            bool hasHammer = false;

            foreach (string definitionKey in profile.DeckDefinitionKeys)
            {
                CardDefinition definition =
                    CardDefinitionCatalog.GetByKey(definitionKey);
                switch (definition.Effect)
                {
                    case CardEffectKind.AutoPistol:
                        hasRevolver = true;
                        break;
                    case CardEffectKind.MilitaryKnife:
                        hasKnife = true;
                        break;
                    case CardEffectKind.ThreatHammer:
                        hasHammer = true;
                        break;
                    case CardEffectKind.Poison:
                    case CardEffectKind.LieDetector:
                    case CardEffectKind.Flamethrower:
                    case CardEffectKind.PocketWatch:
                        AddUnique(
                            GetAutomaticCardAction(definitionKey),
                            keys,
                            unique);
                        break;
                }
            }

            if (hasRevolver)
            {
                AddManualResultKeys(
                    ActionRevolverBefore,
                    ActionRevolverHit,
                    ActionRevolverMiss,
                    keys,
                    unique);
            }

            if (hasKnife)
            {
                AddManualResultKeys(
                    ActionKnifeBefore,
                    ActionKnifeBust,
                    ActionKnifeNoBust,
                    keys,
                    unique);
            }

            if (hasHammer)
            {
                AddManualResultKeys(
                    ActionHammerBefore,
                    ActionHammerBust,
                    ActionHammerNoBust,
                    keys,
                    unique);
            }

            foreach (string definitionKey in
                profile.DemonContractDefinitionKeys)
            {
                switch (definitionKey)
                {
                    case DemonContractCatalog.BelphegorKey:
                    case DemonContractCatalog.BeelzebubKey:
                    case DemonContractCatalog.MammonKey:
                    case DemonContractCatalog.AsmodeusKey:
                    case DemonContractCatalog.SatanKey:
                    case DemonContractCatalog.AzazelKey:
                        AddUnique(
                            GetDemonContractAction(definitionKey),
                            keys,
                            unique);
                        break;
                }
            }

            return keys.AsReadOnly();
        }

        private static void AddManualResultKeys(
            string before,
            string success,
            string failure,
            List<string> keys,
            HashSet<string> unique)
        {
            AddUnique(before, keys, unique);
            AddUnique(success, keys, unique);
            AddUnique(failure, keys, unique);
        }

        private static void AddUnique(
            string key,
            List<string> keys,
            HashSet<string> unique)
        {
            if (unique.Add(key))
            {
                keys.Add(key);
            }
        }
    }
}
