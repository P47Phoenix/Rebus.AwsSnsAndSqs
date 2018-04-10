﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Rebus.AwsSnsAndSqs.Amazon.Extensions
{
    internal static class AmazonWebServiceResponseExtensions
    {
        public static AmazonWebServiceException CreateAmazonExceptionFromResponse(this AmazonWebServiceResponse amazonWebServiceResponse, Exception exception = null)
        {
            var jArray = JArray.FromObject(amazonWebServiceResponse.ResponseMetadata.Metadata.ToList());

            if (exception == null)
            {
                return new AmazonWebServiceException(
                    $"Http status code was {amazonWebServiceResponse.HttpStatusCode}. Reponse details: {jArray.ToString(Formatting.Indented)}");
            }

            return new AmazonWebServiceException(
                $"Http status code was {amazonWebServiceResponse.HttpStatusCode}. Reponse details: {jArray.ToString(Formatting.Indented)}", exception);
        }
    }
}
