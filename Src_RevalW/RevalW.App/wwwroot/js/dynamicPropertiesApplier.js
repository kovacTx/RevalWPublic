class DynamicPropertiesApplier {
    $appliedProperties;
    $propertiesHolder;

    applyDynamicProperties(event) {
        const appliedProperties = {};
        let isValid = true;
        this.$propertiesHolder.find('.dynamic-property').each(function () {
            const propertyName = this.dataset.propertyName;
            const propertyLabel = this.dataset.propertyLabel;
            const validityState = this.validity;

            if (!validityState.valid) {
                event.preventDefault(); // Prevent form submission
                isValid = false;

                let errorMessage = '';
                if (validityState.rangeOverflow) {
                    errorMessage = `Value for ${propertyLabel} is too large. (max:${this.max})`;
                } else if (validityState.rangeUnderflow) {
                    errorMessage = `Value for ${propertyLabel} is too small. (min:${this.min})`;
                } else if (validityState.valueMissing) {
                    errorMessage = `The ${propertyLabel} field is required.`;
                } else if (validityState.stepMismatch) {
                    errorMessage = `Value for ${propertyLabel} must be a multiple of ${this.step} , plus minimum ${this.min} .`;
                } else {
                    errorMessage = `Invalid value for ${propertyLabel}. (min:${this.min} , max:${this.max} , step:${this.step})`;
                }
                // Find or create an error message element
                let errorElement = document.getElementById(`${propertyName}-error`);
                if (!errorElement) {
                    errorElement = document.createElement('span');
                    errorElement.id = `${propertyName}-error`;
                    errorElement.className = 'text-danger';
                    this.parentNode.appendChild(errorElement);
                }

                errorElement.textContent = errorMessage;
            } else {
                // Clear any previous error message
                const errorElement = document.getElementById(`${propertyName}-error`);
                if (errorElement) errorElement.textContent = '';
            }
            appliedProperties[this.dataset.propertyName] = this.value;
        });

        if (isValid) {
            this.$appliedProperties.val(JSON.stringify(appliedProperties));
        }
    }
}