using Microsoft.EntityFrameworkCore;
using Mithya.Core.Entities;
using Mithya.Core.Interfaces;
using Mithya.Infrastructure.Data;

namespace Mithya.Infrastructure.Repositories;

public class ScenarioRepository : IScenarioRepository
{
    private readonly MithyaDbContext _context;

    public ScenarioRepository(MithyaDbContext context)
    {
        _context = context;
    }

    public async Task<Scenario?> GetByIdAsync(Guid id)
    {
        return await _context.Scenarios.FindAsync(id);
    }

    public async Task<Scenario?> GetByIdWithStepsAsync(Guid id)
    {
        return await _context.Scenarios
            .Include(s => s.Steps)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Scenario>> GetAllAsync()
    {
        return await _context.Scenarios
            .Include(s => s.Steps)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Scenario>> GetActiveWithStepsAsync()
    {
        return await _context.Scenarios
            .Where(s => s.IsActive)
            .Include(s => s.Steps)
            .ToListAsync();
    }

    public async Task AddAsync(Scenario scenario)
    {
        scenario.Id = Guid.NewGuid();
        scenario.CreatedAt = DateTime.UtcNow;
        scenario.UpdatedAt = DateTime.UtcNow;
        await _context.Scenarios.AddAsync(scenario);
    }

    public async Task UpdateAsync(Scenario scenario)
    {
        scenario.UpdatedAt = DateTime.UtcNow;
        _context.Scenarios.Update(scenario);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var scenario = await _context.Scenarios.FindAsync(id);
        if (scenario != null)
            _context.Scenarios.Remove(scenario);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}

public class ScenarioStepRepository : IScenarioStepRepository
{
    private readonly MithyaDbContext _context;

    public ScenarioStepRepository(MithyaDbContext context)
    {
        _context = context;
    }

    public async Task<ScenarioStep?> GetByIdAsync(Guid id)
    {
        return await _context.ScenarioSteps.FindAsync(id);
    }

    public async Task<IEnumerable<ScenarioStep>> GetByScenarioIdAsync(Guid scenarioId)
    {
        return await _context.ScenarioSteps
            .Where(s => s.ScenarioId == scenarioId)
            .OrderBy(s => s.Priority)
            .ToListAsync();
    }

    public async Task AddAsync(ScenarioStep step)
    {
        step.Id = Guid.NewGuid();
        await _context.ScenarioSteps.AddAsync(step);
    }

    public async Task UpdateAsync(ScenarioStep step)
    {
        _context.ScenarioSteps.Update(step);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var step = await _context.ScenarioSteps.FindAsync(id);
        if (step != null)
            _context.ScenarioSteps.Remove(step);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
