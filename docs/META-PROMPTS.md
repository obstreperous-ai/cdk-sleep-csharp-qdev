# Meta-Prompts for Agentic TDD Infrastructure as Code

> **Reusable patterns and prompts extracted from the Sleep Audio Pipeline experiment**

## 📘 Overview

This document captures reusable meta-prompting patterns, agent guidelines, and templates for building infrastructure projects using Test-Driven Development (TDD) with AWS CDK and AI agents (like Amazon Q Developer). These patterns were refined through 12 issues of strict TDD development on the Sleep Audio Pipeline project.

**Target Audience**: AI agents, developers, and teams building cloud infrastructure with a TDD-first approach.

---

## 🎯 Core Philosophy: Issue-Driven TDD for IaC

### The Foundation

**Principle**: Every infrastructure change is driven by a GitHub issue that defines tests first, then implementation.

**Why This Works**:
- Clear acceptance criteria before coding begins
- Tests document expected behavior
- Prevents scope creep and gold-plating
- Enables AI agents to work autonomously with clear boundaries
- Creates an audit trail of all architectural decisions

### The Workflow Pattern

```
GitHub Issue → Failing Tests → Minimal Implementation → Green Tests → Refactor → Documentation Update → PR Merge
```

**Each Issue Must Include**:
1. **Context**: Link to ARCHITECTURE.md for system understanding
2. **Goal**: What infrastructure should be added/changed
3. **Test Requirements**: Specific tests that must pass
4. **Success Criteria**: Clear, verifiable outcomes
5. **Constraints**: Security, cost, or compliance requirements

---

## 🤖 Agent Instruction Templates

### Template 1: Foundation Infrastructure Issue

```markdown
**Goal**: Implement [COMPONENT_NAME] with security best practices

**Strict Discipline**:
- Review ARCHITECTURE.md for design context
- Write failing tests first (TDD Red phase)
- Implement minimal code to pass tests (Green phase)
- Refactor for quality (Refactor phase)
- Update ARCHITECTURE.md if introducing new patterns

**Test Requirements**:
1. Resource Creation Test
   - Verify [RESOURCE_TYPE] exists in CloudFormation template
   - Assert correct resource count

2. Security Tests
   - Encryption enabled (KMS for S3/DynamoDB, etc.)
   - Public access blocked (S3 buckets)
   - Least-privilege IAM permissions
   - HTTPS enforcement

3. Configuration Tests
   - Verify property values match requirements
   - Assert environment-specific settings

**Success Criteria**:
- All tests pass: `dotnet test src/CdkBase.sln`
- CDK synth succeeds: `cdk synth`
- CI pipeline passes
- ARCHITECTURE.md updated (if applicable)
```

### Template 2: Integration/Orchestration Issue

```markdown
**Goal**: Wire [COMPONENT_A] to [COMPONENT_B] with proper error handling

**Strict Discipline**:
- Review data flow in ARCHITECTURE.md
- Write integration tests first
- Implement IAM permissions using grant methods
- Add error handling and retries
- Test both success and failure paths

**Test Requirements**:
1. Integration Tests
   - Verify [COMPONENT_A] has permission to invoke [COMPONENT_B]
   - Assert event routing configuration
   - Validate input/output transformations

2. Error Handling Tests
   - Catch blocks exist for [ERROR_TYPES]
   - Retry policies configured with exponential backoff
   - Error notifications sent to appropriate channels

3. Observability Tests
   - CloudWatch logging enabled
   - X-Ray tracing configured (if applicable)
   - Alarms defined for critical failures

**Success Criteria**:
- All integration tests pass
- Both happy path and error path validated
- IAM permissions follow least-privilege principle
- Observability hooks in place
```

### Template 3: Multi-Environment Configuration Issue

```markdown
**Goal**: Implement environment-specific configurations for dev/stage/prod

**Strict Discipline**:
- Define environment context in cdk.json
- Use CDK context to load environment settings
- Tag all resources with Environment tag
- Test synthesis for all environments

**Test Requirements**:
1. Environment Synthesis Tests
   - Stack can be synthesized for dev, stage, and prod
   - Environment-specific values applied correctly
   - Resource naming includes environment identifier

2. Configuration Tests
   - Dev: Lower costs, shorter retention, no alarms
   - Stage: Mirrors production config
   - Prod: Full alarms, longer retention, optimized settings

3. Tagging Tests
   - All resources tagged with Environment
   - Tags include Project and ManagedBy

**Success Criteria**:
- `cdk synth -c environment=dev` succeeds
- `cdk synth -c environment=stage` succeeds
- `cdk synth -c environment=prod` succeeds
- Environment-specific settings validated in tests
```

---

## 🧪 TDD Patterns for Infrastructure

### Pattern 1: Resource Existence Tests

**When to Use**: Verifying that a CDK construct creates the expected AWS resource

**Test Pattern**:
```csharp
[Fact]
public void Stack_ShouldContain[ResourceType]()
{
    // ARRANGE
    var app = new App();
    var stack = new CdkBaseStack(app, "TestStack");
    
    // ACT
    var template = Template.FromStack(stack);
    
    // ASSERT
    template.ResourceCountIs("AWS::[Service]::[Resource]", expectedCount);
}
```

**Example**:
```csharp
template.ResourceCountIs("AWS::S3::Bucket", 2); // Input + Output buckets
```

### Pattern 2: Security Property Assertions

**When to Use**: Validating security configurations (encryption, access control, IAM)

**Test Pattern**:
```csharp
[Fact]
public void [Resource]_ShouldHave[SecurityProperty]()
{
    // ARRANGE
    var app = new App();
    var stack = new CdkBaseStack(app, "TestStack");
    
    // ACT
    var template = Template.FromStack(stack);
    
    // ASSERT
    template.HasResourceProperties("[ResourceType]", new Dictionary<string, object>
    {
        { "[PropertyName]", [ExpectedValue] }
    });
}
```

**Examples**:
```csharp
// S3 Encryption
template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
{
    { "BucketEncryption", Match.ObjectLike(new Dictionary<string, object>
        { { "ServerSideEncryptionConfiguration", Match.AnyValue() } }) }
});

// Public Access Blocking
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

### Pattern 3: IAM Permission Tests

**When to Use**: Ensuring least-privilege IAM policies

**Test Pattern**:
```csharp
[Fact]
public void [Component]_ShouldHaveLeastPrivilegePermissionsFor[Action]()
{
    // ARRANGE
    var app = new App();
    var stack = new CdkBaseStack(app, "TestStack");
    
    // ACT
    var template = Template.FromStack(stack);
    
    // ASSERT
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
                                        { "Action", "[specific-action]" },
                                        { "Resource", Match.StringLikeRegexp("arn:aws:.*") }
                                    })
                                })
                            }
                        })
                    }
                })
            })
        }
    });
}
```

### Pattern 4: Integration Flow Tests

**When to Use**: Validating event routing, state machine flows, or service integrations

**Test Pattern**:
```csharp
[Fact]
public void [Source]_ShouldTrigger[Target]_OnEvent()
{
    // ARRANGE
    var app = new App();
    var stack = new CdkBaseStack(app, "TestStack");
    
    // ACT
    var template = Template.FromStack(stack);
    
    // ASSERT
    // Verify event rule exists
    template.HasResourceProperties("AWS::Events::Rule", Match.ObjectLike(...));
    
    // Verify rule targets the correct service
    template.HasResourceProperties("AWS::Events::Rule", new Dictionary<string, object>
    {
        { "Targets", Match.ArrayWith(new[]
            {
                Match.ObjectLike(new Dictionary<string, object>
                {
                    { "Arn", Match.AnyValue() }
                })
            })
        }
    });
}
```

### Pattern 5: End-to-End Validation Tests

**When to Use**: Verifying complete pipeline integration

**Test Pattern**:
```csharp
[Fact]
public void EndToEnd_Complete[Workflow]ShouldBeFullyConfigured()
{
    // ARRANGE
    var app = new App();
    var stack = new CdkBaseStack(app, "TestStack");
    
    // ACT
    var template = Template.FromStack(stack);
    
    // ASSERT
    // Step 1: Source exists
    template.ResourceCountIs("AWS::S3::Bucket", expectedCount);
    
    // Step 2: Event routing configured
    template.ResourceCountIs("AWS::Events::Rule", expectedCount);
    
    // Step 3: Processing orchestration exists
    template.ResourceCountIs("AWS::StepFunctions::StateMachine", 1);
    
    // Step 4: Processing logic exists
    template.ResourceCountIs("AWS::Lambda::Function", expectedCount);
    
    // Step 5: Metadata storage exists
    template.ResourceCountIs("AWS::DynamoDB::Table", 1);
    
    // Step 6: Notifications configured
    template.ResourceCountIs("AWS::SNS::Topic", expectedCount);
}
```

---

## 📋 Reusable Checklists

### Pre-Issue Checklist for Agents

Before starting work on any infrastructure issue:

- [ ] Read ARCHITECTURE.md thoroughly
- [ ] Understand the component's role in the overall system
- [ ] Review existing tests for similar components
- [ ] Check for environment-specific requirements
- [ ] Identify security requirements (encryption, IAM, access control)
- [ ] Note any cost optimization considerations
- [ ] Verify CI/CD pipeline is passing

### Test Writing Checklist

For each new infrastructure component:

- [ ] Resource creation test (verify resource exists)
- [ ] Encryption test (S3, DynamoDB, SNS, logs)
- [ ] Public access test (S3 buckets blocked)
- [ ] IAM permission test (least-privilege)
- [ ] Configuration test (property values correct)
- [ ] Integration test (event routing, service connections)
- [ ] Error handling test (catch blocks, retries)
- [ ] Observability test (logging, tracing, alarms)
- [ ] Environment-specific test (dev/stage/prod variations)

### Implementation Checklist

When implementing CDK constructs:

- [ ] Use strong typing (avoid `var` for public APIs)
- [ ] Enable encryption by default
- [ ] Block public access by default
- [ ] Use CDK grant methods for IAM permissions
- [ ] Add resource tags (Environment, Project, ManagedBy)
- [ ] Set appropriate removal policies (RETAIN for data stores)
- [ ] Enable versioning where applicable (S3, DynamoDB PITR)
- [ ] Configure CloudWatch logging
- [ ] Add X-Ray tracing for Lambda and Step Functions
- [ ] Document rationale for architectural decisions

### Pre-PR Checklist

Before submitting a pull request:

- [ ] All tests pass locally: `dotnet test src/CdkBase.sln`
- [ ] CDK synth succeeds: `cdk synth`
- [ ] No compilation warnings: `dotnet build src/CdkBase.sln`
- [ ] ARCHITECTURE.md updated (if new patterns introduced)
- [ ] Code follows project conventions (naming, structure)
- [ ] Security best practices followed
- [ ] Environment-specific configs tested
- [ ] CI pipeline passes

---

## 🎨 Common Patterns Library

### Secure S3 Bucket Pattern

```csharp
var encryptionKey = new Key(this, "BucketKey", new KeyProps
{
    EnableKeyRotation = true,
    Description = "Encryption key for [bucket purpose]"
});

var bucket = new Bucket(this, "[BucketId]", new BucketProps
{
    Encryption = BucketEncryption.KMS,
    EncryptionKey = encryptionKey,
    BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
    Versioned = true, // or false, depending on requirements
    RemovalPolicy = RemovalPolicy.RETAIN,
    EnforceSSL = true,
    EventBridgeEnabled = true // If event-driven architecture
});
```

### Least-Privilege IAM Role Pattern

```csharp
var role = new Role(this, "[RoleId]", new RoleProps
{
    AssumedBy = new ServicePrincipal("[service].amazonaws.com"),
    Description = "Role for [component purpose]"
});

// Use grant methods instead of manual policies
inputBucket.GrantRead(role);
outputBucket.GrantWrite(role);
table.GrantReadWriteData(role);

// For specific actions, use scoped policies
role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
{
    Effect = Effect.ALLOW,
    Actions = new[] { "specific:Action" },
    Resources = new[] { "arn:aws:service:region:account:resource/*" }
}));
```

### Step Functions with Error Handling Pattern

```csharp
var errorHandlerState = new DynamoPutItem(this, "UpdateStatusFailed", new DynamoPutItemProps
{
    Table = metadataTable,
    Item = new Dictionary<string, DynamoAttributeValue>
    {
        { "audioId", DynamoAttributeValue.FromString(JsonPath.StringAt("$.detail.object.key")) },
        { "status", DynamoAttributeValue.FromString("FAILED") },
        { "errorDetails", DynamoAttributeValue.FromString(JsonPath.StringAt("$.error")) }
    }
});

var taskState = new LambdaInvoke(this, "ProcessTask", new LambdaInvokeProps
{
    LambdaFunction = processingFunction,
    ResultPath = "$.processResult"
});

// Add retry policy
taskState.AddRetry(new RetryProps
{
    Errors = new[] { "Lambda.ServiceException", "Lambda.TooManyRequestsException" },
    Interval = Duration.Seconds(2),
    MaxAttempts = 2,
    BackoffRate = 2.0
});

// Add catch block
taskState.AddCatch(errorHandlerState, new CatchProps
{
    Errors = new[] { "States.ALL" },
    ResultPath = "$.error"
});
```

### Multi-Environment Configuration Pattern

```csharp
// In Program.cs or stack constructor
var environment = app.Node.TryGetContext("environment") as string ?? "dev";

// In cdk.json
{
  "context": {
    "environments": {
      "dev": {
        "logRetentionDays": 7,
        "enableAlarms": false,
        "lambdaMemorySize": 256
      },
      "prod": {
        "logRetentionDays": 90,
        "enableAlarms": true,
        "lambdaMemorySize": 1024
      }
    }
  }
}

// Apply environment-specific config
var logRetention = (environment == "prod") ? 90 : 7;
```

---

## 🚀 Agent Success Patterns

### Pattern: "Architecture First, Code Second"

**Problem**: Agents implementing solutions that don't align with system architecture  
**Solution**: Always consult ARCHITECTURE.md before writing any code

**Agent Instruction**:
```
"Before implementing [FEATURE], review ARCHITECTURE.md to understand:
1. How this component fits in the overall system
2. What AWS services are already in use
3. Security patterns already established
4. Naming conventions for similar components"
```

### Pattern: "Test Names as Specifications"

**Problem**: Unclear test requirements  
**Solution**: Use descriptive test method names that read as specifications

**Good Examples**:
```csharp
InputBucket_ShouldHaveKMSEncryptionEnabled()
EventBridgeRule_ShouldTargetStepFunctionsStateMachine()
LambdaFunction_ShouldHaveLeastPrivilegeIAMPermissions()
```

**Bad Examples**:
```csharp
TestBucket()
CheckRule()
ValidatePermissions()
```

### Pattern: "Incremental Complexity"

**Problem**: Trying to implement everything at once  
**Solution**: Break complex features into small, testable increments

**Example Progression**:
1. Issue #1: Create S3 bucket (just existence)
2. Issue #2: Add encryption to S3 bucket
3. Issue #3: Block public access on S3 bucket
4. Issue #4: Add EventBridge notifications

### Pattern: "Security by Default"

**Problem**: Adding security as an afterthought  
**Solution**: Include security tests in the initial issue

**Security Test Template**:
```csharp
// Always include these tests for new resources
[Fact] public void [Resource]_ShouldHaveEncryptionEnabled() { }
[Fact] public void [Resource]_ShouldBlockPublicAccess() { }
[Fact] public void [Resource]_ShouldHaveLeastPrivilegeIAM() { }
[Fact] public void [Resource]_ShouldEnforceHTTPS() { }
```

---

## 📖 Meta-Prompt Examples

### Example 1: Bootstrapping a New IaC Project

```markdown
You are an AI agent tasked with creating a new AWS CDK infrastructure project following TDD principles.

**Project Setup Requirements**:
1. Create project structure with src/, tests/, and docs/ folders
2. Initialize .NET CDK app with C#
3. Set up xUnit test project with Amazon.CDK.Assertions
4. Create ARCHITECTURE.md documenting planned infrastructure
5. Create AGENT_GUIDELINES.md with TDD workflow
6. Configure GitHub Actions CI pipeline

**Success Criteria**:
- `dotnet build` succeeds
- `dotnet test` runs (even with no tests yet)
- `cdk synth` generates valid CloudFormation
- CI pipeline configured and passing

Follow strict TDD: Write failing tests first, then implement.
```

### Example 2: Adding a New AWS Service

```markdown
You are an AI agent implementing [AWS_SERVICE] integration.

**Context**: Review ARCHITECTURE.md section on [RELATED_COMPONENTS]

**Requirements**:
1. Write tests first (Red phase):
   - Resource existence test
   - Security configuration tests
   - Integration with existing components
   
2. Implement minimal code (Green phase):
   - Add CDK construct for [AWS_SERVICE]
   - Configure security (encryption, IAM)
   - Wire to existing components
   
3. Refactor (Refactor phase):
   - Extract reusable patterns to constructs
   - Add XML documentation
   - Update ARCHITECTURE.md

**Constraints**:
- Use customer-managed KMS keys
- Follow least-privilege IAM principle
- Enable CloudWatch logging
- Add environment tags

**Success Criteria**:
- All tests pass
- CDK synth succeeds
- ARCHITECTURE.md updated
- Security checklist completed
```

---

## 🎓 Lessons from the Sleep Audio Pipeline

### Key Insights

1. **TDD Prevents Over-Engineering**: Tests force focus on requirements, not "nice-to-haves"
2. **Architecture Documentation is Critical**: Agents need a single source of truth to maintain consistency
3. **Small Issues Win**: 12 focused issues beat 1 massive feature branch
4. **Security Tests Catch Issues Early**: Encryption and IAM tests prevented multiple potential vulnerabilities
5. **CI Automation Enables Confidence**: Every commit validated without manual intervention

### What Worked Well

- **Issue Templates**: Consistent structure made agent instructions clear
- **ARCHITECTURE.md as North Star**: Prevented architectural drift
- **CDK Assertions**: Made infrastructure testing straightforward
- **Incremental Approach**: Each issue built on previous work without breaking changes

### What Could Improve

- **Earlier Multi-Environment Setup**: Would have saved refactoring in later issues
- **More Integration Tests**: CDK tests validate templates, but real deployments found edge cases
- **Cost Tracking from Day 1**: Would have benefited from early AWS Budget setup

---

## 🔗 References

### Internal Documentation
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture and design decisions
- [AGENT_GUIDELINES.md](AGENT_GUIDELINES.md) - Development standards and patterns
- [SUMMARY.md](SUMMARY.md) - Project overview and lessons learned

### AWS Best Practices
- AWS Well-Architected Framework (Security, reliability, performance)
- AWS CDK Best Practices (Official CDK documentation)
- Infrastructure as Code Best Practices (AWS whitepaper)

---

**This document is a living artifact**: Extract patterns from new projects and add them here to build a comprehensive meta-prompting library for agentic IaC development.
