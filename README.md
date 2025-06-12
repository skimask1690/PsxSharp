# ⚙️ PsxSharp

**PsxSharp** is a C# utility that spawns a LocalSystem (`NT AUTHORITY\SYSTEM`) shell by duplicating a SYSTEM process token.  
It acts as a lightweight, open-source alternative to PsExec, without services, remote handles, or EULA prompts.

## 🔍 Features

- Launches a process as `NT AUTHORITY\SYSTEM` without:
  - Installing a service
  - Opening remote process handles
  - Accepting a EULA

## 🚀 Usage

### 🔧 Compile

```sh
csc.exe /debug- /optimize+ /t:winexe /nologo PsxSharp.cs
```

### ▶️ Run

```sh
PsxSharp.exe [command]
```

- `[command]` *(optional)*: The command to run as SYSTEM (default: `cmd`)

### 💡 Example

```sh
PsxSharp.exe cmd /k whoami
```

## 📜 Notes

- Must be run **as Administrator**
- Works only on **local** machine

## 🧠 How It Works

1. Enumerates all running processes
2. Identifies one running as `NT AUTHORITY\SYSTEM`
3. Calls `OpenProcessToken` and `DuplicateTokenEx` to duplicate the SYSTEM token
4. Invokes `CreateProcessWithTokenW` to spawn a new process with that token
5. Defaults to spawning `cmd.exe` if no command is specified

---

## ⚠️ Disclaimer

This tool is provided for **educational and authorized security research** purposes only.  
**Do not** use it on machines you do not own or have explicit permission to test.  
The author assumes **no liability** for misuse or damage.

---

## 📜 License

This project is released under the [MIT License](LICENSE).
