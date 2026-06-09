using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CdkBase
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            
            // Retrieve environment from CDK context (can be set via -c environment=dev)
            // Defaults to null for environment-agnostic deployment
            var environmentName = app.Node.TryGetContext("environment")?.ToString();
            
            // Build stack ID with environment suffix if specified
            var stackId = string.IsNullOrEmpty(environmentName) 
                ? "CdkBaseStack" 
                : $"CdkBaseStack-{environmentName}";
            
            // Create the main application stack with environment support
            new CdkBaseStack(app, stackId, new StackProps
            {
                // If you don't specify 'env', this stack will be environment-agnostic.
                // Account/Region-dependent features and context lookups will not work,
                // but a single synthesized template can be deployed anywhere.

                // Uncomment the next block to specialize this stack for the AWS Account
                // and Region that are implied by the current CLI configuration.
                /*
                Env = new Amazon.CDK.Environment
                {
                    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                    Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION"),
                }
                */

                // Uncomment the next block if you know exactly what Account and Region you
                // want to deploy the stack to.
                /*
                Env = new Amazon.CDK.Environment
                {
                    Account = "123456789012",
                    Region = "us-east-1",
                }
                */

                // For more information, see https://docs.aws.amazon.com/cdk/latest/guide/environments.html
            }, environmentName);
            
            // Optionally retrieve environment-specific configurations from context
            if (!string.IsNullOrEmpty(environmentName))
            {
                try
                {
                    var envConfig = app.Node.TryGetContext("environments") as Dictionary<string, object>;
                    if (envConfig?.ContainsKey(environmentName) == true)
                    {
                        Console.WriteLine($"Deploying to environment: {environmentName}");
                        // Environment-specific configuration is available in context
                        // Future: Apply environment-specific settings here
                    }
                }
                catch
                {
                    // Environment context not found - proceed with defaults
                }
            }
            app.Synth();
        }
    }
}
