# GameServer Windows Service Scripts

This folder contains PowerShell scripts for managing the GameServer as a Windows Service.

## Scripts

### 1. Publish-GameServer.ps1
Builds and publishes the GameServer application as a self-contained Windows x64 executable.

**Usage:**
```powershell
.\Publish-GameServer.ps1 -Configuration Release
```

**Output:** Creates a `GameServer-Published` folder in the solution root with all files needed for deployment.

---

### 2. Install-GameServerService.ps1
Installs the GameServer as a Windows Service that runs on startup.

**Requirements:** Must be run as Administrator

**Usage:**
```powershell
# After copying published files to C:\Services\GameServer
.\Install-GameServerService.ps1 -ServicePath "C:\Services\GameServer"
```

**Parameters:**
- `-ServicePath` (required): Path to the folder containing GameServer.exe
- `-ServiceName` (optional): Name of the Windows Service (default: "GameServer")
- `-DisplayName` (optional): Display name in Services console (default: "Game Server")
- `-Description` (optional): Service description (default: "Background service for Game Server")

**What it does:**
- Removes any existing GameServer service
- Creates a new Windows Service
- Sets startup type to "Automatic" (starts on server reboot)
- Configures recovery settings (auto-restart on failure)
- Starts the service

---

### 3. Uninstall-GameServerService.ps1
Removes the GameServer Windows Service.

**Requirements:** Must be run as Administrator

**Usage:**
```powershell
.\Uninstall-GameServerService.ps1
```

**Parameters:**
- `-ServiceName` (optional): Name of the Windows Service to remove (default: "GameServer")

---

## Deployment Workflow

### On Your Development Machine:
1. Run `Publish-GameServer.ps1` to build the application
2. Copy the contents of the `GameServer-Published` folder to your Windows Server

### On Windows Server (as Administrator):
1. Create a folder: `C:\Services\GameServer`
2. Copy all published files into `C:\Services\GameServer`
3. Run `Install-GameServerService.ps1 -ServicePath "C:\Services\GameServer"`
4. Verify the service is running: `Get-Service -Name "GameServer"`

### To Stop/Remove:
```powershell
Stop-Service -Name "GameServer"
.\Uninstall-GameServerService.ps1
```

---

## Monitoring

View the service status:
```powershell
Get-Service -Name "GameServer"
```

View service logs in Windows Event Viewer:
- Open Event Viewer → Applications and Services Logs → Look for your service

For detailed application logging, configure logging in your code (e.g., file logging, event log).
