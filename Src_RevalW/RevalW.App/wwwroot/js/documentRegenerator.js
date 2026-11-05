class DocumentRegenerator {
    appendProgressMessage;
    handleErr;
    handleFinish;
    hubUrl;

    generationConnection = null;
    generationConnectionStarted = false;
    currentOperationId = null;
    
    async ensureConnection() {
        console.log('ensureConnection', this.hubUrl)
        if (this.generationConnectionStarted) return;
        this.generationConnection = new signalR.HubConnectionBuilder()
            .withUrl(this.hubUrl)
            .withAutomaticReconnect()
            .build();

        this.generationConnection.on("progress", this.appendProgressMessage);
        this.generationConnection.on("ok", this.handleFinish);
        this.generationConnection.on("err", this.handleErr);
        this.generationConnection.onclose((err) => {
            this.generationConnectionStarted = false;
            console.warn("SignalR closed", err);
        });

        try {
            await this.generationConnection.start();
            this.generationConnectionStarted = true;
            console.info("SignalR connected to", this.hubUrl);
        } catch (err) {
            console.error("SignalR start failed", err);
            this.appendProgressMessage('SignalR unavailable; progress will be returned after generation completes. (' + (err?.message || err) + ')');
            throw err;
        }
    }

    async regenerateDocumentAsync(btn) {
        $('#stderrDisplay').text('');
        const $btn = $(btn).closest("button");
        if ($btn.hasClass('btn-primary')) return;

        window.$btn = $btn;
        $('#stderrDisplay').text('generating...');

        try {
            await this.ensureConnection();
        } catch {
            // continue - server will still run and return op id
        }

        $.ajax({
            url: '/Items/GenerateDocument',
            type: 'GET',
            data: {
                itemId: window.$btn.data('item-id'),
                documentId: window.$btn.data('document-id')
            },
            success: (response) => {
                try {
                    const opId = response?.operationId || response?.opId;
                    if (opId) {
                        this.currentOperationId = opId;
                        this.appendProgressMessage('Started generation. operationId: ' + opId);
                        console.log(this.generationConnection)
                        if (this.generationConnection && this.generationConnection.invoke) {
                            try {
                                console.log('joining')
                                this.generationConnection.invoke("JoinGroup", opId);
                            } catch (e) {
                                console.warn('JoinGroup failed', e);
                            }
                        }
                    } else {
                        this.appendProgressMessage('No operationId returned. Generation may be synchronous.');
                    }
                } catch (err) {
                    this.appendProgressMessage('Error processing server response');
                }
            },
            error: (xhr, status, error) => {
                this.appendProgressMessage('Error communicating with server: ' + error);
            }
        });
    }

    afterSuccessResponse(response) {
        var error;
        if (response.stdErr) {
            this.appendProgressMessage(response.stdErr);
            error = true;
        } else {
            $('#stderrDisplay').html('');
            error = false;
        }

        var $wrapper = window.$btn.closest('.input-row');

        if (error) {
            try {
                // if already a generate button, nothing to do
                if (window.$btn.hasClass('generateDocumentBtn')) return;

                const itemId = window.$btn.data('item-id');
                const documentId = window.$btn.data('document-id');

                // try to get title from existing anchor (if present) or button text
                var $anchor = $wrapper.find('a.value-button').first();
                var title = $anchor.length ? $anchor.text().trim() : (window.$btn.text().trim() || 'Document');

                var $newBtn = $('<button>', {
                    type: 'button',
                    class: 'btn btn-danger value-button rounded-start-5 rounded-end-5 generateDocumentBtn',
                    text: title
                })
                    .data('item-id', itemId)
                    .data('document-id', documentId);

                // replace all contents of wrapper with the single generate button
                $wrapper.empty().append($newBtn);
            } catch (err) {
                this.appendProgressMessage('Error processing server response');
            }
        } else {
            try {
                // if already a regenerate button, nothing to do
                if (window.$btn.hasClass('regenerateDocumentBtn')) return;

                const outPath = response.outPath || '';
                // determine title: if wrapper had anchor use it, otherwise use clicked button text
                var $existingAnchor = $wrapper.find('a.value-button').first();
                var title = $existingAnchor.length ? $existingAnchor.text().trim() : (window.$btn.text().trim() || 'Document');

                // capture item/document ids for regenerate button
                const itemId = window.$btn.data('item-id');
                const documentId = window.$btn.data('document-id');

                var $a = $('<a/>', {
                    class: 'btn btn-primary value-button rounded-start-5 rounded-end-0',
                    href: '/open?fp=' + encodeURIComponent(outPath),
                    target: '_blank',
                    text: title
                });

                // insert the small regenerate button next to the anchor (match server-side markup lines ~70-74)
                var $regBtn = $('<button>', {
                    type: 'button',
                    class: 'btn btn-info value-button rounded-start-0 rounded-end-5 regenerateDocumentBtn'
                })
                    .data('item-id', itemId)
                    .data('document-id', documentId)
                    .append($('<i/>', { class: 'bi bi-arrow-counterclockwise' }));

                // Replace wrapper contents with anchor + regenerate button
                $wrapper.empty().append($a).append($regBtn);
            } catch (err) {
                this.appendProgressMessage('Error processing server response');
            }
        }
    }
}