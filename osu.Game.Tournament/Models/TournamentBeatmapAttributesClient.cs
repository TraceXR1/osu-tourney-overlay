// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;

namespace osu.Game.Tournament.Models
{
    public partial class TournamentBeatmapAttributesClient : Component
    {
        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        public async Task<double?> GetStarRatingAsync(int beatmapID, string ruleset, int mods, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request = new GetBeatmapAttributesRequest(beatmapID, ruleset, mods);

            try
            {
                Logger.Log($"Requesting modded star rating for beatmap {beatmapID}, ruleset {ruleset}, mods {mods}", LoggingTarget.Network);
                await api.PerformAsync(request).ConfigureAwait(false);

                if (request.CompletionState != APIRequestCompletionState.Completed)
                    throw new InvalidOperationException($"Beatmap attributes request completed with state {request.CompletionState}");
            }
            catch (Exception error)
            {
                Logger.Error(error, $"Failed to load beatmap attributes for beatmap {beatmapID} with mods {mods}");
                throw;
            }

            cancellationToken.ThrowIfCancellationRequested();
            double? starRating = request.Response?.Attributes?.StarRating;

            if (starRating.HasValue)
                Logger.Log($"Received modded star rating {starRating.Value} for beatmap {beatmapID}, mods {mods}", LoggingTarget.Network);
            else
                Logger.Log($"No modded star rating returned for beatmap {beatmapID}, mods {mods}", LoggingTarget.Network);

            return starRating;
        }
    }
}
