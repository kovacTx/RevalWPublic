class MainGridColumnsInserter {
    columnSelectJquery;
    $dynamicPropertiesJsonHolder;
    $form;
    columnSelectedOptionValueFetcher;
    mainGridColumnsJquery;
    columnsContainerJquery;
    clearValueFunc;
    addColumnBtnJquery;

    mainGridColumnInserter = new ValueInserter();

    init() {
        this.addColumnBtnJquery.on('click', this.addColumn.bind(this));
        this.$dynamicPropertiesJsonHolder.on('change', this.dynamicPropertiesChanged.bind(this));
        this.$form.on('submit', this.addColumn.bind(this));

        this.mainGridColumnInserter.clearValueFunc = this.clearValueFunc;
        this.mainGridColumnInserter.isEmpty = (value) => !value;
        this.mainGridColumnInserter.compareTo = (value1, value2) => value1.Name == value2.Name;
        this.mainGridColumnInserter.buttonLabelGetterFunc = (value) => value.Label;
        this.mainGridColumnInserter.createNewValueElementFunc = (value) => this.mainGridColumnInserter.createRemovableNewValueElement(value);
        this.mainGridColumnInserter.$valuesJsonHolder = this.mainGridColumnsJquery;
        this.mainGridColumnInserter.$valueElementsContainer = this.columnsContainerJquery;
        this.mainGridColumnInserter.init();
        this.dynamicPropertiesChanged();
    };
    dynamicPropertiesChanged() {
        var values = this.$dynamicPropertiesJsonHolder.val() || '';
        var objects = values ? JSON.parse(values) : [];
        const availableColumns = objects.map(dp => {
            return { Name: dp.Name, Label: dp.Label, Prefix: dp.Prefix };
        });
        this.columnSelectJquery.empty();
        this.columnSelectJquery.append($('<option>').val('').prop('selected', true).prop('disabled', true).text('-- Select a value --'));
        availableColumns.forEach(device => {
            const option = $('<option>')
                .text(device.Label)
                .attr('data-value', JSON.stringify(device));
            this.columnSelectJquery.append(option);
        });
        var alreadySelected = [];
        if (this.mainGridColumnsJquery.val()) alreadySelected = JSON.parse(this.mainGridColumnsJquery.val());
        const missingIndexes = alreadySelected
            .map((a, i) => ({ item: a, index: i }))
            .filter(({ item }) => !availableColumns.some(ac => ac.Name == item.Name))
            .map(({ index }) => index);
        missingIndexes.forEach(i => this.mainGridColumnInserter.removeValueAtIndex(i));
    
    }
    addColumn(event) {
        this.mainGridColumnInserter.addValue(this.columnSelectedOptionValueFetcher());
    }
}