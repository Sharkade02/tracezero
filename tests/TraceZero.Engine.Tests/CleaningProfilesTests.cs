using TraceZero.Application.Automation;
using TraceZero.Domain;
using TraceZero.Domain.Automation;

namespace TraceZero.Engine.Tests;

public sealed class CleaningProfilesTests
{
    [Theory]
    [InlineData(CleaningProfile.Safe, RiskLevel.Safe, true)]
    [InlineData(CleaningProfile.Safe, RiskLevel.Privacy, false)]
    [InlineData(CleaningProfile.Safe, RiskLevel.Review, false)]
    [InlineData(CleaningProfile.Privacy, RiskLevel.Safe, true)]
    [InlineData(CleaningProfile.Privacy, RiskLevel.Privacy, true)]
    [InlineData(CleaningProfile.Privacy, RiskLevel.Review, false)]
    public void Profile_includes_expected_risk_levels(CleaningProfile profile, RiskLevel risk, bool expected)
    {
        // Aucun profil automatique n'inclut jamais REVIEW (§15).
        Assert.Equal(expected, CleaningProfiles.Includes(profile, risk));
    }
}
