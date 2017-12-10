---
title: Getting Started
sidebar: winnvm_100_sidebar
permalink: v100_getting_started.html
folder: v100/

---

## Prerequisite
1. Windows with .NET Framework 4 or later.

## Environment Variables.
The following environment variables should be present.

1. `NVM_HOME` -> This should be a folder name which already exists.
2. `NVM_SYMLINK` -> This should also be a folder name but only the parent folder should exists.

Add `NVM_SYMLINK` to you `PATH`

## Installation
Download [WinNvm](https://github.com/seenukarthi/winnvm/releases/download/v1.0.0/WinNvm_1.0.0.zip) and extract to a folder. Add the extracted folder to path.

## Testing the installation.
Open command prompt and try `winnvm -v` which should print the version.

{% include links.html %}
