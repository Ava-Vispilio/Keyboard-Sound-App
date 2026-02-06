# Keyboard Sound App v2 – Iterative Rebuild Steps

Reference for rebuilding the Electron app one feature at a time, with verification at each step.

---

## Phase 0: Project Setup & Debug Tools

| Step | Task | Deliverable | Verify |
|------|------|-------------|--------|
| 0.1 | Create project: `npm init`, add `electron`, `electron-store`, `electron-builder` | `package.json` with `main`, scripts (`start`, `build:mac`, `build:win`) | `npm start` opens blank Electron window |
| 0.2 | Create `main.js` – basic `app.whenReady`, create minimal visible window | App launches, window appears | Window shows |
| 0.3 | Create `utils/logger.cjs` – file logger to `userData/debug.log` (mirror v1 Logger.cs) | `log()`, `logError()` | Entries appear in log file |
| 0.4 | Wire logger into main process, replace `console.log` with logger | All main logs go to file | Logs visible in terminal + file |
| 0.5 | Add DevTools shortcut: `before-input-event` → Cmd+Option+I (mac) / Ctrl+Shift+I (win) to toggle DevTools | Shortcut toggles DevTools | Shortcut works when window focused |

---

## Phase 1: Configuration & Tray (No Windows Yet)

| Step | Task | Deliverable | Verify |
|------|------|-------------|--------|
| 1.1 | Add `electron-store` with `defaultAudioFile: ''`, `isEnabled: true` | `store.get/set` work | Log store values on startup |
| 1.2 | Create tray icon (from `assets/icons/icon.png`), menu: "Exit" only | Tray icon visible, Exit quits | Tray icon appears, Exit works |
| 1.3 | Add menu items "Settings" (placeholder), "Enable"/"Disable" (reads `isEnabled`), "Exit" | Tray menu with all items | Menu reflects `isEnabled` |
| 1.4 | Implement Enable/Disable toggle: update store, log, refresh menu | Toggle works | Log shows store updates, menu label updates |
| 1.5 | Add single instance lock; second instance focuses first instance's window | Only one app runs | Second `npm start` focuses existing window |
| 1.6 | macOS: `app.dock.hide()` for tray-only (optional) | App in menu bar only | Dock icon hidden |

---

## Phase 2: Audio File Manager

| Step | Task | Deliverable | Verify |
|------|------|-------------|--------|
| 2.1 | Create `utils/audio-file-manager.cjs`: `ensureStorageExists()`, storage path = `userData/AudioFiles` | Storage directory created | Directory exists under userData |
| 2.2 | Add `validateAudioFile(path)` – check extension (mp3,wav,wma,m4a,aac,ogg,flac) | Validation function | Returns true/false for test paths |
| 2.3 | Add `addFile(sourcePath)` – copy to storage, handle duplicates (append " (1)"), validate | Files copied | Files appear in userData/AudioFiles |
| 2.4 | Add `deleteFile(fileName)`, `getAllFiles()`, `getFullPath(fileName)` | Full CRUD API | List/delete work correctly |
| 2.5 | Wire file manager in main; add IPC handlers: `add-audio-file`, `delete-audio-file`, `get-audio-files`, `get-audio-file-path` | Main process can manage files | Invoke handlers via test script or DevTools |

---

## Phase 3: Settings Window (UI Only)

| Step | Task | Deliverable | Verify |
|------|------|-------------|--------|
| 3.1 | Create `preload.cjs` with `contextBridge` – expose: `getStore`, `set`, `openFileDialog`, `showMessageBox`, `addAudioFile`, `deleteAudioFile`, `getAudioFiles`, `getAudioFilePath`, `closeSettingsWindow`, `onSettingUpdate` | Preload API defined | Call from renderer, no errors |
| 3.2 | Create `settings.html` – layout: Enable checkbox, file list, Add/Delete/Set Default/Close buttons, status area | Basic UI | All elements visible |
| 3.3 | Create `renderer/settings.js` – `loadFileList()` from `getAudioFiles()`, render list | List shows files | Add file via IPC, list updates |
| 3.4 | Add File button: `openFileDialog`, then `addAudioFile(path)`, refresh list | Add File works | New files appear in list |
| 3.5 | Delete button: select item, confirmation via `showMessageBox`, `deleteAudioFile`, refresh; if deleted was default, clear `defaultAudioFile` | Delete works | Confirm dialog, file removed, default cleared if needed |
| 3.6 | Set as Default: select item, `set('defaultAudioFile', name)`, refresh list, mark default | Default selection works | Default file highlighted |
| 3.7 | Enable checkbox: `set('isEnabled', checked)`; Close button: `closeSettingsWindow()` | Enable and Close work | Checkbox updates store, Close hides window |
| 3.8 | Load settings on open; subscribe `onSettingUpdate` for `isEnabled` and `defaultAudioFile` | UI stays in sync with store | Changes from tray update settings UI |
| 3.9 | Create settings window with `show: false`, load `settings.html`, show on first launch and from tray "Settings" | Window behavior correct | First launch shows window, tray opens it again |

---

## Phase 4: Custom Protocol for Audio Files

| Step | Task | Deliverable | Verify |
|------|------|-------------|--------|
| 4.1 | Register `app-audio://` scheme with `protocol.handle`; serve from `userData/AudioFiles` by filename | Protocol serves files | `fetch('app-audio://audio/filename.mp3')` returns bytes |
| 4.2 | Add `getAudioFileUrl(fileName)` in preload – returns `app-audio://audio/${encodeURIComponent(fileName)}` | Renderer gets URL | URL resolves for known file |

---

## Phase 5: Audio Playback (No Keyboard Yet)

| Step | Task | Deliverable | Verify |
|------|------|-------------|--------|
| 5.1 | Create `renderer/audio.js`: Web Audio `AudioContext`, load default file from `getAudioFileUrl`, decode to `AudioBuffer` | Buffer loaded | Log success/failure of load |
| 5.2 | Add `play()` – create `AudioBufferSourceNode`, connect to `destination`, `start(0)` | Single play works | Call `play()` manually from DevTools, hear sound |
| 5.3 | Subscribe to `onSettingUpdate('defaultAudioFile')` – reload buffer when default changes | Default changes update buffer | Change default in UI, reload, play works |
| 5.4 | Subscribe to `onSettingUpdate('isEnabled')` – keep store in sync; only play when `isEnabled` | Play respects enabled flag | Enable/disable affects playback |

---

## Phase 6: Native Key Listeners

| Step | Task | Deliverable | Verify |
|------|------|-------------|--------|
| 6.1 | Add `mac-key-listener.swift` – CGEvent tap, keyDown only, output `{"keydown": keyCode}` per line | Binary runs, prints JSON | Run manually, press keys, see JSON |
| 6.2 | Add `win-key-listener.cpp` – `WH_KEYBOARD_LL`, WM_KEYDOWN/WM_SYSKEYDOWN, same JSON format | Same behavior on Windows | Same manual test |
| 6.3 | Add build scripts: `build:mac-listener`, `build:win-listener` | Binaries build | Binaries produced in `libs/key-listeners/` |
| 6.4 | (Optional) Filter modifier-only keys in listeners | Cleaner behavior | Modifier-only presses don't emit |

---

## Phase 7: Integrate Key Capture → Playback

| Step | Task | Deliverable | Verify |
|------|------|-------------|--------|
| 7.1 | In main: `getListenerPath()` – dev = `libs/key-listeners/`, packaged = `process.resourcesPath` | Path logic correct | Log path, confirm binary exists |
| 7.2 | `startKeyListener()` – spawn listener, parse stdout line-by-line as JSON, log each `keydown` | Main receives keydown | Log shows keydown events |
| 7.3 | Add IPC channel for keydown – main sends `keydown` with `{ keycode }` to renderer | IPC wired | Renderer logs received events |
| 7.4 | Expose `onKeyDown(callback)` in preload; in `audio.js` call `play()` on keydown | Playback on keypress | Press keys, hear sound |
| 7.5 | Start listener only when `isEnabled`; stop on disable; `stopKeyListener()` on quit | Lifecycle correct | Disable stops sound; quit stops listener |

---

## Phase 8: macOS Permission Gate (macOS Only)

| Step | Task | Deliverable | Verify |
|------|------|-------------|--------|
| 8.1 | Add `verifyAccessibilityPermission()` – spawn listener briefly, interpret exit code | Permission check works | Returns true when permitted |
| 8.2 | On first run (macOS, no permission): show `permission.html` with instructions and "Open System Settings" | Permission UI shown | First run shows permission screen |
| 8.3 | Add "Verify Permission" button – re-run check, then load `settings.html` if granted | Flow works | After granting, settings load |

---

## Phase 9: Polish & Cleanup

| Step | Task | Deliverable | Verify |
|------|------|-------------|--------|
| 9.1 | Clean shutdown: `before-quit` – stop listener, destroy tray, close windows | No lingering processes | Quit cleanly, no zombies |
| 9.2 | Add renderer logging (`[Renderer]` prefix) for key events, playback, store updates | Logs trace flow | Follow full flow in log file |
| 9.3 | `window-all-closed` – on macOS keep app running; on Windows quit if all windows closed | macOS keeps running | Behavior correct |
| 9.4 | Configure electron-builder for packaging | Builds produce installers | DMG (mac) / installer (win) |

---

## Debugging Reference

| Tool | Location / Command |
|------|--------------------|
| **Log file** | macOS: `~/Library/Application Support/Keyboard Sound App/debug.log` |
| | Windows: `%APPDATA%\Keyboard Sound App\debug.log` |
| **DevTools** | Cmd+Option+I (mac) / Ctrl+Shift+I (win) when settings window focused |
| **Renderer checks** | Ensure `window.api` exists; call `api.getStore()`, `api.getAudioFiles()` |
| **Listener check** | Run listener binary directly; press keys; confirm JSON output |
| **Main process** | Log each parsed keydown line before sending IPC |

---

## Recommended Order

1. Phase 0 (setup + debug)
2. Phase 1 (config + tray)
3. Phase 2 (file manager)
4. Phase 3 (settings UI)
5. Phase 4 (audio protocol)
6. Phase 5 (audio playback)
7. Phase 6 (key listeners)
8. Phase 7 (integration)
9. Phase 8 (macOS permission)
10. Phase 9 (polish)
