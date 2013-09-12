-- Generated with nant generateORMTables
DELETE FROM s_group_file_info;
DELETE FROM s_volume_partner_group_partner;
DELETE FROM s_default_file_volume;
DELETE FROM s_volume_partner_group;
DELETE FROM pm_application_file;
DELETE FROM pm_document_file;
DELETE FROM p_partner_contact_file;
DELETE FROM pm_person_file;
DELETE FROM p_partner_file;
DELETE FROM p_partner_graphic;
DELETE FROM p_file_info;
DELETE FROM s_volume;
DELETE FROM s_workflow_instance_step;
DELETE FROM s_workflow_instance;
DELETE FROM s_function_relationship;
DELETE FROM s_workflow_step;
DELETE FROM s_workflow_group;
DELETE FROM s_workflow_user;
DELETE FROM s_workflow_definition;
DELETE FROM p_foundation_deadline;
DELETE FROM p_foundation_proposal_detail;
DELETE FROM p_foundation_proposal;
DELETE FROM p_foundation_proposal_status;
DELETE FROM p_foundation;
DELETE FROM p_proposal_submission_type;
DELETE FROM p_partner_comment;
DELETE FROM s_label;
DELETE FROM s_change_event;
DELETE FROM s_group_extract;
DELETE FROM s_group_cost_centre;
DELETE FROM s_group_ledger;
DELETE FROM s_group_data_label;
DELETE FROM s_group_partner_location;
DELETE FROM s_group_location;
DELETE FROM s_group_partner_reminder;
DELETE FROM s_group_partner_contact;
DELETE FROM s_group_motivation;
DELETE FROM s_group_gift;
DELETE FROM p_partner_set_partner;
DELETE FROM s_group_partner_set;
DELETE FROM p_partner_set;
DELETE FROM s_job_group;
DELETE FROM s_group_function;
DELETE FROM s_function;
DELETE FROM a_ap_anal_attrib;
DELETE FROM a_ap_document_detail;
DELETE FROM a_trans_anal_attrib;
DELETE FROM a_transaction;
DELETE FROM a_processed_fee;
DELETE FROM a_gift_detail;
DELETE FROM a_gift;
DELETE FROM a_gift_batch;
DELETE FROM a_recurring_gift_detail;
DELETE FROM a_recurring_gift;
DELETE FROM a_recurring_gift_batch;
DELETE FROM a_recurring_trans_anal_attrib;
DELETE FROM a_recurring_transaction;
DELETE FROM a_motivation_detail_fee;
DELETE FROM a_ep_transaction;
DELETE FROM a_ep_match;
DELETE FROM a_ep_account;
DELETE FROM a_motivation_detail;
DELETE FROM a_ich_stewardship;
DELETE FROM a_general_ledger_master_period;
DELETE FROM a_general_ledger_master;
DELETE FROM a_fees_receivable;
DELETE FROM a_fees_payable;
DELETE FROM a_analysis_attribute;
DELETE FROM a_budget_period;
DELETE FROM a_budget;
DELETE FROM a_valid_ledger_number;
DELETE FROM a_cost_centre;
DELETE FROM a_key_focus_area;
DELETE FROM p_partner_action;
DELETE FROM p_partner_state;
DELETE FROM p_partner_short_code;
DELETE FROM p_partner_field_of_service;
DELETE FROM p_partner_reminder;
DELETE FROM p_partner_merge;
DELETE FROM p_partner_interest;
DELETE FROM p_tax;
DELETE FROM ph_room_booking;
DELETE FROM ph_booking;
DELETE FROM pc_conference_venue;
DELETE FROM pc_room_attribute;
DELETE FROM pc_room_alloc;
DELETE FROM pc_room;
DELETE FROM pc_building;
DELETE FROM pc_supplement;
DELETE FROM pc_group;
DELETE FROM pc_early_late;
DELETE FROM pc_extra_cost;
DELETE FROM pc_conference_cost;
DELETE FROM pc_attendee;
DELETE FROM pc_discount;
DELETE FROM pc_conference_option;
DELETE FROM pc_conference;
DELETE FROM um_unit_evaluation;
DELETE FROM um_unit_cost;
DELETE FROM um_unit_language;
DELETE FROM um_unit_ability;
DELETE FROM pm_job_assignment;
DELETE FROM um_job_qualification;
DELETE FROM um_job_language;
DELETE FROM um_job_requirement;
DELETE FROM um_job;
DELETE FROM pm_person_commitment_status;
DELETE FROM pm_staff_data;
DELETE FROM pm_special_need;
DELETE FROM pm_person_absence;
DELETE FROM pm_person_evaluation;
DELETE FROM p_data_label_value_application;
DELETE FROM p_data_label_value_partner;
DELETE FROM pm_personal_data;
DELETE FROM pm_formal_education;
DELETE FROM pm_person_skill;
DELETE FROM pm_person_qualification;
DELETE FROM pm_person_ability;
DELETE FROM pm_past_experience;
DELETE FROM pm_person_language;
DELETE FROM pm_passport_details;
DELETE FROM pm_document;
DELETE FROM pm_year_program_application;
DELETE FROM pm_short_term_application;
DELETE FROM pm_application_status_history;
DELETE FROM pm_general_application;
DELETE FROM a_ar_invoice_detail_discount;
DELETE FROM a_ar_invoice_discount;
DELETE FROM a_ar_invoice_detail;
DELETE FROM a_ar_invoice;
DELETE FROM a_ep_document_payment;
DELETE FROM a_ep_payment;
DELETE FROM a_ap_document_payment;
DELETE FROM a_ap_payment;
DELETE FROM a_crdt_note_invoice_link;
DELETE FROM a_ap_document;
DELETE FROM a_ap_supplier;
DELETE FROM a_suspense_account;
DELETE FROM a_journal;
DELETE FROM a_recurring_journal;
DELETE FROM a_transaction_type;
DELETE FROM a_email_destination;
DELETE FROM a_account_hierarchy_detail;
DELETE FROM a_account_hierarchy;
DELETE FROM a_account_property;
DELETE FROM a_ep_statement;
DELETE FROM a_account;
DELETE FROM p_partner_contact_attribute;
DELETE FROM p_partner_contact;
DELETE FROM p_subscription;
DELETE FROM p_customised_greeting;
DELETE FROM m_extract;
DELETE FROM p_partner_ledger;
DELETE FROM p_partner_relationship;
DELETE FROM p_partner_type;
DELETE FROM p_partner_tax_deductible_pct;
DELETE FROM p_banking_details_usage;
DELETE FROM p_partner_banking_details;
DELETE FROM p_banking_details;
DELETE FROM p_venue;
DELETE FROM p_bank;
DELETE FROM p_organisation;
DELETE FROM p_church;
DELETE FROM p_person;
DELETE FROM p_family;
DELETE FROM um_unit_structure;
DELETE FROM p_unit;
DELETE FROM p_partner_attribute;
DELETE FROM p_partner_location;
DELETE FROM p_recent_partners;
DELETE FROM p_partner;
DELETE FROM p_first_contact;
DELETE FROM p_action;
DELETE FROM p_state;
DELETE FROM p_process;
DELETE FROM p_reminder_category;
DELETE FROM p_interest;
DELETE FROM p_interest_category;
DELETE FROM pc_room_attribute_type;
DELETE FROM pc_discount_criteria;
DELETE FROM pc_conference_option_type;
DELETE FROM pc_cost_type;
DELETE FROM pt_assignment_type;
DELETE FROM pt_position;
DELETE FROM pm_commitment_status;
DELETE FROM p_data_label_lookup;
DELETE FROM p_data_label_use;
DELETE FROM p_data_label;
DELETE FROM p_data_label_lookup_category;
DELETE FROM pt_driver_status;
DELETE FROM pt_skill_level;
DELETE FROM pt_skill_category;
DELETE FROM pt_qualification_level;
DELETE FROM pt_qualification_area;
DELETE FROM pt_ability_level;
DELETE FROM pt_ability_area;
DELETE FROM pt_language_level;
DELETE FROM pt_passport_type;
DELETE FROM pm_document_type;
DELETE FROM pm_document_category;
DELETE FROM pt_travel_type;
DELETE FROM pt_congress_code;
DELETE FROM pt_outreach_preference_level;
DELETE FROM pt_arrival_point;
DELETE FROM pt_leadership_rating;
DELETE FROM pt_special_applicant;
DELETE FROM pt_contact;
DELETE FROM pt_application_type;
DELETE FROM pt_applicant_status;
DELETE FROM a_ar_default_discount;
DELETE FROM a_ar_discount_per_category;
DELETE FROM a_ar_discount;
DELETE FROM a_ar_article_price;
DELETE FROM a_ar_article;
DELETE FROM a_ar_category;
DELETE FROM a_currency_language;
DELETE FROM a_system_interface;
DELETE FROM a_special_trans_type;
DELETE FROM a_batch;
DELETE FROM a_recurring_batch;
DELETE FROM a_motivation_group;
DELETE FROM a_method_of_payment;
DELETE FROM a_method_of_giving;
DELETE FROM a_freeform_analysis;
DELETE FROM a_form_element;
DELETE FROM a_form_element_type;
DELETE FROM a_form;
DELETE FROM p_email;
DELETE FROM a_daily_exchange_rate;
DELETE FROM a_corporate_exchange_rate;
DELETE FROM a_analysis_type;
DELETE FROM a_analysis_store_table;
DELETE FROM a_accounting_system_parameter;
DELETE FROM a_accounting_period;
DELETE FROM a_budget_revision;
DELETE FROM a_cost_centre_types;
DELETE FROM a_account_property_code;
DELETE FROM a_budget_type;
DELETE FROM a_ledger_init_flag;
DELETE FROM a_tax_table;
DELETE FROM a_ledger;
DELETE FROM a_tax_type;
DELETE FROM a_sub_system;
DELETE FROM p_method_of_contact;
DELETE FROM p_contact_attribute_detail;
DELETE FROM p_contact_attribute;
DELETE FROM p_reason_subscription_cancelled;
DELETE FROM p_reason_subscription_given;
DELETE FROM p_publication_cost;
DELETE FROM p_publication;
DELETE FROM p_postcode_region_range;
DELETE FROM p_postcode_region;
DELETE FROM p_postcode_range;
DELETE FROM p_merge_field;
DELETE FROM p_merge_form;
DELETE FROM p_label;
DELETE FROM p_formality;
DELETE FROM p_addressee_title_override;
DELETE FROM p_address_line;
DELETE FROM p_address_element;
DELETE FROM p_address_layout;
DELETE FROM p_address_layout_code;
DELETE FROM p_mailing;
DELETE FROM m_extract_parameter;
DELETE FROM m_extract_master;
DELETE FROM m_extract_type;
DELETE FROM p_relation;
DELETE FROM p_relation_category;
DELETE FROM p_type;
DELETE FROM p_type_category;
DELETE FROM p_banking_details_usage_type;
DELETE FROM p_banking_type;
DELETE FROM p_business;
DELETE FROM p_denomination;
DELETE FROM p_occupation;
DELETE FROM pt_marital_status;
DELETE FROM u_unit_type;
DELETE FROM p_partner_attribute_type;
DELETE FROM p_location_type;
DELETE FROM p_location;
DELETE FROM p_partner_classes;
DELETE FROM p_title;
DELETE FROM p_addressee_type;
DELETE FROM p_acquisition;
DELETE FROM p_partner_status;
DELETE FROM s_error_log;
DELETE FROM s_system_defaults;
DELETE FROM s_user_defaults;
DELETE FROM s_system_status;
DELETE FROM s_reports_to_archive;
DELETE FROM s_patch_log;
DELETE FROM s_logon_message;
DELETE FROM s_login;
DELETE FROM s_language_specific;
DELETE FROM s_user_table_access_permission;
DELETE FROM s_user_module_access_permission;
DELETE FROM s_group_table_access_permission;
DELETE FROM s_group_module_access_permission;
DELETE FROM s_valid_output_form;
DELETE FROM s_module;
DELETE FROM s_user_group;
DELETE FROM s_group;
DELETE FROM s_form;
DELETE FROM a_currency;
DELETE FROM p_country;
DELETE FROM p_international_postal_type;
DELETE FROM a_frequency;
DELETE FROM p_language;
DELETE FROM s_user;
