// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Tournament.Models;
using osuTK;

namespace osu.Game.Tournament.Components
{
    public partial class DrawableTeamFlag : Container
    {
        private readonly TournamentTeam? team;

        [UsedImplicitly]
        private Bindable<string>? flag;

        private Sprite? flagSprite;

        public DrawableTeamFlag(TournamentTeam? team)
        {
            this.team = team;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures)
        {
            if (team == null) return;

            Size = new Vector2(54);
            Masking = true;
            CornerRadius = 5;
            Children = new Drawable[]
            {
                new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Masking = true,
                    CornerRadius = 5,
                    Children = new Drawable[]
                    {
                        flagSprite = new Sprite()
                    }
                },
            };

            (flag = team.FlagName.GetBoundCopy()).BindValueChanged(_ => updateFlag(textures.Get($@"Flags/{team.FlagName}")), true);
        }

        private void updateFlag(Texture? texture)
        {
            flagSprite!.Texture = texture;

            if (texture == null || texture.DisplayWidth <= 0 || texture.DisplayHeight <= 0)
                return;

            float scale = Math.Min(Size.X / texture.DisplayWidth, Size.Y / texture.DisplayHeight);
            flagSprite.Size = new Vector2(texture.DisplayWidth * scale, texture.DisplayHeight * scale);
        }
    }
}
