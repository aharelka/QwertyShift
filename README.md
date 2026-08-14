
# QwertyShift ⌨️🔊

### Know your keyboard layout without looking at your screen.

**QwertyShift** is a lightweight Windows utility that tells you which keyboard layout is active — using **sound or voice**.

No more typing an entire paragraph in `English` when you meant `Русский`.
No more looking at the taskbar.
No more discovering the mistake after hitting **Enter**.

> **Your keyboard can speak. Let it.**

[![Windows](https://img.shields.io/badge/Windows-11%20%7C%2010-0078D4?logo=windows\&logoColor=white)](#requirements)
[![C%23](https://img.shields.io/badge/C%23-.NET-512BD4?logo=csharp\&logoColor=white)](#technology)
[![License](https://img.shields.io/badge/license-MIT-green)](#license)

---

## 🎧 What does QwertyShift do?

Imagine this:

You start typing without looking at the screen.

You type:

```text
Ghbdtn! Rfr ltkf?
```

You look up.

**Oh no.**

You meant:

```text
Привет! Как дела?
```
QwertyShift is designed to prevent exactly this situation.
When your keyboard layout changes, QwertyShift can announce the new layout using a short sound or a spoken message.
So instead of checking the taskbar...

**you just hear it.**

---

## ✨ Features

### 🔊 Sound feedback

Prefer something subtle?
Assign a `.wav` sound to each keyboard layout.
For example:

```text
🇷🇺 Russian  →  short low beep
🇬🇧 English  →  short high beep
🇫🇷 French   →  custom sound
```

After a while, you may recognize your layout without consciously thinking about it.

---

### 🗣️ Voice announcements

QwertyShift can speak the name of the active keyboard layout.

For example:

> **"Russian"**

or:

> **"English"**

or whatever name you choose.

---

### 🧠 Smart typing awareness

QwertyShift is designed not to constantly interrupt you while you type.
It can wait until your typing activity has paused before announcing the layout.
That means you don't get:

```text
ENGLISH
ENGLISH
ENGLISH
ENGLISH
ENGLISH
```
every time you press a key.

The goal is simple:

> **Useful feedback without becoming another distraction.**

---

### 🪟 Windows 11-style interface

QwertyShift uses a clean Windows desktop interface designed to feel at home on modern Windows.

No browser window.
No account.
No cloud service.

Just a small utility that does one thing.

---

### 📌 Lives quietly in the system tray

QwertyShift is designed to stay out of your way.
Launch it, minimize it to the tray, and keep working.
You don't need to keep a window open.

---

### 🚀 Starts with Windows

Enable automatic startup and QwertyShift will be ready whenever you start Windows.
No need to remember to launch it manually.

---


## Screenshots

![Main Interface](QwertyShift/screenshot_main.png)


## Installation & Usage

### Option 1 — Download QwertyShift

The easiest way to get started.

1. Open the **[Releases](../../releases)** page.
2. Download the latest `QwertyShift.zip`.
3. Extract the archive.
4. Run `QwertyShift.exe`.

That's it.
QwertyShift is a small desktop utility and does not require a complicated installation process.


### Option 2 — Build from source

Clone the repository:

```bash
git clone https://github.com/aharelka/QwertyShift.git
cd QwertyShift
```
Open the solution in Visual Studio and build the project.

---

## ⚙️ Requirements

* Windows 10 or Windows 11
* A keyboard layout configured in Windows
* For development: Visual Studio with the required .NET Framework tooling

---

## 🔐 Privacy

QwertyShift is a **local desktop application**.
It does not need an online account or cloud service to perform its core function.
Keyboard-layout detection and audio feedback happen locally on your computer.

---
## 💡 Why does this exist?

Because the problem is stupidly small.
And stupidly annoying.
Most of us have done this:

```text
Start typing
     ↓
Keep typing
     ↓
Keep typing
     ↓
Look at the screen
     ↓
"OH COME ON."
```

QwertyShift was born from a simple idea:

> **What if the computer told you immediately?**

Not with another notification.
Not with another popup.
Not with another icon competing for your attention.

Just a tiny sound.
Or a voice.
And then you can keep typing.

---

## ❤️ Open Source

QwertyShift is open source because small ideas are worth sharing.
If you find it useful:

⭐ **Give the project a Star.**

It helps the project get discovered by other people who have spent far too much time typing in the wrong keyboard layout.
If you have an idea, find a bug, or want to improve something:

* Open an [Issue](../../issues)
* Submit a [Pull Request](../../pulls)
* Fork the project and experiment

Even a small improvement is welcome.

---

## 🤝 Contributing

Contributions are welcome.
