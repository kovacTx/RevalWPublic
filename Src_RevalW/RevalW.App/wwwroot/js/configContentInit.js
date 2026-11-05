const configContentMonacoArea = new MonacoArea();

document.addEventListener('DOMContentLoaded', function (event) {
    configContentMonacoArea.getPayload = () => {
        return {
            id: Number($('#Id').val()) || 0,
            configId: Number($('#Id').val()) || 0,
            containerId: 'contentContainer',
            content: configContentMonacoArea.monacoEditor.getValue()
        };
    };
    configContentMonacoArea.isAutoSaveable = (payLoad) => {
        if (!(payLoad.id > 0)) return false;
        return true;
    };
    configContentMonacoArea.autosaveUrl = '/Configs/Autosave';
    configContentMonacoArea.hiddenTextareaJquery = $('#ConfigContentHidden');
    configContentMonacoArea.monacoContainerJquery = $('#monaco-ConfigContent');
    configContentMonacoArea.resizerJquery = $('#monaco-ConfigContent-resizer');
    configContentMonacoArea.onDomReady();

    const form = document.getElementById('UpsertForm');
    if (!form) return;

    form.addEventListener('submit', () => {
        if (configContentMonacoArea.monacoEditor) configContentMonacoArea.hiddenTextareaJquery.val(configContentMonacoArea.monacoEditor.getValue());
    });
});