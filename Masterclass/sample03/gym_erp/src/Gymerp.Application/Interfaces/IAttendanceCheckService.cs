using System.Threading.Tasks;

namespace Gymerp.Application.Interfaces
{
    public interface IAttendanceCheckService
    {
        /// <summary>
        /// Verifica a presença dos alunos em aulas agendadas para a data atual
        /// e gera multas para ausências não justificadas.
        /// </summary>
        Task CheckTodayAttendancesAsync();
    }
} 