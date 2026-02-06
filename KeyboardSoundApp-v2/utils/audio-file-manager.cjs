'use strict';

const fs = require('fs');
const path = require('path');

const ALLOWED_EXTENSIONS = ['.mp3', '.wav', '.wma', '.m4a', '.aac', '.ogg', '.flac'];

/**
 * Create an audio file manager. Storage path = userDataPath/AudioFiles.
 * @param {string} userDataPath - e.g. app.getPath('userData')
 * @returns {{ ensureStorageExists, validateAudioFile, addFile, deleteFile, getAllFiles, getFullPath }}
 */
function createAudioFileManager(userDataPath) {
    const storageDir = path.join(userDataPath, 'AudioFiles');

    function ensureStorageExists() {
        if (!fs.existsSync(storageDir)) {
            fs.mkdirSync(storageDir, { recursive: true });
        }
        return storageDir;
    }

    function validateAudioFile(filePath) {
        if (!filePath || typeof filePath !== 'string') return false;
        const ext = path.extname(filePath).toLowerCase();
        return ALLOWED_EXTENSIONS.includes(ext);
    }

    function addFile(sourcePath) {
        try {
            if (!fs.existsSync(sourcePath) || !fs.statSync(sourcePath).isFile()) {
                return false;
            }
            if (!validateAudioFile(sourcePath)) {
                return false;
            }
            ensureStorageExists();
            let fileName = path.basename(sourcePath);
            let destPath = path.join(storageDir, fileName);
            let counter = 1;
            const base = path.basename(fileName, path.extname(fileName));
            const ext = path.extname(fileName);
            while (fs.existsSync(destPath)) {
                fileName = base + ' (' + counter + ')' + ext;
                destPath = path.join(storageDir, fileName);
                counter++;
            }
            fs.copyFileSync(sourcePath, destPath);
            return true;
        } catch (err) {
            return false;
        }
    }

    function deleteFile(fileName) {
        try {
            if (!fileName || path.isAbsolute(fileName) || fileName.includes('..')) {
                return false;
            }
            const filePath = path.join(storageDir, fileName);
            if (fs.existsSync(filePath) && fs.statSync(filePath).isFile()) {
                fs.unlinkSync(filePath);
                return true;
            }
            return false;
        } catch (err) {
            return false;
        }
    }

    function getAllFiles() {
        try {
            ensureStorageExists();
            const names = fs.readdirSync(storageDir);
            return names
                .filter((name) => {
                    const full = path.join(storageDir, name);
                    return fs.statSync(full).isFile() && validateAudioFile(name);
                })
                .sort();
        } catch (err) {
            return [];
        }
    }

    function getFullPath(fileName) {
        if (!fileName || path.isAbsolute(fileName) || fileName.includes('..')) {
            return null;
        }
        const filePath = path.join(storageDir, fileName);
        return fs.existsSync(filePath) && fs.statSync(filePath).isFile() ? filePath : null;
    }

    return {
        ensureStorageExists,
        validateAudioFile,
        addFile,
        deleteFile,
        getAllFiles,
        getFullPath,
    };
}

module.exports = { createAudioFileManager };
