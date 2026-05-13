# AWS CDK Sleep Audio Pipeline (C# TDD)

> **Event-driven sleep audio pipeline built with AWS CDK, C#, and Test-Driven Development**

[![CI](https://github.com/obstreperous-ai/cdk-sleep-csharp-qdev/actions/workflows/ci.yml/badge.svg)](https://github.com/obstreperous-ai/cdk-sleep-csharp-qdev/actions/workflows/ci.yml)

## 🎯 Project Vision

This project follows a **TDD-first, issue-driven development** approach to build a robust, event-driven AWS infrastructure for sleep audio processing.

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
2. Write failing tests first (TDD!)
3. Implement minimal code to pass tests
4. Ensure all tests pass: `dotnet test src/CdkBase.sln`
5. Ensure CDK synth works: `cdk synth`
6. Submit pull request

## 📖 Documentation

- [Architecture Documentation](docs/ARCHITECTURE.md) - Detailed system design
- [AWS CDK C# Developer Guide](https://docs.aws.amazon.com/cdk/api/v2/dotnet/api/) - Official CDK reference

## 📝 License

See [LICENSE](LICENSE) file for details.

---

**Built with ❤️ using TDD, AWS CDK, and C#**
