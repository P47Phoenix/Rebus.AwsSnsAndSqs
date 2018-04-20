# Load test data

Aws rebus sns and sqs load test results.
Test were run without compression or encryption.
Added different message sizes to show performance difference.
As always with tcp you want to keep your packet size below 64k.
Keeping your messages below 32k will keep headers, etc from bumping your message over 64k.
It will also help you keep your aws costs down as going over 64k will double your cost per message

Test | max # concurrent publishes | Publish per second | Receive # of workers | Receive max parallelism | # msgs Receive per second
-- | -- | -- | -- | -- | --
send msgs at 4 kilobytes in size | 200 | 160.265903678306 | 8 | 200 | 154.811530153061
send msgs at 16 kilobytes in size | 200 | 70.589457160458 | 8 | 200 | 67.8350059786644
send msgs at 32 kilobytes in size | 200 | 69.2408535473752 | 8 | 200 | 68.0220606651901
send msgs at 64 kilobytes in size | 200 | 65.5018936877669 | 8 | 200 | 63.9549052025787
send msgs at 128 kilobytes in size | 200 | 37.1433978514032 | 8 | 200 | 35.9827981479999
