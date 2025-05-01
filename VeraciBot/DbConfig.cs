using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VeraciBot.Data;

namespace VeraciBot
{

    public class DbConfig
    {

        public static async Task<DateTime> GetLastDateTimeForTwitterCheck(VeraciDbContext dbContext)
        {

            Config lastCheck = await dbContext.Configs.FirstOrDefaultAsync(e => e.Id == "TWIT_last_check");
            if (lastCheck == null)
            {
                lastCheck = new Config()
                {
                    Id = "TWIT_last_check",
                    Value = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                dbContext.Configs.Add(lastCheck);
                dbContext.SaveChanges();
            }

            return DateTime.Parse(lastCheck.Value);

        }

        public static async Task SetLastDateTimeForTwitterCheck(VeraciDbContext dbContext, DateTime last)
        {

            Config lastCheck = await dbContext.Configs.FirstOrDefaultAsync(e => e.Id == "TWIT_last_check");
            if (lastCheck == null)
            {
                
                lastCheck = new Config()
                {
                    Id = "TWIT_last_check",
                    Value = last.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                dbContext.Configs.Add(lastCheck);
                dbContext.SaveChanges();

            }
            else
            {

                lastCheck.Value = last.ToString("yyyy-MM-ddTHH:mm:ssZ");
                dbContext.Configs.Update(lastCheck);
                dbContext.SaveChanges();

            }
                            
        }

    }

}
