class DynamicPropertyInserter {
    $dynamicPropertyInsertBtn;
    $valuesJsonHolder;
    $valueElementsContainer;
    $addDynamicPropertyBtn;
    $cancelAddDynamicPropertyBtn;
    isOpened;

    init() {
        // Bind instance methods so "this" inside handlers refers to the class instance
        this.$addDynamicPropertyBtn.on('click', this.comitDynamicProperty.bind(this));
        this.$dynamicPropertyInsertBtn.on('click', this.wantAddDynamicProperty.bind(this));
        this.$cancelAddDynamicPropertyBtn.on('click', this.dontAddDynamicProperty.bind(this));

        // Use arrow functions so "this" is lexical (the class instance)
        $('#PropInputType').on('change', (e) => {
            $('#PropDefault').val('');
            this.toggleConditionalFields();
        });
        $('#PropInputType').on('setDynamicProperty', (e) => {
            this.toggleConditionalFields();
        }); // Event listener for input type change

        this.toggleConditionalFields(); // Initialize visibility on page load

        this.valueInserter = new ValueInserter();
        this.valueInserter.onOpenValueFunc = this.editDynamicProperty.bind(this);
        this.valueInserter.isEmpty = (value) => !value.Name;
        this.valueInserter.compareTo = (value1, value2) => value1.Name == value2.Name;
        this.valueInserter.buttonLabelGetterFunc = (value) => value.Label;
        this.valueInserter.clearValueFunc = () => this.setDynamicProperty({});
        this.valueInserter.createNewValueElementFunc = (value) => this.valueInserter.createRemovableNewValueElement(value);
        this.valueInserter.$valuesJsonHolder = this.$valuesJsonHolder;
        this.valueInserter.$valueElementsContainer = this.$valueElementsContainer;
        this.valueInserter.init();
    }

    toggleConditionalFields() {
        var inputType = $('#PropInputType').val();

        // Hide all conditional fields
        $('.number-dependent').hide();
        $('.combobox-dependent').hide();

        const propDefault = $('#PropDefault');
        // Show fields based on selected input type
        if (inputType === 'number') {
            $('.number-dependent').show();
            propDefault.attr('type', 'number');
            propDefault.addClass('form-control');
        } else if (inputType === 'comboBox') {
            $('.combobox-dependent').show();
            propDefault.attr('type', 'text');
            propDefault.addClass('form-control');
        } else if (inputType === 'checkbox') {
            $('.radio-dependent').show();
            propDefault.attr('type', 'checkbox');
            propDefault.attr('value', 'True');
            propDefault.removeClass('form-control');
        } else {
            propDefault.attr('type', 'text');
            propDefault.addClass('form-control');
        }
    }
    wantAddDynamicProperty() {
        this.isOpened = true;
        this.setDynamicPropertyIndexInModal(-1);
        this.setDynamicProperty({});
        $('#DynamicPropertyModal').modal('show');
    }
    dontAddDynamicProperty() {
        this.isOpened = false;
    }
    editDynamicProperty(editIndex, dynamicProperty) {
        this.setDynamicPropertyIndexInModal(editIndex);
        this.setDynamicProperty(dynamicProperty);
        this.toggleConditionalFields();

        $('#Name-error').text('');
        this.isOpened = true;
        $('#DynamicPropertyModal').modal('show');
    }
    comitDynamicProperty() {
        if (!this.isOpened) return;

        $('#Name-error').text('');

        var editIndex = this.getDynamicPropertyIndexFromModal();
        if (editIndex == -1) {
            var value = this.getDynamicProperty();
            if (!value.Name) {
                $('#Name-error').text('empty Name?');
                return;
            }
            //if (getNameError()) return;
            if (this.getDuplicatedNameError()) return;
            if (!this.valueInserter.addValue(value)) {
                $('#Name-error').text('could not add property');
                return;
            }
        }
        else this.valueInserter.updateValue(editIndex, this.getDynamicProperty());

        // Close the modal
        this.isOpened = false;
        $('#DynamicPropertyModal').modal('hide');
    }
    getDuplicatedNameError() {
        var objects = [];
        if (this.$valuesJsonHolder && this.$valuesJsonHolder.val()) objects = JSON.parse(this.$valuesJsonHolder.val());
        var names = objects.map(i => i.Name);
        names.push($('#PropName').val());
        if (names.length !== new Set(names).size) {
            $('#Name-error').text('There is already property with the same name.');
            return true;
        }
        $('#Name-error').text('');
        return false;
    }
    getDynamicProperty() {
        return {
            Name: $('#PropName').val().replace(/[\^\-\+\*\/\\\(\)\[\]\{\}\<\>\|\&\!\?\°\'\:;\,\.\s]/g, ''),
            Label: $('#PropLabel').val() || $('#PropName').val(),
            Required: $('#PropRequired').is(':checked'),
            Readonly: $('#PropReadonly').is(':checked'),
            InputType: $('#PropInputType').val(),
            Min: $('#PropMin').val(),
            Max: $('#PropMax').val(),
            Step: $('#PropStep').val(),
            ComboOptions: $('#PropComboOptions').val(),
            Default: $('#PropDefault').val()
        };
    }
    setDynamicProperty(prop) {
        $('#PropName').val(prop.Name);
        $('#PropLabel').val(prop.Label);
        $('#PropRequired').prop('checked', prop.Required === true || prop.Required === 'true');
        $('#PropReadonly').prop('checked', prop.Readonly === true || prop.Readonly === 'true');
        if (prop.InputType) $('#PropInputType').val(prop.InputType).trigger('setDynamicProperty');
        else $('#PropInputType').val($('#PropInputType option[selected]').val()).trigger('setDynamicProperty');
        $('#PropMin').val(prop.Min);
        $('#PropMax').val(prop.Max);
        $('#PropStep').val(prop.Step);
        $('#PropComboOptions').val(prop.ComboOptions);
        $('#PropDefault').val(prop.Default);
    }
    getDynamicPropertyIndexFromModal() {
        return $('#DynamicPropertyModal').data('edit-index');
    }
    setDynamicPropertyIndexInModal(index) {
        $('#DynamicPropertyModal').data('edit-index', index);
    }
}