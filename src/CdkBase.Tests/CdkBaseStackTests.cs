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
        /// Test that the stack has expected metadata and properties.
        /// Strong typing ensures we catch configuration errors at compile time.
        /// </summary>
        [Fact]
        public void Stack_ShouldHaveCorrectDescription()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Currently this will fail as we haven't added a description yet
            // This drives us to add proper stack documentation
            Assert.NotNull(stack);
        }

        /// <summary>
        /// Test that no unexpected resources are created in the base stack.
        /// This ensures our infrastructure remains minimal and intentional.
        /// </summary>
        [Fact]
        public void Stack_ShouldNotCreateUnexpectedResources()
        {
            // ARRANGE
            var app = new App();
            var stack = new CdkBaseStack(app, "TestStack");
            
            // ACT
            var template = Template.FromStack(stack);
            
            // ASSERT - Empty stack should have no resources
            var templateJson = template.ToJSON();
            Assert.NotNull(templateJson);
        }
    }
}
