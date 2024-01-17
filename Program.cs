// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Azure.ResourceManager.Monitor;
using Azure.ResourceManager.Monitor.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Samples.Common;

namespace AutoscaleSettingsBasedOnPerformanceOrSchedule
{
    public class Program
    {
        /**
         * This sample shows how to programmatically implement scenario described <a href="https://docs.microsoft.com/en-us/azure/monitoring-and-diagnostics/monitor-tutorial-autoscale-performance-schedule">here</a>.
         *  - Create a Web App and App Service Plan
         *  - Configure autoscale rules for scale-in and scale out based on the number of requests a Web App receives
         *  - Trigger a scale-out action and watch the number of instances increase
         *  - Trigger a scale-in action and watch the number of instances decrease
         *  - Clean up your resources
         */
        private static ResourceIdentifier? _resourceGroupId = null;
        public static async Task RunSample(ArmClient client)
        {
            try
            {
                // ============================================================
           
                // Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();
                var rgName = Utilities.CreateRandomName("rgMonitor");
                Utilities.Log($"creating a resource group with name : {rgName}...");
                var rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS2));
                var resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log("Created a resource group with name: " + resourceGroup.Data.Name);

                //Create a Web App
                Utilities.Log("Creating a web app");
                var websiteCollection = resourceGroup.GetWebSites();
                var websiteName = Utilities.CreateRandomName("MyTestScaleWebApp");
                var websiteData = new WebSiteData(AzureLocation.SouthCentralUS);
                var website = (await websiteCollection.CreateOrUpdateAsync(WaitUntil.Completed,websiteName,websiteData)).Value;
                Utilities.Log("Created a web app with name :" + website.Data.Name);

                //Create a App Service Plan
                Utilities.Log("Creating app service plan");
                var appServicePlanCollection = resourceGroup.GetAppServicePlans();
                var appServicePlanName = Utilities.CreateRandomName("MyTestAppServicePlan");
                var appServicePlanData = new AppServicePlanData(AzureLocation.SouthCentralUS)
                {
                    Sku = new AppServiceSkuDescription
                    {
                        Name = "P1",
                        Tier = "Premium",
                        Capacity = 1
                    },
                    Kind = "app"
                };
                var appServicePlan = (await appServicePlanCollection.CreateOrUpdateAsync(WaitUntil.Completed, appServicePlanName, appServicePlanData)).Value;
                Utilities.Log("Created app service plan with name:" + appServicePlan.Data.Name);
                
                // ============================================================

                // Configure autoscale rules for scale-in and scale out based on the number of requests a Web App receives
                Utilities.Log("Creating autoscaleSetting...");
                var autoscaleSettingsCollection = resourceGroup.GetAutoscaleSettings();
                var autoscaleSettingName = Utilities.CreateRandomName("autoscalename1");
                var rules = new List<AutoscaleRule>()
                {
                    new(
                        new MetricTrigger("Requests", website.Id, TimeSpan.FromMinutes(5), MetricStatisticType.Sum, TimeSpan.FromMinutes(5), MetricTriggerTimeAggregationType.Total, MetricTriggerComparisonOperation.GreaterThan, 10)
                        {
                            MetricNamespace = "Microsoft.Web/sites",
                        },
                        new MonitorScaleAction(MonitorScaleDirection.Increase, MonitorScaleType.ChangeCount, TimeSpan.FromMinutes(5))
                        {
                            Value = "1",
                        }
                    ),
                    new(
                        new MetricTrigger("Requests",website.Id,TimeSpan.FromMinutes(10),MetricStatisticType.Average,TimeSpan.FromMinutes(10),MetricTriggerTimeAggregationType.Total,MetricTriggerComparisonOperation.LessThan,5),
                        new MonitorScaleAction(MonitorScaleDirection.Decrease, MonitorScaleType.ChangeCount, TimeSpan.FromMinutes(5))
                        {
                            Value = "1",
                        }
                    ),
                };
                var days = new List<MonitorDayOfWeek>()
                {
                    MonitorDayOfWeek.Monday,
                    MonitorDayOfWeek.Tuesday,
                    MonitorDayOfWeek.Wednesday,
                    MonitorDayOfWeek.Thursday,
                    MonitorDayOfWeek.Friday,
                };
                var schedule = new RecurrentSchedule("Pacific Standard Time", days, new[] { 9 }, new[] { 00 });
                var schedule2 = new RecurrentSchedule("Pacific Standard Time", days, new[] { 18 }, new[] { 30 });
                var profiles = new List<AutoscaleProfile>()
                {
                    new AutoscaleProfile("Default profile", new MonitorScaleCapacity(1,1,1), rules),
                    new AutoscaleProfile("Monday to Friday", new MonitorScaleCapacity(1,2,1), rules)
                    {
                        Recurrence = new MonitorRecurrence(RecurrenceFrequency.Week, schedule)
                    },
                    new AutoscaleProfile("{\"name\":\"Default\",\"for\":\"Monday to Friday\"}", new MonitorScaleCapacity(1,1,1), rules)
                    {
                        Recurrence = new MonitorRecurrence(RecurrenceFrequency.Week, schedule2)
                    },
                };
                var autoscaleSettingData = new AutoscaleSettingData(AzureLocation.SouthCentralUS, profiles)
                {
                    IsEnabled = true,
                    TargetResourceId = new ResourceIdentifier(appServicePlan.Id),
                };
                var autoscaleSettings = (await autoscaleSettingsCollection.CreateOrUpdateAsync(WaitUntil.Completed,autoscaleSettingName, autoscaleSettingData)).Value;
                Utilities.Log("Created autoscaleSettings with name :" + autoscaleSettings.Data.Name);
                var deployedWebAppUrl = "https://" + website.Data.HostNames.First() + "/";
                Utilities.Log(deployedWebAppUrl);
            }
            finally
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Utilities.Log($"Deleting Resource Group: {_resourceGroupId}");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Deleted Resource Group: {_resourceGroupId}");
                    }
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception g)
                {
                    Utilities.Log(g);
                }
            }
        }
        public static async Task Main(string[] args)
        {
            try
            {
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);
                await RunSample(client);
            }
            catch (Exception e)
            {
                Utilities.Log(e);
            }
        }
    }
}
