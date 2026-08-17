using TraceZero.Domain.Ntfs;
using TraceZero.Engine.Ntfs;

namespace TraceZero.Engine.Tests;

public sealed class NtfsAnalyzerTests
{
    [Fact]
    public void Analyze_returns_explained_artifacts()
    {
        var artifacts = new NtfsAnalyzer().Analyze();

        Assert.NotEmpty(artifacts);
        foreach (var artifact in artifacts)
        {
            Assert.False(string.IsNullOrWhiteSpace(artifact.Name));
            Assert.False(string.IsNullOrWhiteSpace(artifact.Explanation));
            Assert.False(string.IsNullOrWhiteSpace(artifact.Why));
        }
    }

    [Fact]
    public void Mft_and_logfile_are_detected_only_never_cleanable()
    {
        var artifacts = new NtfsAnalyzer().Analyze();

        var mft = artifacts.Single(a => a.Id == "ntfs.mft");
        Assert.Equal(NtfsArtifactStatus.DetectedOnly, mft.Status);

        var usn = artifacts.Single(a => a.Id == "ntfs.usn");
        Assert.Equal(NtfsArtifactStatus.ManagedByWindows, usn.Status);

        // Aucun artefact n'est présenté comme directement « nettoyable » : seul l'espace libre est atténuable.
        Assert.All(
            artifacts.Where(a => a.Status == NtfsArtifactStatus.MitigableByFreeSpaceWipe),
            a => Assert.StartsWith("ntfs.freespace.", a.Id));
    }
}
