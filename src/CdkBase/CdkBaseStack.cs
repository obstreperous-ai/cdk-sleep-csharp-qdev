using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.KMS;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.IAM;
using System.Collections.Generic;
using Amazon.CDK.AWS.CloudWatch;
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

        /// <summary>
        /// SNS topic for successful pipeline completion notifications.
        /// </summary>
        public ITopic PipelineCompletedTopic { get; }

        /// <summary>
        /// SNS topic for pipeline failure notifications.
        /// </summary>
        public ITopic PipelineFailedTopic { get; }

        /// <summary>
        /// Lambda function for audio processing, metadata enrichment, or validation logic.
        /// </summary>
        public IFunction AudioProcessorFunction { get; }

        internal CdkBaseStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        /// <summary>
        /// Environment name for this stack (dev, stage, prod, or null for default).
        /// </summary>
        public string Environment { get; }

        internal CdkBaseStack(Construct scope, string id, IStackProps props = null, string environment = null) : base(scope, id, props)
        {
            Environment = environment ?? "default";
            
            // Apply environment tags for cost allocation and resource organization
            Tags.SetTag("Environment", Environment);
            Tags.SetTag("Project", "SleepAudioPipeline");
            Tags.SetTag("ManagedBy", "CDK");
            
            InitializeStack();
        }

        /// <summary>
        /// Initialize all stack resources with environment-specific configurations.
        /// </summary>
        private void InitializeStack()
        {
            // Create KMS key for S3 bucket encryption
            EncryptionKey = new Key(this, "SleepAudioS3EncryptionKey", new KeyProps
            {
                Description = "KMS key for encrypting Sleep Audio Pipeline S3 buckets",
                EnableKeyRotation = true,
                RemovalPolicy = RemovalPolicy.RETAIN
            });

            // Create SNS topics for pipeline notifications with encryption
            PipelineCompletedTopic = new Topic(this, "SleepAudioPipelineCompleted", new TopicProps
            {
                DisplayName = "Sleep Audio Pipeline Completed",
                TopicName = "SleepAudioPipelineCompleted",
                MasterKey = EncryptionKey
            });

            PipelineFailedTopic = new Topic(this, "SleepAudioPipelineFailed", new TopicProps
            {
                DisplayName = "Sleep Audio Pipeline Failed",
                TopicName = "SleepAudioPipelineFailed",
                MasterKey = EncryptionKey
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

            // Create Lambda function for audio processing
            AudioProcessorFunction = new Function(this, "SleepAudioProcessorFunction", new FunctionProps
            {
                Runtime = Runtime.PYTHON_3_12,
                Handler = "index.lambda_handler",
                Code = Code.FromAsset("src/Lambda/SleepAudioProcessor"),
                FunctionName = "SleepAudioProcessorFunction",
                Description = "Processes audio files, validates input, and enriches metadata for the sleep audio pipeline",
                Timeout = Duration.Minutes(5),
                MemorySize = 512,
                Environment = new Dictionary<string, string>
                {
                    { "METADATA_TABLE_NAME", MetadataTable.TableName },
                    { "OUTPUT_BUCKET_NAME", OutputBucket.BucketName }
                },
                LogRetention = RetentionDays.TWO_WEEKS,
                Tracing = Tracing.ACTIVE
            });

            // Grant Lambda permissions to access DynamoDB table
            MetadataTable.GrantReadWriteData(AudioProcessorFunction);

            // Grant Lambda permissions to read from input bucket and write to output bucket
            InputBucket.GrantRead(AudioProcessorFunction);
            OutputBucket.GrantWrite(AudioProcessorFunction);

            // Grant Lambda permissions to use Amazon Polly for text-to-speech
            // Issue #11: Lambda needs Polly permissions for TTS processing of text files
            AudioProcessorFunction.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
            {
                Actions = new[] { "polly:SynthesizeSpeech" },
                Resources = new[] { "*" }
            }));

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
                ResultPath = "$.dynamoResult",
                RetryOnServiceExceptions = true
            });

            // Add explicit retry configuration for DynamoDB write task (Issue #10)
            writeToDynamoDB.AddRetry(new RetryProps
            {
                Errors = new[] { "States.TaskFailed", "DynamoDB.ProvisionedThroughputExceededException" },
                Interval = Duration.Seconds(1),
                MaxAttempts = 3,
                BackoffRate = 2.0
            });

            // Define Lambda invocation task to process audio
            // This task invokes the audio processor Lambda function with S3 event details
            var processAudio = new LambdaInvoke(this, "ProcessAudioWithLambda", new LambdaInvokeProps
            {
                LambdaFunction = AudioProcessorFunction,
                Payload = TaskInput.FromObject(new Dictionary<string, object>
                {
                    { "detail", JsonPath.ObjectAt("$.detail") }
                }),
                ResultPath = "$.processorResult",
                RetryOnServiceExceptions = true
            });

            // Add explicit retry configuration for Lambda invocation (Issue #10)
            processAudio.AddRetry(new RetryProps
            {
                Errors = new[] { 
                    "Lambda.ServiceException", 
                    "Lambda.AWSLambdaException", 
                    "Lambda.SdkClientException",
                    "Lambda.TooManyRequestsException",
                    "States.TaskFailed" 
                },
                Interval = Duration.Seconds(2),
                MaxAttempts = 2,
                BackoffRate = 2.0
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
                ResultPath = "$.updateResult",
                RetryOnServiceExceptions = true
            });

            // Add retry configuration for DynamoDB update on success (Issue #10)
            updateStatusToCompleted.AddRetry(new RetryProps
            {
                Errors = new[] { "States.TaskFailed", "DynamoDB.ProvisionedThroughputExceededException" },
                Interval = Duration.Seconds(1),
                MaxAttempts = 3,
                BackoffRate = 2.0
            });

            // Define DynamoDB UpdateItem task to mark processing as failed
            var updateStatusToFailed = new DynamoUpdateItem(this, "UpdateStatusToFailed", new DynamoUpdateItemProps
            {
                Table = MetadataTable,
                Key = new Dictionary<string, DynamoAttributeValue>
                {
                    { "audioId", DynamoAttributeValue.FromString(JsonPath.Format("s3-{}-{}",
                        JsonPath.StringAt("$.detail.bucket.name"),
                        JsonPath.StringAt("$.detail.object.key"))) }
                },
                UpdateExpression = "SET #status = :failed, #updatedAt = :timestamp, #errorDetails = :error",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#status", "status" },
                    { "#updatedAt", "updatedAt" },
                    { "#errorDetails", "errorDetails" }
                },
                ExpressionAttributeValues = new Dictionary<string, DynamoAttributeValue>
                {
                    { ":failed", DynamoAttributeValue.FromString("FAILED") },
                    { ":timestamp", DynamoAttributeValue.FromString(JsonPath.StringAt("$$.State.EnteredTime")) },
                    { ":error", DynamoAttributeValue.FromString(JsonPath.StringAt("$.Error")) }
                },
                ResultPath = "$.updateResult",
                RetryOnServiceExceptions = true
            });

            // Add retry configuration for DynamoDB update on failure (Issue #10)
            updateStatusToFailed.AddRetry(new RetryProps
            {
                Errors = new[] { "States.TaskFailed", "DynamoDB.ProvisionedThroughputExceededException" },
                Interval = Duration.Seconds(1),
                MaxAttempts = 3,
                BackoffRate = 2.0
            });

            // Define SNS Publish task for success notification
            var publishSuccessNotification = new SnsPublish(this, "PublishSuccessNotification", new SnsPublishProps
            {
                Topic = PipelineCompletedTopic,
                Message = TaskInput.FromObject(new Dictionary<string, object>
                {
                    { "status", "COMPLETED" },
                    { "message", "Sleep audio pipeline completed successfully" },
                    { "audioId", JsonPath.Format("s3-{}-{}", 
                        JsonPath.StringAt("$.detail.bucket.name"),
                        JsonPath.StringAt("$.detail.object.key")) },
                    { "timestamp", JsonPath.StringAt("$$.State.EnteredTime") }
                }),
                Subject = "Sleep Audio Pipeline Completed",
                ResultPath = "$.snsResult"
            });

            // Define SNS Publish task for failure notification
            var publishFailureNotification = new SnsPublish(this, "PublishFailureNotification", new SnsPublishProps
            {
                Topic = PipelineFailedTopic,
                Message = TaskInput.FromObject(new Dictionary<string, object>
                {
                    { "status", "FAILED" },
                    { "message", "Sleep audio pipeline failed" },
                    { "audioId", JsonPath.Format("s3-{}-{}", 
                        JsonPath.StringAt("$.detail.bucket.name"),
                        JsonPath.StringAt("$.detail.object.key")) },
                    { "error", JsonPath.StringAt("$.Error") },
                    { "timestamp", JsonPath.StringAt("$$.State.EnteredTime") }
                }),
                Subject = "Sleep Audio Pipeline Failed",
                ResultPath = "$.snsResult"
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

            // Add explicit retry configuration for Polly task (Issue #10)
            pollyTask.AddRetry(new RetryProps
            {
                Errors = new[] { 
                    "Polly.ServiceFailureException",
                    "States.TaskFailed",
                    "States.Timeout"
                },
                Interval = Duration.Seconds(2),
                MaxAttempts = 2,
                BackoffRate = 2.0
            });

            // Add advanced error handling to Polly task with specific error types (Issue #10)
            pollyTask.AddCatch(updateStatusToFailed.Next(publishFailureNotification), new CatchProps
            {
                Errors = new[] { 
                    "Polly.ServiceFailureException",
                    "Polly.TextLengthExceededException",
                    "Polly.InvalidSsmlException",
                    "States.TaskFailed",
                    "States.Timeout",
                    "States.Permissions",
                    "States.ALL"
                },
                ResultPath = "$.error"
            });

            // Add advanced error handling to Lambda task with specific error types (Issue #10)
            processAudio.AddCatch(updateStatusToFailed.Next(publishFailureNotification), new CatchProps
            {
                Errors = new[] { 
                    "Lambda.ServiceException",
                    "Lambda.AWSLambdaException",
                    "Lambda.SdkClientException",
                    "Lambda.TooManyRequestsException",
                    "Lambda.Unknown",
                    "States.TaskFailed",
                    "States.Timeout",
                    "States.Permissions",
                    "States.ALL"
                },
                ResultPath = "$.error"
            });

            // Define state machine definition with error handling
            // Chain: Write initial metadata → Lambda processor → Polly TTS (with error handling) → Update status to completed → Publish success notification
            var definition = writeToDynamoDB
                .Next(processAudio)
                .Next(pollyTask)
                .Next(updateStatusToCompleted)
                .Next(publishSuccessNotification);

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

            // Grant state machine permission to publish to SNS topics
            PipelineCompletedTopic.GrantPublish(AudioPipelineStateMachine);
            // Grant state machine permission to invoke Lambda function
            AudioProcessorFunction.GrantInvoke(AudioPipelineStateMachine);

            PipelineFailedTopic.GrantPublish(AudioPipelineStateMachine);


            // Create CloudWatch Alarms for observability (Issue #10)
            // Alarm for State Machine Execution Failures
            var stateMachineFailureAlarm = new Alarm(this, "StateMachineExecutionFailureAlarm", new AlarmProps
            {
                Metric = AudioPipelineStateMachine.MetricFailed(new MetricOptions
                {
                    Period = Duration.Minutes(5),
                    Statistic = "Sum"
                }),
                Threshold = 1,
                EvaluationPeriods = 1,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
                AlarmDescription = "Alarm when state machine execution fails",
                AlarmName = "SleepAudioPipeline-StateMachineFailures",
                TreatMissingData = TreatMissingData.NOT_BREACHING
            });

            // Alarm for Lambda Function Errors
            var lambdaErrorAlarm = new Alarm(this, "LambdaErrorAlarm", new AlarmProps
            {
                Metric = AudioProcessorFunction.MetricErrors(new MetricOptions
                {
                    Period = Duration.Minutes(5),
                    Statistic = "Sum"
                }),
                Threshold = 2,
                EvaluationPeriods = 1,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
                AlarmDescription = "Alarm when Lambda function has errors",
                AlarmName = "SleepAudioPipeline-LambdaErrors",
                TreatMissingData = TreatMissingData.NOT_BREACHING
            });

            // Alarm for State Machine Throttled Executions
            var stateMachineThrottledAlarm = new Alarm(this, "StateMachineThrottledAlarm", new AlarmProps
            {
                Metric = AudioPipelineStateMachine.MetricThrottled(new MetricOptions
                {
                    Period = Duration.Minutes(5),
                    Statistic = "Sum"
                }),
                Threshold = 1,
                EvaluationPeriods = 1,
                ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
                AlarmDescription = "Alarm when state machine executions are throttled",
                AlarmName = "SleepAudioPipeline-StateMachineThrottled",
                TreatMissingData = TreatMissingData.NOT_BREACHING
            });

            // Optionally publish alarms to SNS topic for notifications
            // Future enhancement: Configure alarm actions to send notifications via SNS
            // stateMachineFailureAlarm.AddAlarmAction(new SnsAction(PipelineFailedTopic));
            // lambdaErrorAlarm.AddAlarmAction(new SnsAction(PipelineFailedTopic));
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

            new CfnOutput(this, "PipelineCompletedTopicArn", new CfnOutputProps
            {
                Value = PipelineCompletedTopic.TopicArn,
                Description = "ARN of the pipeline completed SNS topic",
                ExportName = $"{id}-PipelineCompletedTopicArn"
            });

            new CfnOutput(this, "PipelineFailedTopicArn", new CfnOutputProps
            {
                Value = PipelineFailedTopic.TopicArn,
                Description = "ARN of the pipeline failed SNS topic",
                ExportName = $"{id}-PipelineFailedTopicArn"
            });

            new CfnOutput(this, "AudioProcessorFunctionArn", new CfnOutputProps
            {
                Value = AudioProcessorFunction.FunctionArn,
                Description = "ARN of the audio processor Lambda function",
                ExportName = $"{id}-AudioProcessorFunctionArn"
            });
        }
    }
}
