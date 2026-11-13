// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Tournament.Models;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Tournament.Screens.Gameplay.Components
{
    public partial class TeamScore : CompositeDrawable
    {
        private readonly Bindable<int?> currentTeamScore = new Bindable<int?>();
        private readonly StarCounter counter;

        public TeamScore(Bindable<int?> score, Bindable<Colour4> teamColour, TeamColour colour, int count)
        {
            bool flip = colour == TeamColour.Blue;
            var anchor = flip ? Anchor.TopRight : Anchor.TopLeft;

            AutoSizeAxes = Axes.Both;

            InternalChild = counter = new TeamScoreStarCounter(count, teamColour)
            {
                Anchor = anchor,
                Scale = flip ? new Vector2(-1, 1) : Vector2.One,
            };

            currentTeamScore.BindValueChanged(scoreChanged);
            currentTeamScore.BindTo(score);
        }

        private void scoreChanged(ValueChangedEvent<int?> score) => counter.Current = score.NewValue ?? 0;

        public partial class TeamScoreStarCounter : StarCounter
        {
            public TeamScoreStarCounter(int count, Bindable<Colour4> teamColour)
                : base(count)
            {
                foreach (var star in ((FillFlowContainer<Star>)Children[0]).Children)
                {
                    if (star is LightSquare square)
                        square.BindTeamColour(teamColour);
                }
            }

            public override Star CreateStar() => new LightSquare();

            public partial class LightSquare : Star
            {
                private readonly Box box;

                public void BindTeamColour(Bindable<Colour4> teamColour)
                {
                    teamColour.BindValueChanged(colour =>
                    {
                        var hsl = colour.NewValue.ToHSL();
                        float saturation = hsl.Y;
                        float lightness = Math.Clamp(hsl.Z + 0.3f, 0.68f, 0.82f);
                        var squareColour = Colour4.FromHSL(hsl.X, saturation, lightness);
                        box.Colour = squareColour;
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Glow,
                            Colour = Colour4.FromHSL(hsl.X, saturation, Math.Min(lightness, 0.62f)).Opacity(0.28f),
                            Hollow = true,
                            Radius = 20,
                            Roundness = 10,
                        };
                    }, true);
                }

                public LightSquare()
                {
                    Size = new Vector2(22.5f);

                    InternalChildren = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            BorderColour = OsuColour.Gray(0.5f),
                            BorderThickness = 3,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Colour = Color4.Transparent,
                                    RelativeSizeAxes = Axes.Both,
                                    AlwaysPresent = true,
                                },
                            }
                        },
                        box = new Box
                        {
                            Colour = Color4Extensions.FromHex("#FFE8AD"),
                            RelativeSizeAxes = Axes.Both,
                        },
                    };

                    Masking = true;
                    EdgeEffect = new EdgeEffectParameters { Type = EdgeEffectType.Glow, Hollow = true, Radius = 20, Roundness = 10 };
                }

                public override void DisplayAt(float scale)
                {
                    box.FadeTo(scale, 500, Easing.OutQuint);
                    FadeEdgeEffectTo(0.2f * scale, 500, Easing.OutQuint);
                }
            }
        }
    }
}
