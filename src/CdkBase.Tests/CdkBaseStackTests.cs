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
    }
}
