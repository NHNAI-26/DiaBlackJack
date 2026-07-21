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

            bool includeElite = _eliteOfferChancePercent > 0 &&
                _random.Next(100) < _eliteOfferChancePercent;
            OpponentSelectionCandidate firstCandidate;
            OpponentSelectionCandidate secondCandidate;

            if (includeElite)
            {
                EnemyProfilePreview normal = SelectRandom(_normalPreviews);
                EnemyProfilePreview elite = SelectRandom(_elitePreviews);
                bool normalFirst = _random.Next(2) == 0;
                firstCandidate = CreateCandidate(normalFirst ? normal : elite);
                secondCandidate = CreateCandidate(normalFirst ? elite : normal);
            }
            else
            {
                int firstIndex = _random.Next(_normalPreviews.Count);
                int secondIndex = _random.Next(_normalPreviews.Count - 1);
                if (secondIndex >= firstIndex)
                {
                    secondIndex++;
                }

                firstCandidate = CreateCandidate(_normalPreviews[firstIndex]);
                secondCandidate = CreateCandidate(_normalPreviews[secondIndex]);
            }

            var offer = new OpponentSelectionOffer(
                _nextOfferId,
                stageIndex,
                new[] { firstCandidate, secondCandidate });
            _nextOfferId++;
            return offer;
        }

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

        private static OpponentSelectionCandidate CreateCandidate(
            EnemyProfilePreview preview)
        {
            return new OpponentSelectionCandidate(preview.ProfileKey, preview);
        }
    }
}
