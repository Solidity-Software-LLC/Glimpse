## Overview

Glimpse is a modern looking and familiar Windows 11-like panel for Linux.  It was written to provide a high quality alternative to existing panels and to increase developer accessibility by leveraging modern tools and programming paradigms.  Its goal is to be an all-in-one package that minimizes dependencies on other libraries and components.

As of right now, Glimpse is tested on xUbuntu 22.04 but should work in other environments.

## Setup

1. Remove any org.kde.StatusNotifierWatcher hosts
   1. e.g., ayatana-indicator-application
2. Disable/remove existing panels
3. Free up the SUPER_L keyboard key
   1. e.g., Remove the Whiskermenu shortcut
   2. e.g., Disable xcape from remapping SUPER_L
      1. Goto Settings -> Session and Startup -> Application Autostart -> Uncheck Bind Super Key
4. Optional
   1. Add blur effects in your compositor
