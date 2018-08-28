using Common.Logging;
using Salesforce.Common.Models.Json;
using Salesforce.Force;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
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
            string query = GetObjectQuery(objectName).Result + " WHERE " + filterField + " = '" + filterValue + "' ORDER BY Id";
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

        public async Task<bool> InsertUpdateObject(object myObject, string objectName, string id)
        {
            SuccessResponse response = string.IsNullOrEmpty(id) ?
               await _client.CreateAsync(objectName, myObject) :
               await _client.UpdateAsync(objectName, id.ToString(), myObject);

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
            return "SELECT " + await GetFieldsListAsync(objectName) + " FROM " + objectName;
        }

        public async Task<string> GetFieldsListAsync(string objectName)
        {
            IDictionary<string, object> objectProperties = await _client.DescribeAsync<ExpandoObject>(objectName);
            objectProperties.TryGetValue("fields", out object fields);
            List<string> objectDescription = new List<string>();
            foreach (dynamic field in fields as IEnumerable)
            {
                objectDescription.Add(((ExpandoObject)(field)).FirstOrDefault(x => x.Key == "name").Value.ToString());
            }
            return string.Join(", ", objectDescription);
        }

        private async Task<IEnumerable<string>> GetObjectDescription(string objectName)
        {
            IDictionary<string, object> objectProperties = await _client.DescribeAsync<ExpandoObject>(objectName);
            objectProperties.TryGetValue("fields", out object fields);
            List<string> objectDescription = new List<string>();
            foreach (dynamic field in fields as IEnumerable)
            {
                objectDescription.Add(((ExpandoObject)(field)).FirstOrDefault(x => x.Key == "name").Value.ToString());
            }
            return objectDescription;
        }

        public static string ConvertToForceDateTime(DateTime dateTime)
        {
            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }   
    }
}
