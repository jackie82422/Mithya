using MockServer.Core.Entities;

namespace MockServer.Core.Interfaces;

public interface IScenarioRepository
{
    Task<Scenario?> GetByIdAsync(Guid id);
    Task<Scenario?> GetByIdWithStepsAsync(Guid id);
    Task<IEnumerable<Scenario>> GetAllAsync();
    Task<IEnumerable<Scenario>> GetActiveWithStepsAsync();
    Task AddAsync(Scenario scenario);
    Task UpdateAsync(Scenario scenario);
    Task DeleteAsync(Guid id);
    Task<bool> SaveChangesAsync();
}

public interface IScenarioStepRepository
{
    Task<ScenarioStep?> GetByIdAsync(Guid id);
    Task<IEnumerable<ScenarioStep>> GetByScenarioIdAsync(Guid scenarioId);
    Task AddAsync(ScenarioStep step);
    Task UpdateAsync(ScenarioStep step);
    Task DeleteAsync(Guid id);
    Task<bool> SaveChangesAsync();
}
