//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       ChristianK, timop, TimI
//
// Copyright 2004-2021 by OM International
//
// This file is part of OpenPetra.org.
//
// OpenPetra.org is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra.org is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using Ict.Common;
using Ict.Common.DB.Exceptions;
using Ict.Common.Data;
using Ict.Common.DB;
using Ict.Common.Exceptions;
using Ict.Common.Remoting.Server;
using Ict.Common.Remoting.Shared;
using Ict.Common.Session;
using Ict.Petra.Server.App.Core;
using Ict.Petra.Shared;
using Ict.Petra.Server.MCommon.WebConnectors;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Server.MPartner.Partner.Data.Access;
using Npgsql;

namespace Ict.Petra.Server.MCommon
{
    /// <summary>
    /// Contains utility and helper functions that are Petra Server specific and are
    /// shared between several Petra modules.
    /// </summary>
    public class MCommonMain
    {
        #region Functions

        /// <summary>
        /// Retrieves the Partner ShortName, the PartnerClass and PartnerStatus.
        /// </summary>
        /// <param name="APartnerKey">PartnerKey to identify the Partner.</param>
        /// <param name="APartnerShortName">Returns the ShortName.</param>
        /// <param name="APartnerClass">Returns the PartnerClass (FAMILY, ORGANISATION, etc).</param>
        /// <param name="APartnerStatus">Returns the PartnerStatus (eg. ACTIVE, DIED).</param>
        /// <param name="ATransaction">Open DB Transaction.</param>
        /// <returns>True if partner was found, otherwise false.</returns>
        public static Boolean RetrievePartnerShortName(Int64 APartnerKey,
            out String APartnerShortName,
            out TPartnerClass APartnerClass,
            out TStdPartnerStatusCode APartnerStatus,
            TDBTransaction ATransaction)
        {
            Boolean ReturnValue;
            StringCollection RequiredColumns;
            PPartnerTable PartnerTable;

            // initialise out Arguments
            APartnerShortName = "";

            // Default. This is not really correct but the best compromise if PartnerKey is 0 or Partner isn't found since we have an enum here.
            APartnerClass = TPartnerClass.FAMILY;

            // Default. This is not really correct but the best compromise if PartnerKey is 0 or Partner isn't found since we have an enum here.
            APartnerStatus = TStdPartnerStatusCode.spscINACTIVE;

            if (APartnerKey != 0)
            {
                // only some fields are needed
                RequiredColumns = new StringCollection();
                RequiredColumns.Add(PPartnerTable.GetPartnerShortNameDBName());
                RequiredColumns.Add(PPartnerTable.GetPartnerClassDBName());
                RequiredColumns.Add(PPartnerTable.GetStatusCodeDBName());

                PartnerTable = PPartnerAccess.LoadByPrimaryKey(APartnerKey, RequiredColumns, ATransaction, null, 0, 0);

                if (PartnerTable.Rows.Count == 0)
                {
                    ReturnValue = false;
                }
                else
                {
                    // since we loaded by primary key there must just be one partner row
                    APartnerShortName = PartnerTable[0].PartnerShortName;
                    APartnerClass = SharedTypes.PartnerClassStringToEnum(PartnerTable[0].PartnerClass);

                    APartnerStatus = SharedTypes.StdPartnerStatusCodeStringToEnum(PartnerTable[0].StatusCode);
                    ReturnValue = true;
                }
            }
            else
            {
                // Return result as valid if Partner Key is 0.
                ReturnValue = true;
            }

            return ReturnValue;
        }

        /// <summary>
        /// Checks for the existance of a Partner.
        /// </summary>
        /// <param name="APartnerKey">PartnerKey of the Partner to check for.</param>
        /// <param name="AMustNotBeMergedPartner">Set to true to check whether the Partner
        /// must not be a Merged Partner.</param>
        /// <returns>True if the Partner exists (taking AMustNotBeMergedPartner into consideration),
        /// otherwise False.</returns>
        public static bool CheckPartnerExists1(Int64 APartnerKey, bool AMustNotBeMergedPartner)
        {
            return CheckPartnerExists2(APartnerKey, AMustNotBeMergedPartner) != null;
        }

        /// <summary>
        /// Checks for the existance of a Partner.
        /// </summary>
        /// <param name="APartnerKey">PartnerKey of the Partner to check for.</param>
        /// <param name="AMustNotBeMergedPartner">Set to true to check whether the Partner
        /// must not be a Merged Partner.</param>
        /// <returns>An instance of PPartnerRow if the Partner exists (taking AMustNotBeMergedPartner into consideration),
        /// otherwise null.</returns>
        /// <param name="ADataBase">An instantiated <see cref="TDataBase" /> object, or null (default = null). If null
        /// gets passed then the Method executes DB commands with a new Database connection</param>
        public static PPartnerRow CheckPartnerExists2(Int64 APartnerKey, bool AMustNotBeMergedPartner,
            TDataBase ADataBase = null)
        {
            PPartnerRow ReturnValue = null;
            TDBTransaction ReadTransaction = new TDBTransaction();
            PPartnerTable PartnerTable = null;

            if (APartnerKey != 0)
            {
                TDataBase db = DBAccess.Connect("CheckPartnerExists2", ADataBase);
                db.ReadTransaction(ref ReadTransaction,
                    delegate
                    {
                        PartnerTable = PPartnerAccess.LoadByPrimaryKey(APartnerKey, ReadTransaction);
                    });

                if (PartnerTable.Rows.Count != 0)
                {
                    if (AMustNotBeMergedPartner)
                    {
                        if (SharedTypes.StdPartnerStatusCodeStringToEnum(
                                PartnerTable[0].StatusCode) != TStdPartnerStatusCode.spscMERGED)
                        {
                            ReturnValue = PartnerTable[0];
                        }
                    }
                    else
                    {
                        ReturnValue = PartnerTable[0];
                    }
                }
            }

            return ReturnValue;
        }

        #endregion
    }
    #region TPagedDataSet

    /// <summary>
    /// Universal class that can run any SQL SELECT query and return the resulting
    /// rows in 'pages' that contain a certain amount of rows.
    /// Especially useful for Find screens, but also for other screens where a lot of
    /// data is requested but might never be fully accessed.
    ///
    /// Execution of the query can happen asynchronously by executing ExecuteQuery
    /// in a separate Thread!
    ///
    /// </summary>
    public class TPagedDataSet
    {
        private string FProgressID;

        /// <summary>An instance of TAsyncFindParameters containing parameters for the query execution</summary>
        TAsyncFindParameters FFindParameters;

        /// <summary>SQL command that will be used to execute the query</summary>
        String FSelectSQL;

        /// <summary>DataTable holding all DataRows that are returned by the query</summary>
        DataTable FTmpDataTable = new DataTable();

        /// <summary>DataTable holding DataRows for the current Page</summary>
        DataTable FPageDataTable;

        /// <summary>Last retrieved page</summary>
        System.Int32 FLastRetrievedPage = -1;

        /// <summary>Property value.</summary>
        System.Int32 FTotalRecords = -1;

        /// <summary>Property value.</summary>
        System.Int16 FTotalPages = -1;

        /// <summary>Pass in an instance of TAsyncFindParameters to set the parameters for the query execution</summary>
        public TAsyncFindParameters FindParameters
        {
            get
            {
                return FFindParameters;
            }

            set
            {
                FFindParameters = value;
            }
        }

        /// <summary>get the current progress</summary>
        public TProgressState Progress
        {
            get
            {
                return TProgressTracker.GetCurrentState(FProgressID);
            }
        }

        /// <summary>Returns the number of records that was returned by the query</summary>
        public System.Int32 TotalRecords
        {
            get
            {
                return FTotalRecords;
            }
        }

        /// <summary>Returns the number of Pages that were created</summary>
        public System.Int16 TotalPages
        {
            get
            {
                return FTotalPages;
            }
        }

        /// <summary>
        /// Runs a SQL SELECT query and returns the first 'page' containing a certain
        /// amount of rows.
        ///
        /// @see For an example of how to use this class: look at the
        /// Ict.Petra.Server.MPartner.Partner.UIConnectors.TPartnerFindUIConnector
        /// class.
        ///
        /// </summary>
        /// <param name="ATypedTable">if you want the result to be a typed datatable, you need to specify the type here, otherwise just pass new DataTable()</param>
        /// <returns>void</returns>
        public TPagedDataSet(DataTable ATypedTable) : base()
        {
            if (ATypedTable == null)
            {
                FTmpDataTable = new DataTable();
            }
            else
            {
                FTmpDataTable = ATypedTable.Clone();
            }

            TLogging.LogAtLevel(7, "TPagedDataSet created.");
        }

        /// <summary>
        /// destructor for debugging purposes only
        /// </summary>
        ~TPagedDataSet()
        {
            TLogging.LogAtLevel(7, "TPagedDataSet.FINALIZE called!");
        }

        /// <summary>
        /// Executes the query. Always call this method in a separate Thread to execute the query asynchronously!
        /// </summary>
        /// <param name="AConfigFileName">path to the config file</param>
        /// <param name="ASessionID">the id of the current session</param>
        /// <param name="AContext">Context in which this quite generic Method gets called (e.g. 'Partner Find'). This is
        /// optional but should be specified to aid in debugging as it gets logged in case Exceptions happen when the
        /// DB Transaction is taken out and the Query gets executed.</param>
        /// <param name="ADataBase">An instantiated <see cref="TDataBase" /> object, or null (default = null). If null
        /// gets passed then the Method executes DB commands with a new Database connection</param>
        /// <remarks>An instance of TAsyncFindParameters with set up Properties must exist before this procedure can get
        /// called!
        /// </remarks>
        public void ExecuteQuery(string AConfigFileName, string ASessionID, string AContext = null, TDataBase ADataBase = null)
        {
            try
            {
                TSession.InitThread("Paged DataSet ExecuteQuery", AConfigFileName, ASessionID);

                // need to initialize the database session
                TDataBase db = DBAccess.Connect("ExecuteQuery", ADataBase);

                FProgressID = Guid.NewGuid().ToString();
                TProgressTracker.InitProgressTracker(FProgressID, "Executing Query...", 100.0m);

                // Create SQL statement and execute it to return all records
                ExecuteFullQuery(AContext, db);

                db.CloseDBConnection();
            }
            catch (Exception exp)
            {
                TLogging.Log(this.GetType().FullName + ".ExecuteQuery" +
                    (AContext == null ? "" : " (Context: " + AContext + ")") + ": Exception occured: " + exp.ToString());

                // Inform the caller that something has gone wrong...
                TProgressTracker.CancelJob(FProgressID);

                /*
                 *     WE MUST 'SWALLOW' ANY EXCEPTION HERE, OTHERWISE THE WHOLE
                 *     PETRASERVER WILL GO DOWN!!! (THIS BEHAVIOUR IS NEW WITH .NET 2.0.)
                 *
                 * --> ANY EXCEPTION THAT WOULD LEAVE THIS METHOD WOULD BE SEEN AS AN   <--
                 * --> UNHANDLED EXCEPTION IN A THREAD, AND THE .NET/MONO RUNTIME       <--
                 * --> WOULD BRING DOWN THE WHOLE PETRASERVER PROCESS AS A CONSEQUENCE! <--
                 *
                 */
            }
        }

        /// <summary>
        /// Executes the query directly. not in a separate thread. without progress tracker.
        /// </summary>
        /// <param name="AContext">Context in which this quite generic Method gets called (e.g. 'Partner Find'). This is
        /// optional but should be specified to aid in debugging as it gets logged in case Exceptions happen when the
        /// DB Transaction is taken out and the Query gets executed.</param>
        /// <param name="ADataBase">An instantiated <see cref="TDataBase" /> object, or null (default = null). If null
        /// gets passed then the Method executes DB commands with a new Database connection</param>
        /// <remarks>An instance of TAsyncFindParameters with set up Properties must exist before this procedure can get
        /// called!
        /// </remarks>
        public void ExecuteQueryDirectly(string AContext = null, TDataBase ADataBase = null)
        {
            try
            {
                // need to initialize the database session
                TDataBase db = DBAccess.Connect("ExecuteQuery", ADataBase);

                // Create SQL statement and execute it to return all records
                ExecuteFullQuery(AContext, db);

                db.CloseDBConnection();
            }
            catch (Exception exp)
            {
                TLogging.Log(this.GetType().FullName + ".ExecuteQuery" +
                    (AContext == null ? "" : " (Context: " + AContext + ")") + ": Exception occured: " + exp.ToString());
            }
        }

        private void ExecuteFullQuery(string AContext = null, TDataBase ADataBase = null)
        {
            TDataBase DBConnectionObj = null;
            TDBTransaction ReadTransaction = new TDBTransaction();
            bool SeparateDBConnectionEstablished = false;

            if (FFindParameters.FParametersGivenSeparately)
            {
                string SQLOrderBy = "";
                string SQLWhereCriteria = "";

                if (FFindParameters.FPagedTableWhereCriteria != "")
                {
                    SQLWhereCriteria = "WHERE " + FFindParameters.FPagedTableWhereCriteria;
                }

                if (FFindParameters.FPagedTableOrderBy != "")
                {
                    SQLOrderBy = " ORDER BY " + FFindParameters.FPagedTableOrderBy;
                }

                FSelectSQL = "SELECT " + FFindParameters.FPagedTableColumns + " FROM " + FFindParameters.FPagedTable +
                             ' ' +
                             SQLWhereCriteria + SQLOrderBy;
            }
            else
            {
                FSelectSQL = FFindParameters.FSqlQuery;
            }

            TLogging.LogAtLevel(9, (this.GetType().FullName + ".ExecuteFullQuery SQL:" + FSelectSQL));

            // clear temp table. do not recreate because it may be typed
            FTmpDataTable.Clear();

            try
            {
                if (ADataBase == null)
                {
                    ADataBase = new TDataBase();
                    ADataBase.EstablishDBConnection(AContext + " Connection");
                    SeparateDBConnectionEstablished = true;
                }

                ReadTransaction = ADataBase.BeginTransaction(IsolationLevel.ReadCommitted,
                    -1, AContext + " Transaction");

                // Fill temporary table with query results (all records)
                FTotalRecords = ADataBase.SelectUsingDataAdapter(FSelectSQL, ReadTransaction,
                    ref FTmpDataTable,
                    delegate(ref IDictionaryEnumerator AEnumerator)
                    {
                        if (FFindParameters.FColumNameMapping != null)
                        {
                            AEnumerator = FFindParameters.FColumNameMapping.GetEnumerator();

                            return FFindParameters.FPagedTable + "_for_paging";
                        }
                        else
                        {
                            return String.Empty;
                        }
                    }, 60, FFindParameters.FParametersArray);
            }
            catch (PostgresException Exp)
            {
                if (Exp.SqlState == "57014")  // Exception with Code 57014 is what Npgsql raises as a response to a Cancel request of a Command
                {
                    TLogging.LogAtLevel(7, this.GetType().FullName + ".ExecuteFullQuery: Query got cancelled; proper reply from Npgsql!");
                }
                else
                {
                    TLogging.Log(this.GetType().FullName + ".ExecuteFullQuery: Query got cancelled; general PostgresException occured: " + Exp.ToString());
                }

                TProgressTracker.SetCurrentState(FProgressID, "Query cancelled!", 0.0m);
                TProgressTracker.CancelJob(FProgressID);
                return;
            }
            catch (Exception Exp)
            {
                TLogging.Log(this.GetType().FullName + ".ExecuteFullQuery: Query got cancelled; general Exception occured: " + Exp.ToString());

                TProgressTracker.SetCurrentState(FProgressID, "Query cancelled!", 0.0m);
                TProgressTracker.CancelJob(FProgressID);

                return;
            }
            finally
            {
                ReadTransaction.Rollback();

                // Close separate DB Connection if we opened one earlier
                if (SeparateDBConnectionEstablished)
                {
                    DBConnectionObj.CloseDBConnection();
                }
            }

            TLogging.LogAtLevel(7,
                (this.GetType().FullName + ".ExecuteFullQuery: FDataAdapter.Fill finished. FTotalRecords: " + FTotalRecords.ToString()));

            FPageDataTable = FTmpDataTable.Clone();
            FPageDataTable.TableName = FFindParameters.FSearchName;
            TProgressTracker.SetCurrentState(FProgressID, "Query executed.", 100.0m);
            TProgressTracker.FinishJob(FProgressID);
        }

        /// <summary>
        /// Returns a DataTable for the requested 'Page'.
        /// </summary>
        /// <remarks><see cref="ExecuteQuery" /> (or <see cref="ExecuteFullQuery" />)
        /// needs to be called before to execute the SQL SELECT Statement to be able to return data.</remarks>
        /// <param name="APage">The 'Page' that should be returned</param>
        /// <param name="APageSize">Number of DataRows that a 'Page' should contain.</param>
        /// <returns>DataTable containing 'PageSize' (or less if the 'TotalRecords' are
        /// less than that value) DataRows.
        /// </returns>
        public DataTable GetData(Int16 APage, Int16 APageSize)
        {
            TLogging.LogAtLevel(7, String.Format("TPagedDataSet.GetData called (APage: {0}, APageSize={1})", APage, APageSize));

            // wait until the query has been run in the other thread
            while (FTotalRecords == -1)
            {
                System.Threading.Thread.Sleep(500);
            }

            if (APage != FLastRetrievedPage)
            {
                FLastRetrievedPage = APage;

                if (APage == 0)
                {
                    FTotalPages = Convert.ToInt16(Math.Ceiling(((double)FTotalRecords) / ((double)APageSize)));

                    TLogging.LogAtLevel(7, "FTotalPages: " + FTotalPages.ToString());

                    if (FTotalRecords > 0)
                    {
                        // Build FPageDataTable
                        CopyRowsInPage(0, APageSize);
                    }
                }
                else
                {
                    // page > 0

                    if (FTotalRecords > 0)
                    {
                        if (APage <= FTotalPages)
                        {
                            FPageDataTable.Rows.Clear();

                            // Build FPageDataTable
                            CopyRowsInPage(APage, APageSize);
                        }
                        // FRecordsAffected > 0
                        else
                        {
                            throw new EPagedTableNoSuchPageException(
                                "Tried to retrieve page " + APage.ToString() + ", but there are only " + FTotalPages.ToString() +
                                " pages in the paged table!");
                        }
                    }
                    else
                    {
                        throw new EPagedTableNoRecordsException();
                    }
                }
            }
            // (APage > FLastRetrievedPage) or (APage = 0)
            else
            {
                if (APage >= 0)
                {
                    // return empty DataTable
                    FPageDataTable.Rows.Clear();
                }
                else
                {
                    throw new EPagedTableNoSuchPageException("Invalid page " + APage.ToString() + " was requested");
                }
            }

            TProgressTracker.SetCurrentState(FProgressID, "Query executed.", 100.0m);
            TProgressTracker.FinishJob(FProgressID);
            return FPageDataTable;
        }

        /// <summary>
        /// Returns all data that was found by executing the SQL SELECT Statement.
        /// </summary>
        /// <remarks><see cref="ExecuteQuery" /> (or <see cref="ExecuteFullQuery" />)
        /// needs to be called before to execute the SQL SELECT Statement to be able to return data.</remarks>
        /// <returns>DataTable holding all data that was found by executing the SQL
        /// SELECT Statement. This is an copy of the <see cref="TPagedDataSet" />'s
        /// internal DataTable, so the caller is free to do with it what it wants
        /// without affecting the internal DataTable of the <see cref="TPagedDataSet" />.</returns>
        public DataTable GetAllData()
        {
            return FTmpDataTable.Copy();
        }

        private void CopyRowsInPage(Int16 APage, Int16 APageSize)
        {
            TLogging.LogAtLevel(7, String.Format("TPagedDataSet.CopyRowsInPage called (APage: {0}, APageSize={1})", APage, APageSize));

            Int32 RowInPage;
            Int32 MaxRowInPage;

            MaxRowInPage = (APageSize * APage) + APageSize;

            if (MaxRowInPage > FTotalRecords)
            {
                MaxRowInPage = FTotalRecords;
            }

            for (RowInPage = (APageSize * APage); RowInPage <= MaxRowInPage - 1; RowInPage += 1)
            {
                FPageDataTable.ImportRow(FTmpDataTable.Rows[RowInPage]);
            }

            TLogging.LogAtLevel(7, String.Format("TPagedDataSet.CopyRowsInPage imported {0} rows into FPageDataTable", RowInPage));
        }

        /**
         * Nested Class for passing in of parameters.
         *
         * This is used because the main execution occurs in a Thread, and it's not
         * straightforward to pass in several typed parameters to a Thread Delegate.
         *
         */
        public class TAsyncFindParameters : object
        {
            /// Columns part of the SQL SELECT statement
            internal String FPagedTableColumns;

            /// Table part of the SQL SELECT statement
            internal String FPagedTable;

            /// Set this to the name of the search
            public String FSearchName;

            /// WHERE part of the SQL SELECT statement
            internal String FPagedTableWhereCriteria;

            /// ORDER BY part of the SQL SELECT statement
            internal String FPagedTableOrderBy;

            /// HashTable containing a mapping of source column names to result column names
            internal Hashtable FColumNameMapping;

            /// Array containing the OdbcParameters for the parameterised query
            internal OdbcParameter[] FParametersArray;

            internal bool FParametersGivenSeparately;

            internal String FSqlQuery;

            /// <summary>
            /// Initialises the fields.
            /// </summary>
            /// <param name="AColumns">Columns part of the SQL SELECT statement</param>
            /// <param name="ATable">Table part of the SQL SELECT statement</param>
            /// <param name="AWhereCriteria">WHERE part of the SQL SELECT statement</param>
            /// <param name="AOrderBy">ORDER BY part of the SQL SELECT statement</param>
            /// <param name="AColumNameMapping">HashTable containing a mapping of source column names to result column names</param>
            /// <param name="AParametersArray">Array containing the OdbcParameters for the parameterised query</param>
            public TAsyncFindParameters(
                String AColumns,
                String ATable,
                String AWhereCriteria,
                String AOrderBy,
                Hashtable AColumNameMapping,
                OdbcParameter[] AParametersArray) : base()
            {
                FPagedTableColumns = AColumns;
                FPagedTable = ATable;
                FPagedTableWhereCriteria = AWhereCriteria;
                FPagedTableOrderBy = AOrderBy;
                FColumNameMapping = AColumNameMapping;
                FParametersArray = AParametersArray;
                FParametersGivenSeparately = true;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="ASqlQuery">Completely formed SqlQuery</param>
            public TAsyncFindParameters(String ASqlQuery)
            {
                FSqlQuery = ASqlQuery;
                FPagedTable = "Find";
                FParametersGivenSeparately = false;
            }
        }
    }
    #endregion

    #region TDynamicSearchHelper

    /// <summary>
    /// Universal class to help assemble the ODBC parameters for a dynamic search
    /// </summary>
    public class TDynamicSearchHelper
    {
        private
        String FDBFieldName;
        DataRow FDataRow;
        String FCriteriaField;
        String FMatchField;
        String FSearchDelimiter = "";

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ADBFieldName"></param>
        /// <param name="ADataRow"></param>
        /// <param name="ACriteriaField"></param>
        /// <param name="AMatchField"></param>
        public TDynamicSearchHelper(String ADBFieldName, DataRow ADataRow, String ACriteriaField, String AMatchField)
        {
            FDBFieldName = ADBFieldName;
            FDataRow = ADataRow;
            FCriteriaField = ACriteriaField;
            FMatchField = AMatchField;
        }

        /// <summary>
        /// todoComment
        /// </summary>
        /// <param name="ADBFieldName"></param>
        /// <param name="ADataRow"></param>
        /// <param name="ACriteriaField"></param>
        /// <param name="AMatchField"></param>
        /// <param name="AOdbcType"></param>
        /// <param name="AOdbcSize"></param>
        /// <param name="AWhereClause"></param>
        /// <param name="AIntParamArray"></param>
        public TDynamicSearchHelper(String ADBFieldName,
            DataRow ADataRow,
            String ACriteriaField,
            String AMatchField,
            OdbcType AOdbcType,
            Int32 AOdbcSize,
            ref String AWhereClause,
            ref ArrayList AIntParamArray)
        {
            object ParameterValue;
            OdbcParameter miParam;

            FDBFieldName = ADBFieldName;
            FDataRow = ADataRow;
            FCriteriaField = ACriteriaField;
            FMatchField = AMatchField;

            ParameterValue = GetParameterValue();

            if (ParameterValue.ToString() != String.Empty)
            {
                miParam = new OdbcParameter("", AOdbcType, AOdbcSize);
                miParam.Value = ParameterValue;

                AIntParamArray.Add(miParam);
                AWhereClause = AWhereClause + GetWhereString();
            }
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ATableId"></param>
        /// <param name="AColumnNr"></param>
        /// <param name="ACriteriaDataRow"></param>
        /// <param name="ACriteriaField"></param>
        /// <param name="ACriteriaMatchField"></param>
        /// <param name="AWhereClause"></param>
        /// <param name="AIntParamArray"></param>
        public TDynamicSearchHelper(
            short ATableId,
            short AColumnNr,
            DataRow ACriteriaDataRow,
            String ACriteriaField,
            String ACriteriaMatchField,
            ref String AWhereClause,
            ref ArrayList AIntParamArray)
        {
            FDataRow = ACriteriaDataRow;
            FCriteriaField = ACriteriaField;
            FMatchField = ACriteriaMatchField;

            FDBFieldName = "PUB_" +
                           TTypedDataTable.GetTableNameSQL(ATableId) +
                           "." +
                           TTypedDataTable.GetColumnNameSQL(ATableId, AColumnNr);

            object ParameterValue = GetParameterValue();

            if (ParameterValue.ToString() != String.Empty)
            {
                OdbcParameter miParam = TTypedDataTable.CreateOdbcParameter(ATableId, AColumnNr);
                miParam.Value = ParameterValue;

                AIntParamArray.Add(miParam);
                AWhereClause = AWhereClause + GetWhereString();
            }
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ATableId"></param>
        /// <param name="AColumnNr"></param>
        /// <param name="ACriteriaDataRow"></param>
        /// <param name="ACriteriaField"></param>
        /// <param name="ACriteriaMatchField"></param>
        /// <param name="AWhereClause"></param>
        /// <param name="AIntParamArray"></param>
        /// <param name="ASearchDelimiter"></param>
        public TDynamicSearchHelper(
            short ATableId,
            short AColumnNr,
            DataRow ACriteriaDataRow,
            String ACriteriaField,
            String ACriteriaMatchField,
            ref String AWhereClause,
            ref ArrayList AIntParamArray,
            String ASearchDelimiter)
            : this(ATableId, AColumnNr, ACriteriaDataRow, ACriteriaField,
                   ACriteriaMatchField, ref AWhereClause, ref AIntParamArray)
        {
            FSearchDelimiter = ASearchDelimiter;
        }

        /// <summary>
        /// todoComment
        /// </summary>
        /// <returns></returns>
        public object GetParameterValue()
        {
            String matchvalue;

            try
            {
                // not all fields have a MatchField defined yet
                matchvalue = this.FDataRow[FMatchField].ToString();
            }
            catch (Exception)
            {
                matchvalue = "BEGINS";
            }

            if (FMatchField == "")
            {
                matchvalue = "EXACT";
            }

            String criteriavalue = FDataRow[FCriteriaField].ToString().Replace("*", String.Empty);

            if (criteriavalue.Length > 0)
            {
                if ((matchvalue == "BEGINS" || matchvalue == "CONTAINS") && !criteriavalue.EndsWith("%"))
                {
                    criteriavalue = criteriavalue + '%';
                }

                if ((matchvalue == "ENDS" || matchvalue == "CONTAINS") && !criteriavalue.StartsWith("%"))
                {
                    criteriavalue = '%' + criteriavalue;
                }

                if (matchvalue == "EXACT")
                {
                    criteriavalue = FDataRow[FCriteriaField].ToString().ToLower();
                }

                criteriavalue = criteriavalue.Replace("%%", "%");
            }

            return criteriavalue;
        }

        /// <summary>
        /// todoComment
        /// </summary>
        /// <returns></returns>
        public String GetWhereString()
        {
            String matchvalue;
            String criteriavalue;
            String outcome = "";
            double DoubleValue = -1;

            if (this.FSearchDelimiter == "")
            {
                FSearchDelimiter = "AND";
            }

            try
            {
                // not all fields have a MatchField defined yet
                matchvalue = this.FDataRow[FMatchField].ToString().ToUpper();
            }
            catch (Exception)
            {
                matchvalue = "BEGINS";
            }

            if (FMatchField == "")
            {
                matchvalue = "EXACT";
            }

            if (matchvalue == "BEGINS")
            {
                outcome = ' ' + FSearchDelimiter + ' ' + FDBFieldName + " LIKE ?";
            }

            if (matchvalue == "ENDS")
            {
                outcome = ' ' + FSearchDelimiter + ' ' + FDBFieldName + " LIKE ?";
            }

            if (matchvalue == "CONTAINS")
            {
                outcome = ' ' + FSearchDelimiter + ' ' + FDBFieldName + " LIKE ?";
            }

            if (matchvalue == "EXACT")
            {
                // Check if criteria value is a valid number
                criteriavalue = FDataRow[FCriteriaField].ToString();
                Double.TryParse(criteriavalue, out DoubleValue);

                if (DoubleValue == 0)
                {
                    // Criteria value isn't a valid number, so convert it to lowercase to make
                    // for case-insensitive searches.
                    outcome = ' ' + FSearchDelimiter + " LOWER(" + FDBFieldName + ") = ?";
                }
                else
                {
                    // Criteria value is a number, therefore don't convert it to lowercase.
                    outcome = ' ' + FSearchDelimiter + " " + FDBFieldName + " = ?";
                }
            }

//            TLogging.LogAtLevel(6, "WhereString: " + outcome);

            return outcome;
        }
    }

    #endregion

    /// <summary>Reporting Query for use with 'FastReports', with Cancel Option.</summary>
    public class TReportingDbAdapter
    {
        private Exception FRunQueryException = null;

        /// <summary>Use this object for creating DB Transactions, etc</summary>
        public TDataBase FPrivateDatabaseObj;

        /// <summary>We don't support cancelling anymore</summary>
        public Boolean IsCancelled
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Exception that occured during a RunQuery call (if any!)
        /// </summary>
        public Exception RunQueryException
        {
            get
            {
                return FRunQueryException;
            }
        }

        /// <summary>
        /// Constructor. It establishes a DB Connection for a FastReports Report.
        /// </summary>
        public TReportingDbAdapter()
        {
            FPrivateDatabaseObj = EstablishDBConnection("FastReports Report DB Connection");
        }

        /// <summary>
        /// Establishes a DB Connection for a FastReports Report.
        /// </summary>
        /// <returns>Instance of <see cref="TDataBase" /> that has an open DB Connection.</returns>
        public static TDataBase EstablishDBConnection(String AConnectionName = "")
        {
            return DBAccess.Connect(AConnectionName);
        }

        /// <summary>
        /// Call this to ensure that the DB Connection that got established for the Report gets closed. This only really
        /// happens if <see cref="EstablishDBConnection" /> got called with Argument 'ASeparateDBConnection' set to
        /// true, otherwise this Method does nothing.
        /// </summary>
        public void CloseConnection()
        {
            FPrivateDatabaseObj.CloseDBConnection();
        }

        /// <summary>
        /// Run this Database query.
        /// If FReportingQueryCancelFlag is set, this returns immediately with an empty table.
        /// The query can be cancelled WHILE IT IS RUNNING. In this case the returned table may be partially filled.
        /// </summary>
        /// <returns>DataTable. May be empty (even with no fields defined!) if cancellation happens or has happened.</returns>
        public DataTable RunQuery(String Query, String TableName, TDBTransaction Trans,
            TDataBase.TOptionalColumnMappingDelegate AOptionalColumnNameMapping = null,
            int ASelectCommandTimeout = -1, DbParameter[] AParametersArray = null)
        {
            List <object[]>ParameterValues = null;
            List <object>ObjectValues = null;

            if (AParametersArray != null)
            {
                ParameterValues = new List <object[]>(1);
                ObjectValues = new List <object>();

                for (int Counter = 0; Counter < AParametersArray.Length; Counter++)
                {
                    ObjectValues.Add(AParametersArray[Counter].Value);
                }

                ParameterValues.Add(ObjectValues.ToArray());
            }

            return RunQueryMultiParams(Query, TableName, Trans, AOptionalColumnNameMapping,
                ASelectCommandTimeout, AParametersArray, ParameterValues, false);
        }

        /// <summary>
        /// Run this Database query.
        /// If FReportingQueryCancelFlag is set, this returns immediately with an empty table.
        /// The query can be cancelled WHILE IT IS RUNNING. In this case the returned table may be partially filled.
        /// </summary>
        /// <remarks>For details on the Arguments that can be passed with <paramref name="AOptionalColumnNameMapping"/>,
        /// <paramref name="ASelectCommandTimeout"/>, <paramref name="AParameterDefinitions"/>,
        /// <paramref name="AParameterValues"/>, <paramref name="APrepareSelectCommand"/>,
        /// <paramref name="AProgressUpdateEveryNRecs"/> and
        /// <paramref name="AMultipleParamQueryProgressUpdateCallback"/> please see their respective XML Comments on
        /// Method <see cref="TDataBase.SelectUsingDataAdapterMulti"/>!/</remarks>
        /// <returns>DataTable. May be empty (even with no fields defined!) if cancellation happens or has happened.</returns>
        public DataTable RunQueryMultiParams(String Query, String TableName, TDBTransaction Trans,
            TDataBase.TOptionalColumnMappingDelegate AOptionalColumnNameMapping = null,
            int ASelectCommandTimeout = -1, DbParameter[] AParameterDefinitions = null, List <object[]>AParameterValues = null,
            bool APrepareSelectCommand = false, Int16 AProgressUpdateEveryNRecs = 0,
            TDataBase.MultipleParamQueryProgressUpdateDelegate AMultipleParamQueryProgressUpdateCallback = null)
        {
            var ResultDT = new DataTable(TableName);

            try
            {
                FPrivateDatabaseObj.SelectUsingDataAdapterMulti(Query, Trans, ref ResultDT,
                    AOptionalColumnNameMapping, ASelectCommandTimeout, AParameterDefinitions, AParameterValues,
                    APrepareSelectCommand, AProgressUpdateEveryNRecs, AMultipleParamQueryProgressUpdateCallback);
            }
            catch (PostgresException Exp)
            {
                if (Exp.SqlState == "57014")  // Exception with Code 57014 is what Npgsql raises as a response to a Cancel request of a Command
                {
                    TLogging.LogAtLevel(7, this.GetType().FullName + ".RunQuery: Query got cancelled; proper reply from Npgsql!");
                }
                else if (Exp.SqlState == "25P02")  // Exception with Code 25P02 is what Npgsql raises as a response to a cancellation of a request of a Command when that happens in another code path (eg. on a different Thread [e.g. Partner Find
                {                               // screen: Cancel got pressed while Report Query ran, for instance])
                    TLogging.LogAtLevel(1, this.GetType().FullName +
                        ".RunQuery: Query got cancelled (likely trought another code path [likely on another Thread]); proper reply from Npgsql!");
                }
                else
                {
                    TLogging.Log(this.GetType().FullName + ".RunQuery: Query got cancelled; general PostgresException occured: " + Exp.ToString());
                }

                return null;
            }
            catch (Exception Exc)
            {
                TLogging.Log("ReportingQueryWithCancelOption: Query Raised exception: " + Exc.ToString() +
                    "\nQuery: " + Query);

                FRunQueryException = Exc;

                /*
                 *     WE MUST 'SWALLOW' ANY EXCEPTION HERE, OTHERWISE THE WHOLE
                 *     PETRASERVER WILL GO DOWN!!! (THIS BEHAVIOUR IS NEW WITH .NET 2.0.)
                 *
                 * --> ANY EXCEPTION THAT WOULD LEAVE THIS METHOD WOULD BE SEEN AS AN   <--
                 * --> UNHANDLED EXCEPTION IN A THREAD, AND THE .NET/MONO RUNTIME       <--
                 * --> WOULD BRING DOWN THE WHOLE PETRASERVER PROCESS AS A CONSEQUENCE. <--
                 *
                 */
            }

            return ResultDT;
        }
    }
}
