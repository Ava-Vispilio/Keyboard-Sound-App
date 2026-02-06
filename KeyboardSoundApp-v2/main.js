import { app, Tray, Menu, nativeImage, ipcMain, BrowserWindow, dialog, protocol } from 'electron';
import path from 'path';
import { fileURLToPath } from 'url';
import fs from 'fs';
import { spawn } from 'child_process';
import Store from 'electron-store';
import { log, logError } from './utils/logger.cjs';
import { createAudioFileManager } from './utils/audio-file-manager.cjs';

const MIME_TYPES = {
    '.mp3': 'audio/mpeg',
    '.wav': 'audio/wav',
    '.ogg': 'audio/ogg',
    '.m4a': 'audio/mp4',
    '.aac': 'audio/aac',
    '.flac': 'audio/flac',
    '.wma': 'audio/x-ms-wma',
};

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const store = new Store({
    defaults: {
        defaultAudioFile: '',
        isEnabled: true,
    },
});

let tray = null;
let settingsWindow = null;
let audioWindow = null;
let audioFileManager = null;
let hasShownSettingsOnce = false;
let keyListenerProcess = null;
let keyListenerBuffer = '';

protocol.registerSchemesAsPrivileged([
    { scheme: 'app-audio', privileges: { standard: true, secure: true, supportFetchAPI: true } },
]);

// Single instance lock: only one app instance; second launch focuses existing
const gotTheLock = app.requestSingleInstanceLock();
if (!gotTheLock) {
    app.quit();
} else {
    app.on('second-instance', () => {
        log('[Main] second-instance: focusing window');
        if (settingsWindow && !settingsWindow.isDestroyed()) {
            settingsWindow.show();
            settingsWindow.focus();
        }
    });
}

function getTrayIcon() {
    // No icon supplied for now: use empty image so tray still works
    const icon = nativeImage.createEmpty();
    return icon;
}

/** Dev: libs/key-listeners/; packaged: process.resourcesPath. Binary: keyboard-sound-listener[.exe] */
function getListenerPath() {
    const isPackaged = app.isPackaged;
    const baseDir = isPackaged ? process.resourcesPath : path.join(__dirname, 'libs', 'key-listeners');
    const exe = process.platform === 'win32' ? 'keyboard-sound-listener.exe' : 'keyboard-sound-listener';
    return path.join(baseDir, exe);
}

function stopKeyListener() {
    if (keyListenerProcess) {
        log('[Main] key listener: stopping');
        keyListenerProcess.kill();
        keyListenerProcess = null;
    }
    keyListenerBuffer = '';
}

function startKeyListener() {
    if (keyListenerProcess) return;
    const listenerPath = getListenerPath();
    if (!fs.existsSync(listenerPath)) {
        log('[Main] key listener: binary not found at ' + listenerPath + ' (run npm run build:mac-listener or build:win-listener)');
        return;
    }
    log('[Main] key listener: starting ' + listenerPath);
    keyListenerProcess = spawn(listenerPath, [], { stdio: ['ignore', 'pipe', 'pipe'] });
    keyListenerProcess.stdout.on('data', (chunk) => {
        keyListenerBuffer += chunk.toString();
        let idx;
        while ((idx = keyListenerBuffer.indexOf('\n')) !== -1) {
            const line = keyListenerBuffer.slice(0, idx).trim();
            keyListenerBuffer = keyListenerBuffer.slice(idx + 1);
            if (!line) continue;
            try {
                const obj = JSON.parse(line);
                if (typeof obj.keydown === 'number') {
                    const keycode = obj.keydown;
                    const when = new Date().toISOString();
                    log('[Main] keydown key=' + keycode + ' at ' + when + ' -> sending to audio window');
                    safeSendToAudio('keydown', { keycode, when });
                }
            } catch (_) {
                // ignore parse errors
            }
        }
    });
    keyListenerProcess.stderr.on('data', (chunk) => {
        log('[Main] key listener stderr: ' + chunk.toString().trim());
    });
    keyListenerProcess.on('error', (err) => {
        logError('[Main] key listener error', err);
        keyListenerProcess = null;
    });
    keyListenerProcess.on('exit', (code, signal) => {
        log('[Main] key listener exited code=' + code + ' signal=' + signal);
        keyListenerProcess = null;
    });
}

function safeSendToSettings(channel, ...args) {
    if (settingsWindow && !settingsWindow.isDestroyed() && !settingsWindow.webContents.isDestroyed()) {
        try {
            settingsWindow.webContents.send(channel, ...args);
        } catch (err) {
            logError('send to settings failed', err);
        }
    }
}

/** Send to the hidden audio window (used for keydown → play; survives settings close). */
function safeSendToAudio(channel, ...args) {
    if (audioWindow && !audioWindow.isDestroyed() && !audioWindow.webContents.isDestroyed()) {
        try {
            audioWindow.webContents.send(channel, ...args);
        } catch (err) {
            logError('send to audio window failed', err);
        }
    }
}

function createSettingsWindow() {
    if (settingsWindow && !settingsWindow.isDestroyed()) {
        return settingsWindow;
    }
    log('[Main] createSettingsWindow');
    settingsWindow = new BrowserWindow({
        width: 420,
        height: 420,
        show: false,
        webPreferences: {
            preload: path.join(__dirname, 'preload.cjs'),
            contextIsolation: true,
            nodeIntegration: false,
        },
    });
    settingsWindow.setMenu(null);
    settingsWindow.loadFile('settings.html');
    settingsWindow.webContents.on('did-finish-load', () => {
        log('[Main] Settings window finished loading: ' + settingsWindow.webContents.getURL());
    });
    settingsWindow.webContents.on('did-fail-load', (_, code, desc) => {
        logError('[Main] Settings window failed to load: ' + code + ' ' + desc);
    });
    settingsWindow.webContents.on('before-input-event', (_, input) => {
        if ((input.control || input.meta) && input.shift && input.key.toLowerCase() === 'i') {
            settingsWindow.webContents.toggleDevTools();
        }
    });
    settingsWindow.on('close', (e) => {
        if (!app.isQuitting) {
            e.preventDefault();
            settingsWindow.hide();
            log('[Main] Settings window hidden');
        }
    });
    settingsWindow.on('closed', () => {
        settingsWindow = null;
    });
    return settingsWindow;
}

/** Hidden window that runs audio.js and receives keydown; never shown, so decodeAudioData does not blank UI. */
function createAudioWindow() {
    if (audioWindow && !audioWindow.isDestroyed()) {
        return audioWindow;
    }
    log('[Main] createAudioWindow (hidden)');
    audioWindow = new BrowserWindow({
        show: false,
        webPreferences: {
            preload: path.join(__dirname, 'preload.cjs'),
            contextIsolation: true,
            nodeIntegration: false,
        },
    });
    audioWindow.loadFile('audio.html');
    audioWindow.webContents.on('did-finish-load', () => {
        log('[Main] Audio window ready');
    });
    audioWindow.webContents.on('did-fail-load', (_, code, desc) => {
        logError('[Main] Audio window failed to load: ' + code + ' ' + desc);
    });
    audioWindow.on('closed', () => {
        audioWindow = null;
    });
    return audioWindow;
}

function showSettingsWindow() {
    const win = createSettingsWindow();
    win.show();
    win.focus();
}

function updateTrayMenu() {
    if (!tray || tray.isDestroyed()) return;
    const isEnabled = store.get('isEnabled');
    const menu = Menu.buildFromTemplate([
        {
            label: 'Settings',
            click: () => showSettingsWindow(),
        },
        {
            label: isEnabled ? 'Disable' : 'Enable',
            click: () => {
                const next = !store.get('isEnabled');
                store.set('isEnabled', next);
                log('[Main] isEnabled set to ' + next + ' (tray)');
                updateTrayMenu();
                safeSendToSettings('updated-isEnabled', next);
                safeSendToAudio('updated-isEnabled', next);
                if (next) startKeyListener();
                else stopKeyListener();
            },
        },
        { type: 'separator' },
        {
            label: 'Exit',
            click: () => app.quit(),
        },
    ]);
    tray.setContextMenu(menu);
}

app.whenReady().then(() => {
    log('[Main] App starting, platform: ' + process.platform);
    log('[Main] Store: defaultAudioFile=' + JSON.stringify(store.get('defaultAudioFile')) + ', isEnabled=' + store.get('isEnabled'));

    protocol.handle('app-audio', (request) => {
        const url = new URL(request.url);
        const fileName = decodeURIComponent(url.pathname.slice(1));
        const audioPath = path.join(app.getPath('userData'), 'AudioFiles', fileName);
        log('[Main] app-audio request: ' + fileName);
        const ext = path.extname(fileName).toLowerCase();
        const contentType = MIME_TYPES[ext] || 'application/octet-stream';
        try {
            const buffer = fs.readFileSync(audioPath);
            return new Response(buffer, {
                headers: { 'Content-Type': contentType },
            });
        } catch {
            return new Response('Not found', { status: 404 });
        }
    });

    audioFileManager = createAudioFileManager(app.getPath('userData'));
    audioFileManager.ensureStorageExists();
    log('[Main] Audio file manager ready: ' + app.getPath('userData') + '/AudioFiles');

    const icon = getTrayIcon();
    tray = new Tray(icon);
    tray.setToolTip('Keyboard Sound App');
    updateTrayMenu();

    if (process.platform === 'darwin') {
        app.dock?.hide();
        log('[Main] Dock hidden (menu bar only)');
    }

    createSettingsWindow();
    createAudioWindow();
    if (!hasShownSettingsOnce) {
        hasShownSettingsOnce = true;
        showSettingsWindow();
        log('[Main] Settings window shown on first launch');
    }

    if (store.get('isEnabled')) {
        startKeyListener();
    }

    log('[Main] Phase 6/7 ready: tray + store + audio file manager + settings window + app-audio protocol + key listener');
});

ipcMain.on('renderer-log', (_, msg) => {
    log('[Rdr] ' + msg);
});

ipcMain.on('get-store', (e) => {
    log('[Main] IPC get-store');
    e.returnValue = store.store;
});

ipcMain.handle('store-set', async (_, key, value) => {
    log('[Main] IPC store-set: ' + key + ' = ' + JSON.stringify(value));
    store.set(key, value);
    safeSendToSettings('updated-' + key, value);
    safeSendToAudio('updated-' + key, value);
    if (key === 'isEnabled') {
        log('[Main] isEnabled set to ' + value + ' (settings)');
        updateTrayMenu();
        if (value) startKeyListener();
        else stopKeyListener();
    }
});

ipcMain.handle('open-file-dialog', async () => {
    log('[Main] IPC open-file-dialog');
    const result = await dialog.showOpenDialog(settingsWindow || null, {
        properties: ['openFile'],
        filters: [
            { name: 'Audio Files', extensions: ['mp3', 'wav', 'wma', 'm4a', 'aac', 'ogg', 'flac'] },
            { name: 'All Files', extensions: ['*'] },
        ],
    });
    if (result.canceled || result.filePaths.length === 0) {
        log('[Main] open-file-dialog canceled');
        return null;
    }
    log('[Main] open-file-dialog selected: ' + result.filePaths[0]);
    return result.filePaths[0];
});

ipcMain.handle('show-message-box', async (_, options) => {
    log('[Main] IPC show-message-box');
    return dialog.showMessageBox(settingsWindow || null, options);
});

ipcMain.on('close-settings-window', () => {
    log('[Main] IPC close-settings-window');
    if (settingsWindow && !settingsWindow.isDestroyed()) {
        settingsWindow.hide();
    }
});

// Phase 2: IPC handlers for audio file manager (used by settings window in Phase 3)
ipcMain.handle('add-audio-file', async (_, sourcePath) => {
    if (!audioFileManager) return false;
    const ok = audioFileManager.addFile(sourcePath);
    log('[Main] add-audio-file: ' + sourcePath + ' -> ' + ok);
    return ok;
});
ipcMain.handle('delete-audio-file', async (_, fileName) => {
    if (!audioFileManager) return false;
    const ok = audioFileManager.deleteFile(fileName);
    log('[Main] delete-audio-file: ' + fileName + ' -> ' + ok);
    return ok;
});
ipcMain.handle('get-audio-files', async () => {
    if (!audioFileManager) return [];
    return audioFileManager.getAllFiles();
});
ipcMain.handle('get-audio-file-path', async (_, fileName) => {
    if (!audioFileManager) return null;
    return audioFileManager.getFullPath(fileName);
});

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

app.on('before-quit', () => {
    app.isQuitting = true;
    stopKeyListener();
    if (tray && !tray.isDestroyed()) {
        tray.destroy();
        tray = null;
    }
    if (audioWindow && !audioWindow.isDestroyed()) {
        audioWindow.close();
    }
    if (settingsWindow && !settingsWindow.isDestroyed()) {
        settingsWindow.removeAllListeners('close');
        settingsWindow.close();
    }
    log('[Main] App quitting');
});
