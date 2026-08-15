Nocturne Detailed Skill Info
Version 1.0.0
=============================

Overview
--------
Nocturne Detailed Skill Info expands skill descriptions in
Shin Megami Tensei III Nocturne HD Remaster with verified numerical and
mechanical information.

Japanese and English are supported automatically by the same DLL.

The mod reads SMT3HD's own internal skill data at runtime and deliberately
avoids exposing unresolved raw values merely because they exist.

Displayed information
---------------------
- Damage power
- Practical skill accuracy
- Critical rate
- Recovery base value
- Known single-status ailment application values
- Verified multi-hit / multi-target attack counts
- Basic buff / debuff stage changes
- Verified corrections for special physical skills

Japanese example:
  突撃
  威力:41　命中:76　CT:24%

English example:
  Lunge
  Power:41  Accuracy:76  Crit:24%

Other verified examples:
  デスバウンド / Deathbound
  Power / 威力:38
  Accuracy / 命中:95
  Crit / CT:21%
  Maximum hit behavior shown with its verified single-enemy limit.

  ブギウギ / Boogie Woogie
  Power / 威力:8 per hit
  Accuracy / 命中:97
  Crit / CT:13%
  Hits / 回数:4

  ラクンダ / Rakunda
  Defense -1 stage / 防御:-1段階

Safety policy
-------------
The following remain hidden until their player-facing meaning is sufficiently
validated:
- MagicBase / MagicLimit
- Program special behavior
- unknown Hojo bits
- unknown / compound ailment masks
- other unresolved raw fields

Internal, reserve, placeholder, and enemy-only entries are filtered from
automatic Detailed Help generation.

Language support
----------------
Supported in 1.0.0:
- Japanese
- English

The mod follows the game's currently active localized skill-help text.
No separate Japanese or English DLL is required.

Requirements
------------
- Shin Megami Tensei III Nocturne HD Remaster (Steam)
- MelonLoader

Installation
------------
Copy:

NocturneDetailedSkillInfo.dll

to:

<SMT3HD>\Mods\NocturneDetailedSkillInfo.dll

If you used development versions, remove the old file:

SMT3DetailedPoC.dll

Do not load both DLLs at the same time.

Official source folder
----------------------
Canonical local development root:

C:\SMT3Modding\NocturneDetailedSkillInfo

Clean build + install in one paste
----------------------------------
cd C:\SMT3Modding\NocturneDetailedSkillInfo

Remove-Item .\bin -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\obj -Recurse -Force -ErrorAction SilentlyContinue

dotnet build `
".\NocturneDetailedSkillInfo.csproj" `
-c Release `
/p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\smt3hd"

if ($LASTEXITCODE -eq 0) {
    Copy-Item `
    ".\bin\Release\net6.0\NocturneDetailedSkillInfo.dll" `
    "C:\Program Files (x86)\Steam\steamapps\common\smt3hd\Mods\NocturneDetailedSkillInfo.dll" `
    -Force

    Remove-Item `
    "C:\Program Files (x86)\Steam\steamapps\common\smt3hd\Mods\SMT3DetailedPoC.dll" `
    -Force -ErrorAction SilentlyContinue

    Write-Host "Build + Install OK"
} else {
    Write-Host "Build failed - DLL was NOT copied"
}

Audit behavior
--------------
The localized full-audit implementation remains in the source for future
development and regression testing, but automatic audit export is disabled in
the public 1.0.0 release.

Modpack / collection inclusion
------------------------------
Modpack inclusion is welcome.

You may include Nocturne Detailed Skill Info in modpacks, compatibility
collections, installers, and similar community distributions under the MIT
License.

Please keep the MIT copyright/license notice with the software. Credit to
Gray Ghost and a link back to the original project page are appreciated so
users can find updates, source code, and documentation.

Forks, compatibility patches, translations, and integrations are also welcome
under the MIT License.

License scope
-------------
The source code in this project is released under the MIT License. See LICENSE.

The MIT License applies only to this project's own source code and packaged
software. It does not grant rights to Shin Megami Tensei III Nocturne HD
Remaster, ATLUS/SEGA assets, trademarks, game data, or other third-party
materials.

Credits / inspiration
---------------------
Project / SMT3HD implementation:
  Gray Ghost

Development and analysis assistance:
  ChatGPT

Inspired by Tyrant-Thanatos' "Detailed Skill Descriptions" mod for
Shin Megami Tensei V: Vengeance.

This SMT3HD mod is an independent implementation. It does not convert or
redistribute the original SMT5V mod files or assets. SMT3HD's own internal
skill data is read and interpreted at runtime.

No affiliation with or endorsement by the original mod author is implied.

Validation summary
------------------
Before the 1.0.0 release:
- Japanese localized skill audit: 512 rows reviewed
- English localized skill audit: 512 rows reviewed
- 150 validated Detailed Help changes in Japanese
- 150 validated Detailed Help changes in English
- reserve / placeholder false positives filtered
- verified special physical-skill behavior retained
- unresolved raw fields kept hidden
