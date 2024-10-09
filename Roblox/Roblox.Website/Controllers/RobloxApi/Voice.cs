using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Roblox.Services;
using Roblox.Website.WebsiteModels;
using Roblox.Dto.Avatar;
using Roblox.Exceptions;
using Roblox.Models.Avatar;
using Roblox.Services.App.FeatureFlags;
using ServiceProvider = Roblox.Services.ServiceProvider;
#pragma warning disable CS8600
namespace Roblox.Website.Controllers;

[Route("/")]
public class Voice : ControllerBase
{
    [HttpGetBypass("v1/settings")]
    public dynamic VoiceSettingsGlobal()
    {
        return new
        {
            IsVoiceEnabled = true,
            IsUserOptIn = true,
            IsUserEligible = true,
            IsBanned = false,
            CanVerifyAgeForVoice = true,
            IsVerifiedForVoice = true,
            DenialReason = 0,
            IsOptInDisabled = true,
            HasEverOpted = true,
            IsAvatarVideoEnabled = true,
            IsAvatarVideoOptIn = true,
            IsAvatarVideoOptInDisabled = true,
            IsAvatarVideoEligible = true,
            HasEverOptedAvatarVideo = true
        };
    }

    [HttpGetBypass("v1/settings/universe/{universeId:long}")]
    public dynamic VoiceSettingsUniverse(long universeId)
    {
        return new
        {
            isUniverseEnabledForVoice = true,
            isPlaceEnabledForVoice = true,
            isUniverseEnabledForAvatarVideo = true,
            isPlaceEnabledForAvatarVideo = true
        };
    }
}