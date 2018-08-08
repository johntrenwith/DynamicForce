using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace DynamicForce
{
    public interface ISalesforceService : IDisposable
    {        
        Task<ExpandoObject> GetObjectByIdentifier(string objectName, string id);
        Task<ExpandoObject> GetObjectByExternalIdentifier(string objectName, string externalIdField, string externalId);
        Task<List<ExpandoObject>> GetObjectsAfterCreatedDate(string objectName, DateTime createdDate);
        Task<List<ExpandoObject>> GetObjectsAfterLastModifiedDate(string objectName, DateTime lastModifiedDate);
        Task<List<ExpandoObject>> GetObjectsByFilterCriterion(string objectName, string filterField, string filterValue);
        Task<List<ExpandoObject>> GetObjectsByQuery(string query);
        Task<ExpandoObject> GetObjectByQuery(string query);
        Task<bool> InsertUpdateObject(dynamic dynamicObject, string objectName, string id);
        Task<bool> DeleteObject(string id, string objectName);
    }
}

