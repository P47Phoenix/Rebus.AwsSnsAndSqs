# Aws Sns and Sqs Permisions
I will list the permissions needed by the rebus sns and sqs transport

## SNS

### Permisions used
* sns:CreateTopic
* sns:ListSubscriptions
* sns:ListSubscriptionsByTopic
* sns:ListTopics
* sns:Publish
* sns:Subscribe
* sns:Unsubscribe

### Policy
Sns policy template
```json
{
  "Id": "Policy1523639845348",
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "Stmt1523639840839",
      "Action": [
        "sns:CreateTopic",
        "sns:ListSubscriptions",
        "sns:ListSubscriptionsByTopic",
        "sns:ListTopics",
        "sns:Publish",
        "sns:Subscribe",
        "sns:Unsubscribe"
      ],
      "Effect": "Allow",
      "Resource": "arn:aws:sns:<region>:<account_ID>:<topic_name>",
      "Principal": "<YourIamRoleOrLtkUser>"
    }
  ]
}
```

If you prefix your topics then you can create a generic policy
The following example would give you access to any Lead topics
```json
{
  "Id": "Policy1523639845348",
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "Stmt1523639840839",
      "Action": [
        "sns:CreateTopic",
        "sns:ListSubscriptions",
        "sns:ListSubscriptionsByTopic",
        "sns:ListTopics",
        "sns:Publish",
        "sns:Subscribe",
        "sns:Unsubscribe"
      ],
      "Effect": "Allow",
      "Resource": "arn:aws:sns:<region>:<account_ID>:Lead-*",
      "Principal": "<YourIamRoleOrLtkUser>"
    }
  ]
}
```

## SQS

### Permisions used
* sqs:ChangeMessageVisibility
* sqs:ChangeMessageVisibilityBatch
* sqs:CreateQueue
* sqs:DeleteMessage
* sqs:DeleteMessageBatch
* sqs:GetQueueAttributes
* sqs:GetQueueUrl
* sqs:ListQueues
* sqs:ReceiveMessage
* sqs:SendMessage
* sqs:SendMessageBatch
* sqs:SetQueueAttributes


### Policy
Sns policy template
```json
{
  "Id": "Policy1523640379661",
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "Stmt1523640377200",
      "Action": [
        "sqs:ChangeMessageVisibility",
        "sqs:ChangeMessageVisibilityBatch",
        "sqs:CreateQueue",
        "sqs:DeleteMessage",
        "sqs:DeleteMessageBatch",
        "sqs:GetQueueAttributes",
        "sqs:GetQueueUrl",
        "sqs:ListQueues",
        "sqs:ReceiveMessage",
        "sqs:SendMessage",
        "sqs:SendMessageBatch",
        "sqs:SetQueueAttributes"
      ],
      "Effect": "Allow",
      "Resource": "arn:aws:sqs:<region>:<account_ID>:<queue_name>",
      "Principal": "<YourIamRoleOrLtkUser>"
    }
  ]
}
```

Prefix you queue names to simplify the policy.
The following example would allow you create queues prefixed for lead work
```json
{
  "Id": "Policy1523640379661",
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "Stmt1523640377200",
      "Action": [
        "sqs:ChangeMessageVisibility",
        "sqs:ChangeMessageVisibilityBatch",
        "sqs:CreateQueue",
        "sqs:DeleteMessage",
        "sqs:DeleteMessageBatch",
        "sqs:GetQueueAttributes",
        "sqs:GetQueueUrl",
        "sqs:ListQueues",
        "sqs:ReceiveMessage",
        "sqs:SendMessage",
        "sqs:SendMessageBatch",
        "sqs:SetQueueAttributes"
      ],
      "Effect": "Allow",
      "Resource": "arn:aws:sqs:<region>:<account_ID>:Lead-*",
      "Principal": "<YourIamRoleOrLtkUser>"
    }
  ]
}
```