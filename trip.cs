using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using System.ServiceModel;
using System.ServiceModel;
using Microsoft.Xrm.Sdk.Query;

namespace MyPlugins
{
    public class trip : IPlugin
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
                    DateTime startdate = entity.GetAttributeValue<DateTime>("crdce_startdate");
                    DateTime enddate = entity.GetAttributeValue<DateTime>("crdce_enddate");

                    QueryExpression query = new QueryExpression("crdce_tripresourc");
                    query.ColumnSet = new ColumnSet(new string[] { "crdce_availablefrom" , "crdce_availableto" });
                    query.Criteria.AddCondition("crdce_availablefrom", ConditionOperator.LessEqual, startdate);
                    query.Criteria.AddCondition("crdce_availableto", ConditionOperator.GreaterEqual, enddate);

                    EntityCollection collection = service.RetrieveMultiple(query);

                    foreach(Entity rec in collection.Entities)
                    {
                        ColumnSet columns = new ColumnSet("crdce_amountperhead", "crdce_headcount");
                        Entity record = service.Retrieve("crdce_tripresourc", rec.Id, columns);
                        record["crdce_trip"] = new EntityReference("crdce_trip", entity.Id);
                        service.Update(record);

                        int headCOuntOfTrip = (int)entity.Attributes["crdce_headcount"];
                        int headCountOfTripResource = (int)record.Attributes["crdce_headcount"];
                        Money amount = (Money)record.Attributes["crdce_amountperhead"];
                        if (headCountOfTripResource >= headCOuntOfTrip)
                        {
                            entity["crdce_amount"] = headCOuntOfTrip * amount.Value;
                            service.Update(entity);
                        }

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