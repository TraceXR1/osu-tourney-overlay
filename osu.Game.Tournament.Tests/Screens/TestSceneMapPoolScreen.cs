// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.MapPool;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Tournament.Tests.Screens
{
    public partial class TestSceneMapPoolScreen : TournamentScreenTestScene
    {
        private MapPoolScreen screen = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(screen = new TestMapPoolScreen { Width = 0.7f });
        }

        [SetUpSteps]
        public override void SetUpSteps()
        {
            AddStep("reset state", resetState);
        }

        private void resetState()
        {
            Ladder.SplitMapPoolByMods.Value = true;
            Ladder.ProtectOrder.Value = OrderType.ABAB;
            Ladder.BanOrder.Value = OrderType.ABAB;
            Ladder.PickOrder.Value = OrderType.ABAB;
            Ladder.PhaseStarterDistribution.Value = PhaseStarterDistributionType.ProtectBanTogether;

            Ladder.CurrentMatch.Value = new TournamentMatch();
            Ladder.CurrentMatch.Value = Ladder.Matches.First();
            Ladder.CurrentMatch.Value.PicksBans.Clear();
            Ladder.CurrentMatch.Value.Protects.Clear();
            Ladder.CurrentMatch.Value.Round.Value!.ProtectCount.Value = 0;
            Ladder.CurrentMatch.Value.Round.Value!.BanCount.Value = 1;
            Ladder.CurrentMatch.Value.Round.Value!.AllowPickingOpponentProtects.Value = true;
            Ladder.CurrentMatch.Value = Ladder.Matches.First();
        }

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
        });

        [Test]
        public void TestFewMaps()
        {
            AddStep("load few maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 8; i++)
                    addBeatmap();
            });

            AddStep("reset match", () =>
            {
                Ladder.CurrentMatch.Value = new TournamentMatch();
            });

            assertTwoWide();
        }

        [Test]
        public void TestJustEnoughMaps()
        {
            AddStep("load just enough maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 18; i++)
                    addBeatmap();
            });

            AddStep("reset state", resetState);

            assertTwoWide();
        }

        [Test]
        public void TestManyMaps()
        {
            AddStep("load many maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 19; i++)
                    addBeatmap();
            });

            AddStep("reset state", resetState);

            assertThreeWide();
        }

        [Test]
        public void TestJustEnoughMods()
        {
            AddStep("load many maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 11; i++)
                    addBeatmap(i > 4 ? Ruleset.Value.CreateInstance().AllMods.ElementAt(i).Acronym : "NM");
            });

            AddStep("reset state", resetState);

            assertTwoWide();
        }

        private void assertTwoWide() =>
            AddAssert("ensure layout width is 2", () => screen.ChildrenOfType<FillFlowContainer<FillFlowContainer<TournamentBeatmapPanel>>>().First().Padding.Left > 0);

        private void assertThreeWide() =>
            AddAssert("ensure layout width is 3", () => screen.ChildrenOfType<FillFlowContainer<FillFlowContainer<TournamentBeatmapPanel>>>().First().Padding.Left == 0);

        [Test]
        public void TestManyMods()
        {
            AddStep("load many maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 12; i++)
                    addBeatmap(i > 4 ? Ruleset.Value.CreateInstance().AllMods.ElementAt(i).Acronym : "NM");
            });

            AddStep("reset state", resetState);

            assertThreeWide();
        }

        [Test]
        public void TestSplitMapPoolByMods()
        {
            AddStep("load many maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 12; i++)
                    addBeatmap(i > 4 ? Ruleset.Value.CreateInstance().AllMods.ElementAt(i).Acronym : "NM");
            });

            AddStep("disable splitting map pool by mods", () => Ladder.SplitMapPoolByMods.Value = false);

            AddStep("reset state", resetState);
        }

        [Test]
        public void TestPhaseStarterDistributions()
        {
            AddStep("verify phase starters from red protect", () =>
            {
                var match = new TournamentMatch();
                match.Protects.Add(new BeatmapChoice { Team = TeamColour.Red, Type = ChoiceType.Protect });

                assertPhaseStarters(match, new[]
                {
                    (TeamColour.Red, TeamColour.Red, TeamColour.Red),
                    (TeamColour.Red, TeamColour.Red, TeamColour.Blue),
                    (TeamColour.Red, TeamColour.Blue, TeamColour.Red),
                    (TeamColour.Red, TeamColour.Blue, TeamColour.Blue)
                });
            });

            AddStep("verify opposite phase starters from blue ban", () =>
            {
                var match = new TournamentMatch();
                match.PicksBans.Add(new BeatmapChoice { Team = TeamColour.Blue, Type = ChoiceType.Ban });

                assertPhaseStarters(match, new[]
                {
                    (TeamColour.Blue, TeamColour.Blue, TeamColour.Blue),
                    (TeamColour.Blue, TeamColour.Blue, TeamColour.Red),
                    (TeamColour.Red, TeamColour.Blue, TeamColour.Red),
                    (TeamColour.Red, TeamColour.Blue, TeamColour.Blue)
                });
            });
        }

        private static void assertPhaseStarters(TournamentMatch match, (TeamColour Protect, TeamColour Ban, TeamColour Pick)[] expected)
        {
            var distributions = new[]
            {
                PhaseStarterDistributionType.AllTogether,
                PhaseStarterDistributionType.ProtectBanTogether,
                PhaseStarterDistributionType.ProtectPickTogether,
                PhaseStarterDistributionType.BanPickTogether
            };

            for (int i = 0; i < distributions.Length; i++)
            {
                var (protect, ban, pick) = expected[i];
                Assert.That(match.GetMapPoolPhaseStarter(ChoiceType.Protect, distributions[i]), Is.EqualTo(protect));
                Assert.That(match.GetMapPoolPhaseStarter(ChoiceType.Ban, distributions[i]), Is.EqualTo(ban));
                Assert.That(match.GetMapPoolPhaseStarter(ChoiceType.Pick, distributions[i]), Is.EqualTo(pick));
                Assert.That(match.GetMapPoolTeamColour(1, distributions[i]), Is.EqualTo(protect));
                Assert.That(match.GetMapPoolTeamColour(2, distributions[i]), Is.EqualTo(protect == TeamColour.Red ? TeamColour.Blue : TeamColour.Red));
            }
        }

        [Test]
        public void TestProtectAndPick()
        {
            AddStep("configure protects", () =>
            {
                var round = Ladder.CurrentMatch.Value!.Round.Value!;
                round.ProtectCount.Value = 1;
                round.AllowPickingOpponentProtects.Value = false;
                round.Beatmaps.Clear();
                for (int i = 0; i < 4; i++)
                    addBeatmap();
            });
            AddStep("update displayed maps", () => Ladder.SplitMapPoolByMods.Value = false);
            AddStep("red protect mode", () => screen.ChildrenOfType<TourneyButton>().First(btn => btn.Text == "Red Protect").TriggerClick());
            assertSelectedActionButton(TeamColour.Red, ChoiceType.Protect);
            AddStep("protect first map", () => clickBeatmapPanel(0));
            assertSelectedActionButton(TeamColour.Blue, ChoiceType.Protect);
            AddAssert("red protect recorded", () => Ladder.CurrentMatch.Value!.Protects.Single().Team == TeamColour.Red);
            checkTotalPickBans(0);
            AddStep("protect second map", () => clickBeatmapPanel(1));
            assertSelectedActionButton(TeamColour.Red, ChoiceType.Ban);
            AddAssert("blue protect recorded", () => Ladder.CurrentMatch.Value!.Protects.Last().Team == TeamColour.Blue);
            AddStep("red ban mode", () => screen.ChildrenOfType<TourneyButton>().First(btn => btn.Text == "Red Ban").TriggerClick());
            AddStep("attempt to ban protected map", () => clickBeatmapPanel(0));
            checkTotalPickBans(0);
            AddStep("blue pick mode", () => screen.ChildrenOfType<TourneyButton>().First(btn => btn.Text == "Blue Pick").TriggerClick());
            AddStep("attempt opponent protect pick", () => clickBeatmapPanel(0));
            checkTotalPickBans(0);
            AddStep("red pick mode", () => screen.ChildrenOfType<TourneyButton>().First(btn => btn.Text == "Red Pick").TriggerClick());
            AddStep("pick own protect", () => clickBeatmapPanel(0));
            checkTotalPickBans(1);
            checkLastPick(ChoiceType.Pick, TeamColour.Red);
            AddAssert("protect preserved", () => Ladder.CurrentMatch.Value!.Protects.Count == 2);
            AddStep("reset choices", () => screen.ChildrenOfType<TourneyButton>().First(btn => btn.Text == "Reset").TriggerClick());
            checkTotalPickBans(0);
            AddAssert("protects cleared", () => Ladder.CurrentMatch.Value!.Protects.Count == 0);
        }

        [Test]
        public void TestBanOrderMultipleBans()
        {
            AddStep("set ban count", () => Ladder.CurrentMatch.Value!.Round.Value!.BanCount.Value = 2);

            AddStep("load some maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 5; i++)
                    addBeatmap();
            });

            AddStep("update displayed maps", () => Ladder.SplitMapPoolByMods.Value = false);

            AddStep("start bans from blue team", () => screen.ChildrenOfType<TourneyButton>().First(btn => btn.Text == "Blue Ban").TriggerClick());
            assertSelectedActionButton(TeamColour.Blue, ChoiceType.Ban);

            AddStep("ban map", () => clickBeatmapPanel(0));
            assertSelectedActionButton(TeamColour.Red, ChoiceType.Ban);
            checkTotalPickBans(1);
            checkLastPick(ChoiceType.Ban, TeamColour.Blue);

            AddStep("ban map", () => clickBeatmapPanel(1));
            assertSelectedActionButton(TeamColour.Blue, ChoiceType.Ban);
            checkTotalPickBans(2);
            checkLastPick(ChoiceType.Ban, TeamColour.Red);

            AddStep("ban map", () => clickBeatmapPanel(2));
            assertSelectedActionButton(TeamColour.Red, ChoiceType.Ban);
            checkTotalPickBans(3);
            checkLastPick(ChoiceType.Ban, TeamColour.Blue);

            AddStep("fourth ban", () => clickBeatmapPanel(3));
            assertSelectedActionButton(TeamColour.Red, ChoiceType.Pick);
            checkTotalPickBans(4);
            checkLastPick(ChoiceType.Ban, TeamColour.Red);

            AddStep("pick map", () => clickBeatmapPanel(4));
            assertSelectedActionButton(TeamColour.Blue, ChoiceType.Pick);
            checkTotalPickBans(5);
            checkLastPick(ChoiceType.Pick, TeamColour.Red);
        }

        [Test]
        public void TestPickBanOrder()
        {
            AddStep("set ban count", () => Ladder.CurrentMatch.Value!.Round.Value!.BanCount.Value = 1);

            AddStep("load some maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 5; i++)
                    addBeatmap();
            });

            AddStep("update displayed maps", () => Ladder.SplitMapPoolByMods.Value = false);

            AddStep("start bans from blue team", () => screen.ChildrenOfType<TourneyButton>().First(btn => btn.Text == "Blue Ban").TriggerClick());
            assertSelectedActionButton(TeamColour.Blue, ChoiceType.Ban);

            AddStep("ban map", () => clickBeatmapPanel(0));
            assertSelectedActionButton(TeamColour.Red, ChoiceType.Ban);
            checkTotalPickBans(1);
            checkLastPick(ChoiceType.Ban, TeamColour.Blue);

            AddStep("ban map", () => clickBeatmapPanel(1));
            assertSelectedActionButton(TeamColour.Red, ChoiceType.Pick);
            checkTotalPickBans(2);
            checkLastPick(ChoiceType.Ban, TeamColour.Red);

            AddStep("pick map", () => clickBeatmapPanel(2));
            assertSelectedActionButton(TeamColour.Blue, ChoiceType.Pick);
            checkTotalPickBans(3);
            checkLastPick(ChoiceType.Pick, TeamColour.Red);

            AddStep("pick map", () => clickBeatmapPanel(3));
            assertSelectedActionButton(TeamColour.Red, ChoiceType.Pick);
            checkTotalPickBans(4);
            checkLastPick(ChoiceType.Pick, TeamColour.Blue);

            AddStep("pick map", () => clickBeatmapPanel(4));
            assertSelectedActionButton(TeamColour.Blue, ChoiceType.Pick);
            checkTotalPickBans(5);
            checkLastPick(ChoiceType.Pick, TeamColour.Red);

            AddStep("reset match", () =>
            {
                Ladder.CurrentMatch.Value = new TournamentMatch();
                Ladder.CurrentMatch.Value = Ladder.Matches.First();
                Ladder.CurrentMatch.Value.PicksBans.Clear();
            });
        }

        [Test]
        public void TestSelectedActionButtonsFollowConfiguredOrder()
        {
            AddStep("configure ban and pick orders", () =>
            {
                var round = Ladder.CurrentMatch.Value!.Round.Value!;
                round.ProtectCount.Value = 0;
                round.BanCount.Value = 2;
                Ladder.BanOrder.Value = OrderType.ABBA;
                Ladder.PickOrder.Value = OrderType.ABBA;
                Ladder.PhaseStarterDistribution.Value = PhaseStarterDistributionType.ProtectBanTogether;
                Ladder.SplitMapPoolByMods.Value = false;
                round.Beatmaps.Clear();

                for (int i = 0; i < 8; i++)
                    addBeatmap();
            });
            AddStep("refresh displayed maps", () =>
            {
                Ladder.SplitMapPoolByMods.Value = true;
                Ladder.SplitMapPoolByMods.Value = false;
            });
            AddAssert("all test maps displayed", () => screen.ChildrenOfType<TournamentBeatmapPanel>().Count(), () => Is.EqualTo(8));

            AddStep("select blue ban before first action", () => screen.ChildrenOfType<TourneyButton>().First(btn => btn.Text == "Blue Ban").TriggerClick());
            assertSelectedActionButton(TeamColour.Blue, ChoiceType.Ban);

            var banActions = new[]
            {
                (TeamColour.Blue, TeamColour.Red),
                (TeamColour.Red, TeamColour.Red),
                (TeamColour.Red, TeamColour.Blue),
                (TeamColour.Blue, TeamColour.Red)
            };

            for (int i = 0; i < banActions.Length; i++)
            {
                int mapIndex = i;
                var (current, next) = banActions[i];
                assertSelectedActionButton(current, ChoiceType.Ban);
                AddStep($"record {current} ban", () => clickBeatmapPanel(mapIndex));
                assertSelectedActionButton(next, i == banActions.Length - 1 ? ChoiceType.Pick : ChoiceType.Ban);
            }

            var pickActions = new[]
            {
                (TeamColour.Red, TeamColour.Blue),
                (TeamColour.Blue, TeamColour.Blue),
                (TeamColour.Blue, TeamColour.Red),
                (TeamColour.Red, TeamColour.Red)
            };

            for (int i = 0; i < pickActions.Length; i++)
            {
                int mapIndex = i + banActions.Length;
                var (current, next) = pickActions[i];
                assertSelectedActionButton(current, ChoiceType.Pick);
                AddStep($"record {current} pick", () => clickBeatmapPanel(mapIndex));
                assertSelectedActionButton(next, ChoiceType.Pick);
            }
        }

        [Test]
        public void TestMultipleTeamBans()
        {
            AddStep("set ban count", () => Ladder.CurrentMatch.Value!.Round.Value!.BanCount.Value = 3);

            AddStep("load some maps", () =>
            {
                Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Clear();

                for (int i = 0; i < 12; i++)
                    addBeatmap();
            });

            AddStep("update displayed maps", () => Ladder.SplitMapPoolByMods.Value = false);

            AddStep("start bans with red team", () => screen.ChildrenOfType<TourneyButton>().First(btn => btn.Text == "Red Ban").TriggerClick());

            AddStep("first ban", () => clickBeatmapPanel(0));
            AddAssert("red ban registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban && pb.Team == TeamColour.Red),
                () => Is.EqualTo(1));

            AddStep("ban two more maps", () =>
            {
                clickBeatmapPanel(1);
                clickBeatmapPanel(2);
            });

            AddAssert("three bans registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban),
                () => Is.EqualTo(3));
            AddAssert("two bans for red team so far",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban && pb.Team == TeamColour.Red),
                () => Is.EqualTo(2));

            AddStep("ban two more maps", () =>
            {
                clickBeatmapPanel(3);
                clickBeatmapPanel(4);
            });

            AddAssert("five bans registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban),
                () => Is.EqualTo(5));
            AddAssert("three bans for red team",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban && pb.Team == TeamColour.Red),
                () => Is.EqualTo(3));

            AddStep("ban last map", () => clickBeatmapPanel(5));
            AddAssert("six bans registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban),
                () => Is.EqualTo(6));
            AddAssert("red banned three",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban && pb.Team == TeamColour.Red),
                () => Is.EqualTo(3));
            AddAssert("blue banned three",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Ban && pb.Team == TeamColour.Blue),
                () => Is.EqualTo(3));

            AddStep("pick map", () => clickBeatmapPanel(6));
            AddAssert("one pick registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Pick),
                () => Is.EqualTo(1));
            AddAssert("pick was blue's",
                () => Ladder.CurrentMatch.Value!.PicksBans.Last().Team,
                () => Is.EqualTo(TeamColour.Blue));

            AddStep("pick map", () => clickBeatmapPanel(7));
            AddAssert("two picks registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Pick),
                () => Is.EqualTo(2));
            AddAssert("pick was red's",
                () => Ladder.CurrentMatch.Value!.PicksBans.Last().Team,
                () => Is.EqualTo(TeamColour.Red));

            AddStep("pick map", () => clickBeatmapPanel(8));
            AddAssert("three picks registered",
                () => Ladder.CurrentMatch.Value!.PicksBans.Count(pb => pb.Type == ChoiceType.Pick),
                () => Is.EqualTo(3));
            AddAssert("pick was blue's",
                () => Ladder.CurrentMatch.Value!.PicksBans.Last().Team,
                () => Is.EqualTo(TeamColour.Blue));

            AddStep("reset match", () =>
            {
                Ladder.CurrentMatch.Value = new TournamentMatch();
                Ladder.CurrentMatch.Value = Ladder.Matches.First();
                Ladder.CurrentMatch.Value.PicksBans.Clear();
            });
        }

        private void checkTotalPickBans(int expected) => AddAssert($"total pickbans is {expected}", () => Ladder.CurrentMatch.Value!.PicksBans, () => Has.Count.EqualTo(expected));

        private void checkLastPick(ChoiceType expectedChoice, TeamColour expectedColour) =>
            AddAssert($"last choice was {expectedChoice} by {expectedColour}",
                () => Ladder.CurrentMatch.Value!.PicksBans.Select(pb => (pb.Type, pb.Team)).Last(),
                () => Is.EqualTo((expectedChoice, expectedColour)));

        private void assertSelectedActionButton(TeamColour expectedColour, ChoiceType expectedChoice) =>
            AddAssert($"selected button is {expectedColour} {expectedChoice}", () =>
            {
                string action = expectedChoice.ToString();
                string expectedText = $"{(expectedColour == TeamColour.Red ? Ladder.RedSide.Name.Value : Ladder.BlueSide.Name.Value)} {action}";
                var selectedButtons = screen.ChildrenOfType<TourneyButton>()
                    .Where(button => button.Text.ToString().EndsWith($" {action}", System.StringComparison.Ordinal) && button.Colour == Color4.White)
                    .ToArray();

                return selectedButtons.Length == 1 && selectedButtons[0].Text.ToString() == expectedText;
            });

        private void addBeatmap(string mods = "NM")
        {
            Ladder.CurrentMatch.Value!.Round.Value!.Beatmaps.Add(new RoundBeatmap
            {
                Beatmap = CreateSampleBeatmap(),
                Mods = mods
            });
        }

        private void clickBeatmapPanel(int index)
        {
            InputManager.MoveMouseTo(screen.ChildrenOfType<TournamentBeatmapPanel>().ElementAt(index));
            InputManager.Click(MouseButton.Left);
        }

        private partial class TestMapPoolScreen : MapPoolScreen
        {
            // this is a bit of a test-specific workaround.
            // the way pick/ban is implemented is a bit funky; the screen itself is what handles the mouse there,
            // rather than the beatmap panels themselves.
            // in some extreme situations headless it may turn out that the panels overflow the screen,
            // and as such picking stops working anymore outside of the bounds of the screen drawable.
            // this override makes it so the screen sees all of the input at all times, making that impossible to happen.
            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;
        }
    }
}
