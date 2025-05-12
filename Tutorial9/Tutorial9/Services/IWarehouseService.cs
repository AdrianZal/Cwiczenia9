using Tutorial9.Models;

namespace Tutorial9.Services;

public interface IWarehouseService
{
    Task<int> AddEntry(Entry entry);
    //Task ProcedureAsync();
}