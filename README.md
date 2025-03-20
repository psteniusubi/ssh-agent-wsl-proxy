# SSH agent WSL proxy

## Overview

SSH agent WSL proxy allows forwaring ssh-agent requests from WSL to an OpenSSH SSH Agent running on the Windows host.
This way SSH private keys can be stored on the Windows host only. There is no need to copy SSH private keys to WSL.

https://github.com/psteniusubi/ssh-agent-wsl-proxy

## How this works

It is possible to [launch Windows executables](https://learn.microsoft.com/en-us/windows/wsl/filesystems#run-windows-tools-from-linux) from inside WSL. The Windows executable runs in the context of the Windows logged in user. WSL comminucates with the Windows executable using stdin/stdout.

The Linux side WSL proxy executable `ssh-agent-wsl-proxy` listens on a Unix socket `$SSH_AUTH_SOCK`. When the socket is connected, by `ssh` command for example, WSL proxy launches the Windows side WSL proxy executable `ssh-agent-wsl-proxy.exe`. The Windows side WSL proxy then connects to the named pipe `openssh-ssh-agent`of the OpenSSH agent.

## Microsoft OpenSSH

Windows:

```
winget install Microsoft.OpenSSH.Beta
```

### Enable and start ssh-agent service

Windows:

```
sc config ssh-agent start= auto
sc start ssh-agent
```

### Add private keys to OpenSSH agent

Select your own key files for ssh-add.exe

Windows:

```
ssh-add.exe .ssh\agent\id_rsa .ssh\agent\id_ed25519
```

Use *Task Scheduler* to make `ssh-add.exe` run *At log on*

### Test OpenSSH agent

Windows:

```
ssh-add.exe -L
```

The command should output a list of SSH keys on Windows OpenSSH agent

## Download Binaries

WSL/Linux:

```
cd $HOME
wget https://github.com/psteniusubi/ssh-agent-wsl-proxy/releases/download/v0.0.2/ssh-agent-wsl-proxy-v0.0.2.tar.gz
rm -rf $HOME/ssh-agent-wsl-proxy
tar -zxf ssh-agent-wsl-proxy-v0.0.2.tar.gz
```

Contents of ~/ssh-agent-wsl-proxy

* ssh-agent-wsl-proxy.service
    * systemd user service
* linux-x64/ssh-agent-wsl-proxy
    * WSL/Linux side executable
* win-x64/ssh-agent-wsl-proxy.exe
    * Windows side executable

## Build from Source

### Checkout

WSL:

```
mkdir ~/src
cd ~/src
git clone https://github.com/psteniusubi/ssh-agent-wsl-proxy 
```

### Compile

WSL:

```
cd ~/src/ssh-agent-wsl-proxy 
dotnet publish ssh-agent-wsl-proxy --sc=true -r linux-x64 -c Release -o Publish/linux-x64 /property:PublishSingleFile=true
dotnet publish ssh-agent-wsl-proxy --sc=true -r win-x64 -c Release -o Publish/win-x64 /property:PublishSingleFile=true
```

### Deploy

WSL:

```
rm -rf $HOME/ssh-agent-wsl-proxy
mkdir -p $HOME/ssh-agent-wsl-proxy
tar -C Publish -cf - . | tar -C $HOME/ssh-agent-wsl-proxy -xf -
cp ssh-agent-wsl-proxy.service $HOME/ssh-agent-wsl-proxy/
```

Contents of ~/ssh-agent-wsl-proxy

* ssh-agent-wsl-proxy.service
    * systemd user service
* linux-x64/ssh-agent-wsl-proxy
    * WSL/Linux side executable
* win-x64/ssh-agent-wsl-proxy.exe
    * Windows side executable

## Install

### $HOME/.profile

Add following to `~/.profile`

```
SSH_AUTH_SOCK="${XDG_RUNTIME_DIR}/ssh-agent.socket"
export SSH_AUTH_SOCK
```

### Enable SystemD user service

WSL:

```
sudo systemctl restart user@$(id -u).service
```

### Create and start ssh-agent-wsl-proxy.service

WSL:

```
systemctl --user link $HOME/ssh-agent-wsl-proxy/ssh-agent-wsl-proxy.service
systemctl --user enable ssh-agent-wsl-proxy.service
systemctl --user daemon-reload
systemctl --user start ssh-agent-wsl-proxy.service
```

### Check SystemD status

WSL:

```
systemctl --user status ssh-agent-wsl-proxy.service
```

You should see the following output

```
ssh-agent-wsl-proxy[673]: listening on /run/user/1000/ssh-agent.socket
```

### Check SSH agent

WSL:

```
ssh-add -L
```

The command should output a list of SSH keys on Windows OpenSSH agent
