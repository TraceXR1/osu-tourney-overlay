// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net.Http;
using Newtonsoft.Json;
using osu.Framework.IO.Network;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    public class GetBeatmapAttributesRequest : APIRequest<APIBeatmapAttributesResponse>
    {
        private readonly int beatmapID;
        private readonly int mods;
        private readonly string ruleset;

        public GetBeatmapAttributesRequest(int beatmapID, string ruleset, int mods)
        {
            this.beatmapID = beatmapID;
            this.ruleset = ruleset;
            this.mods = mods;
        }

        protected override string Target => $"beatmaps/{beatmapID}/attributes";

        protected override WebRequest CreateWebRequest()
        {
            var request = base.CreateWebRequest();
            request.Method = HttpMethod.Post;
            request.ContentType = "application/json";
            request.AddRaw(JsonConvert.SerializeObject(new RequestBody
            {
                Mods = mods,
                Ruleset = ruleset,
            }));
            return request;
        }

        private class RequestBody
        {
            [JsonProperty("mods")]
            public int Mods { get; set; }

            [JsonProperty("ruleset")]
            public string Ruleset { get; set; } = string.Empty;
        }
    }
}
