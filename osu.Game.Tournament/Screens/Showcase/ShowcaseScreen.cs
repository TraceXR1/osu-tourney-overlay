// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Tournament.Components;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Cursor;
using osu.Game.Tournament.Models;
using osu.Game.Overlays.Settings;
using osuTK;

namespace osu.Game.Tournament.Screens.Showcase
{
    public partial class ShowcaseScreen : BeatmapInfoScreen
    {
        [Resolved]
        private LadderInfo ladder { get; set; } = null!;

        private Drawable chroma = null!;

        private readonly BindableNumber<float> chromaScale = new BindableFloat(1)
        {
            MinValue = 0.25f,
            MaxValue = 1,
            Precision = 0.01f,
        };

        private readonly Bindable<TournamentRound?> selectedRound = new Bindable<TournamentRound?>();

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
                                        new SettingsRoundDropdown(ladder.Rounds)
                                        {
                                            LabelText = "Round",
                                            Current = selectedRound,
                                        },
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
            selectedRound.BindValueChanged(round => SongBar.Round = round.NewValue, true);
        }

        public override void Show()
        {
            chroma.ClearTransforms();
            chroma.Scale = new Vector2(chromaScale.Value);
            base.Show();
        }

        public void ExpandChroma() => chroma.Scale = new Vector2(chromaScale.Value);

        public void CollapseChroma(System.Action completed)
        {
            chroma.Scale = Vector2.Zero;
            completed();
        }

        protected override void CurrentMatchChanged(ValueChangedEvent<TournamentMatch?> match)
        {
            // showcase screen doesn't care about a match being selected.
            // base call intentionally omitted to not show match warning.
        }

        private partial class SettingsRoundDropdown : SettingsDropdown<TournamentRound?>
        {
            public SettingsRoundDropdown(BindableList<TournamentRound> rounds)
            {
                Current = new Bindable<TournamentRound?>();

                foreach (var round in rounds.Prepend(new TournamentRound()))
                    add(round);

                rounds.CollectionChanged += (_, args) =>
                {
                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            Debug.Assert(args.NewItems != null);
                            foreach (TournamentRound round in args.NewItems!)
                                add(round);
                            break;

                        case NotifyCollectionChangedAction.Remove:
                            Debug.Assert(args.OldItems != null);
                            foreach (TournamentRound round in args.OldItems!)
                                Control.RemoveDropdownItem(round);
                            break;
                    }
                };
            }

            private void add(TournamentRound round)
            {
                Control.AddDropdownItem(round);
                var name = (Bindable<string>)round.Name.GetBoundCopy();
                name.BindValueChanged(_ =>
                {
                    Control.RemoveDropdownItem(round);
                    Control.AddDropdownItem(round);
                });
            }
        }
    }
}
