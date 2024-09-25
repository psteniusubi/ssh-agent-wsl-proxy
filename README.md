# SSH agent WSL proxy

## Install

### Build

```
rm -rf ssh-agent-wsl-proxy/bin/Release

dotnet publish --sc=true -r linux-x64 -c Release ssh-agent-wsl-proxy 
dotnet publish --sc=true -r win-x64 -c Release ssh-agent-wsl-proxy 
```

### Deploy

```
mkdir -p $HOME/ssh-agent-wsl-proxy
cp ssh-agent-wsl-proxy/bin/Release/net8.0/linux-x64/publish/ssh-agent-wsl-proxy $HOME/ssh-agent-wsl-proxy/
cp ssh-agent-wsl-proxy/bin/Release/net8.0/win-x64/publish/ssh-agent-wsl-proxy.exe $HOME/ssh-agent-wsl-proxy/
cp ssh-agent-wsl-proxy.service $HOME/ssh-agent-wsl-proxy/
```

### $HOME/.profile

```
SSH_AUTH_SOCK="${XDG_RUNTIME_DIR}/ssh-agent.socket"
export SSH_AUTH_SOCK
```

### Enable SystemD user service

```
sudo systemctl restart user@$(id -u).service
```

### Create SystemD user service

```
systemctl --user link $HOME/ssh-agent-wsl-proxy/ssh-agent-wsl-proxy.service
systemctl --user enable ssh-agent-wsl-proxy.service
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
