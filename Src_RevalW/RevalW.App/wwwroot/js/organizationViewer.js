document.addEventListener('DOMContentLoaded', function (event) {
    $('.tag-checkbox').on('change', tagChecked);
});
function tagChecked() {
    const selectedTags = $('.tag-checkbox:checked').map(function () { return this.value; }).get();

    const joined = selectedTags.join(' ');

    $.ajax({
        url: '/Organization/OrganizationsByTags?tags=' + joined,
        method: 'GET',
        success: function (response) {
            $.ajax({
                url: '/Home/OrganizationsMap',
                method: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify(response),
                success: function (html) {
                    $('#mapContainer').html(html);
                },
                error: function (xhr) {
                    console.error('Error rendering map:', xhr);
                }
            });
        },
        error: function (xhr) {
            console.error('Error:', xhr);
        }
    });
}