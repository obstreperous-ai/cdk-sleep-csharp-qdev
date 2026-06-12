# AWS CDK Sleep Audio Pipeline (C# TDD)

> **Event-driven sleep audio pipeline built with AWS CDK, C#, and Test-Driven Development**

[![CI](https://github.com/obstreperous-ai/cdk-sleep-csharp-qdev/actions/workflows/ci.yml/badge.svg)](https://github.com/obstreperous-ai/cdk-sleep-csharp-qdev/actions/workflows/ci.yml)

## 🎉 Project Status: **COMPLETE**

The Sleep Audio Pipeline is **production-ready** with comprehensive test coverage, documentation, and following strict TDD principles. See [docs/SUMMARY.md](docs/SUMMARY.md) for a complete project overview.

## 🎯 Project Vision

This project demonstrates a **TDD-first, issue-driven development** approach to build a production-grade, event-driven AWS infrastructure for sleep audio processing. Built across 12 issues with 60+ tests, every feature was test-driven from day one.

## 🏗️ Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed architecture documentation.

## 🔧 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/) (for AWS CDK CLI)
- [AWS CLI](https://aws.amazon.com/cli/) configured with credentials
- [AWS CDK CLI](https://docs.aws.amazon.com/cdk/latest/guide/cli.html): `npm install -g aws-cdk`

## 🚀 Getting Started

### 1. Clone and Restore
```bash
git clone https://github.com/obstreperous-ai/cdk-sleep-csharp-qdev.git
cd cdk-sleep-csharp-qdev
dotnet restore src/CdkBase.sln
```

### 2. Run Tests (TDD First!)
```bash
# Run all tests
dotnet test src/CdkBase.sln

# Run tests with detailed output
dotnet test src/CdkBase.sln --verbosity normal

# Run tests with coverage
dotnet test src/CdkBase.sln /p:CollectCoverage=true
```

### 3. Build
```bash
dotnet build src/CdkBase.sln
```

### 4. Synthesize CloudFormation
```bash
cdk synth
```

### 5. Deploy to AWS
```bash
# Bootstrap CDK (first time only)
cdk bootstrap

# Deploy stack
cdk deploy
```

## 🧪 TDD Workflow
### 6. Deploy to Specific Environment
```bash
# Deploy to development
cdk deploy -c environment=dev

# Deploy to staging
cdk deploy -c environment=stage

# Deploy to production
cdk deploy -c environment=prod
```

## 🧪 Testing the Pipeline

### Unit Tests (Infrastructure)
Run all 60+ infrastructure tests to validate the CDK stack:
```bash
# Run all tests
dotnet test src/CdkBase.sln

# Run with detailed output
dotnet test src/CdkBase.sln --verbosity normal

# Run specific test
dotnet test src/CdkBase.sln --filter "EndToEnd_CompletePipelineShouldBeFullyConfigured"
```

### Manual End-to-End Validation

After deploying to AWS, validate the complete pipeline:

#### 1. Prepare Test Data
Create a test audio file or text file:

```bash
# Create a simple text file for TTS processing
echo "Welcome to the sleep audio pipeline. This is a test of the text-to-speech functionality." > test-input.txt

# Or use an existing audio file (MP3, WAV, M4A)
```

#### 2. Upload to Input Bucket
```bash
# Get the Input bucket name from CloudFormation outputs
aws cloudformation describe-stacks --stack-name CdkBaseStack \
  --query 'Stacks[0].Outputs[?OutputKey==`InputBucketName`].OutputValue' --output text

# Upload test file
aws s3 cp test-input.txt s3://<INPUT-BUCKET-NAME>/test/test-input.txt
```

#### 3. Monitor Processing
```bash
# Watch Step Functions execution
aws stepfunctions list-executions --state-machine-arn <STATE-MACHINE-ARN> --max-results 1

# Check execution details
aws stepfunctions describe-execution --execution-arn <EXECUTION-ARN>

# View CloudWatch Logs
aws logs tail /aws/lambda/<LAMBDA-FUNCTION-NAME> --follow
```

#### 4. Verify Output
```bash
# List processed files in Output bucket
aws s3 ls s3://<OUTPUT-BUCKET-NAME>/ --recursive

# Download processed audio
aws s3 cp s3://<OUTPUT-BUCKET-NAME>/processed-<TIMESTAMP>.mp3 ./output.mp3
```

#### 5. Check Metadata
```bash
# Query DynamoDB for processing status
aws dynamodb scan --table-name <METADATA-TABLE-NAME>

# Get specific item by audioId
aws dynamodb get-item --table-name <METADATA-TABLE-NAME> \
  --key '{"audioId": {"S": "s3-<BUCKET>-test/test-input.txt"}}'
```

### Validation Checklist
- ✅ S3 upload triggers EventBridge rule
- ✅ Step Functions execution starts automatically
- ✅ Lambda function processes input successfully
- ✅ Processed audio appears in Output bucket
- ✅ DynamoDB record shows COMPLETED status
- ✅ SNS notification sent (if subscribed)


This project strictly follows Test-Driven Development:

1. **Red**: Write a failing test in `src/CdkBase.Tests/`
2. **Green**: Implement minimal code in `src/CdkBase/` to pass the test
3. **Refactor**: Improve code quality while keeping tests green

### Example TDD Cycle
```bash
# 1. Write failing test
# 2. Run tests (should fail)
dotnet test src/CdkBase.Tests/
# 3. Implement feature
# 4. Run tests (should pass)
dotnet test src/CdkBase.Tests/
# 5. Refactor and repeat
```

## 📚 Useful CDK Commands
### Test Coverage Summary

| Category | Test Count | Description |
|----------|------------|-------------|
| **Infrastructure** | 15 tests | S3, KMS, EventBridge, State Machine basics |
| **Security** | 12 tests | Encryption, IAM permissions, public access |
| **Integration** | 18 tests | Complete pipeline flow, error paths |
| **Observability** | 8 tests | CloudWatch, X-Ray, alarms, logging |
| **Multi-Environment** | 5 tests | Dev/stage/prod configurations |
| **E2E Validation** | 5 tests | Complete end-to-end flow verification |
| **Total** | **60+ tests** | Comprehensive infrastructure coverage |

## 🔍 Troubleshooting

### Common Issues

**CDK Synth Fails**
- Ensure .NET 8.0 SDK is installed: `dotnet --version`
- Run `dotnet restore src/CdkBase.sln`
- Check for compilation errors: `dotnet build src/CdkBase.sln`

**Tests Fail**
- Verify all dependencies are restored
- Check test output for specific failures
- Ensure CDK version matches (v2.252.0+)

**Deployment Fails**
- Verify AWS credentials are configured: `aws sts get-caller-identity`
- Bootstrap CDK if first deployment: `cdk bootstrap`
- Check for service quota limits in AWS account

**Pipeline Not Triggering**
- Verify EventBridge is enabled on S3 bucket
- Check EventBridge rule is active in AWS Console
- Ensure file uploaded to correct bucket
- Review CloudWatch Logs for EventBridge rule

**Lambda Errors**
- Check Lambda CloudWatch Logs for error details
- Verify Lambda has required IAM permissions
- Ensure environment variables are set correctly
- Check input file format is supported (.mp3, .wav, .m4a, .txt, .json)

## 🌟 Key Features

### Built-In Capabilities
- ✅ **Event-Driven Architecture**: S3 uploads automatically trigger processing
- ✅ **Serverless**: No servers to manage, auto-scaling by default
- ✅ **Secure**: KMS encryption, least-privilege IAM, private buckets
- ✅ **Observable**: CloudWatch Logs, X-Ray tracing, alarms
- ✅ **Resilient**: Exponential backoff retries, comprehensive error handling
- ✅ **Multi-Environment**: Deploy to dev, stage, and prod with different configs
- ✅ **Well-Tested**: 60+ infrastructure tests with CI automation
- ✅ **Documented**: Comprehensive architecture and API documentation

### Pipeline Flow
1. User uploads audio/text file to S3 Input Bucket
2. EventBridge detects upload and triggers Step Functions
3. State Machine writes initial metadata to DynamoDB (status: PROCESSING)
4. Lambda function validates and processes the input:
   - Text files → Amazon Polly TTS synthesis
   - Audio files → Basic processing/normalization
5. Processed audio uploaded to S3 Output Bucket
6. DynamoDB metadata updated with output location (status: COMPLETED)
7. SNS notification sent on success or failure


| Command | Description |
|---------|-------------|
| `dotnet build src` | Compile the C# solution |
| `dotnet test src` | Run all xUnit tests |
| `cdk synth` | Synthesize CloudFormation template |
| `cdk diff` | Compare deployed stack with current state |
| `cdk deploy` | Deploy stack to AWS |
| `cdk destroy` | Remove stack from AWS |
| `cdk ls` | List all stacks in the app |

## 🔄 CI/CD Pipeline

Every push and pull request triggers:
- ✅ Dependency restoration
- ✅ Solution build
- ✅ Test execution with xUnit
- ✅ CDK synthesis
- ✅ Infrastructure diff (on PRs)

See [.github/workflows/ci.yml](.github/workflows/ci.yml) for details.

## 🤝 Contributing

1. Create an issue describing the feature/fix
2. Review [docs/AGENT_GUIDELINES.md](docs/AGENT_GUIDELINES.md) for development standards
3. Consult [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for system design
4. Write failing tests first (TDD!)
5. Implement minimal code to pass tests
6. Ensure all tests pass: `dotnet test src/CdkBase.sln`
7. Ensure CDK synth works: `cdk synth`
8. Submit pull request

## 📖 Documentation

- [Architecture Documentation](docs/ARCHITECTURE.md) - Detailed system design
- [Project Summary](docs/SUMMARY.md) - Project overview, decisions, and lessons learned
- [Agent Guidelines](docs/AGENT_GUIDELINES.md) - Development standards and patterns
- **[Meta-Prompts](docs/META-PROMPTS.md) - Reusable patterns for agentic TDD IaC** ⭐ **NEW**

### Reusable Patterns

The **[META-PROMPTS.md](docs/META-PROMPTS.md)** file contains extracted patterns from this experiment:
Agent instruction templates, TDD test patterns, reusable checklists, and common CDK constructs for future projects.

## 📝 License

See [LICENSE](LICENSE) file for details.

---

**Built with ❤️ using TDD, AWS CDK, and C#**
*Completed across 12 issues following strict Test-Driven Development principles*
