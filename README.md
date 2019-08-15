---
page_type: sample
languages:
- csharp
products:
- azure
extensions:
- services: Monitor
- platforms: dotnet
---

# Configuring autoscale settings to scale out based on webapp request count statistic. #

 This sample shows how to programmatically implement scenario described <a href="https://docs.microsoft.com/en-us/azure/monitoring-and-diagnostics/monitor-tutorial-autoscale-performance-schedule">here</a>.
  - Create a Web App and App Service Plan
  - Configure autoscale rules for scale-in and scale out based on the number of requests a Web App receives
  - Trigger a scale-out action and watch the number of instances increase
  - Trigger a scale-in action and watch the number of instances decrease
  - Clean up your resources


## Running this Sample ##

To run this sample:

Set the environment variable `AZURE_AUTH_LOCATION` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

    git clone https://github.com/Azure-Samples/monitor-dotnet-autoscale-based-on-performance.git

    cd monitor-dotnet-autoscale-based-on-performance

    dotnet build

    bin\Debug\net452\AutoscaleSettingsBasedOnPerformanceOrSchedule.exe

## More information ##

[Azure Management Libraries for C#](https://github.com/Azure/azure-sdk-for-net/tree/Fluent)
[Azure .Net Developer Center](https://azure.microsoft.com/en-us/develop/net/)
If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.