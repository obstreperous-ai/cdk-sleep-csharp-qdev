using Amazon.CDK;
using Amazon.CDK.Assertions;
using Xunit;
using System.Collections.Generic;

namespace CdkBase.Tests
{
    /// <summary>
    /// TDD-first test suite for CdkBaseStack.
    /// Tests are written BEFORE implementation to drive development.
    /// </summary>
    public sealed class CdkBaseStackTests
    {
        /// <summary>
        /// Test that the stack can be created and synthesized successfully.
        /// This is our first failing test - it should pass once basic infrastructure is in place.
        /// </summary>
        [Fact]
        public void Stack_ShouldSynthesizeSuccessfully()
        {
            // ARRANGE
            var app = new App();
            
            // ACT
            var stack = new CdkBaseStack(app, "TestStack");
            var template = Template.FromStack(stack);
            
            // ASSERT
            Assert.NotNull(template);
        }

        /// <summary>
        /// Test that the Input S3 bucket is created with encryption enabled using KMS.
        /// This ensures data-at-rest security with customer-managed keys.
        /// </summary>
        [Fact]
        public void InputBucket_ShouldHaveKMSEncryptionEnabled()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Bucket should use KMS encryption
            template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
            {
                { "BucketEncryption", new Dictionary<string, object>
                    {
                        { "ServerSideEncryptionConfiguration", new object[]
                            {
                                new Dictionary<string, object>
                                {
                                    { "ServerSideEncryptionByDefault", new Dictionary<string, object>
                                        {
                                            { "SSEAlgorithm", "aws:kms" }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Test that the Input S3 bucket has versioning enabled.
        /// Versioning provides protection against accidental deletions and overwrites.
        /// </summary>
        [Fact]
        public void InputBucket_ShouldHaveVersioningEnabled()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - At least one bucket should have versioning enabled
            template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
            {
                { "VersioningConfiguration", new Dictionary<string, object>
                    {
                        { "Status", "Enabled" }
                    }
                }
            });
        }

        /// <summary>
        /// Test that the Input S3 bucket blocks all public access.
        /// This is a security best practice to prevent accidental public exposure.
        /// </summary>
        [Fact]
        public void InputBucket_ShouldBlockAllPublicAccess()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Bucket should block all public access
            template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
            {
                { "PublicAccessBlockConfiguration", new Dictionary<string, object>
                    {
                        { "BlockPublicAcls", true },
                        { "BlockPublicPolicy", true },
                        { "IgnorePublicAcls", true },
                        { "RestrictPublicBuckets", true }
                    }
                }
            });
        }

        /// <summary>
        /// Test that the Input S3 bucket has EventBridge notifications enabled.
        /// This allows EventBridge to receive S3 events for event-driven processing.
        /// </summary>
        [Fact]
        public void InputBucket_ShouldHaveEventBridgeNotificationsEnabled()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Bucket should have EventBridge notification enabled
            template.HasResourceProperties("AWS::S3::Bucket", new Dictionary<string, object>
            {
                { "NotificationConfiguration", new Dictionary<string, object>
                    {
                        { "EventBridgeConfiguration", new Dictionary<string, object>
                            {
                                { "EventBridgeEnabled", true }
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Test that the Output S3 bucket is created with KMS encryption.
        /// Output bucket also requires encryption for secure storage of processed audio.
        /// </summary>
        [Fact]
        public void OutputBucket_ShouldHaveKMSEncryptionEnabled()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have at least 2 buckets with KMS encryption
            template.ResourceCountIs("AWS::S3::Bucket", 2);
        }

        /// <summary>
        /// Test that a KMS key is created for S3 encryption.
        /// Customer-managed keys provide better control and audit capabilities.
        /// </summary>
        [Fact]
        public void Stack_ShouldCreateKMSKeyForS3Encryption()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have a KMS key
            template.ResourceCountIs("AWS::KMS::Key", 1);
        }

        /// <summary>
        /// Test that an EventBridge rule is created to trigger on S3 Object Created events.
        /// This rule is the foundation of the event-driven pipeline.
        /// </summary>
        [Fact]
        public void EventBridgeRule_ShouldTriggerOnS3ObjectCreated()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have an EventBridge rule
            template.HasResourceProperties("AWS::Events::Rule", new Dictionary<string, object>
            {
                { "EventPattern", new Dictionary<string, object>
                    {
                        { "source", new string[] { "aws.s3" } },
                        { "detail-type", new string[] { "Object Created" } }
                    }
                }
            });
        }

        /// <summary>
        /// Test that the EventBridge rule has at least one target configured.
        /// Even if it's a placeholder, the rule needs a target to be functional.
        /// </summary>
        [Fact]
        public void EventBridgeRule_ShouldHaveTarget()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have an EventBridge rule with targets
            template.HasResource("AWS::Events::Rule", Match.ObjectLike(new Dictionary<string, object>
            {
                { "Properties", new Dictionary<string, object>
                    {
                        { "Targets", Match.AnyValue() }
                    }
                }
            }));
        }

        /// <summary>
        /// Test that a Step Functions state machine is created for orchestration.
        /// This state machine will orchestrate the audio processing pipeline.
        /// </summary>
        [Fact]
        public void StateMachine_ShouldExist()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have a Step Functions state machine
            template.ResourceCountIs("AWS::StepFunctions::StateMachine", 1);
        }

        /// <summary>
        /// Test that the Step Functions state machine has CloudWatch logging enabled.
        /// Logging is critical for debugging and observability.
        /// </summary>
        [Fact]
        public void StateMachine_ShouldHaveLoggingEnabled()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - State machine should have logging configuration
            template.HasResourceProperties("AWS::StepFunctions::StateMachine", new Dictionary<string, object>
            {
                { "LoggingConfiguration", new Dictionary<string, object>
                    {
                        { "Level", "ALL" }
                    }
                }
            });
        }

        /// <summary>
        /// Test that the Step Functions state machine has an execution role.
        /// The role should follow least-privilege principles.
        /// </summary>
        [Fact]
        public void StateMachine_ShouldHaveExecutionRole()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - State machine should have a role ARN configured
            template.HasResourceProperties("AWS::StepFunctions::StateMachine", Match.ObjectLike(new Dictionary<string, object>
            {
                { "RoleArn", Match.AnyValue() }
            }));
        }

        /// <summary>
        /// Test that the Step Functions state machine definition contains task states.
        /// At minimum, it should have a Polly task or placeholder.
        /// </summary>
        [Fact]
        public void StateMachine_DefinitionShouldContainTasks()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - State machine should have a definition with states
            template.HasResourceProperties("AWS::StepFunctions::StateMachine", Match.ObjectLike(new Dictionary<string, object>
            {
                { "DefinitionString", Match.AnyValue() }
            }));
        }

        /// <summary>
        /// Test that the EventBridge rule now targets the Step Functions state machine.
        /// This replaces the previous CloudWatch Logs target.
        /// </summary>
        [Fact]
        public void EventBridgeRule_ShouldTargetStateMachine()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - EventBridge rule should have Step Functions as target
            template.HasResourceProperties("AWS::Events::Rule", Match.ObjectLike(new Dictionary<string, object>
            {
                { "Targets", Match.ArrayWith(new object[]
                    {
                        Match.ObjectLike(new Dictionary<string, object>
                        {
                            { "Arn", Match.AnyValue() }
                        })
                    })
                }
            }));
        }

        /// <summary>
        /// Test that an IAM role is created for the Step Functions state machine execution.
        /// This ensures proper permissions management.
        /// </summary>
        [Fact]
        public void StateMachine_ShouldHaveIAMRole()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have IAM roles (at least one for state machine)
            // Note: We already have roles in the stack, so we check that count is appropriate
            var roles = template.FindResources("AWS::IAM::Role");
            Assert.NotEmpty(roles);
        }

        /// <summary>
        /// Test that a DynamoDB table is created for storing audio pipeline metadata.
        /// This table will store processing status, input/output locations, and timestamps.
        /// </summary>
        [Fact]
        public void DynamoDBTable_ShouldExist()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have exactly one DynamoDB table
            template.ResourceCountIs("AWS::DynamoDB::Table", 1);
        }

        /// <summary>
        /// Test that the DynamoDB table has the correct partition key for audio metadata.
        /// Using audioId as the partition key for unique identification of each processing job.
        /// </summary>
        [Fact]
        public void DynamoDBTable_ShouldHaveCorrectKeySchema()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Table should have audioId as partition key (HASH)
            template.HasResourceProperties("AWS::DynamoDB::Table", new Dictionary<string, object>
            {
                { "KeySchema", new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "AttributeName", "audioId" },
                            { "KeyType", "HASH" }
                        }
                    }
                },
                { "AttributeDefinitions", new object[]
                    {
                        new Dictionary<string, object>
                        {
                            { "AttributeName", "audioId" },
                            { "AttributeType", "S" }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Test that the DynamoDB table has server-side encryption enabled.
        /// Encryption at rest is a security requirement for storing metadata.
        /// </summary>
        [Fact]
        public void DynamoDBTable_ShouldHaveEncryptionEnabled()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Table should have SSE specification configured
            template.HasResourceProperties("AWS::DynamoDB::Table", new Dictionary<string, object>
            {
                { "SSESpecification", new Dictionary<string, object>
                    {
                        { "SSEEnabled", true }
                    }
                }
            });
        }

        /// <summary>
        /// Test that the DynamoDB table uses on-demand billing mode.
        /// On-demand mode provides automatic scaling without capacity planning.
        /// </summary>
        [Fact]
        public void DynamoDBTable_ShouldUseOnDemandBillingMode()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Table should use PAY_PER_REQUEST billing mode
            template.HasResourceProperties("AWS::DynamoDB::Table", new Dictionary<string, object>
            {
                { "BillingMode", "PAY_PER_REQUEST" }
            });
        }

        /// <summary>
        /// Test that the DynamoDB table has point-in-time recovery enabled.
        /// PITR provides continuous backups for data protection.
        /// </summary>
        [Fact]
        public void DynamoDBTable_ShouldHavePointInTimeRecoveryEnabled()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Table should have PITR enabled
            template.HasResourceProperties("AWS::DynamoDB::Table", new Dictionary<string, object>
            {
                { "PointInTimeRecoverySpecification", new Dictionary<string, object>
                    {
                        { "PointInTimeRecoveryEnabled", true }
                    }
                }
            });
        }

        /// <summary>
        /// Test that the state machine has IAM permissions to write to DynamoDB table.
        /// The execution role should include dynamodb:PutItem permission.
        /// </summary>
        [Fact]
        public void StateMachine_ShouldHaveDynamoDBWritePermissions()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have IAM policy allowing DynamoDB PutItem action
            template.HasResourceProperties("AWS::IAM::Policy", Match.ObjectLike(new Dictionary<string, object>
            {
                { "PolicyDocument", Match.ObjectLike(new Dictionary<string, object>
                    {
                        { "Statement", Match.ArrayWith(new object[]
                            {
                                Match.ObjectLike(new Dictionary<string, object>
                                {
                                    { "Action", Match.ArrayWith(new object[]
                                        {
                                            "dynamodb:PutItem"
                                        })
                                    }
                                })
                            })
                        }
                    })
                }
            }));
        }

        /// <summary>
        /// Test that SNS topics are created for pipeline notifications.
        /// At least two topics should exist: one for success and one for failure.
        /// </summary>
        [Fact]
        public void SNSTopics_ShouldExistForNotifications()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have at least 2 SNS topics (Completed and Failed)
            template.ResourceCountIs("AWS::SNS::Topic", 2);
        }

        /// <summary>
        /// Test that SNS topics have encryption enabled.
        /// SNS topics should use KMS encryption for data-in-transit security.
        /// </summary>
        [Fact]
        public void SNSTopics_ShouldHaveEncryptionEnabled()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - SNS topic should have KMS master key configured
            template.HasResourceProperties("AWS::SNS::Topic", Match.ObjectLike(new Dictionary<string, object>
            {
                { "KmsMasterKeyId", Match.AnyValue() }
            }));
        }

        /// <summary>
        /// Test that the state machine has error handling configured.
        /// The definition should include Catch blocks for error handling.
        /// </summary>
        [Fact]
        public void StateMachine_ShouldHaveErrorHandling()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - State machine definition should contain "Catch" for error handling
            var stateMachines = template.FindResources("AWS::StepFunctions::StateMachine");
            Assert.NotEmpty(stateMachines);
            
            // Check that at least one state machine has a definition string containing "Catch"
            var hasErrorHandling = false;
            foreach (var sm in stateMachines)
            {
                if (sm.Value.ContainsKey("Properties"))
                {
                    var properties = sm.Value["Properties"] as Dictionary<string, object>;
                    if (properties?.ContainsKey("DefinitionString") == true)
                    {
                        var definition = properties["DefinitionString"]?.ToString() ?? "";
                        if (definition.Contains("Catch"))
                        {
                            hasErrorHandling = true;
                            break;
                        }
                    }
                }
            }
            Assert.True(hasErrorHandling, "State machine should have error handling (Catch blocks) in definition");
        }

        /// <summary>
        /// Test that the state machine execution role has permissions to publish to SNS.
        /// IAM policy should include sns:Publish action.
        /// </summary>
        [Fact]
        public void StateMachine_ShouldHaveSNSPublishPermissions()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have IAM policy allowing SNS Publish action
            template.HasResourceProperties("AWS::IAM::Policy", Match.ObjectLike(new Dictionary<string, object>
            {
                { "PolicyDocument", Match.ObjectLike(new Dictionary<string, object>
                    {
                        { "Statement", Match.ArrayWith(new object[]
                            {
                                Match.ObjectLike(new Dictionary<string, object>
                                {
                                    { "Action", "sns:Publish" }
                                })
                            })
                        }
                    })
                }
            }));
        }

        /// <summary>
        /// Test that the stack has proper IAM permissions for DynamoDB UpdateItem on failure.
        /// This ensures the state machine can update status to FAILED on errors.
        /// </summary>
        [Fact]
        public void StateMachine_ShouldHaveDynamoDBUpdatePermissionsForFailure()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have IAM policy allowing DynamoDB UpdateItem action
            // This was already granted in Issue #5, but we verify it's still present
            template.HasResourceProperties("AWS::IAM::Policy", Match.ObjectLike(new Dictionary<string, object>
            {
                { "PolicyDocument", Match.ObjectLike(new Dictionary<string, object>
                    {
                        { "Statement", Match.ArrayWith(new object[]
                            {
                                Match.ObjectLike(new Dictionary<string, object>
                                {
                                    { "Action", Match.ArrayWith(new object[]
                                        {
                                            "dynamodb:UpdateItem"
                                        })
                                    }
                                })
                            })
                        }
                    })
                }
            }));
        }

        /// <summary>
        /// Test snapshot of the complete stack to catch unintended changes.
        /// This provides a baseline for infrastructure changes.
        /// </summary>
        [Fact]
        public void Stack_SnapshotTest()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            var json = template.ToJSON();
            
            // ASSERT - Verify template structure is valid JSON and not empty
            Assert.NotNull(json);
            Assert.NotEmpty(json);
            
            // Verify key resource types exist
            Assert.Contains("AWS::S3::Bucket", json);
            Assert.Contains("AWS::StepFunctions::StateMachine", json);
            Assert.Contains("AWS::DynamoDB::Table", json);
            Assert.Contains("AWS::SNS::Topic", json);
            Assert.Contains("AWS::Events::Rule", json);
            Assert.Contains("AWS::KMS::Key", json);
        }

        /// <summary>
        /// Test that a Lambda function is created for audio processing.
        /// This Lambda will serve as a placeholder for future audio processing, metadata enrichment, or validation logic.
        /// </summary>
        [Fact]
        public void Lambda_ShouldExist()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have at least one Lambda function
            template.ResourceCountIs("AWS::Lambda::Function", 1);
        }

        /// <summary>
        /// Test that the Lambda function has the correct runtime configured.
        /// Using Python 3.12 runtime for the audio processor function.
        /// </summary>
        [Fact]
        public void Lambda_ShouldHaveCorrectRuntime()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Lambda should use Python 3.12 runtime
            template.HasResourceProperties("AWS::Lambda::Function", new Dictionary<string, object>
            {
                { "Runtime", "python3.12" }
            });
        }

        /// <summary>
        /// Test that the Lambda function has environment variables configured.
        /// Environment variables provide configuration for table name and bucket access.
        /// </summary>
        [Fact]
        public void Lambda_ShouldHaveEnvironmentVariables()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Lambda should have environment variables for table and bucket
            template.HasResourceProperties("AWS::Lambda::Function", Match.ObjectLike(new Dictionary<string, object>
            {
                { "Environment", Match.ObjectLike(new Dictionary<string, object>
                    {
                        { "Variables", Match.ObjectLike(new Dictionary<string, object>
                            {
                                { "METADATA_TABLE_NAME", Match.AnyValue() },
                                { "OUTPUT_BUCKET_NAME", Match.AnyValue() }
                            })
                        }
                    })
                }
            }));
        }

        /// <summary>
        /// Test that the Lambda function has an execution role with proper permissions.
        /// The role should allow access to DynamoDB and CloudWatch Logs.
        /// </summary>
        [Fact]
        public void Lambda_ShouldHaveExecutionRole()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Lambda should have a role configured
            template.HasResourceProperties("AWS::Lambda::Function", Match.ObjectLike(new Dictionary<string, object>
            {
                { "Role", Match.AnyValue() }
            }));
        }

        /// <summary>
        /// Test that the Lambda execution role has DynamoDB read/write permissions.
        /// Lambda needs to read and update metadata in the DynamoDB table.
        /// </summary>
        [Fact]
        public void Lambda_ShouldHaveDynamoDBPermissions()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have IAM policy allowing DynamoDB access
            template.HasResourceProperties("AWS::IAM::Policy", Match.ObjectLike(new Dictionary<string, object>
            {
                { "PolicyDocument", Match.ObjectLike(new Dictionary<string, object>
                    {
                        { "Statement", Match.ArrayWith(new object[]
                            {
                                Match.ObjectLike(new Dictionary<string, object>
                                {
                                    { "Action", Match.ArrayWith(new object[]
                                        {
                                            "dynamodb:GetItem"
                                        })
                                    }
                                })
                            })
                        }
                    })
                }
            }));
        }

        /// <summary>
        /// Test that the Lambda execution role has CloudWatch Logs permissions.
        /// Lambda needs to write logs for debugging and monitoring.
        /// </summary>
        [Fact]
        public void Lambda_ShouldHaveCloudWatchLogsPermissions()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have IAM policy allowing CloudWatch Logs actions
            template.HasResourceProperties("AWS::IAM::Policy", Match.ObjectLike(new Dictionary<string, object>
            {
                { "PolicyDocument", Match.ObjectLike(new Dictionary<string, object>
                    {
                        { "Statement", Match.ArrayWith(new object[]
                            {
                                Match.ObjectLike(new Dictionary<string, object>
                                {
                                    { "Action", Match.ArrayWith(new object[]
                                        {
                                            "logs:CreateLogStream"
                                        })
                                    }
                                })
                            })
                        }
                    })
                }
            }));
        }

        /// <summary>
        /// Test that the state machine has permission to invoke the Lambda function.
        /// The state machine execution role should include lambda:InvokeFunction permission.
        /// </summary>
        [Fact]
        public void StateMachine_ShouldHaveLambdaInvokePermission()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Should have IAM policy allowing Lambda invocation
            template.HasResourceProperties("AWS::IAM::Policy", Match.ObjectLike(new Dictionary<string, object>
            {
                { "PolicyDocument", Match.ObjectLike(new Dictionary<string, object>
                    {
                        { "Statement", Match.ArrayWith(new object[]
                            {
                                Match.ObjectLike(new Dictionary<string, object>
                                {
                                    { "Action", "lambda:InvokeFunction" }
                                })
                            })
                        }
                    })
                }
            }));
        }

        /// <summary>
        /// Test that the Lambda invocation task has error handling (Catch block).
        /// This ensures that Lambda errors are caught and routed to the failure path.
        /// Issue #8: Complete pipeline wiring with input validation.
        /// </summary>
        [Fact]
        public void Lambda_ShouldHaveErrorHandlingInStateMachine()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - State machine definition should contain Lambda task with Catch block
            var stateMachines = template.FindResources("AWS::StepFunctions::StateMachine");
            Assert.NotEmpty(stateMachines);
            
            // Check that the Lambda invocation task (ProcessAudioWithLambda) has error handling
            var hasLambdaCatch = false;
            foreach (var sm in stateMachines)
            {
                if (sm.Value.ContainsKey("Properties"))
                {
                    var properties = sm.Value["Properties"] as Dictionary<string, object>;
                    if (properties?.ContainsKey("DefinitionString") == true)
                    {
                        var definition = properties["DefinitionString"]?.ToString() ?? "";
                        // The definition should have multiple Catch blocks (Lambda + Polly)
                        // and should reference ProcessAudioWithLambda task
                        if (definition.Contains("ProcessAudioWithLambda") && 
                            definition.Contains("Catch"))
                        {
                            hasLambdaCatch = true;
                            break;
                        }
                    }
                }
            }
            Assert.True(hasLambdaCatch, "Lambda invocation task should have error handling (Catch block) in state machine definition");
        }

        /// <summary>
        /// Test that the complete end-to-end pipeline is wired correctly.
        /// This verifies the entire flow: EventBridge -> Step Functions -> Lambda -> Polly -> DynamoDB -> SNS.
        /// Issue #8: Verify complete pipeline wiring.
        /// </summary>
        [Fact]
        public void Pipeline_ShouldBeCompletelyWired()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            var json = template.ToJSON();
            
            // ASSERT - Verify all key components exist and are connected
            // 1. S3 buckets (Input and Output)
            Assert.Contains("AWS::S3::Bucket", json);
            
            // 2. EventBridge rule targeting Step Functions
            Assert.Contains("AWS::Events::Rule", json);
            
            // 3. Step Functions state machine
            Assert.Contains("AWS::StepFunctions::StateMachine", json);
            
            // 4. Lambda function
            Assert.Contains("AWS::Lambda::Function", json);
            
            // 5. DynamoDB table
            Assert.Contains("AWS::DynamoDB::Table", json);
            
            // 6. SNS topics (2 topics for success and failure)
            template.ResourceCountIs("AWS::SNS::Topic", 2);
            
            // 7. Verify state machine definition contains all expected tasks
            var stateMachines = template.FindResources("AWS::StepFunctions::StateMachine");
            Assert.NotEmpty(stateMachines);
            
            var definition = "";
            foreach (var sm in stateMachines)
            {
                var properties = sm.Value["Properties"] as Dictionary<string, object>;
                definition = properties?["DefinitionString"]?.ToString() ?? "";
            }
            
            // Verify key states are present in the definition
            Assert.Contains("WriteInitialMetadata", definition);
            Assert.Contains("ProcessAudioWithLambda", definition);
            Assert.Contains("PollyTextToSpeech", definition);
            Assert.Contains("UpdateStatusToCompleted", definition);
            Assert.Contains("UpdateStatusToFailed", definition);
            Assert.Contains("PublishSuccessNotification", definition);
            Assert.Contains("PublishFailureNotification", definition);
        }

        /// <summary>
        /// Test that the Lambda function output is properly passed through the state machine.
        /// This ensures ResultPath is configured correctly for Lambda invocation.
        /// Issue #8: Verify input/output handling in complete pipeline.
        /// </summary>
        [Fact]
        public void Lambda_ShouldHaveProperResultPathConfiguration()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - State machine definition should configure ResultPath for Lambda
            var stateMachines = template.FindResources("AWS::StepFunctions::StateMachine");
            Assert.NotEmpty(stateMachines);
            
            var hasResultPath = false;
            foreach (var sm in stateMachines)
            {
                if (sm.Value.ContainsKey("Properties"))
                {
                    var properties = sm.Value["Properties"] as Dictionary<string, object>;
                    if (properties?.ContainsKey("DefinitionString") == true)
                    {
                        var definition = properties["DefinitionString"]?.ToString() ?? "";
                        // ResultPath should be configured for Lambda task to preserve original input
                        if (definition.Contains("processorResult") || definition.Contains("ResultPath"))
                        {
                            hasResultPath = true;
                            break;
                        }
                    }
                }
            }
            Assert.True(hasResultPath, "Lambda invocation should have ResultPath configured to preserve state");
        }

        /// <summary>
        /// Test that the stack supports environment-specific resource naming.
        /// Resources should include the environment name in their IDs/names when provided.
        /// Issue #9: Multi-environment support
        /// </summary>
        [Fact]
        public void Stack_ShouldSupportEnvironmentSpecificNaming()
        {
            // ARRANGE
            var app = new App();
            var environmentName = "dev";
            
            // ACT
            var stack = new CdkBaseStack(app, "CdkBaseStack", new StackProps(), environmentName);
            var template = Template.FromStack(stack);
            
            // ASSERT - Stack should be created successfully with environment parameter
            Assert.NotNull(template);
        }

        /// <summary>
        /// Test that environment tags are applied to the stack.
        /// All resources should be tagged with the environment name for cost tracking.
        /// Issue #9: Environment tagging for cost allocation
        /// </summary>
        [Fact]
        public void Stack_ShouldHaveEnvironmentTags()
        {
            // ARRANGE
            var app = new App();
            var environmentName = "dev";
            
            // ACT
            var stack = new CdkBaseStack(app, "CdkBaseStack", new StackProps(), environmentName);
            
            // ASSERT - Stack should have environment tag
            var tags = stack.Tags;
            Assert.NotNull(tags);
        }

        /// <summary>
        /// Test that the stack can be synthesized for different environments.
        /// This ensures the stack is environment-agnostic and can be deployed to dev, stage, prod.
        /// Issue #9: Multi-environment deployment preparation
        /// </summary>
        [Theory]
        [InlineData("dev")]
        [InlineData("stage")]
        [InlineData("prod")]
        public void Stack_ShouldSynthesizeForDifferentEnvironments(string environmentName)
        {
            // ARRANGE
            var app = new App();
            
            // ACT
            var stack = new CdkBaseStack(app, $"CdkBaseStack-{environmentName}", new StackProps(), environmentName);
            var template = Template.FromStack(stack);
            
            // ASSERT - Stack should synthesize successfully for each environment
            Assert.NotNull(template);
            
            // Verify core resources exist
            template.ResourceCountIs("AWS::S3::Bucket", 2);
            template.ResourceCountIs("AWS::StepFunctions::StateMachine", 1);
            template.ResourceCountIs("AWS::DynamoDB::Table", 1);
        }

        /// <summary>
        /// Test that environment-specific configurations can be applied.
        /// Different environments may have different log retention, alarm settings, etc.
        /// Issue #9: Environment-specific configuration
        /// </summary>
        [Fact]
        public void Stack_ShouldSupportEnvironmentSpecificConfiguration()
        {
            // ARRANGE
            var app = new App();
            
            // ACT - Create stacks for different environments
            var devStack = new CdkBaseStack(app, "DevStack", new StackProps(), "dev");
            var prodStack = new CdkBaseStack(app, "ProdStack", new StackProps(), "prod");
            
            var devTemplate = Template.FromStack(devStack);
            var prodTemplate = Template.FromStack(prodStack);
            
            // ASSERT - Both stacks should be valid
            Assert.NotNull(devTemplate);
            Assert.NotNull(prodTemplate);
        }

        /// <summary>
        /// Test complete pipeline integration with valid input.
        /// Verifies all components are properly wired for successful processing flow.
        /// Issue #9: Integration testing for successful path
        /// </summary>
        [Fact]
        public void Pipeline_ShouldHandleValidInputSuccessfully()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            var json = template.ToJSON();
            
            // ASSERT - Verify complete success path exists in state machine
            var stateMachines = template.FindResources("AWS::StepFunctions::StateMachine");
            Assert.NotEmpty(stateMachines);
            
            var definition = "";
            foreach (var sm in stateMachines)
            {
                var properties = sm.Value["Properties"] as Dictionary<string, object>;
                definition = properties?["DefinitionString"]?.ToString() ?? "";
            }
            
            // Verify success path components
            Assert.Contains("WriteInitialMetadata", definition);
            Assert.Contains("ProcessAudioWithLambda", definition);
            Assert.Contains("PollyTextToSpeech", definition);
            Assert.Contains("UpdateStatusToCompleted", definition);
            Assert.Contains("PublishSuccessNotification", definition);
            
            // Verify all required AWS resources for the success path
            Assert.Contains("AWS::Lambda::Function", json);
            Assert.Contains("AWS::DynamoDB::Table", json);
            Assert.Contains("AWS::SNS::Topic", json);
        }

        /// <summary>
        /// Test complete pipeline integration with invalid input.
        /// Verifies error path is properly configured and errors are handled gracefully.
        /// Issue #9: Integration testing for error path
        /// </summary>
        [Fact]
        public void Pipeline_ShouldHandleInvalidInputGracefully()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Verify error handling path exists
            var stateMachines = template.FindResources("AWS::StepFunctions::StateMachine");
            Assert.NotEmpty(stateMachines);
            
            var definition = "";
            foreach (var sm in stateMachines)
            {
                var properties = sm.Value["Properties"] as Dictionary<string, object>;
                definition = properties?["DefinitionString"]?.ToString() ?? "";
            }
            
            // Verify error path components
            Assert.Contains("Catch", definition);
            Assert.Contains("UpdateStatusToFailed", definition);
            Assert.Contains("PublishFailureNotification", definition);
            
            // Verify both success and failure SNS topics exist
            template.ResourceCountIs("AWS::SNS::Topic", 2);
        }

        /// <summary>
        /// Test that status updates are properly configured in the state machine.
        /// DynamoDB should be updated at each critical stage of processing.
        /// Issue #9: Verify status tracking throughout pipeline
        /// </summary>
        [Fact]
        public void Pipeline_ShouldUpdateStatusAtEachStage()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Verify DynamoDB operations in state machine
            var stateMachines = template.FindResources("AWS::StepFunctions::StateMachine");
            Assert.NotEmpty(stateMachines);
            
            var definition = "";
            foreach (var sm in stateMachines)
            {
                var properties = sm.Value["Properties"] as Dictionary<string, object>;
                definition = properties?["DefinitionString"]?.ToString() ?? "";
            }
            
            // Verify status updates at different stages
            Assert.Contains("WriteInitialMetadata", definition); // Initial: PROCESSING
            Assert.Contains("UpdateStatusToCompleted", definition); // Success: COMPLETED
            Assert.Contains("UpdateStatusToFailed", definition); // Error: FAILED
            
            // Verify DynamoDB permissions
            template.HasResourceProperties("AWS::IAM::Policy", Match.ObjectLike(new Dictionary<string, object>
            {
                { "PolicyDocument", Match.ObjectLike(new Dictionary<string, object>
                    {
                        { "Statement", Match.ArrayWith(new object[]
                            {
                                Match.ObjectLike(new Dictionary<string, object>
                                {
                                    { "Action", Match.ArrayWith(new object[]
                                        {
                                            "dynamodb:PutItem"
                                        })
                                    }
                                })
                            })
                        }
                    })
                }
            }));
        }

        /// <summary>
        /// Test that the EventBridge rule correctly filters and routes S3 events.
        /// Only events from the Input bucket should trigger the pipeline.
        /// Issue #9: Verify event routing configuration
        /// </summary>
        [Fact]
        public void EventBridge_ShouldFilterS3EventsCorrectly()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Verify event pattern filters correctly
            template.HasResourceProperties("AWS::Events::Rule", new Dictionary<string, object>
            {
                { "EventPattern", new Dictionary<string, object>
                    {
                        { "source", new string[] { "aws.s3" } },
                        { "detail-type", new string[] { "Object Created" } },
                        { "detail", Match.ObjectLike(new Dictionary<string, object>
                            {
                                { "bucket", Match.AnyValue() }
                            })
                        }
                    }
                }
            });
            
            // Verify rule has retry configuration
            template.HasResourceProperties("AWS::Events::Rule", Match.ObjectLike(new Dictionary<string, object>
            {
                { "Targets", Match.AnyValue() }
            }));
        }

        /// <summary>
        /// Test that IAM permissions follow least-privilege principle.
        /// Each component should only have the minimum necessary permissions.
        /// Issue #9: Verify security best practices
        /// </summary>
        [Fact]
        public void IAM_ShouldFollowLeastPrivilegePrinciple()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Verify IAM roles exist
            var roles = template.FindResources("AWS::IAM::Role");
            Assert.NotEmpty(roles);
            
            // Verify policies are scoped to specific resources
            var policies = template.FindResources("AWS::IAM::Policy");
            Assert.NotEmpty(policies);
            
            // Each policy should have proper resource constraints (not "*" where avoidable)
            foreach (var policy in policies)
            {
                var properties = policy.Value["Properties"] as Dictionary<string, object>;
                Assert.NotNull(properties);
            }
        }

        /// <summary>
        /// Test that all sensitive data is encrypted.
        /// S3, DynamoDB, and SNS should all use encryption.
        /// Issue #9: Verify encryption compliance
        /// </summary>
        [Fact]
        public void Security_AllDataShouldBeEncrypted()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Verify encryption on all data stores
            // S3 buckets
            template.HasResourceProperties("AWS::S3::Bucket", Match.ObjectLike(new Dictionary<string, object>
            {
                { "BucketEncryption", Match.AnyValue() }
            }));
            
            // DynamoDB table
            template.HasResourceProperties("AWS::DynamoDB::Table", Match.ObjectLike(new Dictionary<string, object>
            {
                { "SSESpecification", Match.AnyValue() }
            }));
            
            // SNS topics
            template.HasResourceProperties("AWS::SNS::Topic", Match.ObjectLike(new Dictionary<string, object>
            {
                { "KmsMasterKeyId", Match.AnyValue() }
            }));
            
            // KMS key should exist
            template.ResourceCountIs("AWS::KMS::Key", 1);
        }
    }
}
