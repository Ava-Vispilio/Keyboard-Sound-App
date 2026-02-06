'use strict';

const fs = require('fs');
const path = require('path');

let logPath = null;

function getLogPath() {
    if (logPath) return logPath;
    try {
        const { app } = require('electron');
        if (!app.isReady()) return null;
        const userData = app.getPath('userData');
        logPath = path.join(userData, 'debug.log');
        return logPath;
    } catch {
        return null;
    }
}

function formatTimestamp() {
    const now = new Date();
    return now.toISOString().replace('T', ' ').slice(0, 23);
}

function log(message) {
    const ts = formatTimestamp();
    const line = `[${ts}] ${message}`;
    console.log(line);
    try {
        const p = getLogPath();
        if (p) {
            const dir = path.dirname(p);
            if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
            fs.appendFileSync(p, line + '\n');
        }
    } catch (err) {
        console.error('Logger failed:', err.message);
    }
}

function logError(message, err = null) {
    log(`ERROR: ${message}`);
    if (err) {
        log(`  ${err.name}: ${err.message}`);
        if (err.stack) log(`  ${err.stack}`);
    }
}

module.exports = { log, logError };
