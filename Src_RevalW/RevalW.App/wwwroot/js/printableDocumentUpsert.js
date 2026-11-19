const printableDocumentInserter = new ValueInserter();

document.addEventListener('DOMContentLoaded', function (event) {
    $('#AddPrintableDocument').on('click', addPrintableDocumentBtn);
    $('#PrintableDocumentAddBtn').on('click', wantAddPrintableDocument);
    $('#PrintableDocumentModal').on('shown.bs.modal', layoutMonaco);

    printableDocumentInserter.onOpenValueFunc = editPrintableDocument;
    printableDocumentInserter.isEmpty = (value) => !value.Label;
    printableDocumentInserter.compareTo = (value1, value2) => value1.Label == value2.Label;
    printableDocumentInserter.buttonLabelGetterFunc = (value) => value.Label;
    printableDocumentInserter.clearValueFunc = () => setPrintableDocument({});
    printableDocumentInserter.createNewValueElementFunc = (value) => printableDocumentInserter.createRemovableNewValueElement(value);
    printableDocumentInserter.$valuesJsonHolder = $('#PrintableDocumentsJson');
    printableDocumentInserter.$valueElementsContainer = $('#PrintableDocumentsContainer');
    printableDocumentInserter.init();
});
function wantAddPrintableDocument() {
    setPrintableDocumentIndexInModal(-1);
    setPrintableDocument({
        ConfigId: $('#Id').val()
    });
}
function editPrintableDocument(editIndex, printableDocument) {
    setPrintableDocumentIndexInModal(editIndex);
    setPrintableDocument(printableDocument);

    $('#DocumentName-error').text('');
    $('#Name-error').text('');
    $('#DocumentTemplate-error').text('');
    $('#PrintableDocumentModal').modal('show');
}
function addPrintableDocumentBtn(e) {
    var value = getPrintableDocument();
    if (getDocumentError(value)) return;

    var editIndex = getPrintableDocumentIndexFromModal();
    if (editIndex == -1) {
        if (getDuplicatedNameError()) return;
        if (!printableDocumentInserter.addValue(value)) {
            $('#Name-error').text('could not add document');
            return;
        }
    }
    else printableDocumentInserter.updateValue(editIndex, value);

    // Close the modal
    $('button[data-bs-target="#PrintableDocumentModal"]').focus();
    $('#PrintableDocumentModal').modal('hide');
}
function getDocumentError(value) {
    var err = false;
    $('#DocumentName-error').text('');
    $('#Name-error').text('');
    $('#DocumentTemplate-error').text('');

    if (!value.Label) {
        $('#DocumentName-error').text('empty Label?');
        err = true;
    }
    if (!value.Template) {
        $('#DocumentTemplate-error').text('empty Template?');
        err = true;
    }
    return err;
}
function getDuplicatedNameError() {
    const jsonElement = $('#PrintableDocumentsJson');
    var objects = [];
    if (jsonElement.val()) objects = JSON.parse(jsonElement.val());
    var names = objects.map(i => i.Label);
    names.push($('#DocumentLabel').val());
    if (names.length !== new Set(names).size) {
        $('#Name-error').text('There is already document with the same name.');
        return true;
    }
    $('#Name-error').text('');
    return false;
}
function getPrintableDocument() {
    return {
        Id: Number($('#DocumentId').val()) || 0,
        ConfigId: Number($('#Id').val()) || 0,
        Label: $('#DocumentLabel').val(),
        Enabled: $('#DocumentEnabled').val(),
        AlwaysTrue: String($('#AlwaysTrue').prop('checked')),
        RestParams: $('#DocumentRestParams').val(),
        Template: $('#DocumentTemplateHidden').val()
    };
}
function setPrintableDocument(value) {
    $('#DocumentId').val(value.Id);
    $('#ConfigId').val(value.ConfigId);
    $('#DocumentLabel').val(value.Label);
    $('#DocumentEnabled').val(value.Enabled);
    $('#AlwaysTrue').prop('checked', String(value.AlwaysTrue ?? '').toLowerCase().startsWith('t'));
    $('#DocumentRestParams').val(value.RestParams);
    $('#DocumentTemplateHidden').val(value.Template);

    try {
        const monacoInst = window.docContentMonacoArea;
        if (monacoInst && monacoInst.monacoEditor) {
            monacoInst.monacoEditor.setValue(value.Template || '');
        }
    } catch (e) { /* ignore */ }
}
function layoutMonaco() {
    try {
        const monacoInst = window.docContentMonacoArea;
        if (monacoInst && monacoInst.monacoEditor) {
            monacoInst.monacoEditor.layout();
        }
    } catch (e) { /* ignore */ }
}
function getPrintableDocumentIndexFromModal() {
    return $('#PrintableDocumentModal').data('edit-index');
}
function setPrintableDocumentIndexInModal(index) {
    $('#PrintableDocumentModal').data('edit-index', index);
}