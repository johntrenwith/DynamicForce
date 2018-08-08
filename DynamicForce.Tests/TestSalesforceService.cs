using System.Configuration;
using System.Dynamic;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicForce.Tests
{
    [TestClass]
    public class TestSalesforceService
    {
        private ILog log = LogManager.GetLogger<TestSalesforceService>();
        private HttpClient jsonHttpClient = new HttpClient();
        private HttpClient xmlHttpClient = new HttpClient();
        private string consumerKey = ConfigurationManager.AppSettings["consumerKey"];
        private string consumerSecret = ConfigurationManager.AppSettings["consumerSecret"];
        private string userName = ConfigurationManager.AppSettings["userName"];
        private string passwordAndToken = ConfigurationManager.AppSettings["passwordAndToken"];
       

        [TestMethod]
        public async Task TestInsertObject()
        {
            IConnector connector = new Connector(consumerKey, consumerSecret, userName,
               passwordAndToken, false, log, jsonHttpClient, xmlHttpClient);
            ISalesforceService salesforceService = new SalesforceService(connector, log);
            dynamic lead = new ExpandoObject();
            lead.FirstName = "Jim";
            lead.LastName = "Robot";
            lead.Email = "fake@news.com";
            lead.Company = "Fake News Inc.";
            using (salesforceService)
            {
                bool isCreated = await salesforceService.InsertUpdateObject(lead, "Lead", "");
                Assert.IsTrue(isCreated);
            }
        }
              

        [TestMethod]
        public async Task TestUpdateObject()
        {
            IConnector connector = new Connector(consumerKey, consumerSecret, userName,
               passwordAndToken, false, log, jsonHttpClient, xmlHttpClient);
            ISalesforceService salesforceService = new SalesforceService(connector, log);
            using (salesforceService)
            {
                dynamic lead = await salesforceService.GetObjectByExternalIdentifier("Lead", "Email", "fake@news.com");
                Assert.AreEqual("Fake News Inc.", lead.Company);

                dynamic updatedLead = new ExpandoObject();
                updatedLead.Company = "Fake News Corp.";
           
                bool isUpdated = await salesforceService.InsertUpdateObject(updatedLead, "Lead", lead.Id);
                Assert.IsTrue(isUpdated);
            }
        }

        [TestMethod]
        public async Task TestGetObjectByQuery()
        {
            IConnector connector = new Connector(consumerKey, consumerSecret, userName,
                passwordAndToken, false, log, jsonHttpClient, xmlHttpClient);
            ISalesforceService salesforceService = new SalesforceService(connector, log);
            string query = "SELECT Id, Name FROM Lead WHERE Company = 'Fake News Corp.'";

            using (salesforceService)
            {
                dynamic lead = salesforceService.GetObjectByQuery(query).GetAwaiter().GetResult();
                Assert.AreEqual(lead.Name, "Jim Robot");
                lead = await salesforceService.GetObjectByIdentifier("Lead", lead.Id);
                Assert.AreEqual(lead.FirstName, "Jim");
            }
        }
        
        [TestMethod]
        public async Task TestDeleteObject()
        {
            IConnector connector = new Connector(consumerKey, consumerSecret, userName,
               passwordAndToken, false, log, jsonHttpClient, xmlHttpClient);
            ISalesforceService salesforceService = new SalesforceService(connector, log);
            dynamic lead = new ExpandoObject();
            using (salesforceService)
            {
                lead = await salesforceService.GetObjectByExternalIdentifier("Lead", "Email", "fake@news.com");
                bool isDeleted = await salesforceService.DeleteObject(lead.Id, "Lead");
                Assert.IsTrue(isDeleted);
            }
        }

        
    }
}
