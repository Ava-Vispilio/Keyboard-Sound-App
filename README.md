# Keyboard App!

## Purpose

This plays a sound whenever a key is pressed - that's it.
Sounds are NOT provided (although you can upload as many as you want and choose a default)

## How to use

1. Go to the **Releases** section on this GitHub repo (on the right side of the page)
2. Download the right file for your system (`.dmg` for MacOS, `.exe` for Windows)
3. Open the app. The first time you run it, the Settings window will open!
4. Add a sound (in **Add File**), set one as default, then close the window (the app stays running in the menu bar on Mac or taskbar on Windows)
5. ???
6. Profit

> Note for Mac users: macOS will ask for Accessibility (or Input Monitoring) permission so the app can detect your keypresses. This is normal and required for the app to work. You can turn the permission on in System Settings → Privacy & Security → Accessibility (or Input Monitoring)

## Bug Report(s)

The app writes a small log file that helps me fix issues. If something isn’t working, you can send us this log when you report the bug

### Where is the log file?

- Mac(OS):
  Open Finder → press `Cmd+Shift+G` (Go to Folder), paste `~/Library/Application Support/Keyboard Sound App` and press Enter    
  The log file is named `debug.log`

- Windows:
  Press `Win+R` to open Run, then type `%APPDATA%\Keyboard Sound App` and press Enter  
  The log file is named `debug.log`

### How to send in a bug report

1. Experience a bug (I am sorry in advance!)
2. Take note of whatever caused the bug
3. Open the `debug.log` file with a text editor (e.g. Notepad on Windows, TextEdit on Mac)
4. Select all the text (`Ctrl+A` or `Cmd+A`) and copy it (I believe I do not need to specify the shortcut for this)
5. In this repo, click **Issues** (top of the page), then **New issue**
6. Pick a type (Bug report in this case), add a clear title, then DESCRIBE what happened (you know, from Step 2) and PASTE THE DEBUG LOG INSIDE PLEASE
7. If you want to make my life easier you can put the contents of the debug log in a code block (use the **<>** button in the issue editor and paste inside)

## Building from source

> Builds are done from the **`KeyboardSoundApp-v2`** folder. The app uses **native key listeners** (one for macOS, one for Windows) that must be built for the platform you are targeting before you run the Electron packager

### What you need

- **Node.js** and **npm** (install from [nodejs.org](https://nodejs.org))
- **macOS build:** Apple’s Swift compiler (to install Xcode Command Line Tools, run `xcode-select --install` in Terminal)
- **Windows build:** A C++ compiler (good luck with this one!)

### Build steps

1. Open Terminal and go into the project:  
   `cd KeyboardSoundApp-v2`
2. Install dependencies:  
   `npm install`
3. Build the key listener (required):  
   ```shell
   npm run build:mac-listener
   # or
   npm run build:win-listener
   ```
4. Build the app:  
   ```shell
   npm run build:mac
   # or
   npm run build:win
   ```   

> After a successful build, output goes into the `exports` folder inside `KeyboardSoundApp-v2` (look for your `.exe.` or `.dmg` there!)

## Notes
- Most audio file formats should be supported (e.g. mp3, wav, ogg, m4a).
- Feel free to make a pull request for contributions!

## Credits

Credit goes to [@joshxviii](https://github.com/joshxviii) for the v2 version of the app. The native key listeners used in this project are also loosely based off of the ones on their [Animalese Typing](https://github.com/joshxviii/animalese-typing-desktop) desktop app.

As a result, this project inherits the same MIT license as the original project