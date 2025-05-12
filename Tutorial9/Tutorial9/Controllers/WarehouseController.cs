using Microsoft.AspNetCore.Mvc;
using Tutorial9.Models;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WarehouseController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;
    public WarehouseController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }
    
    [HttpPost]
    public async Task<IActionResult> AddEntry([FromBody] Entry entry)
    {
        var id = await _warehouseService.AddEntry(entry);
        if (id == null) return BadRequest();
        return Ok(id);
    }
}