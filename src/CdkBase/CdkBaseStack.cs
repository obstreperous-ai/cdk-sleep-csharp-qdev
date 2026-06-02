using Amazon.CDK;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.KMS;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace CdkBase
{
    public class CdkBaseStack : Stack
    {
        /// <summary>
        /// Input S3 bucket for raw audio files and text uploads.
        /// </summary>
        public IBucket InputBucket { get; }

        /// <summary>
        /// Output S3 bucket for processed audio files.
        /// </summary>
        public IBucket OutputBucket { get; }

        /// <summary>
        /// KMS key for encrypting S3 bucket contents.
        /// </summary>
        public IKey EncryptionKey { get; }

        /// <summary>
        /// EventBridge rule that triggers on S3 Object Created events.
        /// </summary>
        public Rule S3EventRule { get; }

        internal CdkBaseStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // Create KMS key for S3 bucket encryption
            EncryptionKey = new Key(this, "SleepAudioS3EncryptionKey", new KeyProps
            {
                Description = "KMS key for encrypting Sleep Audio Pipeline S3 buckets",
                EnableKeyRotation = true,
                RemovalPolicy = RemovalPolicy.RETAIN
            });

            // Create Input S3 Bucket
            InputBucket = new Bucket(this, "SleepAudioInputBucket", new BucketProps
            {
                Encryption = BucketEncryption.KMS,
                EncryptionKey = EncryptionKey,
                Versioned = true,
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                EventBridgeEnabled = true,
                RemovalPolicy = RemovalPolicy.RETAIN,
                EnforceSSL = true
            });

            // Create Output S3 Bucket
            OutputBucket = new Bucket(this, "SleepAudioOutputBucket", new BucketProps
            {
                Encryption = BucketEncryption.KMS,
                EncryptionKey = EncryptionKey,
                Versioned = true,
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                RemovalPolicy = RemovalPolicy.RETAIN,
                EnforceSSL = true
            });

            // Create placeholder CloudWatch Log Group for EventBridge rule target
            var logGroup = new LogGroup(this, "SleepAudioEventLogGroup", new LogGroupProps
            {
                Retention = RetentionDays.ONE_WEEK,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // Create EventBridge rule to capture S3 Object Created events
            S3EventRule = new Rule(this, "S3ObjectCreatedRule", new RuleProps
            {
                Description = "Triggers on S3 Object Created events in the Input bucket",
                EventPattern = new EventPattern
                {
                    Source = new[] { "aws.s3" },
                    DetailType = new[] { "Object Created" },
                    Detail = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "bucket", new System.Collections.Generic.Dictionary<string, object>
                            {
                                { "name", new[] { InputBucket.BucketName } }
                            }
                        }
                    }
                },
                Targets = new IRule.ITarget[]
                {
                    new CloudWatchLogGroup(logGroup)
                }
            });

            // Add stack outputs
            new CfnOutput(this, "InputBucketName", new CfnOutputProps
            {
                Value = InputBucket.BucketName,
                Description = "Name of the Input S3 bucket",
                ExportName = $"{id}-InputBucketName"
            });

            new CfnOutput(this, "OutputBucketName", new CfnOutputProps
            {
                Value = OutputBucket.BucketName,
                Description = "Name of the Output S3 bucket",
                ExportName = $"{id}-OutputBucketName"
            });

            new CfnOutput(this, "EventRuleName", new CfnOutputProps
            {
                Value = S3EventRule.RuleName,
                Description = "Name of the EventBridge rule",
                ExportName = $"{id}-EventRuleName"
            });
        }
    }
}
