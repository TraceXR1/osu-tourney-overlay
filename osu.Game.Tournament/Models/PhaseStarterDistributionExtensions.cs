// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Tournament.Models
{
    public static class PhaseStarterDistributionExtensions
    {
        public static int GetStarterTeamNumber(this PhaseStarterDistributionType distribution, ChoiceType choiceType)
        {
            switch (distribution)
            {
                case PhaseStarterDistributionType.AllTogether:
                    switch (choiceType)
                    {
                        case ChoiceType.Protect:
                        case ChoiceType.Ban:
                        case ChoiceType.Pick:
                            return 1;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(choiceType), choiceType, null);
                    }

                case PhaseStarterDistributionType.ProtectBanTogether:
                    switch (choiceType)
                    {
                        case ChoiceType.Protect:
                        case ChoiceType.Ban:
                            return 1;
                        case ChoiceType.Pick:
                            return 2;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(choiceType), choiceType, null);
                    }

                case PhaseStarterDistributionType.ProtectPickTogether:
                    switch (choiceType)
                    {
                        case ChoiceType.Protect:
                        case ChoiceType.Pick:
                            return 1;
                        case ChoiceType.Ban:
                            return 2;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(choiceType), choiceType, null);
                    }

                case PhaseStarterDistributionType.BanPickTogether:
                    switch (choiceType)
                    {
                        case ChoiceType.Protect:
                            return 1;
                        case ChoiceType.Ban:
                        case ChoiceType.Pick:
                            return 2;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(choiceType), choiceType, null);
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(distribution), distribution, null);
            }
        }
    }
}
