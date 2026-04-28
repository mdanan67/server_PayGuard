using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server.Data;
using server.model;

namespace server.controller;

[ApiController]
[Route("api/[controller]")]
public class ParentController : ControllerBase
{
    private readonly AppDBContext _context;

    public ParentController(AppDBContext context)
    {
        _context = context;
    }

    [HttpPost("add-balance")]
    public async Task<IActionResult> AddBalance(Guid userId, decimal amount)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound("User not found");

        var parentBalance = await _context.ParentBalances
            .FirstOrDefaultAsync(pb => pb.UserId == userId);

        if (parentBalance == null)
        {
            parentBalance = new ParentBalance
            {
                UserId = userId,
                TotalDeposited = amount,
                CurrentBalance = amount,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ParentBalances.Add(parentBalance);
        }
        else
        {
            parentBalance.TotalDeposited += amount;
            parentBalance.CurrentBalance += amount;
            parentBalance.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(parentBalance);
    }
}