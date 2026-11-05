const tagInserter = new ValueInserter();

document.addEventListener('DOMContentLoaded', function (event) {
    $('#AddTagBtn').on('click', addTag);
    $('#OrganizationUpdateForm').on('submit', addTag);

    tagInserter.clearValueFunc = () => $('#Tag').val('');
    tagInserter.isEmpty = (value) => !value;
    tagInserter.compareTo = (value1, value2) => value1 == value2;
    tagInserter.buttonLabelGetterFunc = (value) => value;
    tagInserter.createNewValueElementFunc = (value) => tagInserter.createRemovableNewValueElement(value);
    tagInserter.$valuesJsonHolder = $('#TagsJson');
    tagInserter.$valueElementsContainer = $('#TagsContainer');
    tagInserter.init();
});
function addTag() {
    const tagInput = $('#TagInput').val();
    tagInserter.addValue(tagInput);
    
    $('#TagInput').val('');
}