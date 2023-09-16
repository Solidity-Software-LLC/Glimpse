## Overview

Glimpse is a modern looking and familiar Windows 11-like panel for Linux.  It was written to provide a high quality alternative to existing panels and to increase developer accessibility by leveraging modern tools and programming paradigms.  Its goal is to be an all-in-one package that minimizes dependencies on other libraries and components.

As of right now, Glimpse is tested on xUbuntu 22.04 but should work in other environments.

## Setup

1. Remove any org.kde.StatusNotifierWatcher hosts
   1. e.g., ayatana-indicator-application
2. Disable/remove existing panels
3. Bind a key (usually SUPER_L) to send the OpenStartMenu action to Glimpse
   1. In XFCE, this is under the keyboard settings
   2. Here's the command that I use
      3. ```gdbus call --session --dest org.solidity-software-llc.glimpse --object-path /org/solidity_software_llc/glimpse --method org.gtk.Actions.Activate  "OpenStartMenu" [] {}```
      4. Note that dbus-send doesn't support nested types so it can't call the Activate method.
4. Optional
   1. Add blur effects in your compositor
