# SSH agent WSL proxy

## Install

### Build

```
rm -rf Publish
dotnet publish ssh-agent-wsl-proxy --sc=true -r linux-x64 -c Release -o Publish/linux-x64 /property:PublishSingleFile=true
dotnet publish ssh-agent-wsl-proxy --sc=true -r win-x64 -c Release -o Publish/win-x64 /property:PublishSingleFile=true
```

### Deploy

```
rm -rf $HOME/ssh-agent-wsl-proxy/*
mkdir -p $HOME/ssh-agent-wsl-proxy
tar -C Publish -cf - . | tar -C $HOME/ssh-agent-wsl-proxy -xf -
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
