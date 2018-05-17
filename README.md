# DynamicForce
**A wrapper for the Force.com Toolkit for .NET using dynamic types for extensibility.**

*Have you ever wanted to query a Salesforce instance from a .NET-based client without the hassle of generating WSDLs or writing cumbersome model classes to represent your Salesforce objects?*

The DynamicForce library is built on top of the officially supported [Force.com Toolkit for .NET](https://github.com/developerforce/Force.com-Toolkit-for-NET) and leverages .NET dynamic types to query Salesforce objects at runtime and build an object model on-the-fly.

Here's an example of locating a Lead using en email address, then testing the value of the Lead's Company:
```c#
dynamic lead = await salesforceService.GetObjectByExternalIdentifier("Lead", "Email", "fake@news.com");
Assert.AreEqual("Fake News Inc.", lead.Company);
```
I don't need to define the "Company" attribute anywhere; when I call "GetObjectByExternalIdentifier" this method calls an internal method (GetObjectDescription) which uses Salesforce's "DescribeObject" REST API to build up a list of fields in the Lead object. 
````c#
string objectDescription = await _connector.DescribeObject("/services/data/"
+ _connector.ApiVersion + "/sobjects/" + objectName + "/describe/");
return JsonConvert.DeserializeObject<ObjectDescription>(objectDescription);
````
This works for custom fields as well; I could equally test for a value of lead.MyCustomField__c if I know this field is present on the Lead object.

## Getting Started ##
There is a bit of spade work to get up and running. First, you'll need to [register DynamicForce as a "Connected App"](https://help.salesforce.com/articleView?id=connected_app_create.htm&type=5) in your Salesforce instance. Allow the "Access and manage your data (api)" privilege on the configuration page. Be sure to collect the "Consumer Key" and "Consumer Secret" values from this page - you'll need them later. 

Set up an API user to call DynamicForce functions if you don't have one already. This user should have permission to do whatever it is you'd like to do using the DynamicForce library, i.e. if you're not needing to update records then a read-only privileged account is fine. [Grab a security token](https://help.salesforce.com/articleView?id=user_security_token.htm) for this user. 

Once that's all done you can plug the following attributes into your application's configuration file as follows:

````xml
  <appSettings>
    <add key="consumerKey" value="KEY GOES HERE" />
    <add key="consumerSecret" value="SECRET GOES HERE" />
    <add key="userName" value="API USER GOES HERE" />
    <add key="passwordAndToken" value="PASSWORD + SECURITY TOKEN GOES HERE" />
  </appSettings>
````
So far so good...here's how to make a connection to Salesforce and instantiate the SalesforceService object:

````c#
 IConnector connector = new Connector(consumerKey, consumerSecret, userName,
 passwordAndToken, false, log, jsonHttpClient, xmlHttpClient);
 ISalesforceService salesforceService = new SalesforceService(connector, log);
 ````
 
 The "log" variable is of type "ILog" from the ["Common.Logging" library available on Nuget](https://www.nuget.org/packages/Common.Logging/). In theory this will allow you to plug in one of a variety of logging frameworks - I've only tried using log4net so your mileage may vary. If you don't plan on doing any logging because you're a terrible person, just add the Nuget packages "Common.Logging" and "Common.Logging.Core" and you can  instantiate a "dummy" log variable to pass in the "log" parameter:
````c# 
private static ILog log = LogManager.GetLogger<MyApp>();
````

Watch for the "isSandboxUser" parameter - this should be set to false if you're connecting to a production or developer sandbox (login.salesforce.com) and true if you're connecting to an actual sandbox / test environment (test.salesforce.com).

The jsonHttpClient and xmlHttpClient parameter take just a new instance of System.Net.Http.HttpClient.

And that's it - you're now ready to query your Salesforce instance using the methods on the SalesforceService object. 
