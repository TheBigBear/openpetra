//
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
using Ict.Common.Data;
using Ict.Common.Verification;
using Ict.Petra.Shared;
using Ict.Petra.Shared.MCommon.Data;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Shared.MPartner.Mailroom.Data;
using Ict.Petra.Shared.MPersonnel.Personnel.Data;
using Ict.Petra.Shared.MPersonnel.Units.Data;

namespace Ict.Petra.Server.MCommon.Validation
{
    /// <summary>
    /// Contains functions for the validation of Cacheable DataTables.
    /// </summary>
    public static partial class TValidation_CacheableDataTables
    {
        /// <summary>
        /// Validates the Setup Countries screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateCountrySetupManual(object AContext, PCountryRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'International Access Code' must have a value
            ValidationColumn = ARow.Table.Columns[PCountryTable.ColumnInternatAccessCodeId];

            VerificationResult = TStringChecks.StringMustNotBeEmpty(ARow.InternatAccessCode,
                String.Empty,
                AContext, ValidationColumn);

            // Handle addition to/removal from TVerificationResultCollection
            AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);

            if (!ARow.IsInternatTelephoneCodeNull())
            {
                // 'International Telephone Code' must be positive
                ValidationColumn = ARow.Table.Columns[PCountryTable.ColumnInternatTelephoneCodeId];

                VerificationResult = TNumericalChecks.IsPositiveOrZeroInteger(ARow.InternatTelephoneCode,
                    String.Empty,
                    AContext, ValidationColumn);

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }

            if (!ARow.IsTimeZoneMinimumNull()
                && !ARow.IsTimeZoneMaximumNull())
            {
                // 'Time Zone From' must be <= 'Time Zone To'
                ValidationColumn = ARow.Table.Columns[PCountryTable.ColumnTimeZoneMinimumId];

                VerificationResult = TNumericalChecks.FirstLesserOrEqualThanSecondDecimal(
                    ARow.TimeZoneMinimum, ARow.TimeZoneMaximum,
                    String.Empty, String.Empty,
                    AContext, ValidationColumn);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }

            // 'International Postal Type' must be in 'p_international_postal_type' DB Table (this DB Table is not a Cacheable DataTable)
            ValidationColumn = ARow.Table.Columns[PCountryTable.ColumnInternatPostalTypeCodeId];

            VerificationResult = TCommonValidation.IsValidInternationalPostalCode(ARow.InternatPostalTypeCode,
                String.Empty,
                AContext, ValidationColumn);

            // Handle addition to/removal from TVerificationResultCollection
            AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
        }

        /// <summary>
        /// Validates the Setup Frequency screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateFrequencySetupManual(object AContext, AFrequencyRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult;
            bool bFoundNegativeValue = false;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'NumberOfYears' cannot be negative
            ValidationColumn = ARow.Table.Columns[AFrequencyTable.ColumnNumberOfYearsId];

            VerificationResult = TNumericalChecks.IsPositiveOrZeroInteger(ARow.NumberOfYears,
                String.Empty,
                AContext, ValidationColumn);

            // Handle addition/removal to/from TVerificationResultCollection
            AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            bFoundNegativeValue |= (VerificationResult != null);

            // 'NumberOfMonths' cannot be negative
            ValidationColumn = ARow.Table.Columns[AFrequencyTable.ColumnNumberOfMonthsId];

            VerificationResult = TNumericalChecks.IsPositiveOrZeroInteger(ARow.NumberOfMonths,
                String.Empty,
                AContext, ValidationColumn);

            // Handle addition/removal to/from TVerificationResultCollection
            AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            bFoundNegativeValue |= (VerificationResult != null);

            // 'NumberOfDays' cannot be negative
            ValidationColumn = ARow.Table.Columns[AFrequencyTable.ColumnNumberOfDaysId];

            if (true)
            {
                VerificationResult = TNumericalChecks.IsPositiveOrZeroInteger(ARow.NumberOfDays,
                    String.Empty,
                    AContext, ValidationColumn);

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
                bFoundNegativeValue |= (VerificationResult != null);
            }

            // 'NumberOfHours' cannot be negative
            ValidationColumn = ARow.Table.Columns[AFrequencyTable.ColumnNumberOfHoursId];

            if (true)
            {
                VerificationResult = TNumericalChecks.IsPositiveOrZeroInteger(ARow.NumberOfHours,
                    String.Empty,
                    AContext, ValidationColumn);

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
                bFoundNegativeValue |= (VerificationResult != null);
            }

            // 'NumberOfMinutes' cannot be negative
            ValidationColumn = ARow.Table.Columns[AFrequencyTable.ColumnNumberOfMinutesId];

            if (true)
            {
                VerificationResult = TNumericalChecks.IsPositiveOrZeroInteger(ARow.NumberOfMinutes,
                    String.Empty,
                    AContext, ValidationColumn);

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
                bFoundNegativeValue |= (VerificationResult != null);
            }

            // Finally, having checked that no single box is negative, at least one of the boxes (any box) must be a positive number
            // So our test is going to fail if the sum of the boxes is 0 and we did not get any negatives
            // We pick the first box and invalidate that, because this is only one error despite all boxes being 0.
            // This does mean that the tooltip will only pop up if the focus is associated with this one box, but the validation will still work.
            // It will not be possible to leave this record.
            ValidationColumn = ARow.Table.Columns[AFrequencyTable.ColumnNumberOfYearsId];

            if (true)
            {
                // Check for success as a positive integer in TotalOfBoxes
                // If we had a negative number anywhere we always make this test pass, because that is a more serious error
                int TotalOfBoxes = ARow.NumberOfYears + ARow.NumberOfMonths + ARow.NumberOfDays + ARow.NumberOfHours + ARow.NumberOfMinutes;

                if (bFoundNegativeValue)
                {
                    TotalOfBoxes = 1;
                }

                VerificationResult = TNumericalChecks.IsPositiveInteger(TotalOfBoxes,
                    String.Empty,
                    AContext, ValidationColumn);

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);

                if (VerificationResult != null)
                {
                    // Over-ride the message as follows...
                    string msg = String.Format(Catalog.GetString(
                            "A quantity of time must be defined for the '{0}' frequency."), ARow.FrequencyDescription);
                    VerificationResult.OverrideResultText(msg);
                }
            }
        }

        /// <summary>
        /// Validates the Setup Partner Acquisition Code screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateAcquisitionCodeSetup(object AContext, PAcquisitionRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AcquisitionDescription' must have a value
            ValidationColumn = ARow.Table.Columns[PAcquisitionTable.ColumnAcquisitionDescriptionId];

            if (true)
            {
                VerificationResult = TStringChecks.StringMustNotBeEmpty(ARow.AcquisitionDescription,
                    String.Empty,
                    AContext, ValidationColumn);

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPartner Marital Status screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateMaritalStatus(object AContext, PtMaritalStatusRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtMaritalStatusTable.ColumnAssignableDateId];

            if (!ARow.AssignableFlag)
            {
                VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.AssignableDate,
                    String.Empty, AVerificationResultCollection, true,
                    AContext, ValidationColumn);
            }

            // Handle addition to/removal from TVerificationResultCollection
            AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
        }

        /// <summary>
        /// Validates the MPartner Relation Category screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateRelationCategory(object AContext, PRelationCategoryRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'UnssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtMaritalStatusTable.ColumnAssignableDateId];

            if (ARow.UnassignableFlag)
            {
                VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                    String.Empty, AVerificationResultCollection, true,
                    AContext, ValidationColumn);
            }

            // Handle addition to/removal from TVerificationResultCollection
            AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
        }

        /// <summary>
        /// Validates the MCommon Local Data Field Setup screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateLocalDataFieldSetup(object AContext, PDataLabelRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // If the 'DataType' is 'lookup' then categoryCode cannot be empty string (which would indicate no entries in the DataLabelCategory DB table)
            VerificationResult = null;
            ValidationColumn = ARow.Table.Columns[PDataLabelTable.ColumnLookupCategoryCodeId];

            if (String.Compare(ARow.DataType, "lookup", true) == 0)
            {
                VerificationResult = TStringChecks.StringMustNotBeEmpty(ARow.LookupCategoryCode,
                    String.Empty,
                    AContext, ValidationColumn);

                if (VerificationResult != null)
                {
                    VerificationResult.OverrideResultText(Catalog.GetString(
                            "You cannot use the option list until you have defined at least one option using the 'Local Data Option List Names' main menu selection"));
                }
            }

            // Handle addition to/removal from TVerificationResultCollection
            AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
        }

        /// <summary>
        /// Validates the MPersonnel Application Type screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateApplicationType(object AContext, PtApplicationTypeRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtApplicationTypeTable.ColumnUnassignableDateId];

            if (ARow.UnassignableFlag)
            {
                VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                    String.Empty, AVerificationResultCollection, true,
                    AContext, ValidationColumn);
            }

             // Handle addition to/removal from TVerificationResultCollection
             AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
        }

        /// <summary>
        /// Validates the MPersonnel Applicant Status screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateApplicantStatus(object AContext, PtApplicantStatusRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtApplicantStatusTable.ColumnUnassignableDateId];

            if (ARow.UnassignableFlag)
            {
                VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                    String.Empty, AVerificationResultCollection, true,
                    AContext, ValidationColumn);
            }

            // Handle addition to/removal from TVerificationResultCollection
            AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
        }

        /// <summary>
        /// Validates the MPersonnel LeadershipRating screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateLeadershipRating(object AContext, PtLeadershipRatingRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtLeadershipRatingTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Event Role screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateEventRole(object AContext, PtCongressCodeRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtCongressCodeTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Arrival/Departure Point screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateArrivalDeparturePoint(object AContext, PtArrivalPointRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtArrivalPointTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Ability Area screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateAbilityArea(object AContext, PtAbilityAreaRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtAbilityAreaTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Ability Level screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateAbilityLevel(object AContext, PtAbilityLevelRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtAbilityLevelTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Professional Area screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateProfessionalArea(object AContext, PtQualificationAreaRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtQualificationAreaTable.ColumnQualificationDateId];

            if (true)
            {
                if (ARow.QualificationFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.QualificationDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Qualification Level screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateQualificationLevel(object AContext, PtQualificationLevelRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtQualificationLevelTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Positions screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidatePositions(object AContext, PtPositionRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtPositionTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Job Assignment Type screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateJobAssignmentTypes(object AContext, PtAssignmentTypeRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtAssignmentTypeTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Language Level screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateLanguageLevel(object AContext, PtLanguageLevelRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtLanguageLevelTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Passport Type screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidatePassportType(object AContext, PtPassportTypeRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtPassportTypeTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Document Type screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateDocumentType(object AContext, PmDocumentTypeRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PmDocumentTypeTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }

            // 'Document Category' must not be unassignable
            ValidationColumn = ARow.Table.Columns[PmDocumentTypeTable.ColumnDocCategoryId];

            if (true)
            {
                PmDocumentCategoryTable DocumentCategoryTable;
                PmDocumentCategoryRow DocumentCategoryRow;

                VerificationResult = null;

                DocumentCategoryTable = (PmDocumentCategoryTable)TSharedDataCache.TMPersonnel.GetCacheablePersonnelTable(
                    TCacheablePersonTablesEnum.DocumentTypeCategoryList);
                DocumentCategoryRow = (PmDocumentCategoryRow)DocumentCategoryTable.Rows.Find(new object[] { ARow.DocCategory });

                // 'Document Category' must not be unassignable
                if ((DocumentCategoryRow != null)
                    && DocumentCategoryRow.UnassignableFlag
                    && (DocumentCategoryRow.IsUnassignableDateNull()
                        || (DocumentCategoryRow.UnassignableDate <= DateTime.Today)))
                {
                    // if 'Document Category' is unassignable then check if the value has been changed or if it is a new record
                    if (TValidationHelper.IsRowAddedOrFieldModified(ARow, PmDocumentTypeTable.GetDocCategoryDBName()))
                    {
                        VerificationResult = new TScreenVerificationResult(new TVerificationResult(AContext,
                                ErrorCodes.GetErrorInfo(PetraErrorCodes.ERR_VALUEUNASSIGNABLE_WARNING,
                                    new string[] { String.Empty, ARow.DocCategory })),
                            ValidationColumn);
                    }
                }

                // Handle addition/removal to/from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Document Type Category screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateDocumentTypeCategory(object AContext, PmDocumentCategoryRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'Unssignable Date' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PmDocumentCategoryTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Skill Category screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateSkillCategory(object AContext, PtSkillCategoryRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtSkillCategoryTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Skill Level screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateSkillLevel(object AContext, PtSkillLevelRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtSkillLevelTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Country Event Level screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateCountryEventLevel(object AContext, PtOutreachPreferenceLevelRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtOutreachPreferenceLevelTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Organisation Contact screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateOrganisationContact(object AContext, PtContactRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // Don't validate deleted DataRows
            if (ARow.RowState == DataRowState.Deleted)
            {
                return;
            }

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtContactTable.ColumnUnassignableDateId];

            if (true)
            {
                if (ARow.UnassignableFlag)
                {
                    VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                        String.Empty, AVerificationResultCollection, true,
                        AContext, ValidationColumn);
                }

                // Handle addition to/removal from TVerificationResultCollection
                AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
            }
        }

        /// <summary>
        /// Validates the MPersonnel Transport Type screen data.
        /// </summary>
        /// <param name="AContext">Context that describes where the data validation failed.</param>
        /// <param name="ARow">The <see cref="DataRow" /> which holds the the data against which the validation is run.</param>
        /// <param name="AVerificationResultCollection">Will be filled with any <see cref="TVerificationResult" /> items if
        /// data validation errors occur.</param>
        public static void ValidateTransportType(object AContext, PtTravelTypeRow ARow,
            ref TVerificationResultCollection AVerificationResultCollection)
        {
            DataColumn ValidationColumn;
            TVerificationResult VerificationResult = null;

            // 'AssignableDate' must not be empty if the flag is set
            ValidationColumn = ARow.Table.Columns[PtTravelTypeTable.ColumnUnassignableDateId];

            if (ARow.UnassignableFlag)
            {
                VerificationResult = TValidationControlHelper.IsNotInvalidDate(ARow.UnassignableDate,
                    String.Empty, AVerificationResultCollection, true,
                    AContext, ValidationColumn);
            }

            // Handle addition to/removal from TVerificationResultCollection
            AVerificationResultCollection.Auto_Add_Or_AddOrRemove(AContext, VerificationResult);
        }
    }
}
