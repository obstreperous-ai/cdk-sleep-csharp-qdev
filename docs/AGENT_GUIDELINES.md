# Agent Development Guidelines

## Overview

This document provides guidelines for AI agents and developers working on the Sleep Audio Pipeline project. All future development issues must adhere to these principles to maintain consistency, security, and code quality.

---

## Source of Truth: Architecture Documentation

**[ARCHITECTURE.md](ARCHITECTURE.md)** is the authoritative source for all architectural decisions, system design, and AWS service configurations.

### Before Starting Any Issue
1. **Read ARCHITECTURE.md thoroughly** to understand:
   - Overall system design and data flow
   - AWS services used and why they were chosen
   - Security requirements and patterns
   - Multi-environment configuration
   - Naming conventions and structure

2. **Verify alignment** between your implementation and the documented architecture
3. **Update ARCHITECTURE.md** if your work introduces new patterns or services

---

## Development Workflow: TDD-First Approach

This project follows **strict Test-Driven Development (TDD)**. Code without tests will not be accepted.

### The Red-Green-Refactor Cycle

#### 1. **Red Phase** - Write Failing Test First
```csharp
[Fact]
public void InputS3Bucket_ShouldBeEncrypted()
{
    // ARRANGE
    var app = new App();
    var stack = new CdkBaseStack(app, "TestStack");
    
    // ACT
    var template = Template.FromStack(stack);
    
    // ASSERT
    template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
    {
        { "BucketEncryption", Match.ObjectLike(new Dictionary<string, object>
            { { "ServerSideEncryptionConfiguration", Match.AnyValue() } }) }
    });
}
```

**Run test**: `dotnet test src/CdkBase.Tests/` → Should **FAIL** ❌

#### 2. **Green Phase** - Implement Minimal Code
```csharp
public class CdkBaseStack : Stack
{
    internal CdkBaseStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        var encryptionKey = new Key(this, "S3EncryptionKey");
        
        new Bucket(this, "InputBucket", new BucketProps
        {
            Encryption = BucketEncryption.KMS,
            EncryptionKey = encryptionKey
        });
    }
}
```

**Run test**: `dotnet test src/CdkBase.Tests/` → Should **PASS** ✅

#### 3. **Refactor Phase** - Improve Code Quality
- Extract magic strings to constants
- Add strong typing (interfaces, enums, sealed classes)
- Add XML documentation comments
- Ensure all tests still pass

---

## Code Quality Standards

### C# Best Practices
1. **Enable nullable reference types**: `<Nullable>enable</Nullable>` in .csproj
2. **Use explicit types**: Avoid `var` for public APIs
3. **Prefer sealed classes**: Use `sealed` for classes that shouldn't be inherited
4. **Use required properties**: Mark required properties with `required` keyword
5. **Follow naming conventions**:
   - PascalCase for public members
   - camelCase for private fields with `_` prefix
   - UPPER_CASE for constants

### CDK Constructs
1. **Use construct IDs consistently**: Follow the naming patterns in ARCHITECTURE.md
2. **Apply security by default**:
   - Enable encryption for S3 buckets
   - Block public access on S3 buckets
   - Use customer-managed KMS keys
   - Apply least-privilege IAM policies
3. **Tag all resources**: Include `Environment`, `Project`, `ManagedBy` tags
4. **Add removal policies carefully**: Use `RemovalPolicy.RETAIN` for production data stores

---

## Security Requirements

Every AWS resource must follow these security principles:

### Encryption
- ✅ **S3**: Use SSE-KMS with customer-managed keys
- ✅ **DynamoDB**: Enable encryption at rest with KMS
- ✅ **SNS**: Enable encryption for topics
- ✅ **CloudWatch Logs**: Encrypt log groups with KMS

### IAM Policies
- ✅ **Least Privilege**: Grant only the minimum required permissions
- ✅ **Explicit ARNs**: Avoid wildcards (`*`) in resource ARNs
- ✅ **Deny Public Access**: Explicitly block public access on S3 buckets

### Network Security
- ✅ **Private Buckets**: Never allow public read/write
- ✅ **HTTPS Only**: Enforce SSL/TLS for all S3 bucket policies

---

## Testing Guidelines

### Unit Tests (CDK Assertions)
Every CDK construct must have tests covering:

1. **Resource Creation**: Verify resource exists in template
   ```csharp
   template.ResourceCountIs("AWS::S3::Bucket", 2); // Input + Output buckets
   ```

2. **Security Properties**: Assert encryption, public access settings
   ```csharp
   template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
   {
       { "PublicAccessBlockConfiguration", new Dictionary<string, bool>
           {
               { "BlockPublicAcls", true },
               { "BlockPublicPolicy", true },
               { "IgnorePublicAcls", true },
               { "RestrictPublicBuckets", true }
           }
       }
   });
   ```

3. **IAM Permissions**: Validate least-privilege policies
   ```csharp
   template.HasResourceProperties("AWS::IAM::Role", new Dictionary<string, object>
   {
       { "Policies", Match.ArrayWith(new[]
           {
               Match.ObjectLike(new Dictionary<string, object>
               {
                   { "PolicyDocument", Match.ObjectLike(new Dictionary<string, object>
                       {
                           { "Statement", Match.ArrayWith(new[]
                               {
                                   Match.ObjectLike(new Dictionary<string, object>
                                   {
                                       { "Effect", "Allow" },
                                       { "Action", "s3:GetObject" },
                                       { "Resource", Match.StringLikeRegexp("arn:aws:s3:::.*") }
                                   })
                               })
                           }
                       })
                   }
               })
           })
       }
   });
   ```

### Test Coverage
- **Minimum**: 80% code coverage for stack constructs
- **Run coverage**: `dotnet test /p:CollectCoverage=true`

---

## Multi-Environment Configuration

### CDK Context
Use CDK context to manage environment-specific settings:

```json
{
  "environments": {
    "dev": { "account": "111111111111", "region": "us-east-1" },
    "stage": { "account": "222222222222", "region": "us-east-1" },
    "prod": { "account": "333333333333", "region": "us-east-1" }
  }
}
```

### Accessing Context in Code
```csharp
var environment = this.Node.TryGetContext("environment") as string ?? "dev";
var config = this.Node.TryGetContext($"environments:{environment}");
```

### Environment-Specific Behavior
- **Dev**: Lower log retention, no alarms, smaller resources
- **Stage**: Production-like configuration for testing
- **Prod**: Full alarms, longer retention, optimized resources

---

## CI/CD Pipeline

Every commit triggers automated checks:

1. ✅ **Restore**: `dotnet restore src/CdkBase.sln`
2. ✅ **Build**: `dotnet build src/CdkBase.sln`
3. ✅ **Test**: `dotnet test src/CdkBase.sln`
4. ✅ **Synth**: `cdk synth` (generates CloudFormation)
5. ✅ **Diff**: `cdk diff` (shows infrastructure changes)

**All checks must pass** before merge.

---

## Issue-Driven Development

### Creating New Issues
1. **Reference ARCHITECTURE.md**: Ensure the issue aligns with documented design
2. **Define Success Criteria**: Clear, testable acceptance criteria
3. **Follow TDD**: Specify tests that must pass
4. **Small, Focused Changes**: One logical feature per issue

### Working on Issues
1. **Read ARCHITECTURE.md**: Understand the context
2. **Write tests first**: Red-Green-Refactor cycle
3. **Follow naming conventions**: Match patterns in existing code
4. **Update documentation**: If adding new patterns or services
5. **Run full test suite**: Ensure no regressions

---

## Documentation Standards

### Code Comments
- **XML Documentation**: Add `/// <summary>` for all public APIs
- **Inline Comments**: Explain "why", not "what"
- **TODO Comments**: Use `// TODO: [Issue #X]` format

### Architecture Updates
When adding new services or patterns:
1. Update ARCHITECTURE.md with rationale
2. Add Mermaid diagram updates if flow changes
3. Document security considerations
4. Update cost and observability sections

---

## Common Patterns

### Creating S3 Buckets
```csharp
var encryptionKey = new Key(this, "BucketKey", new KeyProps
{
    EnableKeyRotation = true,
    Description = "Encryption key for sleep audio buckets"
});

var bucket = new Bucket(this, "InputBucket", new BucketProps
{
    Encryption = BucketEncryption.KMS,
    EncryptionKey = encryptionKey,
    BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
    Versioned = false,
    RemovalPolicy = RemovalPolicy.RETAIN,
    EnforceSSL = true
});
```

### Creating IAM Roles
```csharp
var role = new Role(this, "ProcessingRole", new RoleProps
{
    AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
    Description = "Role for audio processing Lambda functions",
    ManagedPolicies = new[]
    {
        ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole")
    }
});

inputBucket.GrantRead(role);
outputBucket.GrantWrite(role);
```

### Creating Lambda Functions
```csharp
var function = new Function(this, "ValidateAudioFunction", new FunctionProps
{
    Runtime = Runtime.DOTNET_8,
    Handler = "ValidateAudio::ValidateAudio.Function::FunctionHandler",
    Code = Code.FromAsset("src/Lambda/ValidateAudio/bin/Release/net8.0/publish"),
    Timeout = Duration.Minutes(5),
    MemorySize = 1024,
    Environment = new Dictionary<string, string>
    {
        { "INPUT_BUCKET_NAME", inputBucket.BucketName },
        { "DYNAMODB_TABLE_NAME", table.TableName }
    },
    Role = role
});
```

---

## References

### Internal Documentation
- **[ARCHITECTURE.md](ARCHITECTURE.md)**: System architecture and design decisions
- **[README.md](../README.md)**: Getting started guide and project overview

### External Resources
- AWS CDK C# Developer Guide (Official CDK documentation)
- AWS Well-Architected Framework (Security, reliability, performance best practices)
- C# Coding Conventions (Microsoft documentation)

---

## Summary: Key Principles

1. ✅ **ARCHITECTURE.md is the source of truth** - Always consult it first
2. ✅ **TDD is mandatory** - Red-Green-Refactor for all code
3. ✅ **Security by default** - Encryption, least privilege, private access
4. ✅ **Strong typing** - Explicit types, nullable annotations, sealed classes
5. ✅ **Test everything** - Aim for 80%+ coverage
6. ✅ **Small, focused changes** - One feature per issue
7. ✅ **Document decisions** - Update ARCHITECTURE.md for new patterns
8. ✅ **CI/CD must pass** - All checks green before merge

---

**Last Updated**: Issue #2 - Architecture Documentation
**Next Steps**: Issue #3 will begin TDD implementation of S3 buckets and EventBridge rules
