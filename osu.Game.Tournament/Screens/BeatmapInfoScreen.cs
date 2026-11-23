// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Tournament.Components;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Screens
{
    public abstract partial class BeatmapInfoScreen : TournamentMatchScreen
    {
        protected readonly SongBar SongBar;
        private MatchIPCInfo? ipc;
        private bool ipcBound;

        protected BeatmapInfoScreen()
        {
            AddInternal(SongBar = new SongBar
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Depth = float.MinValue,
            });
        }

        [BackgroundDependencyLoader]
        private void load(MatchIPCInfo ipc)
        {
            this.ipc = ipc;
        }

        public override void Show()
        {
            base.Show();

            if (ipc == null || ipcBound)
                return;

            ipc.Beatmap.BindValueChanged(beatmapChanged, true);
            ipc.Mods.BindValueChanged(modsChanged, true);
            ipcBound = true;
        }

        public override void Hide()
        {
            if (ipc != null && ipcBound)
            {
                ipc.Beatmap.ValueChanged -= beatmapChanged;
                ipc.Mods.ValueChanged -= modsChanged;
                ipcBound = false;
            }

            base.Hide();
        }

        private void modsChanged(ValueChangedEvent<LegacyMods> mods)
        {
            SongBar.Mods = mods.NewValue;
        }

        private void beatmapChanged(ValueChangedEvent<TournamentBeatmap?> beatmap)
        {
            SongBar.FadeInFromZero(300, Easing.OutQuint);
            SongBar.Beatmap = beatmap.NewValue;
        }
    }
}
