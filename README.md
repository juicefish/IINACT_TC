![icon](https://github.com/marzent/IINACT/blob/main/images/icon.ico?raw=true)

# IINACT_TC

Dalamud repo url : `https://raw.githubusercontent.com/juicefish/IINACT_TC/main/repo.json`

為 [IINACT](https://github.com/marzent/IINACT) 搭配 FFXIV繁中版 的 [FFXIVTCLauncher](https://github.com/cycleapple/XIVTCLauncher/) 使用 Dalamud API 12 進行了一些更動

由於本人沒有玩過國際服以及使用ACT、Cactbot相關功能，目前沒有測試完全，有興趣的人可以接手

以下列出搭配測試的項目

## 安裝

請在Dalamud的第三方插件加入對應 repo url

Dalamud repo url : `https://raw.githubusercontent.com/juicefish/IINACT_TC/main/repo.json`

## 環境

Windows 10

[.NET 9.0 Runtime](https://dotnet.microsoft.com/zh-tw/download/dotnet/9.0) 理論上有用 [FFXIVTCLauncher](https://github.com/cycleapple/XIVTCLauncher/) 應該不需要再安裝

[Browsingway](https://github.com/Styr1x/Browsingway)
繁中可用，啟用後用/xllog看出錯訊息可查詢需安裝的 .Net 版本，印象中是6

[LMeter_TC](https://github.com/juicefish/LMeter_TC) LMeter @ Dalamud API 12 

| OverlayURI | 測試 | 用途 |
| :--- | :---: | :--- |
| Cactbot Configuration | :heavy_check_mark: |  |
| Cactbot DPS Xephero | :question: |  |
| Cactbot DPS Rdmty | :question: |  |
| Cactbot Eureka | :question: |  |
| Cactbot Fisher | :question: |  |
| Cactbot Jobs |  :heavy_check_mark:| 職能資訊? |
| Cactbot OopsyRaidsy | :question: |  |
| Cactbot PullCounter | :question: |  |
| Cactbot Radar | :heavy_check_mark: | 野生王方位, 含TTS |
| Cactbot Raidboss Alerts only | :question: |  |
| Cactbot Raidboss Combine | :question: |  |
| Cactbot Raidboss Timeline Only | :question: |  |
| Cactbot Test | :heavy_check_mark: | 偵錯資訊 |
| Ember Overlay | :question: |  |
| Ember SpellTimers | :question: |  |
| Horizoverlay | :question: |  |
| Ikegami | :heavy_check_mark: | DPS計數器 |
| Kagerou | :heavy_check_mark: | DPS計數器 |
| MopiMopi | :heavy_check_mark: | DPS計數器, 原介面韓文 |
| NextUI | :question: |  |
| Skyline | :heavy_check_mark: | DPS計數器 |

## 其他修改
| 路徑 | 修改 |
| :--- | :--- |
| Machina\Machina.csproj | .NET 9 |
| Machina.FFXIV\Machina.FFXIV.csproj | .NET 9 |
| Machina.FFXIV\Deucalion\DeucalionInjector.cs | 讓ACT使用Region.TC |
| Machina.FFXIV\Headers\Opcodes\OpcodeManager.cs | Debug message |
| Machina.FFXIV\Headers\Opcodes\TraditionalChinese.txt | OPCode Update |

## 參考
| 名稱 | 簡介 | Url |
| :--- | :--- | :--- |
| FFXIVTCLauncher | | https://github.com/cycleapple/XIVTCLauncher/ |
| FFXIVClientStructs |  | https://github.com/aers/FFXIVClientStructs |
| | | |
| FFXIV_ACT_Plugin |  | https://github.com/ravahn/FFXIV_ACT_Plugin |
| OverlayPlugin | | https://github.com/OverlayPlugin/OverlayPlugin |
| Cactbot |  | https://github.com/OverlayPlugin/cactbot/ |
| | | |
| IINACT | 原版IINACT | https://github.com/marzent/IINACT |
| Browsingway | 遊戲內瀏覽器 | https://github.com/Styr1x/Browsingway |
| LMeter | DPS計數器 | https://github.com/lichie567/LMeter |
| | | |
| Deucalion | ACT使用的反混淆 | https://github.com/ff14wed/deucalion |
| Unscrambler | IINACT使用的反混淆 | https://github.com/perchbirdd/Unscrambler |
| Unscrambler | CN | https://github.com/Latihas/Unscrambler  |
| | | |
| EXE Storage |  | https://github.com/extrant/FFXIV.EXE |
| Link Storage |  | https://karashiiro.github.io/xiv-resources/ |
| OPCode Storage |  | https://github.com/karashiiro/FFXIVOpcodes |
| OPCode Storage | 主要是CN的  | https://github.com/zhyupe/ffxiv-opcode-worker/ |
| | | |
| XivAlexander |  | https://github.com/Soreepeong/XivAlexander |
| OPCode Find | 使用XivAlexander尋找OPCode | https://github.com/Soreepeong/XivAlexander/wiki/How-to-find-opcodes |
| | | |
| OPCode Finder | Opcode program address | 	https://github.com/moewcorp/FFXIVNetworkOpcodes/ |
| OPCode Finder | Opcode for switch | https://github.com/nyaoouo/IdaFFxivOpcodes/ |
