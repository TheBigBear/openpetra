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
using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;

using Ict.Testing.NUnitPetraServer;
using Ict.Testing.NUnitTools;
using Ict.Common;
using Ict.Common.IO;
using Ict.Common.DB;
using Ict.Common.Verification;
using Ict.Common.Remoting.Shared;
using Ict.Petra.Server.App.Core;
using Ict.Petra.Server.MFinance.Gift;
using Ict.Petra.Server.MFinance.Gift.WebConnectors;
using Ict.Petra.Shared.MFinance.Gift.Data;

namespace Tests.MFinance.Server.Gift
{
    /// This will test the generation of the annual gift receipts on the server
    [TestFixture]
    public class TGiftAnnualReceiptTest
    {
        Int32 FLedgerNumber = -1;

        /// <summary>
        /// open database connection or prepare other things for this test
        /// </summary>
        [OneTimeSetUp]
        public void Init()
        {
            //new TLogging("TestServer.log");
            TPetraServerConnector.Connect("../../etc/TestServer.config");
            FLedgerNumber = TAppSettingsManager.GetInt32("LedgerNumber", 43);
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
        /// prepare the test case
        /// </summary>
        public static bool ImportAndPostGiftBatch(int ALedgerNumber, out TVerificationResultCollection VerificationResult)
        {
            TGiftImporting importer = new TGiftImporting();

            string testFile = TAppSettingsManager.GetValue("GiftBatch.file", "../../csharp/ICT/Testing/lib/MFinance/SampleData/sampleGiftBatch.csv");

            StreamReader sr = new StreamReader(testFile);
            string FileContent = sr.ReadToEnd();

            FileContent = FileContent.Replace("{ledgernumber}", ALedgerNumber.ToString());
            FileContent = FileContent.Replace("{thisyear}", DateTime.Today.Year.ToString());

            sr.Close();

            Hashtable parameters = new Hashtable();
            parameters.Add("Delimiter", ",");
            parameters.Add("ALedgerNumber", ALedgerNumber);
            parameters.Add("DateFormatString", "yyyy-MM-dd");
            parameters.Add("DatesMayBeIntegers", false);
            parameters.Add("NumberFormat", "American");
            parameters.Add("NewLine", Environment.NewLine);

            GiftBatchTDSAGiftDetailTable NeedRecipientLedgerNumber;
            bool refreshRequired;

            if (!importer.ImportGiftBatches(parameters, FileContent, out NeedRecipientLedgerNumber, out refreshRequired, out VerificationResult))
            {
                return false;
            }

            int BatchNumber = importer.GetLastGiftBatchNumber();
            Int32 generatedGlBatchNumber;

            if (!TGiftTransactionWebConnector.PostGiftBatch(ALedgerNumber, BatchNumber, out generatedGlBatchNumber, out VerificationResult))
            {
                CommonNUnitFunctions.EnsureNullOrOnlyNonCriticalVerificationResults(VerificationResult);

                return false;
            }

            TDataBase db = DBAccess.Connect("FixSendMailPartnerLocation");
            TDBTransaction t = new TDBTransaction();
            bool SubmissionOK = false;

            db.WriteTransaction(ref t, ref SubmissionOK,
                delegate
                {
                    // need to set sendmail = true for the donor with partner key 43005001
                    string sql = "UPDATE p_partner_location SET p_send_mail_l = true WHERE p_partner_key_n = 43005001";
                    db.ExecuteNonQuery(sql, t);
                    SubmissionOK = true;
                });

            return true;
        }

        /// <summary>
        /// print annual receipt
        /// </summary>
        [Test]
        public void TestAnnualReceipt()
        {
            CommonNUnitFunctions.ResetDatabase();
            TPetraServerConnector.Connect("../../etc/TestServer.config");

            // import a test gift batch
            TVerificationResultCollection VerificationResult;

            if (!ImportAndPostGiftBatch(FLedgerNumber, out VerificationResult))
            {
                Assert.Fail("ImportAndPostGiftBatch failed: " + VerificationResult.BuildVerificationResultString());
            }

            // TODO test reversed gifts

            string formletterTemplateFile = TAppSettingsManager.GetValue("ReceiptTemplate.file",
                "../../csharp/ICT/Testing/lib/MFinance/SampleData/AnnualReceiptTemplate.html");
            Encoding encodingOfHTMLfile = TTextFile.GetFileEncoding(formletterTemplateFile);
            StreamReader sr = new StreamReader(formletterTemplateFile, encodingOfHTMLfile, false);
            string FileContent = sr.ReadToEnd();

            sr.Close();

            string formletterExpectedFile = TAppSettingsManager.GetValue("ReceiptExpected.file",
                "../../csharp/ICT/Testing/lib/MFinance/SampleData/AnnualReceiptExpected.html");

            Catalog.Init("de-DE", "de-DE");
            sr = new StreamReader(formletterExpectedFile, encodingOfHTMLfile, false);
            string ExpectedFormletterContent = sr.ReadToEnd().
                                               Replace("#TODAY#", DateTime.Now.ToString("d. MMMM yyyy")).
                                               Replace("#THISYEAR#", DateTime.Today.Year.ToString());
            sr.Close();

            StreamWriter sw = new StreamWriter(formletterExpectedFile + ".updated", false, encodingOfHTMLfile);
            sw.WriteLine(ExpectedFormletterContent);
            sw.Close();

            //TODO: Calendar vs Financial Date Handling - Check if this should use financial year start/end and not assume calendar
            string receipts;
            string receiptsPDF;
            TVerificationResultCollection verification;
            bool result =
                TReceiptingWebConnector.CreateAnnualGiftReceipts(FLedgerNumber, "ANNUAL",
                    new DateTime(DateTime.Today.Year, 1, 1), new DateTime(DateTime.Today.Year, 12, 31),
                    FileContent, null, String.Empty, null, String.Empty, "de-DE",
                    "Jahreszuwendungsbestätigung 2022",
                    "Hallo {{donorName}}<br/>, im Anhang ist die Jahreszuwendungsbestätigung für das Jahr 2022",
                    "buchhaltung@example.org", "Buchhaltung MeinVerein e.V.",
                    "Zuwendungsbestätigung2022.pdf",
                    out receiptsPDF, out receipts, out verification);
            receipts = THttpBinarySerializer.DeserializeFromBase64(receipts);

            Assert.AreEqual(true, result, "receipt was empty");

            sw = new StreamWriter(formletterExpectedFile + ".new", false, encodingOfHTMLfile);
            sw.WriteLine(receipts);
            sw.WriteLine();
            sw.Close();

            Assert.IsTrue(
                TTextFile.SameContent(formletterExpectedFile + ".updated", formletterExpectedFile + ".new"),
                "receipt was not printed as expected, check " + formletterExpectedFile + ".new");

            File.Delete(formletterExpectedFile + ".new");
            File.Delete(formletterExpectedFile + ".updated");
        }
    }
}
