//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//       Tim Ingham
//       ChristianK
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
using System.Collections.Generic;
using System.Data;
using System.Xml;

using Ict.Common;
using Ict.Common.Exceptions;
using Ict.Common.IO;
using Ict.Common.Remoting.Server;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Shared.MPartner.Mailroom.Data;
using Ict.Petra.Shared.MPartner;
using Ict.Petra.Shared;
using Ict.Petra.Server.MSysMan.Common.WebConnectors;
using Ict.Petra.Shared.MPersonnel.Personnel.Data;
using Ict.Petra.Server.MPersonnel.Personnel.Data.Access;
using Ict.Petra.Server.MPartner.Partner.ServerLookups.WebConnectors;
using Ict.Common.Verification;
using Ict.Common.DB;
using Ict.Petra.Server.MPartner.Mailroom.Data.Access;
using Ict.Petra.Server.MPartner.Partner.Data.Access;
using Ict.Petra.Server.App.Core;
using Ict.Petra.Shared.MSysMan;
using Ict.Petra.Server.MPartner.Common;
using Ict.Petra.Server.MPartner.Partner.WebConnectors;

namespace Ict.Petra.Server.MPartner.ImportExport
{
    /// <summary>
    /// This will create a new partner and new relationships, match addresses etc
    /// </summary>
    public class TPartnerImportCSV
    {
        private Int32 FLocationKey = -1;
        private TVerificationResultCollection ResultsCol;
        private Int32 FCurrentLine = -1;
        private String ResultsContext;
        private List<String> FUnusedColumns;

        private void AddVerificationResult(String AResultText, TResultSeverity ASeverity = TResultSeverity.Resv_Critical)
        {
            if (ASeverity != TResultSeverity.Resv_Status)
            {
                TLogging.Log(AResultText);
            }

            ResultsCol.Add(new TVerificationResult(ResultsContext, AResultText, ASeverity));
        }

        private string GetAttribute(XmlNode ANode, string AAttrName)
        {
            if (FUnusedColumns.Contains(AAttrName))
            {
                FUnusedColumns.Remove(AAttrName);
            }
            return TXMLParser.GetAttribute(ANode, AAttrName);
        }

        /// <summary>
        /// Import data from a CSV file
        /// </summary>
        /// <param name="ANode"></param>
        /// <param name="AColumnNames"></param>
        /// <param name="ADateFormat">A date format string like MDY or DMY.  Only the first character is significant and must be M for month first.
        /// The date format string is only relevant to ambiguous dates which typically have a 1 or 2 digit month</param>
        /// <param name="AReferenceResults"></param>
        /// <returns></returns>
        public PartnerImportExportTDS ImportData(XmlNode ANode, List<string> AColumnNames, string ADateFormat, ref TVerificationResultCollection AReferenceResults)
        {
            PartnerImportExportTDS ResultDS = new PartnerImportExportTDS();

            FLocationKey = -1;
            int BankingDetailsKey = -1;
            ResultsCol = AReferenceResults;
            TDBTransaction Transaction = new TDBTransaction();
            bool SubmissionOK = true;
            FUnusedColumns = new List<string>(AColumnNames);

            // starting in line 2, because there is the line with the captions
            FCurrentLine = 2;

            DBAccess.WriteTransaction(ref Transaction,
                ref SubmissionOK,
                delegate
                {
                    while (ANode != null)
                    {
                        ResultsContext = "CSV Import";
                        String PartnerClass = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PARTNERCLASS).ToUpper();
                        Int64 PartnerKey = 0;
                        int LocationKey = 0;

                        if (PartnerClass.Length == 0)
                        {
                            PartnerClass = MPartnerConstants.PARTNERCLASS_FAMILY;

                            // if the import line contains an event then this will need to be for a person
                            if (ANode.Attributes[MPartnerConstants.PARTNERIMPORT_EVENTKEY] != null)
                            {
                                PartnerClass = MPartnerConstants.PARTNERCLASS_PERSON;
                            }

                            //TODOWB: if partner class is not set then check if for example a value is set for column "EventPartnerKey" in which case we can assume it is a Person that is imported
                            // there may be other fields that hint for using Person
                        }

                        if ((PartnerClass == MPartnerConstants.PARTNERCLASS_FAMILY) || (PartnerClass == MPartnerConstants.PARTNERCLASS_PERSON))
                        {
                            Boolean FamilyForPerson = (PartnerClass == MPartnerConstants.PARTNERCLASS_PERSON);
                            ResultsContext = "CSV Import Family";
                            PartnerKey = CreateNewFamily(ANode, FamilyForPerson, out LocationKey, ref ResultDS, Transaction);
                            CreateSpecialTypes(ANode, PartnerKey, "SpecialTypeFamily_", ref ResultDS, Transaction);
                            string AccountName = (GetAttribute(ANode, "FirstName") + " " +
                                GetAttribute(ANode, "FamilyName")).Trim();
                            CreateBankAccounts(ANode, PartnerKey, AccountName, ref BankingDetailsKey, "IBAN",
                                ref ResultDS, Transaction, ref ResultsCol);
                            CreateOutputData(ANode, PartnerKey, MPartnerConstants.PARTNERCLASS_FAMILY,
                                (PartnerClass == MPartnerConstants.PARTNERCLASS_FAMILY), ref ResultDS);
                        }

                        if (PartnerClass == MPartnerConstants.PARTNERCLASS_PERSON)
                        {
                            ResultsContext = "CSV Import Person";
                            Int64 PersonKey = CreateNewPerson(PartnerKey, LocationKey, ANode, ADateFormat, ref ResultDS, Transaction);
                            CreateShortTermApplication(ANode, PersonKey, ADateFormat, ref ResultDS, Transaction);
                            CreateSpecialTypes(ANode, PersonKey, ref ResultDS, Transaction);
                            CreateSubscriptions(ANode, PersonKey, ref ResultDS, Transaction);
                            CreateContacts(ANode, PersonKey, ADateFormat, ref ResultDS, "_1", Transaction);
                            CreateContacts(ANode, PersonKey, ADateFormat, ref ResultDS, "_2", Transaction);
                            CreatePassport(ANode, PersonKey, ADateFormat, ref ResultDS, Transaction);
                            CreateSpecialNeeds(ANode, PersonKey, ref ResultDS, Transaction);
                            CreateOutputData(ANode, PersonKey, PartnerClass, true, ref ResultDS);
                        }

                        ANode = ANode.NextSibling;
                        FCurrentLine += 1;
                    }
                });

            if (FUnusedColumns.Count > 0)
            {
                AddVerificationResult("Unknown Column(s): " + String.Join(" ", FUnusedColumns.ToArray()));
                return new PartnerImportExportTDS();
            }

            return ResultDS;
        }

        /// <summary>
        /// Create new partner, family, location and PartnerLocation records in MainDS
        /// </summary>
        private Int64 CreateNewFamily(XmlNode ANode, Boolean AFamilyForPerson, out int ALocationKey, ref PartnerImportExportTDS AMainDS,
            TDBTransaction ATransaction)
        {
            Int64 FamilyKey = 0;
            Boolean IsNewRecord = true;

            PPartnerRow newPartner = AMainDS.PPartner.NewRowTyped();

            AMainDS.PPartner.Rows.Add(newPartner);

            // first check in FamilyPartnerKey for Person Key, otherwise try to find family for given PersonPartnerKey, or then check in field PartnerKey
            String strPartnerKey = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_FAMILYPARTNERKEY);
            newPartner.PartnerKey = 0;

            if (strPartnerKey.Length > 0)
            {
                try
                {
                    FamilyKey = long.Parse(strPartnerKey);
                }
                catch (System.FormatException)
                {
                    AddVerificationResult("Bad number format in FamilyPartnerKey: " + strPartnerKey);
                }
            }
            else
            {
                if (AFamilyForPerson)
                {
                    // if we don't have the family partner key then we can check if we can find the family for a person partner key
                    String strPersonKey = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PERSONPARTNERKEY);

                    if (strPersonKey.Length > 0)
                    {
                        try
                        {
                            FamilyKey = TPartnerServerLookups.GetFamilyKeyForPerson(long.Parse(strPersonKey));
                        }
                        catch (System.FormatException)
                        {
                            AddVerificationResult("Bad number format in PersonPartnerKey: " + strPartnerKey);
                        }
                        catch (EOPAppException)
                        {
                            AddVerificationResult(string.Format("There is no matching Family key for the specified Person Partner key {0}",
                                    strPersonKey) +
                                "  Did you enter the key correctly?");
                        }
                    }
                }

                if ((FamilyKey == 0) && !AFamilyForPerson)
                {
                    strPartnerKey = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PARTNERKEY);

                    if (strPartnerKey.Length > 0)
                    {
                        try
                        {
                            FamilyKey = long.Parse(strPartnerKey);
                        }
                        catch (System.FormatException)
                        {
                            AddVerificationResult("Bad number format in PartnerKey: " + strPartnerKey);
                        }
                    }
                }
            }

            if (FamilyKey > 0)
            {
                //TODOWBxxx
                // now we know the family record that we need to look for in db
                PPartnerTable PartnerTable = PPartnerAccess.LoadByPrimaryKey(FamilyKey, ATransaction);

                if ((PartnerTable.Count > 0)
                    && (((PPartnerRow)PartnerTable.Rows[0]).PartnerClass == MPartnerConstants.PARTNERCLASS_FAMILY))
                {
                    // we have an existing family partner record
                    IsNewRecord = false;
                    newPartner.ItemArray = (object[])PartnerTable.Rows[0].ItemArray.Clone();
                    newPartner.AcceptChanges();
                }
                else
                {
                    // no partner found with this family key in this db --> need to create new family
                    IsNewRecord = true;
                    FamilyKey = 0;
                }
            }

            // now preset the partner key if it is not contained in the csv file or if given key cannot be found in db
            newPartner.PartnerKey = FamilyKey;

            if (newPartner.PartnerKey == 0)
            {
                newPartner.PartnerKey = (AMainDS.PPartner.Rows.Count + 1) * -1;
            }

            newPartner.PartnerClass = MPartnerConstants.PARTNERCLASS_FAMILY;
            newPartner.StatusCode = MPartnerConstants.PARTNERSTATUS_ACTIVE;

            if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_NOTESFAMILY))
            {
                newPartner.Comment = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_NOTESFAMILY);
            }

            if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_AQUISITION))
            {
                String AcquisitionCode = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_AQUISITION);

                newPartner.AcquisitionCode = (AcquisitionCode.Length > 0) ? AcquisitionCode : MPartnerConstants.PARTNERIMPORT_AQUISITION_DEFAULT;
            }
            else if (IsNewRecord)
            {
                newPartner.AcquisitionCode = MPartnerConstants.PARTNERIMPORT_AQUISITION_DEFAULT;
            }

            if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_ADDRESSEE_TYPE))
            {
                newPartner.AddresseeTypeCode = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_ADDRESSEE_TYPE);
            }
            else if (IsNewRecord)
            {
                newPartner.AddresseeTypeCode = MPartnerConstants.ADDRESSEETYPE_DEFAULT;

                string gender = GetGenderCode(ANode);

                if (gender == MPartnerConstants.GENDER_MALE)
                {
                    newPartner.AddresseeTypeCode = MPartnerConstants.ADDRESSEETYPE_MALE;
                }
                else if (gender == MPartnerConstants.GENDER_FEMALE)
                {
                    newPartner.AddresseeTypeCode = MPartnerConstants.ADDRESSEETYPE_FEMALE;
                }
            }

            if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_LANGUAGE))
            {
                newPartner.LanguageCode = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_LANGUAGE);
            }
            else if (IsNewRecord && TUserDefaults.HasDefault(MSysManConstants.PARTNER_LANGUAGECODE))
            {
                newPartner.LanguageCode = TUserDefaults.GetStringDefault(MSysManConstants.PARTNER_LANGUAGECODE);
            }

            if (IsNewRecord)
            {
                string[] giftReceiptingDefaults = new TSystemDefaults(ATransaction.DataBaseObj).GetStringDefault("GiftReceiptingDefaults", ",no").Split(new char[] { ',' });
                newPartner.ReceiptLetterFrequency = giftReceiptingDefaults[0];
                newPartner.ReceiptEachGift = giftReceiptingDefaults[1].ToUpper() == "YES" || giftReceiptingDefaults[1].ToUpper() == "TRUE";
            }

            PFamilyRow newFamily = AMainDS.PFamily.NewRowTyped();
            AMainDS.PFamily.Rows.Add(newFamily);

            if (!IsNewRecord)
            {
                // now we know the family record that we need to look for in db
                PFamilyTable FamilyTable = PFamilyAccess.LoadByPrimaryKey(FamilyKey, ATransaction);

                if (FamilyTable.Count > 0)
                {
                    // we have an existing family partner record
                    newFamily.ItemArray = (object[])FamilyTable.Rows[0].ItemArray.Clone();
                    newFamily.AcceptChanges();
                }
            }

            newFamily.PartnerKey = newPartner.PartnerKey;

            if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_FIRSTNAME))
            {
                newFamily.FirstName = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_FIRSTNAME);
            }

            if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_FAMILYNAME))
            {
                newFamily.FamilyName = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_FAMILYNAME);
            }

            if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_MARITALSTATUS))
            {
                newFamily.MaritalStatus = GetMaritalStatusCode(ANode, ATransaction);
            }

            if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_TITLE))
            {
                newFamily.Title = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_TITLE);
            }

            if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_LANGUAGE))
            {
                newPartner.LanguageCode = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_LANGUAGE);
            }

            if ((newFamily.FirstName == String.Empty) && (newFamily.FamilyName == String.Empty))
            {
                AddVerificationResult("Missing Firstname or family name in line " + FCurrentLine.ToString());
            }

            newPartner.PartnerShortName = Calculations.DeterminePartnerShortName(newFamily.FamilyName, newFamily.Title, newFamily.FirstName);
            PLocationRow newLocation = AMainDS.PLocation.NewRowTyped(true);
            AMainDS.PLocation.Rows.Add(newLocation);
            newLocation.LocationKey = FLocationKey;
            newLocation.Locality = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_ADDRESS1);
            newLocation.StreetName = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_STREET);
            newLocation.Address3 = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_ADDRESS3);
            newLocation.PostalCode = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_POSTCODE);
            newLocation.City = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_CITY);
            newLocation.County = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_COUNTY);
            newLocation.CountryCode = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_COUNTRYCODE);

            TPartnerContactDetails_LocationConversionHelper myHelper =
                new TPartnerContactDetails_LocationConversionHelper();

            PPartnerLocationRow partnerlocation = AMainDS.PPartnerLocation.NewRowTyped(true);
            myHelper.AddOldDBTableColumnsToPartnerLocation(AMainDS.PPartnerLocation);

            partnerlocation.LocationKey = FLocationKey;
            partnerlocation.SiteKey = 0;
            partnerlocation.PartnerKey = newPartner.PartnerKey;
            partnerlocation.DateEffective = DateTime.Now.Date;
            partnerlocation.LocationType = MPartnerConstants.LOCATIONTYPE_HOME;
            partnerlocation.SendMail = true;

            partnerlocation["p_email_address_c"] =
                GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_EMAIL);        // Important: Do not use 'partnerlocation.EmailAddress' as this Column will get removed once Contact Details conversion is finished!
            partnerlocation["p_telephone_number_c"] =
                GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PHONE);        // Important: Do not use 'partnerlocation.TelephoneNumber' as this Column will get removed once Contact Details conversion is finished!
            partnerlocation["p_mobile_number_c"] =
                GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_MOBILEPHONE);  // Important: Do not use 'partnerlocation.MobileNumber' as this Column will get removed once Contact Details conversion is finished!

            if ((newFamily.FirstName == String.Empty) && (newFamily.FamilyName == String.Empty))
            {
                AddVerificationResult("Missing Firstname or family name in line " + FCurrentLine.ToString());
            }

            bool HasAddress = (newLocation.StreetName != String.Empty) || (newLocation.City != String.Empty) || (newLocation.CountryCode != String.Empty);
            if (HasAddress)
            {
                if ((newLocation.StreetName == String.Empty) || (newLocation.City == String.Empty) || (newLocation.CountryCode == String.Empty))
                {
                    AddVerificationResult("Address is incomplete, we need streetname, city and country in line " + FCurrentLine.ToString());
                    HasAddress = false;
                }
            }
            bool HasContactDetail = (partnerlocation["p_email_address_c"].ToString() != String.Empty) || 
                (partnerlocation["p_telephone_number_c"].ToString() != String.Empty) ||
                (partnerlocation["p_mobile_number_c"].ToString() != String.Empty);
            if (!HasAddress && !HasContactDetail)
            {
                AddVerificationResult("Missing an address (streetname, city, country code) or phone number or email address in line " + FCurrentLine.ToString());
            }

            AMainDS.PPartnerLocation.Rows.Add(partnerlocation);

            ALocationKey = FLocationKey;
            FLocationKey--;

            return newPartner.PartnerKey;
        }

        /// <summary>
        /// Create a Partner and a Person having this FamilyKey, living at this address.
        /// </summary>
        /// <param name="AFamilyKey"></param>
        /// <param name="ALocationKey"></param>
        /// <param name="ANode"></param>
        /// <param name="ADateFormat">A date format string like MDY or DMY.  Only the first character is significant and must be M for month first.
        /// The date format string is only relevant to ambiguous dates which typically have a 1 or 2 digit month</param>
        /// <param name="AMainDS"></param>
        /// <param name="ATransaction"></param>
        private Int64 CreateNewPerson(Int64 AFamilyKey, int ALocationKey, XmlNode ANode, string ADateFormat,
            ref PartnerImportExportTDS AMainDS, TDBTransaction ATransaction)
        {
            Int64 PersonKey = 0;
            Boolean IsNewRecord = true;

            AMainDS.PFamily.DefaultView.RowFilter = String.Format("{0}={1}", PFamilyTable.GetPartnerKeyDBName(), AFamilyKey);
            PFamilyRow FamilyRow = (PFamilyRow)AMainDS.PFamily.DefaultView[0].Row;

            AMainDS.PPartner.DefaultView.RowFilter = String.Format("{0}={1}", PPartnerTable.GetPartnerKeyDBName(), AFamilyKey);
            PPartnerRow PartnerRow = (PPartnerRow)AMainDS.PPartner.DefaultView[0].Row;

            PPartnerRow newPartner = AMainDS.PPartner.NewRowTyped();
            AMainDS.PPartner.Rows.Add(newPartner);

            // check in PersonPartnerKey for Person Key, otherwise check in field PartnerKey
            String strPartnerKey = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PERSONPARTNERKEY);
            newPartner.PartnerKey = 0;

            if (strPartnerKey.Length > 0)
            {
                try
                {
                    PersonKey = long.Parse(strPartnerKey);
                }
                catch (System.FormatException)
                {
                    AddVerificationResult("Bad number format in PersonPartnerKey: " + strPartnerKey);
                }
            }
            else
            {
                strPartnerKey = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PARTNERKEY);

                if (strPartnerKey.Length > 0)
                {
                    try
                    {
                        PersonKey = long.Parse(strPartnerKey);
                    }
                    catch (System.FormatException)
                    {
                        AddVerificationResult("Bad number format in PartnerKey: " + strPartnerKey);
                    }
                }
            }

            if (PersonKey > 0)
            {
                //TODOWBxxx
                // now we know the family record that we need to look for in db
                PPartnerTable PartnerTable = PPartnerAccess.LoadByPrimaryKey(PersonKey, ATransaction);

                if ((PartnerTable.Count > 0)
                    && (((PPartnerRow)PartnerTable.Rows[0]).PartnerClass == MPartnerConstants.PARTNERCLASS_PERSON))
                {
                    // we have an existing person partner record
                    IsNewRecord = false;
                    newPartner.ItemArray = (object[])PartnerTable.Rows[0].ItemArray.Clone();
                    newPartner.AcceptChanges();
                }
                else
                {
                    // no partner found with this person key in this db --> need to create new person
                    IsNewRecord = true;
                    PersonKey = 0;
                }
            }

            // now preset the partner key if it is not contained in the csv file or if given key cannot be found in db
            newPartner.PartnerKey = PersonKey;

            if (newPartner.PartnerKey == 0)
            {
                newPartner.PartnerKey = (AMainDS.PPartner.Rows.Count + 1) * -1;
            }

            newPartner.PartnerClass = MPartnerConstants.PARTNERCLASS_PERSON;
            newPartner.AddresseeTypeCode = PartnerRow.AddresseeTypeCode;
            newPartner.PartnerShortName = PartnerRow.PartnerShortName;
            newPartner.LanguageCode = PartnerRow.LanguageCode;
            newPartner.Comment = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_NOTES);
            newPartner.AcquisitionCode = PartnerRow.AcquisitionCode;
            newPartner.StatusCode = MPartnerConstants.PARTNERSTATUS_ACTIVE;

            PPersonRow newPerson = AMainDS.PPerson.NewRowTyped();
            AMainDS.PPerson.Rows.Add(newPerson);

            if (!IsNewRecord)
            {
                // now we know the family record that we need to look for in db
                PPersonTable PersonTable = PPersonAccess.LoadByPrimaryKey(PersonKey, ATransaction);

                if (PersonTable.Count > 0)
                {
                    // we have an existing person partner record
                    newPerson.ItemArray = (object[])PersonTable.Rows[0].ItemArray.Clone();
                    newPerson.AcceptChanges();
                }
            }

            newPerson.PartnerKey = newPartner.PartnerKey;
            newPerson.FamilyKey = AFamilyKey;
            // When this record is imported, newPerson.FamilyId must be unique for this family!
            newPerson.FirstName = FamilyRow.FirstName;
            newPerson.FamilyName = FamilyRow.FamilyName;
            newPerson.Title = FamilyRow.Title;
            newPerson.Gender = GetGenderCode(ANode);
            newPerson.MaritalStatus = FamilyRow.MaritalStatus;

            String TimeString = "";
            try
            {
                TimeString = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_DATEOFBIRTH);

                if (TimeString.Length > 0)
                {
                    newPerson.DateOfBirth = DateTime.Parse(TimeString, StringHelper.GetCultureInfoForDateFormat(ADateFormat));
                }
            }
            catch (System.FormatException)
            {
                string fmt = ADateFormat.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "month-day-year" : "day-month-year";
                AddVerificationResult(string.Format("Bad date of birth: {0} (Expected format: {1})", TimeString, fmt), TResultSeverity.Resv_Critical);
            }


            PPartnerLocationRow newPartnerLocation = AMainDS.PPartnerLocation.NewRowTyped();
            AMainDS.PPartnerLocation.Rows.Add(newPartnerLocation);

            newPartnerLocation.LocationKey = ALocationKey; // This person lives at the same address as the family.
            newPartnerLocation.SiteKey = 0;
            newPartnerLocation.PartnerKey = newPartner.PartnerKey;
            newPartnerLocation.DateEffective = DateTime.Now.Date;
            newPartnerLocation.LocationType = MPartnerConstants.LOCATIONTYPE_HOME;
            newPartnerLocation.SendMail = false;

            string email = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_EMAIL);
            string phone = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PHONE);
            string mobile = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_MOBILEPHONE);

            if (email.Length > 0)
            {
                PPartnerAttributeRow partnerAttributeRow = AMainDS.PPartnerAttribute.NewRowTyped();
                partnerAttributeRow.PartnerKey = newPartner.PartnerKey;
                partnerAttributeRow.AttributeType = MPartnerConstants.ATTR_TYPE_EMAIL;
                partnerAttributeRow.Index = 0;
                partnerAttributeRow.Primary = true;
                partnerAttributeRow.Value = email;
                AMainDS.PPartnerAttribute.Rows.Add(partnerAttributeRow);

                partnerAttributeRow = AMainDS.PPartnerAttribute.NewRowTyped();
                partnerAttributeRow.PartnerKey = newPartner.PartnerKey;
                partnerAttributeRow.AttributeType = MPartnerConstants.ATTR_TYPE_PARTNERS_PRIMARY_CONTACT_METHOD;
                partnerAttributeRow.Index = 9999;
                partnerAttributeRow.Primary = false;
                partnerAttributeRow.Value = MPartnerConstants.ATTR_TYPE_EMAIL;
                AMainDS.PPartnerAttribute.Rows.Add(partnerAttributeRow);
            }

            if (phone.Length > 0)
            {
                PPartnerAttributeRow partnerAttributeRow = AMainDS.PPartnerAttribute.NewRowTyped();
                partnerAttributeRow.PartnerKey = newPartner.PartnerKey;
                partnerAttributeRow.AttributeType = MPartnerConstants.ATTR_TYPE_PHONE;
                partnerAttributeRow.Index = 0;
                partnerAttributeRow.Primary = true;
                partnerAttributeRow.Value = phone;
                AMainDS.PPartnerAttribute.Rows.Add(partnerAttributeRow);

                partnerAttributeRow = AMainDS.PPartnerAttribute.NewRowTyped();
                partnerAttributeRow.PartnerKey = newPartner.PartnerKey;
                partnerAttributeRow.AttributeType = MPartnerConstants.ATTR_TYPE_PARTNERS_PRIMARY_CONTACT_METHOD;
                partnerAttributeRow.Index = 9999;
                partnerAttributeRow.Primary = false;
                partnerAttributeRow.Value = MPartnerConstants.ATTR_TYPE_PHONE;
                AMainDS.PPartnerAttribute.Rows.Add(partnerAttributeRow);
            }

            if (mobile.Length > 0)
            {
                PPartnerAttributeRow partnerAttributeRow = AMainDS.PPartnerAttribute.NewRowTyped();
                partnerAttributeRow.PartnerKey = newPartner.PartnerKey;
                partnerAttributeRow.AttributeType = MPartnerConstants.ATTR_TYPE_MOBILE_PHONE;
                partnerAttributeRow.Index = 0;
                partnerAttributeRow.Primary = true;
                partnerAttributeRow.Value = mobile;
                AMainDS.PPartnerAttribute.Rows.Add(partnerAttributeRow);

                partnerAttributeRow = AMainDS.PPartnerAttribute.NewRowTyped();
                partnerAttributeRow.PartnerKey = newPartner.PartnerKey;
                partnerAttributeRow.AttributeType = MPartnerConstants.ATTR_TYPE_PARTNERS_PRIMARY_CONTACT_METHOD;
                partnerAttributeRow.Index = 9999;
                partnerAttributeRow.Primary = false;
                partnerAttributeRow.Value = MPartnerConstants.ATTR_TYPE_MOBILE_PHONE;
                AMainDS.PPartnerAttribute.Rows.Add(partnerAttributeRow);
            }

            AddVerificationResult("Person Record Created.", TResultSeverity.Resv_Status);

            return newPerson.PartnerKey;
        }

        private bool CreateBankAccounts(XmlNode ANode,
            Int64 APartnerKey,
            string AAccountName,
            ref int ABankingDetailsKey,
            String ACSVKey,
            ref PartnerImportExportTDS AMainDS,
            TDBTransaction ATransaction,
            ref TVerificationResultCollection AVerificationResult)
        {
            TVerificationResultCollection VerificationResult = new TVerificationResultCollection();
            bool MainAccount = true;

            for (int Idx = -1; Idx < 6; Idx++)
            {
                String IBAN = GetAttribute(ANode, ACSVKey + (Idx>=0?Idx.ToString():String.Empty)).Replace(" ", "");

                if (IBAN.Length > 0)
                {
                    // Validate IBAN, and calculate the BIC
                    string BIC;
                    string BankName;
                    if (!TSimplePartnerEditWebConnector.ValidateIBAN(IBAN, out BIC, out BankName, out VerificationResult))
                    {
                        AVerificationResult.Add(VerificationResult[0]);
                        return false;
                    }

                    // TODO: check for existing bank accounts, if this partner already exists in the database
                    // TODO: also check for main account
                    PBankingDetailsRow row = AMainDS.PBankingDetails.NewRowTyped();
                    row.BankingDetailsKey = ABankingDetailsKey;
                    row.BankingType = 0; // BANK ACCOUNT
                    row.AccountName = AAccountName;
                    row.Iban = IBAN;
                    row.BankKey = TSimplePartnerEditWebConnector.FindOrCreateBank(BIC, BankName);
                    AMainDS.PBankingDetails.Rows.Add(row);

                    PPartnerBankingDetailsRow pdrow = AMainDS.PPartnerBankingDetails.NewRowTyped();
                    pdrow.PartnerKey = APartnerKey;
                    pdrow.BankingDetailsKey = ABankingDetailsKey;
                    AMainDS.PPartnerBankingDetails.Rows.Add(pdrow);

                    if (MainAccount)
                    {
                        PBankingDetailsUsageRow newUsageRow = AMainDS.PBankingDetailsUsage.NewRowTyped(true);
                        newUsageRow.PartnerKey = APartnerKey;
                        newUsageRow.BankingDetailsKey = row.BankingDetailsKey;
                        newUsageRow.Type = MPartnerConstants.BANKINGUSAGETYPE_MAIN;
                        AMainDS.PBankingDetailsUsage.Rows.Add(newUsageRow);                        
                    }

                    MainAccount = false;
                    ABankingDetailsKey -= 1;
                }
            }

            return true;
        }

        private void CreateSpecialTypes(XmlNode ANode,
            Int64 APartnerKey,
            String ACSVKey,
            ref PartnerImportExportTDS AMainDS,
            TDBTransaction ATransaction)
        {
            for (int Idx = 1; Idx < 6; Idx++)
            {
                String SpecialType = GetAttribute(ANode, ACSVKey + Idx.ToString());

                if (SpecialType.Length > 0)
                {
                    // if partner type already exists for this partner then we don't need to import it again
                    if (!PPartnerTypeAccess.Exists(APartnerKey, SpecialType, ATransaction))
                    {
                        PPartnerTypeRow partnerType = AMainDS.PPartnerType.NewRowTyped(true);
                        partnerType.PartnerKey = APartnerKey;
                        partnerType.TypeCode = SpecialType;
                        AMainDS.PPartnerType.Rows.Add(partnerType);
                    }
                }
            }
        }

        private void CreateSpecialTypes(XmlNode ANode, Int64 APartnerKey, ref PartnerImportExportTDS AMainDS, TDBTransaction ATransaction)
        {
            // This previous code requires a format that doesn't conform to the documented standard:

/*
 *          if (GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_SPECIALTYPES).Length != 0)
 *          {
 *              string specialTypes = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_SPECIALTYPES);
 *
 *              while (specialTypes.Length > 0)
 *              {
 *                  PPartnerTypeRow partnerType = AMainDS.PPartnerType.NewRowTyped(true);
 *                  partnerType.PartnerKey = PartnerKey;
 *                  partnerType.TypeCode = StringHelper.GetNextCSV(ref specialTypes, ",").Trim().ToUpper();
 *                  AMainDS.PPartnerType.Rows.Add(partnerType);
 *              }
 *          }
 */
            CreateSpecialTypes(ANode, APartnerKey, "SpecialType_", ref AMainDS, ATransaction);
        }

        private void CreateShortTermApplication(XmlNode ANode,
            Int64 APartnerKey,
            string ADateFormat,
            ref PartnerImportExportTDS AMainDS,
            TDBTransaction ATransaction)
        {
            String strEventKey = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_EVENTKEY);
            long EventKey = -1;
            Boolean IsNewApplication = true;

            if (strEventKey.Length > 0)
            {
                try
                {
                    EventKey = long.Parse(strEventKey);
                }
                catch (System.FormatException)
                {
                    AddVerificationResult("Bad number format in EventKey: " + strEventKey);
                }

                if (!PUnitAccess.Exists(EventKey, ATransaction))
                {
                    AddVerificationResult("EventKey not known - application cannot be imported: " + EventKey);
                    return;
                }

                PmGeneralApplicationRow GenAppRow = AMainDS.PmGeneralApplication.NewRowTyped();
                PmShortTermApplicationRow ShortTermRow = AMainDS.PmShortTermApplication.NewRowTyped();


                //TODO: if an application of this person for this event already exists then load that data
                PmShortTermApplicationTable ShortTermTable = null;
                PmGeneralApplicationTable GenAppTable = null;
                PmShortTermApplicationRow TemplateShortTermRow = new PmShortTermApplicationTable().NewRowTyped(false);
                TemplateShortTermRow.PartnerKey = APartnerKey;
                TemplateShortTermRow.StConfirmedOption = EventKey;

                ShortTermTable = PmShortTermApplicationAccess.LoadUsingTemplate(TemplateShortTermRow, ATransaction);

                if (ShortTermTable.Count > 0)
                {
                    IsNewApplication = false;

                    // we have an existing application for this event, now find the general application record for it
                    ShortTermRow.ItemArray = (object[])ShortTermTable.Rows[0].ItemArray.Clone();

                    GenAppTable = PmGeneralApplicationAccess.LoadByPrimaryKey(APartnerKey,
                        ShortTermRow.ApplicationKey,
                        ShortTermRow.RegistrationOffice,
                        ATransaction);

                    if (GenAppTable.Count > 0)
                    {
                        GenAppRow.ItemArray = (object[])GenAppTable.Rows[0].ItemArray.Clone();
                    }
                    else
                    {
                        // this should not happen as there should always be a GenApp row for a ShortTermApp row
                        return;
                    }
                }
                else
                {
                    IsNewApplication = true;

                    // we need to create a new application
                    // initialize GenApp record
                    GenAppRow.PartnerKey = APartnerKey;
                    GenAppRow.ApplicationKey = (int)ATransaction.DataBaseObj.GetNextSequenceValue("seq_application", ATransaction);
                    GenAppRow.OldLink =
                        new TSystemDefaults(ATransaction.DataBaseObj).GetSiteKeyDefault() + ";" + GenAppRow.ApplicationKey.ToString();
                    GenAppRow.RegistrationOffice = DomainManager.GSiteKey; // When this is imported, RegistrationOffice can't be null.

                    // and initialize record for ShortTermApp
                    ShortTermRow.PartnerKey = APartnerKey;
                    ShortTermRow.ApplicationKey = GenAppRow.ApplicationKey;
                    ShortTermRow.RegistrationOffice = GenAppRow.RegistrationOffice; // When this is imported, RegistrationOffice can't be null.
                    ShortTermRow.StBasicOutreachId = GenAppRow.OldLink; //"Unused field"; // This field is scheduled for deletion, but NOT NULL now.
                    ShortTermRow.StAppDate = GenAppRow.GenAppDate;
                    ShortTermRow.StApplicationType = GenAppRow.AppTypeName;
                    ShortTermRow.StConfirmedOption = EventKey;
                }

                AMainDS.PmGeneralApplication.Rows.Add(GenAppRow);
                AMainDS.PmShortTermApplication.Rows.Add(ShortTermRow);
                AddVerificationResult("Application Record Created.", TResultSeverity.Resv_Status);

                if (!IsNewApplication)
                {
                    GenAppRow.AcceptChanges();
                    ShortTermRow.AcceptChanges();
                }

                // Make sure that application date is definitely set. If not in import file then use today's date.
                if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_APPDATE))
                {
                    string appDate = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_APPDATE);

                    if (appDate.Length > 0)
                    {
                        try
                        {
                            GenAppRow.GenAppDate = DateTime.Parse(appDate, StringHelper.GetCultureInfoForDateFormat(ADateFormat));
                        }
                        catch (FormatException)
                        {
                            string fmt = ADateFormat.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "month-day-year" : "day-month-year";
                            AddVerificationResult(string.Format("Bad general application date: {0} (Expected format: {1})",
                                    appDate,
                                    fmt), TResultSeverity.Resv_Critical);
                        }
                    }
                }

                if (IsNewApplication
                    && GenAppRow.IsGenAppDateNull())
                {
                    GenAppRow.GenAppDate = DateTime.Now;
                }

                ShortTermRow.StAppDate = GenAppRow.GenAppDate;

                if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_APPTYPE))
                {
                    GenAppRow.AppTypeName = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_APPTYPE);
                    ShortTermRow.StApplicationType = GenAppRow.AppTypeName;
                }

                if (GenAppRow.AppTypeName == "")
                {
                    // if column value is missing then preset this with TEENSTREET
                    GenAppRow.AppTypeName = "TEENSTREET";
                    ShortTermRow.StApplicationType = GenAppRow.AppTypeName;
                }

                if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_APPSTATUS))
                {
                    GenAppRow.GenApplicationStatus = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_APPSTATUS);
                }

                if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_APPCOMMENTS))
                {
                    GenAppRow.Comment = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_APPCOMMENTS);
                }

                String TimeString = "";

                try
                {
                    TimeString = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_ARRIVALDATE);

                    if (TimeString.Length > 0)
                    {
                        ShortTermRow.Arrival = DateTime.Parse(TimeString, StringHelper.GetCultureInfoForDateFormat(ADateFormat));
                    }

                    TimeString = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_DEPARTUREDATE);

                    if (TimeString.Length > 0)
                    {
                        ShortTermRow.Departure = DateTime.Parse(TimeString, StringHelper.GetCultureInfoForDateFormat(ADateFormat));
                    }
                }
                catch (System.FormatException)
                {
                    string fmt = ADateFormat.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "month-day-year" : "day-month-year";
                    AddVerificationResult(string.Format("Bad arrival/departure date format in Application: {0} (Expected format: {1})", TimeString,
                            fmt), TResultSeverity.Resv_Critical);
                }

                DateTime TempTime;

                TimeString = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_ARRIVALTIME);

                if (TimeString.Length > 0)
                {
                    try
                    {
                        TempTime = DateTime.Parse(TimeString, StringHelper.GetCultureInfoForDateFormat(ADateFormat));
                        ShortTermRow.ArrivalHour = TempTime.Hour;
                        ShortTermRow.ArrivalMinute = TempTime.Minute;
                    }
                    catch (System.FormatException)
                    {
                        string fmt = ADateFormat.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "month-day-year" : "day-month-year";
                        AddVerificationResult(string.Format("Bad arrival time format in Application: {0} (Expected format: {1})", TimeString,
                                fmt), TResultSeverity.Resv_Critical);
                    }
                }

                TimeString = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_DEPARTURETIME);

                if (TimeString.Length > 0)
                {
                    try
                    {
                        TempTime = DateTime.Parse(TimeString, StringHelper.GetCultureInfoForDateFormat(ADateFormat));
                        ShortTermRow.DepartureHour = TempTime.Hour;
                        ShortTermRow.DepartureMinute = TempTime.Minute;
                    }
                    catch (System.FormatException)
                    {
                        string fmt = ADateFormat.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "month-day-year" : "day-month-year";
                        AddVerificationResult(string.Format("Bad departure time format in Application: {0} (Expected format: {1})", TimeString,
                                fmt), TResultSeverity.Resv_Critical);
                    }
                }

                if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_EVENTROLE))
                {
                    ShortTermRow.StCongressCode = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_EVENTROLE);
                }

                String ChargedField = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_CHARGEDFIELD);

                if (ChargedField.Length > 0)
                {
                    try
                    {
                        ShortTermRow.StFieldCharged = long.Parse(ChargedField);
                    }
                    catch
                    {
                        AddVerificationResult("Bad number format in ChargedField: " + ChargedField);
                    }
                }
            }
        }

        private void CreatePassport(XmlNode ANode, Int64 APartnerKey, string ADateFormat, ref PartnerImportExportTDS AMainDS,
            TDBTransaction ATransaction)
        {
            string PassportNum = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PASSPORTNUMBER);

            if (PassportNum.Length > 0)
            {
                String DateString = "";
                Boolean IsNewRecord = true;

                PmPassportDetailsTable PassportTable = PmPassportDetailsAccess.LoadByPrimaryKey(APartnerKey, PassportNum, ATransaction);

                PmPassportDetailsRow NewRow = AMainDS.PmPassportDetails.NewRowTyped();

                if (PassportTable.Count > 0)
                {
                    IsNewRecord = false;
                    // we have an existing passport record
                    NewRow.ItemArray = (object[])PassportTable.Rows[0].ItemArray.Clone();
                }
                else
                {
                    IsNewRecord = true;
                    NewRow.PartnerKey = APartnerKey;
                    NewRow.PassportNumber = PassportNum;
                }

                AMainDS.PmPassportDetails.Rows.Add(NewRow);
                AddVerificationResult("Passport Record Created.", TResultSeverity.Resv_Status);

                if (!IsNewRecord)
                {
                    NewRow.AcceptChanges();
                }

                NewRow.FullPassportName = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PASSPORTNAME);
                NewRow.PassportDetailsType = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PASSPORTTYPE);
                NewRow.PlaceOfBirth = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PASSPORTPLACEOFBIRTH);
                NewRow.PassportNationalityCode = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PASSPORTNATIONALITY);
                NewRow.PlaceOfIssue = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PASSPORTPLACEOFISSUE);
                NewRow.CountryOfIssue = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PASSPORTCOUNTRYOFISSUE);

                DateString = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PASSPORTDATEOFISSUE);

                if (DateString.Length > 0)
                {
                    try
                    {
                        NewRow.DateOfIssue = DateTime.Parse(DateString, StringHelper.GetCultureInfoForDateFormat(ADateFormat));
                    }
                    catch (FormatException)
                    {
                        string fmt = ADateFormat.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "month-day-year" : "day-month-year";
                        AddVerificationResult(string.Format("Bad passport date of issue: {0} (Expected format: {1})", DateString, fmt),
                            TResultSeverity.Resv_Critical);
                    }
                }

                DateString = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_PASSPORTDATEOFEXPIRATION);

                if (DateString.Length > 0)
                {
                    try
                    {
                        NewRow.DateOfExpiration = DateTime.Parse(DateString, StringHelper.GetCultureInfoForDateFormat(ADateFormat));
                    }
                    catch (FormatException)
                    {
                        string fmt = ADateFormat.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "month-day-year" : "day-month-year";
                        AddVerificationResult(string.Format("Bad passport date of expiry: {0} (Expected format: {1})", DateString, fmt),
                            TResultSeverity.Resv_Critical);
                    }
                }
            }
        }

        private void CreateSpecialNeeds(XmlNode ANode, Int64 APartnerKey, ref PartnerImportExportTDS AMainDS,
            TDBTransaction ATransaction)
        {
            // only create special need record if data exists in import file
            if (TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_VEGETARIAN)
                || TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_MEDICALNEEDS)
                || TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_DIETARYNEEDS)
                || TXMLParser.HasAttribute(ANode, MPartnerConstants.PARTNERIMPORT_OTHERNEEDS))
            {
                PmSpecialNeedRow NewRow = AMainDS.PmSpecialNeed.NewRowTyped();
                Boolean IsNewRecord = true;

                PmSpecialNeedTable SpecialNeedTable = PmSpecialNeedAccess.LoadByPrimaryKey(APartnerKey, ATransaction);

                if (SpecialNeedTable.Count > 0)
                {
                    IsNewRecord = false;
                    // we have an existing special needs record
                    NewRow.ItemArray = (object[])SpecialNeedTable.Rows[0].ItemArray.Clone();
                }
                else
                {
                    IsNewRecord = true;
                    NewRow.PartnerKey = APartnerKey;
                }

                AMainDS.PmSpecialNeed.Rows.Add(NewRow);
                AddVerificationResult("Special Need Record Created.", TResultSeverity.Resv_Status);

                if (!IsNewRecord)
                {
                    NewRow.AcceptChanges();
                }

                NewRow.MedicalComment = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_MEDICALNEEDS);
                NewRow.DietaryComment = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_DIETARYNEEDS);
                NewRow.OtherSpecialNeed = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_OTHERNEEDS);

                if (GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_VEGETARIAN).ToLower() == "yes")
                {
                    NewRow.VegetarianFlag = true;
                }
                else
                {
                    NewRow.VegetarianFlag = false;
                }
            }
        }

        private void CreateSubscriptions(XmlNode ANode, Int64 APartnerKey, ref PartnerImportExportTDS AMainDS,
            TDBTransaction ATransaction)
        {
            int SubsCount = 0;
            String PublicationCode = "";

            foreach (XmlAttribute Attr in ANode.Attributes)
            {
                if ((Attr.Name.ToLower().IndexOf("subscribe_") == 0) && (Attr.Value.ToLower() == "yes"))
                {
                    PublicationCode = Attr.Name.Substring(10).ToUpper();

                    // only add subscription if it does not exist yet
                    //TODOWB: what about existing subscriptions that are cancelled? They would not get updated here.
                    if (!PSubscriptionAccess.Exists(PublicationCode, APartnerKey, ATransaction))
                    {
                        PSubscriptionRow NewRow = AMainDS.PSubscription.NewRowTyped();
                        NewRow.PartnerKey = APartnerKey;
                        NewRow.PublicationCode = PublicationCode;
                        AMainDS.PSubscription.Rows.Add(NewRow);
                        SubsCount++;
                    }
                }
            }

            if (SubsCount > 0)
            {
                AddVerificationResult("Subscriptions Created.", TResultSeverity.Resv_Status);
            }
        }

        private void CreateContacts(XmlNode ANode,
            Int64 APartnerKey,
            string ADateFormat,
            ref PartnerImportExportTDS AMainDS,
            string Suffix,
            TDBTransaction ATransaction)
        {
            //TODOWBxxx check for update/create

            string ContactCode = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_CONTACTCODE + Suffix);

            if (ContactCode.Length > 0)
            {
                String DateString = "";

                PartnerImportExportTDSPContactLogRow ContactLogRow = AMainDS.PContactLog.NewRowTyped();
                ContactLogRow.ContactCode = ContactCode;

                DateString = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_CONTACTDATE + Suffix);

                if (DateString.Length > 0)
                {
                    try
                    {
                        ContactLogRow.ContactDate = DateTime.Parse(DateString, StringHelper.GetCultureInfoForDateFormat(ADateFormat));
                    }
                    catch (FormatException)
                    {
                        string fmt = ADateFormat.StartsWith("M", StringComparison.OrdinalIgnoreCase) ? "month-day-year" : "day-month-year";
                        AddVerificationResult(string.Format("Bad contact date: {0} (Expected format: {1})", DateString, fmt),
                            TResultSeverity.Resv_Critical);
                    }
                }

                //DateTime ContactTime = DateTime.Parse(GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_CONTACTTIME + Suffix));
                //ContactLogRow.ContactTime = ((ContactTime.Hour * 60) + ContactTime.Minute * 60) + ContactTime.Second;
                ContactLogRow.Contactor = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_CONTACTOR + Suffix);
                ContactLogRow.ContactComment = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_CONTACTNOTES + Suffix);
                ContactLogRow.ContactAttr = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_CONTACTATTR + Suffix);
                ContactLogRow.ContactDetail = GetAttribute(ANode, MPartnerConstants.PARTNERIMPORT_CONTACTDETAIL + Suffix);
                AMainDS.PContactLog.Rows.Add(ContactLogRow);
                AddVerificationResult("Contact Log Record Created.", TResultSeverity.Resv_Status);

                var PartnerContactRow = AMainDS.PPartnerContact.NewRowTyped();
                PartnerContactRow.PartnerKey = APartnerKey;
                PartnerContactRow.ContactLogId = ContactLogRow.ContactLogId;
                AMainDS.PPartnerContact.Rows.Add(PartnerContactRow);
                AddVerificationResult("Contact Associated with Partner", TResultSeverity.Resv_Status);
            }
        }

        /// <summary>
        /// Returns the gender of the currently selected partner
        /// </summary>
        /// <returns></returns>
        private string GetGenderCode(XmlNode ACurrentPartnerNode)
        {
            string gender = GetAttribute(ACurrentPartnerNode, MPartnerConstants.PARTNERIMPORT_GENDER);

            if ((gender.ToLower() == Catalog.GetString("Female").ToLower()) || (gender.ToLower() == "female"))
            {
                return MPartnerConstants.GENDER_FEMALE;
            }
            else if ((gender.ToLower() == Catalog.GetString("Male").ToLower()) || (gender.ToLower() == "male"))
            {
                return MPartnerConstants.GENDER_MALE;
            }

            return MPartnerConstants.GENDER_UNKNOWN;
        }

        private string GetTitle(XmlNode ACurrentPartnerNode)
        {
            if (TXMLParser.HasAttribute(ACurrentPartnerNode, MPartnerConstants.PARTNERIMPORT_TITLE))
            {
                return GetAttribute(ACurrentPartnerNode, MPartnerConstants.PARTNERIMPORT_TITLE);
            }

            string genderCode = GetGenderCode(ACurrentPartnerNode);

            if (genderCode == MPartnerConstants.GENDER_MALE)
            {
                return Catalog.GetString("Mr");
            }
            else if (genderCode == MPartnerConstants.GENDER_FEMALE)
            {
                return Catalog.GetString("Ms");
            }

            return "";
        }

        private string GetMaritalStatusCode(XmlNode ACurrentPartnerNode, TDBTransaction ATransaction)
        {
            if (TXMLParser.HasAttribute(ACurrentPartnerNode, MPartnerConstants.PARTNERIMPORT_MARITALSTATUS))
            {
                string maritalStatus = GetAttribute(ACurrentPartnerNode, MPartnerConstants.PARTNERIMPORT_MARITALSTATUS);

                // first look for special cases
                if (maritalStatus.ToLower() == Catalog.GetString("married").ToLower())
                {
                    return MPartnerConstants.MARITALSTATUS_MARRIED;
                }
                else if (maritalStatus.ToLower() == Catalog.GetString("engaged").ToLower())
                {
                    return MPartnerConstants.MARITALSTATUS_ENGAGED;
                }
                else if (maritalStatus.ToLower() == Catalog.GetString("single").ToLower())
                {
                    return MPartnerConstants.MARITALSTATUS_SINGLE;
                }
                else if (maritalStatus.ToLower() == Catalog.GetString("divorced").ToLower())
                {
                    return MPartnerConstants.MARITALSTATUS_DIVORCED;
                }

                // now check for value of marital status in setup table
                if (PtMaritalStatusAccess.Exists(maritalStatus, ATransaction))
                {
                    return maritalStatus;
                }
            }

            return "";
        }

        #region Output CSV Data

        private void CreateOutputData(XmlNode ANode,
            Int64 APartnerKey,
            string APartnerClass,
            bool AIsFromFile,
            ref PartnerImportExportTDS AMainDS)
        {
            PartnerImportExportTDSOutputDataRow newRow = AMainDS.OutputData.NewRowTyped();

            newRow.IsFromFile = AIsFromFile;
            newRow.PartnerClass = APartnerClass;
            newRow.ImportStatus = "N";
            newRow.PartnerShortName = string.Empty;
            newRow.ImportID = string.Empty;

            if (APartnerClass == MPartnerConstants.PARTNERCLASS_FAMILY)
            {
                newRow.OutputFamilyPartnerKey = APartnerKey;
            }
            else if (APartnerClass == MPartnerConstants.PARTNERCLASS_PERSON)
            {
                newRow.OutputPersonPartnerKey = APartnerKey;
            }

            if (AIsFromFile)
            {
                if (ANode.Attributes["ImportID"] != null)
                {
                    newRow.ImportID = ANode.Attributes["ImportID"].Value;
                }
                else if (ANode.Attributes["EnrolmentID"] != null)
                {
                    // British spelling
                    newRow.ImportID = ANode.Attributes["EnrolmentID"].Value;
                }
                else if (ANode.Attributes["EnrollmentID"] != null)
                {
                    // American spelling
                    newRow.ImportID = ANode.Attributes["EnrollmentID"].Value;
                }

                if ((ANode.Attributes["FirstName"] != null) && (ANode.Attributes["FamilyName"] != null))
                {
                    string title = string.Empty;

                    if (ANode.Attributes["Title"] != null)
                    {
                        title = ANode.Attributes["Title"].Value;
                    }

                    newRow.PartnerShortName =
                        Calculations.DeterminePartnerShortName(ANode.Attributes["FamilyName"].Value, title, ANode.Attributes["FirstName"].Value);
                }

                long key;

                if (APartnerClass == MPartnerConstants.PARTNERCLASS_FAMILY)
                {
                    if ((ANode.Attributes["FamilyPartnerKey"] != null) && (ANode.Attributes["FamilyPartnerKey"].Value.Length > 0))
                    {
                        if (Int64.TryParse(ANode.Attributes["FamilyPartnerKey"].Value, out key))
                        {
                            newRow.InputPartnerKey = key;
                        }
                    }
                }
                else if (APartnerClass == MPartnerConstants.PARTNERCLASS_PERSON)
                {
                    if ((ANode.Attributes["PersonPartnerKey"] != null) && (ANode.Attributes["PersonPartnerKey"].Value.Length > 0))
                    {
                        if (Int64.TryParse(ANode.Attributes["PersonPartnerKey"].Value, out key))
                        {
                            newRow.InputPartnerKey = key;
                        }
                    }
                }
            }

            AMainDS.OutputData.Rows.Add(newRow);
        }

        #endregion
    }
}
