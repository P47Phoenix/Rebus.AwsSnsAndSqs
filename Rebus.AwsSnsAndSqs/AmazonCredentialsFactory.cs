using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rebus.AwsSnsAndSqs
{
    using global::Amazon.Runtime;

    public interface IAmazonCredentialsFactory
    {
        AWSCredentials Create();
    }
}
