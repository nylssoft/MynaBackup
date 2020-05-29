# Myna Backup

## Overview

An application to backup files for Windows using WPF with .NET Core and Entity Framework Core.

![screenshot](Screenshots/mynabackup.png)

## Installation

The latest MSI file can be found here: https://github.com/nylssoft/MynaBasckup/releases/download/V1.0.8/MynaBackup.msi

The program requires .NET Core 3.1 Desktop Runtime (v3.1.4) for x86,
see https://dotnet.microsoft.com/download/dotnet-core/thank-you/runtime-desktop-3.1.4-windows-x86-installer

## Features

* Backup of single file collections or directories into multiple destination directories
* Keeps history of changed files in destination directories, see .history subdirectory.
* Include and explude patterns to filter source files, see https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference
* Automatic backup hourly or daily
* Copies files only if the content hash of source files differs with the content hash of the destination files
* Tracks changes if the backup is based on a source file directory, e.g. if files are added or removed
* Supports languages German and English

## Screenshots

### Menu Items

#### File Menu Item

![File Menu Items Screenshot](Screenshots/mynabackup_file.png)

#### Edit Menu Item

![Edit Menu Items Screenshot](Screenshots/mynabackup_edit.png)

#### View Menu Item

![View Menu Items Screenshot](Screenshots/mynabackup_view.png)

### Create New Backup

![Create New Backup Screenshot](Screenshots/mynabackup_new.png)

### Rename Backup

![Rename Backup Screenshot](Screenshots/mynabackup_rename.png)

### Settings

![Settings Screenshot](Screenshots/mynabackup_settings.png)

### Overview

![Overview Screenshot](Screenshots/mynabackup_overview.png)

### Perform Backup

![Perform Backup Screenshot](Screenshots/mynabackup_backup.png)

### Copy Errors

![Copy Errors Screenshot](Screenshots/mynabackup_copyerrors.png)

## Build

- Build with VS 2019
- WiX ToolSet is required to build a MSI, see https://wixtoolset.org/

## Licenses

The following icons are used from the Open Icon Library (https://sourceforge.net/projects/openiconlibrary):

application-exit-5.png / nuovext2 / LGPL-2.1<br>
document-new-6.ico / oxygen / CC-BY-SA 3.0 or LGPL<br>
document-properties-2.png / gnome / GPLv2<br>
list-add-4.ico / oxygen / CC-BY-SA 3.0 or LGPL<br>
list-remove-4.ico / oxygen / CC-BY-SA 3.0 or LGPL<br>

TODO: add missing files
