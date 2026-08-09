using DiaBlackJack.CoreLoop;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEngine;

namespace DiaBlackJack.StageProgression.Tests
{
    [Category("GFR02")]
    public sealed class Gfr02RunResultTransitionTests
    {
        [Test]
        public void GFR02_U01_TerminalScreensResolveDistinctExitTransitions()
        {
            Assert.That(
                RunResultTransitionView.ResolveKind(
                    GameFlowScreen.RunDefeat),
                Is.EqualTo(RunResultExitTransitionKind.Defeat));
            Assert.That(
                RunResultTransitionView.ResolveKind(
                    GameFlowScreen.RunVictory),
                Is.EqualTo(RunResultExitTransitionKind.Victory));
            Assert.That(
                RunResultTransitionView.ResolveKind(
                    GameFlowScreen.Combat),
                Is.EqualTo(RunResultExitTransitionKind.None));
        }

        [Test]
        public void GFR02_U02_DefeatBurstReusesEightSoulLossUnits()
        {
            SoulLossRecord record =
                RunResultTransitionView.CreateDefeatSoulLossRecord();

            Assert.That(record.TargetSide, Is.EqualTo(CombatantSide.Player));
            Assert.That(
                record.LossAmount,
                Is.EqualTo(RunResultTransitionView.DefeatSoulTokenCount));
            Assert.That(record.SoulAfter, Is.Zero);
            Assert.That(record.Cause, Is.EqualTo(SoulLossCause.RoundDamage));
        }

        [Test]
        public void GFR02_U03_TransitionLocksReentryAndCompletesOnce()
        {
            GameObject root = new GameObject("GFR02 Transition Test");
            RunResultTransitionView view =
                root.AddComponent<RunResultTransitionView>();
            int completedCount = 0;
            try
            {
                Assert.That(
                    view.TryPlay(
                        GameFlowScreen.RunDefeat,
                        null,
                        () => completedCount++),
                    Is.True);
                Assert.That(view.IsPlaying, Is.True);
                Assert.That(
                    view.TryPlay(
                        GameFlowScreen.RunDefeat,
                        null,
                        () => completedCount++),
                    Is.False);

                view.DebugCompleteImmediately();
                view.DebugCompleteImmediately();

                Assert.That(completedCount, Is.EqualTo(1));
            }
            finally
            {
                view.CancelAndRestore();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GFR02_U04_VictoryCreatesGaussianBlurWithEyeClose()
        {
            GameObject root = new GameObject("GFR02 Victory Test");
            RunResultTransitionView view =
                root.AddComponent<RunResultTransitionView>();
            try
            {
                Assert.That(
                    view.TryPlay(
                        GameFlowScreen.RunVictory,
                        null,
                        () => { }),
                    Is.True);
                Assert.That(view.HasVictoryBlur, Is.True);
                Assert.That(view.HasVictoryEyelidFeather, Is.True);
                Assert.That(view.VictoryBlurVolumeLayerCount, Is.GreaterThan(0));
                Assert.That(view.VictoryBlurWeight, Is.Zero);
                Assert.That(
                    RunResultTransitionView.VictoryBlurSeconds,
                    Is.EqualTo(1.5f));
                Assert.That(
                    RunResultTransitionView.VictoryFirstEyeCloseSeconds,
                    Is.GreaterThan(0f));
            }
            finally
            {
                view.CancelAndRestore();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GFR02_U05_VictoryReopenShowsThirtyPercentGap()
        {
            float visibleGap =
                1f - RunResultTransitionView.VictoryEyeReopenScale;

            Assert.That(visibleGap, Is.EqualTo(0.3f).Within(0.0001f));
        }

        [Test]
        public void GFR02_U06_VictoryClosesReopensThenClosesAgain()
        {
            float firstCloseEnd =
                RunResultTransitionView.VictoryEyeCloseDelaySeconds +
                RunResultTransitionView.VictoryFirstEyeCloseSeconds;
            float reopenEnd = firstCloseEnd +
                RunResultTransitionView.VictoryEyeReopenSeconds;
            float finalCloseEnd = reopenEnd +
                RunResultTransitionView.VictoryFinalEyeCloseSeconds;

            Assert.That(
                RunResultTransitionView.VictoryEyeCloseDelaySeconds,
                Is.EqualTo(0.65f));
            Assert.That(
                RunResultTransitionView.VictoryEyeCloseDelaySeconds,
                Is.LessThan(RunResultTransitionView.VictoryBlurSeconds));
            Assert.That(firstCloseEnd, Is.EqualTo(1.5f));
            Assert.That(reopenEnd, Is.GreaterThan(firstCloseEnd));
            Assert.That(finalCloseEnd, Is.GreaterThan(reopenEnd));
        }

        [Test]
        public void GFR02_U07_VictoryEyelidFeatherFadesMonotonically()
        {
            float transparentEdge =
                RunResultTransitionView.EvaluateVictoryEyelidFeatherAlpha(0f);
            float quarter =
                RunResultTransitionView.EvaluateVictoryEyelidFeatherAlpha(0.25f);
            float middle =
                RunResultTransitionView.EvaluateVictoryEyelidFeatherAlpha(0.5f);
            float threeQuarters =
                RunResultTransitionView.EvaluateVictoryEyelidFeatherAlpha(0.75f);
            float opaqueEdge =
                RunResultTransitionView.EvaluateVictoryEyelidFeatherAlpha(1f);

            Assert.That(transparentEdge, Is.Zero);
            Assert.That(quarter, Is.GreaterThan(transparentEdge));
            Assert.That(middle, Is.GreaterThan(quarter));
            Assert.That(threeQuarters, Is.GreaterThan(middle));
            Assert.That(opaqueEdge, Is.EqualTo(1f));
        }

        [Test]
        public void GFR02_U08_CancelRestoresVictoryRuntimeResources()
        {
            GameObject root = new GameObject("GFR02 Cancel Test");
            RunResultTransitionView view =
                root.AddComponent<RunResultTransitionView>();
            try
            {
                Assert.That(
                    view.TryPlay(
                        GameFlowScreen.RunVictory,
                        null,
                        () => { }),
                    Is.True);
                Assert.That(view.HasVictoryBlur, Is.True);
                Assert.That(view.HasVictoryEyelidFeather, Is.True);

                view.CancelAndRestore();

                Assert.That(view.IsPlaying, Is.False);
                Assert.That(view.HasVictoryBlur, Is.False);
                Assert.That(view.HasVictoryEyelidFeather, Is.False);
                Assert.That(view.VictoryBlurVolumeLayerCount, Is.Zero);
                Assert.That(view.VictoryBlurWeight, Is.Zero);
            }
            finally
            {
                view.CancelAndRestore();
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GFR02_U09_VictoryBlurResolvesCameraVolumeLayers()
        {
            Assert.That(
                RunResultTransitionView.FindFirstIncludedVolumeLayer(1),
                Is.Zero);
            Assert.That(
                RunResultTransitionView.FindFirstIncludedVolumeLayer(32),
                Is.EqualTo(5));
            Assert.That(
                RunResultTransitionView.FindFirstIncludedVolumeLayer(128),
                Is.EqualTo(7));
            Assert.That(
                RunResultTransitionView.FindFirstIncludedVolumeLayer(0),
                Is.EqualTo(-1));
        }

        [Test]
        public void GFR02_U10_VictoryBlurCrossfadesFromSharpToBlurred()
        {
            float start =
                RunResultTransitionView.EvaluateVictorySharpFrameAlpha(0f);
            float quarter =
                RunResultTransitionView.EvaluateVictorySharpFrameAlpha(0.25f);
            float middle =
                RunResultTransitionView.EvaluateVictorySharpFrameAlpha(0.5f);
            float threeQuarters =
                RunResultTransitionView.EvaluateVictorySharpFrameAlpha(0.75f);
            float end =
                RunResultTransitionView.EvaluateVictorySharpFrameAlpha(1f);

            Assert.That(start, Is.EqualTo(1f));
            Assert.That(quarter, Is.LessThan(start));
            Assert.That(middle, Is.LessThan(quarter));
            Assert.That(threeQuarters, Is.LessThan(middle));
            Assert.That(end, Is.Zero);
        }

    }
}
