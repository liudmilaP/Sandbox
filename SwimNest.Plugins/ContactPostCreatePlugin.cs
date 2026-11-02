using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Extensions;

namespace SwimNest.Plugins;

public sealed class ContactPostCreatePlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        var context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext
            ?? throw new InvalidPluginExecutionException("The plugin execution context is unavailable.");
        var tracingService = serviceProvider.GetService(typeof(ITracingService)) as ITracingService
            ?? throw new InvalidPluginExecutionException("The plugin tracing service is unavailable.");
        var organisationService = serviceProvider.GetService(typeof(IOrganizationService)) as IOrganizationService
            ?? throw new InvalidPluginExecutionException("The plugin organization service factory is unavailable.");

        if (!string.Equals(context.MessageName, "Create", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(context.PrimaryEntityName, "contact", StringComparison.OrdinalIgnoreCase)
            || context.Stage != 40)
        {
            throw new InvalidPluginExecutionException(
                "ContactPostCreatePlugin must be registered for the Contact Create message in the PostOperation stage.");
        }

        if (!context.InputParameters.TryGetValue("Target", out var target)
            || target is not Entity contact
            || !string.Equals(contact.LogicalName, "contact", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidPluginExecutionException("The Contact record was not available in the plugin context.");
        }

        tracingService.Trace("ContactPostCreatePlugin executed for contact {0}.", context.PrimaryEntityId);

        CreateAndSendWelcomeEmailForPotentialMember(contact, organisationService, tracingService);
    }

    private void CreateAndSendWelcomeEmailForPotentialMember(
        Entity contact,
        IOrganizationService service,
        ITracingService tracingService)
    {
        tracingService.Trace("Creating and sending welcome email for contact {0}.", contact.Id);
        CreateAndSendEmail(contact, service, tracingService);
    }

    private void CreateAndSendEmail(
        Entity contact,
        IOrganizationService service,
        ITracingService tracingService)
    {
        var email = new Entity("email");
        email["subject"] = "Welcome to SwimNest!";
        email["description"] = $"Hello {contact.GetAttributeValue<string>("firstname")},\n\n" +
                              "Welcome to SwimNest! We are excited to have you as a potential member.\n\n" +
                              "Best regards,\nThe SwimNest Team";

        email["to"] = new EntityCollection(
            new List<Entity>
            {
                new Entity("activityparty")
                {
                    ["partyid"] = contact.ToEntityReference()
                }
            });

        var emailId = service.Create(email);

        var request = new SendEmailRequest
        {
            EmailId = emailId,
            TrackingToken = string.Empty,
            IssueSend = true
        };

        service.Execute(request);

        tracingService.Trace("Welcome email sent for contact {0}.", contact.Id);
    }
}
