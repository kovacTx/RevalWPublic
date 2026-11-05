class ValueInserter {
    isEmpty;
    compareTo;
    buttonLabelGetterFunc;
    clearValueFunc;
    onOpenValueFunc;
    onRemoveValueFunc;
    onRefreshValueFunc;
    createNewValueElementFunc;
    $valuesJsonHolder;
    $valueElementsContainer;

    init() {
        this.$valueElementsContainer.on('click', '.open-button', this.openValueHandler.bind(this)); // open
        this.$valueElementsContainer.on('click', '.remove-button', this.removeValueHandler.bind(this)); // Handle "×" click
        this.$valueElementsContainer.on('click', '.refresh-button', this.refreshValueHandler.bind(this)); // Handle refresh click
        this.redrawValues();
    }
    clear() {
        this.$valuesJsonHolder.val('');
        this.$valueElementsContainer.empty();
    }
    redrawValues() {
        var values = [];
        if (this.$valuesJsonHolder.val()) values = JSON.parse(this.$valuesJsonHolder.val());
        for (const val of values) {
            this.createAndAddNewValueElement(val);
        }
    }
    addValue(value) {
        if (this.isEmpty(value)) return false;
        var values = [];
        if (this.$valuesJsonHolder.val()) values = JSON.parse(this.$valuesJsonHolder.val());

        for (const val of values) {
            if (this.compareTo(val, value)) return false;
        }
        values.push(value);
        this.$valuesJsonHolder.val(JSON.stringify(values)).trigger('change');

        this.createAndAddNewValueElement(value);
        this.clearValueFunc?.();
        return true;
    }
    updateValue(editIndex, newValue) {
        let objects = [];
        if (this.$valuesJsonHolder.val()) objects = JSON.parse(this.$valuesJsonHolder.val());
        objects.splice(editIndex, 1, newValue);

        this.$valuesJsonHolder.val(JSON.stringify(objects)).trigger('change');
        // Update the button's label
        this.$valueElementsContainer.children().eq(editIndex).find('.open-button').text(this.buttonLabelGetterFunc(newValue));
        return true;
    }
    removeValueAtIndex(index) {
        if (index < 0) return;

        // Remove from Json
        let objects = [];
        if (this.$valuesJsonHolder.val()) objects = JSON.parse(this.$valuesJsonHolder.val());

        const removed = objects.length ? objects[index] : null;
        objects.splice(index, 1);

        this.$valuesJsonHolder.val(JSON.stringify(objects)).trigger('change');
        this.$valueElementsContainer.children().eq(index).remove();
        this.$valueElementsContainer.children().each(function (i, row) {
            $(row).data('edit-index', i);
        });
        if (this.onRemoveValueFunc) this.onRemoveValueFunc(removed);
    }

    openValueHandler(e) {
        //if (!e.target.classList.contains('open-button')) return;
        if (!this.onOpenValueFunc) return;

        const row = e.target.closest('.input-row');
        const index = $(row).data('edit-index');
        if (index < 0) return;
        var values = [];
        if (this.$valuesJsonHolder.val()) values = JSON.parse(this.$valuesJsonHolder.val());
        if (!this.onOpenValueFunc) return;
        const openFunc = this.onOpenValueFunc.bind(e.target)
        openFunc(index, values.length ? values[index] : null);
    }
    removeValueHandler(e) {
        //if (!e.target.classList.contains('remove-button')) return;

        const row = e.target.closest('.input-row');
        const index = $(row).data('edit-index');
        this.removeValueAtIndex(index);
    }
    refreshValueHandler(e) {
        //if (!e.target.classList.contains('refresh-button')) return; // when pressing the inner <i the handler was not firing correctly
        if (!this.onRefreshValueFunc) return;
        const row = e.target.closest('.input-row');
        const index = $(row).data('edit-index');
        if (index < 0) return;
        var values = [];
        if (this.$valuesJsonHolder.val()) values = JSON.parse(this.$valuesJsonHolder.val());
        if (this.onRefreshValueFunc) this.onRefreshValueFunc(index, values.length ? values[index] : null, e.target);
    }
    createAndAddNewValueElement(value) {
        const element = this.createNewValueElementFunc?.(value);
        if (!element) return;
        this.$valueElementsContainer.append(element);
        const editIndex = $(element).index();
        $(element).data('edit-index', editIndex);
    }
    createRemovableNewValueElement(value) {
        const newParent = document.createElement('div');
        newParent.className = 'd-flex align-items-center input-row';

        console.log(value)
        // Create a button for the new value
        const editButton = document.createElement('button');
        editButton.type = 'button';
        editButton.className = 'btn btn-primary open-button rounded-start-5 rounded-end-0';
        editButton.textContent = this.buttonLabelGetterFunc(value);
        if (!this.onOpenValueFunc) editButton.disabled = true;

        const removeBtn = document.createElement('button');
        removeBtn.type = 'button';
        removeBtn.className = 'btn btn-danger remove-button rounded-start-0 rounded-end-5';
        removeBtn.textContent = '×';

        newParent.appendChild(editButton);
        newParent.appendChild(removeBtn);

        return newParent;
    }
    createRefreshableNewValueA(value, url) {
        console.log(value)
        console.log(url)
        const newParent = document.createElement("div");
        newParent.className = "d-flex align-items-center input-row";

        const link = document.createElement("a");
        link.className = "btn btn-primary open-button rounded-start-5 rounded-end-0";
        link.target = "_blank";
        link.href = url;
        link.textContent = this.buttonLabelGetterFunc(value);

        const refreshBtn = document.createElement("button");
        refreshBtn.type = "button";
        refreshBtn.className = "btn btn-info refresh-button rounded-start-0 rounded-end-5";
        refreshBtn.innerHTML = '<i class="bi bi-arrow-counterclockwise"></i>';

        newParent.appendChild(link);
        newParent.appendChild(refreshBtn);

        return {
            parent: newParent,
            mainBtn: link,
            refreshBtn: refreshBtn
        };
    }
    createNewValueElement(value) {
        const newParent = document.createElement('div');
        newParent.className = 'd-flex align-items-center input-row';

        // Create a button for the new value
        const editButton = document.createElement('button');
        editButton.type = 'button';
        editButton.className = 'btn btn-primary open-button rounded-start-5 rounded-end-5';
        editButton.textContent = this.buttonLabelGetterFunc(value);
        if (!this.onOpenValueFunc) editButton.disabled = true;

        newParent.appendChild(editButton);

        return newParent;
    }
}