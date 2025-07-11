﻿namespace LiteGraph.GraphRepositories.Interfaces
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.Serialization;
    using Microsoft.Data.Sqlite;

    /// <summary>
    /// Interface for credential methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public interface ICredentialMethods
    {
        /// <summary>
        /// Create a credential.
        /// </summary>
        /// <param name="credential">Credential.</param>
        /// <returns>Credential.</returns>
        Credential Create(Credential credential);

        /// <summary>
        /// Read all credentials in tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <returns>Credentials.</returns>
        IEnumerable<Credential> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read credentials.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="bearerToken">Bearer token.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <returns>Credentials.</returns>
        IEnumerable<Credential> ReadMany(
            Guid? tenantGuid,
            Guid? userGuid,
            string bearerToken,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read a credential by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>Credential.</returns>
        Credential ReadByGuid(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Read credential by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <returns>Credential.</returns>
        IEnumerable<Credential> ReadByGuids(Guid tenantGuid, List<Guid> guids);

        /// <summary>
        /// Read a credential by bearer token.
        /// </summary>
        /// <param name="bearerToken">Bearer token.</param>
        /// <returns>Credential.</returns>
        Credential ReadByBearerToken(string bearerToken);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result containing a page of objects.</returns>
        EnumerationResult<Credential> Enumerate(EnumerationRequest query);

        /// <summary>
        /// Get the record count.  Optionally supply a marker object GUID to indicate that only records from that marker record should be counted.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="markerGuid">Marker GUID.</param>
        /// <returns>Number of records.</returns>
        int GetRecordCount(
            Guid? tenantGuid,
            Guid? userGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null);

        /// <summary>
        /// Update a credential.
        /// </summary>
        /// <param name="cred">Credential.</param>
        /// <returns>Credential.</returns>
        Credential Update(Credential cred);

        /// <summary>
        /// Delete credentials associated with a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        void DeleteAllInTenant(Guid tenantGuid);

        /// <summary>
        /// Delete a credential.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        void DeleteByGuid(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Delete credentials associated with a user.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="userGuid">User GUID.</param>
        void DeleteByUser(Guid tenantGuid, Guid userGuid);

        /// <summary>
        /// Check if a credential exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        bool ExistsByGuid(Guid tenantGuid, Guid guid);
    }
}
