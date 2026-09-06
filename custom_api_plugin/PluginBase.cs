using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Extensions;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using System;
using System.Runtime.CompilerServices;
using System.ServiceModel;

namespace custom_api_plugin
{
    /// <summary>
    /// Base class for all plug-in classes.
    /// Plugin development guide: https://docs.microsoft.com/powerapps/developer/common-data-service/plug-ins
    /// Best practices and guidance: https://docs.microsoft.com/powerapps/developer/common-data-service/best-practices/business-logic/
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        protected string PluginClassName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginBase"/> class.
        /// </summary>
        /// <param name="pluginClassName">The <see cref="Type"/> of the plugin class.</param>
        internal PluginBase(Type pluginClassName)
        {
            PluginClassName = pluginClassName.ToString();
        }

        /// <summary>
        /// Main entry point for he business logic that the plug-in is to execute.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <remarks>
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Execute")]
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new InvalidPluginExecutionException(nameof(serviceProvider));
            }

            // Construct the local plug-in context.
            var localPluginContext = new LocalPluginContext(serviceProvider);

            TraceExecutionContext(localPluginContext);

            try
            {
                // Invoke the custom implementation
                ExecuteDataversePlugin(localPluginContext);

                // Now exit - if the derived plugin has incorrectly registered overlapping event registrations, guard against multiple executions.
                return;
            }
            catch (FaultException<OrganizationServiceFault> orgServiceFault)
            {
                TraceOrganizationServiceFault(localPluginContext, orgServiceFault);

                throw new InvalidPluginExecutionException(
                    $"A Dataverse error occurred while processing the Affinity request in {PluginClassName}. Review the plug-in trace log using Correlation Id {localPluginContext.PluginExecutionContext.CorrelationId}. Detail: {orgServiceFault.Message}",
                    orgServiceFault);
            }
            catch (InvalidPluginExecutionException invalidPluginExecutionException)
            {
                localPluginContext.Trace($"Plugin validation/error response: {invalidPluginExecutionException}");
                throw;
            }
            catch (Exception ex)
            {
                localPluginContext.Trace($"Unexpected exception: {ex}");
                throw new InvalidPluginExecutionException(
                    $"An unexpected error occurred while processing the Affinity request. Correlation Id: {localPluginContext.PluginExecutionContext.CorrelationId}.",
                    ex);
            }
            finally
            {
                localPluginContext.Trace($"Exiting {PluginClassName}.Execute()");
            }
        }

        /// <summary>
        /// Placeholder for a custom plug-in implementation.
        /// </summary>
        /// <param name="localPluginContext">Context for the current plug-in.</param>
        protected virtual void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            // Do nothing.
        }

        private void TraceExecutionContext(ILocalPluginContext localPluginContext)
        {
            IPluginExecutionContext context = localPluginContext.PluginExecutionContext;

            localPluginContext.Trace(
                $"Entered {PluginClassName}.Execute() " +
                $"Correlation Id: {context.CorrelationId}, " +
                $"Initiating User: {context.InitiatingUserId}, " +
                $"Message: {context.MessageName}, " +
                $"Primary Entity: {context.PrimaryEntityName}, " +
                $"Primary Id: {context.PrimaryEntityId}, " +
                $"Stage: {context.Stage} ({GetPipelineStageLabel(context.Stage)}), " +
                $"Mode: {context.Mode} ({GetExecutionModeLabel(context.Mode)}), " +
                $"Depth: {context.Depth}, " +
                $"Input Parameters: {context.InputParameters.Count}, " +
                $"Output Parameters: {context.OutputParameters.Count}");
        }

        private static void TraceOrganizationServiceFault(
            ILocalPluginContext localPluginContext,
            FaultException<OrganizationServiceFault> orgServiceFault)
        {
            OrganizationServiceFault fault = orgServiceFault.Detail;
            if (fault == null)
            {
                localPluginContext.Trace($"Dataverse fault without detail: {orgServiceFault}");
                return;
            }

            localPluginContext.Trace(
                $"Dataverse fault encountered. ErrorCode: {fault.ErrorCode}, " +
                $"Timestamp: {fault.Timestamp}, " +
                $"Message: {fault.Message}");

            if (!string.IsNullOrWhiteSpace(fault.TraceText))
            {
                localPluginContext.Trace($"Dataverse fault trace text: {fault.TraceText}");
            }

            if (fault.InnerFault != null)
            {
                localPluginContext.Trace(
                    $"Dataverse inner fault. ErrorCode: {fault.InnerFault.ErrorCode}, " +
                    $"Message: {fault.InnerFault.Message}");
            }
        }

        private static string GetPipelineStageLabel(int stage)
        {
            switch (stage)
            {
                case 10:
                    return "PreValidation";
                case 20:
                    return "PreOperation";
                case 40:
                    return "PostOperation";
                default:
                    return "Unknown";
            }
        }

        private static string GetExecutionModeLabel(int mode)
        {
            switch (mode)
            {
                case 0:
                    return "Synchronous";
                case 1:
                    return "Asynchronous";
                default:
                    return "Unknown";
            }
        }
    }

    /// <summary>
    /// This interface provides an abstraction on top of IServiceProvider for commonly used PowerPlatform Dataverse Plugin development constructs
    /// </summary>
    public interface ILocalPluginContext
    {
        /// <summary>
        /// The PowerPlatform Dataverse organization service for the Current Executing user.
        /// </summary>
        IOrganizationService InitiatingUserService { get; }

        /// <summary>
        /// The PowerPlatform Dataverse organization service for the Account that was registered to run this plugin, This could be the same user as InitiatingUserService.
        /// </summary>
        IOrganizationService PluginUserService { get; }

        /// <summary>
        /// IPluginExecutionContext contains information that describes the run-time environment in which the plug-in executes, information related to the execution pipeline, and entity business information.
        /// </summary>
        IPluginExecutionContext PluginExecutionContext { get; }

        /// <summary>
        /// Synchronous registered plug-ins can post the execution context to the Microsoft Azure Service Bus. <br/>
        /// It is through this notification service that synchronous plug-ins can send brokered messages to the Microsoft Azure Service Bus.
        /// </summary>
        IServiceEndpointNotificationService NotificationService { get; }

        /// <summary>
        /// Provides logging run-time trace information for plug-ins.
        /// </summary>
        ITracingService TracingService { get; }

        /// <summary>
        /// General Service Provide for things not accounted for in the base class.
        /// </summary>
        IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// OrganizationService Factory for creating connection for other then current user and system.
        /// </summary>
        IOrganizationServiceFactory OrgSvcFactory { get; }

        /// <summary>
        /// ILogger for this plugin.
        /// </summary>
        ILogger Logger { get;  }

        /// <summary>
        /// Writes a trace message to the trace log.
        /// </summary>
        /// <param name="message">Message name to trace.</param>
        void Trace(string message, [CallerMemberName] string method = null);
    }

    /// <summary>
    /// Plug-in context object.
    /// </summary>
    public class LocalPluginContext : ILocalPluginContext
    {
        /// <summary>
        /// The PowerPlatform Dataverse organization service for the Current Executing user.
        /// </summary>
        public IOrganizationService InitiatingUserService { get; }

        /// <summary>
        /// The PowerPlatform Dataverse organization service for the Account that was registered to run this plugin, This could be the same user as InitiatingUserService.
        /// </summary>
        public IOrganizationService PluginUserService { get; }

        /// <summary>
        /// IPluginExecutionContext contains information that describes the run-time environment in which the plug-in executes, information related to the execution pipeline, and entity business information.
        /// </summary>
        public IPluginExecutionContext PluginExecutionContext { get; }

        /// <summary>
        /// Synchronous registered plug-ins can post the execution context to the Microsoft Azure Service Bus. <br/>
        /// It is through this notification service that synchronous plug-ins can send brokered messages to the Microsoft Azure Service Bus.
        /// </summary>
        public IServiceEndpointNotificationService NotificationService { get; }

        /// <summary>
        /// Provides logging run-time trace information for plug-ins.
        /// </summary>
        public ITracingService TracingService { get; }

        /// <summary>
        /// General Service Provider for things not accounted for in the base class.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// OrganizationService Factory for creating connection for other then current user and system.
        /// </summary>
        public IOrganizationServiceFactory OrgSvcFactory { get; }

        /// <summary>
        /// ILogger for this plugin.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Helper object that stores the services available in this plug-in.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public LocalPluginContext(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new InvalidPluginExecutionException(nameof(serviceProvider));
            }

            ServiceProvider = serviceProvider;

            Logger = serviceProvider.Get<ILogger>();

            PluginExecutionContext = serviceProvider.Get<IPluginExecutionContext>();

            TracingService = new LocalTracingService(serviceProvider);

            NotificationService = serviceProvider.Get<IServiceEndpointNotificationService>();

            OrgSvcFactory = serviceProvider.Get<IOrganizationServiceFactory>();

            PluginUserService = serviceProvider.GetOrganizationService(PluginExecutionContext.UserId); // User that the plugin is registered to run as, Could be same as current user.

            InitiatingUserService = serviceProvider.GetOrganizationService(PluginExecutionContext.InitiatingUserId); //User who's action called the plugin.

        }

        /// <summary>
        /// Writes a trace message to the trace log.
        /// </summary>
        /// <param name="message">Message name to trace.</param>
        public void Trace(string message, [CallerMemberName] string method = null)
        {
            if (string.IsNullOrWhiteSpace(message) || TracingService == null)
            {
                return;
            }

            if (method != null)
                TracingService.Trace($"[{method}] - {message}");
            else
                TracingService.Trace($"{message}");
        }
    }

    /// <summary>
    /// Specialized ITracingService implementation that prefixes all traced messages with a time delta for Plugin performance diagnostics
    /// </summary>
    public class LocalTracingService : ITracingService
    {
        private readonly ITracingService _tracingService;

        private DateTime _previousTraceTime;

        public LocalTracingService(IServiceProvider serviceProvider)
        {
            DateTime utcNow = DateTime.UtcNow;

            var context = (IExecutionContext)serviceProvider.GetService(typeof(IExecutionContext));

            DateTime initialTimestamp = context.OperationCreatedOn;

            if (initialTimestamp > utcNow)
            {
                initialTimestamp = utcNow;
            }

            _tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            _previousTraceTime = initialTimestamp;
        }

        public void Trace(string message, params object[] args)
        {
            var utcNow = DateTime.UtcNow;

            // The duration since the last trace.
            var deltaMilliseconds = utcNow.Subtract(_previousTraceTime).TotalMilliseconds;

            try
            {

                if (args == null || args.Length == 0)
                    _tracingService.Trace($"[+{deltaMilliseconds:N0}ms] - {message}");
                else
                    _tracingService.Trace($"[+{deltaMilliseconds:N0}ms] - {string.Format(message, args)}");
            }
            catch (FormatException ex)
            {
                throw new InvalidPluginExecutionException($"Failed to write trace message due to error {ex.Message}", ex);
            }
            _previousTraceTime = utcNow;
        }
    }
}
