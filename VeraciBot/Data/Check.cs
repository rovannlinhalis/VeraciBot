using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VeraciBot
{ 

    public class Check
    {

        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Text { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;

        public int Result { get; set; } = 0;

        public string Rationale { get; set; } = string.Empty;

    }

}
