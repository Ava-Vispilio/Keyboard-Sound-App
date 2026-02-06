#!/usr/bin/env swift
// Keyboard Sound App – macOS key listener (simplified: every keydown → one JSON line).
// Build: swiftc -o keyboard-sound-listener mac-key-listener.swift
// Requires: Accessibility permission (Input Monitoring).

import Foundation
import CoreGraphics
import ApplicationServices

let keyDownMask = CGEventMask(1 << CGEventType.keyDown.rawValue)

func keyTapCallback(proxy: CGEventTapProxy, type: CGEventType, event: CGEvent, refcon: UnsafeMutableRawPointer?) -> Unmanaged<CGEvent>? {
    if type == .keyDown {
        let keyCode = event.getIntegerValueField(.keyboardEventKeycode)
        print("{\"keydown\": \(keyCode)}")
        fflush(stdout)
    }
    return Unmanaged.passUnretained(event)
}

func run() {
    guard let tap = CGEvent.tapCreate(
        tap: .cgSessionEventTap,
        place: .headInsertEventTap,
        options: .defaultTap,
        eventsOfInterest: keyDownMask,
        callback: keyTapCallback,
        userInfo: nil
    ) else {
        fputs("Failed to create event tap. Grant Input Monitoring / Accessibility.\n", stderr)
        exit(1)
    }

    let runLoopSource = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, tap, 0)
    CFRunLoopAddSource(CFRunLoopGetCurrent(), runLoopSource, .defaultMode)
    CGEvent.tapEnable(tap: tap, enable: true)
    CFRunLoopRun()
}

run()
