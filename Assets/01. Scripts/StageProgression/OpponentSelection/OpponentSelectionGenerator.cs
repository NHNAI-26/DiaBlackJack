using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class OpponentSelectionGenerator
    {
        public const int DefaultEliteOfferChancePercent = 35;

        private readonly EnemyCombatProfileCatalog _catalog;
        private readonly int _eliteOfferChancePercent;
        private readonly ReadOnlyCollection<EnemyProfilePreview> _elitePreviews;
        private readonly ReadOnlyCollection<EnemyProfilePreview> _normalPreviews;
        private readonly Random _random;
        private readonly int _seed;
        private readonly HashSet<string> _previousOfferProfileKeys =
            new HashSet<string>(StringComparer.Ordinal);
        private int _nextOfferId;

        public OpponentSelectionGenerator(
            EnemyCombatProfileCatalog catalog,
            int seed,
            int eliteOfferChancePercent = DefaultEliteOfferChancePercent)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (eliteOfferChancePercent < 0 || eliteOfferChancePercent > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(eliteOfferChancePercent),
                    "Elite offer chance must be between zero and one hundred.");
            }

            var normalPreviews = new List<EnemyProfilePreview>();
            var elitePreviews = new List<EnemyProfilePreview>();
            for (int index = 0; index < catalog.Previews.Count; index++)
            {
                EnemyProfilePreview preview = catalog.Previews[index];
                switch (preview.Grade)
                {
                    case EnemyGrade.Normal:
                        normalPreviews.Add(preview);
                        break;
                    case EnemyGrade.Elite:
                        elitePreviews.Add(preview);
                        break;
                    case EnemyGrade.Boss:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(catalog),
                            "Enemy profile catalog contains an invalid grade.");
                }
            }

            if (normalPreviews.Count < 2)
            {
                throw new ArgumentException(
                    "Opponent selection requires at least two normal profiles.",
                    nameof(catalog));
            }

            if (eliteOfferChancePercent > 0 && elitePreviews.Count == 0)
            {
                throw new ArgumentException(
                    "A positive elite offer chance requires at least one elite profile.",
                    nameof(catalog));
            }

            _seed = seed;
            _eliteOfferChancePercent = eliteOfferChancePercent;
            _normalPreviews = normalPreviews.AsReadOnly();
            _elitePreviews = elitePreviews.AsReadOnly();
            _random = new Random(seed);
        }

        public OpponentSelectionOffer Generate(int stageIndex)
        {
            if (stageIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stageIndex),
                    "Opponent offer stage index cannot be negative.");
            }

            if (_nextOfferId == int.MaxValue)
            {
                throw new InvalidOperationException("Opponent offer ids are exhausted.");
            }

            List<EnemyProfilePreview> eligibleNormals = FilterEligible(
                _normalPreviews);
            List<EnemyProfilePreview> eligibleElites = FilterEligible(
                _elitePreviews);
            bool isFirstOffer = _nextOfferId == 0;
            if (!isFirstOffer &&
                eligibleNormals.Count < 2 &&
                (eligibleNormals.Count == 0 || eligibleElites.Count == 0))
            {
                // Small injected catalogs used by isolated tests and development tools
                // may not contain enough distinct profiles to form another legal offer.
                // Production has enough profiles to keep the previous-offer exclusion;
                // only relax it when a two-candidate offer would otherwise be impossible.
                eligibleNormals = new List<EnemyProfilePreview>(_normalPreviews);
                eligibleElites = new List<EnemyProfilePreview>(_elitePreviews);
            }

            bool includeElite = !isFirstOffer &&
                _eliteOfferChancePercent > 0 &&
                _random.Next(100) < _eliteOfferChancePercent;
            OpponentSelectionCandidate firstCandidate;
            OpponentSelectionCandidate secondCandidate;

            if (includeElite &&
                eligibleNormals.Count > 0 &&
                eligibleElites.Count > 0)
            {
                EnemyProfilePreview normal = SelectRandom(eligibleNormals);
                EnemyProfilePreview elite = SelectRandom(eligibleElites);
                bool normalFirst = _random.Next(2) == 0;
                firstCandidate = CreateCandidate(normalFirst ? normal : elite);
                secondCandidate = CreateCandidate(normalFirst ? elite : normal);
            }
            else if (eligibleNormals.Count >= 2)
            {
                int firstIndex = _random.Next(eligibleNormals.Count);
                int secondIndex = _random.Next(eligibleNormals.Count - 1);
                if (secondIndex >= firstIndex)
                {
                    secondIndex++;
                }

                firstCandidate = CreateCandidate(eligibleNormals[firstIndex]);
                secondCandidate = CreateCandidate(eligibleNormals[secondIndex]);
            }
            else if (!isFirstOffer &&
                eligibleNormals.Count == 1 &&
                eligibleElites.Count > 0)
            {
                EnemyProfilePreview normal = eligibleNormals[0];
                EnemyProfilePreview elite = SelectRandom(eligibleElites);
                bool normalFirst = _random.Next(2) == 0;
                firstCandidate = CreateCandidate(normalFirst ? normal : elite);
                secondCandidate = CreateCandidate(normalFirst ? elite : normal);
            }
            else
            {
                throw new InvalidOperationException(
                    "Opponent selection has too few eligible profiles after excluding the previous offer.");
            }

            var offer = new OpponentSelectionOffer(
                _nextOfferId,
                stageIndex,
                new[] { firstCandidate, secondCandidate });
            _previousOfferProfileKeys.Clear();
            _previousOfferProfileKeys.Add(firstCandidate.ProfileKey);
            _previousOfferProfileKeys.Add(secondCandidate.ProfileKey);
            _nextOfferId++;
            return offer;
        }

        internal int NextOfferOrdinal => _nextOfferId;

        internal OpponentSelectionGenerator CreateFresh()
        {
            return new OpponentSelectionGenerator(
                _catalog,
                _seed,
                _eliteOfferChancePercent);
        }

        private EnemyProfilePreview SelectRandom(
            IReadOnlyList<EnemyProfilePreview> previews)
        {
            return previews[_random.Next(previews.Count)];
        }

        private List<EnemyProfilePreview> FilterEligible(
            IReadOnlyList<EnemyProfilePreview> previews)
        {
            var eligible = new List<EnemyProfilePreview>(previews.Count);
            for (int index = 0; index < previews.Count; index++)
            {
                EnemyProfilePreview preview = previews[index];
                if (!_previousOfferProfileKeys.Contains(preview.ProfileKey))
                {
                    eligible.Add(preview);
                }
            }

            return eligible;
        }

        private static OpponentSelectionCandidate CreateCandidate(
            EnemyProfilePreview preview)
        {
            return new OpponentSelectionCandidate(preview.ProfileKey, preview);
        }
    }
}
