using System;

namespace Rebus.AwsSnsAndSqs.RebusAmazon
{
    public class AmazonPeekLockDuration
    {
        private TimeSpan _PeekLockDuration = TimeSpan.FromMinutes(5);
        private TimeSpan _PeekLockRenewalInterval = TimeSpan.FromMinutes(4);

        public TimeSpan PeekLockDuration
        {
            get => _PeekLockDuration;
            set
            {
                _PeekLockDuration = value;
                _PeekLockRenewalInterval = TimeSpan.FromMinutes(_PeekLockDuration.TotalMinutes * 0.8);
            }
        }

        public TimeSpan PeekLockRenewalInterval => _PeekLockRenewalInterval;
    }
}
