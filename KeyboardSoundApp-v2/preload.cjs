'use strict';

const { contextBridge, ipcRenderer } = require('electron');

console.log('[Preload] Running, exposing api');

contextBridge.exposeInMainWorld('api', {
    getStore: () => ipcRenderer.sendSync('get-store'),
    set: (key, value) => ipcRenderer.invoke('store-set', key, value),
    openFileDialog: () => ipcRenderer.invoke('open-file-dialog'),
    showMessageBox: (options) => ipcRenderer.invoke('show-message-box', options),
    addAudioFile: (sourcePath) => ipcRenderer.invoke('add-audio-file', sourcePath),
    deleteAudioFile: (fileName) => ipcRenderer.invoke('delete-audio-file', fileName),
    getAudioFiles: () => ipcRenderer.invoke('get-audio-files'),
    getAudioFilePath: (fileName) => ipcRenderer.invoke('get-audio-file-path', fileName),
    getAudioFileUrl: (fileName) => {
        if (!fileName) return null;
        return 'app-audio://audio/' + encodeURIComponent(fileName);
    },
    closeSettingsWindow: () => ipcRenderer.send('close-settings-window'),
    onSettingUpdate: (key, callback) => {
        const channel = 'updated-' + key;
        ipcRenderer.on(channel, (_, value) => callback(value));
        return () => ipcRenderer.removeAllListeners(channel);
    },
    onKeyDown: (callback) => {
        ipcRenderer.on('keydown', (_, payload) => callback(payload));
        return () => ipcRenderer.removeAllListeners('keydown');
    },
    logToMain: (msg) => ipcRenderer.send('renderer-log', msg),
});
