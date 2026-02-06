(function () {
    function rlog(msg) {
        console.log('[Renderer] ' + msg);
        if (window.api && window.api.logToMain) window.api.logToMain(msg);
    }

    rlog('audio.js loaded');

    if (!window.api) {
        console.error('[Renderer] audio.js: window.api not found');
        return;
    }

    let defaultFileName = null;
    let store = window.api.getStore();
    /** Cached blob URL for current default file (HTMLAudioElement won't play app-audio:// directly). */
    let cachedBlobUrl = null;
    let cachedForFileName = null;

    /**
     * Ensure we have a blob URL for the current default file. Fetch via app-audio (works),
     * then createObjectURL so <audio> can play it. No decodeAudioData.
     */
    async function ensureBlobUrl() {
        if (!defaultFileName) return null;
        if (cachedBlobUrl && cachedForFileName === defaultFileName) return cachedBlobUrl;
        if (cachedBlobUrl) {
            URL.revokeObjectURL(cachedBlobUrl);
            cachedBlobUrl = null;
            cachedForFileName = null;
        }
        const url = window.api.getAudioFileUrl(defaultFileName);
        if (!url) return null;
        try {
            const response = await fetch(url);
            if (!response.ok) throw new Error('Fetch ' + response.status);
            const blob = await response.blob();
            cachedBlobUrl = URL.createObjectURL(blob);
            cachedForFileName = defaultFileName;
            return cachedBlobUrl;
        } catch (err) {
            rlog('audio: ensureBlobUrl failed ' + err.message);
            return null;
        }
    }

    /**
     * Play using HTMLAudioElement with a blob URL (no Web Audio decodeAudioData).
     * app-audio:// is not a "supported source" for <audio>; blob URLs are.
     */
    function play(payload) {
        if (!store.isEnabled) return;
        if (!defaultFileName) return;
        const keycode = payload && typeof payload.keycode === 'number' ? payload.keycode : null;
        const when = payload && payload.when ? payload.when : new Date().toISOString();
        ensureBlobUrl().then((blobUrl) => {
            if (!blobUrl) {
                rlog('audio: keydown key=' + keycode + ' at ' + when + ' -> skipped (no blob)');
                return;
            }
            try {
                const el = new window.Audio(blobUrl);
                el.volume = 1;
                el.play().then(() => {
                    rlog('audio: keydown key=' + keycode + ' at ' + when + ' -> sound played');
                }).catch((err) => {
                    rlog('audio: keydown key=' + keycode + ' at ' + when + ' -> play failed ' + err.message);
                });
            } catch (err) {
                rlog('audio: keydown key=' + keycode + ' at ' + when + ' -> error ' + err.message);
            }
        });
    }

    window.__audioPlay = () => play({ keycode: null, when: new Date().toISOString() });

    if (window.api.onKeyDown) {
        window.api.onKeyDown((payload) => play(payload));
        rlog('audio: onKeyDown registered');
    }

    window.api.onSettingUpdate('defaultAudioFile', () => {
        store = window.api.getStore();
        const prev = defaultFileName;
        defaultFileName = store.defaultAudioFile;
        if (cachedBlobUrl && (prev !== defaultFileName || !defaultFileName)) {
            URL.revokeObjectURL(cachedBlobUrl);
            cachedBlobUrl = null;
            cachedForFileName = null;
        }
        rlog('audio: defaultAudioFile changed to ' + (defaultFileName || '(none)') + ', will use on next play');
    });

    window.api.onSettingUpdate('isEnabled', () => {
        store = window.api.getStore();
    });

    store = window.api.getStore();
    defaultFileName = store.defaultAudioFile;
    rlog('audio: init defaultFile=' + (defaultFileName || '(none)') + ' (HTMLAudioElement + blob URL, no decode)');
})();
