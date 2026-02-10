using System.ComponentModel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mithya.Core.Entities;
using Mithya.Infrastructure.Data;
using Mithya.Infrastructure.Repositories;
using Xunit;

namespace Mithya.Tests.Unit;

public class ScenarioRepositoryTests
{
    private MithyaDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MithyaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MithyaDbContext(options);
    }

    [Fact]
    [DisplayName("新增 Scenario 應該成功")]
    public async Task AddAsync_ShouldAddScenario()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ScenarioRepository(context);
        var scenario = new Scenario
        {
            Name = "Login Flow",
            Description = "Test login scenario",
            InitialState = "logged_out",
            CurrentState = "logged_out",
            IsActive = true
        };

        // Act
        await repository.AddAsync(scenario);
        await repository.SaveChangesAsync();

        // Assert
        scenario.Id.Should().NotBe(Guid.Empty);
        scenario.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    [DisplayName("根據 ID 取得 Scenario 應返回資料")]
    public async Task GetByIdAsync_ShouldReturnScenario()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ScenarioRepository(context);
        var scenario = new Scenario
        {
            Name = "Test Scenario",
            InitialState = "start",
            CurrentState = "start",
            IsActive = true
        };
        await repository.AddAsync(scenario);
        await repository.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(scenario.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Scenario");
    }

    [Fact]
    [DisplayName("取得所有 Scenario 應按建立時間倒序")]
    public async Task GetAllAsync_ShouldReturnOrderedByCreatedAtDesc()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ScenarioRepository(context);

        await repository.AddAsync(new Scenario
        {
            Name = "First",
            InitialState = "start",
            CurrentState = "start",
            IsActive = true
        });
        await repository.SaveChangesAsync();

        await Task.Delay(10);

        await repository.AddAsync(new Scenario
        {
            Name = "Second",
            InitialState = "start",
            CurrentState = "start",
            IsActive = true
        });
        await repository.SaveChangesAsync();

        // Act
        var scenarios = (await repository.GetAllAsync()).ToList();

        // Assert
        scenarios.Should().HaveCount(2);
        scenarios[0].Name.Should().Be("Second");
    }

    [Fact]
    [DisplayName("取得啟用的 Scenario 應只返回 IsActive")]
    public async Task GetActiveWithStepsAsync_ShouldReturnOnlyActive()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ScenarioRepository(context);

        await repository.AddAsync(new Scenario
        {
            Name = "Active",
            InitialState = "start",
            CurrentState = "start",
            IsActive = true
        });
        await repository.AddAsync(new Scenario
        {
            Name = "Inactive",
            InitialState = "start",
            CurrentState = "start",
            IsActive = false
        });
        await repository.SaveChangesAsync();

        // Act
        var scenarios = (await repository.GetActiveWithStepsAsync()).ToList();

        // Assert
        scenarios.Should().HaveCount(1);
        scenarios[0].Name.Should().Be("Active");
    }

    [Fact]
    [DisplayName("更新 Scenario 應成功並更新 UpdatedAt")]
    public async Task UpdateAsync_ShouldUpdateScenario()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ScenarioRepository(context);
        var scenario = new Scenario
        {
            Name = "Original",
            InitialState = "start",
            CurrentState = "start",
            IsActive = true
        };
        await repository.AddAsync(scenario);
        await repository.SaveChangesAsync();

        // Act
        scenario.Name = "Updated";
        await repository.UpdateAsync(scenario);
        await repository.SaveChangesAsync();

        // Assert
        var updated = await repository.GetByIdAsync(scenario.Id);
        updated!.Name.Should().Be("Updated");
        updated.UpdatedAt.Should().BeAfter(updated.CreatedAt);
    }

    [Fact]
    [DisplayName("刪除 Scenario 應成功")]
    public async Task DeleteAsync_ShouldRemoveScenario()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new ScenarioRepository(context);
        var scenario = new Scenario
        {
            Name = "To Delete",
            InitialState = "start",
            CurrentState = "start",
            IsActive = true
        };
        await repository.AddAsync(scenario);
        await repository.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(scenario.Id);
        await repository.SaveChangesAsync();

        // Assert
        var result = await repository.GetByIdAsync(scenario.Id);
        result.Should().BeNull();
    }
}

public class ScenarioStepRepositoryTests
{
    private MithyaDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<MithyaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MithyaDbContext(options);
    }

    private async Task<Scenario> CreateTestScenario(MithyaDbContext context)
    {
        var repo = new ScenarioRepository(context);
        var scenario = new Scenario
        {
            Name = "Test Scenario",
            InitialState = "start",
            CurrentState = "start",
            IsActive = true
        };
        await repo.AddAsync(scenario);
        await repo.SaveChangesAsync();
        return scenario;
    }

    [Fact]
    [DisplayName("新增 ScenarioStep 應該成功")]
    public async Task AddAsync_ShouldAddStep()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var scenario = await CreateTestScenario(context);
        var repository = new ScenarioStepRepository(context);
        var step = new ScenarioStep
        {
            ScenarioId = scenario.Id,
            StateName = "start",
            NextState = "step2",
            ResponseStatusCode = 200,
            ResponseBody = "{}",
            Priority = 1
        };

        // Act
        await repository.AddAsync(step);
        await repository.SaveChangesAsync();

        // Assert
        step.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    [DisplayName("根據 ScenarioId 取得 Steps 應按 Priority 排序")]
    public async Task GetByScenarioIdAsync_ShouldReturnOrderedByPriority()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var scenario = await CreateTestScenario(context);
        var repository = new ScenarioStepRepository(context);

        await repository.AddAsync(new ScenarioStep
        {
            ScenarioId = scenario.Id,
            StateName = "start",
            NextState = "step2",
            ResponseStatusCode = 200,
            ResponseBody = "{}",
            Priority = 10
        });
        await repository.AddAsync(new ScenarioStep
        {
            ScenarioId = scenario.Id,
            StateName = "start",
            NextState = "step3",
            ResponseStatusCode = 200,
            ResponseBody = "{}",
            Priority = 1
        });
        await repository.SaveChangesAsync();

        // Act
        var steps = (await repository.GetByScenarioIdAsync(scenario.Id)).ToList();

        // Assert
        steps.Should().HaveCount(2);
        steps[0].Priority.Should().Be(1);
        steps[1].Priority.Should().Be(10);
    }

    [Fact]
    [DisplayName("刪除 ScenarioStep 應成功")]
    public async Task DeleteAsync_ShouldRemoveStep()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var scenario = await CreateTestScenario(context);
        var repository = new ScenarioStepRepository(context);
        var step = new ScenarioStep
        {
            ScenarioId = scenario.Id,
            StateName = "start",
            NextState = "end",
            ResponseStatusCode = 200,
            ResponseBody = "{}",
            Priority = 1
        };
        await repository.AddAsync(step);
        await repository.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(step.Id);
        await repository.SaveChangesAsync();

        // Assert
        var result = await repository.GetByIdAsync(step.Id);
        result.Should().BeNull();
    }
}
