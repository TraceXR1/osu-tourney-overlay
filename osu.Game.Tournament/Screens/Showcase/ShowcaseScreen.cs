// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Tournament.Components;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Threading;
using osu.Game.Tournament.Models;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Settings;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Showcase
{
    public partial class ShowcaseScreen : BeatmapInfoScreen
    {
        [Resolved]
        private LadderInfo ladder { get; set; } = null!;

        private Drawable chroma = null!;
        private const double chroma_open_delay = 200;
        private const double chroma_transition_duration = 500;
        private ScheduledDelegate? scheduledChromaOpen;
        private readonly BindableNumber<float> chromaScale = new BindableFloat(1)
        {
            MinValue = 0.25f,
            MaxValue = 1,
            Precision = 0.01f,
        };

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(new PopoverContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                new TournamentLogo(),
                new TourneyVideo("showcase")
                {
                    Loop = true,
                    RelativeSizeAxes = Axes.Both,
                },
                new Container
                {
                    Padding = new MarginPadding { Bottom = SongBar.HEIGHT },
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        chroma = new Box
                        {
                            // chroma key area for stable gameplay
                            Name = "chroma",
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.BottomCentre,
                            Width = 1920,
                            Height = 1080 - SongBar.HEIGHT,
                            Scale = new Vector2(1, 0),
                            Colour = ladder.ChromaKeyColour.Value,
                        },
                        new ControlPanel
                        {
                            Children = new Drawable[]
                            {
                                new SettingsSlider<float>
                                {
                                    LabelText = "Chroma scale",
                                    Current = chromaScale,
                                },
                                new SettingsColour
                                {
                                    LabelText = "Chroma colour",
                                    Current = ladder.ChromaKeyColour,
                                },
                            }
                        },
                    }
                }
                    }
                }
            });

            chromaScale.BindValueChanged(scale => chroma.Scale = new Vector2(scale.NewValue, scale.NewValue), true);
            ladder.ChromaKeyColour.BindValueChanged(colour => chroma.Colour = colour.NewValue);
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
            chroma.Scale = Vector2.Zero;
            base.Show();
            scheduledChromaOpen = Scheduler.AddDelayed(ExpandChroma, chroma_open_delay);
        }

        public void ExpandChroma() => chroma.ScaleTo(new Vector2(chromaScale.Value), chroma_transition_duration, Easing.OutQuint);

        public void CollapseChroma(System.Action completed)
        {
            scheduledChromaOpen?.Cancel();
            chroma.ScaleTo(Vector2.Zero, chroma_transition_duration, Easing.InQuint).OnComplete(_ => completed());
        }

        protected override void CurrentMatchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            // showcase screen doesn't care about a match being selected.
            // base call intentionally omitted to not show match warning.
        }
    }
}
