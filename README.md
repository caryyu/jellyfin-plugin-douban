# Open Douban plugin for Jellyfin

This plugin is another implementation based on https://github.com/caryyu/douban-openapi-server without any `ApiKey` preset, which's originated from the plugin
 (https://github.com/Libitum/jellyfin-plugin-douban) and has already made a lot changes, ideally, these two plugins can coexist in Jellyfin, really appreciate the original author for providing a Douban-integrated plugin 

## Install Plugin

Only support `10.7.0-rc3` and above

Automatically:

- China: https://gitee.com/caryyu/jellyfin-plugin-repo/raw/master/manifest-cn.json
- U.S: https://github.com/caryyu/jellyfin-plugin-repo/raw/master/manifest-us.json

Manually:
 
```shell
dotnet build Jellyfin.Plugin.OpenDouban
```

Move `Jellyfin.Plugin.OpenDouban.dll` to `<jellyfin_config>/plugins/Jellyfin.Plugin.OpenDouban`

> Note: Don't forget the folder of `plugins/Jellyfin.Plugin.OpenDouban`, otherwise, Jellyfin won't load

## Docker Compose Reference

```yaml
version: "2"
services:
  doubanos:
    image: caryyu/douban-openapi-server:latest
    container_name: doubanos
    network_mode: "host"
    restart: "unless-stopped"

  jellyfin:
    image: jellyfin/jellyfin:10.7.5
    container_name: jellyfin
    user: <uid>:<gid>
    network_mode: "host"
    volumes:
      - <path>/jellyfin-config:/config
      - <path>/jellyfin-cache:/cache
      - <path>/media:/media
    restart: "unless-stopped"
```

# About deploy-exec

the `deploy-exec` script can be debugged by the following method:

```shell
docker run --rm -v $(pwd):/app -e SSH_PRIVKEY_GITHUB=$SSH_PRIVKEY_GITHUB -e SSH_PRIVKEY_GITEE=$SSH_PRIVKEY_GITEE -e TRAVIS_TAG=v1.0.3 --workdir /app caryyu-ubuntu:16.04 bash /app/deploy-exec
```

> ubuntu should have `git/jq/md5deep/zip` installed


<a href="https://www.buymeacoffee.com/caryyu" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/yellow_img.png" alt="Buy Me A Coffee" style="height: auto !important;width: auto !important;" ></a>

