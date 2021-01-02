﻿//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       christiank, timop
//
// Copyright 2004-2020 by OM International
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
using System.Data;

using Ict.Common;
using Ict.Common.DB;
using Ict.Petra.Shared.MPartner;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Shared.MCommon;
using Ict.Petra.Server.MPartner.Partner.Data.Access;

namespace Ict.Petra.Server.MPartner.Common
{
    /// <summary>
    /// Contains functions to be used by the Server that perform
    /// certain calculations - specific for the Partner Module.
    /// </summary>
    public class ServerCalculations
    {
        /// <summary>
        /// Determines which address is the 'Best Address' of a Partner.
        /// </summary>
        /// <remarks>There are two similar shared Methods in Namespace Ict.Petra.Server.MPartner.Common.Calculations,
        /// both called 'DetermineBestAddress' which work by passing in the PartnerLocations of a Partner in an Argument
        /// and which return a <see cref="TLocationPK" />. As those Methods don't access the database, these Methods
        /// can be used client-side as well!</remarks>
        /// <param name="APartnerKey">PartnerKey of the Partner whose addresses should be checked.</param>
        /// <returns>A <see cref="TLocationPK" /> which points to the 'Best Address'. If no 'Best Address' was found,
        /// SiteKey and LocationKey of this instance will be both -1.</returns>
        public static TLocationPK DetermineBestAddress(Int64 APartnerKey)
        {
            TLocationPK BestLocation = new TLocationPK();
            PPartnerLocationRow Tmp;

            BestLocation = DetermineBestAddress(APartnerKey, out Tmp);

            return BestLocation;
        }

        /// <summary>
        /// Determines which address is the 'Best Address' of a Partner, and returns the PPartnerLocation record which is the
        /// 'Best Address'.
        /// </summary>
        /// <remarks>There are two similar shared Methods in Namespace Ict.Petra.Server.MPartner.Common.Calculations,
        /// both called 'DetermineBestAddress' which work by passing in the PartnerLocations of a Partner in an Argument
        /// and which return a <see cref="TLocationPK" />. As those Methods don't access the database, these Methods
        /// can be used client-side as well!</remarks>
        /// <param name="APartnerKey">PartnerKey of the Partner whose addresses should be checked.</param>
        /// <param name="APartnerLocationDR">PPartnerLocation Record that is the record that is the Location of the 'Best Address'.</param>
        /// <param name="ADataBase">An instantiated <see cref="TDataBase" /> object, or null (default = null). If null
        /// gets passed then the Method executes DB commands with a new Database connection</param>
        /// <returns>A <see cref="TLocationPK" /> which points to the 'Best Address'. If no 'Best Address' was found,
        /// SiteKey and LocationKey of this instance will be both -1.</returns>
        public static TLocationPK DetermineBestAddress(Int64 APartnerKey, out PPartnerLocationRow APartnerLocationDR,
            TDataBase ADataBase = null)
        {
            TLocationPK ReturnValue = new TLocationPK();
            PPartnerLocationTable PartnerLocationDT;
            Boolean NewTransaction;

            TDataBase db = DBAccess.Connect("DetermineBestAddress", ADataBase);
            TDBTransaction ReadTransaction = db.GetNewOrExistingTransaction(
                MCommonConstants.CACHEABLEDT_ISOLATIONLEVEL,
                out NewTransaction);

            try
            {
                PartnerLocationDT = PPartnerLocationAccess.LoadViaPPartner(APartnerKey, ReadTransaction);
                ReturnValue = Ict.Petra.Server.MPartner.Common.Calculations.DetermineBestAddress(PartnerLocationDT);

                APartnerLocationDR = (PPartnerLocationRow)PartnerLocationDT.Rows.Find(new object[]
                    { APartnerKey, ReturnValue.SiteKey, ReturnValue.LocationKey });
            }
            finally
            {
                if (NewTransaction)
                {
                    ReadTransaction.Commit();
                    TLogging.LogAtLevel(7, "ServerCalculations.DetermineBestAddress: commited own transaction.");
                }
            }

            return ReturnValue;
        }

        /// <summary>
        /// Determines which address is the 'Best Address' of a Partner, and returns the PLocation record which the
        /// 'Best Address' is pointing to.
        /// </summary>
        /// <remarks>There are two similar shared Methods in Namespace Ict.Petra.Server.MPartner.Common.Calculations,
        /// both called 'DetermineBestAddress' which work by passing in the PartnerLocations of a Partner in an Argument
        /// and which return a <see cref="TLocationPK" />. As those Methods don't access the database, these Methods
        /// can be used client-side as well!</remarks>
        /// <param name="APartnerKey">PartnerKey of the Partner whose addresses should be checked.</param>
        /// <param name="ALocationDR">PLocation Record that the 'Best Address' is pointing to.</param>
        /// <returns>A <see cref="TLocationPK" /> which points to the 'Best Address'. If no 'Best Address' was found,
        /// SiteKey and LocationKey of this instance will be both -1.</returns>
        public static TLocationPK DetermineBestAddress(Int64 APartnerKey, out PLocationRow ALocationDR)
        {
            TLocationPK BestLocation = new TLocationPK();
            PPartnerLocationRow Tmp;

            ALocationDR = null;

            BestLocation = DetermineBestAddress(APartnerKey, out Tmp, out ALocationDR);

            return BestLocation;
        }

        /// <summary>
        /// Determines which address is the 'Best Address' of a Partner, and returns the PLocation record which the
        /// 'Best Address' is pointing to.
        /// </summary>
        /// <remarks>There are two similar shared Methods in Namespace Ict.Petra.Server.MPartner.Common.Calculations,
        /// both called 'DetermineBestAddress' which work by passing in the PartnerLocations of a Partner in an Argument
        /// and which return a <see cref="TLocationPK" />. As those Methods don't access the database, these Methods
        /// can be used client-side as well!</remarks>
        /// <param name="APartnerKey">PartnerKey of the Partner whose addresses should be checked.</param>
        /// <param name="APartnerLocationDR">PPartnerLocation Record that is the record that is the Location of the 'Best Address'.</param>
        /// <param name="ALocationDR">PLocation Record that the 'Best Address' is pointing to.</param>
        /// <param name="ADataBase">An instantiated <see cref="TDataBase" /> object, or null (default = null). If null
        /// gets passed then the Method executes DB commands with a new Database connection</param>
        /// <returns>A <see cref="TLocationPK" /> which points to the 'Best Address'. If no 'Best Address' was found,
        /// SiteKey and LocationKey of this instance will be both -1.</returns>
        public static TLocationPK DetermineBestAddress(Int64 APartnerKey, out PPartnerLocationRow APartnerLocationDR,
            out PLocationRow ALocationDR, TDataBase ADataBase = null)
        {
            PLocationTable LocationDT;
            TLocationPK BestLocation = new TLocationPK();
            Boolean NewTransaction;

            APartnerLocationDR = null;
            ALocationDR = null;

            BestLocation = DetermineBestAddress(APartnerKey, out APartnerLocationDR);

            TDataBase db = DBAccess.Connect("DetermineBestAddress", ADataBase);
            TDBTransaction ReadTransaction = db.GetNewOrExistingTransaction(
                MCommonConstants.CACHEABLEDT_ISOLATIONLEVEL,
                out NewTransaction);

            try
            {
                LocationDT = PLocationAccess.LoadByPrimaryKey(BestLocation.SiteKey, BestLocation.LocationKey, ReadTransaction);

                if (LocationDT.Rows.Count > 0)
                {
                    ALocationDR = LocationDT[0];
                }
            }
            finally
            {
                if (NewTransaction)
                {
                    ReadTransaction.Commit();
                    TLogging.LogAtLevel(7, "ServerCalculations.DetermineBestAddress: commited own transaction.");
                }
            }

            return BestLocation;
        }
    }
}
