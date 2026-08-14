using DigitalFootprintAuditor.Application.Abstractions;
using DigitalFootprintAuditor.Application.Models;
using DigitalFootprintAuditor.Domain.Entities;
using DigitalFootprintAuditor.Domain.Enums;
using DigitalFootprintAuditor.Infrastructure.Scanners;
using Moq;

namespace DigitalFootprintAuditor.UnitTests.Scanners;

public class GravatarScannerTests
{
    [Fact]
    public async Task ScanAsync_ShouldReturnProfileFoundFinding_WhenProfileExists()
    {
        var clientMock = new Mock<IGravatarClient>();

        clientMock
            .Setup(client => client.GetProfileAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GravatarProfileResult(
                "https://gravatar.com/example",
                "Example User",
                "example"));

        var scanner = new GravatarScanner(clientMock.Object);
        var target = CreateEmailTarget();

        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        var finding = Assert.Single(findings);

        Assert.Equal("Gravatar Profili Bulundu", finding.Title);
        Assert.Equal(FindingSeverity.Info, finding.Severity);
        Assert.Equal(nameof(GravatarScanner), finding.ScannerName);
        Assert.Equal(0, finding.ScoreImpact);
        Assert.Contains("risk puanını artırmaz", finding.Description);
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnNotFoundFinding_WhenProfileDoesNotExist()
    {
        var clientMock = new Mock<IGravatarClient>();

        clientMock
            .Setup(client => client.GetProfileAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((GravatarProfileResult?)null);

        var scanner = new GravatarScanner(clientMock.Object);
        var target = CreateEmailTarget();

        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        var finding = Assert.Single(findings);

        Assert.Equal("Gravatar Profili Bulunamadı", finding.Title);
        Assert.Equal(FindingSeverity.Info, finding.Severity);
        Assert.Equal(0, finding.ScoreImpact);
        Assert.Contains("risk puanını artırmaz", finding.Description);
    }

    [Fact]
    public async Task ScanAsync_ShouldNotExposeRawEmail_WhenFindingIsCreated()
    {
        const string email = "user@example.com";

        var clientMock = new Mock<IGravatarClient>();

        clientMock
            .Setup(client => client.GetProfileAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GravatarProfileResult(
                "https://gravatar.com/example",
                "Example User",
                "example"));

        var scanner = new GravatarScanner(clientMock.Object);
        var target = CreateEmailTarget(email);

        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        var finding = Assert.Single(findings);

        Assert.DoesNotContain(email, finding.Title);
        Assert.DoesNotContain(email, finding.Description);
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnErrorFinding_WhenClientThrowsHttpRequestException()
    {
        var clientMock = new Mock<IGravatarClient>();

        clientMock
            .Setup(client => client.GetProfileAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException());

        var scanner = new GravatarScanner(clientMock.Object);
        var target = CreateEmailTarget();

        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        var finding = Assert.Single(findings);

        Assert.Equal(
            "Gravatar API'ye Ulaşılamadı",
            finding.Title);
        Assert.Equal(FindingSeverity.Low, finding.Severity);
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnTimeoutFinding_WhenClientTimesOut()
    {
        var clientMock = new Mock<IGravatarClient>();

        clientMock
            .Setup(client => client.GetProfileAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException());

        var scanner = new GravatarScanner(clientMock.Object);
        var target = CreateEmailTarget();

        var findings = await scanner.ScanAsync(
            target,
            CancellationToken.None);

        var finding = Assert.Single(findings);

        Assert.Equal(
            "Gravatar API Zaman Aşımına Uğradı",
            finding.Title);
        Assert.Equal(FindingSeverity.Low, finding.Severity);
    }

    [Fact]
    public async Task ScanAsync_ShouldThrowArgumentException_WhenTargetIsNotEmail()
    {
        var clientMock = new Mock<IGravatarClient>();
        var scanner = new GravatarScanner(clientMock.Object);

        var target = new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.GitHubUsername,
            TargetValue = "octocat"
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => scanner.ScanAsync(
                target,
                CancellationToken.None));
    }

    private static ScanTarget CreateEmailTarget(
        string email = "user@example.com")
    {
        return new ScanTarget
        {
            ScanId = Guid.NewGuid(),
            TargetType = TargetType.Email,
            TargetValue = email
        };
    }
}
