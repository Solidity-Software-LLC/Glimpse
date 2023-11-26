## Overview

Glimpse is a modern looking and familiar Windows 11-like panel for Linux.  It was written to provide a high quality alternative to existing panels and to increase developer accessibility by leveraging modern tools and programming paradigms.  Its goal is to be an all-in-one package that minimizes dependencies on other libraries and components.

As of right now, Glimpse is tested on xUbuntu 22.04 but should work in other environments.

## Installation

1. Run ```Glimpse install```.
2. Installation will do the following (see install.sh):
   * Installs xmlstarlet using apt-get.  It's used to modify the keyboard shortcuts XFCE XML config.
   * Copies the Glimpse binary into the ~/.local/bin/ directory.
   * Creates an autostarting desktop file so Glimpse launches when you login.
   * Updates the SUPER_L shortcut to open the Glimpse start menu instead of the whisker menu.
   * Disables the XFCE panel.
   * Kills the XFCE panel process.
   * Removes the ayatana-indicator-application package.  Glimpse handles status notifier items itself.
