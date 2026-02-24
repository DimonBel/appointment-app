using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Appointment.E2ETests.Fixtures;
using AppointmentApp.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Appointment.E2ETests.Scenarios;

/// <summary>
/// End-to-End tests for domain configuration journeys
/// Module 1.4: Domain Configuration Module
/// </summary>
[Collection("E2E Tests")]
public class DomainConfigurationJourneyTests : IAsyncLifetime
{
    private readonly E2ETestFixture _fixture;

    public DomainConfigurationJourneyTests()
    {
        _fixture = new E2ETestFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    [Fact]
    public async Task AdminCreatesDomainConfiguration_Success()
    {
        var admin = await _fixture.CreateAdminAsync();
        
        var medicalConfig = await _fixture.CreateDomainConfigurationAsync(
            DomainType.Medical,
            "General Medical Consultation");

        medicalConfig.Should().NotBeNull();
        medicalConfig.DomainType.Should().Be(DomainType.Medical);
        medicalConfig.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleDomainTypes_CreatedSuccessfully()
    {
        var admin = await _fixture.CreateAdminAsync();

        var medical = await _fixture.CreateDomainConfigurationAsync(DomainType.Medical, "Medical Service");
        var legal = await _fixture.CreateDomainConfigurationAsync(DomainType.Legal, "Legal Service");
        var consulting = await _fixture.CreateDomainConfigurationAsync(DomainType.Consulting, "Consulting Service");

        var context = await _fixture.GetDbContextAsync();
        var allConfigs = await context.DomainConfigurations.ToListAsync();

        allConfigs.Should().HaveCount(3);
        allConfigs.Should().Contain(c => c.DomainType == DomainType.Medical);
        allConfigs.Should().Contain(c => c.DomainType == DomainType.Legal);
        allConfigs.Should().Contain(c => c.DomainType == DomainType.Consulting);
    }

    [Fact]
    public async Task DeactivateDomainConfiguration_OnlyActiveShown()
    {
        var admin = await _fixture.CreateAdminAsync();
        var config = await _fixture.CreateDomainConfigurationAsync(DomainType.Medical, "Test Config");

        var context = await _fixture.GetDbContextAsync();
        config.IsActive = false;
        await context.SaveChangesAsync();

        var client = _fixture.CreateAuthenticatedClient(admin.Token);
        var response = await client.GetAsync("/api/domain-configurations?onlyActive=true");
        response.EnsureSuccessStatusCode();

        var activeConfigs = await response.Content.ReadFromJsonAsync<DomainConfiguration[]>();
        activeConfigs.Should().NotBeNull();
        activeConfigs!.Should().BeEmpty();
    }
}