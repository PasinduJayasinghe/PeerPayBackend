using Domain.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IJobRepository
    {
        Task<IEnumerable<Job>> GetActiveJobsAsync();
        Task<IEnumerable<Job>> GetJobsByEmployerAsync(string employerId);
        Task<IEnumerable<Job>> GetJobsByCategoryAsync(string categoryId);
    }
}
