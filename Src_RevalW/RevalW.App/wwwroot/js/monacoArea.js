class MonacoArea {
    autosaveTimer = null;
    getPayload;
    monacoEditor;
    resizerJquery;
    hiddenTextareaJquery;
    monacoContainerJquery;
    autosaveUrl;
    isAutoSaveable;

    constructor() {
        this.startY = 0;
        this.startH = 0;
        this.onPointerMoveBound = this.onPointerMove.bind(this);
        this.onPointerUpBound = this.onPointerUp.bind(this);
    }

    init() {
        document.addEventListener('DOMContentLoaded', () => this.onDomReady());
    }

    onDomReady() {
        if (!this.hiddenTextareaJquery?.length || !this.monacoContainerJquery?.length) return;

        if (window.monaco) {
            try { this.onMonacoLoaded(window.monaco); } catch (e) { }
            return;
        }

        if (MonacoArea._loaderAppended) {
            document.addEventListener('monaco-loaded', () => {
                try { this.onMonacoLoaded(window.monaco); } catch (e) { }
            }, { once: true });
            return;
        }

        MonacoArea._loaderAppended = true;
        const loaderUrl = 'https://cdn.jsdelivr.net/npm/monaco-editor@0.39.0/min/vs/loader.js';
        const script = document.createElement('script');
        script.src = loaderUrl;
        script.onload = () => this.onMonacoScriptLoaded();
        document.body.appendChild(script);
    }

    onMonacoScriptLoaded() {
        if (typeof require === 'undefined') return;

        require.config({ paths: { 'vs': 'https://cdn.jsdelivr.net/npm/monaco-editor@0.39.0/min/vs' } });
        require(['vs/editor/editor.main'], (monaco) => {
            MonacoArea.monacoLoaded = true;
            document.dispatchEvent(new Event('monaco-loaded'));
            try { this.onMonacoLoaded(monaco); } catch (e) { }
        });
    }

    onMonacoLoaded(monaco) {
        this.defineMonacoTheme(monaco);

        const initialSiteTheme = document.documentElement.getAttribute('data-bs-theme') || 'dark';
        try { monaco.editor.setTheme(this.mapTheme(initialSiteTheme)); } catch (e) { /* ignore */ }

        // create editors and wire UI
        this.createEditors(monaco);
        this.attachResizers();
        this.attachTextareaSync();

        // register editor-specific Ctrl/Cmd+S and guarded global handler
        this.registerEditorShortcuts(monaco);
        window.addEventListener('keydown', this.onGlobalKeydown.bind(this));
    }

    // ----------------- Editors, sync and UI -----------------

    createEditors(monaco) {
        const monacoContainerDom = this.monacoContainerJquery[0];
        const initialValue = this.hiddenTextareaJquery.val() || '';

        this.monacoEditor = monaco.editor.create(monacoContainerDom, {
            value: initialValue,
            language: 'html',
            automaticLayout: true,
            minimap: { enabled: false },
            wordWrap: 'on',
            fontFamily: 'monospace',
            fontSize: 13,
            scrollBeyondLastLine: false,
            // enforce 2-space indentation in editor options
            tabSize: 2,
            insertSpaces: true
        });

        // also update the model to ensure indentation is applied and not auto-detected
        try {
            const model = this.monacoEditor.getModel();
            if (model) {
                model.updateOptions({
                    tabSize: 2,
                    insertSpaces: true,
                    detectIndentation: false
                });
            } else {
                this.monacoEditor.updateOptions({
                    tabSize: 2,
                    insertSpaces: true
                });
            }
        } catch (e) { /* ignore */ }
    }

    attachTextareaSync() {
        if (!this.monacoEditor) return;
        this.monacoEditor.onDidBlurEditorText(() => {
            this.hiddenTextareaJquery.val(this.monacoEditor.getValue());
        });
        this.monacoEditor.onDidChangeModelContent(() => {
            this.hiddenTextareaJquery.val(this.monacoEditor.getValue());
        });
    }

    attachResizers() {
        const initialHeight = parseInt(this.resizerJquery.data('initial-height') || '300', 10);

        if (this.monacoContainerJquery.length === 0) return;

        this.resizerJquery.on('pointerdown', (ev) => {
            ev.preventDefault();
            this.startY = ev.clientY;
            this.startH = this.monacoContainerJquery[0].offsetHeight;
            document.addEventListener('pointermove', this.onPointerMoveBound);
            document.addEventListener('pointerup', this.onPointerUpBound);
        });

        this.resizerJquery.on('dblclick', () => {
            this.monacoContainerJquery.height(initialHeight);
            try { this.monacoEditor.layout(); } catch (err) { /* ignore */ }
        }, false);
    }

    onPointerMove(e) {
        const ev = e.touches && e.touches[0] ? e.touches[0] : e;
        const clientY = ev.clientY;
        const deltaY = clientY - this.startY;
        const minHeight = 150;
        const maxHeight = 2000;
        const newH = Math.max(minHeight, Math.min(maxHeight, this.startH + deltaY));
        this.monacoContainerJquery.height(newH);

        try { this.monacoEditor.layout(); } catch (err) { /* ignore */ }
    }
    onPointerUp() {
        document.removeEventListener('pointermove', this.onPointerMoveBound);
        document.removeEventListener('pointerup', this.onPointerUpBound);
    };

    // ----------------- Autosave / Ctrl+S -----------------
    registerEditorShortcuts(monaco) {
        try {
            if (typeof this.monacoEditor.addCommand !== 'function') return;

            this.monacoEditor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KEY_S, () => {
                const node = this.monacoEditor.getDomNode && this.monacoEditor.getDomNode();
                const containerId = (node && node.id) ? node.id : null;
                this.doAutosaveForEditor(containerId);
            });
        } catch (e) { /* ignore */ }
    }

    onGlobalKeydown(e) {
        // only run autosave if this instance has an editor, the editor is focused, and user pressed Ctrl/Cmd+S
        if (!this.autosaveUrl) return;
        if (!(e.ctrlKey || e.metaKey) || !(e.key === 's' || e.key === 'S')) return;
        e.preventDefault();
        // if we have an editor instance, ensure it has focus before handling the global shortcut
        if (this.monacoEditor) {
            try {
                if (typeof this.monacoEditor.hasTextFocus === 'function') {
                    if (!this.monacoEditor.hasTextFocus()) return;
                } else {
                    // fallback: check whether editor DOM contains the active element
                    const node = this.monacoEditor.getDomNode && this.monacoEditor.getDomNode();
                    if (!node || !node.contains(document.activeElement)) return;
                }
            } catch (err) {
                return;
            }
        } else {
            return;
        }
        if (!this.isAutoSaveable(this.getPayload())) {
            this.showStatus('Run manual save (create) on config first!');
            return;
        }
        try { this.doAutosaveForEditor(); } catch (err) { /* ignore */ }
    }

    doAutosaveForEditor() {
        if (this.autosaveTimer) clearTimeout(this.autosaveTimer);
        this.autosaveTimer = setTimeout(async () => {
            const payload = this.getPayload();
            this.showStatus('Saving...');
            const ok = await this.sendAutosave(payload);
            this.showStatus(ok ? 'Saved' : 'Save failed', ok ? 1200 : 3000);
        }, 120);
    }

    async sendAutosave(payload) {
        try {
            const headers = { 'Content-Type': 'application/json' };
            const token = this.getAntiForgeryToken();
            if (token) headers['RequestVerificationToken'] = token;
            const res = await fetch(this.autosaveUrl, {
                method: 'POST',
                headers: headers,
                body: JSON.stringify(payload),
                credentials: 'same-origin'
            });

            if (!res.ok) {
                const text = await res.text().catch(() => '');
                throw new Error('Status ' + res.status + ' ' + text);
            }
            return true;
        } catch (err) {
            return false;
        }
    }

    // ----------------- Utilities ----------------- 

    getAntiForgeryToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : null;
    }

    ensureStatusEl() {
        return MonacoArea.ensureStatusElStatic();
    }

    showStatus(text, timeout = 2000) {
        MonacoArea.showStatusStatic(text, timeout);
    }

    // static helpers for status element so multiple instances would share the UI
    static ensureStatusElStatic() {
        let el = document.getElementById('AutoSaveStatus');
        if (el) return el;
        el = document.createElement('div');
        el.id = 'AutoSaveStatus';
        el.setAttribute('aria-live', 'polite');
        el.style.position = 'fixed';
        el.style.right = '16px';
        el.style.top = '16px';
        el.style.display = 'none';
        el.style.padding = '6px 10px';
        el.style.borderRadius = '6px';
        el.style.background = 'rgba(32,33,36,0.9)';
        el.style.color = '#fff';
        el.style.fontSize = '13px';
        el.style.zIndex = '99999';
        el.style.boxShadow = '0 2px 8px rgba(0,0,0,0.3)';
        document.body.appendChild(el);
        return el;
    }
    static showStatusStatic(text, timeout = 2000) {
        const el = MonacoArea.ensureStatusElStatic();
        el.textContent = text;
        el.style.display = 'block';
        clearTimeout(el._hideTimeout);
        el._hideTimeout = setTimeout(() => el.style.display = 'none', timeout);
    }

    mapTheme(siteTheme) {
        return (siteTheme === 'dark') ? 'reval-dark-enhanced' : 'vs';
    }

    defineMonacoTheme(monaco) {
        monaco.editor.defineTheme('reval-dark-enhanced', {
            base: 'vs-dark',
            inherit: true,
            rules: [
                { token: 'comment', foreground: '6A9955' },
                { token: 'string', foreground: 'CE9178' },
                { token: 'number', foreground: 'B5CEA8' },
                { token: 'keyword', foreground: 'C586C0' },
                { token: 'delimiter.html', foreground: 'D4D4D4' },
                { token: 'tag.html', foreground: '569CD6' },
                { token: 'attribute.name.html', foreground: '9CDCFE' },
                { token: 'attribute.value.html', foreground: 'CE9178' },
                { token: 'type', foreground: '4EC9B0' },
                { token: 'identifier', foreground: 'D4D4D4' }
            ],
            colors: {
                'editor.background': '#1e1e1e',
                'editor.foreground': '#d4d4d4',
                'editorLineNumber.foreground': '#858585'
            }
        });
    }
}