// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Testing;
using osu.Framework.Threading;
using osu.Game.Overlays.Settings;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;
using osu.Game.Tournament.Screens.Gameplay.Components;
using osu.Game.Tournament.Screens.MapPool;
using osu.Game.Tournament.Screens.TeamWin;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay
{
    public partial class GameplayScreen : BeatmapInfoScreen
    {
        private readonly BindableBool warmup = new BindableBool();

        public readonly Bindable<TourneyState> State = new Bindable<TourneyState>();
        private MatchIPCInfo ipc = null!;

        [Resolved]
        private TournamentSceneManager? sceneManager { get; set; }

        [Resolved]
        private TournamentMatchChatDisplay chat { get; set; } = null!;

        private Drawable chroma = null!;
        private const double chroma_open_delay = 200;
        private const double chroma_transition_duration = 500;
        private ScheduledDelegate? scheduledChromaOpen;

        [BackgroundDependencyLoader]
        private void load(MatchIPCInfo ipc)
        {
            this.ipc = ipc;

            LabelledSwitchButton chatToggle;

            AddInternal(new PopoverContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                new TourneyVideo("gameplay")
                {
                    Loop = true,
                    RelativeSizeAxes = Axes.Both,
                },
                header = new MatchHeader
                {
                    ShowLogo = false,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Y = 110,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Children = new[]
                    {
                        chroma = new Container
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Height = 512,
                            Children = new Drawable[]
                            {
                                new ChromaArea
                                {
                                    Name = "Left chroma",
                                    IsRightSide = false,
                                    RelativeSizeAxes = Axes.Both,
                                    Width = 0.5f,
                                },
                                new ChromaArea
                                {
                                    Name = "Right chroma",
                                    IsRightSide = true,
                                    RelativeSizeAxes = Axes.Both,
                                    Anchor = Anchor.TopRight,
                                    Origin = Anchor.TopRight,
                                    Width = 0.5f,
                                }
                            }
                        },
                    }
                },
                scoreDisplay = new TournamentMatchScoreDisplay
                {
                    Y = -147,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.TopCentre,
                },
                new ControlPanel
                {
                    Children = new Drawable[]
                    {
                        new LabelledSwitchButton
                        {
                            Label = "Warmup",
                            Current = warmup,
                        },
                        chatToggle = new LabelledSwitchButton
                        {
                            Label = "Show chat",
                        },
                        new SettingsSlider<int>
                        {
                            LabelText = "Chroma width",
                            Current = LadderInfo.ChromaKeyWidth,
                            KeyboardStep = 1,
                        },
                        new SettingsColour
                        {
                            LabelText = "Chroma colour",
                            Current = LadderInfo.ChromaKeyColour,
                        },
                        new SettingsSlider<int>
                        {
                            LabelText = "Players per team",
                            Current = LadderInfo.PlayersPerTeam,
                            KeyboardStep = 1,
                        },
                    }
                }
                    }
                }
            });

            State.BindValueChanged(_ => chatToggle.Current.Value = State.Value == TourneyState.Idle, true);
            chatToggle.Current.BindValueChanged(v => State.Value = v.NewValue ? TourneyState.Idle : TourneyState.Playing);

            LadderInfo.ChromaKeyWidth.BindValueChanged(width => chroma.Width = width.NewValue, true);

            warmup.BindValueChanged(w => header.ShowScores = !w.NewValue, true);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            State.BindTo(ipc.State);
            State.BindValueChanged(_ => updateState(), true);
        }

        protected override void CurrentMatchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            base.CurrentMatchChanged(match);

            if (match.NewValue == null)
                return;

            warmup.Value = match.NewValue.Team1Score.Value + match.NewValue.Team2Score.Value == 0;
        }

        private ScheduledDelegate? scheduledContract;

        private TournamentMatchScoreDisplay scoreDisplay = null!;

        private TourneyState lastState;
        private MatchHeader header = null!;

        private void contract()
        {
            if (!IsLoaded)
                return;

            scheduledContract?.Cancel();

            SongBar.Expanded = false;
            scoreDisplay.FadeOut(100);
            using (chat.BeginDelayedSequence(500))
                chat.Expand();
        }

        private void expand()
        {
            if (!IsLoaded)
                return;

            scheduledContract?.Cancel();

            chat.Contract();

            using (BeginDelayedSequence(300))
            {
                scoreDisplay.FadeIn(100);
                SongBar.Expanded = true;
            }
        }

        private void updateState()
        {
            try
            {
                if (State.Value == TourneyState.Ranking)
                {
                    if (warmup.Value || CurrentMatch.Value == null) return;

                    if (ipc.Score1.Value > ipc.Score2.Value)
                        CurrentMatch.Value.Team1Score.Value++;
                    else if (ipc.Score1.Value < ipc.Score2.Value)
                        CurrentMatch.Value.Team2Score.Value++;
                }

                switch (State.Value)
                {
                    case TourneyState.Idle:
                        contract();

                        if (LadderInfo.AutoProgressScreens.Value)
                        {
                            // if we've returned to idle and the last screen was ranking
                            // we should automatically proceed after a short delay
                            if (lastState == TourneyState.Ranking && !warmup.Value)
                            {
                                if (CurrentMatch.Value?.Completed.Value == true)
                                    sceneManager?.SetScreen(typeof(TeamWinScreen));
                                else if (CurrentMatch.Value?.Completed.Value == false)
                                    sceneManager?.SetScreen(typeof(MapPoolScreen));
                            }
                        }

                        break;

                    case TourneyState.Ranking:
                        scheduledContract = Scheduler.AddDelayed(contract, 2000);
                        break;

                    default:
                        expand();
                        break;
                }
            }
            finally
            {
                lastState = State.Value;
            }
        }

        public override void Hide()
        {
            scheduledChromaOpen?.Cancel();
            base.Hide();
        }

        public override void Show()
        {
            scheduledChromaOpen?.Cancel();
            chroma.ClearTransforms();
            chroma.Width = 0;
            updateState();
            base.Show();
            scheduledChromaOpen = Scheduler.AddDelayed(ExpandChroma, chroma_open_delay);
        }

        public void ExpandChroma()
        {
            chroma.Width = LadderInfo.ChromaKeyWidth.Value;

            foreach (var area in chroma.ChildrenOfType<ChromaArea>())
                area.AnimateWindowsIn();
        }

        public void CollapseChroma(Action completed)
        {
            scheduledChromaOpen?.Cancel();

            var areas = chroma.ChildrenOfType<ChromaArea>().ToArray();
            int remainingAreas = areas.Length;

            if (remainingAreas == 0)
            {
                chroma.Width = 0;
                completed();
                return;
            }

            foreach (var area in areas)
            {
                area.CollapseWindows(() =>
                {
                    if (--remainingAreas == 0)
                    {
                        chroma.Width = 0;
                        completed();
                    }
                });
            }
        }

        private partial class ChromaArea : CompositeDrawable
        {
            public bool IsRightSide { get; init; }

            [Resolved]
            private LadderInfo ladder { get; set; } = null!;

            [BackgroundDependencyLoader]
            private void load()
            {
                // chroma key area for stable gameplay
                Colour = ladder.ChromaKeyColour.Value;

                ladder.ChromaKeyColour.BindValueChanged(colour => Colour = colour.NewValue);

                ladder.PlayersPerTeam.BindValueChanged(performLayout, true);
            }

            private void performLayout(ValueChangedEvent<int> playerCount)
            {
                switch (playerCount.NewValue)
                {
                    case 3:
                        Anchor windowAnchor = IsRightSide ? Anchor.TopLeft : Anchor.TopRight;
                        Anchor bottomWindowAnchor = IsRightSide ? Anchor.BottomLeft : Anchor.BottomRight;

                        InternalChildren = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Width = 0.5f,
                                Height = 0.5f,
                                Anchor = windowAnchor,
                                Origin = windowAnchor,
                                RelativePositionAxes = Axes.X,
                                Position = new Vector2(IsRightSide ? 0.25f : -0.25f, 0),
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Anchor = bottomWindowAnchor,
                                Origin = bottomWindowAnchor,
                                Height = 0.5f,
                            },
                        };
                        break;

                    default:
                        InternalChild = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = IsRightSide ? Anchor.CentreLeft : Anchor.CentreRight,
                            Origin = IsRightSide ? Anchor.CentreLeft : Anchor.CentreRight,
                        };
                        break;
                }
            }

            public void AnimateWindowsIn()
            {
                var windows = InternalChildren.OfType<Box>().ToArray();

                foreach (var window in windows)
                {
                    window.ClearTransforms();
                    window.Scale = new Vector2(0, 1);
                    window.ScaleTo(Vector2.One, chroma_transition_duration, Easing.OutQuint);
                }
            }

            public void CollapseWindows(Action completed)
            {
                var windows = InternalChildren.OfType<Box>().ToArray();
                int remainingWindows = windows.Length;

                if (remainingWindows == 0)
                {
                    completed();
                    return;
                }

                foreach (var window in windows)
                {
                    window.ScaleTo(new Vector2(0, 1), chroma_transition_duration, Easing.InQuint)
                          .OnComplete(_ =>
                          {
                              if (--remainingWindows == 0)
                                  completed();
                          });
                }
            }
        }
    }
}
