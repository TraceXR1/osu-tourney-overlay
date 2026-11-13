// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;

namespace osu.Game.Tournament.Models
{
    public partial class TournamentBeatmapAttributesCache : MemoryCachingComponent<TournamentBeatmapAttributesCache.Lookup, double?>
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        protected override bool CacheNullValues => false;

        public Task<double?> GetStarRatingAsync(int beatmapID, string ruleset, string[] mods, CancellationToken cancellationToken = default)
        {
            var lookup = new Lookup(beatmapID, ruleset, string.Join(",", mods.OrderBy(mod => mod, StringComparer.Ordinal)));
            return GetAsync(lookup, cancellationToken);
        }

        protected override async Task<double?> ComputeValueAsync(Lookup lookup, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = new GetBeatmapAttributesRequest(lookup.BeatmapID, lookup.Ruleset, lookup.Mods.Split(',', StringSplitOptions.RemoveEmptyEntries));
            await api.PerformAsync(request).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            return request.Response?.Attributes?.StarRating;
        }

        public readonly record struct Lookup(int BeatmapID, string Ruleset, string Mods);
    }
}
