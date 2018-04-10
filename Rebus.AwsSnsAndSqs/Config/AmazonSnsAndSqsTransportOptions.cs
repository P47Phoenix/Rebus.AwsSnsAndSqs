﻿using System;
using Amazon.Runtime;
using Amazon.SQS;
using Rebus.Bus;
using Rebus.Transport;

namespace Rebus.AwsSnsAndSqs.Config
{
    using Amazon;

    /// <summary>
    /// Holds all of the exposed options which can be applied when using the SQS transport.
    /// </summary>
    public class AmazonSnsAndSqsTransportOptions
    {
        /// <summary>
        /// Sets the WaitTimeSeconds on the ReceiveMessage. The default setting is 1, which enables long
        /// polling for a single second. The number of seconds can be set up to 20 seconds. 
        /// In case no long polling is desired, then set the value to 0.
        /// </summary>
        public int ReceiveWaitTimeSeconds { get; set; }

        /// <summary>
        /// Configures whether SQS's built-in deferred messages mechanism is to be used when you <see cref="IBus.Defer"/> messages.
        /// Defaults to <code>true</code>.
        /// Please note that SQS's mechanism is only capably of deferring messages up 900 seconds, so you might need to
        /// set <see cref="UseNativeDeferredMessages"/> to <code>false</code> and then use a "real" timeout manager like e.g.
        /// one that uses SQL Server to store timeouts.
        /// </summary>
        public bool UseNativeDeferredMessages { get; set; }
        


        /// <summary>
        /// Default constructor of the exposed SQS transport options.
        /// </summary>
        public AmazonSnsAndSqsTransportOptions()
        {
            ReceiveWaitTimeSeconds = 20;
            UseNativeDeferredMessages = true;
        }
    }
}
