// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       Timotheus Pokorra <timotheus.pokorra@solidcharity.com>
//       Christopher Jäkel <cj@tbits.net>
//
// Copyright 2017-2018 by TBits.net
// Copyright 2020-2021 by SolidCharity.com
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

$('document').ready(function () {
  LoadTree();
})

function LoadTree() {
  let x = {
    ALedgerNumber: window.localStorage.getItem('current_ledger')
  };
  api.post('serverMFinance.asmx/TGLSetupWebConnector_LoadCostCentreHierarchyHtmlCode', x).then(function (data) {
    _html_ = data.data.d;
    $('#browse_container').html(_html_);
  })
}

function import_file(file_field) {
  let self = $(file_field);
  var filename = self.val();

	// see http://www.html5rocks.com/en/tutorials/file/dndfiles/
	if (window.File && window.FileReader && window.FileList && window.Blob) {
		//alert("Great success! All the File APIs are supported.");
	} else {
	  alert('The File APIs are not fully supported in this browser.');
	}

	var reader = new FileReader();

	reader.onload = (function(theFile) {
		return function(e) {
			s = e.target.result;

			p = {AYmlHierarchy: s,
				ALedgerNumber: window.localStorage.getItem('current_ledger')};

			api.post('serverMFinance.asmx/TGLSetupWebConnector_ImportCostCentreHierarchy', p)
			.then(function (result) {
				result = result.data;
				if (result != '') {
					var parsed = JSON.parse(result.d);
					if (parsed.result == true) {
						display_message(i18next.t('CostCenterTree.uploadsuccess'), "success");
						LoadTree();
					} else {
						display_error(parsed.VerificationResult);
					}
				}
			})
			.catch(error => {
				display_message(i18next.t('CostCenterTree.uploaderror'), "fail");
			});
		};
	})(self[0].files[0]);

	// Read in the file as a data URL.
	reader.readAsText(self[0].files[0]);
};

function export_file() {
  let x = {
    ALedgerNumber: window.localStorage.getItem('current_ledger')
  };
  api.post('serverMFinance.asmx/TGLSetupWebConnector_ExportCostCentreHierarchyYml', x).then(function (data) {
    var parsed = JSON.parse(data.data.d);
    var _file_ = b64DecodeUnicode(parsed.AHierarchyYml);
    var link = document.createElement("a");
    link.style = "display: none";
    link.href = 'data:text/plain;charset=utf-8,'+encodeURIComponent(_file_);
    link.download = i18next.t('CostCenterTree.costcentres_file') + '.yml';
    document.body.appendChild(link);
    link.click();
    link.remove();
  })

}
