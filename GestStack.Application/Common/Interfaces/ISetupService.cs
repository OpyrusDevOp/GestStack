using GestStack.Application.Common.Models;

namespace GestStack.Application.Common.Interfaces;

public interface ISetupService
{
    Task<StatusResult> GetStatusAsync();
}
