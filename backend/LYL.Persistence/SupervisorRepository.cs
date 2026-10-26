using System;
using LYL.Domain.Model;
using LYL.Persistence.Interfaces;
using System.Collections.Concurrent;

namespace LYL.Persistence;

public class SupervisorRepository : ISupervisorRepository
{

    private readonly ConcurrentDictionary<Guid, Supervisor> supervisors = new();
    public Task<Supervisor?> GetByIdAsync(Guid supervisorId)
    {
        // Add one in memory fake supervisor for test operations
        var supervisor = supervisors.GetOrAdd(supervisorId, id => new Supervisor
        {
            SupervisorId = id,
            Email = "docent@mail.com"
        });

        return Task.FromResult<Supervisor?>(supervisor);
    }
}
