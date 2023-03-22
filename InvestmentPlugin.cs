using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using System.ServiceModel;

namespace MyPlugins
{
    public class InvestmentPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // Extract the tracing service for use in debugging sandboxed plug-ins.  
            // If you are not registering the plug-in in the sandbox, then you do  
            // not have to add any tracing service related code.  
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.  
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the organization service reference which you will need for  
            // web service calls.  
            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);



            // The InputParameters collection contains all the data passed in the message request.  
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.  
                Entity entity = (Entity)context.InputParameters["Target"];


                try
                {
                    // Plug-in business logic goes here.  
                  

                    OptionSetValue optionSetValue = entity.GetAttributeValue<OptionSetValue>("crdce_investmentterm");
                    int value = optionSetValue.Value;
                    Money amount = (Money)entity.Attributes["crdce_amount"];
                    decimal amountPerInvest = amount.Value/value;
                    Money amountTotal = new Money(amountPerInvest);

                    DateTime curretDate = DateTime.Now;

                    for (int i = 0; i < value; i++)
                    {
                        Entity EMI = new Entity("crdce_emi");
                        EMI["crdce_name"] = "EMI -"+i;
                        EMI["crdce_parentinvestment"] = new EntityReference("crdce_investment", entity.Id);
                        EMI["crdce_amount"] = amountTotal;
                        EMI["crdce_duedate"] = curretDate; 
                        curretDate=curretDate.AddMonths(1);
                    
                        Guid guid = service.Create(EMI);
                    }
                }

                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException("An error occurred in MyPlug-in.", ex);
                }

                catch (Exception ex)
                {
                    tracingService.Trace("MyPlugin: {0}", ex.ToString());
                    throw;
                }
            }
        }
    }
}