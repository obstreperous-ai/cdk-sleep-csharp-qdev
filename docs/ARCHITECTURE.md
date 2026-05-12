# AWS CDK Sleep Audio Pipeline Architecture

## Overview

This project implements an event-driven sleep audio pipeline using AWS CDK with C# and follows Test-Driven Development (TDD) principles.

## Project Structure

```
cdk-sleep-csharp-qdev/
├── src/
│   ├── CdkBase/                    # Main CDK application
│   │   ├── CdkBase.csproj         # Main project dependencies
│   │   ├── Program.cs             # CDK app entry point
│   │   └── CdkBaseStack.cs        # Primary infrastructure stack
│   └── CdkBase.Tests/             # Test project
│       ├── CdkBase.Tests.csproj   # Test dependencies (xUnit, CDK Assertions)
│       └── CdkBaseStackTests.cs   # Stack unit tests
├── .github/
│   └── workflows/
│       └── ci.yml                 # CI/CD pipeline
├── docs/
│   └── ARCHITECTURE.md            # This file
└── cdk.json                       # CDK configuration
```

## Technology Stack

### Core Framework
- **.NET 8.0**: Modern C# runtime with enhanced performance and features
- **AWS CDK 2.252.0**: Infrastructure as Code framework
- **Amazon.CDK.Lib**: CDK construct library

### Testing
- **xUnit 2.9.2**: Test framework for .NET
- **Amazon.CDK.Assertions**: CDK-specific assertions for infrastructure testing
- **Coverlet**: Code coverage collection

### CI/CD
- **GitHub Actions**: Automated build, test, and synth pipeline
- **AWS CDK CLI**: Infrastructure synthesis and deployment

## Development Workflow (TDD-First)

### 1. Red Phase - Write Failing Test
```csharp
[Fact]
public void NewFeature_ShouldMeetRequirement()
{
    // ARRANGE: Set up test context
    var app = new App();
    var stack = new CdkBaseStack(app, "TestStack");
    
    // ACT: Execute the code under test
    var template = Template.FromStack(stack);
    
    // ASSERT: Verify expected behavior
    template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
    {
        { "BucketName", "expected-bucket-name" }
    });
}
```

### 2. Green Phase - Implement Minimal Code
```csharp
public class CdkBaseStack : Stack
{
    internal CdkBaseStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        // Implement just enough to make the test pass
        new Bucket(this, "MyBucket", new BucketProps
        {
            BucketName = "expected-bucket-name"
        });
    }
}
```

### 3. Refactor Phase - Improve Code Quality
- Extract constants and configuration
- Apply strong typing with interfaces and enums
- Add documentation and comments
- Ensure code follows C# best practices

## CI/CD Pipeline

The GitHub Actions workflow runs on every push and pull request:

1. **Restore**: `dotnet restore` - Downloads all NuGet dependencies
2. **Build**: `dotnet build` - Compiles the solution
3. **Test**: `dotnet test` - Runs all xUnit tests with CDK Assertions
4. **Synth**: `cdk synth` - Generates CloudFormation templates
5. **Diff**: `cdk diff` - Shows infrastructure changes (on PRs)

## Strong Typing Guidelines

### Use Explicit Types
```csharp
// ✅ Good: Explicit and type-safe
public sealed class EventConfig
{
    public required string EventSource { get; init; }
    public required int TimeoutSeconds { get; init; }
}

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
