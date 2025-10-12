using Domain.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IPaymentRepository
    {
        Task<IEnumerable<Payment>> GetByEmployerIdAsync(string employerId);
        Task<IEnumerable<Payment>> GetByStudentIdAsync(string studentId);
        Task<Payment> GetByJobIdAsync(string jobId);
    }
}
