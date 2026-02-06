(function () {
    function rlog(msg) {
        console.log('[Renderer] ' + msg);
        if (window.api && window.api.logToMain) window.api.logToMain(msg);
    }

    rlog('settings.js loaded');

    window.addEventListener('unhandledrejection', function (event) {
        console.error('[Renderer] unhandled rejection', event.reason);
        if (window.api && window.api.logToMain) window.api.logToMain('unhandled rejection ' + String(event.reason));
    });

    const fileListEl = document.getElementById('fileList');
    const btnAdd = document.getElementById('btnAdd');
    const btnDelete = document.getElementById('btnDelete');
    const btnSetDefault = document.getElementById('btnSetDefault');
    const btnPlayDefault = document.getElementById('btnPlayDefault');
    const btnClose = document.getElementById('btnClose');
    const checkEnable = document.getElementById('checkEnable');
    const statusEl = document.getElementById('status');

    if (!window.api) {
        console.error('[Renderer] window.api not found');
        statusEl.textContent = 'Error: window.api not found';
        return;
    }
    rlog('settings window.api OK');

    let store = window.api.getStore();
    rlog('settings store defaultAudioFile=' + (store && store.defaultAudioFile));
    let selectedFileName = null;

    function setStatus(msg) {
        statusEl.textContent = msg || 'Ready';
    }

    async function loadFileList() {
        rlog('settings loadFileList start');
        const files = await window.api.getAudioFiles();
        rlog('settings loadFileList getAudioFiles done n=' + (files ? files.length : 0));
        fileListEl.innerHTML = '';
        files.forEach((name) => {
            const div = document.createElement('div');
            div.className = 'file-item' + (name === store.defaultAudioFile ? ' default' : '');
            div.textContent = name;
            div.dataset.file = name;
            div.addEventListener('click', () => {
                fileListEl.querySelectorAll('.file-item').forEach((e) => e.classList.remove('selected'));
                div.classList.add('selected');
                selectedFileName = name;
                updateButtonStates();
            });
            fileListEl.appendChild(div);
        });
        selectedFileName = null;
        updateButtonStates();
        rlog('settings loadFileList done');
    }

    function updateButtonStates() {
        const hasSelection = selectedFileName !== null;
        const hasDefault = !!(store && store.defaultAudioFile);
        btnDelete.disabled = !hasSelection;
        btnSetDefault.disabled = !hasSelection;
        btnPlayDefault.disabled = !hasDefault;
        if (hasSelection && selectedFileName === store.defaultAudioFile) {
            btnSetDefault.textContent = 'Default (Current)';
        } else {
            btnSetDefault.textContent = 'Set as Default';
        }
    }

    btnAdd.addEventListener('click', async () => {
        console.log('[Renderer] Add File clicked');
        const path = await window.api.openFileDialog();
        console.log('[Renderer] openFileDialog returned:', path || '(canceled)');
        if (!path) return;
        const ok = await window.api.addAudioFile(path);
        console.log('[Renderer] addAudioFile result:', ok);
        if (ok) {
            await loadFileList();
            setStatus('File added successfully.');
        } else {
            setStatus('Failed to add file. Use a supported format (mp3, wav, wma, m4a, aac, ogg, flac).');
        }
    });

    btnDelete.addEventListener('click', async () => {
        if (!selectedFileName) {
            setStatus('Please select a file to delete.');
            return;
        }
        const result = await window.api.showMessageBox({
            type: 'question',
            title: 'Confirm Delete',
            message: 'Are you sure you want to delete "' + selectedFileName + '"?',
            buttons: ['Yes', 'No'],
            defaultId: 1,
        });
        if (result.response === 0) {
            const ok = await window.api.deleteAudioFile(selectedFileName);
            if (ok) {
                if (store.defaultAudioFile === selectedFileName) {
                    await window.api.set('defaultAudioFile', '');
                    store = window.api.getStore();
                }
                await loadFileList();
                setStatus('File deleted successfully.');
            } else {
                setStatus('Failed to delete file.');
            }
        }
    });

    btnSetDefault.addEventListener('click', async () => {
        if (!selectedFileName) {
            setStatus('Please select a file to set as default.');
            return;
        }
        await window.api.set('defaultAudioFile', selectedFileName);
        store = window.api.getStore();
        await loadFileList();
        setStatus('Default file set to: ' + selectedFileName);
    });

    btnPlayDefault.addEventListener('click', () => {
        if (!store.defaultAudioFile) {
            setStatus('No default file set.');
            return;
        }
        if (typeof window.__audioPlay === 'function') {
            window.__audioPlay();
            setStatus('Playing default sound…');
        } else {
            setStatus('Playback not ready.');
        }
    });

    btnClose.addEventListener('click', () => {
        console.log('[Renderer] Close clicked');
        window.api.closeSettingsWindow();
    });

    checkEnable.addEventListener('change', () => {
        console.log('[Renderer] Enable checkbox changed:', checkEnable.checked);
        window.api.set('isEnabled', checkEnable.checked);
    });

    window.api.onSettingUpdate('isEnabled', (value) => {
        checkEnable.checked = value;
    });

    window.api.onSettingUpdate('defaultAudioFile', () => {
        store = window.api.getStore();
        loadFileList().catch((err) => {
            console.error('[Renderer] loadFileList after defaultAudioFile update failed', err);
            setStatus('Error refreshing list');
        });
    });

    loadFileList().catch((err) => {
        console.error('[Renderer] settings initial loadFileList failed', err);
        if (window.api && window.api.logToMain) window.api.logToMain('settings init loadFileList failed ' + err.message);
    });
    checkEnable.checked = store.isEnabled;
    rlog('settings.js init complete');
})();
