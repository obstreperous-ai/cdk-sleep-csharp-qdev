using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.KMS;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;
using Amazon.CDK.AWS.IAM;
using System.Collections.Generic;
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

        /// <summary>
        /// Step Functions state machine for orchestrating the audio processing pipeline.
        /// </summary>
        public StateMachine AudioPipelineStateMachine { get; }

        /// <summary>
        /// DynamoDB table for storing audio pipeline metadata and processing status.
        /// </summary>
        public ITable MetadataTable { get; }

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

            // Create DynamoDB table for audio metadata storage
            MetadataTable = new Table(this, "SleepAudioMetadataTable", new TableProps
            {
                PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute
                {
                    Name = "audioId",
                    Type = AttributeType.STRING
                },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                Encryption = TableEncryption.AWS_MANAGED,
                PointInTimeRecovery = true,
                RemovalPolicy = RemovalPolicy.RETAIN,
                // Table will store: audioId, status, inputBucket, inputKey, outputKey,
                // createdAt, updatedAt, and other processing metadata
                TableName = "SleepAudioMetadataTable"
            });

            // Grant KMS key usage for DynamoDB encryption
            EncryptionKey.GrantEncryptDecrypt(new ServicePrincipal("dynamodb.amazonaws.com"));

            // Create CloudWatch Log Group for Step Functions logging
            var stateMachineLogGroup = new LogGroup(this, "SleepAudioStateMachineLogGroup", new LogGroupProps
            {
                Retention = RetentionDays.TWO_WEEKS,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            // Define DynamoDB PutItem task to write initial metadata
            // This task stores the initial processing record with status=PROCESSING
            var writeToDynamoDB = new DynamoPutItem(this, "WriteInitialMetadata", new DynamoPutItemProps
            {
                Table = MetadataTable,
                Item = new Dictionary<string, DynamoAttributeValue>
                {
                    { "audioId", DynamoAttributeValue.FromString(JsonPath.Format("s3-{}-{}", 
                        JsonPath.StringAt("$.detail.bucket.name"),
                        JsonPath.StringAt("$.detail.object.key"))) },
                    { "status", DynamoAttributeValue.FromString("PROCESSING") },
                    { "inputBucket", DynamoAttributeValue.FromString(JsonPath.StringAt("$.detail.bucket.name")) },
                    { "inputKey", DynamoAttributeValue.FromString(JsonPath.StringAt("$.detail.object.key")) },
                    { "createdAt", DynamoAttributeValue.FromString(JsonPath.StringAt("$$.State.EnteredTime")) },
                    { "updatedAt", DynamoAttributeValue.FromString(JsonPath.StringAt("$$.State.EnteredTime")) }
                },
                ResultPath = "$.dynamoResult"
            });

            // Define DynamoDB UpdateItem task to mark processing as complete
            var updateStatusToCompleted = new DynamoUpdateItem(this, "UpdateStatusToCompleted", new DynamoUpdateItemProps
            {
                Table = MetadataTable,
                Key = new Dictionary<string, DynamoAttributeValue>
                {
                    { "audioId", DynamoAttributeValue.FromString(JsonPath.Format("s3-{}-{}",
                        JsonPath.StringAt("$.detail.bucket.name"),
                        JsonPath.StringAt("$.detail.object.key"))) }
                },
                UpdateExpression = "SET #status = :completed, #updatedAt = :timestamp",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#status", "status" },
                    { "#updatedAt", "updatedAt" }
                },
                ExpressionAttributeValues = new Dictionary<string, DynamoAttributeValue>
                {
                    { ":completed", DynamoAttributeValue.FromString("COMPLETED") },
                    { ":timestamp", DynamoAttributeValue.FromString(JsonPath.StringAt("$$.State.EnteredTime")) }
                },
                ResultPath = "$.updateResult"
            });

            // Define Polly task - placeholder for text-to-speech synthesis
            // Using CallAwsService task to invoke Amazon Polly StartSpeechSynthesisTask
            var pollyTask = new CallAwsService(this, "PollyTextToSpeech", new CallAwsServiceProps
            {
                Service = "polly",
                Action = "startSpeechSynthesisTask",
                Parameters = new Dictionary<string, object>
                {
                    { "Engine", "neural" },
                    { "LanguageCode", "en-US" },
                    { "OutputFormat", "mp3" },
                    { "OutputS3BucketName", OutputBucket.BucketName },
                    { "Text", "This is a placeholder for sleep audio content. Future implementation will process actual input." },
                    { "VoiceId", "Joanna" }
                },
                IamResources = new[] { "*" },
                ResultPath = "$.pollyResult"
            });

            // Define state machine definition
            // Chain: Write initial metadata → Polly TTS → Update status to completed
            var definition = writeToDynamoDB
                .Next(pollyTask)
                .Next(updateStatusToCompleted);

            // Create Step Functions state machine
            AudioPipelineStateMachine = new StateMachine(this, "SleepAudioPipelineStateMachine", new StateMachineProps
            {
                StateMachineName = "SleepAudioPipelineStateMachine",
                DefinitionBody = DefinitionBody.FromChainable(definition),
                Logs = new LogOptions
                {
                    Destination = stateMachineLogGroup,
                    Level = LogLevel.ALL,
                    IncludeExecutionData = true
                },
                TracingEnabled = true
            });

            // Grant state machine permission to write to output bucket
            OutputBucket.GrantWrite(AudioPipelineStateMachine);

            // Grant state machine permission to use KMS key
            EncryptionKey.GrantEncryptDecrypt(AudioPipelineStateMachine);

            // Grant state machine permission to read/write DynamoDB table
            MetadataTable.GrantReadWriteData(AudioPipelineStateMachine);

            // Create placeholder CloudWatch Log Group for additional event logging
            var eventLogGroup = new LogGroup(this, "SleepAudioEventLogGroup", new LogGroupProps
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
                    new SfnStateMachine(AudioPipelineStateMachine, new SfnStateMachineProps
                    {
                        Input = RuleTargetInput.FromEventPath("$"),
                        DeadLetterQueue = null,
                        MaxEventAge = Duration.Hours(1),
                        RetryAttempts = 2
                    }),
                    new CloudWatchLogGroup(eventLogGroup)
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

            new CfnOutput(this, "StateMachineArn", new CfnOutputProps
            {
                Value = AudioPipelineStateMachine.StateMachineArn,
                Description = "ARN of the Step Functions state machine",
                ExportName = $"{id}-StateMachineArn"
            });

            new CfnOutput(this, "MetadataTableName", new CfnOutputProps
            {
                Value = MetadataTable.TableName,
                Description = "Name of the DynamoDB metadata table",
                ExportName = $"{id}-MetadataTableName"
            });
        }
    }
}
