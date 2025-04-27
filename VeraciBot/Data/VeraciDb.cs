using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VeraciBot
{

    public class VeraciDb : DbContext
    {

        public VeraciDb(DbContextOptions<VeraciDb> options)
            : base(options)
        {
        }

        public DbSet<Check> Checks { get; set; } = default!;

    }

}
