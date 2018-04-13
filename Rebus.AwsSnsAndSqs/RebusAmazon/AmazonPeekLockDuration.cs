using System;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    public class AmazonPeekLockDuration
    {
        private TimeSpan _PeekLockDuration = TimeSpan.FromMinutes(5);

        public TimeSpan PeekLockDuration
        {
            get => _PeekLockDuration;
            set
            {
                _PeekLockDuration = value;
                PeekLockRenewalInterval = TimeSpan.FromMinutes(_PeekLockDuration.TotalMinutes * 0.8);
            }
        }

        public TimeSpan PeekLockRenewalInterval { get; private set; } = TimeSpan.FromMinutes(4);
    }
}
