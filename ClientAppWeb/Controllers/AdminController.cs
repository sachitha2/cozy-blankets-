using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClientAppWeb.Data;

namespace ClientAppWeb.Controllers;

/// <summary>
/// Admin controller for viewing contact submissions
/// Accessible to Manufacturer, Distributor, and Seller roles
/// </summary>
[Authorize(Roles = "Manufacturer,Distributor,Seller")]
public class AdminController : Controller
{
    private readonly AppDbContext _dbContext;

    public AdminController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Display all contact form submissions
    /// </summary>
    public async Task<IActionResult> ContactSubmissions()
    {
        var submissions = await _dbContext.ContactSubmissions
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        return View(submissions);
    }
}
