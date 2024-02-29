using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniReply.Utils
{
    public class Exceptions
    {
        public class SandBoxTimeoutException : Exception
        {
            public SandBoxTimeoutException() : base("Timeout!")
            {
            }
        }
    }
}
