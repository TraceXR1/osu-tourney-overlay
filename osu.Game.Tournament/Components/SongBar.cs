// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Models;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Menu;
using osu.Game.Tournament.Models;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Components
{
    public partial class SongBar : CompositeDrawable
    {
        private IBeatmapInfo? beatmap;
        private TournamentRound? round;
        private OsuColour colours = new OsuColour();
        private double? moddedStarRating;
        private int starRatingRequestGeneration;

        public const float HEIGHT = 145 / 2f;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private TournamentBeatmapAttributesCache attributesCache { get; set; } = null!;

        public IBeatmapInfo? Beatmap
        {
            set
            {
                if (beatmap == value)
                    return;

                beatmap = value;
                updateModdedStarRating();
                refreshContent();
            }
        }

        public TournamentRound? Round
        {
            get => round;
            set
            {
                if (round == value)
                    return;

                round = value;
                refreshContent();
            }
        }

        private LegacyMods mods;

        public LegacyMods Mods
        {
            get => mods;
            set
            {
                if (mods == value)
                    return;

                mods = value;
                updateModdedStarRating();
                refreshContent();
            }
        }

        private FillFlowContainer flow = null!;

        private bool expanded;

        public bool Expanded
        {
            get => expanded;
            set
            {
                expanded = value;
                flow.Direction = expanded ? FillDirection.Full : FillDirection.Vertical;
            }
        }

        // Todo: This is a hack for https://github.com/ppy/osu-framework/issues/3617 since this container is at the very edge of the screen and potentially initially masked away.
        protected override bool ComputeIsMaskedAway(RectangleF maskingBounds) => false;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            this.colours = colours;
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            Masking = true;
            CornerRadius = 5;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = colours.Gray3,
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.4f,
                },
                flow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Full,
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                }
            };

            Expanded = true;
        }

        private void refreshContent()
        {
            beatmap ??= new BeatmapInfo
            {
                Metadata = new BeatmapMetadata
                {
                    Artist = "unknown",
                    Title = "no beatmap selected",
                    Author = new RealmUser { Username = "unknown" },
                },
                DifficultyName = "unknown",
                BeatmapSet = new BeatmapSetInfo(),
                StarRating = 0,
                Difficulty = new BeatmapDifficulty
                {
                    CircleSize = 0,
                    DrainRate = 0,
                    OverallDifficulty = 0,
                    ApproachRate = 0,
                },
            };

            var rulesetInstance = ruleset.Value.CreateInstance();

            var convertedMods = rulesetInstance.ConvertFromLegacyMods(mods).ToList();
            var adjustedDifficulty = rulesetInstance.GetAdjustedDisplayDifficulty(beatmap, convertedMods);

            double rate = ModUtils.CalculateRateWithMods(convertedMods);
            double bpm = FormatUtils.RoundBPM(beatmap.BPM, rate);
            double length = beatmap.Length / rate;

            string srExtra = "";

            if (convertedMods.Any(x => x is ModHardRock) || convertedMods.Any(x => x is ModDoubleTime))
            {
                srExtra = "*";
            }

            Color4? getStatColour(string stat)
            {
                bool increasedByHardRock = convertedMods.Any(x => x is ModHardRock) && stat is "CS" or "AR" or "OD";
                bool increasedByDoubleTime = convertedMods.Any(x => x is ModDoubleTime) && stat is "AR" or "OD" or "BPM" or "Length";

                if (increasedByHardRock || increasedByDoubleTime)
                    return colours.Red1;

                if (convertedMods.Any(x => x is ModEasy) && stat is "CS" or "AR" or "OD")
                    return colours.Green1;

                return null;
            }

            (string heading, string content, Color4? colour)[] stats;

            switch (ruleset.Value.OnlineID)
            {
                default:
                    stats = new (string heading, string content, Color4? colour)[]
                    {
                        ("CS", $"{adjustedDifficulty.CircleSize:0.#}", getStatColour("CS")),
                        ("AR", $"{adjustedDifficulty.ApproachRate:0.#}", getStatColour("AR")),
                        ("OD", $"{adjustedDifficulty.OverallDifficulty:0.#}", getStatColour("OD")),
                    };
                    break;

                case 1:
                case 3:
                    stats = new (string heading, string content, Color4? colour)[]
                    {
                        ("OD", $"{adjustedDifficulty.OverallDifficulty:0.#}", getStatColour("OD")),
                        ("HP", $"{adjustedDifficulty.DrainRate:0.#}", getStatColour("HP"))
                    };
                    break;

                case 2:
                    stats = new (string heading, string content, Color4? colour)[]
                    {
                        ("CS", $"{adjustedDifficulty.CircleSize:0.#}", getStatColour("CS")),
                        ("AR", $"{adjustedDifficulty.ApproachRate:0.#}", getStatColour("AR")),
                    };
                    break;
            }

            flow.Children = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = HEIGHT,
                    Width = 0.5f,
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,

                    Children = new Drawable[]
                    {
                        new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.Relative, 0.25f),
                                new Dimension(GridSizeMode.Relative, 0.25f),
                                new Dimension(GridSizeMode.Relative, 0.47f),
                            },

                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Direction = FillDirection.Vertical,
                                        Children = new Drawable[]
                                        {
                                            new DiffPiece(colours, stats),
                                            createStarRatingPiece(beatmap.StarRating, srExtra, convertedMods)
                                        }
                                    },
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Direction = FillDirection.Vertical,
                                        Children = new Drawable[]
                                        {
                                            new DiffPiece(colours, ("Length", length.ToFormattedDuration().ToString(), getStatColour("Length"))),
                                            new DiffPiece(colours, ("BPM", $"{bpm:0.#}", getStatColour("BPM"))),
                                        }
                                    },
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Children = new[]
                                        {
                                            createSlotDisplay(),
                                        }
                                    },
                                },
                            }
                        }
                    }
                },
                new TournamentBeatmapPanel(beatmap)
                {
                    RelativeSizeAxes = Axes.X,
                    Width = 0.5f,
                    Height = HEIGHT,
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                }
            };
        }

        private void updateModdedStarRating()
        {
            int requestGeneration = ++starRatingRequestGeneration;
            moddedStarRating = null;

            if (beatmap is not TournamentBeatmap currentBeatmap || currentBeatmap.OnlineID <= 0)
                return;

            var convertedMods = ruleset.Value.CreateInstance().ConvertFromLegacyMods(mods).ToArray();

            if (convertedMods.Length == 0)
                return;

            var ratingTask = attributesCache.GetStarRatingAsync(currentBeatmap.OnlineID, ruleset.Value.ShortName, convertedMods.Select(mod => mod.Acronym).ToArray());
            ratingTask.ContinueWith(task =>
            {
                if (!task.IsCompletedSuccessfully)
                    return;

                Scheduler.AddOnce(() =>
                {
                    if (requestGeneration != starRatingRequestGeneration)
                        return;

                    moddedStarRating = task.GetResultSafely();

                    if (moddedStarRating.HasValue)
                        refreshContent();
                });
            });
        }

        private DiffPiece createStarRatingPiece(double baseStarRating, string fallbackSuffix, IReadOnlyCollection<Mod> convertedMods)
        {
            string displayValue = moddedStarRating?.FormatStarRating().ToString()
                                  ?? $"{baseStarRating.FormatStarRating()}{fallbackSuffix}";
            string originalValue = moddedStarRating.HasValue ? $" ({baseStarRating.FormatStarRating()})" : string.Empty;

            Color4? colour = null;

            if (moddedStarRating.HasValue)
            {
                if (convertedMods.Any(mod => mod is ModHidden || mod is ModHardRock || mod is ModDoubleTime))
                    colour = colours.Red1;
                else if (convertedMods.Any(mod => mod is ModEasy))
                    colour = colours.Green1;
            }

            return new DiffPiece(colours, ("Star Rating", displayValue, colour), originalValue);
        }

        private Drawable createSlotDisplay()
        {
            if (round == null)
            {
                return new OsuLogo
                {
                    Triangles = false,
                    Scale = new Vector2(0.08f),
                    Margin = new MarginPadding(50),
                    X = -10,
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                };
            }

            const int columns = 8;
            var beatmaps = round.Beatmaps.ToArray();
            int rowCount = Math.Max(1, (beatmaps.Length + columns - 1) / columns);
            var rows = new Drawable[rowCount][];
            var slotsPerMod = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            string[] labels = new string[beatmaps.Length];

            for (int i = 0; i < beatmaps.Length; i++)
            {
                string mod = beatmaps[i].Mods;
                slotsPerMod.TryGetValue(mod, out int modSlot);
                labels[i] = $"{mod}{++modSlot}";
                slotsPerMod[mod] = modSlot;
            }

            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                int startIndex = rowIndex * columns;
                int slotCount = Math.Min(columns, beatmaps.Length - startIndex);
                var row = new Drawable[slotCount];

                for (int column = 0; column < slotCount; column++)
                {
                    int index = startIndex + column;
                    bool isCurrent = beatmap is TournamentBeatmap currentBeatmap
                                     && currentBeatmap.OnlineID > 0
                                     && beatmaps[index].Beatmap?.OnlineID == currentBeatmap.OnlineID;

                    if (isCurrent)
                    {
                        row[column] = new TournamentSpriteText
                        {
                            Text = labels[index],
                            Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 16),
                            Colour = new Color4(1f, 0.906f, 0.498f, 1f),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                        };
                    }
                    else
                    {
                        row[column] = new TournamentSpriteText
                        {
                            Text = labels[index],
                            Font = OsuFont.Torus.With(weight: FontWeight.Bold, size: 16),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Colour = Color4.White,
                        };
                    }
                }

                rows[rowIndex] = row;
            }

            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 2),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Children = rows.Select(row => (Drawable)new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 20,
                    Child = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(20, 0),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Children = row,
                    }
                }).ToArray(),
            };
        }

        public partial class DiffPiece : OsuTextFlowContainer
        {
            public DiffPiece(OsuColour colours, params (string heading, string content, Color4? contentColour)[] tuples)
                : this(tuples, null, colours)
            {
            }

            public DiffPiece(OsuColour colours, (string heading, string content, Color4? contentColour) tuple, string? uncolouredSuffix)
                : this(new[] { tuple }, uncolouredSuffix, colours)
            {
            }

            private DiffPiece((string heading, string content, Color4? contentColour)[] tuples, string? uncolouredSuffix, OsuColour colours)
            {
                Margin = new MarginPadding { Horizontal = 15, Vertical = 1 };
                AutoSizeAxes = Axes.Both;

                static void cp(SpriteText s, bool bold)
                {
                    s.Font = OsuFont.Torus.With(weight: bold ? FontWeight.Bold : FontWeight.Regular, size: 15);
                }

                for (int i = 0; i < tuples.Length; i++)
                {
                    (string heading, string content, Color4? contentColour) = tuples[i];

                    if (i > 0)
                    {
                        AddText(" / ", s =>
                        {
                            cp(s, false);
                            s.Spacing = new Vector2(-2, 0);
                        });
                    }

                    AddText(new TournamentSpriteText { Text = heading }, s => cp(s, false));
                    AddText(" ", s => cp(s, false));
                    AddText(new TournamentSpriteText { Text = content }, s =>
                    {
                        cp(s, true);

                        if (contentColour.HasValue)
                            s.Colour = contentColour.Value;
                    });

                    if (heading != "Length" && contentColour == colours.Red1)
                    {
                        AddArbitraryDrawable(new SpriteIcon
                        {
                            Icon = FontAwesome.Solid.CaretUp,
                            Size = new Vector2(8, 12),
                            Colour = colours.Red1,
                            Margin = new MarginPadding { Right = 2 },
                        });
                    }
                    else if (heading != "Length" && contentColour == colours.Green1)
                    {
                        AddArbitraryDrawable(new SpriteIcon
                        {
                            Icon = FontAwesome.Solid.CaretDown,
                            Size = new Vector2(8, 12),
                            Colour = colours.Green1,
                            Margin = new MarginPadding { Right = 2 },
                        });
                    }

                    if (i == tuples.Length - 1 && !string.IsNullOrEmpty(uncolouredSuffix))
                    {
                        AddText(uncolouredSuffix, s =>
                        {
                            cp(s, true);
                            s.Colour = Color4.White;
                        });
                    }
                }
            }
        }
    }
}
