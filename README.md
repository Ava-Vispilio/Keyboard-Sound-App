# Keyboard App!

## Purpose

This plays a sound whenever a key is pressed - that's it.
Sounds are NOT provided (although you can upload as many as you want and choose a default)

## How to use

1. Navigate to `Releases` and download `publish.zip` for v0.1.0
2. Make sure you have `.NET 8.0` and above installed (Windows should have this by default)
3. Unzip it and run `KeyboardSoundApp.exe`
4. Select a sound, pick a default, then close the app (it will minimize to the Tray)
5. Enjoy!

## Notes
- Most audio file formats should be supported
- Feel free to make a pull request for contributions!

## Debug logs (v2 Electron app)

The v2 app (KeyboardSoundApp-v2) writes logs to `debug.log` in its userData folder. Dev (`npm start`) and the packaged app use different folders.

**Clear both logs**
```bash
rm ~/Library/Application\ Support/Keyboard\ Sound\ App/debug.log
rm ~/Library/Application\ Support/keyboard-sound-app/debug.log
```

**View both logs**
```bash
cat ~/Library/Application\ Support/Keyboard\ Sound\ App/debug.log
cat ~/Library/Application\ Support/keyboard-sound-app/debug.log
```

**Clear then view (e.g. before a packaged run)**
```bash
rm -f ~/Library/Application\ Support/Keyboard\ Sound\ App/debug.log ~/Library/Application\ Support/keyboard-sound-app/debug.log
# run packaged app, then:
cat ~/Library/Application\ Support/Keyboard\ Sound\ App/debug.log
```

**View both (dev first, then packaged)**
```bash
echo "=== Dev (keyboard-sound-app) ===" && cat ~/Library/Application\ Support/keyboard-sound-app/debug.log
echo "=== Packaged (Keyboard Sound App) ===" && cat ~/Library/Application\ Support/Keyboard\ Sound\ App/debug.log
```
