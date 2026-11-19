class RoleInserter {
    $roleInsertBtn;
    $valuesJsonHolder;
    $valueElementsContainer;
    $addRoleBtn;
    $cancelAddRoleBtn;
    $modalBody;
    allPermissions;
    isOpened;

    init() {
        // Dynamically build permission checkboxes from allPermissions (key = input id, value = label)
        try {
            const container = this.$modalBody.find('#RolePermissionsContainer');
            container.empty();
            if (this.allPermissions) {
                Object.entries(this.allPermissions).forEach(([id, label]) => {
                    const grp = $('<div class="form-group"></div>');
                    const checkbox = $(`<input type="checkbox" id="${id}">`);
                    const lab = $(`<label class="control-label" for="${id}"></label>`);
                    lab.prepend(checkbox).append('&nbsp;' + label);
                    grp.append(lab);
                    container.append(grp);
                });
            }
        } catch (e) {
            console.error('Failed to render role permissions dynamically', e);
        }

        // Bind instance methods so "this" inside handlers refers to the class instance
        this.$addRoleBtn.on('click', this.comitRole.bind(this));
        this.$roleInsertBtn.on('click', this.wantAddRole.bind(this));
        this.$cancelAddRoleBtn.on('click', this.dontAddRole.bind(this));

        this.valueInserter = new ValueInserter();
        this.valueInserter.onOpenValueFunc = this.editRole.bind(this);
        this.valueInserter.isEmpty = (value) => !value.Name;
        this.valueInserter.compareTo = (value1, value2) => value1.Name == value2.Name;
        this.valueInserter.buttonLabelGetterFunc = (value) => value.Name;
        this.valueInserter.clearValueFunc = () => this.setRole({});
        this.valueInserter.createNewValueElementFunc = (value) => this.valueInserter.createRemovableNewValueElement(value);
        this.valueInserter.$valuesJsonHolder = this.$valuesJsonHolder;
        this.valueInserter.$valueElementsContainer = this.$valueElementsContainer;
        this.valueInserter.init();
    }

    wantAddRole() {
        this.isOpened = true;
        this.setRoleIndexInModal(-1);
        this.setRole({});
        $('#RoleModal').modal('show');
    }
    dontAddRole() {
        this.isOpened = false;
    }
    editRole(editIndex, role) {
        this.setRoleIndexInModal(editIndex);
        this.setRole(role);

        $('#Role-Name-error').text('');
        this.isOpened = true;
        $('#RoleModal').modal('show');
    }
    comitRole() {
        if (!this.isOpened) return;

        $('#Role-Name-error').text('');

        var editIndex = this.getRoleIndexFromModal();
        if (editIndex == -1) {
            var value = this.getRole();
            if (!value.Name) {
                $('#Role-Name-error').text('empty Name?');
                return;
            }
            //if (getNameError()) return;
            if (this.getDuplicatedNameError()) return;
            if (!this.valueInserter.addValue(value)) {
                $('#Role-Name-error').text('could not add property');
                return;
            }
        }
        else this.valueInserter.updateValue(editIndex, this.getRole());

        // Close the modal
        this.isOpened = false;
        $('#RoleModal').modal('hide');
    }
    getDuplicatedNameError() {
        var objects = [];
        if (this.$valuesJsonHolder && this.$valuesJsonHolder.val()) objects = JSON.parse(this.$valuesJsonHolder.val());
        var names = objects.map(i => i.Name);
        names.push($('#PropName').val());
        if (names.length !== new Set(names).size) {
            $('#Role-Name-error').text('There is already property with the same name.');
            return true;
        }
        $('#Role-Name-error').text('');
        return false;
    }
    getRole() {
        return {
            Name: $('#roleName').val(),
            Permissions: (function () {
                const perms = {};
                // collect all checkboxes inside modal and add checked ones
                $('#roleModalBody').find('input[type="checkbox"]').each(function () {
                    const id = $(this).attr('id');
                    if (!id) return;
                    if ($(this).is(':checked')) perms[id] = $(this).siblings('span').text() || $(this).closest('label').text().trim();
                });
                return perms;
            })()
        };
    }
    setRole(prop) {
        console.log(prop);
        this.$modalBody.find('#roleName').val(prop?.Name ?? '');
        this.$modalBody.find('input[type="checkbox"]').prop('checked', false);

        // If new shape with Permissions dictionary is provided, check only those keys
        if (!prop || !prop.Permissions || typeof prop.Permissions !== 'object') return;
        try {
            Object.keys(prop.Permissions).forEach(id => {
                const $cb = this.$modalBody.find(`#${CSS.escape ? CSS.escape(id) : id}`);
                if ($cb && $cb.length) $cb.prop('checked', true);
            });
            return;
        } catch (e) {
            console.warn('setRole: error applying Permissions object, falling back to legacy shape', e);
        }
    }
    getRoleIndexFromModal() {
        return $('#RoleModal').data('edit-index');
    }
    setRoleIndexInModal(index) {
        $('#RoleModal').data('edit-index', index);
    }
}