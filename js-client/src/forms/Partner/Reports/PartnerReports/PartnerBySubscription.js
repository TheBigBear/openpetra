// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       Timotheus Pokorra <timotheus.pokorra@solidcharity.com>
//
// Copyright 2019-2020 by SolidCharity.com
//
// This file is part of OpenPetra.
//
// OpenPetra is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.  If not, see <http://www.gnu.org/licenses/>.
//

var last_opened_entry_data = {};

function get_publications() {
	let x = {};
	api.post('serverMPartner.asmx/TPartnerSetupWebConnector_LoadPublications', x).then(function (data) {
		data = JSON.parse(data.data.d);
		publications = data.result.PPublication;
		for (publication of publications) {
			let y = $('<option value="'+publication.p_publication_code_c+'">'+publication.p_publication_code_c+'</option>');
			$('#PublicationCode').append(y);
		}
	})
}

$(function() {
	get_publications();
});

function calculate_report() {
	let obj = $('#reportfilter');
	// extract information from a jquery object
	let params = extract_data(obj);

	calculate_report_common("forms/Partner/Reports/PartnerReports/PartnerBySubscription.json", params);
}

function loadInConsents() {
	api.post('serverMPartner.asmx/TDataHistoryWebConnector_GetConsentChannelAndPurpose', {}).then(function (data) {
		var parsed = JSON.parse(data.data.d);
		var Consents = $(`#reportfilter [consents]`);
		for (var purpose of parsed.result.PConsentPurpose) {
			let name = i18next.t('MaintainPartners.'+purpose.p_name_c, purpose.p_name_c);
			var ConsentTemp = $(`[phantom] .consent-option`).clone();
			ConsentTemp.find(".name").text(name);
			ConsentTemp.find("[name=param_consent]").attr("value", purpose.p_purpose_code_c);
			Consents.append(ConsentTemp);
		}
	})
}

$("document").ready(function () {
	loadInConsents();
})
