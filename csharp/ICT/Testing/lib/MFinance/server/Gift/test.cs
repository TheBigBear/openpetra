//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//
// Copyright 2004-2023 by OM International
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
using System.Configuration;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using System.IO;
using Ict.Testing.NUnitPetraServer;
using Ict.Common;
using Ict.Common.DB;
using Ict.Petra.Server.MFinance.Gift.WebConnectors;
using Ict.Common.Data;
using Ict.Common.Verification;

namespace Tests.MFinance.Server.Gift
{
    /// This will test the business logic directly on the server
    [TestFixture]
    public class TGiftTest
    {
        /// <summary>
        /// open database connection or prepare other things for this test
        /// </summary>
        [OneTimeSetUp]
        public void Init()
        {
            //new TLogging("TestServer.log");
            TPetraServerConnector.Connect("../../etc/TestServer.config");
        }

        /// <summary>
        /// cleaning up everything that was set up for this test
        /// </summary>
        [OneTimeTearDown]
        public void TearDown()
        {
            TPetraServerConnector.Disconnect();
        }

        /// <summary>
        /// Test the number of ledgers. This demonstrates a simple access to the database
        /// </summary>
        [Test]
        public void TestSimpleDatabaseAccess()
        {
            bool NewTransaction = false;
            TDataBase db = DBAccess.Connect("TestSimpleDatabaseAccess");

            TDBTransaction Transaction = db.GetNewOrExistingTransaction(IsolationLevel.Serializable, out NewTransaction);

            try
            {
                Assert.AreEqual(1, Ict.Petra.Server.MFinance.Account.Data.Access.ALedgerAccess.CountAll(Transaction), "Testing the number of ledgers");
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (NewTransaction)
                {
                    Transaction.Rollback();
                }
            }
        }

        /// <summary>
        /// this demonstrates a simple test of a webconnector.
        /// </summary>
        [Test]
        public void TestSimpleWebConnector()
        {
            // TReceiptingWebConnector is from Ict.Petra.Server.MFinance.Gift.WebConnectors
            // for the moment, we expect that there are no gifts in the database in the year 1978
            // the template would need to be loaded from an HTML file
            string receipts;
            string receiptsPDF;
            TVerificationResultCollection verification;
            bool result = TReceiptingWebConnector.CreateAnnualGiftReceipts(43, "Annual", new DateTime(1978, 1, 1), new DateTime(1978, 1, 31),
                "invalid HTML template", null, String.Empty, null, String.Empty,
                "de-DE",
                "Jahreszuwendungsbestätigung 2022",
                "Hallo {{donorName}}<br/>, im Anhang ist die Jahreszuwendungsbestätigung für das Jahr 2022",
                "buchhaltung@example.org", "Buchhaltung MeinVerein e.V.",
                "Zuwendungsbestätigung2022.pdf",
                out receiptsPDF, out receipts, out verification);

            Assert.AreEqual(false, result, "Testing if using a web connector works");
        }
    }
}
