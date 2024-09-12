using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DowngradVPN.Models
{
    public class Account : DomainObject
    {
        public User AcoountHolder { get; set; }
    }
}
