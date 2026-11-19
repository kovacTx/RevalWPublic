const docContentMonacoArea = new MonacoArea();

document.addEventListener('DOMContentLoaded', function (event) {
    window.docContentMonacoArea = docContentMonacoArea;
    docContentMonacoArea.getPayload = () => {
        return {
            id: Number($('#DocumentId').val()) || 0,
            configId: Number($('#Id').val()) || 0,
            containerId: 'documentContainer',
            content: docContentMonacoArea.monacoEditor.getValue()
        };
    };
    docContentMonacoArea.isAutoSaveable = (payLoad) => {
        if (!(payLoad.id > 0)) return false;
        if (!(payLoad.configId > 0)) return false;
        return true;
    };
    docContentMonacoArea.autosaveUrl = '/Configs/Autosave';
    docContentMonacoArea.hiddenTextareaJquery = $('#DocumentTemplateHidden');
    docContentMonacoArea.monacoContainerJquery = $('#monaco-DocumentContent');
    docContentMonacoArea.resizerJquery = $('#monaco-DocumentContent-resizer');
    docContentMonacoArea.onDomReady();

    const form = document.getElementById('UpsertForm');
    if (!form) return;

    form.addEventListener('submit', () => {
        if (docContentMonacoArea.monacoEditor) docContentMonacoArea.hiddenTextareaJquery.val(docContentMonacoArea.monacoEditor.getValue());
    });
});

function getPrintableDocumentIndexFromModal() {
    return $('#PrintableDocumentModal').data('edit-index');
}