using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Veracibot.API.Data;
using Veracibot.API.Models;

namespace Veracibot.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class VeracibotController : ControllerBase
    {
        private readonly VeracibotDbContext veracibotDbContext;
        public VeracibotController(VeracibotDbContext veracibotDbContext)
        {
            this.veracibotDbContext = veracibotDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok("Veracibot API is running.");
        }


        [HttpGet("balance/{authorId}")]
        public async Task<ActionResult<IEnumerable<AuthorBalance>>> GetAuthorBalance(string authorId)
        {
            var data = await veracibotDbContext.AuthorBalances
                .Where(x => x.AuthorId == authorId)
                .ToListAsync();

            return Ok(data);
        }




    }
}
