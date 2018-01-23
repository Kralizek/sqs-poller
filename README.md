# SQS Poller
SQS Poller is an AWS Lambda function that fetches a message from any specified queue and passes the message body as paylod to any specified Lambda function.

## IAM Role
Despite being designed to pull from any queue and invoke any function, SQS Poller needs to be provided with a role that allows it. The IAM policy written below grants the Lambda hosting SQS Poller access to all SQS queues and the possibility to invoke any AWS Lambda.

If you don't need such flexibility, you can change the policy below to reflect better your needs.
```
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Action": [
        "sqs:DeleteMessage",
        "sqs:ReceiveMessage"
      ],
      "Effect": "Allow",
      "Resource": "*"
    },
    {
      "Action": [
        "lambda:InvokeAsync",
        "lambda:InvokeFunction"
      ],
      "Effect": "Allow",
      "Resource": "*"
    }
  ]
}
```