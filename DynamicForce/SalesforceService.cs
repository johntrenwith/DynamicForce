using Common.Logging;
using Newtonsoft.Json;
using Salesforce.Common.Models.Json;
using Salesforce.Force;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DynamicForce
{
    public class SalesforceService : ISalesforceService
    {
        private ForceClient _client;
        private ILog _log;
        private IConnector _connector;

        public SalesforceService(IConnector connector, ILog log)
        {
            _client = connector.Authenticate().Result;
            _log = log;
            _connector = connector;
        }

        public async Task<ExpandoObject> GetObjectByIdentifier(string objectName, string id)
        {
            string query = GetObjectQuery(objectName).Result + " WHERE Id = '" + id + "'";
            QueryResult<ExpandoObject> results = await _client.QueryAsync<ExpandoObject>(query);
            return results.TotalSize > 0 ? results.Records[0] : null;
        }

        public async Task<ExpandoObject> GetObjectByExternalIdentifier(string objectName, string externalIdField, string externalId)
        {
            string query = GetObjectQuery(objectName).Result + " WHERE " + externalIdField + " = '" + externalId + "'";
            QueryResult<ExpandoObject> results = await _client.QueryAsync<ExpandoObject>(query);
            int size = results.TotalSize;
            int size2 = results.Records.Count;
            return results.TotalSize > 0 ? results.Records[0] : null;
        }

        public async Task<ExpandoObject> GetObjectByQuery(string query)
        {
            QueryResult<ExpandoObject> results = await _client.QueryAsync<ExpandoObject>(query);
            return results.TotalSize > 0 ? results.Records[0] : null;
        }

        public async Task<List<ExpandoObject>> GetObjectsAfterCreatedDate(string objectName, DateTime createdDate)
        {
            string query = GetObjectQuery(objectName).Result + " WHERE CreatedDate >= " + ConvertToForceDateTime(createdDate);
            QueryResult<ExpandoObject> results = await _client.QueryAsync<ExpandoObject>(query);
            return results.TotalSize > 0 ? results.Records : null;
        }

        public async Task<List<ExpandoObject>> GetObjectsAfterLastModifiedDate(string objectName, DateTime createdDate)
        {
            string query = GetObjectQuery(objectName).Result + " WHERE LastModifiedDate >= " + ConvertToForceDateTime(createdDate);
            QueryResult<ExpandoObject> results = await _client.QueryAsync<ExpandoObject>(query);
            return results.TotalSize > 0 ? results.Records : null;
        }

        public async Task<List<ExpandoObject>> GetObjectsByFilterCriterion(string objectName, string filterField, string filterValue)
        {
            string query = GetObjectQuery(objectName).Result + " WHERE " + filterField + " = '" + filterValue + "'";
            QueryResult<ExpandoObject> results = await _client.QueryAsync<ExpandoObject>(query);
            return results.TotalSize > 0 ? results.Records : null;           
        }
       
        public async Task<List<ExpandoObject>> GetObjectsByQuery(string query)
        {
            QueryResult<ExpandoObject> results = await _client.QueryAsync<ExpandoObject>(query);
            var nextRecordsUrl = results.NextRecordsUrl;
            var objects = new List<ExpandoObject>();
            objects.AddRange(results.Records);

            if (!string.IsNullOrEmpty(results.NextRecordsUrl))
            {             
                while (true)
                {
                    var continuationResults = await _client.QueryContinuationAsync<ExpandoObject>(nextRecordsUrl);
                    objects.AddRange(continuationResults.Records);
                    nextRecordsUrl = continuationResults.NextRecordsUrl;
                    if (string.IsNullOrEmpty(nextRecordsUrl)) break;  
                }
            }

            return objects;
        }
        
        public async Task<bool> InsertUpdateObject(dynamic dynamicObject, string objectName)
        {
            var expandoObject = new ExpandoObject() as IDictionary<string, Object>;
            PropertyInfo[] propertyInfos = dynamicObject.GetType().GetProperties();
            string id = "";
            for (int i = 0; i < propertyInfos.Length; i++)
            {
                var propertyValue = propertyInfos[i].GetValue(dynamicObject);
                if (propertyInfos[i].Name == "Id")
                {
                    id = propertyValue;
                }
                else
                {
                    expandoObject.Add(propertyInfos[i].Name, propertyValue);
                }
            }

            object customer = "";
            expandoObject.TryGetValue("Customer__c", out customer);
            string c = customer.ToString();
            
            SuccessResponse response = string.IsNullOrEmpty(id.ToString()) ?
               await _client.CreateAsync(objectName, expandoObject) :
               await _client.UpdateAsync(objectName, id.ToString(), expandoObject);

            if (!response.Success)
            {
                _log.Error(response.Errors.ToString());
            }

            return response.Success;
        }
                
        public async Task<bool> DeleteObject(string id, string objectName)
        {
            bool response = await _client.DeleteAsync(objectName, id);
            return response;
        }

        public void Dispose() => _client.Dispose();

        private async Task<string> GetObjectQuery(string objectName)
        {
            ObjectDescription objectDescription = await GetObjectDescription(objectName);
            StringBuilder stringBuilder = new StringBuilder("SELECT ");

            foreach (Field field in objectDescription.Fields)
            {
                stringBuilder.Append(field.Name + ", ");
            }

            string objectQuery = stringBuilder.ToString();
            // delete trailing comma and space...
            return objectQuery.Remove(objectQuery.Length - 2) + " FROM " + objectName;
        }

        private async Task<ObjectDescription> GetObjectDescription(string objectName)
        {
            string objectDescription = await _connector.DescribeObject("/services/data/"
                + _connector.ApiVersion + "/sobjects/" + objectName + "/describe/");
            return JsonConvert.DeserializeObject<ObjectDescription>(objectDescription);
        }

        public static string ConvertToForceDateTime(DateTime dateTime)
        {
            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }
            
    }
}
