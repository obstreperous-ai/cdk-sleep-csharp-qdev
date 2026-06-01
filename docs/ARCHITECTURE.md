# Event-Driven Sleep Audio Pipeline Architecture

## Overview

This project implements a production-grade, event-driven sleep audio processing pipeline using AWS CDK with C# and follows Test-Driven Development (TDD) principles. The system enables users to upload raw audio files (voice recordings, ambient sounds, or text-to-speech requests) and automatically processes them through a serverless pipeline that generates soothing sleep audio content.

### Key Capabilities
- **Automated Audio Processing**: Event-driven architecture triggers processing on file upload
- **AI-Enhanced Audio**: Leverages Amazon Polly for text-to-speech and Bedrock for AI-generated soundscapes
- **Scalable & Reliable**: Serverless architecture scales automatically with demand
- **Observable**: Comprehensive logging and monitoring via CloudWatch
- **Secure**: Least-privilege IAM roles, encryption at rest, private S3 buckets
- **Multi-Environment**: Supports dev, stage, and prod environments via CDK context

---

## System Architecture Diagram

```mermaid
flowchart TD
    %% User and Input
    User([User]) -->|Upload Audio/Text| InputBucket[S3 Input Bucket<br/>Raw Audio Files]
    
    %% Event Detection
    InputBucket -->|S3 Event Notification| EventBridge[Amazon EventBridge<br/>Event Detection & Routing]
    
    %% Processing Orchestration
    EventBridge -->|Trigger Processing| StepFunctions[AWS Step Functions<br/>Orchestration Workflow]
    
    %% Processing Steps
    StepFunctions -->|1. Validate & Extract| ValidateLambda[Lambda: Validate Audio<br/>Extract Metadata]
    StepFunctions -->|2. Generate Voice| PollyLambda[Lambda: Polly TTS<br/>Text-to-Speech Generation]
    StepFunctions -->|3. AI Enhancement| BedrockLambda[Lambda: Bedrock<br/>AI Sleep Sound Generation]
    
    %% External Services
    PollyLambda -->|Synthesize Speech| Polly[Amazon Polly<br/>Neural TTS Engine]
    BedrockLambda -->|Generate Audio| Bedrock[Amazon Bedrock<br/>Generative AI]
    
    %% Output Storage
    ValidateLambda -->|Store Metadata| DynamoDB[(DynamoDB Table<br/>Audio Metadata & Status)]
    PollyLambda -->|Save Processed Audio| OutputBucket[S3 Output Bucket<br/>Processed Audio Files<br/>Versioning Enabled]
    BedrockLambda -->|Save Enhanced Audio| OutputBucket
    
    %% Notifications
    StepFunctions -->|Success/Failure| SNS[Amazon SNS Topic<br/>Notifications]
    SNS -->|Email/SMS| User
    
    %% Monitoring & Security
    StepFunctions -.->|Logs| CloudWatch[CloudWatch Logs<br/>& Alarms]
    ValidateLambda -.->|Logs| CloudWatch
    PollyLambda -.->|Logs| CloudWatch
    BedrockLambda -.->|Logs| CloudWatch
    
    KMS[AWS KMS<br/>Encryption Keys] -.->|Encrypt| InputBucket
    KMS -.->|Encrypt| OutputBucket
    KMS -.->|Encrypt| DynamoDB
    
    IAM[IAM Roles<br/>Least Privilege] -.->|Authorize| StepFunctions
    IAM -.->|Authorize| ValidateLambda
    IAM -.->|Authorize| PollyLambda
    IAM -.->|Authorize| BedrockLambda
    
    %% Styling
    classDef storage fill:#2e7d32,stroke:#1b5e20,stroke-width:2px,color:#fff
    classDef compute fill:#1565c0,stroke:#0d47a1,stroke-width:2px,color:#fff
    classDef ai fill:#6a1b9a,stroke:#4a148c,stroke-width:2px,color:#fff
    classDef monitoring fill:#ef6c00,stroke:#e65100,stroke-width:2px,color:#fff
    classDef security fill:#c62828,stroke:#b71c1c,stroke-width:2px,color:#fff
    
    class InputBucket,OutputBucket,DynamoDB storage
    class EventBridge,StepFunctions,ValidateLambda,PollyLambda,BedrockLambda,SNS compute
    class Polly,Bedrock ai
    class CloudWatch monitoring
    class KMS,IAM security
```

---

## Data Flow

### 1. Audio Upload & Event Detection
**Trigger**: User uploads a raw audio file (e.g., `user123/voice-recording.mp3`) or text file (e.g., `user123/bedtime-story.txt`) to the **Input S3 Bucket**.

**Event Flow**:
1. S3 emits an `s3:ObjectCreated:*` event notification
2. EventBridge rule matches the event pattern for the input bucket
3. EventBridge triggers the Step Functions state machine, passing object metadata (bucket name, object key, size, timestamp)

### 2. Processing Orchestration (Step Functions)
**State Machine Workflow**:

```
┌─────────────────┐
│  Start          │
└────────┬────────┘
         │
         ▼
┌─────────────────────────┐
│ Validate & Extract      │  ← Lambda: Check file type, extract metadata
│ Metadata                │    Store initial record in DynamoDB
└────────┬────────────────┘
         │
         ▼
    ┌────────┐
    │ Choice │  ← Branch based on input type
    └───┬────┘
        │
        ├─────► Text Input? ──► Polly TTS Generation ──┐
        │                                              │
        └─────► Audio Input? ──► Bedrock Enhancement ──┤
                                                       │
                                                       ▼
                                            ┌─────────────────┐
                                            │ Update DynamoDB │
                                            │ (Success Status)│
                                            └────────┬────────┘
                                                     │
                                                     ▼
                                            ┌─────────────────┐
                                            │ Send SNS        │
                                            │ Notification    │
                                            └────────┬────────┘
                                                     │
                                                     ▼
                                                  [End]

[Any Step Error] ──► Error Handler ──► Update DynamoDB (Failed Status) ──► SNS Error Notification ──► [End]
```

### 3. Processing Steps

#### Step 1: Validation & Metadata Extraction
- **Lambda Function**: `ValidateAudioFunction`
- **Actions**:
  - Validates file format (MP3, WAV, M4A, or TXT)
  - Extracts metadata: file size, duration (for audio), MIME type
  - Generates unique processing ID
  - Creates initial DynamoDB record:
    ```json
    {
      "ProcessingId": "proc-abc123",
      "UserId": "user123",
      "InputKey": "user123/voice-recording.mp3",
      "Status": "PROCESSING",
      "CreatedAt": "2024-01-15T10:30:00Z",
      "Duration": 120.5,
      "FileSize": 2048576
    }
    ```

#### Step 2: Text-to-Speech (Polly)
- **Lambda Function**: `PollyProcessingFunction`
- **Triggered When**: Input file is text (`.txt`, `.json` with text field)
- **Actions**:
  - Reads text content from input file
  - Invokes Amazon Polly with neural voice (e.g., `Joanna`, `Matthew`)
  - Applies SSML tags for soothing speech patterns (slower rate, softer volume)
  - Saves synthesized audio to Output S3 Bucket
  - Updates DynamoDB with output location and audio duration

**Example Polly Request**:
```xml
<speak>
  <prosody rate="slow" volume="soft">
    Close your eyes and imagine a peaceful forest...
  </prosody>
</speak>
```

#### Step 3: AI Enhancement (Bedrock)
- **Lambda Function**: `BedrockEnhancementFunction`
- **Triggered When**: Input file is audio or TTS output requires enhancement
- **Actions**:
  - Analyzes audio characteristics
  - Generates complementary ambient soundscapes (rain, ocean waves, white noise)
  - Mixes generated sounds with original audio
  - Applies sleep-optimized audio processing (gradual fade-out, binaural beats)
  - Saves enhanced audio to Output S3 Bucket

### 4. Output Storage & Metadata
- **Output S3 Bucket**: Stores processed files with versioning enabled
  - Path structure: `{user_id}/{processing_id}/output.mp3`
  - Metadata tags: processing_id, user_id, duration, format
- **DynamoDB Table**: Updated with final status
  ```json
  {
    "ProcessingId": "proc-abc123",
    "UserId": "user123",
    "Status": "COMPLETED",
    "OutputKey": "user123/proc-abc123/output.mp3",
    "ProcessingTimeMs": 3450,
    "CompletedAt": "2024-01-15T10:30:15Z"
  }
  ```

### 5. Notifications
- **SNS Topic**: Publishes messages on completion or failure
- **Success Notification**:
  ```json
  {
    "Subject": "Sleep Audio Processing Complete",
    "Message": "Your audio file has been processed successfully.",
    "ProcessingId": "proc-abc123",
    "OutputUrl": "https://output-bucket.s3.amazonaws.com/user123/proc-abc123/output.mp3"
  }
  ```
- **Error Notification**: Includes error details and processing ID for troubleshooting

---

## AWS Services & Architecture Decisions

### S3 Buckets (Input & Output)
**Why S3?**
- Durable, scalable object storage for audio files
- Native event notifications for event-driven architecture
- Versioning protects against accidental overwrites
- Lifecycle policies can archive old files to Glacier for cost savings

**Configuration**:
- **Encryption**: SSE-KMS with customer-managed keys
- **Access**: Private buckets with bucket policies (no public access)
- **Versioning**: Enabled on output bucket for audit trail
- **CORS**: Configured for pre-signed URL uploads (future web UI)

### Amazon EventBridge
**Why EventBridge over S3 Direct Lambda Trigger?**
- Decouples event source from targets (flexibility to add more consumers)
- Advanced filtering capabilities (e.g., filter by file extension, prefix)
- Built-in retry and dead-letter queue support
- Event archive for debugging and replay

**Event Pattern Example**:
```json
{
  "source": ["aws.s3"],
  "detail-type": ["Object Created"],
  "detail": {
    "bucket": {"name": ["sleep-audio-input-bucket"]},
    "object": {"key": [{"prefix": ""}]}
  }
}
```

### AWS Step Functions
**Why Step Functions over Direct Lambda Chaining?**
- Visual workflow representation for complex orchestration
- Built-in error handling, retries, and exponential backoff
- State management eliminates need for custom coordination logic
- Integration with CloudWatch for execution history and debugging
- Supports long-running workflows (up to 1 year)

**State Machine Type**: Standard (for complex workflows with guaranteed execution order)

**Error Handling**:
- Automatic retries with exponential backoff for transient errors
- Catch blocks for graceful failure handling
- Dead-letter queue for unrecoverable errors

### AWS Lambda (Processing Functions)
**Runtime**: .NET 8 (C#) for consistency with CDK code
**Memory**: 1024 MB (adjustable per function based on workload)
**Timeout**: 5 minutes (sufficient for most audio processing tasks)

**Function Responsibilities**:
1. **ValidateAudioFunction**: Lightweight validation and metadata extraction
2. **PollyProcessingFunction**: Text-to-speech synthesis
3. **BedrockEnhancementFunction**: AI-powered audio generation and enhancement

### Amazon Polly
**Why Polly?**
- High-quality neural text-to-speech voices
- SSML support for fine-grained speech control
- Multiple languages and voices for personalization
- Cost-effective (pay-per-character)

**Voice Selection**: Neural voices (Joanna, Matthew) for natural, soothing narration

### Amazon Bedrock
**Why Bedrock?**
- Access to state-of-the-art generative AI models
- Potential for audio synthesis, sound generation, and enhancement
- Foundation model flexibility (Anthropic Claude, Stability AI, etc.)
- Serverless inference (no infrastructure management)

**Use Cases**:
- Generate ambient soundscapes (rain, ocean, forest)
- Create personalized sleep stories from prompts
- Enhance audio with binaural beats or white noise

### DynamoDB
**Why DynamoDB?**
- Serverless, auto-scaling NoSQL database
- Low-latency reads/writes for metadata storage
- Flexible schema for evolving metadata requirements
- Integrated with Step Functions for state management

**Table Schema**:
- **Primary Key**: `ProcessingId` (String)
- **GSI**: `UserId-CreatedAt-index` for user-specific queries
- **Attributes**: Status, InputKey, OutputKey, Duration, FileSize, Timestamps, ErrorDetails

**Capacity Mode**: On-Demand (scales automatically without capacity planning)

### Amazon SNS
**Why SNS?**
- Pub/Sub messaging for fan-out notifications
- Multiple subscription types (Email, SMS, SQS, Lambda)
- Supports future integrations (e.g., webhooks, mobile push)

**Topics**:
- `SleepAudioProcessingTopic`: Success and error notifications

### AWS KMS
**Why Customer-Managed Keys?**
- Fine-grained control over encryption key rotation
- Audit trail of key usage via CloudTrail
- Compliance requirements for data-at-rest encryption

**Keys**:
- `SleepAudioS3Key`: Encrypts S3 objects
- `SleepAudioDynamoDBKey`: Encrypts DynamoDB data

### CloudWatch Logs & Alarms
**Observability Strategy**:
- **Logs**: All Lambda functions and Step Functions executions logged
- **Metrics**: Custom metrics for processing duration, error rates, file sizes
- **Alarms**:
  - High error rate in Step Functions (>5% failures)
  - Lambda function throttling
  - DynamoDB capacity exceeded

**Log Retention**: 30 days (configurable per environment)

---

## Security Architecture

### Principle of Least Privilege
Every AWS service and Lambda function has minimal IAM permissions:

- **Step Functions Execution Role**:
  - Invoke Lambda functions (specific function ARNs only)
  - Publish to SNS topic
  - Write logs to CloudWatch

- **Lambda Execution Roles**:
  - **ValidateAudioFunction**: Read from input bucket, write to DynamoDB, CloudWatch Logs
  - **PollyProcessingFunction**: Read from input bucket, write to output bucket, invoke Polly, update DynamoDB
  - **BedrockEnhancementFunction**: Read from input/output buckets, write to output bucket, invoke Bedrock, update DynamoDB

### Data Encryption
- **At Rest**: All S3 objects and DynamoDB data encrypted with KMS customer-managed keys
- **In Transit**: HTTPS/TLS 1.2+ for all AWS service communication

### Network Security
- **Private S3 Buckets**: Block all public access
- **VPC Endpoints**: (Future enhancement) Lambda functions in VPC with S3/DynamoDB VPC endpoints for private communication

### Audit & Compliance
- **CloudTrail**: Logs all API calls for auditing
- **S3 Access Logging**: Tracks bucket access patterns
- **DynamoDB Point-in-Time Recovery**: Enabled for data protection

---

## Multi-Environment Support

### CDK Context Configuration
Environments are defined in `cdk.json` context:

```json
{
  "environments": {
    "dev": {
      "account": "111111111111",
      "region": "us-east-1",
      "bucketPrefix": "sleep-audio-dev",
      "logRetentionDays": 7
    },
    "stage": {
      "account": "222222222222",
      "region": "us-east-1",
      "bucketPrefix": "sleep-audio-stage",
      "logRetentionDays": 30
    },
    "prod": {
      "account": "333333333333",
      "region": "us-east-1",
      "bucketPrefix": "sleep-audio-prod",
      "logRetentionDays": 90,
      "enableAlarms": true
    }
  }
}
```

### Environment-Specific Configurations
- **Dev**: Reduced log retention, no alarms, smaller Lambda memory
- **Stage**: Mirrors production configuration for testing
- **Prod**: Full alarms, longer log retention, optimized resources

---

## Cost Considerations

### Cost Optimization Strategies
1. **S3 Lifecycle Policies**: Transition processed files to Glacier after 90 days
2. **DynamoDB On-Demand**: Pay only for actual reads/writes (avoid over-provisioning)
3. **Lambda Memory Sizing**: Right-size based on CloudWatch performance metrics
4. **Step Functions Express**: Consider Express workflows for high-throughput, short-duration workflows
5. **Polly & Bedrock Usage**: Implement client-side caching for repeated requests

### Cost Monitoring
- **AWS Cost Explorer**: Track spending by service and environment
- **Budget Alerts**: Set monthly budgets with SNS notifications at 80% threshold
- **Tagged Resources**: Tag all resources with `Environment`, `Project`, `CostCenter` for granular analysis

---

## Observability & Monitoring

### CloudWatch Dashboards
**Sleep Audio Pipeline Dashboard**:
- Step Functions execution count (success/failed)
- Lambda invocation count, duration, errors
- DynamoDB read/write capacity utilization
- S3 bucket size and request metrics

### Alarms (Production Only)
1. **HighStepFunctionsErrorRate**: Triggers when error rate >5% over 5 minutes
2. **LambdaThrottling**: Any Lambda function throttled >10 times in 5 minutes
3. **DynamoDBCapacityExceeded**: On-demand capacity throttled
4. **S3InputBucketSizeExceeded**: Bucket size >100 GB (configurable)

### Distributed Tracing (Future Enhancement)
- **AWS X-Ray**: End-to-end request tracing across Step Functions and Lambda

---

## Future Extensibility

### Planned Enhancements
1. **User Authentication**: Integrate with Amazon Cognito for user management
2. **GraphQL API**: Add AWS AppSync for real-time status updates
3. **Batch Processing**: Process multiple files in parallel with SQS and Lambda
4. **Custom Audio Effects**: Allow users to select voice styles, background sounds, and effects
5. **Machine Learning**: Train custom models for sleep quality prediction
6. **Mobile App Integration**: Native iOS/Android apps with push notifications
7. **Analytics Dashboard**: User engagement metrics and popular audio types

### Scalability Roadmap
- **Multi-Region**: Deploy to multiple AWS regions for global user base
- **CDN**: Use CloudFront for fast, cached audio delivery
- **Database Replication**: DynamoDB global tables for multi-region data access

---

## Project Structure

```
cdk-sleep-csharp-qdev/
├── src/
│   ├── CdkBase/                     # Main CDK application
│   │   ├── CdkBase.csproj          # Main project dependencies
│   │   ├── Program.cs              # CDK app entry point
│   │   ├── CdkBaseStack.cs         # Primary infrastructure stack
│   │   └── Constructs/             # Reusable CDK constructs (future)
│   │       ├── StorageConstruct.cs # S3 buckets and KMS keys
│   │       ├── ProcessingConstruct.cs # Step Functions and Lambda
│   │       └── MonitoringConstruct.cs # CloudWatch and alarms
│   ├── CdkBase.Tests/              # Test project
│   │   ├── CdkBase.Tests.csproj    # Test dependencies (xUnit, CDK Assertions)
│   │   └── CdkBaseStackTests.cs    # Stack unit tests
│   └── Lambda/                     # Lambda function code (future)
│       ├── ValidateAudio/
│       ├── PollyProcessing/
│       └── BedrockEnhancement/
├── .github/
│   └── workflows/
│       └── ci.yml                  # CI/CD pipeline
├── docs/
│   ├── ARCHITECTURE.md             # This file
│   └── AGENT_GUIDELINES.md         # Development guidelines
└── cdk.json                        # CDK configuration with context
```

---

## Technology Stack

### Core Framework
- **.NET 8.0**: Modern C# runtime with enhanced performance and features
- **AWS CDK 2.252.0**: Infrastructure as Code framework
- **Amazon.CDK.Lib**: CDK construct library

### Testing
- **xUnit 2.9.2**: Test framework for .NET
- **Amazon.CDK.Assertions**: CDK-specific assertions for infrastructure testing
- **Coverlet**: Code coverage collection
---


### CI/CD
- **GitHub Actions**: Automated build, test, and synth pipeline
- **AWS CDK CLI**: Infrastructure synthesis and deployment
public void InputS3Bucket_ShouldHaveEncryptionEnabled()
## Development Workflow (TDD-First)

### 1. Red Phase - Write Failing Test
```csharp
[Fact]
public void NewFeature_ShouldMeetRequirement()
{
    // ARRANGE: Set up test context
    var app = new App();
        { "BucketEncryption", Match.ObjectLike(new Dictionary<string, object>
            { { "ServerSideEncryptionConfiguration", Match.AnyValue() } }) }
    
    // ACT: Execute the code under test
    var template = Template.FromStack(stack);
    
    // ASSERT: Verify expected behavior
    template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
    {
        { "BucketName", "expected-bucket-name" }
    });
        var encryptionKey = new Key(this, "S3EncryptionKey");
        
        new Bucket(this, "InputBucket", new BucketProps

            Encryption = BucketEncryption.KMS,
            EncryptionKey = encryptionKey,
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Versioned = false
```csharp
public class CdkBaseStack : Stack
{
    internal CdkBaseStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        // Implement just enough to make the test pass
        new Bucket(this, "MyBucket", new BucketProps
        {
            BucketName = "expected-bucket-name"
---

## Testing Strategy

### Unit Tests (CDK Assertions)
- Verify S3 bucket encryption and public access settings
- Validate IAM role permissions (least privilege)
- Check EventBridge rule event patterns
- Assert Step Functions state machine structure
- Confirm DynamoDB table schema and indexes

### Integration Tests (Future)
- End-to-end workflow testing with sample audio files
- CloudFormation stack deployment in test environment

---

        });
    }
}
```

### 3. Refactor Phase - Improve Code Quality
- Extract constants and configuration
- Apply strong typing with interfaces and enums
- Add documentation and comments
---

- Ensure code follows C# best practices

## CI/CD Pipeline

The GitHub Actions workflow runs on every push and pull request:
public sealed class AudioProcessingConfig
1. **Restore**: `dotnet restore` - Downloads all NuGet dependencies
    public required string InputBucketName { get; init; }
    public required string OutputBucketName { get; init; }
    public required int LambdaTimeoutSeconds { get; init; }
    public required int LambdaMemoryMB { get; init; }
4. **Synth**: `cdk synth` - Generates CloudFormation templates
5. **Diff**: `cdk diff` - Shows infrastructure changes (on PRs)
var config = new { Bucket = "my-bucket", Timeout = 300 };
## Strong Typing Guidelines

### Use Explicit Types
```csharp
// ✅ Good: Explicit and type-safe
public sealed class EventConfig
{
    public required string EventSource { get; init; }
---

## References & Documentation

### Internal Documentation
- [AGENT_GUIDELINES.md](AGENT_GUIDELINES.md) - Development guidelines for future issues
- [README.md](../README.md) - Getting started guide
}
### AWS Service Documentation
- AWS CDK C# Developer Guide (Official CDK documentation)
- Amazon S3 Documentation
- AWS Step Functions Developer Guide
- Amazon Polly Documentation
- Amazon Bedrock Documentation
- Amazon DynamoDB Developer Guide
- Amazon EventBridge User Guide

---

## Conclusion

This architecture provides a solid foundation for building a production-grade, event-driven sleep audio processing pipeline. The design emphasizes:

- **Scalability**: Serverless architecture scales automatically with demand
- **Reliability**: Built-in retries, error handling, and state management
- **Security**: Encryption, least-privilege IAM, and private networking
- **Observability**: Comprehensive logging, metrics, and alarms
- **Extensibility**: Modular design enables future enhancements

The next phase (Issue #3) will begin TDD implementation of core S3 buckets and EventBridge rules.
// ❌ Avoid: Dynamic or object types
var config = new { EventSource = "s3", Timeout = 30 };
```

### Leverage Nullable Reference Types
```csharp
// Enable in project file: <Nullable>enable</Nullable>
public string GetBucketName() => "my-bucket";  // Never null
public string? GetOptionalConfig() => null;     // May be null
```

## Future Architecture Components

As the sleep audio pipeline evolves through TDD, we'll add event-driven components including S3 buckets, Lambda functions, EventBridge rules, and Step Functions workflows. Each component will be test-driven using CDK Assertions to ensure infrastructure correctness.
