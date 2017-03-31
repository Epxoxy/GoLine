using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServices
{
    internal class CustomContract
    {
        public static void Requires<TException>(bool predicate, string message)
            where TException : Exception, new()
        {
            if (!predicate)
            {
                System.Diagnostics.Debug.WriteLine(message);
                throw new TException();
            }
        }
    }
}
