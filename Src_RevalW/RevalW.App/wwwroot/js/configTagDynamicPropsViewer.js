class ConfigTagDynamicPropsViewer {
    ajaxAddress;
    readonly = false;
    $propertiesWrapper;
    $propertiesGroupsContainer;
    $propertiesDefinition;
    $baseValues;
    $initValues;
    baseValues;
    initValues;

    init() {
        const definitions = JSON.parse(this.$propertiesDefinition.val());
        if (!definitions) return;
        definitions?.forEach((def) => {
            this.onSuccessNewTagPropsFetch(def.DynamicProperties, def.TagId, def.TagName);
        });
    }
    clear() {
        this.$propertiesGroupsContainer.empty();
    }
    onTagRemove(removed) {
        if (!removed) return;
        const tagId = (removed.Id ?? removed.id ?? '').toString();
        if (!tagId) return;

        $(`[data-tag-id="${tagId}"]`).remove();

        // hide wrapper if no client props left
        if (this.$propertiesGroupsContainer.children().length === 0) {
            this.$propertiesWrapper.hide();
        }
    }
    async addTag(dataId, tagName) {
        if (!dataId) return; // no tag selected - nothing to fetch

        try {
            const dtoList = await new Promise((resolve, reject) => {
                $.getJSON(this.ajaxAddress, { id: dataId })
                    .done(resolve)
                    .fail((xhr, status, err) => reject(err));
            });

            this.onSuccessNewTagPropsFetch(dtoList, dataId, tagName);
        } catch (err) {
            console.error('Failed to load new item properties:', err);
        }
    }
    onSuccessNewTagPropsFetch(dtoList, tagId, tagName) {
        // dtoList expected to be an array
        this.renderProperties(dtoList, tagId, tagName);
        this.$propertiesWrapper.show();
    }
    // Renders DynamicPropertyDto[] to $propertiesGroupsContainer
    renderProperties(dtoList, tagId, tagName) {
        // create a per-tag group wrapper so multiple tags' properties are visible
        const $group = $('<div>').addClass('tag-props-group border form-group p-2 mb-2').attr('data-tag-id', tagId);

        if (!Array.isArray(dtoList) || dtoList.length === 0) {
            $group.append($('<h6>').text(`${tagName} (no properties)`));
            this.$propertiesGroupsContainer.append($group);
            return;
        }

        // header with tag name and optional small remove hint
        const $header = $('<div>').addClass('d-flex justify-content-between align-items-center mb-2');
        $header.append($('<h6>').addClass('mb-0').text(tagName));
        $group.append($header);

        this.baseValues = this.parseJsonSafe(this.$baseValues?.val());
        this.initValues = {
            ...this.baseValues,
            ...this.parseJsonSafe(this.$initValues?.val())
        };
        console.log(this.initValues)
        const registeredNames = [];

        dtoList.forEach((raw) => {
            // support PascalCase or camelCase payloads
            const dp = {
                Name: raw.Name || raw.name,
                Label: raw.Label || raw.label,
                InputType: raw.InputType || raw.inputType,
                Required: raw.Required ?? raw.required,
                Readonly: raw.Readonly ?? raw.readonly,
                ComboOptions: raw.ComboOptions || raw.comboOptions,
                Default: raw.Default || raw.default,
                Max: raw.Max ?? raw.max,
                Min: raw.Min ?? raw.min,
                Step: raw.Step ?? raw.step
            };

            const $formGroup = $('<div>').addClass('form-group');
            const $label = $('<label>').addClass('control-label').attr('for', dp.Name).text(dp.Label || dp.Name);
            $formGroup.append($label);

            const isRequired = dp.Required === true || dp.Required === 'True' || dp.Required === 'true';
            const isReadonly = dp.Readonly === true || dp.Readonly === 'True' || dp.Readonly === 'true';
            const propName = tagId + '.' + dp.Name;
            if ((dp.InputType || '').toString().toLowerCase() === 'combobox' || (dp.InputType || '').toString().toLowerCase() === 'comboBox'.toLowerCase()) {
                const $select = $('<select>')
                    .addClass('form-control dynamic-property')
                    .attr('data-property-name', propName)
                    .attr('data-property-label', dp.Label || dp.Name);

                if (isRequired) $select.attr('required', 'required');
                if (isReadonly) $select.attr('readonly', 'readonly');

                $select.append($('<option>').attr('value', '').attr('selected', 'selected').text('-- Select --'));

                const optionsRaw = (dp.ComboOptions || '') + '';
                const options = optionsRaw ? optionsRaw.split(',') : [];
                options.forEach(function (optRaw) {
                    const opt = optRaw?.trim() ?? '';
                    const $opt = $('<option>').val(opt).text(opt);
                    if (opt && (opt === (dp.Default ?? ''))) {
                        $opt.prop('selected', true);
                        if (this.readonly) $select.attr('readonly', 'readonly');
                    }
                    $select.append($opt);
                });

                $formGroup.append($select);
            } else {
                const type = dp.InputType || 'text';
                const $input = $('<input>')
                    .addClass('form-control dynamic-property')
                    .attr('data-property-name', propName)
                    .attr('data-property-label', dp.Label || dp.Name)
                    .attr('type', type);

                if (dp.Max !== undefined && dp.Max !== null) $input.attr('max', dp.Max);
                if (dp.Min !== undefined && dp.Min !== null) $input.attr('min', dp.Min);
                if (dp.Step !== undefined && dp.Step !== null) $input.attr('step', dp.Step);
                if (dp.Default !== undefined && dp.Default !== null) $input.val(dp.Default);
                if (isRequired) $input.attr('required', 'required');
                if (isReadonly) $input.attr('readonly', 'readonly');

                const value = this.initValues[propName];
                if (value !== undefined) $input.val(value);

                if (this.baseValues[propName] && this.readonly) $input.attr('readonly', 'readonly');
                
                $formGroup.append($input);
            }

            $formGroup.append($('<span>').attr('id', `${dp.Name}-error`).addClass('text-danger'));
            $group.append($formGroup);

            registeredNames.push(dp.Name);
        });

        // append this tag's group to the main container (do not clear existing groups)
        this.$propertiesGroupsContainer.append($group);
    }
    parseJsonSafe = (val) => {
        if (typeof val !== 'string') return {};
        const trimmed = val.trim();
        return trimmed ? JSON.parse(trimmed) : {};
    };
}