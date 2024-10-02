# SSH agent WSL proxy

## Overview

SSH agent WSL proxy allows forwaring ssh-agent requests from WSL to a OpenSSH SSH Agent running on the Windows host.
This way SSH private keys can be stored on the Windows host only. There is no need to copy SSH private keys to WSL.

https://github.com/psteniusubi/ssh-agent-wsl-proxy

## Microsoft OpenSSH

```
winget install Microsoft.OpenSSH.Beta
```

### Enable and start ssh-agent service

```
sc config ssh-agent start= auto
sc start ssh-agent
```

## Download Binaries

```
cd $HOME
wget https://github.com/psteniusubi/ssh-agent-wsl-proxy/releases/download/v0.0.2/ssh-agent-wsl-proxy-v0.0.2.tar.gz
rm -rf $HOME/ssh-agent-wsl-proxy
tar -zxf ssh-agent-wsl-proxy-v0.0.2.tar.gz
```

## Build from Source

### Checkout

```
mkdir ~/src
cd ~/src
git clone https://github.com/psteniusubi/ssh-agent-wsl-proxy 
```

### Compile

```
cd ~/src/ssh-agent-wsl-proxy 
dotnet publish ssh-agent-wsl-proxy --sc=true -r linux-x64 -c Release -o Publish/linux-x64 /property:PublishSingleFile=true
dotnet publish ssh-agent-wsl-proxy --sc=true -r win-x64 -c Release -o Publish/win-x64 /property:PublishSingleFile=true
```

### Deploy

```
rm -rf $HOME/ssh-agent-wsl-proxy
mkdir -p $HOME/ssh-agent-wsl-proxy
tar -C Publish -cf - . | tar -C $HOME/ssh-agent-wsl-proxy -xf -
cp ssh-agent-wsl-proxy.service $HOME/ssh-agent-wsl-proxy/
```

## Install

### $HOME/.profile

```
SSH_AUTH_SOCK="${XDG_RUNTIME_DIR}/ssh-agent.socket"
export SSH_AUTH_SOCK
```

### Enable SystemD user service

```
sudo systemctl restart user@$(id -u).service
```

### Create and start ssh-agent-wsl-proxy.service

```
systemctl --user link $HOME/ssh-agent-wsl-proxy/ssh-agent-wsl-proxy.service
systemctl --user enable ssh-agent-wsl-proxy.service
systemctl --user daemon-reload
systemctl --user start ssh-agent-wsl-proxy.service
```

### Check SystemD status

```
systemctl --user status ssh-agent-wsl-proxy.service
```

### Check SSH agent

```
ssh-add -L
```
