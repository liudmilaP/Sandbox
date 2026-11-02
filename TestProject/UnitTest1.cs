using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Moq;
using Xunit;

namespace SwimNest.Plugins.Tests;

public class ContactPostCreatePluginTests
{
    [Fact]
    public void Execute_WhenContactIsCreatedInPostOperation_CreatesAndSendsWelcomeEmail()
    {
        // Arrange
        var plugin = new ContactPostCreatePlugin();

        var contact = new Entity("contact")
        {
            Id = Guid.NewGuid(),
            ["firstname"] = "John"
        };

        var context = new Mock<IPluginExecutionContext>();
        context.SetupGet(x => x.MessageName).Returns("Create");
        context.SetupGet(x => x.PrimaryEntityName).Returns("contact");
        context.SetupGet(x => x.Stage).Returns(40);
        context.SetupGet(x => x.PrimaryEntityId).Returns(contact.Id);
        context.SetupGet(x => x.InputParameters).Returns(new ParameterCollection
        {
            ["Target"] = contact
        });

        var tracing = new Mock<ITracingService>();
        var organizationService = new Mock<IOrganizationService>();
        organizationService
            .Setup(x => x.Create(It.IsAny<Entity>()))
            .Returns(Guid.NewGuid());

        organizationService
            .Setup(x => x.Execute(It.IsAny<SendEmailRequest>()))
            .Returns(new SendEmailResponse());

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IPluginExecutionContext))).Returns(context.Object);
        serviceProvider.Setup(x => x.GetService(typeof(ITracingService))).Returns(tracing.Object);
        serviceProvider.Setup(x => x.GetService(typeof(IOrganizationService))).Returns(organizationService.Object);

        // Act
        plugin.Execute(serviceProvider.Object);

        // Assert
        organizationService.Verify(
            x => x.Create(It.Is<Entity>(e =>
                e.LogicalName == "email" &&
                e["subject"] != null &&
                e["subject"].ToString() == "Welcome to SwimNest!" &&
                e["description"] != null &&
                e["description"].ToString().Contains("Hello John"))),
            Times.Once);

        organizationService.Verify(
            x => x.Execute(It.Is<SendEmailRequest>(r =>
                r.IssueSend == true &&
                r.EmailId != Guid.Empty)),
            Times.Once);
    }

    [Fact]
    public void Execute_WhenMessageNameIsNotCreate_ThrowsInvalidPluginExecutionException()
    {
        // Arrange
        var plugin = new ContactPostCreatePlugin();

        var contact = new Entity("contact") { ["firstname"] = "Jane" };

        var context = new Mock<IPluginExecutionContext>();
        context.SetupGet(x => x.MessageName).Returns("Update");
        context.SetupGet(x => x.PrimaryEntityName).Returns("contact");
        context.SetupGet(x => x.Stage).Returns(40);
        context.SetupGet(x => x.InputParameters).Returns(new ParameterCollection
        {
            ["Target"] = contact
        });

        var tracing = new Mock<ITracingService>();
        var organizationService = new Mock<IOrganizationService>();

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IPluginExecutionContext))).Returns(context.Object);
        serviceProvider.Setup(x => x.GetService(typeof(ITracingService))).Returns(tracing.Object);
        serviceProvider.Setup(x => x.GetService(typeof(IOrganizationService))).Returns(organizationService.Object);

        // Act + Assert
        Assert.Throws<InvalidPluginExecutionException>(() => plugin.Execute(serviceProvider.Object));
    }

    [Fact]
    public void Execute_WhenTargetEntityIsMissing_ThrowsInvalidPluginExecutionException()
    {
        // Arrange
        var plugin = new ContactPostCreatePlugin();

        var context = new Mock<IPluginExecutionContext>();
        context.SetupGet(x => x.MessageName).Returns("Create");
        context.SetupGet(x => x.PrimaryEntityName).Returns("contact");
        context.SetupGet(x => x.Stage).Returns(40);
        context.SetupGet(x => x.InputParameters).Returns(new ParameterCollection());

        var tracing = new Mock<ITracingService>();
        var organizationService = new Mock<IOrganizationService>();

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IPluginExecutionContext))).Returns(context.Object);
        serviceProvider.Setup(x => x.GetService(typeof(ITracingService))).Returns(tracing.Object);
        serviceProvider.Setup(x => x.GetService(typeof(IOrganizationService))).Returns(organizationService.Object);

        // Act + Assert
        Assert.Throws<InvalidPluginExecutionException>(() => plugin.Execute(serviceProvider.Object));
    }
}
