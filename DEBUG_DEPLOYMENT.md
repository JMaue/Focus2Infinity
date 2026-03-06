# Debugging deployment when the app does not start

If the app never starts and **no bootstrap log file is written** (not in the app folder, not in temp), the failure happens **before any of your code runs**. In-app logging cannot help. You must use **external** diagnostics only.

---

## HTTP Error 500.30 (IIS: “ANCM In-Process Start Failure”)

**What it means:** The app is being started by IIS, but it **crashes or fails during startup**. IIS only shows “500.30”; the **real** error is written elsewhere.

**Get the real error in one of these two ways:**

### A. Windows Event Viewer (quickest)

1. On the server, open **Event Viewer** → **Windows Logs** → **Application**.
2. Look for a **red** entry at the time you loaded the site. Source is often **“IIS AspNetCore Module V2”** or **“.NET Runtime”**.
3. Open it and read the message. You’ll usually see the real exception (e.g. missing `account` file, missing DLL, config error).

### B. Enable stdout logging (detailed startup log)

1. On the server, open the **web.config** in the site’s physical folder (next to `Focus2Infinity.dll`).
2. Find the `<aspNetCore ... />` element. Add or set these two attributes:
   - `stdoutLogEnabled="true"`
   - `stdoutLogFile=".\logs\stdout"`
   (If the element is split over several lines, add them to the same `<aspNetCore>` tag.)
3. Ensure the app pool identity can write to the site folder (so the `logs` folder can be created). Default app pool user is `IIS AppPool\YourAppPoolName`.
4. Restart the site or app pool (recycle in IIS).
5. Reproduce the 500.30 (e.g. open the site in a browser).
6. In the site folder, open the **logs** folder. You’ll see one or more files like `stdout_*.log`. Open the newest; it contains the .NET exception and stack trace from startup.

**Typical causes of 500.30:**

- **Missing `account` file** in the site folder → copy it next to `Focus2Infinity.dll`.
- **Missing or wrong .NET runtime** → install [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) (for IIS).
- **Missing DLL** in the publish folder → redeploy or fix the build so all dependencies are published.
- **Config or code throwing during startup** → the Event Viewer or stdout log will show the exact exception.

After you’ve fixed the issue, you can set `stdoutLogEnabled="false"` again (or remove it) so logs don’t grow indefinitely.

---

## HTTP Error 500.32 (IIS: “Failed to load .NET Core host” / bitness mismatch)

**What it means:** The app and the IIS worker process (**w3wp.exe**) are built for **different architectures**: one is 32-bit, the other 64-bit. They must match.

**Fix: make the app pool match the app**

This project is built as **Any CPU** and runs as **64-bit** on a 64-bit server by default. So the app pool must be **64-bit**.

1. On the server, open **IIS Manager** → **Application Pools**.
2. Select the app pool used by your site (e.g. *DefaultAppPool* or the one your site is assigned to).
3. In the right-hand **Actions** pane, click **Advanced Settings**.
4. Find **General** → **Enable 32-Bit Applications**.
5. Set it to **False** (so the pool runs as 64-bit).
6. Click **OK**, then **recycle** the app pool (right‑click → Recycle).

Try the site again. If you still get 500.32, confirm the server is 64-bit and that you didn’t publish with a 32-bit runtime (e.g. `win-x86`). If you intentionally use a 32-bit app pool (e.g. for a 32-bit dependency), you must **publish the app for 32-bit** (e.g. `dotnet publish -r win-x86`) and keep **Enable 32-Bit Applications** = **True** for that pool.

---

## 1. Run the minimal test app on the server (isolate runtime vs app)

The solution includes **DeployTest**, a tiny .NET 8 console app with no extra dependencies. It only writes two files and prints to the console.

**On your dev machine:**

```bash
cd d:\Jens\Repositories\Focus2Infinity
dotnet publish DeployTest\DeployTest.csproj -c Release -o .\DeployTest\publish
```

Copy the **contents** of `DeployTest\publish` to the **server** (same place you’d run Focus2Infinity from, or any folder).

**On the server**, in that folder:

```bash
dotnet DeployTest.dll
```

- **If this fails:** The message in the console (or in Windows Event Viewer → Application) is the real cause. Typical: missing .NET 8 runtime, wrong path, or permission. Fix that first (e.g. install .NET 8 runtime).
- **If this works:** You’ll see “OK” and two files created (`DeployTest-ok.txt` in temp and in the current folder). Then the environment can run .NET and write files; the problem is specific to the Focus2Infinity app (e.g. a missing DLL or dependency).

---

## 2. Run Focus2Infinity from the command line on the server

On the server, open a shell and go to the **exact folder** where the deployed app runs (same as IIS app path, or where the service runs):

```bash
cd "C:\path\to\your\published\Focus2Infinity"
dotnet Focus2Infinity.dll
```

Watch the console. You should see either:

- The app starting and listening (then the problem is how the host starts it), or  
- An **error message** (missing framework, missing DLL, “account file not found”, etc.). That message is the cause.

Do **not** close the window; leave it open to read the output. If the process exits immediately, the last lines are usually the error.

---

## 3. Check host and platform logs (no app log = look here)

When no log file from the app is written, the only place the error can appear is in whatever starts or hosts the process.

| Environment | Where to look |
|-------------|----------------|
| **Windows** | **Event Viewer** → Windows Logs → **Application**. Look for entries from “.NET Runtime”, “ASP.NET Core”, or your app name. Note the exact error and event ID. |
| **IIS** | Same as above; also enable **stdout** logging. In the site’s `web.config`, set `<aspNetCore ... stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" ... />` and ensure the app pool identity can write to that folder. Restart the app and check the stdout log. |
| **Linux (systemd)** | `journalctl -u your-service-name -n 200 --no-pager` |
| **Docker** | `docker logs <container>` |
| **Azure App Service** | Log stream in the portal; enable “Application Logging” and “Detailed errors”. |

Typical messages you might see:

- *“The framework 'Microsoft.NETCore.App', version '8.0.0' was not found”* → Install .NET 8 runtime (or match the version your app targets).
- *“Could not load file or assembly ...”* → A dependency DLL is missing in the publish folder; check deployment.
- Process exits with a non‑zero code → Use the exit code and the last log line to search for the cause.

---

## 4. Checklist when no log file is written

1. Run **DeployTest** on the server (step 1). If it fails, fix the environment (runtime, path, permissions) first.
2. Run **Focus2Infinity** from the command line on the server (step 2). Note the exact error.
3. Open **Event Viewer** (or your host’s log) and look for errors at the time you started the app (step 3).
4. Confirm the **deployed folder** is the one you’re running from and that it contains `Focus2Infinity.dll`, the `account` file, and all dependencies.

In-app bootstrap logs (e.g. `startup-bootstrap.log`, `Focus2Infinity-bootstrap.log`) only appear **after** the process has loaded your assembly. If no such file is written, the failure is before that; use the steps above to find the real error outside the app.
