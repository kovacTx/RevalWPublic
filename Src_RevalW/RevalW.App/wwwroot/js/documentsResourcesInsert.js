const fileInserter = new ValueInserter();
const fileInputs = [];

document.addEventListener('DOMContentLoaded', function (event) {
    $('#DocumentsResourcesSelector').on('change', addFile);
    $('#UpsertForm').on('submit', addTag);

    fileInserter.clearValueFunc = () => $('#DocumentResourcesSelector').val('');
    fileInserter.isEmpty = (value) => !value.FileName;
    fileInserter.compareTo = (value1, value2) => value1.FileKey == value2.FileKey;
    fileInserter.buttonLabelGetterFunc = (value) => value.FileName;
    fileInserter.createNewValueElementFunc = (value) => fileInserter.createRemovableNewValueElement(value);
    fileInserter.onRemoveValueFunc = onValueRemove;
    fileInserter.$valuesJsonHolder = $('#OldDocumentsResourcesJson');
    fileInserter.$valueElementsContainer = $('#DocumentsResourcesContainer');
    fileInserter.init();
});
function addFile(event) {
    const files = event.target.files;
    if (!files || files.length === 0) return;
    const file = getNewFile(files[0]);
    if (fileInserter.addValue(file)) {
        fileInputs.push(createHiddenInputForFile(file));
    }
    $('#DocumentsResourcesSelector').val('');
}

function onValueRemove(value) {
    if (!value || !value.FileKey) return;

    const idx = fileInputs.findIndex(inp => inp && inp.dataset && inp.dataset.FileKey === value.FileKey);
    if (idx === -1) return;
    const inputElem = fileInputs[idx];
    try {
        if (inputElem && inputElem.parentNode) inputElem.parentNode.removeChild(inputElem);
    } catch (e) {
        console.warn('Failed to remove input element', e);
    }
    fileInputs.splice(idx, 1);
}
function createHiddenInputForFile(file) {
    const input = document.createElement('input');
    input.type = 'file';
    input.name = 'NewDocumentsResources';
    input.dataset.FileKey = file.FileKey;
    input.style.display = 'none';
    // create DataTransfer to set FileList
    const dt = new DataTransfer();
    dt.items.add(file.WholeFile);
    input.files = dt.files;
    // append to form (selector.closest('form'))
    const form = $('#DocumentsResourcesSelector').closest('form');
    if (form && form.length) form[0].appendChild(input);
    else document.body.appendChild(input);
    return input;
}
function getNextIndex(items) {
    if (!Array.isArray(items) || items.length === 0) return 0;
    let max = -1;
    for (const it of items) {
        const idx = Number(it?.FileIndex);
        if (Number.isFinite(idx) && idx > max) max = idx;
    }
    return max + 1;
}
function getNewFile(file) {
    var values = [];
    if (fileInserter.$valuesJsonHolder.val()) values = JSON.parse(fileInserter.$valuesJsonHolder.val());
    return {
        FileName: file.name,
        FileKey: `${file.name}::${file.size}::${file.lastModified ?? 0}`,
        FileIndex: getNextIndex(values),
        WholeFile: file
    };
}